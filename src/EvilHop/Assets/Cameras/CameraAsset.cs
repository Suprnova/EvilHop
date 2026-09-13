using EvilHop.Common;
using EvilHop.Primitives;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="BaseAsset"/> defining a camera the game can cut to, such as during a conversation or
/// a scripted moment.
/// </summary>
/// <remarks>
/// <para>
/// Not instantiated directly, instead used by <see cref="FollowCameraAsset"/>,
/// <see cref="ShoulderCameraAsset"/>, <see cref="StaticCameraAsset"/>, <see cref="PathCameraAsset"/>,
/// and <see cref="StaticFollowCameraAsset"/> branching based on <see cref="Kind"/>.
/// </para>
/// <seealso href="https://heavyironmodding.org/wiki/CAM">Heavy Iron Modding documentation</seealso>
/// </remarks>
public abstract partial class CameraAsset() : BaseAsset(AssetType.Camera), IPhysicalCameraAsset
{
    /// <summary>The camera's position.</summary>
    public Vector3 Position { get; set; }

    /// <summary>The camera's normalized forward vector.</summary>
    public Vector3 Forward { get; set; }

    /// <summary>The camera's normalized up vector.</summary>
    public Vector3 Up { get; set; }

    /// <summary>The camera's normalized left vector.</summary>
    public Vector3 Left { get; set; }

    /// <summary>The offset from <see cref="Position"/> the camera actually views from.</summary>
    public Vector3 ViewOffset { get; set; }

    /// <summary>The number of frames to ease in over when this camera becomes active. Usually 30.</summary>
    public short OffsetStartFrames { get; set; }

    /// <summary>The number of frames to ease out over when this camera stops being active. Usually 45.</summary>
    public short OffsetEndFrames { get; set; }

    /// <summary>The camera's field of view, in degrees.</summary>
    public float Fov { get; set; }

    /// <summary>The time, in seconds, it takes to move the camera into this position.</summary>
    public float TransitionTime { get; set; }

    /// <summary>How the camera eases into position over <see cref="TransitionTime"/>.</summary>
    public CameraTransitionType TransitionType { get; set; }

    /// <summary>Unknown.</summary>
    /// TODO: probably controls a fade from black
    public float FadeUp { get; set; }

    /// <summary>Unknown.</summary>
    /// TODO: probably controls a fade to black
    public float FadeDown { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the first <see cref="AssetType.Marker"/> associated with this
    /// camera, if any.
    /// </summary>
    public AssetId MarkerId1 { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the second <see cref="AssetType.Marker"/> associated with this
    /// camera, if any.
    /// </summary>
    public AssetId MarkerId2 { get; set; }

    /// <summary>
    /// Which camera type this asset is, determining which concrete <see cref="CameraAsset"/> subclass
    /// it can be.
    /// </summary>
    public abstract CameraKind Kind { get; }

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalCameraAsset Physical => this;

    private protected uint _cameraFlags;
    uint IPhysicalCameraAsset.CameraFlags { get => _cameraFlags; set => _cameraFlags = value; }

    private protected uint _validFlags;
    uint IPhysicalCameraAsset.ValidFlags { get => _validFlags; set => _validFlags = value; }

    /// <summary>
    /// The size, in bytes, of the type-specific region every <see cref="CameraKind"/> shares.
    /// </summary>
    private protected const int TypeDataSize = 24;

    /// <summary>
    /// Reads this camera's type-specific fields from a reader scoped to exactly
    /// <see cref="TypeDataSize"/> bytes.
    /// </summary>
    private protected abstract void ReadTypeFields(EndianReader reader);

    /// <summary>
    /// Writes this camera's type-specific fields, always exactly <see cref="TypeDataSize"/> bytes.
    /// </summary>
    private protected abstract void WriteTypeFields(EndianWriter writer);

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.Camera"/> is known to be read by.
    /// </summary>
    /// <remarks>
    /// <see cref="GameVersion.N100F"/> stores a shorter, differently laid out <c>CAM</c> and is not
    /// yet modelled.
    /// </remarks>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.BFBB,
        GameVersion.TSSM,
        GameVersion.Incredibles,
        GameVersion.ROTU,
        GameVersion.Ratatouille,
    };
}

/// <summary>
/// An explicit interface used to interact with <see cref="CameraAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalCameraAsset : IPhysicalBaseAsset
{
    /// <summary>Unknown. Usually 0.</summary>
    uint CameraFlags { get; set; }

    /// <summary>
    /// Unknown.
    /// </summary>
    uint ValidFlags { get; set; }
}

/// <summary>
/// Represents all known values for <see cref="CameraAsset.Kind"/>.
/// </summary>
public enum CameraKind : byte
{
    /// <summary>A <see cref="FollowCameraAsset"/>.</summary>
    Follow = 0,
    /// <summary>A <see cref="ShoulderCameraAsset"/>.</summary>
    Shoulder = 1,
    /// <summary>A <see cref="StaticCameraAsset"/>.</summary>
    Static = 2,
    /// <summary>A <see cref="PathCameraAsset"/>.</summary>
    Path = 3,
    /// <summary>A <see cref="StaticFollowCameraAsset"/>.</summary>
    StaticFollow = 4,
}

/// <summary>
/// Represents all known values for <see cref="CameraAsset.TransitionType"/> and
/// <see cref="CameraCurveAsset.TransitionType"/>.
/// </summary>
public enum CameraTransitionType
{
    /// <summary>No transition.</summary>
    None = 0,
    /// <summary>Interpolates using the first easing curve.</summary>
    Interp1 = 1,
    /// <summary>Interpolates using the second easing curve.</summary>
    Interp2 = 2,
    /// <summary>Interpolates using the third easing curve.</summary>
    Interp3 = 3,
    /// <summary>Interpolates using the fourth easing curve.</summary>
    Interp4 = 4,
    /// <summary>Interpolates linearly.</summary>
    Linear = 5,
    /// <summary><see cref="Interp1"/>, reversed.</summary>
    Interp1Rev = 6,
    /// <summary><see cref="Interp2"/>, reversed.</summary>
    Interp2Rev = 7,
    /// <summary><see cref="Interp3"/>, reversed.</summary>
    Interp3Rev = 8,
    /// <summary><see cref="Interp4"/>, reversed.</summary>
    Interp4Rev = 9,
}
