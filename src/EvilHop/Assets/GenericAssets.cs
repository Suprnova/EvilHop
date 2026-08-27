namespace EvilHop.Assets;

/// <summary>
/// The <see cref="Asset"/> a type with no known shape parses into. Its entire slice is preserved as
/// unparsed bytes.
/// </summary>
internal sealed class GenericAsset : Asset;

/// <summary>
/// A <see cref="BaseAsset"/> with no implemented codec.
/// </summary>
internal sealed class GenericBaseAsset : BaseAsset;

/// <summary>
/// An <see cref="EntityAsset"/> with no implemented codec.
/// </summary>
internal sealed class GenericEntityAsset : EntityAsset;

/// <summary>
/// A <see cref="DynaAsset"/> with no implemented codec.
/// </summary>
internal sealed class GenericDynaAsset : DynaAsset;

/// <summary>
/// A <see cref="PayloadAsset"/> with no implemented codec.
/// </summary>
internal sealed class GenericPayloadAsset : PayloadAsset;
