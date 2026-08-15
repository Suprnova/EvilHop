using EvilHop.Blocks;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

/// <summary>
/// Exposes <see cref="SerializerV1.ReadBlock"/> for per-block-type tests, which need to read a
/// single block (envelope + fields) without going through the full <see cref="SerializerV1.Read"/>
/// entry point.
/// </summary>
internal class TestSerializer : SerializerV1
{
    public Block ReadBlockPublic(BinaryReader reader) => ReadBlock(reader);
}
