using Microsoft.AspNetCore.Authentication.Cookies;
using FinanceTracker.Web.Database;
using FinanceTracker.Web.Services;
using FinanceTracker.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Serilog;

// Bootstrap Serilog early so startup errors are captured
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog from appsettings + code
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
        .WriteTo.File(
            path: Path.Combine(builder.Environment.ContentRootPath, "Logs", "financetracker-.log"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"));

    // Add services to the container.
    builder.Services.AddControllersWithViews();
    builder.Services.AddMemoryCache();

    // Authentication
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
            options.AccessDeniedPath = "/Account/Login";
            options.ExpireTimeSpan = TimeSpan.FromDays(7);
        });

    // ── Storage: file (default) or PostgreSQL ──────────────────────────────
    var useDatabase = builder.Configuration.GetValue<bool>("Storage:UseDatabase");

    if (useDatabase)
    {
        var connectionString = builder.Configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Storage:UseDatabase is true but ConnectionStrings:Postgres is not configured.");

        builder.Services.AddDbContextFactory<FinanceTrackerDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "public");
                npgsql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
            }));

        builder.Services.AddScoped<IUserRepository, PostgresUserRepository>();
        builder.Services.AddScoped<IPortfolioRepository, PostgresPortfolioRepository>();

        Log.Information("Storage mode: PostgreSQL ({ConnectionString})", MaskConnectionString(connectionString));
    }
    else
    {
        builder.Services.AddSingleton<IUserRepository, JsonUserRepository>();
        builder.Services.AddSingleton<IPortfolioRepository, JsonPortfolioRepository>();

        Log.Information("Storage mode: JSON files (file-based)");
    }

    // Always register file parser and market data
    builder.Services.AddSingleton<IFileParserService, FileParserService>();
    builder.Services.AddHttpClient<IMarketDataService, MarketDataService>();
    builder.Services.AddHttpClient<ISymbolLookupService, SymbolLookupService>();
    builder.Services.AddSingleton<ISymbolMappingService, SymbolMappingService>();

    // Expense filtering, tags, categories, and async import
    builder.Services.AddScoped<IExpenseQueryService, ExpenseQueryService>();
    builder.Services.AddSingleton<ITagService, TagService>();
    builder.Services.AddSingleton<ICategoryService, CategoryService>();

    var importChannel = System.Threading.Channels.Channel.CreateUnbounded<FinanceTracker.Web.Services.ExpenseImportJob>(
        new System.Threading.Channels.UnboundedChannelOptions { SingleReader = true });
    builder.Services.AddSingleton(importChannel);
    builder.Services.AddSingleton(importChannel.Writer);
    builder.Services.AddSingleton(importChannel.Reader);
    builder.Services.AddHostedService<ExpenseImportBackgroundService>();

    var app = builder.Build();

    // Apply pending EF Core migrations automatically on startup when using DB
    if (useDatabase)
    {
        using var scope = app.Services.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<FinanceTrackerDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();
        await db.Database.MigrateAsync();
        app.Logger.LogInformation("Database migrations applied");
    }

    // Log startup
    app.Logger.LogInformation("FinanceTracker starting up in {Environment} environment", app.Environment.EnvironmentName);

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    // Log HTTP requests
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    });

    if (app.Environment.IsProduction())
    {
        app.UseHttpsRedirection();
    }
    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapStaticAssets();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
        .WithStaticAssets();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Mask the password in the connection string for safe logging
static string MaskConnectionString(string cs)
{
    var idx = cs.IndexOf("Password=", StringComparison.OrdinalIgnoreCase);
    if (idx < 0) return cs;
    var end = cs.IndexOf(';', idx);
    var masked = cs[..idx] + "Password=***" + (end >= 0 ? cs[end..] : string.Empty);
    return masked;
}
