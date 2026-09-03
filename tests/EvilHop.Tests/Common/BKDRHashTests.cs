using EvilHop.Common;

namespace EvilHop.Tests.Common;

public class BKDRHashTests
{
    [Theory]
    [InlineData("_PRJ_VWEP_WITCHDOCTOR", 0x001422B9u)]
    [InlineData("PARSYS_SD_LAND_WOOD", 0x003FB436u)]
    [InlineData("csd_jumpspring_00.anm", 0x00769287u)]
    [InlineData("PARSYS_PLAT_BREAKAWAY", 0x007D1398u)]
    [InlineData("white", 0x00E211F1u)]
    [InlineData("PAREMIT_PLAT_TREMBLE", 0x014DAF70u)]
    [InlineData("scooby_one_03b", 0x01761D12u)]
    [InlineData("scooby_one_06b", 0x01761E9Bu)]
    [InlineData("CAULDRON 1`0", 0x0571CD67u)]
    public void Calculate_RealAssetName_ExpectedValue(string name, uint expected)
    {
        uint hash = BKDRHash.Calculate(name);

        Assert.Equal(expected, hash);
    }

    [Theory]
    [InlineData("foo", "foo\0")]
    [InlineData("foo", "\0foo")]
    [InlineData("foobar", "foo\0bar")]
    [InlineData("foobar", "foo\0\0bar\0")]
    public void Calculate_StrayNullTerminators_DoesNotAffectHash(string name, string nameWithNulls) =>
        Assert.Equal(BKDRHash.Calculate(name), BKDRHash.Calculate(nameWithNulls));
}
