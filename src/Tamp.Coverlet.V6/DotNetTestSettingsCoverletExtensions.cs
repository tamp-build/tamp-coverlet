using Tamp.NetCli.V10;

namespace Tamp.Coverlet.V6;

/// <summary>
/// Cross-package extensions on <see cref="DotNetTestSettings"/> for inline
/// Coverlet integration. Lives in <c>Tamp.Coverlet.V6</c> rather than
/// <c>Tamp.NetCli.V10</c> because the configuration surface is Coverlet's
/// concern — the bridge is provided here for adopter ergonomics.
/// </summary>
/// <remarks>
/// HoldFast canary friction #19 (2026-05-13). Adopters routinely wrote the
/// four-step dance manually:
/// <list type="number">
///   <item>Build settings via <see cref="Coverlet.Configure"/>.</item>
///   <item>Call <see cref="CoverletSettings.ToRunSettingsXml"/>.</item>
///   <item>Write the XML to a temp file.</item>
///   <item>Pass the file path to <see cref="DotNetTestSettings.SetSettings"/>.</item>
/// </list>
/// Took reflection to discover. <see cref="WithCoverlet"/> collapses it to
/// one fluent call.
/// </remarks>
public static class DotNetTestSettingsCoverletExtensions
{
    /// <summary>
    /// Configure Coverlet for this <c>dotnet test</c> invocation: writes the
    /// generated runsettings XML to a temp file under the OS scratch root and
    /// wires it via <see cref="DotNetTestSettings.SetSettings"/>. Returns the
    /// settings instance for chaining.
    /// </summary>
    /// <param name="settings">The <c>dotnet test</c> settings to extend.</param>
    /// <param name="configure">Coverlet-side configuration callback.</param>
    /// <example>
    /// <code>
    /// DotNet.Test(s => s
    ///     .SetProject(Solution.Path)
    ///     .WithCoverlet(c => c
    ///         .AddFormat(CoverletFormat.OpenCover)
    ///         .AddInclude("[MyApp]*")
    ///         .AddExclude("[*.Tests]*"))
    ///     .SetResultsDirectory(Artifacts / "test-results"));
    /// </code>
    /// </example>
    /// <remarks>
    /// The temp file lives under <see cref="AbsolutePath.CreateTempFile"/>;
    /// the caller does not need to clean it up (OS temp-root maintenance
    /// applies). If you want lifecycle-managed cleanup, capture the runsettings
    /// path manually via <see cref="Coverlet.Configure"/> +
    /// <see cref="CoverletSettings.ToRunSettingsXml"/> and route through
    /// <c>TampBuild.Scratch(...)</c> in your build script.
    /// </remarks>
    public static DotNetTestSettings WithCoverlet(
        this DotNetTestSettings settings,
        Action<CoverletSettings> configure)
    {
        if (settings is null) throw new ArgumentNullException(nameof(settings));
        if (configure is null) throw new ArgumentNullException(nameof(configure));

        var coverlet = Coverlet.Configure(configure);
        var runsettingsPath = AbsolutePath.CreateTempFile(".runsettings");
        runsettingsPath.WriteAllText(coverlet.ToRunSettingsXml());
        settings.SetSettings(runsettingsPath.Value);
        return settings;
    }

    /// <summary>
    /// Overload accepting a pre-built <see cref="CoverletSettings"/>. Use this
    /// when you want to share one Coverlet config across multiple
    /// <c>dotnet test</c> calls (separate test targets for fast vs. full,
    /// integration vs. unit, etc.).
    /// </summary>
    public static DotNetTestSettings WithCoverlet(
        this DotNetTestSettings settings,
        CoverletSettings coverletSettings)
    {
        if (settings is null) throw new ArgumentNullException(nameof(settings));
        if (coverletSettings is null) throw new ArgumentNullException(nameof(coverletSettings));

        var runsettingsPath = AbsolutePath.CreateTempFile(".runsettings");
        runsettingsPath.WriteAllText(coverletSettings.ToRunSettingsXml());
        settings.SetSettings(runsettingsPath.Value);
        return settings;
    }
}
