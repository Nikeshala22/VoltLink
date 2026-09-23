using System.ComponentModel.DataAnnotations;

namespace VoltLink.Api.Dtos;


public record ReservationResponse(
    string Id,
    string ReservationNo,
    string ProsumerNic,
    string? ProsumerName,
    string StationId,
    string? StationName,
    string SlotId,
    DateTime ReservationStartUtc,
    DateTime ReservationEndUtc,
    double EnergyKwh,
    string Type,
    string Status,
    bool CanBeModified,
    bool CanBeCancelled,
    bool HasQrCode,
    DateTime CreatedAtUtc,
    DateTime? CancelledAtUtc,
    DateTime? CompletedAtUtc);


public record ReservationSummaryResponse(
    string Action,
    string Message,
    ReservationResponse Reservation);

public class CreateReservationRequest
{
    [Required(ErrorMessage = "Slot identifier is required.")]
    public string SlotId { get; set; } = string.Empty;

    
    public string? ProsumerNic { get; set; }

    [Required(ErrorMessage = "Reservation type is required.")]
    public string Type { get; set; } = string.Empty;
}


public class UpdateReservationRequest
{
    [Required(ErrorMessage = "Slot identifier is required.")]
    public string SlotId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Reservation type is required.")]
    public string Type { get; set; } = string.Empty;
}


public record QrCodeResponse(
    string ReservationId,
    string ReservationNo,
    string Token,
    DateTime IssuedAtUtc,
    DateTime ReservationStartUtc);


public class VerifyQrRequest
{
    [Required(ErrorMessage = "Token is required.")]
    public string Token { get; set; } = string.Empty;
}


public record ProsumerDashboardResponse(
    string ProsumerNic,
    long PendingCount,
    long ApprovedFutureCount,
    long CompletedCount,
    long CancelledCount,
    ReservationResponse? NextReservation);


public record OperatorDashboardResponse(
    long PendingCount,
    long ApprovedFutureCount,
    long CompletedTodayCount,
    long ActiveStationCount,
    IReadOnlyList<ReservationResponse> TodaySchedule);
