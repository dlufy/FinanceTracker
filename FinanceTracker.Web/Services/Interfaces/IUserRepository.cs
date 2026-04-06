namespace FinanceTracker.Web.Services.Interfaces;

using FinanceTracker.Web.Models;

public interface IUserRepository
{
    Task<User?> GetByUserNameAsync(string userName);
    Task<User?> GetByIdAsync(string id);
    Task<bool> CreateAsync(User user);
    Task<List<User>> GetAllAsync();
}
