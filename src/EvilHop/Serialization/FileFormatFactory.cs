using EvilHop.Blocks;
using EvilHop.Primitives;
using EvilHop.Serialization.Serializers;
using System.Text;

namespace EvilHop.Serialization;

public enum FileFormatVersion
{
    ScoobyPrototype,
    Scooby,
    BattleV1,
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
            FileFormatVersion.BattleV1 => new BattleV1Serializer(),
            FileFormatVersion.Battle => new BattleSerializer(),
            _ => throw new NotImplementedException()
        };
    }

    public static FileFormatVersion SniffVersion(BinaryReader reader)
    {
        long currentOffset = reader.BaseStream.Position;

        // attempt to read ClientVersion from PVER
        reader.BaseStream.Seek(0x1C, SeekOrigin.Current);
        var clientVersion = (ClientVersion)reader.ReadEvilInt();
        reader.BaseStream.Position = currentOffset;

        if (clientVersion == ClientVersion.N100FPrototype) return FileFormatVersion.ScoobyPrototype;
        else if (clientVersion == ClientVersion.N100FRelease) return FileFormatVersion.Scooby;

        // determine if PLAT block exists
        reader.BaseStream.Seek(0x7E, SeekOrigin.Current);
        var blockId = Encoding.ASCII.GetString(reader.ReadBytes(4));
        reader.BaseStream.Position = currentOffset;

        // weird case with font2.hip in bfbb
        if (blockId != "PLAT") return FileFormatVersion.BattleV1;

        // attempt to read PlatformName (or Language in v3) from PLAT
        reader.BaseStream.Seek(0x8A, SeekOrigin.Current);
        var candidatePlatformName = reader.ReadEvilString();
        reader.BaseStream.Position = currentOffset;

        switch (candidatePlatformName)
        {
            case "GameCube":
            case "Xbox":
            case "PlayStation 2":
                return FileFormatVersion.Battle;
            default:
                break;
        }

        throw new NotImplementedException();
    }
}
