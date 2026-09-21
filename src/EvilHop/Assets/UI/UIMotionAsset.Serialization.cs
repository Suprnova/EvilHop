using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public sealed partial class UIMotionAsset
{
    internal static UIMotionAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new UIMotionAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        asset.Physical.CommandCount = reader.ReadByte();
        asset.Physical.InFlag = reader.ReadByte();
        reader.ReadBytes(2); // padding, always zero
        asset.Physical.CommandsSize = reader.ReadUInt32();
        asset.TotalTime = reader.ReadSingle();
        asset.LoopTime = reader.ReadSingle();

        byte commandCount = asset.Physical.CommandCount;
        for (int i = 0; i < commandCount; i++)
            asset.Commands.Add(UIMotionCommand.Read(reader, profile));

        asset.Physical.CommandCount = (byte)asset.Commands.Count;
        asset.Physical.CommandsSize = asset.ComputedCommandsSize;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(UIMotionAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);

        writer.Write(asset.Physical.CommandCount);
        writer.Write(asset.Physical.InFlag);
        writer.Write(new byte[2]); // padding
        writer.Write(asset.Physical.CommandsSize);
        writer.Write(asset.TotalTime);
        writer.Write(asset.LoopTime);

        foreach (var command in asset.Commands)
            UIMotionCommand.Write(command, writer, profile);

        writer.Write(asset.GetUnparsedTail());
    }
}
