using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.Immutable;

namespace EvilHop.Assets;

/// <summary>
/// Owns and drives playback of a single <see cref="CutsceneAsset"/> instance placed in the level,
/// cueing particle emitters on and off as it plays and forwarding player control and camera state
/// around the transition.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/CSNM">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class CutsceneManagerAsset() : BaseAsset(AssetType.CutsceneManager), IPhysicalCutsceneManagerAsset
{
    private const int EmitterCueSlotCount = 15;

    /// <summary>
    /// The <see cref="CutsceneAsset"/> this manager plays.
    /// </summary>
    public AssetId CutsceneId { get; set; }

    /// <summary>
    /// The interpolation speed used during this cutscene's playback.
    /// </summary>
    /// TODO: validate against decompiled source - declared but never referenced in the game code
    /// available so far.
    public float InterpSpeed { get; set; }

    /// <summary>
    /// The subtitles shown during this cutscene's playback. Only present in
    /// <see cref="GameVersion.TSSM"/> and <see cref="GameVersion.Incredibles"/>.
    /// </summary>
    public AssetId SubtitlesId { get; set; }

    /// <summary>
    /// Always exactly 15 <see cref="CutsceneEmitterCue"/> slots.
    /// </summary>
    /// <exception cref="ArgumentException">The assigned value's length isn't 15.</exception>
    public ImmutableArray<CutsceneEmitterCue> EmitterCues
    {
        get;
        set => field = value.Length == EmitterCueSlotCount
            ? value
            : throw new ArgumentException($"{nameof(EmitterCues)} must contain exactly {EmitterCueSlotCount} elements.", nameof(value));
    } = [.. new CutsceneEmitterCue[EmitterCueSlotCount]];

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalCutsceneManagerAsset Physical => this;

    private uint _managerFlags;
    uint IPhysicalCutsceneManagerAsset.ManagerFlags { get => _managerFlags; set => _managerFlags = value; }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.CutsceneManager"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.N100F,
        GameVersion.BFBB,
        GameVersion.TSSM,
        GameVersion.Incredibles,
    };

    internal static CutsceneManagerAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new CutsceneManagerAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        asset.CutsceneId = reader.ReadAssetId();
        asset.Physical.ManagerFlags = reader.ReadUInt32();
        asset.InterpSpeed = reader.ReadSingle();

        if (profile.Game is GameVersion.TSSM or GameVersion.Incredibles)
            asset.SubtitlesId = reader.ReadAssetId();

        var startTimes = new float[EmitterCueSlotCount];
        for (int i = 0; i < EmitterCueSlotCount; i++)
            startTimes[i] = reader.ReadSingle();

        var endTimes = new float[EmitterCueSlotCount];
        for (int i = 0; i < EmitterCueSlotCount; i++)
            endTimes[i] = reader.ReadSingle();

        var cues = new CutsceneEmitterCue[EmitterCueSlotCount];
        for (int i = 0; i < EmitterCueSlotCount; i++)
        {
            cues[i] = new CutsceneEmitterCue
            {
                EmitterId = reader.ReadAssetId(),
                StartTime = startTimes[i],
                EndTime = endTimes[i],
            };
        }
        asset.EmitterCues = [.. cues];

        LinkSerialization.Read(asset, reader, asset.Physical.LinkCount);
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(CutsceneManagerAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);

        writer.Write(asset.CutsceneId);
        writer.Write(asset.Physical.ManagerFlags);
        writer.Write(asset.InterpSpeed);

        if (profile.Game is GameVersion.TSSM or GameVersion.Incredibles)
            writer.Write(asset.SubtitlesId);

        foreach (var cue in asset.EmitterCues)
            writer.Write(cue.StartTime);
        foreach (var cue in asset.EmitterCues)
            writer.Write(cue.EndTime);
        foreach (var cue in asset.EmitterCues)
            writer.Write(cue.EmitterId);

        LinkSerialization.Write(asset, writer);
        writer.Write(asset.GetUnparsedTail());
    }
}

/// <summary>
/// An explicit interface used to interact with <see cref="CutsceneManagerAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalCutsceneManagerAsset : IPhysicalBaseAsset
{
    /// <summary>
    /// Unknown.
    /// </summary>
    uint ManagerFlags { get; set; }
}

/// <summary>
/// One particle emitter cue in a <see cref="CutsceneManagerAsset"/>, enabling <see cref="EmitterId"/>
/// between <see cref="StartTime"/> and <see cref="EndTime"/> while the cutscene plays.
/// </summary>
public record struct CutsceneEmitterCue
{
    /// <summary>
    /// The particle emitter this cue targets, or <see cref="AssetId.None"/> if this slot is unused.
    /// </summary>
    /// TODO: confirm this targets an <see cref="AssetType.ParticleEmitter"/> instance - inferred from
    /// decompiled source's zParEmitterFind lookup, which isn't yet a modelled asset type.
    public AssetId EmitterId { get; set; }

    /// <summary>
    /// The cutscene time, in seconds, at which <see cref="EmitterId"/> is enabled.
    /// </summary>
    public float StartTime { get; set; }

    /// <summary>
    /// The cutscene time, in seconds, at which <see cref="EmitterId"/> is disabled.
    /// </summary>
    public float EndTime { get; set; }
}
