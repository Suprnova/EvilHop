using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Text;

namespace EvilHop.Assets;

public sealed partial class CutsceneAsset
{
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
