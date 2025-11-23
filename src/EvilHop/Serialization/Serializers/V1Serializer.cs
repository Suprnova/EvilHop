using EvilHop.Blocks;
using EvilHop.Primitives;
using EvilHop.Serialization.Validation;
using System.Text;

namespace EvilHop.Serialization.Serializers;

public abstract partial class V1Serializer : IFormatSerializer
{
    protected readonly SerializerOptions _defaultOptions = new();
    protected readonly IFormatValidator _validator = new V1Validator();

    /// <summary>
    /// Stores pointers to a Block's ReadBlockData() method as well as the block's type. Indexed via its 4-byte ID.
    /// </summary>
    protected readonly Dictionary<string, (Func<BinaryReader, uint, Block> Read, Type Type)> _readFactory = [];
    /// <summary>
    /// Stores pointers to a Block's WriteBlockData() method, indexed via its 4-byte ID.
    /// </summary>
    protected readonly Dictionary<Type, Action<BinaryWriter, Block>> _writeFactory = [];

    protected internal V1Serializer()
    {
        Register("HIPA", (r, l) => new HIPA());

        Register("PACK", (r, l) => new Package());
        {
            Register("PVER", (r, l) => ReadPackageVersion(r), WritePackageVersion);
            Register("PFLG", (r, l) => ReadPackageFlags(r), WritePackageFlags);
            Register("PCNT", (r, l) => ReadPackageCount(r), WritePackageCount);
            Register("PCRT", (r, l) => ReadPackageCreated(r), WritePackageCreated);
            Register("PMOD", (r, l) => ReadPackageModified(r), WritePackageModified);
            Register("PLAT", (r, l) => ReadPackagePlatform(r));
        }

        Register("DICT", (r, l) => new Dictionary());
        {
            Register("ATOC", (r, l) => new AssetTable());
            Register("AINF", (r, l) => ReadAssetInf(r), WriteAssetInf);
            Register("AHDR", (r, l) => ReadAssetHeader(r), WriteAssetHeader);
            Register("ADBG", (r, l) => ReadAssetDebug(r), WriteAssetDebug);
            Register("LTOC", (r, l) => new LayerTable());
            Register("LINF", (r, l) => ReadLayerInf(r), WriteLayerInf);
            Register("LHDR", (r, l) => ReadLayerHeader(r), WriteLayerHeader);
            Register("LDBG", (r, l) => ReadLayerDebug(r), WriteLayerDebug);
        }

        Register("STRM", (r, l) => new AssetStream());
        {
            Register("DHDR", (r, l) => ReadStreamHeader(r), WriteStreamHeader);
            Register("DPAK", ReadStreamData, WriteStreamData);
        }
    }

    public HipFile ReadArchive(BinaryReader reader)
    {
        return ReadArchive(reader, null);
    }

    public HipFile ReadArchive(BinaryReader reader, SerializerOptions? options = null)
    {
        options ??= _defaultOptions;
        HIPA hipa = Read<HIPA>(reader, options);
        Package package = Read<Package>(reader, options);
        Dictionary dictionary = Read<Dictionary>(reader, options);
        AssetStream stream = Read<AssetStream>(reader, options);

        HipFile hipFile = new(hipa, package, dictionary, stream);
        if (options.Mode == ValidationMode.None) return hipFile;

        var issues = ValidateArchive(hipFile);

        foreach (var issue in issues)
        {
            options.OnValidationIssue?.Invoke(issue);

            // todo: make Context nullable, we wouldn't need it for HipFile validation since it's cross-referential
            if (options.Mode == ValidationMode.Strict && issue.Severity >= ValidationSeverity.Warning)
                throw new InvalidDataException($"Strict Validation Failed: {issue.Message} on {issue.Context.Id} block.");
        }

        return hipFile;
    }

    public void WriteArchive(BinaryWriter writer, HipFile archive)
    {
        Write(writer, archive.HIPA);
        Write(writer, archive.Package);
        Write(writer, archive.Dictionary);
        Write(writer, archive.AssetStream);
    }

    public Block Read(BinaryReader reader, SerializerOptions? options = null)
    {
        options ??= _defaultOptions;

        // parse header
        string blockId = Encoding.ASCII.GetString(reader.ReadBytes(4));
        uint blockLength = reader.ReadEvilInt();

        // parse block-specific data
        Block block = _readFactory.TryGetValue(blockId, out var readHandler)
            ? readHandler.Read(reader, blockLength)
            : throw new InvalidDataException($"Unknown block type '{blockId}' at address '{reader.BaseStream.Position - 8}'.");

        while (GetBlockLength(block) < blockLength)
        {
            block.Children.Add(Read(reader));
        }

        if (options.Mode == ValidationMode.None) return block;

        var issues = Validate(block);

        foreach (var issue in issues)
        {
            options.OnValidationIssue?.Invoke(issue);

            if (options.Mode == ValidationMode.Strict && issue.Severity == ValidationSeverity.Warning)
                throw new InvalidDataException($"Strict Validation Failed: '{issue.Message}' on {issue.Context.Id} block.");
        }

        return block;
    }

    public T Read<T>(BinaryReader reader, SerializerOptions? options = null) where T : Block
    {
        long peekOffset = reader.BaseStream.Position;
        string blockId = Encoding.ASCII.GetString(reader.ReadBytes(4));

        if (!_readFactory.TryGetValue(blockId, out var readHandler))
            throw new InvalidDataException($"Unknown block type '{blockId}' at address '{reader.BaseStream.Position - 4}'.");

        Type actualType = readHandler.Type;
        reader.BaseStream.Position = peekOffset;

        if (!typeof(T).IsAssignableFrom(actualType))
            throw new InvalidCastException($"Read block is {actualType.Name}, expected {typeof(T).Name}.");

        return (Read(reader, options) as T)!;
    }

    public void Write(BinaryWriter writer, Block block)
    {
        if (!_writeFactory.TryGetValue(block.GetType(), out var writeHandler))
            return;

        writer.Write(block.Id.ToEvilBytes()[..^2]);
        writer.Write(GetBlockLength(block).ToEvilBytes());

        writeHandler(writer, block);

        foreach (var child in block.Children)
        {
            Write(writer, child);
        }
    }

    public IEnumerable<ValidationIssue> Validate(Block block)
    {
        return _validator.Validate(block);
    }

    public IEnumerable<ValidationIssue> ValidateArchive(HipFile hip)
    {
        return _validator.ValidateArchive(hip);
    }

    public uint GetBlockLength(Block block) => GetBlockDataLength(block) + (uint)block.Children.Sum(c => c.HeaderLength + this.GetBlockLength(c));

    protected virtual uint GetBlockDataLength(Block block)
    {
        return block switch
        {
            _ => block.DataLength
        };
    }

    protected void Register<T>(
        string id,
        Func<BinaryReader, uint, T> readHandler,
        Action<BinaryWriter, T>? writeHandler = null
        ) where T : Block
    {
        _readFactory[id] = ((reader, length) => readHandler(reader, length), typeof(T));
        if (writeHandler != null)
        {
            _writeFactory[typeof(T)] = (writer, block) => writeHandler(writer, (T)block);
        }
    }
}

public partial class ScoobyPrototypeSerializer : V1Serializer
{
}

public partial class ScoobySerializer : V1Serializer
{
}
