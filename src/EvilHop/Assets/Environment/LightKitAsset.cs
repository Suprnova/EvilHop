using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>
/// A set of <see cref="LightKitLight"/>s applied together to a <see cref="AssetType.JSP"/> or the
/// entities placed within it.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/LKIT">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class LightKitAsset() : Asset(AssetType.LightKit), IPhysicalLightKitAsset
{
    /// <summary>
    /// The <see cref="AssetType.Group"/> of entities this light kit is applied to, in addition to
    /// whichever single object referenced it directly, if any.
    /// </summary>
    public AssetId GroupId { get; set; }

    /// <summary>
    /// The kit's lights.
    /// </summary>
    public Collection<LightKitLight> Lights { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalLightKitAsset Physical => this;

    private uint _magic = 0x54494B4C; // "TIKL"
    uint IPhysicalLightKitAsset.Magic { get => _magic; set => _magic = value; }

    private uint? _overriddenLightCount;
    uint IPhysicalLightKitAsset.LightCount
    {
        get => _overriddenLightCount ?? (uint)Lights.Count;
        set => _overriddenLightCount = value == (uint)Lights.Count ? null : value;
    }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.LightKit"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.BFBB,
        GameVersion.TSSM,
        GameVersion.Incredibles,
        GameVersion.ROTU,
        GameVersion.Ratatouille,
    };

    internal static LightKitAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new LightKitAsset();
        AssetFields.Populate(asset, header, debug);

        asset.Physical.Magic = reader.ReadUInt32();
        asset.GroupId = reader.ReadAssetId();
        uint lightCount = reader.ReadUInt32();
        reader.ReadUInt32(); // runtime-resolved lightList, always 0

        if (profile.Game is GameVersion.ROTU or GameVersion.Ratatouille)
            reader.ReadUInt32(); // "blended" - always 0xCDCDCDCD (uninitialized) on disk, reset to false at load

        for (int i = 0; i < lightCount; i++)
            asset.Lights.Add(LightKitLight.Read(reader, profile));

        asset.Physical.LightCount = lightCount;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(LightKitAsset asset, EndianWriter writer, FormatProfile profile)
    {
        writer.Write(asset.Physical.Magic);
        writer.Write(asset.GroupId);
        writer.Write(asset.Physical.LightCount);
        writer.Write(0u); // runtime-resolved

        if (profile.Game is GameVersion.ROTU or GameVersion.Ratatouille)
            writer.Write(0xCDCDCDCDu); // "blended"

        foreach (var light in asset.Lights)
            LightKitLight.Write(light, writer, profile);

        writer.Write(asset.GetUnparsedTail());
    }
}

/// <summary>
/// An explicit interface used to interact with <see cref="LightKitAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalLightKitAsset : IPhysicalAsset
{
    /// <summary>
    /// A four-character magic number.
    /// </summary>
    uint Magic { get; set; }

    /// <summary>
    /// The number of <see cref="LightKitAsset.Lights"/> stored for this asset, read directly from
    /// its header.
    /// </summary>
    /// <remarks>
    /// When disagreements with <see cref="LightKitAsset.Lights"/>.Count exist, this field wins during
    /// serialization.
    /// </remarks>
    uint LightCount { get; set; }
}
