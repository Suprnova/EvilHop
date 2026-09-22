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
    private protected abstract MotionType Type { get; }

    /// <remarks>
    /// Every <see cref="MotionType"/> but <see cref="MotionType.None"/> shares its value with the
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
    /// <exception cref="InvalidDataException">The block's type is <see cref="MotionType.None"/> or unknown.</exception>
    internal static EntityMotion Read(EndianReader reader, FormatProfile profile)
    {
        using var block = ReadBlock(reader, BlockSize(profile.Game));
        var type = (MotionType)block.ReadByte();
        byte useBanking = block.ReadByte();
        var flags = (MotionFlags)block.ReadUInt16();

        EntityMotion motion = type switch
        {
            MotionType.ExtendRetract => ExtendRetractMotion.Read(block, profile),
            MotionType.Orbit => OrbitMotion.Read(block, profile),
            MotionType.Spline => SplineMotion.Read(block, profile),
            MotionType.MovePoint => MovePointMotion.Read(block, profile),
            MotionType.Mechanism => MechanismMotion.Read(block, profile),
            MotionType.Pendulum => PendulumMotion.Read(block, profile),
            _ => throw new InvalidDataException($"Motion type 0x{(byte)type:X2} has no {nameof(EntityMotion)}."),
        };

        if (motion is MovePointMotion movePoint)
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
            block.Write((byte)(value is MovePointMotion { UseBanking: true } ? 1 : 0));
            block.Write((ushort)value.Flags);
            switch (value)
            {
                case ExtendRetractMotion m: ExtendRetractMotion.Write(m, block, profile); break;
                case OrbitMotion m: OrbitMotion.Write(m, block, profile); break;
                case SplineMotion m: SplineMotion.Write(m, block, profile); break;
                case MovePointMotion m: MovePointMotion.Write(m, block, profile); break;
                case MechanismMotion m: MechanismMotion.Write(m, block, profile); break;
                case PendulumMotion m: PendulumMotion.Write(m, block, profile); break;
            }
        });

    /// <summary>
    /// Reads a Motion block of type <see cref="MotionType.None"/>, as stored alongside a
    /// <see cref="PlatformMotion"/>, returning the only thing it holds - its flags.
    /// </summary>
    /// <exception cref="InvalidDataException">The block's type isn't <see cref="MotionType.None"/>.</exception>
    internal static MotionFlags ReadEmpty(EndianReader reader, FormatProfile profile)
    {
        using var block = ReadBlock(reader, BlockSize(profile.Game));
        var type = (MotionType)block.ReadByte();
        if (type is not MotionType.None)
            throw new InvalidDataException($"Expected an empty Motion block, found motion type 0x{(byte)type:X2}.");

        block.ReadByte(); // use_banking, always zero
        return (MotionFlags)block.ReadUInt16();
    }

    /// <summary>
    /// Writes a Motion block of type <see cref="MotionType.None"/> holding only <paramref name="flags"/>.
    /// </summary>
    internal static void WriteEmpty(EndianWriter writer, FormatProfile profile, MotionFlags flags) =>
        WriteBlock(writer, BlockSize(profile.Game), block =>
        {
            block.Write((byte)MotionType.None);
            block.Write((byte)0); // use_banking
            block.Write((ushort)flags);
        });

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
}
