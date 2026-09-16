using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public sealed partial class MovePointAsset
{
    internal static MovePointAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new MovePointAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        asset.Position = reader.ReadVector3();
        asset.Weight = (ushort)reader.ReadInt16();
        asset.Kind = (MovePointKind)reader.ReadByte();
        asset.BezierRole = (MovePointBezierRole)reader.ReadByte();
        asset.Physical.FlagsProps = reader.ReadByte();
        reader.ReadByte(); // pad, always zero
        int numPoints = (ushort)reader.ReadInt16();
        asset.Delay = reader.ReadSingle();

        if (profile.Game is not GameVersion.N100F)
        {
            asset.ZoneRadius = reader.ReadSingle();
            asset.ArenaRadius = reader.ReadSingle();
        }

        for (int i = 0; i < numPoints; i++)
            asset.SiblingIds.Add(reader.ReadAssetId());

        LinkSerialization.Read(asset, reader, asset.Physical.LinkCount);
        asset.Physical.NumPoints = (ushort)asset.SiblingIds.Count;
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(MovePointAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);

        writer.Write(asset.Position);
        writer.Write((short)asset.Weight);
        writer.Write((byte)asset.Kind);
        writer.Write((byte)asset.BezierRole);
        writer.Write(asset.Physical.FlagsProps);
        writer.Write((byte)0); // pad
        writer.Write((short)asset.Physical.NumPoints);
        writer.Write(asset.Delay);

        if (profile.Game is not GameVersion.N100F)
        {
            writer.Write(asset.ZoneRadius);
            writer.Write(asset.ArenaRadius);
        }

        foreach (var siblingId in asset.SiblingIds)
            writer.Write(siblingId);

        LinkSerialization.Write(asset, writer);
        writer.Write(asset.GetUnparsedTail());
    }
}
