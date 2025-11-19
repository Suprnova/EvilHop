using EvilHop.Primitives;

namespace EvilHop.Tests.Primitives;

public class EvilStringTests
{
    [Theory]
    [InlineData(new byte[] { 0x00, 0x00 }, "")]
    [InlineData(new byte[] { 0x61, 0x62, 0x63, 0x00 }, "abc")]
    [InlineData(new byte[] { 0x61, 0x62, 0x63, 0x64, 0x00, 0x00 }, "abcd")]
    public void ReadEvilString_ValidBytes_CorrectString(byte[] bytes, string expected)
    {
        using BinaryReader reader = new(new MemoryStream(bytes));
        Assert.Equal(expected, reader.ReadEvilString());
        // asserts that we didn't consume too much of the stream
        Assert.Equal(reader.BaseStream.Length, reader.BaseStream.Position);
    }

    [Theory]
    [InlineData(new byte[] { 0x61, 0x00, 0x62, 0x63, 0x00, 0x00, 0x64, 0x65, 0x66, 0x00 }, "a", "bc", "def")]
    public void ReadEvilString_ValidBytesConsecutive_CorrectStrings(byte[] bytes, params string[] expectedStrs)
    {
        using BinaryReader reader = new(new MemoryStream(bytes));
        foreach (var expected in expectedStrs)
        {
            Assert.Equal(expected, reader.ReadEvilString());
        }
    }

    [Theory]
    [InlineData(new byte[] { 0x00, 0x00, 0x00, 0x00 }, "", 2)]
    [InlineData(new byte[] { 0x61, 0x62, 0x63, 0x00, 0x64, 0x65, 0x66 }, "abc", 4)]
    [InlineData(new byte[] { 0x61, 0x62, 0x63, 0x64, 0x00, 0x00, 0x65, 0x66 }, "abcd", 6)]
    public void ReadEvilString_ValidBytes_TerminatesCorrectly(byte[] bytes, string expectedStr, long expectedOffset)
    {
        using BinaryReader reader = new(new MemoryStream(bytes));
        Assert.Equal(expectedStr, reader.ReadEvilString());
        Assert.Equal(expectedOffset, reader.BaseStream.Position);
    }

    [Fact]
    public void ReadEvilString_NotTerminated_Throws()
    {
        byte[] bytes = [0x61];
        using BinaryReader reader = new(new MemoryStream(bytes));
        Assert.Throws<EndOfStreamException>(() => reader.ReadEvilString());
    }

    // TODO: update to custom exception
    [Fact]
    public void ReadEvilString_NotProperlyTerminatedEOF_Throws()
    {
        byte[] bytes = [0x61, 0x62, 0x00];
        using BinaryReader reader = new(new MemoryStream(bytes));
        Assert.Throws<ArgumentOutOfRangeException>(() => reader.ReadEvilString());
    }

    //TODO: update to custom exception
    [Fact]
    public void ReadEvilString_NotProperlyTerminatedConsecutive_Throws()
    {
        byte[] bytes = [0x61, 0x62, 0x00, 0x61, 0x62, 0x63, 0x00];
        using BinaryReader reader = new(new MemoryStream(bytes));
        Assert.Throws<ArgumentOutOfRangeException>(() => reader.ReadEvilString());
    }

    [Theory]
    [InlineData("", new byte[] { 0x00, 0x00 })]
    [InlineData("abc", new byte[] { 0x61, 0x62, 0x63, 0x00 })]
    [InlineData("abcd", new byte[] { 0x61, 0x62, 0x63, 0x64, 0x00, 0x00 })]
    public void ToEvilBytes_ValidString_CorrectBytes(string str, byte[] expected) => Assert.Equal(expected, str.ToEvilBytes());

    [Fact]
    public void ToEvilBytes_InvalidCharacter_IsQuestionMark() => Assert.Equal(new byte[] { 0x3F, 0x00 }, "€".ToEvilBytes());

    [Theory]
    [InlineData(new byte[] { 0x00, 0x00 }, "")]
    [InlineData(new byte[] { 0x61, 0x62, 0x63, 0x00 }, "abc")]
    [InlineData(new byte[] { 0x61, 0x62, 0x63, 0x64, 0x00, 0x00 }, "abcd")]
    public void ToEvilString_ValidBytes_CorrectString(byte[] bytes, string expectedStr) => Assert.Equal(expectedStr, bytes.ToEvilString());

    [Fact]
    public void ToEvilString_NotTerminated_Throws()
    {
        byte[] bytes = [0x61, 0x62, 0x63];
        Assert.Throws<ArgumentOutOfRangeException>(() => bytes.ToEvilString());
    }

    // to demonstrate that ToEvilString does NOT care about properly terminated nulls via padding
    [Fact]
    public void ToEvilString_NotProperlyTerminated_DoesNotThrow()
    {
        byte[] bytes = [0x61, 0x62, 0x00];
        bytes.ToEvilString();
        Assert.True(true);
    }
}
