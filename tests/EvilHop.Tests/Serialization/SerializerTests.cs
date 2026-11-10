using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

/// <summary>
/// <see cref="Serializer"/> behavior that's independent of any specific game - exercised directly
/// through <see cref="TestSerializer"/> rather than <see cref="SerializerContractTests"/>'s
/// per-game subclasses.
/// </summary>
public class SerializerTests
{
    private sealed class NonSeekableStream : MemoryStream
    {
        public override bool CanSeek => false;
    }

    private sealed class UnregisteredBlock : Block
    {
        protected internal override string Tag => "ZZZZ";
    }

    [Fact]
    public void Write_NonSeekableStream_ThrowsArgumentException()
    {
        using var stream = new NonSeekableStream();

        var ex = Assert.Throws<ArgumentException>(() => new TestSerializer().Write(stream, []));
        Assert.Contains("seeking", ex.Message);
    }

    [Fact]
    public void WriteBlock_UnregisteredTag_ThrowsFormatException()
    {
        using var stream = new MemoryStream();
        using var writer = new EndianWriter(stream, Endianness.Big, leaveOpen: true);

        var ex = Assert.Throws<FormatException>(() => new TestSerializer().WriteBlockPublic(writer, new UnregisteredBlock()));
        Assert.Contains("ZZZZ", ex.Message);
    }

    public static TheoryData<GameVersion, Type> GameSerializers => new()
    {
        { GameVersion.N100F, typeof(N100FSerializer) },
        { GameVersion.BFBB, typeof(BFBBSerializer) },
        { GameVersion.Incredibles, typeof(IncrediblesSerializer) },
        { GameVersion.TSSM, typeof(TSSMSerializer) },
        { GameVersion.ROTU, typeof(ROTUSerializer) },
        { GameVersion.Ratatouille, typeof(RatatouilleSerializer) }
    };

    [Theory]
    [MemberData(nameof(GameSerializers))]
    public void Create_GameOnly_ReturnsThatGamesSerializerWithNoPlatform(GameVersion game, Type serializerType)
    {
        var serializer = Serializer.Create(game);

        Assert.IsType(serializerType, serializer);
        Assert.Equal(game, serializer.Profile.Game);
        Assert.Null(serializer.Profile.Platform);
    }

    [Theory]
    [MemberData(nameof(GameSerializers))]
    public void Create_GameAndPlatform_ReturnsThatGamesSerializerStatingThePlatform(GameVersion game, Type serializerType)
    {
        var serializer = Serializer.Create(game, Platform.Xbox);

        Assert.IsType(serializerType, serializer);
        Assert.Equal(game, serializer.Profile.Game);
        Assert.Equal(Platform.Xbox, serializer.Profile.Platform);
    }

    [Fact]
    public void Create_GameAndPlatform_KeepsTheGamesOtherSwitches()
    {
        var withPlatform = Serializer.Create(GameVersion.BFBB, Platform.PlayStation2).Profile;

        Assert.Equal(Serializer.Create(GameVersion.BFBB).Profile with { Platform = Platform.PlayStation2 }, withPlatform);
    }

    [Fact]
    public void Create_UnknownGame_ThrowsNotSupportedException() =>
        Assert.Throws<NotSupportedException>(() => Serializer.Create((GameVersion)999, Platform.GameCube));
}
