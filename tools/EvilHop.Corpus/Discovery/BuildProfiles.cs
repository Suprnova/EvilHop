using EvilHop.Common;
using EvilHop.Serialization;

namespace EvilHop.Corpus.Discovery;

/// <summary>
/// Resolves the <see cref="GameVersion"/> and <see cref="Platform"/> a build's directory implies,
/// and constructs the <see cref="Serializer"/> to read it with. Convention only - the manifest
/// overrides this per-archive where convention doesn't hold.
/// </summary>
public static class BuildProfiles
{
    private static readonly IReadOnlyDictionary<string, GameVersion> GameByDirectoryName =
        new Dictionary<string, GameVersion>(StringComparer.OrdinalIgnoreCase)
        {
            ["bfbb"] = GameVersion.BFBB,
            ["incredibles"] = GameVersion.Incredibles,
            ["n100f"] = GameVersion.N100F,
            ["rat"] = GameVersion.Ratatouille,
            ["rotu"] = GameVersion.ROTU,
            ["tssm"] = GameVersion.TSSM
        };

    private static readonly IReadOnlyDictionary<string, Platform> PlatformByDirectoryName =
        new Dictionary<string, Platform>(StringComparer.OrdinalIgnoreCase)
        {
            ["GC"] = Platform.GameCube,
            ["PS2"] = Platform.PlayStation2,
            ["XBOX"] = Platform.Xbox
        };

    /// <summary>
    /// Resolves the game a build directory implies, from its path's first segment.
    /// </summary>
    /// <param name="buildDirectory">The build's directory, relative to the artifact root.</param>
    /// <returns>The implied <see cref="GameVersion"/>.</returns>
    /// <exception cref="InvalidOperationException">No segment of the path names a known game.</exception>
    public static GameVersion GameFor(string buildDirectory)
    {
        string first = Segments(buildDirectory).First();
        return GameByDirectoryName.TryGetValue(first, out var game)
            ? game
            : throw new InvalidOperationException($"'{buildDirectory}' doesn't start with a known game directory.");
    }

    /// <summary>
    /// Resolves the platform a build directory implies, from a path segment naming a known console.
    /// </summary>
    /// <param name="buildDirectory">The build's directory, relative to the artifact root.</param>
    /// <returns>The implied <see cref="Platform"/>.</returns>
    /// <exception cref="InvalidOperationException">No segment of the path names a known platform.</exception>
    public static Platform PlatformFor(string buildDirectory)
    {
        foreach (string segment in Segments(buildDirectory))
            if (PlatformByDirectoryName.TryGetValue(segment, out var platform))
                return platform;

        throw new InvalidOperationException($"'{buildDirectory}' doesn't contain a known platform directory.");
    }

    /// <summary>
    /// The <see cref="FormatProfile"/> to read a build with, from its implied game and platform.
    /// </summary>
    /// <param name="game">The game to read the archive as.</param>
    /// <param name="platform">The console the archive targets.</param>
    /// <returns>That game's default profile, adjusted for <paramref name="platform"/>.</returns>
    public static FormatProfile ProfileFor(GameVersion game, Platform platform) => DefaultProfileFor(game) with { Platform = platform };

    /// <summary>
    /// Builds the <see cref="Serializer"/> to read an archive with.
    /// </summary>
    /// <param name="game">The game to read the archive as.</param>
    /// <param name="profile">The profile to construct the serializer with.</param>
    public static Serializer SerializerFor(GameVersion game, FormatProfile profile) => game switch
    {
        GameVersion.BFBB => new BFBBSerializer(profile),
        GameVersion.Incredibles => new IncrediblesSerializer(profile),
        GameVersion.N100F => new N100FSerializer(profile),
        GameVersion.ROTU => new ROTUSerializer(profile),
        GameVersion.Ratatouille => new RatatouilleSerializer(profile),
        GameVersion.TSSM => new TSSMSerializer(profile),
        _ => throw new ArgumentOutOfRangeException(nameof(game), game, "Unknown game.")
    };

    private static FormatProfile DefaultProfileFor(GameVersion game) => game switch
    {
        GameVersion.BFBB => BFBBSerializer.DefaultProfile,
        GameVersion.Incredibles => IncrediblesSerializer.DefaultProfile,
        GameVersion.N100F => N100FSerializer.DefaultProfile,
        GameVersion.ROTU => ROTUSerializer.DefaultProfile,
        GameVersion.Ratatouille => RatatouilleSerializer.DefaultProfile,
        GameVersion.TSSM => TSSMSerializer.DefaultProfile,
        _ => throw new ArgumentOutOfRangeException(nameof(game), game, "Unknown game.")
    };

    private static string[] Segments(string path) => path.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
}
