using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>
/// A graphic drawn on top of the scene, capable of accepting user input. Displays either a 2D
/// <see cref="AssetType.Texture"/> or a 3D <see cref="AssetType.Model"/>, but never both.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="EntityAsset.Angle"/> and <see cref="EntityAsset.Scale"/> only apply to a
/// <see cref="UIAsset"/> displaying a model; one displaying <see cref="TextureId"/> ignores them.
/// <see cref="EntityAsset.Position"/> is in screen space: X/Y are pixels measured from the top-left
/// corner, and Z is the draw order - a <see cref="UIAsset"/> with a higher Z draws behind one with a
/// lower Z.
/// </para>
/// <seealso href="https://heavyironmodding.org/wiki/UI">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class UIAsset() : EntityAsset(AssetType.UI, baseType: 0x20), IHasSurface, IHasModel, IHasAnimList
{
    /// <summary>
    /// Behavior flags for this <see cref="UIAsset"/>.
    /// </summary>
    public UIFlags Flags { get; set; }

    /// <summary>
    /// The width, in pixels, this <see cref="UIAsset"/> is drawn at.
    /// </summary>
    public ushort Width { get; set; }

    /// <summary>
    /// The height, in pixels, this <see cref="UIAsset"/> is drawn at.
    /// </summary>
    public ushort Height { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the <see cref="AssetType.Texture"/> this <see cref="UIAsset"/>
    /// draws, if any. Takes priority over <see cref="IHasModel.ModelId"/> when set.
    /// </summary>
    public AssetId TextureId { get; set; }

    /// <summary>The texture coordinate mapped to the top-left corner.</summary>
    public Vector2 TopLeftUV { get; set; }

    /// <summary>The texture coordinate mapped to the top-right corner.</summary>
    public Vector2 TopRightUV { get; set; }

    /// <summary>The texture coordinate mapped to the bottom-right corner.</summary>
    public Vector2 BottomRightUV { get; set; }

    /// <summary>The texture coordinate mapped to the bottom-left corner.</summary>
    public Vector2 BottomLeftUV { get; set; }

    AssetId IHasSurface.SurfaceId { get => Physical.SurfaceId; set => Physical.SurfaceId = value; }
    AssetId IHasModel.ModelId { get => Physical.ModelId; set => Physical.ModelId = value; }
    AssetId IHasAnimList.AnimListId { get => Physical.AnimListId; set => Physical.AnimListId = value; }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.UI"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.N100F,
        GameVersion.BFBB,
    };

    internal static UIAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new UIAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);
        EntityAssetPrefix.Read(asset, reader, profile);

        asset.Flags = (UIFlags)reader.ReadUInt32();
        asset.Width = reader.ReadUInt16();
        asset.Height = reader.ReadUInt16();
        asset.TextureId = reader.ReadAssetId();
        asset.TopLeftUV = reader.ReadVector2();
        asset.TopRightUV = reader.ReadVector2();
        asset.BottomRightUV = reader.ReadVector2();
        asset.BottomLeftUV = reader.ReadVector2();

        for (var i = 0; i < asset.Physical.LinkCount; i++)
            asset.Links.Add(Link.Read(reader, profile));
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(UIAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);
        EntityAssetPrefix.Write(asset, writer, profile);

        writer.Write((uint)asset.Flags);
        writer.Write(asset.Width);
        writer.Write(asset.Height);
        writer.Write(asset.TextureId);
        writer.Write(asset.TopLeftUV);
        writer.Write(asset.TopRightUV);
        writer.Write(asset.BottomRightUV);
        writer.Write(asset.BottomLeftUV);

        foreach (var link in asset.Links)
            Link.Write(link, writer, profile);
        writer.Write(asset.GetUnparsedTail());
    }
}

/// <summary>
/// Flags controlling UI visibility, focus state, rendering, and interaction behavior.
/// </summary>
[Flags]
public enum UIFlags : uint
{
    /// <summary>
    /// No flags are set.
    /// </summary>
    None = 0,
    /// <summary>
    /// Grants this UI asset input priority while focused.
    /// </summary>
    InputPriority = 1 << 0,
    /// <summary>
    /// The <see cref="UIAsset"/> starts selected.
    /// </summary>
    Selected = 1 << 1,
    /// <summary>
    /// The <see cref="UIAsset"/> renders itself directly, as a 2D sprite or model, instead of using
    /// the default entity renderer.
    /// </summary>
    RendersDirectly = 1 << 2,
    /// <summary>
    /// The <see cref="UIAsset"/> starts focused.
    /// </summary>
    Focused = 1 << 3,
    /// <summary>
    /// The <see cref="UIAsset"/> becomes visible automatically when it gains focus.
    /// </summary>
    ShowOnFocus = 1 << 4,
    /// <summary>
    /// The <see cref="UIAsset"/> becomes invisible automatically when it loses focus.
    /// </summary>
    HideOnUnfocus = 1 << 5,
}
