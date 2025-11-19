namespace EvilHop.Serialization;

public enum FileFormatVersion
{
    Version1 = 1,
    Version2 = 2,
    Version3 = 3,
}

public static class FileFormatFactory
{
    // todo: maybe not in this class, but implement a Peek function to determine a file format automatically
    public static IFormatSerializer GetSerializer(FileFormatVersion version)
    {
        return version switch
        {
            FileFormatVersion.Version1 => new V1Serializer(),
            FileFormatVersion.Version2 => new V2Serializer(),
            FileFormatVersion.Version3 => throw new NotImplementedException(),
            _ => throw new NotImplementedException()
        };
    }
}
