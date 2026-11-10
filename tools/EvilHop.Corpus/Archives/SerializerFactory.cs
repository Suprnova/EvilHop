using EvilHop.Common;
using EvilHop.Serialization;

namespace EvilHop.Corpus.Archives;

/// <summary>
/// Resolves a <see cref="GameVersion"/> and its <see cref="FormatProfile"/> to a <see cref="Serializer"/> instance.
/// </summary>
internal static class SerializerFactory
{
    /// <summary>
    /// Creates the serializer for <paramref name="profile"/>'s <see cref="FormatProfile.Game"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Thrown when no serializer exists for that game yet.</exception>
    public static Serializer Create(FormatProfile profile) => profile.Game switch
    {
        GameVersion.N100F => new N100FSerializer(profile),
        GameVersion.BFBB => new BFBBSerializer(profile),
        GameVersion.Incredibles => new IncrediblesSerializer(profile),
        GameVersion.TSSM => new TSSMSerializer(profile),
        GameVersion.ROTU => new ROTUSerializer(profile),
        GameVersion.Ratatouille => new RatatouilleSerializer(profile),
        _ => throw new NotSupportedException(
            $"No serializer exists for {profile.Game} yet. Available: {GameVersion.N100F}, {GameVersion.BFBB}, {GameVersion.Incredibles}, {GameVersion.TSSM}, {GameVersion.ROTU}, {GameVersion.Ratatouille}.")
    };

    /// <summary>
    /// <paramref name="profile"/> with the <see cref="Platform"/> the archive at
    /// <paramref name="path"/> declares for itself, or <see cref="Platform.GameCube"/> when it isn't
    /// recognizable.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>--serializer</c> names a game, not a build, and every game's default profile states no
    /// platform, so a corpus root's archives would otherwise have no platform to read assets under. Nothing in the block layer reads
    /// <see cref="FormatProfile.Platform"/>; the asset layer takes both its endianness and its data
    /// alignment from it, so this only began to matter once <c>--round-trip</c> descended into assets.
    /// </para>
    /// <para>
    /// This can't speak for every archive. <see cref="GameVersion.N100F"/> leaves <c>PFLG</c>'s
    /// platform bits zero and ships no <c>PLAT</c> block to fall back on, and
    /// <see cref="Serializer.Sniff"/> reports that silence as <see cref="Platform.GameCube"/> rather
    /// than as "unknown" - so N100F's non-GameCube builds are named in <c>BuildProfiles.json</c>
    /// instead, which is applied after this and wins.
    /// </para>
    /// </remarks>
    public static FormatProfile WithSniffedPlatform(FormatProfile profile, string path)
    {
        using var stream = File.OpenRead(path);
        return profile with { Platform = Serializer.Sniff(stream).Profile?.Platform ?? Platform.GameCube };
    }

    /// <summary>
    /// Returns <paramref name="game"/>'s default <see cref="FormatProfile"/>, which states no platform.
    /// </summary>
    /// <exception cref="NotSupportedException">Thrown when no serializer exists for that game yet.</exception>
    public static FormatProfile DefaultProfileFor(GameVersion game) => Serializer.Create(game).Profile;
}
