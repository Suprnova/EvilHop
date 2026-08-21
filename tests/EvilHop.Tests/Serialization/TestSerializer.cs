using EvilHop.Blocks;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

/// <summary>
/// The base <see cref="Serializer"/>'s registrations and nothing else - the registry test's
/// baseline, and a demonstration that deriving from <see cref="Serializer"/> directly (rather than
/// from a specific game) is a supported escape hatch, since <see cref="N100FSerializer"/> is sealed.
/// Also exposes <see cref="Serializer.ReadBlock"/> for per-block-type tests, which need to read a
/// single block (envelope + fields) without going through the full <see cref="Serializer.Read"/>
/// entry point.
/// </summary>
internal class TestSerializer : Serializer
{
    public TestSerializer() : base(N100FSerializer.DefaultProfile) { }

    public TestSerializer(FormatProfile profile) : base(profile) { }

    public Block ReadBlockPublic(BinaryReader reader) => ReadBlock(reader);
}
