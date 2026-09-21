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
public sealed class ReactiveAnimationAsset() : BaseAsset(AssetType.ReactiveAnimation, baseType: 0x00), IPhysicalReactiveAnimationAsset
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
