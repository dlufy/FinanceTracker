namespace FinanceTracker.Web.Services.Interfaces;

using FinanceTracker.Web.Models;

public interface IExpenseQueryService
{
    Task<PagedResult<Expense>> GetFilteredAsync(string userId, ExpenseFilter filter);
}
