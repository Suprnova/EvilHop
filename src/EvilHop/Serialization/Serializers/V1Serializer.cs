using EvilHop.Blocks;
using EvilHop.Primitives;
using EvilHop.Serialization.Validation;
using System.Text;

namespace EvilHop.Serialization.Serializers;

public abstract partial class V1Serializer : IFormatSerializer
{
    protected readonly SerializerOptions _defaultOptions = new();
    protected readonly IFormatValidator _validator;

    /// <summary>
    /// Stores pointers to a Block's initialization logic. Indexed via its 4-byte ID.
    /// </summary>
    protected readonly Dictionary<Type, Func<Block>> _initFactory = [];
    /// <summary>
    /// Stores pointers to a Block's ReadBlockData() method as well as the block's type. Indexed via its 4-byte ID.
    /// </summary>
    protected readonly Dictionary<string, (Func<BinaryReader, uint, Block> Read, Type Type)> _readFactory = [];
    /// <summary>
    /// Stores pointers to a Block's WriteBlockData() method, indexed via its 4-byte ID.
    /// </summary>
    protected readonly Dictionary<Type, Action<BinaryWriter, Block>> _writeFactory = [];

    protected internal V1Serializer(IFormatValidator validator)
    {
        Register("HIPA", () => new HIPA());

        Register("PACK", InitPackage, (r, l) => new Package());
        {
            Register("PVER", InitPackageVersion, (r, l) => ReadPackageVersion(r), WritePackageVersion);
            Register("PFLG", InitPackageFlags, (r, l) => ReadPackageFlags(r), WritePackageFlags);
            Register("PCNT", () => new PackageCount(), (r, l) => ReadPackageCount(r), WritePackageCount);
            Register("PCRT", () => new PackageCreated(), (r, l) => ReadPackageCreated(r), WritePackageCreated);
            Register("PMOD", () => new PackageModified(), (r, l) => ReadPackageModified(r), WritePackageModified);
        }

        Register("DICT", () => new Dictionary());
        {
            Register("ATOC", () => new AssetTable());
            Register("AINF", () => new AssetInf(), (r, l) => ReadAssetInf(r), WriteAssetInf);
            Register("AHDR", InitAssetHeader, (r, l) => ReadAssetHeader(r), WriteAssetHeader);
            Register("ADBG", () => new AssetDebug(), (r, l) => ReadAssetDebug(r), WriteAssetDebug);
            Register("LTOC", () => new LayerTable());
            Register("LINF", () => new LayerInf(), (r, l) => ReadLayerInf(r), WriteLayerInf);
            Register("LHDR", InitLayerHeader, (r, l) => ReadLayerHeader(r), WriteLayerHeader);
            Register("LDBG", () => new LayerDebug(), (r, l) => ReadLayerDebug(r), WriteLayerDebug);
        }

        Register("STRM", () => new AssetStream());
        {
            Register("DHDR", () => new StreamHeader(), (r, l) => ReadStreamHeader(r), WriteStreamHeader);
            // todo: could probably benefit with serializer specific values
            Register("DPAK", () => new StreamData(), ReadStreamData, WriteStreamData);
        }
        _validator = validator;
    }

    public HipFile NewHip()
    {
        HIPA hipa = New<HIPA>();
        Package package = New<Package>();
        Dictionary dictionary = New<Dictionary>();
        AssetStream stream = New<AssetStream>();

        return new HipFile(hipa, package, dictionary, stream);
    }

    public HipFile ReadHip(BinaryReader reader, SerializerOptions? options = null)
    {
        options ??= _defaultOptions;
        HIPA hipa = Read<HIPA>(reader, options);
        Package package = Read<Package>(reader, options);
        Dictionary dictionary = Read<Dictionary>(reader, options);
        AssetStream stream = Read<AssetStream>(reader, options);

        HipFile hipFile = new(hipa, package, dictionary, stream);
        if (options.Mode == ValidationMode.None) return hipFile;

        var issues = ValidateHip(hipFile);

        foreach (var issue in issues)
        {
            try
            {
                options.OnValidationIssue?.Invoke(issue);
            }
            catch { }
            ;

            // todo: make Context nullable, we wouldn't need it for HipFile validation since it's cross-referential
            if (options.Mode == ValidationMode.Strict && issue.Severity >= ValidationSeverity.Warning)
                throw new InvalidDataException($"Strict Validation Failed: {issue.Message} on {issue.Context.Id} block.");
        }

        return hipFile;
    }

