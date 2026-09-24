using VoltLink.Api.Dtos;
using VoltLink.Api.Models;
using VoltLink.Api.Repositories;

namespace VoltLink.Api.Services;

public interface IDashboardService
{
  
    Task<ProsumerDashboardResponse> GetProsumerDashboardAsync(
        string prosumerNic, CancellationToken cancellationToken = default);

    Task<OperatorDashboardResponse> GetOperatorDashboardAsync(
        CancellationToken cancellationToken = default);
}


public class DashboardService : IDashboardService
{
    private readonly IReservationRepository _reservations;
    private readonly IStationRepository _stations;

    public DashboardService(IReservationRepository reservations, IStationRepository stations)
    {
        _reservations = reservations;
        _stations = stations;
    }

 
    public async Task<ProsumerDashboardResponse> GetProsumerDashboardAsync(
        string prosumerNic, CancellationToken cancellationToken = default)
    {
      
        var now = DateTime.UtcNow;

        var pending = await _reservations.CountAsync(new ReservationQuery
        {
            ProsumerNic = prosumerNic,
            Status = ReservationStatus.Pending
        }, cancellationToken);

       
        var approvedFuture = await _reservations.CountAsync(new ReservationQuery
        {
            ProsumerNic = prosumerNic,
            Status = ReservationStatus.Approved,
            FromUtc = now
        }, cancellationToken);

        var completed = await _reservations.CountAsync(new ReservationQuery
        {
            ProsumerNic = prosumerNic,
            Status = ReservationStatus.Completed
        }, cancellationToken);

        var cancelled = await _reservations.CountAsync(new ReservationQuery
        {
            ProsumerNic = prosumerNic,
            Status = ReservationStatus.Cancelled
        }, cancellationToken);

        var next = await _reservations.GetNextUpcomingAsync(prosumerNic, now, cancellationToken);

       
        string? stationName = null;
        if (next is not null)
        {
            var station = await _stations.GetByIdAsync(next.StationId, cancellationToken);
            stationName = station?.Name;
        }

        return new ProsumerDashboardResponse(
            prosumerNic,
            pending,
            approvedFuture,
            completed,
            cancelled,
            next?.ToResponse(stationName: stationName, nowUtc: now));
    }

   
    public async Task<OperatorDashboardResponse> GetOperatorDashboardAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

      
        var startOfDay = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
        var startOfNextDay = startOfDay.AddDays(1);

        var pending = await _reservations.CountAsync(new ReservationQuery
        {
            Status = ReservationStatus.Pending
        }, cancellationToken);

        var approvedFuture = await _reservations.CountAsync(new ReservationQuery
        {
            Status = ReservationStatus.Approved,
            FromUtc = now
        }, cancellationToken);

        
        var completedToday = await _reservations.CountAsync(new ReservationQuery
        {
            Status = ReservationStatus.Completed,
            CompletedFromUtc = startOfDay,
            CompletedToUtc = startOfNextDay
        }, cancellationToken);

        
        var schedule = await _reservations.SearchAsync(new ReservationQuery
        {
            Statuses = ReservationStatus.Active,
            FromUtc = startOfDay,
            ToUtc = startOfNextDay,
            Limit = 100
        }, cancellationToken);

        var stations = await _stations.ListAsync(isActive: true, cancellationToken: cancellationToken);
        var stationNames = stations.ToDictionary(s => s.Id, s => s.Name);

        return new OperatorDashboardResponse(
            pending,
            approvedFuture,
            completedToday,
            stations.Count,
            schedule.ToResponseList(stationNames: stationNames));
    }
}
