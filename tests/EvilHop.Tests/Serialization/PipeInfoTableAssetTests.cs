using EvilHop.Assets;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class PipeInfoTableAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new N100FSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.PipeInfoTable;
        header.Debug = debug;

        return (header, debug);
    }

    private static Asset Read(byte[] data, FormatProfile profile)
    {
        var (header, debug) = HeaderFor();
        using var reader = new EndianReader(new MemoryStream(data), profile.Endianness);
        return AssetCodecs.Read(reader, header, debug, profile);
    }

    private static byte[] Write(Asset asset, FormatProfile profile)
    {
        using var stream = new MemoryStream();
        using (var writer = new EndianWriter(stream, profile.Endianness, leaveOpen: true))
            AssetCodecs.Write(asset, writer, profile);
        return stream.ToArray();
    }

    private static byte[] Count(int count) => [.. BitConverter.GetBytes(count).Reverse()];

    private static byte[] BfbbEntry(uint modelId, uint subObjectBits, uint pipeFlags) =>
    [
        .. BitConverter.GetBytes(modelId).Reverse(),
        .. BitConverter.GetBytes(subObjectBits).Reverse(),
        .. BitConverter.GetBytes(pipeFlags).Reverse(),
    ];

    private static byte[] MovieEntry(uint modelId, uint subObjectBits, uint flags, byte layer, byte alphaDiscard) =>
    [
        .. BitConverter.GetBytes(modelId).Reverse(),
        .. BitConverter.GetBytes(subObjectBits).Reverse(),
        .. BitConverter.GetBytes(flags).Reverse(),
        layer,
        alphaDiscard,
        0x00, 0x00, // PipePad
    ];

    private static byte[] BfbbData() =>
    [
        .. Count(2),
        .. BfbbEntry(0x11111111, 0xFFFFFFFF, 0x00992505),
        .. BfbbEntry(0x22222222, 0x00000001, 0x00000000),
    ];

    private static byte[] TssmData() =>
    [
        .. Count(2),
        .. MovieEntry(0x11111111, 0xFFFFFFFF, 0x00006501, 0x1C, 0x80),
        .. MovieEntry(0x22222222, 0x00000004, 0x00000000, 0x18, 0x00),
    ];

    [Fact]
    public void Read_PipeInfoTable_UnderBfbb_ProducesPipeInfoTableAsset() =>
        Assert.IsType<PipeInfoTableAsset>(Read(BfbbData(), BFBBSerializer.DefaultProfile));

    [Fact]
    public void Read_PipeInfoTable_UnderBfbb_PopulatesEveryField()
    {
        var asset = (PipeInfoTableAsset)Read(BfbbData(), BFBBSerializer.DefaultProfile);

        Assert.Equal(2, asset.Entries.Count);

        var first = asset.Entries[0];
        Assert.Equal(new AssetId(0x11111111), first.ModelId);
        Assert.Equal(0xFFFFFFFFu, first.SubObjectBits);
        Assert.Equal(0x00992505u, first.Flags.Value);
        Assert.Equal(0, first.Flags.AlphaCompare);
        Assert.True(first.Flags.IgnoreFog);
        Assert.Equal(RwBlendFunction.One, first.Flags.DestinationBlend);
        Assert.Equal(RwBlendFunction.SourceAlpha, first.Flags.SourceBlend);
        Assert.Equal(PipeLightingMode.LightKitOnly, first.Flags.LightingMode);
        Assert.Equal(PipeCullMode.Unknown, first.Flags.CullMode);
        Assert.Equal(PipeZWriteMode.Disabled, first.Flags.ZWriteMode);
        Assert.Equal(default, first.Layer);
        Assert.Equal(0, first.AlphaDiscard);

        var second = asset.Entries[1];
        Assert.Equal(new AssetId(0x22222222), second.ModelId);
        Assert.Equal(1u, second.SubObjectBits);
        Assert.Equal(0u, second.Flags.Value);
    }

    [Fact]
    public void Read_PipeInfoTable_UnderTSSM_PopulatesLayerAndAlphaDiscard()
    {
        var asset = (PipeInfoTableAsset)Read(TssmData(), TSSMSerializer.DefaultProfile);

        var first = asset.Entries[0];
        Assert.Equal(0x00006501u, first.Flags.Value);
        Assert.Equal(PipeLayer.PreLastFx, first.Layer);
        Assert.Equal(0x80, first.AlphaDiscard);

        var second = asset.Entries[1];
        Assert.Equal(PipeLayer.PrePtank, second.Layer);
        Assert.Equal(0, second.AlphaDiscard);
    }

    [Fact]
    public void Read_ThenWrite_PipeInfoTableUnderBfbb_ReproducesInputBytes()
    {
        byte[] data = BfbbData();
        var profile = BFBBSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_PipeInfoTableUnderTSSM_ReproducesInputBytes()
    {
        byte[] data = TssmData();
        var profile = TSSMSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_PipeInfoTableUnderIncredibles_ReproducesInputBytes()
    {
        byte[] data = TssmData();
        var profile = IncrediblesSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_PipeInfoTableUnderROTU_ReproducesInputBytes()
    {
        byte[] data = TssmData();
        var profile = ROTUSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_PipeInfoTableUnderRatatouille_ReproducesInputBytes()
    {
        byte[] data = TssmData();
        var profile = RatatouilleSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_PipeInfoTableWithNoEntries_ReproducesInputBytes()
    {
        byte[] data = Count(0);
        var profile = BFBBSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_PipeInfoTableWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. BfbbData(), 0xDE, 0xAD, 0xBE, 0xEF];
        var profile = BFBBSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_PipeInfoTable_UnderN100F_DegradesToGenericAsset()
    {
        byte[] data = BfbbData();

        var asset = Read(data, N100FSerializer.DefaultProfile);

        Assert.IsNotType<PipeInfoTableAsset>(asset);
        Assert.Equal(data, asset.GetUnparsedTail().ToArray());
    }

    [Fact]
    public void Count_DisagreeingWithEntries_IsStoredIndependently()
    {
        var asset = new PipeInfoTableAsset();
        asset.Entries.Add(new PipeInfoEntry());

        asset.Physical.Count = 5;

        Assert.Equal(5, asset.Physical.Count);
        Assert.Single(asset.Entries);
    }

    [Fact]
    public void Count_MatchingEntries_DerivesFromEntries()
    {
        var asset = new PipeInfoTableAsset();
        asset.Entries.Add(new PipeInfoEntry());
        asset.Entries.Add(new PipeInfoEntry());

        asset.Physical.Count = 2;
        asset.Entries.Add(new PipeInfoEntry());

        Assert.Equal(3, asset.Physical.Count);
    }

    [Theory]
    [InlineData(0xFF000000u, 255)]
    [InlineData(0x00000000u, 0)]
    public void PipeRenderFlags_WithAlphaCompare_ReplacesOnlyThatField(uint initial, byte expected)
    {
        var flags = new PipeRenderFlags(initial).WithAlphaCompare(expected);

        Assert.Equal(expected, flags.AlphaCompare);
        Assert.Equal(initial & 0x00FFFFFFu, flags.Value & 0x00FFFFFFu);
    }

    [Fact]
    public void PipeRenderFlags_WithSourceAndDestinationBlend_MatchSourceBits()
    {
        var flags = new PipeRenderFlags(0)
            .WithSourceBlend(RwBlendFunction.SourceAlpha)
            .WithDestinationBlend(RwBlendFunction.InverseSourceAlpha);

        Assert.Equal(RwBlendFunction.SourceAlpha, flags.SourceBlend);
        Assert.Equal(RwBlendFunction.InverseSourceAlpha, flags.DestinationBlend);
        Assert.Equal(0x00006500u, flags.Value);
    }
}
