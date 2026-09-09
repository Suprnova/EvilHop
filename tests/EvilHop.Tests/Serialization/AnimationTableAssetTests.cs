using EvilHop.Assets;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class AnimationTableAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new BFBBSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.AnimationTable;
        header.Debug = debug;

        return (header, debug);
    }

    private static Asset Read(byte[] data, FormatProfile? profile = null)
    {
        profile ??= BFBBSerializer.DefaultProfile;
        var (header, debug) = HeaderFor();
        using var reader = new EndianReader(new MemoryStream(data), profile.Endianness);
        return AssetCodecs.Read(reader, header, debug, profile);
    }

    private static byte[] Write(Asset asset, FormatProfile? profile = null)
    {
        profile ??= BFBBSerializer.DefaultProfile;
        using var stream = new MemoryStream();
        using (var writer = new EndianWriter(stream, profile.Endianness, leaveOpen: true))
            AssetCodecs.Write(asset, writer, profile);
        return stream.ToArray();
    }

    private static byte[] U16(ushort value) => BitConverter.GetBytes(value).Reverse().ToArray();
    private static byte[] U32(uint value) => BitConverter.GetBytes(value).Reverse().ToArray();
    private static byte[] I32(int value) => BitConverter.GetBytes(value).Reverse().ToArray();
    private static byte[] F32(float value) => BitConverter.GetBytes(value).Reverse().ToArray();

    private static byte[] TableHeader(int rawCount, int fileCount, int stateCount, uint constructFunc) =>
    [
        .. U32(0x4C425441), // Magic
        .. U32((uint)rawCount),
        .. U32((uint)fileCount),
        .. U32((uint)stateCount),
        .. U32(constructFunc),
    ];

    private static byte[] File(uint fileFlags, float duration, float timeOffset, ushort numAnimsX, ushort numAnimsY,
        uint rawDataOffset, int physics, int startPose, int endPose) =>
    [
        .. U32(fileFlags), .. F32(duration), .. F32(timeOffset),
        .. U16(numAnimsX), .. U16(numAnimsY),
        .. U32(rawDataOffset), .. I32(physics), .. I32(startPose), .. I32(endPose),
    ];

    private static byte[] State(uint stateId, uint fileIndex, uint effectCount, uint effectOffset, float speed,
        uint subStateId, uint subStateCount) =>
    [
        .. U32(stateId), .. U32(fileIndex), .. U32(effectCount), .. U32(effectOffset),
        .. F32(speed), .. U32(subStateId), .. U32(subStateCount),
    ];

    private static byte[] SingleFileTableData() =>
    [
        .. TableHeader(rawCount: 1, fileCount: 1, stateCount: 1, constructFunc: 36),
        .. U32(0xAAAAAAAA), // Raw[0]
        .. File(0xF0, 0.0f, -1.0f, 1, 1, 0xD4, -1, -1, -1),
        .. State(0x6C9F2581, 0, 0, 0xD4, 1.0f, 0, 0),
        .. U32(0), // trailing raw-index pool: file 0's single index into Raw
    ];

    [Fact]
    public void Read_AnimationTable_ProducesAnimationTableAsset() =>
        Assert.IsType<AnimationTableAsset>(Read(SingleFileTableData()));

    [Fact]
    public void Read_AnimationTable_PopulatesRawFilesAndStates()
    {
        var asset = (AnimationTableAsset)Read(SingleFileTableData());

        Assert.Equal(36u, asset.ConstructFunc);
        Assert.Single(asset.Raw);
        Assert.Equal(new AssetId(0xAAAAAAAA), asset.Raw[0]);

        Assert.Single(asset.Files);
        var file = asset.Files[0];
        Assert.Equal(0xF0u, file.FileFlags);
        Assert.Equal(-1.0f, file.TimeOffset);
        Assert.Equal(1, file.NumAnimsX);
        Assert.Equal(1, file.NumAnimsY);
        Assert.Equal(-1, file.Physics);
        Assert.Equal(-1, file.StartPose);
        Assert.Equal(-1, file.EndPose);

        Assert.Single(asset.States);
        var state = asset.States[0];
        Assert.Equal(0x6C9F2581u, state.StateId);
        Assert.Equal(0u, state.EffectCount);
        Assert.Equal(1.0f, state.Speed);
    }

    [Fact]
    public void Read_AnimationTable_RawCountKeepsDerivingAfterRawIsMutated()
    {
        var asset = (AnimationTableAsset)Read(SingleFileTableData());
        Assert.Equal(1u, asset.Physical.RawCount);

        asset.Raw.Add(AssetId.None);

        Assert.Equal(2u, asset.Physical.RawCount);
    }

    [Fact]
    public void Read_ThenWrite_AnimationTable_ReproducesInputBytes()
    {
        byte[] data = SingleFileTableData();

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_AnimationTableWithMultipleEntriesAndEffectData_ReproducesInputBytes()
    {
        // The bytes between the state array and the trailing raw-index pool are effect records this
        // type doesn't individually parse; they must still round-trip byte-exact via the unparsed tail.
        byte[] data =
        [
            .. TableHeader(rawCount: 2, fileCount: 2, stateCount: 2, constructFunc: 1),
            .. U32(0x11111111), .. U32(0x22222222), // Raw
            .. File(0xF0, 1.0f, 0.0f, 1, 1, 0x60, -1, -1, -1),
            .. File(0xF0, 2.0f, 0.0f, 1, 1, 0x64, -1, -1, -1),
            .. State(0xAAAAAAAA, 0, 1, 0x68, 1.0f, 0, 0),
            .. State(0xBBBBBBBB, 1, 0, 0x68, 1.0f, 0, 0),
            .. new byte[] { 0x01, 0x02, 0x03, 0x04 }, // opaque effect record bytes
            .. U32(0), .. U32(1), // trailing raw-index pool
        ];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_AnimationTableWithNoEntries_ReproducesInputBytes()
    {
        byte[] data = TableHeader(rawCount: 0, fileCount: 0, stateCount: 0, constructFunc: 0);

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_AnimationTableUnderRatatouille_ReproducesInputBytes()
    {
        byte[] data = SingleFileTableData();
        var profile = RatatouilleSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_AnimationTable_UnderN100F_DegradesToGenericAsset()
    {
        byte[] data = SingleFileTableData();

        var asset = Read(data, N100FSerializer.DefaultProfile);

        Assert.IsNotType<AnimationTableAsset>(asset);
        Assert.Equal(data, asset.GetUnparsedTail().ToArray());
    }
}
