using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>
/// A node in a spline-based path network, defining movement speed, optional hover behavior,
/// and connections to forward and backward <see cref="AssetType.SplinePath"/>s along a
/// <see cref="AssetType.Spline"/> curve.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/SPLP">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class SplinePathAsset() : BaseAsset(AssetType.SplinePath, baseType: 0xE5), IPhysicalSplinePathAsset
{
    /// <summary>Whether this spline path is exclusive to a single actor.</summary>
    public bool IsExclusive
    {
        get => Physical.IsExclusive != 0;
        set => Physical.IsExclusive = (byte)(value ? 1 : 0);
    }

    /// <summary>Whether this spline path has a hover point.</summary>
    public bool HasHover
    {
        get => Physical.HasHover != 0;
        set => Physical.HasHover = (byte)(value ? 1 : 0);
    }

    /// <summary>The travel speed along this spline path.</summary>
    public float Speed { get; set; }

    /// <summary>The time, in seconds, spent hovering at <see cref="HoverPoint"/>.</summary>
    public float HoverTime { get; set; }

    /// <summary>The position at which an actor hovers when <see cref="HasHover"/> is active.</summary>
    public Vector3 HoverPoint { get; set; }

    /// <summary>The <see cref="AssetType.Spline"/> asset this path follows.</summary>
    public AssetId SplineId { get; set; }

    /// <summary>The <see cref="AssetType.SplinePath"/> assets reachable in the forward direction.</summary>
    public Collection<AssetId> ForwardAssetIds { get; } = [];

    /// <summary>The <see cref="AssetType.SplinePath"/> assets reachable in the backward direction.</summary>
    public Collection<AssetId> BackwardAssetIds { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalSplinePathAsset Physical => this;

    private byte _used = 0x4D;
    byte IPhysicalSplinePathAsset.Used { get => _used; set => _used = value; }

    private byte _pad0 = 0x53;
    byte IPhysicalSplinePathAsset.Pad0 { get => _pad0; set => _pad0 = value; }

    private byte _isExclusive;
    byte IPhysicalSplinePathAsset.IsExclusive { get => _isExclusive; set => _isExclusive = value; }

    private byte _hasHover;
    byte IPhysicalSplinePathAsset.HasHover { get => _hasHover; set => _hasHover = value; }

    private ushort? _overriddenForwardCount;
    ushort IPhysicalSplinePathAsset.ForwardCount
    {
        get => _overriddenForwardCount ?? (ushort)ForwardAssetIds.Count;
        set => _overriddenForwardCount = value == (ushort)ForwardAssetIds.Count ? null : value;
    }

    private ushort? _overriddenBackwardCount;
    ushort IPhysicalSplinePathAsset.BackwardCount
    {
        get => _overriddenBackwardCount ?? (ushort)BackwardAssetIds.Count;
        set => _overriddenBackwardCount = value == (ushort)BackwardAssetIds.Count ? null : value;
    }

    private uint _forwardIdsPointer;
    uint IPhysicalSplinePathAsset.ForwardIdsPointer { get => _forwardIdsPointer; set => _forwardIdsPointer = value; }

    private uint _backwardIdsPointer;
    uint IPhysicalSplinePathAsset.BackwardIdsPointer { get => _backwardIdsPointer; set => _backwardIdsPointer = value; }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.SplinePath"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.Incredibles,
    };

    internal static SplinePathAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new SplinePathAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        asset.Physical.IsExclusive = reader.ReadByte();
        asset.Physical.Used = reader.ReadByte();
        asset.Physical.HasHover = reader.ReadByte();
        asset.Physical.Pad0 = reader.ReadByte();

        ushort forwardCount = reader.ReadUInt16();
        ushort backwardCount = reader.ReadUInt16();

        asset.Speed = reader.ReadSingle();
        asset.HoverTime = reader.ReadSingle();
        asset.HoverPoint = reader.ReadVector3();
        asset.SplineId = reader.ReadAssetId();
        asset.Physical.ForwardIdsPointer = reader.ReadUInt32();
        asset.Physical.BackwardIdsPointer = reader.ReadUInt32();

        for (int i = 0; i < forwardCount; i++)
            asset.ForwardAssetIds.Add(reader.ReadAssetId());
        asset.Physical.ForwardCount = (ushort)asset.ForwardAssetIds.Count;

        for (int i = 0; i < backwardCount; i++)
            asset.BackwardAssetIds.Add(reader.ReadAssetId());
        asset.Physical.BackwardCount = (ushort)asset.BackwardAssetIds.Count;

        for (var i = 0; i < asset.Physical.LinkCount; i++)
            asset.Links.Add(Link.Read(reader, profile));
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(SplinePathAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);

        writer.Write(asset.Physical.IsExclusive);
        writer.Write(asset.Physical.Used);
        writer.Write(asset.Physical.HasHover);
        writer.Write(asset.Physical.Pad0);

        writer.Write(asset.Physical.ForwardCount);
        writer.Write(asset.Physical.BackwardCount);

        writer.Write(asset.Speed);
        writer.Write(asset.HoverTime);
        writer.Write(asset.HoverPoint);
        writer.Write(asset.SplineId);
        writer.Write(asset.Physical.ForwardIdsPointer);
        writer.Write(asset.Physical.BackwardIdsPointer);

        foreach (var id in asset.ForwardAssetIds)
            writer.Write(id);

        foreach (var id in asset.BackwardAssetIds)
            writer.Write(id);

        foreach (var link in asset.Links)
            Link.Write(link, writer, profile);
        writer.Write(asset.GetUnparsedTail());
    }
}

/// <summary>
/// An explicit interface used to interact with <see cref="SplinePathAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalSplinePathAsset : IPhysicalBaseAsset
{
    /// <summary>
    /// Runtime flag indicating whether the path is exclusive to a single actor.
    /// </summary>
    byte IsExclusive { get; set; }

    /// <summary>
    /// Runtime flag indicating whether the path is in use. Preserved from disk.
    /// </summary>
    byte Used { get; set; }

    /// <summary>
    /// Runtime flag indicating whether the path has a hover point.
    /// </summary>
    byte HasHover { get; set; }

    /// <summary>
    /// Padding byte after <see cref="SplinePathAsset.HasHover"/>. Preserved from disk.
    /// </summary>
    byte Pad0 { get; set; }

    /// <summary>
    /// The number of forward <see cref="AssetType.SplinePath"/> references stored for this asset.
    /// </summary>
    /// <remarks>
    /// When disagreements with <see cref="SplinePathAsset.ForwardAssetIds"/>.Count exist, this field wins during serialization.
    /// </remarks>
    ushort ForwardCount { get; set; }

    /// <summary>
    /// The number of backward <see cref="AssetType.SplinePath"/> references stored for this asset.
    /// </summary>
    /// <remarks>
    /// When disagreements with <see cref="SplinePathAsset.BackwardAssetIds"/>.Count exist, this field wins during serialization.
    /// </remarks>
    ushort BackwardCount { get; set; }

    /// <summary>
    /// Runtime pointer to the forward IDs array. Preserved from disk.
    /// </summary>
    uint ForwardIdsPointer { get; set; }

    /// <summary>
    /// Runtime pointer to the backward IDs array. Preserved from disk.
    /// </summary>
    uint BackwardIdsPointer { get; set; }
}
