using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;
using VoltLink.Api.Data;
using VoltLink.Api.Models;

namespace VoltLink.Api.Repositories;


public class UserRepository : IUserRepository
{
    private readonly IMongoCollection<User> _users;

    
    private static readonly FilterDefinitionBuilder<User> Filter = Builders<User>.Filter;

   
    public UserRepository(MongoContext context)
    {
        _users = context.Users;
    }

  
    public async Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
       
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return await _users.Find(Filter.Eq(u => u.Id, id))
                           .FirstOrDefaultAsync(cancellationToken);
    }

   
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        
        var normalised = email.Trim().ToLowerInvariant();

        return await _users.Find(Filter.Eq(u => u.Email, normalised))
                           .FirstOrDefaultAsync(cancellationToken);
    }

   
    public async Task<IReadOnlyList<User>> ListAsync(
        string? role = null,
        bool? isActive = null,
        bool? deactivationRequested = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
      
        var filters = new List<FilterDefinition<User>> { Filter.Empty };

        if (!string.IsNullOrWhiteSpace(role))
        {
            filters.Add(Filter.Eq(u => u.Role, role));
        }

        if (isActive.HasValue)
        {
            filters.Add(Filter.Eq(u => u.IsActive, isActive.Value));
        }

        if (deactivationRequested.HasValue)
        {
            filters.Add(Filter.Eq(u => u.DeactivationRequested, deactivationRequested.Value));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            
            var pattern = new BsonRegularExpression(
                Regex.Escape(search.Trim()), "i");

            filters.Add(Filter.Or(
                Filter.Regex(u => u.Id, pattern),
                Filter.Regex(u => u.FullName, pattern),
                Filter.Regex(u => u.Email, pattern)));
        }

        return await _users.Find(Filter.And(filters))
                           .SortBy(u => u.FullName)
                           .ToListAsync(cancellationToken);
    }

   
    public async Task InsertAsync(User user, CancellationToken cancellationToken = default)
    {
        await _users.InsertOneAsync(user, cancellationToken: cancellationToken);
    }

 
    public async Task ReplaceAsync(User user, CancellationToken cancellationToken = default)
    {
        await _users.ReplaceOneAsync(
            Filter.Eq(u => u.Id, user.Id), user, cancellationToken: cancellationToken);
    }

  
    public async Task<bool> EmailExistsAsync(
        string email, string? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        var normalised = email.Trim().ToLowerInvariant();
        var filter = Filter.Eq(u => u.Email, normalised);

        
        if (!string.IsNullOrWhiteSpace(excludeUserId))
        {
            filter = Filter.And(filter, Filter.Ne(u => u.Id, excludeUserId));
        }

        return await _users.Find(filter).AnyAsync(cancellationToken);
    }

   
    public async Task<long> CountAsync(
        string? role = null, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var filters = new List<FilterDefinition<User>> { Filter.Empty };

        if (!string.IsNullOrWhiteSpace(role))
        {
            filters.Add(Filter.Eq(u => u.Role, role));
        }

        if (isActive.HasValue)
        {
            filters.Add(Filter.Eq(u => u.IsActive, isActive.Value));
        }

        return await _users.CountDocumentsAsync(Filter.And(filters), cancellationToken: cancellationToken);
    }
}
