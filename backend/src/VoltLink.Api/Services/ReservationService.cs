using MongoDB.Bson;
using VoltLink.Api.Dtos;
using VoltLink.Api.Middleware;
using VoltLink.Api.Models;
using VoltLink.Api.Repositories;
using VoltLink.Api.Security;

namespace VoltLink.Api.Services;


public class ReservationService : IReservationService
{
    private readonly IReservationRepository _reservations;
    private readonly ISlotRepository _slots;
    private readonly IStationRepository _stations;
    private readonly IUserRepository _users;
    private readonly IQrTokenService _qrTokens;
    private readonly ILogger<ReservationService> _logger;


    public ReservationService(
        IReservationRepository reservations,
        ISlotRepository slots,
        IStationRepository stations,
        IUserRepository users,
        IQrTokenService qrTokens,
        ILogger<ReservationService> logger)
    {
        _reservations = reservations;
        _slots = slots;
        _stations = stations;
        _users = users;
        _qrTokens = qrTokens;
        _logger = logger;
    }


    public async Task<ReservationSummaryResponse> CreateAsync(
        CreateReservationRequest request,
        CallerContext caller,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

      
        var prosumerNic = caller.IsStaff
            ? (request.ProsumerNic ?? string.Empty).Trim().ToUpperInvariant()
            : caller.UserId;

        if (string.IsNullOrWhiteSpace(prosumerNic))
        {
            throw new ValidationException("A prosumer NIC is required when staff create a booking.");
        }

        EnsureTypeValid(request.Type);

        
        var prosumer = await _users.GetByIdAsync(prosumerNic, cancellationToken)
            ?? throw new NotFoundException($"No prosumer was found with NIC '{prosumerNic}'.");

        if (prosumer.Role != UserRoles.Prosumer)
        {
            throw new ValidationException(
                "Bookings can only be made for prosumer accounts.", ErrorCodes.RoleNotAllowed);
        }

        if (!prosumer.IsActive)
        {
            throw new BusinessRuleViolationException(
                ErrorCodes.AccountInactive,
                "This prosumer account is not active and cannot make bookings.");
        }

        var slot = await GetRequiredSlotAsync(request.SlotId, cancellationToken);

        if (!slot.IsActive)
        {
            throw new BusinessRuleViolationException(
                ErrorCodes.SlotInactive, "This booking window is no longer available.");
        }

        
        var station = await _stations.GetByIdAsync(slot.StationId, cancellationToken)
            ?? throw new NotFoundException("The station for this booking window no longer exists.");

        if (!station.IsActive)
        {
            throw new BusinessRuleViolationException(
                ErrorCodes.StationInactive, "This station is not currently in service.");
        }

        
        EnsureWithinBookingHorizon(slot.StartTimeUtc, now);

       
        if (await _reservations.ExistsActiveForProsumerAndSlotAsync(
                prosumerNic, slot.Id, cancellationToken: cancellationToken))
        {
            throw new ConflictException(
                ErrorCodes.DuplicateReservation,
                "You already hold a booking for this window.");
        }

       
        var claimed = await _slots.TryClaimPlaceAsync(slot.Id, cancellationToken)
            ?? throw new ConflictException(
                ErrorCodes.SlotFull, "This booking window is now full.");

       
        var id = ObjectId.GenerateNewId();
        var reservation = new EnergyReservation
        {
            Id = id.ToString(),
            ReservationNo = BuildReservationNo(id, now),
            ProsumerNic = prosumerNic,
            StationId = station.Id,
            SlotId = claimed.Id,
            ReservationStartUtc = claimed.StartTimeUtc,
            ReservationEndUtc = claimed.EndTimeUtc,
            EnergyKwh = claimed.EnergyKwhPerSlot,
            Type = request.Type,
            Status = ReservationStatus.Pending,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

      
        try
        {
            await _reservations.InsertAsync(reservation, cancellationToken);
        }
        catch
        {
            await _slots.ReleasePlaceAsync(claimed.Id, CancellationToken.None);
            throw;
        }

        _logger.LogInformation(
            "Reservation {No} created for prosumer {Nic}.", reservation.ReservationNo, prosumerNic);

        return BuildSummary(
            "Created",
            $"Booking {reservation.ReservationNo} has been requested and is awaiting approval.",
            reservation, prosumer.FullName, station.Name);
    }

       public async Task<ReservationSummaryResponse> UpdateAsync(
        string id,
        UpdateReservationRequest request,
        CallerContext caller,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var reservation = await GetRequiredAsync(id, cancellationToken);

        EnsureCallerMayAct(reservation, caller);
        EnsureTypeValid(request.Type);
        EnsureStillOpen(reservation);


        EnsureChangeNoticeGiven(reservation.ReservationStartUtc, now, "changed");

        var movingSlot = !string.Equals(request.SlotId, reservation.SlotId, StringComparison.Ordinal);
        var previousSlotId = reservation.SlotId;

        var station = await _stations.GetByIdAsync(reservation.StationId, cancellationToken);

        if (movingSlot)
        {
            var newSlot = await GetRequiredSlotAsync(request.SlotId, cancellationToken);

            if (!newSlot.IsActive)
            {
                throw new BusinessRuleViolationException(
                    ErrorCodes.SlotInactive, "The chosen booking window is no longer available.");
            }

            station = await _stations.GetByIdAsync(newSlot.StationId, cancellationToken)
                ?? throw new NotFoundException("The station for this booking window no longer exists.");

            if (!station.IsActive)
            {
                throw new BusinessRuleViolationException(
                    ErrorCodes.StationInactive, "That station is not currently in service.");
            }

          
            EnsureWithinBookingHorizon(newSlot.StartTimeUtc, now);

            if (await _reservations.ExistsActiveForProsumerAndSlotAsync(
                    reservation.ProsumerNic, newSlot.Id, reservation.Id, cancellationToken))
            {
                throw new ConflictException(
                    ErrorCodes.DuplicateReservation,
                    "You already hold a booking for that window.");
            }

            
            var claimed = await _slots.TryClaimPlaceAsync(newSlot.Id, cancellationToken)
                ?? throw new ConflictException(
                    ErrorCodes.SlotFull, "That booking window is now full.");

            reservation.SlotId = claimed.Id;
            reservation.StationId = station.Id;
            reservation.ReservationStartUtc = claimed.StartTimeUtc;
            reservation.ReservationEndUtc = claimed.EndTimeUtc;
            reservation.EnergyKwh = claimed.EnergyKwhPerSlot;
        }

        reservation.Type = request.Type;
        reservation.UpdatedAtUtc = now;

       
        reservation.Status = ReservationStatus.Pending;
        reservation.QrToken = null;
        reservation.QrIssuedAtUtc = null;

        await _reservations.ReplaceAsync(reservation, cancellationToken);

        
        if (movingSlot)
        {
            await _slots.ReleasePlaceAsync(previousSlotId, cancellationToken);
        }

        _logger.LogInformation("Reservation {No} updated.", reservation.ReservationNo);

        return BuildSummary(
            "Updated",
            $"Booking {reservation.ReservationNo} has been updated and is awaiting approval again.",
            reservation, stationName: station?.Name);
    }

   
    public async Task<ReservationSummaryResponse> CancelAsync(
        string id, CallerContext caller, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var reservation = await GetRequiredAsync(id, cancellationToken);

        EnsureCallerMayAct(reservation, caller);
        EnsureStillOpen(reservation);

        
        EnsureChangeNoticeGiven(reservation.ReservationStartUtc, now, "cancelled");

       
        var cancelled = await _reservations.TryCancelAsync(reservation.Id, now, cancellationToken)
            ?? throw new BusinessRuleViolationException(
                ErrorCodes.ReservationAlreadyClosed,
                "This booking has already been closed and cannot be cancelled.");

        await _slots.ReleasePlaceAsync(cancelled.SlotId, cancellationToken);

        _logger.LogInformation("Reservation {No} cancelled.", cancelled.ReservationNo);

        var station = await _stations.GetByIdAsync(cancelled.StationId, cancellationToken);

        return BuildSummary(
            "Cancelled",
            $"Booking {cancelled.ReservationNo} has been cancelled.",
            cancelled, stationName: station?.Name);
    }

   
    public async Task<ReservationResponse> ApproveAsync(
        string id, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var reservation = await GetRequiredAsync(id, cancellationToken);

        if (reservation.Status != ReservationStatus.Pending)
        {
            throw new BusinessRuleViolationException(
                ErrorCodes.ReservationNotPending,
                $"Only a pending booking can be approved; this one is {reservation.Status}.");
        }

        
        var token = _qrTokens.Issue(reservation.Id, reservation.ReservationStartUtc);

        var approved = await _reservations.TryApproveAsync(
                reservation.Id, token, now, cancellationToken)
            ?? throw new BusinessRuleViolationException(
                ErrorCodes.ReservationNotPending,
                "This booking is no longer pending and could not be approved.");

        _logger.LogInformation("Reservation {No} approved.", approved.ReservationNo);

        return await BuildResponseAsync(approved, cancellationToken);
    }

   
    public async Task<ReservationResponse> RejectAsync(
        string id, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var reservation = await GetRequiredAsync(id, cancellationToken);

        var rejected = await _reservations.TryRejectAsync(reservation.Id, now, cancellationToken)
            ?? throw new BusinessRuleViolationException(
                ErrorCodes.ReservationNotPending,
                $"Only a pending booking can be rejected; this one is {reservation.Status}.");

       
        await _slots.ReleasePlaceAsync(rejected.SlotId, cancellationToken);

        _logger.LogInformation("Reservation {No} rejected.", rejected.ReservationNo);

        return await BuildResponseAsync(rejected, cancellationToken);
    }

   
    public async Task<ReservationResponse> GetByIdAsync(
        string id, CallerContext caller, CancellationToken cancellationToken = default)
    {
        var reservation = await GetRequiredAsync(id, cancellationToken);

        EnsureCallerMayAct(reservation, caller);

        return await BuildResponseAsync(reservation, cancellationToken);
    }

   
    public async Task<IReadOnlyList<ReservationResponse>> SearchAsync(
        ReservationQuery query, CallerContext caller, CancellationToken cancellationToken = default)
    {
        // A prosumer cannot widen the search to other people by supplying a
        // different NIC: their own is forced in regardless of what was sent.
        if (!caller.IsStaff)
        {
            query.ProsumerNic = caller.UserId;
        }

        if (!string.IsNullOrWhiteSpace(query.Status) && !ReservationStatus.All.Contains(query.Status))
        {
            throw new ValidationException($"Unknown reservation status '{query.Status}'.");
        }

        var reservations = await _reservations.SearchAsync(query, cancellationToken);

       
        var stationNames = await LoadStationNamesAsync(reservations, cancellationToken);
        var prosumerNames = await LoadProsumerNamesAsync(reservations, cancellationToken);

        return reservations.ToResponseList(prosumerNames, stationNames);
    }

   
    public async Task<QrCodeResponse> GetQrCodeAsync(
        string id, CallerContext caller, CancellationToken cancellationToken = default)
    {
        var reservation = await GetRequiredAsync(id, cancellationToken);

        EnsureCallerMayAct(reservation, caller);

        if (reservation.Status != ReservationStatus.Approved
            || string.IsNullOrWhiteSpace(reservation.QrToken))
        {
            throw new BusinessRuleViolationException(
                ErrorCodes.ReservationNotApproved,
                "A transaction QR code is only available once the booking has been approved.");
        }

        return new QrCodeResponse(
            reservation.Id,
            reservation.ReservationNo,
            reservation.QrToken,
            reservation.QrIssuedAtUtc ?? reservation.UpdatedAtUtc,
            reservation.ReservationStartUtc);
    }

  
    public async Task<ReservationResponse> VerifyQrAsync(
        string token, CancellationToken cancellationToken = default)
    {
        
        var reservationId = _qrTokens.Verify(token)
            ?? throw new BusinessRuleViolationException(
                ErrorCodes.QrInvalid, "This QR code is not valid.");

        var reservation = await _reservations.GetByIdAsync(reservationId, cancellationToken)
            ?? throw new BusinessRuleViolationException(
                ErrorCodes.QrInvalid, "This QR code does not match any booking.");

      
        if (!string.Equals(reservation.QrToken, token, StringComparison.Ordinal))
        {
            throw new BusinessRuleViolationException(
                ErrorCodes.QrInvalid,
                "This QR code is no longer valid because the booking has changed.");
        }

        if (reservation.Status == ReservationStatus.Completed)
        {
            throw new BusinessRuleViolationException(
                ErrorCodes.QrAlreadyUsed,
                "This booking has already been completed.");
        }

        if (reservation.Status != ReservationStatus.Approved)
        {
            throw new BusinessRuleViolationException(
                ErrorCodes.ReservationNotApproved,
                $"This booking is {reservation.Status} and cannot be processed.");
        }

        return await BuildResponseAsync(reservation, cancellationToken);
    }

   
    public async Task<ReservationSummaryResponse> CompleteAsync(
        string id, CallerContext caller, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var reservation = await GetRequiredAsync(id, cancellationToken);

        
        var completed = await _reservations.TryCompleteAsync(
                reservation.Id, caller.UserId, now, cancellationToken)
            ?? throw new BusinessRuleViolationException(
                reservation.Status == ReservationStatus.Completed
                    ? ErrorCodes.QrAlreadyUsed
                    : ErrorCodes.ReservationNotApproved,
                reservation.Status == ReservationStatus.Completed
                    ? "This booking has already been completed."
                    : $"This booking is {reservation.Status} and cannot be completed.");

        _logger.LogInformation(
            "Reservation {No} completed by operator {OperatorId}.",
            completed.ReservationNo, caller.UserId);

        var station = await _stations.GetByIdAsync(completed.StationId, cancellationToken);

        return BuildSummary(
            "Completed",
            $"Energy transfer for booking {completed.ReservationNo} has been completed.",
            completed, stationName: station?.Name);
    }

    private static void EnsureWithinBookingHorizon(DateTime slotStartUtc, DateTime nowUtc)
    {
        if (slotStartUtc <= nowUtc)
        {
            throw new BusinessRuleViolationException(
                ErrorCodes.ReservationInPast,
                "This booking window has already started and can no longer be reserved.");
        }

        var latestAllowed = nowUtc.AddDays(BusinessRules.MaxBookingHorizonDays);

        if (slotStartUtc > latestAllowed)
        {
            throw new BusinessRuleViolationException(
                ErrorCodes.ReservationOutside7Days,
                $"Bookings can only be made up to {BusinessRules.MaxBookingHorizonDays} days in advance.");
        }
    }


    private static void EnsureChangeNoticeGiven(
        DateTime reservationStartUtc, DateTime nowUtc, string action)
    {
        var deadline = reservationStartUtc.AddHours(-BusinessRules.MinChangeNoticeHours);

        if (nowUtc >= deadline)
        {
            throw new BusinessRuleViolationException(
                ErrorCodes.ChangeWindowExpired,
                $"A booking can only be {action} at least " +
                $"{BusinessRules.MinChangeNoticeHours} hours before it starts.");
        }
    }


    private static void EnsureStillOpen(EnergyReservation reservation)
    {
        if (!ReservationStatus.Active.Contains(reservation.Status))
        {
            throw new BusinessRuleViolationException(
                ErrorCodes.ReservationAlreadyClosed,
                $"This booking is {reservation.Status} and can no longer be changed.");
        }
    }

   
    private static void EnsureTypeValid(string type)
    {
        if (!ReservationType.All.Contains(type))
        {
            throw new ValidationException(
                $"Reservation type must be either {ReservationType.Injection} " +
                $"or {ReservationType.Withdrawal}.");
        }
    }

 
    private static void EnsureCallerMayAct(EnergyReservation reservation, CallerContext caller)
    {
        if (caller.IsStaff)
        {
            return;
        }

        if (!string.Equals(reservation.ProsumerNic, caller.UserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenException("You may only act on your own bookings.");
        }
    }

    private static string BuildReservationNo(ObjectId id, DateTime nowUtc)
    {
        var suffix = id.ToString()[^6..].ToUpperInvariant();
        return $"RS-{nowUtc:yyyyMMdd}-{suffix}";
    }

  
    private static ReservationSummaryResponse BuildSummary(
        string action,
        string message,
        EnergyReservation reservation,
        string? prosumerName = null,
        string? stationName = null)
    {
        return new ReservationSummaryResponse(
            action, message, reservation.ToResponse(prosumerName, stationName));
    }

  
    private async Task<ReservationResponse> BuildResponseAsync(
        EnergyReservation reservation, CancellationToken cancellationToken)
    {
        var station = await _stations.GetByIdAsync(reservation.StationId, cancellationToken);
        var prosumer = await _users.GetByIdAsync(reservation.ProsumerNic, cancellationToken);

        return reservation.ToResponse(prosumer?.FullName, station?.Name);
    }

   
    private async Task<IReadOnlyDictionary<string, string>> LoadStationNamesAsync(
        IReadOnlyList<EnergyReservation> reservations, CancellationToken cancellationToken)
    {
        var names = new Dictionary<string, string>();

        var stationIds = reservations
            .Select(r => r.StationId)
            .Where(sid => !string.IsNullOrWhiteSpace(sid))
            .Distinct()
            .ToList();

   
        if (stationIds.Count == 0)
        {
            return names;
        }

        var stations = await _stations.ListAsync(cancellationToken: cancellationToken);

        foreach (var station in stations.Where(s => stationIds.Contains(s.Id)))
        {
            names[station.Id] = station.Name;
        }

        return names;
    }

  
    private async Task<IReadOnlyDictionary<string, string>> LoadProsumerNamesAsync(
        IReadOnlyList<EnergyReservation> reservations, CancellationToken cancellationToken)
    {
        var names = new Dictionary<string, string>();

        var nics = reservations
            .Select(r => r.ProsumerNic)
            .Where(nic => !string.IsNullOrWhiteSpace(nic))
            .Distinct()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (nics.Count == 0)
        {
            return names;
        }

      
        var prosumers = await _users.ListAsync(
            role: UserRoles.Prosumer, cancellationToken: cancellationToken);

        foreach (var prosumer in prosumers.Where(p => nics.Contains(p.Id)))
        {
            names[prosumer.Id] = prosumer.FullName;
        }

        return names;
    }

    private async Task<EnergyReservation> GetRequiredAsync(
        string id, CancellationToken cancellationToken)
    {
        return await _reservations.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"No booking was found with identifier '{id}'.");
    }

 
    private async Task<EnergyBookingSlot> GetRequiredSlotAsync(
        string slotId, CancellationToken cancellationToken)
    {
        return await _slots.GetByIdAsync(slotId, cancellationToken)
            ?? throw new NotFoundException($"No booking window was found with identifier '{slotId}'.");
    }
}
