namespace EvilHop.Common;

/// <summary>
/// The platform identifiers a <c>PLAT</c> block's first string uses.
/// </summary>
internal static class PlatformIds
{
    /// <summary>
    /// The <see cref="Platform"/> named by <paramref name="id"/>, or <see langword="null"/> when it
    /// names none EvilHop knows.
    /// </summary>
    public static Platform? Parse(string id) => id switch
    {
        "GC" => Platform.GameCube,
        "XB" or "BX" => Platform.Xbox,
        "P2" or "PS2" => Platform.PlayStation2,
        _ => null
    };
}
