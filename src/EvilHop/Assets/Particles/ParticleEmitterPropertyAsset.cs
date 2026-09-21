using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>
/// Describes how a particle changes over its lifetime - its emission rate, color, size, and
/// velocity - shared by every <see cref="AssetType.ParticleEmitter"/> that references it.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/PARP">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed partial class ParticleEmitterPropertyAsset() : BaseAsset(AssetType.ParticleEmitterProperty, baseType: 0x2E)
{
    /// <summary>The <see cref="AssetType.ParticleSystem"/> this emitter's particles use.</summary>
    public AssetId ParSysId { get; set; }

    /// <summary>How many particles are emitted per second.</summary>
    public ParticleInterpolation Rate { get; set; } = new();

    /// <summary>How long, in seconds, a particle lives before expiring.</summary>
    public ParticleInterpolation Life { get; set; } = new();

    /// <summary>A particle's size, in units, when it spawns.</summary>
    public ParticleInterpolation SizeBirth { get; set; } = new();

    /// <summary>A particle's size, in units, just before it expires.</summary>
    public ParticleInterpolation SizeDeath { get; set; } = new();

    /// <summary>A particle's color when it spawns.</summary>
    public ParticleColorInterpolation ColorBirth { get; set; } = new();

    /// <summary>A particle's color just before it expires.</summary>
    public ParticleColorInterpolation ColorDeath { get; set; } = new();

    /// <summary>Not used by the particle system.</summary>
    public ParticleInterpolation VelocityScale { get; set; } = new();

    /// <summary>Not used by the particle system.</summary>
    public ParticleInterpolation VelocityAngle { get; set; } = new();

    /// <summary>Not used by the particle system. Always zero.</summary>
    public Vector3 Velocity { get; set; }

    /// <summary>
    /// The maximum number of particles this emitter may have alive at once, or -1 for unlimited.
    /// </summary>
    public int EmitLimit { get; set; } = -1;

    /// <summary>
    /// The time, in seconds, before the count toward <see cref="EmitLimit"/> resets. Usually 0.
    /// </summary>
    public float EmitLimitResetTime { get; set; }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.ParticleEmitterProperty"/> is known to be
    /// read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.BFBB,
        GameVersion.TSSM,
        GameVersion.ROTU,
        GameVersion.Ratatouille,
    };
    internal static ParticleEmitterPropertyAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new ParticleEmitterPropertyAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        asset.ParSysId = reader.ReadAssetId();
        asset.Rate = ParticleInterpolation.Read(reader, profile);
        asset.Life = ParticleInterpolation.Read(reader, profile);
        asset.SizeBirth = ParticleInterpolation.Read(reader, profile);
        asset.SizeDeath = ParticleInterpolation.Read(reader, profile);
        asset.ColorBirth = ParticleColorInterpolation.Read(reader, profile);
        asset.ColorDeath = ParticleColorInterpolation.Read(reader, profile);
        asset.VelocityScale = ParticleInterpolation.Read(reader, profile);
        asset.VelocityAngle = ParticleInterpolation.Read(reader, profile);
        asset.Velocity = reader.ReadVector3();
        asset.EmitLimit = reader.ReadInt32();
        asset.EmitLimitResetTime = reader.ReadSingle();

        for (var i = 0; i < asset.Physical.LinkCount; i++)
            asset.Links.Add(Link.Read(reader, profile));
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(ParticleEmitterPropertyAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);

        writer.Write(asset.ParSysId);
        ParticleInterpolation.Write(asset.Rate, writer, profile);
        ParticleInterpolation.Write(asset.Life, writer, profile);
        ParticleInterpolation.Write(asset.SizeBirth, writer, profile);
        ParticleInterpolation.Write(asset.SizeDeath, writer, profile);
        ParticleColorInterpolation.Write(asset.ColorBirth, writer, profile);
        ParticleColorInterpolation.Write(asset.ColorDeath, writer, profile);
        ParticleInterpolation.Write(asset.VelocityScale, writer, profile);
        ParticleInterpolation.Write(asset.VelocityAngle, writer, profile);
        writer.Write(asset.Velocity);
        writer.Write(asset.EmitLimit);
        writer.Write(asset.EmitLimitResetTime);

        foreach (var link in asset.Links)
            Link.Write(link, writer, profile);
        writer.Write(asset.GetUnparsedTail());
    }
}
