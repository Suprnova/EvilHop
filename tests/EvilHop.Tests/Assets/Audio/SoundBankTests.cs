using System.Buffers.Binary;
using System.Text;
using static EvilHop.Assets.SoundInfoAsset;

namespace EvilHop.Tests.Assets.Audio;

public class SoundBankTests
{
    private const uint MonoGcAdpcm = 0x02001021; // LoopOff | Mono | Hardware3D | GcAdpcm

    internal static byte[] Little(int value) => BitConverter.GetBytes(value);

    internal static byte[] Little(uint value) => BitConverter.GetBytes(value);

    internal static byte[] Little(ushort value) => BitConverter.GetBytes(value);

    internal static byte[] Little(short value) => BitConverter.GetBytes(value);

    internal static byte[] Little(float value) => BitConverter.GetBytes(value);

    /// <summary>One channel's DSP state, big-endian, with coefficients 1..16 offset by <paramref name="seed"/>.</summary>
    internal static byte[] DspChannelBytes(short seed)
    {
        var bytes = new byte[0x2E];
        for (int i = 0; i < 16; i++) BinaryPrimitives.WriteInt16BigEndian(bytes.AsSpan(i * 2), (short)(seed + i));
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(0x22), (ushort)(seed & 0xFF)); // pred_scale
        BinaryPrimitives.WriteInt16BigEndian(bytes.AsSpan(0x2C), (short)-seed);           // loop_yn2
        return bytes;
    }

    internal static byte[] FullHeader(string name, uint sampleCount, int dataSize, uint loopStart, uint loopEnd, uint mode, uint rate, params byte[][] dsp) =>
    [
        .. Little((ushort)(0x50 + dsp.Length * 0x2E)),
        .. Encoding.ASCII.GetBytes(name.PadRight(30, '\0')),
        .. Little(sampleCount),
        .. Little(dataSize),
        .. Little(loopStart),
        .. Little(loopEnd),
        .. Little(mode),
        .. Little(rate),
        .. Little((ushort)200), // volume
        .. Little((short)100),  // pan
        .. Little((ushort)128), // priority
        .. Little((ushort)dsp.Length),
        .. Little(2.5f),        // min distance
        .. Little(500f),        // max distance
        .. Little(-300),        // frequency variation
        .. Little((ushort)7),   // volume variation
        .. Little((short)-9),   // pan variation
        .. dsp.SelectMany(channel => channel),
    ];

    internal static byte[] BasicHeader(uint sampleCount, int dataSize, params byte[][] dsp) =>
        [.. Little(sampleCount), .. Little(dataSize), .. dsp.SelectMany(channel => channel)];

    internal static byte[] Bank(uint mode, byte[][] headers, byte[][] data) =>
    [
        .. "FSB3"u8,
        .. Little(headers.Length),
        .. Little(headers.Sum(header => header.Length)),
        .. Little(data.Sum(d => d.Length)),
        .. Little(0x30001u),
        .. Little(mode),
        .. headers.SelectMany(header => header),
        .. data.SelectMany(d => d),
    ];

    private static readonly byte[] FirstData = [0x24, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07];

    private static readonly byte[] SecondData = [0x44, 0x11, 0x12, 0x13];

    /// <summary>A two-sample bank storing its second sample as a basic header.</summary>
    internal static byte[] BasicHeaderBank() => Bank(2,
        [FullHeader("first_sample", 14, FirstData.Length, 0, 13, MonoGcAdpcm, 32000, DspChannelBytes(0x24)),
         BasicHeader(7, SecondData.Length, DspChannelBytes(0x44))],
        [FirstData, SecondData]);

    private static SoundBank Load(byte[] bytes) => SoundBank.Load(new MemoryStream(bytes));

    private static byte[] Save(SoundBank bank)
    {
        using var stream = new MemoryStream();
        bank.SaveTo(stream);
        return stream.ToArray();
    }

    [Fact]
    public void Load_FullHeader_PopulatesEveryField()
    {
        byte[] bytes = Bank(0, [FullHeader("water", 14, FirstData.Length, 3, 13, MonoGcAdpcm, 22050, DspChannelBytes(0x24))], [FirstData]);

        var sample = Assert.Single(Load(bytes).Samples);

        Assert.Equal("water", sample.Name);
        Assert.Equal(14u, sample.SampleCount);
        Assert.Equal(3u, sample.LoopStart);
        Assert.Equal(13u, sample.LoopEnd);
        Assert.Equal(SampleMode.LoopOff | SampleMode.Mono | SampleMode.Hardware3D | SampleMode.GcAdpcm, sample.Mode);
        Assert.Equal(22050u, sample.SampleRate);
        Assert.Equal(200, sample.Volume);
        Assert.Equal(100, sample.Pan);
        Assert.Equal(128, sample.Priority);
        Assert.Equal(1, sample.ChannelCount);
        Assert.Equal(2.5f, sample.MinDistance);
        Assert.Equal(500f, sample.MaxDistance);
        Assert.Equal(-300, sample.SampleRateVariation);
        Assert.Equal(7, sample.VolumeVariation);
        Assert.Equal(-9, sample.PanVariation);
        Assert.Equal(FirstData, sample.Data);
    }

    [Fact]
    public void Load_DspChannel_ReadsBigEndian()
    {
        byte[] bytes = Bank(0, [FullHeader("water", 14, FirstData.Length, 0, 13, MonoGcAdpcm, 32000, DspChannelBytes(0x24))], [FirstData]);

        var channel = Assert.Single(Assert.Single(Load(bytes).Samples).DspChannels);

        Assert.Equal(Enumerable.Range(0x24, 16).Select(i => (short)i), channel.Coefficients);
        Assert.Equal(0x24, channel.PredictorScale);
        Assert.Equal(-0x24, channel.LoopHistory2);
    }

    [Fact]
    public void Load_StereoFullHeader_ReadsOneDspChannelPerChannel()
    {
        byte[] bytes = Bank(0, [FullHeader("music", 14, FirstData.Length, 0, 13, 0x02080041, 32000, DspChannelBytes(0x10), DspChannelBytes(0x20))], [FirstData]);

        var sample = Assert.Single(Load(bytes).Samples);

        Assert.Equal(2, sample.ChannelCount);
        Assert.Equal(2, sample.DspChannels.Count);
        Assert.Equal(0x20, sample.DspChannels[1].PredictorScale);
    }

    [Fact]
    public void Load_BasicHeader_InheritsFirstSampleFields()
    {
        var bank = Load(BasicHeaderBank());

        var second = bank.Samples[1];

        Assert.Equal(string.Empty, second.Name);
        Assert.Equal(7u, second.SampleCount);
        Assert.Equal(6u, second.LoopEnd);
        Assert.Equal(bank.Samples[0].SampleRate, second.SampleRate);
        Assert.Equal(bank.Samples[0].Mode, second.Mode);
        Assert.Equal(bank.Samples[0].MaxDistance, second.MaxDistance);
        Assert.Equal(0x44, Assert.Single(second.DspChannels).PredictorScale);
        Assert.Equal(SecondData, second.Data);
    }

    [Fact]
    public void Load_ThenSave_BasicHeaderBank_ReproducesInputBytes()
    {
        byte[] bytes = BasicHeaderBank();

        Assert.Equal(bytes, Save(Load(bytes)));
    }

    [Fact]
    public void Load_ThenSave_FullHeaderBank_ReproducesInputBytes()
    {
        byte[] bytes = Bank(0,
            [FullHeader("first", 14, FirstData.Length, 0, 13, MonoGcAdpcm, 32000, DspChannelBytes(0x24)),
             FullHeader("second", 7, SecondData.Length, 2, 6, 0x02001022, 16000, DspChannelBytes(0x44))],
            [FirstData, SecondData]);

        Assert.Equal(bytes, Save(Load(bytes)));
    }

    [Fact]
    public void Save_SampleMovedOutOfBasicHeaderBank_WritesItAsFullHeader()
    {
        var source = Load(BasicHeaderBank());
        var sample = source.Samples[1];
        source.Samples.Remove(sample);
        var bank = new SoundBank { Mode = BankMode.BasicHeaders };
        bank.Samples.Add(sample);

        byte[] expected = Bank(2, [FullHeader("", 7, SecondData.Length, 0, 6, MonoGcAdpcm, 32000, DspChannelBytes(0x44))], [SecondData]);

        Assert.Equal(expected, Save(bank));
    }

    [Fact]
    public void Load_WrongMagic_ThrowsInvalidDataException()
    {
        byte[] bytes = [.. "FSB4"u8, .. new byte[20]];

        Assert.Throws<InvalidDataException>(() => Load(bytes));
    }

    [Fact]
    public void WritePadded_WithNoPadding_ZeroFillsToNext32Bytes()
    {
        var bank = Load(BasicHeaderBank());

        byte[] written = SoundBank.WritePadded(bank);

        Assert.Equal(0, written.Length % 32);
        Assert.All(written[BasicHeaderBank().Length..], b => Assert.Equal(0, b));
    }

    [Fact]
    public void WritePadded_WithLongerPadding_TruncatesToGap()
    {
        var bank = Load(BasicHeaderBank());
        bank.Padding = [.. Enumerable.Repeat((byte)0xCD, 64)];

        byte[] written = SoundBank.WritePadded(bank);

        int length = BasicHeaderBank().Length;
        Assert.Equal((length + 31) / 32 * 32, written.Length);
        Assert.All(written[length..], b => Assert.Equal(0xCD, b));
    }
}
