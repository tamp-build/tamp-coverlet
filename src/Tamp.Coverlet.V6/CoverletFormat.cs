namespace Tamp.Coverlet.V6;

/// <summary>
/// Output formats supported by Coverlet 6.x. Wire-value names match
/// what Coverlet expects in <c>Format=</c> options (lowercase).
/// </summary>
public enum CoverletFormat
{
    /// <summary>Coverlet's native JSON format.</summary>
    Json,
    /// <summary>Cobertura — widely supported by CI dashboards (ADO, GitHub Actions, GitLab).</summary>
    Cobertura,
    /// <summary>OpenCover — the format SonarQube reads. Strata's primary format.</summary>
    OpenCover,
    /// <summary>lcov — primary format for nyc / Istanbul ecosystem tools.</summary>
    Lcov,
    /// <summary>TeamCity service-message format.</summary>
    TeamCity,
}

internal static class CoverletFormatExtensions
{
    public static string ToWireValue(this CoverletFormat f) => f switch
    {
        CoverletFormat.Json => "json",
        CoverletFormat.Cobertura => "cobertura",
        CoverletFormat.OpenCover => "opencover",
        CoverletFormat.Lcov => "lcov",
        CoverletFormat.TeamCity => "teamcity",
        _ => throw new ArgumentOutOfRangeException(nameof(f), f, "Unknown CoverletFormat."),
    };
}
