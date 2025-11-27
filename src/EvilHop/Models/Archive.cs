using EvilHop.Blocks;
using EvilHop.Serialization;

namespace EvilHop.Models;

public class Archive(HipFile hipFile, IFormatSerializer serializer)
{
    private readonly HipFile hipFile = hipFile;
    private readonly IFormatSerializer serializer = serializer;

    // TODO: this will have a lot more functions and fields, it's designed to be the high level abstraction of HipFile
    // todo 2: tbh don't like having this model, can't validate the assets on HipFile since you have to initialize this first. bad?
    // also it's readonly LOL. pretty bad but it's a nice middle-ground i guess

    public IEnumerable<AssetView> Assets
    {
        get
        {
            throw new NotImplementedException();
        }
    }
}
