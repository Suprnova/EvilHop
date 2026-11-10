using EvilHop.Common;
using EvilHop.Serialization;

namespace EvilHop.Tests;

internal static class FormatProfileTestExtensions
{
    /// <summary>
    /// <paramref name="profile"/> stating <see cref="Platform.GameCube"/>, for tests that need an
    /// asset-capable profile and don't care which platform.
    /// </summary>
    public static FormatProfile OnGameCube(this FormatProfile profile) => profile with { Platform = Platform.GameCube };
}
