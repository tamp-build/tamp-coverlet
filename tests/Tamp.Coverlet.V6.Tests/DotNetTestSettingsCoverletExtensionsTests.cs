using System.IO;
using System.Xml.Linq;
using Tamp.NetCli.V10;
using Xunit;

namespace Tamp.Coverlet.V6.Tests;

/// <summary>
/// Tests for <see cref="DotNetTestSettingsCoverletExtensions.WithCoverlet(DotNetTestSettings, System.Action{CoverletSettings})"/>
/// — the cross-package extension that collapses the Coverlet-Configure
/// → ToRunSettingsXml → write-to-temp → SetSettings dance into one call
/// (HoldFast canary friction #19, TAM-209).
/// </summary>
public sealed class DotNetTestSettingsCoverletExtensionsTests
{
    [Fact]
    public void WithCoverlet_Configures_DotNetTestSettings_Settings_File()
    {
        var s = new DotNetTestSettings();
        s.WithCoverlet(c => c
            .AddFormat(CoverletFormat.OpenCover)
            .AddInclude("[MyApp]*")
            .AddExclude("[*.Tests]*"));

        Assert.NotNull(s.Settings);
        Assert.True(File.Exists(s.Settings!), $"runsettings should exist at {s.Settings}");
        Assert.EndsWith(".runsettings", s.Settings);
    }

    [Fact]
    public void WithCoverlet_Writes_Valid_Runsettings_XML()
    {
        var s = new DotNetTestSettings();
        s.WithCoverlet(c => c
            .AddFormat(CoverletFormat.OpenCover)
            .AddInclude("[MyApp]*"));

        // Parse the written XML — must round-trip cleanly. XDocument.Load
        // is encoding-strict against the XML declaration, so we read the
        // file as text first and parse the string (avoids the
        // utf-8-text-with-utf-16-declaration mismatch some emitters produce).
        var text = File.ReadAllText(s.Settings!);
        var doc = XDocument.Parse(text);
        Assert.NotNull(doc.Root);
        Assert.Equal("RunSettings", doc.Root!.Name.LocalName);
    }

    [Fact]
    public void WithCoverlet_Includes_Coverlet_Format_In_RunSettings()
    {
        var s = new DotNetTestSettings();
        s.WithCoverlet(c => c.AddFormat(CoverletFormat.Cobertura));

        var content = File.ReadAllText(s.Settings!);
        Assert.Contains("cobertura", content, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WithCoverlet_Includes_Inclusion_Patterns_In_RunSettings()
    {
        var s = new DotNetTestSettings();
        s.WithCoverlet(c => c
            .AddInclude("[ProjectA]*")
            .AddInclude("[ProjectB]*")
            .AddExclude("[*.Tests]*"));

        var content = File.ReadAllText(s.Settings!);
        Assert.Contains("ProjectA", content);
        Assert.Contains("ProjectB", content);
        Assert.Contains("*.Tests", content);
    }

    [Fact]
    public void WithCoverlet_Returns_Same_Instance_For_Chaining()
    {
        var s = new DotNetTestSettings();
        var returned = s.WithCoverlet(c => c.AddFormat(CoverletFormat.OpenCover));
        Assert.Same(s, returned);
    }

    [Fact]
    public void WithCoverlet_Pre_Built_Settings_Overload_Works()
    {
        var coverlet = Coverlet.Configure(c => c.AddFormat(CoverletFormat.OpenCover));
        var s = new DotNetTestSettings().WithCoverlet(coverlet);

        Assert.NotNull(s.Settings);
        Assert.True(File.Exists(s.Settings!));
    }

    [Fact]
    public void WithCoverlet_Each_Call_Creates_Distinct_Runsettings_File()
    {
        var s1 = new DotNetTestSettings().WithCoverlet(c => c.AddFormat(CoverletFormat.OpenCover));
        var s2 = new DotNetTestSettings().WithCoverlet(c => c.AddFormat(CoverletFormat.Cobertura));

        Assert.NotEqual(s1.Settings, s2.Settings);
        Assert.True(File.Exists(s1.Settings!));
        Assert.True(File.Exists(s2.Settings!));
    }

    [Fact]
    public void WithCoverlet_Throws_On_Null_Configure()
    {
        var s = new DotNetTestSettings();
        Assert.Throws<System.ArgumentNullException>(() =>
            s.WithCoverlet((System.Action<CoverletSettings>)null!));
    }

    [Fact]
    public void WithCoverlet_Throws_On_Null_PreBuilt_Settings()
    {
        var s = new DotNetTestSettings();
        Assert.Throws<System.ArgumentNullException>(() =>
            s.WithCoverlet((CoverletSettings)null!));
    }

    [Fact]
    public void WithCoverlet_Throws_On_Null_DotNetTestSettings()
    {
        DotNetTestSettings? s = null;
        Assert.Throws<System.ArgumentNullException>(() =>
            s!.WithCoverlet(c => c.AddFormat(CoverletFormat.OpenCover)));
    }

    [Fact]
    public void WithCoverlet_Composes_With_Other_DotNetTest_Setters()
    {
        // The full adopter idiom — chain WithCoverlet with the normal
        // DotNetTestSettings fluent surface.
        var s = new DotNetTestSettings()
            .SetProject("MySolution.slnx")
            .SetConfiguration(Configuration.Release)
            .SetNoBuild(true)
            .WithCoverlet(c => c.AddFormat(CoverletFormat.OpenCover))
            .SetResultsDirectory("./artifacts/coverage");

        Assert.Equal("MySolution.slnx", s.Project);
        Assert.Equal(Configuration.Release, s.Configuration);
        Assert.True(s.NoBuild);
        Assert.NotNull(s.Settings);
        Assert.Equal("./artifacts/coverage", s.ResultsDirectory);
    }
}
