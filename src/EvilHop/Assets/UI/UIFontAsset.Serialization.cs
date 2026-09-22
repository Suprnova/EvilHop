using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

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
        asset.TopLeftUV = reader.ReadVector2();
        asset.TopRightUV = reader.ReadVector2();
        asset.BottomRightUV = reader.ReadVector2();
        asset.BottomLeftUV = reader.ReadVector2();

        asset.FontFlags = (Behavior)reader.ReadUInt16();
        asset.Mode = (FormattingMode)reader.ReadByte();
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

        for (var i = 0; i < asset.Physical.LinkCount; i++)
            asset.Links.Add(Link.Read(reader, profile));
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(UIFontAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);
        EntityAssetPrefix.Write(asset, writer, profile);

        writer.Write((uint)asset.Flags);
        writer.Write(asset.Width);
        writer.Write(asset.Height);
        writer.Write(asset.TextureId);
        writer.Write(asset.TopLeftUV);
        writer.Write(asset.TopRightUV);
        writer.Write(asset.BottomRightUV);
        writer.Write(asset.BottomLeftUV);

        writer.Write((ushort)asset.FontFlags);
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

        foreach (var link in asset.Links)
            Link.Write(link, writer, profile);
        writer.Write(asset.GetUnparsedTail());
    }
}
