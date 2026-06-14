module.exports = {
  apps: [
    {
      name: "backend",
      script: "dotnet",
      args: "run --project src/CorePortfolio.API/CorePortfolio.API.csproj -c Release",
      cwd: "./backend",
      env: {
        ASPNETCORE_ENVIRONMENT: "Production",
        ASPNETCORE_URLS: "http://0.0.0.0:5211"
      }
    },
    {
      name: "frontend",
      script: "npm",
      args: "run preview -- --host --port 5173",
      cwd: "./frontend",
      env: {
        NODE_ENV: "production"
      }
    }
  ]
};
