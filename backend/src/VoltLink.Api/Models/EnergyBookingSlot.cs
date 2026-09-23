using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace VoltLink.Api.Models;


public class EnergyBookingSlot
{
   
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    
    [BsonElement("stationId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string StationId { get; set; } = string.Empty;

    
    [BsonElement("startTimeUtc")]
    public DateTime StartTimeUtc { get; set; }

    
    [BsonElement("endTimeUtc")]
    public DateTime EndTimeUtc { get; set; }

    
    [BsonElement("capacity")]
    public int Capacity { get; set; }

   
    [BsonElement("bookedCount")]
    public int BookedCount { get; set; }

    
    [BsonElement("energyKwhPerSlot")]
    public double EnergyKwhPerSlot { get; set; }


    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    
    [BsonElement("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; }

    [BsonElement("updatedAtUtc")]
    public DateTime UpdatedAtUtc { get; set; }
}
