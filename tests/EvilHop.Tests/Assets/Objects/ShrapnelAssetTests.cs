using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.IO;

namespace EvilHop.Tests.Serialization;

public class ShrapnelAssetTests
{
    private readonly ShrapnelAsset _asset;

    public ShrapnelAssetTests()
    {
        _asset = new ShrapnelAsset();
    }

    private static (AssetHeader Header, AssetDebug Debug) HeaderFor(uint id = 0x12345678)
    {
        var serializer = new BFBBSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.Shrapnel;
        header.Id = id;
        header.Debug = debug;

        return (header, debug);
    }

    private static Asset Read(byte[] data, FormatProfile? profile = null, uint id = 0x12345678)
    {
        profile ??= BFBBSerializer.DefaultProfile;
        var (header, debug) = HeaderFor(id);
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

    private static byte[] U32(uint value, bool bigEndian = true)
    {
        var bytes = BitConverter.GetBytes(value);
        return bigEndian ? [.. bytes.Reverse()] : bytes;
    }

    private static byte[] S32(int value, bool bigEndian = true) => U32((uint)value, bigEndian);

    private static byte[] F32(float value, bool bigEndian = true)
    {
        var bytes = BitConverter.GetBytes(value);
        return bigEndian ? [.. bytes.Reverse()] : bytes;
    }

    private static byte[] HeaderBytes(int fragCount, uint shrapnelId = 0x12345678, uint initCb = 0, bool bigEndian = true) =>
    [
        .. S32(fragCount, bigEndian),
        .. U32(shrapnelId, bigEndian),
        .. U32(initCb, bigEndian),
    ];

    private static byte[] FragBytes(
        ShrapnelFragType type,
        uint id,
        uint parentId0 = 0,
        uint parentId1 = 0,
        float lifetime = 1.0f,
        float delay = 0.0f,
        byte[]? payload = null,
        bool bigEndian = true) =>
    [
        .. U32((uint)type, bigEndian),
        .. U32(id, bigEndian),
        .. U32(parentId0, bigEndian),
        .. U32(parentId1, bigEndian),
        .. F32(lifetime, bigEndian),
        .. F32(delay, bigEndian),
        .. (payload ?? []),
    ];

    [Fact]
    public void Type_IsShrapnel()
    {
        Assert.Equal(AssetType.Shrapnel, _asset.Type);
    }

    [Fact]
    public void Frags_DefaultsToEmpty()
    {
        Assert.Empty(_asset.Frags);
    }

    [Fact]
    public void Physical_FragCount_DefaultsToFragsCount()
    {
        Assert.Equal(0, _asset.Physical.FragCount);

        _asset.Frags.Add(new ShrapnelFrag());
        Assert.Equal(1, _asset.Physical.FragCount);

        _asset.Frags.Add(new ShrapnelFrag());
        Assert.Equal(2, _asset.Physical.FragCount);
    }

    [Fact]
    public void Physical_ShrapnelId_DefaultsToAssetId()
    {
        Assert.Equal(_asset.Id, _asset.Physical.ShrapnelId);

        _asset.Id = new AssetId(0xAABBCCDD);
        Assert.Equal(new AssetId(0xAABBCCDD), _asset.Physical.ShrapnelId);
    }

    [Fact]
    public void Physical_FragCount_WhenOverridden_DoesNotTrackFragsCountUntilCleared()
    {
        _asset.Physical.FragCount = 10;
        Assert.Equal(10, _asset.Physical.FragCount);

        _asset.Frags.Add(new ShrapnelFrag());
        Assert.Equal(10, _asset.Physical.FragCount);

        _asset.Physical.FragCount = 1;
        Assert.Equal(1, _asset.Physical.FragCount);

        _asset.Frags.Add(new ShrapnelFrag());
        Assert.Equal(2, _asset.Physical.FragCount);
    }

    [Fact]
    public void Physical_ShrapnelId_WhenOverridden_DoesNotTrackAssetIdUntilCleared()
    {
        _asset.Physical.ShrapnelId = new AssetId(0x11111111);
        Assert.Equal(new AssetId(0x11111111), _asset.Physical.ShrapnelId);

        _asset.Id = new AssetId(0x22222222);
        Assert.Equal(new AssetId(0x11111111), _asset.Physical.ShrapnelId);

        _asset.Physical.ShrapnelId = new AssetId(0x22222222);
        Assert.Equal(new AssetId(0x22222222), _asset.Physical.ShrapnelId);

        _asset.Id = new AssetId(0x33333333);
        Assert.Equal(new AssetId(0x33333333), _asset.Physical.ShrapnelId);
    }

    [Fact]
    public void ShrapnelFrag_PropertiesSetAndGet()
    {
        var frag = new ShrapnelFrag
        {
            Type = ShrapnelFragType.Shrapnel,
            Id = new AssetId(0x11223344),
            ParentId0 = new AssetId(0x22334455),
            ParentId1 = new AssetId(0x33445566),
            Lifetime = 2.5f,
            Delay = 0.5f,
            Data = [0xAA, 0xBB, 0xCC, 0xDD],
        };

        Assert.Equal(ShrapnelFragType.Shrapnel, frag.Type);
        Assert.Equal(new AssetId(0x11223344), frag.Id);
        Assert.Equal(new AssetId(0x22334455), frag.ParentId0);
        Assert.Equal(new AssetId(0x33445566), frag.ParentId1);
        Assert.Equal(2.5f, frag.Lifetime);
        Assert.Equal(0.5f, frag.Delay);
        Assert.Equal(new byte[] { 0xAA, 0xBB, 0xCC, 0xDD }, frag.Data);
    }

    [Fact]
    public void Read_HeaderOnly_EmptyFrags_RoundTrips()
    {
        byte[] data = HeaderBytes(0, shrapnelId: 0x12345678, initCb: 0);

        var asset = (ShrapnelAsset)Read(data, id: 0x12345678);

        Assert.Empty(asset.Frags);
        Assert.Equal(0, asset.Physical.FragCount);
        Assert.Equal(new AssetId(0x12345678), asset.Physical.ShrapnelId);
        Assert.Empty(asset.GetUnparsedTail().ToArray());

        Assert.Equal(data, Write(asset));
    }

    [Fact]
    public void Read_PopulatesFragsAndRoundTrips()
    {
        byte[] frag2Payload = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08]; // 8 bytes (0x20 - 24)
        byte[] frag6Payload = new byte[0x4C - 24]; // 52 bytes under BFBB
        frag6Payload[0] = 0xEE;
        frag6Payload[^1] = 0xFF;

        byte[] data =
        [
            .. HeaderBytes(2, shrapnelId: 0x12345678),
            .. FragBytes(ShrapnelFragType.Shrapnel, 0x10101010, 0x20202020, 0x30303030, lifetime: 3.0f, delay: 0.25f, payload: frag2Payload),
            .. FragBytes(ShrapnelFragType.Sound, 0x40404040, 0x50505050, 0x60606060, lifetime: 1.5f, delay: 0.1f, payload: frag6Payload),
        ];

        var asset = (ShrapnelAsset)Read(data, id: 0x12345678);

        Assert.Equal(2, asset.Frags.Count);

        var f0 = asset.Frags[0];
        Assert.Equal(ShrapnelFragType.Shrapnel, f0.Type);
        Assert.Equal(new AssetId(0x10101010), f0.Id);
        Assert.Equal(new AssetId(0x20202020), f0.ParentId0);
        Assert.Equal(new AssetId(0x30303030), f0.ParentId1);
        Assert.Equal(3.0f, f0.Lifetime);
        Assert.Equal(0.25f, f0.Delay);
        Assert.Equal(frag2Payload, f0.Data);

        var f1 = asset.Frags[1];
        Assert.Equal(ShrapnelFragType.Sound, f1.Type);
        Assert.Equal(new AssetId(0x40404040), f1.Id);
        Assert.Equal(new AssetId(0x50505050), f1.ParentId0);
        Assert.Equal(new AssetId(0x60606060), f1.ParentId1);
        Assert.Equal(1.5f, f1.Lifetime);
        Assert.Equal(0.1f, f1.Delay);
        Assert.Equal(frag6Payload, f1.Data);

        Assert.Equal(data, Write(asset));
    }

