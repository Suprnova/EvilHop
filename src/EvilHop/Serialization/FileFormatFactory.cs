using EvilHop.Blocks;
using EvilHop.Primitives;
using EvilHop.Serialization.Serializers;

namespace EvilHop.Serialization;

public enum FileFormatVersion
{
    ScoobyPrototype,
    Scooby,
    Battle,
    Movie,
    Incredibles,
    ROTU,
    Rat
}

public static class FileFormatFactory
{
    public static IFormatSerializer GetSerializer(BinaryReader reader) => GetSerializer(SniffVersion(reader));

    public static IFormatSerializer GetSerializer(FileFormatVersion version)
    {
        return version switch
        {
            FileFormatVersion.ScoobyPrototype => new ScoobyPrototypeSerializer(),
            FileFormatVersion.Scooby => new ScoobySerializer(),
            FileFormatVersion.Battle => new BattleSerializer(),
            _ => throw new NotImplementedException()
        };
    }

    public static FileFormatVersion SniffVersion(BinaryReader reader)
    {
        long currentOffset = reader.BaseStream.Position;

        reader.BaseStream.Seek(28, SeekOrigin.Current);
        var clientVersion = (ClientVersion)reader.ReadEvilInt();
        reader.BaseStream.Position = currentOffset;

        if (clientVersion == ClientVersion.N100FPrototype) return FileFormatVersion.ScoobyPrototype;
        else if (clientVersion == ClientVersion.N100FRelease) return FileFormatVersion.Scooby;

        throw new NotImplementedException();
    }
}
