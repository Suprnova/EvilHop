using EvilHop.Blocks;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

/// <summary>
/// The base <see cref="Serializer"/>'s registrations and nothing else - the registry test's
/// baseline, and a demonstration that deriving from <see cref="Serializer"/> directly (rather than
/// from a specific game) is a supported escape hatch, since <see cref="N100FSerializer"/> is sealed.
/// Also exposes <see cref="Serializer.ReadBlock"/> and <see cref="Serializer.WriteBlock"/> for
/// per-block-type tests, which need to read or write a single block (envelope + fields) without
/// going through the full <see cref="Serializer.Read"/>/<see cref="Serializer.Write"/> entry points.
/// </summary>
internal sealed class TestSerializer : Serializer
{
    public TestSerializer() : base(N100FSerializer.DefaultProfile) { }

    public TestSerializer(FormatProfile profile) : base(profile) { }

    public Block ReadBlockPublic(EndianReader reader) => ReadBlock(reader);

    public void WriteBlockPublic(EndianWriter writer, Block block) => WriteBlock(writer, block);
}
