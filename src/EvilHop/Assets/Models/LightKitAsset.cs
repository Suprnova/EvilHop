using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>
/// A set of <see cref="LightKitLight"/>s applied together to a <see cref="AssetType.JSP"/> or the
/// entities placed within it.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/LKIT">Heavy Iron Modding documentation</seealso>
/// </remarks>
/// TODO: evaluate asset category (environment?)
public sealed class LightKitAsset() : Asset(AssetType.LightKit), IPhysicalLightKitAsset
{
    /// <summary>
    /// The <see cref="AssetType.Group"/> of entities this light kit is applied to, in addition to
    /// whichever single object referenced it directly, if any.
    /// </summary>
    /// TODO: what does that second half mean
    public AssetId GroupId { get; set; }

    /// <summary>
    /// The kit's lights.
    /// </summary>
    public Collection<LightKitLight> Lights { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalLightKitAsset Physical => this;

    private uint _magic = 0x54494B4C; // "TIKL"
    uint IPhysicalLightKitAsset.Magic { get => _magic; set => _magic = value; }

    private uint? _overriddenLightCount;
    uint IPhysicalLightKitAsset.LightCount
    {
        get => _overriddenLightCount ?? (uint)Lights.Count;
        set => _overriddenLightCount = value == (uint)Lights.Count ? null : value;
    }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.LightKit"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.BFBB,
        GameVersion.TSSM,
        GameVersion.Incredibles,
        GameVersion.ROTU,
        GameVersion.Ratatouille,
    };

    internal static LightKitAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new LightKitAsset();
        AssetFields.Populate(asset, header, debug);

        asset.Physical.Magic = reader.ReadUInt32();
        asset.GroupId = reader.ReadAssetId();
        uint lightCount = reader.ReadUInt32();
        reader.ReadUInt32(); // runtime-resolved lightList, always 0

        if (profile.Game is GameVersion.ROTU or GameVersion.Ratatouille)
            reader.ReadUInt32(); // "blended" - always 0xCDCDCDCD (uninitialized) on disk, reset to false at load
            // TODO: validate against decompiled source. what data type is this really?

        for (int i = 0; i < lightCount; i++)
        {
            var light = new LightKitLight
            {
                Type = (LightKitLightType)reader.ReadUInt32(),
                Color = new RgbaColor(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
                Right = reader.ReadVector3(),
            };

            // TODO: definitely should store these homogeneous components, they count as data
            reader.ReadSingle(); // Right's homogeneous component; always 0
            light.Up = reader.ReadVector3();
            reader.ReadSingle(); // Up's homogeneous component; always 0
            light.At = reader.ReadVector3();
            reader.ReadSingle(); // At's homogeneous component; always 0
            light.Position = reader.ReadVector3();
            light.PositionW = reader.ReadSingle();
            light.Radius = reader.ReadSingle();
            light.Angle = reader.ReadSingle();
            reader.ReadUInt32(); // runtime-resolved platLight, always 0

            asset.Lights.Add(light);
        }

        asset.Physical.LightCount = (uint)asset.Lights.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(LightKitAsset asset, EndianWriter writer, FormatProfile profile)
    {
        writer.Write(asset.Physical.Magic);
        writer.Write(asset.GroupId);
        writer.Write(asset.Physical.LightCount);
        writer.Write(0u); // runtime-resolved

        if (profile.Game is GameVersion.ROTU or GameVersion.Ratatouille)
            writer.Write(0xCDCDCDCDu); // "blended"

        foreach (var light in asset.Lights)
        {
            writer.Write((uint)light.Type);
            writer.Write(light.Color.R);
            writer.Write(light.Color.G);
            writer.Write(light.Color.B);
            writer.Write(light.Color.A);
            writer.Write(light.Right);
            writer.Write(0f); // Right's homogeneous component
            writer.Write(light.Up);
            writer.Write(0f); // Up's homogeneous component
            writer.Write(light.At);
            writer.Write(0f); // At's homogeneous component
            writer.Write(light.Position);
            writer.Write(light.PositionW);
            writer.Write(light.Radius);
            writer.Write(light.Angle);
            writer.Write(0u); // runtime-resolved
        }

        writer.Write(asset.GetUnparsedTail());
    }
}

/// <summary>
/// An explicit interface used to interact with <see cref="LightKitAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalLightKitAsset : IPhysicalAsset
{
    /// <summary>
    /// A four-character magic number.
    /// </summary>
    uint Magic { get; set; }

    /// <summary>
    /// The number of <see cref="LightKitAsset.Lights"/> stored for this asset, read directly from
    /// its header.
    /// </summary>
    /// <remarks>
    /// When disagreements with <see cref="LightKitAsset.Lights"/>.Count exist, this field wins during
    /// serialization.
    /// </remarks>
    uint LightCount { get; set; }
}

/// <summary>
/// One light in a <see cref="LightKitAsset"/>.
/// </summary>
/// <remarks>
/// In decompiled source, <see cref="Right"/>, <see cref="Up"/>, and <see cref="At"/> are individually
/// normalized and assembled into the light's orientation, with <see cref="Right"/> and <see cref="At"/>
/// negated in the process; <see cref="Position"/> becomes its world position. For an
/// <see cref="LightKitLightType.Ambient"/> light, all four are <see cref="Vector3.Zero"/>.
/// </remarks>
/// TODO: elaborate on what this means. it seems archaic, can we abstract it?
/// TODO: abstract LightKitLight into subclasses branching from LightKitLightType
public sealed class LightKitLight
{
    /// <summary>
    /// This light's type, determining which of this light's other fields apply.
    /// </summary>
    public LightKitLightType Type { get; set; }

