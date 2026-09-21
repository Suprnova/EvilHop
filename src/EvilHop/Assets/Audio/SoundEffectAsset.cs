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
public sealed class SoundEffectAsset() : BaseAsset(AssetType.SoundEffect, baseType: 0x4B), Physical.ISoundEffectAsset
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
        get => Physical.SoundFlags.HasFlag(SoundFlags.PlayFromEntity);
        set => Physical.SoundFlags = value
            ? Physical.SoundFlags | SoundFlags.PlayFromEntity
            : Physical.SoundFlags & ~SoundFlags.PlayFromEntity;
    }

    /// <inheritdoc cref="Asset.Physical"/>
    public override Physical.ISoundEffectAsset Physical => this;

    private SoundFlags _soundFlags;
    SoundFlags Physical.ISoundEffectAsset.SoundFlags { get => _soundFlags; set => _soundFlags = value; }

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

    internal static SoundEffectAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new SoundEffectAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        asset.SoundGroupId = reader.ReadAssetId();
        asset.AttachId = reader.ReadAssetId();
        asset.Position = reader.ReadVector3();
        asset.Physical.SoundFlags = (SoundFlags)reader.ReadUInt32();

        for (var i = 0; i < asset.Physical.LinkCount; i++)
            asset.Links.Add(Link.Read(reader, profile));
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(SoundEffectAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);

        writer.Write(asset.SoundGroupId);
        writer.Write(asset.AttachId);
        writer.Write(asset.Position);
        writer.Write((uint)asset.Physical.SoundFlags);

        foreach (var link in asset.Links)
            Link.Write(link, writer, profile);
        writer.Write(asset.GetUnparsedTail());
    }

    /// <summary>
    /// Flags controlling playback behavior for a <see cref="SoundEffectAsset"/>.
    /// </summary>
    [Flags]
    public enum SoundFlags : uint
    {
        /// <summary>No flags are set.</summary>
        None = 0,

        /// <summary>
        /// The sound plays from <see cref="AttachId"/>'s live position, instead of
        /// the fixed <see cref="Position"/>.
        /// </summary>
        PlayFromEntity = 1 << 2,
    }
}

public static partial class Physical
{
    /// <summary>
    /// An explicit interface used to interact with <see cref="SoundEffectAsset"/>'s underlying values.
    /// </summary>
    public interface ISoundEffectAsset : IBaseAsset
    {
        /// <summary>The sound's raw flags word, read directly from disk.</summary>
        /// <remarks>
        /// <see cref="SoundEffectAsset.SoundFlags.PlayFromEntity"/> is exposed logically as <see cref="SoundEffectAsset.PlayFromEntity"/>.
        /// </remarks>
        SoundEffectAsset.SoundFlags SoundFlags { get; set; }
    }
}
