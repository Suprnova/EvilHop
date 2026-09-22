using EvilHop.Common;

namespace EvilHop.Assets;

public partial class UIFontAsset
{
    /// <summary>
    /// Flags controlling font alignment, backdrop rendering, text dimming, and dynamic bounds expansion.
    /// </summary>
    [Flags]
    public enum Behavior : ushort
    {
        /// <summary>
        /// No flags are set.
        /// </summary>
        None = 0,
        /// <summary>
        /// The text is center-aligned. <see cref="AlignRight"/> takes priority when both are set.
        /// </summary>
        AlignCenter = 1 << 1,
        /// <summary>
        /// The text is right-aligned, taking priority over <see cref="AlignCenter"/> when both are set.
        /// </summary>
        AlignRight = 1 << 2,
        /// <summary>
        /// A backdrop is drawn behind the text, colored by <see cref="BackdropColor"/>. Set
        /// and cleared at runtime by the <b>FontBackdropOn</b>/<b>FontBackdropOff</b> events.
        /// </summary>
        HasBackdrop = 1 << 3,
        /// <summary>
        /// Enables the <see cref="DimWhenUnselected"/>/<see cref="DimWhenUnfocused"/> opacity dimming.
        /// </summary>
        DimmingEnabled = 1 << 4,
        /// <summary>
        /// Halves the text's opacity while its <see cref="AssetType.UI"/> is not selected. Requires
        /// <see cref="DimmingEnabled"/>.
        /// </summary>
        DimWhenUnselected = 1 << 5,
        /// <summary>
        /// Halves the text's opacity while its <see cref="AssetType.UI"/> is not focused. Requires
        /// <see cref="DimmingEnabled"/>.
        /// </summary>
        DimWhenUnfocused = 1 << 6,
        /// <summary>
        /// When the text overflows <see cref="MaxHeight"/>, the text box grows upward,
        /// keeping its bottom edge fixed. Combined with <see cref="GrowDownward"/> for centered growth.
        /// </summary>
        GrowUpward = 1 << 10,
        /// <summary>
        /// When the text overflows <see cref="MaxHeight"/>, the text box grows downward,
        /// keeping its top edge fixed. Combined with <see cref="GrowUpward"/> for centered growth.
        /// </summary>
        GrowDownward = 1 << 11,
    }
}
