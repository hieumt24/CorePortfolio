module.exports = {
  apps: [
    {
      name: "backend",
      script: "dotnet",
      args: "run --project src/CorePortfolio.API/CorePortfolio.API.csproj -c Release",
      cwd: "/home/hieu-mai-trong/Project/CorePortfolio/backend",
      env: {
        ASPNETCORE_ENVIRONMENT: "Production",
        ASPNETCORE_URLS: "http://0.0.0.0:5211",
        VERSION: "1.0.0",
        DNSE__BaseUrl: process.env.DNSE__BaseUrl || process.env.DNSE_BASE_URL || "https://openapi.dnse.com.vn",
        DNSE__ApiVersion: process.env.DNSE__ApiVersion || process.env.DNSE_API_VERSION || "2026-05-07",
        ...(process.env.DNSE__ApiKey || process.env.DNSE_API_KEY
          ? { DNSE__ApiKey: process.env.DNSE__ApiKey || process.env.DNSE_API_KEY }
          : {}),
        ...(process.env.DNSE__SecretKey || process.env.DNSE_SECRET_KEY
          ? { DNSE__SecretKey: process.env.DNSE__SecretKey || process.env.DNSE_SECRET_KEY }
          : {})
      }
    },
    {
      name: "frontend",
      script: "npm",
      args: "run preview -- --host --port 5173 --strictPort",
      cwd: "/home/hieu-mai-trong/Project/CorePortfolio/frontend",
      env: {
        NODE_ENV: "production"
      }
    }
  ]
};
