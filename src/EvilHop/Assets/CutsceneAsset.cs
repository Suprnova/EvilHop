using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;
using System.Text;

namespace EvilHop.Assets;

/// <summary>
/// A cinematic sequence of streamed camera, animation, sound, and model data, played back from
/// aligned chunks.
/// </summary>
/// <remarks>
/// <para>
/// Only this asset's fixed-size header and its table of referenced models are currently modelled.
/// The chunked data is available via <see cref="Asset.GetUnparsedTail"/>, and will be implemented
/// properly once their dependent asset types are appropriately modelled. 
/// </para>
/// <seealso href="https://heavyironmodding.org/wiki/CSN">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class CutsceneAsset : Asset, IPhysicalCutsceneAsset
{
    /// <summary>
    /// The models this cutscene needs loaded before playback.
    /// </summary>
    public Collection<CutsceneDataEntry> Data { get; } = [];

    /// <summary>
    /// The left-channel sound name for this cutscene's dialog track.
    /// <see cref="GameVersion.N100F"/>/<see cref="GameVersion.BFBB"/> only.
    /// </summary>
    public string SoundLeft { get; set; } = string.Empty;

    /// <summary>
    /// The right-channel sound name for this cutscene's dialog track.
    /// <see cref="GameVersion.N100F"/>/<see cref="GameVersion.BFBB"/> only.
    /// </summary>
    /// Validation TODO: Always empty?
    public string SoundRight { get; set; } = string.Empty;

    /// <summary>
    /// Up to 32 stereo sound track slots this cutscene can play from.
    /// <see cref="GameVersion.TSSM"/>/<see cref="GameVersion.Incredibles"/> only.
    /// </summary>
    /// Validation TODO: No more than 32 entries.
    public Collection<CutsceneAudioTrack> AudioTracks { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalCutsceneAsset Physical => this;

    private AssetId? _overriddenAssetId;
    AssetId IPhysicalCutsceneAsset.AssetId
    {
        get => _overriddenAssetId ?? Id;
        set => _overriddenAssetId = value == Id ? null : value;
    }

    private uint? _overriddenNumData;
    uint IPhysicalCutsceneAsset.NumData
    {
        get => _overriddenNumData ?? (uint)Data.Count;
        set => _overriddenNumData = value == (uint)Data.Count ? null : value;
    }

    private uint _numTime;
    uint IPhysicalCutsceneAsset.NumTime { get => _numTime; set => _numTime = value; }

    private uint _maxModel;
    uint IPhysicalCutsceneAsset.MaxModel { get => _maxModel; set => _maxModel = value; }

    private uint _maxBufEven;
    uint IPhysicalCutsceneAsset.MaxBufEven { get => _maxBufEven; set => _maxBufEven = value; }

    private uint _maxBufOdd;
    uint IPhysicalCutsceneAsset.MaxBufOdd { get => _maxBufOdd; set => _maxBufOdd = value; }

    private uint _headerSize;
    uint IPhysicalCutsceneAsset.HeaderSize { get => _headerSize; set => _headerSize = value; }

    private uint _visCount;
    uint IPhysicalCutsceneAsset.VisCount { get => _visCount; set => _visCount = value; }

    private uint _visSize;
    uint IPhysicalCutsceneAsset.VisSize { get => _visSize; set => _visSize = value; }

    private uint _breakCount;
    uint IPhysicalCutsceneAsset.BreakCount { get => _breakCount; set => _breakCount = value; }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.Cutscene"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.N100F,
        GameVersion.BFBB,
        GameVersion.TSSM,
        GameVersion.Incredibles,
    };

    internal CutsceneAsset() { }

    internal static CutsceneAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new CutsceneAsset();
        AssetFields.Populate(asset, header, debug);

        reader.ReadUInt32(); // Magic
        asset.Physical.AssetId = reader.ReadAssetId();
        asset.Physical.NumData = reader.ReadUInt32();
        asset.Physical.NumTime = reader.ReadUInt32();
        asset.Physical.MaxModel = reader.ReadUInt32();
        asset.Physical.MaxBufEven = reader.ReadUInt32();
        asset.Physical.MaxBufOdd = reader.ReadUInt32();
        asset.Physical.HeaderSize = reader.ReadUInt32();
        asset.Physical.VisCount = reader.ReadUInt32();
        asset.Physical.VisSize = reader.ReadUInt32();
        asset.Physical.BreakCount = reader.ReadUInt32();
        reader.ReadUInt32(); // padding, always zero

        if (profile.Game is GameVersion.TSSM or GameVersion.Incredibles)
            ReadAudioTracks(asset, reader, AudioTrackSoundLength(asset.Physical));
        else
        {
            asset.SoundLeft = ReadFixedString(reader, 16);
            asset.SoundRight = ReadFixedString(reader, 16);
        }

        for (int i = 0; i < asset.Physical.NumData; i++)
        {
            asset.Data.Add(new CutsceneDataEntry
            {
                DataType = (CutsceneDataType)reader.ReadUInt32(),
                AssetId = reader.ReadAssetId(),
                ChunkSize = reader.ReadUInt32(),
                FileOffset = reader.ReadUInt32(),
            });
        }
        asset.Physical.NumData = (uint)asset.Data.Count;

        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(CutsceneAsset asset, EndianWriter writer, FormatProfile profile)
    {
        writer.Write(0x4E535443u); // "NSTC" (CTSN) magic
        writer.Write(asset.Physical.AssetId);
        writer.Write(asset.Physical.NumData);
        writer.Write(asset.Physical.NumTime);
        writer.Write(asset.Physical.MaxModel);
        writer.Write(asset.Physical.MaxBufEven);
        writer.Write(asset.Physical.MaxBufOdd);
        writer.Write(asset.Physical.HeaderSize);
        writer.Write(asset.Physical.VisCount);
        writer.Write(asset.Physical.VisSize);
        writer.Write(asset.Physical.BreakCount);
        writer.Write(0u); // padding

        if (profile.Game is GameVersion.TSSM or GameVersion.Incredibles)
            WriteAudioTracks(asset, writer, AudioTrackSoundLength(asset.Physical));
        else
        {
            WriteFixedString(writer, asset.SoundLeft, 16);
            WriteFixedString(writer, asset.SoundRight, 16);
        }

        foreach (var entry in asset.Data)
        {
            writer.Write((uint)entry.DataType);
            writer.Write(entry.AssetId);
            writer.Write(entry.ChunkSize);
            writer.Write(entry.FileOffset);
        }

        writer.Write(asset.GetUnparsedTail());
    }

    /// <summary>
    /// Derives the per-track sound-name field length from <paramref name="physical"/>'s header
    /// counts.
    /// </summary>
    private static int AudioTrackSoundLength(IPhysicalCutsceneAsset physical)
    {
        uint fixedTotal = physical.HeaderSize
            - physical.NumData * 16u
            - (physical.NumTime + 1) * 4u
            - physical.VisSize * 4u
            - physical.BreakCount * 8u;
        uint trackSize = (fixedTotal - 0x30u) / 32u;
        return (int)(trackSize - 8) / 2;
    }

    private static void ReadAudioTracks(CutsceneAsset asset, EndianReader reader, int soundLength)
    {
        for (int i = 0; i < 32; i++)
        {
            var leftId = reader.ReadAssetId();
            var rightId = reader.ReadAssetId();
            var left = ReadFixedString(reader, soundLength);
            var right = ReadFixedString(reader, soundLength);
            asset.AudioTracks.Add(new CutsceneAudioTrack(leftId, rightId, left, right));
        }
    }

    private static void WriteAudioTracks(CutsceneAsset asset, EndianWriter writer, int soundLength)
    {
        for (int i = 0; i < 32; i++)
        {
            var track = i < asset.AudioTracks.Count
                ? asset.AudioTracks[i]
                : new CutsceneAudioTrack(default, default, string.Empty, string.Empty);
            writer.Write(track.LeftSoundId);
            writer.Write(track.RightSoundId);
            WriteFixedString(writer, track.LeftSound, soundLength);
            WriteFixedString(writer, track.RightSound, soundLength);
        }
    }

    /// <summary>
    /// Reads a fixed-<paramref name="length"/> ASCII field, null-terminated and null-padded when the
    /// value is shorter than <paramref name="length"/>.
    /// </summary>
    /// <exception cref="InvalidDataException">The field has non-zero bytes after its null terminator.</exception>
    private static string ReadFixedString(EndianReader reader, int length)
    {
        byte[] bytes = reader.ReadBytes(length);
        int nullIndex = Array.IndexOf(bytes, (byte)0);
        if (nullIndex < 0)
            return Encoding.ASCII.GetString(bytes); // special handling for truncated strings
        for (int i = nullIndex; i < bytes.Length; i++)
            if (bytes[i] != 0)
                throw new InvalidDataException($"Expected null padding after byte {nullIndex} of {length}.");

        return Encoding.ASCII.GetString(bytes, 0, nullIndex);
    }

    /// <exception cref="ArgumentException"><paramref name="value"/> is longer than <paramref name="length"/> bytes.</exception>
    private static void WriteFixedString(EndianWriter writer, string value, int length)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(value);
        if (bytes.Length > length)
            throw new ArgumentException($"Must be at most {length} bytes.", nameof(value));

        writer.Write(bytes);
        writer.Write(new byte[length - bytes.Length]);
    }
}

