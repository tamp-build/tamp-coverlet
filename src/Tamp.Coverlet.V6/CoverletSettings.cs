using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace Tamp.Coverlet.V6;

/// <summary>
/// Configuration for a Coverlet collector run (data-collector flavor:
/// <c>dotnet test --collect "XPlat Code Coverage" ...</c>). Use the
/// fluent setters via <see cref="Coverlet.Configure"/>, then emit:
///
/// <list type="bullet">
///   <item><see cref="ToCollectArgument"/> for an inline <c>--collect</c> string.</item>
///   <item><see cref="ToRunSettingsXml"/> for a runsettings.xml file body.</item>
/// </list>
///
/// <para>Choose runsettings.xml for non-trivial config — the inline
/// <c>--collect</c> string has shell-quoting quirks (semicolons are
/// the option separator, but bash treats <c>;</c> as a statement
/// terminator unless quoted).</para>
/// </summary>
public sealed class CoverletSettings
{
    /// <summary>Output formats. Repeated entries produce a comma-joined <c>Format=cobertura,opencover</c> option.</summary>
    public List<CoverletFormat> Formats { get; } = [];

    /// <summary>Assembly filters to INCLUDE. Each entry is a Coverlet glob like <c>[Strata.Functions]*</c> — assembly name in brackets, then a class pattern.</summary>
    public List<string> Include { get; } = [];

    /// <summary>Assembly filters to EXCLUDE. Same shape as <see cref="Include"/>. Common entries: <c>[*.Tests]*</c>, <c>[xunit.*]*</c>.</summary>
    public List<string> Exclude { get; } = [];

    /// <summary>File-path-based exclusions (regex on full path). Maps to <c>ExcludeByFile</c>.</summary>
    public List<string> ExcludeByFile { get; } = [];

    /// <summary>Attribute-name-based exclusions (e.g. <c>[Generated]</c>). Maps to <c>ExcludeByAttribute</c>.</summary>
    public List<string> ExcludeByAttribute { get; } = [];

    /// <summary>Single hits vs. cumulative hits. Maps to <c>SingleHit</c>.</summary>
    public bool SingleHit { get; set; }

    /// <summary>Skip Coverlet's auto-merge between test runs. Maps to <c>MergeWith</c>'s null path.</summary>
    public string? MergeWith { get; set; }

    /// <summary>Include test-assembly source. Maps to <c>IncludeTestAssembly</c>.</summary>
    public bool IncludeTestAssembly { get; set; }

    /// <summary>
    /// Emit source paths via SourceLink URLs instead of local paths.
    /// Default: <c>false</c>.
    ///
    /// <para><strong>Set to <c>false</c> for Sonar integration.</strong>
    /// Sonar correlates coverage data to source files via the path in
    /// the OpenCover XML. SourceLink emits URLs like
    /// <c>https://raw.githubusercontent.com/.../Foo.cs</c>, which Sonar
    /// can't match against your project's source tree — coverage shows
    /// as 0% on every file. Lesson learned in TAM-80.</para>
    /// </summary>
    public bool UseSourceLink { get; set; }

    /// <summary>Skip autoprobe of deps. Maps to <c>SkipAutoProps</c>.</summary>
    public bool SkipAutoProps { get; set; }

    /// <summary>Deterministic report output. Maps to <c>DeterministicReport</c>.</summary>
    public bool DeterministicReport { get; set; }

    /// <summary>
    /// Build the inline <c>--collect</c> argument string:
    /// <c>"XPlat Code Coverage;Format=cobertura,opencover;Include=[Strata]*"</c>.
    ///
    /// <para>Pass this to <c>DotNet.Test(s =&gt; s.AddDataCollector(coverlet.ToCollectArgument()))</c>.</para>
    /// </summary>
    public string ToCollectArgument()
    {
        var sb = new StringBuilder("XPlat Code Coverage");
        foreach (var (key, value) in EnumerateInlineOptions())
        {
            sb.Append(';');
            sb.Append(key);
            sb.Append('=');
            sb.Append(value);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Build a runsettings.xml file body containing the
    /// <c>XPlat Code Coverage</c> data collector with these settings.
    /// Pass the resulting file path to <c>DotNet.Test(s =&gt; s.SetSettings(path))</c>.
    /// </summary>
    public string ToRunSettingsXml()
    {
        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("RunSettings",
                new XElement("DataCollectionRunSettings",
                    new XElement("DataCollectors",
                        new XElement("DataCollector",
                            new XAttribute("friendlyName", "XPlat Code Coverage"),
                            new XElement("Configuration",
                                BuildConfigurationElements()))))));

        // XDocument.ToString() omits the XML declaration. Use XmlWriter
        // explicitly so the file starts with `<?xml ... ?>` — VSTest is
        // tolerant either way, but the declaration helps downstream
        // tools (XML editors, Sonar XML parsers) handle the file
        // confidently.
        var sw = new System.IO.StringWriter();
        var settings = new System.Xml.XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8, OmitXmlDeclaration = false };
        using (var xw = System.Xml.XmlWriter.Create(sw, settings))
            doc.Save(xw);
        return sw.ToString();
    }

