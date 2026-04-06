using System.Text;
using ClosedXML.Excel;
using FinanceTracker.Web.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace FinanceTracker.Tests.Services;

public class FileParserServiceTests
{
    private readonly FileParserService _parser = new(NullLogger<FileParserService>.Instance);

    // ── CSV Tests (existing) ────────────────────────────────

    [Fact]
    public void ParseEquityFile_CsvValid_ReturnsHoldings()
    {
        var csv = "Symbol, Company Name, Quantity, Average Price\nRELIANCE, Reliance Industries Ltd, 10, 2500.00\nTCS, Tata Consultancy Services, 5, 3200.50";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = _parser.ParseEquityFile(stream, "holdings.csv");

        Assert.Equal(2, result.Count);
        Assert.Equal("RELIANCE", result[0].Symbol);
        Assert.Equal("Reliance Industries Ltd", result[0].CompanyName);
        Assert.Equal(10, result[0].Quantity);
        Assert.Equal(2500.00m, result[0].AverageBuyPrice);
        Assert.Equal("TCS", result[1].Symbol);
        Assert.Equal(5, result[1].Quantity);
        Assert.Equal(3200.50m, result[1].AverageBuyPrice);
    }

    [Fact]
    public void ParseEquityFile_CsvEmpty_ReturnsEmptyList()
    {
        var csv = "Symbol, Company Name, Quantity, Average Price\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = _parser.ParseEquityFile(stream, "empty.csv");

        Assert.Empty(result);
    }

    [Fact]
    public void ParseEquityFile_CsvGeneratesUniqueIds()
    {
        var csv = "Symbol, Company Name, Quantity, Average Price\nRELIANCE, Rel, 10, 2500\nTCS, TCS, 5, 3200";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = _parser.ParseEquityFile(stream, "holdings.csv");

        Assert.NotEqual(result[0].Id, result[1].Id);
        Assert.All(result, h => Assert.False(string.IsNullOrEmpty(h.Id)));
    }

    [Fact]
    public void ParseMutualFundFile_CsvValid_ReturnsHoldings()
    {
        var csv = "Scheme Code, Scheme Name, Folio Number, Units, Average NAV\n119551, Axis Bluechip Fund, 12345, 100.5, 45.00\n120505, HDFC Top 100, 67890, 200.25, 32.50";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = _parser.ParseMutualFundFile(stream, "mf.csv");

        Assert.Equal(2, result.Count);
        Assert.Equal("119551", result[0].SchemeCode);
        Assert.Equal("Axis Bluechip Fund", result[0].SchemeName);
        Assert.Equal("12345", result[0].FolioNumber);
        Assert.Equal(100.5m, result[0].Units);
        Assert.Equal(45.00m, result[0].AverageNav);
    }

    [Fact]
    public void ParseMutualFundFile_CsvEmpty_ReturnsEmptyList()
    {
        var csv = "Scheme Code, Scheme Name, Folio Number, Units, Average NAV\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = _parser.ParseMutualFundFile(stream, "mf.csv");

        Assert.Empty(result);
    }

    [Fact]
    public void ParseEquityFile_CsvSetsDefaultValues()
    {
        var csv = "Symbol, Company Name, Quantity, Average Price\nINFY, Infosys, 20, 1500";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = _parser.ParseEquityFile(stream, "test.csv");

        Assert.Single(result);
        Assert.Equal(0m, result[0].CurrentPrice);
        Assert.Equal("NSE", result[0].Exchange);
    }

    // ── XLSX Tests ──────────────────────────────────────────

