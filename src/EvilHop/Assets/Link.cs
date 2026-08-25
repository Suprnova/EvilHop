using EvilHop.Primitives;

namespace EvilHop.Assets;

/// <summary>
/// Connects a <see cref="BaseAsset"/> to another asset via a source and destination event pair.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/Events#Links">Heavy Iron Modding documentation</seealso>
/// </remarks>
/// TODO: type-safe abstraction for events
public struct Link
{
    /// <summary>
    /// The event on the owning <see cref="BaseAsset"/> that triggers this <see cref="Link"/>.
    /// </summary>
    public short SourceEvent { get; set; }
    /// <summary>
    /// The event this <see cref="Link"/> sends to <see cref="DestinationAssetId"/>.
    /// </summary>
    public short DestinationEvent { get; set; }
    /// <summary>
    /// The <see cref="AssetId"/> this <see cref="Link"/> sends <see cref="DestinationEvent"/> to.
    /// </summary>
    public AssetId DestinationAssetId { get; set; }
    /// <summary>
    /// The four parameter slots passed alongside <see cref="DestinationEvent"/>. Always exactly 4
    /// elements, in order.
    /// </summary>
    /// TODO: Determine if sanitising this is the codec's responsibility, and if we just zero
    /// unallocated indices to reach 4 elements.
    public Parameter[] Params { get; set; }
    /// <summary>
    /// A supplemental <see cref="AssetId"/> parameter for <see cref="DestinationEvent"/>. 
    /// </summary>
    public AssetId ParamWidgetAssetId { get; set; }
    /// <summary>
    /// The <see cref="AssetId"/> that <see cref="SourceEvent"/> must've been received from to
    /// trigger this <see cref="Link"/>, if non-null.
    /// </summary>
    public AssetId CheckAssetId { get; set; }
}
