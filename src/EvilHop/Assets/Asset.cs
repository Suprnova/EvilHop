using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;

namespace EvilHop.Assets;

/// <summary>
/// An individual resource or object located in a level's archive.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/Assets">Heavy Iron Modding documentation</seealso>
/// </remarks>
public abstract class Asset : IPhysicalAsset
{
    /// <summary>
    /// The <see cref="Asset"/>'s ID.
    /// </summary>
    public AssetId Id { get; set; }

    /// <summary>
    /// The <see cref="Asset"/>'s type.
    /// </summary>
    public AssetType Type { get; internal set; }

    /// <summary>
    /// The <see cref="Asset"/>'s name.
    /// </summary>
    public string Name { get; set; } = String.Empty;

    /// <summary>
    /// The <see cref="Asset"/>'s filename.
    /// </summary>
    public string FileName { get; set; } = String.Empty;

    /// <summary>
    /// The <see cref="Layer"/> that this <see cref="Asset"/> belongs to.
    /// </summary>
    public Layer? Layer { get; internal set; }

    /// <summary>
    /// This <see cref="Asset"/>'s underlying on-disk values, including those with no logical
    /// equivalent on the <see cref="Asset"/> itself.
    /// </summary>
    /// <remarks>
    /// Most consumers never need this. It exists for reproducing an archive's exact bytes,
    /// authoring deliberately malformed data, and the library's own codecs - anything that has to
    /// address the format as stored rather than as modelled.
    /// </remarks>
    public virtual IPhysicalAsset Physical => this;

    internal byte[] UnparsedTail { get; set; } = [];

    private AssetType? _overriddenType;
    AssetType IPhysicalAsset.Type
    {
        get => _overriddenType ?? this.Type;
        // Assigning the value this already derives clears the override rather than pinning one, so
        // a codec can populate this unconditionally from disk without freezing the asset out of
        // tracking later changes to Type. See IPhysicalBaseAsset.BaseId for the same pattern.
        set => _overriddenType = value == Type ? null : value;
    }

    private int _alignment;
    int IPhysicalAsset.Alignment
    {
        get => _alignment;
        set => _alignment = value;
    }

    private AssetFlags _flags;
    AssetFlags IPhysicalAsset.Flags
    {
        get => _flags;
        set => _flags = value;
    }

    /// <summary>
    /// Returns the bytes that this <see cref="Asset"/> was unable to parse from its
    /// slice of <see cref="StreamData"/>.
    /// </summary>
    /// <returns>The unparsed bytes for this <see cref="Asset"/>.</returns>
    public Span<byte> GetUnparsedTail() => UnparsedTail;

    /// <summary>
    /// Overwrites the existing <see cref="UnparsedTail"/>, if any, with the provided bytes.
    /// </summary>
    /// <param name="bytes">The bytes to append at the end of this <see cref="Asset"/>'s data.</param>
    /// <exception cref="NotSupportedException">
    /// If this <see cref="Asset"/> has no unparsed region for the bytes to go in.
    /// </exception>
    public virtual void SetUnparsedTail(byte[] bytes) => UnparsedTail = bytes;
}

/// <summary>
/// An explicit interface used to interact with <see cref="Asset"/>'s underlying values.
/// </summary>
public interface IPhysicalAsset
{
    /// <summary>
    /// The <see cref="Asset"/>'s type, retrieved from <see cref="AssetHeader.Type"/>.
    /// </summary>
    AssetType Type { get; set; }
    /// <summary>
    /// The <see cref="Asset"/>'s alignment, retrieved from <see cref="AssetDebug.Alignment"/>.
    /// </summary>
    int Alignment { get; set; }
    /// <summary>
    /// The <see cref="Asset"/>'s <see cref="AssetFlags"/>, retrieved from <see cref="AssetHeader.Flags"/>.
    /// </summary>
    AssetFlags Flags { get; set; }
}
