
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace VoltLink.Api.Models;


public class User
{
   
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;

   
    [BsonElement("fullName")]
    public string FullName { get; set; } = string.Empty;

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;


    [BsonElement("phone")]
    public string? Phone { get; set; }

   
    [BsonElement("address")]
    public string? Address { get; set; }

    [BsonElement("passwordHash")]
    public string PasswordHash { get; set; } = string.Empty;


    [BsonElement("role")]
    public string Role { get; set; } = UserRoles.Prosumer;


    [BsonElement("isActive")]
    public bool IsActive { get; set; }

    [BsonElement("deactivationRequested")]
    public bool DeactivationRequested { get; set; }

    [BsonElement("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; }

    [BsonElement("updatedAtUtc")]
    public DateTime UpdatedAtUtc { get; set; }
}
