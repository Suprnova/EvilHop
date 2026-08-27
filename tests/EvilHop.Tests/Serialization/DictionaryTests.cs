using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;

namespace EvilHop.Tests.Serialization;

public class DictionaryTests
{
    [Fact]
    public void ReadBlock_Ainf_ReadsExpectedFields()
    {
        var content = BlockBytes.Content(w => w.Write(0));
        var reader = BlockBytes.Reader("AINF", content);

        var block = (AssetInf)new TestSerializer().ReadBlockPublic(reader);

        Assert.Equal(0u, block.Value);
    }

    [Fact]
    public void WriteBlock_Ainf_WritesExpectedBytes()
    {
        var serializer = new TestSerializer();
        var block = serializer.CreateBlock<AssetInf>();
        block.Value = 0;

        var expected = BlockBytes.Build("AINF", BlockBytes.Content(w => w.Write(0)));
        Assert.Equal(expected, BlockBytes.WriteBlock(serializer, block));
    }

    [Fact]
    public void ReadBlock_Ahdr_ReadsExpectedFieldsAndAdbgChild()
    {
        var adbg = BlockBytes.Content(w =>
        {
            w.Write([0xFF, 0xFF, 0xFF, 0xFF]);
            w.WriteEvilString("test_asset");
            w.WriteEvilString("");
            w.Write(0x12345678);
        });
        var content = BlockBytes.Content(w =>
        {
            w.Write(1001);
            w.Write((uint)AssetType.Texture);
            w.Write(0);
            w.Write(8);
            w.Write(0);
            w.Write((uint)AssetFlags.None);
            w.Write(BlockBytes.Build("ADBG", adbg));
        });
        var reader = BlockBytes.Reader("AHDR", content);

        var block = (AssetHeader)new TestSerializer().ReadBlockPublic(reader);

        Assert.Equal(1001u, block.Id);
        Assert.Equal(AssetType.Texture, block.Type);
        Assert.Equal(0u, block.Offset);
        Assert.Equal(8u, block.Size);
        Assert.Equal(0u, block.Plus);
        Assert.Equal(AssetFlags.None, block.Flags);
        Assert.Equal(-1, block.Debug.Alignment);
        Assert.Equal("test_asset", block.Debug.Name);
    }

    [Fact]
    public void WriteBlock_Ahdr_WritesExpectedFieldsAndAdbgChild()
    {
        var serializer = new TestSerializer();
        var debug = serializer.CreateBlock<AssetDebug>();
        debug.Alignment = -1;
        debug.Name = "test_asset";
        debug.FileName = "";
        debug.Checksum = 0x12345678;

        var block = serializer.CreateBlock<AssetHeader>();
        block.Id = 1001;
        block.Type = AssetType.Texture;
        block.Offset = 0;
        block.Size = 8;
        block.Plus = 0;
        block.Flags = AssetFlags.None;
        block.Debug = debug;

        var expectedAdbg = BlockBytes.Content(w =>
        {
            w.Write([0xFF, 0xFF, 0xFF, 0xFF]);
            w.WriteEvilString("test_asset");
            w.WriteEvilString("");
            w.Write(0x12345678);
        });
        var expected = BlockBytes.Build("AHDR", BlockBytes.Content(w =>
        {
            w.Write(1001);
            w.Write((uint)AssetType.Texture);
            w.Write(0);
            w.Write(8);
            w.Write(0);
            w.Write((uint)AssetFlags.None);
            w.Write(BlockBytes.Build("ADBG", expectedAdbg));
        }));
        Assert.Equal(expected, BlockBytes.WriteBlock(serializer, block));
    }

    [Fact]
    public void ReadBlock_Ahdr_NonStandardTypeValue_ReadsAsIs()
    {
        var adbg = BlockBytes.Content(w =>
        {
            w.Write(0);
            w.WriteEvilString("");
            w.WriteEvilString("");
            w.Write(0);
        });
        var content = BlockBytes.Content(w =>
        {
            w.Write(0);
            w.Write(0xFFFFFFFF);
            w.Write(0);
            w.Write(0);
            w.Write(0);
            w.Write(0);
            w.Write(BlockBytes.Build("ADBG", adbg));
        });
        var reader = BlockBytes.Reader("AHDR", content);

        var block = (AssetHeader)new TestSerializer().ReadBlockPublic(reader);

        Assert.Equal((AssetType)0xFFFFFFFF, block.Type);
    }

    [Fact]
    public void ReadBlock_Adbg_ReadsExpectedFields()
    {
        var content = BlockBytes.Content(w =>
        {
            w.Write([0xFF, 0xFF, 0xFF, 0xFF]);
            w.WriteEvilString("test_asset");
            w.WriteEvilString("source.txt");
            w.Write(0x12345678);
        });
        var reader = BlockBytes.Reader("ADBG", content);

        var block = (AssetDebug)new TestSerializer().ReadBlockPublic(reader);

        Assert.Equal(-1, block.Alignment);
        Assert.Equal("test_asset", block.Name);
        Assert.Equal("source.txt", block.FileName);
        Assert.Equal(0x12345678u, block.Checksum);
    }

