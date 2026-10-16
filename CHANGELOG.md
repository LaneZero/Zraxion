# Changelog

All notable changes to Zraxion are documented in this file.

The project follows semantic versioning for public releases.

## [Unreleased]

### Planned

- Post-v1.0 maintenance and SSMS 22 compatibility improvements.

## [1.0.0] - 2026-10-17

### Added

- SSMS 22 Results Grid context-menu integration.
- Selection analytics: SUM, AVG, COUNT, MIN, MAX.
- Whole-column analytics.
- Persistent, theme-aware Analytics Bar.
- Automatic and manual Analytics Bar refresh modes.
- Baseline A / B comparison workflow.
- Exact, absolute-tolerance, percentage-tolerance, and combined-tolerance comparison modes.
- Per-tab comparison and baseline isolation.
- Comparison reason/status display.
- Exact arbitrary-precision decimal engine for financial Results Grid values.
- Exact SQL-style `DECIMAL/NUMERIC` aggregation beyond CLR `decimal` range.
- High-precision percentage calculation and display.
- Conservative culture-aware numeric parsing.
- Exact/invariant copy actions.
- Persistent analytics preferences.
- Persistent default tolerance preferences.
- Welcome/About tab integrated into SSMS.
- One-time welcome display after install or version upgrade.
- GitHub release update checker with non-blocking SSMS InfoBar notification.
- Project/support links.
- Optional cryptocurrency support section with local copy actions.
- Zraxion branding and release UI polish.

### Changed

- Refined context-menu structure for Results Grid workflows.
- Replaced legacy metric MessageBoxes with integrated Analytics Bar behavior.
- Improved theme handling and dark-theme readability.
- Improved tooltip behavior and automatic hiding.
- Hardened settings file persistence and corruption recovery.
- Migrated analytics and comparison arithmetic away from CLR `decimal` to exact decimal handling.

### Fixed

- Per-tab state leakage between Results tabs.
- Analytics Bar lifecycle issues during result reruns and tab switching.
- Copy Summary first-click reliability.
- Tiny percentage differences incorrectly appearing as zero.
- Ambiguous culture parsing being accepted incorrectly.
- Legacy numeric overflow paths and obsolete decimal-range messaging.
- View tooltip overlap/flicker behavior.

### Compatibility

- SQL Server Management Studio 22
- Windows x64
- .NET Framework 4.7.2+

[Unreleased]: https://github.com/LaneZero/Zraxion/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/LaneZero/Zraxion/releases/tag/v1.0.0
