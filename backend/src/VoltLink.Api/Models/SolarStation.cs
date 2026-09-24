using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.GeoJsonObjectModel;

namespace VoltLink.Api.Models;

public class OperatingHours
{
    [BsonElement("openTime")]
    public string OpenTime { get; set; } = "06:00";

    [BsonElement("closeTime")]
    public string CloseTime { get; set; } = "20:00";
}


public class SolarStation
{
    
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    
    [BsonElement("code")]
    public string Code { get; set; } = string.Empty;

   
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    
    [BsonElement("addressLine")]
    public string AddressLine { get; set; } = string.Empty;

  
    [BsonElement("city")]
    public string City { get; set; } = string.Empty;

    
    [BsonElement("location")]
    public GeoJsonPoint<GeoJson2DGeographicCoordinates> Location { get; set; } = default!;

    
    [BsonElement("capacityKwh")]
    public double CapacityKwh { get; set; }

   
    [BsonElement("totalBatterySlots")]
    public int TotalBatterySlots { get; set; }

    
    [BsonElement("availableBatterySlots")]
    public int AvailableBatterySlots { get; set; }

   
    [BsonElement("operatingHours")]
    public OperatingHours OperatingHours { get; set; } = new();

   
    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

   
    [BsonElement("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; }

    [BsonElement("updatedAtUtc")]
    public DateTime UpdatedAtUtc { get; set; }
}
