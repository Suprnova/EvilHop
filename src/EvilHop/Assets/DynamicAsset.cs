using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="BaseAsset"/> representing a <see cref="AssetType.Dynamic"/> asset: a container for
/// many unrelated object types, each identified by its <see cref="Kind"/> and laid out entirely
/// differently from the rest.
/// </summary>
/// <remarks>
/// <para>
/// Not instantiated directly. A <see cref="DynamicKind"/> with a typed model reads as its own
/// <c>FooDynamicAsset</c> subclass; every other kind, and every layout (game and
/// <see cref="Physical.IDynamicAsset.Version"/>) a typed model doesn't cover, reads as a
/// <see cref="GenericDynamicAsset"/>.
/// </para>
/// <para>
/// On disk, a dynamic is its 8-byte base header, <see cref="Kind"/>,
/// <see cref="Physical.IDynamicAsset.Version"/>, and <see cref="Physical.IDynamicAsset.Handle"/>,
/// then its kind's own fields, then its <see cref="BaseAsset.Links"/>, which always occupy the final
/// bytes. Its unparsed tail is whatever a kind's fields leave unread, so it sits before the links
/// rather than after them.
/// </para>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/DYNA">Heavy Iron Modding documentation</seealso>
/// </remarks>
/// <param name="version">The <see cref="Physical.IDynamicAsset.Version"/> a new instance is written with.</param>
public abstract class DynamicAsset(short version = 0) : BaseAsset(AssetType.Dynamic), Physical.IDynamicAsset
{
    /// <summary>
    /// Which kind of object this dynamic is, determining its on-disk layout.
    /// </summary>
    public abstract DynamicKind Kind { get; }

    /// <inheritdoc cref="Asset.Physical"/>
    public override Physical.IDynamicAsset Physical => this;

    private DynamicKind? _overriddenKind;
    DynamicKind Physical.IDynamicAsset.Kind
    {
        get => _overriddenKind ?? Kind;
        set => _overriddenKind = value == Kind ? null : value;
    }

    private short _version = version;
    short Physical.IDynamicAsset.Version { get => _version; set => _version = value; }

    private short _handle;
    short Physical.IDynamicAsset.Handle { get => _handle; set => _handle = value; }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.Dynamic"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.BFBB,
        GameVersion.TSSM,
        GameVersion.Incredibles,
        GameVersion.ROTU,
        GameVersion.Ratatouille,
    };

    internal static DynamicAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var (kind, version) = DynamicAssetPrefix.Peek(reader);
        var asset = DynamicCodecs.Create(kind, version, profile.Game);
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);
        DynamicAssetPrefix.Read(asset, reader);

        var remaining = (int)(reader.BaseStream.Length - reader.BaseStream.Position);
        var bodySize = remaining - Link.SizeOf(profile) * asset.Physical.LinkCount;
        if (bodySize < 0)
        {
            // the links can't be located, so leave them in the tail and keep LinkCount as read
            asset.SetUnparsedTail(reader.ReadRemainingBytes());
            return asset;
        }

        using (var body = new EndianReader(new MemoryStream(reader.ReadBytes(bodySize)), profile.Endianness))
        {
            DynamicCodecs.ReadBody(asset, body, profile);
            asset.SetUnparsedTail(body.ReadRemainingBytes());
        }

        for (var i = 0; i < asset.Physical.LinkCount; i++)
            asset.Links.Add(Link.Read(reader, profile));
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        return asset;
    }

    internal static void Write(DynamicAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);
        DynamicAssetPrefix.Write(asset, writer);
        DynamicCodecs.WriteBody(asset, writer, profile);
        writer.Write(asset.GetUnparsedTail());

        foreach (var link in asset.Links)
            Link.Write(link, writer, profile);
    }
}

public static partial class Physical
{
    /// <summary>
    /// An explicit interface used to interact with <see cref="DynamicAsset"/>'s underlying values.
    /// </summary>
    public interface IDynamicAsset : IBaseAsset
    {
        /// <summary>
        /// The <see cref="DynamicAsset"/>'s <see cref="DynamicKind"/>, as stored. Defaults to
        /// <see cref="DynamicAsset.Kind"/>.
        /// </summary>
        DynamicKind Kind { get; set; }

        /// <summary>
        /// The revision of <see cref="Kind"/>'s layout this <see cref="DynamicAsset"/> was written
        /// with. Together with the game, selects the layout its fields are read with.
        /// </summary>
        short Version { get; set; }

        /// <summary>
        /// Unknown. Zero in every shipped archive.
        /// </summary>
        short Handle { get; set; }
    }
}
