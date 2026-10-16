<div align="center">

<img src="docs/assets/zraxion-logo.png" alt="Zraxion" width="430">

# Zraxion

### Financial-grade Results Grid analytics for SQL Server Management Studio 22

**Exact precision · Selection analytics · Column analytics · A/B comparison · Tolerance rules · Per-tab state**

[![Release](https://img.shields.io/badge/release-v1.0.0-0A84FF?style=flat-square)](https://github.com/LaneZero/Zraxion/releases)
[![SSMS](https://img.shields.io/badge/SSMS-22-CC2927?style=flat-square)](https://learn.microsoft.com/sql/ssms/)
[![Platform](https://img.shields.io/badge/platform-Windows%20x64-0078D4?style=flat-square)](#requirements)
[![License](https://img.shields.io/badge/license-Apache--2.0-4C8BF5?style=flat-square)](LICENSE)

[Features](#features) · [Video Guide](#video-guide) · [Installation](#installation) · [Usage](#usage) · [Uninstall](#uninstall-zraxion) · [Precision](#financial-precision) · [Support](#support-the-project) · [Build](#building-from-source) · [Contributing](CONTRIBUTING.md)

</div>

---

## Overview

**Zraxion** is an open-source productivity extension for **SQL Server Management Studio 22** focused on fast, precise analysis directly inside the SSMS Results Grid.

Its main goal is simple: let database developers, DBAs, analysts, and finance-oriented SQL users inspect numeric result sets without repeatedly copying data into another tool.

Zraxion adds selection metrics, whole-column metrics, persistent analytics, A/B comparison, absolute and percentage tolerance rules, exact high-precision arithmetic, copy helpers, per-tab comparison state, and a native SSMS welcome/about experience.

The extension is designed around one principle:

> **Do the analysis where the data already is.**

Zraxion does not replace SQL, Power BI, Excel, or a reconciliation platform. It removes small but frequent workflow interruptions when inspecting query results inside SSMS.

---

## Video Guide

A complete walkthrough of Zraxion, including Results Grid analytics, the Analytics Bar, A/B comparison, tolerance rules, and the general workflow, is available on YouTube.

<div align="center">

[![Watch the Zraxion usage guide on YouTube](https://img.youtube.com/vi/lxmo-smLcOY/hqdefault.jpg)](https://www.youtube.com/watch?v=lxmo-smLcOY)

**[▶ Watch: How to use Zraxion](https://www.youtube.com/watch?v=lxmo-smLcOY)**

</div>

---

## Requirements

| Requirement | Supported |
|---|---|
| SQL Server Management Studio | **SSMS 22** |
| Operating system | **Windows x64** |
| Runtime | **.NET Framework 4.7.2+** |
| Package format | **VSIX** |
| Development environment | Visual Studio 2026 / compatible VSSDK tooling |

> [!IMPORTANT]
> Zraxion v1.0.0 targets **SSMS 22 only**. SSMS 21 and older releases are not supported by this release.

---

## Features

### Selection Analytics

Select numeric cells in an SSMS Results Grid and calculate:

- `SUM`
- `AVG`
- `COUNT`
- `MIN`
- `MAX`

`COUNT` represents the number of successfully parsed numeric cells, not the total number of selected cells.

Null/empty cells and non-numeric values are tracked independently so that mixed selections remain understandable rather than quietly producing misleading totals.

### Column Analytics

Run the same metrics against a complete result-grid column:

- SUM
- AVG
- COUNT
- MIN
- MAX

This is useful when validating totals, checking aggregates, or comparing query output without rewriting the SQL solely to inspect a column.

### Analytics Bar

Zraxion can attach a compact analytics bar directly to the active Results Grid.

The bar supports:

- live or manual refresh
- SUM / COUNT / AVG / MIN / MAX
- null/empty count
- non-numeric count
- baseline view
- last comparison view
- exact-value copy
- per-user metric visibility settings

The bar follows the SSMS theme and is intentionally compact so it does not compete with the query results.

### A/B Comparison

A selected value can be stored as **Baseline A**, then another selected value can be compared as **B**.

Zraxion reports:

- A
- B
- Difference (`B - A`)
- Absolute difference
- Difference percentage
- Match / mismatch status
- Match reason

Comparison state is isolated per Results tab. A baseline created in one Results tab is not silently reused in another.

### Tolerance Rules

Comparisons can use:

- Exact matching
- Absolute tolerance
- Percentage tolerance
- Combined tolerance

Combined mode uses:

```text
Absolute tolerance OR Percentage tolerance
```

Tolerance boundaries are inclusive.

Example:

```text
A = 100
B = 100.50
Absolute tolerance = 0.50
Result = MATCH
```

For percentage comparison, the baseline is treated by absolute magnitude:

```text
DifferencePercent = (B - A) / abs(A) * 100
```

If `A = 0`, percentage tolerance is unavailable for a non-zero `B`, while absolute tolerance can still be used.

### Copy Tools

Zraxion exposes copy actions for:

- current selection summary
- Baseline A
- B
- difference
- difference percentage
- full comparison summary

Exact/raw copies use invariant formatting so values are suitable for logs, reports, issue descriptions, and downstream tooling.

---

## Financial Precision

Zraxion v1.0.0 includes a dedicated arbitrary-precision decimal implementation for Results Grid analytics.

The numeric engine does **not** use `double` or `float` for financial calculations.

It supports:

- SQL `DECIMAL(38, n)` / `NUMERIC(38, n)` style values
- sums that grow beyond 38 digits
- exact subtraction
- exact minimum and maximum
- exact comparison boundaries
- controlled average division
- high-precision percentage display
- invariant raw copy

Example:

```sql
SELECT CAST(99999999999999999999999999999999999999 AS decimal(38,0)) AS Amount
UNION ALL
SELECT CAST(1 AS decimal(38,0));
```

Zraxion calculates the exact sum:

```text
100000000000000000000000000000000000000
```

No floating-point approximation is introduced.

### Culture-aware parsing

Results Grid text can be affected by Windows/SSMS culture settings.

Zraxion attempts parsing using both the current culture and invariant culture.

If both interpretations are valid but produce different numeric values, the value is treated as **ambiguous** instead of guessing.

This conservative behavior is intentional. For financial tooling, an explicit failure is safer than a plausible but incorrect number.

---

## Installation

1. Download the latest `Zraxion.Extension.vsix` from the GitHub Releases page.
2. Close SSMS.
3. Run the VSIX installer.
4. Install Zraxion for SQL Server Management Studio 22.
5. Start SSMS.

After installation or after upgrading to a newer Zraxion version, the Zraxion welcome tab is shown once.

### Results Grid context menu

Execute a query, then right-click inside the Results Grid:

```text
Zraxion
├─ Selection Analytics
│  ├─ SUM
│  ├─ AVG
│  ├─ COUNT
│  ├─ MIN
│  └─ MAX
├─ Column Analytics
│  ├─ SUM
│  ├─ AVG
│  ├─ COUNT
│  ├─ MIN
│  └─ MAX
├─ Compare
│  ├─ Set Selection as A
│  ├─ Compare Selection with A
│  ├─ Comparison Tolerance...
│  └─ Clear Baseline A
└─ Show Analytics Bar
```

---

## Usage

### Quick selection analysis

1. Run a query.
2. Select one or more numeric cells.
3. Right-click the Results Grid.
4. Open **Zraxion → Selection Analytics**.
5. Choose the required metric.

### Compare two values

1. Select the first numeric value.
2. Choose **Zraxion → Compare → Set Selection as A**.
3. Select the second value.
4. Choose **Compare Selection with A**.
5. Review the Analytics Bar.

### Reconciliation workflow

For financial reconciliation:

1. Keep the authoritative or expected value as **A**.
2. Compare the actual/current result as **B**.
3. Use `B - A` as the signed difference.
4. Configure absolute and/or percentage tolerance.
5. Copy the full comparison summary when evidence is required.

---

## Uninstall Zraxion

The recommended first option is to remove Zraxion through the normal VSIX/extension-management workflow available on the machine. If the extension remains installed, the normal uninstaller is unavailable, or an SSMS profile contains a stale Zraxion extension folder, the following PowerShell cleanup can be used.

> [!WARNING]
> Save all open queries and work before continuing. If SSMS is running, the script asks for confirmation before closing it. After confirmation, it first requests a normal close and then force-stops any remaining SSMS process so the extension files are not locked.

The script intentionally does **not** hard-code a Windows username or an SSMS profile hash. It discovers SSMS 22 profiles under the current user's `%LOCALAPPDATA%`.

Run the following in PowerShell:

```powershell
Clear-Host
$ErrorActionPreference = 'Stop'

$dllName = 'Zraxion.Extension.dll'
$ssmsRoot = Join-Path $env:LOCALAPPDATA 'Microsoft\SSMS'
$settingsPath = Join-Path $env:LOCALAPPDATA 'Zraxion'

Write-Host 'Zraxion manual uninstall' -ForegroundColor Cyan
Write-Host '-------------------------' -ForegroundColor Cyan

# SSMS must be closed before extension files are removed.
$ssmsProcesses = @(Get-Process -Name 'ssms' -ErrorAction SilentlyContinue)

if ($ssmsProcesses.Count -gt 0) {
    Write-Host ''
    Write-Warning 'SQL Server Management Studio is currently running.'
    Write-Warning 'Save all open queries and work before continuing.'
    Write-Warning 'If SSMS does not close normally, this script may force-stop the remaining process after confirmation.'

    $answer = (Read-Host 'Close SSMS and continue with the Zraxion uninstall? [y/N]').Trim()

    if ($answer -notmatch '^(?i:y|yes)$') {
        Write-Host 'Uninstall cancelled. No files were removed.' -ForegroundColor Yellow
        exit 0
    }

    Write-Host 'Requesting SSMS to close...' -ForegroundColor Cyan

    foreach ($process in $ssmsProcesses) {
        try {
            [void]$process.CloseMainWindow()
        }
        catch {
            # Graceful close is best-effort; remaining processes are handled below.
        }
    }

    $deadline = (Get-Date).AddSeconds(10)
    do {
        Start-Sleep -Milliseconds 500
        $remaining = @(Get-Process -Name 'ssms' -ErrorAction SilentlyContinue)
    } while ($remaining.Count -gt 0 -and (Get-Date) -lt $deadline)

    if ($remaining.Count -gt 0) {
        Write-Warning 'SSMS did not exit within 10 seconds. Force-closing the remaining process(es)...'

        foreach ($process in $remaining) {
            try {
                Stop-Process -Id $process.Id -Force -ErrorAction Stop
                Write-Host "Stopped ssms.exe (PID: $($process.Id))" -ForegroundColor Green
            }
            catch {
                Write-Host "Failed to stop ssms.exe (PID: $($process.Id)): $($_.Exception.Message)" -ForegroundColor Red
                exit 1
            }
        }
    }
    else {
        Write-Host 'SSMS closed successfully.' -ForegroundColor Green
    }
}
else {
    Write-Host 'SSMS is not running.' -ForegroundColor Green
}

Write-Host ''
Write-Host 'Searching for Zraxion in SSMS 22 profiles...' -ForegroundColor Cyan

if (-not (Test-Path -LiteralPath $ssmsRoot -PathType Container)) {
    Write-Host "SSMS user-data path was not found: $ssmsRoot" -ForegroundColor Yellow
    exit 0
}

$profileDirs = @(
    Get-ChildItem -LiteralPath $ssmsRoot -Directory -ErrorAction Stop |
        Where-Object { $_.Name -like '22.0_*' }
)

if ($profileDirs.Count -eq 0) {
    Write-Host "No SSMS 22 profile directories were found under: $ssmsRoot" -ForegroundColor Yellow
    exit 0
}

# Locate Zraxion by its assembly instead of relying on a machine-specific
# profile name or VSIX-generated extension directory name.
$matchingDirs = @(
    foreach ($profile in $profileDirs) {
        $extensionsPath = Join-Path $profile.FullName 'Extensions'

        if (-not (Test-Path -LiteralPath $extensionsPath -PathType Container)) {
            continue
        }

        Get-ChildItem -LiteralPath $extensionsPath -File -Filter $dllName -Recurse -ErrorAction SilentlyContinue |
            ForEach-Object { $_.Directory.FullName }
    }
) | Sort-Object -Unique

if ($matchingDirs.Count -eq 0) {
    Write-Host "No installed Zraxion extension containing '$dllName' was found." -ForegroundColor Yellow
}
else {
    Write-Host "Found $($matchingDirs.Count) Zraxion extension folder(s):" -ForegroundColor Cyan
    $matchingDirs | ForEach-Object { Write-Host "  $_" }

    $failed = $false

    foreach ($dir in $matchingDirs) {
        Write-Host "Removing: $dir" -ForegroundColor Yellow

        try {
            Remove-Item -LiteralPath $dir -Recurse -Force -ErrorAction Stop
            Write-Host 'Removed successfully.' -ForegroundColor Green
        }
        catch {
            $failed = $true
            Write-Host "Failed to remove '$dir': $($_.Exception.Message)" -ForegroundColor Red
        }
    }

    if ($failed) {
        Write-Host 'One or more Zraxion folders could not be removed.' -ForegroundColor Red
        exit 1
    }
}

# User settings are separate from the VSIX installation. Keep them by default.
if (Test-Path -LiteralPath $settingsPath -PathType Container) {
    Write-Host ''
    $removeSettings = (Read-Host "Also remove Zraxion local settings from '$settingsPath'? [y/N]").Trim()

    if ($removeSettings -match '^(?i:y|yes)$') {
        try {
            Remove-Item -LiteralPath $settingsPath -Recurse -Force -ErrorAction Stop
            Write-Host 'Local Zraxion settings removed.' -ForegroundColor Green
        }
        catch {
            Write-Host "Failed to remove local settings: $($_.Exception.Message)" -ForegroundColor Red
            exit 1
        }
    }
    else {
        Write-Host 'Local Zraxion settings were kept.' -ForegroundColor DarkGray
    }
}

Write-Host ''
Write-Host 'Zraxion uninstall cleanup completed.' -ForegroundColor Green
```

After the script completes, start SSMS again and confirm that **Zraxion** no longer appears in the Results Grid context menu or under **Tools → Zraxion**.

> [!NOTE]
> Zraxion settings are stored separately under `%LOCALAPPDATA%\Zraxion`. The script keeps these settings by default and asks before deleting them, which makes reinstalling or upgrading less destructive.

---

## Settings

Zraxion stores local preferences under:

```text
%LocalAppData%\Zraxion
```

Current settings include:

- Analytics Bar metric visibility
- Auto Refresh preference
- Default comparison tolerance
- Welcome-version state

Comparison baselines and last comparison values are session/tab state and are not persisted as user data.

---

## Update Checks and Privacy

Zraxion contains a lightweight release checker.

On startup it can query the public GitHub Releases API to determine whether a newer Zraxion release exists. If an update is available, SSMS displays a non-blocking InfoBar.

Zraxion does **not** require:

- an account
- an API key
- database credentials beyond those already used by SSMS
- wallet access
- telemetry submission

The update check is for release-version discovery only.

---

## Support the Project

Zraxion is free and open source. If it saves time in your SQL or reconciliation workflow, optional donations can support continued development, testing, documentation, and compatibility work.

Support does not unlock hidden features.

### Bitcoin

**Network:** Bitcoin

```text
bc1q5tl36qpk27hf7upl8l753xa0gcm57adrvmwgkz
```

### Tether

**Network:** TRON / TRC20

```text
TA5pibChqS7CeHiDvfP7V3uQVMynk6SxLq
```

### EVM Networks

**Supported networks:** Ethereum, BNB Smart Chain, Polygon

```text
0x6634E26BA0e323182B7A9a89278E9a7BbCa9aF70
```

### Solana

**Network:** Solana

```text
91kuBFEZAnRk8ixsuzAQnAVvtzLAsQpPFRsfe285ZKUC
```

> [!CAUTION]
> Always verify the **complete wallet address** and **blockchain network** before transferring cryptocurrency. Cryptocurrency transactions are generally irreversible. Zraxion only displays/copies these addresses; it does not connect to wallets, create transactions, request signatures, or request private keys/seed phrases.

---

## Building from Source

Clone the repository:

```powershell
git clone https://github.com/LaneZero/Zraxion.git
Set-Location Zraxion
```

Build:

```powershell
dotnet restore .\Zraxion.slnx
dotnet build .\Zraxion.slnx -c Debug
```

Release build:

```powershell
dotnet clean .\Zraxion.slnx -c Release
dotnet build .\Zraxion.slnx -c Release
```

Expected VSIX:

```text
src\Zraxion.Extension\bin\Release\net472\Zraxion.Extension.vsix
```

A clean release build should complete with:

```text
0 Warning(s)
0 Error(s)
```

---

## Project Structure

```text
Zraxion/
├─ .github/                 GitHub configuration
├─ build/                   Build and release helpers
├─ docs/                    Public documentation and assets
├─ src/
│  └─ Zraxion.Extension/    SSMS 22 extension
├─ tests/                   Tests and validation assets
├─ CHANGELOG.md
├─ CONTRIBUTING.md
├─ LICENSE
├─ NOTICE
└─ README.md
```

---

## Known v1.0.0 Boundaries

Zraxion v1.0.0 intentionally keeps the scope focused.

- SSMS 22 only
- Windows x64 only
- Results Grid analytics, not SQL editor result transformation
- Percentage comparison with zero baseline is unavailable for non-zero B
- User-entered tolerance values currently follow the supported tolerance input range even though grid-value arithmetic itself supports SQL-style 38-digit precision
- Zraxion does not alter source query results or database data
- Zraxion does not automatically upload analytics or comparison data

These boundaries are intentionally documented so release behavior and scope remain clear.

---

## Contributing

Contributions are welcome.

Please read [CONTRIBUTING.md](CONTRIBUTING.md) before opening a pull request.

Useful contribution areas include:

- SSMS 22 compatibility validation
- Results Grid integration
- financial/reconciliation UX
- accessibility
- theme compatibility
- precision edge cases
- automated regression tests
- documentation

---

## Acknowledgements

Zraxion is an independent implementation.

The project acknowledges **[Axial SQL Tools](https://github.com/Axial-SQL/AxialSqlTools)** for publicly demonstrating practical SSMS productivity extensions and for providing useful precedent around SSMS Results Grid/context-menu integration.

Zraxion's codebase, numeric engine, analytics workflow, comparison model, user interface, and implementation are maintained independently.

SQL Server Management Studio, SQL Server, Visual Studio, and related names are trademarks of Microsoft Corporation. Zraxion is not affiliated with or endorsed by Microsoft.

---

## License

Zraxion is licensed under the **Apache License 2.0**.

See:

- [LICENSE](LICENSE)
- [NOTICE](NOTICE)

Apache 2.0 permits use, modification, distribution, and commercial use subject to its conditions. It requires preservation of license/copyright notices and prominent notices for modified files, includes an express patent grant, and does not grant rights to use Zraxion trademarks except as permitted by the license.

The **Zraxion name, logo, and project branding are not licensed as trademarks** by the Apache License 2.0.

---

<div align="center">

**Zraxion · SSMS 22 Results Grid analytics with exact financial precision**

[GitHub](https://github.com/LaneZero/Zraxion) · [Issues](https://github.com/LaneZero/Zraxion/issues) · [Releases](https://github.com/LaneZero/Zraxion/releases)

</div>
