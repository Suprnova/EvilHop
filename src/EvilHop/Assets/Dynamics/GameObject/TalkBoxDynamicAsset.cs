using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="DynamicAsset"/> that runs a conversation: it plays <see cref="AssetType.Text"/> through
/// a dialog box, optionally trapping the player and prompting them to continue, answer, or quit.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/DYNA/game_object:talk_box">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed partial class TalkBoxDynamicAsset() : DynamicAsset(version: 11), Physical.ITalkBoxDynamicAsset
{
    /// <inheritdoc/>
    public override DynamicKind Kind => DynamicKind.TalkBox;

    /// <summary>
    /// The <see cref="AssetId"/> of the <see cref="DynamicKind.TextBox"/> the conversation's text is
    /// shown in.
    /// </summary>
    public AssetId DialogBoxId { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the <see cref="DynamicKind.TextBox"/> prompts are shown in, if
    /// any.
    /// </summary>
    public AssetId PromptBoxId { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the <see cref="DynamicKind.TextBox"/> <see cref="QuitPromptId"/>
    /// and <see cref="NoQuitPromptId"/> are shown in, if any.
    /// </summary>
    public AssetId QuitBoxId { get; set; }

    /// <summary>
    /// Whether the player loses control for the duration of the conversation. Also stops the
    /// player's streaming sound when the conversation starts.
    /// </summary>
    public bool TrapsPlayer { get; set; }

    /// <summary>Whether the player can quit the conversation early.</summary>
    public bool AllowQuit { get; set; }

    /// <summary>When the player's button presses are passed on as events while talking.</summary>
    public PadTriggerMode TriggerPads { get; set; }

    /// <summary>What happens to the audio while the conversation runs.</summary>
    public AudioEffectKind AudioEffect { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the <see cref="DynamicKind.Pointer"/> (or location) the player is
    /// moved to by a <c>{teleport}</c> tag in the conversation's text that names no target of its
    /// own, if any.
    /// </summary>
    public AssetId TeleportTargetId { get; set; }

    /// <summary>What the conversation waits on before advancing, unless its text says otherwise.</summary>
    public AutoWaitSettings AutoWait { get; set; } = new();

    /// <summary>
    /// The <see cref="AssetId"/> of the <see cref="AssetType.Text"/> prompting the player to continue,
    /// while they can.
    /// </summary>
    public AssetId SkipPromptId { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the <see cref="AssetType.Text"/> shown in place of
    /// <see cref="SkipPromptId"/> while the player can't continue yet.
    /// </summary>
    public AssetId NoSkipPromptId { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the <see cref="AssetType.Text"/> prompting the player that they
    /// can quit, shown while <see cref="AllowQuit"/> is set.
    /// </summary>
    public AssetId QuitPromptId { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the <see cref="AssetType.Text"/> shown in place of
    /// <see cref="QuitPromptId"/> while the player can't quit.
    /// </summary>
    public AssetId NoQuitPromptId { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the <see cref="AssetType.Text"/> prompting the player to answer
    /// yes or no.
    /// </summary>
    public AssetId YesNoPromptId { get; set; }

    /// <inheritdoc cref="Asset.Physical"/>
    public override Physical.ITalkBoxDynamicAsset Physical => this;

    private byte _pause;
    byte Physical.ITalkBoxDynamicAsset.Pause { get => _pause; set => _pause = value; }

    private byte _page;
    byte Physical.ITalkBoxDynamicAsset.Page { get => _page; set => _page = value; }

    private byte _show;
    byte Physical.ITalkBoxDynamicAsset.Show { get => _show; set => _show = value; }

    private byte _hide;
    byte Physical.ITalkBoxDynamicAsset.Hide { get => _hide; set => _hide = value; }

    /// <summary>
    /// The layouts <see cref="DynamicKind.TalkBox"/> is known to be read with.
    /// </summary>
    internal static IReadOnlySet<(GameVersion Game, short Version)> SupportedLayouts { get; } = new HashSet<(GameVersion, short)>
    {
        (GameVersion.BFBB, 11),
        (GameVersion.TSSM, 11),
        (GameVersion.Incredibles, 11),
    };

    internal static void Read(TalkBoxDynamicAsset asset, EndianReader reader, FormatProfile profile)
    {
        asset.DialogBoxId = reader.ReadAssetId();
        asset.PromptBoxId = reader.ReadAssetId();
        asset.QuitBoxId = reader.ReadAssetId();
        asset.TrapsPlayer = reader.ReadByte() != 0;
        asset.Physical.Pause = reader.ReadByte();
        asset.AllowQuit = reader.ReadByte() != 0;
        asset.TriggerPads = (PadTriggerMode)reader.ReadByte();
        asset.Physical.Page = reader.ReadByte();
        asset.Physical.Show = reader.ReadByte();
        asset.Physical.Hide = reader.ReadByte();
        asset.AudioEffect = (AudioEffectKind)reader.ReadByte();
        asset.TeleportTargetId = reader.ReadAssetId();
        asset.AutoWait = AutoWaitSettings.Read(reader, profile);
        asset.SkipPromptId = reader.ReadAssetId();
        asset.NoSkipPromptId = reader.ReadAssetId();
        asset.QuitPromptId = reader.ReadAssetId();
        asset.NoQuitPromptId = reader.ReadAssetId();
        asset.YesNoPromptId = reader.ReadAssetId();
    }

    internal static void Write(TalkBoxDynamicAsset asset, EndianWriter writer, FormatProfile profile)
    {
        writer.Write(asset.DialogBoxId);
        writer.Write(asset.PromptBoxId);
        writer.Write(asset.QuitBoxId);
        writer.Write((byte)(asset.TrapsPlayer ? 1 : 0));
        writer.Write(asset.Physical.Pause);
        writer.Write((byte)(asset.AllowQuit ? 1 : 0));
        writer.Write((byte)asset.TriggerPads);
        writer.Write(asset.Physical.Page);
        writer.Write(asset.Physical.Show);
        writer.Write(asset.Physical.Hide);
        writer.Write((byte)asset.AudioEffect);
        writer.Write(asset.TeleportTargetId);
        AutoWaitSettings.Write(asset.AutoWait, writer, profile);
        writer.Write(asset.SkipPromptId);
        writer.Write(asset.NoSkipPromptId);
        writer.Write(asset.QuitPromptId);
        writer.Write(asset.NoQuitPromptId);
        writer.Write(asset.YesNoPromptId);
    }

    /// <summary>
    /// When a <see cref="TalkBoxDynamicAsset"/> passes the player's button presses on as events.
    /// </summary>
    public enum PadTriggerMode : byte
    {
        /// <summary>Never.</summary>
        Never = 0,
        /// <summary>Only while the player has lost control.</summary>
        Trapped = 1,
        /// <summary>Only while the player has control.</summary>
        Active = 2,
    }

    /// <summary>
    /// What a <see cref="TalkBoxDynamicAsset"/> does to the audio while its conversation runs.
    /// </summary>
    public enum AudioEffectKind : byte
    {
        /// <summary>Nothing.</summary>
        None = 0,
        /// <summary>Fades the music down, restoring it when the conversation ends.</summary>
        FadeMusic = 1,
    }
}

public static partial class Physical
{
    /// <summary>
    /// An explicit interface used to interact with <see cref="TalkBoxDynamicAsset"/>'s underlying
    /// values.
    /// </summary>
    public interface ITalkBoxDynamicAsset : IDynamicAsset
    {
        /// <summary>Unknown. Named <c>pause</c> in the game's source.</summary>
        byte Pause { get; set; }

        /// <summary>Unknown. Named <c>page</c> in the game's source.</summary>
        byte Page { get; set; }

        /// <summary>Unknown. Named <c>show</c> in the game's source.</summary>
        byte Show { get; set; }

        /// <summary>Unknown. Named <c>hide</c> in the game's source.</summary>
        byte Hide { get; set; }
    }
}
