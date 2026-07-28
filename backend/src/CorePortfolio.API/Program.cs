using CorePortfolio.API.Features.Admin.Categories;
using CorePortfolio.API.Features.Admin.MarketAssets;
using CorePortfolio.API.Features.Admin.Migration;
using CorePortfolio.API.Features.Admin.Settings;
using CorePortfolio.API.Features.Admin;
using CorePortfolio.API.Features.Admin.ControlPlane;
using CorePortfolio.API.Features.Assets.CreateAsset;
using CorePortfolio.API.Features.Assets.DeleteAsset;
using CorePortfolio.API.Features.Assets.SearchAvailableMarketAssets;
using CorePortfolio.API.Features.MarketAssets.UpdateMarketAssetPrice;
using CorePortfolio.API.Features.Portfolios.CreatePortfolio;
using CorePortfolio.API.Features.Portfolios.GetPortfolios;
using CorePortfolio.API.Features.Portfolios.GetPortfolioSummary;
using CorePortfolio.API.Features.Portfolios.UpdatePortfolio;
using CorePortfolio.API.Features.Transactions.CreateTransaction;
using CorePortfolio.API.Features.Transactions.DeleteAllTransactions;
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
using CorePortfolio.API.Features.MarketIndices;
using CorePortfolio.API.Features.DcaPlans;
using CorePortfolio.API.Features.SavingGoals;
using CorePortfolio.API.Features.Dashboard;
using CorePortfolio.API.Features.RecurringCashflows;
using CorePortfolio.API.Features.Notifications;
using CorePortfolio.API.Features.MarketPrices;
using CorePortfolio.API.Features.Profile;
using CorePortfolio.API.Features.Performance;
using CorePortfolio.API.Services;
using CorePortfolio.API.Features.Auth;
using CorePortfolio.API.Features.Auth.Login;
using CorePortfolio.API.Features.Auth.TwoFactor;
using CorePortfolio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using CorePortfolio.Domain.Interfaces;
using CorePortfolio.Coingecko;
using CorePortfolio.Telegram;
using CorePortfolio.KBS;
using CorePortfolio.Fmarket;
using CorePortfolio.API.Common;
using CorePortfolio.Domain.Accounting;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using CorePortfolio.API.Features.Cashflows.CreateCashflowRecord;

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
                configuredOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials()
                  .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
        });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    var defaultDatabasePath = OperatingSystem.IsLinux() ? "/home/data/CorePortfolio.db" : "CorePortfolio.db";
    connectionString = $"Data Source={defaultDatabasePath}";
}

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite(connectionString);
});

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(AdminPermissionBehavior<,>));
    cfg.AddOpenBehavior(typeof(PrivilegedMfaAdminBehavior<,>));
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.Configure<UserActivityOptions>(
    builder.Configuration.GetSection(UserActivityOptions.SectionName));
builder.Services.AddScoped<IUserActivityService, UserActivityService>();
builder.Services.AddScoped<PerformanceDataService>();
builder.Services.AddScoped<IPortfolioReportService, PortfolioReportService>();
builder.Services.AddScoped<ITelegramCommandProcessor, TelegramCommandProcessor>();
builder.Services.AddScoped<TransactionLedgerService>();
builder.Services.AddScoped<CashflowRecordWriter>();
builder.Services.AddScoped<NotificationWriter>();
builder.Services.AddScoped<AuditWriter>();
builder.Services.AddScoped<AuthSessionService>();
builder.Services.AddOptions<TwoFactorOptions>()
    .Bind(builder.Configuration.GetSection(TwoFactorOptions.SectionName))
    .Validate(
        options => !options.EnforceForPrivilegedRoles || options.HasValidEncryptionKey(),
        "A valid 32-byte Security:TwoFactor:EncryptionKey is required when privileged-role enforcement is enabled.")
    .ValidateOnStart();
