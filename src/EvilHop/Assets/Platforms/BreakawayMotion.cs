using EvilHop.Common;
using EvilHop.Primitives;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="PlatformMotion"/> that falls a short while after the player stands on it, then resets.
/// </summary>
public sealed class BreakawayMotion : PlatformMotion
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
    /// falls, if any. <see cref="GameVersion.N100F"/> and <see cref="GameVersion.BFBB"/> only.
    /// </summary>
    public AssetId BustModelId { get; set; }

    /// <summary>
    /// This platform's breakaway flags. Not present in <see cref="GameVersion.N100F"/>.
    /// </summary>
    public BreakawayFlags BreakFlags { get; set; }

    /// <summary>
    /// The time, in seconds, before a falling platform stops colliding, judging by its name. Not
    /// present in <see cref="GameVersion.N100F"/> or <see cref="GameVersion.BFBB"/>.
    /// </summary>
    public float CollisionOffTime { get; set; }

    internal override PlatformType PlatformType => PlatformType.Breakaway;

    private protected override void ReadFields(EndianReader reader, GameVersion game)
    {
        BreakDelay = reader.ReadSingle();
        if (IsBFBBOrEarlier(game))
        {
            BustModelId = reader.ReadAssetId();
            ResetDelay = reader.ReadSingle();
            if (game is GameVersion.BFBB) BreakFlags = (BreakawayFlags)reader.ReadUInt32();
        }
        else
        {
            ResetDelay = reader.ReadSingle();
            BreakFlags = (BreakawayFlags)reader.ReadUInt32();
            CollisionOffTime = reader.ReadSingle();
        }
    }

    private protected override void WriteFields(EndianWriter writer, GameVersion game)
    {
        writer.Write(BreakDelay);
        if (IsBFBBOrEarlier(game))
        {
            writer.Write(BustModelId);
            writer.Write(ResetDelay);
            if (game is GameVersion.BFBB) writer.Write((uint)BreakFlags);
        }
        else
        {
            writer.Write(ResetDelay);
            writer.Write((uint)BreakFlags);
            writer.Write(CollisionOffTime);
        }
    }
}

/// <summary>
/// Represents all known values for <see cref="BreakawayMotion.BreakFlags"/>.
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
