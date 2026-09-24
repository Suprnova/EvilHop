using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>
/// An <see cref="Asset"/> holding a wireframe model - vertices joined by lines - drawn on the
/// loading screens.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/WIRE">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class WireframeAsset() : Asset(AssetType.Wireframe), Physical.IWireframeAsset
{
    /// <summary>
    /// The model's vertices, indexed by <see cref="Lines"/>.
    /// </summary>
    public Collection<Vector3> Vertices { get; } = [];

    /// <summary>
    /// The model's lines, each joining two <see cref="Vertices"/>.
    /// </summary>
    public Collection<Line> Lines { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override Physical.IWireframeAsset Physical => this;

    private uint? _overriddenSize;
    uint Physical.IWireframeAsset.Size
    {
        get => _overriddenSize ?? DerivedSize;
        set => _overriddenSize = value == DerivedSize ? null : value;
    }

    private uint DerivedSize => (uint)(HeaderSize + Vertices.Count * 12 + Lines.Count * 4 + GetUnparsedTail().Length);

    private uint? _overriddenVertexCount;
    uint Physical.IWireframeAsset.VertexCount
    {
        get => _overriddenVertexCount ?? (uint)Vertices.Count;
        set => _overriddenVertexCount = value == (uint)Vertices.Count ? null : value;
    }

    private uint? _overriddenLineCount;
    uint Physical.IWireframeAsset.LineCount
    {
        get => _overriddenLineCount ?? (uint)Lines.Count;
        set => _overriddenLineCount = value == (uint)Lines.Count ? null : value;
    }

    private uint _verticesPointer;
    uint Physical.IWireframeAsset.VerticesPointer { get => _verticesPointer; set => _verticesPointer = value; }

    private uint _linesPointer;
    uint Physical.IWireframeAsset.LinesPointer { get => _linesPointer; set => _linesPointer = value; }

    /// <summary>
    /// The size, in bytes, of the fields ahead of <see cref="Vertices"/>.
    /// </summary>
    private const int HeaderSize = 20;

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.Wireframe"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.Incredibles,
        GameVersion.ROTU,
    };

    internal static WireframeAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new WireframeAsset();
        AssetFields.Populate(asset, header, debug);

        uint size = reader.ReadUInt32();
        uint vertexCount = reader.ReadUInt32();
        uint lineCount = reader.ReadUInt32();
        asset.Physical.VerticesPointer = reader.ReadUInt32();
        asset.Physical.LinesPointer = reader.ReadUInt32();

        for (uint i = 0; i < vertexCount; i++)
            asset.Vertices.Add(reader.ReadVector3());
        for (uint i = 0; i < lineCount; i++)
            asset.Lines.Add(Line.Read(reader, profile));

        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        asset.Physical.VertexCount = vertexCount;
        asset.Physical.LineCount = lineCount;
        asset.Physical.Size = size;
        return asset;
    }

    internal static void Write(WireframeAsset asset, EndianWriter writer, FormatProfile profile)
    {
        writer.Write(asset.Physical.Size);
        writer.Write(asset.Physical.VertexCount);
        writer.Write(asset.Physical.LineCount);
        writer.Write(asset.Physical.VerticesPointer);
        writer.Write(asset.Physical.LinesPointer);

        foreach (var vertex in asset.Vertices)
            writer.Write(vertex);
        foreach (var line in asset.Lines)
            Line.Write(line, writer, profile);

        writer.Write(asset.GetUnparsedTail());
    }

    /// <summary>
    /// One <see cref="WireframeAsset"/> line, joining two <see cref="Vertices"/>.
    /// </summary>
    /// <param name="Start">The index, into <see cref="Vertices"/>, of the line's first end.</param>
    /// <param name="End">The index, into <see cref="Vertices"/>, of the line's second end.</param>
    public readonly record struct Line(ushort Start, ushort End)
    {
        internal static Line Read(EndianReader reader, FormatProfile _) => new(reader.ReadUInt16(), reader.ReadUInt16());

        internal static void Write(Line value, EndianWriter writer, FormatProfile _)
        {
            writer.Write(value.Start);
            writer.Write(value.End);
        }
    }
}

public static partial class Physical
{
    /// <summary>
    /// An explicit interface used to interact with <see cref="WireframeAsset"/>'s underlying values.
    /// </summary>
    public interface IWireframeAsset : IAsset
    {
        /// <summary>
        /// The asset's total size, in bytes, read directly from its leading field.
        /// </summary>
        /// <remarks>
        /// When disagreements with the size <see cref="WireframeAsset.Vertices"/> and
        /// <see cref="WireframeAsset.Lines"/> imply exist, this field wins during serialization.
        /// </remarks>
        uint Size { get; set; }

        /// <summary>
        /// The number of <see cref="WireframeAsset.Vertices"/> stored for this asset, read directly from
        /// its count field.
        /// </summary>
        /// <remarks>
        /// When disagreements with <see cref="WireframeAsset.Vertices"/>.Count exist, this field wins
        /// during serialization.
        /// </remarks>
        uint VertexCount { get; set; }

        /// <summary>
        /// The number of <see cref="WireframeAsset.Lines"/> stored for this asset, read directly from its
        /// count field.
        /// </summary>
        /// <remarks>
        /// When disagreements with <see cref="WireframeAsset.Lines"/>.Count exist, this field wins during
        /// serialization.
        /// </remarks>
        uint LineCount { get; set; }

        /// <summary>
        /// The export tool's own memory address for <see cref="WireframeAsset.Vertices"/>. Never read by
        /// the game.
        /// </summary>
        uint VerticesPointer { get; set; }

        /// <summary>
        /// The export tool's own memory address for <see cref="WireframeAsset.Lines"/>. Never read by the
        /// game.
        /// </summary>
        uint LinesPointer { get; set; }
    }
}
