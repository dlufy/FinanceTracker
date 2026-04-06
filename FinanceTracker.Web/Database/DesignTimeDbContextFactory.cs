namespace FinanceTracker.Web.Database;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

/// <summary>
/// Enables EF Core tooling (dotnet ef migrations add / database update) to instantiate
/// FinanceTrackerDbContext at design time without running the full ASP.NET host.
/// The connection string here is only used for migration generation — it does not need
/// to point to a live database.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<FinanceTrackerDbContext>
{
    public FinanceTrackerDbContext CreateDbContext(string[] args)
    {
        // Prefer FINANCETRACKER_DB env var so CI/CD can inject the real connection string.
        // Falls back to a placeholder that allows migration generation without a live DB.
        var connectionString = Environment.GetEnvironmentVariable("FINANCETRACKER_DB")
            ?? "Host=localhost;Port=5432;Database=financetracker_design;Username=postgres;Password=design_time_placeholder";

        var optionsBuilder = new DbContextOptionsBuilder<FinanceTrackerDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.MigrationsHistoryTable("__ef_migrations_history", "public");
        });

        return new FinanceTrackerDbContext(optionsBuilder.Options);
    }
}
