using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;

namespace EvilHop.Assets;

/// <summary>
/// Forwards any event it receives on to a fixed set of other assets, chosen according to
/// <see cref="GroupFlags"/>.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/GRUP">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class GroupAsset() : BaseAsset(AssetType.Group, baseType: 0x11), Physical.IGroupAsset
{
    /// <summary>The assets an event received by this group is forwarded to.</summary>
    public Collection<AssetId> Items { get; } = [];

    /// <summary>Which of <see cref="Items"/> a received event is forwarded to.</summary>
    public GroupEventMode GroupFlags { get; set; }

    /// <inheritdoc cref="Asset.Physical"/>
    public override Physical.IGroupAsset Physical => this;

    private ushort? _overriddenItemCount;
    ushort Physical.IGroupAsset.ItemCount
    {
        get => _overriddenItemCount ?? (ushort)Items.Count;
        set => _overriddenItemCount = value == (ushort)Items.Count ? null : value;
    }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.Group"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.N100F,
        GameVersion.BFBB,
        GameVersion.TSSM,
        GameVersion.Incredibles,
        GameVersion.ROTU,
        GameVersion.Ratatouille,
    };

    internal static GroupAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new GroupAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        ushort itemCount = reader.ReadUInt16();
        asset.GroupFlags = (GroupEventMode)reader.ReadInt16();

        for (int i = 0; i < itemCount; i++)
            asset.Items.Add(reader.ReadAssetId());

        for (var i = 0; i < asset.Physical.LinkCount; i++)
            asset.Links.Add(Link.Read(reader, profile));

        asset.Physical.ItemCount = (ushort)asset.Items.Count;
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(GroupAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);
        writer.Write(asset.Physical.ItemCount);
        writer.Write((short)asset.GroupFlags);

        foreach (var item in asset.Items)
            writer.Write(item);

        foreach (var link in asset.Links)
            Link.Write(link, writer, profile);
        writer.Write(asset.GetUnparsedTail());
    }

    /// <summary>Which of a <see cref="GroupAsset"/>'s <see cref="Items"/> a received event is forwarded to.</summary>
    public enum GroupEventMode : short
    {
        /// <summary>The event is forwarded to every item.</summary>
        All = 0,

        /// <summary>The event is forwarded to one randomly chosen item.</summary>
        Random = 1,

        /// <summary>
        /// The event is forwarded to one item, advancing to the next item (wrapping back to the first)
        /// each time an event is received.
        /// </summary>
        Sequential = 2,
    }
}

public static partial class Physical
{
    /// <summary>
    /// An explicit interface used to interact with <see cref="GroupAsset"/>'s underlying values.
    /// </summary>
    public interface IGroupAsset : IBaseAsset
    {
        /// <summary>
        /// The number of <see cref="GroupAsset.Items"/> stored for this asset, read directly from its
        /// leading count field.
        /// </summary>
        /// <remarks>
        /// When disagreements with <see cref="GroupAsset.Items"/>.Count exist, this field wins during
        /// serialization.
        /// </remarks>
        ushort ItemCount { get; set; }
    }
}
