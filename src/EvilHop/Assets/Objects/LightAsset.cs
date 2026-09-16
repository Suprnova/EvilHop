using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>
/// A dynamic light source, optionally following <see cref="AttachedEntityId"/>'s position each frame.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/LITE">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class LightAsset() : BaseAsset(AssetType.Light)
{
    /// <summary>This light's type.</summary>
    public LightType LightType { get; set; }

    /// <summary>Which flickering, strobing, dimming, or color-cycling effect this light plays.</summary>
    public LightEffect LightEffect { get; set; }

    /// <summary>Whether this light is turned on, and whether it affects level geometry.</summary>
    public LightFlags Flags { get; set; }

    /// <summary>This light's color.</summary>
    public Rgba Color { get; set; }

    /// <summary>This light's rotation.</summary>
    public Vector3 Direction { get; set; }

    /// <summary>The cone angle of a <see cref="LightType.Spot"/> light.</summary>
    /// TODO: the wiki claims radians, but every value observed in the N100F corpus is 45 or 90 -
    /// round numbers for degrees, not radians. Unconfirmed either way: every real archive checked
    /// uses a non-Spot LightType, for which this field goes unused.
    public float ConeAngle { get; set; }

    /// <summary>This light's position, ignored while attached to <see cref="AttachedEntityId"/>.</summary>
    public Vector3 Position { get; set; }

    /// <summary>How far this light reaches.</summary>
    public float Radius { get; set; }

    /// <summary>
    /// An optional entity this light follows, moved to its position plus one Y unit every frame
    /// instead of staying at <see cref="Position"/>.
    /// </summary>
    public AssetId AttachedEntityId { get; set; }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.Light"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.N100F,
    };

    internal static LightAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile _)
    {
        var asset = new LightAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        asset.LightType = (LightType)reader.ReadByte();
        asset.LightEffect = (LightEffect)reader.ReadByte();
        reader.ReadInt16(); // padding, always zero
        asset.Flags = (LightFlags)reader.ReadUInt32();
        asset.Color = reader.ReadRgba();
        asset.Direction = reader.ReadVector3();
        asset.ConeAngle = reader.ReadSingle();
        asset.Position = reader.ReadVector3();
        asset.Radius = reader.ReadSingle();
        asset.AttachedEntityId = reader.ReadAssetId();

        LinkSerialization.Read(asset, reader, asset.Physical.LinkCount);
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(LightAsset asset, EndianWriter writer, FormatProfile _)
    {
        BaseAssetPrefix.Write(asset, writer);

        writer.Write((byte)asset.LightType);
        writer.Write((byte)asset.LightEffect);
        writer.Write((short)0); // padding
        writer.Write((uint)asset.Flags);
        writer.Write(asset.Color);
        writer.Write(asset.Direction);
        writer.Write(asset.ConeAngle);
        writer.Write(asset.Position);
        writer.Write(asset.Radius);
        writer.Write(asset.AttachedEntityId);

        LinkSerialization.Write(asset, writer);
        writer.Write(asset.GetUnparsedTail());
    }
}

/// <summary>A <see cref="LightAsset"/>'s type.</summary>
public enum LightType : byte
{
    /// <summary>A light radiating from <see cref="LightAsset.Position"/> in every direction, out to <see cref="LightAsset.Radius"/>.</summary>
    Point = 0,

    /// <summary>
    /// A light radiating from <see cref="LightAsset.Position"/> in a cone along
    /// <see cref="LightAsset.Direction"/>, out to <see cref="LightAsset.Radius"/> and
    /// <see cref="LightAsset.ConeAngle"/>.
    /// </summary>
    Spot = 1,

    /// <summary>Functionally identical to <see cref="Point"/> in decompiled source.</summary>
    /// TODO: validate against decompiled source; never observed in the N100F corpus
    Point2 = 2,

    /// <summary>Functionally identical to <see cref="Point"/> in decompiled source.</summary>
    /// TODO: validate against decompiled source; the only value observed in the N100F corpus
    Point3 = 3,
}

/// <summary>Which flickering, strobing, dimming, or color-cycling effect a <see cref="LightAsset"/> plays.</summary>
public enum LightEffect : byte
{
    /// <summary>No effect.</summary>
    None = 0,

    /// <summary>Unknown. The wiki also labels this the same as <see cref="None"/>.</summary>
    /// TODO: validate against decompiled source
    Unknown1 = 1,

    /// <summary>Flickers slowly.</summary>
    FlickerSlow = 2,

    /// <summary>Flickers.</summary>
    Flicker = 3,

    /// <summary>Flickers erratically.</summary>
    FlickerErratic = 4,

    /// <summary>Strobes slowly.</summary>
    StrobeSlow = 5,

    /// <summary>Strobes.</summary>
    Strobe = 6,

    /// <summary>Strobes fast.</summary>
    StrobeFast = 7,

    /// <summary>Dims slowly.</summary>
    DimSlow = 8,

    /// <summary>Dims.</summary>
    Dim = 9,

    /// <summary>Dims fast.</summary>
    DimFast = 10,

    /// <summary>Dims to half brightness slowly.</summary>
    HalfDimSlow = 11,

    /// <summary>Dims to half brightness.</summary>
    HalfDim = 12,

    /// <summary>Dims to half brightness fast.</summary>
    HalfDimFast = 13,

    /// <summary>Cycles through random colors slowly.</summary>
    RandomColSlow = 14,

    /// <summary>Cycles through random colors.</summary>
    RandomCol = 15,

    /// <summary>Cycles through random colors fast.</summary>
    RandomColFast = 16,

    /// <summary>Flickers with the warm, uneven glow of a cauldron fire.</summary>
    Cauldron = 17,
}

/// <summary>Toggles a <see cref="LightAsset"/> on or off, and whether it affects level geometry.</summary>
[Flags]
public enum LightFlags : uint
{
    /// <summary>The light is turned off.</summary>
    None = 0,

    /// <summary>Unknown.</summary>
    /// TODO: validate against decompiled source
    Unknown1 = 1 << 0,

    /// <summary>Unknown.</summary>
    /// TODO: validate against decompiled source
    Unknown2 = 1 << 1,

    /// <summary>Unknown.</summary>
    /// TODO: validate against decompiled source
    Unknown4 = 1 << 2,

    /// <summary>The light affects level geometry, not just entities.</summary>
    Environment = 1 << 3,

    /// <summary>Unknown.</summary>
    /// TODO: validate against decompiled source
    Unknown16 = 1 << 4,

    /// <summary>The light is turned on.</summary>
    On = 1 << 5,
}
