using EvilHop.Blocks;
using EvilHop.Serialization;
using EvilHop.Serialization.Validation;

namespace EvilHop.Tests.Blocks;

public class PackageTests
{
    private static readonly IFormatSerializer _scoobyPrototype = FileFormatFactory.GetSerializer(FileFormatVersion.ScoobyPrototype);
    private static readonly IFormatSerializer _scooby = FileFormatFactory.GetSerializer(FileFormatVersion.Scooby);

    private static readonly IFormatSerializer[] serializers = [_scoobyPrototype, _scooby];

    [Theory]
    [InlineData(
        FileFormatVersion.Scooby,
        new byte[]
        {
            0x50, 0x41, 0x43, 0x4B, 0x00, 0x00, 0x00, 0x6E, 0x50, 0x56, 0x45, 0x52, 0x00, 0x00, 0x00, 0x0C,
            0x00, 0x00, 0x00, 0x02, 0x00, 0x04, 0x00, 0x06, 0x00, 0x00, 0x00, 0x01, 0x50, 0x46, 0x4C, 0x47,
            0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x2E, 0x50, 0x43, 0x4E, 0x54, 0x00, 0x00, 0x00, 0x14,
            0x00, 0x00, 0x00, 0x34, 0x00, 0x00, 0x00, 0x0D, 0x0D, 0x88, 0xCF, 0x1C, 0x0D, 0x88, 0xCF, 0x1C,
            0x00, 0x00, 0x80, 0xB8, 0x50, 0x43, 0x52, 0x54, 0x00, 0x00, 0x00, 0x1E, 0x43, 0xC4, 0xD2, 0xA0,
            0x57, 0x65, 0x64, 0x20, 0x4A, 0x61, 0x6E, 0x20, 0x31, 0x31, 0x20, 0x30, 0x31, 0x3A, 0x34, 0x30,
            0x3A, 0x34, 0x38, 0x20, 0x32, 0x30, 0x30, 0x36, 0x00, 0x00, 0x50, 0x4D, 0x4F, 0x44, 0x00, 0x00,
            0x00, 0x04, 0x43, 0xC4, 0xD2, 0xA0,
        }
    )]
    public void Package_ValidBytes_InitializesCorrectly(FileFormatVersion fileVersion, byte[] bytes)
    {
        using BinaryReader reader = new(new MemoryStream(bytes));
        IFormatSerializer s = FileFormatFactory.GetSerializer(fileVersion);
        Package package = s.Read<Package>(reader);
        Assert.True(package.IsValid(s, out _));
    }

    [Theory]
    [InlineData(
        FileFormatVersion.ScoobyPrototype,
        new byte[]
        {
            0x50, 0x56, 0x45, 0x52, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x01,
            0x00, 0x00, 0x00, 0x01,
        }
    )]
    [InlineData(
        FileFormatVersion.Scooby,
        new byte[]
        {
            0x50, 0x56, 0x45, 0x52, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00, 0x00, 0x02, 0x00, 0x04, 0x00, 0x06,
            0x00, 0x00, 0x00, 0x01,
        }
    )]
    public void PackageVersion_ValidBytes_InitializesCorrectly(FileFormatVersion fileVersion, byte[] bytes)
    {
        using BinaryReader reader = new(new MemoryStream(bytes));
        IFormatSerializer s = FileFormatFactory.GetSerializer(fileVersion);
        PackageVersion version = s.Read<PackageVersion>(reader);
        Assert.True(version.IsValid(s, out _));
    }

    [Theory]
    [InlineData(
        new byte[]
        {
            0x50, 0x56, 0x45, 0x52, 0x00,
        }
    )]
    [InlineData(
        new byte[]
        {
            0x50, 0x56, 0x45, 0x52, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00,
        }
    )]
    [InlineData(
        new byte[]
        {
            0x50, 0x56, 0x45, 0x52, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00, 0x00, 0x02, 0x00, 0x04, 0x00, 0x06,
            0x00, 0x00,
        }
    )]
    public void PackageVersion_MalformedLength_Throws(byte[] bytes)
    {
        using BinaryReader reader = new(new MemoryStream(bytes));

        foreach (var s in serializers)
        {
            reader.BaseStream.Position = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() => s.Read<PackageVersion>(reader));
        }
    }

    [Theory]
    [InlineData(
        new byte[]
        {
            0x50, 0x56, 0x45, 0x52, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0A, 0x00, 0x0F,
            0x00, 0x00, 0x00, 0x01
        }
    )]
    public void PackageVersion_V1_MalformedSubVersion_Throws(byte[] bytes)
    {
        using BinaryReader reader = new(new MemoryStream(bytes));

        foreach (var s in serializers)
        {
            reader.BaseStream.Position = 0;
            Assert.False(s.Read<PackageVersion>(reader).IsValid(s, out _));
        }
    }

    [Theory]
    [InlineData(
        new byte[]
        {
            0x50, 0x56, 0x45, 0x52, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x01
        }
    )]
    public void PackageVersion_V1_MalformedClientVersion_Throws(byte[] bytes)
    {
        using BinaryReader reader = new(new MemoryStream(bytes));

        foreach (var s in serializers)
        {
            reader.BaseStream.Position = 0;
            Assert.False(s.Read<PackageVersion>(reader).IsValid(s, out _));
        }
    }

    // todo: tests for ClientVersion validity based on serializer

    [Theory]
    [InlineData(
        new byte[]
        {
            0x50, 0x56, 0x45, 0x52, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00, 0x00, 0x02, 0x00, 0x0A, 0x00, 0x0F,
            0x00, 0x00, 0x00, 0x00
        }
    )]
    public void PackageVersion_V1_MalformedCompatVersion_Throws(byte[] bytes)
    {
        using BinaryReader reader = new(new MemoryStream(bytes));

        foreach (var s in serializers)
        {
            reader.BaseStream.Position = 0;
            Assert.False(s.Read<PackageVersion>(reader).IsValid(s, out _));
        }
    }
}
