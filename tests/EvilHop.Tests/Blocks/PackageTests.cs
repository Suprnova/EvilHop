using EvilHop.Blocks;

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
}
