using System.ComponentModel.DataAnnotations;

namespace VoltLink.Api.Dtos;


public record OperatingHoursDto(string OpenTime, string CloseTime);


public record StationResponse(
    string Id,
    string Code,
    string Name,
    string AddressLine,
    string City,
    double Latitude,
    double Longitude,
    double CapacityKwh,
    int TotalBatterySlots,
    int AvailableBatterySlots,
    OperatingHoursDto OperatingHours,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);


public record NearbyStationResponse(
    StationResponse Station,
    double DistanceMeters);


public class CreateStationRequest
{
    [Required(ErrorMessage = "Station code is required.")]
    [StringLength(30, MinimumLength = 2)]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Station name is required.")]
    [StringLength(120, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Address is required.")]
    public string AddressLine { get; set; } = string.Empty;

    [Required(ErrorMessage = "City is required.")]
    public string City { get; set; } = string.Empty;

    
    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
    public double Latitude { get; set; }

    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
    public double Longitude { get; set; }

    [Range(0.1, 100000, ErrorMessage = "Capacity must be greater than zero.")]
    public double CapacityKwh { get; set; }

    [Range(0, 10000, ErrorMessage = "Total battery slots cannot be negative.")]
    public int TotalBatterySlots { get; set; }


    [Range(0, 10000, ErrorMessage = "Available battery slots cannot be negative.")]
    public int? AvailableBatterySlots { get; set; }

    [RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "Open time must be in HH:mm format.")]
    public string OpenTime { get; set; } = "06:00";

    [RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "Close time must be in HH:mm format.")]
    public string CloseTime { get; set; } = "20:00";
}


public class UpdateStationRequest
{
    [Required(ErrorMessage = "Station name is required.")]
    [StringLength(120, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Address is required.")]
    public string AddressLine { get; set; } = string.Empty;

    [Required(ErrorMessage = "City is required.")]
    public string City { get; set; } = string.Empty;

    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
    public double Latitude { get; set; }

    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
    public double Longitude { get; set; }

    [Range(0.1, 100000, ErrorMessage = "Capacity must be greater than zero.")]
    public double CapacityKwh { get; set; }

    [Range(0, 10000, ErrorMessage = "Total battery slots cannot be negative.")]
    public int TotalBatterySlots { get; set; }

    [RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "Open time must be in HH:mm format.")]
    public string OpenTime { get; set; } = "06:00";

    [RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "Close time must be in HH:mm format.")]
    public string CloseTime { get; set; } = "20:00";
}


public class UpdateBatterySlotsRequest
{
    [Range(0, 10000, ErrorMessage = "Available battery slots cannot be negative.")]
    public int AvailableBatterySlots { get; set; }
}
