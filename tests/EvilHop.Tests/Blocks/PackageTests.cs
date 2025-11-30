using EvilHop.Blocks;
using EvilHop.Serialization;
using EvilHop.Serialization.Validation;

namespace EvilHop.Tests.Blocks;

public class PackageTests
{
    private static readonly IFormatSerializer _scoobyPrototype = FileFormatFactory.GetSerializer(FileFormatVersion.ScoobyPrototype);
    private static readonly IFormatSerializer _scooby = FileFormatFactory.GetSerializer(FileFormatVersion.Scooby);

    private static readonly IFormatSerializer[] serializers = [_scoobyPrototype, _scooby];
    private static readonly IFormatSerializer[] v1Serializers = [_scoobyPrototype, _scooby];
    private static readonly IFormatSerializer[] v2Serializers = [];
    private static readonly IFormatSerializer[] v3Serializers = [];
    private static readonly IFormatSerializer[] v4Serializers = [];

    private static readonly byte[] validScoobyPackage =
        [
            0x50, 0x41, 0x43, 0x4B, 0x00, 0x00, 0x00, 0x6E, 0x50, 0x56, 0x45, 0x52, 0x00, 0x00, 0x00, 0x0C,
            0x00, 0x00, 0x00, 0x02, 0x00, 0x04, 0x00, 0x06, 0x00, 0x00, 0x00, 0x01, 0x50, 0x46, 0x4C, 0x47,
            0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x2E, 0x50, 0x43, 0x4E, 0x54, 0x00, 0x00, 0x00, 0x14,
            0x00, 0x00, 0x00, 0x34, 0x00, 0x00, 0x00, 0x0D, 0x0D, 0x88, 0xCF, 0x1C, 0x0D, 0x88, 0xCF, 0x1C,
            0x00, 0x00, 0x80, 0xB8, 0x50, 0x43, 0x52, 0x54, 0x00, 0x00, 0x00, 0x1E, 0x43, 0xC4, 0xD2, 0xA0,
            0x57, 0x65, 0x64, 0x20, 0x4A, 0x61, 0x6E, 0x20, 0x31, 0x31, 0x20, 0x30, 0x31, 0x3A, 0x34, 0x30,
            0x3A, 0x34, 0x38, 0x20, 0x32, 0x30, 0x30, 0x36, 0x0A, 0x00, 0x50, 0x4D, 0x4F, 0x44, 0x00, 0x00,
            0x00, 0x04, 0x43, 0xC4, 0xD2, 0xA0,
        ];

    private static readonly byte[] validScoobyPrototypePackageVersion =
        [
            0x50, 0x56, 0x45, 0x52, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x01,
            0x00, 0x00, 0x00, 0x01,
        ];
    private static readonly byte[] validScoobyPackageVersion =
        [
            0x50, 0x56, 0x45, 0x52, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00, 0x00, 0x02, 0x00, 0x04, 0x00, 0x06,
            0x00, 0x00, 0x00, 0x01,
        ];
    private static readonly byte[] validOtherPackageVersion =
        [
            0x50, 0x56, 0x45, 0x52, 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00, 0x00, 0x02, 0x00, 0x0A, 0x00, 0x0F,
            0x00, 0x00, 0x00, 0x01,
        ];

    [Fact]
    public void Package_IFormatSerializer_New_IsValid()
    {
        foreach (var s in serializers)
            Assert.True(s.NewBlock<Package>().IsValid(s, out _));
    }

    [Fact]
    public void Package_V1Serializer_RemoveChild_IsNotValid()
    {
        foreach (var s in v1Serializers)
        {
            for (int i = 0; i < 5; i++)
            {
                Package package = s.NewBlock<Package>();
                package.Children.RemoveAt(i);
                Assert.False(package.IsValid(s, out _));
            }
        }
    }

