using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="Motion"/> stored in the Motion block the format shares between
/// <see cref="AssetType.Platform"/> and <see cref="AssetType.Button"/>, moving its entity by itself.
/// </summary>
public abstract partial class EntityMotion : Motion
{
    private protected EntityMotion() { }

    /// <summary>
    /// The type this motion stores in its Motion block.
    /// </summary>
    private protected abstract Kind Type { get; }

    /// <remarks>
    /// Every <see cref="Kind"/> but <see cref="Kind.None"/> shares its value with the
    /// <see cref="Assets.PlatformType"/> it drives.
    /// </remarks>
    internal sealed override PlatformType PlatformType => (PlatformType)Type;

    /// <summary>
    /// The size, in bytes, of a Motion block under <paramref name="game"/>.
    /// </summary>
    internal static int BlockSize(GameVersion game) => IsBFBBOrEarlier(game) ? 0x30 : 0x3C;

    /// <summary>
    /// Reads one Motion block.
    /// </summary>
    /// <exception cref="InvalidDataException">The block's type is <see cref="Kind.None"/> or unknown.</exception>
    internal static EntityMotion Read(EndianReader reader, FormatProfile profile)
    {
        using var block = ReadBlock(reader, BlockSize(profile.Game));
        var type = (Kind)block.ReadByte();
        byte useBanking = block.ReadByte();
        var flags = (Behavior)block.ReadUInt16();

        EntityMotion motion = type switch
        {
            Kind.ExtendRetract => ExtendRetract.Read(block, profile),
            Kind.Orbit => Orbit.Read(block, profile),
            Kind.Spline => Spline.Read(block, profile),
            Kind.MovePoint => MovePoint.Read(block, profile),
            Kind.Mechanism => Mechanism.Read(block, profile),
            Kind.Pendulum => Pendulum.Read(block, profile),
            _ => throw new InvalidDataException($"Motion type 0x{(byte)type:X2} has no {nameof(EntityMotion)}."),
        };

        if (motion is MovePoint movePoint)
            movePoint.UseBanking = useBanking == 1;
        motion.Flags = flags;
        return motion;
    }

    /// <summary>
    /// Writes this motion as one Motion block.
    /// </summary>
    internal static void Write(EntityMotion value, EndianWriter writer, FormatProfile profile) =>
        WriteBlock(writer, BlockSize(profile.Game), block =>
        {
            block.Write((byte)value.Type);
            block.Write((byte)(value is MovePoint { UseBanking: true } ? 1 : 0));
            block.Write((ushort)value.Flags);
            switch (value)
            {
                case ExtendRetract m: ExtendRetract.Write(m, block, profile); break;
                case Orbit m: Orbit.Write(m, block, profile); break;
                case Spline m: Spline.Write(m, block, profile); break;
                case MovePoint m: MovePoint.Write(m, block, profile); break;
                case Mechanism m: Mechanism.Write(m, block, profile); break;
                case Pendulum m: Pendulum.Write(m, block, profile); break;
            }
        });

    /// <summary>
    /// Reads a Motion block of type <see cref="Kind.None"/>, as stored alongside a
    /// <see cref="PlatformMotion"/>, returning the only thing it holds - its flags.
    /// </summary>
    /// <exception cref="InvalidDataException">The block's type isn't <see cref="Kind.None"/>.</exception>
    internal static Behavior ReadEmpty(EndianReader reader, FormatProfile profile)
    {
        using var block = ReadBlock(reader, BlockSize(profile.Game));
        var type = (Kind)block.ReadByte();
        if (type is not Kind.None)
            throw new InvalidDataException($"Expected an empty Motion block, found motion type 0x{(byte)type:X2}.");

        block.ReadByte(); // use_banking, always zero
        return (Behavior)block.ReadUInt16();
    }

    /// <summary>
    /// Writes a Motion block of type <see cref="Kind.None"/> holding only <paramref name="flags"/>.
    /// </summary>
    internal static void WriteEmpty(EndianWriter writer, FormatProfile profile, Behavior flags) =>
        WriteBlock(writer, BlockSize(profile.Game), block =>
        {
            block.Write((byte)Kind.None);
            block.Write((byte)0); // use_banking
            block.Write((ushort)flags);
        });

    /// <summary>
    /// Every type a Motion block can store.
    /// </summary>
    internal enum Kind : byte
    {
        ExtendRetract = 0,
        Orbit = 1,
        Spline = 2,
        MovePoint = 3,
        Mechanism = 4,
        Pendulum = 5,
        None = 6,
    }
}
