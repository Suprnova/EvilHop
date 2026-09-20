using EvilHop.Common;
using EvilHop.Primitives;
using System.Collections.ObjectModel;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>
/// The end-credits sequence played after completing the game: a set of independently-scrolling
/// <see cref="CreditsSection"/>s, optionally encrypted on disk.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/CRDT">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed partial class CreditsAsset() : Asset(AssetType.Credits), IPhysicalCreditsAsset
{
    private const int HeaderSize = 24;

    /// <summary>
    /// Whether this <see cref="CreditsAsset"/> is stored encrypted on disk.
    /// </summary>
    public bool IsEncrypted
    {
        get => Physical.State == CreditsState.Encrypted;
        set => Physical.State = value ? CreditsState.Encrypted : CreditsState.NotEncrypted;
    }

    /// <summary>
    /// The credits sequence's total duration, in seconds. Loops back to the start once elapsed.
    /// </summary>
    public float Duration { get; set; }

    /// <summary>
    /// This <see cref="CreditsAsset"/>'s independently-scrolling sections, shown one after another.
    /// </summary>
    public Collection<CreditsSection> Sections { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalCreditsAsset Physical => this;

    private uint _magic = 0xBEEEEEEF;
    uint IPhysicalCreditsAsset.Magic { get => _magic; set => _magic = value; }

    private uint _version;
    uint IPhysicalCreditsAsset.Version { get => _version; set => _version = value; }

    private AssetId _creditsId;
    AssetId IPhysicalCreditsAsset.CreditsId { get => _creditsId; set => _creditsId = value; }

    private CreditsState _state;
    CreditsState IPhysicalCreditsAsset.State { get => _state; set => _state = value; }

    private uint? _overriddenTotalSize;
    uint IPhysicalCreditsAsset.TotalSize
    {
        get => _overriddenTotalSize ?? ComputedTotalSize;
        set => _overriddenTotalSize = value == ComputedTotalSize ? null : value;
    }

    private uint ComputedTotalSize =>
        (uint)(HeaderSize + Sections.Sum(SectionByteLength) + GetUnparsedTail().Length);

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.Credits"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.BFBB,
        GameVersion.TSSM,
        GameVersion.Incredibles,
        GameVersion.ROTU,
        GameVersion.Ratatouille,
    };
}

/// <summary>
/// An explicit interface used to interact with <see cref="CreditsAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalCreditsAsset : IPhysicalAsset
{
    /// <summary>
    /// A magic number used to validate the payload.
    /// </summary>
    uint Magic { get; set; }

    /// <summary>
    /// The credits format version. 256 (1.0) in <see cref="GameVersion.BFBB"/> and
    /// <see cref="GameVersion.Incredibles"/>; 512 (2.0) from <see cref="GameVersion.TSSM"/> onward.
    /// </summary>
    uint Version { get; set; }

    /// <summary>
    /// Unknown.
    /// </summary>
    AssetId CreditsId { get; set; }

    /// <summary>
    /// Whether the body following this header is encrypted.
    /// </summary>
    CreditsState State { get; set; }

    /// <summary>
    /// The total size, in bytes, of this <see cref="CreditsAsset"/>'s entire on-disk representation,
    /// including this header.
    /// </summary>
    /// <remarks>
    /// When disagreements with the actual encoded size exist, this field wins during serialization.
    /// </remarks>
    uint TotalSize { get; set; }
}

/// <summary>
/// Defines the playback and display state of a credits entry.
/// </summary>
public enum CreditsState : uint
{
    /// <summary>The body following the header is stored as-is.</summary>
    NotEncrypted = 1,
    /// <summary>The body following the header is encrypted and must be decrypted before use.</summary>
    Encrypted = 3,
}

/// <summary>
/// One independently-scrolling block of a <see cref="CreditsAsset"/>: a set of reusable text/texture
/// presets, and the timed <see cref="CreditsHunk"/>s that use them.
/// </summary>
public sealed class CreditsSection
{
    /// <summary>
    /// This section's duration, in seconds.
    /// </summary>
    public float Duration { get; set; }

