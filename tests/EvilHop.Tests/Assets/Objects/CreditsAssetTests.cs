using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;
using System.Text;

namespace EvilHop.Tests.Serialization;

public class CreditsAssetTests
{
    private const string CipherKey = "xCMChunkHand";

    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new BFBBSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.Credits;
        header.Debug = debug;

        return (header, debug);
    }

    private static Asset Read(byte[] data, FormatProfile? profile = null)
    {
        profile ??= BFBBSerializer.DefaultProfile;
        var (header, debug) = HeaderFor();
        using var reader = new EndianReader(new MemoryStream(data), profile.Endianness);
        return AssetCodecs.Read(reader, header, debug, profile);
    }

    private static byte[] Write(Asset asset, FormatProfile? profile = null)
    {
        profile ??= BFBBSerializer.DefaultProfile;
        using var stream = new MemoryStream();
        using (var writer = new EndianWriter(stream, profile.Endianness, leaveOpen: true))
            AssetCodecs.Write(asset, writer, profile);
        return stream.ToArray();
    }

    private static byte[] U16(ushort value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] U32(uint value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] F32(float value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] V2(Vector2 v) => [.. F32(v.X), .. F32(v.Y)];

    private static byte[] HeaderBytes(uint magic, uint version, uint creditsId, uint state, float duration, uint totalSize) =>
    [
        .. U32(magic), .. U32(version), .. U32(creditsId), .. U32(state), .. F32(duration), .. U32(totalSize),
    ];

    private static byte[] PaddedText(string text)
    {
        byte[] bytes = Encoding.Latin1.GetBytes(text);
        int paddedLength = (bytes.Length + 1 + 3) & ~3;
        return [.. bytes, .. new byte[paddedLength - bytes.Length]];
    }

    private static byte Channel(float normalized) => (byte)MathF.Round(normalized * 255f);

    private static byte[] Textbox(uint font, Rgba color, Vector2 charSize, Vector2 charSpacing, Vector2 size) =>
    [
        .. U32(font), Channel(color.R), Channel(color.G), Channel(color.B), Channel(color.A),
        .. V2(charSize), .. V2(charSpacing), .. V2(size),
    ];

    private static byte[] Texture(uint textureId, Rgba color, Vector2 position, Vector2 size, uint handle, uint padding) =>
    [
        .. U32(textureId), Channel(color.R), Channel(color.G), Channel(color.B), Channel(color.A),
        .. V2(position), .. V2(size),
        .. U32(handle), .. U32(padding),
    ];

    private static byte[] Preset(ushort index, CreditsPresetAlignment alignment, float delay, float innerSpacing, byte[] box0, byte[] box1) =>
    [
        .. U16(index), .. U16((ushort)alignment), .. F32(delay), .. F32(innerSpacing), .. box0, .. box1,
    ];

    private static byte[] Hunk(int presetIndex, float startTime, float endTime, uint text1Offset, uint text2Offset, byte[] text1Bytes, byte[] text2Bytes) =>
    [
        .. U32((uint)(24 + text1Bytes.Length + text2Bytes.Length)),
        .. U32(unchecked((uint)presetIndex)),
        .. F32(startTime), .. F32(endTime),
        .. U32(text1Offset), .. U32(text2Offset),
        .. text1Bytes, .. text2Bytes,
    ];

    private static byte[] Section(float duration, uint flags, Vector2 start, Vector2 end, float scrollRate, float lifetime,
        float fadeInStart, float fadeInEnd, float fadeOutStart, float fadeOutEnd, int presetCount, byte[] presets, byte[] hunks) =>
    [
        .. U32((uint)(56 + presets.Length + hunks.Length)),
        .. F32(duration), .. U32(flags),
        .. V2(start), .. V2(end),
        .. F32(scrollRate), .. F32(lifetime),
        .. F32(fadeInStart), .. F32(fadeInEnd), .. F32(fadeOutStart), .. F32(fadeOutEnd),
        .. U32((uint)presetCount),
        .. presets, .. hunks,
    ];

    // Section 0: one Center preset (two textboxes) and one hunk with Text1 set.
    private static byte[] Section0PresetBytes() => Preset(
        index: 7, alignment: CreditsPresetAlignment.Center, delay: 0.25f, innerSpacing: 0f,
        box0: Textbox(font: 1, color: new Rgba(255 / 255f, 255 / 255f, 0 / 255f, 255 / 255f), charSize: new Vector2(10, 12), charSpacing: new Vector2(1, 2), size: new Vector2(0.8f, 0.05f)),
        box1: Textbox(font: 2, color: new Rgba(0 / 255f, 255 / 255f, 255 / 255f, 128 / 255f), charSize: new Vector2(8, 10), charSpacing: new Vector2(0.5f, 1), size: new Vector2(0.6f, 0.04f)));

    // Text1's absolute offset (from the asset's magic number) for this hunk: 24 (header) + 56
    // (section header) + 76 (preset) + 24 (hunk header) = 180.
    private static byte[] Section0HunkBytes() =>
        Hunk(presetIndex: 0, startTime: 0f, endTime: 5f, text1Offset: 180, text2Offset: 0, text1Bytes: PaddedText("Test"), text2Bytes: []);

    private static byte[] Section0Bytes() => Section(
        duration: 12.5f, flags: 1, start: new Vector2(0, 1), end: new Vector2(0.5f, 0),
        scrollRate: 20, lifetime: 30, fadeInStart: 0, fadeInEnd: 0.1f, fadeOutStart: 0.9f, fadeOutEnd: 1,
        presetCount: 1, presets: Section0PresetBytes(), hunks: Section0HunkBytes());

    // Section 1: one Texture preset (two textures) and one textless hunk.
    private static byte[] Section1PresetBytes() => Preset(
        index: 3, alignment: CreditsPresetAlignment.Texture, delay: 1.5f, innerSpacing: 0.02f,
        // Handle and padding are non-zero here on purpose: real archives carry leftover garbage in
        // both, so a fixture that zeroes them can't catch a codec that discards rather than
        // preserves them.
        box0: Texture(textureId: 0x11223344, color: new Rgba(10 / 255f, 20 / 255f, 30 / 255f, 255 / 255f), position: new Vector2(0.1f, 0.1f), size: new Vector2(0.2f, 0.2f), handle: 0x3DCCCCCD, padding: 0),
        box1: Texture(textureId: 0x55667788, color: new Rgba(40 / 255f, 50 / 255f, 60 / 255f, 200 / 255f), position: new Vector2(0.3f, 0.3f), size: new Vector2(0.25f, 0.25f), handle: 0, padding: 0x3F19999A));

    private static byte[] Section1HunkBytes() =>
        Hunk(presetIndex: 0, startTime: 1f, endTime: 6f, text1Offset: 0, text2Offset: 0, text1Bytes: [], text2Bytes: []);

    private static byte[] Section1Bytes() => Section(
        duration: 8f, flags: 0, start: new Vector2(0.2f, 0.3f), end: new Vector2(0.4f, 0.6f),
        scrollRate: 15, lifetime: 9, fadeInStart: 0.05f, fadeInEnd: 0.15f, fadeOutStart: 0.8f, fadeOutEnd: 0.95f,
        presetCount: 1, presets: Section1PresetBytes(), hunks: Section1HunkBytes());

    private static byte[] SampleBody() => [.. Section0Bytes(), .. Section1Bytes()];

    private static byte[] SampleCreditsData(uint state = (uint)CreditsState.NotEncrypted)
    {
        byte[] body = SampleBody();
        return [.. HeaderBytes(0xBEEEEEEF, 512, 0, state, 148.5f, 24 + (uint)body.Length), .. body];
    }

    private static byte[] Encrypt(byte[] plaintext)
    {
        byte[] ciphertext = new byte[plaintext.Length];
        byte previous = 0;
        for (int i = 0; i < plaintext.Length; i++)
        {
            byte value = plaintext[i];
            ciphertext[i] = (byte)(value ^ previous ^ CipherKey[i % CipherKey.Length]);
            previous = value;
        }
        return ciphertext;
    }

    [Fact]
    public void Read_Credits_ProducesCreditsAsset() =>
        Assert.IsType<CreditsAsset>(Read(HeaderBytes(0xBEEEEEEF, 256, 0, 1, 148.5f, 24)));

    [Fact]
    public void Read_Credits_PopulatesHeaderFields()
    {
        var asset = (CreditsAsset)Read(HeaderBytes(0xBEEEEEEF, 256, 0xAABBCCDD, 1, 148.5f, 24));

        Assert.Equal(0xBEEEEEEFu, asset.Physical.Magic);
        Assert.Equal(256u, asset.Physical.Version);
        Assert.Equal(new AssetId(0xAABBCCDD), asset.Physical.CreditsId);
        Assert.Equal(CreditsState.NotEncrypted, asset.Physical.State);
        Assert.False(asset.IsEncrypted);
        Assert.Equal(148.5f, asset.Duration);
    }

    [Fact]
    public void Read_Credits_PopulatesSections()
    {
        var asset = (CreditsAsset)Read(SampleCreditsData());

        Assert.Equal(2, asset.Sections.Count);

        var section = asset.Sections[0];
        Assert.Equal(12.5f, section.Duration);
        Assert.Equal(1u, section.Flags);
        Assert.Equal(new Vector2(0, 1), section.Start);
        Assert.Equal(new Vector2(0.5f, 0), section.End);
        Assert.Equal(20f, section.ScrollRate);
        Assert.Equal(30f, section.Lifetime);
        Assert.Equal(0f, section.FadeInStart);
        Assert.Equal(0.1f, section.FadeInEnd);
        Assert.Equal(0.9f, section.FadeOutStart);
        Assert.Equal(1f, section.FadeOutEnd);
    }

    [Fact]
    public void Read_Credits_PopulatesTextboxPreset()
    {
        var asset = (CreditsAsset)Read(SampleCreditsData());

        var preset = Assert.Single(asset.Sections[0].Presets);
        Assert.Equal(7, preset.Index);
        Assert.Equal(CreditsPresetAlignment.Center, preset.Alignment);
        Assert.Equal(0.25f, preset.Delay);
        Assert.Equal(0f, preset.InnerSpacing);
        Assert.Empty(preset.Textures);

        Assert.Equal(2, preset.Textboxes.Count);
        var textbox = preset.Textboxes[0];
        Assert.Equal(1u, textbox.Font);
        Assert.Equal(new Rgba(255 / 255f, 255 / 255f, 0 / 255f, 255 / 255f), textbox.Color);
        Assert.Equal(new Vector2(10, 12), textbox.CharSize);
        Assert.Equal(new Vector2(1, 2), textbox.CharSpacing);
        Assert.Equal(new Vector2(0.8f, 0.05f), textbox.Size);
    }

    [Fact]
    public void Read_Credits_PopulatesTexturePreset()
    {
        var asset = (CreditsAsset)Read(SampleCreditsData());

        var preset = Assert.Single(asset.Sections[1].Presets);
        Assert.Equal(3, preset.Index);
        Assert.Equal(CreditsPresetAlignment.Texture, preset.Alignment);
        Assert.Equal(1.5f, preset.Delay);
        Assert.Equal(0.02f, preset.InnerSpacing);
        Assert.Empty(preset.Textboxes);

        Assert.Equal(2, preset.Textures.Count);
        var texture = preset.Textures[0];
        Assert.Equal(new AssetId(0x11223344), texture.TextureId);
        Assert.Equal(new Rgba(10 / 255f, 20 / 255f, 30 / 255f, 255 / 255f), texture.Color);
        Assert.Equal(new Vector2(0.1f, 0.1f), texture.Position);
        Assert.Equal(new Vector2(0.2f, 0.2f), texture.Size);
        Assert.Equal(0x3DCCCCCDu, texture.Handle);
        Assert.Equal(0u, texture.Padding);
    }

    [Fact]
    public void Read_Credits_PopulatesHunkWithText()
    {
        var asset = (CreditsAsset)Read(SampleCreditsData());

        var hunk = Assert.Single(asset.Sections[0].Hunks);
        Assert.Equal(0, hunk.PresetIndex);
        Assert.Equal(0f, hunk.StartTime);
        Assert.Equal(5f, hunk.EndTime);
        Assert.Equal("Test", hunk.Text1);
        Assert.Null(hunk.Text2);
    }

    [Fact]
    public void Read_Credits_PopulatesHunkWithoutText()
    {
        var asset = (CreditsAsset)Read(SampleCreditsData());

        var hunk = Assert.Single(asset.Sections[1].Hunks);
        Assert.Equal(1f, hunk.StartTime);
        Assert.Equal(6f, hunk.EndTime);
        Assert.Null(hunk.Text1);
        Assert.Null(hunk.Text2);
    }

    [Fact]
    public void Read_EncryptedCredits_DecryptsBody()
    {
        byte[] body = SampleBody();
        byte[] data = [.. HeaderBytes(0xBEEEEEEF, 512, 0, (uint)CreditsState.Encrypted, 148.5f, 24 + (uint)body.Length), .. Encrypt(body)];

        var asset = (CreditsAsset)Read(data);

        Assert.Equal(2, asset.Sections.Count);
        Assert.Equal("Test", asset.Sections[0].Hunks[0].Text1);
    }

    [Fact]
    public void Read_Credits_TotalSizeDerivesFromActualEncodedSize()
    {
        // The header's own total_size field is deliberately wrong here - it should be discarded and
        // re-derived from the actual encoded size of the header plus every section once fully read.
        byte[] body = SampleBody();
        byte[] data = [.. HeaderBytes(0xBEEEEEEF, 512, 0, (uint)CreditsState.NotEncrypted, 10f, totalSize: 999), .. body];

        var asset = (CreditsAsset)Read(data);

        Assert.Equal((uint)(24 + body.Length), asset.Physical.TotalSize);
    }

    [Theory]
    [InlineData(true, CreditsState.Encrypted)]
    [InlineData(false, CreditsState.NotEncrypted)]
    public void IsEncrypted_Set_UpdatesPhysicalState(bool isEncrypted, CreditsState expectedState)
    {
        var asset = new CreditsAsset
        {
            IsEncrypted = isEncrypted
        };

        Assert.Equal(expectedState, asset.Physical.State);
    }

    [Fact]
    public void TotalSize_DisagreeingWithComputedSize_IsStoredIndependently()
    {
        var asset = new CreditsAsset();
        asset.Sections.Add(new CreditsSection());

        asset.Physical.TotalSize = 999;

        Assert.Equal(999u, asset.Physical.TotalSize);
        Assert.Single(asset.Sections);
    }

    [Fact]
    public void TotalSize_MatchingComputedSize_DerivesFromSections()
    {
        var asset = new CreditsAsset();
        asset.Sections.Add(new CreditsSection());

        asset.Physical.TotalSize = 24 + 56;
        asset.Sections.Add(new CreditsSection());

        Assert.Equal((uint)(24 + 56 * 2), asset.Physical.TotalSize);
    }

    [Fact]
    public void Read_ThenWrite_Credits_ReproducesInputBytes()
    {
        byte[] data = HeaderBytes(0xBEEEEEEF, 256, 0, 1, 148.5f, 24);

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_CreditsWithSections_ReproducesInputBytes()
    {
        byte[] data = SampleCreditsData();

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_CreditsWithUnparsedTail_ReproducesInputBytes()
    {
        // total_size describes the asset's entire on-disk size, so a real file with trailing bytes
        // neither section could account for would still declare them as part of its own length.
        byte[] body = SampleBody();
        byte[] tail = [0xDE, 0xAD, 0xBE, 0xEF];
        byte[] data = [.. HeaderBytes(0xBEEEEEEF, 512, 0, (uint)CreditsState.NotEncrypted, 148.5f, 24 + (uint)body.Length + (uint)tail.Length), .. body, .. tail];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_EncryptedCredits_ReproducesInputBytes()
    {
        byte[] body = SampleBody();
        byte[] data = [.. HeaderBytes(0xBEEEEEEF, 512, 0, (uint)CreditsState.Encrypted, 148.5f, 24 + (uint)body.Length), .. Encrypt(body)];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_Credits_UnderN100F_DegradesToGenericAsset()
    {
        byte[] data = HeaderBytes(0xBEEEEEEF, 256, 0, 1, 148.5f, 24);

        var asset = Read(data, N100FSerializer.DefaultProfile);

        Assert.IsNotType<CreditsAsset>(asset);
        Assert.Equal(data, asset.GetUnparsedTail().ToArray());
    }
}
