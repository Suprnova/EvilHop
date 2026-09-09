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
    /// <paramref name="path"/> declares for itself, or unchanged when it isn't recognizable.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every game's <c>DefaultProfile</c> targets <see cref="Platform.GameCube"/> and
    /// <c>--serializer</c> names a game, not a build, so a corpus root's Xbox and PlayStation 2
    /// archives would otherwise all be read as GameCube ones. Nothing in the block layer reads
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
        var sniffed = Serializer.Sniff(stream).Profile;
        return sniffed is null ? profile : profile with { Platform = sniffed.Platform };
    }

    /// <summary>
    /// Returns <paramref name="game"/>'s default <see cref="FormatProfile"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Thrown when no serializer exists for that game yet.</exception>
    public static FormatProfile DefaultProfileFor(GameVersion game) => game switch
    {
        GameVersion.N100F => N100FSerializer.DefaultProfile,
        GameVersion.BFBB => BFBBSerializer.DefaultProfile,
        GameVersion.Incredibles => IncrediblesSerializer.DefaultProfile,
        GameVersion.TSSM => TSSMSerializer.DefaultProfile,
        GameVersion.ROTU => ROTUSerializer.DefaultProfile,
        GameVersion.Ratatouille => RatatouilleSerializer.DefaultProfile,
        _ => throw new NotSupportedException(
            $"No serializer exists for {game} yet. Available: {GameVersion.N100F}, {GameVersion.BFBB}, {GameVersion.Incredibles}, {GameVersion.TSSM}, {GameVersion.ROTU}, {GameVersion.Ratatouille}.")
    };
}
