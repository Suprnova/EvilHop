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
public sealed class ModelInfoAsset() : Asset(AssetType.ModelInfo), IPhysicalModelInfoAsset
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
    public override IPhysicalModelInfoAsset Physical => this;

    private uint _magic = 0x464E494D; // "FNIM"
    uint IPhysicalModelInfoAsset.Magic { get => _magic; set => _magic = value; }

    private uint? _overriddenModelInstanceCount;
    uint IPhysicalModelInfoAsset.ModelInstanceCount
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
            asset.ModelInstances.Add(ReadInstance(reader));

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
            asset.Parameters.Add(ModelInfoParameterSerialization.Read(reader));
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
            WriteInstance(writer, instance);

        foreach (var parameter in asset.Parameters)
            ModelInfoParameterSerialization.Write(writer, parameter);

        writer.Write(asset.GetUnparsedTail());
    }

    private static ModelInfoInstance ReadInstance(EndianReader reader) => new()
    {
        ModelId = reader.ReadAssetId(),
        Flags = reader.ReadUInt16(),
        Parent = reader.ReadByte(),
        Bone = reader.ReadByte(),
        Right = reader.ReadVector3(),
        Up = reader.ReadVector3(),
        At = reader.ReadVector3(),
        Position = reader.ReadVector3(),
    };

    private static void WriteInstance(EndianWriter writer, ModelInfoInstance instance)
    {
        writer.Write(instance.ModelId);
        writer.Write(instance.Flags);
        writer.Write(instance.Parent);
        writer.Write(instance.Bone);
        writer.Write(instance.Right);
        writer.Write(instance.Up);
        writer.Write(instance.At);
        writer.Write(instance.Position);
    }
}

/// <summary>
/// An explicit interface used to interact with <see cref="ModelInfoAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalModelInfoAsset : IPhysicalAsset
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

/// <summary>
/// One <see cref="ModelInfoAsset"/> model instance, attached to <see cref="Parent"/>'s
/// <see cref="Bone"/>.
/// </summary>
/// <remarks>
/// <see cref="Flags"/>, <see cref="Parent"/>, and <see cref="Bone"/> are only read for every instance
/// after the first - the root instance (index 0) ignores them entirely.
/// </remarks>
public sealed class ModelInfoInstance
{
    /// <summary>The <see cref="AssetType.Model"/> - or nested <see cref="AssetType.ModelInfo"/> - this instance displays.</summary>
    public AssetId ModelId { get; set; }

    /// <summary>Flags applied when attaching this instance to <see cref="Parent"/>. Unused for the root instance.</summary>
    public ushort Flags { get; set; }

    /// <summary>
    /// The index into <see cref="ModelInfoAsset.ModelInstances"/> this instance attaches to. Unused
    /// for the root instance.
    /// </summary>
    public byte Parent { get; set; }

    /// <summary>The bone on <see cref="Parent"/> this instance attaches to. Unused for the root instance.</summary>
    public byte Bone { get; set; }

    /// <summary>The right vector of this instance's orientation relative to <see cref="Parent"/>, usually (1, 0, 0).</summary>
    public Vector3 Right { get; set; }

    /// <summary>The up vector of this instance's orientation relative to <see cref="Parent"/>, usually (0, 1, 0).</summary>
    public Vector3 Up { get; set; }

    /// <summary>The forward vector of this instance's orientation relative to <see cref="Parent"/>, usually (0, 0, 1).</summary>
    public Vector3 At { get; set; }

    /// <summary>This instance's position relative to <see cref="Parent"/>, usually zero.</summary>
    public Vector3 Position { get; set; }
}
