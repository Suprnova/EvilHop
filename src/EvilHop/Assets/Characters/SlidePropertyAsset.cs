using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="BaseAsset"/> holding properties for sliding in the level it's in. Every field's
/// meaning is unknown; the game registers the asset type but never reads it.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/SLID">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class SlidePropertyAsset() : BaseAsset(AssetType.SlideProperty, baseType: 0x46), Physical.ISlidePropertyAsset
{
    /// <inheritdoc cref="Asset.Physical"/>
    public override Physical.ISlidePropertyAsset Physical => this;

    private float _unknown1;
    float Physical.ISlidePropertyAsset.Unknown1 { get => _unknown1; set => _unknown1 = value; }

    private float _unknown2;
    float Physical.ISlidePropertyAsset.Unknown2 { get => _unknown2; set => _unknown2 = value; }

    private float _unknown3 = 3f;
    float Physical.ISlidePropertyAsset.Unknown3 { get => _unknown3; set => _unknown3 = value; }

    private float _unknown4;
    float Physical.ISlidePropertyAsset.Unknown4 { get => _unknown4; set => _unknown4 = value; }

    private float _unknown5;
    float Physical.ISlidePropertyAsset.Unknown5 { get => _unknown5; set => _unknown5 = value; }

    private float _unknown6 = 45f;
    float Physical.ISlidePropertyAsset.Unknown6 { get => _unknown6; set => _unknown6 = value; }

    private float _unknown7 = 0.003f;
    float Physical.ISlidePropertyAsset.Unknown7 { get => _unknown7; set => _unknown7 = value; }

    private float _unknown8 = 3f;
    float Physical.ISlidePropertyAsset.Unknown8 { get => _unknown8; set => _unknown8 = value; }

    private float _unknown9 = 70f;
    float Physical.ISlidePropertyAsset.Unknown9 { get => _unknown9; set => _unknown9 = value; }

    private float _unknown10 = 70f;
    float Physical.ISlidePropertyAsset.Unknown10 { get => _unknown10; set => _unknown10 = value; }

    private float _unknown11 = 10f;
    float Physical.ISlidePropertyAsset.Unknown11 { get => _unknown11; set => _unknown11 = value; }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.SlideProperty"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.Incredibles,
    };

    internal static SlidePropertyAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new SlidePropertyAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        asset.Physical.Unknown1 = reader.ReadSingle();
        asset.Physical.Unknown2 = reader.ReadSingle();
        asset.Physical.Unknown3 = reader.ReadSingle();
        asset.Physical.Unknown4 = reader.ReadSingle();
        asset.Physical.Unknown5 = reader.ReadSingle();
        asset.Physical.Unknown6 = reader.ReadSingle();
        asset.Physical.Unknown7 = reader.ReadSingle();
        asset.Physical.Unknown8 = reader.ReadSingle();
        asset.Physical.Unknown9 = reader.ReadSingle();
        asset.Physical.Unknown10 = reader.ReadSingle();
        asset.Physical.Unknown11 = reader.ReadSingle();

        for (var i = 0; i < asset.Physical.LinkCount; i++)
            asset.Links.Add(Link.Read(reader, profile));
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(SlidePropertyAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);

        writer.Write(asset.Physical.Unknown1);
        writer.Write(asset.Physical.Unknown2);
        writer.Write(asset.Physical.Unknown3);
        writer.Write(asset.Physical.Unknown4);
        writer.Write(asset.Physical.Unknown5);
        writer.Write(asset.Physical.Unknown6);
        writer.Write(asset.Physical.Unknown7);
        writer.Write(asset.Physical.Unknown8);
        writer.Write(asset.Physical.Unknown9);
        writer.Write(asset.Physical.Unknown10);
        writer.Write(asset.Physical.Unknown11);

        foreach (var link in asset.Links)
            Link.Write(link, writer, profile);
        writer.Write(asset.GetUnparsedTail());
    }
}

public static partial class Physical
{
    /// <summary>
    /// An explicit interface used to interact with <see cref="SlidePropertyAsset"/>'s underlying values.
    /// </summary>
    public interface ISlidePropertyAsset : IBaseAsset
    {
        /// <summary>Unknown.</summary>
        float Unknown1 { get; set; }

        /// <summary>Unknown.</summary>
        float Unknown2 { get; set; }

        /// <summary>Unknown. Always 3.</summary>
        float Unknown3 { get; set; }

        /// <summary>Unknown.</summary>
        float Unknown4 { get; set; }

        /// <summary>Unknown.</summary>
        float Unknown5 { get; set; }

        /// <summary>Unknown. Always 45.</summary>
        float Unknown6 { get; set; }

        /// <summary>Unknown. Always 0.003.</summary>
        float Unknown7 { get; set; }

        /// <summary>Unknown. Always 3.</summary>
        float Unknown8 { get; set; }

        /// <summary>Unknown. Always 70.</summary>
        float Unknown9 { get; set; }

        /// <summary>Unknown. Always 70.</summary>
        float Unknown10 { get; set; }

        /// <summary>Unknown. Always 10.</summary>
        float Unknown11 { get; set; }
    }
}
