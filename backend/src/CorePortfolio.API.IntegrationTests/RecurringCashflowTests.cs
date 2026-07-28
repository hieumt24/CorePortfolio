using System.Net;
using System.Net.Http.Json;
using CorePortfolio.API.IntegrationTests.Infrastructure;
using CorePortfolio.Domain.Entities;
using CorePortfolio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CorePortfolio.API.IntegrationTests;

public sealed class RecurringCashflowTests
{
    [Fact]
    public async Task CreateRule_WithAnotherUsersPortfolio_ReturnsNotFoundAndPersistsNothing()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CorePortfolioApiFactory();
        var firstUser = TestData.CreateUser("recurring-owner");
        var secondUser = TestData.CreateUser("recurring-other");
        var firstPortfolio = TestData.CreatePortfolio(firstUser, "Owner portfolio");
        var secondPortfolio = TestData.CreatePortfolio(secondUser, "Other portfolio");
        var category = CreateCategory(firstUser, "Owner category");

        await SeedAsync(
            factory,
            cancellationToken,
            firstUser,
            secondUser,
            firstPortfolio,
            secondPortfolio,
            category);

        using var client = factory.CreateAuthenticatedClient(firstUser.Id);
        var response = await client.PostAsJsonAsync(
            "/api/recurring-cashflows",
            ValidRequest(secondPortfolio.Id, category.Id),
            cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, await CountRulesAsync(factory, cancellationToken));
    }

    [Fact]
    public async Task CreateRule_WithAnotherUsersPrivateCategory_ReturnsNotFoundAndPersistsNothing()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CorePortfolioApiFactory();
        var firstUser = TestData.CreateUser("category-owner");
        var secondUser = TestData.CreateUser("category-other");
        var firstPortfolio = TestData.CreatePortfolio(firstUser, "Owner portfolio");
        var otherCategory = CreateCategory(secondUser, "Other private category");

        await SeedAsync(
            factory,
            cancellationToken,
            firstUser,
            secondUser,
            firstPortfolio,
            otherCategory);

        using var client = factory.CreateAuthenticatedClient(firstUser.Id);
        var response = await client.PostAsJsonAsync(
            "/api/recurring-cashflows",
            ValidRequest(firstPortfolio.Id, otherCategory.Id),
            cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, await CountRulesAsync(factory, cancellationToken));
    }

    [Fact]
    public async Task CreateRule_WithInvalidAmount_ReturnsBadRequest()
    {
        await AssertInvalidRequestAsync(request => request with { Amount = 0 });
    }

    [Fact]
    public async Task CreateRule_WithInvalidCurrency_ReturnsBadRequest()
    {
        await AssertInvalidRequestAsync(request => request with { Currency = "EUR" });
    }

    [Fact]
    public async Task CreateRule_WithInvalidFrequency_ReturnsBadRequest()
    {
        await AssertInvalidRequestAsync(request => request with { Frequency = "Sometimes" });
    }

    [Fact]
    public async Task CreateRule_WithMissingNextOccurrence_ReturnsBadRequest()
    {
        await AssertInvalidRequestAsync(request => request with { NextOccurrence = default });
    }

    [Fact]
    public async Task CreateRule_WithEndDateBeforeNextOccurrence_ReturnsBadRequest()
    {
        await AssertInvalidRequestAsync(request => request with
        {
            EndDate = request.NextOccurrence.AddDays(-1)
        });
    }

    [Fact]
    public async Task CreateRule_NormalizesValuesAndPersistsDateOnlyUtc()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CorePortfolioApiFactory();
        var user = TestData.CreateUser("recurring-success");
        var portfolio = TestData.CreatePortfolio(user, "Recurring portfolio");
        var category = CreateCategory(user, "Recurring category");
        await SeedAsync(factory, cancellationToken, user, portfolio, category);

        using var client = factory.CreateAuthenticatedClient(user.Id);
        var response = await client.PostAsJsonAsync(
            "/api/recurring-cashflows",
            ValidRequest(portfolio.Id, category.Id) with
            {
                Currency = " usd ",
                Frequency = " monthly ",
                NextOccurrence = new DateTime(2026, 8, 15, 18, 30, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2026, 12, 15, 7, 0, 0, DateTimeKind.Utc),
                Description = "  Monthly salary  "
            },
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var rule = Assert.Single(db.RecurringCashflowRules);
        Assert.Equal(user.Id, rule.UserId);
        Assert.Equal("USD", rule.Currency);
        Assert.Equal("Monthly", rule.Frequency);
        Assert.Equal(new DateTime(2026, 8, 15, 0, 0, 0, DateTimeKind.Utc), rule.NextOccurrence);
        Assert.Equal(new DateTime(2026, 12, 15, 0, 0, 0, DateTimeKind.Utc), rule.EndDate);
        Assert.Equal("Monthly salary", rule.Description);
        Assert.True(rule.IsActive);
    }

    private static async Task AssertInvalidRequestAsync(
        Func<RecurringCashflowRequestPayload, RecurringCashflowRequestPayload> mutate)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var factory = new CorePortfolioApiFactory();
        var user = TestData.CreateUser($"invalid-recurring-{Guid.NewGuid():N}");
        var portfolio = TestData.CreatePortfolio(user, "Invalid recurring portfolio");
        var category = CreateCategory(user, "Invalid recurring category");
        await SeedAsync(factory, cancellationToken, user, portfolio, category);

        using var client = factory.CreateAuthenticatedClient(user.Id);
        var response = await client.PostAsJsonAsync(
            "/api/recurring-cashflows",
            mutate(ValidRequest(portfolio.Id, category.Id)),
            cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await CountRulesAsync(factory, cancellationToken));
    }

    private static RecurringCashflowRequestPayload ValidRequest(Guid portfolioId, Guid categoryId) =>
        new(
            portfolioId,
            categoryId,
            2_000_000m,
            "VND",
            "Monthly",
            new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            "Monthly recurring cashflow");

    private static CashflowCategory CreateCategory(User user, string name) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = CashflowType.Expense,
            Icon = "test",
            Color = "#000000",
            IsGlobal = false,
            UserId = user.Id,
            User = user
        };

    private static async Task SeedAsync(
        CorePortfolioApiFactory factory,
        CancellationToken cancellationToken,
        params object[] entities)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.AddRange(entities);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<int> CountRulesAsync(
        CorePortfolioApiFactory factory,
        CancellationToken cancellationToken)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.RecurringCashflowRules.CountAsync(cancellationToken);
    }

    private sealed record RecurringCashflowRequestPayload(
        Guid PortfolioId,
        Guid CategoryId,
        decimal Amount,
        string Currency,
        string Frequency,
        DateTime NextOccurrence,
        DateTime? EndDate,
        string Description);
}
