using CorePortfolio.API.Features.Assets.CreateAsset;
using CorePortfolio.API.Features.Assets.UpdateAssetPrice;
using CorePortfolio.API.Features.Portfolios.CreatePortfolio;
using CorePortfolio.API.Features.Portfolios.GetPortfolios;
using CorePortfolio.API.Features.Portfolios.GetPortfolioSummary;
using CorePortfolio.API.Features.Portfolios.UpdatePortfolio;
using CorePortfolio.API.Features.Transactions.CreateTransaction;
using CorePortfolio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using CorePortfolio.API.Features.Transactions.GetAssetTransactions;

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

// Global exception handling can be added here
// app.UseMiddleware<GlobalExceptionMiddleware>();

app.MapGet("/", () => "Welcome to CorePortfolio API")
    .WithName("GetRoot");

// Map Endpoints
app.MapCreatePortfolioEndpoint();
app.MapGetPortfoliosEndpoint();
app.MapGetPortfolioSummaryEndpoint();
app.MapUpdatePortfolioEndpoint();
app.MapCreateAssetEndpoint();
app.MapUpdateAssetPriceEndpoint();
app.MapCreateTransactionEndpoint();
app.MapGetAssetTransactionsEndpoint();

app.Run();
