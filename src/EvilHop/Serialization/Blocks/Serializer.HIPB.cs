using EvilHop.Blocks;
using EvilHop.Primitives;
using System.Diagnostics.CodeAnalysis;

namespace EvilHop.Serialization;

public partial class Serializer
{
    /// <summary>
    /// Reads the fields of a <see cref="HIPB"/> (HIPB) block. Every field past
    /// <see cref="HIPB.Version"/> is gated by it, matching HipHopFile's own read order exactly, so
    /// any version that library ever wrote round-trips.
    /// </summary>
    /// <remarks>
    /// HIPB is unofficial, always last in the archive, and nothing depends on its content - so
    /// malformed content degrades instead of throwing.
    /// </remarks>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "HIPB is unofficial and best-effort; any malformed content must degrade rather than fail the whole archive.")]
    protected static void ReadHIPB(EndianReader reader, HIPB block, uint size)
    {
        long contentStart = reader.BaseStream.Position;
        try
        {
            uint version = reader.ReadUInt32();
            block.Version = version;

            if (version >= 1)
                block.HasNoLayers = reader.ReadUInt32();

            if (version >= 2)
            {
                block.Platform = (HIPBPlatform)reader.ReadUInt32();

                uint layerNameCount = reader.ReadUInt32();
                for (uint i = 0; i < layerNameCount; i++)
                {
                    int index = reader.ReadInt32();
                    block.LayerNames[index] = reader.ReadEvilString();
                }
            }

            if (version >= 3)
                block.Game = (HIPBGame)reader.ReadUInt32();
        }
        catch (Exception)
        {
            // Whatever fields were read before the failure stay set; the reader is unconditionally
            // repositioned to the block's declared end below regardless of how far parsing got.
        }

        reader.BaseStream.Position = contentStart + size;
    }

    /// <summary>
    /// Writes the fields of a <see cref="HIPB"/> (HIPB) block, gated by
    /// <see cref="HIPB.Version"/> exactly as <see cref="ReadHIPB"/> reads them.
    /// </summary>
    protected static void WriteHIPB(EndianWriter writer, HIPB block)
    {
        writer.Write(block.Version);

        if (block.Version >= 1)
            writer.Write(block.HasNoLayers);

        if (block.Version >= 2)
        {
            writer.Write((uint)block.Platform);

            writer.Write(block.LayerNames.Count);
            foreach (var (index, name) in block.LayerNames)
            {
                writer.Write(index);
                writer.WriteEvilString(name);
            }
        }

        if (block.Version >= 3)
            writer.Write((uint)block.Game);
    }
}
