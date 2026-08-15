using EvilHop.Blocks;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Globalization;

namespace EvilHop.Tests.Serialization;

public class SerializerV1Tests
{
    private static FileStream OpenMinimalFixture() =>
        File.OpenRead(Path.Combine(AppContext.BaseDirectory, "TestData", "v1", "minimal.hip"));

    [Fact]
    public void CreateBlock_ReturnsStandaloneInstanceOfRequestedType()
    {
        var hipa = new SerializerV1().CreateBlock<HIPA>();

        Assert.Equal("HIPA", hipa.Tag);
        Assert.Null(hipa.Parent);
        Assert.Empty(hipa.Children);
    }

    [Fact]
    public void CreateBlock_InvokesTypesOwnConstructor()
    {
        var created = new SerializerV1().CreateBlock<PackageCreated>();

        Assert.NotEqual(default, created.CreatedDate);
        Assert.Equal(
            created.CreatedDate.ToString("ddd MMM dd HH:mm:ss yyyy", new CultureInfo("en-US")),
            created.CreatedDateString);
    }

    [Fact]
    public void CreateBlock_ReturnsDistinctInstancesEachCall()
    {
        var serializer = new SerializerV1();

        Assert.NotSame(serializer.CreateBlock<HIPA>(), serializer.CreateBlock<HIPA>());
    }

    [Fact]
    public void Read_MinimalFixture_ReturnsFourRootsInOrder()
    {
        var roots = new SerializerV1().Read(OpenMinimalFixture());

        Assert.Equal(4, roots.Count);
        Assert.Equal(["HIPA", "PACK", "DICT", "STRM"], roots.Select(r => r.Tag));
    }

    [Fact]
    public void Read_MinimalFixture_PackHasExpectedChildrenAndNoPlat()
    {
        var roots = new SerializerV1().Read(OpenMinimalFixture());
        var pack = (Package)roots[1];

        Assert.Equal(["PVER", "PFLG", "PCNT", "PCRT", "PMOD"], pack.Children.Select(c => c.Tag));
        Assert.Null(pack.Platform);
    }

    [Fact]
    public void Read_MinimalFixture_PackFieldsMatchFixture()
    {
        var roots = new SerializerV1().Read(OpenMinimalFixture());
        var pack = (Package)roots[1];

        Assert.Equal(2u, pack.Version.SubVersion);
        Assert.Equal(ClientVersion.N100FRelease, pack.Version.ClientVersion);
        Assert.Equal(1u, pack.Version.CompatVersion);
        Assert.Equal(PackFlags.Default, pack.Flags.Flags);
        Assert.Equal(1u, pack.Counts.AssetCount);
        Assert.Equal(1u, pack.Counts.LayerCount);
        Assert.Equal(8u, pack.Counts.MaxAssetSize);
        Assert.Equal(8u, pack.Counts.MaxLayerSize);
        Assert.Equal(0u, pack.Counts.MaxXFormAssetSize);
        Assert.Equal(1029000000, pack.Modified.ModifiedDate.ToUnixTimeSeconds());
    }

    [Fact]
    public void Read_MinimalFixture_PcrtDateRoundTripsWithDateString()
    {
        var roots = new SerializerV1().Read(OpenMinimalFixture());
        var pack = (Package)roots[1];

        Assert.Equal(1028661674, pack.Created.CreatedDate.ToUnixTimeSeconds());
        Assert.Equal("Tue Aug 06 12:21:14 2002\n", pack.Created.CreatedDateString);
    }

    [Fact]
    public void Read_MinimalFixture_DictHasExpectedStructure()
    {
        var roots = new SerializerV1().Read(OpenMinimalFixture());
        var dict = (Dictionary)roots[2];

        Assert.Equal(["ATOC", "LTOC"], dict.Children.Select(c => c.Tag));
        Assert.Single(dict.AssetTable.Headers);
        Assert.Single(dict.LayerTable.Headers);
    }

