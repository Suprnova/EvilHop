using EvilHop.Blocks;
using EvilHop.Common;
using System.Diagnostics.CodeAnalysis;

namespace EvilHop.Assets;

/// <summary>
/// An individual resource or object located in a level's archive.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/Assets">Heavy Iron Modding documentation</seealso>
/// </remarks>
public abstract class Asset(AssetType type) : IPhysicalAsset
{
    /// <summary>
    /// The <see cref="Asset"/>'s ID.
    /// </summary>
    public AssetId Id { get; set; }

    /// <summary>
    /// The <see cref="Asset"/>'s type. Fixed by the concrete class that declares it, except on the
    /// shape-generic classes, which take it as a constructor argument.
    /// </summary>
    public AssetType Type { get; internal set; } = type;

    /// <summary>
    /// The <see cref="Asset"/>'s name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The <see cref="Asset"/>'s filename.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// The <see cref="Assets.Layer"/> that this <see cref="Asset"/> belongs to.
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
    /// The CRC-32/MPEG-2 of this <see cref="Asset"/>'s data as last read or serialized - the value
    /// <see cref="IPhysicalAsset.Checksum"/> reports when nothing overrides it.
    /// </summary>
    /// <remarks>
    /// When overridden with <see cref="IPhysicalAsset.Checksum"/>, that value will have priority,
    /// even when the <see cref="Asset"/> has been modified.
    /// </remarks>
    internal uint ComputedChecksum { get; set; }

    private uint? _overriddenChecksum;
    uint IPhysicalAsset.Checksum
    {
        get => _overriddenChecksum ?? ComputedChecksum;
        set => _overriddenChecksum = value == ComputedChecksum ? null : value;
    }

    /// <summary>
    /// Returns the bytes that this <see cref="Asset"/> was unable to parse from its
    /// slice of <see cref="StreamData"/>.
    /// </summary>
    /// <returns>The unparsed bytes for this <see cref="Asset"/>.</returns>
    [SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "Span<byte> is deliberate and can't be a field.")]
    public Span<byte> GetUnparsedTail() => UnparsedTail;

    /// <summary>
    /// Overwrites the existing <see cref="UnparsedTail"/>, if any, with the provided bytes.
    /// </summary>
    /// <param name="bytes">The bytes to append at the end of this <see cref="Asset"/>'s data.</param>
    /// <exception cref="NotSupportedException">
    /// If this <see cref="Asset"/> has no unparsed region for the bytes to go in, such as <see cref="PayloadAsset"/>.
    /// </exception>
    public virtual void SetUnparsedTail(byte[] bytes) => UnparsedTail = bytes;

    /// <summary>
    /// Recalculates and assigns <see cref="Id"/> from this <see cref="Asset"/>'s <see cref="Name"/>
    /// and <see cref="Type"/>.
    /// </summary>
    /// <remarks>
    /// Does not modify fields under <see cref="Physical"/>, such as
    /// <see cref="IPhysicalBaseAsset.BaseId"/>.
    /// </remarks>
    public void CalculateId() => Id = AssetId.FromName(Name, Type);
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
    /// <summary>
    /// The <see cref="Asset"/>'s CRC-32/MPEG-2 checksum, retrieved from
    /// <see cref="AssetDebug.Checksum"/> and derived from the asset's own data unless overridden.
    /// </summary>
    /// <remarks>
    /// When disagreements with the <see cref="Asset"/>'s data exist, this field wins during
    /// serialization. 
    /// </remarks>
    uint Checksum { get; set; }
}
