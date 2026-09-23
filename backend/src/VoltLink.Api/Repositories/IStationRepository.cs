using VoltLink.Api.Models;

namespace VoltLink.Api.Repositories;

public interface IStationRepository
{
   
    Task<SolarStation?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<SolarStation?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

   
    Task<IReadOnlyList<SolarStation>> ListAsync(
        bool? isActive = null,
        string? city = null,
        string? search = null,
        CancellationToken cancellationToken = default);


    Task<IReadOnlyList<SolarStationWithDistance>> FindNearbyAsync(
        double latitude,
        double longitude,
        double radiusMetres,
        int limit,
        CancellationToken cancellationToken = default);

    Task InsertAsync(SolarStation station, CancellationToken cancellationToken = default);

    Task ReplaceAsync(SolarStation station, CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(
        string code, string? excludeStationId = null, CancellationToken cancellationToken = default);
}