    /// <summary>
    /// This light's color.
    /// </summary>
    public RgbaColor Color { get; set; }

    /// <summary>
    /// The right vector of this light's orientation.
    /// </summary>
    public Vector3 Right { get; set; }

    /// <summary>
    /// The up vector of this light's orientation.
    /// </summary>
    public Vector3 Up { get; set; }

    /// <summary>
    /// The direction a <see cref="LightKitLightType.Directional"/> light points towards, as an angle
    /// throughout the level rather than a position.
    /// </summary>
    public Vector3 At { get; set; }

    /// <summary>
    /// This light's world position, for <see cref="LightKitLightType.Point"/> and
    /// <see cref="LightKitLightType.Spot"/> lights.
    /// </summary>
    public Vector3 Position { get; set; }

    /// <summary>
    /// Unknown. The fourth component alongside <see cref="Position"/>, forming a complete
    /// homogeneous 4-vector with <see cref="Right"/>/<see cref="Up"/>/<see cref="At"/> (each of whose
    /// own fourth components are always 0 and not modelled). Always 1 for every
    /// non-<see cref="LightKitLightType.Ambient"/> light type.
    /// </summary>
    public float PositionW { get; set; }

    /// <summary>
    /// The radius of a <see cref="LightKitLightType.Point"/> or <see cref="LightKitLightType.Spot"/>
    /// light.
    /// </summary>
    public float Radius { get; set; }

    /// <summary>
    /// The cone angle of a <see cref="LightKitLightType.Spot"/> light.
    /// </summary>
    public float Angle { get; set; }
}

/// <summary>
/// Represents all known values for <see cref="LightKitLight.Type"/>.
/// </summary>
public enum LightKitLightType : uint
{
    /// <summary>
    /// A uniform light with no direction or position, illuminating everything equally.
    /// </summary>
    Ambient = 1,
    /// <summary>
    /// A light shining uniformly along <see cref="LightKitLight.At"/> from everywhere, with no
    /// position of its own.
    /// </summary>
    Directional = 2,
    /// <summary>
    /// A light radiating from <see cref="LightKitLight.Position"/> in every direction, out to
    /// <see cref="LightKitLight.Radius"/>.
    /// </summary>
    Point = 3,
    /// <summary>
    /// A light radiating from <see cref="LightKitLight.Position"/> in a cone along
    /// <see cref="LightKitLight.At"/>, out to <see cref="LightKitLight.Radius"/> and
    /// <see cref="LightKitLight.Angle"/>.
    /// </summary>
    Spot = 4,
}
