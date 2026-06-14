# CorePortfolio Backend

The backend infrastructure for CorePortfolio, following **Clean Architecture** principles and implementing the **CQRS** pattern using MediatR.

## Solution Structure

The `.slnx` solution consists of the following projects:

- **CorePortfolio.API**: The presentation layer providing REST endpoints.
- **CorePortfolio.Domain**: Enterprise logic, entities, value objects, and domain events.
- **CorePortfolio.Infrastructure**: Data access, external service integrations, and cross-cutting concerns.
- **CorePortfolio.Coingecko**: Specialized integration layer for Coingecko's Cryptocurrency API.
- **CorePortfolio.DNSE**: Specialized integration layer for DNSE (Vietnamese Stock Market broker).
- **CorePortfolio.Telegram**: Specialized integration layer for Telegram Bot API for alerts and notifications.

## Key Design Patterns
- **Clean Architecture**: Ensures the business logic is independent of UI, databases, and external services.
- **CQRS**: Command Query Responsibility Segregation to optimize read and write operations.
- **Dependency Injection**: Extensively used for decoupling implementations.

## Running the API

To start the API, run the following command from the `CorePortfolio.API` folder:
```bash
dotnet run
```