/// <summary>
/// An explicit interface used to interact with <see cref="CutsceneAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalCutsceneAsset : IPhysicalAsset
{
    /// <summary>
    /// The <see cref="Asset"/>'s ID, as stored in the <see cref="CutsceneAsset"/>'s own header. This
    /// field is stored independently from <see cref="Asset.Id"/>, mirroring
    /// <see cref="IPhysicalBaseAsset.BaseId"/>.
    /// </summary>
    AssetId AssetId { get; set; }
    /// <summary>
    /// The number of <see cref="CutsceneAsset.Data"/> entries, read directly from the header.
    /// </summary>
    uint NumData { get; set; }
    /// <summary>
    /// The number of TimeChunk offsets following the (currently unparsed) region after
    /// <see cref="CutsceneAsset.Data"/>.
    /// </summary>
    uint NumTime { get; set; }
    /// <summary>The largest model, in bytes including padding, in the unparsed chunk data.</summary>
    uint MaxModel { get; set; }
    /// <summary>The largest TimeChunk with an even <c>ChunkIndex</c>, in bytes including padding.</summary>
    uint MaxBufEven { get; set; }
    /// <summary>The largest TimeChunk with an odd <c>ChunkIndex</c>, in bytes including padding.</summary>
    uint MaxBufOdd { get; set; }
    /// <summary>
    /// The size, in bytes, of this asset's header region - this fixed header, <see cref="CutsceneAsset.Data"/>,
    /// and the unparsed TimeChunk-offset/visibility/break tables that follow it, before the
    /// chunked model/animation/camera/sound data begins.
    /// </summary>
    uint HeaderSize { get; set; }
    /// <summary>The number of visibility entries in the unparsed region.</summary>
    uint VisCount { get; set; }
    /// <summary>The total size, in 4-byte steps, of the visibility entries in the unparsed region.</summary>
    uint VisSize { get; set; }
    /// <summary>The number of break entries in the unparsed region.</summary>
    uint BreakCount { get; set; }
}

