using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Assets;

public partial class CreditsAsset
{
    /// <summary>
    /// One texture drawn at a fixed screen position, in place of scrolling text.
    /// </summary>
    public sealed class CreditsTexture
    {
        /// <summary>
        /// The <see cref="AssetType.Texture"/> to draw.
        /// </summary>
        public AssetId TextureId { get; set; }

        /// <summary>
        /// The texture's color.
        /// </summary>
        public Rgba Color { get; set; }

        /// <summary>
        /// The texture's position, as a percentage (0 to 1) of the screen.
        /// </summary>
        public Vector2 Position { get; set; }

        /// <summary>
        /// The texture's width and height, as a percentage (0 to 1) of the screen.
        /// </summary>
        public Vector2 Size { get; set; }

        /// <summary>
        /// Unknown.
        /// </summary>
        public uint Handle { get; set; }

        /// <summary>
        /// Unknown.
        /// </summary>
        public uint Padding { get; set; }

        internal static CreditsTexture Read(EndianReader reader, FormatProfile _) => new()
        {
            TextureId = reader.ReadAssetId(),
            Color = reader.ReadRgba32(),
            Position = new Vector2(reader.ReadSingle(), reader.ReadSingle()),
            Size = new Vector2(reader.ReadSingle(), reader.ReadSingle()),
            Handle = reader.ReadUInt32(),
            Padding = reader.ReadUInt32(),
        };

        internal static void Write(CreditsTexture texture, EndianWriter writer, FormatProfile _)
        {
            writer.Write(texture.TextureId);
            writer.WriteRgba32(texture.Color);
            writer.Write(texture.Position.X);
            writer.Write(texture.Position.Y);
            writer.Write(texture.Size.X);
            writer.Write(texture.Size.Y);
            writer.Write(texture.Handle);
            writer.Write(texture.Padding);
        }
    }
}
