using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public sealed partial class UIMotionAsset
{
    internal static UIMotionAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile _)
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
            asset.Commands.Add(ReadCommand(reader));

        asset.Physical.CommandCount = (byte)asset.Commands.Count;
        asset.Physical.CommandsSize = asset.ComputedCommandsSize;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(UIMotionAsset asset, EndianWriter writer, FormatProfile _)
    {
        BaseAssetPrefix.Write(asset, writer);

        writer.Write(asset.Physical.CommandCount);
        writer.Write(asset.Physical.InFlag);
        writer.Write(new byte[2]); // padding
        writer.Write(asset.Physical.CommandsSize);
        writer.Write(asset.TotalTime);
        writer.Write(asset.LoopTime);

        foreach (var command in asset.Commands)
            WriteCommand(command, writer);

        writer.Write(asset.GetUnparsedTail());
    }

    /// <exception cref="InvalidDataException">The stored command type is not a known <see cref="UIMotionCommandType"/>.</exception>
    private static UIMotionCommand ReadCommand(EndianReader reader)
    {
        var type = (UIMotionCommandType)reader.ReadUInt32();
        float startTime = reader.ReadSingle();
        float endTime = reader.ReadSingle();
        float accelTime = reader.ReadSingle();
        float decelTime = reader.ReadSingle();
        bool enabled = reader.ReadByte() != 0;
        reader.ReadBytes(3); // padding, always zero

        UIMotionCommand command = type switch
        {
            UIMotionCommandType.Move => new MoveCommand(),
            UIMotionCommandType.Scale => new ScaleCommand(),
            UIMotionCommandType.Rotate => new RotateCommand(),
            UIMotionCommandType.Opacity => new OpacityCommand(),
            UIMotionCommandType.AbsoluteScale => new AbsoluteScaleCommand(),
            UIMotionCommandType.Brightness => new BrightnessCommand(),
            UIMotionCommandType.Color => new ColorCommand(),
            UIMotionCommandType.UVScroll => new UVScrollCommand(),
            _ => throw new InvalidDataException($"Unknown UI Motion command type 0x{(uint)type:X8}."),
        };

        command.StartTime = startTime;
        command.EndTime = endTime;
        command.AccelTime = accelTime;
        command.DecelTime = decelTime;
        command.Enabled = enabled;
        command.ReadFields(reader);
        return command;
    }

    private static void WriteCommand(UIMotionCommand command, EndianWriter writer)
    {
        writer.Write((uint)command.Type);
        writer.Write(command.StartTime);
        writer.Write(command.EndTime);
        writer.Write(command.AccelTime);
        writer.Write(command.DecelTime);
        writer.Write((byte)(command.Enabled ? 1 : 0));
        writer.Write(new byte[3]); // padding
        command.WriteFields(writer);
    }
}
