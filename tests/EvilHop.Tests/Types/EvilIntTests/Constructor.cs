using Xunit;

using EvilHop.Types;

namespace EvilHop.Tests.Types;

public partial class EvilIntTests
{
    public class Constructor
    {
        [Fact]
        public void Constructor_UInt32_MinValue_Initializes()
        {
            EvilInt evilInt = new EvilInt(UInt32.MinValue);
            Assert.Equal(evilInt.Value, UInt32.MinValue);
        }

        [Fact]
        public void Constructor_UInt32_MaxValue_Initializes()
        {
            EvilInt evilInt = new EvilInt(UInt32.MaxValue);
            Assert.Equal(evilInt.Value, UInt32.MaxValue);
        }

        [Theory]
        [InlineData(new byte[] { 0x00, 0x00, 0x00, 0x00 }, 0)]
        [InlineData(new byte[] { 0x00, 0x00, 0x00, 0x6E }, 110)]
        [InlineData(new byte[] { 0x3D, 0x50, 0x21, 0xAA }, 1028661674)]
        public void Constructor_Span_Initializes(byte[] bytes, UInt32 expectedValue)
        {
            EvilInt evilInt = new EvilInt(bytes.AsSpan());
            Assert.Equal(evilInt.Value, expectedValue);
        }
    }
}
