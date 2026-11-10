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

    /// <summary>
    /// The identifier a <c>PLAT</c> block stamps for <paramref name="platform"/>: the one every
    /// <see cref="GameVersion.TSSM"/> archive on that platform carries.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="platform"/> isn't a defined value.</exception>
    public static string Id(Platform platform) => platform switch
    {
        Platform.GameCube => "GC",
        Platform.Xbox => "BX",
        Platform.PlayStation2 => "PS2",
        _ => throw new ArgumentOutOfRangeException(nameof(platform), platform, null)
    };
}
