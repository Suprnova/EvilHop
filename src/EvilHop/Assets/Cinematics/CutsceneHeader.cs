using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace EvilHop.Assets;

/// <summary>
/// The xCutsceneInfo header - the portion of the <see cref="AssetType.Cutscene"/> format duplicated
/// verbatim, sans the alignment padding that precedes it in a standalone file, by every
/// <see cref="CutsceneTableAsset"/> entry. Implemented by <see cref="CutsceneAsset"/> and
/// <see cref="CutsceneTableEntry"/> so <see cref="ReadHeader"/>/<see cref="WriteHeader"/> can serve
/// both without either type pretending to be the other.
/// </summary>
internal interface ICutsceneHeader
{
    /// <inheritdoc cref="CutsceneAsset.Data"/>
    Collection<CutsceneDataEntry> Data { get; }

    /// <inheritdoc cref="CutsceneAsset.SoundLeft"/>
    string SoundLeft { get; set; }

    /// <inheritdoc cref="CutsceneAsset.SoundRight"/>
    string SoundRight { get; set; }

    /// <inheritdoc cref="CutsceneAsset.AudioTracks"/>
    Collection<CutsceneAudioTrack> AudioTracks { get; }

    /// <inheritdoc cref="Asset.Physical"/>
    IPhysicalCutsceneHeader Physical { get; }

    /// <inheritdoc cref="Asset.GetUnparsedTail"/>
    Span<byte> GetUnparsedTail();

    /// <inheritdoc cref="Asset.SetUnparsedTail"/>
    void SetUnparsedTail(byte[] bytes);

    /// <summary>
    /// Reads the xCutsceneInfo header and referenced-model table into <paramref name="header"/> - the
    /// portion of this format that a <see cref="CutsceneTableEntry"/> duplicates verbatim, without the
    /// chunked media data that follows it in a standalone <see cref="AssetType.Cutscene"/> file.
    /// </summary>
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "HeaderReader's result never owns a resource worth disposing - see its remarks.")]
    internal static void ReadHeader(ICutsceneHeader header, EndianReader reader, FormatProfile profile)
    {
        reader = HeaderReader(reader, profile);

        reader.ReadUInt32(); // Magic
        header.Physical.AssetId = reader.ReadAssetId();
        header.Physical.NumData = reader.ReadUInt32();
        header.Physical.NumTime = reader.ReadUInt32();
        header.Physical.MaxModel = reader.ReadUInt32();
        header.Physical.MaxBufEven = reader.ReadUInt32();
        header.Physical.MaxBufOdd = reader.ReadUInt32();
        header.Physical.HeaderSize = reader.ReadUInt32();
        header.Physical.VisCount = reader.ReadUInt32();
        header.Physical.VisSize = reader.ReadUInt32();
        header.Physical.BreakCount = reader.ReadUInt32();
        reader.ReadUInt32(); // padding, always zero

        if (profile.Game is GameVersion.TSSM or GameVersion.Incredibles)
            ReadAudioTracks(header, reader, AudioTrackSoundLength(header.Physical), profile);
        else
        {
            header.SoundLeft = ReadFixedString(reader, 16);
            header.SoundRight = ReadFixedString(reader, 16);
        }

        for (int i = 0; i < header.Physical.NumData; i++)
            header.Data.Add(CutsceneDataEntry.Read(reader, profile));
        header.Physical.NumData = (uint)header.Data.Count;
    }

    /// <summary>
    /// Writes the xCutsceneInfo header and referenced-model table. See <see cref="ReadHeader"/>.
    /// </summary>
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "HeaderWriter's result never owns a resource worth disposing - see HeaderReader's remarks.")]
    internal static void WriteHeader(ICutsceneHeader header, EndianWriter writer, FormatProfile profile)
    {
        writer = HeaderWriter(writer, profile);

        writer.Write(0x4E535443u); // "NSTC" (CTSN) magic
        writer.Write(header.Physical.AssetId);
        writer.Write(header.Physical.NumData);
        writer.Write(header.Physical.NumTime);
        writer.Write(header.Physical.MaxModel);
        writer.Write(header.Physical.MaxBufEven);
        writer.Write(header.Physical.MaxBufOdd);
        writer.Write(header.Physical.HeaderSize);
        writer.Write(header.Physical.VisCount);
        writer.Write(header.Physical.VisSize);
        writer.Write(header.Physical.BreakCount);
        writer.Write(0u); // padding

        if (profile.Game is GameVersion.TSSM or GameVersion.Incredibles)
            WriteAudioTracks(header, writer, AudioTrackSoundLength(header.Physical), profile);
        else
        {
            WriteFixedString(writer, header.SoundLeft, 16);
            WriteFixedString(writer, header.SoundRight, 16);
        }

        foreach (var entry in header.Data)
            CutsceneDataEntry.Write(entry, writer, profile);
    }

    /// <summary>
    /// Wraps <paramref name="reader"/> to force <see cref="Endianness.Little"/> under
    /// <see cref="GameVersion.N100F"/>, whose <see cref="AssetType.Cutscene"/> and
    /// <see cref="AssetType.CutsceneTable"/> payloads are written little-endian across all platforms.
    /// Shared by <see cref="ReadHeader"/>/<see cref="WriteHeader"/> and <see cref="CutsceneTableAsset"/>'s
    /// own leading count field.
    /// </summary>
    /// <remarks>
    /// Never disposed: it shares <paramref name="reader"/>'s underlying stream with
    /// <c>leaveOpen: true</c> and owns nothing else, so there is nothing for a missing
    /// <see cref="IDisposable.Dispose"/> call to leak.
    /// </remarks>
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Thin, non-owning wrapper over the caller's own stream - see remarks.")]
    internal static EndianReader HeaderReader(EndianReader reader, FormatProfile profile) =>
        profile.Game == GameVersion.N100F && reader.Endianness != Endianness.Little
            ? new EndianReader(reader.BaseStream, Endianness.Little, leaveOpen: true)
            : reader;

    /// <summary>See <see cref="HeaderReader"/>.</summary>
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Thin, non-owning wrapper over the caller's own stream - see HeaderReader.")]
    internal static EndianWriter HeaderWriter(EndianWriter writer, FormatProfile profile) =>
        profile.Game == GameVersion.N100F && writer.Endianness != Endianness.Little
            ? new EndianWriter(writer.BaseStream, Endianness.Little, leaveOpen: true)
            : writer;

    /// <summary>
    /// Derives the per-track sound-name field length from <paramref name="physical"/>'s header
    /// counts.
    /// </summary>
    private static int AudioTrackSoundLength(IPhysicalCutsceneHeader physical)
    {
        uint fixedTotal = physical.HeaderSize
            - physical.NumData * 16u
            - (physical.NumTime + 1) * 4u
            - physical.VisSize * 4u
            - physical.BreakCount * 8u;
        uint trackSize = (fixedTotal - 0x30u) / 32u;
        return (int)(trackSize - 8) / 2;
    }

    private static void ReadAudioTracks(ICutsceneHeader header, EndianReader reader, int soundLength, FormatProfile profile)
    {
        for (int i = 0; i < 32; i++)
            header.AudioTracks.Add(CutsceneAudioTrack.Read(reader, soundLength, profile));
    }

    private static void WriteAudioTracks(ICutsceneHeader header, EndianWriter writer, int soundLength, FormatProfile profile)
    {
        for (int i = 0; i < 32; i++)
        {
            var track = i < header.AudioTracks.Count
                ? header.AudioTracks[i]
                : new CutsceneAudioTrack(default, default, string.Empty, string.Empty);
            CutsceneAudioTrack.Write(track, writer, soundLength, profile);
        }
    }

    /// <summary>
    /// Reads a fixed-<paramref name="length"/> ASCII field, null-terminated and null-padded when the
    /// value is shorter than <paramref name="length"/>.
    /// </summary>
    /// <exception cref="InvalidDataException">The field has non-zero bytes after its null terminator.</exception>
    internal static string ReadFixedString(EndianReader reader, int length)
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
    internal static void WriteFixedString(EndianWriter writer, string value, int length)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(value);
        if (bytes.Length > length)
            throw new ArgumentException($"Must be at most {length} bytes.", nameof(value));

        writer.Write(bytes);
        writer.Write(new byte[length - bytes.Length]);
    }
}

