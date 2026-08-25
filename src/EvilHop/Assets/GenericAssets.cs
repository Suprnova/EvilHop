namespace EvilHop.Assets;

/// <summary>
/// The <see cref="Asset"/> a type with no known shape parses into. Its entire slice is preserved as
/// unparsed bytes.
/// </summary>
/// <remarks>
/// One instance of this class stands in for every unclassified <see cref="Common.AssetType"/>, which
/// is why <see cref="Asset.Type"/> is a stored field rather than something a class could hardcode.
/// The same applies to the four classes below.
/// </remarks>
internal sealed class GenericAsset : Asset;

/// <summary>
/// The <see cref="BaseAsset"/> a <c>BaseAsset</c>-shaped type with no concrete codec parses into.
/// Its fixed header is read; everything past it, links included, stays unparsed.
/// </summary>
internal sealed class GenericBaseAsset : BaseAsset;

/// <summary>
/// The <see cref="EntityAsset"/> an <c>EntityAsset</c>-shaped type with no concrete codec parses
/// into. Its shared prefix is read; everything past it, links included, stays unparsed.
/// </summary>
internal sealed class GenericEntityAsset : EntityAsset;

/// <summary>
/// The <see cref="DynaAsset"/> a <c>DYNA</c> subtype with no concrete codec parses into. Its shared
/// prefix is read; everything past it, links included, stays unparsed.
/// </summary>
internal sealed class GenericDynaAsset : DynaAsset;

/// <summary>
/// The <see cref="PayloadAsset"/> a payload type with no concrete codec parses into. Its whole slice
/// is the embedded file.
/// </summary>
internal sealed class GenericPayloadAsset : PayloadAsset;
