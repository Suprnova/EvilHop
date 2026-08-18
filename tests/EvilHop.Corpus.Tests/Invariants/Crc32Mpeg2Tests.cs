using EvilHop.Corpus.Invariants;
using System.Text;

namespace EvilHop.Corpus.Tests.Invariants;

public class Crc32Mpeg2Tests
{
    [Fact]
    public void Compute_EmptyData_ReturnsInitialValue()
    {
        uint crc = Crc32Mpeg2.Compute([]);

        Assert.Equal(0xFFFFFFFFu, crc);
    }

    [Fact]
    public void Compute_StandardCheckVector_MatchesKnownValue()
    {
        // "123456789" is the standard CRC-32/MPEG-2 check vector; expected value per the reveng catalogue.
        uint crc = Crc32Mpeg2.Compute(Encoding.ASCII.GetBytes("123456789"));

        Assert.Equal(0x0376E6E7u, crc);
    }
}
