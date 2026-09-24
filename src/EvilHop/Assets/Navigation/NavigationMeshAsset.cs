using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="BaseAsset"/> describing the walkable ground NPCs navigate across: one or more
/// triangle meshes, with the routing tables the game uses to path between any two triangles.
/// </summary>
/// <remarks>
/// <para>
/// The game locates every table by walking the asset in order, sized by
/// <see cref="SubMesh.Triangles"/> and <see cref="SubMeshes"/>' counts, rather than by any stored
/// offset. A table whose length doesn't match those counts therefore shifts everything after it.
/// </para>
/// <para>
/// In <see cref="GameVersion.Incredibles"/>, the base type stored in the header is uninitialized
/// memory (<c>0xCD</c>) left by the export tool rather than a real base type.
/// </para>
/// </remarks>
public sealed partial class NavigationMeshAsset() : BaseAsset(AssetType.NavigationMesh, baseType: 0x9A), Physical.INavigationMeshAsset
{
    /// <summary>
    /// The mesh's sub-meshes, each its own separately routed piece of walkable ground.
    /// </summary>
    /// <remarks>
    /// Adding or removing a sub-mesh without resizing every sub-mesh's
    /// <see cref="SubMesh.LevelTwoRouteExits"/> to match, or without updating each
    /// <see cref="SubMeshExit.NeighborSubMesh"/> index, corrupts the mesh.
    /// </remarks>
    public Collection<SubMesh> SubMeshes { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override Physical.INavigationMeshAsset Physical => this;

    private int? _overriddenSubMeshCount;
    int Physical.INavigationMeshAsset.SubMeshCount
    {
        get => _overriddenSubMeshCount ?? SubMeshes.Count;
        set => _overriddenSubMeshCount = value == SubMeshes.Count ? null : value;
    }

    private uint _subMeshesPointer;
    uint Physical.INavigationMeshAsset.SubMeshesPointer { get => _subMeshesPointer; set => _subMeshesPointer = value; }

    private uint _circleList;
    uint Physical.INavigationMeshAsset.CircleList { get => _circleList; set => _circleList = value; }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.NavigationMesh"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.Incredibles,
        GameVersion.ROTU,
        GameVersion.Ratatouille,
    };

    internal static NavigationMeshAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new NavigationMeshAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        int subMeshCount = reader.ReadInt32();
        asset.Physical.SubMeshesPointer = reader.ReadUInt32();
        if (profile.Game is not GameVersion.Incredibles)
            asset.Physical.CircleList = reader.ReadUInt32();

        for (int i = 0; i < subMeshCount; i++)
            asset.SubMeshes.Add(SubMesh.ReadHeader(reader, profile));
        foreach (var subMesh in asset.SubMeshes)
            SubMesh.ReadData(reader, profile, subMesh, subMeshCount);
        reader.ReadBytes(PaddingAt(reader.BaseStream.Position)); // padding, always zero

        asset.Physical.SubMeshCount = subMeshCount;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(NavigationMeshAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);

        writer.Write(asset.Physical.SubMeshCount);
        writer.Write(asset.Physical.SubMeshesPointer);
        if (profile.Game is not GameVersion.Incredibles)
            writer.Write(asset.Physical.CircleList);

        foreach (var subMesh in asset.SubMeshes)
            SubMesh.WriteHeader(subMesh, writer, profile);
        foreach (var subMesh in asset.SubMeshes)
            SubMesh.WriteData(subMesh, writer, profile);
        writer.Write(new byte[PaddingAt(writer.BaseStream.Position)]); // padding

        writer.Write(asset.GetUnparsedTail());
    }

    /// <summary>
    /// The number of zero bytes that align <paramref name="position"/>, relative to the start of
    /// the asset, to 4 bytes.
    /// </summary>
    private static int PaddingAt(long position) => (int)(-position & 3);

    /// <summary>
    /// One <see cref="SubMesh"/> triangle: three <see cref="SubMesh.Vertices"/> indices.
    /// </summary>
    /// <param name="A">The first of the triangle's three <see cref="SubMesh.Vertices"/> indices.</param>
    /// <param name="B">The second of the triangle's three <see cref="SubMesh.Vertices"/> indices.</param>
    /// <param name="C">The third of the triangle's three <see cref="SubMesh.Vertices"/> indices.</param>
    /// <param name="Flags">Unknown.</param>
    public readonly record struct Triangle(byte A, byte B, byte C, byte Flags)
    {
        internal static Triangle Read(EndianReader reader, FormatProfile _) =>
            new(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte());

        internal static void Write(Triangle value, EndianWriter writer, FormatProfile _)
        {
            writer.Write(value.A);
            writer.Write(value.B);
            writer.Write(value.C);
            writer.Write(value.Flags);
        }
    }

    /// <summary>
    /// A triangle on a <see cref="SubMesh"/>'s boundary that leads onto another sub-mesh.
    /// </summary>
    /// <param name="ExitTriangle">The index, into this sub-mesh's <see cref="SubMesh.Triangles"/>, of the triangle to exit from.</param>
    /// <param name="DestinationTriangle">The index, into the neighboring sub-mesh's <see cref="SubMesh.Triangles"/>, of the triangle to arrive on.</param>
    /// <param name="NeighborSubMesh">The index, into <see cref="SubMeshes"/>, of the neighboring sub-mesh.</param>
    public readonly record struct SubMeshExit(int ExitTriangle, int DestinationTriangle, int NeighborSubMesh)
    {
        internal static SubMeshExit Read(EndianReader reader, FormatProfile _) =>
            new(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());

        internal static void Write(SubMeshExit value, EndianWriter writer, FormatProfile _)
        {
            writer.Write(value.ExitTriangle);
            writer.Write(value.DestinationTriangle);
            writer.Write(value.NeighborSubMesh);
        }
    }
}

public static partial class Physical
{
    /// <summary>
    /// An explicit interface used to interact with <see cref="NavigationMeshAsset"/>'s underlying values.
    /// </summary>
    public interface INavigationMeshAsset : IBaseAsset
    {
        /// <summary>
        /// The number of <see cref="NavigationMeshAsset.SubMeshes"/> stored for this asset, read
        /// directly from its leading count field.
        /// </summary>
        /// <remarks>
        /// When disagreements with <see cref="NavigationMeshAsset.SubMeshes"/>.Count exist, this field
        /// wins during serialization.
        /// </remarks>
        int SubMeshCount { get; set; }

        /// <summary>
        /// The export tool's own memory address for <see cref="NavigationMeshAsset.SubMeshes"/>. Never
        /// read by the game.
        /// </summary>
        uint SubMeshesPointer { get; set; }

        /// <summary>
        /// Uninitialized memory left by the export tool in a field the game only uses at runtime. Not
        /// present in <see cref="GameVersion.Incredibles"/>.
        /// </summary>
        uint CircleList { get; set; }
    }
}