builder.Services.AddScoped<TwoFactorPolicy>();
builder.Services.AddScoped<TwoFactorSecretProtector>();
builder.Services.AddScoped<TotpService>();
builder.Services.AddScoped<RecoveryCodeService>();
builder.Services.AddScoped<TwoFactorChallengeService>();
builder.Services.AddScoped<AuthLoginCompletionService>();
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler,
    PrivilegedMfaAuthorizationHandler>();
builder.Services.AddHostedService<TwoFactorChallengeCleanupService>();
builder.Services.AddScoped<ExchangeRateService>();
builder.Services.AddSingleton<ProductionOperationsState>();
builder.Services.AddHttpClient();
builder.Services.AddHostedService<TelegramCronService>();
builder.Services.AddHostedService<DailySnapshotService>();
builder.Services.AddHostedService<MarketPriceRefreshService>();
builder.Services.AddHostedService<ScheduledBackupService>();
builder.Services.AddScoped<BackupService>();
builder.Services.AddScoped<MigrationService>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth-login", context => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.AddPolicy("auth-register", context => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.AddPolicy("auth-refresh", context => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 30,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.AddPolicy("auth-2fa", context => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20,
            Window = TimeSpan.FromMinutes(5),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});

// External Infrastructures
builder.Services.AddCoinGeckoInfrastructure(builder.Configuration);
builder.Services.AddTelegramInfrastructure(builder.Configuration);
builder.Services.AddKbsInfrastructure(builder.Configuration);
builder.Services.AddFmarketInfrastructure(builder.Configuration);

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
            ClockSkew = TimeSpan.Zero,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userIdValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var tokenRole = context.Principal?.FindFirstValue(ClaimTypes.Role);
                var tokenId = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Jti);
                if (!Guid.TryParse(userIdValue, out var userId))
                {
                    context.Fail("Invalid user identity.");
                    return;
                }

                var userActivityService = context.HttpContext.RequestServices
                    .GetRequiredService<IUserActivityService>();
                var hasAccess = await userActivityService.ValidateAccessAndTrackAsync(
                    userId,
                    tokenRole,
                    tokenId,
                    context.HttpContext.RequestAborted);

                if (!hasAccess)
                    context.Fail("User access has changed. Please sign in again.");
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    foreach (var permission in AdminPermissionCatalog.All)
    {
        options.AddPolicy(permission, policy => policy
            .RequireAuthenticatedUser()
            .RequireAssertion(context => AdminPermissionCatalog.Has(
                context.User.FindFirstValue(ClaimTypes.Role),
                permission))
            .AddRequirements(new PrivilegedMfaRequirement()));
    }
});

var app = builder.Build();

var forwardedHeadersEnabled = builder.Configuration.GetValue<bool>("ForwardedHeaders:Enabled");
if (forwardedHeadersEnabled)
{
    var forwardedHeadersOptions = new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
        ForwardLimit = Math.Clamp(
            builder.Configuration.GetValue<int?>("ForwardedHeaders:ForwardLimit") ?? 1,
            1,
            5)
    };

    if (builder.Configuration.GetValue<bool>("ForwardedHeaders:TrustAllProxies"))
    {
        forwardedHeadersOptions.KnownIPNetworks.Clear();
        forwardedHeadersOptions.KnownProxies.Clear();
    }

    app.UseForwardedHeaders(forwardedHeadersOptions);
}

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
        ForbiddenAccessException => (StatusCodes.Status403Forbidden, "Forbidden"),
        AccountingValidationException => (StatusCodes.Status400BadRequest, "Dữ liệu giao dịch không hợp lệ"),
        RequestValidationException => (StatusCodes.Status400BadRequest, "Dữ liệu yêu cầu không hợp lệ"),
        ArgumentException => (StatusCodes.Status400BadRequest, "Dữ liệu yêu cầu không hợp lệ"),
        FileNotFoundException => (StatusCodes.Status404NotFound, "Không tìm thấy tệp"),
        InvalidDataException => (StatusCodes.Status422UnprocessableEntity, "Dữ liệu sao lưu không hợp lệ"),
        ResourceNotFoundException => (StatusCodes.Status404NotFound, "Không tìm thấy dữ liệu"),
        ResourceConflictException => (StatusCodes.Status409Conflict, "Xung đột dữ liệu"),
        DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Dữ liệu đã được thay đổi bởi yêu cầu khác"),
        UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Chưa xác thực"),
        _ => (StatusCodes.Status500InternalServerError, "Đã xảy ra lỗi hệ thống")
    };
    if (status == 500 && app.Logger != null) app.Logger.LogError(exception, "Unhandled API exception");

    await Results.Problem(statusCode: status, title: title,
        detail: app.Environment.IsDevelopment() ? exception?.ToString() : null).ExecuteAsync(context);
}));

