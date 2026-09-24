using EvilHop.Common;
using EvilHop.Serialization;
using System.Collections.ObjectModel;

namespace EvilHop.Assets;

/// <summary>
/// Describes every sound in a level: the encoding, sample rate, and size of each
/// <see cref="AssetType.Sound"/>/<see cref="AssetType.StreamingSound"/> asset, or - on platforms where
/// sound data lives in this asset instead - the sounds themselves.
/// </summary>
/// <remarks>
/// On <see cref="Platform.Xbox"/> and <see cref="Platform.PlayStation2"/>, and on
/// <see cref="Platform.GameCube"/> for <see cref="GameVersion.N100F"/> and <see cref="GameVersion.BFBB"/>,
/// this asset holds one <see cref="SoundHeader"/> per SND/SNDS asset, in <see cref="Effects"/> and
/// <see cref="Streams"/>. On every other GameCube game, sounds are instead stored directly in this asset
/// as FMOD "FSB3" sample banks - see <see cref="SoundBanks"/> and <see cref="Sounds"/>.
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/Sound_Format">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed partial class SoundInfoAsset() : Asset(AssetType.SoundInfo), Physical.ISoundInfoAsset
{
    /// <summary>
    /// Headers for this level's sound effects, one per <see cref="AssetType.Sound"/> asset.
    /// </summary>
    /// <remarks>Empty on the FSB3-embedded layout.</remarks>
    public Collection<SoundHeader> Effects { get; } = [];

    /// <summary>
    /// Headers for this level's streaming sounds (voice lines and music), one per
    /// <see cref="AssetType.StreamingSound"/> asset.
    /// </summary>
    /// <remarks>Empty on the FSB3-embedded layout.</remarks>
    public Collection<SoundHeader> Streams { get; } = [];

    /// <summary>
    /// Headers linking to this level's cutscene audio, one per <see cref="AssetType.CutsceneStreamingSound"/>
    /// asset.
    /// </summary>
    /// <remarks>
    /// Only present on <see cref="Platform.Xbox"/>, on <see cref="Platform.GameCube"/> for
    /// <see cref="GameVersion.BFBB"/>, and in the FSB3-embedded layout.
    /// </remarks>
    public Collection<SoundHeader> Cutscenes { get; } = [];

    /// <summary>
    /// The raw FMOD "FSB3" sample bank files embedded directly in this asset. The first bank is
    /// loaded into RAM in full and may hold multiple sounds; every subsequent bank is streamed from
    /// disk and holds exactly one sound.
    /// </summary>
    /// <remarks>
    /// EvilHop does not parse FSB3 - each entry is exactly the bytes of one bank file, byte-exact but
    /// opaque. Populated for every GameCube-supported game other than <see cref="GameVersion.N100F"/>
    /// and <see cref="GameVersion.BFBB"/>.
    /// </remarks>
    public Collection<byte[]> SoundBanks { get; } = [];

    /// <summary>
    /// Metadata for every sound stored across <see cref="SoundBanks"/>.
    /// </summary>
    /// <remarks>Populated alongside <see cref="SoundBanks"/>.</remarks>
    public Collection<Sound> Sounds { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override Physical.ISoundInfoAsset Physical => this;

    private AssetId? _overriddenSoundInfoId;
    AssetId Physical.ISoundInfoAsset.SoundInfoId
    {
        get => _overriddenSoundInfoId ?? Id;
        set => _overriddenSoundInfoId = value == Id ? null : value;
    }

    private int? _overriddenEffectCount;
    int Physical.ISoundInfoAsset.EffectCount
    {
        get => _overriddenEffectCount ?? Effects.Count;
        set => _overriddenEffectCount = value == Effects.Count ? null : value;
    }

    private int? _overriddenStreamCount;
    int Physical.ISoundInfoAsset.StreamCount
    {
        get => _overriddenStreamCount ?? Streams.Count;
        set => _overriddenStreamCount = value == Streams.Count ? null : value;
    }

    private int? _overriddenCutsceneCount;
    int Physical.ISoundInfoAsset.CutsceneCount
    {
        get => _overriddenCutsceneCount ?? Cutscenes.Count;
        set => _overriddenCutsceneCount = value == Cutscenes.Count ? null : value;
    }

    private int? _overriddenSoundBankCount;
    int Physical.ISoundInfoAsset.SoundBankCount
    {
        get => _overriddenSoundBankCount ?? SoundBanks.Count;
        set => _overriddenSoundBankCount = value == SoundBanks.Count ? null : value;
    }

    private int? _overriddenSoundCount;
    int Physical.ISoundInfoAsset.SoundCount
    {
        get => _overriddenSoundCount ?? Sounds.Count;
        set => _overriddenSoundCount = value == Sounds.Count ? null : value;
    }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.SoundInfo"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.N100F,
        GameVersion.BFBB,
        GameVersion.TSSM,
        GameVersion.Incredibles,
        GameVersion.ROTU,
        GameVersion.Ratatouille,
    };
}

