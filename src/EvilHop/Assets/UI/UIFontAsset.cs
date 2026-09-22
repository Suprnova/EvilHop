using EvilHop.Common;
using EvilHop.Primitives;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>
/// A text element drawn on top of the scene, capable of accepting user input. Shares
/// <see cref="AssetType.UI"/>'s placement, flags, and texture-mapping fields, and adds its own font,
/// color, and layout data.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="EntityAsset.Angle"/>, <see cref="EntityAsset.Scale"/>, and
/// <see cref="EntityAsset.ColorMultiplier"/> are all ignored. <see cref="EntityAsset.Position"/> is in
/// screen space: X/Y are pixels measured from the top-left corner, and Z is the draw order - a
/// <see cref="UIFontAsset"/> with a higher Z draws behind one with a lower Z. Never has a
/// <see cref="AssetType.Surface"/>, <see cref="AssetType.Model"/>, or <see cref="AssetType.Animation"/>.
/// </para>
/// <seealso href="https://heavyironmodding.org/wiki/UIFT">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed partial class UIFontAsset() : EntityAsset(AssetType.UIFont, baseType: 0x21)
{
    /// <summary>
    /// Behavior flags shared with <see cref="AssetType.UI"/>.
    /// </summary>
    public UIFlags Flags { get; set; }

    /// <summary>
    /// The width, in pixels, this <see cref="UIFontAsset"/> is drawn at.
    /// </summary>
    public ushort Width { get; set; }

    /// <summary>
    /// The height, in pixels, this <see cref="UIFontAsset"/> is drawn at.
    /// </summary>
    public ushort Height { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the <see cref="AssetType.Texture"/> this <see cref="UIFontAsset"/>
    /// draws, if any.
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

    /// <summary>
    /// Behavior flags specific to this <see cref="UIFontAsset"/>.
    /// </summary>
    public Behavior FontFlags { get; set; }

    /// <summary>
    /// Which built-in rendering mode this <see cref="UIFontAsset"/> uses.
    /// </summary>
    public FormattingMode Mode { get; set; }

    /// <summary>
    /// The font to render this <see cref="UIFontAsset"/>'s text with.
    /// </summary>
    public byte FontId { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the <see cref="AssetType.Text"/> asset this
    /// <see cref="UIFontAsset"/> displays, if any.
    /// </summary>
    public AssetId TextId { get; set; }

    /// <summary>
    /// The color of the backdrop drawn behind the text when <see cref="Behavior.HasBackdrop"/> is
    /// set.
    /// </summary>
    public Rgba BackdropColor { get; set; }

    /// <summary>
    /// The text's color.
    /// </summary>
    public Rgba Color { get; set; }

    /// <summary>The text bounds' inset from the top edge, in pixels.</summary>
    public short InsetTop { get; set; }

    /// <summary>The text bounds' inset from the bottom edge, in pixels.</summary>
    public short InsetBottom { get; set; }

    /// <summary>The text bounds' inset from the left edge, in pixels.</summary>
    public short InsetLeft { get; set; }

    /// <summary>The text bounds' inset from the right edge, in pixels.</summary>
    public short InsetRight { get; set; }

    /// <summary>The horizontal spacing between characters, in pixels.</summary>
    public short SpaceX { get; set; }

    /// <summary>
    /// Unknown.
    /// </summary>
    public short SpaceY { get; set; }

    /// <summary>The width of a single character, in pixels.</summary>
    public short CharacterWidth { get; set; }

    /// <summary>The height of a single character, in pixels.</summary>
    public short CharacterHeight { get; set; }

    /// <summary>
    /// The maximum height, in pixels, the text box grows to when <see cref="Behavior.GrowUpward"/>
    /// or <see cref="Behavior.GrowDownward"/> is set. Only present in <see cref="GameVersion.BFBB"/>.
    /// </summary>
    public uint MaxHeight { get; set; }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.UIFont"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.N100F,
        GameVersion.BFBB,
    };

    /// <summary>
    /// Specifies the built-in text rendering layout and formatting mode.
    /// </summary>
    public enum FormattingMode : byte
    {
        /// <summary>Unknown.</summary>
        Mode1 = 1,
        /// <summary>Unknown.</summary>
        Mode2 = 2,
        /// <summary>Unknown.</summary>
        Mode3 = 3,
        /// <summary>Unknown.</summary>
        Mode4 = 4,
        /// <summary>Unknown.</summary>
        Mode5 = 5,
        /// <summary>Unknown.</summary>
        Mode6 = 6,
    }
}
