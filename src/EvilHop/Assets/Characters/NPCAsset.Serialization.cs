using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public sealed partial class NPCAsset
{
    internal static NPCAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new NPCAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);
        EntityAssetPrefix.Read(asset, reader, profile);

        // TODO: Partial implementation - N100F Prototype fields not yet parsed
        if (profile.NPCHasExtendedFields)
        {
            asset.ActivateRadius = reader.ReadSingle();
            asset.ActivateFOV = reader.ReadSingle();
            asset.DetectHeight = reader.ReadSingle();
            asset.DetectHeightOffset = reader.ReadSingle();
            asset.SpeedMovement = reader.ReadSingle();
            asset.SpeedPursue = reader.ReadSingle();
            asset.SpeedTurn = reader.ReadSingle();
            asset.PursuitRange = reader.ReadSingle();
            asset.DazedDuration = reader.ReadInt16();
            asset.GloatDuration = reader.ReadInt16();
            asset.GummedDuration = reader.ReadInt16();
            asset.BubbleDuration = reader.ReadInt16();
            asset.Hitpoints = reader.ReadByte();
            asset.BehaviorState = reader.ReadByte();
            reader.ReadInt16(); // pad, always zero
            asset.Physical.VillFlags = reader.ReadUInt32();
            asset.LobSpeed = reader.ReadSingle();
            asset.LobDurReload = reader.ReadSingle();
            asset.LobRange = reader.ReadSingle();
            asset.LobSalvo = reader.ReadUInt32();
            asset.ProjectileTypeId = reader.ReadAssetId();
            asset.BullseyeId = reader.ReadAssetId();
            asset.LobArcness = reader.ReadSingle();
            asset.LobHeavy = reader.ReadSingle();
            asset.ExtenderRange = reader.ReadSingle();
            asset.ExtenderWidth = reader.ReadSingle();
            asset.ExtenderDuration = reader.ReadSingle();
            asset.ExtenderRate = reader.ReadSingle();
            asset.ExtenderReloadTime = reader.ReadSingle();
            asset.MovePointId = reader.ReadAssetId();
            asset.Physical.PathAssetId = reader.ReadAssetId();
            asset.MinPlayerPowerups = reader.ReadInt32();
            asset.MinGameDifficulty = reader.ReadInt32();

            for (var i = 0; i < asset.Physical.LinkCount; i++)
                asset.Links.Add(Link.Read(reader, profile));
            asset.Physical.LinkCount = (byte)asset.Links.Count;
        }

        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(NPCAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);
        EntityAssetPrefix.Write(asset, writer, profile);

        if (profile.NPCHasExtendedFields)
        {
            writer.Write(asset.ActivateRadius);
            writer.Write(asset.ActivateFOV);
            writer.Write(asset.DetectHeight);
            writer.Write(asset.DetectHeightOffset);
            writer.Write(asset.SpeedMovement);
            writer.Write(asset.SpeedPursue);
            writer.Write(asset.SpeedTurn);
            writer.Write(asset.PursuitRange);
            writer.Write(asset.DazedDuration);
            writer.Write(asset.GloatDuration);
            writer.Write(asset.GummedDuration);
            writer.Write(asset.BubbleDuration);
            writer.Write(asset.Hitpoints);
            writer.Write(asset.BehaviorState);
            writer.Write((short)0); // pad
            writer.Write(asset.Physical.VillFlags);
            writer.Write(asset.LobSpeed);
            writer.Write(asset.LobDurReload);
            writer.Write(asset.LobRange);
            writer.Write(asset.LobSalvo);
            writer.Write(asset.ProjectileTypeId);
            writer.Write(asset.BullseyeId);
            writer.Write(asset.LobArcness);
            writer.Write(asset.LobHeavy);
            writer.Write(asset.ExtenderRange);
            writer.Write(asset.ExtenderWidth);
            writer.Write(asset.ExtenderDuration);
            writer.Write(asset.ExtenderRate);
            writer.Write(asset.ExtenderReloadTime);
            writer.Write(asset.MovePointId);
            writer.Write(asset.Physical.PathAssetId);
            writer.Write(asset.MinPlayerPowerups);
            writer.Write(asset.MinGameDifficulty);

            foreach (var link in asset.Links)
                Link.Write(link, writer, profile);
        }

        writer.Write(asset.GetUnparsedTail());
    }
}
