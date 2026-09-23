using MongoDB.Bson;
using MongoDB.Driver;
using VoltLink.Api.Data;
using VoltLink.Api.Models;

namespace VoltLink.Api.Repositories;


public class SlotRepository : ISlotRepository
{
    private readonly IMongoCollection<EnergyBookingSlot> _slots;

       private static readonly FilterDefinitionBuilder<EnergyBookingSlot> Filter =
        Builders<EnergyBookingSlot>.Filter;

    private static readonly UpdateDefinitionBuilder<EnergyBookingSlot> Update =
        Builders<EnergyBookingSlot>.Update;

       public SlotRepository(MongoContext context)
    {
        _slots = context.BookingSlots;
    }


    public async Task<EnergyBookingSlot?> GetByIdAsync(
        string id, CancellationToken cancellationToken = default)
    {
     
        if (!ObjectId.TryParse(id, out _))
        {
            return null;
        }

        return await _slots.Find(Filter.Eq(s => s.Id, id))
                           .FirstOrDefaultAsync(cancellationToken);
    }

  
    public async Task<IReadOnlyList<EnergyBookingSlot>> ListByStationAsync(
        string stationId,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(stationId, out _))
        {
            return Array.Empty<EnergyBookingSlot>();
        }

       
        var filters = new List<FilterDefinition<EnergyBookingSlot>>
        {
            Filter.Eq(s => s.StationId, stationId)
        };

        if (fromUtc.HasValue)
        {
            filters.Add(Filter.Gte(s => s.StartTimeUtc, fromUtc.Value));
        }

        if (toUtc.HasValue)
        {
            filters.Add(Filter.Lt(s => s.StartTimeUtc, toUtc.Value));
        }

        if (isActive.HasValue)
        {
            filters.Add(Filter.Eq(s => s.IsActive, isActive.Value));
        }

        return await _slots.Find(Filter.And(filters))
                           .SortBy(s => s.StartTimeUtc)
                           .ToListAsync(cancellationToken);
    }


  
    public async Task InsertAsync(
        EnergyBookingSlot slot, CancellationToken cancellationToken = default)
    {
        await _slots.InsertOneAsync(slot, cancellationToken: cancellationToken);
    }

    public async Task ReplaceAsync(
        EnergyBookingSlot slot, CancellationToken cancellationToken = default)
    {
        await _slots.ReplaceOneAsync(
            Filter.Eq(s => s.Id, slot.Id), slot, cancellationToken: cancellationToken);
    }


    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        await _slots.DeleteOneAsync(Filter.Eq(s => s.Id, id), cancellationToken);
    }

   
    public async Task<bool> ExistsAtStartAsync(
        string stationId,
        DateTime startTimeUtc,
        string? excludeSlotId = null,
        CancellationToken cancellationToken = default)
    {
        var filter = Filter.And(
            Filter.Eq(s => s.StationId, stationId),
            Filter.Eq(s => s.StartTimeUtc, startTimeUtc));

      
        if (!string.IsNullOrWhiteSpace(excludeSlotId))
        {
            filter = Filter.And(filter, Filter.Ne(s => s.Id, excludeSlotId));
        }

        return await _slots.Find(filter).AnyAsync(cancellationToken);
    }

    public async Task<EnergyBookingSlot?> TryClaimPlaceAsync(
        string slotId, CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(slotId, out _))
        {
            return null;
        }

        FilterDefinition<EnergyBookingSlot> hasFreePlace = new BsonDocument(
            "$expr", new BsonDocument("$lt", new BsonArray { "$bookedCount", "$capacity" }));

       
        var filter = Filter.And(
            Filter.Eq(s => s.Id, slotId),
            Filter.Eq(s => s.IsActive, true),
            hasFreePlace);

        var update = Update
            .Inc(s => s.BookedCount, 1)
            .Set(s => s.UpdatedAtUtc, DateTime.UtcNow);

        var options = new FindOneAndUpdateOptions<EnergyBookingSlot>
        {
            ReturnDocument = ReturnDocument.After
        };

       
        return await _slots.FindOneAndUpdateAsync(
            filter, update, options, cancellationToken);
    }


   
    public async Task ReleasePlaceAsync(
        string slotId, CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(slotId, out _))
        {
            return;
        }

  
        var filter = Filter.And(
            Filter.Eq(s => s.Id, slotId),
            Filter.Gt(s => s.BookedCount, 0));

        var update = Update
            .Inc(s => s.BookedCount, -1)
            .Set(s => s.UpdatedAtUtc, DateTime.UtcNow);

        await _slots.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }
}
