using EvilHop.Blocks;
using EvilHop.Primitives;

namespace EvilHop.Tests.Serialization;

/// <summary>
/// Builds the raw byte envelope (tag + size + content) for a single block, for feeding directly
/// into <see cref="TestSerializer.ReadBlockPublic"/> or <see cref="EvilHop.Serialization.Serializer.Read"/>.
/// </summary>
internal static class BlockBytes
{
    public static byte[] Build(string tag, byte[] content)
    {
        using var stream = new MemoryStream();
        using (var writer = new EndianWriter(stream, Endianness.Big, leaveOpen: true))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes(tag));
            writer.Write((uint)content.Length);
            writer.Write(content);
        }
        return stream.ToArray();
    }

    public static EndianReader Reader(string tag, byte[] content) =>
        new(new MemoryStream(Build(tag, content)), Endianness.Big);

    public static byte[] Content(Action<EndianWriter> build)
    {
        using var stream = new MemoryStream();
        using var writer = new EndianWriter(stream, Endianness.Big, leaveOpen: true);
        build(writer);
        return stream.ToArray();
    }

    /// <summary>
    /// Writes <paramref name="block"/> through <paramref name="serializer"/>'s
    /// <see cref="TestSerializer.WriteBlockPublic"/>, returning the full raw byte envelope
    /// (tag + size + content), for comparison against <see cref="Build"/>'s expected bytes.
    /// </summary>
    public static byte[] WriteBlock(TestSerializer serializer, Block block)
    {
        using var stream = new MemoryStream();
        using (var writer = new EndianWriter(stream, Endianness.Big, leaveOpen: true))
            serializer.WriteBlockPublic(writer, block);
        return stream.ToArray();
    }
}
