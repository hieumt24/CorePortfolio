# Privileged-account two-factor authentication

CorePortfolio supports RFC 6238 TOTP authenticator apps and single-use recovery
codes. The login and Profile Security experiences support enrollment by QR or
manual key, TOTP verification, recovery-code login, recovery-code rotation, and
optional disablement.

## Enforcement scope

When `Security__TwoFactor__EnforceForPrivilegedRoles=true`, every role with the
`AdminAccess` permission must complete two-factor authentication:

- `SuperAdmin`
- `Admin`
- `Operations`
- `Support`
- `MarketDataManager`
- `Auditor`

Accounts that have voluntarily enabled two-factor authentication must always
complete it, regardless of the enforcement flag.

Password verification for a protected account creates only a short-lived,
hashed challenge. CorePortfolio does not create a JWT, refresh cookie, or
`UserSession` until the TOTP or recovery code succeeds.

## Production configuration

Configure these settings through the Azure App Service configuration or another
secret store:

```text
Security__TwoFactor__EncryptionKey=<Base64-encoded 32-byte random key>
Security__TwoFactor__EnforceForPrivilegedRoles=false
```

The encryption key must not be committed, logged, exposed to the frontend, or
stored in a database backup. It protects each user's TOTP secret with AES-GCM
and binds the ciphertext to that user ID. Back up the key independently and
restrict access to the API runtime identity. Losing or changing the key without
a migration makes enrolled TOTP secrets unreadable.

Generate the value with a cryptographically secure random-number generator.
Do not reuse the JWT signing key or a provider credential.

Startup validation rejects enforcement when the configured key is absent or
not exactly 32 decoded bytes. Keep enforcement disabled until every privileged
operator has a tested enrollment/recovery path. The Admin User Security page
reports enrollment coverage and readiness.

When enforcement is disabled, the API can still start without the key so that
existing non-2FA sign-in remains available. In that state,
`GET /api/profile/2fa` returns `isAvailable: false`, the Profile Security UI
disables enrollment, and setup requests return HTTP 503 instead of an
unhandled HTTP 500. Configure the key and restart the API before enrolling any
account.

For Azure App Service, add `Security__TwoFactor__EncryptionKey` under
Environment variables, mark it as a deployment-slot setting when slots are
used, save the configuration, and restart the API. A valid value can be
generated locally with:

```powershell
[Convert]::ToBase64String(
  [Security.Cryptography.RandomNumberGenerator]::GetBytes(32)
)
```

Store the generated value directly in the platform secret configuration. Do
not paste it into source files, logs, tickets, or chat.

## API contract

- `POST /api/auth/login` returns `Authenticated`, `TwoFactorRequired`, or
  `TwoFactorSetupRequired`.
- `POST /api/auth/2fa/setup` exchanges an enrollment challenge for an
  authenticator provisioning URI and manual key.
- `POST /api/auth/2fa/verify` consumes a TOTP or recovery code and only then
  issues the JWT and refresh cookie.
- `GET /api/profile/2fa` returns the authenticated user's status and server
  enrollment availability.
- `POST /api/profile/2fa/setup` begins voluntary enrollment after password
  re-verification.
- `POST /api/profile/2fa/recovery-codes` rotates recovery codes after password
  and TOTP verification.
- `DELETE /api/profile/2fa` disables optional 2FA after password and TOTP
  verification. Enforced privileged accounts cannot disable it.

TOTP challenges expire after five minutes by default and lock after five
failed attempts. The anonymous setup and verification endpoints are also
rate-limited by client IP. Recovery codes are high-entropy, single-use values;
only SHA-256 hashes are persisted.

Expired or consumed challenges are removed by the hosted cleanup service after
the configured retention period:

```text
Security__TwoFactor__CleanupIntervalMinutes=60
Security__TwoFactor__ChallengeRetentionHours=24
```

When privileged-role enforcement is enabled, Admin authorization policies and
the MediatR admin-command boundary both require an `amr` claim produced by TOTP
or recovery-code verification. Session validation independently checks the
persisted MFA verification timestamp.

## SuperAdmin recovery reset

Only `SuperAdmin` has the `TwoFactor.Reset` permission. The Admin User Security
page requires the operator to enter the target username exactly and provide a
10–200 character audit reason. A reset cannot target the current SuperAdmin
account. It clears the target's authenticator secret, recovery codes, and
challenges, revokes all sessions, and records `UserTwoFactorReset`.

Use a separate enrolled SuperAdmin account for this break-glass operation.
After reset, a privileged target must enroll again at the next login when
enforcement is enabled.

## Deployment and rollback

1. Configure and back up the encryption key.
2. Deploy the API and apply `AddAdminTwoFactorFoundation`.
3. Leave privileged-role enforcement disabled.
4. Deploy the enrollment/login and Profile Security UI.
5. Enroll and test every privileged operator, including recovery codes.
6. Confirm the Admin coverage panel reports 100% and "ready for enforcement".
7. Enable enforcement and restart the API.
8. Confirm password-only Admin sessions receive HTTP 403 and a fresh TOTP login
   restores Admin access.

The migration is additive and does not enable 2FA for existing users. A safe
application rollback leaves the new tables and columns in place and turns the
enforcement flag off. Do not roll the database migration back after any user
has enrolled.

Enabling, disabling, verification failures, recovery-code rotation, and
successful second-factor verification produce audit events without including
secrets or submitted codes.
