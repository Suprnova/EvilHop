using EvilHop.Blocks;
using EvilHop.Primitives;
using System.Reflection;
using System.Text;

namespace EvilHop.Serialization;

/// <summary>
/// Reads and writes HIP archives. Every game agrees on the same block envelope - tag, size, fields,
/// then children until the declared size is consumed - and a <see cref="FormatProfile"/> carries the
/// handful of quirks that envelope alone doesn't resolve.
/// </summary>
public abstract partial class Serializer
{
    private readonly record struct BlockHandler(
        Func<Block> Create,
        Action<BinaryReader, Block, uint>? ReadFields,
        Action<BinaryWriter, Block>? WriteFields
    );

    private readonly Dictionary<string, BlockHandler> _handlers = [];

    /// <summary>The format quirks and game identity this serializer reads with.</summary>
    public FormatProfile Profile { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="Serializer"/>, registering all twenty base block
    /// types.
    /// </summary>
    /// <param name="profile">The format quirks and game identity this serializer reads with.</param>
    protected Serializer(FormatProfile profile)
    {
        Profile = profile;

        RegisterBlock<HIPA>();

        RegisterBlock<Package>();
        RegisterBlock<PackageVersion>(ReadPackageVersion);
        RegisterBlock<PackageFlags>(ReadPackageFlags);
        RegisterBlock<PackageCount>(ReadPackageCount);
        RegisterBlock<PackageCreated>(ReadPackageCreated);
        RegisterBlock<PackageModified>(ReadPackageModified);
        RegisterBlock<PackagePlatform>(ReadPackagePlatform);

        RegisterBlock<Dictionary>();
        RegisterBlock<AssetTable>();
        RegisterBlock<AssetInf>(ReadAssetInf);
        RegisterBlock<AssetHeader>(ReadAssetHeader);
        RegisterBlock<AssetDebug>(ReadAssetDebug);
        RegisterBlock<LayerTable>();
        RegisterBlock<LayerInf>(ReadLayerInf);
        RegisterBlock<LayerHeader>(ReadLayerHeader);
        RegisterBlock<LayerDebug>(ReadLayerDebug);

        RegisterBlock<AssetStream>();
        RegisterBlock<StreamHeader>(ReadStreamHeader);
        RegisterBlock<StreamData>(ReadStreamData);
    }

    /// <summary>
    /// Registers a block type <typeparamref name="T"/>'s creation and field read/write handlers,
    /// keyed by the block's own <see cref="Block.Tag"/>. Re-registering an already-registered tag
    /// overwrites its handler.
    /// </summary>
    /// <typeparam name="T">The <see cref="Block"/> type to register.</typeparam>
    /// <param name="readFields">Reads the block's type-specific fields, if any.</param>
    /// <param name="writeFields">Writes the block's type-specific fields, if any.</param>
    protected void RegisterBlock<T>(
        Action<BinaryReader, T, uint>? readFields = null,
        Action<BinaryWriter, T>? writeFields = null) where T : Block
    {
        static Block Create() => (T)Activator.CreateInstance(typeof(T), nonPublic: true)!;

        _handlers[Create().Tag] = new BlockHandler(
            Create,
            readFields != null ? (r, b, s) => readFields(r, (T)b, s) : null,
            writeFields != null ? (w, b) => writeFields(w, (T)b) : null
        );
    }

    /// <summary>
    /// Creates a standalone block of type <typeparamref name="T"/>. Consumers populate its fields
    /// via property assignment.
    /// </summary>
    /// <typeparam name="T">The <see cref="Block"/> type to create.</typeparam>
    /// <returns>A new instance of <typeparamref name="T"/>.</returns>
#pragma warning disable CA1822 // Mark members as static
    public T CreateBlock<T>() where T : Block => (T)Activator.CreateInstance(typeof(T), nonPublic: true)!;
#pragma warning restore CA1822 // Mark members as static

    /// <summary>
    /// Reads a HIP archive from <paramref name="stream"/>, producing an ordered list of root
    /// blocks (typically four: HIPA, PACK, DICT, STRM). Closes <paramref name="stream"/> before
    /// returning.
    /// </summary>
    /// <param name="stream">The stream to read the archive from.</param>
    /// <returns>The ordered list of root <see cref="Block"/>s read from the stream.</returns>
    /// <exception cref="FormatException">
    /// Thrown when a block has an unrecognized tag, or a block's content doesn't consume exactly
    /// the number of bytes its Size field declares.
    /// </exception>
    public List<Block> Read(Stream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.ASCII, leaveOpen: false);

        var roots = new List<Block>();
        while (reader.BaseStream.Position < reader.BaseStream.Length)
            roots.Add(ReadBlock(reader));

        return roots;
    }

    /// <summary>
    /// Reads one block, including its children, from the current position of <paramref name="reader"/>.
    /// </summary>
    /// <param name="reader">The reader to read the block from.</param>
    /// <returns>The <see cref="Block"/> read from the stream, including its children.</returns>
    /// <exception cref="FormatException">
    /// Thrown when the block's tag has no registered handler, or its content doesn't consume
    /// exactly the number of bytes its Size field declares.
    /// </exception>
    protected Block ReadBlock(BinaryReader reader)
    {
        string tag = ReadTag(reader);
        uint size = reader.ReadEvilInt();
        long contentStart = reader.BaseStream.Position;

        if (!_handlers.TryGetValue(tag, out var handler))
            throw new FormatException($"Unknown block tag '{tag}'.");

        Block block = handler.Create();
        handler.ReadFields?.Invoke(reader, block, size);

        while ((reader.BaseStream.Position - contentStart) < size)
            block.Children.Add(ReadBlock(reader));

        if ((reader.BaseStream.Position - contentStart) != size)
            throw new FormatException(
                $"Block '{tag}' at offset {contentStart}: consumed {reader.BaseStream.Position - contentStart} content bytes, expected {size}.");

        return block;
    }

    /// <summary>
    /// Reads a block's 4-byte ASCII tag from the current position of <paramref name="reader"/>.
    /// </summary>
    /// <param name="reader">The reader to read the tag from.</param>
    /// <returns>The 4-character tag.</returns>
    protected static string ReadTag(BinaryReader reader) => Encoding.ASCII.GetString(reader.ReadBytes(4));

    /// <summary>
    /// Maps each registered block tag to its <c>ReadFields</c> delegate's <see cref="MethodInfo"/>
    /// (<see langword="null"/> if the tag has no field reader). Used by the contract test suite to
    /// detect when a serializer has replaced a base registration without declaring it.
    /// </summary>
    internal IReadOnlyDictionary<string, MethodInfo?> HandlerFingerprint() =>
        _handlers.ToDictionary(kv => kv.Key, kv => kv.Value.ReadFields?.Method);
}
