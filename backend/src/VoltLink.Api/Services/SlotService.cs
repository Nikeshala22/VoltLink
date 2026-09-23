using VoltLink.Api.Dtos;
using VoltLink.Api.Middleware;
using VoltLink.Api.Models;
using VoltLink.Api.Repositories;

namespace VoltLink.Api.Services;


public class SlotService : ISlotService
{
    private readonly ISlotRepository _slots;
    private readonly IStationRepository _stations;
    private readonly IReservationRepository _reservations;
    private readonly ILogger<SlotService> _logger;


    public SlotService(
        ISlotRepository slots,
        IStationRepository stations,
        IReservationRepository reservations,
        ILogger<SlotService> logger)
    {
        _slots = slots;
        _stations = stations;
        _reservations = reservations;
        _logger = logger;
    }

   
    public async Task<IReadOnlyList<SlotResponse>> ListByStationAsync(
        string stationId,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        
        await GetRequiredStationAsync(stationId, cancellationToken);

        var slots = await _slots.ListByStationAsync(
            stationId, fromUtc, toUtc, isActive, cancellationToken);

        return slots.ToResponseList();
    }

       public async Task<SlotResponse> GetByIdAsync(
        string id, CancellationToken cancellationToken = default)
    {
        var slot = await GetRequiredSlotAsync(id, cancellationToken);
        return slot.ToResponse();
    }


    public async Task<SlotResponse> CreateAsync(
        string stationId, CreateSlotRequest request, CancellationToken cancellationToken = default)
    {
        var station = await GetRequiredStationAsync(stationId, cancellationToken);

       
        if (!station.IsActive)
        {
            throw new BusinessRuleViolationException(
                ErrorCodes.StationInactive,
                "Booking windows cannot be added to a station that is not active.");
        }

        
        var startUtc = NormaliseToUtc(request.StartTimeUtc);
        var endUtc = NormaliseToUtc(request.EndTimeUtc);

        EnsureWindowValid(startUtc, endUtc);

       
        if (await _slots.ExistsAtStartAsync(stationId, startUtc, cancellationToken: cancellationToken))
        {
            throw new ConflictException(
                ErrorCodes.SlotHasBookings,
                $"This station already offers a booking window starting at {startUtc:u}.");
        }

        var now = DateTime.UtcNow;
        var slot = new EnergyBookingSlot
        {
            StationId = stationId,
            StartTimeUtc = startUtc,
            EndTimeUtc = endUtc,
            Capacity = request.Capacity,
            BookedCount = 0,
            EnergyKwhPerSlot = request.EnergyKwhPerSlot,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await _slots.InsertAsync(slot, cancellationToken);
        _logger.LogInformation(
            "Booking window created at station {StationId} for {Start:u}.", stationId, startUtc);

        return slot.ToResponse();
    }

  
    public async Task<SlotResponse> UpdateAsync(
        string id, UpdateSlotRequest request, CancellationToken cancellationToken = default)
    {
        var slot = await GetRequiredSlotAsync(id, cancellationToken);

        var startUtc = NormaliseToUtc(request.StartTimeUtc);
        var endUtc = NormaliseToUtc(request.EndTimeUtc);

        EnsureWindowValid(startUtc, endUtc);

       
        if (request.Capacity < slot.BookedCount)
        {
            throw new BusinessRuleViolationException(
                ErrorCodes.SlotHasBookings,
                $"Capacity cannot be reduced to {request.Capacity} because " +
                $"{slot.BookedCount} place(s) are already booked.");
        }

     
        if (startUtc != slot.StartTimeUtc &&
            await _slots.ExistsAtStartAsync(slot.StationId, startUtc, slot.Id, cancellationToken))
        {
            throw new ConflictException(
                ErrorCodes.SlotHasBookings,
                $"This station already offers a booking window starting at {startUtc:u}.");
        }

        slot.StartTimeUtc = startUtc;
        slot.EndTimeUtc = endUtc;
        slot.Capacity = request.Capacity;
        slot.EnergyKwhPerSlot = request.EnergyKwhPerSlot;
        slot.IsActive = request.IsActive;
        slot.UpdatedAtUtc = DateTime.UtcNow;

        await _slots.ReplaceAsync(slot, cancellationToken);
        return slot.ToResponse();
    }

   
    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var slot = await GetRequiredSlotAsync(id, cancellationToken);

        
        if (await _reservations.HasActiveForSlotAsync(slot.Id, cancellationToken))
        {
            throw new BusinessRuleViolationException(
                ErrorCodes.SlotHasBookings,
                "This booking window cannot be deleted because prosumers still hold " +
                "active reservations for it. Cancel those reservations first.");
        }

        await _slots.DeleteAsync(slot.Id, cancellationToken);
        _logger.LogInformation("Booking window {SlotId} deleted.", slot.Id);
    }

  
    private static void EnsureWindowValid(DateTime startUtc, DateTime endUtc)
    {
        if (endUtc <= startUtc)
        {
            throw new ValidationException("The window end time must be after its start time.");
        }

        // A transfer window longer than a day almost certainly means the caller
        // sent the wrong date, so reject it rather than store it.
        if (endUtc - startUtc > TimeSpan.FromHours(24))
        {
            throw new ValidationException("A booking window cannot be longer than 24 hours.");
        }
    }

  
    private static DateTime NormaliseToUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,

           
            DateTimeKind.Local => value.ToUniversalTime(),

          
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }


    private async Task<SolarStation> GetRequiredStationAsync(
        string stationId, CancellationToken cancellationToken)
    {
        var station = await _stations.GetByIdAsync(stationId, cancellationToken);

        if (station is null)
        {
            throw new NotFoundException($"No station was found with identifier '{stationId}'.");
        }

        return station;
    }

  
    private async Task<EnergyBookingSlot> GetRequiredSlotAsync(
        string id, CancellationToken cancellationToken)
    {
        var slot = await _slots.GetByIdAsync(id, cancellationToken);

        if (slot is null)
        {
            throw new NotFoundException($"No booking window was found with identifier '{id}'.");
        }

        return slot;
    }
}
