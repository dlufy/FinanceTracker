namespace FinanceTracker.Web.Services;

using System.Globalization;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using FinanceTracker.Web.Models;
using FinanceTracker.Web.Services.Interfaces;

public class FileParserService : IFileParserService
{
    private readonly ILogger<FileParserService> _logger;

    public FileParserService(ILogger<FileParserService> logger)
    {
        _logger = logger;
    }

    private static bool IsXlsx(string fileName) =>
        fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ||
        fileName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase);

    // ── Equity ──────────────────────────────────────────────

    public List<EquityHolding> ParseEquityFile(Stream fileStream, string fileName)
    {
        _logger.LogInformation("Parsing equity file: {FileName} (format: {Format})", fileName, IsXlsx(fileName) ? "XLSX" : "CSV");
        var result = IsXlsx(fileName)
            ? ParseEquityXlsx(fileStream)
            : ParseEquityCsv(fileStream);
        _logger.LogInformation("Parsed {Count} equity holdings from {FileName}", result.Count, fileName);
        return result;
    }

    private List<EquityHolding> ParseEquityCsv(Stream csvStream)
    {
        using var reader = new StreamReader(csvStream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            HeaderValidated = null,
            MissingFieldFound = null
        });

        csv.Context.RegisterClassMap<EquityCsvMap>();
        var holdings = csv.GetRecords<EquityHolding>().ToList();

        // Validate that every row has a non-empty Symbol
        var missingSymbolRows = holdings
            .Select((h, i) => (h, row: i + 2))
            .Where(x => string.IsNullOrWhiteSpace(x.h.Symbol))
            .ToList();

        if (missingSymbolRows.Any())
        {
            var names = missingSymbolRows
                .Select(x => string.IsNullOrWhiteSpace(x.h.CompanyName) ? $"row {x.row}" : x.h.CompanyName);
            throw new InvalidOperationException(
                $"Symbol is required for all holdings but is missing for: " +
                $"{string.Join(", ", names)}. " +
                "Please fill in the 'Symbol' column with NSE/BSE ticker codes before uploading.");
        }

        // Normalise symbols to uppercase
        foreach (var h in holdings)
            h.Symbol = h.Symbol.ToUpperInvariant();

        return holdings;
    }

    private List<EquityHolding> ParseEquityXlsx(Stream xlsxStream)
    {
        using var workbook = new XLWorkbook(xlsxStream);
        var ws = workbook.Worksheets.First();
        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var maxCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;

        for (int col = 1; col <= maxCol; col++)
        {
            var headerValue = ws.Cell(1, col).GetString().Trim();
            if (!string.IsNullOrEmpty(headerValue))
                headers[headerValue] = col;
        }

        // Validate required columns
        var requiredColumns = new[] { "Stock Name", "Quantity", "Average buy price" };
        var missing = requiredColumns.Where(c => !headers.ContainsKey(c)).ToList();
        if (missing.Any())
            throw new InvalidOperationException(
                $"Missing required columns: {string.Join(", ", missing)}. " +
                $"Found columns: {string.Join(", ", headers.Keys)}");

        // Symbol/Ticker column is required
        bool hasSymbolCol = headers.ContainsKey("Symbol") || headers.ContainsKey("Ticker");
        if (!hasSymbolCol)
            throw new InvalidOperationException(
                "Missing required column: 'Symbol' (or 'Ticker'). " +
                "Please add a Symbol column with NSE/BSE ticker codes (e.g., RELIANCE, INFY). " +
                $"Found columns: {string.Join(", ", headers.Keys)}");

        var holdings = new List<EquityHolding>();
        var missingSymbolRows = new List<string>();

        for (int row = 2; row <= ws.LastRowUsed()?.RowNumber(); row++)
        {
            var stockName = GetCellString(ws, row, headers, "Stock Name");
            if (string.IsNullOrWhiteSpace(stockName)) continue;

            var symbol = IfEmpty(
                GetCellString(ws, row, headers, "Symbol").Trim(),
                GetCellString(ws, row, headers, "Ticker").Trim());

            if (string.IsNullOrWhiteSpace(symbol))
            {
                missingSymbolRows.Add(stockName);
                continue;
            }

            var holding = new EquityHolding
            {
                CompanyName = stockName,
                Symbol = symbol.ToUpperInvariant(),
                Isin = GetCellString(ws, row, headers, "ISIN"),
                Exchange = GetCellString(ws, row, headers, "Exchange"),
                Quantity = (int)GetCellDecimal(ws, row, headers, "Quantity"),
                AverageBuyPrice = GetCellDecimal(ws, row, headers, "Average buy price"),
                CurrentPrice = GetCellDecimal(ws, row, headers, "Closing price"),
                LastPriceUpdate = DateTime.UtcNow
            };

            holdings.Add(holding);
        }

        if (missingSymbolRows.Any())
            throw new InvalidOperationException(
                $"Symbol is required for all holdings but is missing for: " +
                $"{string.Join(", ", missingSymbolRows)}. " +
                "Please fill in the 'Symbol' column with NSE/BSE ticker codes before uploading.");

        return holdings;
    }

    // ── Mutual Fund ─────────────────────────────────────────

    public List<MutualFundHolding> ParseMutualFundFile(Stream fileStream, string fileName)
    {
        _logger.LogInformation("Parsing MF file: {FileName} (format: {Format})", fileName, IsXlsx(fileName) ? "XLSX" : "CSV");
        var result = IsXlsx(fileName)
            ? ParseMutualFundXlsx(fileStream)
            : ParseMutualFundCsv(fileStream);
        _logger.LogInformation("Parsed {Count} MF holdings from {FileName}", result.Count, fileName);
        return result;
    }

    private List<MutualFundHolding> ParseMutualFundCsv(Stream csvStream)
    {
        using var reader = new StreamReader(csvStream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            HeaderValidated = null,
            MissingFieldFound = null
        });

        csv.Context.RegisterClassMap<MutualFundCsvMap>();
        return csv.GetRecords<MutualFundHolding>().ToList();
    }

    private List<MutualFundHolding> ParseMutualFundXlsx(Stream xlsxStream)
    {
        using var workbook = new XLWorkbook(xlsxStream);
        var ws = workbook.Worksheets.First();
        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var maxCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;

        for (int col = 1; col <= maxCol; col++)
        {
            var headerValue = ws.Cell(1, col).GetString().Trim();
            if (!string.IsNullOrEmpty(headerValue))
                headers[headerValue] = col;
        }

        var requiredColumns = new[] { "Scheme Name", "Units", "Invested Value" };
        var missing = requiredColumns.Where(c => !headers.ContainsKey(c)).ToList();
        if (missing.Any())
            throw new InvalidOperationException(
                $"Missing required columns: {string.Join(", ", missing)}. " +
                $"Found columns: {string.Join(", ", headers.Keys)}");

        var holdings = new List<MutualFundHolding>();

        for (int row = 2; row <= ws.LastRowUsed()?.RowNumber(); row++)
        {
            var schemeName = GetCellString(ws, row, headers, "Scheme Name");
            if (string.IsNullOrWhiteSpace(schemeName)) continue;

            var units = GetCellDecimal(ws, row, headers, "Units");
            var investedValue = GetCellDecimal(ws, row, headers, "Invested Value");
            var currentValue = GetCellDecimal(ws, row, headers, "Current Value");

            var holding = new MutualFundHolding
            {
                SchemeName = schemeName,
                // Prefer explicit Scheme Code column if present
                SchemeCode = IfEmpty(GetCellString(ws, row, headers, "Scheme Code"),
                    GetCellString(ws, row, headers, "SchemeCode")),
                Amc = GetCellString(ws, row, headers, "AMC"),
                Category = GetCellString(ws, row, headers, "Category"),
                FolioNumber = GetCellString(ws, row, headers, "Folio No."),
                Units = units,
                AverageNav = units != 0 ? Math.Round(investedValue / units, 4) : 0,
                CurrentNav = units != 0 ? Math.Round(currentValue / units, 4) : 0,
                LastNavUpdate = DateTime.UtcNow
            };

            holdings.Add(holding);
        }

        return holdings;
    }

    // ── Helpers ──────────────────────────────────────────────

    private static string GetCellString(IXLWorksheet ws, int row, Dictionary<string, int> headers, string column)
    {
        if (!headers.TryGetValue(column, out int col)) return string.Empty;
        return ws.Cell(row, col).GetString().Trim();
    }

    private static decimal GetCellDecimal(IXLWorksheet ws, int row, Dictionary<string, int> headers, string column)
    {
        if (!headers.TryGetValue(column, out int col)) return 0;
        var cell = ws.Cell(row, col);
        if (cell.IsEmpty()) return 0;
        if (cell.DataType == XLDataType.Number)
            return (decimal)cell.GetDouble();
        if (decimal.TryParse(cell.GetString().Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
            return val;
        return 0;
    }

    /// <summary>Returns <paramref name="fallback"/> when the string is null or whitespace.</summary>
    private static string IfEmpty(string s, string fallback) =>
        string.IsNullOrWhiteSpace(s) ? fallback : s;

    // ── CSV Maps ────────────────────────────────────────────

    private sealed class EquityCsvMap : ClassMap<EquityHolding>
    {
        public EquityCsvMap()
        {
            Map(m => m.Symbol).Name("Symbol");
            Map(m => m.CompanyName).Name("Company Name");
            Map(m => m.Quantity).Name("Quantity");
            Map(m => m.AverageBuyPrice).Name("Average Price");
            Map(m => m.Id).Ignore();
            Map(m => m.Isin).Ignore();
            Map(m => m.Exchange).Ignore();
            Map(m => m.CurrentPrice).Ignore();
            Map(m => m.LastPriceUpdate).Ignore();
            Map(m => m.AddedAt).Ignore();
        }
    }

    private sealed class MutualFundCsvMap : ClassMap<MutualFundHolding>
    {
        public MutualFundCsvMap()
        {
            Map(m => m.SchemeCode).Name("Scheme Code");
            Map(m => m.SchemeName).Name("Scheme Name");
            Map(m => m.FolioNumber).Name("Folio Number");
            Map(m => m.Units).Name("Units");
            Map(m => m.AverageNav).Name("Average NAV");
            Map(m => m.Id).Ignore();
            Map(m => m.Amc).Ignore();
            Map(m => m.Category).Ignore();
            Map(m => m.CurrentNav).Ignore();
            Map(m => m.LastNavUpdate).Ignore();
            Map(m => m.AddedAt).Ignore();
        }
    }

    // ── Expenses ─────────────────────────────────────────────

    public List<Expense> ParseExpenseFile(Stream fileStream, string fileName)
    {
        _logger.LogInformation("Parsing expense file: {FileName} (format: {Format})", fileName, IsXlsx(fileName) ? "XLSX" : "CSV");
        var result = IsXlsx(fileName)
            ? ParseExpenseXlsx(fileStream)
            : ParseExpenseCsv(fileStream);
        _logger.LogInformation("Parsed {Count} expenses from {FileName}", result.Count, fileName);
        return result;
    }

    private List<Expense> ParseExpenseCsv(Stream csvStream)
    {
        using var reader = new StreamReader(csvStream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null
        });

        csv.Read();
        csv.ReadHeader();
        var headers = csv.HeaderRecord ?? Array.Empty<string>();

        var required = new[] { "Date", "Amount", "Category" };
        var missing = required.Where(r => !headers.Any(h => h.Equals(r, StringComparison.OrdinalIgnoreCase))).ToList();
        if (missing.Any())
            throw new InvalidOperationException($"Missing required columns: {string.Join(", ", missing)}. Expected: Date, Amount, Category, Description, Tags");

        bool hasDescription = headers.Any(h => h.Equals("Description", StringComparison.OrdinalIgnoreCase));
        bool hasTags = headers.Any(h => h.Equals("Tags", StringComparison.OrdinalIgnoreCase));

        var expenses = new List<Expense>();
        while (csv.Read())
        {
            var dateStr = csv.GetField("Date") ?? string.Empty;
            var amountStr = csv.GetField("Amount") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(dateStr) && string.IsNullOrWhiteSpace(amountStr)) continue;

            DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date);
            decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount);

            var tagsRaw = hasTags ? (csv.GetField("Tags") ?? string.Empty) : string.Empty;

            expenses.Add(new Expense
            {
                Date = date == default ? DateTime.Today : date,
                Amount = amount,
                Category = csv.GetField("Category") ?? string.Empty,
                Description = hasDescription ? (csv.GetField("Description") ?? string.Empty) : string.Empty,
                Tags = ParseTagsList(tagsRaw),
                Source = "CSV"
            });
        }
        return expenses;
    }

    private List<Expense> ParseExpenseXlsx(Stream xlsxStream)
    {
        using var wb = new XLWorkbook(xlsxStream);
        var ws = wb.Worksheets.First();
        var maxCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;
        var maxRow = ws.LastRowUsed()?.RowNumber() ?? 0;

        if (maxRow < 2) return new List<Expense>();

        // Build header map
        var colMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int c = 1; c <= maxCol; c++)
        {
            var header = ws.Cell(1, c).GetString().Trim();
            if (!string.IsNullOrEmpty(header))
                colMap[header] = c;
        }

        var required = new[] { "Date", "Amount", "Category" };
        var missing = required.Where(r => !colMap.ContainsKey(r)).ToList();
        if (missing.Any())
            throw new InvalidOperationException($"Missing required columns: {string.Join(", ", missing)}. Expected: Date, Amount, Category, Description, Tags");

        string Get(int row, string col) =>
            colMap.TryGetValue(col, out var c) ? ws.Cell(row, c).GetString().Trim() : string.Empty;

        var expenses = new List<Expense>();
        for (int r = 2; r <= maxRow; r++)
        {
            var dateStr = Get(r, "Date");
            var amountStr = Get(r, "Amount");
            if (string.IsNullOrEmpty(dateStr) && string.IsNullOrEmpty(amountStr)) continue;

            DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date);
            decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount);

            expenses.Add(new Expense
            {
                Date = date == default ? DateTime.Today : date,
                Amount = amount,
                Category = Get(r, "Category"),
                Description = Get(r, "Description"),
                Tags = ParseTagsList(Get(r, "Tags")),
                Source = "CSV"
            });
        }
        return expenses;
    }

    private static List<string> ParseTagsList(string raw) =>
        string.IsNullOrWhiteSpace(raw)
            ? new List<string>()
            : raw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                 .Select(t => t.Trim())
                 .Where(t => !string.IsNullOrEmpty(t))
                 .Distinct(StringComparer.OrdinalIgnoreCase)
                 .ToList();

    private sealed class ExpenseCsvMap : ClassMap<Expense>
    {
        public ExpenseCsvMap()
        {
            Map(m => m.Date).Name("Date");
            Map(m => m.Amount).Name("Amount");
            Map(m => m.Category).Name("Category");
            Map(m => m.Description).Name("Description").Optional();
            Map(m => m.Id).Ignore();
            Map(m => m.Source).Ignore();
            Map(m => m.AddedAt).Ignore();
            Map(m => m.Tags).Ignore();
        }
    }
}
