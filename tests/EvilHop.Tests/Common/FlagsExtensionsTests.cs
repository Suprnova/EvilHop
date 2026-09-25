using EvilHop.Common;

namespace EvilHop.Tests.Common;

public class FlagsExtensionsTests
{
    [Flags]
    private enum Bits : byte
    {
        A = 1 << 0,
        B = 1 << 1,
        High = 1 << 7,
    }

    [Fact]
    public void WithFlag_True_SetsOnlyThatBit() =>
        Assert.Equal(Bits.A | Bits.B, Bits.A.WithFlag(Bits.B, true));

    [Fact]
    public void WithFlag_False_ClearsOnlyThatBit() =>
        Assert.Equal(Bits.A, (Bits.A | Bits.B).WithFlag(Bits.B, false));

    [Fact]
    public void WithFlag_HighBitOfUnsignedEnum_RoundTrips() =>
        Assert.Equal(Bits.High, ((Bits)0).WithFlag(Bits.High, true));

    [Fact]
    public void WithFlag_UndefinedBitsAlreadySet_ArePreserved() =>
        Assert.Equal((Bits)0x43, ((Bits)0x41).WithFlag(Bits.B, true));
}
