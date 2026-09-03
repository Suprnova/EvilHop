using EvilHop.Blocks;
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
}
