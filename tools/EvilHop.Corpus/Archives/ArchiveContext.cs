using EvilHop.Blocks;

namespace EvilHop.Corpus.Archives;

/// <summary>
/// A single parsed archive, in hand just long enough to be observed by extraction and invariants
/// before being discarded.
/// </summary>
internal sealed class ArchiveContext
{
    /// <summary>
    /// The build key this archive is attributed to, e.g. <c>n100f/release/GC/NTSC-U/US</c>.
    /// </summary>
    public required string BuildKey { get; init; }

    /// <summary>
    /// The corpus-relative path used as an exemplar when a value is traced back to a file, e.g.
    /// <c>n100f/release/GC/NTSC-U/US/boot.HIP</c>.
    /// </summary>
    public required string RelativePath { get; init; }

    /// <summary>
    /// The root blocks read from the archive, typically HIPA, PACK, DICT, and STRM.
    /// </summary>
    public required IReadOnlyList<Block> Roots { get; init; }

    /// <summary>
    /// The total size of the archive file in bytes.
    /// </summary>
    public required long ArchiveLength { get; init; }

    /// <summary>
    /// Every block reachable from <see cref="Roots"/>, including the roots themselves.
    /// </summary>
    public IEnumerable<Block> AllBlocks => Roots.AllBlocks();
}
