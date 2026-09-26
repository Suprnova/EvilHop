using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public partial class PlatformMotion
{
    /// <summary>
    /// A <see cref="PlatformMotion"/> that falls a short while after the player stands on it, then resets.
    /// </summary>
    public sealed class Breakaway() : PlatformMotion
    {
        /// <summary>
        /// The time, in seconds, the platform trembles after the player stands on it before it falls.
        /// </summary>
        public float BreakDelay { get; set; }

        /// <summary>
        /// The time, in seconds, after the platform falls before it resets.
        /// </summary>
        public float ResetDelay { get; set; }

        /// <summary>
        /// The <see cref="AssetType.Model"/> the platform switches to as it falls, if any.
        /// </summary>
        /// <remarks>
        /// Only present in <see cref="GameVersion.N100F"/> and <see cref="GameVersion.BFBB"/>.
        /// </remarks>
        public AssetId BustModelId { get; set; }

        /// <summary>
        /// This platform's breakaway flags.
        /// </summary>
        /// <remarks>
        /// Not present in <see cref="GameVersion.N100F"/>.
        /// </remarks>
        public BreakawayBehavior BreakFlags { get; set; }

        /// <summary>
        /// The time, in seconds, after the platform breaks before the player and NPCs stop colliding
        /// with it.
        /// </summary>
        /// <remarks>
        /// Not present in <see cref="GameVersion.N100F"/> or <see cref="GameVersion.BFBB"/>.
        /// </remarks>
        public float CollisionOffTime { get; set; }

        internal override PlatformType PlatformType => PlatformType.Breakaway;

        internal static Breakaway Read(EndianReader reader, FormatProfile profile)
        {
            var game = profile.Game;
            var motion = new Breakaway { BreakDelay = reader.ReadSingle() };
            if (IsBFBBOrEarlier(game))
            {
                motion.BustModelId = reader.ReadAssetId();
                motion.ResetDelay = reader.ReadSingle();
                if (game is GameVersion.BFBB) motion.BreakFlags = (BreakawayBehavior)reader.ReadUInt32();
            }
            else
            {
                motion.ResetDelay = reader.ReadSingle();
                motion.BreakFlags = (BreakawayBehavior)reader.ReadUInt32();
                motion.CollisionOffTime = reader.ReadSingle();
            }
            return motion;
        }

        internal static void Write(Breakaway value, EndianWriter writer, FormatProfile profile)
        {
            var game = profile.Game;
            writer.Write(value.BreakDelay);
            if (IsBFBBOrEarlier(game))
            {
                writer.Write(value.BustModelId);
                writer.Write(value.ResetDelay);
                if (game is GameVersion.BFBB) writer.Write((uint)value.BreakFlags);
            }
            else
            {
                writer.Write(value.ResetDelay);
                writer.Write((uint)value.BreakFlags);
                writer.Write(value.CollisionOffTime);
            }
        }

        /// <summary>
        /// Behavior switches for a <see cref="Breakaway"/> platform.
        /// </summary>
        [Flags]
        public enum BreakawayBehavior : uint
        {
            /// <summary>
            /// No flags are set.
            /// </summary>
            None = 0,
            /// <summary>
            /// The platform doesn't break while the player is sneaking on it.
            /// </summary>
            /// <remarks>
            /// Ignored by <see cref="GameVersion.TSSM"/>.
            /// </remarks>
            AllowSneak = 1 << 0,
        }
    }
}
