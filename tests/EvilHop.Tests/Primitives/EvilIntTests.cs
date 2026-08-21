using EvilHop.Primitives;

namespace EvilHop.Tests.Primitives;

public class EvilIntTests
{
    public static IEnumerable<object[]> EvilIntData =>
    [
        [new byte[] { 0x00, 0x00, 0x00, 0x00 }, 0u],
        [new byte[] { 0x00, 0x00, 0x00, 0x6E }, 110u],
        [new byte[] { 0x00, 0x00, 0x00, 0x0C }, 12u],
        [new byte[] { 0x00, 0x01, 0xA8, 0xE0 }, 108768u],
        [new byte[] { 0x04, 0x9F, 0x00, 0x1A }, 77529114u],
        [new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, uint.MaxValue],
    ];

    [Theory]
    [MemberData(nameof(EvilIntData))]
    public void EvilInt_ReadEvilInt_ExpectedValue(byte[] data, uint expected)
    {
        BinaryReader reader = new(new MemoryStream(data));

        uint value = reader.ReadEvilInt();

        Assert.Equal(expected, value);
    }

    [Fact]
    public void EvilInt_ReadEvilInt_InsufficientBytes_ThrowsArgumentOutOfRangeException()
    {
        BinaryReader reader = new(new MemoryStream([0x00, 0x01]));

        Assert.Throws<ArgumentOutOfRangeException>(() => reader.ReadEvilInt());
    }

    [Theory]
    [MemberData(nameof(EvilIntData))]
    public void EvilInt_WriteEvilInt_ExpectedBytes(byte[] data, uint value)
    {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);

        writer.WriteEvilInt(value);

        Assert.Equal(data, stream.ToArray());
    }

    [Theory]
    [MemberData(nameof(EvilIntData))]
    public void EvilInt_ToEvilBytes_ExpectedBytes(byte[] data, uint value)
    {
        byte[] bytes = value.ToEvilBytes();

        Assert.Equal(data, bytes);
    }

    [Theory]
    [MemberData(nameof(EvilIntData))]
    public void EvilInt_ToEvilInt_ExpectedValue(byte[] data, uint expected)
    {
        uint value = data.AsSpan().ToEvilInt();

        Assert.Equal(expected, value);
    }
}
