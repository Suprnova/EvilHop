using EvilHop.Blocks;
using EvilHop.Serialization;
using System.Globalization;

namespace EvilHop.Tests.Serialization;

/// <summary>
/// The contract every <see cref="Serializer"/> owes, independent of game: the shared block envelope,
/// exercised through <see cref="OpenMinimalFixture"/>, which contains no <c>PLAT</c> block and uses
/// standard <c>DPAK</c> padding. A concrete game's own quirks are covered separately, at the block
/// level, parameterized over its profile.
/// </summary>
public abstract class SerializerContractTests
{
    protected abstract Serializer CreateSerializer();

    protected virtual FileStream OpenMinimalFixture() =>
        File.OpenRead(Path.Combine(AppContext.BaseDirectory, "TestData", "n100f", "minimal.hip"));

    /// <summary>
    /// Tags whose base registration this serializer is declared to replace with its own handler.
    /// Empty for every serializer today.
    /// </summary>
    protected virtual IReadOnlySet<string> DeclaredHandlerReplacements => new HashSet<string>();

    [Fact]
    public void CreateBlock_ReturnsStandaloneInstanceOfRequestedType()
    {
        var hipa = CreateSerializer().CreateBlock<HIPA>();

        Assert.Equal("HIPA", hipa.Tag);
        Assert.Null(hipa.Parent);
        Assert.Empty(hipa.Children);
    }

    [Fact]
    public void CreateBlock_InvokesTypesOwnConstructor()
    {
        var created = CreateSerializer().CreateBlock<PackageCreated>();

        Assert.NotEqual(default, created.CreatedDate);
        Assert.Equal(
            created.CreatedDate.ToString("ddd MMM dd HH:mm:ss yyyy", new CultureInfo("en-US")),
            created.CreatedDateString);
    }

    [Fact]
    public void CreateBlock_ReturnsDistinctInstancesEachCall()
    {
        var serializer = CreateSerializer();

        Assert.NotSame(serializer.CreateBlock<HIPA>(), serializer.CreateBlock<HIPA>());
    }

    [Fact]
    public void Read_MinimalFixture_ReturnsFourRootsInOrder()
    {
        var roots = CreateSerializer().Read(OpenMinimalFixture());

        Assert.Equal(4, roots.Count);
        Assert.Equal(["HIPA", "PACK", "DICT", "STRM"], roots.Select(r => r.Tag));
    }

    [Fact]
    public void Read_MinimalFixture_PackHasExpectedChildrenAndNoPlat()
    {
        var roots = CreateSerializer().Read(OpenMinimalFixture());
        var pack = (Package)roots[1];

        Assert.Equal(["PVER", "PFLG", "PCNT", "PCRT", "PMOD"], pack.Children.Select(c => c.Tag));
        Assert.Null(pack.Platform);
    }

    [Fact]
    public void Read_MinimalFixture_PackFieldsMatchFixture()
    {
        var roots = CreateSerializer().Read(OpenMinimalFixture());
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
        var roots = CreateSerializer().Read(OpenMinimalFixture());
        var pack = (Package)roots[1];

        Assert.Equal(1028661674, pack.Created.CreatedDate.ToUnixTimeSeconds());
        Assert.Equal("Tue Aug 06 12:21:14 2002\n", pack.Created.CreatedDateString);
    }

    [Fact]
    public void Read_MinimalFixture_DictHasExpectedStructure()
    {
        var roots = CreateSerializer().Read(OpenMinimalFixture());
        var dict = (Dictionary)roots[2];

        Assert.Equal(["ATOC", "LTOC"], dict.Children.Select(c => c.Tag));
        Assert.Single(dict.AssetTable.Headers);
        Assert.Single(dict.LayerTable.Headers);
    }

    [Fact]
    public void Read_MinimalFixture_AhdrHasExactlyOneAdbgChild()
    {
        var roots = CreateSerializer().Read(OpenMinimalFixture());
        var dict = (Dictionary)roots[2];
        var ahdr = dict.AssetTable.Headers.Single();

        Assert.Single(ahdr.Children);
        Assert.IsType<AssetDebug>(ahdr.Children[0]);
    }

    [Fact]
    public void Read_MinimalFixture_LhdrHasExactlyOneLdbgChild()
    {
        var roots = CreateSerializer().Read(OpenMinimalFixture());
        var dict = (Dictionary)roots[2];
        var lhdr = dict.LayerTable.Headers.Single();

        Assert.Single(lhdr.Children);
        Assert.IsType<LayerDebug>(lhdr.Children[0]);
    }

