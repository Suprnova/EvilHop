using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>
/// Defines an enemy or NPC's model hierarchy - one or more <see cref="AssetType.Model"/>s (or nested
/// <see cref="AssetType.ModelInfo"/>s) attached together - plus named parameters read by the NPC's
/// own AI code.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/MINF">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed partial class ModelInfoAsset() : Asset(AssetType.ModelInfo), Physical.IModelInfoAsset
{
    /// <summary>The <see cref="AssetType.AnimationTable"/> driving this model's animations.</summary>
    public AssetId AnimTableId { get; set; }

    /// <summary>
    /// A shared combat/attack table this model's entity uses, if any. Not present in
    /// <see cref="GameVersion.N100F"/>.
    /// </summary>
    public AssetId CombatId { get; set; }

    /// <summary>
    /// Identifies which AI behavior this model's entity uses. Not present in
    /// <see cref="GameVersion.N100F"/>.
    /// </summary>
    public AssetId BrainId { get; set; }

    /// <summary>
    /// The model hierarchy's instances. The first entry is the root; every other entry attaches to
    /// <see cref="ModelInfoInstance.Parent"/>, an index into this same collection.
    /// </summary>
    public Collection<ModelInfoInstance> ModelInstances { get; } = [];

    /// <summary>Named parameters this model's NPC AI code reads by hash.</summary>
    public Collection<ModelInfoParameter> Parameters { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override Physical.IModelInfoAsset Physical => this;

    private uint _magic = 0x464E494D; // "FNIM"
    uint Physical.IModelInfoAsset.Magic { get => _magic; set => _magic = value; }

    private uint? _overriddenModelInstanceCount;
    uint Physical.IModelInfoAsset.ModelInstanceCount
    {
        get => _overriddenModelInstanceCount ?? (uint)ModelInstances.Count;
        set => _overriddenModelInstanceCount = value == (uint)ModelInstances.Count ? null : value;
    }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.ModelInfo"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.N100F,
        GameVersion.BFBB,
        GameVersion.TSSM,
        GameVersion.Incredibles,
        GameVersion.ROTU,
        GameVersion.Ratatouille,
    };

    internal static ModelInfoAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new ModelInfoAsset();
        AssetFields.Populate(asset, header, debug);

        asset.Physical.Magic = reader.ReadUInt32();
        uint modelInstanceCount = reader.ReadUInt32();
        asset.AnimTableId = reader.ReadAssetId();

        if (profile.Game is not GameVersion.N100F)
        {
            asset.CombatId = reader.ReadAssetId();
            asset.BrainId = reader.ReadAssetId();
        }

        for (int i = 0; i < modelInstanceCount; i++)
            asset.ModelInstances.Add(ModelInfoInstance.Read(reader, profile));

        while (reader.BaseStream.Length - reader.BaseStream.Position >= 5)
        {
            long entryStart = reader.BaseStream.Position;
            reader.ReadUInt32();
            byte wordLength = reader.ReadByte();
            int stringAreaLength = (wordLength + 1) * 4 - 1;

            if (reader.BaseStream.Length - reader.BaseStream.Position < stringAreaLength)
            {
                reader.BaseStream.Position = entryStart;
                break;
            }

            reader.BaseStream.Position = entryStart;
            asset.Parameters.Add(ModelInfoParameter.Read(reader, profile));
        }

        asset.Physical.ModelInstanceCount = modelInstanceCount;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(ModelInfoAsset asset, EndianWriter writer, FormatProfile profile)
    {
        writer.Write(asset.Physical.Magic);
        writer.Write(asset.Physical.ModelInstanceCount);
        writer.Write(asset.AnimTableId);

        if (profile.Game is not GameVersion.N100F)
        {
            writer.Write(asset.CombatId);
            writer.Write(asset.BrainId);
        }

        foreach (var instance in asset.ModelInstances)
            ModelInfoInstance.Write(instance, writer, profile);

        foreach (var parameter in asset.Parameters)
            ModelInfoParameter.Write(parameter, writer, profile);

        writer.Write(asset.GetUnparsedTail());
    }
}

public static partial class Physical
{
    /// <summary>
    /// An explicit interface used to interact with <see cref="ModelInfoAsset"/>'s underlying values.
    /// </summary>
    public interface IModelInfoAsset : IAsset
    {
        /// <summary>A four-character magic number.</summary>
        uint Magic { get; set; }

        /// <summary>
        /// The number of <see cref="ModelInfoAsset.ModelInstances"/> stored for this asset, read
        /// directly from its leading count field.
        /// </summary>
        /// <remarks>
        /// When disagreements with <see cref="ModelInfoAsset.ModelInstances"/>.Count exist, this field
        /// wins during serialization.
        /// </remarks>
        uint ModelInstanceCount { get; set; }
    }
}
