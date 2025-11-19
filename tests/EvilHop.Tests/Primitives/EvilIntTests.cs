using EvilHop.Primitives;

namespace EvilHop.Tests.Primitives;

public class EvilIntTests
{
    [Theory]
    [InlineData(new byte[] { 0x00, 0x00, 0x00, 0x00 }, 0)]
    [InlineData(new byte[] { 0x00, 0x00, 0x00, 0x6E }, 110)]
    [InlineData(new byte[] { 0x3D, 0x50, 0x21, 0xAA }, 1028661674)]
    public void ReadEvilInt_ValidBytes_CorrectUInt(byte[] bytes, uint expected)
    {
        using BinaryReader reader = new(new MemoryStream(bytes));
        Assert.Equal(expected, reader.ReadEvilInt());
        // asserts that we didn't consume too much of the stream
        Assert.Equal(reader.BaseStream.Length, reader.BaseStream.Position);
    }

    [Theory]
    [InlineData(0, new byte[] { 0x00, 0x00, 0x00, 0x00 })]
    [InlineData(110, new byte[] { 0x00, 0x00, 0x00, 0x6E })]
    [InlineData(1028661674, new byte[] { 0x3D, 0x50, 0x21, 0xAA })]
    public void ToEvilBytes_ValidUInt_CorrectBytes(uint val, byte[] expected) => Assert.Equal(expected, val.ToEvilBytes());

    [Theory]
    [InlineData(new byte[] { 0x00, 0x00, 0x00, 0x00 }, 0)]
    [InlineData(new byte[] { 0x00, 0x00, 0x00, 0x6E }, 110)]
    [InlineData(new byte[] { 0x3D, 0x50, 0x21, 0xAA }, 1028661674)]
    public void ToEvilInt_ValidBytes_CorrectUInt(byte[] bytes, uint expected) => Assert.Equal(expected, bytes.ToEvilInt());

    [Fact]
    public void ToEvilInt_InvalidBytes_Throws()
    {
        byte[] bytes = [0x00, 0x00, 0x00];
        Assert.Throws<ArgumentOutOfRangeException>(() => bytes.ToEvilInt());
    }
}
