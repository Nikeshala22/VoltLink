using VoltLink.Api.Models;

namespace VoltLink.Api.Dtos;


public static class StationMappings
{
   
    public static StationResponse ToResponse(this SolarStation station)
    {
        
        var longitude = station.Location?.Coordinates?.Longitude ?? 0d;
        var latitude = station.Location?.Coordinates?.Latitude ?? 0d;

        return new StationResponse(
            station.Id,
            station.Code,
            station.Name,
            station.AddressLine,
            station.City,
            latitude,
            longitude,
            station.CapacityKwh,
            station.TotalBatterySlots,
            station.AvailableBatterySlots,
            new OperatingHoursDto(station.OperatingHours.OpenTime, station.OperatingHours.CloseTime),
            station.IsActive,
            station.CreatedAtUtc,
            station.UpdatedAtUtc);
    }

 
    public static IReadOnlyList<StationResponse> ToResponseList(this IEnumerable<SolarStation> stations)
    {
        return stations.Select(s => s.ToResponse()).ToList();
    }

  
    public static SlotResponse ToResponse(this EnergyBookingSlot slot)
    {
        
        var remaining = Math.Max(0, slot.Capacity - slot.BookedCount);

        return new SlotResponse(
            slot.Id,
            slot.StationId,
            slot.StartTimeUtc,
            slot.EndTimeUtc,
            slot.Capacity,
            slot.BookedCount,
            remaining,
            slot.EnergyKwhPerSlot,
            slot.IsActive);
    }

   
    public static IReadOnlyList<SlotResponse> ToResponseList(this IEnumerable<EnergyBookingSlot> slots)
    {
        return slots.Select(s => s.ToResponse()).ToList();
    }
}
