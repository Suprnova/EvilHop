using EvilHop.Common;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="BaseAsset"/> representing a <see cref="AssetType.Dynamic"/> asset
/// a dynamically-typed object whose concrete shape is determined by
/// <see cref="Physical.IDynaAsset.DynaType"/>.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/DYNA">Heavy Iron Modding documentation</seealso>
/// </remarks>
public abstract class DynaAsset(AssetType type) : BaseAsset(type), Physical.IDynaAsset
{
    /// <inheritdoc cref="Asset.Physical"/>
    public override Physical.IDynaAsset Physical => this;

    private protected uint _dynaType;
    uint Physical.IDynaAsset.DynaType
    {
        get => _dynaType;
        set => _dynaType = value;
    }

    private protected short _version;
    short Physical.IDynaAsset.Version
    {
        get => _version;
        set => _version = value;
    }

    private protected short _handle;
    short Physical.IDynaAsset.Handle
    {
        get => _handle;
        set => _handle = value;
    }
}

public static partial class Physical
{
    /// <summary>
    /// An explicit interface used to interact with <see cref="DynaAsset"/>'s underlying values.
    /// </summary>
    public interface IDynaAsset : IBaseAsset
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
}
