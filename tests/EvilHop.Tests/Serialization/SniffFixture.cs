using EvilHop.Blocks;
using EvilHop.Primitives;

namespace EvilHop.Tests.Serialization;

/// <summary>
/// Hand-builds the raw bytes of a minimal HIP archive for <see cref="Serializer.Sniff"/> tests,
/// alongside <see cref="BlockBytes"/>'s per-block helpers. Every parameter is optional - omitting
/// one omits its block (or, for <see cref="AssetTypes"/>/<see cref="LayerRawType"/>, its entries)
/// entirely, rather than writing a zeroed placeholder.
/// </summary>
internal sealed class SniffFixture
{
    public ClientVersion? ClientVersion { get; init; }
    public PackFlags? Flags { get; init; }
    public IReadOnlyList<string>? PlatformStrings { get; init; }
    public DateTimeOffset? Created { get; init; }
    public IReadOnlyList<string> AssetTypes { get; init; } = [];
    public uint? LayerRawType { get; init; }
    public uint LayerDebugValue { get; init; } = 0xFFFFFFFF;
    public bool DpakPaddingPresent { get; init; }
    public byte DpakFillByte { get; init; } = 0x33;

    public byte[] Build()
    {
        byte[] hipa = BlockBytes.Build("HIPA", []);
        byte[] pack = BlockBytes.Build("PACK", [.. BuildPackChildren()]);
        byte[] dict = BlockBytes.Build("DICT", [.. BuildAtoc(), .. BuildLtoc()]);
        byte[] strm = BlockBytes.Build("STRM", [.. BuildDhdr(), .. BuildDpak()]);

        return [.. hipa, .. pack, .. dict, .. strm];
    }

    private IEnumerable<byte> BuildPackChildren()
    {
        if (ClientVersion is EvilHop.Blocks.ClientVersion clientVersion)
            foreach (byte b in BlockBytes.Build("PVER", BlockBytes.Content(w =>
            {
                w.Write(2u); // SubVersion, unused by Sniff
                w.Write((uint)clientVersion);
                w.Write(1u); // CompatVersion, unused by Sniff
            })))
                yield return b;

        if (Flags is PackFlags flags)
            foreach (byte b in BlockBytes.Build("PFLG", BlockBytes.Content(w => w.Write((uint)flags))))
                yield return b;

        if (Created is DateTimeOffset created)
            // Sniff only reads the raw timestamp, so the block needs no trailing date string.
            foreach (byte b in BlockBytes.Build("PCRT", BlockBytes.Content(w => w.Write((uint)created.ToUnixTimeSeconds()))))
                yield return b;

        if (PlatformStrings is not null)
            foreach (byte b in BlockBytes.Build("PLAT", BlockBytes.Content(w =>
            {
                foreach (string s in PlatformStrings) w.WriteEvilString(s);
            })))
                yield return b;
    }

    private IEnumerable<byte> BuildAtoc()
    {
        List<byte> content = [.. BlockBytes.Build("AINF", BlockBytes.Content(w => w.Write(0u)))];

        uint id = 0;
        foreach (string type in AssetTypes)
            // Sniff reads only Id + Type, then skips the rest by declared size - ADBG is never
            // entered, so the AHDR needs no real ADBG content behind it.
            content.AddRange(BlockBytes.Build("AHDR", BlockBytes.Content(w =>
            {
                w.Write(id++);
                w.Write(System.Text.Encoding.ASCII.GetBytes(type));
                w.Write(0u); w.Write(0u); w.Write(0u); w.Write(0u); // Offset, Size, Plus, Flags
            })));

        return BlockBytes.Build("ATOC", [.. content]);
    }

    private IEnumerable<byte> BuildLtoc()
    {
        List<byte> content = [.. BlockBytes.Build("LINF", BlockBytes.Content(w => w.Write(0u)))];

        if (LayerRawType is uint layerRawType)
        {
            byte[] ldbg = BlockBytes.Build("LDBG", BlockBytes.Content(w => w.Write(LayerDebugValue)));
            byte[] lhdrContent = BlockBytes.Content(w =>
            {
                w.Write(layerRawType);
                w.Write(0u); // AssetCount = 0 - no ids to keep the fixture minimal
            });
            content.AddRange(BlockBytes.Build("LHDR", [.. lhdrContent, .. ldbg]));
        }

        return BlockBytes.Build("LTOC", [.. content]);
    }

    private static byte[] BuildDhdr() => BlockBytes.Build("DHDR", BlockBytes.Content(w => w.Write(0xFFFFFFFF)));

    private byte[] BuildDpak()
    {
        if (!DpakPaddingPresent) return BlockBytes.Build("DPAK", []);

        return BlockBytes.Build("DPAK", BlockBytes.Content(w =>
        {
            w.Write(4u); // PaddingAmount
            w.Write(new[] { DpakFillByte, DpakFillByte, DpakFillByte, DpakFillByte });
        }));
    }
}
