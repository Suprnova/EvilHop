using EvilHop.Common;

namespace EvilHop.Assets;

/// <summary>
/// One <see cref="PickupTableAsset"/> entry, describing a single kind of pickup.
/// </summary>
public sealed class PickupTableEntry
{
    /// <summary>
    /// The hash <see cref="AssetType.Pickup"/> assets use to identify this pickup kind.
    /// </summary>
    public uint PickupHash { get; set; }

    /// <summary>
    /// Unknown.
    /// </summary>
    public byte PickupType { get; set; }

    /// <summary>
    /// Unknown.
    /// </summary>
    public byte PickupIndex { get; set; }

    /// <summary>
    /// Unknown.
    /// </summary>
    public ushort Flags { get; set; }

    /// <summary>
    /// How many of this pickup are granted at once.
    /// </summary>
    public uint Quantity { get; set; }

    /// <summary>
    /// The <see cref="AssetType.Model"/> or <see cref="AssetType.ModelInfo"/> this pickup displays as.
    /// </summary>
    public AssetId ModelId { get; set; }

    /// <summary>
    /// The <see cref="AssetType.Animation"/> or <see cref="AssetType.AnimationList"/> this pickup
    /// plays, if any.
    /// </summary>
    public AssetId AnimId { get; set; }
}
