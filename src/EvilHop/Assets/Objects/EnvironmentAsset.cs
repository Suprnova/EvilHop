using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Buffers.Binary;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>
/// The playable level itself: the <see cref="AssetType.JSP"/> geometry it loads, the weather and
/// lighting applied over it, and the starting <see cref="AssetType.Camera"/>.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/ENV">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class EnvironmentAsset() : BaseAsset(AssetType.Environment), IPhysicalEnvironmentAsset
{
    /// <summary>
    /// The <see cref="AssetType.JSP"/> this environment loads as its main level geometry.
    /// </summary>
    /// TODO: pretty sure this is BSP in N100F, and BFBB might support both
    public AssetId BspId { get; set; }

    /// <summary>
    /// The <see cref="AssetType.Camera"/> active when the level starts.
    /// </summary>
    public AssetId StartCameraId { get; set; }

    /// <summary>
    /// Which weather effect plays over this environment.
    /// </summary>
    public ClimateFlags ClimateFlags { get; set; }

    /// <summary>
    /// The low end of <see cref="ClimateFlags"/>'s effect strength.
    /// </summary>
    public float ClimateStrengthMin { get; set; }

    /// <summary>
    /// The high end of <see cref="ClimateFlags"/>'s effect strength.
    /// </summary>
    public float ClimateStrengthMax { get; set; }

    /// <summary>
    /// The <see cref="AssetType.LightKit"/> applied to <see cref="BspId"/>'s level geometry.
    /// </summary>
    public AssetId BspLightKitId { get; set; }

    /// <summary>
    /// The <see cref="AssetType.LightKit"/> applied to the entities placed within the level.
    /// </summary>
    public AssetId ObjectLightKitId { get; set; }

    /// <summary>
    /// A secondary <see cref="AssetType.JSP"/> loaded as collision geometry, if any, separate from
    /// <see cref="BspId"/>'s render geometry.
    /// </summary>
    public AssetId BspCollisionId { get; set; }

    /// <summary>
    /// A secondary <see cref="AssetType.JSP"/> loaded for effects, if any.
    /// </summary>
    public AssetId BspFxId { get; set; }

    /// <summary>
    /// A secondary <see cref="AssetType.JSP"/> loaded for camera occlusion, if any.
    /// </summary>
    public AssetId BspCameraId { get; set; }

    /// <summary>
    /// The <see cref="AssetType.SurfaceMapper"/> registered for <see cref="BspId"/>'s texture
    /// animations.
    /// </summary>
    public AssetId BspMapperId { get; set; }

    /// <summary>
    /// The <see cref="AssetType.SurfaceMapper"/> for <see cref="BspCollisionId"/>, if any.
    /// </summary>
    public AssetId BspMapperCollisionId { get; set; }

    /// <summary>
    /// The <see cref="AssetType.SurfaceMapper"/> for <see cref="BspFxId"/>, if any.
    /// </summary>
    public AssetId BspMapperFxId { get; set; }

    /// <summary>
    /// The minimum corner of the level's bounding box. Not present in <see cref="GameVersion.N100F"/>
    /// or <see cref="GameVersion.BFBB"/>.
    /// </summary>
    public Vector3 MinBounds { get; set; }

    /// <summary>
    /// The maximum corner of the level's bounding box. Not present in <see cref="GameVersion.N100F"/>
    /// or <see cref="GameVersion.BFBB"/>.
    /// </summary>
    public Vector3 MaxBounds { get; set; }

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalEnvironmentAsset Physical => this;

    private uint _environmentFlags;
    uint IPhysicalEnvironmentAsset.EnvironmentFlags { get => _environmentFlags; set => _environmentFlags = value; }

    private float _loldHeight;
    float IPhysicalEnvironmentAsset.LoldHeight { get => _loldHeight; set => _loldHeight = value; }

    internal static EnvironmentAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new EnvironmentAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        asset.BspId = reader.ReadAssetId();
        asset.StartCameraId = reader.ReadAssetId();
        asset.ClimateFlags = (ClimateFlags)reader.ReadUInt32();
        asset.ClimateStrengthMin = reader.ReadSingle();
        asset.ClimateStrengthMax = reader.ReadSingle();
        asset.BspLightKitId = reader.ReadAssetId();
        asset.ObjectLightKitId = reader.ReadAssetId();
        asset.Physical.EnvironmentFlags = reader.ReadUInt32();
        asset.BspCollisionId = reader.ReadAssetId();
        asset.BspFxId = reader.ReadAssetId();
        asset.BspCameraId = reader.ReadAssetId();
        asset.BspMapperId = reader.ReadAssetId();
        asset.BspMapperCollisionId = reader.ReadAssetId();
        asset.BspMapperFxId = reader.ReadAssetId();

        if (profile.Game is not GameVersion.N100F)
            asset.Physical.LoldHeight = BinaryPrimitives.ReadSingleLittleEndian(reader.ReadBytes(4));

        if (profile.Game is GameVersion.TSSM or GameVersion.Incredibles or GameVersion.ROTU or GameVersion.Ratatouille)
        {
            asset.MinBounds = reader.ReadVector3();
            asset.MaxBounds = reader.ReadVector3();
        }

        LinkSerialization.Read(asset, reader, asset.Physical.LinkCount);
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(EnvironmentAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);

        writer.Write(asset.BspId);
        writer.Write(asset.StartCameraId);
        writer.Write((uint)asset.ClimateFlags);
        writer.Write(asset.ClimateStrengthMin);
        writer.Write(asset.ClimateStrengthMax);
        writer.Write(asset.BspLightKitId);
        writer.Write(asset.ObjectLightKitId);
        writer.Write(asset.Physical.EnvironmentFlags);
        writer.Write(asset.BspCollisionId);
        writer.Write(asset.BspFxId);
        writer.Write(asset.BspCameraId);
        writer.Write(asset.BspMapperId);
        writer.Write(asset.BspMapperCollisionId);
        writer.Write(asset.BspMapperFxId);

        if (profile.Game is not GameVersion.N100F)
        {
            Span<byte> loldHeight = stackalloc byte[4];
            BinaryPrimitives.WriteSingleLittleEndian(loldHeight, asset.Physical.LoldHeight);
            writer.Write(loldHeight);
        }

        if (profile.Game is GameVersion.TSSM or GameVersion.Incredibles or GameVersion.ROTU or GameVersion.Ratatouille)
        {
            writer.Write(asset.MinBounds);
            writer.Write(asset.MaxBounds);
        }

        LinkSerialization.Write(asset, writer);
        writer.Write(asset.GetUnparsedTail());
    }
}

/// <summary>
/// An explicit interface used to interact with <see cref="EnvironmentAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalEnvironmentAsset : IPhysicalBaseAsset
{
    /// <summary>
    /// Unknown. Called <c>padF1</c> in <see cref="GameVersion.BFBB"/>'s decompiled source, where it
    /// is always 0; renamed <c>flags</c> from <see cref="GameVersion.TSSM"/> onward, where it is
    /// occasionally 1.
    /// </summary>
    uint EnvironmentFlags { get; set; }

    /// <summary>
    /// Unknown.. Not present in <see cref="GameVersion.N100F"/>.
    /// </summary>
    float LoldHeight { get; set; }
}

/// <summary>
/// Represents all known values for <see cref="EnvironmentAsset.ClimateFlags"/>.
/// </summary>
[Flags]
public enum ClimateFlags : uint
{
    /// <summary>
    /// No weather effect plays.
    /// </summary>
    None = 0,
    /// <summary>
    /// Rain plays. Takes priority over <see cref="Snow"/>..
    /// </summary>
    Rain = 1 << 0,
    /// <summary>
    /// Snow plays.
    /// </summary>
    Snow = 1 << 1,
}
