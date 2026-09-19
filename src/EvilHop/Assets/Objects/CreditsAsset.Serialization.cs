using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;
using System.Text;

namespace EvilHop.Assets;

public sealed partial class CreditsAsset
{
    /// <summary>
    /// The key an encrypted <see cref="CreditsAsset"/>'s body is XORed against.
    /// </summary>
    private const string CipherKey = "xCMChunkHand";

    private const int SectionHeaderSize = 56;
    private const int PresetSize = 12 + 2 * 32;
    private const int HunkHeaderSize = 24;

    internal static CreditsAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new CreditsAsset();
        AssetFields.Populate(asset, header, debug);

        asset.Physical.Magic = reader.ReadUInt32();
        asset.Physical.Version = reader.ReadUInt32();
        asset.Physical.CreditsId = reader.ReadAssetId();
        asset.Physical.State = (CreditsState)reader.ReadUInt32();
        asset.Duration = reader.ReadSingle();
        asset.Physical.TotalSize = reader.ReadUInt32();

        byte[] body = reader.ReadRemainingBytes();
        if (asset.Physical.State is CreditsState.Encrypted)
            Decrypt(body);

        using var bodyReader = new EndianReader(new MemoryStream(body), profile.Endianness);
        while (body.Length - bodyReader.BaseStream.Position >= SectionHeaderSize)
            asset.Sections.Add(ReadSection(bodyReader));