    /// <summary>
    /// Unknown. Alternates between 0 and 1 across a <see cref="CreditsAsset"/>'s sections.
    /// </summary>
    public uint Flags { get; set; }

    /// <summary>
    /// The scroll position this section starts at, as a percentage (0 to 1) of the screen - 0 is the
    /// top, 1 is the bottom.
    /// </summary>
    public Vector2 Start { get; set; }

    /// <summary>
    /// The scroll position this section ends at, as a percentage (0 to 1) of the screen - 0 is the
    /// top, 1 is the bottom.
    /// </summary>
    public Vector2 End { get; set; }

    /// <summary>
    /// The rate this section scrolls at.
    /// </summary>
    public float ScrollRate { get; set; }

    /// <summary>
    /// Unknown.
    /// </summary>
    public float Lifetime { get; set; }

    /// <summary>
    /// The point, from 0 (start) to 1 (end), at which this section's fade-in begins.
    /// </summary>
    public float FadeInStart { get; set; }

    /// <summary>
    /// The point, from 0 (start) to 1 (end), at which this section's fade-in ends.
    /// </summary>
    public float FadeInEnd { get; set; }

    /// <summary>
    /// The point, from 0 (start) to 1 (end), at which this section's fade-out begins.
    /// </summary>
    public float FadeOutStart { get; set; }

    /// <summary>
    /// The point, from 0 (start) to 1 (end), at which this section's fade-out ends.
    /// </summary>
    public float FadeOutEnd { get; set; }

    /// <summary>
    /// This section's reusable text and texture styles, referenced by position from
    /// <see cref="Hunks"/>.
    /// </summary>
    public Collection<CreditsPreset> Presets { get; } = [];

    /// <summary>
    /// This section's timed lines of credits text.
    /// </summary>
    public Collection<CreditsHunk> Hunks { get; } = [];
}

/// <summary>
/// One reusable text or texture style, referenced by position from a <see cref="CreditsSection"/>'s
/// <see cref="CreditsHunk"/>s.
/// </summary>
public sealed class CreditsPreset
{
    /// <summary>
    /// An index recorded alongside this preset. <see cref="CreditsHunk"/>s reference a preset by its
    /// position within <see cref="CreditsSection.Presets"/>, not by this value.
    /// </summary>
    public ushort Index { get; set; }

    /// <summary>
    /// How this preset's <see cref="Textboxes"/> or <see cref="Textures"/> are laid out.
    /// </summary>
    public CreditsPresetAlignment Alignment { get; set; }

    /// <summary>
    /// Unknown.
    /// </summary>
    public float Delay { get; set; }

    /// <summary>
    /// The gap between the two <see cref="Textboxes"/>, for a <see cref="CreditsPresetAlignment"/>
    /// that shows both at once.
    /// </summary>
    public float InnerSpacing { get; set; }

    /// <summary>
    /// This preset's text styles. Populated when <see cref="Alignment"/> is not
    /// <see cref="CreditsPresetAlignment.Texture"/>, empty otherwise.
    /// </summary>
    public Collection<CreditsTextbox> Textboxes { get; } = [];

    /// <summary>
    /// This preset's texture. Populated when <see cref="Alignment"/> is
    /// <see cref="CreditsPresetAlignment.Texture"/>, empty otherwise.
    /// </summary>
    public Collection<CreditsTexture> Textures { get; } = [];
}

// TODO: names for <see cref="Center"/>, <see cref="Left"/>, <see cref="Right"/>, and
// <see cref="Inner"/> are inferred from a dead-stripped debug string list's declaration order
// (<c>CM_ALIGN_CENTER</c>, <c>CM_ALIGN_LEFT</c>, <c>CM_ALIGN_RIGHT</c>, <c>CM_ALIGN_INNER</c>,
// <c>CM_ALIGN_TEXTURE</c>), not confirmed against their numeric values directly. Only
// <see cref="Center"/> and <see cref="Texture"/> are confirmed by the render switch itself; only
// <see cref="Center"/>, <see cref="Inner"/>, and <see cref="Texture"/> are ever observed in the
// corpus.

