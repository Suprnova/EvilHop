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
