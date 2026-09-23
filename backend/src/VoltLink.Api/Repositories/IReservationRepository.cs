using VoltLink.Api.Models;

namespace VoltLink.Api.Repositories;


public class ReservationQuery
{

    public string? ProsumerNic { get; set; }


    public string? StationId { get; set; }


    public string? Status { get; set; }

    public string[]? Statuses { get; set; }

    public DateTime? FromUtc { get; set; }

  
    public DateTime? ToUtc { get; set; }

  

    public DateTime? CompletedFromUtc { get; set; }

    public DateTime? CompletedToUtc { get; set; }

    public string? Search { get; set; }

  
    public int Limit { get; set; } = 200;
}

public interface IReservationRepository
{
 
    Task<EnergyReservation?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

  
    Task<EnergyReservation?> GetByQrTokenAsync(
        string qrToken, CancellationToken cancellationToken = default);

 
    Task<IReadOnlyList<EnergyReservation>> SearchAsync(
        ReservationQuery query, CancellationToken cancellationToken = default);

   
    Task<long> CountAsync(
        ReservationQuery query, CancellationToken cancellationToken = default);


    Task<EnergyReservation?> GetNextUpcomingAsync(
        string prosumerNic, DateTime fromUtc, CancellationToken cancellationToken = default);

    Task<bool> ExistsActiveForProsumerAndSlotAsync(
        string prosumerNic,
        string slotId,
        string? excludeReservationId = null,
        CancellationToken cancellationToken = default);

   
    Task InsertAsync(EnergyReservation reservation, CancellationToken cancellationToken = default);

  
    Task ReplaceAsync(EnergyReservation reservation, CancellationToken cancellationToken = default);

   
    Task<EnergyReservation?> TryApproveAsync(
        string reservationId, string qrToken, DateTime nowUtc,
        CancellationToken cancellationToken = default);

   
    Task<EnergyReservation?> TryRejectAsync(
        string reservationId, DateTime nowUtc, CancellationToken cancellationToken = default);

 
    Task<EnergyReservation?> TryCancelAsync(
        string reservationId, DateTime nowUtc, CancellationToken cancellationToken = default);

  
    Task<EnergyReservation?> TryCompleteAsync(
        string reservationId, string operatorId, DateTime nowUtc,
        CancellationToken cancellationToken = default);

   
    Task<bool> HasActiveForStationAsync(
        string stationId, DateTime fromUtc, CancellationToken cancellationToken = default);

 
    Task<bool> HasActiveForSlotAsync(
        string slotId, CancellationToken cancellationToken = default);
}
