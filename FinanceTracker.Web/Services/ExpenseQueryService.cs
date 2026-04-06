namespace FinanceTracker.Web.Services;

using FinanceTracker.Web.Models;
using FinanceTracker.Web.Services.Interfaces;

public class ExpenseQueryService : IExpenseQueryService
{
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly ILogger<ExpenseQueryService> _logger;

    public ExpenseQueryService(IPortfolioRepository portfolioRepository, ILogger<ExpenseQueryService> logger)
    {
        _portfolioRepository = portfolioRepository;
        _logger = logger;
    }

    public async Task<PagedResult<Expense>> GetFilteredAsync(string userId, ExpenseFilter filter)
    {
        var portfolio = await _portfolioRepository.GetPortfolioAsync(userId);
        var all = portfolio.Expenses.AsEnumerable();

        // Date range filter
        var dateFrom = filter.EffectiveDateFrom.Date;
        var dateTo = filter.EffectiveDateTo.Date;
        all = all.Where(e => e.Date.Date >= dateFrom && e.Date.Date <= dateTo);

        // Category filter
        if (filter.HasCategoryFilter)
            all = all.Where(e => e.Category.Equals(filter.Category, StringComparison.OrdinalIgnoreCase));

        // Tags filter — expense must have ALL specified tags
        if (filter.HasTagFilter)
        {
            var filterTagsNorm = filter.Tags
                .Select(t => t.Trim().ToLowerInvariant())
                .Where(t => !string.IsNullOrEmpty(t))
                .ToList();

            all = all.Where(e =>
                filterTagsNorm.All(ft =>
                    e.Tags.Any(et => et.ToLowerInvariant() == ft)));
        }

        var matched = all.OrderByDescending(e => e.Date).ThenByDescending(e => e.AddedAt).ToList();

        // Aggregate totals across ALL matched (not just current page)
        var totalAmount = matched.Sum(e => e.Amount);
        var categoryTotals = matched
            .GroupBy(e => e.Category, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));

        // Pagination
        var page = Math.Max(1, filter.Page);
        var pageSize = filter.PageSize > 0 ? filter.PageSize : 20;
        var items = matched.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        _logger.LogDebug("Expense query user={UserId} dateFrom={From} dateTo={To} cat={Cat} tags=[{Tags}] → {Total} matched, page {Page}/{TotalPages}",
            userId, dateFrom, dateTo, filter.Category, string.Join(",", filter.Tags),
            matched.Count, page, (int)Math.Ceiling((double)matched.Count / pageSize));

        return new PagedResult<Expense>
        {
            Items = items,
            TotalCount = matched.Count,
            TotalAmount = totalAmount,
            CategoryTotals = categoryTotals,
            Page = page,
            PageSize = pageSize
        };
    }
}
