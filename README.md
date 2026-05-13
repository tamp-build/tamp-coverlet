# Tamp.Coverlet

Config-builder wrapper for **Coverlet 6.x** — the `dotnet test
--collect "XPlat Code Coverage"` data-collector flavor. Type-safe
formats, Include/Exclude filters, SourceLink toggle, and a runsettings.xml
generator.

```csharp
using Tamp.Coverlet.V6;
```

| Package | Coverlet | Status |
|---|---|---|
| `Tamp.Coverlet.V6` | 6.x | preview |

Requires `Tamp.Core ≥ 1.0.5`.

## Why a config builder, not a CLI wrapper

Coverlet doesn't have a CLI you invoke directly (well, `coverlet.console`
exists, but most projects use `coverlet.collector` via `dotnet test`).
Coverage runs as a **data collector** with config passed via either:

1. An inline `--collect "XPlat Code Coverage;Format=opencover;..."` string, OR
2. A `.runsettings` XML file referenced by `--settings path/to/file.xml`.

Both shapes are fiddly. This wrapper is a type-safe builder that emits
both. You hold one `CoverletSettings` object as a build-script field
and feed it into every test target's data collector.

## Minimal adoption snippet (0.2.0+) — `WithCoverlet` one-liner

```csharp
using Tamp.NetCli.V10;
using Tamp.Coverlet.V6;

Target Test => _ => _.Executes(() =>
    DotNet.Test(s => s
        .SetProject(Solution.Path)
        .WithCoverlet(c => c
            .AddFormat(CoverletFormat.OpenCover)
            .AddInclude("[MyApp]*")
            .AddExclude("[*.Tests]*"))
        .SetResultsDirectory(Artifacts / "test-results")));
```

`WithCoverlet` is the cross-package extension method that handles the runsettings-XML generation + temp-file write + `SetSettings(...)` wiring in one fluent call. **This is the recommended shape for 0.2.0+ adopters.** The lower-level `Coverlet.Configure` + `ToCollectArgument` / `ToRunSettingsXml` surface stays available for adopters who need to share one config across multiple test targets or manage the runsettings file's lifecycle themselves.

## Quick example — Strata-like setup (lower-level)

```csharp
using Tamp;
using Tamp.NetCli.V10;
using Tamp.Coverlet.V6;

readonly CoverletSettings Coverage = Coverlet.Configure(s => s
    .AddFormat(CoverletFormat.OpenCover)              // Sonar reads this
    .AddFormat(CoverletFormat.Cobertura)              // ADO/GH coverage panel reads this
    .AddInclude("[Strata.Api]*")                       // scope to Strata code
    .AddInclude("[Strata.Functions]*")
    .AddExclude("[*.Tests]*")                          // skip test assemblies themselves
    .AddExcludeByAttribute("GeneratedCode")
    .SetUseSourceLink(false));                         // false for Sonar (TAM-80 lesson)

Target Test => _ => _.Executes(() =>
    DotNet.Test(s => s
        .SetProject(Solution.Path)
        .SetConfiguration(Configuration)
        .AddDataCollector(Coverage.ToCollectArgument())
        .SetResultsDirectory(Artifacts / "test-results")));

// Or via a runsettings.xml file (preferred for non-trivial config):
Target WriteRunSettings => _ => _.Executes(() =>
{
    var rs = Artifacts / "coverlet.runsettings";
    File.WriteAllText(rs, Coverage.ToRunSettingsXml());
});

Target TestWithRunSettings => _ => _
    .DependsOn(nameof(WriteRunSettings))
    .Executes(() =>
        DotNet.Test(s => s
            .SetProject(Solution.Path)
            .SetSettings(Artifacts / "coverlet.runsettings")
            .AddDataCollector("XPlat Code Coverage")));
```

## What it's good at

### Format type safety

```csharp
.AddFormat(CoverletFormat.Cobertura)         // "cobertura"
.AddFormat(CoverletFormat.OpenCover)         // "opencover"
.AddFormat(CoverletFormat.Lcov)              // "lcov"
.AddFormat(CoverletFormat.Json)              // "json" (Coverlet native)
.AddFormat(CoverletFormat.TeamCity)          // "teamcity"
```

The wrapper joins multiple formats with comma in the wire output.

### Filter validation

`AddInclude` / `AddExclude` require the `[Assembly]` bracket prefix —
the #1 footgun is pasting raw type names ("Strata.Functions") instead
of Coverlet's filter shape ("[Strata.Functions]\*"). The wrapper
throws with a helpful example.

```csharp
.AddInclude("[Strata.Functions]*")        // ✅
.AddInclude("[*]Strata.Functions.*")      // ✅ — match all assemblies, namespace filter
.AddInclude("Strata.Functions")           // ❌ throws ArgumentException
```

### `UseSourceLink=false` for Sonar (TAM-80)

The default is `false`, and the wrapper emits it explicitly in the
runsettings even when not opted in — making it grep-friendly when
debugging "why is my Sonar coverage 0%?". The answer is almost always
that `UseSourceLink=true` substituted raw.githubusercontent.com URLs
for local file paths in the OpenCover XML, and Sonar couldn't
correlate them to source.

## Cross-project source bleed via ProjectReference

Strata's pain (pain-point #6 from the Tamp.AdoRest seed message):
`Strata.Functions` has a `ProjectReference` to `Strata.Api` (for
shared types). The dotnet sonar-scanner walks all referenced project
sources, so `Strata.Api/**/*.cs` ends up indexed under the
strata-functions Sonar analysis. Coverage results for those files get
attributed to whichever scan reports a hit first.

The wrapper's `Include` filter is **one of three layers** needed to
fix this:

1. **Coverlet `Include=[Strata.Functions]*`** — only count hits in the
   target assembly. This wrapper does that.
2. **Per-project `sonar.cs.opencover.reportsPaths`** — point each
   Sonar scan at its own coverage file (not `**` matched across
   sibling test-results dirs, which over-matches).
3. **`<SonarQubeExclude>true</SonarQubeExclude>`** conditional MSBuild
   property — skip the strata-api project entirely from the
   strata-functions scan.

This wrapper covers layer 1. Layers 2 and 3 are project / Sonar
config concerns. Both are documented in
[`Tamp.SonarScanner.V10`](https://github.com/tamp-build/tamp-sonar)'s
README.

## Inline `--collect` vs runsettings.xml

| When to use | Inline `--collect` | runsettings.xml |
|---|---|---|
| Trivial config (Format only) | ✅ | overkill |
| Multiple formats + filters | ⚠️ shell-quoting quirks (semicolons) | ✅ |
| Want to commit the config | inline is fine in `Build.cs` | ✅ commit alongside csproj |
| Sonar coverage reports | either works | ✅ canonical |

Use inline for one-liners. Use runsettings.xml for anything you'd
want to review or commit.

## What's NOT in v0.1.0

- `coverlet.msbuild` flavor (`/p:CollectCoverage=true /p:CoverletOutputFormat=...`) — different surface, MSBuild integration. Add if there's demand.
- `coverlet.console` standalone CLI — different invocation shape; would be a separate satellite.
- Threshold enforcement (`/p:Threshold=80 /p:ThresholdType=line` MSBuild knobs).

## Releasing

See [MAINTAINERS.md](MAINTAINERS.md).
