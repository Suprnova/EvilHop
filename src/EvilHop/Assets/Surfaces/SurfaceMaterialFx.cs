using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="SurfaceAsset"/>'s material appearance.
/// </summary>
public sealed class SurfaceMaterialFx
{
    /// <summary>
    /// Unknown.
    /// </summary>
    public SurfaceMaterialFxFlags Flags { get; set; }

    /// <summary>
    /// The bump map applied to this surface, if any.
    /// </summary>
    public AssetId BumpMapId { get; set; }

    /// <summary>
    /// The environment map applied to this surface, if any.
    /// </summary>
    public AssetId EnvMapId { get; set; }

    /// <summary>
    /// How shiny this surface's environment reflection is.
    /// </summary>
    public float Shininess { get; set; }

    /// <summary>
    /// How bumpy this surface's bump map is.
    /// </summary>
    public float Bumpiness { get; set; }

    /// <summary>
    /// A secondary map applied to this surface, if any.
    /// </summary>
    public AssetId DualMapId { get; set; }

    internal static SurfaceMaterialFx Read(EndianReader reader, FormatProfile _) => new()
    {
        Flags = (SurfaceMaterialFxFlags)reader.ReadUInt32(),
        BumpMapId = reader.ReadAssetId(),
        EnvMapId = reader.ReadAssetId(),
        Shininess = reader.ReadSingle(),
        Bumpiness = reader.ReadSingle(),
        DualMapId = reader.ReadAssetId(),
    };

    internal static void Write(SurfaceMaterialFx material, EndianWriter writer, FormatProfile _)
    {
        writer.Write((uint)material.Flags);
        writer.Write(material.BumpMapId);
        writer.Write(material.EnvMapId);
        writer.Write(material.Shininess);
        writer.Write(material.Bumpiness);
        writer.Write(material.DualMapId);
    }
}

/// <summary>
/// Flags governing material shader and texture mapping effects applied to a surface.
/// </summary>
[Flags]
public enum SurfaceMaterialFxFlags : uint
{
    /// <summary>
    /// No flags are set.
    /// </summary>
    None = 0,
}
