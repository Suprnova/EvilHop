using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public partial class CameraCurveAsset
{
    /// <summary>
    /// One tuning point along a <see cref="CameraCurveAsset"/>'s length.
    /// </summary>
    public sealed class Bead
    {
        /// <summary>The parameter along the first rail this bead sits at.</summary>
        public float CurveU1 { get; set; }

        /// <summary>The parameter along the second rail this bead sits at.</summary>
        public float CurveU2 { get; set; }

        /// <summary>An adjustment to the camera's distance from its target at this bead.</summary>
        public float DistanceAdjust { get; set; }

        /// <summary>An adjustment to the camera's pitch at this bead.</summary>
        public float PitchOffset { get; set; }

        /// <summary>The radius, around the target, the camera tries to keep in view at this bead.</summary>
        public float TargetRadius { get; set; }

        /// <summary>The angle of margin allowed before the camera adjusts to keep its target in view.</summary>
        public float TargetMarginAngle { get; set; }

        /// <summary>How far ahead of its target the camera leads at this bead.</summary>
        public float LeadOffset { get; set; }

        /// <summary>A vertical offset applied to the camera at this bead.</summary>
        public float YOffset { get; set; }

        /// <summary>An adjustment made to keep the camera from clipping into a nearby wall.</summary>
        public float NearWallAdjust { get; set; }

        /// <summary>An adjustment made to keep the camera from drifting too far past a distant wall.</summary>
        public float FarWallAdjust { get; set; }

        internal static Bead Read(EndianReader reader, FormatProfile _) => new()
        {
            CurveU1 = reader.ReadSingle(),
            CurveU2 = reader.ReadSingle(),
            DistanceAdjust = reader.ReadSingle(),
            PitchOffset = reader.ReadSingle(),
            TargetRadius = reader.ReadSingle(),
            TargetMarginAngle = reader.ReadSingle(),
            LeadOffset = reader.ReadSingle(),
            YOffset = reader.ReadSingle(),
            NearWallAdjust = reader.ReadSingle(),
            FarWallAdjust = reader.ReadSingle(),
        };

        internal static void Write(Bead value, EndianWriter writer, FormatProfile _)
        {
            writer.Write(value.CurveU1);
            writer.Write(value.CurveU2);
            writer.Write(value.DistanceAdjust);
            writer.Write(value.PitchOffset);
            writer.Write(value.TargetRadius);
            writer.Write(value.TargetMarginAngle);
            writer.Write(value.LeadOffset);
            writer.Write(value.YOffset);
            writer.Write(value.NearWallAdjust);
            writer.Write(value.FarWallAdjust);
        }
    }
}
