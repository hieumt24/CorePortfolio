---
name: coreportfolio-code-review
description: "Reviews C# code changes before raising a PR against team coding standards, OWASP security requirements, and best practices. Use when: reviewing PRs, checking new feature slices, or validating domain/infrastructure code."
---

# Code Review

Perform a comprehensive code review of C# files.

## References

> Use `semantic_search` or `grep_search` to query only the relevant sections of these files. Call `read_file` only when a targeted search returns insufficient context:
> - **For all code changes**: [csharp.instructions.md](../../instructions/csharp.instructions.md) — naming conventions, code conventions, formatting
> - **For security-sensitive changes**: [security.instructions.md](../../instructions/security.instructions.md) — OWASP Top 10 security checklist

## Procedure

### Step 1: Analyze Code Changes
Review the provided code or the git diff. Look for logical errors, performance issues, and architectural violations.

### Step 2: Enforce C# Standards
Check the code against `csharp.instructions.md`. Key checks:
- File-scoped namespaces
- Proper naming conventions (PascalCase for classes/methods, camelCase with `_` for private fields)
- Proper use of asynchronous programming (`async`/`await`)
- Vertical Slice Architecture boundaries are respected (e.g., Feature A does not tightly couple to Feature B)

### Step 3: Enforce Security Guidelines
Check the code against `security.instructions.md`. Key checks:
- Proper input validation
- Authorization checks are present
- No SQL injection vectors (ensure EF Core is used safely without raw unparameterized SQL)
- No hardcoded sensitive data

### Step 4: Format the Output
Produce a review report in the following structure:

**📌 Applied Standards**
(List the standard documents referenced)

**✅ What's Good**
(Highlight 1-2 positive aspects of the code)

**⚠️ Issues Found**
For each issue, provide:
- **Severity**: High/Medium/Low
- **Category**: Security/Architecture/Naming/Logic/Performance
- **Location**: File name and approximate line numbers
- **Issue**: Description of the problem
- **Violation**: The specific rule being broken
- **Recommendation**: How to fix it (provide code snippet if helpful)

**📋 Summary**
- Total issues: X
- Must-fix count: Y
- Nice-to-have count: Z