        asset.SetUnparsedTail(body[(int)bodyReader.BaseStream.Position..]);
        asset.Physical.TotalSize = asset.ComputedTotalSize;
        return asset;
    }

    internal static void Write(CreditsAsset asset, EndianWriter writer, FormatProfile profile)
    {
        writer.Write(asset.Physical.Magic);
        writer.Write(asset.Physical.Version);
        writer.Write(asset.Physical.CreditsId);
        writer.Write((uint)asset.Physical.State);
        writer.Write(asset.Duration);
        writer.Write(asset.Physical.TotalSize);

        using var bodyStream = new MemoryStream();
        using (var bodyWriter = new EndianWriter(bodyStream, profile.Endianness, leaveOpen: true))
        {
            foreach (var section in asset.Sections)
                WriteSection(bodyWriter, section);
            bodyWriter.Write(asset.GetUnparsedTail());
        }

        byte[] body = bodyStream.ToArray();
        if (asset.Physical.State is CreditsState.Encrypted)
            Encrypt(body);

        writer.Write(body);
    }

    private static CreditsSection ReadSection(EndianReader r)
    {
        long sectionStart = r.BaseStream.Position;
        uint creditsSize = r.ReadUInt32();

        var section = new CreditsSection
        {
            Duration = r.ReadSingle(),
            Flags = r.ReadUInt32(),
            Start = new Vector2(r.ReadSingle(), r.ReadSingle()),
            End = new Vector2(r.ReadSingle(), r.ReadSingle()),
            ScrollRate = r.ReadSingle(),
            Lifetime = r.ReadSingle(),
            FadeInStart = r.ReadSingle(),
            FadeInEnd = r.ReadSingle(),
            FadeOutStart = r.ReadSingle(),
            FadeOutEnd = r.ReadSingle(),
        };

        uint numPresets = r.ReadUInt32();
        for (uint i = 0; i < numPresets; i++)
            section.Presets.Add(ReadPreset(r));

        while (r.BaseStream.Position - sectionStart < creditsSize)
            section.Hunks.Add(ReadHunk(r));

        return section;
    }

    private static void WriteSection(EndianWriter w, CreditsSection section)
    {
        w.Write((uint)SectionByteLength(section));
        w.Write(section.Duration);
        w.Write(section.Flags);
        w.Write(section.Start.X);
        w.Write(section.Start.Y);
        w.Write(section.End.X);
        w.Write(section.End.Y);
        w.Write(section.ScrollRate);
        w.Write(section.Lifetime);
        w.Write(section.FadeInStart);
        w.Write(section.FadeInEnd);
        w.Write(section.FadeOutStart);
        w.Write(section.FadeOutEnd);
        w.Write((uint)section.Presets.Count);

        foreach (var preset in section.Presets)
            WritePreset(w, preset);

        foreach (var hunk in section.Hunks)
            WriteHunk(w, hunk);
    }

    private static CreditsPreset ReadPreset(EndianReader r)
    {
        var preset = new CreditsPreset
        {
            Index = r.ReadUInt16(),
            Alignment = (CreditsPresetAlignment)r.ReadUInt16(),
            Delay = r.ReadSingle(),
            InnerSpacing = r.ReadSingle(),
        };

        if (preset.Alignment is CreditsPresetAlignment.Texture)
        {
            for (int i = 0; i < 2; i++)
                preset.Textures.Add(ReadTexture(r));
        }
        else
        {
            for (int i = 0; i < 2; i++)
                preset.Textboxes.Add(ReadTextbox(r));
        }

        return preset;
    }

    private static void WritePreset(EndianWriter w, CreditsPreset preset)
    {
        w.Write(preset.Index);
        w.Write((ushort)preset.Alignment);
        w.Write(preset.Delay);
        w.Write(preset.InnerSpacing);

        if (preset.Alignment is CreditsPresetAlignment.Texture)
        {
            for (int i = 0; i < 2; i++)
                WriteTexture(w, i < preset.Textures.Count ? preset.Textures[i] : new CreditsTexture());
        }
        else
        {
            for (int i = 0; i < 2; i++)
                WriteTextbox(w, i < preset.Textboxes.Count ? preset.Textboxes[i] : new CreditsTextbox());
        }
    }

    private static CreditsTextbox ReadTextbox(EndianReader r) => new()
    {
        Font = r.ReadUInt32(),
        Color = r.ReadRgba32(),
        CharSize = new Vector2(r.ReadSingle(), r.ReadSingle()),
        CharSpacing = new Vector2(r.ReadSingle(), r.ReadSingle()),
        Size = new Vector2(r.ReadSingle(), r.ReadSingle()),
    };

    private static void WriteTextbox(EndianWriter w, CreditsTextbox textbox)
    {
        w.Write(textbox.Font);
        w.WriteRgba32(textbox.Color);
        w.Write(textbox.CharSize.X);
        w.Write(textbox.CharSize.Y);
        w.Write(textbox.CharSpacing.X);
        w.Write(textbox.CharSpacing.Y);
        w.Write(textbox.Size.X);
        w.Write(textbox.Size.Y);
    }

    private static CreditsTexture ReadTexture(EndianReader r) => new()
    {
        TextureId = r.ReadAssetId(),
        Color = r.ReadRgba32(),
        Position = new Vector2(r.ReadSingle(), r.ReadSingle()),
        Size = new Vector2(r.ReadSingle(), r.ReadSingle()),
        Handle = r.ReadUInt32(),
        Padding = r.ReadUInt32(),
    };

    private static void WriteTexture(EndianWriter w, CreditsTexture texture)
    {
        w.Write(texture.TextureId);
        w.WriteRgba32(texture.Color);
        w.Write(texture.Position.X);
        w.Write(texture.Position.Y);
        w.Write(texture.Size.X);
        w.Write(texture.Size.Y);
        w.Write(texture.Handle);
        w.Write(texture.Padding);
    }

    /// <remarks>
    /// <paramref name="r"/>'s position is restored before returning; a hunk header's text offsets
    /// are absolute.
    /// </remarks>
    private static CreditsHunk ReadHunk(EndianReader r)
    {
        long hunkStart = r.BaseStream.Position;
        uint hunkSize = r.ReadUInt32();
        int presetIndex = r.ReadInt32();
        float t0 = r.ReadSingle();
        float t1 = r.ReadSingle();
        uint text1Offset = r.ReadUInt32();
        uint text2Offset = r.ReadUInt32();

        var hunk = new CreditsHunk
        {
            PresetIndex = presetIndex,
            StartTime = t0,
            EndTime = t1,
            Text1 = ReadInlineText(r, text1Offset),
            Text2 = ReadInlineText(r, text2Offset),
        };

        r.BaseStream.Position = hunkStart + hunkSize;
        return hunk;
    }

    /// <remarks>
    /// Each text immediately follows the hunk header.
    /// </remarks>
    private static void WriteHunk(EndianWriter w, CreditsHunk hunk)
    {
        long hunkStart = w.BaseStream.Position;
        int text1Length = PaddedTextLength(hunk.Text1);
        int text2Length = PaddedTextLength(hunk.Text2);

        w.Write((uint)(HunkHeaderSize + text1Length + text2Length));
        w.Write(hunk.PresetIndex);
        w.Write(hunk.StartTime);
        w.Write(hunk.EndTime);
        w.Write(hunk.Text1 is null ? 0u : (uint)(HeaderSize + hunkStart + HunkHeaderSize));
        w.Write(hunk.Text2 is null ? 0u : (uint)(HeaderSize + hunkStart + HunkHeaderSize + text1Length));

        WriteInlineText(w, hunk.Text1);
        WriteInlineText(w, hunk.Text2);
    }

    /// <summary>
    /// Reads the null-terminated string at <paramref name="offsetFromAssetStart"/>, an absolute
    /// offset from the start of this <see cref="CreditsAsset"/>'s header (i.e. its magic number), or
    /// <see langword="null"/> when the offset is 0.
    /// </summary>
    private static string? ReadInlineText(EndianReader r, uint offsetFromAssetStart)
    {
        if (offsetFromAssetStart == 0)
            return null;

        long savedPosition = r.BaseStream.Position;
        r.BaseStream.Position = offsetFromAssetStart - HeaderSize;

        var bytes = new List<byte>();
        byte next;
        while ((next = r.ReadByte()) != 0)
            bytes.Add(next);

        r.BaseStream.Position = savedPosition;
        return Encoding.Latin1.GetString([.. bytes]);
    }

    private static void WriteInlineText(EndianWriter w, string? text)
    {
        if (text is null)
            return;

        byte[] bytes = Encoding.Latin1.GetBytes(text);
        w.Write(bytes);
        for (int i = bytes.Length; i < PaddedTextLength(text); i++)
            w.Write((byte)0);
    }

    /// <summary>The on-disk length of <paramref name="text"/>: null-terminated, 4-byte aligned.</summary>
    private static int PaddedTextLength(string? text) =>
        text is null ? 0 : (Encoding.Latin1.GetByteCount(text) + 1 + 3) & ~3;

    private static int SectionByteLength(CreditsSection section) =>
        SectionHeaderSize + section.Presets.Count * PresetSize + section.Hunks.Sum(HunkByteLength);

    private static int HunkByteLength(CreditsHunk hunk) =>
        HunkHeaderSize + PaddedTextLength(hunk.Text1) + PaddedTextLength(hunk.Text2);

    private static void Decrypt(byte[] body)
    {
        byte last = 0;
        for (int i = 0; i < body.Length; i++)
        {
            last = (byte)(body[i] ^ last ^ CipherKey[i % CipherKey.Length]);
            body[i] = last;
        }
    }

    private static void Encrypt(byte[] body)
    {
        byte previousPlaintext = 0;
        for (int i = 0; i < body.Length; i++)
        {
            byte plaintext = body[i];
            body[i] = (byte)(plaintext ^ previousPlaintext ^ CipherKey[i % CipherKey.Length]);
            previousPlaintext = plaintext;
        }
    }
}
