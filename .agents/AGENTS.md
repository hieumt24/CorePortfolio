# CorePortfolio Agent Rules

These rules apply to all AI agents working within the CorePortfolio workspace. You MUST follow them strictly.

## Backend Architecture Requirements (C# / ASP.NET Core)
1. **Vertical Slice Architecture**: All features MUST be organized inside the `CorePortfolio.API/Features` directory. Features should be grouped logically by domain (e.g., `Features/Portfolios`, `Features/Cashflows`).
2. **MediatR**: All business logic MUST be encapsulated in Commands and Queries using the MediatR pattern. Handlers should be placed alongside their respective Commands/Queries.
3. **No MVC Controllers**: You must NEVER use `[ApiController]`, `ControllerBase`, or MVC routing.
4. **Minimal APIs**: All API endpoints MUST be exposed using Minimal APIs. 
   - Each feature domain should have its own static endpoint class (e.g., `CashflowsEndpoints.cs`).
   - Define a static extension method (e.g., `public static void MapCashflowsEndpoints(this IEndpointRouteBuilder app)`).
   - Ensure you register this extension method inside `Program.cs` (e.g., `app.MapCashflowsEndpoints();`).

## Agent Behavior Requirements
1. **Always Review Skills**: Before writing or modifying any code, you MUST explicitly check if there is an applicable Skill in the user's workspace or global config. Use the `view_file` tool to read the `SKILL.md` file of any relevant skill (e.g., `coreportfolio-fe-architecture`, `write-fe-component`, `coreportfolio-react-best-practices`) and follow its instructions completely.
2. **Analyze Existing Patterns**: Never assume the project uses standard boilerplate (like MVC). Always use `grep_search` or `view_file` on `Program.cs` or existing feature folders to understand the exact architecture before writing new endpoints or components.
