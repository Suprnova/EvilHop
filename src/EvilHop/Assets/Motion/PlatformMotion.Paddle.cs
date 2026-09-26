using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.Immutable;

namespace EvilHop.Assets;

public partial class PlatformMotion
{
    /// <summary>
    /// A <see cref="PlatformMotion"/> that rotates between a set of orientations when hit.
    /// </summary>
    /// <remarks>
    /// Not present in <see cref="GameVersion.N100F"/>.
    /// </remarks>
    public sealed class Paddle() : PlatformMotion
    {
        /// <summary>
        /// The most <see cref="Orientations"/> a paddle can have.
        /// </summary>
        public const int MaxOrientations = 6;

        /// <summary>
        /// The index into <see cref="Orientations"/> the paddle starts at.
        /// </summary>
        public int StartOrientation { get; set; }

        /// <summary>
        /// The yaws, in degrees, the paddle can rest at.
        /// </summary>
        /// <remarks>
        /// Relative to the paddle's <see cref="EntityAsset.Angle"/>. At most
        /// <see cref="MaxOrientations"/> elements.
        /// </remarks>
        /// <exception cref="ArgumentException">The assigned value has more than <see cref="MaxOrientations"/> elements.</exception>
        public ImmutableArray<float> Orientations
        {
            get;
            set => field = value.Length <= MaxOrientations
                ? value
                : throw new ArgumentException($"{nameof(Orientations)} must contain at most {MaxOrientations} elements.", nameof(value));
        } = [];

        /// <summary>
        /// The orientation, in degrees, that stands in for the first of <see cref="Orientations"/> when
        /// wrapping around.
        /// </summary>
        /// <remarks>
        /// Typically the first orientation plus a full turn, such as 360.
        /// </remarks>
        public float OrientationLoop { get; set; }

        /// <summary>
        /// This paddle's flags.
        /// </summary>
        public PaddleBehavior PaddleFlags { get; set; }

        /// <summary>The paddle's rotation speed, in degrees per second.</summary>
        public float RotateSpeed { get; set; }

        /// <summary>The time, in seconds, to ease into a rotation.</summary>
        public float AccelTime { get; set; }

        /// <summary>The time, in seconds, to ease out of a rotation.</summary>
        public float DecelTime { get; set; }

        /// <summary>Unknown.</summary>
        public float HubRadius { get; set; }

        internal override PlatformType PlatformType => PlatformType.Paddle;

        internal static Paddle Read(EndianReader reader, FormatProfile profile)
        {
            if (profile.Game is GameVersion.N100F)
                throw new InvalidDataException($"{nameof(GameVersion.N100F)} has no paddle platforms.");

            var motion = new Paddle { StartOrientation = reader.ReadInt32() };
            int count = reader.ReadInt32();
            motion.OrientationLoop = reader.ReadSingle();
            float[] slots = [.. Enumerable.Range(0, MaxOrientations).Select(_ => reader.ReadSingle())];
            if (count is < 0 or > MaxOrientations)
                throw new InvalidDataException($"Paddle orientation count {count} is outside 0-{MaxOrientations}.");

            motion.Orientations = [.. slots.Take(count)];
            motion.PaddleFlags = (PaddleBehavior)reader.ReadUInt32();
            motion.RotateSpeed = reader.ReadSingle();
            motion.AccelTime = reader.ReadSingle();
            motion.DecelTime = reader.ReadSingle();
            motion.HubRadius = reader.ReadSingle();
            return motion;
        }

        internal static void Write(Paddle value, EndianWriter writer, FormatProfile profile)
        {
            if (profile.Game is GameVersion.N100F) return;

            writer.Write(value.StartOrientation);
            writer.Write(value.Orientations.Length);
            writer.Write(value.OrientationLoop);
            foreach (float orientation in value.Orientations) writer.Write(orientation);
            writer.Write(new byte[sizeof(float) * (MaxOrientations - value.Orientations.Length)]);
            writer.Write((uint)value.PaddleFlags);
            writer.Write(value.RotateSpeed);
            writer.Write(value.AccelTime);
            writer.Write(value.DecelTime);
            writer.Write(value.HubRadius);
        }

        /// <summary>
        /// Switches controlling which hits rotate a paddle, and how.
        /// </summary>
        [Flags]
        public enum PaddleBehavior : uint
        {
            /// <summary>
            /// No flags are set.
            /// </summary>
            None = 0,
            /// <summary>
            /// A hit can rotate the paddle forward, toward its next orientation.
            /// </summary>
            /// <remarks>
            /// Otherwise the paddle stutters in place. A <b>Run</b> event rotates it either way.
            /// </remarks>
            RotatesForward = 1 << 0,
            /// <summary>
            /// A hit can rotate the paddle backward, toward its previous orientation.
            /// </summary>
            /// <remarks>
            /// Otherwise the paddle stutters in place. A <b>Run</b> event rotates it either way.
            /// </remarks>
            RotatesBackward = 1 << 1,
            /// <summary>
            /// The paddle wraps around between its last and first orientations.
            /// </summary>
            Wraps = 1 << 2,
            /// <summary>
            /// The Bubble Bowl can rotate the paddle.
            /// </summary>
            HitByBubbleBowl = 1 << 4,
            /// <summary>
            /// The Cruise Bubble can rotate the paddle.
            /// </summary>
            HitByCruiseBubble = 1 << 5,
        }
    }
}
