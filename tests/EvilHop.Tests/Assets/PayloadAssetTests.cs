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
}
