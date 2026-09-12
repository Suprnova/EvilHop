using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// A stationary object which may or may not be visible or collidable, optionally playing an
/// <see cref="AssetType.Animation"/>.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/SIMP">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class SimpleObjectAsset : EntityAsset, IHasModel, IHasAnimList, IHasSurface, IPhysicalSimpleObjectAsset
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
    /// This object's collision type. Shares its bit values with every other entity type's collision
    /// type, though only two are ever meaningful here:
    /// <list type="bullet">
    /// <item><c>0x00</c> (None) - no collision.</item>
    /// <item><c>0x02</c> (Static) - collision matching this object's model.</item>
    /// </list>
    /// The remaining bits (<c>0x01</c> Trigger, <c>0x04</c> Dynamic, <c>0x08</c> NPC, <c>0x10</c>
    /// Player) are part of the shared scheme but do not affect a <see cref="SimpleObjectAsset"/>, and
    /// have not been observed set here.
    /// </summary>
    public byte CollisionType { get; set; }

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalSimpleObjectAsset Physical => this;

    private byte _flags;
    byte IPhysicalSimpleObjectAsset.SimpleFlags { get => _flags; set => _flags = value; }

    AssetId IHasModel.ModelId { get => Physical.ModelId; set => Physical.ModelId = value; }
    AssetId IHasAnimList.AnimListId { get => Physical.AnimListId; set => Physical.AnimListId = value; }
    AssetId IHasSurface.SurfaceId { get => Physical.SurfaceId; set => Physical.SurfaceId = value; }

    internal SimpleObjectAsset() { }

    internal static SimpleObjectAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new SimpleObjectAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);
        EntityAssetPrefix.Read(asset, reader, profile.EntityHasPadding);

        asset.AnimationSpeed = reader.ReadSingle();
        asset.InitialAnimationState = reader.ReadUInt32();
        asset.CollisionType = reader.ReadByte();
        asset.Physical.SimpleFlags = reader.ReadByte();
        reader.ReadInt16(); // padding, always zero

        LinkSerialization.Read(asset, reader, asset.Physical.LinkCount);
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(SimpleObjectAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);
        EntityAssetPrefix.Write(asset, writer, profile.EntityHasPadding);

        writer.Write(asset.AnimationSpeed);
        writer.Write(asset.InitialAnimationState);
        writer.Write(asset.CollisionType);
        writer.Write(asset.Physical.SimpleFlags);
        writer.Write((short)0); // padding

        LinkSerialization.Write(asset, writer);
        writer.Write(asset.GetUnparsedTail());
    }
}

/// <summary>
/// An explicit interface used to interact with <see cref="SimpleObjectAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalSimpleObjectAsset : IPhysicalEntityAsset
{
    /// <summary>
    /// Unknown. Always 0 in <see cref="GameVersion.N100F"/> and <see cref="GameVersion.BFBB"/>, as
    /// the wiki claims; from <see cref="GameVersion.TSSM"/> onward, real archives commonly carry
    /// <c>0x08</c> and occasionally other bit patterns, with no explanation in available decompiled
    /// source.
    /// </summary>
    byte SimpleFlags { get; set; }
}
