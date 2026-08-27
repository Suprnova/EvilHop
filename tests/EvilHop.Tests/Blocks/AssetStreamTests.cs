using EvilHop.Blocks;
using EvilHop.Serialization;
using EvilHop.Tests.Serialization;

namespace EvilHop.Tests.Blocks;

public class AssetStreamTests
{
    public static IEnumerable<object[]> StreamDataUnmanagedFields =>
    [
        [(Action<StreamData>)((d) => d.PaddingAmount = 0)],
        [(Action<StreamData>)((d) => d.Padding = [])],
    ];

    [Fact]
    public void AssetStream_Tag_IsCorrect()
    {
        var assetStream = new AssetStream();
        Assert.Equal("STRM", assetStream.Tag);
    }

    [Fact]
    public void AssetStream_StreamHeader_IsRequired()
    {
        var assetStream = new AssetStream();
        Assert.Throws<InvalidOperationException>(() => assetStream.Header);
    }

    [Fact]
    public void AssetStream_StreamHeader_Setter_ReturnsSetValue()
    {
        var assetStream = new AssetStream();
        var header = new StreamHeader();

        assetStream.Header = header;
        Assert.Same(header, assetStream.Header);
    }

    [Fact]
    public void AssetStream_StreamData_IsCorrect()
    {
        var assetStream = new AssetStream();
        Assert.Throws<InvalidOperationException>(() => assetStream.Data);
    }

    [Fact]
    public void AssetStream_StreamData_Setter_ReturnsSetValue()
    {
        var assetStream = new AssetStream();
        var data = new StreamData();

        assetStream.Data = data;
        Assert.Same(data, assetStream.Data);
    }

    [Fact]
    public void StreamHeader_Tag_IsCorrect()
    {
        var header = new StreamHeader();
        Assert.Equal("DHDR", header.Tag);
    }

    [Fact]
    public void StreamData_Tag_IsCorrect()
    {
        var data = new StreamData();
        Assert.Equal("DPAK", data.Tag);
    }

    [Fact]
    public void StreamData_Data_IsManaged()
    {
        var data = new StreamData()
        {
            AreBlockFieldsLocked = true
        };

        Assert.Throws<InvalidOperationException>(() => data.Data = []);
    }

    [Theory]
    [MemberData(nameof(StreamDataUnmanagedFields))]
    public void StreamData_UnmanagedFields_AreNotManaged(Action<StreamData> setter)
    {
        var data = new StreamData()
        {
            AreBlockFieldsLocked = true
        };

        var exception = Record.Exception(() => setter(data));

        Assert.Null(exception);
    }

    [Fact]
    public void ReadBlock_Dpak_WithoutPaddingField_ReadsAllContentAsData()
    {
        var profile = N100FSerializer.DefaultProfile with { StreamDataHasPaddingField = false };
        byte[] content = [0xDE, 0xAD, 0xBE, 0xEF, 0x01, 0x02];
        using var reader = BlockBytes.Reader("DPAK", content);

        var data = (StreamData)new TestSerializer(profile).ReadBlockPublic(reader);

        Assert.Null(data.PaddingAmount);
        Assert.Empty(data.Padding);
        Assert.Equal(content, data.Data);
    }

    [Fact]
    public void ReadBlock_Dpak_WithoutPaddingField_NoAssets_StaysEmpty()
    {
        var profile = N100FSerializer.DefaultProfile with { StreamDataHasPaddingField = false };
        using var reader = BlockBytes.Reader("DPAK", []);

        var data = (StreamData)new TestSerializer(profile).ReadBlockPublic(reader);

        Assert.Null(data.PaddingAmount);
        Assert.Empty(data.Padding);
        Assert.Empty(data.Data);
    }
}
