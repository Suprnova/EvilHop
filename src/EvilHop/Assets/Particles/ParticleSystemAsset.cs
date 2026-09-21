using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace EvilHop.Assets;

/// <summary>
/// A source of particles - sprites, streaks, or other small rendered shapes - spawned from a
/// <see cref="AssetType.Texture"/> and shaped over their lifetime by a packed list of particle
/// commands.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/PARS">Heavy Iron Modding documentation</seealso>
/// </remarks>
// TODO: Partial implementation - particle commands are undecoded and stored as raw bytes
public sealed partial class ParticleSystemAsset() : BaseAsset(AssetType.ParticleSystem, baseType: 0x27), IPhysicalParticleSystemAsset
{
    /// <summary>
    /// The <see cref="AssetType.ParticleSystem"/> this one inherits unset fields from, usually
    /// <see cref="AssetId.None"/>.
    /// </summary>
    public AssetId ParentId { get; set; }

    /// <summary>The <see cref="AssetType.Texture"/> particles are rendered with.</summary>
    public AssetId TextureId { get; set; }

    /// <summary>This system's flags.</summary>
    public ParticleSystemFlags Flags { get; set; }

    /// <summary>This system's rendering and update priority relative to other particle systems.</summary>
    public byte Priority { get; set; }

    /// <summary>The maximum number of particles alive at once.</summary>
    public ushort MaxParticles { get; set; }

    /// <summary>How this system's particles are rendered.</summary>
    public ParticleSystemRenderFunction RenderFunction { get; set; }

    /// <summary>The source <see cref="RwBlendFunction"/> used when blending particles.</summary>
    public RwBlendFunction SourceBlend { get; set; }

    /// <summary>The destination <see cref="RwBlendFunction"/> used when blending particles.</summary>
    public RwBlendFunction DestinationBlend { get; set; }

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalParticleSystemAsset Physical => this;

    private int _systemType;
    int IPhysicalParticleSystemAsset.SystemType { get => _systemType; set => _systemType = value; }

    private byte _commandCount;
    byte IPhysicalParticleSystemAsset.CommandCount { get => _commandCount; set => _commandCount = value; }

    private byte[] _commandData = [];
    byte[] IPhysicalParticleSystemAsset.CommandData { get => _commandData; set => _commandData = value; }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.ParticleSystem"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.N100F,
        GameVersion.BFBB,
        GameVersion.TSSM,
        GameVersion.ROTU,
        GameVersion.Ratatouille,
    };

    internal static ParticleSystemAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new ParticleSystemAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        asset.Physical.SystemType = reader.ReadInt32();
        asset.ParentId = reader.ReadAssetId();
        asset.TextureId = reader.ReadAssetId();
        asset.Flags = (ParticleSystemFlags)reader.ReadByte();
        asset.Priority = reader.ReadByte();
        asset.MaxParticles = reader.ReadUInt16();
        asset.RenderFunction = (ParticleSystemRenderFunction)reader.ReadByte();
        asset.SourceBlend = (RwBlendFunction)(byte)(reader.ReadByte() + 1);
        asset.DestinationBlend = (RwBlendFunction)(byte)(reader.ReadByte() + 1);
        asset.Physical.CommandCount = reader.ReadByte();

        int commandDataSize = reader.ReadInt32();
        asset.Physical.CommandData = reader.ReadBytes(commandDataSize);

        for (var i = 0; i < asset.Physical.LinkCount; i++)
            asset.Links.Add(Link.Read(reader, profile));
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(ParticleSystemAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);

        writer.Write(asset.Physical.SystemType);
        writer.Write(asset.ParentId);
        writer.Write(asset.TextureId);
        writer.Write((byte)asset.Flags);
        writer.Write(asset.Priority);
        writer.Write(asset.MaxParticles);
        writer.Write((byte)asset.RenderFunction);
        writer.Write((byte)(asset.SourceBlend - 1));
        writer.Write((byte)(asset.DestinationBlend - 1));
        writer.Write(asset.Physical.CommandCount);

        writer.Write(asset.Physical.CommandData.Length);
        writer.Write(asset.Physical.CommandData);

        foreach (var link in asset.Links)
            Link.Write(link, writer, profile);
        writer.Write(asset.GetUnparsedTail());
    }
}

