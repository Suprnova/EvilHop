using EvilHop.Primitives;

namespace EvilHop.Assets;

/// <summary>
/// Changes the UI's brightness from <see cref="StartBrightness"/> to <see cref="EndBrightness"/>,
/// overwriting its previous brightness.
/// </summary>
public sealed class BrightnessCommand : UIMotionCommand
{
    /// <summary>The starting brightness (0-255).</summary>
    public byte StartBrightness { get; set; }

    /// <summary>The ending brightness (0-255).</summary>
    public byte EndBrightness { get; set; }

    /// <inheritdoc/>
    public override UIMotionCommandType Type => UIMotionCommandType.Brightness;

    private protected override int FieldsSize => 4;

    internal override void ReadFields(EndianReader reader)
    {
        StartBrightness = reader.ReadByte();
        EndBrightness = reader.ReadByte();
        reader.ReadBytes(2); // padding, always zero
    }

    internal override void WriteFields(EndianWriter writer)
    {
        writer.Write(StartBrightness);
        writer.Write(EndBrightness);
        writer.Write(new byte[2]); // padding
    }
}