    [Fact]
    public void Read_FragCountAndShrapnelIdKeepDerivingAfterCollectionsAreMutated()
    {
        byte[] data =
        [
            .. HeaderBytes(1, shrapnelId: 0x12345678),
            .. FragBytes(ShrapnelFragType.Shrapnel, 0x10101010, payload: new byte[8]),
        ];

        var asset = (ShrapnelAsset)Read(data, id: 0x12345678);
        Assert.Equal(1, asset.Physical.FragCount);
        Assert.Equal(new AssetId(0x12345678), asset.Physical.ShrapnelId);

        asset.Frags.Add(new ShrapnelFrag { Type = ShrapnelFragType.Shrapnel, Data = new byte[8] });
        Assert.Equal(2, asset.Physical.FragCount);

        asset.Id = new AssetId(0x99999999);
        Assert.Equal(new AssetId(0x99999999), asset.Physical.ShrapnelId);
    }

    [Fact]
    public void Read_PreservesUnparsedTail()
    {
        byte[] unparsedTail = [0xFE, 0xED, 0xFA, 0xCE];
        byte[] data =
        [
            .. HeaderBytes(0, shrapnelId: 0x12345678),
            .. unparsedTail,
        ];

        var asset = (ShrapnelAsset)Read(data, id: 0x12345678);
        Assert.Equal(unparsedTail, asset.GetUnparsedTail().ToArray());

        Assert.Equal(data, Write(asset));
    }

