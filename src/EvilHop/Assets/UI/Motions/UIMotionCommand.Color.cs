using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public partial class UIMotionCommand
{
    /// <summary>
    /// Changes the UI's color from <see cref="StartColor"/> to <see cref="EndColor"/>, overwriting its
    /// previous color.
    /// </summary>
    public sealed class Color : UIMotionCommand
    {
        /// <summary>The starting color.</summary>
        public Rgb StartColor { get; set; }

        /// <summary>The ending color.</summary>
        public Rgb EndColor { get; set; }

        /// <inheritdoc/>
        public override Command Type => Command.Color;

        private protected override int FieldsSize => 8;

        internal static new Color Read(EndianReader reader, FormatProfile _)
        {
            var value = new Color
            {
                StartColor = reader.ReadRgb24(),
                EndColor = reader.ReadRgb24(),
            };
            reader.ReadBytes(2); // padding, always zero
            return value;
        }

        internal static void Write(Color value, EndianWriter writer, FormatProfile _)
        {
            writer.WriteRgb24(value.StartColor);
            writer.WriteRgb24(value.EndColor);
            writer.Write(new byte[2]); // padding
        }
    }
}
