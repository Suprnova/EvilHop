using EvilHop.Blocks;

namespace EvilHop;

public class HipFile
{
    internal HIPA HIPA { get; set; }
    public Package Package { get; set; }
    public Dictionary Dictionary { get; set; }
    public AssetStream AssetStream { get; set; }

    public HipFile()
    {
        HIPA = new HIPA();
        Package = new Package();
        Dictionary = new Dictionary();
        AssetStream = new AssetStream();
    }

    public HipFile(HIPA hipa, Package package, Dictionary dictionary, AssetStream stream)
    {
        HIPA = hipa;
        Package = package;
        Dictionary = dictionary;
        AssetStream = stream;
    }
}
