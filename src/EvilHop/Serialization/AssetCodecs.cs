using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;

namespace EvilHop.Serialization;

/// <summary>
/// Maps each <see cref="AssetType"/> to the codec that reads and writes it.
/// </summary>
/// <remarks>
/// Every known type is seeded at static construction with a generic handler for its shape. Registering
/// a concrete codec overwrites that entry, mirroring
/// <see cref="Serializer.RegisterBlock{T}(Action{EndianReader, T, uint}?, Action{EndianWriter, T}?)"/>.
/// </remarks>
internal static class AssetCodecs
{
    /// <summary>
    /// Reads one asset from a reader scoped to its slice of the asset stream.
    /// </summary>
    internal delegate Asset ReadFunc(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile);

    /// <summary>
    /// Writes one asset's data.
    /// </summary>
    internal delegate void WriteFunc(Asset asset, EndianWriter writer, FormatProfile profile);

    /// <summary>
    /// Reads one asset of type <typeparamref name="T"/> from a reader scoped to its slice of the
    /// asset stream.
    /// </summary>
    internal delegate T ReadFunc<out T>(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
        where T : Asset;

    /// <summary>
    /// Writes one asset of type <typeparamref name="T"/>'s data.
    /// </summary>
    internal delegate void WriteFunc<in T>(T asset, EndianWriter writer, FormatProfile profile)
        where T : Asset;

    private readonly record struct CodecHandler(ReadFunc Read, WriteFunc Write, IReadOnlySet<GameVersion>? Games = null);

    private static readonly Dictionary<AssetType, CodecHandler> Handlers = [];

    /// <summary>
    /// Used for any <see cref="AssetType"/> with no entry at all.
    /// </summary>
    private static readonly CodecHandler Fallback = new(ReadPlain, WritePlain);

    static AssetCodecs()
    {
        RegisterGenericShapes();
        RegisterConcreteCodecs();
    }

    /// <summary>
    /// Registers every asset type with a real, hand-written codec. Each entry just points at its
    /// type's own <c>Read</c>/<c>Write</c> pair - the logic, including any per-game branching,
    /// lives with the asset class itself, not here.
    /// </summary>
    private static void RegisterConcreteCodecs()
    {
        Register(AssetType.Counter, CounterAsset.Read, CounterAsset.Write);
        Register(AssetType.Marker, MarkerAsset.Read, MarkerAsset.Write, MarkerAsset.SupportedGames);
    }

    /// <summary>
    /// Registers <typeparamref name="T"/>'s codec for <paramref name="type"/>, replacing whatever
    /// was there.
    /// </summary>
    /// <typeparam name="T">The <see cref="Asset"/> type this codec produces.</typeparam>
    /// <param name="type">The <see cref="AssetType"/> to register against.</param>
    /// <param name="read">Reads an asset of this type.</param>
    /// <param name="write">Writes an asset of this type.</param>
    /// <param name="games">
    /// Which <see cref="GameVersion"/>s <paramref name="read"/> applies to, or <see langword="null"/>
    /// if every game does.
    /// </param>
    public static void Register<T>(AssetType type, ReadFunc<T> read, WriteFunc<T> write, IReadOnlySet<GameVersion>? games = null) where T : Asset =>
        Handlers[type] = new CodecHandler(
            (data, header, debug, profile) => read(data, header, debug, profile),
            Guarded(write),
            games);

    /// <summary>
    /// Reads one asset, dispatching based on <see cref="AssetHeader.Type"/> and
    /// <see cref="FormatProfile.Game"/>.
    /// </summary>
    public static Asset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile) =>
        ResolveRead(header.Type, profile.Game)(reader, header, debug, profile);

    /// <summary>
    /// Writes one asset's data, dispatching based on its <see cref="Asset.Type"/>.
    /// </summary>
    /// <remarks>
    /// Dispatches on the logical <see cref="Asset.Type"/>, not <c>Physical.Type</c>.
    /// </remarks>
    public static void Write(Asset asset, EndianWriter writer, FormatProfile profile) =>
        Resolve(asset.Type).Write(asset, writer, profile);

    private static CodecHandler Resolve(AssetType type) =>
        Handlers.TryGetValue(type, out var handler) ? handler : Fallback;

    /// <summary>
    /// Resolves the read function for <paramref name="type"/> under <paramref name="game"/>, falling
    /// back to <paramref name="type"/>'s shape-generic reader - or <see cref="Fallback"/> - when its
    /// registered codec declares <paramref name="game"/> unsupported.
    /// </summary>
    private static ReadFunc ResolveRead(AssetType type, GameVersion game)
    {
        var handler = Resolve(type);
        return handler.Games is null || handler.Games.Contains(game)
            ? handler.Read
            : ShapeHandlerFor(type).Read;
    }

