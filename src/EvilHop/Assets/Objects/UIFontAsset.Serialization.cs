using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Assets;

public sealed partial class UIFontAsset
{
    internal static UIFontAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new UIFontAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);
        EntityAssetPrefix.Read(asset, reader, profile);

        asset.Flags = (UIFlags)reader.ReadUInt32();
        asset.Width = reader.ReadUInt16();
        asset.Height = reader.ReadUInt16();
        asset.TextureId = reader.ReadAssetId();
        asset.TopLeftUV = ReadVector2(reader);
        asset.TopRightUV = ReadVector2(reader);
        asset.BottomRightUV = ReadVector2(reader);
        asset.BottomLeftUV = ReadVector2(reader);

        asset.FontFlags = (UIFontFlags)reader.ReadUInt16();
        asset.Mode = (UIFontMode)reader.ReadByte();
        asset.FontId = reader.ReadByte();
        asset.TextId = reader.ReadAssetId();
        asset.BackdropColor = reader.ReadRgba32();
        asset.Color = reader.ReadRgba32();
        asset.InsetTop = reader.ReadInt16();
        asset.InsetBottom = reader.ReadInt16();
        asset.InsetLeft = reader.ReadInt16();
        asset.InsetRight = reader.ReadInt16();
        asset.SpaceX = reader.ReadInt16();
        asset.SpaceY = reader.ReadInt16();
        asset.CharacterWidth = reader.ReadInt16();
        asset.CharacterHeight = reader.ReadInt16();

        if (profile.Game == GameVersion.BFBB)
            asset.MaxHeight = reader.ReadUInt32();

        LinkSerialization.Read(asset, reader, asset.Physical.LinkCount);
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(UIFontAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);
        EntityAssetPrefix.Write(asset, writer, profile);

        writer.Write((uint)asset.Flags);
        writer.Write((short)asset.Width);
        writer.Write((short)asset.Height);
        writer.Write(asset.TextureId);
        WriteVector2(writer, asset.TopLeftUV);
        WriteVector2(writer, asset.TopRightUV);
        WriteVector2(writer, asset.BottomRightUV);
        WriteVector2(writer, asset.BottomLeftUV);

        writer.Write((short)(ushort)asset.FontFlags);
        writer.Write((byte)asset.Mode);
        writer.Write(asset.FontId);
        writer.Write(asset.TextId);
        writer.WriteRgba32(asset.BackdropColor);
        writer.WriteRgba32(asset.Color);
        writer.Write(asset.InsetTop);
        writer.Write(asset.InsetBottom);
        writer.Write(asset.InsetLeft);
        writer.Write(asset.InsetRight);
        writer.Write(asset.SpaceX);
        writer.Write(asset.SpaceY);
        writer.Write(asset.CharacterWidth);
        writer.Write(asset.CharacterHeight);

        if (profile.Game == GameVersion.BFBB)
            writer.Write(asset.MaxHeight);

        LinkSerialization.Write(asset, writer);
        writer.Write(asset.GetUnparsedTail());
    }

    private static Vector2 ReadVector2(EndianReader reader) => new(reader.ReadSingle(), reader.ReadSingle());

    private static void WriteVector2(EndianWriter writer, Vector2 value)
    {
        writer.Write(value.X);
        writer.Write(value.Y);
    }
}
