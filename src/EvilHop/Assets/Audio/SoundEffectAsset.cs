using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>
/// Plays a <see cref="AssetType.SoundGroup"/>, either from a fixed <see cref="Position"/> or from
/// <see cref="AttachId"/>'s live position when <see cref="PlayFromEntity"/> is set.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/SDFX">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class SoundEffectAsset() : BaseAsset(AssetType.SoundEffect), IPhysicalSoundEffectAsset
{
    /// <summary>The <see cref="AssetType.SoundGroup"/> played by this sound.</summary>
    public AssetId SoundGroupId { get; set; }

    /// <summary>The entity this sound plays from when <see cref="PlayFromEntity"/> is set.</summary>
    public AssetId AttachId { get; set; }

    /// <summary>The position this sound plays from when <see cref="PlayFromEntity"/> is not set.</summary>
    public Vector3 Position { get; set; }

    /// <summary>
    /// Whether this sound plays from <see cref="AttachId"/>'s live position, instead of the fixed
    /// <see cref="Position"/>.
    /// </summary>
    public bool PlayFromEntity
    {
        get => (Physical.SoundFlags & 0x4) != 0;
        set => Physical.SoundFlags = value ? Physical.SoundFlags | 0x4u : Physical.SoundFlags & ~0x4u;
    }

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalSoundEffectAsset Physical => this;

    private uint _soundFlags;
    uint IPhysicalSoundEffectAsset.SoundFlags { get => _soundFlags; set => _soundFlags = value; }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.SoundEffect"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.TSSM,
        GameVersion.Incredibles,
        GameVersion.ROTU,
        GameVersion.Ratatouille,
    };

    internal static SoundEffectAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile _)
    {
        var asset = new SoundEffectAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        asset.SoundGroupId = reader.ReadAssetId();
        asset.AttachId = reader.ReadAssetId();
        asset.Position = reader.ReadVector3();
        asset.Physical.SoundFlags = reader.ReadUInt32();

        LinkSerialization.Read(asset, reader, asset.Physical.LinkCount);
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(SoundEffectAsset asset, EndianWriter writer, FormatProfile _)
    {
        BaseAssetPrefix.Write(asset, writer);

        writer.Write(asset.SoundGroupId);
        writer.Write(asset.AttachId);
        writer.Write(asset.Position);
        writer.Write(asset.Physical.SoundFlags);

        LinkSerialization.Write(asset, writer);
        writer.Write(asset.GetUnparsedTail());
    }
}

/// <summary>
/// An explicit interface used to interact with <see cref="SoundEffectAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalSoundEffectAsset : IPhysicalBaseAsset
{
    /// <summary>The sound's raw flags word, read directly from disk.</summary>
    /// <remarks>
    /// Bit 0x4 is exposed logically as <see cref="SoundEffectAsset.PlayFromEntity"/>. The remaining
    /// bits are pure runtime playback state - set once the sound starts or stops playing, sends its
    /// Done event, or resolves <see cref="SoundEffectAsset.SoundGroupId"/> to a handle - never touched
    /// at authoring time.
    /// </remarks>
    uint SoundFlags { get; set; }
}