public static partial class Physical
{
    /// <summary>
    /// An explicit interface used to interact with <see cref="SoundInfoAsset"/>'s underlying values.
    /// </summary>
    public interface ISoundInfoAsset : IAsset
    {
        /// <summary>
        /// This asset's own ID, read directly from its leading header field.
        /// </summary>
        /// <remarks>
        /// Only meaningful for the FSB3-embedded layout, and on <see cref="Platform.PlayStation2"/> for
        /// <see cref="GameVersion.Incredibles"/> and <see cref="GameVersion.ROTU"/>. When disagreements with
        /// <see cref="Asset.Id"/> exist, this field wins during serialization.
        /// </remarks>
        AssetId SoundInfoId { get; set; }

        /// <summary>
        /// The number of <see cref="SoundInfoAsset.Effects"/> stored for this asset, read directly from
        /// its leading count field.
        /// </summary>
        /// <remarks>
        /// Only meaningful for the header-table layout. When disagreements with
        /// <see cref="SoundInfoAsset.Effects"/>.Count exist, this field wins during serialization.
        /// </remarks>
        int EffectCount { get; set; }

        /// <summary>
        /// The number of <see cref="SoundInfoAsset.Streams"/> stored for this asset, read directly from
        /// its leading count field.
        /// </summary>
        /// <remarks>
        /// Only meaningful for the header-table layout. When disagreements with
        /// <see cref="SoundInfoAsset.Streams"/>.Count exist, this field wins during serialization.
        /// </remarks>
        int StreamCount { get; set; }

        /// <summary>
        /// The number of <see cref="SoundInfoAsset.Cutscenes"/> stored for this asset, read directly from
        /// its leading count field.
        /// </summary>
        /// <remarks>
        /// When disagreements with <see cref="SoundInfoAsset.Cutscenes"/>.Count exist, this field wins
        /// during serialization.
        /// </remarks>
        int CutsceneCount { get; set; }

        /// <summary>
        /// The number of <see cref="SoundInfoAsset.SoundBanks"/> stored for this asset, read directly
        /// from its FSB3-embedded layout's header.
        /// </summary>
        /// <remarks>
        /// Only meaningful for the FSB3-embedded layout. When disagreements with
        /// <see cref="SoundInfoAsset.SoundBanks"/>.Count exist, this field wins during serialization.
        /// </remarks>
        int SoundBankCount { get; set; }

        /// <summary>
        /// The number of <see cref="SoundInfoAsset.Sounds"/> stored for this asset, read directly from
        /// its FSB3-embedded layout's header.
        /// </summary>
        /// <remarks>
        /// Only meaningful for the FSB3-embedded layout. When disagreements with
        /// <see cref="SoundInfoAsset.Sounds"/>.Count exist, this field wins during serialization.
        /// </remarks>
        int SoundCount { get; set; }
    }
}
