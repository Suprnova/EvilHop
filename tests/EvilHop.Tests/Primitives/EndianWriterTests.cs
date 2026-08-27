using EvilHop.Primitives;
using System.Numerics;

namespace EvilHop.Tests.Primitives;

public class EndianWriterTests
{
    private static byte[] Written(Endianness endianness, Action<EndianWriter> write)
    {
        using var stream = new MemoryStream();
        using (var writer = new EndianWriter(stream, endianness, leaveOpen: true))
            write(writer);
        return stream.ToArray();
    }

    public static IEnumerable<object[]> Int16Data =>
    [
        [Endianness.Big, new byte[] { 0x11, 0x22 }],
        [Endianness.Little, new byte[] { 0x22, 0x11 }],
    ];

    [Theory]
    [MemberData(nameof(Int16Data))]
    public void Write_Int16_RespectsConstructedEndianness(Endianness endianness, byte[] expected) =>
        Assert.Equal(expected, Written(endianness, w => w.Write((short)0x1122)));

    public static IEnumerable<object[]> Int32Data =>
    [
        [Endianness.Big, new byte[] { 0x11, 0x22, 0x33, 0x44 }],
        [Endianness.Little, new byte[] { 0x44, 0x33, 0x22, 0x11 }],
    ];

    [Theory]
    [MemberData(nameof(Int32Data))]
    public void Write_Int32_RespectsConstructedEndianness(Endianness endianness, byte[] expected) =>
        Assert.Equal(expected, Written(endianness, w => w.Write(0x11223344)));

    public static IEnumerable<object[]> UInt32Data =>
    [
        [Endianness.Big, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }],
        [Endianness.Little, new byte[] { 0xEF, 0xBE, 0xAD, 0xDE }],
    ];

    [Theory]
    [MemberData(nameof(UInt32Data))]
    public void Write_UInt32_RespectsConstructedEndianness(Endianness endianness, byte[] expected) =>
        Assert.Equal(expected, Written(endianness, w => w.Write(0xDEADBEEFu)));

    public static IEnumerable<object[]> SingleData =>
    [
        [Endianness.Big, new byte[] { 0x3F, 0xC0, 0x00, 0x00 }],
        [Endianness.Little, new byte[] { 0x00, 0x00, 0xC0, 0x3F }],
    ];

    [Theory]
    [MemberData(nameof(SingleData))]
    public void Write_Single_RespectsConstructedEndianness(Endianness endianness, byte[] expected) =>
        Assert.Equal(expected, Written(endianness, w => w.Write(1.5f)));

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
    public void Write_Vector3_RespectsConstructedEndianness(Endianness endianness, byte[] expected) =>
        Assert.Equal(expected, Written(endianness, w => w.Write(new Vector3(1.5f, -2.5f, 0.5f))));

    public static IEnumerable<object[]> AssetIdData =>
    [
        [Endianness.Big, new byte[] { 0x12, 0x34, 0x56, 0x78 }],
        [Endianness.Little, new byte[] { 0x78, 0x56, 0x34, 0x12 }],
    ];

    [Theory]
    [MemberData(nameof(AssetIdData))]
    public void Write_AssetId_RespectsConstructedEndianness(Endianness endianness, byte[] expected) =>
        Assert.Equal(expected, Written(endianness, w => w.Write(new AssetId(0x12345678))));
}