    [Fact]
    public void Package_OtherSerializer_RemoveChild_IsNotValid()
    {
        foreach (var s in serializers.Where(s => !v1Serializers.Contains(s)))
        {
            for (int i = 0; i < 6; i++)
            {
                Package package = s.NewBlock<Package>();
                package.Children.RemoveAt(i);
                Assert.False(package.IsValid(s, out _));
            }
        }
    }

    [Fact]
    public void Package_Scooby_Read_ValidBytes_IsValid()
    {
        using BinaryReader reader = new(new MemoryStream(validScoobyPackage));
        Assert.True(_scooby.ReadBlock<Package>(reader).IsValid(_scooby, out _));
    }

    [Fact]
    public void Package_Scooby_Read_ValidBytes_CorrectOffset()
    {
        using BinaryReader reader = new(new MemoryStream(validScoobyPackage));
        _ = _scooby.ReadBlock<Package>(reader);
        Assert.Equal(reader.BaseStream.Length, reader.BaseStream.Position);
    }

    [Fact]
    public void Package_Scooby_WriteFromRead_Matches()
    {
        using BinaryReader reader = new(new MemoryStream(validScoobyPackage));
        byte[] writeBytes = new byte[validScoobyPackage.Length];
        using BinaryWriter writer = new(new MemoryStream(writeBytes));

        Package package = _scooby.ReadBlock<Package>(reader);
        _scooby.WriteBlock(writer, package);
        Assert.Equal(validScoobyPackage, writeBytes);
    }

    [Fact]
    public void PackageVersion_IFormatSerializer_New_IsValid()
    {
        foreach (var s in serializers)
            Assert.True(s.NewBlock<PackageVersion>().IsValid(s, out _));
    }

    [Theory]
    [InlineData(0x00000000)]
    [InlineData(0x00000001)]
    [InlineData(0xFFFFFFFF)]
    public void PackageVersion_IFormatSerializer_InvalidSubVersion_IsNotValid(uint subVersion)
    {
        foreach (var s in serializers)
        {
            PackageVersion version = s.NewBlock<PackageVersion>();
            version.SubVersion = subVersion;
            Assert.False(version.IsValid(s, out _));
        }
    }

    [Theory]
    [InlineData(0x00000000)]
    [InlineData(0xFFFFFFFF)]
    public void PackageVersion_IFormatSerializer_InvalidClientVersion_IsNotValid(uint clientVersion)
    {
        foreach (var s in serializers)
        {
            PackageVersion version = s.NewBlock<PackageVersion>();
            version.ClientVersion = (ClientVersion)clientVersion;
            Assert.False(version.IsValid(s, out _));
        }
    }

    [Theory]
    [InlineData(0x00000000)]
    [InlineData(0xFFFFFFFF)]
    public void PackageVersion_IFormatSerializer_InvalidCompatVersion_IsNotValid(uint compatVersion)
    {
        foreach (var s in serializers)
        {
            PackageVersion version = s.NewBlock<PackageVersion>();
            version.CompatVersion = compatVersion;
            Assert.False(version.IsValid(s, out _));
        }
    }

    [Fact]
    public void PackageVersion_ScoobyPrototype_Read_IsValid()
    {
        using BinaryReader reader = new(new MemoryStream(validScoobyPrototypePackageVersion));
        Assert.True(_scoobyPrototype.ReadBlock<PackageVersion>(reader).IsValid(_scoobyPrototype, out _));
    }

    [Fact]
    public void PackageVersion_ScoobyPrototype_Read_CorrectOffset()
    {
        using BinaryReader reader = new(new MemoryStream(validScoobyPrototypePackageVersion));
        _ = _scoobyPrototype.ReadBlock<PackageVersion>(reader);
        Assert.Equal(reader.BaseStream.Length, reader.BaseStream.Position);
    }

