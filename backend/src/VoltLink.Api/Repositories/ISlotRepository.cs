
using VoltLink.Api.Models;

namespace VoltLink.Api.Repositories;

public interface ISlotRepository
{
 
    Task<EnergyBookingSlot?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

   
    Task<IReadOnlyList<EnergyBookingSlot>> ListByStationAsync(
        string stationId,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default);

    Task InsertAsync(EnergyBookingSlot slot, CancellationToken cancellationToken = default);

  
    Task ReplaceAsync(EnergyBookingSlot slot, CancellationToken cancellationToken = default);

  
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);


    Task<bool> ExistsAtStartAsync(
        string stationId,
        DateTime startTimeUtc,
        string? excludeSlotId = null,
        CancellationToken cancellationToken = default);

   
    Task<EnergyBookingSlot?> TryClaimPlaceAsync(
        string slotId, CancellationToken cancellationToken = default);


    Task ReleasePlaceAsync(string slotId, CancellationToken cancellationToken = default);
}
