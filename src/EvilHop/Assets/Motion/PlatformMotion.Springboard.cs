using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.Immutable;
using System.Numerics;

namespace EvilHop.Assets;

public partial class PlatformMotion
{
    /// <summary>
    /// A <see cref="PlatformMotion"/> that launches the player into the air.
    /// </summary>
    public sealed class Springboard() : PlatformMotion
    {
        /// <summary>
        /// Exactly 3 jump heights for the springboard.
        /// </summary>
        /// <exception cref="ArgumentException">The assigned value's length isn't 3.</exception>
        public ImmutableArray<float> JumpHeights
        {
            get;
            set => field = value.Length == 3
                ? value
                : throw new ArgumentException($"{nameof(JumpHeights)} must contain exactly 3 elements.", nameof(value));
        } = [0f, 0f, 0f];

        /// <summary>
        /// The height of a Bubble Bounce off this springboard. Not present in <see cref="GameVersion.N100F"/>.
        /// </summary>
        public float BounceHeight { get; set; }

        /// <summary>
        /// The <see cref="AssetId"/> of the <see cref="AssetType.Animation"/> played when the
        /// springboard launches the player, if any.
        /// </summary>
        public AssetId SpringAnimationId { get; set; }

        /// <summary>
        /// The <see cref="AssetId"/> of the <see cref="AssetType.Animation"/> played while the
        /// springboard is idle, if any.
        /// </summary>
        public AssetId IdleAnimationId { get; set; }

        /// <summary>
        /// The normalized direction the player is launched in.
        /// </summary>
        public Vector3 JumpDirection { get; set; }

        /// <summary>
        /// This springboard's flags. Not present in <see cref="GameVersion.N100F"/>.
        /// </summary>
        public LockingBehavior SpringFlags { get; set; }

        internal override PlatformType PlatformType => PlatformType.Springboard;

        internal static Springboard Read(EndianReader reader, FormatProfile profile)
        {
            var game = profile.Game;
            var motion = new Springboard
            {
                JumpHeights = [reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()],
            };
            if (game is not GameVersion.N100F) motion.BounceHeight = reader.ReadSingle();
            motion.SpringAnimationId = reader.ReadAssetId();
            motion.IdleAnimationId = reader.ReadAssetId();
            reader.ReadAssetId(); // padding
            motion.JumpDirection = reader.ReadVector3();
            if (game is not GameVersion.N100F) motion.SpringFlags = (LockingBehavior)reader.ReadUInt32();
            return motion;
        }

        internal static void Write(Springboard value, EndianWriter writer, FormatProfile profile)
        {
            var game = profile.Game;
            foreach (float height in value.JumpHeights) writer.Write(height);
            if (game is not GameVersion.N100F) writer.Write(value.BounceHeight);
            writer.Write(value.SpringAnimationId);
            writer.Write(value.IdleAnimationId);
            writer.Write(AssetId.None); // padding
            writer.Write(value.JumpDirection);
            if (game is not GameVersion.N100F) writer.Write((uint)value.SpringFlags);
        }

        /// <summary>
        /// Flags controlling camera lock and player movement restrictions during a springboard launch.
        /// </summary>
        [Flags]
        public enum LockingBehavior : uint
        {
            /// <summary>
            /// No flags are set.
            /// </summary>
            None = 0,
            /// <summary>
            /// The camera looks down at the player while they're launched.
            /// </summary>
            LockView = 1 << 0,
            /// <summary>
            /// The player can't move while they're launched.
            /// </summary>
            LockMovement = 1 << 2,
        }
    }
}
