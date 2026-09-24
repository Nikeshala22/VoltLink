using MongoDB.Bson.Serialization.Attributes;

namespace VoltLink.Api.Models;

public class SolarStationWithDistance : SolarStation
{
   
    [BsonElement("distanceMeters")]
    public double DistanceMeters { get; set; }
}
