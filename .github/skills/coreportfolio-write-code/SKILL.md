---
name: coreportfolio-write-code
description: "Write or update C# code for the CorePortfolio Service following Clean Architecture and Vertical Slice patterns. Use when: creating a new API endpoint, implementing domain logic, setting up EF Core entities with SQLite, or scaffolding a new feature slice."
---

# Write Code

Implement or modify C# code for the CorePortfolio Service. This skill orchestrates code creation following the project's dev instructions, coding standards, and security requirements.

## When to Use

- Creating a new API endpoint (full vertical slice: Endpoint → Handler/Command/Query → Database)
- Defining or modifying Entity Framework Core entities and migrations
- Writing or updating request/response mapping logic
- Scaffolding feature folder structure for a new slice
- Updating existing endpoint logic (validation, error handling, mapping)

## References

> Use `semantic_search` or `grep_search` to query only the relevant sections of these files. Call `read_file` only when a targeted search returns insufficient context and the file is < 150 lines:
> - **For all code changes**: [csharp.instructions.md](../../instructions/csharp.instructions.md) — naming conventions, code conventions, formatting
> - **For new API endpoints or workflow guidance**: [dev.instructions.md](../../instructions/dev.instructions.md) — full development workflow
> - **For security-sensitive changes**: [security.instructions.md](../../instructions/security.instructions.md) — OWASP Top 10 security checklist

## Procedure

### Step 1: Gather Context

Before writing code:
1. **Look up C# standards** — Use `semantic_search` or `grep_search` on `csharp.instructions.md` for specific naming or formatting rules.
2. **Check security requirements** — Use `semantic_search` on `security.instructions.md` for the OWASP checks relevant to your change.

### Step 2: Create Feature Folder Structure

For a **new API feature**, create the vertical slice folders inside the API project. Example for a 'CreateTransaction' feature:

```
src/CorePortfolio.API/Features/CreateTransaction/
├── CreateTransactionEndpoint.cs
├── CreateTransactionCommand.cs (or Query)
├── CreateTransactionHandler.cs
├── CreateTransactionRequest.cs
├── CreateTransactionResponse.cs
└── CreateTransactionValidator.cs
```

### Step 3: Implement the API Layer (Endpoint)

Follow these rules from `csharp.instructions.md`:
- **File-scoped namespaces**
- **PascalCase** for classes, methods, properties
- **camelCase with underscore prefix** for private fields
- **No magic strings**

**Endpoint.cs pattern (using Minimal APIs or FastEndpoints as standard):**

```csharp
namespace CorePortfolio.API.Features.CreateTransaction;

public static class CreateTransactionEndpoint
{
    public static void MapCreateTransactionEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/transactions", HandleAsync)
           .WithName("CreateTransaction")
           .WithTags("Transactions");
    }

    private static async Task<IResult> HandleAsync(
        [FromBody] CreateTransactionRequest request,
        [FromServices] ICommandHandler<CreateTransactionCommand, CreateTransactionResponse> handler,
        CancellationToken cancellationToken)
    {
        // 1. Map request to command
        var command = new CreateTransactionCommand(request.AssetId, request.Amount, request.Price);
        
        // 2. Invoke handler
        var response = await handler.HandleAsync(command, cancellationToken);
        
        // 3. Return TypedResults
        return TypedResults.Ok(response);
    }
}
```

### Step 4: Implement the Domain and Infrastructure Logic

**Entities:** Place core entities in the Domain layer or folder.
**Handlers:** Implement business logic and database interactions in the Handler.

```csharp
namespace CorePortfolio.API.Features.CreateTransaction;

public class CreateTransactionHandler : ICommandHandler<CreateTransactionCommand, CreateTransactionResponse>
{
    private readonly AppDbContext _dbContext;

    public CreateTransactionHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CreateTransactionResponse> HandleAsync(CreateTransactionCommand command, CancellationToken cancellationToken)
    {
        // 1. Create domain entity
        var transaction = new Transaction
        {
            AssetId = command.AssetId,
            Amount = command.Amount,
            Price = command.Price,
            Date = DateTime.UtcNow
        };

        // 2. Save to SQLite via EF Core
        _dbContext.Transactions.Add(transaction);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 3. Return response
        return new CreateTransactionResponse(transaction.Id);
    }
}
```

### Step 5: Validate Against Checklists

**Code quality:**
- [ ] Vertical slice organization (one feature per folder)
- [ ] Dependencies properly injected via DI
- [ ] EF Core usage is optimized (e.g., AsNoTracking for read-only queries)
- [ ] No hardcoded secrets or configuration values
- [ ] Proper error handling (no stack traces exposed)
- [ ] Single Responsibility Principle followed

**Security:**
- [ ] No hardcoded secrets
- [ ] Input validation at API boundary using FluentValidation or DataAnnotations
- [ ] No sensitive data in logs
- [ ] No SQL injection vulnerabilities (EF Core parameterizes LINQ queries automatically)

**Naming:**
- [ ] PascalCase for public members
- [ ] camelCase with `_` prefix for private fields
- [ ] File name matches class name