    [Fact]
    public void Read_MinimalFixture_StrmHasExpectedChildren()
    {
        var roots = CreateSerializer().Read(OpenMinimalFixture());
        var strm = (AssetStream)roots[3];

        Assert.Equal(["DHDR", "DPAK"], strm.Children.Select(c => c.Tag));
    }

    [Fact]
    public void Read_MinimalFixture_DpakDataLengthMatchesSizeMinusPaddingAndHeader()
    {
        // Every game serializer's default profile has the padding field. If a future game's default
        // ever sets it false, this test failing is the correct alarm.
        var roots = CreateSerializer().Read(OpenMinimalFixture());
        var strm = (AssetStream)roots[3];

        Assert.Equal(2u, strm.Data.PaddingAmount);
        Assert.Equal([0x33, 0x33], strm.Data.Padding);
        Assert.Equal([0xDE, 0xAD, 0xBE, 0xEF, 0x01, 0x02, 0x03, 0x04], strm.Data.Data);
    }

    [Fact]
    public void Read_MinimalFixture_BlockTreeParentChildRelationshipsAreConsistent()
    {
        var roots = CreateSerializer().Read(OpenMinimalFixture());

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
    public void Read_ThenWrite_MinimalFixture_ProducesIdenticalBytes()
    {
        using var fixture = OpenMinimalFixture();
        using var fixtureCopy = new MemoryStream();
        fixture.CopyTo(fixtureCopy);
        byte[] originalBytes = fixtureCopy.ToArray();

        var roots = CreateSerializer().Read(new MemoryStream(originalBytes));

        using var rewritten = new MemoryStream();
        CreateSerializer().Write(rewritten, roots);

        Assert.Equal(originalBytes, rewritten.ToArray());
    }

    [Fact]
    public void Read_TruncatedStream_ThrowsEndOfStreamException()
    {
        // PCRT's CreatedDateString is cut off mid-string, with no null terminator before EOF.
        byte[] truncated = [.. "PCRT"u8.ToArray(), 0x00, 0x00, 0x00, 0x0C, 0x00, 0x00, 0x00, 0x00, .. "Tue"u8.ToArray()];
        using var stream = new MemoryStream(truncated);

        Assert.Throws<EndOfStreamException>(() => CreateSerializer().Read(stream));
    }

    [Fact]
    public void Read_UnknownTag_ThrowsFormatException()
    {
        byte[] bytes = BlockBytes.Build("XXXX", []);
        using var stream = new MemoryStream(bytes);

        var ex = Assert.Throws<FormatException>(() => CreateSerializer().Read(stream));
        Assert.Contains("XXXX", ex.Message);
    }

    [Fact]
    public void Read_SizeMismatch_ThrowsFormatException()
    {
        // AINF always reads 4 bytes, but declares a content size of 2 - an overshoot.
        var content = BlockBytes.Content(w => w.Write(0));
        byte[] bytes =
        [
            .. "AINF"u8.ToArray(),
            0x00, 0x00, 0x00, 0x02,
            .. content,
        ];
        using var stream = new MemoryStream(bytes);

        Assert.Throws<FormatException>(() => CreateSerializer().Read(stream));
    }

    [Fact]
    public void Read_MalformedStringTerminator_ThrowsInvalidDataException()
    {
        var content = BlockBytes.Content(w =>
        {
            w.Write(0);
            w.Write("ab"u8.ToArray());
            w.Write((byte)0x00);
            w.Write((byte)0x01); // malformed second null byte
        });
        byte[] bytes = BlockBytes.Build("PCRT", content);
        using var stream = new MemoryStream(bytes);

        Assert.Throws<InvalidDataException>(() => CreateSerializer().Read(stream));
    }

    [Fact]
    public void HandlerRegistrations_MatchTheBaseClass_ExceptWhereDeclared()
    {
        var serializer = CreateSerializer();
        var baseline = new TestSerializer(serializer.Profile).HandlerFingerprint();

        var replaced = serializer.HandlerFingerprint()
            .Where(kv => baseline[kv.Key] != kv.Value)
            .Select(kv => kv.Key)
            .ToHashSet();

        Assert.Equal(DeclaredHandlerReplacements, replaced);
    }
}
