using EvilHop.Blocks;
using EvilHop.Serialization;

namespace EvilHop.Models;

public class Archive
{
    private readonly HipFile hipFile;
    private readonly IFormatSerializer serializer;

    // TODO: this will have a lot more functions and fields, it's designed to be the high level abstraction of HipFile

    public Archive(Stream stream)
    {
        if (!stream.CanSeek) throw new NotSupportedException(
            $"Cannot create {this.GetType().Name} with a stream that doesn't support seeking."
            );

        using BinaryReader reader = new(stream);
        serializer = FileFormatFactory.GetSerializer(reader);
        hipFile = serializer.ReadHip(reader);
    }

    public Archive(HipFile hipFile, IFormatSerializer serializer)
    {
        this.hipFile = hipFile;
        this.serializer = serializer;
    }

    public IEnumerable<AssetView> Assets
    {
        get
        {
            AssetTable assetTable = hipFile.Dictionary.AssetTable;
            StreamData streamData = hipFile.AssetStream.Data;

            uint dataOffset = (uint)(serializer.GetHipSize(hipFile) - streamData.Data.Length);

            foreach (var asset in assetTable.AssetHeaders)
                yield return new AssetView(asset, streamData, asset.Offset - dataOffset);
        }
    }

    // todo: expose Write and Validate methods that call underlying serializer
    // Validate will also validate assets can be populated without slices being invalid
}
