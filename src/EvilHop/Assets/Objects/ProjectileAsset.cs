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
public sealed class ProjectileAsset() : BaseAsset(AssetType.Projectile), IPhysicalProjectileAsset
{
    /// <summary>
    /// Unknown. Selects some effect - likely a trail or impact particle effect - played by this
    /// projectile.
    /// </summary>
    /// TODO: validate against decompiled source
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
    public bool DestructEnabled { get; set; }

    /// <summary>The time, in seconds, after which this projectile destroys itself, if <see cref="DestructEnabled"/>.</summary>
    public float DestructTime { get; set; }

    /// <summary>The distance traveled after which this projectile destroys itself, if <see cref="DestructEnabled"/>.</summary>
    public float DestructDistance { get; set; }

    /// <summary>Whether this projectile orients itself to face its direction of travel.</summary>
    public bool Oriented { get; set; }

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalProjectileAsset Physical => this;

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

    internal static ProjectileAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile _)
    {
        var asset = new ProjectileAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        asset.EffectType = reader.ReadInt32();
        asset.ModelId = reader.ReadAssetId();
        asset.AnimId = reader.ReadAssetId();
        asset.AtRestModelId = reader.ReadAssetId();
        asset.AtRestAnimId = reader.ReadAssetId();
        asset.DestructEnabled = reader.ReadInt32() != 0;
        asset.DestructTime = reader.ReadSingle();
        asset.DestructDistance = reader.ReadSingle();
        asset.Oriented = reader.ReadInt32() != 0;
        asset.Physical.Reserved = reader.ReadBytes(ReservedSize);

        LinkSerialization.Read(asset, reader, asset.Physical.LinkCount);
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(ProjectileAsset asset, EndianWriter writer, FormatProfile _)
    {
        BaseAssetPrefix.Write(asset, writer);

        writer.Write(asset.EffectType);
        writer.Write(asset.ModelId);
        writer.Write(asset.AnimId);
        writer.Write(asset.AtRestModelId);
        writer.Write(asset.AtRestAnimId);
        writer.Write(asset.DestructEnabled ? 1 : 0);
        writer.Write(asset.DestructTime);
        writer.Write(asset.DestructDistance);
        writer.Write(asset.Oriented ? 1 : 0);
        writer.Write(asset.Physical.Reserved);

        LinkSerialization.Write(asset, writer);
        writer.Write(asset.GetUnparsedTail());
    }
}

/// <summary>
/// An explicit interface used to interact with <see cref="ProjectileAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalProjectileAsset : IPhysicalBaseAsset
{
    /// <summary>
    /// Unknown. Always 0 in every sample checked so far.
    /// </summary>
    [SuppressMessage("Design", "CA1819:Properties should not return arrays", Justification = "Fixed-size raw padding with no field structure of its own; a byte[] is the natural representation.")]
    byte[] Reserved { get; set; }
}
