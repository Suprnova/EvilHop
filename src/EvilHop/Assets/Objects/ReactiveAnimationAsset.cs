using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;

namespace EvilHop.Assets;

/// <summary>
/// A table of "reactive" behaviors - models, animations, and sounds to swap to when something
/// touches, passes through, or sets fire to the objects that reference it.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/RANM">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class ReactiveAnimationAsset() : BaseAsset(AssetType.ReactiveAnimation), IPhysicalReactiveAnimationAsset
{
    /// <summary>
    /// The table's format version.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// The table's rows, each describing one reactive behavior.
    /// </summary>
    public Collection<ReactiveAnimationRow> Rows { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalReactiveAnimationAsset Physical => this;

    private int? _overriddenRowCount;
    int IPhysicalReactiveAnimationAsset.RowCount
    {
        get => _overriddenRowCount ?? Rows.Count;
        set => _overriddenRowCount = value == Rows.Count ? null : value;
    }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.ReactiveAnimation"/> is known to be read
    /// by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.TSSM,
        GameVersion.Incredibles,
    };

    internal static ReactiveAnimationAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile _)
    {
        var asset = new ReactiveAnimationAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        asset.Version = reader.ReadInt32();
        int rowCount = reader.ReadInt32();
        for (int i = 0; i < rowCount; i++)
        {
            asset.Rows.Add(new ReactiveAnimationRow
            {
                StaticModelId = reader.ReadAssetId(),
                BoundModelId = reader.ReadAssetId(),
                LodDistance = reader.ReadSingle(),
                IdleAnimationId = reader.ReadAssetId(),
                MoveThroughAnimationId = reader.ReadAssetId(),
                HitAnimationId = reader.ReadAssetId(),
                IdleSoundGroupId = reader.ReadAssetId(),
                MoveThroughSoundGroupId = reader.ReadAssetId(),
                HitSoundGroupId = reader.ReadAssetId(),
                BurntModelId = reader.ReadAssetId(),
                BurnAnimationId = reader.ReadAssetId(),
                BurnFuel = reader.ReadSingle(),
                BurnFlameSize = reader.ReadSingle(),
                BurnEmitScale = reader.ReadSingle(),
                MoveThroughRadius = reader.ReadSingle(),
            });
        }

        asset.Physical.RowCount = asset.Rows.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(ReactiveAnimationAsset asset, EndianWriter writer, FormatProfile _)
    {
        BaseAssetPrefix.Write(asset, writer);

        writer.Write(asset.Version);
        writer.Write(asset.Physical.RowCount);
        foreach (var row in asset.Rows)
        {
            writer.Write(row.StaticModelId);
            writer.Write(row.BoundModelId);
            writer.Write(row.LodDistance);
            writer.Write(row.IdleAnimationId);
            writer.Write(row.MoveThroughAnimationId);
            writer.Write(row.HitAnimationId);
            writer.Write(row.IdleSoundGroupId);
            writer.Write(row.MoveThroughSoundGroupId);
            writer.Write(row.HitSoundGroupId);
            writer.Write(row.BurntModelId);
            writer.Write(row.BurnAnimationId);
            writer.Write(row.BurnFuel);
            writer.Write(row.BurnFlameSize);
            writer.Write(row.BurnEmitScale);
            writer.Write(row.MoveThroughRadius);
        }

        writer.Write(asset.GetUnparsedTail());
    }
}

/// <summary>
/// An explicit interface used to interact with <see cref="ReactiveAnimationAsset"/>'s underlying
/// values.
/// </summary>
public interface IPhysicalReactiveAnimationAsset : IPhysicalBaseAsset
{
    /// <summary>
    /// The number of <see cref="ReactiveAnimationAsset.Rows"/> stored for this asset, read directly
    /// from its leading row count field.
    /// </summary>
    /// <remarks>
    /// When disagreements with <see cref="ReactiveAnimationAsset.Rows"/>.Count exist, this field
    /// wins during serialization.
    /// </remarks>
    int RowCount { get; set; }
}

/// <summary>
/// One <see cref="ReactiveAnimationAsset"/> row: the models, animations, and sounds to swap to for
/// one reactive behavior.
/// </summary>
public sealed class ReactiveAnimationRow
{
    /// <summary>
    /// The <see cref="AssetType.Model"/> used while the reacting object is static.
    /// </summary>
    public AssetId StaticModelId { get; set; }

    /// <summary>
    /// The <see cref="AssetType.Model"/> used for the reacting object's bounding/collision
    /// representation.
    /// </summary>
    public AssetId BoundModelId { get; set; }

    /// <summary>
    /// The distance from the camera beyond which the reacting object's level of detail drops.
    /// </summary>
    public float LodDistance { get; set; }

    /// <summary>
    /// The <see cref="AssetType.Animation"/> played while idle.
    /// </summary>
    public AssetId IdleAnimationId { get; set; }

    /// <summary>
    /// The <see cref="AssetType.Animation"/> played while something moves through the reacting
    /// object.
    /// </summary>
    public AssetId MoveThroughAnimationId { get; set; }

    /// <summary>
    /// The <see cref="AssetType.Animation"/> played when the reacting object is hit.
    /// </summary>
    public AssetId HitAnimationId { get; set; }

    /// <summary>
    /// The <see cref="AssetType.SoundGroup"/> played while idle.
    /// </summary>
    public AssetId IdleSoundGroupId { get; set; }

    /// <summary>
    /// The <see cref="AssetType.SoundGroup"/> played while something moves through the reacting
    /// object.
    /// </summary>
    public AssetId MoveThroughSoundGroupId { get; set; }

    /// <summary>
    /// The <see cref="AssetType.SoundGroup"/> played when the reacting object is hit.
    /// </summary>
    public AssetId HitSoundGroupId { get; set; }

    /// <summary>
    /// The <see cref="AssetType.Model"/> swapped to once the reacting object has burnt up.
    /// </summary>
    public AssetId BurntModelId { get; set; }

    /// <summary>
    /// The <see cref="AssetType.Animation"/> played while the reacting object burns.
    /// </summary>
    public AssetId BurnAnimationId { get; set; }

    /// <summary>
    /// How much fuel the reacting object's fire has before burning out.
    /// </summary>
    /// TODO: figure out what this actually means.
    public float BurnFuel { get; set; }

    /// <summary>
    /// The size of the flame effect while the reacting object burns.
    /// </summary>
    public float BurnFlameSize { get; set; }

    /// <summary>
    /// The scale of the particles emitted while the reacting object burns.
    /// </summary>
    public float BurnEmitScale { get; set; }

    /// <summary>
    /// The radius within which something is considered to be moving through the reacting object.
    /// </summary>
    public float MoveThroughRadius { get; set; }
}
