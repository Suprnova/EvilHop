using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public partial class AnimationTableAsset
{
    /// <summary>
    /// One <see cref="AnimationTableAsset"/> file: one or more of the table's <see cref="Raw"/>
    /// animations, optionally blended together across a bilinear grid.
    /// </summary>
    public sealed class AnimationTableFile
    {
        /// <summary>
        /// Playback and format flags for this file.
        /// </summary>
        public FileFlags Flags { get; set; }

        /// <summary>
        /// The playback duration, in seconds.
        /// </summary>
        public float Duration { get; set; }

        /// <summary>
        /// The time, in seconds, playback starts at within the underlying raw animation(s).
        /// </summary>
        public float TimeOffset { get; set; }

        /// <summary>
        /// The bilinear blend grid's width, in raw animations. 1 for a non-blended file.
        /// </summary>
        public ushort NumAnimsX { get; set; }

        /// <summary>
        /// The bilinear blend grid's height, in raw animations. 1 for a non-blended file.
        /// </summary>
        public ushort NumAnimsY { get; set; }

        /// <summary>
        /// An internal byte offset, from the start of the owning <see cref="AnimationTableAsset"/>'s data,
        /// to this file's <see cref="NumAnimsX"/>*<see cref="NumAnimsY"/> indices into
        /// <see cref="Raw"/>. Not individually resolved; preserved via
        /// <see cref="Asset.GetUnparsedTail"/>.
        /// </summary>
        public uint RawDataOffset { get; set; }

        /// <summary>
        /// Unknown. Usually -1.
        /// </summary>
        public int Physics { get; set; }

        /// <summary>
        /// Unknown. Usually -1.
        /// </summary>
        public int StartPose { get; set; }

        /// <summary>
        /// Unknown. Usually -1.
        /// </summary>
        public int EndPose { get; set; }

        internal static AnimationTableFile Read(EndianReader reader, FormatProfile _) => new()
        {
            Flags = (FileFlags)reader.ReadUInt32(),
            Duration = reader.ReadSingle(),
            TimeOffset = reader.ReadSingle(),
            NumAnimsX = reader.ReadUInt16(),
            NumAnimsY = reader.ReadUInt16(),
            RawDataOffset = reader.ReadUInt32(),
            Physics = reader.ReadInt32(),
            StartPose = reader.ReadInt32(),
            EndPose = reader.ReadInt32(),
        };

        internal static void Write(AnimationTableFile value, EndianWriter writer, FormatProfile _)
        {
            writer.Write((uint)value.Flags);
            writer.Write(value.Duration);
            writer.Write(value.TimeOffset);
            writer.Write(value.NumAnimsX);
            writer.Write(value.NumAnimsY);
            writer.Write(value.RawDataOffset);
            writer.Write(value.Physics);
            writer.Write(value.StartPose);
            writer.Write(value.EndPose);
        }


        /// <summary>
        /// Flags governing the playback and blending of an <see cref="AnimationTableFile"/>.
        /// </summary>
        [Flags]
        public enum FileFlags : uint
        {
            /// <summary>No flags are set.</summary>
            None = 0,

            /// <summary>Plays the animation in reverse.</summary>
            Reverse = 1 << 12,

            /// <summary>Doubles the duration and plays the second half in reverse.</summary>
            ReverseSecondHalf = 1 << 13,

            /// <summary>Marks this file's blend dimensions as an active bilinear blend grid.</summary>
            Bilinear = 1 << 14,

            /// <summary>Marks this as vertex/morph animation data rather than skeletal.</summary>
            Morph = 1 << 15,
        }
    }

}
