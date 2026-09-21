using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;
using System.Numerics;

namespace EvilHop.Assets;

public partial class CreditsAsset
{
    /// <summary>
    /// One independently-scrolling block of a <see cref="CreditsAsset"/>: a set of reusable text/texture
    /// presets, and the timed <see cref="CreditsHunk"/>s that use them.
    /// </summary>
    public sealed class CreditsSection
    {
        /// <summary>
        /// This section's duration, in seconds.
        /// </summary>
        public float Duration { get; set; }

        /// <summary>
        /// Unknown. Alternates between 0 and 1 across a <see cref="CreditsAsset"/>'s sections.
        /// </summary>
        public uint Flags { get; set; }

        /// <summary>
        /// The scroll position this section starts at, as a percentage (0 to 1) of the screen - 0 is the
        /// top, 1 is the bottom.
        /// </summary>
        public Vector2 Start { get; set; }

        /// <summary>
        /// The scroll position this section ends at, as a percentage (0 to 1) of the screen - 0 is the
        /// top, 1 is the bottom.
        /// </summary>
        public Vector2 End { get; set; }

        /// <summary>
        /// The rate this section scrolls at.
        /// </summary>
        public float ScrollRate { get; set; }

        /// <summary>
        /// Unknown.
        /// </summary>
        public float Lifetime { get; set; }

        /// <summary>
        /// The point, from 0 (start) to 1 (end), at which this section's fade-in begins.
        /// </summary>
        public float FadeInStart { get; set; }

        /// <summary>
        /// The point, from 0 (start) to 1 (end), at which this section's fade-in ends.
        /// </summary>
        public float FadeInEnd { get; set; }

        /// <summary>
        /// The point, from 0 (start) to 1 (end), at which this section's fade-out begins.
        /// </summary>
        public float FadeOutStart { get; set; }

        /// <summary>
        /// The point, from 0 (start) to 1 (end), at which this section's fade-out ends.
        /// </summary>
        public float FadeOutEnd { get; set; }

        /// <summary>
        /// This section's reusable text and texture styles, referenced by position from
        /// <see cref="Hunks"/>.
        /// </summary>
        public Collection<CreditsPreset> Presets { get; } = [];

        /// <summary>
        /// This section's timed lines of credits text.
        /// </summary>
        public Collection<CreditsHunk> Hunks { get; } = [];

        internal static int SectionByteLength(CreditsSection section) =>
            SectionHeaderSize + section.Presets.Count * PresetSize + section.Hunks.Sum(CreditsHunk.HunkByteLength);

        internal static CreditsSection Read(EndianReader reader, FormatProfile profile)
        {
            long sectionStart = reader.BaseStream.Position;
            uint creditsSize = reader.ReadUInt32();

            var section = new CreditsSection
            {
                Duration = reader.ReadSingle(),
                Flags = reader.ReadUInt32(),
                Start = new Vector2(reader.ReadSingle(), reader.ReadSingle()),
                End = new Vector2(reader.ReadSingle(), reader.ReadSingle()),
                ScrollRate = reader.ReadSingle(),
                Lifetime = reader.ReadSingle(),
                FadeInStart = reader.ReadSingle(),
                FadeInEnd = reader.ReadSingle(),
                FadeOutStart = reader.ReadSingle(),
                FadeOutEnd = reader.ReadSingle(),
            };

            uint numPresets = reader.ReadUInt32();
            for (uint i = 0; i < numPresets; i++)
                section.Presets.Add(CreditsPreset.Read(reader, profile));

            while (reader.BaseStream.Position - sectionStart < creditsSize)
                section.Hunks.Add(CreditsHunk.Read(reader, profile));

            return section;
        }

        internal static void Write(CreditsSection section, EndianWriter writer, FormatProfile profile)
        {
            writer.Write((uint)SectionByteLength(section));
            writer.Write(section.Duration);
            writer.Write(section.Flags);
            writer.Write(section.Start.X);
            writer.Write(section.Start.Y);
            writer.Write(section.End.X);
            writer.Write(section.End.Y);
            writer.Write(section.ScrollRate);
            writer.Write(section.Lifetime);
            writer.Write(section.FadeInStart);
            writer.Write(section.FadeInEnd);
            writer.Write(section.FadeOutStart);
            writer.Write(section.FadeOutEnd);
            writer.Write((uint)section.Presets.Count);

            foreach (var preset in section.Presets)
                CreditsPreset.Write(preset, writer, profile);

            foreach (var hunk in section.Hunks)
                CreditsHunk.Write(hunk, writer, profile);
        }
    }
}