/// <summary>
/// An explicit interface used to interact with an <see cref="ICutsceneHeader"/>'s underlying values.
/// </summary>
public interface IPhysicalCutsceneHeader
{
    /// <summary>
    /// The <see cref="Common.AssetId"/> stored in the header itself.
    /// </summary>
    /// <remarks>
    /// When disagreements with <see cref="Asset.Id"/> exist, this field wins during serialization.
    /// </remarks>
    AssetId AssetId { get; set; }
    /// <summary>
    /// The number of <see cref="ICutsceneHeader.Data"/> entries, read directly from the header.
    /// </summary>
    /// <remarks>
    /// When disagreements with <see cref="ICutsceneHeader.Data"/>.Count exist, this field wins during serialization.
    /// </remarks>
    uint NumData { get; set; }
    /// <summary>
    /// The number of TimeChunk offsets following the (currently unparsed) region after
    /// <see cref="ICutsceneHeader.Data"/>.
    /// </summary>
    uint NumTime { get; set; }
    /// <summary>The largest model, in bytes including padding, in the unparsed chunk data.</summary>
    uint MaxModel { get; set; }
    /// <summary>The largest TimeChunk with an even <c>ChunkIndex</c>, in bytes including padding.</summary>
    uint MaxBufEven { get; set; }
    /// <summary>The largest TimeChunk with an odd <c>ChunkIndex</c>, in bytes including padding.</summary>
    uint MaxBufOdd { get; set; }
    /// <summary>
    /// The size, in bytes, of this header region - the fixed header, <see cref="ICutsceneHeader.Data"/>,
    /// and the unparsed TimeChunk-offset/visibility/break tables that follow it, before a standalone
    /// <see cref="CutsceneAsset"/>'s chunked model/animation/camera/sound data begins.
    /// </summary>
    uint HeaderSize { get; set; }
    /// <summary>The number of visibility entries in the unparsed region.</summary>
    uint VisCount { get; set; }
    /// <summary>The total size, in 4-byte steps, of the visibility entries in the unparsed region.</summary>
    uint VisSize { get; set; }
    /// <summary>The number of break entries in the unparsed region.</summary>
    uint BreakCount { get; set; }
}