/// <summary>
/// An explicit interface used to interact with <see cref="ParticleSystemAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalParticleSystemAsset : IPhysicalBaseAsset
{
    /// <summary>
    /// Unknown.
    /// </summary>
    int SystemType { get; set; }

    /// <summary>
    /// The number of particle commands packed in <see cref="CommandData"/>, read directly from its
    /// leading count field.
    /// </summary>
    /// <remarks>
    /// <see cref="CommandData"/> is stored raw rather than decoded into individual commands, so
    /// unlike most counts elsewhere in this library, this one cannot be derived and does not
    /// override-clear against anything.
    /// </remarks>
    byte CommandCount { get; set; }

    /// <summary>
    /// This system's particle commands - move, accelerate, fade, and similar per-particle behaviors -
    /// packed back-to-back as raw, undecoded bytes. Each command starts with a shared 4-byte type,
    /// 1-byte enabled flag, 1-byte mode, and 2 bytes of padding, followed by a type-specific payload
    /// whose size is not stored on disk and is not yet modelled here.
    /// </summary>
    [SuppressMessage("Design", "CA1819:Properties should not return arrays", Justification = "Packed, variable-length, and not yet decoded into individual particle commands; a byte[] is the natural representation.")]
    byte[] CommandData { get; set; }
}

/// <summary>
/// Flags controlling particle system simulation, rendering mode, and lifecycle.
/// </summary>
[Flags]
public enum ParticleSystemFlags : byte
{
    /// <summary>No flags are set.</summary>
    None = 0,

    /// <summary>This system is visible and its particles are rendered.</summary>
    Visible = 1 << 0,

    /// <summary>When set, particles do not age - their remaining lifetime never counts down.</summary>
    DisableAging = 1 << 1,

    /// <summary>When set, expired particles are not recycled back to life.</summary>
    DisableBack2Life = 1 << 2,

    /// <summary>
    /// Offsets sprite particles toward the camera's right. Takes priority over
    /// <see cref="PivotRightNegative"/> when both are set.
    /// </summary>
    PivotRight = 1 << 3,

    /// <summary>
    /// Offsets sprite particles away from the camera's up. Takes priority over
    /// <see cref="PivotUp"/> when both are set.
    /// </summary>
    PivotUpNegative = 1 << 4,

    /// <summary>
    /// Offsets sprite particles away from the camera's right. Ignored if <see cref="PivotRight"/> is
    /// also set.
    /// </summary>
    PivotRightNegative = 1 << 5,

    /// <summary>
    /// Offsets sprite particles toward the camera's up. Ignored if <see cref="PivotUpNegative"/> is
    /// also set.
    /// </summary>
    PivotUp = 1 << 6,

    /// <summary>
    /// Renders this system's particles through the particle tank rendering path instead of the
    /// default sprite loop.
    /// </summary>
    UsePTankRender = 1 << 7,
}

/// <summary>
/// Identifies the rendering pipeline function used to draw the particle system.
/// </summary>
public enum ParticleSystemRenderFunction : byte
{
    /// <summary>Renders particles as camera-facing sprites.</summary>
    Sprite = 0,

    /// <summary>Renders particles as streaks.</summary>
    Streak = 1,

    /// <summary>Renders particles as flat, unbillboarded quads.</summary>
    Flat = 2,

    /// <summary>Renders particles statically.</summary>
    Static = 3,

    /// <summary>Renders particles projected onto the ground.</summary>
    Ground = 4,

    /// <summary>Renders particles as quad streaks.</summary>
    QuadStreak = 5,

    /// <summary>Renders particles as inverted streaks.</summary>
    InvStreak = 6,
}
