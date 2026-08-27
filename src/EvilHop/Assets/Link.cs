using EvilHop.Primitives;

namespace EvilHop.Assets;

/// <summary>
/// Connects a <see cref="BaseAsset"/> to another asset via a source and destination event pair.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/Events#Links">Heavy Iron Modding documentation</seealso>
/// </remarks>
public struct Link
{
    /// <summary>
    /// Initializes a new instance of <see cref="Link"/> with four zeroed <see cref="Params"/> slots.
    /// </summary>
    public Link() { }

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

    private Parameter[] _params = ZeroedParams();
    /// <summary>
    /// The four parameter slots passed alongside <see cref="DestinationEvent"/>. Always exactly 4
    /// elements, in order.
    /// </summary>
    /// <exception cref="ArgumentException">The assigned array's length isn't 4.</exception>
    public Parameter[] Params
    {
        readonly get => _params;
        set => _params = value.Length == 4
            ? value
            : throw new ArgumentException($"{nameof(Params)} must contain exactly 4 elements.", nameof(value));
    }

    /// <summary>
    /// A supplemental <see cref="AssetId"/> parameter for <see cref="DestinationEvent"/>.
    /// </summary>
    public AssetId ParamWidgetAssetId { get; set; }
    /// <summary>
    /// The <see cref="AssetId"/> that <see cref="SourceEvent"/> must've been received from to
    /// trigger this <see cref="Link"/>, if non-null.
    /// </summary>
    public AssetId CheckAssetId { get; set; }

    private static Parameter[] ZeroedParams() =>
        [new RawParameter(new byte[4]), new RawParameter(new byte[4]), new RawParameter(new byte[4]), new RawParameter(new byte[4])];
}
