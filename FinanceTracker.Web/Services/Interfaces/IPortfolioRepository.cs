namespace FinanceTracker.Web.Services.Interfaces;

using FinanceTracker.Web.Models;

public interface IPortfolioRepository
{
    Task<UserPortfolio> GetPortfolioAsync(string userId);
    Task SavePortfolioAsync(UserPortfolio portfolio);
}
