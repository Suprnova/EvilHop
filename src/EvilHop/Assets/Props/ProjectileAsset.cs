using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace EvilHop.Assets;

/// <summary>
/// Defines a thrown or fired object - its model, its resting model once it lands, and how it's
/// destroyed once its lifetime runs out.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/PRJT">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class ProjectileAsset() : BaseAsset(AssetType.Projectile, baseType: 0x22), IPhysicalProjectileAsset
{
    /// <summary>
    /// Unknown. Selects some effect - likely a trail or impact particle effect - played by this
    /// projectile.
    /// </summary>
    public int EffectType { get; set; }

    /// <summary>The <see cref="AssetType.Model"/> or <see cref="AssetType.ModelInfo"/> this projectile displays as while in flight.</summary>
    public AssetId ModelId { get; set; }

    /// <summary>The <see cref="AssetType.Animation"/> or <see cref="AssetType.AnimationList"/> this projectile plays while in flight.</summary>
    public AssetId AnimId { get; set; }

    /// <summary>The <see cref="AssetType.Model"/> or <see cref="AssetType.ModelInfo"/> this projectile switches to once it comes to rest.</summary>
    public AssetId AtRestModelId { get; set; }

    /// <summary>The <see cref="AssetType.Animation"/> or <see cref="AssetType.AnimationList"/> this projectile plays once it comes to rest.</summary>
    public AssetId AtRestAnimId { get; set; }

    /// <summary>Whether this projectile destroys itself after <see cref="DestructTime"/> or <see cref="DestructDistance"/>.</summary>
    public bool DestructEnabled
    {
        get => Physical.DestructEnabled != 0;
        set => Physical.DestructEnabled = value ? 1 : 0;
    }

    /// <summary>The time, in seconds, after which this projectile destroys itself, if <see cref="DestructEnabled"/>.</summary>
    public float DestructTime { get; set; }

    /// <summary>The distance traveled after which this projectile destroys itself, if <see cref="DestructEnabled"/>.</summary>
    public float DestructDistance { get; set; }

    /// <summary>Whether this projectile orients itself to face its direction of travel.</summary>
    public bool Oriented
    {
        get => Physical.Oriented != 0;
        set => Physical.Oriented = value ? 1 : 0;
    }

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalProjectileAsset Physical => this;

    private int _destructEnabled;
    int IPhysicalProjectileAsset.DestructEnabled { get => _destructEnabled; set => _destructEnabled = value; }

    private int _oriented;
    int IPhysicalProjectileAsset.Oriented { get => _oriented; set => _oriented = value; }

    private byte[] _reserved = new byte[ReservedSize];
    byte[] IPhysicalProjectileAsset.Reserved { get => _reserved; set => _reserved = value; }

    private const int ReservedSize = 24;

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.Projectile"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.N100F,
    };

    internal static ProjectileAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new ProjectileAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        asset.EffectType = reader.ReadInt32();
        asset.ModelId = reader.ReadAssetId();
        asset.AnimId = reader.ReadAssetId();
        asset.AtRestModelId = reader.ReadAssetId();
        asset.AtRestAnimId = reader.ReadAssetId();
        asset.Physical.DestructEnabled = reader.ReadInt32();
        asset.DestructTime = reader.ReadSingle();
        asset.DestructDistance = reader.ReadSingle();
        asset.Physical.Oriented = reader.ReadInt32();
        asset.Physical.Reserved = reader.ReadBytes(ReservedSize);

        for (var i = 0; i < asset.Physical.LinkCount; i++)
            asset.Links.Add(Link.Read(reader, profile));
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(ProjectileAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);

        writer.Write(asset.EffectType);
        writer.Write(asset.ModelId);
        writer.Write(asset.AnimId);
        writer.Write(asset.AtRestModelId);
        writer.Write(asset.AtRestAnimId);
        writer.Write(asset.Physical.DestructEnabled);
        writer.Write(asset.DestructTime);
        writer.Write(asset.DestructDistance);
        writer.Write(asset.Physical.Oriented);
        writer.Write(asset.Physical.Reserved);

        foreach (var link in asset.Links)
            Link.Write(link, writer, profile);
        writer.Write(asset.GetUnparsedTail());
    }
}

/// <summary>
/// An explicit interface used to interact with <see cref="ProjectileAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalProjectileAsset : IPhysicalBaseAsset
{
    /// <summary>
    /// Whether this projectile destroys itself after <see cref="ProjectileAsset.DestructTime"/> or
    /// <see cref="ProjectileAsset.DestructDistance"/>, as stored on disk.
    /// </summary>
    int DestructEnabled { get; set; }

    /// <summary>
    /// Whether this projectile orients itself to face its direction of travel, as stored on disk.
    /// </summary>
    int Oriented { get; set; }

    /// <summary>
    /// Unknown.
    /// </summary>
    [SuppressMessage("Design", "CA1819:Properties should not return arrays", Justification = "Fixed-size raw padding with no field structure of its own; a byte[] is the natural representation.")]
    byte[] Reserved { get; set; }
}
