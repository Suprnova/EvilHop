using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>
/// One text style: a font plus the color, character size, spacing, and box size text is rendered
/// with.
/// </summary>
public sealed class CreditsTextbox
{
    /// <summary>
    /// Unknown.
    /// </summary>
    public uint Font { get; set; }

    /// <summary>
    /// The text's color.
    /// </summary>
    public Rgba Color { get; set; }

    /// <summary>
    /// The character width and height, in pixels.
    /// </summary>
    public Vector2 CharSize { get; set; }

    /// <summary>
    /// The spacing between characters.
    /// </summary>
    public Vector2 CharSpacing { get; set; }

    /// <summary>
    /// The text box's maximum width and height, as a percentage (0 to 1) of the screen.
    /// </summary>
    public Vector2 Size { get; set; }

    internal static CreditsTextbox Read(EndianReader reader, FormatProfile _) => new()
    {
        Font = reader.ReadUInt32(),
        Color = reader.ReadRgba32(),
        CharSize = new Vector2(reader.ReadSingle(), reader.ReadSingle()),
        CharSpacing = new Vector2(reader.ReadSingle(), reader.ReadSingle()),
        Size = new Vector2(reader.ReadSingle(), reader.ReadSingle()),
    };

    internal static void Write(CreditsTextbox textbox, EndianWriter writer, FormatProfile _)
    {
        writer.Write(textbox.Font);
        writer.WriteRgba32(textbox.Color);
        writer.Write(textbox.CharSize.X);
        writer.Write(textbox.CharSize.Y);
        writer.Write(textbox.CharSpacing.X);
        writer.Write(textbox.CharSpacing.Y);
        writer.Write(textbox.Size.X);
        writer.Write(textbox.Size.Y);
    }
}
