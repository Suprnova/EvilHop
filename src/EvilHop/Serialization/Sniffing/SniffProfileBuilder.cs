using EvilHop.Blocks;
using EvilHop.Common;

namespace EvilHop.Serialization.Sniffing;

/// <summary>
/// Builds the <see cref="FormatProfile"/> a <see cref="GameVersion"/> candidate implies, given the
/// observed <see cref="SniffSignals"/>.
/// </summary>
internal static class SniffProfileBuilder
{
    /// <summary>
    /// Builds <paramref name="game"/>'s <c>DefaultProfile</c>, overridden with whatever
    /// <paramref name="signals"/> imply about <see cref="FormatProfile.Platform"/> and
    /// <see cref="FormatProfile.StreamDataHasPaddingField"/>.
    /// </summary>
    public static FormatProfile Build(GameVersion game, SniffSignals signals) =>
        Serializer.DefaultProfileFor(game) with
        {
            Platform = DerivePlatform(signals.Flags, signals.PlatformStrings),
            StreamDataHasPaddingField = signals.DpakPaddingObserved ?? (signals.ClientVersion != ClientVersion.N100FPrototype)
        };

    /// <summary>
    /// Prefers <see cref="PackFlags.PlatformMask"/>'s exact bit match to <see cref="Platform"/>; if
    /// that's all-zero (true for every N100F build, and for BFBB's earliest prototype), falls back
    /// to <paramref name="platformStrings"/>'s first entry; defaults to <see cref="Platform.GameCube"/>
    /// otherwise, matching every <c>DefaultProfile</c>.
    /// </summary>
    private static Platform DerivePlatform(PackFlags? flags, IReadOnlyList<string> platformStrings)
    {
        var platformBits = (flags ?? 0) & PackFlags.PlatformMask;
        if (platformBits == PackFlags.GameCube) return Platform.GameCube;
        if (platformBits == PackFlags.Xbox) return Platform.Xbox;
        if (platformBits == PackFlags.PlayStation2) return Platform.PlayStation2;

        if (platformStrings.Count == 0) return Platform.GameCube;
        return platformStrings[0] switch
        {
            "GC" => Platform.GameCube,
            "XB" or "BX" => Platform.Xbox,
            "P2" or "PS2" => Platform.PlayStation2,
            _ => Platform.GameCube
        };
    }
}
