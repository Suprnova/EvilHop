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
/// </para>
/// <seealso href="https://heavyironmodding.org/wiki/TRIG">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class TriggerAsset() : EntityAsset(AssetType.Trigger, baseType: 0x01), IPhysicalTriggerAsset
{
    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.Trigger"/> is known to be read by.
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
    /// The direction this trigger must be approached from for <see cref="Flags"/>'s
    /// <see cref="TriggerFlags.DirectionGate"/> to pass.
    /// </summary>
    public Vector3 Direction { get; set; }

    /// <summary>
    /// This trigger's flags.
    /// </summary>
    public TriggerFlags Flags { get; set; }

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalTriggerAsset Physical => this;

    private Vector3 _triggerPosition2;
    Vector3 IPhysicalTriggerAsset.TriggerPosition2 { get => _triggerPosition2; set => _triggerPosition2 = value; }

    private Vector3 _triggerPosition3;
    Vector3 IPhysicalTriggerAsset.TriggerPosition3 { get => _triggerPosition3; set => _triggerPosition3 = value; }

    internal static TriggerAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new TriggerAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);
        EntityAssetPrefix.Read(asset, reader, profile);

        asset.TriggerPosition0 = reader.ReadVector3();
        asset.TriggerPosition1 = reader.ReadVector3();
        asset.Physical.TriggerPosition2 = reader.ReadVector3();
        asset.Physical.TriggerPosition3 = reader.ReadVector3();
        if (profile.TriggerHasDirectionAndFlags)
        {
            asset.Direction = reader.ReadVector3();
            asset.Flags = (TriggerFlags)reader.ReadUInt32();
        }

        LinkSerialization.Read(asset, reader, asset.Physical.LinkCount, profile.LinkHasExtendedFields);
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(TriggerAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);
        EntityAssetPrefix.Write(asset, writer, profile);

        writer.Write(asset.TriggerPosition0);
        writer.Write(asset.TriggerPosition1);
        writer.Write(asset.Physical.TriggerPosition2);
        writer.Write(asset.Physical.TriggerPosition3);
        if (profile.TriggerHasDirectionAndFlags)
        {
            writer.Write(asset.Direction);
            writer.Write((uint)asset.Flags);
        }

        LinkSerialization.Write(asset, writer, profile.LinkHasExtendedFields);
        writer.Write(asset.GetUnparsedTail());
    }
}

/// <summary>
/// An explicit interface used to interact with <see cref="TriggerAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalTriggerAsset : IPhysicalEntityAsset
{
    /// <summary>
    /// Unknown.
    /// </summary>
    Vector3 TriggerPosition2 { get; set; }

    /// <summary>
    /// Unknown.
    /// </summary>
    Vector3 TriggerPosition3 { get; set; }
}

/// <summary>
/// Defines the geometric volume shape (box, sphere, or cylinder) used for trigger collision detection.
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
    /// A vertical cylinder.
    /// </summary>
    Cylinder = 2,
    /// <summary>
    /// Identical to <see cref="Sphere"/>.
    /// </summary>
    VSphere = 3,
}

/// <summary>
/// Flags controlling trigger activation criteria, directionality, and player interaction.
/// </summary>
[Flags]
public enum TriggerFlags : uint
{
    /// <summary>
    /// No flags are set.
    /// </summary>
    None = 0,
    /// <summary>
    /// Restricts this trigger to only fire when approached from <see cref="TriggerAsset.Direction"/>.
    /// </summary>
    DirectionGate = 1 << 0,
}
