# Copilot Instructions for CorePortfolio

## Project Overview

This is the core portfolio tracking web application, containing both Backend and Frontend components.
CorePortfolio is designed using the Clean Architecture and Vertical Slice Architecture for the backend.

The source code solution will be structured with the following key components:
- `Backend` - ASP.NET Core Web API (Domain, Infrastructure with SQLite, and API features grouped by Vertical Slices)
- `Frontend` - React Web Application

## Technology Stack

- **Backend**: ASP.NET Core Web API (C# - .NET 8.0)
- **Frontend**: React
- **Database**: SQLite
- **Testing**: xUnit/NUnit for Backend unit tests (Unit test and Functional tests are NOT required for this project)

---

## Code Standards

When writing, modifying code, or reviewing the code, follow these standards based on file type:

### C# Files (*.cs)
Apply standards from [csharp.instructions.md](.github/instructions/csharp.instructions.md)

---

## Security Requirements

Apply OWASP Top 10 (2021) security checks from [security.instructions.md](.github/instructions/security.instructions.md)

1. **Broken Access Control** - Verify authorization on all endpoints
2. **Cryptographic Failures** - Never hardcode secrets; use strong encryption
3. **Injection** - Use parameterized queries; validate all inputs (especially EF Core/SQLite queries)
4. **Insecure Design** - Implement rate limiting; apply defense in depth
5. **Security Misconfiguration** - Disable debug in production; configure security headers
6. **Vulnerable Components** - Keep dependencies updated; check for CVEs
7. **Authentication Failures** - Use secure session configuration; strong passwords
8. **Data Integrity Failures** - Use safe deserialization; validate integrity
9. **Logging Failures** - Log security events without sensitive data
10. **SSRF** - Validate all user-provided URLs against allowlists

---

## Code Review

Before submitting changes, ensure code passes review against:

1. **Naming conventions** match the established patterns. Strictly follow instructions per file type.
2. **No code smells**: God classes, long methods, magic numbers, duplicate code, deep nesting
3. **Security**: No hardcoded secrets, proper input validation, authorization checks
4. **Testing**: Appropriate unit test coverage for new functionality
5. **Architecture**: Adherence to Vertical Slice Architecture (Features contain their own Request, Handler, Response, and Repository logic where appropriate)

Use the [@coreportfolio-code-reviewer](.github/agents/coreportfolio-code-reviewer.agent.md) agent for comprehensive pre-PR reviews.

---

## Common Patterns

### Vertical Slice Architecture
- Group files by Feature (e.g., `GetPortfolioDetails`, `CreateTransaction`).
- Each Feature folder contains its Endpoint, Command/Query, Handler, and Validation logic.

### Dependency Injection
- Register services in `Program.cs` or specific `DependencyInjection.cs` modules.
- Use constructor injection for dependencies.

### Error Handling
- Use try-catch for expected exceptions or utilize Global Exception Handling middleware.
- Never expose stack traces to users.
- Log errors with context but without sensitive data.

### Unit Testing
- Name tests: `MethodName_Scenario_ExpectedResult`
- Use Arrange-Act-Assert pattern
- Mock external dependencies
- Test edge cases and error paths
