using EvilHop.Blocks;
using EvilHop.Extensions;
using System.Text;

namespace EvilHop.Serialization;

public partial class V1Serializer : IFormatSerializer
{
    public virtual HipFile ReadArchive(BinaryReader reader)
    {
        HIPA hipa = Read(reader) as HIPA ?? throw new InvalidDataException();
        Package package = Read(reader) as Package ?? throw new InvalidDataException();
        return new HipFile(hipa, package);
    }

    public virtual void WriteArchive(BinaryWriter writer, HipFile archive)
    {
        Write(writer, archive.HIPA);
        Write(writer, archive.Package);
    }

    public virtual Block Read(BinaryReader reader)
    {
        // parse header
        string blockId = Encoding.ASCII.GetString(reader.ReadBytes(4));
        uint blockLength = reader.ReadEvilInt();

        // parse block-specific data
        Block block = ReadBlockData(reader, blockId);

        // parse children
        // todo: validate
        while (block.Length < blockLength)
        {
            block.Children.Add(Read(reader));
        }

        return block;
    }

    public T Read<T>(BinaryReader reader) where T : Block
    {
        long peekOffset = reader.BaseStream.Position;
        Type type = BlockFactory.GetBlockType(Encoding.ASCII.GetString(reader.ReadBytes(4)));
        reader.BaseStream.Position = peekOffset;
        
        if (type != typeof(T)) throw new InvalidCastException($"Read block is {type.Name}, expected {typeof(T).Name}.");

        return (Read(reader) as T)!;
    }

    protected virtual Block ReadBlockData(BinaryReader reader, string id)
    {
        Type blockType = BlockFactory.GetBlockType(id);
        return blockType switch
        {
            Type t when t == typeof(HIPA) => new HIPA(),
            Type t when t == typeof(Package) => new Package(),
            Type t when t == typeof(PackageVersion) => ReadPackageVersion(reader),
            Type t when t == typeof(PackageFlags) => ReadPackageFlags(reader),
            Type t when t == typeof(PackageCount) => ReadPackageCount(reader),
            Type t when t == typeof(PackageCreated) => ReadPackageCreated(reader),
            Type t when t == typeof(PackageModified) => ReadPackageModified(reader),
            Type t when t == typeof(PackagePlatform) => ReadPackagePlatform(reader),
            _ => throw new NotImplementedException()
        };
    }

    public virtual void Write(BinaryWriter writer, Block block)
    {
        if (!IsValidType(block)) return;

        writer.Write(block.Id.ToEvilBytes());
        writer.Write(block.Length.ToEvilBytes());

        WriteBlockData(writer, block);

        foreach (var child in block.Children)
        {
            Write(writer, child);
        }
    }

    protected virtual void WriteBlockData(BinaryWriter writer, Block block)
    {
        switch (block)
        {
            case PackageVersion version:
                WritePackageVersion(writer, version); break;
            case PackageFlags flags:
                WritePackageFlags(writer, flags); break;
            case PackageCount count:
                WritePackageCount(writer, count); break;
            case PackageCreated created:
                WritePackageCreated(writer, created); break;
            case PackageModified modified:
                WritePackageModified(writer, modified); break;
            case PackagePlatform platform:
                WritePackagePlatform(writer, platform); break;
            case HIPA:
            case Package:
                break;
            default:
                throw new NotImplementedException();
        }
    }

    protected virtual bool IsValidType(Block block)
    {
        return block switch
        {
            PackagePlatform => false,
            _ => true,
        };
    }
}

public partial class V2Serializer : V1Serializer
{
}
