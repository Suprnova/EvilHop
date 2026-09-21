using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Tests.Serialization;

public class AttackTableAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new IncrediblesSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.AttackTable;
        header.Debug = debug;

        return (header, debug);
    }

    private static Asset Read(byte[] data, FormatProfile? profile = null)
    {
        profile ??= IncrediblesSerializer.DefaultProfile;
        var (header, debug) = HeaderFor();
        using var reader = new EndianReader(new MemoryStream(data), profile.Endianness);
        return AssetCodecs.Read(reader, header, debug, profile);
    }

    private static byte[] Write(Asset asset, FormatProfile? profile = null)
    {
        profile ??= IncrediblesSerializer.DefaultProfile;
        using var stream = new MemoryStream();
        using (var writer = new EndianWriter(stream, profile.Endianness, leaveOpen: true))
            AssetCodecs.Write(asset, writer, profile);
        return stream.ToArray();
    }

    private static byte[] U8(byte value) => [value];
    private static byte[] U16(ushort value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] S16(short value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] U32(uint value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] F32(float value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] V3(Vector3 v) => [.. F32(v.X), .. F32(v.Y), .. F32(v.Z)];

    private static byte[] TableHeader(int sectionCount, int entryCount, int transitionCount, int stateCount) =>
    [
        .. U16((ushort)sectionCount), .. U16((ushort)entryCount), .. U16((ushort)transitionCount), .. U16((ushort)stateCount),
    ];

    private static byte[] Section(uint sectionId, ushort start, ushort count) =>
    [
        .. U32(sectionId), .. U16(start), .. U16(count),
    ];

    private static byte[] Entry(uint animationStateId, ushort animationStart, ushort animationCount, ushort start,
        ushort count, ushort onFlags, ushort offFlags, byte input, byte power, float startTime) =>
    [
        .. U32(animationStateId), .. U32(0), // xAnimState pointer
        .. U16(animationStart), .. U16(animationCount), .. U16(start), .. U16(count),
        .. U16(onFlags), .. U16(offFlags),
        .. U8(input), .. U8(power), .. U8(0), .. U8(0), // pad0, pad1
        .. F32(startTime),
    ];

    private static byte[] Transition(uint sourceState, uint destinationState, float sourceTime, float throughTime,
        float destinationTime, float blendTime, uint flags) =>
    [
        .. U32(sourceState), .. U32(destinationState),
        .. F32(sourceTime), .. F32(throughTime), .. F32(destinationTime), .. F32(blendTime),
        .. U32(flags),
    ];

    private static byte[] HitBone(ushort bone, Vector3 offset, short atomic) =>
    [
        .. U16(bone), .. U16(0), // padding
        .. V3(offset),
        .. S16(atomic), .. U16(0), // padding
    ];

    private static byte[] EffectBone(ushort bone) =>
    [
        .. U16(bone), .. U16(0), // padding
        .. U32(0), // position cache
    ];

    private static byte[] State(
        uint stateId, float moveDistanceZ, float moveDistanceY, float moveTime,
        float attackStart, float attackEnd, float attackRadius,
        (ushort Bone, Vector3 Offset, short Atomic)[] hitBones,
        short damage, ushort source, ushort effect, ushort hitEffect, float effectStart, float effectEnd,
        ushort[] effectBonesOutside, ushort[] effectBonesInside,
        float rumbleStartTime, uint rumbleEmitterId, uint shrapnelId, float shrapnelStartTime,
        float velocityUp, float velocityAway, uint flags,
        float holdTime, float jumpBreakTime, float crouchBreakTime,
        float turnLockStart, float turnLockStop, float climaxTime, Vector3 climaxOffset,
        float drainRate, float blurStart, float blurEnd, float blurLife, float blurAlpha,
        float blurFadeInTime, float blurFadeOutTime, short flashAlpha, float flashTime,
        float comboBonus, short comboType, short powerBonus) =>
    [
        .. U32(stateId), .. F32(moveDistanceZ), .. F32(moveDistanceY), .. F32(moveTime),
        .. F32(attackStart), .. F32(attackEnd), .. F32(attackRadius),
        .. HitBone(hitBones[0].Bone, hitBones[0].Offset, hitBones[0].Atomic),
        .. HitBone(hitBones[1].Bone, hitBones[1].Offset, hitBones[1].Atomic),
        .. HitBone(hitBones[2].Bone, hitBones[2].Offset, hitBones[2].Atomic),
        .. HitBone(hitBones[3].Bone, hitBones[3].Offset, hitBones[3].Atomic),
        .. S16(damage), .. U16(source), .. U16(effect), .. U16(hitEffect),
        .. F32(effectStart), .. F32(effectEnd),
        .. EffectBone(effectBonesOutside[0]), .. EffectBone(effectBonesOutside[1]),
        .. EffectBone(effectBonesInside[0]), .. EffectBone(effectBonesInside[1]),
        .. U32(0), .. U32(0), // bonePositions[2]
        .. F32(rumbleStartTime), .. U32(rumbleEmitterId),
        .. U32(shrapnelId), .. U32(0), // zShrapnelAsset pointer
        .. F32(shrapnelStartTime),
        .. F32(velocityUp), .. F32(velocityAway), .. U32(flags),
        .. F32(holdTime), .. F32(jumpBreakTime), .. F32(crouchBreakTime),
        .. F32(turnLockStart), .. F32(turnLockStop), .. F32(climaxTime), .. V3(climaxOffset),
        .. F32(drainRate), .. F32(blurStart), .. F32(blurEnd), .. F32(blurLife), .. F32(blurAlpha),
        .. F32(blurFadeInTime), .. F32(blurFadeOutTime),
        .. S16(flashAlpha), .. U16(0), // padding
        .. F32(flashTime),
        .. F32(comboBonus), .. S16(comboType), .. S16(powerBonus),
    ];

    private static readonly (ushort Bone, Vector3 Offset, short Atomic)[] SampleHitBones =
    [
        (3, new Vector3(1.0f, 2.0f, 3.0f), 5),
        (0xFFFF, Vector3.Zero, 0),
        (0xFFFF, Vector3.Zero, 0),
        (0xFFFF, Vector3.Zero, 0),
    ];

    private static byte[] SampleTableData() =>
    [
        .. TableHeader(sectionCount: 1, entryCount: 1, transitionCount: 1, stateCount: 1),
        .. Section(sectionId: 0x8F5496C6, start: 23, count: 4),
        .. Entry(animationStateId: 0x4822BD09, animationStart: 1, animationCount: 2, start: 3, count: 4,
            onFlags: 0x10, offFlags: 0x20, input: 6, power: 30, startTime: 1.5f),
        .. Transition(sourceState: 0xE0D6C439, destinationState: 0x4822BC83, sourceTime: 0.25f, throughTime: 0.55f,
            destinationTime: 0.06f, blendTime: 0.15f, flags: 0),
        .. State(
            stateId: 0x01D6EF51, moveDistanceZ: 1.0f, moveDistanceY: 2.0f, moveTime: 0.5f,
            attackStart: -1.0f, attackEnd: 0.0f, attackRadius: 0.75f, hitBones: SampleHitBones,
            damage: 30, source: 12, effect: 0, hitEffect: 3, effectStart: 0.1f, effectEnd: 0.9f,
            effectBonesOutside: [0xFFFF, 0xFFFF], effectBonesInside: [0xFFFF, 0xFFFF],
            rumbleStartTime: 0.2f, rumbleEmitterId: 0, shrapnelId: 0xCAFEF00D, shrapnelStartTime: 0.3f,
            velocityUp: 6.0f, velocityAway: 7.5f, flags: 0x1,
            holdTime: 0.1f, jumpBreakTime: 10000000.0f, crouchBreakTime: 10000000.0f,
            turnLockStart: 0.0f, turnLockStop: 0.0f, climaxTime: 0.0f, climaxOffset: Vector3.Zero,
            drainRate: 15.0f, blurStart: 0.0f, blurEnd: -1.0f, blurLife: 0.0f, blurAlpha: 0.0f,
            blurFadeInTime: 0.0f, blurFadeOutTime: 0.0f, flashAlpha: 0, flashTime: 0.0f,
            comboBonus: 0.0f, comboType: 10, powerBonus: 256),
    ];

    [Fact]
    public void Read_AttackTable_ProducesAttackTableAsset() =>
        Assert.IsType<AttackTableAsset>(Read(SampleTableData()));

    [Fact]
    public void Read_AttackTable_PopulatesSections()
    {
        var asset = (AttackTableAsset)Read(SampleTableData());

        var section = Assert.Single(asset.Sections);
        Assert.Equal(0x8F5496C6u, section.SectionId);
        Assert.Equal(23, section.Start);
        Assert.Equal(4, section.Count);
    }

    [Fact]
    public void Read_AttackTable_PopulatesEntries()
    {
        var asset = (AttackTableAsset)Read(SampleTableData());

        var entry = Assert.Single(asset.Entries);
        Assert.Equal(0x4822BD09u, entry.AnimationStateId);
        Assert.Equal(1, entry.AnimationStart);
        Assert.Equal(2, entry.AnimationCount);
        Assert.Equal(3, entry.Start);
        Assert.Equal(4, entry.Count);
        Assert.Equal(0x10, entry.OnFlags);
        Assert.Equal(0x20, entry.OffFlags);
        Assert.Equal(6, entry.Input);
        Assert.Equal(30, entry.Power);
        Assert.Equal(1.5f, entry.StartTime);
    }

    [Fact]
    public void Read_AttackTable_PopulatesTransitions()
    {
        var asset = (AttackTableAsset)Read(SampleTableData());

        var transition = Assert.Single(asset.Transitions);
        Assert.Equal(0xE0D6C439u, transition.SourceState);
        Assert.Equal(0x4822BC83u, transition.DestinationState);
        Assert.Equal(0.25f, transition.SourceTime);
        Assert.Equal(0.55f, transition.ThroughTime);
        Assert.Equal(0.06f, transition.DestinationTime);
        Assert.Equal(0.15f, transition.BlendTime);
        Assert.Equal(0u, transition.Flags);
    }

    [Fact]
    public void Read_AttackTable_PopulatesStates()
    {
        var asset = (AttackTableAsset)Read(SampleTableData());

        var state = Assert.Single(asset.States);
        Assert.Equal(0x01D6EF51u, state.StateId);
        Assert.Equal(1.0f, state.MoveDistanceZ);
        Assert.Equal(2.0f, state.MoveDistanceY);
        Assert.Equal(-1.0f, state.AttackStart);
        Assert.Equal(0.75f, state.AttackRadius);

        Assert.Equal(4, state.HitBones.Length);
        Assert.Equal(3, state.HitBones[0].Bone);
        Assert.Equal(new Vector3(1.0f, 2.0f, 3.0f), state.HitBones[0].Offset);
        Assert.Equal(5, state.HitBones[0].Atomic);
        Assert.Equal(0xFFFF, state.HitBones[1].Bone);

        Assert.Equal(30, state.Damage);
        Assert.Equal(12, state.Source);
        Assert.Equal(3, state.HitEffect);

        Assert.Equal(2, state.EffectBonesOutside.Length);
        Assert.Equal(0xFFFF, state.EffectBonesOutside[0]);
        Assert.Equal(2, state.EffectBonesInside.Length);

        Assert.Equal(new AssetId(0xCAFEF00D), state.ShrapnelId);
        Assert.Equal(0.3f, state.ShrapnelStartTime);
        Assert.Equal(6.0f, state.VelocityUp);
        Assert.Equal(7.5f, state.VelocityAway);
        Assert.Equal(0x1u, state.Flags);
        Assert.Equal(15.0f, state.DrainRate);
        Assert.Equal(10, state.ComboType);
        Assert.Equal(256, state.PowerBonus);
    }

    [Fact]
    public void Read_ThenWrite_AttackTable_ReproducesInputBytes()
    {
        byte[] data = SampleTableData();

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_AttackTableWithNoEntries_ReproducesInputBytes()
    {
        byte[] data = TableHeader(sectionCount: 0, entryCount: 0, transitionCount: 0, stateCount: 0);

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_AttackTableWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. SampleTableData(), 0xDE, 0xAD, 0xBE, 0xEF];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_AttackTable_UnderBFBB_DegradesToGenericAsset()
    {
        byte[] data = SampleTableData();

        var asset = Read(data, BFBBSerializer.DefaultProfile);

        Assert.IsNotType<AttackTableAsset>(asset);
        Assert.Equal(data, asset.GetUnparsedTail().ToArray());
    }

    [Fact]
    public void EntryCount_DisagreeingWithEntries_IsStoredIndependently()
    {
        var asset = new AttackTableAsset();
        asset.Entries.Add(new AttackTableEntry());

        asset.Physical.EntryCount = 5;

        Assert.Equal(5, asset.Physical.EntryCount);
        Assert.Single(asset.Entries);
    }

    [Fact]
    public void EntryCount_MatchingEntries_DerivesFromEntries()
    {
        var asset = new AttackTableAsset();
        asset.Entries.Add(new AttackTableEntry());
        asset.Entries.Add(new AttackTableEntry());

        asset.Physical.EntryCount = 2;
        asset.Entries.Add(new AttackTableEntry());

        Assert.Equal(3, asset.Physical.EntryCount);
    }

    [Fact]
    public void HitBones_AssignedWithWrongLength_Throws()
    {
        var state = new AttackTableState();

        Assert.Throws<ArgumentException>(() => state.HitBones = [new HitBoneInfo()]);
    }
}
