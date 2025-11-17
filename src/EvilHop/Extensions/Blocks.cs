using EvilHop.Blocks;

namespace EvilHop.Extensions;

/// <summary>
/// This class extends multiple types to support handling block types for input.
/// </summary>
public static class Blocks
{
    extension(BinaryReader reader)
    {
        public HIPA ReadHIPA() => new(reader);
        public Package ReadPACK() => new(reader);
        public Package.PackageVersion ReadPVER() => new(reader);
        public Package.PackageFlags ReadPFLG() => new(reader);
        public Package.PackageCount ReadPCNT() => new(reader);
        public Package.PackageCreated ReadPCRT() => new(reader);
        public Package.PackageModified ReadPMOD() => new(reader);
    }
}