/// <summary>
/// Specifies text alignment and layout positioning for credits lines.
/// </summary>
public enum CreditsPresetAlignment : ushort
{
    /// <summary>A single, centered <see cref="CreditsTextbox"/>.</summary>
    Center = 0,
    /// <summary>Two <see cref="CreditsTextbox"/>s, both left-aligned.</summary>
    Left = 1,
    /// <summary>Two <see cref="CreditsTextbox"/>s, both right-aligned.</summary>
    Right = 2,
    /// <summary>Two <see cref="CreditsTextbox"/>s, facing each other across the gap between them.</summary>
    Inner = 3,
    /// <summary>A single <see cref="CreditsTexture"/>.</summary>
    Texture = 4,
}

/// <summary>
/// One text style: a font plus the color, character size, spacing, and box size text is rendered
/// with.
/// </summary>
public sealed class CreditsTextbox
{
    /// <summary>
    /// Unknown.
    /// </summary>
    public uint Font { get; set; }

    /// <summary>
    /// The text's color.
    /// </summary>
    public Rgba Color { get; set; }

    /// <summary>
    /// The character width and height, in pixels.
    /// </summary>
    public Vector2 CharSize { get; set; }

    /// <summary>
    /// The spacing between characters.
    /// </summary>
    public Vector2 CharSpacing { get; set; }

    /// <summary>
    /// The text box's maximum width and height, as a percentage (0 to 1) of the screen.
    /// </summary>
    public Vector2 Size { get; set; }
}

/// <summary>
/// One texture drawn at a fixed screen position, in place of scrolling text.
/// </summary>
public sealed class CreditsTexture
{
    /// <summary>
    /// The <see cref="AssetType.Texture"/> to draw.
    /// </summary>
    public AssetId TextureId { get; set; }

    /// <summary>
    /// The texture's color.
    /// </summary>
    public Rgba Color { get; set; }

    /// <summary>
    /// The texture's position, as a percentage (0 to 1) of the screen.
    /// </summary>
    public Vector2 Position { get; set; }

    /// <summary>
    /// The texture's width and height, as a percentage (0 to 1) of the screen.
    /// </summary>
    public Vector2 Size { get; set; }

    /// <summary>
    /// Unknown.
    /// </summary>
    public uint Handle { get; set; }

    /// <summary>
    /// Unknown.
    /// </summary>
    public uint Padding { get; set; }
}

/// <summary>
/// One line (or pair of lines) of scrolling credits text, shown between <see cref="StartTime"/> and
/// <see cref="EndTime"/> using one of its <see cref="CreditsSection"/>'s presets.
/// </summary>
public sealed class CreditsHunk
{
    /// <summary>
    /// The position, within the owning <see cref="CreditsSection.Presets"/>, of the preset this hunk
    /// is shown with.
    /// </summary>
    public int PresetIndex { get; set; }

    /// <summary>
    /// The time, in seconds, at which this hunk starts being shown.
    /// </summary>
    public float StartTime { get; set; }

    /// <summary>
    /// The time, in seconds, at which this hunk stops being shown.
    /// </summary>
    public float EndTime { get; set; }

    /// <summary>
    /// The text shown in the preset's first <see cref="CreditsTextbox"/>, or <see langword="null"/>
    /// for a <see cref="CreditsPresetAlignment.Texture"/> preset.
    /// </summary>
    public string? Text1 { get; set; }

    /// <summary>
    /// The text shown in the preset's second <see cref="CreditsTextbox"/>, for a
    /// <see cref="CreditsPresetAlignment"/> that shows two at once.
    /// </summary>
    public string? Text2 { get; set; }
}
