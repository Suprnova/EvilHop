using EvilHop.Common;
using EvilHop.Primitives;
using System.Collections.Immutable;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="PlatformMotion"/> that launches the player into the air.
/// </summary>
public sealed class SpringboardMotion : PlatformMotion
{
    /// <summary>
    /// Exactly 3 jump heights. <see cref="GameVersion.BFBB"/> appears to use the highest.
    /// </summary>
    /// <exception cref="ArgumentException">The assigned value's length isn't 3.</exception>
    public ImmutableArray<float> JumpHeights
    {
        get;
        set => field = value.Length == 3
            ? value
            : throw new ArgumentException($"{nameof(JumpHeights)} must contain exactly 3 elements.", nameof(value));
    } = [0f, 0f, 0f];

    /// <summary>
    /// The height of a Bubble Bounce off this springboard. Not present in <see cref="GameVersion.N100F"/>.
    /// </summary>
    public float BounceHeight { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the <see cref="AssetType.Animation"/> played when the
    /// springboard launches the player, if any.
    /// </summary>
    public AssetId SpringAnimationId { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the <see cref="AssetType.Animation"/> played while the
    /// springboard is idle, if any.
    /// </summary>
    public AssetId IdleAnimationId { get; set; }

    /// <summary>
    /// The normalized direction the player is launched in.
    /// </summary>
    public Vector3 JumpDirection { get; set; }

    /// <summary>
    /// This springboard's flags. Not present in <see cref="GameVersion.N100F"/>.
    /// </summary>
    public SpringboardFlags SpringFlags { get; set; }

    internal override PlatformType PlatformType => PlatformType.Springboard;

    private protected override void ReadFields(EndianReader reader, GameVersion game)
    {
        JumpHeights = [reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()];
        if (game is not GameVersion.N100F) BounceHeight = reader.ReadSingle();
        SpringAnimationId = reader.ReadAssetId();
        IdleAnimationId = reader.ReadAssetId();
        reader.ReadAssetId(); // a third animation slot, never read by the game and always zero
        // TODO: that should probably still be read. If we ever support Physical layers for
        // internal asset structs, it should reside there.
        JumpDirection = reader.ReadVector3();
        if (game is not GameVersion.N100F) SpringFlags = (SpringboardFlags)reader.ReadUInt32();
    }

    private protected override void WriteFields(EndianWriter writer, GameVersion game)
    {
        foreach (float height in JumpHeights) writer.Write(height);
        if (game is not GameVersion.N100F) writer.Write(BounceHeight);
        writer.Write(SpringAnimationId);
        writer.Write(IdleAnimationId);
        writer.Write(AssetId.None); // third animation slot
        writer.Write(JumpDirection);
        if (game is not GameVersion.N100F) writer.Write((uint)SpringFlags);
    }
}

/// <summary>
/// Represents all known values for <see cref="SpringboardMotion.SpringFlags"/>.
/// </summary>
[Flags]
public enum SpringboardFlags : uint
{
    /// <summary>
    /// No flags are set.
    /// </summary>
    None = 0,
    /// <summary>
    /// The camera looks down at the player while they're launched.
    /// </summary>
    LockView = 1 << 0,
    /// <summary>
    /// The player can't move while they're launched.
    /// </summary>
    LockMovement = 1 << 2,
}
