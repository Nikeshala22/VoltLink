
namespace VoltLink.Api.Security;

public class BCryptPasswordHasher : IPasswordHasher
{
    
    private const int WorkFactor = 12;

 
    public string Hash(string plainPassword)
    {
        if (string.IsNullOrWhiteSpace(plainPassword))
        {
            throw new ArgumentException("Password must not be empty.", nameof(plainPassword));
        }

        return BCrypt.Net.BCrypt.HashPassword(plainPassword, WorkFactor);
    }

    
   
    public bool Verify(string plainPassword, string storedHash)
    {
       
        if (string.IsNullOrWhiteSpace(plainPassword) || string.IsNullOrWhiteSpace(storedHash))
        {
            return false;
        }

   
        try
        {
            return BCrypt.Net.BCrypt.Verify(plainPassword, storedHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}
