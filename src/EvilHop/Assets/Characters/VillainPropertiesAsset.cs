using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// An <see cref="Asset"/> holding properties for one kind of NPC. Every field's meaning is unknown;
/// the game registers the asset type but never reads it, and nothing references one.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/VILP">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class VillainPropertiesAsset() : Asset(AssetType.VillainProperties), Physical.IVillainPropertiesAsset
{
    /// <inheritdoc cref="Asset.Physical"/>
    public override Physical.IVillainPropertiesAsset Physical => this;

    private uint _unknown1 = 0xCDCDCDCD;
    uint Physical.IVillainPropertiesAsset.Unknown1 { get => _unknown1; set => _unknown1 = value; }

    private int _unknown2;
    int Physical.IVillainPropertiesAsset.Unknown2 { get => _unknown2; set => _unknown2 = value; }

    private int _unknown3;
    int Physical.IVillainPropertiesAsset.Unknown3 { get => _unknown3; set => _unknown3 = value; }

    private int _unknown4;
    int Physical.IVillainPropertiesAsset.Unknown4 { get => _unknown4; set => _unknown4 = value; }

    private int _unknown5 = 31;
    int Physical.IVillainPropertiesAsset.Unknown5 { get => _unknown5; set => _unknown5 = value; }

    private int _unknown6;
    int Physical.IVillainPropertiesAsset.Unknown6 { get => _unknown6; set => _unknown6 = value; }

    private int _unknown7;
    int Physical.IVillainPropertiesAsset.Unknown7 { get => _unknown7; set => _unknown7 = value; }

    private float _unknown8 = 5f;
    float Physical.IVillainPropertiesAsset.Unknown8 { get => _unknown8; set => _unknown8 = value; }

    private int _unknown9 = -1;
    int Physical.IVillainPropertiesAsset.Unknown9 { get => _unknown9; set => _unknown9 = value; }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.VillainProperties"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.BFBB,
    };

    internal static VillainPropertiesAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile _)
    {
        var asset = new VillainPropertiesAsset();
        AssetFields.Populate(asset, header, debug);

        asset.Physical.Unknown1 = reader.ReadUInt32();
        asset.Physical.Unknown2 = reader.ReadInt32();
        asset.Physical.Unknown3 = reader.ReadInt32();
        asset.Physical.Unknown4 = reader.ReadInt32();
        asset.Physical.Unknown5 = reader.ReadInt32();
        asset.Physical.Unknown6 = reader.ReadInt32();
        asset.Physical.Unknown7 = reader.ReadInt32();
        asset.Physical.Unknown8 = reader.ReadSingle();
        asset.Physical.Unknown9 = reader.ReadInt32();

        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(VillainPropertiesAsset asset, EndianWriter writer, FormatProfile _)
    {
        writer.Write(asset.Physical.Unknown1);
        writer.Write(asset.Physical.Unknown2);
        writer.Write(asset.Physical.Unknown3);
        writer.Write(asset.Physical.Unknown4);
        writer.Write(asset.Physical.Unknown5);
        writer.Write(asset.Physical.Unknown6);
        writer.Write(asset.Physical.Unknown7);
        writer.Write(asset.Physical.Unknown8);
        writer.Write(asset.Physical.Unknown9);

        writer.Write(asset.GetUnparsedTail());
    }
}

public static partial class Physical
{
    /// <summary>
    /// An explicit interface used to interact with <see cref="VillainPropertiesAsset"/>'s underlying values.
    /// </summary>
    public interface IVillainPropertiesAsset : IAsset
    {
        /// <summary>
        /// Uninitialized memory left by the export tool. Always <c>0xCDCDCDCD</c>.
        /// </summary>
        uint Unknown1 { get; set; }

        /// <summary>Unknown. Always 0.</summary>
        int Unknown2 { get; set; }

        /// <summary>Unknown. Always 0.</summary>
        int Unknown3 { get; set; }

        /// <summary>Unknown. Always 0.</summary>
        int Unknown4 { get; set; }

        /// <summary>Unknown. Always 31.</summary>
        int Unknown5 { get; set; }

        /// <summary>Unknown. Always 0.</summary>
        int Unknown6 { get; set; }

        /// <summary>Unknown. Always 0.</summary>
        int Unknown7 { get; set; }

        /// <summary>Unknown. Always 5.</summary>
        float Unknown8 { get; set; }

        /// <summary>Unknown. Always -1.</summary>
        int Unknown9 { get; set; }
    }
}
