using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Assets;

public partial class LightKitAsset
{
    /// <summary>
    /// One light in a <see cref="LightKitAsset"/>.
    /// </summary>
    /// <remarks>
    /// In decompiled source, <see cref="Right"/>, <see cref="Up"/>, and <see cref="At"/> are individually
    /// normalized and assembled into the light's orientation, with <see cref="Right"/> and <see cref="At"/>
    /// negated in the process; <see cref="Position"/> becomes its world position. For an
    /// <see cref="LightKitLightType.Ambient"/> light, all four are <see cref="Vector3.Zero"/>.
    /// </remarks>
    public sealed class LightKitLight
    {
        /// <summary>
        /// This light's type, determining which of this light's other fields apply.
        /// </summary>
        public LightKitLightType Type { get; set; }

        /// <summary>
        /// This light's color.
        /// </summary>
        public Rgba Color { get; set; }

        /// <summary>
        /// The right vector of this light's orientation.
        /// </summary>
        public Vector3 Right { get; set; }

        /// <summary>
        /// The up vector of this light's orientation.
        /// </summary>
        public Vector3 Up { get; set; }

        /// <summary>
        /// The direction a <see cref="LightKitLightType.Directional"/> light points towards, as an angle
        /// throughout the level rather than a position.
        /// </summary>
        public Vector3 At { get; set; }

        /// <summary>
        /// This light's world position, for <see cref="LightKitLightType.Point"/> and
        /// <see cref="LightKitLightType.Spot"/> lights.
        /// </summary>
        public Vector3 Position { get; set; }

        /// <summary>
        /// Unknown. The fourth component alongside <see cref="Position"/>, forming a complete
        /// homogeneous 4-vector with <see cref="Right"/>/<see cref="Up"/>/<see cref="At"/> (each of whose
        /// own fourth components are always 0 and not modelled). Always 1 for every
        /// non-<see cref="LightKitLightType.Ambient"/> light type.
        /// </summary>
        public float PositionW { get; set; }

        /// <summary>
        /// The radius of a <see cref="LightKitLightType.Point"/> or <see cref="LightKitLightType.Spot"/>
        /// light.
        /// </summary>
        public float Radius { get; set; }

        /// <summary>
        /// The cone angle of a <see cref="LightKitLightType.Spot"/> light.
        /// </summary>
        public float Angle { get; set; }

        internal static LightKitLight Read(EndianReader reader, FormatProfile _)
        {
            var light = new LightKitLight
            {
                Type = (LightKitLightType)reader.ReadUInt32(),
                Color = reader.ReadRgba(),
                Right = reader.ReadVector3(),
            };

            reader.ReadSingle(); // Right's homogeneous component; always 0
            light.Up = reader.ReadVector3();
            reader.ReadSingle(); // Up's homogeneous component; always 0
            light.At = reader.ReadVector3();
            reader.ReadSingle(); // At's homogeneous component; always 0
            light.Position = reader.ReadVector3();
            light.PositionW = reader.ReadSingle();
            light.Radius = reader.ReadSingle();
            light.Angle = reader.ReadSingle();
            reader.ReadUInt32(); // runtime-resolved platLight, always 0

            return light;
        }

        internal static void Write(LightKitLight value, EndianWriter writer, FormatProfile _)
        {
            writer.Write((uint)value.Type);
            writer.Write(value.Color);
            writer.Write(value.Right);
            writer.Write(0f); // Right's homogeneous component
            writer.Write(value.Up);
            writer.Write(0f); // Up's homogeneous component
            writer.Write(value.At);
            writer.Write(0f); // At's homogeneous component
            writer.Write(value.Position);
            writer.Write(value.PositionW);
            writer.Write(value.Radius);
            writer.Write(value.Angle);
            writer.Write(0u); // runtime-resolved
        }

        /// <summary>
        /// Defines the lighting type of a <see cref="LightKitLight"/>.
        /// </summary>
        public enum LightKitLightType : uint
        {
            /// <summary>
            /// A uniform light with no direction or position, illuminating everything equally.
            /// </summary>
            Ambient = 1,
            /// <summary>
            /// A light shining uniformly along <see cref="At"/> from everywhere, with no
            /// position of its own.
            /// </summary>
            Directional = 2,
            /// <summary>
            /// A light radiating from <see cref="Position"/> in every direction, out to
            /// <see cref="Radius"/>.
            /// </summary>
            Point = 3,
            /// <summary>
            /// A light radiating from <see cref="Position"/> in a cone along
            /// <see cref="At"/>, out to <see cref="Radius"/> and
            /// <see cref="Angle"/>.
            /// </summary>
            Spot = 4,
        }
    }
}
