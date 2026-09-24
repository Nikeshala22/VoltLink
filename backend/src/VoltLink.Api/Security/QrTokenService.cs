using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using VoltLink.Api.Configuration;

namespace VoltLink.Api.Security;


public class QrTokenService : IQrTokenService
{
    private readonly byte[] _signingKey;

    
    private const char TokenSeparator = '.';

      private const char PayloadSeparator = '|';

 
    public QrTokenService(IOptions<QrSettings> options)
    {
        var key = options.Value.SigningKey;

      
        if (string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key) < 32)
        {
            throw new InvalidOperationException(
                "Qr:SigningKey must be configured and be at least 32 bytes long.");
        }

        _signingKey = Encoding.UTF8.GetBytes(key);
    }


    public string Issue(string reservationId, DateTime slotStartUtc)
    {
       
        var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(8));

       
        var payload = string.Join(PayloadSeparator,
            reservationId,
            nonce,
            slotStartUtc.Ticks.ToString());

        var signature = ComputeSignature(payload);

        return $"{Base64UrlEncode(Encoding.UTF8.GetBytes(payload))}" +
               $"{TokenSeparator}" +
               $"{Base64UrlEncode(signature)}";
    }

  
    public string? Verify(string token)
    {
      
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

     
        try
        {
            var parts = token.Split(TokenSeparator);
            if (parts.Length != 2)
            {
                return null;
            }

            var payloadBytes = Base64UrlDecode(parts[0]);
            var payload = Encoding.UTF8.GetString(payloadBytes);
            var providedSignature = Base64UrlDecode(parts[1]);

            var expectedSignature = ComputeSignature(payload);

          
            if (!CryptographicOperations.FixedTimeEquals(providedSignature, expectedSignature))
            {
                return null;
            }

            var fields = payload.Split(PayloadSeparator);
            if (fields.Length != 3 || string.IsNullOrWhiteSpace(fields[0]))
            {
                return null;
            }

            return fields[0];
        }
        catch (FormatException)
        {
            
            return null;
        }
        catch (ArgumentException)
        {
           
            return null;
        }
    }

  
    private byte[] ComputeSignature(string payload)
    {
        using var hmac = new HMACSHA256(_signingKey);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
    }

    
    private static string Base64UrlEncode(byte[] value)
    {
        return Convert.ToBase64String(value)
                      .TrimEnd('=')
                      .Replace('+', '-')
                      .Replace('/', '_');
    }

    
    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');

        
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }

        return Convert.FromBase64String(padded);
    }
}
