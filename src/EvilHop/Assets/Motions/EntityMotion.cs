using EvilHop.Common;
using EvilHop.Primitives;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="Motion"/> stored in the Motion block the format shares between
/// <see cref="AssetType.Platform"/> and <see cref="AssetType.Button"/>, moving its entity by itself.
/// </summary>
public abstract class EntityMotion : Motion
{
    private protected EntityMotion() { }

    /// <summary>
    /// The type this motion stores in its Motion block.
    /// </summary>
    private protected abstract MotionType Type { get; }

    /// <remarks>
    /// Every <see cref="MotionType"/> but <see cref="MotionType.None"/> shares its value with the
    /// <see cref="Assets.PlatformType"/> it drives.
    /// </remarks>
    internal sealed override PlatformType PlatformType => (PlatformType)Type;

    /// <summary>
    /// Reads this motion's type-specific fields from a reader scoped to the rest of its Motion block.
    /// </summary>
    private protected abstract void ReadFields(EndianReader reader, GameVersion game);

    /// <summary>
    /// Writes this motion's type-specific fields, no more than the rest of its Motion block holds.
    /// </summary>
    private protected abstract void WriteFields(EndianWriter writer, GameVersion game);

    /// <summary>
    /// The size, in bytes, of a Motion block under <paramref name="game"/>.
    /// </summary>
    internal static int BlockSize(GameVersion game) => IsBFBBOrEarlier(game) ? 0x30 : 0x3C;

    /// <summary>
    /// Reads one Motion block.
    /// </summary>
    /// <exception cref="InvalidDataException">The block's type is <see cref="MotionType.None"/> or unknown.</exception>
    internal static EntityMotion Read(EndianReader reader, GameVersion game)
    {
        using var block = ReadBlock(reader, BlockSize(game));
        var type = (MotionType)block.ReadByte();
        byte useBanking = block.ReadByte();
        var flags = (MotionFlags)block.ReadInt16();

        EntityMotion motion = type switch
        {
            MotionType.ExtendRetract => new ExtendRetractMotion(),
            MotionType.Orbit => new OrbitMotion(),
            MotionType.Spline => new SplineMotion(),
            MotionType.MovePoint => new MovePointMotion { UseBanking = useBanking == 1 },
            MotionType.Mechanism => new MechanismMotion(),
            MotionType.Pendulum => new PendulumMotion(),
            _ => throw new InvalidDataException($"Motion type 0x{(byte)type:X2} has no {nameof(EntityMotion)}."),
        };

        motion.Flags = flags;
        motion.ReadFields(block, game);
        return motion;
    }

    /// <summary>
    /// Writes this motion as one Motion block.
    /// </summary>
    internal void Write(EndianWriter writer, GameVersion game) =>
        WriteBlock(writer, BlockSize(game), block =>
        {
            block.Write((byte)Type);
            block.Write((byte)(this is MovePointMotion { UseBanking: true } ? 1 : 0));
            block.Write((short)Flags);
            WriteFields(block, game);
        });

    /// <summary>
    /// Reads a Motion block of type <see cref="MotionType.None"/>, as stored alongside a
    /// <see cref="PlatformMotion"/>, returning the only thing it holds - its flags.
    /// </summary>
    /// <exception cref="InvalidDataException">The block's type isn't <see cref="MotionType.None"/>.</exception>
    internal static MotionFlags ReadEmpty(EndianReader reader, GameVersion game)
    {
        using var block = ReadBlock(reader, BlockSize(game));
        var type = (MotionType)block.ReadByte();
        if (type is not MotionType.None)
            throw new InvalidDataException($"Expected an empty Motion block, found motion type 0x{(byte)type:X2}.");

        block.ReadByte(); // use_banking, always zero
        return (MotionFlags)block.ReadInt16();
    }

    /// <summary>
    /// Writes a Motion block of type <see cref="MotionType.None"/> holding only <paramref name="flags"/>.
    /// </summary>
    internal static void WriteEmpty(EndianWriter writer, GameVersion game, MotionFlags flags) =>
        WriteBlock(writer, BlockSize(game), block =>
        {
            block.Write((byte)MotionType.None);
            block.Write((byte)0); // use_banking
            block.Write((short)flags);
        });
}

/// <summary>
/// Every type a Motion block can store.
/// </summary>
internal enum MotionType : byte
{
    ExtendRetract = 0,
    Orbit = 1,
    Spline = 2,
    MovePoint = 3,
    Mechanism = 4,
    Pendulum = 5,
    None = 6,
}
