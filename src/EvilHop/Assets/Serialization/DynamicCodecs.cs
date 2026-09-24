using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets.Serialization;

/// <summary>
/// Maps each <see cref="DynamicKind"/> with a typed model to the <see cref="DynamicAsset"/> subclass
/// and codec that read and write its fields.
/// </summary>
/// <remarks>
/// The second level of <see cref="AssetType.Dynamic"/>'s dispatch, after <see cref="AssetCodecs"/>.
/// A codec only reads and writes its kind's own fields - the region between the
/// <see cref="DynamicAssetPrefix"/> and the links - so a reader is always scoped to exactly that
/// region. Any kind, game, or version with no registered layout reads as a
/// <see cref="GenericDynamicAsset"/>.
/// </remarks>
internal static class DynamicCodecs
{
    /// <summary>
    /// Reads a <typeparamref name="T"/>'s own fields into <c>asset</c>, whose prefix has already been
    /// read.
    /// </summary>
    internal delegate void ReadFunc<in T>(T asset, EndianReader reader, FormatProfile profile) where T : DynamicAsset;

    /// <summary>
    /// Writes a <typeparamref name="T"/>'s own fields.
    /// </summary>
    internal delegate void WriteFunc<in T>(T asset, EndianWriter writer, FormatProfile profile) where T : DynamicAsset;

    private sealed record Handler(
        Func<DynamicAsset> Create,
        ReadFunc<DynamicAsset> Read,
        WriteFunc<DynamicAsset> Write,
        IReadOnlySet<(GameVersion Game, short Version)> Layouts);

    private static readonly Dictionary<DynamicKind, Handler> Handlers = [];

    static DynamicCodecs() => RegisterConcreteCodecs();

    /// <summary>
    /// Registers every <see cref="DynamicKind"/> with a typed model. Each entry just points at its
    /// subclass's own <c>Read</c>/<c>Write</c> pair.
    /// </summary>
    private static void RegisterConcreteCodecs()
    {
        Register<TalkBoxDynamicAsset>(DynamicKind.TalkBox, TalkBoxDynamicAsset.Read, TalkBoxDynamicAsset.Write, TalkBoxDynamicAsset.SupportedLayouts);
    }

    /// <summary>
    /// Registers <typeparamref name="T"/> as <paramref name="kind"/>'s typed model, replacing
    /// whatever was there.
    /// </summary>
    /// <param name="kind">The <see cref="DynamicKind"/> to register against.</param>
    /// <param name="read">Reads <typeparamref name="T"/>'s own fields.</param>
    /// <param name="write">Writes <typeparamref name="T"/>'s own fields.</param>
    /// <param name="layouts">
    /// Every game and <see cref="Physical.IDynamicAsset.Version"/> pair <paramref name="read"/>
    /// understands. The same version can be laid out differently in different games.
    /// </param>
    public static void Register<T>(DynamicKind kind, ReadFunc<T> read, WriteFunc<T> write, IReadOnlySet<(GameVersion Game, short Version)> layouts)
        where T : DynamicAsset, new() =>
        Handlers[kind] = new Handler(
            () => new T(),
            (asset, reader, profile) => read((T)asset, reader, profile),
            (asset, writer, profile) => write((T)asset, writer, profile),
            layouts);

    /// <summary>
    /// Creates the <see cref="DynamicAsset"/> a dynamic of <paramref name="kind"/> and
    /// <paramref name="version"/> reads into under <paramref name="game"/>.
    /// </summary>
    public static DynamicAsset Create(DynamicKind kind, short version, GameVersion game) =>
        Handlers.TryGetValue(kind, out var handler) && handler.Layouts.Contains((game, version))
            ? handler.Create()
            : new GenericDynamicAsset(kind);

    /// <summary>
    /// Reads <paramref name="asset"/>'s own fields, if it has a typed model.
    /// </summary>
    public static void ReadBody(DynamicAsset asset, EndianReader reader, FormatProfile profile) =>
        HandlerFor(asset)?.Read(asset, reader, profile);

    /// <summary>
    /// Writes <paramref name="asset"/>'s own fields, if it has a typed model.
    /// </summary>
    public static void WriteBody(DynamicAsset asset, EndianWriter writer, FormatProfile profile) =>
        HandlerFor(asset)?.Write(asset, writer, profile);

    private static Handler? HandlerFor(DynamicAsset asset) =>
        asset is not GenericDynamicAsset && Handlers.TryGetValue(asset.Kind, out var handler) ? handler : null;
}
