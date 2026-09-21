using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;

namespace EvilHop.Assets;

public partial class CreditsAsset
{
    /// <summary>
    /// One reusable text or texture style, referenced by position from a <see cref="CreditsSection"/>'s
    /// <see cref="CreditsHunk"/>s.
    /// </summary>
    public sealed class CreditsPreset
    {
        /// <summary>
        /// An index recorded alongside this preset. <see cref="CreditsHunk"/>s reference a preset by its
        /// position within <see cref="CreditsSection.Presets"/>, not by this value.
        /// </summary>
        public ushort Index { get; set; }

        /// <summary>
        /// How this preset's <see cref="Textboxes"/> or <see cref="Textures"/> are laid out.
        /// </summary>
        public CreditsPresetAlignment Alignment { get; set; }

        /// <summary>
        /// Unknown.
        /// </summary>
        public float Delay { get; set; }

        /// <summary>
        /// The gap between the two <see cref="Textboxes"/>, for a <see cref="CreditsPresetAlignment"/>
        /// that shows both at once.
        /// </summary>
        public float InnerSpacing { get; set; }

        /// <summary>
        /// This preset's text styles. Populated when <see cref="Alignment"/> is not
        /// <see cref="CreditsPresetAlignment.Texture"/>, empty otherwise.
        /// </summary>
        public Collection<CreditsTextbox> Textboxes { get; } = [];

        /// <summary>
        /// This preset's texture. Populated when <see cref="Alignment"/> is
        /// <see cref="CreditsPresetAlignment.Texture"/>, empty otherwise.
        /// </summary>
        public Collection<CreditsTexture> Textures { get; } = [];

        internal static CreditsPreset Read(EndianReader reader, FormatProfile profile)
        {
            var preset = new CreditsPreset
            {
                Index = reader.ReadUInt16(),
                Alignment = (CreditsPresetAlignment)reader.ReadUInt16(),
                Delay = reader.ReadSingle(),
                InnerSpacing = reader.ReadSingle(),
            };

            if (preset.Alignment is CreditsPresetAlignment.Texture)
                for (int i = 0; i < 2; i++)
                    preset.Textures.Add(CreditsTexture.Read(reader, profile));
            else
                for (int i = 0; i < 2; i++)
                    preset.Textboxes.Add(CreditsTextbox.Read(reader, profile));

            return preset;
        }

        internal static void Write(CreditsPreset preset, EndianWriter writer, FormatProfile profile)
        {
            writer.Write(preset.Index);
            writer.Write((ushort)preset.Alignment);
            writer.Write(preset.Delay);
            writer.Write(preset.InnerSpacing);

            if (preset.Alignment is CreditsPresetAlignment.Texture)
                for (int i = 0; i < 2; i++)
                    CreditsTexture.Write(i < preset.Textures.Count ? preset.Textures[i] : new CreditsTexture(), writer, profile);
            else
                for (int i = 0; i < 2; i++)
                    CreditsTextbox.Write(i < preset.Textboxes.Count ? preset.Textboxes[i] : new CreditsTextbox(), writer, profile);
        }
    }
}
