using EvilHop.Blocks;
using EvilHop.Common;

namespace EvilHop.Models;

public readonly struct AssetView(AssetHeader header, StreamData stream, uint offset)
{
    private readonly AssetHeader _header = header;
    private readonly StreamData _stream = stream;
    private readonly uint _offset = offset;

    private readonly AssetDebug? _debug = header.Debug;

    public string Name
    {
        get => _debug?.Name ?? String.Empty;
        set => _debug?.Name = value;
    }

    public AssetType Type
    {
        get => _header.Type;
        set => _header.Type = value;
    }

    public uint Size => _header.Size;

    public string FileName
    {
        get => _debug?.FileName ?? String.Empty;
        set => _debug?.FileName = value;
    }

    public AssetFlags Flags
    {
        get => _header.Flags;
        set => _header.Flags = value;
    }

    public ReadOnlySpan<byte> GetBytes() => new Span<byte>(_stream.Data, (int)_offset, (int)_header.Size);
}
