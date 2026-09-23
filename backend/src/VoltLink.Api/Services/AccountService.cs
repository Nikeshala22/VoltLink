using MongoDB.Bson;
using VoltLink.Api.Dtos;
using VoltLink.Api.Middleware;
using VoltLink.Api.Models;
using VoltLink.Api.Repositories;
using VoltLink.Api.Security;

namespace VoltLink.Api.Services;


public class AccountService : IAccountService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AccountService> _logger;

    public AccountService(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        ILogger<AccountService> logger)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _logger = logger;
    }

   
    public async Task<LoginResponse> LoginAsync(
        LoginRequest request, CancellationToken cancellationToken = default)
    {
       
        var user = await _users.GetByEmailAsync(request.Email, cancellationToken);

   
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for {Email}.", request.Email);
            throw new ValidationException(
                "Email or password is incorrect.", ErrorCodes.InvalidCredentials);
        }

        
        if (!user.IsActive)
        {
            throw new ForbiddenException(
                "This account is not active. Please contact the back-office team.",
                ErrorCodes.AccountInactive);
        }

       
        var token = _tokenService.CreateAccessToken(user);

        return new LoginResponse(token.AccessToken, token.ExpiresAtUtc, user.ToResponse());
    }

      public async Task<UserResponse> RegisterProsumerAsync(
        RegisterProsumerRequest request, CancellationToken cancellationToken = default)
    {
        
        return await CreateProsumerInternalAsync(request, activate: false, cancellationToken);
    }

    
    public async Task<UserResponse> CreateProsumerAsync(
        CreateProsumerRequest request, CancellationToken cancellationToken = default)
    {
        return await CreateProsumerInternalAsync(request, request.ActivateImmediately, cancellationToken);
    }

 
    private async Task<UserResponse> CreateProsumerInternalAsync(
        RegisterProsumerRequest request, bool activate, CancellationToken cancellationToken)
    {
        var nic = request.Nic.Trim().ToUpperInvariant();
        var email = request.Email.Trim().ToLowerInvariant();

      
     
        var existing = await _users.GetByIdAsync(nic, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException(
                ErrorCodes.NicAlreadyRegistered,
                $"A prosumer is already registered with NIC {nic}.");
        }

        if (await _users.EmailExistsAsync(email, cancellationToken: cancellationToken))
        {
            throw new ConflictException(
                ErrorCodes.EmailAlreadyUsed,
                $"The email address {email} is already in use.");
        }

        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = nic,
            FullName = request.FullName.Trim(),
            Email = email,
            Phone = request.Phone?.Trim(),
            Address = request.Address?.Trim(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = UserRoles.Prosumer,
            IsActive = activate,
            DeactivationRequested = false,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await _users.InsertAsync(user, cancellationToken);
        _logger.LogInformation("Prosumer {Nic} registered. Active: {IsActive}.", nic, activate);

        return user.ToResponse();
    }

    
    public async Task<UserResponse> CreateStaffUserAsync(
        CreateStaffUserRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Role != UserRoles.Backoffice && request.Role != UserRoles.GridOperator)
        {
            throw new ValidationException(
                $"Role must be either {UserRoles.Backoffice} or {UserRoles.GridOperator}.",
                ErrorCodes.RoleNotAllowed);
        }

        var email = request.Email.Trim().ToLowerInvariant();

        if (await _users.EmailExistsAsync(email, cancellationToken: cancellationToken))
        {
            throw new ConflictException(
                ErrorCodes.EmailAlreadyUsed,
                $"The email address {email} is already in use.");
        }

        var now = DateTime.UtcNow;
        var user = new User
        {
           
            Id = ObjectId.GenerateNewId().ToString(),
            FullName = request.FullName.Trim(),
            Email = email,
            Phone = request.Phone?.Trim(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = request.Role,
            IsActive = true,
            DeactivationRequested = false,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await _users.InsertAsync(user, cancellationToken);
        _logger.LogInformation("Staff account {Email} created with role {Role}.", email, request.Role);

        return user.ToResponse();
    }

 
    public async Task<UserResponse> GetByIdAsync(
        string id, CancellationToken cancellationToken = default)
    {
        var user = await GetRequiredAsync(id, cancellationToken);
        return user.ToResponse();
    }

 
    public async Task<IReadOnlyList<UserResponse>> ListAsync(
        string? role = null,
        bool? isActive = null,
        bool? deactivationRequested = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
       
        if (!string.IsNullOrWhiteSpace(role) && !UserRoles.All.Contains(role))
        {
            throw new ValidationException($"Unknown role '{role}'.");
        }

        var users = await _users.ListAsync(role, isActive, deactivationRequested, search, cancellationToken);
        return users.ToResponseList();
    }

      public async Task<UserResponse> UpdateAsync(
        string id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await GetRequiredAsync(id, cancellationToken);

        user.FullName = request.FullName.Trim();
        user.Phone = request.Phone?.Trim();
        user.Address = request.Address?.Trim();
        user.UpdatedAtUtc = DateTime.UtcNow;

        await _users.ReplaceAsync(user, cancellationToken);
        return user.ToResponse();
    }

    
    public async Task<UserResponse> ActivateAsync(
        string id, CancellationToken cancellationToken = default)
    {
        var user = await GetRequiredAsync(id, cancellationToken);

        user.IsActive = true;

        user.DeactivationRequested = false;
        user.UpdatedAtUtc = DateTime.UtcNow;

        await _users.ReplaceAsync(user, cancellationToken);
        _logger.LogInformation("Account {Id} activated.", id);

        return user.ToResponse();
    }

    
    public async Task<UserResponse> DeactivateAsync(
        string id, CancellationToken cancellationToken = default)
    {
        var user = await GetRequiredAsync(id, cancellationToken);

        user.IsActive = false;

        // The request has now been carried out, so clear the flag.
        user.DeactivationRequested = false;
        user.UpdatedAtUtc = DateTime.UtcNow;

        await _users.ReplaceAsync(user, cancellationToken);
        _logger.LogInformation("Account {Id} deactivated.", id);

        return user.ToResponse();
    }

  
    public async Task<UserResponse> RequestDeactivationAsync(
        string id, CancellationToken cancellationToken = default)
    {
        var user = await GetRequiredAsync(id, cancellationToken);

        
        if (user.Role != UserRoles.Prosumer)
        {
            throw new ValidationException(
                "Only prosumer accounts may request their own deactivation.",
                ErrorCodes.RoleNotAllowed);
        }

        user.DeactivationRequested = true;
        user.UpdatedAtUtc = DateTime.UtcNow;

        await _users.ReplaceAsync(user, cancellationToken);
        _logger.LogInformation("Prosumer {Id} requested deactivation.", id);

        return user.ToResponse();
    }

    private async Task<User> GetRequiredAsync(string id, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(id, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException($"No account was found with identifier '{id}'.");
        }

        return user;
    }
}
