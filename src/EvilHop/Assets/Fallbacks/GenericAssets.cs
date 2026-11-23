using EvilHop.Common;

namespace EvilHop.Assets;

/// <summary>
/// The <see cref="Asset"/> a type with no known shape parses into. Its entire slice is preserved as
/// unparsed bytes.
/// </summary>
internal sealed class GenericAsset(AssetType type) : Asset(type)
{
    public override SupportState Support => SupportState.Untyped;
}

/// <summary>
/// A <see cref="BaseAsset"/> with no implemented codec.
/// </summary>
internal sealed class GenericBaseAsset(AssetType type) : BaseAsset(type)
{
    public override SupportState Support => SupportState.Untyped;
}

/// <summary>
/// An <see cref="EntityAsset"/> with no implemented codec.
/// </summary>
internal sealed class GenericEntityAsset(AssetType type) : EntityAsset(type)
{
    public override SupportState Support => SupportState.Untyped;
}

/// <summary>
/// A <see cref="PayloadAsset"/> with no implemented codec.
/// </summary>
internal sealed class GenericPayloadAsset(AssetType type) : PayloadAsset(type);
