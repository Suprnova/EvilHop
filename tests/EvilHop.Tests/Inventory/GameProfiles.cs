using EvilHop.Common;
using EvilHop.Serialization;

namespace EvilHop.Tests.Inventory;

/// <summary>
/// The <see cref="FormatProfile"/> each game's rules are replayed against. A rule's <c>Platforms</c>
/// and <c>Quirks</c> scoping is empty for every attribute declared so far, so one profile per game -
/// its default - is enough to decide whether a rule applies.
/// </summary>
internal static class GameProfiles
{
    /// <summary>The default <see cref="FormatProfile"/> for <paramref name="game"/>.</summary>
    /// <param name="game">The game to look up.</param>
    public static FormatProfile For(GameVersion game) => game switch
    {
        GameVersion.BFBB => BFBBSerializer.DefaultProfile,
        GameVersion.Incredibles => IncrediblesSerializer.DefaultProfile,
        GameVersion.N100F => N100FSerializer.DefaultProfile,
        GameVersion.ROTU => ROTUSerializer.DefaultProfile,
        GameVersion.Ratatouille => RatatouilleSerializer.DefaultProfile,
        GameVersion.TSSM => TSSMSerializer.DefaultProfile,
        _ => throw new ArgumentOutOfRangeException(nameof(game), game, "Unknown game.")
    };
}
