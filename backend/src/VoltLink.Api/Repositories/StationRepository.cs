using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;
using VoltLink.Api.Data;
using VoltLink.Api.Models;

namespace VoltLink.Api.Repositories;


public class StationRepository : IStationRepository
{
    private readonly IMongoCollection<SolarStation> _stations;

   
    private static readonly FilterDefinitionBuilder<SolarStation> Filter =
        Builders<SolarStation>.Filter;

   
    public StationRepository(MongoContext context)
    {
        _stations = context.SolarStations;
    }

    public async Task<SolarStation?> GetByIdAsync(
        string id, CancellationToken cancellationToken = default)
    {
       
        if (!ObjectId.TryParse(id, out _))
        {
            return null;
        }

        return await _stations.Find(Filter.Eq(s => s.Id, id))
                              .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<SolarStation?> GetByCodeAsync(
        string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var normalised = code.Trim().ToUpperInvariant();

        return await _stations.Find(Filter.Eq(s => s.Code, normalised))
                              .FirstOrDefaultAsync(cancellationToken);
    }

   
    public async Task<IReadOnlyList<SolarStation>> ListAsync(
        bool? isActive = null,
        string? city = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        
        var filters = new List<FilterDefinition<SolarStation>> { Filter.Empty };

        if (isActive.HasValue)
        {
            filters.Add(Filter.Eq(s => s.IsActive, isActive.Value));
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            
            filters.Add(Filter.Regex(s => s.City,
                new BsonRegularExpression($"^{Regex.Escape(city.Trim())}$", "i")));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
          
            var pattern = new BsonRegularExpression(Regex.Escape(search.Trim()), "i");

            filters.Add(Filter.Or(
                Filter.Regex(s => s.Code, pattern),
                Filter.Regex(s => s.Name, pattern),
                Filter.Regex(s => s.AddressLine, pattern)));
        }

        return await _stations.Find(Filter.And(filters))
                              .SortBy(s => s.Name)
                              .ToListAsync(cancellationToken);
    }

   
    public async Task<IReadOnlyList<SolarStationWithDistance>> FindNearbyAsync(
        double latitude,
        double longitude,
        double radiusMetres,
        int limit,
        CancellationToken cancellationToken = default)
    {
        
        var geoNear = new BsonDocument("$geoNear", new BsonDocument
        {
            {
                "near", new BsonDocument
                {
                    { "type", "Point" },
                    { "coordinates", new BsonArray { longitude, latitude } }
                }
            },
           
            { "distanceField", "distanceMeters" },
            { "maxDistance", radiusMetres },

       
            { "spherical", true },

           
            { "query", new BsonDocument("isActive", true) }
        });

      
        var limitStage = new BsonDocument("$limit", limit);

        var pipeline = PipelineDefinition<SolarStation, SolarStationWithDistance>
            .Create(geoNear, limitStage);

        var cursor = await _stations.AggregateAsync(
            pipeline, cancellationToken: cancellationToken);

        return await cursor.ToListAsync(cancellationToken);
    }


    public async Task InsertAsync(
        SolarStation station, CancellationToken cancellationToken = default)
    {
        await _stations.InsertOneAsync(station, cancellationToken: cancellationToken);
    }

      public async Task ReplaceAsync(
        SolarStation station, CancellationToken cancellationToken = default)
    {
        await _stations.ReplaceOneAsync(
            Filter.Eq(s => s.Id, station.Id), station, cancellationToken: cancellationToken);
    }

   
    public async Task<bool> CodeExistsAsync(
        string code, string? excludeStationId = null, CancellationToken cancellationToken = default)
    {
        var normalised = code.Trim().ToUpperInvariant();
        var filter = Filter.Eq(s => s.Code, normalised);

      
        if (!string.IsNullOrWhiteSpace(excludeStationId))
        {
            filter = Filter.And(filter, Filter.Ne(s => s.Id, excludeStationId));
        }

        return await _stations.Find(filter).AnyAsync(cancellationToken);
    }
}
