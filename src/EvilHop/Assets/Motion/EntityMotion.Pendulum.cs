using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public partial class EntityMotion
{
    /// <summary>
    /// An <see cref="EntityMotion"/> that swings from side to side.
    /// </summary>
    public sealed class Pendulum() : EntityMotion
    {
        /// <summary>Unknown.</summary>
        public byte PendulumFlags { get; set; }

        /// <summary>Unknown.</summary>
        public byte Plane { get; set; }

        /// <summary>The height of the pivot point.</summary>
        public float Length { get; set; }

        /// <summary>The angle, in radians, swung to either side.</summary>
        public float Range { get; set; }

        /// <summary>The time, in seconds, one full swing takes.</summary>
        public float Period { get; set; }

        /// <summary>The angle, in radians, into the swing to start at.</summary>
        public float Phase { get; set; }

        private protected override Kind Type => Kind.Pendulum;

        internal static new Pendulum Read(EndianReader reader, FormatProfile _)
        {
            var motion = new Pendulum
            {
                PendulumFlags = reader.ReadByte(),
                Plane = reader.ReadByte(),
            };
            reader.ReadBytes(2); // padding
            motion.Length = reader.ReadSingle();
            motion.Range = reader.ReadSingle();
            motion.Period = reader.ReadSingle();
            motion.Phase = reader.ReadSingle();
            return motion;
        }

        internal static void Write(Pendulum value, EndianWriter writer, FormatProfile _)
        {
            writer.Write(value.PendulumFlags);
            writer.Write(value.Plane);
            writer.Write(new byte[2]); // padding
            writer.Write(value.Length);
            writer.Write(value.Range);
            writer.Write(value.Period);
            writer.Write(value.Phase);
        }
    }
}
