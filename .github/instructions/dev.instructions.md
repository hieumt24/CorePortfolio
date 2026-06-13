# General Development Workflow

1. Identify the Vertical Slice Feature being implemented.
2. Create the appropriate directory under `src/CorePortfolio.API/Features/`.
3. Create Request, Response, Command/Query, and Endpoint classes.
4. Implement the Handler interacting with the SQLite database via EF Core.
5. Register necessary services in DI container.
