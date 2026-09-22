using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public partial class UIMotionCommand
{
    /// <summary>
    /// Changes the UI's brightness from <see cref="StartBrightness"/> to <see cref="EndBrightness"/>,
    /// overwriting its previous brightness.
    /// </summary>
    public sealed class Brightness : UIMotionCommand
    {
        /// <summary>The starting brightness (0-255).</summary>
        public byte StartBrightness { get; set; }

        /// <summary>The ending brightness (0-255).</summary>
        public byte EndBrightness { get; set; }

        /// <inheritdoc/>
        public override Command Type => Command.Brightness;

        private protected override int FieldsSize => 4;

        internal static new Brightness Read(EndianReader reader, FormatProfile _)
        {
            var value = new Brightness
            {
                StartBrightness = reader.ReadByte(),
                EndBrightness = reader.ReadByte(),
            };
            reader.ReadBytes(2); // padding, always zero
            return value;
        }

        internal static void Write(Brightness value, EndianWriter writer, FormatProfile _)
        {
            writer.Write(value.StartBrightness);
            writer.Write(value.EndBrightness);
            writer.Write(new byte[2]); // padding
        }
    }
}
