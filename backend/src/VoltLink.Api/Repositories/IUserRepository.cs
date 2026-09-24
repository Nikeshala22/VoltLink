
using VoltLink.Api.Models;

namespace VoltLink.Api.Repositories;


public interface IUserRepository
{
    
    Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

 
    Task<IReadOnlyList<User>> ListAsync(
        string? role = null,
        bool? isActive = null,
        bool? deactivationRequested = null,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task InsertAsync(User user, CancellationToken cancellationToken = default);

    Task ReplaceAsync(User user, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(
        string email, string? excludeUserId = null, CancellationToken cancellationToken = default);

    Task<long> CountAsync(
        string? role = null, bool? isActive = null, CancellationToken cancellationToken = default);
}
