# Contributing to Zraxion

Thank you for helping improve Zraxion.

Zraxion targets **SQL Server Management Studio 22** and prioritizes correctness, financial precision, predictable Results Grid behavior, and maintainable SSMS integration.

## Before contributing

Please keep changes:

- focused
- reproducible
- easy to review
- compatible with SSMS 22
- safe for financial/reconciliation workflows
- free of unrelated refactors

Large architectural changes should be discussed in an issue before implementation.

## Development requirements

- Windows x64
- SQL Server Management Studio 22
- Visual Studio 2026 or compatible VSSDK tooling
- .NET SDK capable of building the solution
- .NET Framework 4.7.2 targeting support

## Build

```powershell
git clone https://github.com/LaneZero/Zraxion.git
Set-Location Zraxion

dotnet restore .\Zraxion.slnx
dotnet build .\Zraxion.slnx -c Debug
```

Before a pull request, also run:

```powershell
dotnet clean .\Zraxion.slnx -c Release
dotnet build .\Zraxion.slnx -c Release
```

Expected result:

```text
0 Warning(s)
0 Error(s)
```

## Results Grid changes

Changes affecting Results Grid behavior should be manually validated in SSMS 22.

At minimum test:

- selection analytics
- column analytics
- Analytics Bar
- exact comparison
- tolerance comparison
- multiple Results tabs
- tab close/reopen lifecycle
- copy actions
- theme readability

Precision changes should include edge cases around SQL `DECIMAL/NUMERIC`, high significant-digit counts, very small fractional differences, negative values, zero baselines, and culture-sensitive formatting.

## Coding expectations

- Prefer public SSMS/VSSDK APIs over reflection when available.
- Do not silently fall back to floating-point arithmetic for financial values.
- Keep UI-thread requirements explicit.
- Avoid blocking SSMS startup on network or expensive work.
- Treat settings/IO failures as non-fatal unless correctness requires otherwise.
- Preserve per-tab isolation for comparison state.
- Keep user-facing behavior deterministic.

## Commit convention

Use Conventional Commit-style messages:

- `feat:` new functionality
- `fix:` bug fixes
- `docs:` documentation
- `test:` tests
- `refactor:` internal restructuring
- `build:` build or dependency changes
- `chore:` maintenance work

Examples:

```text
feat(compare): add tolerance rule indicator
fix(grid): preserve active tab comparison state
docs(readme): document release installation
```

## Pull requests

A useful pull request should include:

1. What changed
2. Why it changed
3. How it was tested
4. SSMS version used
5. Screenshots for visible UI changes
6. Any known limitations or follow-up work

Do not include private logs, credentials, database connection strings, production data, API keys, or customer information.

## Licensing

By contributing to Zraxion, you agree that your contribution may be distributed under the project's **Apache License 2.0**.

Unless explicitly stated otherwise, contributions intentionally submitted for inclusion in the project are provided under the terms described by Apache License 2.0 Section 5.

## Security

Do not publish credentials or sensitive production data in issues.

For a security-sensitive report, provide the smallest reproducible description possible and avoid posting secrets or exploit material that could unnecessarily expose real systems.
