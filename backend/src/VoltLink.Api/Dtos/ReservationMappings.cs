using VoltLink.Api.Models;

namespace VoltLink.Api.Dtos;


public static class ReservationMappings
{

    public static ReservationResponse ToResponse(
        this EnergyReservation reservation,
        string? prosumerName = null,
        string? stationName = null,
        DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;

      
        var isOpen = reservation.Status == ReservationStatus.Pending
                     || reservation.Status == ReservationStatus.Approved;

        var noticeDeadline = reservation.ReservationStartUtc
            .AddHours(-BusinessRules.MinChangeNoticeHours);

        var withinNotice = now < noticeDeadline;
        var changeable = isOpen && withinNotice;

        return new ReservationResponse(
            reservation.Id,
            reservation.ReservationNo,
            reservation.ProsumerNic,
            prosumerName,
            reservation.StationId,
            stationName,
            reservation.SlotId,
            reservation.ReservationStartUtc,
            reservation.ReservationEndUtc,
            reservation.EnergyKwh,
            reservation.Type,
            reservation.Status,
            CanBeModified: changeable,
            CanBeCancelled: changeable,
            HasQrCode: !string.IsNullOrWhiteSpace(reservation.QrToken)
                       && reservation.Status == ReservationStatus.Approved,
            reservation.CreatedAtUtc,
            reservation.CancelledAtUtc,
            reservation.CompletedAtUtc);
    }


    public static IReadOnlyList<ReservationResponse> ToResponseList(
        this IEnumerable<EnergyReservation> reservations,
        IReadOnlyDictionary<string, string>? prosumerNames = null,
        IReadOnlyDictionary<string, string>? stationNames = null)
    {
       
        var now = DateTime.UtcNow;

        return reservations.Select(r => r.ToResponse(
            prosumerName: LookUp(prosumerNames, r.ProsumerNic),
            stationName: LookUp(stationNames, r.StationId),
            nowUtc: now)).ToList();
    }

  
    private static string? LookUp(IReadOnlyDictionary<string, string>? source, string key)
    {
        if (source is null || string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        return source.TryGetValue(key, out var value) ? value : null;
    }
}
