namespace FinanceTracker.Web.Services;

using FinanceTracker.Web.Database;
using FinanceTracker.Web.Database.Entities;
using FinanceTracker.Web.Models;
using FinanceTracker.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// PostgreSQL-backed implementation of IUserRepository using EF Core.
/// Register this instead of JsonUserRepository when Storage:UseDatabase = true.
/// </summary>
public class PostgresUserRepository : IUserRepository
{
    private readonly IDbContextFactory<FinanceTrackerDbContext> _dbFactory;
    private readonly ILogger<PostgresUserRepository> _logger;

    public PostgresUserRepository(IDbContextFactory<FinanceTrackerDbContext> dbFactory, ILogger<PostgresUserRepository> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task<User?> GetByUserNameAsync(string userName)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entity = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserName == userName);
        return entity is null ? null : MapToDomain(entity);
    }

    public async Task<User?> GetByIdAsync(string id)
    {
        if (!Guid.TryParse(id, out var guid)) return null;
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entity = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == guid);
        return entity is null ? null : MapToDomain(entity);
    }

    public async Task<bool> CreateAsync(User user)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            if (await db.Users.AnyAsync(u => u.UserName == user.UserName))
                return false;

            db.Users.Add(MapToEntity(user));
            await db.SaveChangesAsync();
            _logger.LogInformation("Created user {UserName} in Postgres", user.UserName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user {UserName}", user.UserName);
            return false;
        }
    }

    public async Task<List<User>> GetAllAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entities = await db.Users.AsNoTracking().ToListAsync();
        return entities.Select(MapToDomain).ToList();
    }

    // ── Mappers ──────────────────────────────────────────────────────────────

    private static User MapToDomain(UserEntity e) => new()
    {
        Id = e.Id.ToString(),
        UserName = e.UserName,
        DisplayName = e.DisplayName,
        PasswordHash = e.PasswordHash,
        CreatedAt = e.CreatedAt
    };

    private static UserEntity MapToEntity(User u) => new()
    {
        Id = Guid.TryParse(u.Id, out var g) ? g : Guid.NewGuid(),
        UserName = u.UserName,
        DisplayName = u.DisplayName,
        PasswordHash = u.PasswordHash,
        CreatedAt = u.CreatedAt
    };
}
