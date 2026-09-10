using EvilHop.Blocks;
using EvilHop.Primitives;

namespace EvilHop.Tests.Serialization;

public class HIPBSerializationTests
{
    [Fact]
    public void ReadBlock_Hipb_Version0_ReadsOnlyVersion()
    {
        var content = BlockBytes.Content(w => w.Write(0));
        using var reader = BlockBytes.Reader("HIPB", content);

        var block = (HIPB)new TestSerializer().ReadBlockPublic(reader);

        Assert.Equal(0u, block.Version);
        Assert.Equal(0u, block.HasNoLayers);
        Assert.Equal(HIPBPlatform.Unknown, block.Platform);
        Assert.Empty(block.LayerNames);
        Assert.Equal(HIPBGame.Unknown, block.Game);
    }

    [Fact]
    public void ReadBlock_Hipb_Version1_ReadsHasNoLayers()
    {
        var content = BlockBytes.Content(w =>
        {
            w.Write(1);
            w.Write(1);
        });
        using var reader = BlockBytes.Reader("HIPB", content);

        var block = (HIPB)new TestSerializer().ReadBlockPublic(reader);

        Assert.Equal(1u, block.Version);
        Assert.Equal(1u, block.HasNoLayers);
    }

    [Fact]
    public void ReadBlock_Hipb_Version2_ReadsPlatformAndLayerNames()
    {
        var content = BlockBytes.Content(w =>
        {
            w.Write(2);
            w.Write(0);
            w.Write((int)HIPBPlatform.GameCube);
            w.Write(2);
            w.Write(0);
            w.WriteEvilString("Main");
            w.Write(3);
            w.WriteEvilString("Boss");
        });
        using var reader = BlockBytes.Reader("HIPB", content);

        var block = (HIPB)new TestSerializer().ReadBlockPublic(reader);

        Assert.Equal(2u, block.Version);
        Assert.Equal(HIPBPlatform.GameCube, block.Platform);
        Assert.Equal(new Dictionary<int, string> { [0] = "Main", [3] = "Boss" }, block.LayerNames);
        Assert.Equal(HIPBGame.Unknown, block.Game);
    }

    [Fact]
    public void ReadBlock_Hipb_Version3_ReadsGame()
    {
        var content = BlockBytes.Content(w =>
        {
            w.Write(3);
            w.Write(0);
            w.Write((int)HIPBPlatform.Unknown);
            w.Write(0);
            w.Write((int)HIPBGame.Incredibles);
        });
        using var reader = BlockBytes.Reader("HIPB", content);

        var block = (HIPB)new TestSerializer().ReadBlockPublic(reader);

        Assert.Equal(3u, block.Version);
        Assert.Equal(HIPBGame.Incredibles, block.Game);
    }

    [Fact]
    public void ReadBlock_Hipb_VersionAboveCurrent_StillReadsKnownFields()
    {
        var content = BlockBytes.Content(w =>
        {
            w.Write(4);
            w.Write(1);
            w.Write((int)HIPBPlatform.Xbox);
            w.Write(0);
            w.Write((int)HIPBGame.ROTU);
            w.Write(0xDEADBEEF); // an unknown version-4 field, ignored rather than misread
        });
        using var reader = BlockBytes.Reader("HIPB", content);

        var block = (HIPB)new TestSerializer().ReadBlockPublic(reader);

        Assert.Equal(4u, block.Version);
        Assert.Equal(1u, block.HasNoLayers);
        Assert.Equal(HIPBPlatform.Xbox, block.Platform);
        Assert.Equal(HIPBGame.ROTU, block.Game);
    }

    [Fact]
    public void ReadBlock_Hipb_MalformedLayerNameString_DoesNotThrow()
    {
        var content = BlockBytes.Content(w =>
        {
            w.Write(2);
            w.Write(0);
            w.Write((int)HIPBPlatform.GameCube);
            w.Write(1);
            w.Write(0);
            w.Write("ab"u8.ToArray());
            w.Write((byte)0x00);
            w.Write((byte)0x01); // malformed second null byte
        });
        using var reader = BlockBytes.Reader("HIPB", content);

        var exception = Record.Exception(() => new TestSerializer().ReadBlockPublic(reader));

        Assert.Null(exception);
    }

