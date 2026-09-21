using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>
/// Plays a <see cref="AssetType.Sound"/> or <see cref="AssetType.StreamingSound"/>, either positionally
/// (from a fixed <see cref="Position"/> or from <see cref="AttachId"/>'s live position) or flat, in
/// response to a <see cref="BaseAsset.Links"/> event.
/// </summary>
/// <remarks>
/// Superseded by <see cref="SoundEffectAsset"/> from <see cref="GameVersion.TSSM"/> onward.
/// <seealso href="https://heavyironmodding.org/wiki/SFX">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class SoundFXAsset() : BaseAsset(AssetType.SoundFX, baseType: 0x13), IPhysicalSoundFXAsset
{
    /// <summary>
    /// A base pitch value, multiplied by <see cref="FrequencyMultiplier"/> and passed to the sound
    /// engine as this sound's playback frequency.
    /// </summary>
    public ushort Frequency { get; set; }

    /// <summary>A multiplier applied to <see cref="Frequency"/>.</summary>
    public float FrequencyMultiplier { get; set; }

    /// <summary>The <see cref="AssetType.Sound"/> or <see cref="AssetType.StreamingSound"/> played by this sound.</summary>
    public AssetId SoundId { get; set; }

    /// <summary>The entity this sound plays from when <see cref="Positional"/> and <see cref="PlayFromEntity"/> are both set.</summary>
    public AssetId AttachId { get; set; }

    /// <summary>This sound's playback priority, passed directly to the sound engine.</summary>
    public byte Priority { get; set; }

    /// <summary>Playback volume, from 0 to 100.</summary>
    public byte Volume { get; set; }

    /// <summary>The position this sound plays from when <see cref="Positional"/> is set and <see cref="PlayFromEntity"/> is not.</summary>
    public Vector3 Position { get; set; }

    /// <summary>The radius, when <see cref="Positional"/> is set, inside which this sound plays at maximum volume.</summary>
    public float InnerRadius { get; set; }

    /// <summary>The radius, when <see cref="Positional"/> is set, beyond which this sound stops playing.</summary>
    public float OuterRadius { get; set; }

    /// <summary>
    /// Whether this sound plays from a 3D position - <see cref="AttachId"/>'s live position or the
    /// fixed <see cref="Position"/> - rather than flat.
    /// </summary>
    public bool Positional
    {
        get => Physical.SFXFlags.HasFlag(SFXFlags.Positional);
        set => SetFlag(SFXFlags.Positional, value);
    }

    /// <summary>
    /// Whether this sound plays from <see cref="AttachId"/>'s live position, instead of the fixed
    /// <see cref="Position"/>, when <see cref="Positional"/> is set.
    /// </summary>
    public bool PlayFromEntity
    {
        get => Physical.SFXFlags.HasFlag(SFXFlags.PlayFromEntity);
        set => SetFlag(SFXFlags.PlayFromEntity, value);
    }

    /// <summary>Whether this sound loops.</summary>
    public bool Loop
    {
        get => Physical.SFXFlags.HasFlag(SFXFlags.Loop);
        set => SetFlag(SFXFlags.Loop, value);
    }

    private void SetFlag(SFXFlags bit, bool value) =>
        Physical.SFXFlags = value ? Physical.SFXFlags | bit : Physical.SFXFlags & ~bit;

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalSoundFXAsset Physical => this;

    private SFXFlags _sfxFlags;
    SFXFlags IPhysicalSoundFXAsset.SFXFlags { get => _sfxFlags; set => _sfxFlags = value; }

    private byte _loopCount;
    byte IPhysicalSoundFXAsset.LoopCount { get => _loopCount; set => _loopCount = value; }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.SoundFX"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.N100F,
        GameVersion.BFBB,
    };

    internal static SoundFXAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new SoundFXAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        asset.Physical.SFXFlags = (SFXFlags)reader.ReadUInt16();
        asset.Frequency = reader.ReadUInt16();
        asset.FrequencyMultiplier = reader.ReadSingle();
        asset.SoundId = reader.ReadAssetId();
        asset.AttachId = reader.ReadAssetId();
        asset.Physical.LoopCount = reader.ReadByte();
        asset.Priority = reader.ReadByte();
        asset.Volume = reader.ReadByte();
        reader.ReadByte(); // padding, always zero
        asset.Position = reader.ReadVector3();
        asset.InnerRadius = reader.ReadSingle();
        asset.OuterRadius = reader.ReadSingle();

        for (var i = 0; i < asset.Physical.LinkCount; i++)
            asset.Links.Add(Link.Read(reader, profile));
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(SoundFXAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);

        writer.Write((ushort)asset.Physical.SFXFlags);
        writer.Write(asset.Frequency);
        writer.Write(asset.FrequencyMultiplier);
        writer.Write(asset.SoundId);
        writer.Write(asset.AttachId);
        writer.Write(asset.Physical.LoopCount);
        writer.Write(asset.Priority);
        writer.Write(asset.Volume);
        writer.Write((byte)0); // padding
        writer.Write(asset.Position);
        writer.Write(asset.InnerRadius);
        writer.Write(asset.OuterRadius);

        foreach (var link in asset.Links)
            Link.Write(link, writer, profile);
        writer.Write(asset.GetUnparsedTail());
    }
}

/// <summary>
/// An explicit interface used to interact with <see cref="SoundFXAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalSoundFXAsset : IPhysicalBaseAsset
{
    /// <summary>The sound's raw flags word, read directly from disk.</summary>
    /// <remarks>
    /// <see cref="SFXFlags.Positional"/>, <see cref="SFXFlags.Loop"/>, and
    /// <see cref="SFXFlags.PlayFromEntity"/> are exposed logically as
    /// <see cref="SoundFXAsset.Positional"/>, <see cref="SoundFXAsset.Loop"/>, and
    /// <see cref="SoundFXAsset.PlayFromEntity"/> respectively.
    /// </remarks>
    SFXFlags SFXFlags { get; set; }

    /// <summary>Unknown.</summary>
    byte LoopCount { get; set; }
}

/// <summary>
/// Flags controlling playback and spatialization for a <see cref="SoundFXAsset"/>.
/// </summary>
[Flags]
public enum SFXFlags : ushort
{
    /// <summary>No flags are set.</summary>
    None = 0,

    /// <summary>
    /// The sound plays from a 3D position - <see cref="SoundFXAsset.AttachId"/>'s live position or
    /// the fixed <see cref="SoundFXAsset.Position"/> - rather than flat.
    /// </summary>
    Positional = 1 << 1,

    /// <summary>The sound loops.</summary>
    Loop = 1 << 2,

    /// <summary>
    /// The sound plays from <see cref="SoundFXAsset.AttachId"/>'s live position, instead of the fixed
    /// <see cref="SoundFXAsset.Position"/>, when <see cref="Positional"/> is set.
    /// </summary>
    PlayFromEntity = 1 << 3,
}
