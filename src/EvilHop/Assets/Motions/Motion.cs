using EvilHop.Common;
using EvilHop.Primitives;

namespace EvilHop.Assets;

/// <summary>
/// Describes how an entity moves, or how it reacts when interacted with.
/// </summary>
/// <remarks>
/// <para>
/// Every concrete motion is either an <see cref="EntityMotion"/>, stored in the Motion block the
/// format shares between <see cref="AssetType.Platform"/> and <see cref="AssetType.Button"/>, or a
/// <see cref="PlatformMotion"/>, stored in a <see cref="PlatformAsset"/>'s own type-specific block.
/// </para>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/Data_Types#Motion">Heavy Iron Modding documentation</seealso>
/// </remarks>
public abstract class Motion
{
    /// <summary>
    /// Flags controlling this motion's initial state.
    /// </summary>
    /// <remarks>
    /// Stored in the Motion block's header even for a <see cref="PlatformMotion"/>, whose Motion
    /// block is otherwise empty. A platform's <b>Run</b> and <b>Stop</b> events toggle
    /// <see cref="MotionFlags.Stopped"/> whatever its motion.
    /// </remarks>
    public MotionFlags Flags { get; set; }

    /// <summary>
    /// The <see cref="Assets.PlatformType"/> a <see cref="PlatformAsset"/> with this motion stores.
    /// </summary>
    internal abstract PlatformType PlatformType { get; }

    private protected Motion() { }

    /// <summary>
    /// Whether <paramref name="game"/> uses the layouts that predate <see cref="GameVersion.TSSM"/>.
    /// </summary>
    private protected static bool IsBFBBOrEarlier(GameVersion game) => game is GameVersion.N100F or GameVersion.BFBB;

    /// <summary>
    /// Reads exactly <paramref name="size"/> bytes, returning a reader scoped to them.
    /// </summary>
    private protected static EndianReader ReadBlock(EndianReader reader, int size) =>
        new(new MemoryStream(reader.ReadBytes(size)), reader.Endianness);

    /// <summary>
    /// Writes whatever <paramref name="write"/> writes, then zeros up to exactly
    /// <paramref name="size"/> bytes.
    /// </summary>
    private protected static void WriteBlock(EndianWriter writer, int size, Action<EndianWriter> write)
    {
        using var stream = new MemoryStream();
        using (var block = new EndianWriter(stream, writer.Endianness, leaveOpen: true))
            write(block);
        writer.Write(stream.ToArray());
        writer.Write(new byte[size - (int)stream.Length]);
    }
}

/// <summary>
/// Represents all known values for <see cref="Motion.Flags"/>.
/// </summary>
/// TODO: sweeping change that applies everywhere: we need to stop writing that template summary
/// for Flag enums where we know the general meaning of the flags, like here.
[Flags]
public enum MotionFlags : ushort
{
    /// <summary>
    /// No flags are set.
    /// </summary>
    None = 0,
    /// <summary>
    /// A <see cref="MovePointMotion"/> turns its entity to face the direction it travels.
    /// </summary>
    FaceTravelDirection = 1 << 0,
    /// <summary>
    /// The motion starts stopped, waiting for a <b>Run</b> event. If unset, it starts moving as
    /// soon as the scene is prepared.
    /// </summary>
    Stopped = 1 << 2,
}
