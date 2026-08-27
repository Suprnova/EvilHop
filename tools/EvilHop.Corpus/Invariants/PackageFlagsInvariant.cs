using EvilHop.Blocks;
using EvilHop.Corpus.Archives;
using System.Text.Json.Nodes;

namespace EvilHop.Corpus.Invariants;

/// <summary>
/// Every <see cref="PackageFlags"/> block carries the <see cref="PackFlags.Default"/> bits.
/// </summary>
internal sealed class PackageFlagsDefaultAlwaysPresentInvariant : IInvariant
{
    /// <inheritdoc/>
    public string Name => "packageFlagsDefaultAlwaysPresent";

    private readonly InvariantResult _result = new();

    /// <inheritdoc/>
    public void Check(ArchiveContext archive)
    {
        foreach (var flags in archive.AllBlocks.OfType<PackageFlags>())
        {
            _result.Record(flags.Flags.HasFlag(PackFlags.Default), () => new JsonObject
            {
                ["path"] = archive.RelativePath,
                ["flags"] = $"0x{(uint)flags.Flags:X8}",
                ["expected"] = $"0x{(uint)PackFlags.Default:X8}"
            });
        }
    }

    /// <inheritdoc/>
    public JsonObject ToJson() => _result.ToJson();
}

/// <summary>
/// The upper word of <see cref="PackageFlags.Flags"/> (bits 16-31) is coherent when set. When the
/// <see cref="PackFlags.Platform"/> bit is set, exactly one bit of each of
/// <see cref="PackFlags.PlatformMask"/>, <see cref="PackFlags.RegionMask"/>, and
/// <see cref="PackFlags.LanguageMask"/> is chosen, and the chosen bit must agree with the matching
/// <see cref="PackagePlatform"/> (PLAT) block - platform, region, and language each line up.
/// Otherwise (no <see cref="PackFlags.Platform"/> bit) the whole upper word is zero.
/// </summary>
internal sealed class PackageFlagsUpperWordInvariant : IInvariant
{
    /// <inheritdoc/>
    public string Name => "packageFlagsUpperWordMatchesPlatform";

    private readonly InvariantResult _result = new();

    private static readonly Dictionary<PackFlags, string> PlatformIdByBit = new()
    {
        [PackFlags.GameCube] = "GC",
        [PackFlags.Xbox] = "XB",
        [PackFlags.PlayStation2] = "P2"
    };

    private static readonly Dictionary<PackFlags, string> RegionByBit = new()
    {
        [PackFlags.NTSC] = "NTSC",
        [PackFlags.PAL] = "PAL"
    };

    private static readonly Dictionary<PackFlags, string> LanguageByBit = new()
    {
        [PackFlags.LanguageUSCommon] = "US Common",
        [PackFlags.LanguageUnitedKingdom] = "United Kingdom",
        [PackFlags.LanguageFrench] = "French",
        [PackFlags.LanguageGerman] = "German"
    };

    /// <inheritdoc/>
    public void Check(ArchiveContext archive)
    {
        foreach (var flags in archive.AllBlocks.OfType<PackageFlags>())
        {
            uint raw = (uint)flags.Flags;
            bool passed;
            string reason;
            if (flags.Flags.HasFlag(PackFlags.Platform))
            {
                var plat = archive.AllBlocks.OfType<PackagePlatform>().FirstOrDefault();
                (passed, reason) = CheckPlatformPresent(flags.Flags, plat);
            }
            else if (raw >> 16 != 0)
            {
                passed = false;
                reason = "no Platform bit set but the upper word is non-zero";
            }
            else
            {
                passed = true;
                reason = "";
            }

            _result.Record(passed, () => new JsonObject
            {
                ["path"] = archive.RelativePath,
                ["flags"] = $"0x{raw:X8}",
                ["reason"] = reason
            });
        }
    }

    private static (bool, string) CheckPlatformPresent(PackFlags flags, PackagePlatform? plat)
    {
        var (checkedPlatform, platformReason) = CheckExactlyOne(flags, PackFlags.PlatformMask, PlatformIdByBit, nameof(PackagePlatform.PlatformId));
        if (!checkedPlatform) return (false, platformReason);
        var (checkedRegion, regionReason) = CheckExactlyOne(flags, PackFlags.RegionMask, RegionByBit, nameof(PackagePlatform.Region));
        if (!checkedRegion) return (false, regionReason);
        var (checkedLanguage, languageReason) = CheckExactlyOne(flags, PackFlags.LanguageMask, LanguageByBit, nameof(PackagePlatform.Language));
        if (!checkedLanguage) return (false, languageReason);

        if (plat is null)
            return (false, "Platform bit set but no PLAT block present to agree with");

        string expectedPlatform = PlatformIdByBit[flags & PackFlags.PlatformMask];
        string expectedRegion = RegionByBit[flags & PackFlags.RegionMask];
        string expectedLanguage = LanguageByBit[flags & PackFlags.LanguageMask];

        bool matches = plat.PlatformId == expectedPlatform
            && plat.Region == expectedRegion
            && plat.Language == expectedLanguage;

        string reason = matches ? "" : $"PLAT block disagrees: expected platform '{expectedPlatform}', region '{expectedRegion}', language '{expectedLanguage}' but got '{plat.PlatformId}', '{plat.Region}', '{plat.Language}'";
        return (matches, reason);
    }

    private static (bool, string) CheckExactlyOne(
        PackFlags flags,
        PackFlags mask,
        Dictionary<PackFlags, string> expectedByBit,
        string field)
    {
        PackFlags bits = flags & mask;
        if (!IsSingleBit(bits))
            return (false, $"expected exactly one {mask} bit, found 0x{(uint)bits:X}");

        if (!expectedByBit.ContainsKey(bits))
            return (false, $"unknown {field} bit 0x{(uint)bits:X}");

        return (true, "");
    }

    private static bool IsSingleBit(PackFlags bits)
    {
        uint value = (uint)bits;
        return value != 0 && (value & (value - 1)) == 0;
    }

    /// <inheritdoc/>
    public JsonObject ToJson() => _result.ToJson();
}
