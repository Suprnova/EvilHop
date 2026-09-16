using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Tests.Serialization;

public class LobMasterAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new N100FSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.LobMaster;
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

    private static byte[] Prefix(byte linkCount) =>
    [
        0x00, 0x00, 0x12, 0x34, // BaseId
        0x23,                   // BaseType
        linkCount,
        0x00, 0x1D,             // BaseFlags
    ];

    private static byte[] Int32(int value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] UInt32(uint value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] Single(float value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] Vector(float x, float y, float z) => [.. Single(x), .. Single(y), .. Single(z)];

    private static byte[] LinkBytes(short sourceEvent, short destinationEvent, uint destinationAssetId) =>
    [
        (byte)(sourceEvent >> 8), (byte)sourceEvent,
        (byte)(destinationEvent >> 8), (byte)destinationEvent,
        .. UInt32(destinationAssetId),
        .. new byte[16], // Params
        .. new byte[4],  // ParamWidgetAssetId
        .. new byte[4],  // CheckAssetId
    ];

    private static byte[] Body(
        int lobMasterType, uint projectileId, Vector3 launchPosition, Vector3 launchRotation,
        float launchSpeed, float launchSpeedVariance, Vector3 modelScale, int enablers,
        float maxLifetime, float maxDistance, uint movePointId, int salvoCount, int ammoCount,
        float arcCoefficient, int debrisConeAngle, int bounceCount, int powerupType,
        float heavyFactor, Vector3 tumbleRotation, float collideDelay, float atRestPeriod, uint mode) =>
    [
        .. Int32(lobMasterType),
        .. UInt32(projectileId),
        .. Vector(launchPosition.X, launchPosition.Y, launchPosition.Z),
        .. Vector(launchRotation.X, launchRotation.Y, launchRotation.Z),
        .. Single(launchSpeed),
        .. Single(launchSpeedVariance),
        .. Vector(modelScale.X, modelScale.Y, modelScale.Z),
        .. Int32(enablers),
        .. Single(maxLifetime),
        .. Single(maxDistance),
        .. UInt32(movePointId),
        .. Int32(salvoCount),
        .. Int32(ammoCount),
        .. Single(arcCoefficient),
        .. Int32(debrisConeAngle),
        .. Int32(bounceCount),
        .. Int32(powerupType),
        .. Single(heavyFactor),
        .. Vector(tumbleRotation.X, tumbleRotation.Y, tumbleRotation.Z),
        .. Single(collideDelay),
        .. Single(atRestPeriod),
        .. UInt32(mode),
    ];

    private static byte[] SampleData() =>
    [
        .. Prefix(0),
        .. Body(
            0, 0x4974022F, Vector3.Zero, new Vector3(0, 90.0f, 0), 10.0f, 20.0f, Vector3.One,
            0x37, 10.0f, 70.0f, 0, 3, -1, -1.0f, 70, 1, 4, 10.0f,
            new Vector3(300.0f, 200.0f, 300.0f), 0.5f, 5.0f, 0),
    ];

    [Fact]
    public void Read_LobMaster_UnderN100F_ProducesLobMasterAsset() =>
        Assert.IsType<LobMasterAsset>(Read(SampleData(), N100FSerializer.DefaultProfile));

    [Fact]
    public void Read_LobMaster_PopulatesEveryField()
    {
        var asset = (LobMasterAsset)Read(SampleData(), N100FSerializer.DefaultProfile);

        Assert.Equal(0, asset.LobMasterType);
        Assert.Equal(new AssetId(0x4974022F), asset.ProjectileId);
        Assert.Equal(Vector3.Zero, asset.LaunchPosition);
        Assert.Equal(new Vector3(0, 90.0f, 0), asset.LaunchRotation);
        Assert.Equal(10.0f, asset.LaunchSpeed);
        Assert.Equal(20.0f, asset.LaunchSpeedVariance);
        Assert.Equal(Vector3.One, asset.ModelScale);
        Assert.Equal(
            LobMasterEnablers.Unknown1 | LobMasterEnablers.Unknown2 | LobMasterEnablers.Unknown4 | LobMasterEnablers.Unknown16 | LobMasterEnablers.Unknown32,
            asset.Enablers);
        Assert.Equal(10.0f, asset.MaxLifetime);
        Assert.Equal(70.0f, asset.MaxDistance);
        Assert.Equal(AssetId.None, asset.MovePointId);
        Assert.Equal(3, asset.SalvoCount);
        Assert.Equal(-1, asset.AmmoCount);
        Assert.Equal(-1.0f, asset.ArcCoefficient);
        Assert.Equal(70, asset.DebrisConeAngle);
        Assert.Equal(1, asset.BounceCount);
        Assert.Equal(PowerupType.Unknown4, asset.PowerupType);
        Assert.Equal(10.0f, asset.HeavyFactor);
        Assert.Equal(new Vector3(300.0f, 200.0f, 300.0f), asset.TumbleRotation);
        Assert.Equal(0.5f, asset.CollideDelay);
        Assert.Equal(5.0f, asset.AtRestPeriod);
        Assert.Equal(0u, asset.Mode);
    }

    [Fact]
    public void Read_LobMaster_LinkCountKeepsDerivingAfterLinksAreMutated()
    {
        byte[] data =
        [
            .. Prefix(1),
            .. Body(0, 0, Vector3.Zero, Vector3.Zero, 0, 0, Vector3.Zero, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, Vector3.Zero, 0, 0, 0),
            .. LinkBytes(0, 0, 0),
        ];

        var asset = (LobMasterAsset)Read(data, N100FSerializer.DefaultProfile);
        Assert.Equal(1, asset.Physical.LinkCount);

        asset.Links.Add(new Link());

        Assert.Equal(2, asset.Physical.LinkCount);
    }

    [Fact]
    public void Read_ThenWrite_LobMaster_ReproducesInputBytes()
    {
        byte[] data = SampleData();
        var profile = N100FSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_LobMasterWithLinks_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. Prefix(1),
            .. Body(
                0, 0x4974022F, new Vector3(1, 2, 3), new Vector3(0, 90.0f, 0), 10.0f, 20.0f, Vector3.One,
                0x3F, 10.0f, 70.0f, 0xAABBCCDD, 5, 12, -1.0f, 45, 2, 1, 10.0f,
                new Vector3(300.0f, 200.0f, 300.0f), 0.5f, 5.0f, 2),
            .. LinkBytes(1, 2, 0x11223344),
        ];
        var profile = N100FSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_LobMasterWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. SampleData(), 0xDE, 0xAD, 0xBE, 0xEF];
        var profile = N100FSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_LobMaster_UnderBFBB_DegradesToGenericAsset()
    {
        byte[] data = SampleData();

        var asset = Read(data, BFBBSerializer.DefaultProfile);

        Assert.IsNotType<LobMasterAsset>(asset);
        Assert.Equal(data[8..], asset.GetUnparsedTail().ToArray());
    }
}
