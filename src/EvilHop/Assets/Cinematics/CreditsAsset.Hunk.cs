using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Text;

namespace EvilHop.Assets;

public partial class CreditsAsset
{
    /// <summary>
    /// One line (or pair of lines) of scrolling credits text, shown between <see cref="StartTime"/> and
    /// <see cref="EndTime"/> using one of its <see cref="Section"/>'s presets.
    /// </summary>
    public sealed class Hunk
    {
        /// <summary>
        /// The position, within the owning <see cref="Section.Presets"/>, of the preset this hunk
        /// is shown with.
        /// </summary>
        public int PresetIndex { get; set; }

        /// <summary>
        /// The time, in seconds, at which this hunk starts being shown.
        /// </summary>
        public float StartTime { get; set; }

        /// <summary>
        /// The time, in seconds, at which this hunk stops being shown.
        /// </summary>
        public float EndTime { get; set; }

        /// <summary>
        /// The text shown in the preset's first <see cref="Textbox"/>, or <see langword="null"/>
        /// for a <see cref="Alignment.Texture"/> preset.
        /// </summary>
        public string? Text1 { get; set; }

        /// <summary>
        /// The text shown in the preset's second <see cref="Textbox"/>, for a
        /// <see cref="Alignment"/> that shows two at once.
        /// </summary>
        public string? Text2 { get; set; }

        internal static int HunkByteLength(Hunk hunk) =>
            HunkHeaderSize + PaddedTextLength(hunk.Text1) + PaddedTextLength(hunk.Text2);

        /// <remarks>
        /// <paramref name="reader"/>'s position is restored before returning; a hunk header's text offsets
        /// are absolute.
        /// </remarks>
        internal static Hunk Read(EndianReader reader, FormatProfile _)
        {
            long hunkStart = reader.BaseStream.Position;
            uint hunkSize = reader.ReadUInt32();
            int presetIndex = reader.ReadInt32();
            float t0 = reader.ReadSingle();
            float t1 = reader.ReadSingle();
            uint text1Offset = reader.ReadUInt32();
            uint text2Offset = reader.ReadUInt32();

            var hunk = new Hunk
            {
                PresetIndex = presetIndex,
                StartTime = t0,
                EndTime = t1,
                Text1 = ReadInlineText(reader, text1Offset),
                Text2 = ReadInlineText(reader, text2Offset),
            };

            reader.BaseStream.Position = hunkStart + hunkSize;
            return hunk;
        }

        /// <remarks>
        /// Each text immediately follows the hunk header.
        /// </remarks>
        internal static void Write(Hunk hunk, EndianWriter writer, FormatProfile _)
        {
            long hunkStart = writer.BaseStream.Position;
            int text1Length = PaddedTextLength(hunk.Text1);
            int text2Length = PaddedTextLength(hunk.Text2);

            writer.Write((uint)(HunkHeaderSize + text1Length + text2Length));
            writer.Write(hunk.PresetIndex);
            writer.Write(hunk.StartTime);
            writer.Write(hunk.EndTime);
            writer.Write(hunk.Text1 is null ? 0u : (uint)(HeaderSize + hunkStart + HunkHeaderSize));
            writer.Write(hunk.Text2 is null ? 0u : (uint)(HeaderSize + hunkStart + HunkHeaderSize + text1Length));

            WriteInlineText(writer, hunk.Text1);
            WriteInlineText(writer, hunk.Text2);
        }

        /// <summary>
        /// Reads the null-terminated string at <paramref name="offsetFromAssetStart"/>, an absolute
        /// offset from the start of this <see cref="CreditsAsset"/>'s header (i.e. its magic number), or
        /// <see langword="null"/> when the offset is 0.
        /// </summary>
        private static string? ReadInlineText(EndianReader r, uint offsetFromAssetStart)
        {
            if (offsetFromAssetStart == 0)
                return null;

            long savedPosition = r.BaseStream.Position;
            r.BaseStream.Position = offsetFromAssetStart - HeaderSize;

            var bytes = new List<byte>();
            byte next;
            while ((next = r.ReadByte()) != 0)
                bytes.Add(next);

            r.BaseStream.Position = savedPosition;
            return Encoding.Latin1.GetString([.. bytes]);
        }

        private static void WriteInlineText(EndianWriter w, string? text)
        {
            if (text is null)
                return;

            byte[] bytes = Encoding.Latin1.GetBytes(text);
            w.Write(bytes);
            for (int i = bytes.Length; i < PaddedTextLength(text); i++)
                w.Write((byte)0);
        }

        /// <summary>The on-disk length of <paramref name="text"/>: null-terminated, 4-byte aligned.</summary>
        private static int PaddedTextLength(string? text) =>
            text is null ? 0 : (Encoding.Latin1.GetByteCount(text) + 1 + 3) & ~3;
    }
}
