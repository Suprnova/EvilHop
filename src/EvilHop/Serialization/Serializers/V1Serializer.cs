using EvilHop.Assets;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization.Validation;
using System.Text;

namespace EvilHop.Serialization.Serializers;

public abstract partial class V1Serializer : IFormatSerializer
{
    protected readonly record struct BlockHandler(
        Type Type,
        Func<Block> Init,
        Func<BinaryReader, uint, Block> Read,
        Action<BinaryWriter, Block> Write
    );

    protected readonly record struct AssetHandler(
        AssetType Type,
        Func<Asset> Init,
        Func<BinaryReader, Asset> Read,
        Action<BinaryWriter, Asset> Write
    );

    protected readonly SerializerOptions _defaultOptions = new();
    protected readonly IFormatValidator _validator;

    /// <summary>
    /// Provides a mapping from unique block identifiers to their corresponding block types.
    /// </summary>
    protected readonly Dictionary<string, Type> _idToBlockType = [];
    /// <summary>
    /// Stores pointers to a block's initialization, reading, and writing methods.
    /// </summary>
    protected readonly Dictionary<Type, BlockHandler> _blockFactory = [];
    /// <summary>
    /// Stores pointers to an asset's initialization, reading, and writing methods.
    /// </summary>
    protected readonly Dictionary<AssetType, AssetHandler> _assetFactory = [];

    protected internal V1Serializer(IFormatValidator validator)
    {
        // blocks without data but with children need a read defined, even if it's just a default constructor
        // without it, it'll use the init handler which will populate the children a second time
        RegisterBlock("HIPA", () => new HIPA());

        RegisterBlock("PACK", InitPackage, (_, _) => new Package());
        {
            RegisterBlock("PVER", InitPackageVersion, (r, _) => ReadPackageVersion(r), WritePackageVersion);
            RegisterBlock("PFLG", InitPackageFlags, (r, _) => ReadPackageFlags(r), WritePackageFlags);
            RegisterBlock("PCNT", () => new PackageCount(), (r, _) => ReadPackageCount(r), WritePackageCount);
            RegisterBlock("PCRT", () => new PackageCreated(), (r, _) => ReadPackageCreated(r), WritePackageCreated);
            RegisterBlock("PMOD", () => new PackageModified(), (r, _) => ReadPackageModified(r), WritePackageModified);
        }

        RegisterBlock("DICT", InitDictionary, (_, _) => new Dictionary());
        {
            RegisterBlock("ATOC", InitAssetTable, (_, _) => new AssetTable());
            RegisterBlock("AINF", () => new AssetInf(), (r, _) => ReadAssetInf(r), WriteAssetInf);
            RegisterBlock("AHDR", InitAssetHeader, (r, _) => ReadAssetHeader(r), WriteAssetHeader);
            RegisterBlock("ADBG", () => new AssetDebug(), (r, _) => ReadAssetDebug(r), WriteAssetDebug);
            RegisterBlock("LTOC", InitLayerTable, (_, _) => new LayerTable());
            RegisterBlock("LINF", () => new LayerInf(), (r, _) => ReadLayerInf(r), WriteLayerInf);
            RegisterBlock("LHDR", InitLayerHeader, (r, _) => ReadLayerHeader(r), WriteLayerHeader);
            RegisterBlock("LDBG", () => new LayerDebug(), (r, _) => ReadLayerDebug(r), WriteLayerDebug);
        }

        RegisterBlock("STRM", InitAssetStream, (_, _) => new AssetStream());
        {
            RegisterBlock("DHDR", () => new StreamHeader(), (r, _) => ReadStreamHeader(r), WriteStreamHeader);
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

        // todo: let's read assets here with a second pass, not the final design, just to start testing asset logic

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
        return _blockFactory.TryGetValue(typeof(TBlock), out var handler)
            ? (handler.Init() as TBlock)!
            : throw new InvalidOperationException($"Block type '{typeof(TBlock).Name}' is not valid for this {typeof(IFormatSerializer).Name}.");
    }

    public Block ReadBlock(BinaryReader reader, SerializerOptions? options = null)
    {
        options ??= _defaultOptions;

        // determine block type
        string blockId = Encoding.ASCII.GetString(reader.ReadBytes(4));
        if (!_idToBlockType.TryGetValue(blockId, out var blockType))
            throw new InvalidDataException($"Unknown block type '{blockId}' at address '{reader.BaseStream.Position - 4}'.");

        uint blockLength = reader.ReadEvilInt();

        long currentOffset = reader.BaseStream.Position;
        // parse block-specific data
        Block block = _blockFactory.TryGetValue(blockType, out var handler)
            // done for scenarios like empty DPAKs, might apply elsewhere
            ? blockLength == 0 ? handler.Init() : handler.Read(reader, blockLength)
            // todo: is this the right exception type?
            : throw new InvalidOperationException($"Block type '{blockType.Name}' is not valid for this {typeof(IFormatSerializer).Name}.");

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

            if (options.Mode == ValidationMode.Strict && issue.Severity >= ValidationSeverity.Warning)
                throw new InvalidDataException($"Strict Validation Failed: '{issue.Message}'");
        }

        return block;
    }

