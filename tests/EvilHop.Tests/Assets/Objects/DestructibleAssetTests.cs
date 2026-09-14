using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class DestructibleAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new N100FSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.DestructibleAsset;
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

    private static byte[] U32(uint value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] F32(float value) => [.. BitConverter.GetBytes(value).Reverse()];

    private static byte[] ShortHeader(
        uint modelInfoId, uint stateCount, uint hitPoints, uint hitFilter, uint launchFlag,
        uint behaviour, uint flags, uint idleSoundGroupId, float respawn, byte targetPriority,
        byte[]? padding = null) =>
    [
        .. U32(modelInfoId),
        .. U32(stateCount),
        .. U32(hitPoints),
        .. U32(hitFilter),
        .. U32(launchFlag),
        .. U32(behaviour),
        .. U32(flags),
        .. U32(idleSoundGroupId),
        .. F32(respawn),
        targetPriority,
        .. padding ?? new byte[3],
    ];

    private static byte[] ExtendedHeader(
        uint modelInfoId, uint stateCount, uint hitPoints, uint hitFilter, uint excludedHitFilter,
        uint healthPoints, uint experiencePoints, float healthChance, float experienceChance,
        uint launchFlag, uint behaviour, uint flags, uint idleSoundGroupId, float respawn,
        byte targetPriority, byte[]? padding = null) =>
    [
        .. U32(modelInfoId),
        .. U32(stateCount),
        .. U32(hitPoints),
        .. U32(hitFilter),
        .. U32(excludedHitFilter),
        .. U32(healthPoints),
        .. U32(experiencePoints),
        .. F32(healthChance),
        .. F32(experienceChance),
        .. U32(launchFlag),
        .. U32(behaviour),
        .. U32(flags),
        .. U32(idleSoundGroupId),
        .. F32(respawn),
        targetPriority,
        .. padding ?? new byte[3],
    ];

    private static byte[] StateBytes(
        uint percent, uint modelId, uint shrapnelId, uint hitShrapnelId, uint idleSoundGroupId,
        uint fxSoundGroupId, uint hitSoundGroupId, uint switchFxSoundGroupId, uint switchHitSoundGroupId,
        uint hitRumbleId, uint switchRumbleId, uint fxFlags, params uint[] animationIds) =>
    [
        .. U32(percent),
        .. U32(modelId),
        .. U32(shrapnelId),
        .. U32(hitShrapnelId),
        .. U32(idleSoundGroupId),
        .. U32(fxSoundGroupId),
        .. U32(hitSoundGroupId),
        .. U32(switchFxSoundGroupId),
        .. U32(switchHitSoundGroupId),
        .. U32(hitRumbleId),
        .. U32(switchRumbleId),
        .. U32(fxFlags),
        .. U32((uint)animationIds.Length),
        .. animationIds.SelectMany(U32),
    ];

    private static byte[] OneState() => StateBytes(
        percent: 75, modelId: 0x11111111, shrapnelId: 0x22222222, hitShrapnelId: 0x33333333,
        idleSoundGroupId: 0x44444444, fxSoundGroupId: 0x55555555, hitSoundGroupId: 0x66666666,
        switchFxSoundGroupId: 0x77777777, switchHitSoundGroupId: 0x88888888, hitRumbleId: 0x99999999,
        switchRumbleId: 0xAAAAAAAA, fxFlags: 0xBBBBBBBB, animationIds: 0xCCCCCCCC);

    private static byte[] TssmData() =>
    [
        .. ShortHeader(0xDEADBEEF, 1, 50, 2, 0, 4, 0, 0x12345678, 3.5f, 7),
        .. OneState(),
    ];

    private static byte[] RotuData() =>
    [
        .. ExtendedHeader(0xDEADBEEF, 1, 50, 2, 6, 10, 20, 25.0f, 50.0f, 0, 4, 0, 0x12345678, 3.5f, 7),
        .. OneState(),
    ];

    [Fact]
    public void Read_Destructible_ProducesDestructibleAsset() =>
        Assert.IsType<DestructibleAsset>(Read(TssmData(), TSSMSerializer.DefaultProfile));

    [Fact]
    public void Read_Destructible_UnderTSSM_PopulatesEveryField()
    {
        var asset = (DestructibleAsset)Read(TssmData(), TSSMSerializer.DefaultProfile);

        Assert.Equal(new AssetId(0xDEADBEEF), asset.ModelInfoId);
        Assert.Equal(50u, asset.HitPoints);
        Assert.Equal(2u, asset.Physical.HitFilter);
        Assert.Equal(0u, asset.Physical.LaunchFlag);
        Assert.Equal(4u, asset.Physical.Behaviour);
        Assert.Equal(0u, asset.Physical.DestructibleFlags);
        Assert.Equal(new AssetId(0x12345678), asset.IdleSoundGroupId);
        Assert.Equal(3.5f, asset.RespawnTime);
        Assert.Equal(7, asset.TargetPriority);
        Assert.Equal(0u, asset.Physical.ExcludedHitFilter);
        Assert.Equal(0u, asset.HealthPoints);

        Assert.Single(asset.States);
        var state = asset.States[0];
        Assert.Equal(75u, state.Percent);
        Assert.Equal(new AssetId(0x11111111), state.ModelId);
        Assert.Equal(new AssetId(0x22222222), state.ShrapnelId);
        Assert.Equal(new AssetId(0x33333333), state.HitShrapnelId);
        Assert.Equal(new AssetId(0x44444444), state.IdleSoundGroupId);
        Assert.Equal(new AssetId(0x55555555), state.FxSoundGroupId);
        Assert.Equal(new AssetId(0x66666666), state.HitSoundGroupId);
        Assert.Equal(new AssetId(0x77777777), state.SwitchFxSoundGroupId);
        Assert.Equal(new AssetId(0x88888888), state.SwitchHitSoundGroupId);
        Assert.Equal(new AssetId(0x99999999), state.HitRumbleId);
        Assert.Equal(new AssetId(0xAAAAAAAA), state.SwitchRumbleId);
        Assert.Equal(0xBBBBBBBBu, state.FxFlags);
        Assert.Equal([new AssetId(0xCCCCCCCC)], state.AnimationIds);
    }

    [Fact]
    public void Read_Destructible_UnderROTU_PopulatesExtendedFields()
    {
        var asset = (DestructibleAsset)Read(RotuData(), ROTUSerializer.DefaultProfile);

        Assert.Equal(6u, asset.Physical.ExcludedHitFilter);
        Assert.Equal(10u, asset.HealthPoints);
        Assert.Equal(20u, asset.ExperiencePoints);
        Assert.Equal(25.0f, asset.HealthChance);
        Assert.Equal(50.0f, asset.ExperienceChance);
    }

    [Fact]
    public void Read_ThenWrite_DestructibleUnderTSSM_ReproducesInputBytes()
    {
        byte[] data = TssmData();
        var profile = TSSMSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_DestructibleUnderIncredibles_ReproducesInputBytes()
    {
        byte[] data = TssmData();
        var profile = IncrediblesSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_DestructibleUnderROTU_ReproducesInputBytes()
    {
        byte[] data = RotuData();
        var profile = ROTUSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_DestructibleUnderRatatouille_ReproducesInputBytes()
    {
        byte[] data = RotuData();
        var profile = RatatouilleSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_DestructibleWithMultipleStates_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. ShortHeader(0xDEADBEEF, 2, 50, 2, 0, 4, 0, 0x12345678, 3.5f, 7),
            .. StateBytes(50, 0x11111111, 0x22222222, 0x33333333, 0x44444444, 0x55555555, 0x66666666, 0x77777777, 0x88888888, 0x99999999, 0xAAAAAAAA, 0xBBBBBBBB),
            .. StateBytes(90, 0x11111112, 0x22222223, 0x33333334, 0x44444445, 0x55555556, 0x66666667, 0x77777778, 0x88888889, 0x9999999A, 0xAAAAAAAB, 0xBBBBBBBC, 0xC0000001, 0xC0000002),
        ];
        var profile = TSSMSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_DestructibleWithNoStates_ReproducesInputBytes()
    {
        byte[] data = ShortHeader(0xDEADBEEF, 0, 50, 2, 0, 4, 0, 0x12345678, 3.5f, 7);
        var profile = TSSMSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_DestructibleWithNonZeroPadding_ReproducesInputBytes()
    {
        byte[] data = ShortHeader(0xDEADBEEF, 0, 50, 2, 0, 4, 0, 0x12345678, 3.5f, 7, padding: [0x00, 0x00, 0x64]);
        var profile = TSSMSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_DestructibleWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. TssmData(), 0xFD, 0xFD, 0xFD, 0xFD];
        var profile = TSSMSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_Destructible_UnderBFBB_DegradesToGenericAsset()
    {
        byte[] data = TssmData();

        var asset = Read(data, BFBBSerializer.DefaultProfile);

        Assert.IsNotType<DestructibleAsset>(asset);
        Assert.Equal(data, asset.GetUnparsedTail().ToArray());
    }

    [Fact]
    public void UnknownPhysicalFields_SetThroughPhysical_AreStoredIndependently()
    {
        var asset = new DestructibleAsset();

        asset.Physical.HitFilter = 2;
        asset.Physical.ExcludedHitFilter = 6;
        asset.Physical.LaunchFlag = 1;
        asset.Physical.Behaviour = 11;
        asset.Physical.DestructibleFlags = 0xC0DE;

        Assert.Equal(2u, asset.Physical.HitFilter);
        Assert.Equal(6u, asset.Physical.ExcludedHitFilter);
        Assert.Equal(1u, asset.Physical.LaunchFlag);
        Assert.Equal(11u, asset.Physical.Behaviour);
        Assert.Equal(0xC0DEu, asset.Physical.DestructibleFlags);
    }

    [Fact]
    public void StateCount_DisagreeingWithStates_IsStoredIndependently()
    {
        var asset = new DestructibleAsset();
        asset.States.Add(new DestructibleAssetState());

        asset.Physical.StateCount = 5;

        Assert.Equal(5u, asset.Physical.StateCount);
        Assert.Single(asset.States);
    }

    [Fact]
    public void StateCount_MatchingStates_DerivesFromStates()
    {
        var asset = new DestructibleAsset();
        asset.States.Add(new DestructibleAssetState());
        asset.States.Add(new DestructibleAssetState());

        asset.Physical.StateCount = 2;
        asset.States.Add(new DestructibleAssetState());

        Assert.Equal(3u, asset.Physical.StateCount);
    }
}
