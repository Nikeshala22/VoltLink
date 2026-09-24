using MongoDB.Driver.GeoJsonObjectModel;
using VoltLink.Api.Dtos;
using VoltLink.Api.Middleware;
using VoltLink.Api.Models;
using VoltLink.Api.Repositories;

namespace VoltLink.Api.Services;


public class StationService : IStationService
{
    private readonly IStationRepository _stations;
    private readonly IReservationRepository _reservations;
    private readonly ILogger<StationService> _logger;

   
    private const double MaxSearchRadiusKm = 200d;

 
    private const int MaxNearbyResults = 100;

  
    public StationService(
        IStationRepository stations,
        IReservationRepository reservations,
        ILogger<StationService> logger)
    {
        _stations = stations;
        _reservations = reservations;
        _logger = logger;
    }

    
    public async Task<IReadOnlyList<StationResponse>> ListAsync(
        bool? isActive = null,
        string? city = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var stations = await _stations.ListAsync(isActive, city, search, cancellationToken);
        return stations.ToResponseList();
    }

   
    public async Task<StationResponse> GetByIdAsync(
        string id, CancellationToken cancellationToken = default)
    {
        var station = await GetRequiredAsync(id, cancellationToken);
        return station.ToResponse();
    }

    
    public async Task<IReadOnlyList<NearbyStationResponse>> FindNearbyAsync(
        double latitude,
        double longitude,
        double radiusKm,
        int limit,
        CancellationToken cancellationToken = default)
    {
       
        if (latitude is < -90 or > 90)
        {
            throw new ValidationException("Latitude must be between -90 and 90.");
        }

        if (longitude is < -180 or > 180)
        {
            throw new ValidationException("Longitude must be between -180 and 180.");
        }

        if (radiusKm <= 0)
        {
            throw new ValidationException("Radius must be greater than zero.");
        }

     
        var effectiveRadiusKm = Math.Min(radiusKm, MaxSearchRadiusKm);
        var effectiveLimit = limit <= 0 ? MaxNearbyResults : Math.Min(limit, MaxNearbyResults);

        
        var results = await _stations.FindNearbyAsync(
            latitude, longitude, effectiveRadiusKm * 1000d, effectiveLimit, cancellationToken);

        return results
            .Select(s => new NearbyStationResponse(s.ToResponse(), Math.Round(s.DistanceMeters, 1)))
            .ToList();
    }

   
    public async Task<StationResponse> CreateAsync(
        CreateStationRequest request, CancellationToken cancellationToken = default)
    {
        var code = request.Code.Trim().ToUpperInvariant();

        
        if (await _stations.CodeExistsAsync(code, cancellationToken: cancellationToken))
        {
            throw new ConflictException(
                ErrorCodes.StationCodeAlreadyUsed,
                $"A station already exists with code {code}.");
        }

      
        var available = request.AvailableBatterySlots ?? request.TotalBatterySlots;

        
        if (available > request.TotalBatterySlots)
        {
            throw new ValidationException(
                "Available battery slots cannot exceed the total number of battery slots.");
        }

        EnsureOperatingHoursValid(request.OpenTime, request.CloseTime);

        var now = DateTime.UtcNow;
        var station = new SolarStation
        {
            Code = code,
            Name = request.Name.Trim(),
            AddressLine = request.AddressLine.Trim(),
            City = request.City.Trim(),
            Location = BuildPoint(request.Latitude, request.Longitude),
            CapacityKwh = request.CapacityKwh,
            TotalBatterySlots = request.TotalBatterySlots,
            AvailableBatterySlots = available,
            OperatingHours = new OperatingHours
            {
                OpenTime = request.OpenTime,
                CloseTime = request.CloseTime
            },
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await _stations.InsertAsync(station, cancellationToken);
        _logger.LogInformation("Station {Code} created.", code);

        return station.ToResponse();
    }

   
    public async Task<StationResponse> UpdateAsync(
        string id, UpdateStationRequest request, CancellationToken cancellationToken = default)
    {
        var station = await GetRequiredAsync(id, cancellationToken);

        EnsureOperatingHoursValid(request.OpenTime, request.CloseTime);

       
        var available = Math.Min(station.AvailableBatterySlots, request.TotalBatterySlots);

        station.Name = request.Name.Trim();
        station.AddressLine = request.AddressLine.Trim();
        station.City = request.City.Trim();
        station.Location = BuildPoint(request.Latitude, request.Longitude);
        station.CapacityKwh = request.CapacityKwh;
        station.TotalBatterySlots = request.TotalBatterySlots;
        station.AvailableBatterySlots = available;
        station.OperatingHours = new OperatingHours
        {
            OpenTime = request.OpenTime,
            CloseTime = request.CloseTime
        };
        station.UpdatedAtUtc = DateTime.UtcNow;

        await _stations.ReplaceAsync(station, cancellationToken);
        return station.ToResponse();
    }

    
    public async Task<StationResponse> ActivateAsync(
        string id, CancellationToken cancellationToken = default)
    {
        var station = await GetRequiredAsync(id, cancellationToken);

        station.IsActive = true;
        station.UpdatedAtUtc = DateTime.UtcNow;

        await _stations.ReplaceAsync(station, cancellationToken);
        _logger.LogInformation("Station {Code} activated.", station.Code);

        return station.ToResponse();
    }

   
    public async Task<StationResponse> DeactivateAsync(
        string id, CancellationToken cancellationToken = default)
    {
        var station = await GetRequiredAsync(id, cancellationToken);

        
        var hasActive = await _reservations.HasActiveForStationAsync(
            station.Id, DateTime.UtcNow, cancellationToken);

        if (hasActive)
        {
            throw new BusinessRuleViolationException(
                ErrorCodes.StationHasActiveReservations,
                "This station cannot be deactivated because it still has active energy " +
                "reservations. Cancel or complete them first.");
        }

        station.IsActive = false;
        station.UpdatedAtUtc = DateTime.UtcNow;

        await _stations.ReplaceAsync(station, cancellationToken);
        _logger.LogInformation("Station {Code} deactivated.", station.Code);

        return station.ToResponse();
    }


    public async Task<StationResponse> UpdateBatterySlotsAsync(
        string id, UpdateBatterySlotsRequest request, CancellationToken cancellationToken = default)
    {
        var station = await GetRequiredAsync(id, cancellationToken);

        // The free count can never exceed the number physically installed.
        if (request.AvailableBatterySlots > station.TotalBatterySlots)
        {
            throw new ValidationException(
                $"Available battery slots cannot exceed the installed total of " +
                $"{station.TotalBatterySlots}.");
        }

        station.AvailableBatterySlots = request.AvailableBatterySlots;
        station.UpdatedAtUtc = DateTime.UtcNow;

        await _stations.ReplaceAsync(station, cancellationToken);
        return station.ToResponse();
    }

    
    private static GeoJsonPoint<GeoJson2DGeographicCoordinates> BuildPoint(
        double latitude, double longitude)
    {
        // GeoJson.Geographic takes longitude first, then latitude.
        return GeoJson.Point(GeoJson.Geographic(longitude, latitude));
    }

  
    private static void EnsureOperatingHoursValid(string openTime, string closeTime)
    {
        // The format itself is checked by the request annotations; this is the
        // ordering rule those annotations cannot express.
        if (string.CompareOrdinal(openTime, closeTime) >= 0)
        {
            throw new ValidationException("Close time must be later than open time.");
        }
    }

  
    private async Task<SolarStation> GetRequiredAsync(string id, CancellationToken cancellationToken)
    {
        var station = await _stations.GetByIdAsync(id, cancellationToken);

        if (station is null)
        {
            throw new NotFoundException($"No station was found with identifier '{id}'.");
        }

        return station;
    }
}
