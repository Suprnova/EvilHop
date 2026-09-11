using EvilHop.Common;
using EvilHop.Primitives;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="Motion"/> only a <see cref="PlatformAsset"/> can have, stored in its type-specific
/// block rather than its Motion block.
/// </summary>
public abstract class PlatformMotion : Motion
{
    private protected PlatformMotion() { }

    /// <summary>
    /// Reads this motion's fields from a reader scoped to its type-specific block.
    /// </summary>
    private protected abstract void ReadFields(EndianReader reader, GameVersion game);

    /// <summary>
    /// Writes this motion's fields, no more than its type-specific block holds.
    /// </summary>
    private protected abstract void WriteFields(EndianWriter writer, GameVersion game);

    /// <summary>
    /// The size, in bytes, of a platform's type-specific block under <paramref name="game"/>.
    /// </summary>
    internal static int BlockSize(GameVersion game) => game is GameVersion.N100F ? 0x24 : 0x38;

    /// <summary>
    /// Reads the type-specific block <paramref name="type"/> selects.
    /// </summary>
    /// <exception cref="InvalidDataException"><paramref name="type"/> is unknown, or has no layout under <paramref name="game"/>.</exception>
    internal static PlatformMotion Read(EndianReader reader, PlatformType type, GameVersion game)
    {
        using var block = ReadBlock(reader, BlockSize(game));
        PlatformMotion motion = type switch
        {
            PlatformType.ConveyorBelt => new ConveyorBeltMotion(),
            PlatformType.Falling => new FallingMotion(),
            PlatformType.ForwardReturn => new ForwardReturnMotion(),
            PlatformType.Breakaway => new BreakawayMotion(),
            PlatformType.Springboard => new SpringboardMotion(),
            PlatformType.TeeterTotter => new TeeterTotterMotion(),
            PlatformType.Paddle => new PaddleMotion(),
            PlatformType.FullyManipulable => new FullyManipulableMotion(),
            _ => throw new InvalidDataException($"Unknown platform type 0x{(byte)type:X2}."),
        };

        motion.ReadFields(block, game);
        return motion;
    }

    /// <summary>
    /// Writes this motion as one type-specific block.
    /// </summary>
    internal void Write(EndianWriter writer, GameVersion game) =>
        WriteBlock(writer, BlockSize(game), block => WriteFields(block, game));

    /// <summary>
    /// Reads the empty type-specific block stored alongside an <see cref="EntityMotion"/>.
    /// </summary>
    internal static void ReadEmpty(EndianReader reader, GameVersion game) =>
        reader.ReadBytes(BlockSize(game)); // always zero

    /// <summary>
    /// Writes the empty type-specific block stored alongside an <see cref="EntityMotion"/>.
    /// </summary>
    internal static void WriteEmpty(EndianWriter writer, GameVersion game) =>
        writer.Write(new byte[BlockSize(game)]);
}
