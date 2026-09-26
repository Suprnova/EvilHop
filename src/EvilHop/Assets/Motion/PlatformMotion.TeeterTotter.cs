using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public partial class PlatformMotion
{
    /// <summary>
    /// A <see cref="PlatformMotion"/> that tilts under the player's weight.
    /// </summary>
    public sealed class TeeterTotter() : PlatformMotion
    {
        /// <summary>Unknown.</summary>
        public float InitialTilt { get; set; }

        /// <summary>The platform's maximum tilt, in radians.</summary>
        public float MaxTilt { get; set; }

        /// <summary>
        /// How fast the platform tilts, in degrees per second per unit of the rider's distance from
        /// its center.
        /// </summary>
        /// <remarks>
        /// Without a rider, the platform returns to its <see cref="EntityAsset.Angle"/> at this many
        /// degrees per second.
        /// </remarks>
        public float InverseMass { get; set; }

        /// <summary>
        /// Unknown.
        /// </summary>
        /// <remarks>
        /// Resolved as a <see cref="AssetType.SoundGroup"/> at load. Only present in
        /// <see cref="GameVersion.ROTU"/> and <see cref="GameVersion.Ratatouille"/>.
        /// </remarks>
        public AssetId CreakSoundGroupId { get; set; }

        /// <inheritdoc cref="CreakSoundGroupId"/>
        public AssetId EndSoundGroupId { get; set; }

        internal override PlatformType PlatformType => PlatformType.TeeterTotter;

        private static bool HasSoundGroups(GameVersion game) => game is GameVersion.ROTU or GameVersion.Ratatouille;

        internal static TeeterTotter Read(EndianReader reader, FormatProfile profile)
        {
            var motion = new TeeterTotter
            {
                InitialTilt = reader.ReadSingle(),
                MaxTilt = reader.ReadSingle(),
                InverseMass = reader.ReadSingle(),
            };
            if (HasSoundGroups(profile.Game))
            {
                motion.CreakSoundGroupId = reader.ReadAssetId();
                motion.EndSoundGroupId = reader.ReadAssetId();
            }
            return motion;
        }

        internal static void Write(TeeterTotter value, EndianWriter writer, FormatProfile profile)
        {
            writer.Write(value.InitialTilt);
            writer.Write(value.MaxTilt);
            writer.Write(value.InverseMass);
            if (HasSoundGroups(profile.Game))
            {
                writer.Write(value.CreakSoundGroupId);
                writer.Write(value.EndSoundGroupId);
            }
        }
    }
}
