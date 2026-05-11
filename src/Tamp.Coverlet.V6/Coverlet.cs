namespace Tamp.Coverlet.V6;

/// <summary>
/// Entry point for the Coverlet config builder. Hold the returned
/// <see cref="CoverletSettings"/> as a build-script field and reference
/// it in every test target's data-collector setup.
///
/// <example>
/// <code>
/// readonly CoverletSettings Coverage = Coverlet.Configure(s =&gt; s
///     .AddFormat(CoverletFormat.OpenCover)
///     .AddFormat(CoverletFormat.Cobertura)
///     .AddInclude("[Strata.Api]*")
///     .AddInclude("[Strata.Functions]*")
///     .AddExclude("[*.Tests]*")
///     .SetUseSourceLink(false));        // false for Sonar (TAM-80)
///
/// Target Test =&gt; _ =&gt; _.Executes(() =&gt;
///     DotNet.Test(s =&gt; s
///         .SetProject(Solution.Path)
///         .AddDataCollector(Coverage.ToCollectArgument())   // inline shape
///         .SetResultsDirectory(Artifacts / "test-results")));
///
/// // Or with a runsettings file:
/// var rs = Artifacts / "coverlet.runsettings";
/// File.WriteAllText(rs, Coverage.ToRunSettingsXml());
/// </code>
/// </example>
/// </summary>
public static class Coverlet
{
    /// <summary>Build a <see cref="CoverletSettings"/> via the fluent configurer.</summary>
    public static CoverletSettings Configure(Action<CoverletSettings> configure)
    {
        if (configure is null) throw new ArgumentNullException(nameof(configure));
        var s = new CoverletSettings();
        configure(s);
        return s;
    }
}
