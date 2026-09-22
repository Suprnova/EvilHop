using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public partial class PlatformMotion
{
    /// <summary>
    /// A <see cref="PlatformMotion"/> that tilts under the player's weight.
    /// </summary>
    public sealed class TeeterTotterMotion() : PlatformMotion
    {
        /// <summary>The platform's initial tilt, in radians.</summary>
        public float InitialTilt { get; set; }

        /// <summary>The platform's maximum tilt, in radians.</summary>
        public float MaxTilt { get; set; }

        /// <summary>Determines how quickly the platform tilts.</summary>
        public float InverseMass { get; set; }

        /// <summary>
        /// Unknown. Only present in <see cref="GameVersion.ROTU"/>.
        /// </summary>
        public uint Unknown { get; set; }

        internal override PlatformType PlatformType => PlatformType.TeeterTotter;

        internal static TeeterTotterMotion Read(EndianReader reader, FormatProfile profile)
        {
            var motion = new TeeterTotterMotion
            {
                InitialTilt = reader.ReadSingle(),
                MaxTilt = reader.ReadSingle(),
                InverseMass = reader.ReadSingle(),
            };
            if (profile.Game is GameVersion.ROTU) motion.Unknown = reader.ReadUInt32();
            return motion;
        }

        internal static void Write(TeeterTotterMotion value, EndianWriter writer, FormatProfile profile)
        {
            writer.Write(value.InitialTilt);
            writer.Write(value.MaxTilt);
            writer.Write(value.InverseMass);
            if (profile.Game is GameVersion.ROTU) writer.Write(value.Unknown);
        }
    }
}
