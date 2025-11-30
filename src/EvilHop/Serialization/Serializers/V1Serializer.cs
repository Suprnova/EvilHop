using EvilHop.Assets;
using EvilHop.Blocks;
using EvilHop.Common;
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
    /// <summary>
    /// Stores pointers to an asset's initialization, reading, and writing methods.
    /// </summary>
    protected readonly Dictionary<AssetType, (Func<Asset> Init, Func<BinaryReader, Asset> Read, Action<BinaryWriter, Asset> Write)> _assetFactory = [];
    // todo: i kinda like how this feels, maybe swap the three dictionaries for blocks to one with this kind of style?
    // or alternatively find a better way to do this, this might be overcomplicating things

    protected internal V1Serializer(IFormatValidator validator)
    {
        RegisterBlock("HIPA", () => new HIPA());

        RegisterBlock("PACK", InitPackage, (r, l) => new Package());
        {
            RegisterBlock("PVER", InitPackageVersion, (r, l) => ReadPackageVersion(r), WritePackageVersion);
            RegisterBlock("PFLG", InitPackageFlags, (r, l) => ReadPackageFlags(r), WritePackageFlags);
            RegisterBlock("PCNT", () => new PackageCount(), (r, l) => ReadPackageCount(r), WritePackageCount);
            RegisterBlock("PCRT", () => new PackageCreated(), (r, l) => ReadPackageCreated(r), WritePackageCreated);
            RegisterBlock("PMOD", () => new PackageModified(), (r, l) => ReadPackageModified(r), WritePackageModified);
        }

        RegisterBlock("DICT", () => new Dictionary());
        {
            RegisterBlock("ATOC", () => new AssetTable());
            RegisterBlock("AINF", () => new AssetInf(), (r, l) => ReadAssetInf(r), WriteAssetInf);
            RegisterBlock("AHDR", InitAssetHeader, (r, l) => ReadAssetHeader(r), WriteAssetHeader);
            RegisterBlock("ADBG", () => new AssetDebug(), (r, l) => ReadAssetDebug(r), WriteAssetDebug);
            RegisterBlock("LTOC", () => new LayerTable());
            RegisterBlock("LINF", () => new LayerInf(), (r, l) => ReadLayerInf(r), WriteLayerInf);
            RegisterBlock("LHDR", InitLayerHeader, (r, l) => ReadLayerHeader(r), WriteLayerHeader);
            RegisterBlock("LDBG", () => new LayerDebug(), (r, l) => ReadLayerDebug(r), WriteLayerDebug);
        }

        RegisterBlock("STRM", () => new AssetStream());
        {
            RegisterBlock("DHDR", () => new StreamHeader(), (r, l) => ReadStreamHeader(r), WriteStreamHeader);
            // todo: could probably benefit with serializer specific values
            RegisterBlock("DPAK", () => new StreamData(), ReadStreamData, WriteStreamData);
        }

        RegisterAsset(AssetType.AnimationList, InitAnimationListAsset, ReadAnimationListAsset, WriteAnimationListAsset);

        _validator = validator;
    }

    public HipFile NewHip()
    {
        HIPA hipa = NewBlock<HIPA>();
        Package package = NewBlock<Package>();
        Dictionary dictionary = NewBlock<Dictionary>();
        AssetStream stream = NewBlock<AssetStream>();

        return new HipFile(hipa, package, dictionary, stream);
    }

    public HipFile ReadHip(BinaryReader reader, SerializerOptions? options = null)
    {
        options ??= _defaultOptions;
        HIPA hipa = ReadBlock<HIPA>(reader, options);
        Package package = ReadBlock<Package>(reader, options);
        Dictionary dictionary = ReadBlock<Dictionary>(reader, options);
        AssetStream stream = ReadBlock<AssetStream>(reader, options);

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
                throw new InvalidDataException($"Strict Validation Failed: {issue.Message}");
        }

        return hipFile;
    }

    public void WriteHip(BinaryWriter writer, HipFile archive)
    {
        WriteBlock(writer, archive.HIPA);
        WriteBlock(writer, archive.Package);
        WriteBlock(writer, archive.Dictionary);
        WriteBlock(writer, archive.AssetStream);
    }

    public TBlock NewBlock<TBlock>() where TBlock : Block
    {
        // todo: these should populate children, they don't right now
        return _initFactory.TryGetValue(typeof(TBlock), out var initHandler)
            ? (initHandler() as TBlock)!
            : throw new InvalidOperationException($"Block type '{typeof(TBlock).Name}' is not valid for this {typeof(IFormatSerializer).Name}.");
    }

    public Block ReadBlock(BinaryReader reader, SerializerOptions? options = null)
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
            block.Children.Add(ReadBlock(reader));
            offset = reader.BaseStream.Position - currentOffset;
        }

        if (options.Mode == ValidationMode.None) return block;

        var issues = ValidateBlock(block);

        foreach (var issue in issues)
        {
            try
            {
                options.OnValidationIssue?.Invoke(issue);
            }
            catch { }
            ;

            if (options.Mode == ValidationMode.Strict && issue.Severity == ValidationSeverity.Warning)
                throw new InvalidDataException($"Strict Validation Failed: '{issue.Message}'");
        }

        return block;
    }

    public T ReadBlock<T>(BinaryReader reader, SerializerOptions? options = null) where T : Block
    {
        long peekOffset = reader.BaseStream.Position;
        string blockId = Encoding.ASCII.GetString(reader.ReadBytes(4));

        if (!_readFactory.TryGetValue(blockId, out var readHandler))
            throw new InvalidDataException($"Unknown block type '{blockId}' at address '{reader.BaseStream.Position - 4}'.");

        Type actualType = readHandler.Type;
        reader.BaseStream.Position = peekOffset;

        if (!typeof(T).IsAssignableFrom(actualType))
            throw new InvalidCastException($"Read block is {actualType.Name}, expected {typeof(T).Name}.");

        return (ReadBlock(reader, options) as T)!;
    }

    public void WriteBlock(BinaryWriter writer, Block block)
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
            WriteBlock(writer, child);
        }

        long endDataOffset = writer.BaseStream.Position;
        long blockSize = writer.BaseStream.Position - beginDataOffset;

        writer.BaseStream.Seek(countOffset, SeekOrigin.Begin);
        writer.Write(Convert.ToUInt32(blockSize).ToEvilBytes());

        writer.Seek((int)endDataOffset, SeekOrigin.Begin);
    }

    public IEnumerable<ValidationIssue> ValidateBlock(Block block)
    {
        return _validator.ValidateBlock(block);
    }

    public uint GetBlockSize(Block block)
    {
        // yea yea it's inefficient, but the only alternatives (i think) are hardcoding lengths for
        // each block, or using reflection (ew!) to try to write every field which is bad anyways
        // cuz it gives individual blocks more power than they should have
        using BinaryWriter writer = new(new MemoryStream());
        WriteBlock(writer, block);
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

    protected void RegisterBlock<T>(
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

    protected void RegisterAsset<T>(
        AssetType type,
        Func<T> initHandler,
        Func<BinaryReader, T> readHandler,
        Action<BinaryWriter, T> writeHandler
        ) where T : Asset
    {
        _assetFactory[type] = (
            () => initHandler(),
            (reader) => readHandler(reader),
            (writer, asset) => writeHandler(writer, (T)asset)
            );
    }

    public TAsset NewAsset<TAsset>() where TAsset : Asset
    {
        throw new NotImplementedException();
    }

    public Asset ReadAsset(BinaryReader reader, SerializerOptions? options = null)
    {
        throw new NotImplementedException();
    }

    public TAsset ReadAsset<TAsset>(BinaryReader reader, SerializerOptions? options = null) where TAsset : Asset
    {
        throw new NotImplementedException();
    }

    public void WriteAsset(BinaryWriter writer, Asset asset)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<ValidationIssue> ValidateAsset(Asset asset)
    {
        throw new NotImplementedException();
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
