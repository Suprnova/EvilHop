using EvilHop.Primitives;

namespace EvilHop.Assets;

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

    internal override void ReadFields(EndianReader reader)
    {
        StartOpacity = reader.ReadByte();
        EndOpacity = reader.ReadByte();
        reader.ReadBytes(2); // padding, always zero
    }

    internal override void WriteFields(EndianWriter writer)
    {
        writer.Write(StartOpacity);
        writer.Write(EndOpacity);
        writer.Write(new byte[2]); // padding
    }
}
