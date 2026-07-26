# Production Market Data Runbook

## Current architecture

CorePortfolio uses the native .NET `CorePortfolio.KBS` adapter for Vietnamese
stock and ETF prices. It calls the public KBS market-data endpoints directly and
does not execute the Python `vnstock` package.

The Vnstock account API key is therefore **not an authentication credential for
KBS** and must not be attached to KBS requests. Vnstock documents that the key
activates the Python library's account tier and request quota; it does not
document a stock-price REST gateway contract for the .NET application.

## Azure App Service configuration

Set these values under **App Service → Settings → Environment variables → App
settings**. ASP.NET Core maps the double underscore to nested configuration.

| Setting | Recommended value | Purpose |
| --- | --- | --- |
| `MarketPrices__Enabled` | `true` | Enables background market-price refresh. |
| `MarketPrices__StockRefreshIntervalSeconds` | `1800` | Refreshes stale stocks every 30 minutes. |
| `KBS__BaseUrl` | `https://kbbuddywts.kbsec.com.vn/iis-server/investment/` | KBS market-data base URL. |
| `KBS__TimeoutSeconds` | `20` | Upstream request timeout. |
| `KBS__LookbackDays` | `14` | Daily history window used to find the latest trading session. |
| `KBS__PriceCacheSeconds` | `300` | In-memory quote cache duration. |
| `KBS__InstrumentCacheHours` | `6` | In-memory instrument catalog cache duration. |

Do not add `VNSTOCK_API_KEY` to the current .NET App Service: no current
component consumes it.

Operational recommendations:

1. Allow outbound HTTPS/DNS access to `kbbuddywts.kbsec.com.vn`.
2. Enable **Always On** when the App Service plan supports it so scheduled
   refreshes are not suspended.
3. Keep one application instance while production still uses SQLite and
   in-memory provider caches. Move persistence and distributed locking/cache
   before scaling out.
4. Persist SQLite under `/home/data/CorePortfolio.db`, or configure
   `ConnectionStrings__DefaultConnection` explicitly.
5. Monitor `KBS` warnings and the `PriceStatus`/`LastPriceError` fields. A
   transient failure preserves the last known price and marks it `Stale`.
6. Validate `/health/live` and `/health/ready`.
7. Open the authenticated **Admin → Market Assets** screen, confirm a
   Stock/Cổ phiếu/Chứng khoán category exists, then click **Sync VN100**. The
   operation is idempotent and can be rerun after index rebalancing; it creates
   new constituents and updates existing metadata/reference prices without
   deleting assets that left the index.
8. Open a portfolio, choose **Add Asset**, and verify searching both `HPG` and
   `Hòa Phát` returns the same Market Asset.

## If a Python Vnstock worker is introduced later

Only the Python process that imports `vnstock`/`vnai` should receive:

```text
VNSTOCK_API_KEY=<secret supplied at deployment time>
VNSTOCK_INTERACTIVE=0
```

Recommended Azure setup:

1. Store the key in Azure Key Vault.
2. Enable a managed identity for the worker App Service/container.
3. Grant that identity `Key Vault Secrets User`.
4. Configure `VNSTOCK_API_KEY` as an App Service Key Vault reference and mark it
   as deployment-slot specific.
5. Never put the key in `appsettings.json`, Docker build arguments, frontend
   variables, Vercel, repository secrets exposed to pull requests, command
   output, or application logs.
6. Register/validate the key at worker startup, log only whether activation
   succeeded, and never log any part of the key.
7. Put the worker behind an internal authenticated API and let CorePortfolio
   call that API. Do not install Python inside the existing ASP.NET process.

The free Community tier is documented as 60 library requests per minute. Add a
queue, caching, exponential backoff, and a concurrency limit before routing
production refreshes through that worker.

## Secret incident response

If a key is pasted into chat, an issue, logs, or source control, treat it as
exposed:

1. Revoke or rotate it in the Vnstock account dashboard.
2. Replace the Key Vault secret with the new value.
3. Restart or redeploy only the Python worker that consumes it.
4. Confirm old credentials no longer authenticate.

## References

- [Vnstock official repository and authentication tiers](https://github.com/thinh-vu/vnstock)
- [Vnstock server/CLI environment variables](https://vnstocks.com/onboard-member/cai-dat-go-loi/cai-dat-nang-cao)
- [Azure App Service environment variables](https://learn.microsoft.com/en-us/azure/app-service/configure-common)
- [Azure App Service Key Vault references](https://learn.microsoft.com/en-us/azure/app-service/app-service-key-vault-references)
