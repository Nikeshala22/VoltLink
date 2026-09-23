namespace VoltLink.Api.Models;


public static class UserRoles
{
    public const string Backoffice = "Backoffice";
    public const string GridOperator = "GridOperator";
    public const string Prosumer = "Prosumer";

   
    public static readonly string[] All = { Backoffice, GridOperator, Prosumer };
}


public static class Policies
{
   
    public const string Backoffice = "BackofficePolicy";

   
    public const string GridOperator = "GridOperatorPolicy";

   
    public const string Prosumer = "ProsumerPolicy";

    public const string Staff = "StaffPolicy";
}


public static class ReservationStatus
{
    
    public const string Pending = "Pending";

       public const string Approved = "Approved";

   
    public const string Cancelled = "Cancelled";

    
    public const string Rejected = "Rejected";

    
    public const string Completed = "Completed";

        public static readonly string[] Active = { Pending, Approved };

        public static readonly string[] All = { Pending, Approved, Cancelled, Rejected, Completed };
}

public static class ReservationType
{
       public const string Injection = "Injection";

        public const string Withdrawal = "Withdrawal";

        public static readonly string[] All = { Injection, Withdrawal };
}


public static class BusinessRules
{
       public const int MaxBookingHorizonDays = 7;

    
    public const int MinChangeNoticeHours = 12;
}
