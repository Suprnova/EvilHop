using EvilHop.Blocks;
using EvilHop.Serialization;

namespace EvilHop.Models;

public class Archive(HipFile hipFile, IFormatSerializer serializer)
{
    private readonly HipFile hipFile = hipFile;
    private readonly IFormatSerializer serializer = serializer;

    // TODO: this will have a lot more functions and fields, it's designed to be the high level abstraction of HipFile

    public IEnumerable<AssetView> Assets
    {
        get
        {
            var assetTable = hipFile.Dictionary.AssetTable;
            var streamData = hipFile.AssetStream.Data;

            uint streamOffset = hipFile.HIPA.HeaderLength + serializer.GetBlockLength(hipFile.HIPA)
                + hipFile.Package.HeaderLength + serializer.GetBlockLength(hipFile.Package)
                + hipFile.Dictionary.HeaderLength + serializer.GetBlockLength(hipFile.Dictionary)
                + hipFile.AssetStream.HeaderLength + serializer.GetBlockLength(hipFile.AssetStream)
                - (uint)streamData.Data.Length;

            foreach (var assetHeader in assetTable.GetVariableChildren<AssetHeader>())
            {
                yield return new AssetView(assetHeader, streamData, streamOffset);
            }
        }
    }
}
