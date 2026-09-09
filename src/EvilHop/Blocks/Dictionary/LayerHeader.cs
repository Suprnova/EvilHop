using EvilHop.Common;

namespace EvilHop.Blocks;

/// <summary>
/// A child <see cref="Block"/> of <see cref="LayerTable"/> which defines an entry
/// for a <c>Layer</c>.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/HIP_(File_Format)#LHDR">Heavy Iron Modding documentation</seealso>
/// </remarks>
/// Validation TODO: Required LDBG.
public class LayerHeader : Block
{
    /// <inheritdoc/>
    protected internal override string Tag => "LHDR";

    /// <summary>
    /// The child <see cref="LayerDebug"/> of the <see cref="LayerHeader"/>.
    /// </summary>
    public LayerDebug Debug
    {
        get => GetRequiredChild<LayerDebug>();
        set => SetChild(value);
    }

    /// <summary>
    /// The <c>Layer</c>'s type.
    /// </summary>
    /// Validation TODO: Maps to closed enum value.
    public LayerType Type
    {
        get => GetManagedBlockField(ref field);
        set => SetManagedBlockField(ref field, value);
    }

    /// <summary>
    /// The number of <c>assets</c> present in this <c>Layer</c>.
    /// </summary>
    /// Validation TODO: When summed across all LHDRs, does not exceed count of AHDRs.
    /// Equal to size of AssetIds.
    public uint AssetCount
    {
        get => GetManagedBlockField(ref field);
        set => SetManagedBlockField(ref field, value);
    }

    /// <summary>
    /// A list of the IDs for the <c>assets</c> present in this <c>Layer</c>.
    /// </summary>
    /// Validation TODO: For each ID, an AHDR of that ID exists.
    public IEnumerable<uint> AssetIds
    {
        get => GetManagedBlockField(ref field);
        set => SetManagedBlockField(ref field, value);
    } = [];

    internal LayerHeader() { }
}

#pragma warning disable CS1591 // Missing XML comment

/// <summary>
/// Represents all known values for <see cref="AssetHeader.Flags"/>.
/// Communicates information about an <c>Asset</c>'s data and how it should be handled by the game.
/// </summary>
[Flags]
public enum AssetFlags : uint
{
    None = 0U,
    /// <summary>
    /// The <c>Asset</c>'s data was sourced from an external file.
    /// </summary>
    /// <remarks>
    /// When set, <see cref="AssetDebug.FileName"/> should be populated with the file's source.
    /// This should not be set simultaneously with <see cref="SourceVirtual"/>.
    /// </remarks>
    SourceFile = 1U << 0,
    /// <summary>
    /// The <c>Asset</c>'s data was created by Heavy Iron's internal level editor.
    /// </summary>
    /// <remarks>
    /// When set, <see cref="AssetDebug.FileName"/> should be empty.
    /// This should not be set simultaneously with <see cref="SourceFile"/>.
    /// </remarks>
    SourceVirtual = 1U << 1,
    /// <summary>
    /// The <c>Asset</c>'s data is stored in a special format and must be converted into another
    /// at runtime.
    /// </summary>
    ReadTransform = 1U << 2,
    /// <summary>
    /// The <c>Asset</c>'s data must be transformed from a runtime-specific format into a special
    /// binary format.
    /// </summary>
    WriteTransform = 1U << 3,
    UnknownScooby = 1U << 31
}
