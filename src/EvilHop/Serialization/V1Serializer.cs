using EvilHop.Blocks;
using EvilHop.Extensions;
using EvilHop.Serialization.Validation;
using System.Text;

namespace EvilHop.Serialization;

public partial class V1Serializer() : IFormatSerializer
{
    protected readonly IFormatValidator _validator = new V1Validator();
    protected readonly SerializerOptions _defaultOptions = new();

    public virtual HipFile ReadArchive(BinaryReader reader, SerializerOptions? options = null)
    {
        options ??= _defaultOptions;
        HIPA hipa = Read(reader, options) as HIPA ?? throw new InvalidDataException();
        Package package = Read(reader, options) as Package ?? throw new InvalidDataException();

        HipFile hipFile = new(hipa, package);
        if (options.Mode == ValidationMode.None) return hipFile;

        var issues = ValidateArchive(hipFile);

        foreach (var issue in issues)
        {
            options.OnValidationIssue?.Invoke(issue);

            // todo: make Context nullable, we wouldn't need it for HipFile validation since it's cross-referential
            if (options.Mode == ValidationMode.Strict && issue.Severity == ValidationSeverity.Error)
                throw new InvalidDataException($"Strict Validation Failed: {issue.Message} on {issue.Context.Id} block.");
        }

        return hipFile;
    }

    public virtual void WriteArchive(BinaryWriter writer, HipFile archive)
    {
        Write(writer, archive.HIPA);
        Write(writer, archive.Package);
    }

    public virtual Block Read(BinaryReader reader, SerializerOptions? options = null)
    {
        options ??= _defaultOptions;

        // parse header
        string blockId = Encoding.ASCII.GetString(reader.ReadBytes(4));
        uint blockLength = reader.ReadEvilInt();

        // parse block-specific data
        Block block = ReadBlockData(reader, blockId);

        while (block.Length < blockLength)
        {
            block.Children.Add(Read(reader));
        }

        if (options.Mode == ValidationMode.None) return block;

        var issues = Validate(block);

        foreach (var issue in issues)
        {
            options.OnValidationIssue?.Invoke(issue);

            if (options.Mode == ValidationMode.Strict && issue.Severity == ValidationSeverity.Error)
                throw new InvalidDataException($"Strict Validation Failed: {issue.Message} on {issue.Context.Id} block.");
        }

        return block;
    }

    public T Read<T>(BinaryReader reader, SerializerOptions? options = null) where T : Block
    {
        long peekOffset = reader.BaseStream.Position;
        Type type = BlockFactory.GetBlockType(Encoding.ASCII.GetString(reader.ReadBytes(4)));
        reader.BaseStream.Position = peekOffset;
        
        if (type != typeof(T)) throw new InvalidCastException($"Read block is {type.Name}, expected {typeof(T).Name}.");

        return (Read(reader, options) as T)!;
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

    public virtual IEnumerable<ValidationIssue> Validate(Block block)
    {
        return _validator.Validate(block);
    }

    public virtual IEnumerable<ValidationIssue> ValidateArchive(HipFile hip)
    {
        yield break;
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
    //private readonly V2Validator _validator = new V2Validator();
    public override IEnumerable<ValidationIssue> Validate(Block block)
    {
        yield break;
    }
}
