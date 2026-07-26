# CorePortfolio

CorePortfolio is a comprehensive portfolio tracking platform that consolidates investments across different asset classes, providing real-time tracking, analytics, and notifications.

## Architecture

The project is built using a modern full-stack architecture:

- **Backend** (`/backend/src`): A robust .NET API built with Clean Architecture, CQRS (MediatR), and integration with multiple external services:
  - **Coingecko**: Cryptocurrency data tracking.
  - **KBS**: Keyless Vietnamese stock/ETF prices and instrument lookup.
  - **Telegram**: Real-time notifications and bot interactions.
- **Frontend** (`/frontend`): A fast, responsive Single Page Application (SPA) built with React 19, TypeScript, Vite, and Recharts for data visualization.

## Getting Started

### Prerequisites
- [.NET SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) (v18+)
- npm or yarn

### Running the Backend
1. Navigate to the `backend/src/CorePortfolio.API` directory.
2. Update `appsettings.json` with optional CoinGecko and Telegram credentials if necessary. KBS stock pricing does not require an API key.
3. Run the application:
   ```bash
   dotnet run
   ```

Production market-data settings and secret-handling guidance are documented in
[`docs/PRODUCTION_MARKET_DATA.md`](docs/PRODUCTION_MARKET_DATA.md).
Production client-IP, reverse-proxy, and user-presence settings are documented in
[`docs/PRODUCTION_USER_ACTIVITY.md`](docs/PRODUCTION_USER_ACTIVITY.md).

### Running the Frontend
1. Navigate to the `frontend` directory.
2. Install dependencies:
   ```bash
   npm install
   ```
3. Start the development server:
   ```bash
   npm run dev
   ```

## License
MIT License
