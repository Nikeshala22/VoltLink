
using VoltLink.Api.Dtos;

namespace VoltLink.Api.Services;

public interface IAccountService
{
    
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

   
    Task<UserResponse> RegisterProsumerAsync(
        RegisterProsumerRequest request, CancellationToken cancellationToken = default);

   
    Task<UserResponse> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserResponse>> ListAsync(
        string? role = null,
        bool? isActive = null,
        bool? deactivationRequested = null,
        string? search = null,
        CancellationToken cancellationToken = default);


    Task<UserResponse> CreateStaffUserAsync(
        CreateStaffUserRequest request, CancellationToken cancellationToken = default);

  
    Task<UserResponse> CreateProsumerAsync(
        CreateProsumerRequest request, CancellationToken cancellationToken = default);


    Task<UserResponse> UpdateAsync(
        string id, UpdateUserRequest request, CancellationToken cancellationToken = default);


    Task<UserResponse> ActivateAsync(string id, CancellationToken cancellationToken = default);


    Task<UserResponse> DeactivateAsync(string id, CancellationToken cancellationToken = default);
    Task<UserResponse> RequestDeactivationAsync(string id, CancellationToken cancellationToken = default);
}
