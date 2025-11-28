using EvilHop.Blocks;
using EvilHop.Serialization;

namespace EvilHop.Models;

public class Archive(HipFile hipFile, IFormatSerializer serializer)
{
    private readonly HipFile hipFile = hipFile;
    private readonly IFormatSerializer serializer = serializer;

    // TODO: this will have a lot more functions and fields, it's designed to be the high level abstraction of HipFile

    // todo: constructor for just a Stream object, ensure CanSeek()

    public IEnumerable<AssetView> Assets
    {
        get
        {
            AssetTable assetTable = hipFile.Dictionary.AssetTable;
            StreamData streamData = hipFile.AssetStream.Data;

            uint dataOffset = (uint)(serializer.GetArchiveSize(hipFile) - streamData.Data.Length);

            foreach (var asset in assetTable.AssetHeaders)
                yield return new AssetView(asset, streamData, asset.Offset - dataOffset);
        }
    }

    // todo: expose Write and Validate methods that call underlying serializer
    // Validate will also validate assets can be populated without slices being invalid
}