app.Use(async (context, next) =>
{
    var operationsState = context.RequestServices.GetRequiredService<ProductionOperationsState>();
    var isMutatingRequest = HttpMethods.IsPost(context.Request.Method)
        || HttpMethods.IsPut(context.Request.Method)
        || HttpMethods.IsPatch(context.Request.Method)
        || HttpMethods.IsDelete(context.Request.Method);
    if (operationsState.IsMaintenanceMode &&
        isMutatingRequest &&
        !context.Request.Path.StartsWithSegments("/health"))
    {
        await Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "System maintenance is in progress.")
            .ExecuteAsync(context);
        return;
    }

    await next();
});

try
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}
catch (Exception exception)
{
    app.Logger.LogCritical(exception, "Database migration failed during startup. API liveness remains available; readiness will report unavailable.");
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
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// Global exception handling can be added here
// app.UseMiddleware<GlobalExceptionMiddleware>();

app.MapGet("/api", () => "Welcome to CorePortfolio API")
    .WithName("GetRoot")
    .AllowAnonymous();

app.MapGet("/health/live", () => Results.Ok(new { status = "alive" }))
    .AllowAnonymous();

app.MapGet("/health/ready", async (
    AppDbContext db,
    ProductionOperationsState operationsState,
    CancellationToken cancellationToken) =>
{
    if (operationsState.IsMaintenanceMode)
        return Results.Problem(statusCode: 503, title: "Maintenance in progress");
    return await db.Database.CanConnectAsync(cancellationToken)
        ? Results.Ok(new { status = "ready", database = "healthy" })
        : Results.Problem(statusCode: 503, title: "Database unavailable");
})
    .AllowAnonymous();

app.MapGet("/health", async (
    AppDbContext db,
    ProductionOperationsState operationsState,
    CancellationToken cancellationToken) =>
{
    if (operationsState.IsMaintenanceMode)
        return Results.Problem(statusCode: 503, title: "Maintenance in progress");
    return await db.Database.CanConnectAsync(cancellationToken)
        ? Results.Ok(new { status = "ready", database = "healthy" })
        : Results.Problem(statusCode: 503, title: "Database unavailable");
})
    .AllowAnonymous();

// Map Endpoints
app.MapAuthEndpoints();
app.MapAdminEndpoints();
app.MapAdminControlPlaneEndpoints();
app.MapCategoriesEndpoints();
app.MapMarketAssetsEndpoints();
app.MapSettingsEndpoints();
app.MapMigrationEndpoints();
app.MapCreatePortfolioEndpoint();
app.MapGetPortfoliosEndpoint();
app.MapGetPortfolioSummaryEndpoint();
app.MapUpdatePortfolioEndpoint();
app.MapCreateAssetEndpoint();
app.MapSearchAvailableMarketAssetsEndpoint();
app.MapDeleteAssetEndpoint();
app.MapUpdateMarketAssetPriceEndpoint();
app.MapCreateTransactionEndpoint();
app.MapUpdateTransactionEndpoint();
app.MapDeleteAllTransactionsEndpoint();
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
app.MapMarketPricesEndpoints();
app.MapMarketIndicesEndpoints();
app.MapProfileEndpoints();
app.MapPerformanceEndpoints();

// Map fallback to index.html for SPA routing
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program;
