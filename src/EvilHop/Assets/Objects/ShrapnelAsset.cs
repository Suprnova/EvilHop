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

    private uint _initCallbackPointer;
    uint IPhysicalShrapnelAsset.InitCallbackPointer
    {
        get => _initCallbackPointer;
        set => _initCallbackPointer = value;
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

    internal static int GetFragSize(GameVersion game, ShrapnelFragType type) => type switch
    {
        ShrapnelFragType.Shrapnel => 0x20,
        ShrapnelFragType.Particle => game == GameVersion.BFBB ? 0x1D4 : 0x1F4,
        ShrapnelFragType.Projectile => game == GameVersion.BFBB
            ? 0x90
            : (game is GameVersion.ROTU or GameVersion.Ratatouille ? 0x158 : 0x110),
        ShrapnelFragType.Lightning => game == GameVersion.BFBB ? 0x68 : 0x70,
        ShrapnelFragType.Sound => game == GameVersion.BFBB ? 0x4C : 0x44,
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
    /// <remarks>
    /// The wiki does not document this fragment type's layout. When a fragment is deactivated in
    /// the level editor, its <c>Id</c> field is repurposed to hold a small marker instead of an
    /// asset ID, and the rest of the fragment's original bytes are left on disk unchanged - so its
    /// size can't be derived from the type alone. These sizes were reverse-engineered from every
    /// real occurrence of an inactive fragment in the TSSM corpus; an unrecognized marker still
    /// fails loudly rather than guessing.
    /// </remarks>
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
        asset.Physical.InitCallbackPointer = reader.ReadUInt32();

        for (int i = 0; i < fragCount; i++)
        {
            var fragType = (ShrapnelFragType)reader.ReadUInt32();
            var id = reader.ReadAssetId();
            var parentId0 = reader.ReadAssetId();
            var parentId1 = reader.ReadAssetId();
            float lifetime = reader.ReadSingle();
            float delay = reader.ReadSingle();

            int totalFragSize = fragType == ShrapnelFragType.Inactive
                ? GetInactiveFragSize(profile.Game, id.Value)
                : GetFragSize(profile.Game, fragType);
            if (totalFragSize < 24)
            {
                throw new InvalidDataException($"Unknown or unsupported fragment type {(uint)fragType} under {profile.Game}.");
            }

            int payloadSize = totalFragSize - 24;
            byte[] data = reader.ReadBytes(payloadSize);

            asset.Frags.Add(new ShrapnelFrag
            {
                Type = fragType,
                Id = id,
                ParentId0 = parentId0,
                ParentId1 = parentId1,
                Lifetime = lifetime,
                Delay = delay,
                Data = data,
            });
        }

        asset.Physical.FragCount = asset.Frags.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(ShrapnelAsset asset, EndianWriter writer, FormatProfile _)
    {
        writer.Write(asset.Physical.FragCount);
        writer.Write(asset.Physical.ShrapnelId);
        writer.Write(asset.Physical.InitCallbackPointer);

        foreach (var frag in asset.Frags)
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
    /// Defaults to <see cref="ShrapnelAsset.Frags"/> count unless explicitly overridden.
    /// </remarks>
    int FragCount { get; set; }

    /// <summary>
    /// The asset ID of this shrapnel asset as stored in the header.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="Asset.Id"/> unless explicitly overridden.
    /// </remarks>
    AssetId ShrapnelId { get; set; }

    /// <summary>
    /// A runtime callback function pointer. Always 0 on disk.
    /// </summary>
    uint InitCallbackPointer { get; set; }
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
