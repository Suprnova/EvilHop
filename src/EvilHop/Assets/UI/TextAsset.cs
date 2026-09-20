using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Text;

namespace EvilHop.Assets;

/// <summary>
/// Defines a text string used in-game for messages, UI elements, and dialog boxes.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/TEXT">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class TextAsset() : Asset(AssetType.Text), IPhysicalTextAsset
{
    /// <summary>
    /// The text content stored in this asset.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalTextAsset Physical => this;

    private uint? _overriddenLength;
    uint IPhysicalTextAsset.Length
    {
        get => _overriddenLength ?? CalculateLength();
        set => _overriddenLength = value == CalculateLength() ? null : value;
    }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.Text"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.N100F,
        GameVersion.BFBB,
        GameVersion.TSSM,
        GameVersion.Incredibles,
        GameVersion.ROTU,
        GameVersion.Ratatouille,
    };

    internal uint CalculateLength() => (uint)Encoding.Latin1.GetByteCount(Text ?? string.Empty);

    internal static TextAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new TextAsset();
        AssetFields.Populate(asset, header, debug);

        uint rawLength = reader.ReadUInt32();
        uint length = profile.Game is GameVersion.N100F && rawLength > 0 ? rawLength - 1 : rawLength;
        byte[] textBytes = reader.ReadBytes((int)length);
        asset.Text = Encoding.Latin1.GetString(textBytes);
        asset.Physical.Length = length;

        long remaining = reader.BaseStream.Length - reader.BaseStream.Position;
        if (remaining > 0)
        {
            reader.ReadByte(); // Null terminator
            remaining--;
        }

        int padding = (4 - (((int)length + 1) & 3)) & 3;
        int paddingToRead = (int)Math.Min(padding, remaining);
        if (paddingToRead > 0)
        {
            reader.ReadBytes(paddingToRead);
        }

        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(TextAsset asset, EndianWriter writer, FormatProfile profile)
    {
        byte[] textBytes = Encoding.Latin1.GetBytes(asset.Text ?? string.Empty);
        uint onDiskLength = profile.Game is GameVersion.N100F ? asset.Physical.Length + 1 : asset.Physical.Length;
        writer.Write(onDiskLength);
        writer.Write(textBytes);
        writer.Write((byte)0); // Null terminator

        int padding = (4 - ((textBytes.Length + 1) & 3)) & 3;
        for (int i = 0; i < padding; i++)
        {
            writer.Write((byte)0);
        }

        writer.Write(asset.GetUnparsedTail());
    }
}

/// <summary>
/// An explicit interface used to interact with <see cref="TextAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalTextAsset : IPhysicalAsset
{
    /// <summary>
    /// The length of the text stored in this asset, in bytes (excluding the null terminator).
    /// </summary>
    /// <remarks>
    /// When disagreements with <see cref="TextAsset.Text"/>.Length exist, this field wins during serialization.
    /// <para>
    /// On disk, <see cref="GameVersion.N100F"/> stores this count including the null terminator;
    /// <see cref="TextAsset.Read"/> and <see cref="TextAsset.Write"/> adjust for that so this
    /// property means the same thing across every game.
    /// </para>
    /// </remarks>
    uint Length { get; set; }
}
