using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>
/// A wind zone: while turned on, pushes any entity inside <see cref="VolumeId"/> along
/// <see cref="Velocity"/>, and periodically emits dust or debris particles within it.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/GUST">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class GustAsset() : BaseAsset(AssetType.Gust, baseType: 0x1C)
{
    /// <summary>Whether the gust is turned on, and which kind of particles it emits.</summary>
    public GustFlags Flags { get; set; }

    /// <summary>The <see cref="AssetType.Volume"/> an entity must be inside for the gust to push it.</summary>
    public AssetId VolumeId { get; set; }

    /// <summary>
    /// A second <see cref="AssetType.Volume"/> particles are placed within instead of
    /// <see cref="VolumeId"/>, if set.
    /// </summary>
    public AssetId EffectVolumeId { get; set; }

    /// <summary>The direction and speed an affected entity is pushed.</summary>
    public Vector3 Velocity { get; set; }

    /// <summary>
    /// How many seconds it takes an entity's push to blend fully in or out as it enters or leaves
    /// <see cref="VolumeId"/>.
    /// </summary>
    public float Fade { get; set; }

    /// <summary>
    /// Scales the lifetime of emitted particles. A value of zero or less disables particle emission
    /// entirely.
    /// </summary>
    public float ParticleModifier { get; set; }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.Gust"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.N100F,
    };

    internal static GustAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new GustAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        asset.Flags = (GustFlags)reader.ReadUInt32();
        asset.VolumeId = reader.ReadAssetId();
        asset.EffectVolumeId = reader.ReadAssetId();
        asset.Velocity = reader.ReadVector3();
        asset.Fade = reader.ReadSingle();
        asset.ParticleModifier = reader.ReadSingle();

        for (var i = 0; i < asset.Physical.LinkCount; i++)
            asset.Links.Add(Link.Read(reader, profile));
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(GustAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);

        writer.Write((uint)asset.Flags);
        writer.Write(asset.VolumeId);
        writer.Write(asset.EffectVolumeId);
        writer.Write(asset.Velocity);
        writer.Write(asset.Fade);
        writer.Write(asset.ParticleModifier);

        foreach (var link in asset.Links)
            Link.Write(link, writer, profile);
        writer.Write(asset.GetUnparsedTail());
    }

    /// <summary>Toggles a <see cref="GustAsset"/> on or off, and which particles it emits while on.</summary>
    [Flags]
    public enum GustFlags : uint
    {
        /// <summary>The gust is turned off.</summary>
        None = 0,

        /// <summary>The gust is turned on and actively pushing entities.</summary>
        On = 1 << 0,

        /// <summary>Emits dust particles instead of debris particles.</summary>
        Dust = 1 << 1,
    }
}