    [Fact]
    public void ParseEquityFile_XlsxValid_ReturnsHoldings()
    {
        using var stream = CreateEquityXlsx(new[]
        {
            ("RELIANCE INDUSTRIES", "RELIANCE", "INE002A01018", 10, 2500m, 2650m),
            ("TCS LTD", "TCS", "INE467B01029", 5, 3200m, 3100m)
        });

        var result = _parser.ParseEquityFile(stream, "stocks.xlsx");

        Assert.Equal(2, result.Count);
        Assert.Equal("RELIANCE INDUSTRIES", result[0].CompanyName);
        Assert.Equal("RELIANCE", result[0].Symbol);
        Assert.Equal("INE002A01018", result[0].Isin);
        Assert.Equal(10, result[0].Quantity);
        Assert.Equal(2500m, result[0].AverageBuyPrice);
        Assert.Equal(2650m, result[0].CurrentPrice);
        Assert.Equal(5, result[1].Quantity);
    }

    [Fact]
    public void ParseEquityFile_XlsxMissingRequiredColumn_Throws()
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Sheet1");
        ws.Cell(1, 1).Value = "Name";  // Wrong column name
        ws.Cell(1, 2).Value = "Quantity";
        ws.Cell(2, 1).Value = "RELIANCE";
        ws.Cell(2, 2).Value = 10;

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _parser.ParseEquityFile(ms, "bad.xlsx"));

        Assert.Contains("Missing required columns", ex.Message);
        Assert.Contains("Stock Name", ex.Message);
    }

    [Fact]
    public void ParseEquityFile_XlsxMissingSymbolColumn_Throws()
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Sheet1");
        ws.Cell(1, 1).Value = "Stock Name";
        ws.Cell(1, 2).Value = "Quantity";
        ws.Cell(1, 3).Value = "Average buy price";
        // No Symbol or Ticker column
        ws.Cell(2, 1).Value = "Reliance Industries";
        ws.Cell(2, 2).Value = 10;
        ws.Cell(2, 3).Value = 2500m;

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _parser.ParseEquityFile(ms, "nosymbol.xlsx"));

        Assert.Contains("Symbol", ex.Message);
    }

    [Fact]
    public void ParseEquityFile_XlsxEmptySymbolRow_Throws()
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Sheet1");
        ws.Cell(1, 1).Value = "Stock Name";
        ws.Cell(1, 2).Value = "Symbol";
        ws.Cell(1, 3).Value = "Quantity";
        ws.Cell(1, 4).Value = "Average buy price";
        ws.Cell(2, 1).Value = "Reliance Industries";
        ws.Cell(2, 2).Value = "";  // empty symbol
        ws.Cell(2, 3).Value = 10;
        ws.Cell(2, 4).Value = 2500m;

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _parser.ParseEquityFile(ms, "emptysymbol.xlsx"));

        Assert.Contains("Reliance Industries", ex.Message);
        Assert.Contains("Symbol is required", ex.Message);
    }

    [Fact]
    public void ParseMutualFundFile_XlsxValid_ReturnsHoldings()
    {
        using var stream = CreateMutualFundXlsx(new[]
        {
            ("Parag Parikh Flexi Cap Fund", "PPFAS MF", "Equity", "1234", 100m, 50000m, 55000m),
            ("HDFC Top 100", "HDFC MF", "Equity", "5678", 200m, 80000m, 75000m)
        });

        var result = _parser.ParseMutualFundFile(stream, "mf.xlsx");

        Assert.Equal(2, result.Count);
        Assert.Equal("Parag Parikh Flexi Cap Fund", result[0].SchemeName);
        Assert.Equal("PPFAS MF", result[0].Amc);
        Assert.Equal("Equity", result[0].Category);
        Assert.Equal("1234", result[0].FolioNumber);
        Assert.Equal(100m, result[0].Units);
        Assert.Equal(500m, result[0].AverageNav);  // 50000/100
        Assert.Equal(550m, result[0].CurrentNav);   // 55000/100
    }

    [Fact]
    public void ParseMutualFundFile_XlsxMissingRequiredColumn_Throws()
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Sheet1");
        ws.Cell(1, 1).Value = "Fund Name"; // Wrong
        ws.Cell(1, 2).Value = "Units";

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _parser.ParseMutualFundFile(ms, "bad.xlsx"));

        Assert.Contains("Missing required columns", ex.Message);
    }

    [Fact]
    public void ParseEquityFile_DetectsFormatByExtension()
    {
        // CSV with .csv extension should use CSV parser
        var csv = "Symbol, Company Name, Quantity, Average Price\nINFY, Infosys, 20, 1500";
        using var csvStream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var csvResult = _parser.ParseEquityFile(csvStream, "test.CSV"); // uppercase

        Assert.Single(csvResult);
        Assert.Equal("INFY", csvResult[0].Symbol);

        // XLSX with .xlsx extension should use XLSX parser
        using var xlsxStream = CreateEquityXlsx(new[] { ("RELIANCE INDUSTRIES", "RELIANCE", "INE002A01018", 5, 2000m, 2100m) });
        var xlsxResult = _parser.ParseEquityFile(xlsxStream, "test.XLSX"); // uppercase

        Assert.Single(xlsxResult);
        Assert.Equal("RELIANCE INDUSTRIES", xlsxResult[0].CompanyName);
    }

    // ── XLSX Helpers ────────────────────────────────────────

    private static MemoryStream CreateEquityXlsx(
        (string name, string symbol, string isin, int qty, decimal avgPrice, decimal closingPrice)[] data)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Sheet1");
        ws.Cell(1, 1).Value = "Stock Name";
        ws.Cell(1, 2).Value = "Symbol";
        ws.Cell(1, 3).Value = "ISIN";
        ws.Cell(1, 4).Value = "Quantity";
        ws.Cell(1, 5).Value = "Average buy price";
        ws.Cell(1, 6).Value = "Buy value";
        ws.Cell(1, 7).Value = "Closing price";
        ws.Cell(1, 8).Value = "Closing value";
        ws.Cell(1, 9).Value = "Unrealised P&L";

        for (int i = 0; i < data.Length; i++)
        {
            var row = i + 2;
            ws.Cell(row, 1).Value = data[i].name;
            ws.Cell(row, 2).Value = data[i].symbol;
            ws.Cell(row, 3).Value = data[i].isin;
            ws.Cell(row, 4).Value = data[i].qty;
            ws.Cell(row, 5).Value = (double)data[i].avgPrice;
            ws.Cell(row, 6).Value = (double)(data[i].qty * data[i].avgPrice);
            ws.Cell(row, 7).Value = (double)data[i].closingPrice;
            ws.Cell(row, 8).Value = (double)(data[i].qty * data[i].closingPrice);
            ws.Cell(row, 9).Value = (double)((data[i].closingPrice - data[i].avgPrice) * data[i].qty);
        }

        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }

    private static MemoryStream CreateMutualFundXlsx(
        (string name, string amc, string category, string folio, decimal units, decimal invested, decimal current)[] data)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Holdings");
        ws.Cell(1, 1).Value = "Scheme Name";
        ws.Cell(1, 2).Value = "AMC";
        ws.Cell(1, 3).Value = "Category";
        ws.Cell(1, 4).Value = "Sub-category";
        ws.Cell(1, 5).Value = "Folio No.";
        ws.Cell(1, 6).Value = "Source";
        ws.Cell(1, 7).Value = "Units";
        ws.Cell(1, 8).Value = "Invested Value";
        ws.Cell(1, 9).Value = "Current Value";
        ws.Cell(1, 10).Value = "Returns";
        ws.Cell(1, 11).Value = "XIRR";

        for (int i = 0; i < data.Length; i++)
        {
            var row = i + 2;
            ws.Cell(row, 1).Value = data[i].name;
            ws.Cell(row, 2).Value = data[i].amc;
            ws.Cell(row, 3).Value = data[i].category;
            ws.Cell(row, 4).Value = "";
            ws.Cell(row, 5).Value = data[i].folio;
            ws.Cell(row, 6).Value = "Groww";
            ws.Cell(row, 7).Value = (double)data[i].units;
            ws.Cell(row, 8).Value = (double)data[i].invested;
            ws.Cell(row, 9).Value = (double)data[i].current;
            ws.Cell(row, 10).Value = (double)(data[i].current - data[i].invested);
            ws.Cell(row, 11).Value = "5%";
        }

        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }
}
