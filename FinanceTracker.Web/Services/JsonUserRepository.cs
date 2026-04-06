namespace FinanceTracker.Web.Services;

using System.Text.Json;
using FinanceTracker.Web.Models;
using FinanceTracker.Web.Services.Interfaces;

public class JsonUserRepository : IUserRepository
{
    private readonly string _filePath;
    private readonly ILogger<JsonUserRepository> _logger;
    private static readonly SemaphoreSlim _lock = new(1, 1);
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public JsonUserRepository(IWebHostEnvironment env, ILogger<JsonUserRepository> logger)
    {
        _logger = logger;
        var dataDir = Path.Combine(env.ContentRootPath, "Data");
        Directory.CreateDirectory(dataDir);
        _filePath = Path.Combine(dataDir, "users.json");
        if (!File.Exists(_filePath))
            File.WriteAllText(_filePath, "[]");
        _logger.LogInformation("UserRepository initialized, data file: {FilePath}", _filePath);
    }

    public async Task<List<User>> GetAllAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var json = await File.ReadAllTextAsync(_filePath);
            return JsonSerializer.Deserialize<List<User>>(json, _jsonOptions) ?? new List<User>();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<User?> GetByUserNameAsync(string userName)
    {
        var users = await GetAllAsync();
        return users.FirstOrDefault(u => u.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<User?> GetByIdAsync(string id)
    {
        var users = await GetAllAsync();
        return users.FirstOrDefault(u => u.Id == id);
    }

    public async Task<bool> CreateAsync(User user)
    {
        await _lock.WaitAsync();
        try
        {
            var json = await File.ReadAllTextAsync(_filePath);
            var users = JsonSerializer.Deserialize<List<User>>(json, _jsonOptions) ?? new List<User>();

            if (users.Any(u => u.UserName.Equals(user.UserName, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning("Attempted to create duplicate user: {UserName}", user.UserName);
                return false;
            }

            users.Add(user);
            var updatedJson = JsonSerializer.Serialize(users, _jsonOptions);
            await File.WriteAllTextAsync(_filePath, updatedJson);
            _logger.LogInformation("Created user {UserName} (Id: {UserId})", user.UserName, user.Id);
            return true;
        }
        finally
        {
            _lock.Release();
        }
    }
}
