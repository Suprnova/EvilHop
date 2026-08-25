using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// Maps each <see cref="AssetType"/> to the codec that reads and writes it. The only dispatch
/// mechanism the asset layer has.
/// </summary>
/// <remarks>
/// Every known type is seeded at static construction with a generic handler for its shape - enough
/// to parse the prefix its family shares and preserve the rest. Registering a concrete codec
/// overwrites that entry, the same way <c>Serializer.RegisterBlock</c> overwrites a tag's handler.
/// Nothing outside this class branches on shape; callers look up a type and invoke whatever they
/// find.
/// </remarks>
internal static class AssetCodecs
{
    /// <summary>Reads one asset from its slice of the asset stream.</summary>
    internal delegate Asset ReadFunc(ReadOnlySpan<byte> data, AssetHeader header, AssetDebug debug, FormatProfile profile);

    /// <summary>Writes one asset's data, excluding its <c>AHDR</c>/<c>ADBG</c> blocks.</summary>
    internal delegate void WriteFunc(Asset asset, BinaryWriter writer, FormatProfile profile);

    /// <summary>Typed <see cref="ReadFunc"/>, for a concrete codec registering its own type.</summary>
    internal delegate T ReadFunc<out T>(ReadOnlySpan<byte> data, AssetHeader header, AssetDebug debug, FormatProfile profile)
        where T : Asset;

    /// <summary>Typed <see cref="WriteFunc"/>, for a concrete codec registering its own type.</summary>
    internal delegate void WriteFunc<in T>(T asset, BinaryWriter writer, FormatProfile profile)
        where T : Asset;

    private readonly record struct CodecHandler(ReadFunc Read, WriteFunc Write);

    private static readonly Dictionary<AssetType, CodecHandler> Handlers = [];

    /// <summary>
    /// Used for any <see cref="AssetType"/> with no entry at all. Preserves the whole slice, which
    /// is the only correct thing to do with bytes of entirely unknown shape.
    /// </summary>
    private static readonly CodecHandler Fallback = new(ReadPlain, WritePlain);

    static AssetCodecs() => RegisterGenericShapes();

    /// <summary>
    /// Registers <typeparamref name="T"/>'s codec for <paramref name="type"/>, replacing whatever
    /// was there.
    /// </summary>
    /// <remarks>
    /// Per-game filtering - a codec declaring which <see cref="GameVersion"/>s it supports - arrives
    /// with the first concrete codec, where its semantics can be settled against a real case rather
    /// than guessed at.
    /// </remarks>
    /// <typeparam name="T">The <see cref="Asset"/> type this codec produces.</typeparam>
    /// <param name="type">The <see cref="AssetType"/> to register against.</param>
    /// <param name="read">Reads an asset of this type.</param>
    /// <param name="write">Writes an asset of this type.</param>
    public static void Register<T>(AssetType type, ReadFunc<T> read, WriteFunc<T> write) where T : Asset =>
        Handlers[type] = new CodecHandler(
            (data, header, debug, profile) => read(data, header, debug, profile),
            (asset, writer, profile) => write((T)asset, writer, profile));

    /// <summary>
    /// Reads one asset, dispatching on <see cref="AssetHeader.Type"/> alone.
    /// </summary>
    public static Asset Read(ReadOnlySpan<byte> data, AssetHeader header, AssetDebug debug, FormatProfile profile) =>
        Resolve(header.Type).Read(data, header, debug, profile);

    /// <summary>
    /// Writes one asset's data, dispatching on its <see cref="Asset.Type"/>.
    /// </summary>
    /// <remarks>
    /// Dispatches on the logical <see cref="Asset.Type"/>, not <c>Physical.Type</c> - the codec that
    /// understands an object's fields is the one matching what it actually is, even where the header
    /// tag it writes has been deliberately set to disagree.
    /// </remarks>
    public static void Write(Asset asset, BinaryWriter writer, FormatProfile profile) =>
        Resolve(asset.Type).Write(asset, writer, profile);

    private static CodecHandler Resolve(AssetType type) =>
        Handlers.TryGetValue(type, out var handler) ? handler : Fallback;

