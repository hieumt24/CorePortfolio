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
        VERSION: "1.0.0"
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
