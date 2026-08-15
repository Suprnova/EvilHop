using EvilHop.Primitives;

namespace EvilHop.Tests.Serialization;

/// <summary>
/// Builds the raw byte envelope (tag + size + content) for a single block, for feeding directly
/// into <see cref="TestSerializer.ReadBlockPublic"/> or <see cref="EvilHop.Serialization.SerializerV1.Read"/>.
/// </summary>
internal static class BlockBytes
{
    public static byte[] Build(string tag, byte[] content)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.ASCII, leaveOpen: true))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes(tag));
            writer.WriteEvilInt((uint)content.Length);
            writer.Write(content);
        }
        return stream.ToArray();
    }

    public static BinaryReader Reader(string tag, byte[] content) =>
        new(new MemoryStream(Build(tag, content)));

    public static byte[] Content(Action<BinaryWriter> build)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, System.Text.Encoding.ASCII, leaveOpen: true);
        build(writer);
        return stream.ToArray();
    }
}