    private IEnumerable<XElement> BuildConfigurationElements()
    {
        if (Formats.Count > 0)
            yield return new XElement("Format", string.Join(',', Formats.Select(f => f.ToWireValue())));
        if (Include.Count > 0)
            yield return new XElement("Include", string.Join(',', Include));
        if (Exclude.Count > 0)
            yield return new XElement("Exclude", string.Join(',', Exclude));
        if (ExcludeByFile.Count > 0)
            yield return new XElement("ExcludeByFile", string.Join(',', ExcludeByFile));
        if (ExcludeByAttribute.Count > 0)
            yield return new XElement("ExcludeByAttribute", string.Join(',', ExcludeByAttribute));
        if (SingleHit) yield return new XElement("SingleHit", "true");
        if (!string.IsNullOrEmpty(MergeWith)) yield return new XElement("MergeWith", MergeWith!);
        if (IncludeTestAssembly) yield return new XElement("IncludeTestAssembly", "true");
        // Always emit UseSourceLink explicitly so consumers can see it
        // in the runsettings — easier to grep when debugging Sonar
        // correlation issues.
        yield return new XElement("UseSourceLink", UseSourceLink ? "true" : "false");
        if (SkipAutoProps) yield return new XElement("SkipAutoProps", "true");
        if (DeterministicReport) yield return new XElement("DeterministicReport", "true");
    }

    private IEnumerable<(string Key, string Value)> EnumerateInlineOptions()
    {
        if (Formats.Count > 0)
            yield return ("Format", string.Join(',', Formats.Select(f => f.ToWireValue())));
        if (Include.Count > 0)
            yield return ("Include", string.Join(',', Include));
        if (Exclude.Count > 0)
            yield return ("Exclude", string.Join(',', Exclude));
        if (ExcludeByFile.Count > 0)
            yield return ("ExcludeByFile", string.Join(',', ExcludeByFile));
        if (ExcludeByAttribute.Count > 0)
            yield return ("ExcludeByAttribute", string.Join(',', ExcludeByAttribute));
        if (SingleHit) yield return ("SingleHit", "true");
        if (!string.IsNullOrEmpty(MergeWith)) yield return ("MergeWith", MergeWith!);
        if (IncludeTestAssembly) yield return ("IncludeTestAssembly", "true");
        yield return ("UseSourceLink", UseSourceLink ? "true" : "false");
        if (SkipAutoProps) yield return ("SkipAutoProps", "true");
        if (DeterministicReport) yield return ("DeterministicReport", "true");
    }

    // ---- fluent setters ----

    public CoverletSettings AddFormat(CoverletFormat format) { Formats.Add(format); return this; }
    public CoverletSettings AddInclude(string pattern) { Include.Add(ValidatePattern(pattern, nameof(pattern))); return this; }
    public CoverletSettings AddExclude(string pattern) { Exclude.Add(ValidatePattern(pattern, nameof(pattern))); return this; }
    public CoverletSettings AddExcludeByFile(string regex) { ExcludeByFile.Add(regex); return this; }
    public CoverletSettings AddExcludeByAttribute(string attributeName) { ExcludeByAttribute.Add(attributeName); return this; }
    public CoverletSettings SetSingleHit(bool v = true) { SingleHit = v; return this; }
    public CoverletSettings SetMergeWith(string path) { MergeWith = path; return this; }
    public CoverletSettings SetIncludeTestAssembly(bool v = true) { IncludeTestAssembly = v; return this; }
    public CoverletSettings SetUseSourceLink(bool v) { UseSourceLink = v; return this; }
    public CoverletSettings SetSkipAutoProps(bool v = true) { SkipAutoProps = v; return this; }
    public CoverletSettings SetDeterministicReport(bool v = true) { DeterministicReport = v; return this; }

    private static string ValidatePattern(string pattern, string paramName)
    {
        if (string.IsNullOrEmpty(pattern))
            throw new ArgumentException("Coverlet pattern is required.", paramName);
        // Coverlet's filter shape is [Assembly]TypeOrNamespace.
        // The leading '[' is mandatory; warn-via-throw if missing —
        // this is the #1 footgun (consumers paste raw type names).
        if (!pattern.StartsWith('['))
            throw new ArgumentException(
                $"Coverlet filter pattern must start with '[' (assembly bracket). Got: \"{pattern}\". " +
                $"Example: \"[Strata.Functions]*\" matches everything in the Strata.Functions assembly. " +
                $"Use \"[*]\" to match all assemblies.",
                paramName);
        return pattern;
    }
}
