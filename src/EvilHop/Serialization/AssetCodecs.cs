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

    private readonly record struct CodecHandler(ReadFunc Read, WriteFunc Write);

    private static readonly Dictionary<AssetType, CodecHandler> Handlers = [];

    /// <summary>
    /// Used for any <see cref="AssetType"/> with no entry at all.
    /// </summary>
    private static readonly CodecHandler Fallback = new(ReadPlain, WritePlain);

    static AssetCodecs() => RegisterGenericShapes();

    /// <summary>
    /// Registers <typeparamref name="T"/>'s codec for <paramref name="type"/>, replacing whatever
    /// was there.
    /// </summary>
    /// <typeparam name="T">The <see cref="Asset"/> type this codec produces.</typeparam>
    /// <param name="type">The <see cref="AssetType"/> to register against.</param>
    /// <param name="read">Reads an asset of this type.</param>
    /// <param name="write">Writes an asset of this type.</param>
    /// TODO: per-game filtering - a codec declaring which <see cref="GameVersion"/>s it supports -
    /// arrives with the first concrete codec, where its semantics can be settled against a real
    /// case rather than guessed at.
    public static void Register<T>(AssetType type, ReadFunc<T> read, WriteFunc<T> write) where T : Asset =>
        Handlers[type] = new CodecHandler(
            (data, header, debug, profile) => read(data, header, debug, profile),
            (asset, writer, profile) => write((T)asset, writer, profile));

    /// <summary>
    /// Reads one asset, dispatching based on <see cref="AssetHeader.Type"/>.
    /// </summary>
    public static Asset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile) =>
        Resolve(header.Type).Read(reader, header, debug, profile);

    /// <summary>
    /// Writes one asset's data, dispatching based on its <see cref="Asset.Type"/>.
    /// </summary>
    /// <remarks>
    /// Dispatches on the logical <see cref="Asset.Type"/>, not <c>Physical.Type</c>.
    /// </remarks>
    public static void Write(Asset asset, EndianWriter writer, FormatProfile profile) =>
        Resolve(asset.Type).Write(asset, writer, profile);

    // TODO: should this dispatch on typeof() or "asset is TypedAsset"?
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

    private static Asset ReadPlain(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new GenericAsset();
        AssetFields.Populate(asset, header, debug);
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    private static void WritePlain(Asset asset, EndianWriter writer, FormatProfile profile) =>
        writer.Write(asset.GetUnparsedTail());

    private static Asset ReadBase(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = PopulateBase(new GenericBaseAsset(), header, debug, reader);
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    private static void WriteBase(Asset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write((BaseAsset)asset, writer);
        writer.Write(asset.GetUnparsedTail());
    }

    private static Asset ReadEntity(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = PopulateBase(new GenericEntityAsset(), header, debug, reader);
        EntityAssetPrefix.Read(asset, reader, profile.EntityHasPadding);
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    private static void WriteEntity(Asset asset, EndianWriter writer, FormatProfile profile)
    {
        var entity = (EntityAsset)asset;
        BaseAssetPrefix.Write(entity, writer);
        EntityAssetPrefix.Write(entity, writer, profile.EntityHasPadding);
        writer.Write(asset.GetUnparsedTail());
    }

    private static Asset ReadDyna(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = PopulateBase(new GenericDynaAsset(), header, debug, reader);
        DynaAssetPrefix.Read(asset, reader);
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    private static void WriteDyna(Asset asset, EndianWriter writer, FormatProfile profile)
    {
        var dyna = (DynaAsset)asset;
        BaseAssetPrefix.Write(dyna, writer);
        DynaAssetPrefix.Write(dyna, writer);
        writer.Write(asset.GetUnparsedTail());
    }

    private static Asset ReadPayload(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new GenericPayloadAsset();
        AssetFields.Populate(asset, header, debug);
        asset.Data = reader.ReadRemainingBytes();
        return asset;
    }

    private static void WritePayload(Asset asset, EndianWriter writer, FormatProfile profile) =>
        writer.Write(((PayloadAsset)asset).Data);

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
