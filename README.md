# CorePortfolio

CorePortfolio is a comprehensive portfolio tracking platform that consolidates investments across different asset classes, providing real-time tracking, analytics, and notifications.

## Architecture

The project is built using a modern full-stack architecture:

- **Backend** (`/backend/src`): A robust .NET API built with Clean Architecture, CQRS (MediatR), and integration with multiple external services:
  - **Coingecko**: Cryptocurrency data tracking.
  - **DNSE**: Vietnamese stock market broker integration.
  - **Telegram**: Real-time notifications and bot interactions.
- **Frontend** (`/frontend`): A fast, responsive Single Page Application (SPA) built with React 19, TypeScript, Vite, and Recharts for data visualization.

## Getting Started

### Prerequisites
- [.NET SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) (v18+)
- npm or yarn

### Running the Backend
1. Navigate to the `backend/src/CorePortfolio.API` directory.
2. Update `appsettings.json` with your API keys (Coingecko, DNSE, Telegram) if necessary.
3. Run the application:
   ```bash
   dotnet run
   ```

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