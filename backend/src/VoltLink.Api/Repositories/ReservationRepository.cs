
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;
using VoltLink.Api.Data;
using VoltLink.Api.Models;

namespace VoltLink.Api.Repositories;

public class ReservationRepository : IReservationRepository
{
    private readonly IMongoCollection<EnergyReservation> _reservations;

    private static readonly FilterDefinitionBuilder<EnergyReservation> Filter =
        Builders<EnergyReservation>.Filter;

    private static readonly UpdateDefinitionBuilder<EnergyReservation> Update =
        Builders<EnergyReservation>.Update;

    private static readonly FindOneAndUpdateOptions<EnergyReservation> ReturnUpdated =
        new() { ReturnDocument = ReturnDocument.After };

    
    public ReservationRepository(MongoContext context)
    {
        _reservations = context.Reservations;
    }

    public async Task<EnergyReservation?> GetByIdAsync(
        string id, CancellationToken cancellationToken = default)
    {
       
        if (!ObjectId.TryParse(id, out _))
        {
            return null;
        }

        return await _reservations.Find(Filter.Eq(r => r.Id, id))
                                  .FirstOrDefaultAsync(cancellationToken);
    }


    public async Task<EnergyReservation?> GetByQrTokenAsync(
        string qrToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(qrToken))
        {
            return null;
        }

