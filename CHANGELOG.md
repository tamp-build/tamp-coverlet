# Changelog

All notable changes to **Tamp.Coverlet.V6** are recorded here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/);
versions follow [SemVer](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.3.0] - 2026-11-20 - .NET 11 in, .NET 8 and .NET 9 out

### Changed

- **Multi-targets `net10.0;net11.0`** instead of `net8.0;net9.0;net10.0` ([ADR 0015](https://github.com/tamp-build/tamp/blob/main/docs/adr/0015-target-framework-strategy.md)). .NET 11 shipped 2026-11-10 and .NET 8 and .NET 9 reached end of support the same day; the rule is to add a TFM the day a release ships and drop one the day Microsoft ends its support. `global.json` pins SDK 11.0.100.

  **If you target `net8.0` or `net9.0`, stay on the previous version** — this one carries no assets your project can resolve.

- **`Tamp.*` pinned to 1.21.0**, which carries a SARIF fix worth knowing about if this project's output is read as evidence: before 1.21.0, a SARIF result whose scanner omitted `level` was read as `none` — the least severe value — instead of the declared `warning`, on every runtime before net11 ([tamp#166](https://github.com/tamp-build/tamp/issues/166)).

- **Publishing moved to nuget.org trusted publishing (OIDC).** The `tamp_nuget_api_key` org secret expired 2026-11-01 along with every other pre-2026-08-17 nuget.org key. The publish job now mints a short-lived key from its own OIDC token, so there is no long-lived credential to rotate or leak.

## [0.2.1] — 2026-09-27

### Added

- Package now ships XML documentation files (`.xml`) alongside the assembly, so consumers get IntelliSense and API docs. (Mirrors [tamp-build/tamp#3](https://github.com/tamp-build/tamp/pull/50).)


## [0.2.0] — pending — `DotNetTestSettings.WithCoverlet` cross-package extension (TAM-209)

### Added

- **`DotNetTestSettings.WithCoverlet(Action<CoverletSettings>)`** — extension
  method that collapses the previous four-step adoption dance
  (Configure → ToRunSettingsXml → write-to-temp → SetSettings) into a single
  fluent call:

  ```csharp
  DotNet.Test(s => s
      .SetProject(Solution.Path)
      .WithCoverlet(c => c
          .AddFormat(CoverletFormat.OpenCover)
          .AddInclude("[MyApp]*")
          .AddExclude("[*.Tests]*"))
      .SetResultsDirectory(Artifacts / "test-results"));
  ```

- **`DotNetTestSettings.WithCoverlet(CoverletSettings)`** — overload for
  sharing one Coverlet config across multiple `dotnet test` calls (fast vs.
  full suite, unit vs. integration).

### Why

HoldFast canary friction batch #19 (2026-05-13). The previous adoption shape
required adopters to write the runsettings XML to a temp file by hand and
remember to pass the path via `SetSettings`. Took reflection for HoldFast
to discover the connection between `CoverletSettings.ToRunSettingsXml()`
and `DotNetTestSettings.SetSettings(...)`.

### Cross-package dependency

`Tamp.Coverlet.V6` now depends on `Tamp.NetCli.V10 >= 1.5.0`. Same pattern
as `Tamp.Tauri.V2 → Tamp.Cargo` for `AsTauriShell` — extension methods that
bridge two satellite types live in the satellite whose configuration shape
they represent.

### Tests

11 new tests in `DotNetTestSettingsCoverletExtensionsTests`: settings file
written + has correct extension, XML parses cleanly, format / include /
exclude patterns are present, return-same-instance for chaining, pre-built
settings overload, distinct files per call, null guards on both overloads,
composition with other DotNetTest fluent setters. Total Tamp.Coverlet.V6.Tests
count: 35 (was 24).

## [0.1.0] - 2026-04-15

Initial release. `Coverlet.Configure(...)` + `CoverletSettings` config
builder for `dotnet test` coverage collection. Emits inline `--collect`
argument string OR a `runsettings.xml` file body.
