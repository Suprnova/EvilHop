using EvilHop.Serialization;

namespace EvilHop.Corpus;

/// <summary>
/// Resolves the <c>--serializer</c> flag to a <see cref="SerializerV1"/> instance. A stopgap until
/// a <c>FileFormatFactory</c> can sniff a stream and auto-detect its version.
/// </summary>
internal static class SerializerFactory
{
    /// <summary>
    /// Creates the serializer identified by <paramref name="id"/>.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is not a known serializer.</exception>
    public static SerializerV1 Create(string id) => id switch
    {
        "v1" => new SerializerV1(),
        _ => throw new ArgumentException($"Unknown serializer '{id}'. Known serializers: v1.")
    };
}