    [Fact]
    public void Read_UnderUnsupportedGame_DegradesToGenericAsset()
    {
        byte[] data = HeaderBytes(0, shrapnelId: 0x12345678);

        var asset = Read(data, N100FSerializer.DefaultProfile, id: 0x12345678);

        Assert.IsNotType<ShrapnelAsset>(asset, exactMatch: false);
    }

    [Fact]
    public void Read_ThrowsOnUnsupportedFragTypeForGame()
    {
        // Type 8 (Explosion) is not supported under BFBB
        byte[] data =
        [
            .. HeaderBytes(1, shrapnelId: 0x12345678),
            .. FragBytes(ShrapnelFragType.Explosion, 0x10101010, payload: new byte[48]),
        ];

        Assert.Throws<InvalidDataException>(() => Read(data, BFBBSerializer.DefaultProfile, id: 0x12345678));
    }

    [Fact]
    public void Read_ThrowsOnUnknownFragType()
    {
        byte[] data =
        [
            .. HeaderBytes(1, shrapnelId: 0x12345678),
            .. FragBytes((ShrapnelFragType)99, 0x10101010, payload: new byte[8]),
        ];

        Assert.Throws<InvalidDataException>(() => Read(data, BFBBSerializer.DefaultProfile, id: 0x12345678));
    }

    [Theory]
    [InlineData(GameVersion.BFBB, ShrapnelFragType.Particle, 0x1D4)]
    [InlineData(GameVersion.BFBB, ShrapnelFragType.Projectile, 0x90)]
    [InlineData(GameVersion.BFBB, ShrapnelFragType.Lightning, 0x68)]
    [InlineData(GameVersion.BFBB, ShrapnelFragType.Sound, 0x4C)]
    [InlineData(GameVersion.BFBB, ShrapnelFragType.Shockwave, 0x54)]
    [InlineData(GameVersion.TSSM, ShrapnelFragType.Particle, 0x1F4)]
    [InlineData(GameVersion.TSSM, ShrapnelFragType.Projectile, 0x110)]
    [InlineData(GameVersion.TSSM, ShrapnelFragType.Lightning, 0x70)]
    [InlineData(GameVersion.TSSM, ShrapnelFragType.Sound, 0x44)]
    [InlineData(GameVersion.TSSM, ShrapnelFragType.Explosion, 0x48)]
    [InlineData(GameVersion.TSSM, ShrapnelFragType.Distortion, 0x5C)]
    [InlineData(GameVersion.TSSM, ShrapnelFragType.Fire, 0x5C)]
    [InlineData(GameVersion.Incredibles, ShrapnelFragType.Projectile, 0x110)]
    [InlineData(GameVersion.Incredibles, ShrapnelFragType.Fire, 0x5C)]
    [InlineData(GameVersion.ROTU, ShrapnelFragType.Projectile, 0x158)]
    [InlineData(GameVersion.ROTU, ShrapnelFragType.Fire, 0xB4)]
    [InlineData(GameVersion.ROTU, ShrapnelFragType.Light, 0x60)]
    [InlineData(GameVersion.ROTU, ShrapnelFragType.Smoke, 0x50)]
    [InlineData(GameVersion.ROTU, ShrapnelFragType.Goo, 0x88)]
    [InlineData(GameVersion.Ratatouille, ShrapnelFragType.Projectile, 0x158)]
    [InlineData(GameVersion.Ratatouille, ShrapnelFragType.Light, 0x60)]
    [InlineData(GameVersion.Ratatouille, ShrapnelFragType.Goo, 0x88)]
    public void Read_ThenWrite_EachFragType_RoundTripsAccurately(GameVersion game, ShrapnelFragType type, int totalSize)
    {
        var profile = Serializer.DefaultProfileFor(game);
        byte[] payload = new byte[totalSize - 24];
        for (int i = 0; i < payload.Length; i++)
            payload[i] = (byte)(i & 0xFF);

        byte[] data =
        [
            .. HeaderBytes(1, shrapnelId: 0x55667788),
            .. FragBytes(type, 0x11223344, 0x22334455, 0x33445566, lifetime: 5.0f, delay: 0.5f, payload: payload),
        ];

        var asset = (ShrapnelAsset)Read(data, profile, id: 0x55667788);

        Assert.Single(asset.Frags);
        Assert.Equal(type, asset.Frags[0].Type);
        Assert.Equal(new AssetId(0x11223344), asset.Frags[0].Id);
        Assert.Equal(5.0f, asset.Frags[0].Lifetime);
        Assert.Equal(0.5f, asset.Frags[0].Delay);
        Assert.Equal(payload, asset.Frags[0].Data);

        byte[] written = Write(asset, profile);
        Assert.Equal(data, written);
    }

