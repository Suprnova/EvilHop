using EvilHop.Blocks;
using EvilHop.Exceptions;
using static EvilHop.Blocks.Package;
using static EvilHop.Blocks.Package.PackageVersion;

namespace EvilHop.Tests.Blocks;

public class PackageTests
{
    [Fact]
    public void Package_EmptyConstructor_InitializesCorrectly()
    {
        Package package = new();
        Assert.NotEmpty(package.Children);
        Assert.NotEqual(0U, package.Length);
    }

    // todo: write this test
    [Fact]
    public void Package_ExplicitConstructor_InitializesCorrectly()
    {
        Assert.True(true);
    }

    [Theory]
    [InlineData(
        new byte[]
        {
            0x50, 0x41, 0x43, 0x4B, 0x00, 0x00, 0x00, 0x90, 0x50, 0x56, 0x45, 0x52, 0x00, 0x00, 0x00, 0x0C,
            0x00, 0x00, 0x00, 0x02, 0x00, 0x0A, 0x00, 0x0F, 0x00, 0x00, 0x00, 0x01, 0x50, 0x46, 0x4C, 0x47,
            0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x2E, 0x50, 0x43, 0x4E, 0x54, 0x00, 0x00, 0x00, 0x14,
            0x00, 0x00, 0x00, 0x34, 0x00, 0x00, 0x00, 0x0D, 0x0D, 0x88, 0xCF, 0x1C, 0x0D, 0x88, 0xCF, 0x1C,
            0x00, 0x00, 0x80, 0xB8, 0x50, 0x43, 0x52, 0x54, 0x00, 0x00, 0x00, 0x1E, 0x43, 0xC4, 0xD2, 0xA0,
            0x57, 0x65, 0x64, 0x20, 0x4A, 0x61, 0x6E, 0x20, 0x31, 0x31, 0x20, 0x30, 0x31, 0x3A, 0x34, 0x30,
            0x3A, 0x34, 0x38, 0x20, 0x32, 0x30, 0x30, 0x36, 0x00, 0x00, 0x50, 0x4D, 0x4F, 0x44, 0x00, 0x00,
            0x00, 0x04, 0x43, 0xC4, 0xD2, 0xA0,
        },
        5, 144
    )]
    public void Package_BinaryReaderConstructor_ValidBytes_InitializesCorrectly(byte[] bytes, int expectedCount, uint expectedLength)
    {
        using BinaryReader reader = new(new MemoryStream(bytes));
        Package package = new(reader);
        Assert.Equal(expectedCount, package.Children.Count);
        Assert.Equal(expectedLength, package.Length);
    }

    [Fact]
    public void PackageVersion_EmptyConstructor_InitializesCorrectly()
    {
        PackageVersion packageVersion = new();
        Assert.Empty(packageVersion.Children);
        Assert.Equal(12U, packageVersion.Length);
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
    public void PackageVersion_BinaryReaderConstructor_ValidBytes_InitializesCorrectly(byte[] bytes, SubVersion subVersion, ClientVersion clientVersion, CompatVersion compatVersion)
    {
        using BinaryReader reader = new(new MemoryStream(bytes));
        PackageVersion packageVersion = new(reader);
        Assert.Empty(packageVersion.Children);
        Assert.Equal(12U, packageVersion.Length);
        Assert.Equal(subVersion, packageVersion.SubVer);
        Assert.Equal(clientVersion, packageVersion.ClientVer);
        Assert.Equal(compatVersion, packageVersion.CompatVer);
    }

    [Fact]
    public void PackageVersion_BinaryReaderConstructor_MalformedMagicNumber_Throws()
    {
        byte[] bytes = [0x50, 0x41, 0x43, 0x4B, 0x00, 0x00, 0x00, 0x00];
        using BinaryReader reader = new(new MemoryStream(bytes));
        Assert.Throws<ArgumentException>(() => new PackageVersion(reader));
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
    public void PackageVersion_BinaryReaderConstructor_MalformedLength_Throws(byte[] bytes)
    {
        using BinaryReader reader = new(new MemoryStream(bytes));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PackageVersion(reader));
    }

    [Fact]
    public void PackageVersion_BinaryReaderConstructor_MalformedSubVersion_Throws()
    {
        byte[] bytes = [0x50, 0x56, 0x45, 0x52, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00, 0x00, 0x00];
        using BinaryReader reader = new(new MemoryStream(bytes));
        Assert.Throws<UnrecognizedEnumValueException<SubVersion>>(() => new PackageVersion(reader));
    }

    [Fact]
    public void PackageVersion_BinaryReaderConstructor_MalformedClientVersion_Throws()
    {
        byte[] bytes = [0x50, 0x56, 0x45, 0x52, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00];
        using BinaryReader reader = new(new MemoryStream(bytes));
        Assert.Throws<UnrecognizedEnumValueException<ClientVersion>>(() => new PackageVersion(reader));
    }

    [Fact]
    public void PackageVersion_BinaryReaderConstructor_MalformedCompatVersion_Throws()
    {
        byte[] bytes = [
            0x50, 0x56, 0x45, 0x52, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00, 0x00, 0x02, 0x00, 0x0A, 0x00, 0x0F,
            0x00, 0x00, 0x00, 0x00
        ];
        using BinaryReader reader = new(new MemoryStream(bytes));
        Assert.Throws<UnrecognizedEnumValueException<CompatVersion>>(() => new PackageVersion(reader));
    }
}