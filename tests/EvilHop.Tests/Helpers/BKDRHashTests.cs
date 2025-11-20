using EvilHop.Helpers;

namespace EvilHop.Tests.Helpers;

public class BKDRHashTests
{
    [Theory]
    [InlineData("GLOVE_INTRO_TRIG", 10382301)]
    [InlineData("CHECKPOINT_ANIMLIST_01", 10770806)]
    [InlineData("GARY_SB_MARK", 35375619)]
    public static void Calculate_FromString_CorrectHash(string input, uint expectedHash)
    {
        Assert.Equal(expectedHash, BKDRHash.Calculate(input));
    }

    // todo: write test for strings that get trimmed, determine necessary logic for that
}
