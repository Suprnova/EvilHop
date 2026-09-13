using EvilHop.Primitives;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="CameraAsset"/> that trails behind its target at a fixed distance and height,
/// swinging around as the target turns.
/// </summary>
public sealed class FollowCameraAsset : CameraAsset
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
    /// TODO: hallucination? what does this mean?
    public float EndSpeed { get; set; }

    /// <inheritdoc/>
    public override CameraKind Kind => CameraKind.Follow;

    private protected override void ReadTypeFields(EndianReader reader)
    {
        Rotation = reader.ReadSingle();
        Distance = reader.ReadSingle();
        Height = reader.ReadSingle();
        RubberBand = reader.ReadSingle();
        StartSpeed = reader.ReadSingle();
        EndSpeed = reader.ReadSingle();
    }

    private protected override void WriteTypeFields(EndianWriter writer)
    {
        writer.Write(Rotation);
        writer.Write(Distance);
        writer.Write(Height);
        writer.Write(RubberBand);
        writer.Write(StartSpeed);
        writer.Write(EndSpeed);
    }
}