    [Theory]
    [InlineData(3u, 0x1FC)]
    [InlineData(4u, 0x114)]
    [InlineData(6u, 0x48)]
    [InlineData(9u, 0x60)]
    public void Read_ThenWrite_InactiveFragUnderTSSM_RoundTripsAccurately(uint markerId, int totalSize)
    {
        var profile = Serializer.DefaultProfileFor(GameVersion.TSSM);
        byte[] payload = new byte[totalSize - 24];
        for (int i = 0; i < payload.Length; i++)
            payload[i] = (byte)(i & 0xFF);

        byte[] data =
        [
            .. HeaderBytes(1, shrapnelId: 0x55667788),
            .. FragBytes(ShrapnelFragType.Inactive, markerId, 0x22334455, 0x33445566, lifetime: 0.0f, delay: 0.5f, payload: payload),
        ];

        var asset = (ShrapnelAsset)Read(data, profile, id: 0x55667788);

        Assert.Single(asset.Frags);
        Assert.Equal(ShrapnelFragType.Inactive, asset.Frags[0].Type);
        Assert.Equal(new AssetId(markerId), asset.Frags[0].Id);
        Assert.Equal(payload, asset.Frags[0].Data);

        Assert.Equal(data, Write(asset, profile));
    }

    [Fact]
    public void Read_ThrowsOnUnrecognizedInactiveMarker()
    {
        byte[] data =
        [
            .. HeaderBytes(1, shrapnelId: 0x12345678),
            .. FragBytes(ShrapnelFragType.Inactive, 0xDEADBEEF, payload: []),
        ];

        Assert.Throws<InvalidDataException>(() => Read(data, Serializer.DefaultProfileFor(GameVersion.TSSM), id: 0x12345678));
    }

    [Fact]
    public void Read_ThrowsOnInactiveFragUnderUnverifiedGame()
    {
        // The id=6 -> 0x48 marker is only confirmed for TSSM; other games must still fail loudly.
        byte[] data =
        [
            .. HeaderBytes(1, shrapnelId: 0x12345678),
            .. FragBytes(ShrapnelFragType.Inactive, 6, payload: new byte[0x48 - 24]),
        ];

        Assert.Throws<InvalidDataException>(() => Read(data, BFBBSerializer.DefaultProfile, id: 0x12345678));
    }

