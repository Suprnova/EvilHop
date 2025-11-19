namespace EvilHop.Serialization;

public static class FileFormatFactory
{
    public static IFormatSerializer GetSerializer(int version)
    {
        return version switch
        {
            1 => new V1Serializer(),
            2 => new V2Serializer(),
            _ => throw new NotImplementedException()
        };
    }
}
