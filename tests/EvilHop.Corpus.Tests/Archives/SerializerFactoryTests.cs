using EvilHop.Common;
using EvilHop.Corpus.Archives;
using EvilHop.Serialization;

namespace EvilHop.Corpus.Tests.Archives;

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
    public void Create_Incredibles_ReturnsIncrediblesSerializerCarryingTheProfile()
    {
        var profile = IncrediblesSerializer.DefaultProfile with { StreamDataHasPaddingField = false };

        var serializer = SerializerFactory.Create(profile);

        Assert.IsType<IncrediblesSerializer>(serializer);
        Assert.Equal(profile, serializer.Profile);
    }

    [Fact]
    public void Create_TSSM_ReturnsTSSMSerializerCarryingTheProfile()
    {
        var profile = TSSMSerializer.DefaultProfile with { StreamDataHasPaddingField = false };

        var serializer = SerializerFactory.Create(profile);

        Assert.IsType<TSSMSerializer>(serializer);
        Assert.Equal(profile, serializer.Profile);
    }

    [Fact]
    public void Create_ROTU_ReturnsROTUSerializerCarryingTheProfile()
    {
        var profile = ROTUSerializer.DefaultProfile with { StreamDataHasPaddingField = false };

        var serializer = SerializerFactory.Create(profile);

        Assert.IsType<ROTUSerializer>(serializer);
        Assert.Equal(profile, serializer.Profile);
    }

    [Fact]
    public void Create_Ratatouille_ReturnsRatatouilleSerializerCarryingTheProfile()
    {
        var profile = RatatouilleSerializer.DefaultProfile with { StreamDataHasPaddingField = false };

        var serializer = SerializerFactory.Create(profile);

        Assert.IsType<RatatouilleSerializer>(serializer);
        Assert.Equal(profile, serializer.Profile);
    }

    [Fact]
    public void Create_UnimplementedGame_ThrowsWithAvailableGames()
    {
        // Every GameVersion member has a serializer today, so this uses a value outside the enum's
        // defined range to exercise the switch's default arm rather than a real, still-missing game.
        var profile = new FormatProfile((GameVersion)999, PlatformFieldOrder.PlatformNameRegionLanguage, StreamDataHasPaddingField: true);

        var ex = Assert.Throws<NotSupportedException>(() => SerializerFactory.Create(profile));

        Assert.Contains("N100F", ex.Message);
        Assert.Contains("BFBB", ex.Message);
        Assert.Contains("Incredibles", ex.Message);
        Assert.Contains("TSSM", ex.Message);
        Assert.Contains("ROTU", ex.Message);
        Assert.Contains("Ratatouille", ex.Message);
    }
}