    [Fact]
    public void Read_RealTSSMExemplar_RoundTripsExactly()
    {
        // From de01.HOP: AHDR id=0x4E11860F "SHRP", size=1764. Frag 5 is an Inactive marker (id=6)
        // sandwiched between real Projectile/Particle/Distortion fragments.
        byte[] data =
        [
            .. HeaderBytes(7, shrapnelId: 0x4E11860F),

            .. FragBytes(ShrapnelFragType.Projectile, 0x78EF8BB0, lifetime: 3.0f, payload: new byte[0x110 - 24]),
            .. FragBytes(ShrapnelFragType.Projectile, 0x78EF8BB1, lifetime: 3.0f, payload: new byte[0x110 - 24]),
            .. FragBytes(ShrapnelFragType.Projectile, 0xEB300BFD, lifetime: 3.0f, payload: new byte[0x110 - 24]),
            .. FragBytes(ShrapnelFragType.Projectile, 0xEB300BFE, lifetime: 3.0f, payload: new byte[0x110 - 24]),
            .. FragBytes(ShrapnelFragType.Particle, 0x38A9BED6, lifetime: 0.2f, payload: new byte[0x1F4 - 24]),
            .. FragBytes(ShrapnelFragType.Inactive, 6, 0xD7AFC6CA, lifetime: 0.0f, delay: 3.0f, payload: new byte[0x48 - 24]),
            .. FragBytes(ShrapnelFragType.Distortion, 0x831684E8, lifetime: 1.0f, payload: new byte[0x5C - 24]),
        ];

        var profile = Serializer.DefaultProfileFor(GameVersion.TSSM);
        var asset = (ShrapnelAsset)Read(data, profile, id: 0x4E11860F);

        Assert.Equal(7, asset.Frags.Count);
        Assert.Equal(ShrapnelFragType.Inactive, asset.Frags[5].Type);
        Assert.Equal(new AssetId(6u), asset.Frags[5].Id);
        Assert.Equal(ShrapnelFragType.Distortion, asset.Frags[6].Type);
        Assert.Equal(new AssetId(0x831684E8), asset.Frags[6].Id);

        Assert.Equal(data, Write(asset, profile));
    }

    [Theory]
    [InlineData(ShrapnelFragType.Particle, 0x1D0)]
    [InlineData(ShrapnelFragType.Projectile, 0x58)]
    public void Read_ThenWrite_FragWithoutExtendedFields_ReproducesInputBytes(ShrapnelFragType type, int totalSize)
    {
        // BFBB's leftover gl/Working and gl/New Folder archives predate several fields both of
        // these fragment types later grew - see BuildProfiles.json's
        // "bfbb/**/gl/Working/**"/"bfbb/**/gl/New Folder/**" entries.
        byte[] payload = new byte[totalSize - 24];
        for (int i = 0; i < payload.Length; i++)
            payload[i] = (byte)(i & 0xFF);

        byte[] data =
        [
            .. HeaderBytes(1, shrapnelId: 0x55667788),
            .. FragBytes(type, 0x11223344, 0x22334455, 0x33445566, lifetime: 5.0f, delay: 0.5f, payload: payload),
        ];
        var profile = BFBBSerializer.DefaultProfile with { ShrapnelHasExtendedFragFields = false };

        var asset = (ShrapnelAsset)Read(data, profile, id: 0x55667788);

        Assert.Single(asset.Frags);
        Assert.Equal(type, asset.Frags[0].Type);
        Assert.Equal(payload, asset.Frags[0].Data);
        Assert.Equal(data, Write(asset, profile));
    }

    /// <summary>
    /// db05 pairs 0x40 sound fragments with full-size 0x90 projectiles, so the two switches have to
    /// move independently.
    /// </summary>
    [Fact]
    public void Read_ThenWrite_ShortSoundFragAlongsideFullProjectile_ReproducesInputBytes()
    {
        byte[] soundPayload = new byte[0x40 - 24];
        byte[] projectilePayload = new byte[0x90 - 24];
        for (int i = 0; i < projectilePayload.Length; i++)
            projectilePayload[i] = (byte)(i & 0xFF);

        byte[] data =
        [
            .. HeaderBytes(2, shrapnelId: 0x55667788),
            .. FragBytes(ShrapnelFragType.Projectile, 0x11223344, lifetime: 5.0f, payload: projectilePayload),
            .. FragBytes(ShrapnelFragType.Sound, 0x22334455, lifetime: 2.25f, payload: soundPayload),
        ];
        var profile = BFBBSerializer.DefaultProfile with { ShrapnelSoundHasExtendedFields = false };

        var asset = (ShrapnelAsset)Read(data, profile, id: 0x55667788);

        Assert.Equal(2, asset.Frags.Count);
        Assert.Equal(projectilePayload, asset.Frags[0].Data);
        Assert.Equal(soundPayload, asset.Frags[1].Data);
        Assert.Equal(data, Write(asset, profile));
    }

    [Fact]
    public void Write_GuardedNonShrapnelAsset_DoesNotThrow()
    {
        var generic = new GenericAsset(AssetType.Shrapnel);
        generic.SetUnparsedTail([0x01, 0x02, 0x03, 0x04]);

        byte[] written = Write(generic);
        Assert.Equal([0x01, 0x02, 0x03, 0x04], written);
    }
}
