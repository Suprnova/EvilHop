using EvilHop.Blocks;
using EvilHop.Extensions;

namespace EvilHop.Tests.Blocks;

public class HIPATests
{
    [Fact]
    public void EmptyConstructor_DoesNotThrow()
    {
        _ = new HIPA();
        Assert.True(true);
    }

    [Fact]
    public void BinaryReaderConstructor_ValidBytes_CorrectOffset()
    {
        byte[] bytes = [0x48, 0x49, 0x50, 0x41, 0x00, 0x00, 0x00, 0x00];
        using BinaryReader reader = new(new MemoryStream(bytes));
        _ = new HIPA(reader);
        Assert.Equal(reader.BaseStream.Length, reader.BaseStream.Position);
    }

    [Fact]
    public void BinaryReaderConstructor_InvalidMagicNumber_Throws()
    {
        byte[] bytes = [0x50, 0x41, 0x43, 0x4B, 0x00, 0x00, 0x00, 0x00];
        using BinaryReader reader = new(new MemoryStream(bytes));
        Assert.Throws<ArgumentException>(() => new HIPA(reader));
    }

    [Fact]
    public void BinaryReaderConstructor_InvalidBytes_Throws()
    {
        byte[] bytes = [0x61, 0x62, 0x63, 0x00];
        using BinaryReader reader = new(new MemoryStream(bytes));
        Assert.Throws<ArgumentOutOfRangeException>(() => new HIPA(reader));
    }
}