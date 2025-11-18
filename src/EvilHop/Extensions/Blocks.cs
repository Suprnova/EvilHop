using EvilHop.Blocks;
using static EvilHop.Blocks.Package;

namespace EvilHop.Extensions;

/// <summary>
/// This class extends the BinaryReader class to support handling block types for input.
/// </summary>
public static class Blocks
{
    extension(BinaryReader reader)
    {
        public HIPA ReadHIPA() => new(reader);
        public Package ReadPACK() => new(reader);
        public PackageVersion ReadPVER() => new(reader);
        public PackageFlags ReadPFLG() => new(reader);
        public PackageCount ReadPCNT() => new(reader);
        public PackageCreated ReadPCRT() => new(reader);
        public PackageModified ReadPMOD() => new(reader);
    }
}