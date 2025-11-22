using System.Numerics;

namespace EvilHop.Assets;

[Flags]
public enum EntityFlags : byte
{
    None = 0,
    Visible = 1 << 0,
    Stackable = 1 << 1,
    Unknown = 1 << 3,
    NoShadow = 1 << 6
}

[Flags]
public enum CollisionFlags : byte
{
    None = 0,
    PreciseCollision = 1 << 1,
    Unknown = 1 << 2,
    Grabbable = 1 << 3,
    Hittable = 1 << 4,
    AnimateCollision = 1 << 5,
    LedgeGrabbable = 1 << 7
}

public abstract class EntityAsset : BaseAsset
{
    public EntityFlags Flags { get; set; }
    public byte SubType { get; set; }
    public byte PFlags { get; set; } = 0;
    public CollisionFlags CollisionFlags { get; set; }
    public bool HasPadding { get; set; }
    public uint SurfaceID { get; set; }
    public Vector3 Angle { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 Scale { get; set; }
}
