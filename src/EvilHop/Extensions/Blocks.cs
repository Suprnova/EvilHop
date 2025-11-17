using EvilHop.Blocks;
using System.Buffers.Binary;

namespace EvilHop.Extensions;

/// <summary>
/// This class extends multiple types to support handling block types for input.
/// </summary>
public static class Blocks
{
    extension(BinaryReader reader)
    {
        public HIPA ReadHIPA() => new(reader);
    }
}