    public T ReadBlock<T>(BinaryReader reader, SerializerOptions? options = null) where T : Block
    {
        long peekOffset = reader.BaseStream.Position;

        string blockId = Encoding.ASCII.GetString(reader.ReadBytes(4));
        if (!_idToBlockType.TryGetValue(blockId, out var blockType))
            throw new InvalidDataException($"Unknown block type '{blockId}' at address '{reader.BaseStream.Position - 4}'.");

        reader.BaseStream.Position = peekOffset;

        if (!typeof(T).IsAssignableFrom(blockType))
            throw new InvalidCastException($"Read block is {blockType.Name}, expected {typeof(T).Name}.");

        return (ReadBlock(reader, options) as T)!;
    }

    public void WriteBlock(BinaryWriter writer, Block block)
    {
        // if not valid for this serializer, just skip it
        if (!_blockFactory.TryGetValue(block.GetType(), out var handler)) return;

        // all IDs are 4 bytes, this trims the null characters
        writer.Write(block.Id.ToEvilBytes()[..^2]);

        // save count to write to later
        long countOffset = writer.BaseStream.Position;
        long beginDataOffset = writer.Seek(4, SeekOrigin.Current);

        // write block-specific data, if any
        handler.Write(writer, block);

        foreach (var child in block.Children)
        {
            WriteBlock(writer, child);
        }

        long endDataOffset = writer.BaseStream.Position;
        long blockSize = writer.BaseStream.Position - beginDataOffset;

        // backtrack to write actual size
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
        readHandler ??= (_, _) => initHandler(); // no readHandler means no special logic, just init
        writeHandler ??= (_, _) => { }; // no writeHandler means no special logic, just no-op

        var handler = new BlockHandler(
            typeof(T),
            initHandler,
            readHandler,
            (w, b) => writeHandler(w, (T)b)
            );

        _blockFactory[typeof(T)] = handler;
        _idToBlockType[id] = typeof(T);
    }

    protected void RegisterAsset<T>(
        AssetType type,
        Func<T> initHandler,
        Func<BinaryReader, T> readHandler,
        Action<BinaryWriter, T> writeHandler
        ) where T : Asset
    {
        var handler = new AssetHandler(
            type,
            initHandler,
            readHandler,
            (w, a) => writeHandler(w, (T)a)
            );

        _assetFactory[type] = handler;
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

// weird edge case for battle's font2.hip
public partial class BattleV1Serializer() : V1Serializer(new BattleV1Validator())
{
    protected override PackageVersion InitPackageVersion() => new(ClientVersion.Default);
}
