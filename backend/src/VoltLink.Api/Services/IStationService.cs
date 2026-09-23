using VoltLink.Api.Dtos;

namespace VoltLink.Api.Services;

public interface IStationService
{
    
    Task<IReadOnlyList<StationResponse>> ListAsync(
        bool? isActive = null,
        string? city = null,
        string? search = null,
        CancellationToken cancellationToken = default);

  
    Task<StationResponse> GetByIdAsync(string id, CancellationToken cancellationToken = default);

  
    Task<IReadOnlyList<NearbyStationResponse>> FindNearbyAsync(
        double latitude,
        double longitude,
        double radiusKm,
        int limit,
        CancellationToken cancellationToken = default);

   
    Task<StationResponse> CreateAsync(
        CreateStationRequest request, CancellationToken cancellationToken = default);

     Task<StationResponse> UpdateAsync(
        string id, UpdateStationRequest request, CancellationToken cancellationToken = default);

  
    Task<StationResponse> ActivateAsync(string id, CancellationToken cancellationToken = default);


    Task<StationResponse> DeactivateAsync(string id, CancellationToken cancellationToken = default);

   
    Task<StationResponse> UpdateBatterySlotsAsync(
        string id, UpdateBatterySlotsRequest request, CancellationToken cancellationToken = default);
}
