using EvilHop.Common;
using EvilHop.Serialization;

namespace EvilHop.Corpus.Tests;

public class SerializerFactoryTests
{
    [Fact]
    public void Create_N100F_ReturnsN100FSerializerCarryingTheProfile()
    {
        var profile = N100FSerializer.DefaultProfile with { StreamDataHasPaddingField = false };

        var serializer = SerializerFactory.Create(profile);

        Assert.IsType<N100FSerializer>(serializer);
        Assert.Equal(profile, serializer.Profile);
    }

    [Fact]
    public void Create_BFBB_ReturnsBFBBSerializerCarryingTheProfile()
    {
        var profile = BFBBSerializer.DefaultProfile with { StreamDataHasPaddingField = false };

        var serializer = SerializerFactory.Create(profile);

        Assert.IsType<BFBBSerializer>(serializer);
        Assert.Equal(profile, serializer.Profile);
    }

    [Fact]
    public void Create_UnimplementedGame_ThrowsWithAvailableGames()
    {
        var profile = new FormatProfile(GameVersion.TSSM, PlatformFieldOrder.PlatformNameRegionLanguage, StreamDataHasPaddingField: true);

        var ex = Assert.Throws<NotSupportedException>(() => SerializerFactory.Create(profile));

        Assert.Contains("TSSM", ex.Message);
        Assert.Contains("N100F", ex.Message);
        Assert.Contains("BFBB", ex.Message);
    }
}
