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
    public static void Read(BaseAsset asset, EndianReader reader, int count)
    {
        for (int i = 0; i < count; i++)
        {
            asset.Links.Add(new Link
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
                ParamWidgetAssetId = reader.ReadAssetId(),
                CheckAssetId = reader.ReadAssetId(),
            });
        }
    }

    /// <summary>
    /// Writes <paramref name="asset"/>'s <see cref="BaseAsset.Links"/> to <paramref name="writer"/>.
    /// </summary>
    public static void Write(BaseAsset asset, EndianWriter writer)
    {
        foreach (var link in asset.Links)
        {
            writer.Write(link.SourceEvent);
            writer.Write(link.DestinationEvent);
            writer.Write(link.DestinationAssetId);
            foreach (var param in link.Params)
                param.WriteTo(writer);
            writer.Write(link.ParamWidgetAssetId);
            writer.Write(link.CheckAssetId);
        }
    }
}
