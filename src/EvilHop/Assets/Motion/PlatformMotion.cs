using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="Motion"/> only a <see cref="PlatformAsset"/> can have, stored in its type-specific
/// block rather than its Motion block.
/// </summary>
public abstract class PlatformMotion : Motion
{
    private protected PlatformMotion() { }

    /// <summary>
    /// The size, in bytes, of a platform's type-specific block under <paramref name="game"/>.
    /// </summary>
    internal static int BlockSize(GameVersion game) => game is GameVersion.N100F ? 0x24 : 0x38;

    /// <summary>
    /// Reads the type-specific block <paramref name="discriminator"/> selects.
    /// </summary>
    /// <exception cref="InvalidDataException"><paramref name="discriminator"/> is unknown, or has no layout under this <see cref="FormatProfile"/>.</exception>
    internal static PlatformMotion Read(EndianReader reader, PlatformType discriminator, FormatProfile profile)
    {
        using var block = ReadBlock(reader, BlockSize(profile.Game));
        return discriminator switch
        {
            PlatformType.ConveyorBelt => ConveyorBeltMotion.Read(block, profile),
            PlatformType.Falling => FallingMotion.Read(block, profile),
            PlatformType.ForwardReturn => ForwardReturnMotion.Read(block, profile),
            PlatformType.Breakaway => BreakawayMotion.Read(block, profile),
            PlatformType.Springboard => SpringboardMotion.Read(block, profile),
            PlatformType.TeeterTotter => TeeterTotterMotion.Read(block, profile),
            PlatformType.Paddle => PaddleMotion.Read(block, profile),
            PlatformType.FullyManipulable => FullyManipulableMotion.Read(block, profile),
            _ => throw new InvalidDataException($"Unknown platform type 0x{(byte)discriminator:X2}."),
        };
    }

    /// <summary>
    /// Writes this motion as one type-specific block.
    /// </summary>
    internal static void Write(PlatformMotion value, EndianWriter writer, FormatProfile profile) =>
        WriteBlock(writer, BlockSize(profile.Game), block =>
        {
            switch (value)
            {
                case ConveyorBeltMotion m: ConveyorBeltMotion.Write(m, block, profile); break;
                case FallingMotion m: FallingMotion.Write(m, block, profile); break;
                case ForwardReturnMotion m: ForwardReturnMotion.Write(m, block, profile); break;
                case BreakawayMotion m: BreakawayMotion.Write(m, block, profile); break;
                case SpringboardMotion m: SpringboardMotion.Write(m, block, profile); break;
                case TeeterTotterMotion m: TeeterTotterMotion.Write(m, block, profile); break;
                case PaddleMotion m: PaddleMotion.Write(m, block, profile); break;
                case FullyManipulableMotion m: FullyManipulableMotion.Write(m, block, profile); break;
            }
        });

    /// <summary>
    /// Reads the empty type-specific block stored alongside an <see cref="EntityMotion"/>.
    /// </summary>
    internal static void ReadEmpty(EndianReader reader, FormatProfile profile) =>
        reader.ReadBytes(BlockSize(profile.Game)); // padding

    /// <summary>
    /// Writes the empty type-specific block stored alongside an <see cref="EntityMotion"/>.
    /// </summary>
    internal static void WriteEmpty(EndianWriter writer, FormatProfile profile) =>
        writer.Write(new byte[BlockSize(profile.Game)]);
}