    [Fact]
    public void ReadBlock_Hipb_TruncatedStream_DoesNotThrow()
    {
        // Declares version 1, but the content ends before HasNoLayers.
        var content = BlockBytes.Content(w => w.Write(1));
        using var reader = BlockBytes.Reader("HIPB", content);

        var block = (HIPB)new TestSerializer().ReadBlockPublic(reader);

        Assert.Equal(1u, block.Version);
        Assert.Equal(0u, block.HasNoLayers);
    }

    [Fact]
    public void Read_ArchiveWithMalformedTrailingHipb_StillReturnsPrecedingRoots()
    {
        // A version this build doesn't understand, with no bytes left for even its known fields.
        var hipbContent = BlockBytes.Content(w => w.Write(int.MaxValue));
        byte[] bytes =
        [
            .. BlockBytes.Build("HIPA", []),
            .. BlockBytes.Build("HIPB", hipbContent),
        ];
        using var stream = new MemoryStream(bytes);

        var roots = new TestSerializer().Read(stream);

        Assert.Equal(["HIPA", "HIPB"], roots.Select(r => r.Tag));
    }

    [Fact]
    public void WriteBlock_Hipb_Version0_WritesOnlyVersion()
    {
        var serializer = new TestSerializer();
        var block = serializer.CreateBlock<HIPB>();
        block.Version = 0;
        block.HasNoLayers = 1;
        block.Platform = HIPBPlatform.Xbox;
        block.LayerNames[0] = "Main";
        block.Game = HIPBGame.BFBB;

        var expected = BlockBytes.Build("HIPB", BlockBytes.Content(w => w.Write(0)));
        Assert.Equal(expected, BlockBytes.WriteBlock(serializer, block));
    }

    [Fact]
    public void WriteBlock_Hipb_Version1_WritesVersionAndHasNoLayersOnly()
    {
        var serializer = new TestSerializer();
        var block = serializer.CreateBlock<HIPB>();
        block.Version = 1;
        block.HasNoLayers = 1;
        block.Platform = HIPBPlatform.Xbox;
        block.LayerNames[0] = "Main";
        block.Game = HIPBGame.BFBB;

        var expected = BlockBytes.Build("HIPB", BlockBytes.Content(w =>
        {
            w.Write(1);
            w.Write(1);
        }));
        Assert.Equal(expected, BlockBytes.WriteBlock(serializer, block));
    }

    [Fact]
    public void WriteBlock_Hipb_Version2_WritesPlatformAndLayerNamesButNotGame()
    {
        var serializer = new TestSerializer();
        var block = serializer.CreateBlock<HIPB>();
        block.Version = 2;
        block.HasNoLayers = 0;
        block.Platform = HIPBPlatform.PlayStation2;
        block.LayerNames[5] = "Intro";
        block.Game = HIPBGame.ROTU;

        var expected = BlockBytes.Build("HIPB", BlockBytes.Content(w =>
        {
            w.Write(2);
            w.Write(0);
            w.Write((int)HIPBPlatform.PlayStation2);
            w.Write(1);
            w.Write(5);
            w.WriteEvilString("Intro");
        }));
        Assert.Equal(expected, BlockBytes.WriteBlock(serializer, block));
    }

    [Fact]
    public void WriteBlock_Hipb_Version3_WritesEveryField()
    {
        var serializer = new TestSerializer();
        var block = serializer.CreateBlock<HIPB>();
        block.Version = 3;
        block.HasNoLayers = 0;
        block.Platform = HIPBPlatform.GameCube;
        block.LayerNames[1] = "Kitchen";
        block.Game = HIPBGame.Ratatouille;

        var expected = BlockBytes.Build("HIPB", BlockBytes.Content(w =>
        {
            w.Write(3);
            w.Write(0);
            w.Write((int)HIPBPlatform.GameCube);
            w.Write(1);
            w.Write(1);
            w.WriteEvilString("Kitchen");
            w.Write((int)HIPBGame.Ratatouille);
        }));
        Assert.Equal(expected, BlockBytes.WriteBlock(serializer, block));
    }
}
