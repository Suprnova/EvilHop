using EvilHop.Extensions;

namespace EvilHop.Tests.Extensions;

public class EvilStringTests
{
    [Theory]
    [InlineData("", new byte[] { 0x00, 0x00 })]
    [InlineData("abc", new byte[] { 0x61, 0x62, 0x63, 0x00 })]
    [InlineData("abcd", new byte[] { 0x61, 0x62, 0x63, 0x64, 0x00, 0x00 })]
    public void ToEvilBytes_ValidString_CorrectBytes(string str, byte[] expected) => Assert.Equal(expected, str.ToEvilBytes());
}