    public void WriteHip(BinaryWriter writer, HipFile archive)
    {
        Write(writer, archive.HIPA);
        Write(writer, archive.Package);
        Write(writer, archive.Dictionary);
        Write(writer, archive.AssetStream);
    }

    public TBlock New<TBlock>() where TBlock : Block
    {
        // todo: these should populate children, they don't right now
        return _initFactory.TryGetValue(typeof(TBlock), out var initHandler)
            ? (initHandler() as TBlock)!
            : throw new InvalidDataException($"Block type '{typeof(TBlock).Name}' is not valid for this {typeof(IFormatSerializer).Name}.");
    }

    public Block Read(BinaryReader reader, SerializerOptions? options = null)
    {
        options ??= _defaultOptions;

        // parse header
        string blockId = Encoding.ASCII.GetString(reader.ReadBytes(4));
        uint blockLength = reader.ReadEvilInt();

        long currentOffset = reader.BaseStream.Position;
        // parse block-specific data
        Block block = _readFactory.TryGetValue(blockId, out var readHandler)
            ? readHandler.Read(reader, blockLength)
            : throw new InvalidDataException($"Unknown block type '{blockId}' at address '{reader.BaseStream.Position - 8}'.");

        long offset = reader.BaseStream.Position - currentOffset;
        while (offset < blockLength)
        {
            block.Children.Add(Read(reader));
            offset = reader.BaseStream.Position - currentOffset;
        }

        if (options.Mode == ValidationMode.None) return block;

        var issues = Validate(block);

        foreach (var issue in issues)
        {
            try
            {
                options.OnValidationIssue?.Invoke(issue);
            }
            catch { }
            ;

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
        // all IDs are 4 bytes, this trims the null characters
        writer.Write(block.Id.ToEvilBytes()[..^2]);

        // save count to write to later
        long countOffset = writer.BaseStream.Position;
        long beginDataOffset = writer.Seek(4, SeekOrigin.Current);

        if (_writeFactory.TryGetValue(block.GetType(), out var writeHandler))
            writeHandler(writer, block);

        foreach (var child in block.Children)
        {
            Write(writer, child);
        }

        long endDataOffset = writer.BaseStream.Position;
        long blockSize = writer.BaseStream.Position - beginDataOffset;

        writer.BaseStream.Seek(countOffset, SeekOrigin.Begin);
        writer.Write(Convert.ToUInt32(blockSize).ToEvilBytes());

        writer.Seek((int)endDataOffset, SeekOrigin.Begin);
    }

    public IEnumerable<ValidationIssue> Validate(Block block)
    {
        return _validator.Validate(block);
    }

    public uint GetBlockSize(Block block)
    {
        // yea yea it's inefficient, but the only alternatives (i think) are hardcoding lengths for
        // each block, or using reflection (ew!) to try to write every field which is bad anyways
        // cuz it gives individual blocks more power than they should have
        using BinaryWriter writer = new(new MemoryStream());
        Write(writer, block);
        return (uint)writer.BaseStream.Length;
    }

    public IEnumerable<ValidationIssue> ValidateHip(HipFile hip)
    {
        return _validator.ValidateArchive(hip);
    }

    public uint GetHipSize(HipFile archive)
    {
        return GetBlockSize(archive.HIPA) + GetBlockSize(archive.Package)
            + GetBlockSize(archive.Dictionary) + GetBlockSize(archive.AssetStream);
    }

    protected void Register<T>(
        string id,
        Func<T> initHandler,
        Func<BinaryReader, uint, T>? readHandler = null,
        Action<BinaryWriter, T>? writeHandler = null
        ) where T : Block
    {
        _initFactory[typeof(T)] = () => initHandler();

        // if readHandler is null, there's no special data to read, so we just use the initHandler
        _readFactory[id] = ((reader, length) => readHandler?.Invoke(reader, length) ?? initHandler(), typeof(T));

        if (writeHandler != null)
        {
            _writeFactory[typeof(T)] = (writer, block) => writeHandler(writer, (T)block);
        }
    }
}

public partial class ScoobyPrototypeSerializer() : V1Serializer(new ScoobyPrototypeValidator())
{
    protected override PackageVersion InitPackageVersion() => new(ClientVersion.N100FPrototype);
}

public partial class ScoobySerializer() : V1Serializer(new ScoobyValidator())
{
    protected override PackageVersion InitPackageVersion() => new(ClientVersion.N100FRelease);
}
