using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// Tracks a single integer value that can be incremented, decremented, reset, and queried by other
/// assets via links.
/// </summary>
/// <remarks>
/// <para>
/// A counter can be in a normal or expired state. In the normal state, its value can be freely
/// changed; if a change ever sets it to 0, it becomes expired, and stays that way - ignoring further
/// changes - until explicitly reset back to <see cref="InitialValue"/>. None of this runtime state is
/// stored in the archive.
/// </para>
/// <seealso href="https://heavyironmodding.org/wiki/CNTR">Heavy Iron Modding documentation</seealso>
/// Validation TODO: Physical.BaseType is always 0x16.
/// </remarks>
public sealed class CounterAsset : BaseAsset
{
    /// <summary>
    /// The counter's value when the level loads.
    /// </summary>
    public short InitialValue { get; set; }

    // Not publicly constructible yet - building a new asset from scratch, rather than reading one
    // from an archive, doesn't have a settled API (defaults will likely need to depend on the
    // target game), so this stays internal until that exists.
    internal CounterAsset() { }

    internal static CounterAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new CounterAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);
        asset.InitialValue = reader.ReadInt16();
        reader.ReadInt16(); // 2 bytes of padding, always zero
        LinkSerialization.Read(asset, reader, asset.Physical.LinkCount);
        asset.Physical.LinkCount = (byte)asset.Links.Count; // now agrees - lets it derive
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(CounterAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);
        writer.Write(asset.InitialValue);
        writer.Write((short)0); // padding
        LinkSerialization.Write(asset, writer);
        writer.Write(asset.GetUnparsedTail());
    }
}
