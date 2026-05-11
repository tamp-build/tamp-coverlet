using System.Xml.Linq;
using Bogus;
using Xunit;

namespace Tamp.Coverlet.V6.Tests;

public sealed class CoverletTests
{
    // ---- entry point ----

    [Fact]
    public void Configure_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Coverlet.Configure(null!));
    }

    [Fact]
    public void Configure_Default_Has_Empty_Lists()
    {
        var s = Coverlet.Configure(_ => { });
        Assert.Empty(s.Formats);
        Assert.Empty(s.Include);
        Assert.Empty(s.Exclude);
        Assert.False(s.SingleHit);
        Assert.False(s.UseSourceLink);
    }

    // ---- inline --collect string ----

    [Fact]
    public void ToCollectArgument_Default_Is_Just_The_Friendly_Name_Plus_UseSourceLink()
    {
        var s = Coverlet.Configure(_ => { });
        // UseSourceLink is always emitted (default false) — explicit so
        // the runsettings is grep-able.
        Assert.Equal("XPlat Code Coverage;UseSourceLink=false", s.ToCollectArgument());
    }

    [Fact]
    public void ToCollectArgument_Format_Joined_With_Comma()
    {
        var s = Coverlet.Configure(c => c
            .AddFormat(CoverletFormat.Cobertura)
            .AddFormat(CoverletFormat.OpenCover));
        Assert.Contains("Format=cobertura,opencover", s.ToCollectArgument());
    }

    [Fact]
    public void ToCollectArgument_Include_And_Exclude_Joined_With_Comma()
    {
        var s = Coverlet.Configure(c => c
            .AddInclude("[Strata.Api]*")
            .AddInclude("[Strata.Functions]*")
            .AddExclude("[*.Tests]*"));
        var arg = s.ToCollectArgument();
        Assert.Contains("Include=[Strata.Api]*,[Strata.Functions]*", arg);
        Assert.Contains("Exclude=[*.Tests]*", arg);
    }

    [Fact]
    public void ToCollectArgument_Strata_Like_Shape_Round_Trips()
    {
        // Mirror Strata-Scott's pain-point #6 setup — per-project
        // Include filter to dodge cross-project source bleed via
        // ProjectReference.
        var s = Coverlet.Configure(c => c
            .AddFormat(CoverletFormat.OpenCover)
            .AddFormat(CoverletFormat.Cobertura)
            .AddInclude("[Strata.Functions]*")
            .SetUseSourceLink(false));
        var arg = s.ToCollectArgument();
        Assert.StartsWith("XPlat Code Coverage", arg);
        Assert.Contains("Format=opencover,cobertura", arg);
        Assert.Contains("Include=[Strata.Functions]*", arg);
        Assert.Contains("UseSourceLink=false", arg);
    }

    [Fact]
    public void ToCollectArgument_SingleHit_And_DeterministicReport_Round_Trip()
    {
        var s = Coverlet.Configure(c => c
            .SetSingleHit()
            .SetDeterministicReport());
        var arg = s.ToCollectArgument();
        Assert.Contains("SingleHit=true", arg);
        Assert.Contains("DeterministicReport=true", arg);
    }

    [Fact]
    public void ToCollectArgument_IncludeTestAssembly_When_Set()
    {
        var s = Coverlet.Configure(c => c.SetIncludeTestAssembly());
        Assert.Contains("IncludeTestAssembly=true", s.ToCollectArgument());
    }

    [Fact]
    public void ToCollectArgument_MergeWith_Round_Trips()
    {
        var s = Coverlet.Configure(c => c.SetMergeWith("coverage.json"));
        Assert.Contains("MergeWith=coverage.json", s.ToCollectArgument());
    }

    // ---- include/exclude validation ----

    [Fact]
    public void AddInclude_Without_Bracket_Throws_With_Helpful_Message()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Coverlet.Configure(c => c.AddInclude("Strata.Functions")));
        Assert.Contains("start with '['", ex.Message);
        Assert.Contains("[Strata.Functions]*", ex.Message);
    }

    [Fact]
    public void AddExclude_Without_Bracket_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Coverlet.Configure(c => c.AddExclude("xunit.runner.visualstudio")));
    }

    [Fact]
    public void AddInclude_Empty_Throws()
    {
        Assert.Throws<ArgumentException>(() => Coverlet.Configure(c => c.AddInclude("")));
        Assert.Throws<ArgumentException>(() => Coverlet.Configure(c => c.AddInclude(null!)));
    }

    [Fact]
    public void AddInclude_Wildcard_Assembly_Pattern_Accepted()
    {
        // [*] is the "all assemblies" form — commonly used in
        // "AddInclude("[*]Strata.*") to only count Strata namespaces.
        var s = Coverlet.Configure(c => c.AddInclude("[*]Strata.*"));
        Assert.Contains("[*]Strata.*", s.Include);
    }

    // ---- runsettings xml ----

    [Fact]
    public void ToRunSettingsXml_Has_Expected_Root_Structure()
    {
        var s = Coverlet.Configure(c => c
            .AddFormat(CoverletFormat.OpenCover)
            .AddInclude("[Strata]*"));
        var xml = s.ToRunSettingsXml();
        var doc = XDocument.Parse(xml);
        var dc = doc
            .Element("RunSettings")?
            .Element("DataCollectionRunSettings")?
            .Element("DataCollectors")?
            .Element("DataCollector");
        Assert.NotNull(dc);
        Assert.Equal("XPlat Code Coverage", dc!.Attribute("friendlyName")?.Value);
    }

    [Fact]
    public void ToRunSettingsXml_Configuration_Carries_All_Options()
    {
        var s = Coverlet.Configure(c => c
            .AddFormat(CoverletFormat.Cobertura)
            .AddFormat(CoverletFormat.OpenCover)
            .AddInclude("[Strata.Api]*")
            .AddExclude("[*.Tests]*")
            .AddExcludeByFile(@".*\.generated\.cs")
            .AddExcludeByAttribute("Generated")
            .SetUseSourceLink(true)
            .SetSingleHit()
            .SetDeterministicReport());
        var xml = s.ToRunSettingsXml();
        var doc = XDocument.Parse(xml);
        var config = doc
            .Element("RunSettings")!
            .Element("DataCollectionRunSettings")!
            .Element("DataCollectors")!
            .Element("DataCollector")!
            .Element("Configuration")!;

        Assert.Equal("cobertura,opencover", config.Element("Format")!.Value);
        Assert.Equal("[Strata.Api]*", config.Element("Include")!.Value);
        Assert.Equal("[*.Tests]*", config.Element("Exclude")!.Value);
        Assert.Equal(@".*\.generated\.cs", config.Element("ExcludeByFile")!.Value);
        Assert.Equal("Generated", config.Element("ExcludeByAttribute")!.Value);
        Assert.Equal("true", config.Element("UseSourceLink")!.Value);
        Assert.Equal("true", config.Element("SingleHit")!.Value);
        Assert.Equal("true", config.Element("DeterministicReport")!.Value);
    }

    [Fact]
    public void ToRunSettingsXml_UseSourceLink_Default_Is_False_Explicitly()
    {
        // Sonar correlation needs UseSourceLink=false. The wrapper
        // emits it explicitly (not just on opt-in) so the runsettings
        // is grep-friendly.
        var s = Coverlet.Configure(_ => { });
        var xml = s.ToRunSettingsXml();
        var config = XDocument.Parse(xml)
            .Element("RunSettings")!
            .Element("DataCollectionRunSettings")!
            .Element("DataCollectors")!
            .Element("DataCollector")!
            .Element("Configuration")!;
        Assert.Equal("false", config.Element("UseSourceLink")!.Value);
    }

    [Fact]
    public void ToRunSettingsXml_Empty_Formats_List_Omits_The_Element()
    {
        // When you didn't pick formats, let Coverlet's default apply.
        var s = Coverlet.Configure(_ => { });
        var config = XDocument.Parse(s.ToRunSettingsXml())
            .Element("RunSettings")!
            .Element("DataCollectionRunSettings")!
            .Element("DataCollectors")!
            .Element("DataCollector")!
            .Element("Configuration")!;
        Assert.Null(config.Element("Format"));
    }

    [Fact]
    public void ToRunSettingsXml_Is_Valid_Xml()
    {
        var s = Coverlet.Configure(c => c
            .AddFormat(CoverletFormat.Cobertura)
            .AddInclude("[Strata]*"));
        // Reparse to ensure round-trip validity.
        var doc = XDocument.Parse(s.ToRunSettingsXml());
        Assert.NotNull(doc.Declaration);
        Assert.Equal("RunSettings", doc.Root!.Name.LocalName);
    }

    // ---- enum to wire value ----

    [Theory]
    [InlineData(CoverletFormat.Json, "json")]
    [InlineData(CoverletFormat.Cobertura, "cobertura")]
    [InlineData(CoverletFormat.OpenCover, "opencover")]
    [InlineData(CoverletFormat.Lcov, "lcov")]
    [InlineData(CoverletFormat.TeamCity, "teamcity")]
    public void Format_Enum_Wire_Values(CoverletFormat fmt, string expected)
    {
        var s = Coverlet.Configure(c => c.AddFormat(fmt));
        Assert.Contains($"Format={expected}", s.ToCollectArgument());
    }

    // ---- random filter shapes ----

    [Fact]
    public void Many_Include_Patterns_Preserve_Order_Under_Random_Names()
    {
        // Coverlet's evaluator processes Include patterns left-to-right.
        // Order is observable when patterns overlap.
        var faker = new Faker();
        var patterns = Enumerable.Range(0, 6)
            .Select(_ => $"[{faker.System.FileName().Replace('.', '_')}]*")
            .ToArray();
        var s = Coverlet.Configure(c =>
        {
            foreach (var p in patterns) c.AddInclude(p);
        });
        Assert.Equal(patterns, s.Include);
        Assert.Contains($"Include={string.Join(',', patterns)}", s.ToCollectArgument());
    }
}
