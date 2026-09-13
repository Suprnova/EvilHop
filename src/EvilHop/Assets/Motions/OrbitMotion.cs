using EvilHop.Common;
using EvilHop.Primitives;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>
/// An <see cref="EntityMotion"/> that circles around a center point.
/// </summary>
public sealed class OrbitMotion : EntityMotion
{
    /// <summary>The point the entity orbits around.</summary>
    public Vector3 Center { get; set; }

    /// <summary>The orbit's scale along the X axis.</summary>
    public float Width { get; set; }

    /// <summary>The orbit's scale along the Z axis.</summary>
    public float Height { get; set; }

    /// <summary>The time, in seconds, one full orbit takes.</summary>
    public float Period { get; set; }

    private protected override MotionType Type => MotionType.Orbit;

    private protected override void ReadFields(EndianReader reader, GameVersion _)
    {
        Center = reader.ReadVector3();
        Width = reader.ReadSingle();
        Height = reader.ReadSingle();
        Period = reader.ReadSingle();
    }

    private protected override void WriteFields(EndianWriter writer, GameVersion _)
    {
        writer.Write(Center);
        writer.Write(Width);
        writer.Write(Height);
        writer.Write(Period);
    }
}
