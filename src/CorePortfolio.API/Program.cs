using CorePortfolio.API.Features.Admin.Categories;
using CorePortfolio.API.Features.Admin.MarketAssets;
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
using CorePortfolio.API.Services;
using CorePortfolio.API.Features.Auth;
using CorePortfolio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Global exception handling can be added here
// app.UseMiddleware<GlobalExceptionMiddleware>();

app.MapGet("/", () => "Welcome to CorePortfolio API")
    .WithName("GetRoot");

// Map Endpoints
app.MapAuthEndpoints();
app.MapCategoriesEndpoints();
app.MapMarketAssetsEndpoints();
app.MapSettingsEndpoints();
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

app.Run();
