# Credential exposure response

Use this runbook whenever a token, API key, password, connection string, or signing key is committed to Git.

## Immediate containment

1. Treat the credential as compromised. Do not test whether it still works.
2. Revoke or rotate it at the issuing service before cleaning Git history.
3. Store the replacement directly in .NET User Secrets for local development or in the deployment platform's secret settings.
4. Restart the affected service and verify health without logging the replacement value.

For CorePortfolio, the production environment-variable names are:

- `TelegramBot__Token`
- `TelegramBot__AllowedChatId`
- `CoinGecko__ApiKey`
- `Jwt__Key`
- `Jwt__Issuer`
- `Jwt__Audience`

Rotating `Jwt__Key` invalidates existing access tokens. Users must sign in again.

## Repository cleanup

Removing a secret from the current file does not remove it from Git history. Use `git-filter-repo` from a fresh mirror clone to replace the exact compromised values across every affected ref, verify that no matching values remain, then force-push the rewritten refs.

Before rewriting history:

- Confirm the replacement credentials are already active.
- Record the affected paths and commits without copying secret values into tickets or logs.
- Check forks, pull requests, tags, protected branches, and active collaborators.
- Notify collaborators that commit hashes will change and old clones must not be merged back.

After rewriting history:

- Ask GitHub Support to purge cached views when required.
- Re-clone the repository or reset existing clones to the rewritten history.
- Re-run the repository secret scanner and review GitHub secret-scanning alerts.

## Prevention

Run the local scanner before pushing:

```powershell
npm run check:secrets
```

The scanner inspects tracked files and reports only detector names and locations, never credential values. GitHub Actions runs the same check for pushes and pull requests. Keep GitHub secret scanning and push protection enabled for the repository and user account.