    private static void RegisterGenericShapes()
    {
        foreach (var (type, shape) in ShapesByType)
            Handlers[type] = shape switch
            {
                AssetShape.BaseAsset => new CodecHandler(ReadBase, WriteBase),
                AssetShape.EntityAsset => new CodecHandler(ReadEntity, WriteEntity),
                AssetShape.DynaAsset => new CodecHandler(ReadDyna, WriteDyna),
                AssetShape.Payload => new CodecHandler(ReadPayload, WritePayload),
                _ => Fallback
            };
    }

    private static Asset ReadPlain(ReadOnlySpan<byte> data, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new GenericAsset();
        AssetFields.Populate(asset, header, debug);
        asset.SetUnparsedTail(data.ToArray());
        return asset;
    }

    private static void WritePlain(Asset asset, BinaryWriter writer, FormatProfile profile) =>
        writer.Write(asset.GetUnparsedTail());

    private static Asset ReadBase(ReadOnlySpan<byte> data, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new GenericBaseAsset();
        AssetFields.Populate(asset, header, debug);
        int offset = BaseAssetPrefix.Read(asset, data);
        asset.SetUnparsedTail(data[offset..].ToArray());
        return asset;
    }

    private static void WriteBase(Asset asset, BinaryWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write((BaseAsset)asset, writer);
        writer.Write(asset.GetUnparsedTail());
    }

    private static Asset ReadEntity(ReadOnlySpan<byte> data, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new GenericEntityAsset();
        AssetFields.Populate(asset, header, debug);
        int offset = BaseAssetPrefix.Read(asset, data);
        offset = EntityAssetPrefix.Read(asset, data, offset, profile.EntityHasPadding);
        asset.SetUnparsedTail(data[offset..].ToArray());
        return asset;
    }

    private static void WriteEntity(Asset asset, BinaryWriter writer, FormatProfile profile)
    {
        var entity = (EntityAsset)asset;
        BaseAssetPrefix.Write(entity, writer);
        EntityAssetPrefix.Write(entity, writer, profile.EntityHasPadding);
        writer.Write(asset.GetUnparsedTail());
    }

    private static Asset ReadDyna(ReadOnlySpan<byte> data, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new GenericDynaAsset();
        AssetFields.Populate(asset, header, debug);
        int offset = BaseAssetPrefix.Read(asset, data);
        offset = DynaAssetPrefix.Read(asset, data, offset);
        asset.SetUnparsedTail(data[offset..].ToArray());
        return asset;
    }

    private static void WriteDyna(Asset asset, BinaryWriter writer, FormatProfile profile)
    {
        var dyna = (DynaAsset)asset;
        BaseAssetPrefix.Write(dyna, writer);
        DynaAssetPrefix.Write(dyna, writer);
        writer.Write(asset.GetUnparsedTail());
    }

    private static Asset ReadPayload(ReadOnlySpan<byte> data, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new GenericPayloadAsset();
        AssetFields.Populate(asset, header, debug);
        asset.Data = data.ToArray();
        return asset;
    }

    private static void WritePayload(Asset asset, BinaryWriter writer, FormatProfile profile) =>
        writer.Write(((PayloadAsset)asset).Data);

    private enum AssetShape { BaseAsset, EntityAsset, DynaAsset, Payload }

