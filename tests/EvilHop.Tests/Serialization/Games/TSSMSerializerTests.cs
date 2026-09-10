using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class TSSMSerializerTests : SerializerContractTests
{
    private static readonly string UnofficialArchiveFixturePath =
        Path.Combine(AppContext.BaseDirectory, "TestData", "unofficial", "tssm.hip");

    protected override Serializer CreateSerializer() => new TSSMSerializer();

    protected override FileStream OpenMinimalFixture() =>
        File.OpenRead(Path.Combine(AppContext.BaseDirectory, "TestData", "tssm", "minimal.hip"));

    [Fact]
    public void DefaultProfile_IsTSSMWithLanguageRegionPlatformOrder()
    {
        var profile = TSSMSerializer.DefaultProfile;

        Assert.Equal(GameVersion.TSSM, profile.Game);
        Assert.Equal(PlatformFieldOrder.LanguageRegion, profile.PlatformFieldOrder);
        Assert.True(profile.StreamDataHasPaddingField);
    }

    [Fact]
    public void Constructor_WithoutProfile_UsesDefaultProfile()
    {
        var serializer = new TSSMSerializer();

        Assert.Equal(TSSMSerializer.DefaultProfile, serializer.Profile);
    }

    [Fact]
    public void Constructor_WithOverride_KeepsOtherSwitches()
    {
        var profile = TSSMSerializer.DefaultProfile with { StreamDataHasPaddingField = false };

        var serializer = new TSSMSerializer(profile);

        Assert.False(serializer.Profile.StreamDataHasPaddingField);
        Assert.Equal(TSSMSerializer.DefaultProfile.PlatformFieldOrder, serializer.Profile.PlatformFieldOrder);
    }

    [Fact(Skip = "omitted pending the appropriate licensing of test fixture")]
    public void Read_UnofficialArchiveFixture_ParsesTrailingHipbBlock()
    {
        using var stream = File.OpenRead(UnofficialArchiveFixturePath);
        var roots = new TSSMSerializer().Read(stream);

        var hipb = Assert.IsType<HIPB>(roots[^1]);
        Assert.Equal(2u, hipb.Version);
        Assert.Equal(0u, hipb.HasNoLayers);
        Assert.Equal(HIPBPlatform.GameCube, hipb.Platform);
        Assert.Equal(HIPBGame.Unknown, hipb.Game);
        Assert.Empty(hipb.LayerNames);
    }

    [Fact(Skip = "omitted pending the appropriate licensing of test fixture")]
    public void Read_ThenWrite_UnofficialArchiveFixture_ProducesIdenticalBytes()
    {
        byte[] originalBytes = File.ReadAllBytes(UnofficialArchiveFixturePath);
        var roots = new TSSMSerializer().Read(new MemoryStream(originalBytes));

        using var rewritten = new MemoryStream();
        new TSSMSerializer().Write(rewritten, roots);

        Assert.Equal(originalBytes, rewritten.ToArray());
    }
}
