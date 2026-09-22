using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public partial class EntityMotion
{
    /// <summary>
    /// An <see cref="EntityMotion"/> that follows a <see cref="AssetType.Spline"/>.
    /// </summary>
    /// <remarks>
    /// Does nothing in <see cref="GameVersion.N100F"/> and <see cref="GameVersion.BFBB"/>, which store
    /// only <see cref="SplineId"/>'s slot.
    /// </remarks>
    public sealed class SplineMotion() : EntityMotion
    {
        /// <summary>
        /// The <see cref="AssetId"/> of the <see cref="AssetType.Spline"/> to follow.
        /// </summary>
        public AssetId SplineId { get; set; }

        /// <summary>
        /// The speed to follow the spline at. Not present in <see cref="GameVersion.N100F"/> or
        /// <see cref="GameVersion.BFBB"/>.
        /// </summary>
        public float Speed { get; set; }

        /// <summary>
        /// Unknown. Not present in <see cref="GameVersion.N100F"/> or <see cref="GameVersion.BFBB"/>.
        /// </summary>
        public float LeanModifier { get; set; }

        private protected override MotionType Type => MotionType.Spline;

        internal static new SplineMotion Read(EndianReader reader, FormatProfile profile)
        {
            var motion = new SplineMotion { SplineId = reader.ReadAssetId() };
            if (IsBFBBOrEarlier(profile.Game)) return motion;

            motion.Speed = reader.ReadSingle();
            motion.LeanModifier = reader.ReadSingle();
            return motion;
        }

        internal static void Write(SplineMotion value, EndianWriter writer, FormatProfile profile)
        {
            writer.Write(value.SplineId);
            if (IsBFBBOrEarlier(profile.Game)) return;

            writer.Write(value.Speed);
            writer.Write(value.LeanModifier);
        }
    }
}
