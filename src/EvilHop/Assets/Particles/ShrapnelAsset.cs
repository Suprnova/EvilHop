using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace EvilHop.Assets;

/// <summary>
/// Defines data and fragment definitions for shrapnel spawned during gameplay, such as when
/// destructible objects break or effects explode.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/SHRP">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class ShrapnelAsset() : Asset(AssetType.Shrapnel), IPhysicalShrapnelAsset
{
    /// <summary>This shrapnel asset's fragments.</summary>
    public Collection<ShrapnelFrag> Frags { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalShrapnelAsset Physical => this;

    private int? _overriddenFragCount;
    int IPhysicalShrapnelAsset.FragCount
    {
        get => _overriddenFragCount ?? Frags.Count;
        set => _overriddenFragCount = value == Frags.Count ? null : value;
    }

    private AssetId? _overriddenShrapnelId;
    AssetId IPhysicalShrapnelAsset.ShrapnelId
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
        ShrapnelFragType type,
        bool hasExtendedFragFields = true,
        bool soundHasExtendedFields = true,
        bool projectileHasIntermediateFields = false) => type switch
        {
            ShrapnelFragType.Shrapnel => 0x20,
            ShrapnelFragType.Particle => game == GameVersion.BFBB ? (hasExtendedFragFields ? 0x1D4 : 0x1D0) : 0x1F4,
            ShrapnelFragType.Projectile => game == GameVersion.BFBB
                ? (hasExtendedFragFields ? 0x90 : (projectileHasIntermediateFields ? 0x6C : 0x58))
                : (game is GameVersion.ROTU or GameVersion.Ratatouille ? 0x158 : 0x110),
            ShrapnelFragType.Lightning => game == GameVersion.BFBB ? 0x68 : 0x70,
            ShrapnelFragType.Sound => game == GameVersion.BFBB ? (soundHasExtendedFields ? 0x4C : 0x40) : 0x44,
            ShrapnelFragType.Shockwave => 0x54,
            ShrapnelFragType.Explosion when game != GameVersion.BFBB => 0x48,
            ShrapnelFragType.Distortion when game != GameVersion.BFBB => 0x5C,
            ShrapnelFragType.Fire when game != GameVersion.BFBB =>
                game is GameVersion.ROTU or GameVersion.Ratatouille ? 0xB4 : 0x5C,
            ShrapnelFragType.Light when game is GameVersion.ROTU or GameVersion.Ratatouille => 0x60,
            ShrapnelFragType.Smoke when game is GameVersion.ROTU or GameVersion.Ratatouille => 0x50,
            ShrapnelFragType.Goo when game is GameVersion.ROTU or GameVersion.Ratatouille => 0x88,
            _ => -1,
        };

    /// <summary>
    /// The on-disk size of an <see cref="ShrapnelFragType.Inactive"/> fragment, keyed by its
    /// otherwise-unused <see cref="ShrapnelFrag.Id"/> field.
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
            asset.Frags.Add(ShrapnelFrag.Read(reader, profile));

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
            ShrapnelFrag.Write(frag, writer, profile);

        writer.Write(asset.GetUnparsedTail());
    }
}

/// <summary>
/// An explicit interface used to interact with <see cref="ShrapnelAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalShrapnelAsset : IPhysicalAsset
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

/// <summary>
/// A single fragment entry within a <see cref="ShrapnelAsset"/>.
/// </summary>
public sealed class ShrapnelFrag
{
    /// <summary>The type of effect or object spawned by this fragment.</summary>
    public ShrapnelFragType Type { get; set; }

    /// <summary>This fragment's asset ID.</summary>
    public AssetId Id { get; set; }

    /// <summary>The asset ID of this fragment's first parent, if any.</summary>
    public AssetId ParentId0 { get; set; }

    /// <summary>The asset ID of this fragment's second parent, if any.</summary>
    public AssetId ParentId1 { get; set; }

    /// <summary>The lifetime of this fragment, in seconds.</summary>
    public float Lifetime { get; set; }

    /// <summary>The delay before this fragment spawns, in seconds.</summary>
    public float Delay { get; set; }

    /// <summary>
    /// The type-specific raw data payload following the 24-byte fragment header.
    /// </summary>
    [SuppressMessage("Design", "CA1819:Properties should not return arrays", Justification = "Raw fragment data payload; a byte[] is the natural representation.")]
    public byte[] Data { get; set; } = [];

    internal static ShrapnelFrag Read(EndianReader reader, FormatProfile profile)
    {
        var type = (ShrapnelFragType)reader.ReadUInt32();
        var id = reader.ReadAssetId();
        var parentId0 = reader.ReadAssetId();
        var parentId1 = reader.ReadAssetId();
        float lifetime = reader.ReadSingle();
        float delay = reader.ReadSingle();

        int totalFragSize = type == ShrapnelFragType.Inactive
            ? ShrapnelAsset.GetInactiveFragSize(profile.Game, id.Value)
            : ShrapnelAsset.GetFragSize(profile.Game, type, profile.ShrapnelHasExtendedFragFields, profile.ShrapnelSoundHasExtendedFields, profile.ShrapnelProjectileHasIntermediateFields);
        if (totalFragSize < 24)
        {
            throw new InvalidDataException($"Unknown or unsupported fragment type {(uint)type} under {profile.Game}.");
        }

        return new ShrapnelFrag
        {
            Type = type,
            Id = id,
            ParentId0 = parentId0,
            ParentId1 = parentId1,
            Lifetime = lifetime,
            Delay = delay,
            Data = reader.ReadBytes(totalFragSize - 24),
        };
    }

    internal static void Write(ShrapnelFrag frag, EndianWriter writer, FormatProfile _)
    {
        writer.Write((uint)frag.Type);
        writer.Write(frag.Id);
        writer.Write(frag.ParentId0);
        writer.Write(frag.ParentId1);
        writer.Write(frag.Lifetime);
        writer.Write(frag.Delay);
        if (frag.Data is not null)
            writer.Write(frag.Data);
    }
}

/// <summary>
/// The kind of effect or object spawned by a <see cref="ShrapnelFrag"/>.
/// </summary>
public enum ShrapnelFragType : uint
{
    /// <summary>
    /// Inactive fragment slot. Its <see cref="ShrapnelFrag.Id"/> is repurposed as a marker for
    /// the deactivated fragment's on-disk size - see <see cref="ShrapnelAsset.GetInactiveFragSize"/>.
    /// </summary>
    Inactive = 0,

    /// <summary>Group fragment.</summary>
    Group = 1,

    /// <summary>Nested shrapnel spawner referencing another <see cref="AssetType.Shrapnel"/>.</summary>
    Shrapnel = 2,

    /// <summary>Particle emitter fragment.</summary>
    Particle = 3,

    /// <summary>Physical projectile fragment with bounce, gravity, and model.</summary>
    Projectile = 4,

    /// <summary>Lightning arc fragment.</summary>
    Lightning = 5,

    /// <summary>Sound effect fragment.</summary>
    Sound = 6,

    /// <summary>Expanding shockwave ring fragment.</summary>
    Shockwave = 7,

    /// <summary>Explosion effect fragment.</summary>
    Explosion = 8,

    /// <summary>Screen distortion fragment.</summary>
    Distortion = 9,

    /// <summary>Fire effect fragment.</summary>
    Fire = 10,

    /// <summary>Dynamic light source fragment.</summary>
    Light = 11,

    /// <summary>Smoke plume fragment.</summary>
    Smoke = 12,

    /// <summary>Goo splat fragment.</summary>
    Goo = 13,
}