    [Theory]
    [InlineData(ClientVersion.N100FRelease)]
    [InlineData(ClientVersion.Default)]
    public void PackageVersion_ScoobyPrototype_IncorrectClientVersion_IsNotValid(ClientVersion clientVersion)
    {
        PackageVersion version = _scoobyPrototype.NewBlock<PackageVersion>();
        version.ClientVersion = clientVersion;
        Assert.False(version.IsValid(_scoobyPrototype, out _));
    }

    [Fact]
    public void PackageVersion_Scooby_Read_IsValid()
    {
        using BinaryReader reader = new(new MemoryStream(validScoobyPackageVersion));
        Assert.True(_scooby.ReadBlock<PackageVersion>(reader).IsValid(_scooby, out _));
    }

    [Fact]
    public void PackageVersion_Scooby_Read_CorrectOffset()
    {
        using BinaryReader reader = new(new MemoryStream(validScoobyPackageVersion));
        _ = _scooby.ReadBlock<PackageVersion>(reader);
        Assert.Equal(reader.BaseStream.Length, reader.BaseStream.Position);
    }

    [Theory]
    [InlineData(ClientVersion.N100FPrototype)]
    [InlineData(ClientVersion.Default)]
    public void PackageVersion_Scooby_IncorrectClientVersion_IsNotValid(ClientVersion clientVersion)
    {
        PackageVersion version = _scooby.NewBlock<PackageVersion>();
        version.ClientVersion = clientVersion;
        Assert.False(version.IsValid(_scooby, out _));
    }

    [Fact]
    public void PackageVersion_OtherSerializer_Read_IsValid()
    {
        using BinaryReader reader = new(new MemoryStream(validOtherPackageVersion));

        foreach (var s in serializers.Where((s) => !v1Serializers.Contains(s)))
        {
            reader.BaseStream.Position = 0;
            Assert.True(s.ReadBlock<PackageVersion>(reader).IsValid(s, out _));
        }
    }

    [Fact]
    public void PackageVersion_OtherSerializer_Read_CorrectOffset()
    {
        using BinaryReader reader = new(new MemoryStream(validOtherPackageVersion));

        foreach (var s in serializers.Where((s) => !v1Serializers.Contains(s)))
        {
            reader.BaseStream.Position = 0;
            _ = s.ReadBlock<PackageVersion>(reader);
            Assert.Equal(reader.BaseStream.Length, reader.BaseStream.Position);
        }
    }

    [Theory]
    [InlineData(ClientVersion.N100FPrototype)]
    [InlineData(ClientVersion.Default)]
    public void PackageVersion_OtherSerializer_IncorrectClientVersion_IsNotValid(ClientVersion clientVersion)
    {
        foreach (var s in serializers.Where((s) => !v1Serializers.Contains(s)))
        {
            PackageVersion version = s.NewBlock<PackageVersion>();
            version.ClientVersion = clientVersion;
            Assert.False(version.IsValid(s, out _));
        }
    }

    [Fact]
    public void PackageFlags_IFormatSerializer_New_IsValid()
    {
        foreach (var s in serializers)
            Assert.True(s.NewBlock<PackageFlags>().IsValid(s, out _));
    }

    [Fact]
    public void PackageCount_IFormatSerializer_New_IsValid()
    {
        foreach (var s in serializers)
            Assert.True(s.NewBlock<PackageCount>().IsValid(s, out _));
    }

    [Fact]
    public void PackageCreated_IFormatSerializer_New_IsValid()
    {
        foreach (var s in serializers)
            Assert.True(s.NewBlock<PackageCreated>().IsValid(s, out _));
    }

    [Fact]
    public void PackageModified_IFormatSerializer_New_IsValid()
    {
        foreach (var s in serializers)
            Assert.True(s.NewBlock<PackageModified>().IsValid(s, out _));
    }

    [Fact]
    public void PackagePlatform_V1Serializer_New_Throws()
    {
        foreach (var s in v1Serializers)
            Assert.Throws<InvalidOperationException>(s.NewBlock<PackagePlatform>);
    }

    // todo: add PackagePlatform_V1Serializer_Read_Throws()
}
