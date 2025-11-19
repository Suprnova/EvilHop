using EvilHop.Blocks;

namespace EvilHop;

public class HipFile
{
    internal HIPA HIPA { get; set; }
    public Package Package { get; set; }
    // public Dictionary Dictionary { get; set; }
    // public AssetDataStream AssetDataStream { get; set; }

    public HipFile()
    {
        HIPA = new HIPA();
        Package = new Package();
    }

    public HipFile(HIPA hipa, Package package)
    {
        HIPA = hipa;
        Package = package;
    }
}
