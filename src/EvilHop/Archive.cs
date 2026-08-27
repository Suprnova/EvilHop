using EvilHop.Assets;
using EvilHop.Blocks;
using EvilHop.Serialization;

namespace EvilHop;

/// <summary>
/// Represents a single HIP archive file in memory. This is the primary entry point for
/// all consumers, providing both high-level asset manipulation capabilities alongside low-level
/// block access through a dual-layer architecture with modal access control.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/HIP_(File_Format)">Heavy Iron Modding documentation</seealso>
/// </remarks>
public class Archive(Serializer serializer, IReadOnlyList<Block> roots)
{
    /// <summary>
    /// The <see cref="Serialization.Serializer"/> used to construct this <see cref="Archive"/>. 
    /// </summary>
    public Serializer Serializer { get; } = serializer;

    /// <summary>
    /// The root <see cref="Block"/> objects that make up this <see cref="Archive"/>.
    /// </summary>
    public IReadOnlyList<Block> Roots { get; private set; } = roots;

    /// <summary>
    /// Load a HIP archive using the provided <paramref name="stream"/> and
    /// <paramref name="serializer"/> and return the constructed <see cref="Archive"/>.
    /// </summary>
    /// <param name="stream">The <see cref="Stream"/> to load from.</param>
    /// <param name="serializer">The <see cref="Serialization.Serializer"/> to use for serializing.</param>
    /// <returns>An <see cref="Archive"/> constructed from the provided parameters.</returns>
    public static Archive Load(Stream stream, Serializer serializer) =>
        new(serializer, serializer.Read(stream));

    /// <summary>
    /// Saves a HIP archive to the provided <paramref name="stream"/> using <see cref="Serializer"/>.
    /// </summary>
    /// <param name="stream">The <see cref="Stream"/> to save to.</param>
    public void Save(Stream stream) => Serializer.Write(stream, Roots);

    /// <summary>
    /// Enters Asset Mode, returning a session that owns this <see cref="Archive"/>'s assets until it
    /// is committed or disposed.
    /// </summary>
    /// <remarks>
    /// While the session is open, the <c>ATOC</c>, <c>LTOC</c>, and <c>DPAK</c> blocks are detached
    /// from <see cref="Roots"/> and their fields are locked. Any reference to them taken beforehand
    /// is orphaned for the session's lifetime and is not reused afterward.
    /// </remarks>
    /// <returns>A new <see cref="Assets.AssetSession"/> over this <see cref="Archive"/>.</returns>
    public AssetSession OpenAssets() => AssetSession.Open(this);
}
