namespace VoltLink.Api.Security;


public interface IQrTokenService
{
    
    string Issue(string reservationId, DateTime slotStartUtc);

    
    string? Verify(string token);
}
