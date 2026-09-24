using EvilHop.Common;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>
/// The playable level itself: the <see cref="AssetType.JSP"/> geometry it loads, the weather and
/// lighting applied over it, and the starting <see cref="AssetType.Camera"/>.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/ENV">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed partial class EnvironmentAsset() : BaseAsset(AssetType.Environment, baseType: 0x05), Physical.IEnvironmentAsset
{
    /// <summary>
    /// The <see cref="AssetType.JSP"/> this environment loads as its main level geometry.
    /// </summary>
    public AssetId BspId { get; set; }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.Environment"/> is known to be read by.
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

    /// <summary>
    /// The <see cref="AssetType.Camera"/> active when the level starts.
    /// </summary>
    public AssetId StartCameraId { get; set; }

    /// <summary>
    /// Which weather effect plays over this environment.
    /// </summary>
    public Weather Climate { get; set; }

    /// <summary>
    /// The low end of <see cref="Climate"/>'s effect strength.
    /// </summary>
    public float ClimateStrengthMin { get; set; }

    /// <summary>
    /// The high end of <see cref="Climate"/>'s effect strength.
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
    public override Physical.IEnvironmentAsset Physical => this;

    private uint _environmentFlags;
    uint Physical.IEnvironmentAsset.EnvironmentFlags { get => _environmentFlags; set => _environmentFlags = value; }

    private float _loldHeight;
    float Physical.IEnvironmentAsset.LoldHeight { get => _loldHeight; set => _loldHeight = value; }

    /// <summary>
    /// Flags controlling weather and environmental effects such as rain, snow, or wind.
    /// </summary>
    [Flags]
    public enum Weather : uint
    {
        /// <summary>
        /// No weather effect plays.
        /// </summary>
        None = 0,
        /// <summary>
        /// Rain plays. Takes priority over <see cref="Snow"/>.
        /// </summary>
        Rain = 1 << 0,
        /// <summary>
        /// Snow plays.
        /// </summary>
        Snow = 1 << 1,
    }
}

public static partial class Physical
{
    /// <summary>
    /// An explicit interface used to interact with <see cref="EnvironmentAsset"/>'s underlying values.
    /// </summary>
    public interface IEnvironmentAsset : IBaseAsset
    {
        /// <summary>
        /// Unknown.
        /// </summary>
        uint EnvironmentFlags { get; set; }

        /// <summary>
        /// Unknown. Not present in <see cref="GameVersion.N100F"/>.
        /// </summary>
        float LoldHeight { get; set; }
    }
}
