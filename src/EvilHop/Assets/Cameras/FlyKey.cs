using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>
/// One frame of a <see cref="FlyAsset"/>: the camera's transform, aperture, and focal length at a
/// given frame.
/// </summary>
public sealed class FlyKey
{
    /// <summary>The frame number this key applies at, at 30 FPS.</summary>
    public int Frame { get; set; }

    /// <summary>The camera's normalized right vector.</summary>
    public Vector3 Right { get; set; }

    /// <summary>The camera's normalized up vector.</summary>
    public Vector3 Up { get; set; }

    /// <summary>The camera's normalized forward vector.</summary>
    public Vector3 At { get; set; }

    /// <summary>The camera's position.</summary>
    public Vector3 Position { get; set; }

    /// <summary>The camera's aperture (view window half-extents).</summary>
    public Vector2 Aperture { get; set; }

    /// <summary>The camera's focal length.</summary>
    public float FocalLength { get; set; }

    internal static FlyKey Read(EndianReader reader, FormatProfile _) => new()
    {
        Frame = reader.ReadInt32(),
        Right = reader.ReadVector3(),
        Up = reader.ReadVector3(),
        At = reader.ReadVector3(),
        Position = reader.ReadVector3(),
        Aperture = reader.ReadVector2(),
        FocalLength = reader.ReadSingle(),
    };

    internal static void Write(FlyKey value, EndianWriter writer, FormatProfile _)
    {
        writer.Write(value.Frame);
        writer.Write(value.Right);
        writer.Write(value.Up);
        writer.Write(value.At);
        writer.Write(value.Position);
        writer.Write(value.Aperture);
        writer.Write(value.FocalLength);
    }
}
