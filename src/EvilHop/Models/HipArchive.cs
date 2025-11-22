using EvilHop.Common;
using EvilHop.Serialization;

namespace EvilHop.Models;

public class ArchiveInfo
{
    public ArchiveGame Game { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}

public class HipArchive
{
    public HipFile HipFile { get; private set; }

    public ArchiveInfo Metadata { get; set; }

    private readonly IFormatSerializer _serializer;

    public static HipArchive Load(Stream inputStream)
    {
        using BinaryReader reader = new(inputStream);

        // todo: accept a FileFormatVersion optional argument
        var fileVersion = FileFormatFactory.SniffVersion(reader);
        var serializer = FileFormatFactory.GetSerializer(fileVersion);

        var hipFile = serializer.ReadArchive(reader);
        // todo: populate ArchiveInfo

        return new HipArchive(hipFile, serializer);
    }

    public static HipArchive Load(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        return Load(stream);
    }

    // todo: this isn't efficient right
    public static HipArchive Load(ReadOnlySpan<byte> data)
    {
        using var stream = new MemoryStream(data.ToArray());
        return Load(stream);
    }

    private HipArchive(HipFile hipFile, IFormatSerializer serializer)
    {
        HipFile = hipFile;
        _serializer = serializer;
    }
}
