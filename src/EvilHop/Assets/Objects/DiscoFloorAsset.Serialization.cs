using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;
using System.Text;

namespace EvilHop.Assets;

public sealed partial class DiscoFloorAsset
{
    /// <summary>
    /// The size, in bytes, of the fixed <c>z_disco_floor_asset</c> struct from <c>flags</c> through
    /// <c>states_size</c> - i.e. everything between <see cref="BaseAssetPrefix"/> and
    /// <see cref="BaseAsset.Links"/>.
    /// </summary>
    private const int HeaderSize = 36;

    // Each prefix string is stored null-terminated and padded to a 4-byte alignment, and real
    // archives leave that trailing padding as whatever the authoring tool's buffer already held
    // rather than zeroing it. These cache the exact bytes read so a codec can replay them verbatim
    // as long as the corresponding prefix hasn't changed since Read, instead of always zeroing them.
    private byte[]? _rawOffPrefix;
    private byte[]? _rawTransitionPrefix;
    private byte[]? _rawOnPrefix;

    // The state bitmask array, as a whole, is padded to a 4-byte alignment at its very end - and,
    // like every other padding region in this format, real archives leave those bytes as whatever
    // the authoring tool's buffer already held rather than zeroing them.
    private byte[]? _rawTrailingPadding;

