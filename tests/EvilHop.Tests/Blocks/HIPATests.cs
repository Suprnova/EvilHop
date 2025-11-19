using EvilHop.Blocks;
using EvilHop.Extensions;
using EvilHop.Serialization;

namespace EvilHop.Tests.Blocks;

public class HIPATests
{
    private readonly IFormatSerializer _v1 = FileFormatFactory.GetSerializer(1);

    [Fact]
    public void EmptyConstructor_DoesNotThrow()
    {
        _ = new HIPA();
        Assert.True(true);
    }

    [Fact]
    public void HIPA_V1_ValidBytes_CorrectOffset()
    {
        byte[] bytes = [0x48, 0x49, 0x50, 0x41, 0x00, 0x00, 0x00, 0x00];
        using BinaryReader reader = new(new MemoryStream(bytes));
        _ = _v1.Read<HIPA>(reader);
        Assert.Equal(reader.BaseStream.Length, reader.BaseStream.Position);
    }

    [Fact]
    public void HIPA_V1_InvalidMagicNumber_Throws()
    {
        byte[] bytes = [0x00, 0x41, 0x43, 0x4B, 0x00, 0x00, 0x00, 0x00];
        using BinaryReader reader = new(new MemoryStream(bytes));
        Assert.Throws<InvalidDataException>(() => _v1.Read<HIPA>(reader));
    }

    [Fact]
    public void HIPA_V1_InvalidBytes_Throws()
    {
        byte[] bytes = [0x48, 0x49, 0x50, 0x41];
        using BinaryReader reader = new(new MemoryStream(bytes));
        Assert.Throws<ArgumentOutOfRangeException>(() => _v1.Read<HIPA>(reader));
    }
}