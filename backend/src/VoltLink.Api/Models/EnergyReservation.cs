using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace VoltLink.Api.Models;


public class EnergyReservation
{
  
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("reservationNo")]
    public string ReservationNo { get; set; } = string.Empty;

    [BsonElement("prosumerNic")]
    public string ProsumerNic { get; set; } = string.Empty;

   
    [BsonElement("stationId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string StationId { get; set; } = string.Empty;

  
    [BsonElement("slotId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string SlotId { get; set; } = string.Empty;

 
    [BsonElement("reservationStartUtc")]
    public DateTime ReservationStartUtc { get; set; }

    [BsonElement("reservationEndUtc")]
    public DateTime ReservationEndUtc { get; set; }


    [BsonElement("energyKwh")]
    public double EnergyKwh { get; set; }


    [BsonElement("type")]
    public string Type { get; set; } = ReservationType.Injection;

    [BsonElement("status")]
    public string Status { get; set; } = ReservationStatus.Pending;

   
    [BsonElement("qrToken")]
    [BsonIgnoreIfNull]
    public string? QrToken { get; set; }

    [BsonElement("qrIssuedAtUtc")]
    public DateTime? QrIssuedAtUtc { get; set; }


    [BsonElement("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; }

    [BsonElement("updatedAtUtc")]
    public DateTime UpdatedAtUtc { get; set; }

    [BsonElement("cancelledAtUtc")]
    public DateTime? CancelledAtUtc { get; set; }

    [BsonElement("completedAtUtc")]
    public DateTime? CompletedAtUtc { get; set; }

    [BsonElement("completedByOperatorId")]
    public string? CompletedByOperatorId { get; set; }
}
