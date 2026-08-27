namespace EvilHop.Assets;

/// <summary>
/// A <see cref="BaseAsset"/> representing a <see cref="Common.AssetType.Dynamic"/> asset
/// a dynamically-typed object whose concrete shape is determined by
/// <see cref="IPhysicalDynaAsset.DynaType"/>.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/DYNA">Heavy Iron Modding documentation</seealso>
/// </remarks>
public abstract class DynaAsset : BaseAsset, IPhysicalDynaAsset
{
    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalDynaAsset Physical => this;

    private protected uint _dynaType;
    uint IPhysicalDynaAsset.DynaType
    {
        get => _dynaType;
        set => _dynaType = value;
    }

    private protected short _version;
    short IPhysicalDynaAsset.Version
    {
        get => _version;
        set => _version = value;
    }

    private protected short _handle;
    short IPhysicalDynaAsset.Handle
    {
        get => _handle;
        set => _handle = value;
    }
}

/// <summary>
/// An explicit interface used to interact with <see cref="DynaAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalDynaAsset : IPhysicalBaseAsset
{
    /// <summary>
    /// The <see cref="DynaAsset"/>'s subtype, determining its concrete on-disk layout.
    /// </summary>
    uint DynaType { get; set; }
    /// <summary>
    /// The version of <see cref="DynaType"/>'s layout this <see cref="DynaAsset"/> was written with.
    /// </summary>
    short Version { get; set; }
    /// <summary>
    /// The <see cref="DynaAsset"/>'s runtime handle.
    /// </summary>
    short Handle { get; set; }
}