    [Fact]
    public void Read_MinimalFixture_AhdrHasExactlyOneAdbgChild()
    {
        var roots = new SerializerV1().Read(OpenMinimalFixture());
        var dict = (Dictionary)roots[2];
        var ahdr = dict.AssetTable.Headers.Single();

        Assert.Single(ahdr.Children);
        Assert.IsType<AssetDebug>(ahdr.Children[0]);
    }

    [Fact]
    public void Read_MinimalFixture_LhdrHasExactlyOneLdbgChild()
    {
        var roots = new SerializerV1().Read(OpenMinimalFixture());
        var dict = (Dictionary)roots[2];
        var lhdr = dict.LayerTable.Headers.Single();

        Assert.Single(lhdr.Children);
        Assert.IsType<LayerDebug>(lhdr.Children[0]);
    }

    [Fact]
    public void Read_MinimalFixture_StrmHasExpectedChildren()
    {
        var roots = new SerializerV1().Read(OpenMinimalFixture());
        var strm = (AssetStream)roots[3];

        Assert.Equal(["DHDR", "DPAK"], strm.Children.Select(c => c.Tag));
    }

    [Fact]
    public void Read_MinimalFixture_DpakDataLengthMatchesSizeMinusPaddingAndHeader()
    {
        var roots = new SerializerV1().Read(OpenMinimalFixture());
        var strm = (AssetStream)roots[3];

        Assert.Equal(2u, strm.Data.PaddingAmount);
        Assert.Equal([0x33, 0x33], strm.Data.Padding);
        Assert.Equal([0xDE, 0xAD, 0xBE, 0xEF, 0x01, 0x02, 0x03, 0x04], strm.Data.Data);
    }

    [Fact]
    public void Read_MinimalFixture_BlockTreeParentChildRelationshipsAreConsistent()
    {
        var roots = new SerializerV1().Read(OpenMinimalFixture());

        static void AssertParentage(Block block)
        {
            foreach (var child in block.Children)
            {
                Assert.Same(block, child.Parent);
                AssertParentage(child);
            }
        }

        foreach (var root in roots)
        {
            Assert.Null(root.Parent);
            AssertParentage(root);
        }
    }

    [Fact]
    public void Read_TruncatedStream_ThrowsEndOfStreamException()
    {
        // PCRT's CreatedDateString is cut off mid-string, with no null terminator before EOF.
        byte[] truncated = [.. "PCRT"u8.ToArray(), 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00, 0x00, 0x00, .. "Tue"u8.ToArray()];
        using var stream = new MemoryStream(truncated);

        Assert.Throws<EndOfStreamException>(() => new SerializerV1().Read(stream));
    }

    [Fact]
    public void Read_UnknownTag_ThrowsFormatException()
    {
        byte[] bytes = BlockBytes.Build("XXXX", []);
        using var stream = new MemoryStream(bytes);

        var ex = Assert.Throws<FormatException>(() => new SerializerV1().Read(stream));
        Assert.Contains("XXXX", ex.Message);
    }

    [Fact]
    public void Read_SizeMismatch_ThrowsFormatException()
    {
        // AINF always reads 4 bytes, but declares a content size of 2 - an overshoot.
        var content = BlockBytes.Content(w => w.WriteEvilInt(0));
        byte[] bytes =
        [
            .. "AINF"u8.ToArray(),
            0x00, 0x00, 0x00, 0x02,
            .. content,
        ];
        using var stream = new MemoryStream(bytes);

        Assert.Throws<FormatException>(() => new SerializerV1().Read(stream));
    }

    [Fact]
    public void Read_MalformedStringTerminator_ThrowsInvalidDataException()
    {
        var content = BlockBytes.Content(w =>
        {
            w.WriteEvilInt(0);
            w.Write("ab"u8.ToArray());
            w.Write((byte)0x00);
            w.Write((byte)0x01); // malformed second null byte
        });
        byte[] bytes = BlockBytes.Build("PCRT", content);
        using var stream = new MemoryStream(bytes);

        Assert.Throws<InvalidDataException>(() => new SerializerV1().Read(stream));
    }
}
