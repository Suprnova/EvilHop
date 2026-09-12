using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>
/// A volume of space that detects entry and exit - by the player or another
/// <see cref="EntityAsset"/> - and fires <see cref="BaseAsset.Links"/> accordingly.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="EntityAsset.Position"/> is the pivot this trigger rotates around by
/// <see cref="EntityAsset.Angle"/>, rather than a corner or center of its own.
/// </para>
/// <para>
/// <see cref="Shape"/> selects how <see cref="TriggerPosition0"/> and <see cref="TriggerPosition1"/>
/// are interpreted: absolute box corners, or a sphere's center and radius.
/// <see cref="TriggerShape.Cylinder"/> and <see cref="TriggerShape.VSphere"/> are defined in
/// decompiled source but never observed in any real archive.
/// </para>
/// <seealso href="https://heavyironmodding.org/wiki/TRIG">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class TriggerAsset : EntityAsset
{
    /// <summary>
    /// Which shape this trigger's volume is, and how <see cref="TriggerPosition0"/>/
    /// <see cref="TriggerPosition1"/> are interpreted.
    /// </summary>
    public TriggerShape Shape { get; set; }

    private protected override byte Subtype { get => (byte)Shape; set => Shape = (TriggerShape)value; }

    /// <summary>
    /// For <see cref="TriggerShape.Box"/>, the front-bottom-left corner, in absolute coordinates.
    /// For <see cref="TriggerShape.Sphere"/>, the center, in absolute coordinates.
    /// </summary>
    public Vector3 TriggerPosition0 { get; set; }

    /// <summary>
    /// For <see cref="TriggerShape.Box"/>, the back-top-right corner, in absolute coordinates. For
    /// <see cref="TriggerShape.Sphere"/>, the radius in <see cref="Vector3.X"/>; <see cref="Vector3.Y"/>
    /// and <see cref="Vector3.Z"/> are always 0.
    /// </summary>
    public Vector3 TriggerPosition1 { get; set; }

    /// <summary>
    /// Unknown. Always <see cref="Vector3.Zero"/> for <see cref="TriggerShape.Sphere"/>. For
    /// <see cref="TriggerShape.Box"/>, uninitialized garbage in <see cref="GameVersion.N100F"/>
    /// through <see cref="GameVersion.BFBB"/>; real, varying (but unexplained) values from
    /// <see cref="GameVersion.TSSM"/> onward.
    /// </summary>
    public Vector3 TriggerPosition2 { get; set; }

    /// <summary>
    /// Unknown. Always <see cref="Vector3.Zero"/> for <see cref="TriggerShape.Sphere"/>. For
    /// <see cref="TriggerShape.Box"/>, uninitialized garbage in <see cref="GameVersion.N100F"/>
    /// through <see cref="GameVersion.BFBB"/>; real, varying (but unexplained) values from
    /// <see cref="GameVersion.TSSM"/> onward.
    /// </summary>
    public Vector3 TriggerPosition3 { get; set; }

    /// <summary>
    /// The direction this trigger must be approached from for <see cref="Flags"/>'s direction gate
    /// to pass. Usually <c>(0, -0, 1)</c>.
    /// </summary>
    public Vector3 Direction { get; set; }

    /// <summary>
    /// Usually 0. In decompiled <see cref="GameVersion.BFBB"/> source, bit 0 restricts this trigger
    /// to only fire when approached from <see cref="Direction"/>. Real <see cref="GameVersion.ROTU"/>
    /// archives carry a wide range of other values in this field with no explanation in available
    /// decompiled source.
    /// </summary>
    public uint Flags { get; set; }

    internal TriggerAsset() { }

    internal static TriggerAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new TriggerAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);
        EntityAssetPrefix.Read(asset, reader, profile.EntityHasPadding);

        asset.TriggerPosition0 = reader.ReadVector3();
        asset.TriggerPosition1 = reader.ReadVector3();
        asset.TriggerPosition2 = reader.ReadVector3();
        asset.TriggerPosition3 = reader.ReadVector3();
        asset.Direction = reader.ReadVector3();
        asset.Flags = reader.ReadUInt32();

        LinkSerialization.Read(asset, reader, asset.Physical.LinkCount);
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(TriggerAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);
        EntityAssetPrefix.Write(asset, writer, profile.EntityHasPadding);

        writer.Write(asset.TriggerPosition0);
        writer.Write(asset.TriggerPosition1);
        writer.Write(asset.TriggerPosition2);
        writer.Write(asset.TriggerPosition3);
        writer.Write(asset.Direction);
        writer.Write(asset.Flags);

        LinkSerialization.Write(asset, writer);
        writer.Write(asset.GetUnparsedTail());
    }
}

/// <summary>
/// Represents all known values for <see cref="TriggerAsset.Shape"/>.
/// </summary>
public enum TriggerShape : byte
{
    /// <summary>
    /// An axis-aligned box between <see cref="TriggerAsset.TriggerPosition0"/> and
    /// <see cref="TriggerAsset.TriggerPosition1"/>.
    /// </summary>
    Box = 0,
    /// <summary>
    /// A sphere centered on <see cref="TriggerAsset.TriggerPosition0"/> with a radius of
    /// <see cref="TriggerAsset.TriggerPosition1"/>'s <see cref="Vector3.X"/>.
    /// </summary>
    Sphere = 1,
    /// <summary>
    /// A vertical cylinder. Defined in decompiled source but never observed in any real archive.
    /// </summary>
    Cylinder = 2,
    /// <summary>
    /// Identical to <see cref="Sphere"/> in decompiled source. Never observed in any real archive.
    /// </summary>
    VSphere = 3,
}
