using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using VoltLink.Api.Configuration;
using VoltLink.Api.Models;

namespace VoltLink.Api.Security;


public class JwtTokenService : ITokenService
{
    private readonly JwtSettings _settings;


    public JwtTokenService(IOptions<JwtSettings> options)
    {
        _settings = options.Value;

       
        if (string.IsNullOrWhiteSpace(_settings.Key) || Encoding.UTF8.GetByteCount(_settings.Key) < 32)
        {
            throw new InvalidOperationException(
                "Jwt:Key must be configured and be at least 32 bytes long.");
        }
    }


    public TokenResult CreateAccessToken(User user)
    {
      
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes);

     
        var claims = new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = user.Id,
            [JwtRegisteredClaimNames.Email] = user.Email,
            [JwtRegisteredClaimNames.Name] = user.FullName,
            [ClaimTypes.NameIdentifier] = user.Id,
            [ClaimTypes.Role] = user.Role
        };

       
        var descriptor = new SecurityTokenDescriptor
        {
            Claims = claims,
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            IssuedAt = DateTime.UtcNow,
            Expires = expiresAtUtc,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key)),
                SecurityAlgorithms.HmacSha256)
        };

        
        var token = new JsonWebTokenHandler().CreateToken(descriptor);

        return new TokenResult(token, expiresAtUtc);
    }
}
