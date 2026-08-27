using EvilHop.Primitives;
using System.Numerics;

namespace EvilHop.Tests.Primitives;

public class EndianReaderTests
{
    private static EndianReader Reader(Endianness endianness, byte[] bytes) =>
        new(new MemoryStream(bytes), endianness);

    public static IEnumerable<object[]> Int16Data =>
    [
        [Endianness.Big, new byte[] { 0x11, 0x22 }, (short)0x1122],
        [Endianness.Little, new byte[] { 0x22, 0x11 }, (short)0x1122],
    ];

    [Theory]
    [MemberData(nameof(Int16Data))]
    public void ReadInt16_RespectsConstructedEndianness(Endianness endianness, byte[] bytes, short expected) =>
        Assert.Equal(expected, Reader(endianness, bytes).ReadInt16());

    public static IEnumerable<object[]> Int32Data =>
    [
        [Endianness.Big, new byte[] { 0x11, 0x22, 0x33, 0x44 }, 0x11223344],
        [Endianness.Little, new byte[] { 0x44, 0x33, 0x22, 0x11 }, 0x11223344],
    ];

    [Theory]
    [MemberData(nameof(Int32Data))]
    public void ReadInt32_RespectsConstructedEndianness(Endianness endianness, byte[] bytes, int expected) =>
        Assert.Equal(expected, Reader(endianness, bytes).ReadInt32());

    public static IEnumerable<object[]> UInt32Data =>
    [
        [Endianness.Big, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }, 0xDEADBEEFu],
        [Endianness.Little, new byte[] { 0xEF, 0xBE, 0xAD, 0xDE }, 0xDEADBEEFu],
    ];

    [Theory]
    [MemberData(nameof(UInt32Data))]
    public void ReadUInt32_RespectsConstructedEndianness(Endianness endianness, byte[] bytes, uint expected) =>
        Assert.Equal(expected, Reader(endianness, bytes).ReadUInt32());

    public static IEnumerable<object[]> SingleData =>
    [
        [Endianness.Big, new byte[] { 0x3F, 0xC0, 0x00, 0x00 }, 1.5f],
        [Endianness.Little, new byte[] { 0x00, 0x00, 0xC0, 0x3F }, 1.5f],
    ];

    [Theory]
    [MemberData(nameof(SingleData))]
    public void ReadSingle_RespectsConstructedEndianness(Endianness endianness, byte[] bytes, float expected) =>
        Assert.Equal(expected, Reader(endianness, bytes).ReadSingle());

    public static IEnumerable<object[]> Vector3Data =>
    [
        [Endianness.Big, new byte[]
        {
            0x3F, 0xC0, 0x00, 0x00, // 1.5
            0xC0, 0x20, 0x00, 0x00, // -2.5
            0x3F, 0x00, 0x00, 0x00, // 0.5
        }],
        [Endianness.Little, new byte[]
        {
            0x00, 0x00, 0xC0, 0x3F, // 1.5
            0x00, 0x00, 0x20, 0xC0, // -2.5
            0x00, 0x00, 0x00, 0x3F, // 0.5
        }],
    ];

    [Theory]
    [MemberData(nameof(Vector3Data))]
    public void ReadVector3_RespectsConstructedEndianness(Endianness endianness, byte[] bytes) =>
        Assert.Equal(new Vector3(1.5f, -2.5f, 0.5f), Reader(endianness, bytes).ReadVector3());

    public static IEnumerable<object[]> AssetIdData =>
    [
        [Endianness.Big, new byte[] { 0x12, 0x34, 0x56, 0x78 }],
        [Endianness.Little, new byte[] { 0x78, 0x56, 0x34, 0x12 }],
    ];

    [Theory]
    [MemberData(nameof(AssetIdData))]
    public void ReadAssetId_RespectsConstructedEndianness(Endianness endianness, byte[] bytes) =>
        Assert.Equal(new AssetId(0x12345678), Reader(endianness, bytes).ReadAssetId());

    [Fact]
    public void ReadRemainingBytes_ReturnsEverythingPastTheCurrentPosition()
    {
        var reader = Reader(Endianness.Big, [0x01, 0x02, 0x03, 0x04, 0x05]);
        reader.ReadByte();
        reader.ReadByte();

        Assert.Equal<byte>([0x03, 0x04, 0x05], reader.ReadRemainingBytes());
    }
}
