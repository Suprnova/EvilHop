using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;

namespace EvilHop.Tests.Serialization;

public class DictionaryTests
{
    [Fact]
    public void ReadBlock_Ainf_ReadsExpectedFields()
    {
        var content = BlockBytes.Content(w => w.WriteEvilInt(0));
        var reader = BlockBytes.Reader("AINF", content);

        var block = (AssetInf)new TestSerializer().ReadBlockPublic(reader);

        Assert.Equal(0u, block.Value);
    }

    [Fact]
    public void ReadBlock_Ahdr_ReadsExpectedFieldsAndAdbgChild()
    {
        var adbg = BlockBytes.Content(w =>
        {
            w.Write([0xFF, 0xFF, 0xFF, 0xFF]);
            w.WriteEvilString("test_asset");
            w.WriteEvilString("");
            w.WriteEvilInt(0x12345678);
        });
        var content = BlockBytes.Content(w =>
        {
            w.WriteEvilInt(1001);
            w.WriteEvilInt((uint)AssetType.Texture);
            w.WriteEvilInt(0);
            w.WriteEvilInt(8);
            w.WriteEvilInt(0);
            w.WriteEvilInt((uint)AssetFlags.None);
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
    public void ReadBlock_Ahdr_NonStandardTypeValue_ReadsAsIs()
    {
        var adbg = BlockBytes.Content(w =>
        {
            w.WriteEvilInt(0);
            w.WriteEvilString("");
            w.WriteEvilString("");
            w.WriteEvilInt(0);
        });
        var content = BlockBytes.Content(w =>
        {
            w.WriteEvilInt(0);
            w.WriteEvilInt(0xFFFFFFFF);
            w.WriteEvilInt(0);
            w.WriteEvilInt(0);
            w.WriteEvilInt(0);
            w.WriteEvilInt(0);
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
            w.WriteEvilInt(0x12345678);
        });
        var reader = BlockBytes.Reader("ADBG", content);

        var block = (AssetDebug)new TestSerializer().ReadBlockPublic(reader);

        Assert.Equal(-1, block.Alignment);
        Assert.Equal("test_asset", block.Name);
        Assert.Equal("source.txt", block.FileName);
        Assert.Equal(0x12345678u, block.Checksum);
    }

    [Fact]
    public void ReadBlock_Adbg_PositiveAlignment_ReadsAsSignedInt()
    {
        var content = BlockBytes.Content(w =>
        {
            w.WriteEvilInt(16);
            w.WriteEvilString("");
            w.WriteEvilString("");
            w.WriteEvilInt(0);
        });
        var reader = BlockBytes.Reader("ADBG", content);

        var block = (AssetDebug)new TestSerializer().ReadBlockPublic(reader);

        Assert.Equal(16, block.Alignment);
    }

    [Fact]
    public void ReadBlock_Linf_ReadsExpectedFields()
    {
        var content = BlockBytes.Content(w => w.WriteEvilInt(0));
        var reader = BlockBytes.Reader("LINF", content);

        var block = (LayerInf)new TestSerializer().ReadBlockPublic(reader);

        Assert.Equal(0u, block.Value);
    }

    [Fact]
    public void ReadBlock_Lhdr_ReadsExpectedFieldsAndAssetIdsArray()
    {
        var ldbg = BlockBytes.Content(w => w.WriteEvilInt(0xFFFFFFFF));
        var content = BlockBytes.Content(w =>
        {
            w.WriteEvilInt((uint)LayerType.Default);
            w.WriteEvilInt(3);
            w.WriteEvilInt(101);
            w.WriteEvilInt(102);
            w.WriteEvilInt(103);
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
    public void ReadBlock_Ldbg_ReadsExpectedFields()
    {
        var content = BlockBytes.Content(w => w.WriteEvilInt(0xFFFFFFFF));
        var reader = BlockBytes.Reader("LDBG", content);

        var block = (LayerDebug)new TestSerializer().ReadBlockPublic(reader);

        Assert.Equal(0xFFFFFFFFu, block.Value);
    }
}
