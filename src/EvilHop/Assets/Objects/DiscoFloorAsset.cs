using EvilHop.Common;
using System.Collections.ObjectModel;

namespace EvilHop.Assets;

/// <summary>
/// An arrangement of tiles that light up and damage the player when fully lit, stepping through a
/// user-defined sequence of <see cref="States"/> - one step per pattern position.
/// </summary>
/// <remarks>
/// Each tile is three separately-modeled <see cref="AssetType.SimpleObject"/>s (off/transition/on),
/// located at runtime by matching a numbered <see cref="OffPrefix"/>/<see cref="TransitionPrefix"/>/
/// <see cref="OnPrefix"/> name against every <see cref="AssetType.SimpleObject"/> in the level.
/// <seealso href="https://heavyironmodding.org/wiki/DSCO">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed partial class DiscoFloorAsset() : BaseAsset(AssetType.DiscoFloor, baseType: 0x00), IPhysicalDiscoFloorAsset
{
    /// <summary>
    /// Behavior flags for this <see cref="DiscoFloorAsset"/>.
    /// </summary>
    public DiscoFloorFlags Flags { get; set; }

    /// <summary>
    /// Whether the pattern loops back to its first step once it reaches the last, instead of pausing there.
    /// </summary>
    public bool Loop
    {
        get => (Flags & DiscoFloorFlags.Loop) != 0;
        set => Flags = value ? Flags | DiscoFloorFlags.Loop : Flags & ~DiscoFloorFlags.Loop;
    }

    /// <summary>
    /// The duration, in seconds, of the transition (yellow tile) period between <see cref="States"/>.
    /// </summary>
    public float TransitionDuration { get; set; }

    /// <summary>
    /// The duration, in seconds, each of <see cref="States"/> is held (red tile) before transitioning
    /// to the next.
    /// </summary>
    public float StateDuration { get; set; }

    /// <summary>
    /// The name prefix identifying the SIMPs shown for a tile's "off" (white) state. Numbered SIMPs
    /// whose name starts with this prefix are matched to tiles in ascending order.
    /// </summary>
    public string OffPrefix { get; set; } = string.Empty;

    /// <summary>
    /// The name prefix identifying the SIMPs shown for a tile's "transition" (yellow) state. Numbered
    /// SIMPs whose name starts with this prefix are matched to tiles in ascending order.
    /// </summary>
    public string TransitionPrefix { get; set; } = string.Empty;

    /// <summary>
    /// The name prefix identifying the SIMPs shown for a tile's "on" (red) state. Numbered SIMPs
    /// whose name starts with this prefix are matched to tiles in ascending order.
    /// </summary>
    public string OnPrefix { get; set; } = string.Empty;

    /// <summary>
    /// The pattern this <see cref="DiscoFloorAsset"/> steps through, one entry per step.
    /// </summary>
    public Collection<DiscoFloorState> States { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalDiscoFloorAsset Physical => this;

    private uint? _overriddenTileCount;
    uint IPhysicalDiscoFloorAsset.TileCount
    {
        get => _overriddenTileCount ?? ComputedTileCount;
        set => _overriddenTileCount = value == ComputedTileCount ? null : value;
    }

    private uint ComputedTileCount => States.Count > 0 ? (uint)States[0].Tiles.Count : 0;

    private uint? _overriddenStateCount;
    uint IPhysicalDiscoFloorAsset.StateCount
    {
        get => _overriddenStateCount ?? (uint)States.Count;
        set => _overriddenStateCount = value == (uint)States.Count ? null : value;
    }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.DiscoFloor"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.BFBB,
        GameVersion.TSSM,
    };
}

/// <summary>
/// An explicit interface used to interact with <see cref="DiscoFloorAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalDiscoFloorAsset : IPhysicalBaseAsset
{
    /// <summary>
    /// The number of tiles each of <see cref="DiscoFloorAsset.States"/>' bitmasks describes, read
    /// directly from its stored field.
    /// </summary>
    /// <remarks>
    /// When disagreements with <see cref="DiscoFloorAsset.States"/>' actual tile counts exist, this
    /// field wins during serialization.
    /// </remarks>
    uint TileCount { get; set; }

    /// <summary>
    /// The number of <see cref="DiscoFloorAsset.States"/> stored for this asset, read directly from
    /// its stored field.
    /// </summary>
    /// <remarks>
    /// When disagreements with <see cref="DiscoFloorAsset.States"/>.Count exist, this field wins
    /// during serialization.
    /// </remarks>
    uint StateCount { get; set; }
}

/// <summary>
/// Behavior flags for a <see cref="DiscoFloorAsset"/>.
/// </summary>
[Flags]
public enum DiscoFloorFlags : uint
{
    /// <summary>Neither flag is set.</summary>
    None = 0,
    /// <summary>The pattern loops back to its first step once it reaches the last, instead of pausing there.</summary>
    Loop = 0x1,
    /// <summary>The Disco Floor runs and its SIMPs render.</summary>
    Enabled = 0x2,
}

/// <summary>
/// One step of a <see cref="DiscoFloorAsset"/>'s pattern: the <see cref="TileState"/> every tile is
/// set to during this step.
/// </summary>
public sealed class DiscoFloorState
{
    /// <summary>
    /// Each tile's state during this step, in tile order.
    /// </summary>
    public Collection<TileState> Tiles { get; } = [];

    // The on-disk mask is byte-aligned, so a tile count not divisible by 4 leaves a few unused bit
    // pairs in its last byte - real archives leave those set to whatever the authoring tool's buffer
    // happened to already hold, not zero. Caching the exact bytes here lets a codec replay them
    // verbatim as long as Tiles hasn't changed since Read, instead of always zeroing them.
    private byte[]? _rawMask;

    internal byte[]? GetRawMask() => _rawMask;

    internal void SetRawMask(byte[] mask) => _rawMask = mask;
}

/// <summary>
/// Defines the visual and active state assigned to an individual disco floor tile.
/// </summary>
public enum TileState : byte
{
    /// <summary>The tile is off (white).</summary>
    Off = 0,
    /// <summary>The tile is on (red).</summary>
    On = 1,
    /// <summary>The tile is randomly either on or off, chosen independently each time it is reached.</summary>
    Random = 2,
}
