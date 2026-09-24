using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="BaseAsset"/> holding settings for the scene it's in. Every field's meaning is
/// unknown; the game registers the asset type but never reads it.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/SSET">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class SceneSettingsAsset() : BaseAsset(AssetType.SceneSettings, baseType: 0x54), Physical.ISceneSettingsAsset
{
    /// <inheritdoc cref="Asset.Physical"/>
    public override Physical.ISceneSettingsAsset Physical => this;

    private short _unknown1 = 20;
    short Physical.ISceneSettingsAsset.Unknown1 { get => _unknown1; set => _unknown1 = value; }

    private short _unknown2 = 20;
    short Physical.ISceneSettingsAsset.Unknown2 { get => _unknown2; set => _unknown2 = value; }

    private short _unknown3;
    short Physical.ISceneSettingsAsset.Unknown3 { get => _unknown3; set => _unknown3 = value; }

    private short _unknown4;
    short Physical.ISceneSettingsAsset.Unknown4 { get => _unknown4; set => _unknown4 = value; }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.SceneSettings"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.Incredibles,
    };

    internal static SceneSettingsAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new SceneSettingsAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        asset.Physical.Unknown1 = reader.ReadInt16();
        asset.Physical.Unknown2 = reader.ReadInt16();
        asset.Physical.Unknown3 = reader.ReadInt16();
        asset.Physical.Unknown4 = reader.ReadInt16();

        for (var i = 0; i < asset.Physical.LinkCount; i++)
            asset.Links.Add(Link.Read(reader, profile));
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(SceneSettingsAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);

        writer.Write(asset.Physical.Unknown1);
        writer.Write(asset.Physical.Unknown2);
        writer.Write(asset.Physical.Unknown3);
        writer.Write(asset.Physical.Unknown4);

        foreach (var link in asset.Links)
            Link.Write(link, writer, profile);
        writer.Write(asset.GetUnparsedTail());
    }
}

public static partial class Physical
{
    /// <summary>
    /// An explicit interface used to interact with <see cref="SceneSettingsAsset"/>'s underlying values.
    /// </summary>
    public interface ISceneSettingsAsset : IBaseAsset
    {
        /// <summary>
        /// Unknown. Always 20.
        /// </summary>
        short Unknown1 { get; set; }

        /// <summary>
        /// Unknown. Always 20.
        /// </summary>
        short Unknown2 { get; set; }

        /// <summary>
        /// Unknown. Always 0.
        /// </summary>
        short Unknown3 { get; set; }

        /// <summary>
        /// Unknown. 0 in most scenes, and a seemingly arbitrary value, differing between a scene's own
        /// archives, in the rest.
        /// </summary>
        /// TODO: likely uninitialized memory left by the export tool - validate against decompiled source
        short Unknown4 { get; set; }
    }
}
