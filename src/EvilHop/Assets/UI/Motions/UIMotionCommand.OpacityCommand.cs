using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public partial class UIMotionCommand
{
    /// <summary>
    /// Changes the UI's opacity from <see cref="StartOpacity"/> to <see cref="EndOpacity"/>,
    /// overwriting its previous opacity.
    /// </summary>
    public sealed class OpacityCommand : UIMotionCommand
    {
        /// <summary>The starting opacity/alpha (0-255).</summary>
        public byte StartOpacity { get; set; }

        /// <summary>The ending opacity/alpha (0-255).</summary>
        public byte EndOpacity { get; set; }

        /// <inheritdoc/>
        public override UIMotionCommandType Type => UIMotionCommandType.Opacity;

        private protected override int FieldsSize => 4;

        internal static new OpacityCommand Read(EndianReader reader, FormatProfile _)
        {
            var value = new OpacityCommand
            {
                StartOpacity = reader.ReadByte(),
                EndOpacity = reader.ReadByte(),
            };
            reader.ReadBytes(2); // padding, always zero
            return value;
        }

        internal static void Write(OpacityCommand value, EndianWriter writer, FormatProfile _)
        {
            writer.Write(value.StartOpacity);
            writer.Write(value.EndOpacity);
            writer.Write(new byte[2]); // padding
        }
    }
}
