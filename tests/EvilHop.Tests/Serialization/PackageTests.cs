using EvilHop.Blocks;
using EvilHop.Primitives;

namespace EvilHop.Tests.Serialization;

public class PackageTests
{
    [Fact]
    public void ReadBlock_Pver_ReadsExpectedFields()
    {
        var content = BlockBytes.Content(w =>
        {
            w.WriteEvilInt(2);
            w.WriteEvilInt(0x00040006);
            w.WriteEvilInt(1);
        });
        var reader = BlockBytes.Reader("PVER", content);

        var block = (PackageVersion)new TestSerializer().ReadBlockPublic(reader);

        Assert.Equal(2u, block.SubVersion);
        Assert.Equal(ClientVersion.N100FRelease, block.ClientVersion);
        Assert.Equal(1u, block.CompatVersion);
    }

    [Fact]
    public void ReadBlock_Pflg_ReadsExpectedFields()
    {
        var content = BlockBytes.Content(w => w.WriteEvilInt(0x2E));
        var reader = BlockBytes.Reader("PFLG", content);

        var block = (PackageFlags)new TestSerializer().ReadBlockPublic(reader);

        Assert.Equal(PackFlags.Default, block.Flags);
    }

    [Fact]
    public void ReadBlock_Pflg_NonStandardValue_ReadsAsIs()
    {
        var content = BlockBytes.Content(w => w.WriteEvilInt(0xFFFFFFFF));
        var reader = BlockBytes.Reader("PFLG", content);

        var block = (PackageFlags)new TestSerializer().ReadBlockPublic(reader);

        Assert.Equal((PackFlags)0xFFFFFFFF, block.Flags);
    }

    [Fact]
    public void ReadBlock_Pcnt_ReadsExpectedFields()
    {
        var content = BlockBytes.Content(w =>
        {
            w.WriteEvilInt(1);
            w.WriteEvilInt(2);
            w.WriteEvilInt(3);
            w.WriteEvilInt(4);
            w.WriteEvilInt(5);
        });
        var reader = BlockBytes.Reader("PCNT", content);

        var block = (PackageCount)new TestSerializer().ReadBlockPublic(reader);

        Assert.Equal(1u, block.AssetCount);
        Assert.Equal(2u, block.LayerCount);
        Assert.Equal(3u, block.MaxAssetSize);
        Assert.Equal(4u, block.MaxLayerSize);
        Assert.Equal(5u, block.MaxXFormAssetSize);
    }

    [Fact]
    public void ReadBlock_Pcrt_ReadsRawUnixTimeAsUtcAndReadsDateString()
    {
        var content = BlockBytes.Content(w =>
        {
            w.WriteEvilInt(1028661674);
            w.WriteEvilString("Tue Aug 06 12:21:14 2002\n");
        });
        var reader = BlockBytes.Reader("PCRT", content);

        var block = (PackageCreated)new TestSerializer().ReadBlockPublic(reader);

        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1028661674), block.CreatedDate);
        Assert.Equal("Tue Aug 06 12:21:14 2002\n", block.CreatedDateString);
    }

    [Fact]
    public void ReadBlock_Pmod_ReadsRawUnixTimeAsUtc()
    {
        var content = BlockBytes.Content(w => w.WriteEvilInt(1029000000));
        var reader = BlockBytes.Reader("PMOD", content);

        var block = (PackageModified)new TestSerializer().ReadBlockPublic(reader);

        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1029000000), block.ModifiedDate);
    }
}
