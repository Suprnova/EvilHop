namespace EvilHop.Assets;

/// <summary>
/// A <see cref="BaseAsset"/> representing a <see cref="Common.AssetType.Dynamic"/> asset
/// a dynamically-typed object whose concrete shape is determined by <see cref="DynaType"/>.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/DYNA">Heavy Iron Modding documentation</seealso>
/// </remarks>
public abstract class DynaAsset : BaseAsset
{
    /// <summary>
    /// The <see cref="DynaAsset"/>'s subtype, determining its concrete on-disk layout.
    /// </summary>
    public uint DynaType { get; internal set; }

    /// <summary>
    /// The version of <see cref="DynaType"/>'s layout this <see cref="DynaAsset"/> was written with.
    /// </summary>
    public short Version { get; set; }

    /// <summary>
    /// The <see cref="DynaAsset"/>'s runtime handle.
    /// </summary>
    public short Handle { get; set; }
}
