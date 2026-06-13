# OWASP Top 10 Security Checklist

1. Broken Access Control: Ensure endpoints have proper `[Authorize]` attributes if needed.
2. Cryptographic Failures: Never hardcode secrets. Use UserSecrets in dev, KeyVault in prod.
3. Injection: Ensure EF Core is used with LINQ to automatically parameterize queries. Do not use raw string concatenation in SQL queries.
4. Insecure Design: Validate business logic at the API boundary.
5. Security Misconfiguration: Ensure HTTPS redirection is enabled.
6. Vulnerable Components: Keep NuGet packages updated.
7. Authentication Failures: Ensure Identity configurations are secure.
8. Data Integrity Failures: Validate data mutations.
9. Logging Failures: Do not log sensitive data like passwords or full connection strings.
10. SSRF: Validate all URLs passed from clients.
