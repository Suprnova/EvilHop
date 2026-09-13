using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// An <see cref="EntityAsset"/> the player can stand on, whose <see cref="Motion"/> describes how it
/// moves or reacts - from sliding along a path, to breaking away, to launching the player upward.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/PLAT">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class PlatformAsset() : EntityAsset(AssetType.Platform), IHasModel, IHasSurface, IHasAnimList, IPhysicalPlatformAsset
{
    /// <summary>
    /// This platform's behavior flags.
    /// </summary>
    public PlatformFlags Flags { get; set; }

    /// <summary>
    /// How this platform moves or reacts. Determines <see cref="IPhysicalPlatformAsset.PlatformType"/>.
    /// </summary>
    public Motion Motion { get; set; } = new FullyManipulableMotion();

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalPlatformAsset Physical => this;

    private PlatformType? _overriddenPlatformType;
    PlatformType IPhysicalPlatformAsset.PlatformType
    {
        get => _overriddenPlatformType ?? Motion.PlatformType;
        set => _overriddenPlatformType = value == Motion.PlatformType ? null : value;
    }

    private byte? _overriddenSubtype;
    private protected override byte Subtype
    {
        get => _overriddenSubtype ?? DerivedSubtype;
        set => _overriddenSubtype = value == DerivedSubtype ? null : value;
    }

    /// <summary>
    /// The subtype paired with <see cref="IPhysicalPlatformAsset.PlatformType"/>: 0 for the types
    /// grouped together as a plain "Platform", and the type itself for every other.
    /// </summary>
    private byte DerivedSubtype => Physical.PlatformType is PlatformType.ExtendRetract or PlatformType.Orbit
        or PlatformType.Spline or PlatformType.MovePoint or PlatformType.FullyManipulable
        ? (byte)0
        : (byte)Physical.PlatformType;

    AssetId IHasModel.ModelId { get => Physical.ModelId; set => Physical.ModelId = value; }
    AssetId IHasSurface.SurfaceId { get => Physical.SurfaceId; set => Physical.SurfaceId = value; }
    AssetId IHasAnimList.AnimListId { get => Physical.AnimListId; set => Physical.AnimListId = value; }
    
    /// <exception cref="InvalidDataException">
    /// The stored <see cref="PlatformType"/> is unknown, or its blocks don't hold what it selects.
    /// </exception>
    internal static PlatformAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new PlatformAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);
        EntityAssetPrefix.Read(asset, reader, profile.EntityHasPadding);

        // Subtype derives from PlatformType, which derives from Motion - reassigned once both are read.
        byte subtype = asset.Physical.Subtype;
        var platformType = (PlatformType)reader.ReadByte();
        reader.ReadByte(); // padding, always zero
        asset.Flags = (PlatformFlags)reader.ReadInt16();

        if (platformType <= PlatformType.Pendulum)
        {
            PlatformMotion.ReadEmpty(reader, profile.Game);
            asset.Motion = EntityMotion.Read(reader, profile.Game);
        }
        else
        {
            var motion = PlatformMotion.Read(reader, platformType, profile.Game);
            motion.Flags = EntityMotion.ReadEmpty(reader, profile.Game);
            asset.Motion = motion;
        }

        asset.Physical.PlatformType = platformType;
        asset.Physical.Subtype = subtype;

        LinkSerialization.Read(asset, reader, asset.Physical.LinkCount);
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(PlatformAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);
        EntityAssetPrefix.Write(asset, writer, profile.EntityHasPadding);

        writer.Write((byte)asset.Physical.PlatformType);
        writer.Write((byte)0); // padding
        writer.Write((short)asset.Flags);

        switch (asset.Motion)
        {
            case EntityMotion motion:
                PlatformMotion.WriteEmpty(writer, profile.Game);
                motion.Write(writer, profile.Game);
                break;
            case PlatformMotion motion:
                motion.Write(writer, profile.Game);
                EntityMotion.WriteEmpty(writer, profile.Game, motion.Flags);
                break;
        }

        LinkSerialization.Write(asset, writer);
        writer.Write(asset.GetUnparsedTail());
    }
}

/// <summary>
/// An explicit interface used to interact with <see cref="PlatformAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalPlatformAsset : IPhysicalEntityAsset
{
    /// <summary>
    /// The platform's type, selecting how its type-specific block is read. Follows
    /// <see cref="PlatformAsset.Motion"/>, and is followed in turn by
    /// <see cref="IPhysicalEntityAsset.Subtype"/>.
    /// </summary>
    /// <remarks>
    /// When disagreements with <see cref="PlatformAsset.Motion"/> exist, this field wins during
    /// serialization. Both blocks are still written from <see cref="PlatformAsset.Motion"/>.
    /// </remarks>
    PlatformType PlatformType { get; set; }
}

/// <summary>
/// Represents all known values for <see cref="IPhysicalPlatformAsset.PlatformType"/>, each named for
/// the <see cref="Motion"/> that determines it.
/// </summary>
public enum PlatformType : byte
{
    /// <summary>An <see cref="ExtendRetractMotion"/>.</summary>
    ExtendRetract = 0,
    /// <summary>An <see cref="OrbitMotion"/>.</summary>
    Orbit = 1,
    /// <summary>A <see cref="SplineMotion"/>.</summary>
    Spline = 2,
    /// <summary>A <see cref="MovePointMotion"/>.</summary>
    MovePoint = 3,
    /// <summary>A <see cref="MechanismMotion"/>.</summary>
    Mechanism = 4,
    /// <summary>A <see cref="PendulumMotion"/>.</summary>
    Pendulum = 5,
    /// <summary>A <see cref="ConveyorBeltMotion"/>.</summary>
    ConveyorBelt = 6,
    /// <summary>A <see cref="FallingMotion"/>.</summary>
    Falling = 7,
    /// <summary>A <see cref="ForwardReturnMotion"/>.</summary>
    ForwardReturn = 8,
    /// <summary>A <see cref="BreakawayMotion"/>.</summary>
    Breakaway = 9,
    /// <summary>A <see cref="SpringboardMotion"/>.</summary>
    Springboard = 10,
    /// <summary>A <see cref="TeeterTotterMotion"/>.</summary>
    TeeterTotter = 11,
    /// <summary>A <see cref="PaddleMotion"/>.</summary>
    Paddle = 12,
    /// <summary>A <see cref="FullyManipulableMotion"/>.</summary>
    FullyManipulable = 13,
}

/// <summary>
/// Represents all known values for <see cref="PlatformAsset.Flags"/>.
/// </summary>
/// <remarks>
/// Per <see cref="GameVersion.BFBB"/>. <see cref="GameVersion.TSSM"/> is documented as storing a
/// collision type here instead, and <see cref="GameVersion.ROTU"/> sets bits whose meaning is unknown.
/// </remarks>
/// TODO: not sure what the "storing a collision type here instead" matters to the documentation of
/// the enum itself. The enum should be game-agnostic (see LayerType), matters like that are a job
/// for the serialization method.
[Flags]
public enum PlatformFlags : ushort
{
    /// <summary>
    /// No flags are set.
    /// </summary>
    None = 0,
    /// <summary>
    /// The platform shakes when the player lands on or leaves it.
    /// </summary>
    Shake = 1 << 0,
    /// <summary>
    /// The platform is solid.
    /// </summary>
    Solid = 1 << 2,
}
