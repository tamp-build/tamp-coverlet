# Changelog

All notable changes to **Tamp.Coverlet.V6** are recorded here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/);
versions follow [SemVer](https://semver.org/spec/v2.0.0.html).

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
