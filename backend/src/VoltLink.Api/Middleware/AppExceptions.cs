namespace VoltLink.Api.Middleware;


public static class ErrorCodes
{
    // Generic
    public const string NotFound = "NOT_FOUND";
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string Unexpected = "UNEXPECTED_ERROR";

    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string AccountInactive = "ACCOUNT_INACTIVE";
    public const string EmailAlreadyUsed = "EMAIL_ALREADY_USED";
    public const string NicAlreadyRegistered = "NIC_ALREADY_REGISTERED";
    public const string RoleNotAllowed = "ROLE_NOT_ALLOWED";

  
    public const string StationCodeAlreadyUsed = "STATION_CODE_ALREADY_USED";
    public const string StationInactive = "STATION_INACTIVE";
    public const string StationHasActiveReservations = "STATION_HAS_ACTIVE_RESERVATIONS";
    public const string SlotInactive = "SLOT_INACTIVE";
    public const string SlotHasBookings = "SLOT_HAS_BOOKINGS";
    public const string SlotFull = "SLOT_FULL";

    public const string ReservationInPast = "RESERVATION_IN_PAST";
    public const string ReservationOutside7Days = "RESERVATION_OUTSIDE_7_DAYS";
    public const string ChangeWindowExpired = "CHANGE_WINDOW_EXPIRED";
    public const string ReservationNotPending = "RESERVATION_NOT_PENDING";
    public const string ReservationNotApproved = "RESERVATION_NOT_APPROVED";
    public const string ReservationAlreadyClosed = "RESERVATION_ALREADY_CLOSED";
    public const string DuplicateReservation = "DUPLICATE_RESERVATION";

    public const string QrInvalid = "QR_INVALID";
    public const string QrAlreadyUsed = "QR_ALREADY_USED";
}

public abstract class AppException : Exception
{
    
    protected AppException(string code, string message) : base(message)
    {
        Code = code;
    }

  
    public string Code { get; }
}

public class NotFoundException : AppException
{
    
    public NotFoundException(string message, string code = ErrorCodes.NotFound)
        : base(code, message)
    {
    }
}


public class ValidationException : AppException
{
    
    public ValidationException(string message, string code = ErrorCodes.ValidationFailed)
        : base(code, message)
    {
    }
}


public class BusinessRuleViolationException : AppException
{
  
    public BusinessRuleViolationException(string code, string message)
        : base(code, message)
    {
    }
}


public class ConflictException : AppException
{
   
    public ConflictException(string code, string message)
        : base(code, message)
    {
    }
}


public class ForbiddenException : AppException
{
   
    public ForbiddenException(string message, string code = ErrorCodes.RoleNotAllowed)
        : base(code, message)
    {
    }
}
