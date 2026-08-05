using EvilHop.Serialization;
using System.Text;

namespace EvilHop.Tests.Serialization;

public class MalformedInputTests
{
    private readonly IFormatSerializer _v1 = FileFormatFactory.GetSerializer(FileFormatVersion.Scooby);

    private static BinaryReader ReaderOf(params byte[] bytes) => new(new MemoryStream(bytes));

    private static byte[] BlockHeader(string id, uint length)
    {
        byte[] header = new byte[8];
        Encoding.ASCII.GetBytes(id).CopyTo(header, 0);
        BitConverter.GetBytes(length).Reverse().ToArray().CopyTo(header, 4);
        return header;
    }

    [Fact]
    public void ReadBlock_StreamDataPaddingExceedsBlockLength_Throws()
    {
        // DPAK declaring 8 bytes of content, but 0xFFFFFFFF bytes of padding
        byte[] bytes = [.. BlockHeader("DPAK", 8), 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00];

        Assert.Throws<InvalidDataException>(() => _v1.ReadBlock(ReaderOf(bytes)));
    }

    [Fact]
    public void ReadBlock_StreamDataLengthExceedsStream_Throws()
    {
        // DPAK declaring 1 GiB of content backed by 4 bytes of padding
        byte[] bytes = [.. BlockHeader("DPAK", 0x40000000), 0x00, 0x00, 0x00, 0x00];

        Assert.Throws<InvalidDataException>(() => _v1.ReadBlock(ReaderOf(bytes)));
    }

    [Fact]
    public void ReadBlock_LayerHeaderAssetCountExceedsStream_Throws()
    {
        // LHDR with a default layer type and an asset id count of 0xFFFFFFFF
        byte[] bytes = [.. BlockHeader("LHDR", 8), 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF];

        Assert.Throws<InvalidDataException>(() => _v1.ReadBlock(ReaderOf(bytes)));
    }

    [Fact]
    public void ReadBlock_NestingExceedsMaxDepth_Throws()
    {
        const int depth = 64;

        List<byte> bytes = [];
        for (int i = 0; i < depth; i++)
            bytes.AddRange(BlockHeader("PACK", (uint)(8 * (depth - 1 - i))));

        Assert.Throws<InvalidDataException>(() => _v1.ReadBlock(ReaderOf([.. bytes])));
    }
}
