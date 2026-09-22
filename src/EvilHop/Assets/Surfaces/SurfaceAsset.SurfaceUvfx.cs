using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Assets;

public partial class SurfaceAsset
{
    /// <summary>
    /// One of a <see cref="SurfaceAsset"/>'s two UV animations.
    /// </summary>
    public sealed class SurfaceUvfx
    {
        /// <summary>
        /// Whether this animation is active.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// How this animation drives <see cref="Translation"/>/<see cref="Scale"/>.
        /// </summary>
        public SurfaceUvfxMode Mode { get; set; }

        /// <summary>
        /// The current UV rotation, in degrees.
        /// </summary>
        public float Rotation { get; set; }

        /// <summary>
        /// The speed <see cref="Rotation"/> advances at, in degrees per second.
        /// </summary>
        public float RotationSpeed { get; set; }

        /// <summary>
        /// The current UV translation. <see cref="Vector3.Z"/> is always 0.
        /// </summary>
        public Vector3 Translation { get; set; }

        /// <summary>
        /// The speed <see cref="Translation"/> advances at. <see cref="Vector3.Z"/> is always 0.
        /// </summary>
        public Vector3 TranslationSpeed { get; set; }

        /// <summary>
        /// The current UV scale. <see cref="Vector3.Z"/> is always 0.
        /// </summary>
        public Vector3 Scale { get; set; }

        /// <summary>
        /// The speed <see cref="Scale"/> advances at.
        /// </summary>
        public Vector3 ScaleSpeed { get; set; }

        /// <summary>
        /// For <see cref="SurfaceUvfxMode.MinMaxOscillate"/>, the low end of the UV translation range.
        /// </summary>
        public Vector3 Min { get; set; }

        /// <summary>
        /// For <see cref="SurfaceUvfxMode.MinMaxOscillate"/>, the high end of the UV translation range.
        /// </summary>
        public Vector3 Max { get; set; }

        /// <summary>
        /// For <see cref="SurfaceUvfxMode.MinMaxOscillate"/>, how quickly the translation oscillates
        /// between <see cref="Min"/> and <see cref="Max"/>.
        /// </summary>
        public Vector3 MinMaxSpeed { get; set; }

        internal static SurfaceUvfx Read(EndianReader reader, FormatProfile _) => new()
        {
            Mode = (SurfaceUvfxMode)reader.ReadInt32(),
            Rotation = reader.ReadSingle(),
            RotationSpeed = reader.ReadSingle(),
            Translation = reader.ReadVector3(),
            TranslationSpeed = reader.ReadVector3(),
            Scale = reader.ReadVector3(),
            ScaleSpeed = reader.ReadVector3(),
            Min = reader.ReadVector3(),
            Max = reader.ReadVector3(),
            MinMaxSpeed = reader.ReadVector3(),
        };

        internal static void Write(SurfaceUvfx uvfx, EndianWriter writer, FormatProfile _)
        {
            writer.Write((int)uvfx.Mode);
            writer.Write(uvfx.Rotation);
            writer.Write(uvfx.RotationSpeed);
            writer.Write(uvfx.Translation);
            writer.Write(uvfx.TranslationSpeed);
            writer.Write(uvfx.Scale);
            writer.Write(uvfx.ScaleSpeed);
            writer.Write(uvfx.Min);
            writer.Write(uvfx.Max);
            writer.Write(uvfx.MinMaxSpeed);
        }

        /// <summary>
        /// Defines the animation mode governing UV coordinate scrolling and scaling on a surface.
        /// </summary>
        public enum SurfaceUvfxMode
        {
            /// <summary>
            /// <see cref="SurfaceUvfx.Rotation"/>, <see cref="SurfaceUvfx.Translation"/>, and
            /// <see cref="SurfaceUvfx.Scale"/> advance continuously at their respective speeds, wrapping
            /// around.
            /// </summary>
            Continuous = 0,
            /// <summary>
            /// <see cref="SurfaceUvfx.Translation"/> and <see cref="SurfaceUvfx.Scale"/> follow a sine wave
            /// driven by the frame counter. Never observed in any real archive.
            /// </summary>
            Sine = 1,
            /// <summary>
            /// <see cref="SurfaceUvfx.Translation"/> oscillates between <see cref="SurfaceUvfx.Min"/> and
            /// <see cref="SurfaceUvfx.Max"/> at <see cref="SurfaceUvfx.MinMaxSpeed"/>.
            /// </summary>
            MinMaxOscillate = 2,
        }
    }
}
