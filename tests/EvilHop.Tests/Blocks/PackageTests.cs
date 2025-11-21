using EvilHop.Blocks;
using EvilHop.Serialization;
using EvilHop.Serialization.Validation;
using static EvilHop.Blocks.PackageVersion;

namespace EvilHop.Tests.Blocks;

public class PackageTests
{
    private readonly IFormatSerializer _v1 = FileFormatFactory.GetSerializer(FileFormatVersion.Version1);
    private readonly SerializerOptions _strict = new() { Mode = ValidationMode.Strict };

    [Fact]
    public void Package_ExplicitConstructor_InitializesCorrectly()
    {
        // todo: technically invalid, default initializers for children expect a file version with PackagePlatform
        Package package = new(
            new PackageVersion(),
            new PackageFlags(),
            new PackageCount(),
            new PackageCreated(),
            new PackageModified()
            );
        //Assert.Equal(5, package.Children.Count);
        Assert.Equal(110U, _v1.GetBlockLength(package));
    }

    [Theory]
    [InlineData(
        new byte[]
        {
            0x50, 0x41, 0x43, 0x4B, 0x00, 0x00, 0x00, 0x6E, 0x50, 0x56, 0x45, 0x52, 0x00, 0x00, 0x00, 0x0C,
            0x00, 0x00, 0x00, 0x02, 0x00, 0x0A, 0x00, 0x0F, 0x00, 0x00, 0x00, 0x01, 0x50, 0x46, 0x4C, 0x47,
            0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x2E, 0x50, 0x43, 0x4E, 0x54, 0x00, 0x00, 0x00, 0x14,
            0x00, 0x00, 0x00, 0x34, 0x00, 0x00, 0x00, 0x0D, 0x0D, 0x88, 0xCF, 0x1C, 0x0D, 0x88, 0xCF, 0x1C,
            0x00, 0x00, 0x80, 0xB8, 0x50, 0x43, 0x52, 0x54, 0x00, 0x00, 0x00, 0x1E, 0x43, 0xC4, 0xD2, 0xA0,
            0x57, 0x65, 0x64, 0x20, 0x4A, 0x61, 0x6E, 0x20, 0x31, 0x31, 0x20, 0x30, 0x31, 0x3A, 0x34, 0x30,
            0x3A, 0x34, 0x38, 0x20, 0x32, 0x30, 0x30, 0x36, 0x00, 0x00, 0x50, 0x4D, 0x4F, 0x44, 0x00, 0x00,
            0x00, 0x04, 0x43, 0xC4, 0xD2, 0xA0,
        },
        5, 110
    )]
    public void Package_V1_ValidBytes_InitializesCorrectly(byte[] bytes, int expectedCount, uint expectedLength)
    {
        using BinaryReader reader = new(new MemoryStream(bytes));
        Package package = _v1.Read<Package>(reader, _strict);
        //Assert.Equal(expectedCount, package.Children.Count);
        Assert.Equal(expectedLength, _v1.GetBlockLength(package));
    }

    [Fact]
    public void PackageVersion_EmptyConstructor_InitializesCorrectly()
    {
        PackageVersion packageVersion = new();
        //Assert.Empty(packageVersion.Children);
        Assert.Equal(12U, _v1.GetBlockLength(packageVersion));
        Assert.Equal(SubVersion.Default, packageVersion.SubVer);
        Assert.Equal(ClientVersion.Default, packageVersion.ClientVer);
        Assert.Equal(CompatVersion.Default, packageVersion.CompatVer);
    }

    [Theory]
    [InlineData(
        new byte[]
        {
            0x50, 0x56, 0x45, 0x52, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00, 0x00, 0x02, 0x00, 0x04, 0x00, 0x06,
            0x00, 0x00, 0x00, 0x01,
        }, SubVersion.Default, ClientVersion.N100FRelease, CompatVersion.Default
    )]
    [InlineData(
        new byte[]
        {
            0x50, 0x56, 0x45, 0x52, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00, 0x00, 0x02, 0x00, 0x0A, 0x00, 0x0F,
            0x00, 0x00, 0x00, 0x01,
        }, SubVersion.Default, ClientVersion.Default, CompatVersion.Default
    )]
    public void PackageVersion_V1_ValidBytes_InitializesCorrectly(byte[] bytes, SubVersion subVersion, ClientVersion clientVersion, CompatVersion compatVersion)
    {
        using BinaryReader reader = new(new MemoryStream(bytes));
        PackageVersion packageVersion = _v1.Read<PackageVersion>(reader, _strict);
        //Assert.Empty(packageVersion.Children);
        Assert.Equal(12U, _v1.GetBlockLength(packageVersion));
        Assert.Equal(subVersion, packageVersion.SubVer);
        Assert.Equal(clientVersion, packageVersion.ClientVer);
        Assert.Equal(compatVersion, packageVersion.CompatVer);
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
    public void PackageVersion_V1_MalformedLength_Throws(byte[] bytes)
    {
        using BinaryReader reader = new(new MemoryStream(bytes));
        Assert.Throws<ArgumentOutOfRangeException>(() => _v1.Read<PackageVersion>(reader, _strict));
    }

    [Fact]
    public void PackageVersion_V1_MalformedSubVersion_Throws()
    {
        byte[] bytes = [
            0x50, 0x56, 0x45, 0x52, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0A, 0x00, 0x0F,
            0x00, 0x00, 0x00, 0x01
        ];
        using BinaryReader reader = new(new MemoryStream(bytes));
        Assert.Throws<InvalidDataException>(() => _v1.Read<PackageVersion>(reader, _strict));
    }

    [Fact]
    public void PackageVersion_V1_MalformedClientVersion_Throws()
    {
        byte[] bytes = [
            0x50, 0x56, 0x45, 0x52, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x01
        ];
        using BinaryReader reader = new(new MemoryStream(bytes));
        Assert.Throws<InvalidDataException>(() => _v1.Read<PackageVersion>(reader, _strict));
    }

    [Fact]
    public void PackageVersion_V1_MalformedCompatVersion_Throws()
    {
        byte[] bytes = [
            0x50, 0x56, 0x45, 0x52, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00, 0x00, 0x02, 0x00, 0x0A, 0x00, 0x0F,
            0x00, 0x00, 0x00, 0x00
        ];
        using BinaryReader reader = new(new MemoryStream(bytes));
        Assert.Throws<InvalidDataException>(() => _v1.Read<PackageVersion>(reader, _strict));
    }
}