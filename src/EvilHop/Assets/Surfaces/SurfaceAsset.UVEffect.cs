using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Assets;

public partial class SurfaceAsset
{
    /// <summary>
    /// One of a <see cref="SurfaceAsset"/>'s two UV animations.
    /// </summary>
    public sealed class UVEffect
    {
        /// <summary>
        /// Whether this animation is active.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// How this animation drives <see cref="Translation"/>/<see cref="Scale"/>.
        /// </summary>
        public AnimationMode Mode { get; set; }

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
        /// For <see cref="AnimationMode.MinMaxOscillate"/>, the low end of the UV translation range.
        /// </summary>
        /// <remarks>
        /// Not present in <see cref="GameVersion.N100F"/>.
        /// </remarks>
        public Vector3 Min { get; set; }

        /// <summary>
        /// For <see cref="AnimationMode.MinMaxOscillate"/>, the high end of the UV translation range.
        /// </summary>
        /// <remarks>
        /// Not present in <see cref="GameVersion.N100F"/>.
        /// </remarks>
        public Vector3 Max { get; set; }

        /// <summary>
        /// For <see cref="AnimationMode.MinMaxOscillate"/>, how quickly the translation oscillates
        /// between <see cref="Min"/> and <see cref="Max"/>.
        /// </summary>
        /// <remarks>
        /// Not present in <see cref="GameVersion.N100F"/>.
        /// </remarks>
        public Vector3 MinMaxSpeed { get; set; }

        internal static UVEffect Read(EndianReader reader, FormatProfile profile)
        {
            var uvfx = new UVEffect
            {
                Mode = (AnimationMode)reader.ReadInt32(),
                Rotation = reader.ReadSingle(),
                RotationSpeed = reader.ReadSingle(),
                Translation = reader.ReadVector3(),
                TranslationSpeed = reader.ReadVector3(),
                Scale = reader.ReadVector3(),
                ScaleSpeed = reader.ReadVector3(),
            };
            if (profile.Game is not GameVersion.N100F)
            {
                uvfx.Min = reader.ReadVector3();
                uvfx.Max = reader.ReadVector3();
                uvfx.MinMaxSpeed = reader.ReadVector3();
            }
            return uvfx;
        }

        internal static void Write(UVEffect uvfx, EndianWriter writer, FormatProfile profile)
        {
            writer.Write((int)uvfx.Mode);
            writer.Write(uvfx.Rotation);
            writer.Write(uvfx.RotationSpeed);
            writer.Write(uvfx.Translation);
            writer.Write(uvfx.TranslationSpeed);
            writer.Write(uvfx.Scale);
            writer.Write(uvfx.ScaleSpeed);
            if (profile.Game is not GameVersion.N100F)
            {
                writer.Write(uvfx.Min);
                writer.Write(uvfx.Max);
                writer.Write(uvfx.MinMaxSpeed);
            }
        }

        /// <summary>
        /// Defines the animation mode governing UV coordinate scrolling and scaling on a surface.
        /// </summary>
        public enum AnimationMode
        {
            /// <summary>
            /// <see cref="Rotation"/>, <see cref="Translation"/>, and
            /// <see cref="Scale"/> advance continuously at their respective speeds, wrapping
            /// around.
            /// </summary>
            Continuous = 0,
            /// <summary>
            /// <see cref="Translation"/> and <see cref="Scale"/> follow a sine wave
            /// driven by the frame counter. Never observed in any real archive.
            /// </summary>
            Sine = 1,
            /// <summary>
            /// <see cref="Translation"/> oscillates between <see cref="Min"/> and
            /// <see cref="Max"/> at <see cref="MinMaxSpeed"/>.
            /// </summary>
            MinMaxOscillate = 2,
        }
    }
}
