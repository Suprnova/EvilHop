using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>
/// A set of alternate vertex positions for a model, blended toward by a runtime weight to animate
/// shape changes (for example, facial expressions) without a full skeleton.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/MPHT">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class MorphTargetAsset() : Asset(AssetType.MorphTarget), IPhysicalMorphTargetAsset
{
    /// <summary>
    /// Scales each <see cref="MorphTarget.Vertices"/> component when they're packed as 16-bit
    /// integers on disk, or zero when they're stored as full-precision floats instead.
    /// </summary>
    public float Scale { get; set; }

    /// <summary>The center of the bounding sphere enclosing every <see cref="Targets"/> entry.</summary>
    public Vector3 Center { get; set; }

    /// <summary>The radius of the bounding sphere enclosing every <see cref="Targets"/> entry.</summary>
    public float Radius { get; set; }

    /// <summary>
    /// The morph targets, each holding one alternate position per vertex of the base mesh.
    /// </summary>
    public Collection<MorphTarget> Targets { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalMorphTargetAsset Physical => this;

    private uint _magic = 0x31484D50; // "MPH1"
    uint IPhysicalMorphTargetAsset.Magic { get => _magic; set => _magic = value; }

    private uint _flags;
    uint IPhysicalMorphTargetAsset.MorphFlags { get => _flags; set => _flags = value; }

    private ushort? _overriddenTargetCount;
    ushort IPhysicalMorphTargetAsset.TargetCount
    {
        get => _overriddenTargetCount ?? (ushort)Targets.Count;
        set => _overriddenTargetCount = value == (ushort)Targets.Count ? null : value;
    }

    private ushort? _overriddenVertexCount;
    ushort IPhysicalMorphTargetAsset.VertexCount
    {
        get => _overriddenVertexCount ?? (ushort)(Targets.Count > 0 ? Targets[0].Vertices.Count : 0);
        set => _overriddenVertexCount = value == (ushort)(Targets.Count > 0 ? Targets[0].Vertices.Count : 0) ? null : value;
    }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.MorphTarget"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.N100F,
    };

    internal static MorphTargetAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new MorphTargetAsset();
        AssetFields.Populate(asset, header, debug);

        asset.Physical.Magic = reader.ReadUInt32();
        ushort targetCount = reader.ReadUInt16();
        ushort vertexCount = reader.ReadUInt16();
        asset.Physical.MorphFlags = reader.ReadUInt32();
        asset.Scale = reader.ReadSingle();
        asset.Center = reader.ReadVector3();
        asset.Radius = reader.ReadSingle();

        int elementSize = asset.Scale == 0f ? 4 : 2;
        long rawSize = (long)vertexCount * 3 * elementSize;
        int paddingSize = (int)((rawSize + 15) / 16 * 16 - rawSize);

        for (int t = 0; t < targetCount; t++)
            asset.Targets.Add(MorphTarget.Read(reader, profile, vertexCount, asset.Scale, paddingSize));

        asset.Physical.TargetCount = targetCount;
        asset.Physical.VertexCount = vertexCount;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(MorphTargetAsset asset, EndianWriter writer, FormatProfile profile)
    {
        writer.Write(asset.Physical.Magic);
        writer.Write(asset.Physical.TargetCount);
        writer.Write(asset.Physical.VertexCount);
        writer.Write(asset.Physical.MorphFlags);
        writer.Write(asset.Scale);
        writer.Write(asset.Center);
        writer.Write(asset.Radius);

        int elementSize = asset.Scale == 0f ? 4 : 2;
        long rawSize = (long)asset.Physical.VertexCount * 3 * elementSize;
        int paddingSize = (int)((rawSize + 15) / 16 * 16 - rawSize);

        foreach (var target in asset.Targets)
            MorphTarget.Write(target, writer, profile, asset.Scale, paddingSize);

        writer.Write(asset.GetUnparsedTail());
    }
}

/// <summary>
/// An explicit interface used to interact with <see cref="MorphTargetAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalMorphTargetAsset : IPhysicalAsset
{
    /// <summary>A four-character magic number.</summary>
    uint Magic { get; set; }

    /// <summary>Unknown.</summary>
    uint MorphFlags { get; set; }

    /// <summary>
    /// The number of <see cref="MorphTargetAsset.Targets"/> stored for this asset, read directly
    /// from its leading count field.
    /// </summary>
    /// <remarks>
    /// When disagreements with <see cref="MorphTargetAsset.Targets"/>.Count exist, this field wins
    /// during serialization.
    /// </remarks>
    ushort TargetCount { get; set; }

    /// <summary>
    /// The number of vertices each <see cref="MorphTargetAsset.Targets"/> entry holds, read
    /// directly from its leading count field.
    /// </summary>
    /// <remarks>
    /// When disagreements with the first <see cref="MorphTargetAsset.Targets"/> entry's
    /// <see cref="MorphTarget.Vertices"/>.Count exist, this field wins during serialization.
    /// </remarks>
    ushort VertexCount { get; set; }
}

/// <summary>
/// One <see cref="MorphTargetAsset"/> target: one alternate position per vertex of the base mesh,
/// in the same order.
/// </summary>
public sealed class MorphTarget
{
    /// <summary>This target's vertex positions.</summary>
    public Collection<Vector3> Vertices { get; } = [];

    internal static MorphTarget Read(EndianReader reader, FormatProfile profile, int vertexCount, float scale, int paddingSize)
    {
        var target = new MorphTarget();
        for (int v = 0; v < vertexCount; v++)
            target.Vertices.Add(scale == 0f ? reader.ReadVector3() : ReadScaledVertex(reader, profile, scale));
        reader.ReadBytes(paddingSize); // alignment padding, always zero
        return target;
    }

    internal static void Write(MorphTarget value, EndianWriter writer, FormatProfile profile, float scale, int paddingSize)
    {
        foreach (var vertex in value.Vertices)
        {
            if (scale == 0f) writer.Write(vertex);
            else WriteScaledVertex(vertex, writer, profile, scale);
        }
        for (int i = 0; i < paddingSize; i++) writer.Write((byte)0);
    }

    private static Vector3 ReadScaledVertex(EndianReader reader, FormatProfile _, float scale) => new(
        reader.ReadInt16() * scale,
        reader.ReadInt16() * scale,
        reader.ReadInt16() * scale);

    private static void WriteScaledVertex(Vector3 vertex, EndianWriter writer, FormatProfile _, float scale)
    {
        writer.Write((short)MathF.Round(vertex.X / scale));
        writer.Write((short)MathF.Round(vertex.Y / scale));
        writer.Write((short)MathF.Round(vertex.Z / scale));
    }
}
