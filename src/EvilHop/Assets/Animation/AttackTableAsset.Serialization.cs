using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public sealed partial class AttackTableAsset
{
    internal static AttackTableAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile _)
    {
        var asset = new AttackTableAsset();
        AssetFields.Populate(asset, header, debug);

        int sectionCount = (ushort)reader.ReadInt16();
        int entryCount = (ushort)reader.ReadInt16();
        int transitionCount = (ushort)reader.ReadInt16();
        int stateCount = (ushort)reader.ReadInt16();

        for (int i = 0; i < sectionCount; i++)
        {
            asset.Sections.Add(new AttackTableSection
            {
                SectionId = reader.ReadUInt32(),
                Start = (ushort)reader.ReadInt16(),
                Count = (ushort)reader.ReadInt16(),
            });
        }

        for (int i = 0; i < entryCount; i++)
        {
            var entry = new AttackTableEntry
            {
                AnimationStateId = reader.ReadUInt32(),
            };
            reader.ReadUInt32(); // runtime-resolved xAnimState pointer, always zero on disk
            entry.AnimationStart = (ushort)reader.ReadInt16();
            entry.AnimationCount = (ushort)reader.ReadInt16();
            entry.Start = (ushort)reader.ReadInt16();
            entry.Count = (ushort)reader.ReadInt16();
            entry.OnFlags = (ushort)reader.ReadInt16();
            entry.OffFlags = (ushort)reader.ReadInt16();
            entry.Input = reader.ReadByte();
            entry.Power = reader.ReadByte();
            reader.ReadUInt16(); // padding, always zero
            entry.StartTime = reader.ReadSingle();
            asset.Entries.Add(entry);
        }

        for (int i = 0; i < transitionCount; i++)
        {
            asset.Transitions.Add(new AttackTableTransition
            {
                SourceState = reader.ReadUInt32(),
                DestinationState = reader.ReadUInt32(),
                SourceTime = reader.ReadSingle(),
                ThroughTime = reader.ReadSingle(),
                DestinationTime = reader.ReadSingle(),
                BlendTime = reader.ReadSingle(),
                Flags = reader.ReadUInt32(),
            });
        }

        for (int i = 0; i < stateCount; i++) asset.States.Add(ReadState(reader));

        asset.Physical.SectionCount = (ushort)asset.Sections.Count;
        asset.Physical.EntryCount = (ushort)asset.Entries.Count;
        asset.Physical.TransitionCount = (ushort)asset.Transitions.Count;
        asset.Physical.StateCount = (ushort)asset.States.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    private static AttackTableState ReadState(EndianReader reader)
    {
        var state = new AttackTableState
        {
            StateId = reader.ReadUInt32(),
            MoveDistanceZ = reader.ReadSingle(),
            MoveDistanceY = reader.ReadSingle(),
            MoveTime = reader.ReadSingle(),
            AttackStart = reader.ReadSingle(),
            AttackEnd = reader.ReadSingle(),
            AttackRadius = reader.ReadSingle(),
            HitBones =
            [
                ReadHitBone(reader),
                ReadHitBone(reader),
                ReadHitBone(reader),
                ReadHitBone(reader),
            ],
            Damage = reader.ReadInt16(),
            Source = (ushort)reader.ReadInt16(),
            Effect = (ushort)reader.ReadInt16(),
            HitEffect = (ushort)reader.ReadInt16(),
            EffectStart = reader.ReadSingle(),
            EffectEnd = reader.ReadSingle(),
            EffectBonesOutside = [ReadEffectBone(reader), ReadEffectBone(reader)],
            EffectBonesInside = [ReadEffectBone(reader), ReadEffectBone(reader)]
        };
        reader.ReadUInt32(); // bonePositions[0], runtime-resolved zAnimCacheEntry* cache, always zero
        reader.ReadUInt32(); // bonePositions[1], runtime-resolved zAnimCacheEntry* cache, always zero
        state.RumbleStartTime = reader.ReadSingle();
        state.RumbleEmitterId = reader.ReadUInt32();
        state.ShrapnelId = reader.ReadAssetId();
        reader.ReadUInt32(); // runtime-resolved zShrapnelAsset pointer, always zero on disk
        state.ShrapnelStartTime = reader.ReadSingle();
        state.VelocityUp = reader.ReadSingle();
        state.VelocityAway = reader.ReadSingle();
        state.Flags = reader.ReadUInt32();
        state.HoldTime = reader.ReadSingle();
        state.JumpBreakTime = reader.ReadSingle();
        state.CrouchBreakTime = reader.ReadSingle();
        state.TurnLockStart = reader.ReadSingle();
        state.TurnLockStop = reader.ReadSingle();
        state.ClimaxTime = reader.ReadSingle();
        state.ClimaxOffset = reader.ReadVector3();
        state.DrainRate = reader.ReadSingle();
        state.BlurStart = reader.ReadSingle();
        state.BlurEnd = reader.ReadSingle();
        state.BlurLife = reader.ReadSingle();
        state.BlurAlpha = reader.ReadSingle();
        state.BlurFadeInTime = reader.ReadSingle();
        state.BlurFadeOutTime = reader.ReadSingle();
        state.FlashAlpha = reader.ReadInt16();
        reader.ReadInt16(); // padding, always zero
        state.FlashTime = reader.ReadSingle();
        state.ComboBonus = reader.ReadSingle();
        state.ComboType = reader.ReadInt16();
        state.PowerBonus = reader.ReadInt16();

        return state;
    }

    private static HitBoneInfo ReadHitBone(EndianReader reader)
    {
        var hitBone = new HitBoneInfo { Bone = (ushort)reader.ReadInt16() };
        reader.ReadInt16(); // padding, always zero
        hitBone.Offset = reader.ReadVector3();
        hitBone.Atomic = reader.ReadInt16();
        reader.ReadInt16(); // padding, always zero
        return hitBone;
    }

    private static ushort ReadEffectBone(EndianReader reader)
    {
        ushort bone = (ushort)reader.ReadInt16();
        reader.ReadInt16(); // padding, always zero
        reader.ReadUInt32(); // runtime-resolved xVec3* position cache, always zero on disk
        return bone;
    }

    internal static void Write(AttackTableAsset asset, EndianWriter writer, FormatProfile _)
    {
        writer.Write((short)asset.Physical.SectionCount);
        writer.Write((short)asset.Physical.EntryCount);
        writer.Write((short)asset.Physical.TransitionCount);
        writer.Write((short)asset.Physical.StateCount);

        foreach (var section in asset.Sections)
        {
            writer.Write(section.SectionId);
            writer.Write((short)section.Start);
            writer.Write((short)section.Count);
        }

        foreach (var entry in asset.Entries)
        {
            writer.Write(entry.AnimationStateId);
            writer.Write(0u); // xAnimState pointer
            writer.Write((short)entry.AnimationStart);
            writer.Write((short)entry.AnimationCount);
            writer.Write((short)entry.Start);
            writer.Write((short)entry.Count);
            writer.Write((short)entry.OnFlags);
            writer.Write((short)entry.OffFlags);
            writer.Write(entry.Input);
            writer.Write(entry.Power);
            writer.Write((short)0); // padding
            writer.Write(entry.StartTime);
        }

        foreach (var transition in asset.Transitions)
        {
            writer.Write(transition.SourceState);
            writer.Write(transition.DestinationState);
            writer.Write(transition.SourceTime);
            writer.Write(transition.ThroughTime);
            writer.Write(transition.DestinationTime);
            writer.Write(transition.BlendTime);
            writer.Write(transition.Flags);
        }

        foreach (var state in asset.States) WriteState(writer, state);

        writer.Write(asset.GetUnparsedTail());
    }

    private static void WriteState(EndianWriter writer, AttackTableState state)
    {
        writer.Write(state.StateId);
        writer.Write(state.MoveDistanceZ);
        writer.Write(state.MoveDistanceY);
        writer.Write(state.MoveTime);
        writer.Write(state.AttackStart);
        writer.Write(state.AttackEnd);
        writer.Write(state.AttackRadius);
        foreach (var hitBone in state.HitBones) WriteHitBone(writer, hitBone);

        writer.Write(state.Damage);
        writer.Write((short)state.Source);
        writer.Write((short)state.Effect);
        writer.Write((short)state.HitEffect);
        writer.Write(state.EffectStart);
        writer.Write(state.EffectEnd);
        foreach (var bone in state.EffectBonesOutside) WriteEffectBone(writer, bone);
        foreach (var bone in state.EffectBonesInside) WriteEffectBone(writer, bone);
        writer.Write(0u); // bonePositions[0]
        writer.Write(0u); // bonePositions[1]
        writer.Write(state.RumbleStartTime);
        writer.Write(state.RumbleEmitterId);
        writer.Write(state.ShrapnelId);
        writer.Write(0u); // zShrapnelAsset pointer
        writer.Write(state.ShrapnelStartTime);
        writer.Write(state.VelocityUp);
        writer.Write(state.VelocityAway);
        writer.Write(state.Flags);
        writer.Write(state.HoldTime);
        writer.Write(state.JumpBreakTime);
        writer.Write(state.CrouchBreakTime);
        writer.Write(state.TurnLockStart);
        writer.Write(state.TurnLockStop);
        writer.Write(state.ClimaxTime);
        writer.Write(state.ClimaxOffset);
        writer.Write(state.DrainRate);
        writer.Write(state.BlurStart);
        writer.Write(state.BlurEnd);
        writer.Write(state.BlurLife);
        writer.Write(state.BlurAlpha);
        writer.Write(state.BlurFadeInTime);
        writer.Write(state.BlurFadeOutTime);
        writer.Write(state.FlashAlpha);
        writer.Write((short)0); // padding
        writer.Write(state.FlashTime);
        writer.Write(state.ComboBonus);
        writer.Write(state.ComboType);
        writer.Write(state.PowerBonus);
    }

    private static void WriteHitBone(EndianWriter writer, HitBoneInfo hitBone)
    {
        writer.Write((short)hitBone.Bone);
        writer.Write((short)0); // padding
        writer.Write(hitBone.Offset);
        writer.Write(hitBone.Atomic);
        writer.Write((short)0); // padding
    }

    private static void WriteEffectBone(EndianWriter writer, ushort bone)
    {
        writer.Write((short)bone);
        writer.Write((short)0); // padding
        writer.Write(0u); // xVec3* position cache
    }
}
