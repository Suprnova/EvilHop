using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="PlatformMotion"/> that falls a short while after the player stands on it, then resets.
/// </summary>
public sealed class BreakawayMotion() : PlatformMotion
{
    /// <summary>
    /// The time, in seconds, the platform takes to fall after the player stands on it.
    /// </summary>
    public float BreakDelay { get; set; }

    /// <summary>
    /// The time, in seconds, after falling before the platform resets.
    /// </summary>
    public float ResetDelay { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the <see cref="AssetType.Model"/> the platform switches to as it
    /// falls, if any. Only present in <see cref="GameVersion.N100F"/> and
    /// <see cref="GameVersion.BFBB"/>.
    /// </summary>
    public AssetId BustModelId { get; set; }

    /// <summary>
    /// This platform's breakaway flags. Not present in <see cref="GameVersion.N100F"/>.
    /// </summary>
    public BreakawayFlags BreakFlags { get; set; }

    /// <summary>
    /// Unknown. Not present in <see cref="GameVersion.N100F"/> or <see cref="GameVersion.BFBB"/>.
    /// </summary>
    public float CollisionOffTime { get; set; }

    internal override PlatformType PlatformType => PlatformType.Breakaway;

    internal static BreakawayMotion Read(EndianReader reader, FormatProfile profile)
    {
        var game = profile.Game;
        var motion = new BreakawayMotion { BreakDelay = reader.ReadSingle() };
        if (IsBFBBOrEarlier(game))
        {
            motion.BustModelId = reader.ReadAssetId();
            motion.ResetDelay = reader.ReadSingle();
            if (game is GameVersion.BFBB) motion.BreakFlags = (BreakawayFlags)reader.ReadUInt32();
        }
        else
        {
            motion.ResetDelay = reader.ReadSingle();
            motion.BreakFlags = (BreakawayFlags)reader.ReadUInt32();
            motion.CollisionOffTime = reader.ReadSingle();
        }
        return motion;
    }

    internal static void Write(BreakawayMotion value, EndianWriter writer, FormatProfile profile)
    {
        var game = profile.Game;
        writer.Write(value.BreakDelay);
        if (IsBFBBOrEarlier(game))
        {
            writer.Write(value.BustModelId);
            writer.Write(value.ResetDelay);
            if (game is GameVersion.BFBB) writer.Write((uint)value.BreakFlags);
        }
        else
        {
            writer.Write(value.ResetDelay);
            writer.Write((uint)value.BreakFlags);
            writer.Write(value.CollisionOffTime);
        }
    }
}

/// <summary>
/// Flags governing certain behaviors of a <see cref="BreakawayMotion"/> platform.
/// </summary>
[Flags]
public enum BreakawayFlags : uint
{
    /// <summary>
    /// No flags are set.
    /// </summary>
    None = 0,
    /// <summary>
    /// The platform doesn't break while the player is sneaking on it.
    /// </summary>
    AllowSneak = 1 << 0,
}