    internal static DiscoFloorAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile _)
    {
        var asset = new DiscoFloorAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        long bodyStart = reader.BaseStream.Position;

        asset.Flags = (DiscoFloorFlags)reader.ReadUInt32();
        asset.TransitionDuration = reader.ReadSingle();
        asset.StateDuration = reader.ReadSingle();
        uint offPrefixOffset = reader.ReadUInt32();
        uint transitionPrefixOffset = reader.ReadUInt32();
        uint onPrefixOffset = reader.ReadUInt32();
        uint tileCount = reader.ReadUInt32();
        uint statesOffset = reader.ReadUInt32();
        uint stateCount = reader.ReadUInt32();

        // Placed immediately after the fixed struct above, not addressed by any offset field of its
        // own - mirrors every other BaseAsset's link placement.
        LinkSerialization.Read(asset, reader, asset.Physical.LinkCount);
        asset.Physical.LinkCount = (byte)asset.Links.Count;

        // Each prefix's slice runs up to the next prefix's offset (or states_offset, for the last
        // one) - reading the whole slice, rather than stopping at the null terminator, is what lets
        // its trailing padding be captured for a byte-exact rewrite.
        reader.BaseStream.Position = bodyStart + offPrefixOffset;
        asset._rawOffPrefix = reader.ReadBytes((int)(transitionPrefixOffset - offPrefixOffset));
        asset.OffPrefix = DecodeNullTerminated(asset._rawOffPrefix);

        asset._rawTransitionPrefix = reader.ReadBytes((int)(onPrefixOffset - transitionPrefixOffset));
        asset.TransitionPrefix = DecodeNullTerminated(asset._rawTransitionPrefix);

        asset._rawOnPrefix = reader.ReadBytes((int)(statesOffset - onPrefixOffset));
        asset.OnPrefix = DecodeNullTerminated(asset._rawOnPrefix);

        int maskByteSize = StateMaskByteSize(tileCount);
        long maxOffsetTouched = statesOffset + 4L * stateCount;

        reader.BaseStream.Position = bodyStart + statesOffset;
        var stateOffsets = new uint[stateCount];
        for (int i = 0; i < stateCount; i++)
            stateOffsets[i] = reader.ReadUInt32();

        for (int i = 0; i < stateCount; i++)
        {
            reader.BaseStream.Position = bodyStart + stateOffsets[i];
            byte[] mask = reader.ReadBytes(maskByteSize);
            asset.States.Add(ReadState(mask, tileCount));

            maxOffsetTouched = Math.Max(maxOffsetTouched, stateOffsets[i] + maskByteSize);
        }

        asset.Physical.TileCount = asset.ComputedTileCount;
        asset.Physical.StateCount = (uint)asset.States.Count;

        reader.BaseStream.Position = bodyStart + maxOffsetTouched;
        long alignedEnd = (maxOffsetTouched + 3) & ~3L;
        asset._rawTrailingPadding = reader.ReadBytes((int)(alignedEnd - maxOffsetTouched));

        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(DiscoFloorAsset asset, EndianWriter writer, FormatProfile _)
    {
        BaseAssetPrefix.Write(asset, writer);

        uint linkCount = asset.Physical.LinkCount;
        uint tileCount = asset.Physical.TileCount;
        uint stateCount = asset.Physical.StateCount;
        int maskByteSize = StateMaskByteSize(tileCount);

        byte[] offPrefixBytes = EncodePrefix(asset.OffPrefix, asset._rawOffPrefix);
        byte[] transitionPrefixBytes = EncodePrefix(asset.TransitionPrefix, asset._rawTransitionPrefix);
        byte[] onPrefixBytes = EncodePrefix(asset.OnPrefix, asset._rawOnPrefix);

        uint offPrefixOffset = HeaderSize + linkCount * 32;
        uint transitionPrefixOffset = offPrefixOffset + (uint)offPrefixBytes.Length;
        uint onPrefixOffset = transitionPrefixOffset + (uint)transitionPrefixBytes.Length;
        uint statesOffset = onPrefixOffset + (uint)onPrefixBytes.Length;
        uint bitmaskRegionStart = statesOffset + stateCount * 4;

        writer.Write((uint)asset.Flags);
        writer.Write(asset.TransitionDuration);
        writer.Write(asset.StateDuration);
        writer.Write(offPrefixOffset);
        writer.Write(transitionPrefixOffset);
        writer.Write(onPrefixOffset);
        writer.Write(tileCount);
        writer.Write(statesOffset);
        writer.Write(stateCount);

        LinkSerialization.Write(asset, writer);

        writer.Write(offPrefixBytes);
        writer.Write(transitionPrefixBytes);
        writer.Write(onPrefixBytes);

        for (int i = 0; i < asset.States.Count; i++)
            writer.Write(bitmaskRegionStart + (uint)(i * maskByteSize));

        int totalMaskBytes = 0;
        foreach (var state in asset.States)
        {
            writer.Write(EncodeStateMask(state, tileCount, maskByteSize));
            totalMaskBytes += maskByteSize;
        }

        int trailingPaddingLength = (4 - totalMaskBytes % 4) % 4;
        byte[]? rawTrailingPadding = asset._rawTrailingPadding;
        writer.Write(rawTrailingPadding is not null && rawTrailingPadding.Length == trailingPaddingLength
            ? rawTrailingPadding
            : new byte[trailingPaddingLength]);

        writer.Write(asset.GetUnparsedTail());
    }

    /// <summary>The byte length of one state's tile bitmask: 2 bits per tile, rounded up to a whole byte.</summary>
    private static int StateMaskByteSize(uint tileCount) => (int)((tileCount * 2 + 7) / 8);

    private static DiscoFloorState ReadState(byte[] mask, uint tileCount)
    {
        var state = new DiscoFloorState();
        for (int i = 0; i < tileCount; i++)
            state.Tiles.Add(GetTile(mask, i));
        state.SetRawMask(mask);
        return state;
    }

    /// <summary>
    /// Encodes <paramref name="state"/>'s mask, replaying <paramref name="state"/>'s captured raw
    /// bytes verbatim - including whatever garbage sits in bit pairs beyond <paramref name="tileCount"/>
    /// within the last byte - as long as they still decode to its current <see cref="DiscoFloorState.Tiles"/>.
    /// Falls back to a freshly zero-filled mask once <see cref="DiscoFloorState.Tiles"/> no longer
    /// matches (or none was ever captured).
    /// </summary>
    private static byte[] EncodeStateMask(DiscoFloorState state, uint tileCount, int maskByteSize)
    {
        byte[]? raw = state.GetRawMask();
        if (raw is not null && raw.Length == maskByteSize && MatchesTiles(raw, state.Tiles, tileCount))
            return raw;

        byte[] mask = new byte[maskByteSize];
        for (int i = 0; i < tileCount; i++)
            SetTile(mask, i, i < state.Tiles.Count ? state.Tiles[i] : TileState.Off);
        return mask;
    }

    private static bool MatchesTiles(byte[] mask, Collection<TileState> tiles, uint tileCount)
    {
        if (tiles.Count != tileCount)
            return false;

        for (int i = 0; i < tileCount; i++)
        {
            if (GetTile(mask, i) != tiles[i])
                return false;
        }

        return true;
    }

    // Tiles are packed 4 to a byte, LSB first: tile 0 in bits 0-1, tile 1 in bits 2-3, and so on.
    private static TileState GetTile(byte[] mask, int index) =>
        (TileState)((mask[index >> 2] >> ((index & 3) << 1)) & 3);

    private static void SetTile(byte[] mask, int index, TileState value)
    {
        int byteIndex = index >> 2;
        int shift = (index & 3) << 1;
        mask[byteIndex] = (byte)((mask[byteIndex] & ~(3 << shift)) | ((int)value << shift));
    }

    private static string DecodeNullTerminated(byte[] bytes)
    {
        int nullIndex = Array.IndexOf(bytes, (byte)0);
        return Encoding.Latin1.GetString(bytes, 0, nullIndex >= 0 ? nullIndex : bytes.Length); // todo: latin?
    }

    /// <summary>
    /// Encodes <paramref name="text"/> null-terminated and padded to a 4-byte alignment, replaying
    /// <paramref name="raw"/> verbatim - including its trailing padding garbage - when it still
    /// decodes to <paramref name="text"/>. Falls back to a freshly zero-padded encoding once
    /// <paramref name="text"/> no longer matches (or nothing was ever captured).
    /// </summary>
    private static byte[] EncodePrefix(string text, byte[]? raw)
    {
        if (raw is not null && DecodeNullTerminated(raw) == text)
            return raw;

        byte[] bytes = Encoding.Latin1.GetBytes(text);
        var padded = new byte[(bytes.Length + 1 + 3) & ~3];
        bytes.CopyTo(padded, 0);
        return padded;
    }
}
