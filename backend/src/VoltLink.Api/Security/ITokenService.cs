using VoltLink.Api.Models;

namespace VoltLink.Api.Security;


public record TokenResult(string AccessToken, DateTime ExpiresAtUtc);


public interface ITokenService
{
  
    TokenResult CreateAccessToken(User user);
}
