using EvilHop.Blocks;

namespace EvilHop;

public class HipFile
{
    protected HIPA HIPA {  get; set; }
    public Package Package { get; set; }
    // public Dictionary Dictionary { get; set; }
    // public AssetDataStream AssetDataStream { get; set; }

    public HipFile()
    {
        HIPA = new HIPA();
        Package = new Package();
    }

    public HipFile(BinaryReader reader)
    {
        HIPA = new HIPA(reader);
        Package = new Package(reader);
    }
}