    private static void RegisterGenericShapes()
    {
        foreach (var (type, shape) in ShapesByType)
            Handlers[type] = ShapeHandler(shape);
    }

    private static CodecHandler ShapeHandlerFor(AssetType type) =>
        ShapesByType.TryGetValue(type, out var shape) ? ShapeHandler(shape) : Fallback;

    private static CodecHandler ShapeHandler(AssetShape shape) => shape switch
    {
        AssetShape.BaseAsset => new CodecHandler(ReadBase, Guarded<BaseAsset>(WriteBase)),
        AssetShape.EntityAsset => new CodecHandler(ReadEntity, Guarded<EntityAsset>(WriteEntity)),
        AssetShape.DynaAsset => new CodecHandler(ReadDyna, Guarded<DynaAsset>(WriteDyna)),
        AssetShape.Payload => new CodecHandler(ReadPayload, Guarded<PayloadAsset>(WritePayload)),
        _ => Fallback
    };

    /// <summary>
    /// Wraps a shape-specific writer so a shape mismatch degrades to writing by the asset's own
    /// runtime shape.
    /// </summary>
    private static WriteFunc Guarded<T>(WriteFunc<T> write) where T : Asset =>
        (asset, writer, profile) =>
        {
            if (asset is T typed) write(typed, writer, profile);
            else WriteByShape(asset, writer, profile);
        };

    /// <summary>
    /// Writes <paramref name="asset"/> using the generic writer for its own runtime shape, ignoring
    /// whichever <see cref="AssetType"/>-keyed handler would otherwise apply.
    /// </summary>
    private static void WriteByShape(Asset asset, EndianWriter writer, FormatProfile profile)
    {
        switch (asset)
        {
            case EntityAsset entity: WriteEntity(entity, writer, profile); break;
            case DynaAsset dyna: WriteDyna(dyna, writer, profile); break;
            case BaseAsset baseAsset: WriteBase(baseAsset, writer, profile); break;
            case PayloadAsset payload: WritePayload(payload, writer, profile); break;
            default: WritePlain(asset, writer, profile); break;
        }
    }

    /// <summary>
    /// Populates <paramref name="asset"/>'s header-sourced fields and reads its
    /// <see cref="BaseAssetPrefix"/> - the two steps every <see cref="BaseAsset"/>-shaped codec
    /// starts with, regardless of what follows.
    /// </summary>
    /// <remarks>
    /// <see cref="BaseAssetPrefix.Read"/> always sets <see cref="IPhysicalBaseAsset.LinkCount"/> as
    /// an override, matching the "cannot locate them" half of that property's contract - none of
    /// the shapes below parse links into <see cref="BaseAsset.Links"/>. A future codec that does
    /// must not treat this helper's LinkCount as final.
    /// </remarks>
    private static T PopulateBase<T>(T asset, AssetHeader header, AssetDebug debug, EndianReader reader)
        where T : BaseAsset
    {
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);
        return asset;
    }

    private static GenericAsset ReadPlain(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new GenericAsset();
        AssetFields.Populate(asset, header, debug);
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    private static void WritePlain(Asset asset, EndianWriter writer, FormatProfile profile) =>
        writer.Write(asset.GetUnparsedTail());

    private static GenericBaseAsset ReadBase(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = PopulateBase(new GenericBaseAsset(), header, debug, reader);
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    private static void WriteBase(BaseAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);
        writer.Write(asset.GetUnparsedTail());
    }

    private static GenericEntityAsset ReadEntity(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = PopulateBase(new GenericEntityAsset(), header, debug, reader);
        EntityAssetPrefix.Read(asset, reader, profile.EntityHasPadding);
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    private static void WriteEntity(EntityAsset entity, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(entity, writer);
        EntityAssetPrefix.Write(entity, writer, profile.EntityHasPadding);
        writer.Write(entity.GetUnparsedTail());
    }

    private static GenericDynaAsset ReadDyna(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = PopulateBase(new GenericDynaAsset(), header, debug, reader);
        DynaAssetPrefix.Read(asset, reader);
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    private static void WriteDyna(DynaAsset dyna, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(dyna, writer);
        DynaAssetPrefix.Write(dyna, writer);
        writer.Write(dyna.GetUnparsedTail());
    }

    private static GenericPayloadAsset ReadPayload(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new GenericPayloadAsset();
        AssetFields.Populate(asset, header, debug);
        asset.Data = reader.ReadRemainingBytes();
        return asset;
    }

    private static void WritePayload(PayloadAsset asset, EndianWriter writer, FormatProfile profile) =>
        writer.Write(asset.Data);

    private enum AssetShape { BaseAsset, EntityAsset, DynaAsset, Payload }

    /// <summary>
    /// Which shape each known <see cref="AssetType"/> follows.
    /// </summary>
    /// <remarks>
    /// A type absent from this table has no known shape and falls through to <see cref="Fallback"/>.
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
