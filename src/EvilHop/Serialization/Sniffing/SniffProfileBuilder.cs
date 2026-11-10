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
    /// Builds <paramref name="game"/>'s default profile, overridden with whatever
    /// <paramref name="signals"/> imply about <see cref="FormatProfile.Platform"/>,
    /// <see cref="FormatProfile.StreamDataHasPaddingField"/>,
    /// <see cref="FormatProfile.EntityHasExtendedFields"/>,
    /// <see cref="FormatProfile.LinkHasExtendedFields"/>,
    /// <see cref="FormatProfile.TriggerHasDirectionAndFlags"/>,
    /// <see cref="FormatProfile.CameraHasExtendedFields"/>,
    /// <see cref="FormatProfile.AnimationTableHasStateSpeed"/>,
    /// <see cref="FormatProfile.EnvironmentHasExtendedFields"/>, and
    /// <see cref="FormatProfile.NPCHasExtendedFields"/>.
    /// </summary>
    public static FormatProfile Build(GameVersion game, SniffSignals signals) =>
        Serializer.DefaultProfileFor(game) with
        {
            Platform = DerivePlatform(signals).Platform,
            StreamDataHasPaddingField = signals.DpakPaddingObserved ?? (signals.ClientVersion != ClientVersion.N100FPrototype),
            EntityHasExtendedFields = signals.ClientVersion != ClientVersion.N100FPrototype,
            LinkHasExtendedFields = signals.ClientVersion != ClientVersion.N100FPrototype,
            TriggerHasDirectionAndFlags = signals.ClientVersion != ClientVersion.N100FPrototype,
            CameraHasExtendedFields = signals.ClientVersion != ClientVersion.N100FPrototype,
            AnimationTableHasStateSpeed = signals.ClientVersion != ClientVersion.N100FPrototype,
            EnvironmentHasExtendedFields = signals.ClientVersion != ClientVersion.N100FPrototype,
            NPCHasExtendedFields = signals.ClientVersion != ClientVersion.N100FPrototype
        };

    /// <summary>
    /// The platform <paramref name="signals"/> name, and which evidence named it. The <c>PLAT</c>
    /// block's first string wins; when it names no known platform, <see cref="PackFlags.PlatformMask"/>
    /// must hold exactly one platform bit. Anything else (every N100F build, which has neither, and
    /// TSSM archives without a <c>PLAT</c> block) yields <see langword="null"/> and
    /// <see cref="PlatformSource.None"/>.
    /// </summary>
    public static (Platform? Platform, PlatformSource Source) DerivePlatform(SniffSignals signals)
    {
        if (signals.PlatformStrings.Count > 0 && PlatformIds.Parse(signals.PlatformStrings[0]) is { } stamped)
            return (stamped, PlatformSource.PlatStamp);

        return ((signals.Flags ?? 0) & PackFlags.PlatformMask) switch
        {
            PackFlags.GameCube => (Platform.GameCube, PlatformSource.PackFlags),
            PackFlags.Xbox => (Platform.Xbox, PlatformSource.PackFlags),
            PackFlags.PlayStation2 => (Platform.PlayStation2, PlatformSource.PackFlags),
            _ => (null, PlatformSource.None)
        };
    }
}
