using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;

namespace EvilHop.Assets;

/// <summary>
/// Defines data and fragment definitions for shrapnel spawned during gameplay, such as when
/// destructible objects break or effects explode.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/SHRP">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed partial class ShrapnelAsset() : Asset(AssetType.Shrapnel), Physical.IShrapnelAsset
{
    /// <summary>This shrapnel asset's fragments.</summary>
    public Collection<Fragment> Frags { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override Physical.IShrapnelAsset Physical => this;

    private int? _overriddenFragCount;
    int Physical.IShrapnelAsset.FragCount
    {
        get => _overriddenFragCount ?? Frags.Count;
        set => _overriddenFragCount = value == Frags.Count ? null : value;
    }

    private AssetId? _overriddenShrapnelId;
    AssetId Physical.IShrapnelAsset.ShrapnelId
    {
        get => _overriddenShrapnelId ?? Id;
        set => _overriddenShrapnelId = value == Id ? null : value;
    }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.Shrapnel"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.BFBB,
        GameVersion.TSSM,
        GameVersion.Incredibles,
        GameVersion.ROTU,
        GameVersion.Ratatouille,
    };

    internal static int GetFragSize(
        GameVersion game,
        FragmentKind type,
        bool hasExtendedFragFields = true,
        bool soundHasExtendedFields = true,
        bool projectileHasIntermediateFields = false) => type switch
        {
            FragmentKind.Shrapnel => 0x20,
            FragmentKind.Particle => game == GameVersion.BFBB ? (hasExtendedFragFields ? 0x1D4 : 0x1D0) : 0x1F4,
            FragmentKind.Projectile => game == GameVersion.BFBB
                ? (hasExtendedFragFields ? 0x90 : (projectileHasIntermediateFields ? 0x6C : 0x58))
                : (game is GameVersion.ROTU or GameVersion.Ratatouille ? 0x158 : 0x110),
            FragmentKind.Lightning => game == GameVersion.BFBB ? 0x68 : 0x70,
            FragmentKind.Sound => game == GameVersion.BFBB ? (soundHasExtendedFields ? 0x4C : 0x40) : 0x44,
            FragmentKind.Shockwave => 0x54,
            FragmentKind.Explosion when game != GameVersion.BFBB => 0x48,
            FragmentKind.Distortion when game != GameVersion.BFBB => 0x5C,
            FragmentKind.Fire when game != GameVersion.BFBB =>
                game is GameVersion.ROTU or GameVersion.Ratatouille ? 0xB4 : 0x5C,
            FragmentKind.Light when game is GameVersion.ROTU or GameVersion.Ratatouille => 0x60,
            FragmentKind.Smoke when game is GameVersion.ROTU or GameVersion.Ratatouille => 0x50,
            FragmentKind.Goo when game is GameVersion.ROTU or GameVersion.Ratatouille => 0x88,
            _ => -1,
        };

    /// <summary>
    /// The on-disk size of an <see cref="FragmentKind.Inactive"/> fragment, keyed by its
    /// otherwise-unused <see cref="Fragment.Id"/> field.
    /// </summary>
    internal static int GetInactiveFragSize(GameVersion game, uint id) => (game, id) switch
    {
        (GameVersion.TSSM, 3) => 0x1FC,
        (GameVersion.TSSM, 4) => 0x114,
        (GameVersion.TSSM, 6) => 0x48,
        (GameVersion.TSSM, 9) => 0x60,
        _ => -1,
    };

    internal static ShrapnelAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new ShrapnelAsset();
        AssetFields.Populate(asset, header, debug);

        int fragCount = reader.ReadInt32();
        asset.Physical.ShrapnelId = reader.ReadAssetId();
        reader.ReadUInt32(); // initCB (runtime pointer, constant 0 on disk)

        for (int i = 0; i < fragCount; i++)
            asset.Frags.Add(Fragment.Read(reader, profile));

        asset.Physical.FragCount = asset.Frags.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(ShrapnelAsset asset, EndianWriter writer, FormatProfile profile)
    {
        writer.Write(asset.Physical.FragCount);
        writer.Write(asset.Physical.ShrapnelId);
        writer.Write(0u); // initCB (runtime pointer, constant 0 on disk)

        foreach (var frag in asset.Frags)
            Fragment.Write(frag, writer, profile);

        writer.Write(asset.GetUnparsedTail());
    }
}

public static partial class Physical
{
    /// <summary>
    /// An explicit interface used to interact with <see cref="ShrapnelAsset"/>'s underlying values.
    /// </summary>
    public interface IShrapnelAsset : IAsset
    {
        /// <summary>
        /// The number of fragments stored in this asset.
        /// </summary>
        /// <remarks>
        /// When disagreements with <see cref="ShrapnelAsset.Frags"/>.Count exist, this field wins during serialization.
        /// </remarks>
        int FragCount { get; set; }

        /// <summary>
        /// The asset ID of this shrapnel asset as stored in the header.
        /// </summary>
        /// <remarks>
        /// When disagreements with <see cref="Asset.Id"/> exist, this field wins during serialization.
        /// </remarks>
        AssetId ShrapnelId { get; set; }
    }
}
