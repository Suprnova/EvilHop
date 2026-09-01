using EvilHop.Common;
using EvilHop.Serialization;
using EvilHop.Validation;

namespace EvilHop.Corpus.Tests;

public class ManifestTests : IDisposable
{
    private readonly string path = Path.Combine(Path.GetTempPath(), $"evilhop-corpus-manifest-tests-{Guid.NewGuid()}.json");

    public void Dispose()
    {
        if (File.Exists(path)) File.Delete(path);
        GC.SuppressFinalize(this);
    }

    private Manifest LoadJson(string json)
    {
        File.WriteAllText(path, json);
        return Manifest.Load(path);
    }

    [Fact]
    public void Load_FullManifest_ParsesEveryField()
    {
        var manifest = LoadJson("""
            {
              "schema": 1,
              "defaultGlobals": ["boot.HIP", "font.HIP", "plat.HIP"],
              "builds": [
                { "id": "bfbb-gc-ntsc-release", "directory": "bfbb/gc/ntsc", "globals": ["extra.HIP"] }
              ],
              "cohorts": [
                { "id": "player-pl01", "archive": "PL01.HIP", "members": ["hb0?.hip", "bb0?.hip"] }
              ],
              "overrides": [
                { "path": "bfbb/gc/ntsc/font2.HIP", "quirks": "OmitsPlatformBlock", "note": "lacks a Platform block, otherwise an ordinary BFBB archive" }
              ]
            }
            """);

        Assert.Equal(1, manifest.Schema);
        Assert.Equal(["boot.HIP", "font.HIP", "plat.HIP"], manifest.DefaultGlobals);

        var build = Assert.Single(manifest.Builds);
        Assert.Equal("bfbb-gc-ntsc-release", build.Id);
        Assert.Equal("bfbb/gc/ntsc", build.Directory);
        Assert.Equal(["extra.HIP"], build.Globals);

        var cohort = Assert.Single(manifest.Cohorts);
        Assert.Equal("player-pl01", cohort.Id);
        Assert.Equal("PL01.HIP", cohort.Archive);
        Assert.Equal(["hb0?.hip", "bb0?.hip"], cohort.Members);

        var overRide = Assert.Single(manifest.Overrides);
        Assert.Equal("bfbb/gc/ntsc/font2.HIP", overRide.Path);
        Assert.Equal(FormatQuirks.OmitsPlatformBlock, overRide.Quirks);
        Assert.Equal("lacks a Platform block, otherwise an ordinary BFBB archive", overRide.Note);
    }

    [Fact]
    public void Load_ManifestWithOnlySchemaAndBuilds_DefaultsEverythingElseToEmpty()
    {
        var manifest = LoadJson("""
            {
              "schema": 1,
              "builds": [ { "id": "n100f-gc-ntsc", "directory": "n100f/gc/ntsc" } ]
            }
            """);

        Assert.Empty(manifest.DefaultGlobals);
        Assert.Empty(manifest.Cohorts);
        Assert.Empty(manifest.Overrides);
        Assert.Empty(Assert.Single(manifest.Builds).Globals);
    }

    [Fact]
    public void Load_OverrideWithoutGameRoleOrQuirks_LeavesThemNull()
    {
        var manifest = LoadJson("""
            {
              "schema": 1,
              "builds": [],
              "overrides": [ { "path": "bfbb/gc/ntsc/mystery.HIP", "note": "unresolved" } ]
            }
            """);

        var overRide = Assert.Single(manifest.Overrides);
        Assert.Null(overRide.Game);
        Assert.Null(overRide.Role);
        Assert.Null(overRide.Quirks);
    }

    [Fact]
    public void Load_OverrideWithGameAndRole_ParsesBoth()
    {
        var manifest = LoadJson("""
            {
              "schema": 1,
              "builds": [],
              "overrides": [ { "path": "bfbb/gc/ntsc/oddity.HIP", "game": "N100F", "role": "Global" } ]
            }
            """);

        var overRide = Assert.Single(manifest.Overrides);
        Assert.Equal(GameVersion.N100F, overRide.Game);
        Assert.Equal(ArchiveRole.Global, overRide.Role);
    }

    [Fact]
    public void Empty_HasNoDefaultGlobalsBuildsCohortsOrOverrides()
    {
        Assert.Empty(Manifest.Empty.DefaultGlobals);
        Assert.Empty(Manifest.Empty.Builds);
        Assert.Empty(Manifest.Empty.Cohorts);
        Assert.Empty(Manifest.Empty.Overrides);
    }
}
