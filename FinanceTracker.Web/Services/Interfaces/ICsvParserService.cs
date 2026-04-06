namespace FinanceTracker.Web.Services.Interfaces;

using FinanceTracker.Web.Models;

public interface IFileParserService
{
    List<EquityHolding> ParseEquityFile(Stream fileStream, string fileName);
    List<MutualFundHolding> ParseMutualFundFile(Stream fileStream, string fileName);
    List<Expense> ParseExpenseFile(Stream fileStream, string fileName);
}
