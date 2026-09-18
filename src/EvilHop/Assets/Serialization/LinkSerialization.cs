using EvilHop.Common;
using EvilHop.Primitives;

namespace EvilHop.Assets.Serialization;

/// <summary>
/// Reads and writes a <see cref="BaseAsset"/>'s <see cref="Link"/> array, wherever a codec's layout
/// places it.
/// </summary>
internal static class LinkSerialization
{
    /// <summary>
    /// Reads <paramref name="count"/> <see cref="Link"/>s from <paramref name="reader"/>'s current
    /// position into <paramref name="asset"/>'s <see cref="BaseAsset.Links"/>.
    /// </summary>
    /// <param name="asset">The <see cref="BaseAsset"/> to populate.</param>
    /// <param name="reader">The reader to read from.</param>
    /// <param name="count">How many links to read.</param>
    /// <param name="hasExtendedFields">
    /// Whether each <see cref="Link"/> is followed by <see cref="Link.ParamWidgetAssetId"/> and
    /// <see cref="Link.CheckAssetId"/>. False only for <see cref="GameVersion.N100F"/>'s 2001-06-11
    /// prototype, whose links are 24 bytes instead of 32; true everywhere else.
    /// </param>
    public static void Read(BaseAsset asset, EndianReader reader, int count, bool hasExtendedFields = true)
    {
        for (int i = 0; i < count; i++)
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
            if (hasExtendedFields)
            {
                link.ParamWidgetAssetId = reader.ReadAssetId();
                link.CheckAssetId = reader.ReadAssetId();
            }
            asset.Links.Add(link);
        }
    }

    /// <summary>
    /// Writes <paramref name="asset"/>'s <see cref="BaseAsset.Links"/> to <paramref name="writer"/>.
    /// </summary>
    /// <param name="asset">The <see cref="BaseAsset"/> to read from.</param>
    /// <param name="writer">The writer to write to.</param>
    /// <param name="hasExtendedFields">
    /// Whether each <see cref="Link"/> is followed by <see cref="Link.ParamWidgetAssetId"/> and
    /// <see cref="Link.CheckAssetId"/>. False only for <see cref="GameVersion.N100F"/>'s 2001-06-11
    /// prototype, whose links are 24 bytes instead of 32; true everywhere else.
    /// </param>
    public static void Write(BaseAsset asset, EndianWriter writer, bool hasExtendedFields = true)
    {
        foreach (var link in asset.Links)
        {
            writer.Write(link.SourceEvent);
            writer.Write(link.DestinationEvent);
            writer.Write(link.DestinationAssetId);
            foreach (var param in link.Params)
                param.WriteTo(writer);
            if (hasExtendedFields)
            {
                writer.Write(link.ParamWidgetAssetId);
                writer.Write(link.CheckAssetId);
            }
        }
    }
}
