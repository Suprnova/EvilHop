using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public partial class UIMotionCommand
{
    /// <summary>
    /// Rotates the UI by <see cref="Rotation"/> degrees, relative to its last rotation.
    /// </summary>
    public sealed class RotateCommand : UIMotionCommand
    {
        /// <summary>How far to rotate, in degrees. Positive is clockwise, negative is counter-clockwise.</summary>
        public float Rotation { get; set; }

        /// <summary>The rotation pivot's X axis offset, in pixels.</summary>
        public float CenterOffsetX { get; set; }

        /// <summary>The rotation pivot's Y axis offset, in pixels.</summary>
        public float CenterOffsetY { get; set; }

        /// <inheritdoc/>
        public override UIMotionCommandType Type => UIMotionCommandType.Rotate;

        private protected override int FieldsSize => 12;

        internal static new RotateCommand Read(EndianReader reader, FormatProfile _) => new()
        {
            Rotation = reader.ReadSingle(),
            CenterOffsetX = reader.ReadSingle(),
            CenterOffsetY = reader.ReadSingle(),
        };

        internal static void Write(RotateCommand value, EndianWriter writer, FormatProfile _)
        {
            writer.Write(value.Rotation);
            writer.Write(value.CenterOffsetX);
            writer.Write(value.CenterOffsetY);
        }
    }
}
