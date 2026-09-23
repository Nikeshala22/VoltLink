using VoltLink.Api.Dtos;

namespace VoltLink.Api.Services;


public interface ISlotService
{
    
    Task<IReadOnlyList<SlotResponse>> ListByStationAsync(
        string stationId,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default);

   
    Task<SlotResponse> GetByIdAsync(string id, CancellationToken cancellationToken = default);

  
    Task<SlotResponse> CreateAsync(
        string stationId, CreateSlotRequest request, CancellationToken cancellationToken = default);

   
    Task<SlotResponse> UpdateAsync(
        string id, UpdateSlotRequest request, CancellationToken cancellationToken = default);

  
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}
