using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public sealed partial class TalkBoxDynamicAsset
{
    /// <summary>
    /// What a <see cref="TalkBoxDynamicAsset"/>'s dialog waits on before advancing.
    /// </summary>
    /// TODO: validate against decompiled source - whether multiple enabled conditions must all be met, or any one
    /// <remarks>
    /// These are the talk box's defaults; the dialog's text can override them part-way through with
    /// an <c>{auto_wait}</c> tag.
    /// </remarks>
    public sealed class AutoWaitSettings
    {
        /// <summary>Whether to wait until <see cref="Delay"/> has elapsed.</summary>
        public bool WaitsForTime { get; set; }

        /// <summary>
        /// Whether to wait for the player to respond, showing <see cref="SkipPromptId"/> or
        /// <see cref="YesNoPromptId"/> in <see cref="PromptBoxId"/> meanwhile.
        /// </summary>
        public bool WaitsForPrompt { get; set; }

        /// <summary>Whether to wait for the current line's sound to finish.</summary>
        /// TODO: validate against decompiled source
        public bool WaitsForSound { get; set; }

        /// <summary>Whether to wait for the event selected by <see cref="EventIndex"/>.</summary>
        public bool WaitsForEvent { get; set; }

        /// <summary>The time, in seconds, to wait for when <see cref="WaitsForTime"/> is set.</summary>
        public float Delay { get; set; }

        /// <summary>
        /// Which event to wait for when <see cref="WaitsForEvent"/> is set, from 1 to 31. Any other
        /// value waits for any event.
        /// </summary>
        /// TODO: validate against decompiled source - which events these indices select
        public int EventIndex { get; set; }

        internal static AutoWaitSettings Read(EndianReader reader, FormatProfile _) => new()
        {
            WaitsForTime = reader.ReadByte() != 0,
            WaitsForPrompt = reader.ReadByte() != 0,
            WaitsForSound = reader.ReadByte() != 0,
            WaitsForEvent = reader.ReadByte() != 0,
            Delay = reader.ReadSingle(),
            EventIndex = reader.ReadInt32(),
        };

        internal static void Write(AutoWaitSettings value, EndianWriter writer, FormatProfile _)
        {
            writer.Write((byte)(value.WaitsForTime ? 1 : 0));
            writer.Write((byte)(value.WaitsForPrompt ? 1 : 0));
            writer.Write((byte)(value.WaitsForSound ? 1 : 0));
            writer.Write((byte)(value.WaitsForEvent ? 1 : 0));
            writer.Write(value.Delay);
            writer.Write(value.EventIndex);
        }
    }
}