    /// <summary>
    /// Which shape each known <see cref="AssetType"/> follows. Read once, by
    /// <see cref="RegisterGenericShapes"/>, and never consulted again - by the time a type has a
    /// concrete codec, its class hierarchy states its shape and this entry is gone.
    /// </summary>
    /// <remarks>
    /// A type absent from this table has no known shape and falls through to <see cref="Fallback"/>.
    /// The payload set is a starting set: under-covering it is safe (the type still round-trips,
    /// just without <see cref="PayloadAsset.SaveToFile"/>), over-covering it is not.
    /// </remarks>
    private static readonly Dictionary<AssetType, AssetShape> ShapesByType = new()
    {
        [AssetType.Boulder] = AssetShape.EntityAsset,
        [AssetType.Button] = AssetShape.EntityAsset,
        [AssetType.DestructibleObject] = AssetShape.EntityAsset,
        [AssetType.ElectricArcGenerator] = AssetShape.EntityAsset,
        [AssetType.Hangable] = AssetShape.EntityAsset,
        [AssetType.NPC] = AssetShape.EntityAsset,
        [AssetType.Pendulum] = AssetShape.EntityAsset,
        [AssetType.Pickup] = AssetShape.EntityAsset,
        [AssetType.Platform] = AssetShape.EntityAsset,
        [AssetType.Player] = AssetShape.EntityAsset,
        [AssetType.SimpleObject] = AssetShape.EntityAsset,
        [AssetType.Trigger] = AssetShape.EntityAsset,
        [AssetType.UI] = AssetShape.EntityAsset,
        [AssetType.UIFont] = AssetShape.EntityAsset,
        [AssetType.Villain] = AssetShape.EntityAsset,

        [AssetType.Dynamic] = AssetShape.DynaAsset,

        [AssetType.BSP] = AssetShape.Payload,
        [AssetType.BinkVideo] = AssetShape.Payload,
        [AssetType.JSP] = AssetShape.Payload,
        [AssetType.Model] = AssetShape.Payload,
        [AssetType.RawImage] = AssetShape.Payload,
        [AssetType.Sound] = AssetShape.Payload,
        [AssetType.StreamingSound] = AssetShape.Payload,
        [AssetType.StreamingTexture] = AssetShape.Payload,
        [AssetType.Texture] = AssetShape.Payload,

        [AssetType.Camera] = AssetShape.BaseAsset,
        [AssetType.CameraCurve] = AssetShape.BaseAsset,
        [AssetType.Conditional] = AssetShape.BaseAsset,
        [AssetType.Counter] = AssetShape.BaseAsset,
        [AssetType.CutsceneManager] = AssetShape.BaseAsset,
        [AssetType.DashTrack] = AssetShape.BaseAsset,
        [AssetType.DiscoFloor] = AssetShape.BaseAsset,
        [AssetType.Dispatcher] = AssetShape.BaseAsset,
        [AssetType.Duplicator] = AssetShape.BaseAsset,
        [AssetType.Environment] = AssetShape.BaseAsset,
        [AssetType.Fog] = AssetShape.BaseAsset,
        [AssetType.GrassMesh] = AssetShape.BaseAsset,
        [AssetType.Group] = AssetShape.BaseAsset,
        [AssetType.Gust] = AssetShape.BaseAsset,
        [AssetType.Light] = AssetShape.BaseAsset,
        [AssetType.LobMaster] = AssetShape.BaseAsset,
        [AssetType.MovePoint] = AssetShape.BaseAsset,
        [AssetType.NavigationMesh] = AssetShape.BaseAsset,
        [AssetType.ParticleEmitter] = AssetShape.BaseAsset,
        [AssetType.ParticleEmitterProperty] = AssetShape.BaseAsset,
        [AssetType.ParticleSystem] = AssetShape.BaseAsset,
        [AssetType.PickupTypes] = AssetShape.BaseAsset,
        [AssetType.Portal] = AssetShape.BaseAsset,
        [AssetType.ProgressScript] = AssetShape.BaseAsset,
        [AssetType.Projectile] = AssetShape.BaseAsset,
        [AssetType.ReactiveAnimation] = AssetShape.BaseAsset,
        [AssetType.SceneSettings] = AssetShape.BaseAsset,
        [AssetType.Script] = AssetShape.BaseAsset,
        [AssetType.SlideProperty] = AssetShape.BaseAsset,
        [AssetType.SoundEffect] = AssetShape.BaseAsset,
        [AssetType.SoundFX] = AssetShape.BaseAsset,
        [AssetType.SoundGroup] = AssetShape.BaseAsset,
        [AssetType.Spline] = AssetShape.BaseAsset,
        [AssetType.SplinePath] = AssetShape.BaseAsset,
        [AssetType.Subtitles] = AssetShape.BaseAsset,
        [AssetType.Surface] = AssetShape.BaseAsset,
        [AssetType.ThrowableTable] = AssetShape.BaseAsset,
        [AssetType.Timer] = AssetShape.BaseAsset,
        [AssetType.UIMotion] = AssetShape.BaseAsset,
        [AssetType.Volume] = AssetShape.BaseAsset,
        [AssetType.ZipLine] = AssetShape.BaseAsset,
    };
}
