using EvilHop.Blocks;
using EvilHop.Common;

namespace EvilHop.Models;

public readonly struct AssetView(AssetHeader header, StreamData stream, uint streamOffset)
{
    private readonly AssetHeader _header = header;
    private readonly StreamData _stream = stream;
    private readonly uint _streamOffset = streamOffset;

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

    public ReadOnlySpan<byte> GetBytes()
    {
        uint assetIndex = _header.Offset - _streamOffset;

        return new Span<byte>(_stream.Data, (int)assetIndex, (int)_header.Size);
    }
}
