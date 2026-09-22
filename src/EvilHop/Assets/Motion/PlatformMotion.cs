using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="Motion"/> only a <see cref="PlatformAsset"/> can have, stored in its type-specific
/// block rather than its Motion block.
/// </summary>
public abstract partial class PlatformMotion : Motion
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
            PlatformType.ConveyorBelt => ConveyorBelt.Read(block, profile),
            PlatformType.Falling => Falling.Read(block, profile),
            PlatformType.ForwardReturn => ForwardReturn.Read(block, profile),
            PlatformType.Breakaway => Breakaway.Read(block, profile),
            PlatformType.Springboard => Springboard.Read(block, profile),
            PlatformType.TeeterTotter => TeeterTotter.Read(block, profile),
            PlatformType.Paddle => Paddle.Read(block, profile),
            PlatformType.FullyManipulable => FullyManipulable.Read(block, profile),
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
                case ConveyorBelt m: ConveyorBelt.Write(m, block, profile); break;
                case Falling m: Falling.Write(m, block, profile); break;
                case ForwardReturn m: ForwardReturn.Write(m, block, profile); break;
                case Breakaway m: Breakaway.Write(m, block, profile); break;
                case Springboard m: Springboard.Write(m, block, profile); break;
                case TeeterTotter m: TeeterTotter.Write(m, block, profile); break;
                case Paddle m: Paddle.Write(m, block, profile); break;
                case FullyManipulable m: FullyManipulable.Write(m, block, profile); break;
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
