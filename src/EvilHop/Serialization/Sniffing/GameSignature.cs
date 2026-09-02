using EvilHop.Blocks;
using EvilHop.Common;
using System.Text;

namespace EvilHop.Serialization.Sniffing;

/// <summary>The scoring signature for one <see cref="GameVersion"/> - what its real archives look like.</summary>
/// <param name="ValidClientVersions">Every <see cref="ClientVersion"/> observed for this game.</param>
/// <param name="PlatShape">This game's expected <c>PLAT</c> block shape.</param>
/// <param name="AssetTypeMarkers">
/// Asset types common in this game's real archives, used to score how well an archive's observed
/// <see cref="SniffSignals.AssetTypes"/> fit. Not guaranteed absent from every other candidate - only
/// that they're common enough in this game's own archives to be a useful per-file signal.
/// <see langword="null"/> for a game with no useful markers (N100F, Ratatouille).
/// </param>
/// <param name="CreatedRangeStart">The earliest <see cref="SniffSignals.Created"/> observed for this game, inclusive.</param>
/// <param name="CreatedRangeEnd">The latest <see cref="SniffSignals.Created"/> observed for this game, inclusive.</param>
internal readonly record struct GameSignature(
    ClientVersion[] ValidClientVersions,
    PlatShape PlatShape,
    string[]? AssetTypeMarkers,
    DateTimeOffset CreatedRangeStart,
    DateTimeOffset CreatedRangeEnd);

/// <summary>The shape of a <c>PLAT</c> block's strings, keyed off how many strings it has.</summary>
internal enum PlatShape
{
    None,
    FourString,
    FiveString
}

/// <summary>
/// Per-game <see cref="GameSignature"/>s, re-verified against <c>corpus/*.json</c>.
/// </summary>
internal static class GameSignatures
{
    /// <summary>Returns <paramref name="game"/>'s scoring signature.</summary>
    public static GameSignature For(GameVersion game) => ByGame[game];

    private static string FourCC(AssetType type) => Encoding.ASCII.GetString(
        [(byte)((uint)type >> 24), (byte)((uint)type >> 16), (byte)((uint)type >> 8), (byte)type]);

    private static DateTimeOffset Day(int year, int month, int day, int hour = 0, int minute = 0, int second = 0) =>
        new(year, month, day, hour, minute, second, TimeSpan.Zero);

    private static readonly Dictionary<GameVersion, GameSignature> ByGame = new()
    {
        [GameVersion.N100F] = new(
            [ClientVersion.N100FPrototype, ClientVersion.N100FRelease],
            PlatShape.None,
            null,
            Day(2001, 6, 11), Day(2003, 7, 29, 23, 59, 59)),
        [GameVersion.BFBB] = new(
            [ClientVersion.Default],
            PlatShape.FiveString,
            [FourCC(AssetType.DestructibleObject), FourCC(AssetType.SoundFX), FourCC(AssetType.SimpleShadowTable),
             FourCC(AssetType.UI), FourCC(AssetType.UIFont), FourCC(AssetType.VillainProperties)],
            Day(2003, 2, 21), Day(2003, 11, 24, 23, 59, 59)),
        [GameVersion.Incredibles] = new(
            [ClientVersion.Default],
            PlatShape.FourString,
            [FourCC(AssetType.AttackTable), FourCC(AssetType.DashTrack), FourCC(AssetType.Duplicator),
             FourCC(AssetType.GrassMesh), FourCC(AssetType.OneLiner), FourCC(AssetType.SlideProperty),
             FourCC(AssetType.SceneSettings), FourCC(AssetType.ZipLine)],
            Day(2004, 7, 20), Day(2004, 10, 14, 23, 59, 59)),
        [GameVersion.TSSM] = new(
            [ClientVersion.Default],
            PlatShape.FourString,
            [FourCC(AssetType.DiscoFloor), FourCC(AssetType.ElectricArcGenerator), FourCC(AssetType.JawDataTable),
             FourCC(AssetType.Pickup), FourCC(AssetType.ParticleEmitter), FourCC(AssetType.ParticleEmitterProperty),
             FourCC(AssetType.ParticleSystem)],
            Day(2004, 9, 1), Day(2004, 12, 6, 23, 59, 59)),
        [GameVersion.ROTU] = new(
            [ClientVersion.Default],
            PlatShape.FourString,
            [FourCC(AssetType.Hangable), FourCC(AssetType.Volume)],
            Day(2005, 8, 26), Day(2005, 12, 7, 23, 59, 59)),
        [GameVersion.Ratatouille] = new(
            [ClientVersion.Default],
            PlatShape.FourString,
            null,
            Day(2006, 1, 11), Day(2006, 1, 11, 23, 59, 59))
    };
}
