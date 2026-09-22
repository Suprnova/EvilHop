using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;

namespace EvilHop.Assets;

public partial class DiscoFloorAsset
{
    /// <summary>
    /// One step of a <see cref="DiscoFloorAsset"/>'s pattern: the <see cref="TileState"/> every tile is
    /// set to during this step.
    /// </summary>
    public sealed class DiscoFloorState
    {
        /// <summary>
        /// Each tile's state during this step, in tile order.
        /// </summary>
        public Collection<TileState> Tiles { get; } = [];

        // The on-disk mask is byte-aligned, so a tile count not divisible by 4 leaves a few unused bit
        // pairs in its last byte - real archives leave those set to whatever the authoring tool's buffer
        // happened to already hold, not zero. Caching the exact bytes here lets a codec replay them
        // verbatim as long as Tiles hasn't changed since Read, instead of always zeroing them.
        private byte[]? _rawMask;

        /// <summary>The byte length of one state's tile bitmask: 2 bits per tile, rounded up to a whole byte.</summary>
        internal static int MaskByteSize(uint tileCount) => (int)((tileCount * 2 + 7) / 8);

        internal static DiscoFloorState Read(EndianReader reader, uint tileCount, FormatProfile _)
        {
            byte[] mask = reader.ReadBytes(MaskByteSize(tileCount));

            var state = new DiscoFloorState();
            for (int i = 0; i < tileCount; i++)
                state.Tiles.Add(GetTile(mask, i));
            state._rawMask = mask;
            return state;
        }

        /// <summary>
        /// Writes <paramref name="state"/>'s mask, replaying its captured raw bytes verbatim - including
        /// whatever garbage sits in bit pairs beyond <paramref name="tileCount"/> within the last byte -
        /// as long as they still decode to its current <see cref="Tiles"/>. Falls back to a freshly
        /// zero-filled mask once <see cref="Tiles"/> no longer matches (or none was ever captured).
        /// </summary>
        internal static void Write(DiscoFloorState state, EndianWriter writer, uint tileCount, FormatProfile _)
        {
            int maskByteSize = MaskByteSize(tileCount);
            byte[]? raw = state._rawMask;
            byte[] mask = raw is not null && raw.Length == maskByteSize && MatchesTiles(raw, state.Tiles, tileCount)
                ? raw
                : EncodeMask(state.Tiles, tileCount, maskByteSize);

            writer.Write(mask);
        }

        private static byte[] EncodeMask(Collection<TileState> tiles, uint tileCount, int maskByteSize)
        {
            byte[] mask = new byte[maskByteSize];
            for (int i = 0; i < tileCount; i++)
                SetTile(mask, i, i < tiles.Count ? tiles[i] : TileState.Off);
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
    }
}
