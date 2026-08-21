using EvilHop.Blocks;
using EvilHop.Primitives;
using EvilHop.Serialization;

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
    public void WriteBlock_Pver_WritesExpectedBytes()
    {
        var serializer = new TestSerializer();
        var block = serializer.CreateBlock<PackageVersion>();
        block.SubVersion = 2;
        block.ClientVersion = ClientVersion.N100FRelease;
        block.CompatVersion = 1;

        var expected = BlockBytes.Build("PVER", BlockBytes.Content(w =>
        {
            w.WriteEvilInt(2);
            w.WriteEvilInt(0x00040006);
            w.WriteEvilInt(1);
        }));
        Assert.Equal(expected, BlockBytes.WriteBlock(serializer, block));
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
    public void WriteBlock_Pflg_WritesExpectedBytes()
    {
        var serializer = new TestSerializer();
        var block = serializer.CreateBlock<PackageFlags>();
        block.Flags = PackFlags.Default;

        var expected = BlockBytes.Build("PFLG", BlockBytes.Content(w => w.WriteEvilInt(0x2E)));
        Assert.Equal(expected, BlockBytes.WriteBlock(serializer, block));
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
    public void WriteBlock_Pcnt_WritesExpectedBytes()
    {
        var serializer = new TestSerializer();
        var block = serializer.CreateBlock<PackageCount>();
        block.AssetCount = 1;
        block.LayerCount = 2;
        block.MaxAssetSize = 3;
        block.MaxLayerSize = 4;
        block.MaxXFormAssetSize = 5;

        var expected = BlockBytes.Build("PCNT", BlockBytes.Content(w =>
        {
            w.WriteEvilInt(1);
            w.WriteEvilInt(2);
            w.WriteEvilInt(3);
            w.WriteEvilInt(4);
            w.WriteEvilInt(5);
        }));
        Assert.Equal(expected, BlockBytes.WriteBlock(serializer, block));
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
    public void WriteBlock_Pcrt_WritesRawUnixTimeAndDateString()
    {
        var serializer = new TestSerializer();
        var block = serializer.CreateBlock<PackageCreated>();
        block.CreatedDate = DateTimeOffset.FromUnixTimeSeconds(1028661674);
        block.CreatedDateString = "Tue Aug 06 12:21:14 2002\n";

        var expected = BlockBytes.Build("PCRT", BlockBytes.Content(w =>
        {
            w.WriteEvilInt(1028661674);
            w.WriteEvilString("Tue Aug 06 12:21:14 2002\n");
        }));
        Assert.Equal(expected, BlockBytes.WriteBlock(serializer, block));
    }

    [Fact]
    public void ReadBlock_Pmod_ReadsRawUnixTimeAsUtc()
    {
        var content = BlockBytes.Content(w => w.WriteEvilInt(1029000000));
        var reader = BlockBytes.Reader("PMOD", content);

        var block = (PackageModified)new TestSerializer().ReadBlockPublic(reader);

        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1029000000), block.ModifiedDate);
    }

    [Fact]
    public void WriteBlock_Pmod_WritesRawUnixTime()
    {
        var serializer = new TestSerializer();
        var block = serializer.CreateBlock<PackageModified>();
        block.ModifiedDate = DateTimeOffset.FromUnixTimeSeconds(1029000000);

        var expected = BlockBytes.Build("PMOD", BlockBytes.Content(w => w.WriteEvilInt(1029000000)));
        Assert.Equal(expected, BlockBytes.WriteBlock(serializer, block));
    }

    [Fact]
    public void WriteBlock_Plat_PlatformNameRegionLanguage_WritesBfbbLayout()
    {
        var serializer = new TestSerializer(N100FSerializer.DefaultProfile with
        {
            PlatformFieldOrder = PlatformFieldOrder.PlatformNameRegionLanguage
        });
        var block = serializer.CreateBlock<PackagePlatform>();
        block.PlatformId = "GC";
        block.PlatformName = "GameCube";
        block.Region = "NTSC";
        block.Language = "US Common";
        block.GameName = "Sponge Bob";

        var expected = BlockBytes.Build("PLAT", BlockBytes.Content(w =>
        {
            w.WriteEvilString("GC");
            w.WriteEvilString("GameCube");
            w.WriteEvilString("NTSC");
            w.WriteEvilString("US Common");
            w.WriteEvilString("Sponge Bob");
        }));
        Assert.Equal(expected, BlockBytes.WriteBlock(serializer, block));
    }

    [Fact]
    public void WriteBlock_Plat_LanguageRegion_WritesTssmLayoutAndOmitsPlatformName()
    {
        var serializer = new TestSerializer(N100FSerializer.DefaultProfile with
        {
            PlatformFieldOrder = PlatformFieldOrder.LanguageRegion
        });
        var block = serializer.CreateBlock<PackagePlatform>();
        block.PlatformId = "GC";
        block.Language = "US";
        block.Region = "NTSC";
        block.GameName = "Incredibles";

        var expected = BlockBytes.Build("PLAT", BlockBytes.Content(w =>
        {
            w.WriteEvilString("GC");
            w.WriteEvilString("US");
            w.WriteEvilString("NTSC");
            w.WriteEvilString("Incredibles");
        }));
        Assert.Equal(expected, BlockBytes.WriteBlock(serializer, block));
    }
}
