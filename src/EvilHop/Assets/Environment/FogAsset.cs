using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// Controls the level's fog: its color, background color, density, and the distance range it fades
/// in over.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/FOG">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class FogAsset() : BaseAsset(AssetType.Fog, baseType: 0x24), Physical.IFogAsset
{
    /// <summary>
    /// The color the sky/background is drawn as while this fog is active.
    /// </summary>
    public Rgba BackgroundColor { get; set; }

    /// <summary>
    /// The fog's own color.
    /// </summary>
    public Rgba Color { get; set; }

    /// <summary>
    /// How dense the fog is.
    /// </summary>
    public float Density { get; set; }

    /// <summary>
    /// The distance from the camera the fog begins at.
    /// </summary>
    public float StartDistance { get; set; }

    /// <summary>
    /// The distance from the camera the fog reaches full density at.
    /// </summary>
    public float StopDistance { get; set; }

    /// <summary>
    /// When another <see cref="FogAsset"/> activates while this one is active, how long the fog takes
    /// to transition from this asset's settings to the other's.
    /// </summary>
    public float TransitionTime { get; set; }

    /// <inheritdoc cref="Asset.Physical"/>
    public override Physical.IFogAsset Physical => this;

    private byte _fogType;
    byte Physical.IFogAsset.FogType { get => _fogType; set => _fogType = value; }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.Fog"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.N100F,
        GameVersion.BFBB,
        GameVersion.Incredibles,
        GameVersion.TSSM,
        GameVersion.ROTU,
        GameVersion.Ratatouille,
    };

    internal static FogAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new FogAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        asset.BackgroundColor = reader.ReadRgba32();
        asset.Color = reader.ReadRgba32();
        asset.Density = reader.ReadSingle();
        asset.StartDistance = reader.ReadSingle();
        asset.StopDistance = reader.ReadSingle();
        asset.TransitionTime = reader.ReadSingle();
        asset.Physical.FogType = reader.ReadByte();
        reader.ReadBytes(3); // padding, always zero

        for (var i = 0; i < asset.Physical.LinkCount; i++)
            asset.Links.Add(Link.Read(reader, profile));
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(FogAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);

        writer.WriteRgba32(asset.BackgroundColor);
        writer.WriteRgba32(asset.Color);
        writer.Write(asset.Density);
        writer.Write(asset.StartDistance);
        writer.Write(asset.StopDistance);
        writer.Write(asset.TransitionTime);
        writer.Write(asset.Physical.FogType);
        writer.Write(new byte[3]); // padding

        foreach (var link in asset.Links)
            Link.Write(link, writer, profile);
        writer.Write(asset.GetUnparsedTail());
    }

}

public static partial class Physical
{
    /// <summary>
    /// An explicit interface used to interact with <see cref="FogAsset"/>'s underlying values.
    /// </summary>
    public interface IFogAsset : IBaseAsset
    {
        /// <summary>
        /// Unknown.
        /// </summary>
        byte FogType { get; set; }
    }
}
