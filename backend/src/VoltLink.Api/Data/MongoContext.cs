using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using VoltLink.Api.Configuration;
using VoltLink.Api.Models;

namespace VoltLink.Api.Data;


public class MongoContext
{
   
    private readonly Lazy<IMongoDatabase> _database;

 
    public MongoContext(IOptions<MongoDbSettings> options)
    {
       
        var settings = options.Value;

        _database = new Lazy<IMongoDatabase>(() =>
        {
            
            if (string.IsNullOrWhiteSpace(settings.ConnectionString))
            {
                throw new InvalidOperationException(
                    "MongoDb:ConnectionString is not configured. Set it with " +
                    "dotnet user-secrets set \"MongoDb:ConnectionString\" \"<your connection string>\"");
            }

            
            var client = new MongoClient(settings.ConnectionString);
            return client.GetDatabase(settings.DatabaseName);
        });
    }

   
    private IMongoDatabase Database => _database.Value;

  

    public IMongoCollection<User> Users =>
        Database.GetCollection<User>("users");

    public IMongoCollection<SolarStation> SolarStations =>
        Database.GetCollection<SolarStation>("solarStationInfo");

    public IMongoCollection<EnergyBookingSlot> BookingSlots =>
        Database.GetCollection<EnergyBookingSlot>("energyBookingSlots");

    public IMongoCollection<EnergyReservation> Reservations =>
        Database.GetCollection<EnergyReservation>("energyReservations");


    public async Task PingAsync(CancellationToken cancellationToken = default)
    {
        
        await Database.RunCommandAsync<BsonDocument>(
            new BsonDocument("ping", 1), cancellationToken: cancellationToken);
    }


    public async Task CreateIndexesAsync(CancellationToken cancellationToken = default)
    {
        
        await Users.Indexes.CreateOneAsync(
            new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(u => u.Email),
                new CreateIndexOptions { Name = "ux_users_email", Unique = true }),
            cancellationToken: cancellationToken);

        
        await Users.Indexes.CreateOneAsync(
            new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(u => u.Role).Ascending(u => u.IsActive),
                new CreateIndexOptions { Name = "ix_users_role_isActive" }),
            cancellationToken: cancellationToken);

        
        await SolarStations.Indexes.CreateOneAsync(
            new CreateIndexModel<SolarStation>(
                Builders<SolarStation>.IndexKeys.Ascending(s => s.Code),
                new CreateIndexOptions { Name = "ux_stations_code", Unique = true }),
            cancellationToken: cancellationToken);

      
        await SolarStations.Indexes.CreateOneAsync(
            new CreateIndexModel<SolarStation>(
                Builders<SolarStation>.IndexKeys.Geo2DSphere(s => s.Location),
                new CreateIndexOptions { Name = "ix_stations_location_2dsphere" }),
            cancellationToken: cancellationToken);

       
        await BookingSlots.Indexes.CreateOneAsync(
            new CreateIndexModel<EnergyBookingSlot>(
                Builders<EnergyBookingSlot>.IndexKeys
                    .Ascending(s => s.StationId)
                    .Ascending(s => s.StartTimeUtc),
                new CreateIndexOptions { Name = "ux_slots_station_start", Unique = true }),
            cancellationToken: cancellationToken);

        
        await Reservations.Indexes.CreateOneAsync(
            new CreateIndexModel<EnergyReservation>(
                Builders<EnergyReservation>.IndexKeys.Ascending(r => r.ReservationNo),
                new CreateIndexOptions { Name = "ux_reservations_no", Unique = true }),
            cancellationToken: cancellationToken);

       
        await Reservations.Indexes.CreateOneAsync(
            new CreateIndexModel<EnergyReservation>(
                Builders<EnergyReservation>.IndexKeys
                    .Ascending(r => r.ProsumerNic)
                    .Descending(r => r.ReservationStartUtc),
                new CreateIndexOptions { Name = "ix_reservations_nic_start" }),
            cancellationToken: cancellationToken);

        
        await Reservations.Indexes.CreateOneAsync(
            new CreateIndexModel<EnergyReservation>(
                Builders<EnergyReservation>.IndexKeys
                    .Ascending(r => r.StationId)
                    .Ascending(r => r.Status)
                    .Ascending(r => r.ReservationStartUtc),
                new CreateIndexOptions { Name = "ix_reservations_station_status_start" }),
            cancellationToken: cancellationToken);

        await CreateQrTokenIndexAsync(cancellationToken);
    }

    
    private const string QrTokenIndexName = "ux_reservations_qrToken";

    
    private async Task CreateQrTokenIndexAsync(CancellationToken cancellationToken)
    {
        var model = new CreateIndexModel<EnergyReservation>(
            Builders<EnergyReservation>.IndexKeys.Ascending(r => r.QrToken),
            new CreateIndexOptions<EnergyReservation>
            {
                Name = QrTokenIndexName,
                Unique = true,
                PartialFilterExpression =
                    new BsonDocument("qrToken", new BsonDocument("$type", "string"))
            });

        try
        {
            await Reservations.Indexes.CreateOneAsync(model, cancellationToken: cancellationToken);
        }
        catch (MongoCommandException ex) when (IsIndexConflict(ex))
        {
            
            await Reservations.Indexes.DropOneAsync(QrTokenIndexName, cancellationToken);
            await Reservations.Indexes.CreateOneAsync(model, cancellationToken: cancellationToken);
        }
    }

    
    private static bool IsIndexConflict(MongoCommandException exception)
    {
        if (exception.Code is 85 or 86)
        {
            return true;
        }

        if (exception.CodeName is "IndexOptionsConflict" or "IndexKeySpecsConflict")
        {
            return true;
        }

        return exception.Message.Contains("same name as the requested index",
            StringComparison.OrdinalIgnoreCase);
    }
}
