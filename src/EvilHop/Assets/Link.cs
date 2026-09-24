using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.Immutable;

namespace EvilHop.Assets;

/// <summary>
/// Connects a <see cref="BaseAsset"/> to another asset via a source and destination event pair.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/Events#Links">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class Link()
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
    /// <exception cref="ArgumentException">The assigned value's length isn't 4.</exception>
    public ImmutableArray<Parameter> Params
    {
        get;
        set => field = value.Length == 4
            ? value
            : throw new ArgumentException($"{nameof(Params)} must contain exactly 4 elements.", nameof(value));
    } = ZeroedParams();

    /// <summary>
    /// The size, in bytes, of one <see cref="Link"/> on disk under <paramref name="profile"/>.
    /// </summary>
    internal static int SizeOf(FormatProfile profile) => profile.LinkHasExtendedFields ? 32 : 24;

    /// <summary>
    /// A supplemental <see cref="AssetId"/> parameter for <see cref="DestinationEvent"/>.
    /// </summary>
    public AssetId ParamWidgetAssetId { get; set; }
    /// <summary>
    /// The <see cref="AssetId"/> that <see cref="SourceEvent"/> must've been received from to
    /// trigger this <see cref="Link"/>, if non-null.
    /// </summary>
    public AssetId CheckAssetId { get; set; }

    private static ImmutableArray<Parameter> ZeroedParams() =>
        [new RawParameter(new byte[4]), new RawParameter(new byte[4]), new RawParameter(new byte[4]), new RawParameter(new byte[4])];

    /// <summary>
    /// Reads one <see cref="Link"/> from <paramref name="reader"/>'s current position. The caller owns
    /// the loop over a link list, since the link count is <see cref="BaseAsset"/>'s physical field.
    /// </summary>
    internal static Link Read(EndianReader reader, FormatProfile profile)
    {
        var link = new Link
        {
            SourceEvent = reader.ReadInt16(),
            DestinationEvent = reader.ReadInt16(),
            DestinationAssetId = reader.ReadAssetId(),
            Params =
            [
                new RawParameter(reader.ReadBytes(4)),
                new RawParameter(reader.ReadBytes(4)),
                new RawParameter(reader.ReadBytes(4)),
                new RawParameter(reader.ReadBytes(4)),
            ],
        };
        if (profile.LinkHasExtendedFields)
        {
            link.ParamWidgetAssetId = reader.ReadAssetId();
            link.CheckAssetId = reader.ReadAssetId();
        }
        return link;
    }

    /// <summary>Writes <paramref name="value"/> to <paramref name="writer"/>.</summary>
    internal static void Write(Link value, EndianWriter writer, FormatProfile profile)
    {
        writer.Write(value.SourceEvent);
        writer.Write(value.DestinationEvent);
        writer.Write(value.DestinationAssetId);
        foreach (var param in value.Params)
            param.WriteTo(writer);
        if (profile.LinkHasExtendedFields)
        {
            writer.Write(value.ParamWidgetAssetId);
            writer.Write(value.CheckAssetId);
        }
    }
}
