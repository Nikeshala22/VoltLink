using MongoDB.Bson;
using VoltLink.Api.Models;
using VoltLink.Api.Repositories;
using VoltLink.Api.Security;

namespace VoltLink.Api.Data;


public class DatabaseSeeder
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseSeeder> _logger;


    public DatabaseSeeder(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger<DatabaseSeeder> logger)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _logger = logger;
    }


    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // If an administrator already exists there is nothing to do.
        var existingAdmins = await _users.CountAsync(UserRoles.Backoffice, cancellationToken: cancellationToken);
        if (existingAdmins > 0)
        {
            _logger.LogInformation("Seed skipped: {Count} back-office account(s) already exist.", existingAdmins);
            return;
        }

        
        var email = _configuration["SeedAdmin:Email"] ?? "admin@voltlink.lk";
        var password = _configuration["SeedAdmin:Password"] ?? "Admin@123";
        var fullName = _configuration["SeedAdmin:FullName"] ?? "System Administrator";

        var now = DateTime.UtcNow;
        var admin = new User
        {
            Id = ObjectId.GenerateNewId().ToString(),
            FullName = fullName,
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = _passwordHasher.Hash(password),
            Role = UserRoles.Backoffice,
            IsActive = true,
            DeactivationRequested = false,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await _users.InsertAsync(admin, cancellationToken);

       
        _logger.LogWarning(
            "Seeded initial back-office account '{Email}'. Change this password before deployment.",
            admin.Email);
    }
}
