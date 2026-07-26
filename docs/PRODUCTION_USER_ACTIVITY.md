# Production user activity

CorePortfolio stores only the latest successful-login IP address for each user,
not an IP history. `LastActivityAt` is refreshed by authenticated API traffic and
the Admin Users page derives Online/Offline from that timestamp.

## Runtime settings

Recommended defaults:

```text
UserActivity__OnlineWindowMinutes=5
UserActivity__WriteIntervalSeconds=60
```

- `OnlineWindowMinutes` is clamped to 1–60 minutes.
- `WriteIntervalSeconds` is clamped to 15–300 seconds to avoid writing SQLite on
  every authenticated request.
- The Admin Users screen refreshes presence data every 60 seconds.

## Client IP behind a reverse proxy

The API reads `HttpContext.Connection.RemoteIpAddress`; it never reads a
user-supplied `X-Forwarded-For` header directly. Forwarded-header processing is
disabled by default because trusting an unverified proxy allows IP spoofing.

For Azure App Service, or another deployment where the application worker can
only be reached through the platform ingress, configure:

```text
ForwardedHeaders__Enabled=true
ForwardedHeaders__ForwardLimit=1
ForwardedHeaders__TrustAllProxies=true
```

Set `ForwardLimit` to the exact number of trusted proxy hops. Enable
`TrustAllProxies` only when firewall/network configuration prevents clients from
reaching the application process directly. For a self-managed server, keep it
disabled and terminate traffic through a known loopback proxy, or extend the
application configuration with explicit trusted proxy addresses before launch.

Restart the API after changing these settings, sign in again, and verify that
Admin → Users shows the public client IP rather than the proxy address.

## Privacy and operations

- IP address and activity timestamps are returned only by Admin-protected APIs.
- Treat IP addresses as personal data: document the purpose, limit administrator
  access, and include the field in account-deletion/export procedures.
- The current schema retains one latest IP per account until it is replaced or
  the account is deleted. Add a scheduled retention/anonymization policy before
  using the feature in a jurisdiction or organization that requires it.
- Failed login attempts are not stored by this feature.

The `AddUserLoginTelemetry` EF migration is applied during normal API startup.
After deployment, confirm `/health/ready` is healthy before testing login.
