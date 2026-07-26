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
3. **Rigorous Verification**: You MUST always verify that your code changes compile successfully and fix any build errors BEFORE you create the `walkthrough.md`. 
   - For backend: Do NOT just run `dotnet build` at the root folder as it might hide errors. Explicitly build the modified project (e.g., `dotnet build src/CorePortfolio.API/CorePortfolio.API.csproj`) or use `dotnet run` to catch startup DI errors.
   - For frontend: Always run `npm run build` in the frontend directory.
4. **Vietnamese Localization & String Matching**: This project uses Vietnamese data. When writing hardcoded logic that checks or matches entity names (e.g. Categories, Asset Types), you MUST account for both English keywords and their Vietnamese equivalents (e.g. checking both "stock" and "chứng khoán", "fund" and "chứng chỉ quỹ"). Do not assume English-only data.

## Market Data Provider Rules
1. **Vietnamese Stocks/ETFs**: Use the keyless KBS adapter in `CorePortfolio.KBS` for Vietnamese stock and ETF prices and instrument lookup. Persist the source as `KBS`; `DNSE` is a legacy source value that must be normalized to `KBS`.
2. **Price Unit**: KBS raw daily OHLC `c` values are already absolute VND (for example `22400` means 22,400 VND). Never apply vnstock's display-oriented `/ 1000` conversion when persisting CorePortfolio prices.
3. **Failure Safety**: Cache provider results, validate symbols, and preserve the last known price with `Stale` status on transient upstream failures.
4. **Provider Boundary**: Keep the integration as a small typed .NET HTTP adapter. Do not add a Python runtime or copy vnstock implementation code into this repository. The current KBS integration is intended for CorePortfolio's personal-use scope; re-check upstream data terms before commercial deployment.
5. **Vnstock Account Keys**: `VNSTOCK_API_KEY` activates the Python `vnstock`/`vnai` runtime and is not a KBS credential. Never forward it to KBS, commit it, place it in frontend configuration, or log it. If a future Python market-data worker is approved, inject the key into that isolated worker through a secret store and keep the .NET adapter keyless.
