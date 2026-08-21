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
        RegisterBlock<PackageVersion>(ReadPackageVersion, WritePackageVersion);
        RegisterBlock<PackageFlags>(ReadPackageFlags, WritePackageFlags);
        RegisterBlock<PackageCount>(ReadPackageCount, WritePackageCount);
        RegisterBlock<PackageCreated>(ReadPackageCreated, WritePackageCreated);
        RegisterBlock<PackageModified>(ReadPackageModified, WritePackageModified);
        RegisterBlock<PackagePlatform>(ReadPackagePlatform, WritePackagePlatform);

        RegisterBlock<Dictionary>();
        RegisterBlock<AssetTable>();
        RegisterBlock<AssetInf>(ReadAssetInf, WriteAssetInf);
        RegisterBlock<AssetHeader>(ReadAssetHeader, WriteAssetHeader);
        RegisterBlock<AssetDebug>(ReadAssetDebug, WriteAssetDebug);
        RegisterBlock<LayerTable>();
        RegisterBlock<LayerInf>(ReadLayerInf, WriteLayerInf);
        RegisterBlock<LayerHeader>(ReadLayerHeader, WriteLayerHeader);
        RegisterBlock<LayerDebug>(ReadLayerDebug, WriteLayerDebug);

        RegisterBlock<AssetStream>();
        RegisterBlock<StreamHeader>(ReadStreamHeader, WriteStreamHeader);
        RegisterBlock<StreamData>(ReadStreamData, WriteStreamData);
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
    /// Writes an ordered list of root blocks to <paramref name="stream"/> as a HIP archive. Closes
    /// <paramref name="stream"/> before returning.
    /// </summary>
    /// <param name="stream">
    /// The stream to write the archive to. Must support seeking, so each block's <c>Size</c> field
    /// can be backpatched once its content length is known.
    /// </param>
    /// <param name="roots">The ordered list of root <see cref="Block"/>s to write.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="stream"/> does not support seeking.</exception>
    /// <exception cref="FormatException">Thrown when a block has no registered handler.</exception>
    public void Write(Stream stream, IEnumerable<Block> roots)
    {
        if (!stream.CanSeek)
            throw new ArgumentException(
                "The destination stream must support seeking, to backpatch each block's Size field.", nameof(stream));

        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: false);
        foreach (var root in roots)
            WriteBlock(writer, root);
    }

    /// <summary>
    /// Writes one block, including its children, to the current position of <paramref name="writer"/>.
    /// </summary>
    /// <param name="writer">The writer to write the block to.</param>
    /// <param name="block">The <see cref="Block"/> to write, including its children.</param>
    /// <exception cref="FormatException">Thrown when the block's tag has no registered handler.</exception>
    protected void WriteBlock(BinaryWriter writer, Block block)
    {
        if (!_handlers.TryGetValue(block.Tag, out var handler))
            throw new FormatException($"Unknown block tag '{block.Tag}'.");

        writer.Write(Encoding.ASCII.GetBytes(block.Tag));

        long sizePosition = writer.BaseStream.Position;
        writer.WriteEvilInt(0); // placeholder, backpatched below
        long contentStart = writer.BaseStream.Position;

        handler.WriteFields?.Invoke(writer, block);
        foreach (var child in block.Children)
            WriteBlock(writer, child);

        long contentEnd = writer.BaseStream.Position;
        writer.BaseStream.Position = sizePosition;
        writer.WriteEvilInt((uint)(contentEnd - contentStart));
        writer.BaseStream.Position = contentEnd;
    }

    /// <summary>
    /// Maps each registered block tag to its <c>ReadFields</c> and <c>WriteFields</c> delegates'
    /// <see cref="MethodInfo"/>s (<see langword="null"/> where a tag has no field reader or writer).
    /// Used by the contract test suite to detect when a serializer has replaced a base registration
    /// without declaring it.
    /// </summary>
    internal IReadOnlyDictionary<string, (MethodInfo? Read, MethodInfo? Write)> HandlerFingerprint() =>
        _handlers.ToDictionary(kv => kv.Key, kv => (kv.Value.ReadFields?.Method, kv.Value.WriteFields?.Method));
}
