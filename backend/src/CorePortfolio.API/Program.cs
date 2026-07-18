using CorePortfolio.API.Features.Admin.Categories;
using CorePortfolio.API.Features.Admin.MarketAssets;
using CorePortfolio.API.Features.Admin.Migration;
using CorePortfolio.API.Features.Admin.Settings;
using CorePortfolio.API.Features.Assets.CreateAsset;
using CorePortfolio.API.Features.Assets.DeleteAsset;
using CorePortfolio.API.Features.MarketAssets.UpdateMarketAssetPrice;
using CorePortfolio.API.Features.Portfolios.CreatePortfolio;
using CorePortfolio.API.Features.Portfolios.GetPortfolios;
using CorePortfolio.API.Features.Portfolios.GetPortfolioSummary;
using CorePortfolio.API.Features.Portfolios.UpdatePortfolio;
using CorePortfolio.API.Features.Transactions.CreateTransaction;
using CorePortfolio.API.Features.Transactions.DeleteTransaction;
using CorePortfolio.API.Features.Transactions.GetAssetTransactions;
using CorePortfolio.API.Features.Transactions.UpdateTransaction;
using CorePortfolio.API.Features.Transactions.GetAllTransactions;
using CorePortfolio.API.Features.Reports.GetGlobalHistory;
using CorePortfolio.API.Features.Reports.GetGlobalReport;
using CorePortfolio.API.Features.Reports.TakeDailySnapshot;
using CorePortfolio.API.Features.Portfolios.GetPortfolioHistory;
using CorePortfolio.API.Features.Cashflows;
using CorePortfolio.API.Features.Watchlist;
using CorePortfolio.API.Features.Rebalancing.GetRebalanceSuggestions;
using CorePortfolio.API.Features.Rebalancing.ExecutionPlans;
using CorePortfolio.API.Features.Analytics;
using CorePortfolio.API.Features.Budgets;
using CorePortfolio.API.Features.CashAccounts;
using CorePortfolio.API.Features.DcaPlans;
using CorePortfolio.API.Features.SavingGoals;
using CorePortfolio.API.Features.Dashboard;
using CorePortfolio.API.Features.RecurringCashflows;
using CorePortfolio.API.Features.Notifications;
using CorePortfolio.API.Services;
using CorePortfolio.API.Features.Auth;
using CorePortfolio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using CorePortfolio.Domain.Interfaces;
using CorePortfolio.Coingecko;
using CorePortfolio.Telegram;
using CorePortfolio.DNSE;
using CorePortfolio.API.Common;
using CorePortfolio.Domain.Accounting;
using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();

builder.Services.AddCors(options =>
{
    var configuredOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? new[] { "http://localhost:5173", "https://core-portfolio-taupe.vercel.app" };

    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.SetIsOriginAllowed(origin =>
                configuredOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase)
                || (builder.Environment.IsProduction() &&
                    Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
                    uri.Scheme == Uri.UriSchemeHttps &&
                    uri.Host.EndsWith(".vercel.app", StringComparison.OrdinalIgnoreCase)))
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
        });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IPortfolioReportService, PortfolioReportService>();
builder.Services.AddScoped<ITelegramCommandProcessor, TelegramCommandProcessor>();
builder.Services.AddScoped<TransactionLedgerService>();
builder.Services.AddScoped<ExchangeRateService>();
builder.Services.AddHttpClient();
builder.Services.AddHostedService<TelegramCronService>();
builder.Services.AddHostedService<DailySnapshotService>();
builder.Services.AddScoped<BackupService>();
builder.Services.AddScoped<MigrationService>();

// External Infrastructures
builder.Services.AddCoinGeckoInfrastructure(builder.Configuration);
builder.Services.AddTelegramInfrastructure(builder.Configuration);
builder.Services.AddDnseInfrastructure(builder.Configuration);

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key not found.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
});

var app = builder.Build();

app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? Guid.NewGuid().ToString("N");
    context.Response.Headers["X-Correlation-ID"] = correlationId;
    using (app.Logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        await next();
});

app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
    var (status, title) = exception switch
    {
        AccountingValidationException => (StatusCodes.Status400BadRequest, "Dữ liệu giao dịch không hợp lệ"),
        ResourceNotFoundException => (StatusCodes.Status404NotFound, "Không tìm thấy dữ liệu"),
        ResourceConflictException => (StatusCodes.Status409Conflict, "Xung đột dữ liệu"),
        UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Chưa xác thực"),
        _ => (StatusCodes.Status500InternalServerError, "Đã xảy ra lỗi hệ thống")
    };
    if (status == 500 && app.Logger != null) app.Logger.LogError(exception, "Unhandled API exception");

    await Results.Problem(statusCode: status, title: title,
        detail: app.Environment.IsDevelopment() ? exception?.ToString() : null).ExecuteAsync(context);
}));

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();

    // Fix lowercase GUIDs caused by raw SQL migration
    dbContext.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF;");
    dbContext.Database.ExecuteSqlRaw("UPDATE CashAccounts SET Id = UPPER(Id) WHERE Id != UPPER(Id);");
    dbContext.Database.ExecuteSqlRaw("UPDATE CashLedgerEntries SET Id = UPPER(Id), CashAccountId = UPPER(CashAccountId) WHERE Id != UPPER(Id) OR CashAccountId != UPPER(CashAccountId);");
    dbContext.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON;");
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Serve frontend SPA files
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

// Global exception handling can be added here
// app.UseMiddleware<GlobalExceptionMiddleware>();

app.MapGet("/api", () => "Welcome to CorePortfolio API")
    .WithName("GetRoot")
    .AllowAnonymous();

app.MapGet("/health", async (AppDbContext db, CancellationToken cancellationToken) =>
    await db.Database.CanConnectAsync(cancellationToken)
        ? Results.Ok(new { status = "healthy", database = "healthy" })
        : Results.Problem(statusCode: 503, title: "Database unavailable"))
    .AllowAnonymous();

// Map Endpoints
app.MapAuthEndpoints();
app.MapCategoriesEndpoints();
app.MapMarketAssetsEndpoints();
app.MapSettingsEndpoints();
app.MapMigrationEndpoints();
app.MapCreatePortfolioEndpoint();
app.MapGetPortfoliosEndpoint();
app.MapGetPortfolioSummaryEndpoint();
app.MapUpdatePortfolioEndpoint();
app.MapCreateAssetEndpoint();
app.MapDeleteAssetEndpoint();
app.MapUpdateMarketAssetPriceEndpoint();
app.MapCreateTransactionEndpoint();
app.MapUpdateTransactionEndpoint();
app.MapDeleteTransactionEndpoint();
app.MapGetAssetTransactionsEndpoint();
app.MapGetAllTransactionsEndpoint();
app.MapGetGlobalReportEndpoint();
app.MapTakeDailySnapshotEndpoint();
app.MapMockSnapshotsEndpoint();
app.MapGetGlobalHistoryEndpoint();
app.MapGetPortfolioHistoryEndpoint();
app.MapCashflowsEndpoints();
app.MapWatchlistEndpoints();
app.MapAnalyticsEndpoints();
app.MapBudgetsEndpoints();
app.MapCashAccountsEndpoints();
app.MapGetRebalanceSuggestionsEndpoint();
app.MapRebalanceExecutionPlansEndpoints();
app.MapSavingGoalsEndpoints();
app.MapDcaPlansEndpoints();
app.MapDashboardEndpoints();
app.MapRecurringCashflowsEndpoints();
app.MapNotificationsEndpoints();

// Map fallback to index.html for SPA routing
app.MapFallbackToFile("index.html");

app.Run();
