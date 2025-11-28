using EvilHop.Blocks;
using EvilHop.Models;
using EvilHop.Serialization;

namespace EvilHop;

public class HipFile
{
    public HIPA HIPA { get; set; }
    public Package Package { get; set; }
    public Dictionary Dictionary { get; set; }
    public AssetStream AssetStream { get; set; }

    internal HipFile(HIPA hipa, Package package, Dictionary dictionary, AssetStream stream)
    {
        HIPA = hipa;
        Package = package;
        Dictionary = dictionary;
        AssetStream = stream;
    }
}