    [Fact]
    public void WriteBlock_Adbg_WritesExpectedFields()
    {
        var serializer = new TestSerializer();
        var block = serializer.CreateBlock<AssetDebug>();
        block.Alignment = -1;
        block.Name = "test_asset";
        block.FileName = "source.txt";
        block.Checksum = 0x12345678;

        var expected = BlockBytes.Build("ADBG", BlockBytes.Content(w =>
        {
            w.Write([0xFF, 0xFF, 0xFF, 0xFF]);
            w.WriteEvilString("test_asset");
            w.WriteEvilString("source.txt");
            w.Write(0x12345678);
        }));
        Assert.Equal(expected, BlockBytes.WriteBlock(serializer, block));
    }

    [Fact]
    public void WriteBlock_Adbg_PositiveAlignment_WritesAsSignedInt()
    {
        var serializer = new TestSerializer();
        var block = serializer.CreateBlock<AssetDebug>();
        block.Alignment = 16;

        var expected = BlockBytes.Build("ADBG", BlockBytes.Content(w =>
        {
            w.Write(16);
            w.WriteEvilString("");
            w.WriteEvilString("");
            w.Write(0);
        }));
        Assert.Equal(expected, BlockBytes.WriteBlock(serializer, block));
    }

    [Fact]
    public void ReadBlock_Adbg_PositiveAlignment_ReadsAsSignedInt()
    {
        var content = BlockBytes.Content(w =>
        {
            w.Write(16);
            w.WriteEvilString("");
            w.WriteEvilString("");
            w.Write(0);
        });
        var reader = BlockBytes.Reader("ADBG", content);

        var block = (AssetDebug)new TestSerializer().ReadBlockPublic(reader);

        Assert.Equal(16, block.Alignment);
    }

    [Fact]
    public void ReadBlock_Linf_ReadsExpectedFields()
    {
        var content = BlockBytes.Content(w => w.Write(0));
        var reader = BlockBytes.Reader("LINF", content);

        var block = (LayerInf)new TestSerializer().ReadBlockPublic(reader);

        Assert.Equal(0u, block.Value);
    }

    [Fact]
    public void WriteBlock_Linf_WritesExpectedBytes()
    {
        var serializer = new TestSerializer();
        var block = serializer.CreateBlock<LayerInf>();
        block.Value = 0;

        var expected = BlockBytes.Build("LINF", BlockBytes.Content(w => w.Write(0)));
        Assert.Equal(expected, BlockBytes.WriteBlock(serializer, block));
    }

    [Fact]
    public void ReadBlock_Lhdr_ReadsExpectedFieldsAndAssetIdsArray()
    {
        var ldbg = BlockBytes.Content(w => w.Write(0xFFFFFFFF));
        var content = BlockBytes.Content(w =>
        {
            w.Write((uint)LayerType.Default);
            w.Write(3);
            w.Write(101);
            w.Write(102);
            w.Write(103);
            w.Write(BlockBytes.Build("LDBG", ldbg));
        });
        var reader = BlockBytes.Reader("LHDR", content);

        var block = (LayerHeader)new TestSerializer().ReadBlockPublic(reader);

        Assert.Equal(LayerType.Default, block.Type);
        Assert.Equal(3u, block.AssetCount);
        Assert.Equal([101u, 102u, 103u], block.AssetIds);
        Assert.Equal(0xFFFFFFFFu, block.Debug.Value);
    }

    [Fact]
    public void WriteBlock_Lhdr_WritesExpectedFieldsAssetIdsArrayAndLdbgChild()
    {
        var serializer = new TestSerializer();
        var debug = serializer.CreateBlock<LayerDebug>();
        debug.Value = 0xFFFFFFFF;

        var block = serializer.CreateBlock<LayerHeader>();
        block.Type = LayerType.Default;
        block.AssetCount = 3;
        block.AssetIds = [101u, 102u, 103u];
        block.Debug = debug;

        var expectedLdbg = BlockBytes.Content(w => w.Write(0xFFFFFFFF));
        var expected = BlockBytes.Build("LHDR", BlockBytes.Content(w =>
        {
            w.Write((uint)LayerType.Default);
            w.Write(3);
            w.Write(101);
            w.Write(102);
            w.Write(103);
            w.Write(BlockBytes.Build("LDBG", expectedLdbg));
        }));
        Assert.Equal(expected, BlockBytes.WriteBlock(serializer, block));
    }

    [Fact]
    public void ReadBlock_Ldbg_ReadsExpectedFields()
    {
        var content = BlockBytes.Content(w => w.Write(0xFFFFFFFF));
        var reader = BlockBytes.Reader("LDBG", content);

        var block = (LayerDebug)new TestSerializer().ReadBlockPublic(reader);

        Assert.Equal(0xFFFFFFFFu, block.Value);
    }

    [Fact]
    public void WriteBlock_Ldbg_WritesExpectedBytes()
    {
        var serializer = new TestSerializer();
        var block = serializer.CreateBlock<LayerDebug>();
        block.Value = 0xFFFFFFFF;

        var expected = BlockBytes.Build("LDBG", BlockBytes.Content(w => w.Write(0xFFFFFFFF)));
        Assert.Equal(expected, BlockBytes.WriteBlock(serializer, block));
    }
}
