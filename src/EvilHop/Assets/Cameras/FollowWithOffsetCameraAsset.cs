using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="FollowCameraAsset"/> whose view the player can shift with the camera controls.
/// </summary>
/// <remarks>
/// Camera input shifts the view by amounts scaled by <see cref="FollowCameraAsset.StartSpeed"/> and
/// <see cref="FollowCameraAsset.EndSpeed"/>.
/// </remarks>
public sealed class FollowWithOffsetCameraAsset() : FollowCameraAsset
{
    /// <inheritdoc/>
    public override CameraKind Kind => CameraKind.FollowWithOffset;

    internal static new FollowWithOffsetCameraAsset Read(EndianReader reader, FormatProfile _) =>
        ReadFields(new FollowWithOffsetCameraAsset(), reader);
}
