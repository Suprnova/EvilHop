using EvilHop.Blocks;
using EvilHop.Primitives;
using EvilHop.Serialization;
using EvilHop.Tests.Serialization;

namespace EvilHop.Tests.Blocks;

public class PackageTests
{
    [Fact]
    public void Package_Tag_IsCorrect()
    {
        var pack = new Package();
        Assert.Equal("PACK", pack.Tag);
    }

    [Fact]
    public void Package_Version_IsRequired()
    {
        var pack = new Package();
        Assert.Throws<InvalidOperationException>(() => pack.Version);
    }

    [Fact]
    public void Package_Version_Setter_ReturnsSetValue()
    {
        var pack = new Package();
        var version = new PackageVersion();

        pack.Version = version;
        Assert.Same(version, pack.Version);
    }

    [Fact]
    public void Package_Flags_IsRequired()
    {
        var pack = new Package();
        Assert.Throws<InvalidOperationException>(() => pack.Flags);
    }

    [Fact]
    public void Package_Flags_Setter_ReturnsSetValue()
    {
        var pack = new Package();
        var flags = new PackageFlags();

        pack.Flags = flags;
        Assert.Same(flags, pack.Flags);
    }

    [Fact]
    public void Package_Counts_IsRequired()
    {
        var pack = new Package();
        Assert.Throws<InvalidOperationException>(() => pack.Counts);
    }

    [Fact]
    public void Package_Counts_Setter_ReturnsSetValue()
    {
        var pack = new Package();
        var counts = new PackageCount();

        pack.Counts = counts;
        Assert.Same(counts, pack.Counts);
    }

    [Fact]
    public void Package_Created_IsRequired()
    {
        var pack = new Package();
        Assert.Throws<InvalidOperationException>(() => pack.Created);
    }

    [Fact]
    public void Package_Created_Setter_ReturnsSetValue()
    {
        var pack = new Package();
        var created = new PackageCreated();

        pack.Created = created;
        Assert.Same(created, pack.Created);
    }

    [Fact]
    public void Package_Modified_IsRequired()
    {
        var pack = new Package();
        Assert.Throws<InvalidOperationException>(() => pack.Modified);
    }

    [Fact]
    public void Package_Modified_Setter_ReturnsSetValue()
    {
        var pack = new Package();
        var modified = new PackageModified();

        pack.Modified = modified;
        Assert.Same(modified, pack.Modified);
    }

    [Fact]
    public void Package_Platform_NotSet_ReturnsNull()
    {
        var pack = new Package();
        Assert.Null(pack.Platform);
    }

    [Fact]
    public void Package_Platform_Setter_ReturnsSetValue()
    {
        var pack = new Package();
        var platform = new PackagePlatform();

        pack.Platform = platform;
        Assert.Same(platform, pack.Platform);
    }

    [Fact]
    public void PackageVersion_Tag_IsCorrect()
    {
        var version = new PackageVersion();
        Assert.Equal("PVER", version.Tag);
    }

    [Fact]
    public void PackageFlags_Tag_IsCorrect()
    {
        var flags = new PackageFlags();
        Assert.Equal("PFLG", flags.Tag);
    }

    [Fact]
    public void PackageCount_Tag_IsCorrect()
    {
        var count = new PackageCount();
        Assert.Equal("PCNT", count.Tag);
    }

    [Fact]
    public void PackageCreated_Tag_IsCorrect()
    {
        var created = new PackageCreated();
        Assert.Equal("PCRT", created.Tag);
    }

    [Fact]
    public void PackageModified_Tag_IsCorrect()
    {
        var modified = new PackageModified();
        Assert.Equal("PMOD", modified.Tag);
    }

    [Fact]
    public void PackagePlatform_Tag_IsCorrect()
    {
        var platform = new PackagePlatform();
        Assert.Equal("PLAT", platform.Tag);
    }

    [Fact]
    public void ReadBlock_Plat_PlatformNameRegionLanguage_ReadsBfbbLayout()
    {
        var profile = N100FSerializer.DefaultProfile with { PlatformFieldOrder = PlatformFieldOrder.PlatformNameRegionLanguage };
        var content = BlockBytes.Content(w =>
        {
            w.WriteEvilString("GC");
            w.WriteEvilString("GameCube");
            w.WriteEvilString("NTSC");
            w.WriteEvilString("US Common");
            w.WriteEvilString("Sponge Bob");
        });
        using var reader = BlockBytes.Reader("PLAT", content);

        var platform = (PackagePlatform)new TestSerializer(profile).ReadBlockPublic(reader);

        Assert.Equal("GC", platform.PlatformId);
        Assert.Equal("GameCube", platform.PlatformName);
        Assert.Equal("NTSC", platform.Region);
        Assert.Equal("US Common", platform.Language);
        Assert.Equal("Sponge Bob", platform.GameName);
    }

    [Fact]
    public void ReadBlock_Plat_LanguageRegion_ReadsTssmLayout()
    {
        var profile = N100FSerializer.DefaultProfile with { PlatformFieldOrder = PlatformFieldOrder.LanguageRegion };
        var content = BlockBytes.Content(w =>
        {
            w.WriteEvilString("GC");
            w.WriteEvilString("US");
            w.WriteEvilString("NTSC");
            w.WriteEvilString("Incredibles");
        });
        using var reader = BlockBytes.Reader("PLAT", content);

        var platform = (PackagePlatform)new TestSerializer(profile).ReadBlockPublic(reader);

        Assert.Equal("GC", platform.PlatformId);
        Assert.Null(platform.PlatformName);
        Assert.Equal("US", platform.Language);
        Assert.Equal("NTSC", platform.Region);
        Assert.Equal("Incredibles", platform.GameName);
    }

    [Fact]
    public void ReadBlock_Plat_ShortContent_LeavesTrailingFieldsUnset()
    {
        var profile = N100FSerializer.DefaultProfile with { PlatformFieldOrder = PlatformFieldOrder.PlatformNameRegionLanguage };
        var content = BlockBytes.Content(w =>
        {
            w.WriteEvilString("GC");
            w.WriteEvilString("GameCube");
        });
        using var reader = BlockBytes.Reader("PLAT", content);

        var platform = (PackagePlatform)new TestSerializer(profile).ReadBlockPublic(reader);

        Assert.Equal("GC", platform.PlatformId);
        Assert.Equal("GameCube", platform.PlatformName);
        Assert.Equal("", platform.Region);
        Assert.Equal("", platform.Language);
        Assert.Equal("", platform.GameName);
    }

    [Fact]
    public void Read_PackWithPlat_AttachesPlatformChild()
    {
        var platContent = BlockBytes.Content(w =>
        {
            w.WriteEvilString("GC");
            w.WriteEvilString("GameCube");
            w.WriteEvilString("NTSC");
            w.WriteEvilString("US Common");
            w.WriteEvilString("Sponge Bob");
        });
        var platBytes = BlockBytes.Build("PLAT", platContent);
        using var reader = BlockBytes.Reader("PACK", platBytes);

        var pack = (Package)new TestSerializer().ReadBlockPublic(reader);

        Assert.NotNull(pack.Platform);
    }
}
