using EvilHop.Serialization.Serializers;

namespace EvilHop.Serialization;

public enum FileFormatVersion
{
    Scooby,
    Battle,
    Movie,
    Incredibles,
    ROTU,
    Rat
}

public static class FileFormatFactory
{
    // todo: maybe not in this class, but implement a Peek function to determine a file format automatically
    public static IFormatSerializer GetSerializer(FileFormatVersion version)
    {
        return version switch
        {
            FileFormatVersion.Scooby => new ScoobySerializer(),
            FileFormatVersion.Battle => new BattleSerializer(),
            _ => throw new NotImplementedException()
        };
    }
}
