using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="CameraAsset"/> that trails behind its target at a fixed distance and height,
/// swinging around as the target turns.
/// </summary>
public class FollowCameraAsset() : CameraAsset
{
    /// <summary>The camera's rotation around its target.</summary>
    public float Rotation { get; set; }

    /// <summary>The camera's distance from its target.</summary>
    public float Distance { get; set; }

    /// <summary>The camera's height above its target.</summary>
    public float Height { get; set; }

    /// <summary>How much the camera lags behind its target before catching up.</summary>
    public float RubberBand { get; set; }

    /// <summary>The camera's initial movement speed.</summary>
    public float StartSpeed { get; set; }

    /// <summary>The camera's movement speed once fully caught up.</summary>
    public float EndSpeed { get; set; }

    /// <inheritdoc/>
    public override CameraKind Kind => CameraKind.Follow;

    internal static FollowCameraAsset Read(EndianReader reader, FormatProfile _) => ReadFields(new FollowCameraAsset(), reader);

    private protected static T ReadFields<T>(T value, EndianReader reader) where T : FollowCameraAsset
    {
        value.Rotation = reader.ReadSingle();
        value.Distance = reader.ReadSingle();
        value.Height = reader.ReadSingle();
        value.RubberBand = reader.ReadSingle();
        value.StartSpeed = reader.ReadSingle();
        value.EndSpeed = reader.ReadSingle();
        return value;
    }

    internal static void Write(FollowCameraAsset value, EndianWriter writer, FormatProfile _)
    {
        writer.Write(value.Rotation);
        writer.Write(value.Distance);
        writer.Write(value.Height);
        writer.Write(value.RubberBand);
        writer.Write(value.StartSpeed);
        writer.Write(value.EndSpeed);
    }
}