        return await _reservations.Find(Filter.Eq(r => r.QrToken, qrToken))
                                  .FirstOrDefaultAsync(cancellationToken);
    }

      public async Task<IReadOnlyList<EnergyReservation>> SearchAsync(
        ReservationQuery query, CancellationToken cancellationToken = default)
    {
        var filter = BuildFilter(query);

        
        return await _reservations.Find(filter)
                                  .SortByDescending(r => r.ReservationStartUtc)
                                  .Limit(query.Limit <= 0 ? 200 : query.Limit)
                                  .ToListAsync(cancellationToken);
    }

   
    public async Task<long> CountAsync(
        ReservationQuery query, CancellationToken cancellationToken = default)
    {
        var filter = BuildFilter(query);
        return await _reservations.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }

 
    public async Task<EnergyReservation?> GetNextUpcomingAsync(
        string prosumerNic, DateTime fromUtc, CancellationToken cancellationToken = default)
    {
        var filter = Filter.And(
            Filter.Eq(r => r.ProsumerNic, prosumerNic),
            Filter.In(r => r.Status, ReservationStatus.Active),
            Filter.Gte(r => r.ReservationStartUtc, fromUtc));

        // Ascending order so the first result is the soonest booking.
        return await _reservations.Find(filter)
                                  .SortBy(r => r.ReservationStartUtc)
                                  .FirstOrDefaultAsync(cancellationToken);
    }

   
    public async Task<bool> ExistsActiveForProsumerAndSlotAsync(
        string prosumerNic,
        string slotId,
        string? excludeReservationId = null,
        CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(slotId, out _))
        {
            return false;
        }

        var filter = Filter.And(
            Filter.Eq(r => r.ProsumerNic, prosumerNic),
            Filter.Eq(r => r.SlotId, slotId),
            Filter.In(r => r.Status, ReservationStatus.Active));

       
        if (!string.IsNullOrWhiteSpace(excludeReservationId))
        {
            filter = Filter.And(filter, Filter.Ne(r => r.Id, excludeReservationId));
        }

        return await _reservations.Find(filter).AnyAsync(cancellationToken);
    }


    public async Task InsertAsync(
        EnergyReservation reservation, CancellationToken cancellationToken = default)
    {
        await _reservations.InsertOneAsync(reservation, cancellationToken: cancellationToken);
    }


    public async Task ReplaceAsync(
        EnergyReservation reservation, CancellationToken cancellationToken = default)
    {
        await _reservations.ReplaceOneAsync(
            Filter.Eq(r => r.Id, reservation.Id), reservation,
            cancellationToken: cancellationToken);
    }

   
    public async Task<EnergyReservation?> TryApproveAsync(
        string reservationId, string qrToken, DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
       
        var filter = Filter.And(
            Filter.Eq(r => r.Id, reservationId),
            Filter.Eq(r => r.Status, ReservationStatus.Pending));

        var update = Update
            .Set(r => r.Status, ReservationStatus.Approved)
            .Set(r => r.QrToken, qrToken)
            .Set(r => r.QrIssuedAtUtc, nowUtc)
            .Set(r => r.UpdatedAtUtc, nowUtc);

        return await _reservations.FindOneAndUpdateAsync(
            filter, update, ReturnUpdated, cancellationToken);
    }

  
    public async Task<EnergyReservation?> TryRejectAsync(
        string reservationId, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var filter = Filter.And(
            Filter.Eq(r => r.Id, reservationId),
            Filter.Eq(r => r.Status, ReservationStatus.Pending));

        var update = Update
            .Set(r => r.Status, ReservationStatus.Rejected)
            .Set(r => r.UpdatedAtUtc, nowUtc);

        return await _reservations.FindOneAndUpdateAsync(
            filter, update, ReturnUpdated, cancellationToken);
    }

 
    public async Task<EnergyReservation?> TryCancelAsync(
        string reservationId, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var filter = Filter.And(
            Filter.Eq(r => r.Id, reservationId),
            Filter.In(r => r.Status, ReservationStatus.Active));

        var update = Update
            .Set(r => r.Status, ReservationStatus.Cancelled)
            .Set(r => r.CancelledAtUtc, nowUtc)
            .Set(r => r.UpdatedAtUtc, nowUtc)

            .Unset(r => r.QrToken);

        return await _reservations.FindOneAndUpdateAsync(
            filter, update, ReturnUpdated, cancellationToken);
    }

 
    public async Task<EnergyReservation?> TryCompleteAsync(
        string reservationId, string operatorId, DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        
        var filter = Filter.And(
            Filter.Eq(r => r.Id, reservationId),
            Filter.Eq(r => r.Status, ReservationStatus.Approved));

        var update = Update
            .Set(r => r.Status, ReservationStatus.Completed)
            .Set(r => r.CompletedAtUtc, nowUtc)
            .Set(r => r.CompletedByOperatorId, operatorId)
            .Set(r => r.UpdatedAtUtc, nowUtc);

        return await _reservations.FindOneAndUpdateAsync(
            filter, update, ReturnUpdated, cancellationToken);
    }

    
    public async Task<bool> HasActiveForStationAsync(
        string stationId, DateTime fromUtc, CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(stationId, out _))
        {
            return false;
        }

        
        var filter = Filter.And(
            Filter.Eq(r => r.StationId, stationId),
            Filter.In(r => r.Status, ReservationStatus.Active),
            Filter.Gte(r => r.ReservationStartUtc, fromUtc));

        return await _reservations.Find(filter).AnyAsync(cancellationToken);
    }

 
    public async Task<bool> HasActiveForSlotAsync(
        string slotId, CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(slotId, out _))
        {
            return false;
        }

        var filter = Filter.And(
            Filter.Eq(r => r.SlotId, slotId),
            Filter.In(r => r.Status, ReservationStatus.Active));

        return await _reservations.Find(filter).AnyAsync(cancellationToken);
    }

 
    private static FilterDefinition<EnergyReservation> BuildFilter(ReservationQuery query)
    {
        var filters = new List<FilterDefinition<EnergyReservation>> { Filter.Empty };

        if (!string.IsNullOrWhiteSpace(query.ProsumerNic))
        {
            filters.Add(Filter.Eq(r => r.ProsumerNic, query.ProsumerNic));
        }

        if (!string.IsNullOrWhiteSpace(query.StationId) && ObjectId.TryParse(query.StationId, out _))
        {
            filters.Add(Filter.Eq(r => r.StationId, query.StationId));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            filters.Add(Filter.Eq(r => r.Status, query.Status));
        }

        if (query.Statuses is { Length: > 0 })
        {
            filters.Add(Filter.In(r => r.Status, query.Statuses));
        }

        if (query.FromUtc.HasValue)
        {
            filters.Add(Filter.Gte(r => r.ReservationStartUtc, query.FromUtc.Value));
        }

        if (query.ToUtc.HasValue)
        {
            filters.Add(Filter.Lt(r => r.ReservationStartUtc, query.ToUtc.Value));
        }

        if (query.CompletedFromUtc.HasValue)
        {
            filters.Add(Filter.Gte(r => r.CompletedAtUtc, query.CompletedFromUtc.Value));
        }

        if (query.CompletedToUtc.HasValue)
        {
            filters.Add(Filter.Lt(r => r.CompletedAtUtc, query.CompletedToUtc.Value));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            
            var pattern = new BsonRegularExpression(Regex.Escape(query.Search.Trim()), "i");

            filters.Add(Filter.Or(
                Filter.Regex(r => r.ReservationNo, pattern),
                Filter.Regex(r => r.ProsumerNic, pattern)));
        }

        return Filter.And(filters);
    }
}