/// <summary>
/// One entry in a <see cref="CutsceneAsset.Data"/> table, referencing a model this cutscene needs
/// loaded before playback.
/// </summary>
public record struct CutsceneDataEntry
{
    /// <summary>Which kind of model this entry is.</summary>
    public CutsceneDataType DataType { get; set; }

    /// <summary>The <see cref="Primitives.AssetId"/> of the referenced model.</summary>
    public AssetId AssetId { get; set; }

    /// <summary>The size, in bytes, of this model's data within the cutscene's unparsed chunk data.</summary>
    public uint ChunkSize { get; set; }

    /// <summary>
    /// The offset, in bytes, to this model's data within the cutscene's unparsed chunk data. Zero
    /// alongside a zero <see cref="ChunkSize"/> when the model is instead an external
    /// <see cref="AssetType.Model"/> asset in the level's HOP.
    /// </summary>
    public uint FileOffset { get; set; }
}

/// <summary>
/// Represents all known values for <see cref="CutsceneDataEntry.DataType"/>.
/// </summary>
public enum CutsceneDataType : uint
{
    /// <summary>An embedded RenderWare clump model.</summary>
    RWModel = 1,
    /// <summary>
    /// A JDTM model. Not present in <see cref="GameVersion.N100F"/>.
    /// </summary>
    JdtmModel = 6,
}

/// <summary>
/// One stereo sound track slot in a <see cref="GameVersion.TSSM"/>/<see cref="GameVersion.Incredibles"/>
/// <see cref="CutsceneAsset"/>.
/// </summary>
public readonly record struct CutsceneAudioTrack(AssetId LeftSoundId, AssetId RightSoundId, string LeftSound, string RightSound);
