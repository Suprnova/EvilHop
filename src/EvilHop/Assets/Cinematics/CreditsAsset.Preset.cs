using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;

namespace EvilHop.Assets;

public partial class CreditsAsset
{
    /// <summary>
    /// One reusable text or texture style, referenced by position from a <see cref="Section"/>'s
    /// <see cref="Hunk"/>s.
    /// </summary>
    public sealed class Preset
    {
        /// <summary>
        /// An index recorded alongside this preset. <see cref="Hunk"/>s reference a preset by its
        /// position within <see cref="Section.Presets"/>, not by this value.
        /// </summary>
        public ushort Index { get; set; }

        /// <summary>
        /// How this preset's <see cref="Textboxes"/> or <see cref="Textures"/> are laid out.
        /// </summary>
        public Alignment Alignment { get; set; }

        /// <summary>
        /// Unknown.
        /// </summary>
        public float Delay { get; set; }

        /// <summary>
        /// The gap between the two <see cref="Textboxes"/>, for a <see cref="CreditsAsset.Alignment"/>
        /// that shows both at once.
        /// </summary>
        public float InnerSpacing { get; set; }

        /// <summary>
        /// This preset's text styles. Populated when <see cref="Alignment"/> is not
        /// <see cref="Alignment.Texture"/>, empty otherwise.
        /// </summary>
        public Collection<Textbox> Textboxes { get; } = [];

        /// <summary>
        /// This preset's texture. Populated when <see cref="Alignment"/> is
        /// <see cref="Alignment.Texture"/>, empty otherwise.
        /// </summary>
        public Collection<Texture> Textures { get; } = [];

        internal static Preset Read(EndianReader reader, FormatProfile profile)
        {
            var preset = new Preset
            {
                Index = reader.ReadUInt16(),
                Alignment = (Alignment)reader.ReadUInt16(),
                Delay = reader.ReadSingle(),
                InnerSpacing = reader.ReadSingle(),
            };

            if (preset.Alignment is Alignment.Texture)
                for (int i = 0; i < 2; i++)
                    preset.Textures.Add(Texture.Read(reader, profile));
            else
                for (int i = 0; i < 2; i++)
                    preset.Textboxes.Add(Textbox.Read(reader, profile));

            return preset;
        }

        internal static void Write(Preset preset, EndianWriter writer, FormatProfile profile)
        {
            writer.Write(preset.Index);
            writer.Write((ushort)preset.Alignment);
            writer.Write(preset.Delay);
            writer.Write(preset.InnerSpacing);

            if (preset.Alignment is Alignment.Texture)
                for (int i = 0; i < 2; i++)
                    Texture.Write(i < preset.Textures.Count ? preset.Textures[i] : new Texture(), writer, profile);
            else
                for (int i = 0; i < 2; i++)
                    Textbox.Write(i < preset.Textboxes.Count ? preset.Textboxes[i] : new Textbox(), writer, profile);
        }
    }
}
