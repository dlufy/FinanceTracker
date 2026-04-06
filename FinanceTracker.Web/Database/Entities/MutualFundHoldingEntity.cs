namespace FinanceTracker.Web.Database.Entities;

public class MutualFundHoldingEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string AccountTag { get; set; } = string.Empty;
    public string SchemeCode { get; set; } = string.Empty;
    public string SchemeName { get; set; } = string.Empty;
    public string Amc { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string FolioNumber { get; set; } = string.Empty;
    public decimal Units { get; set; }
    public decimal AverageNav { get; set; }
    public decimal CurrentNav { get; set; }
    public DateTime LastNavUpdate { get; set; }
    public DateTime AddedAt { get; set; }

    // Navigation
    public UserEntity User { get; set; } = null!;
}
