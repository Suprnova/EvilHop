using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Tests.Serialization;

public class AnimationAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new BFBBSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.Animation;
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

    private static byte[] U16(ushort value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] S16(short value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] U32(uint value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] F32(float value) => [.. BitConverter.GetBytes(value).Reverse()];

    private static byte[] Header(uint flags, ushort boneCount, ushort timeCount, uint keyCount, Vector3 scale, uint magic = 0x31424B53) =>
    [
        .. U32(magic),
        .. U32(flags),
        .. U16(boneCount),
        .. U16(timeCount),
        .. U32(keyCount),
        .. F32(scale.X), .. F32(scale.Y), .. F32(scale.Z),
    ];

    private static byte[] Key(ushort timeIndex, short qx, short qy, short qz, short qw, short tx, short ty, short tz) =>
    [
        .. U16(timeIndex),
        .. S16(qx), .. S16(qy), .. S16(qz), .. S16(qw),
        .. S16(tx), .. S16(ty), .. S16(tz),
    ];

    private static byte[] OneBoneOneFrameData() =>
    [
        .. Header(flags: 0, boneCount: 1, timeCount: 2, keyCount: 1, new Vector3(1, 1, 1)),
        .. Key(0, 1, 2, 3, 4, 5, 6, 7),
        .. F32(0.0f), .. F32(0.5f), // Times
        .. U16(0),                  // Offsets: bone 0 at time 0 starts at key 0
    ];

    [Fact]
    public void Read_Animation_ProducesAnimationAsset() =>
        Assert.IsType<AnimationAsset>(Read(OneBoneOneFrameData()));

    [Fact]
    public void Read_Animation_PopulatesHeaderAndKeys()
    {
        var asset = (AnimationAsset)Read(OneBoneOneFrameData());

        Assert.Equal(0x31424B53u, asset.Physical.Magic);
        Assert.Equal(1, asset.Physical.BoneCount);
        Assert.Equal(new Vector3(1, 1, 1), asset.Scale);
        Assert.Single(asset.Keys);
        Assert.Equal(0, asset.Keys[0].TimeIndex);
        Assert.Equal(new Vector4(1, 2, 3, 4), asset.Keys[0].Quat);
        Assert.Equal(new Vector3(5, 6, 7), asset.Keys[0].Tran);
        Assert.Equal(2, asset.Times.Count);
        Assert.Equal(0.0f, asset.Times[0]);
        Assert.Equal(0.5f, asset.Times[1]);
        Assert.Single(asset.Offsets);
        Assert.Equal(0, asset.Offsets[0]);
    }

    [Fact]
    public void Read_Animation_KeyCountKeepsDerivingAfterKeysAreMutated()
    {
        var asset = (AnimationAsset)Read(OneBoneOneFrameData());
        Assert.Equal(1u, asset.Physical.KeyCount);

        asset.Keys.Add(new AnimationKey());

        Assert.Equal(2u, asset.Physical.KeyCount);
    }

    [Fact]
    public void Read_ThenWrite_Animation_ReproducesInputBytes()
    {
        byte[] data = OneBoneOneFrameData();

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_AnimationWithMultipleBonesAndTimes_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. Header(flags: 0x1000, boneCount: 2, timeCount: 3, keyCount: 3, new Vector3(2, 2, 2)),
            .. Key(0, 1, 2, 3, 4, 5, 6, 7),
            .. Key(0, 8, 9, 10, 11, 12, 13, 14),
            .. Key(1, 15, 16, 17, 18, 19, 20, 21),
            .. F32(0.0f), .. F32(0.5f), .. F32(1.0f), // Times
            .. U16(0), .. U16(1), // Offsets @ time 0: bone0->key0, bone1->key1
            .. U16(2), .. U16(2), // Offsets @ time 1: bone0->key2, bone1->key2
        ];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_AnimationWithNoTimes_ReproducesInputBytes()
    {
        byte[] data = Header(flags: 0, boneCount: 0, timeCount: 0, keyCount: 0, Vector3.Zero);

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_AnimationWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. OneBoneOneFrameData(), 0xDE, 0xAD, 0xBE, 0xEF];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_AnimationUnderN100F_ReproducesInputBytes()
    {
        byte[] data = OneBoneOneFrameData();
        var profile = N100FSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_AnimationWithNonStandardMagic_ReproducesInputBytes()
    {
        // Real N100F archives carry a magic other than the SKB1-derived constant every other
        // supported game writes; it must round-trip as read rather than being normalized.
        byte[] data =
        [
            .. Header(flags: 0, boneCount: 1, timeCount: 2, keyCount: 1, new Vector3(1, 1, 1), magic: 0x5153504D),
            .. Key(0, 1, 2, 3, 4, 5, 6, 7),
            .. F32(0.0f), .. F32(0.5f),
            .. U16(0),
        ];
        var profile = N100FSerializer.DefaultProfile;

        var asset = (AnimationAsset)Read(data, profile);

        Assert.Equal(0x5153504Du, asset.Physical.Magic);
        Assert.Equal(data, Write(asset, profile));
    }

    [Fact]
    public void Read_Animation_UnderROTU_DegradesToGenericAsset()
    {
        byte[] data = OneBoneOneFrameData();

        var asset = Read(data, ROTUSerializer.DefaultProfile);

        Assert.IsNotType<AnimationAsset>(asset);
        Assert.Equal(data, asset.GetUnparsedTail().ToArray());
    }

}
