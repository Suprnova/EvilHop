using EvilHop.Assets;

namespace EvilHop.Tests.Assets;

public class PayloadAssetTests
{
    private sealed class TestPayloadAsset : PayloadAsset { }

    [Fact]
    public void SaveToFile_ThenLoadFromFile_RoundTripsData()
    {
        byte[] original = [0xDE, 0xAD, 0xBE, 0xEF];
        var asset = new TestPayloadAsset { Data = original };
        string path = Path.GetTempFileName();

        try
        {
            asset.SaveToFile(path);
            var loaded = new TestPayloadAsset();
            loaded.LoadFromFile(path);

            Assert.Equal(original, loaded.Data);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void SaveTo_ThenLoadFrom_StreamRoundTripsData()
    {
        byte[] original = [0xDE, 0xAD, 0xBE, 0xEF];
        var asset = new TestPayloadAsset { Data = original };
        using var stream = new MemoryStream();

        asset.SaveTo(stream);
        stream.Position = 0;
        var loaded = new TestPayloadAsset();
        loaded.LoadFrom(stream);

        Assert.Equal(original, loaded.Data);
    }

    [Fact]
    public void SetUnparsedTail_ThrowsNotSupported()
    {
        // A payload's whole body is the embedded file, so accepting these bytes would silently
        // discard them at commit.
        var asset = new TestPayloadAsset();

        Assert.Throws<NotSupportedException>(() => asset.SetUnparsedTail([0x01, 0x02]));
    }
}
