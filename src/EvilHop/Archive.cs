using EvilHop.Assets;
using EvilHop.Blocks;
using EvilHop.Common;
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
    public static Archive Load(Stream stream, Serializer serializer)
    {
        ArgumentNullException.ThrowIfNull(serializer);
        return new(serializer, serializer.Read(stream));
    }

    /// <summary>
    /// Loads a HIP archive built for <paramref name="game"/> from <paramref name="stream"/>, without
    /// stating a platform. The block layer works as usual; <see cref="OpenAssets()"/> refuses until a
    /// platform is stated, which <see cref="OpenAssets(Platform)"/> can do.
    /// </summary>
    /// <param name="stream">The <see cref="Stream"/> to load from.</param>
    /// <param name="game">The game the archive was built for.</param>
    /// <returns>An <see cref="Archive"/> constructed from the provided parameters.</returns>
    public static Archive Load(Stream stream, GameVersion game) => Load(stream, Serializer.Create(game));

    /// <summary>
    /// Loads a HIP archive built for <paramref name="game"/> on <paramref name="platform"/> from
    /// <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">The <see cref="Stream"/> to load from.</param>
    /// <param name="game">The game the archive was built for.</param>
    /// <param name="platform">The console the archive was built for.</param>
    /// <returns>An <see cref="Archive"/> constructed from the provided parameters.</returns>
    public static Archive Load(Stream stream, GameVersion game, Platform platform) =>
        Load(stream, Serializer.Create(game, platform));

    /// <summary>
    /// Creates a new, empty archive stamped as built for <paramref name="serializer"/>'s platform, with
    /// the package values every NTSC-U US archive of that platform shares. Its creation and
    /// modification times are the current time.
    /// </summary>
    /// <param name="serializer">
    /// The <see cref="Serialization.Serializer"/> for the game and platform to create the archive for.
    /// </param>
    /// <returns>A new, empty <see cref="Archive"/>.</returns>
    /// <exception cref="NotSupportedException">Thrown when <paramref name="serializer"/>'s game isn't <see cref="GameVersion.TSSM"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="serializer"/> states no platform to stamp.</exception>
    public static Archive Create(Serializer serializer) => Create(serializer, DateTimeOffset.UtcNow);

    internal static Archive Create(Serializer serializer, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(serializer);

        var profile = serializer.Profile;
        if (profile.Game is not GameVersion.TSSM)
            throw new NotSupportedException($"Creating {profile.Game} archives isn't supported yet.");

        var platform = profile.Platform
            ?? throw new InvalidOperationException(
                $"A new archive can't be stamped without a platform: this {nameof(Serializer)} states none.");

        var archive = new Archive(serializer, TSSMArchiveTemplate.Build(serializer, platform, now));
        using (archive.OpenAssets()) { } // an empty commit lays out the counts and stream padding for the platform
        return archive;
    }

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
    /// <returns>A new <see cref="AssetSession"/> over this <see cref="Archive"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="Serializer"/> states no platform. Use <see cref="OpenAssets(Platform)"/> to supply one.
    /// </exception>
    public AssetSession OpenAssets() => AssetSession.Open(this, null);

    /// <summary>
    /// Enters Asset Mode like <see cref="OpenAssets()"/>, supplying the <paramref name="platform"/>
    /// the assets were built for when <see cref="Serializer"/> states none.
    /// </summary>
    /// <param name="platform">The console the archive was built for.</param>
    /// <returns>A new <see cref="AssetSession"/> over this <see cref="Archive"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="Serializer"/> already states a different platform. A stated platform is never overridden.
    /// </exception>
    public AssetSession OpenAssets(Platform platform) => AssetSession.Open(this, platform);
}
