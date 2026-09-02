namespace EvilHop.Serialization.Sniffing;

/// <summary>
/// A forward-only <see cref="Stream"/> wrapper that tracks bytes read as <see cref="Position"/>, so
/// <see cref="SniffScanner"/> can budget-track blocks with <see cref="BinaryReader.ReadBytes(int)"/>
/// instead of relying on the underlying stream's own <see cref="Stream.Position"/> - works whether or
/// not that stream supports seeking.
/// </summary>
/// <param name="inner">The stream to read from.</param>
internal sealed class CountingStream(Stream inner) : Stream
{
    private long _bytesRead;

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => _bytesRead;
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

    public override int Read(Span<byte> buffer)
    {
        int read = inner.Read(buffer);
        _bytesRead += read;
        return read;
    }

    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
