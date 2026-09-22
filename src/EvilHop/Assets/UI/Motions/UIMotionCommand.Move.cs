using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public partial class UIMotionCommand
{
    /// <summary>
    /// Moves the UI by <see cref="DistanceX"/>/<see cref="DistanceY"/> pixels, relative to its last
    /// position.
    /// </summary>
    public sealed class Move : UIMotionCommand
    {
        /// <summary>How far to move on the X axis, in pixels.</summary>
        public float DistanceX { get; set; }

        /// <summary>How far to move on the Y axis, in pixels.</summary>
        public float DistanceY { get; set; }

        /// <inheritdoc/>
        public override Command Type => Command.Move;

        private protected override int FieldsSize => 8;

        internal static new Move Read(EndianReader reader, FormatProfile _) => new()
        {
            DistanceX = reader.ReadSingle(),
            DistanceY = reader.ReadSingle(),
        };

        internal static void Write(Move value, EndianWriter writer, FormatProfile _)
        {
            writer.Write(value.DistanceX);
            writer.Write(value.DistanceY);
        }
    }
}
