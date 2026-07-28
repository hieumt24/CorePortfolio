# Privileged-account two-factor authentication

CorePortfolio supports RFC 6238 TOTP authenticator apps and single-use recovery
codes. The backend foundation is available before the enrollment UI so that the
database and authentication contract can be deployed independently.

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
not exactly 32 decoded bytes. Keep enforcement disabled until the Sprint 1 UI
is deployed and every privileged operator has a tested enrollment/recovery
path.

## API contract

- `POST /api/auth/login` returns `Authenticated`, `TwoFactorRequired`, or
  `TwoFactorSetupRequired`.
- `POST /api/auth/2fa/setup` exchanges an enrollment challenge for an
  authenticator provisioning URI and manual key.
- `POST /api/auth/2fa/verify` consumes a TOTP or recovery code and only then
  issues the JWT and refresh cookie.
- `GET /api/profile/2fa` returns the authenticated user's status.
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

## Deployment and rollback

1. Configure and back up the encryption key.
2. Deploy the API and apply `AddAdminTwoFactorFoundation`.
3. Leave privileged-role enforcement disabled.
4. Deploy the Sprint 1 enrollment/login UI.
5. Enroll and test every privileged operator, including recovery codes.
6. Enable enforcement and restart the API.

The migration is additive and does not enable 2FA for existing users. A safe
application rollback leaves the new tables and columns in place and turns the
enforcement flag off. Do not roll the database migration back after any user
has enrolled.

Enabling, disabling, verification failures, recovery-code rotation, and
successful second-factor verification produce audit events without including
secrets or submitted codes.
