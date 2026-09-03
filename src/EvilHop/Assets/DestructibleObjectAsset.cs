using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// An <see cref="EntityAsset"/> that can be damaged and destroyed, optionally spawning shrapnel,
/// playing sound effects, or swapping to a replacement model along the way.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/DSTR">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class DestructibleObjectAsset : EntityAsset, IHasModel, IHasAnimList
{
    /// <summary>
    /// The playback speed of the animation referenced by <see cref="IHasAnimList.AnimListId"/>.
    /// </summary>
    public float AnimationSpeed { get; set; }

    /// <summary>
    /// The animation state this object starts in.
    /// </summary>
    public uint InitialAnimationState { get; set; }

    /// <summary>
    /// The number of hits this object can take before it is destroyed. Always 1 in known files.
    /// </summary>
    public uint Health { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the item spawned when this object is destroyed, if any.
    /// </summary>
    public AssetId SpawnItemId { get; set; }

    /// <summary>
    /// Which kinds of hits this object reacts to.
    /// </summary>
    public DestructibleHitFlags HitFlags { get; set; }

    /// <summary>
    /// This object's collision type, separate from <see cref="IPhysicalEntityAsset.CollisionFlags"/>.
    /// Usually 0 (dynamic) or 2 (static) - bit 1 gates static collision checks in decompiled source.
    /// </summary>
    public byte CollisionType { get; set; }

    /// <summary>
    /// Which particle effect plays when this object is destroyed.
    /// </summary>
    public DestructibleFxType FxType { get; set; }

    /// <summary>
    /// The radius, in world units, of the blast damage dealt when this object is destroyed.
    /// </summary>
    public float BlastRadius { get; set; }

    /// <summary>
    /// The strength of the blast damage dealt when this object is destroyed.
    /// </summary>
    public float BlastStrength { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the <see cref="AssetType.Shrapnel"/> spawned when this object is
    /// destroyed, if any. Not present in <see cref="GameVersion.N100F"/>.
    /// </summary>
    public AssetId DestroyShrapnelId { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the <see cref="AssetType.Shrapnel"/> spawned when this object is
    /// hit but not destroyed, if any. Not present in <see cref="GameVersion.N100F"/>.
    /// </summary>
    public AssetId HitShrapnelId { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the sound effect played when this object is destroyed, if any.
    /// Not present in <see cref="GameVersion.N100F"/>.
    /// </summary>
    public AssetId DestroySfxId { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the sound effect played when this object is hit but not
    /// destroyed, if any. Not present in <see cref="GameVersion.N100F"/>.
    /// </summary>
    public AssetId HitSfxId { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the <see cref="AssetType.Model"/> this object swaps to when hit
    /// but not destroyed, if any. Not present in <see cref="GameVersion.N100F"/>.
    /// </summary>
    public AssetId HitModelId { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the <see cref="AssetType.Model"/> this object swaps to when
    /// destroyed, if any. Not present in <see cref="GameVersion.N100F"/>.
    /// </summary>
    public AssetId DestroyModelId { get; set; }

    AssetId IHasModel.ModelId { get => Physical.ModelId; set => Physical.ModelId = value; }
    AssetId IHasAnimList.AnimListId { get => Physical.AnimListId; set => Physical.AnimListId = value; }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.DestructibleObject"/> is known to be read
    /// by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.N100F,
        GameVersion.BFBB,
    };

    internal DestructibleObjectAsset() { }

    internal static DestructibleObjectAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new DestructibleObjectAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);
        EntityAssetPrefix.Read(asset, reader, profile.EntityHasPadding);

        asset.AnimationSpeed = reader.ReadSingle();
        asset.InitialAnimationState = reader.ReadUInt32();
        asset.Health = reader.ReadUInt32();
        asset.SpawnItemId = reader.ReadAssetId();
        asset.HitFlags = (DestructibleHitFlags)reader.ReadUInt32();
        asset.CollisionType = reader.ReadByte();
        asset.FxType = (DestructibleFxType)reader.ReadByte();
        reader.ReadInt16(); // 2 bytes of padding, always zero
        asset.BlastRadius = reader.ReadSingle();
        asset.BlastStrength = reader.ReadSingle();

        if (profile.Game == GameVersion.BFBB)
        {
            asset.DestroyShrapnelId = reader.ReadAssetId();
            asset.HitShrapnelId = reader.ReadAssetId();
            asset.DestroySfxId = reader.ReadAssetId();
            asset.HitSfxId = reader.ReadAssetId();
            asset.HitModelId = reader.ReadAssetId();
            asset.DestroyModelId = reader.ReadAssetId();
        }

        LinkSerialization.Read(asset, reader, asset.Physical.LinkCount);
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(DestructibleObjectAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);
        EntityAssetPrefix.Write(asset, writer, profile.EntityHasPadding);

        writer.Write(asset.AnimationSpeed);
        writer.Write(asset.InitialAnimationState);
        writer.Write(asset.Health);
        writer.Write(asset.SpawnItemId);
        writer.Write((uint)asset.HitFlags);
        writer.Write(asset.CollisionType);
        writer.Write((byte)asset.FxType);
        writer.Write((short)0); // padding
        writer.Write(asset.BlastRadius);
        writer.Write(asset.BlastStrength);

        if (profile.Game == GameVersion.BFBB)
        {
            writer.Write(asset.DestroyShrapnelId);
            writer.Write(asset.HitShrapnelId);
            writer.Write(asset.DestroySfxId);
            writer.Write(asset.HitSfxId);
            writer.Write(asset.HitModelId);
            writer.Write(asset.DestroyModelId);
        }

        LinkSerialization.Write(asset, writer);
        writer.Write(asset.GetUnparsedTail());
    }
}

/// <summary>
/// Represents all known values for <see cref="DestructibleObjectAsset.HitFlags"/>.
/// </summary>
[Flags]
public enum DestructibleHitFlags : uint
{
    /// <summary>
    /// No flags are set.
    /// </summary>
    None = 0,
    /// <summary>
    /// Reacts to Patrick's slam attack. <see cref="GameVersion.BFBB"/> only.
    /// </summary>
    PatrickSlam = 1 << 10,
    /// <summary>
    /// Reacts to being thrown. <see cref="GameVersion.BFBB"/> only.
    /// </summary>
    Throw = 1 << 11,
    /// <summary>
    /// Reacts to a bubble bounce. <see cref="GameVersion.BFBB"/> only.
    /// </summary>
    BubbleBounce = 1 << 13,
    /// <summary>
    /// Reacts to a bubble bash attack. <see cref="GameVersion.BFBB"/> only.
    /// </summary>
    BubbleBash = 1 << 14,
    /// <summary>
    /// Unknown. Plays a random hit sound stream when set alongside a reacted-to hit.
    /// <see cref="GameVersion.BFBB"/> only.
    /// </summary>
    Unknown = 1 << 15,
}

/// <summary>
/// Represents all known values for <see cref="DestructibleObjectAsset.FxType"/>.
/// </summary>
public enum DestructibleFxType : byte
{
    /// <summary>
    /// No effect plays.
    /// </summary>
    None = 0,
    /// <summary>
    /// A dust cloud effect plays.
    /// </summary>
    Dust = 1,
    /// <summary>
    /// An explosion effect plays.
    /// </summary>
    Explosion = 2,
    /// <summary>
    /// A web effect plays.
    /// </summary>
    Web = 3,
}
