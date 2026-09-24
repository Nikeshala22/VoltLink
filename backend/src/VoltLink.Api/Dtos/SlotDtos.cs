using System.ComponentModel.DataAnnotations;

namespace VoltLink.Api.Dtos;


public record SlotResponse(
    string Id,
    string StationId,
    DateTime StartTimeUtc,
    DateTime EndTimeUtc,
    int Capacity,
    int BookedCount,
    int RemainingCapacity,
    double EnergyKwhPerSlot,
    bool IsActive);


public class CreateSlotRequest
{
    [Required(ErrorMessage = "Start time is required.")]
    public DateTime StartTimeUtc { get; set; }

    [Required(ErrorMessage = "End time is required.")]
    public DateTime EndTimeUtc { get; set; }

    [Range(1, 1000, ErrorMessage = "Capacity must be at least one.")]
    public int Capacity { get; set; } = 1;

    [Range(0.1, 100000, ErrorMessage = "Energy per slot must be greater than zero.")]
    public double EnergyKwhPerSlot { get; set; }
}


public class UpdateSlotRequest
{
    [Required(ErrorMessage = "Start time is required.")]
    public DateTime StartTimeUtc { get; set; }

    [Required(ErrorMessage = "End time is required.")]
    public DateTime EndTimeUtc { get; set; }

    [Range(1, 1000, ErrorMessage = "Capacity must be at least one.")]
    public int Capacity { get; set; } = 1;

    [Range(0.1, 100000, ErrorMessage = "Energy per slot must be greater than zero.")]
    public double EnergyKwhPerSlot { get; set; }


    public bool IsActive { get; set; } = true;
}
