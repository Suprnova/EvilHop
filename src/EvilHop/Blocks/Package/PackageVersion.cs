namespace EvilHop.Blocks;

/// <summary>
/// A child <see cref="Block"/> of <see cref="Package"/> that contains information
/// about the version of the archive.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/HIP_(File_Format)#PVER">Heavy Iron Modding documentation</seealso>
/// </remarks>
/// Validation TODO: No children.
public class PackageVersion : Block
{
    /// <inheritdoc/>
    protected internal override string Tag => "PVER";

    /// <summary>
    /// Unknown. Always 2.
    /// </summary>
    /// Validation TODO: Always 2.
    public uint SubVersion { get; set; }

    /// <summary>
    /// Indicate the version of the client consuming the archive.
    /// </summary>
    /// Validation TODO: Always 0x00000001 in N100F proto, 0x00040006 in N100F,
    /// and 0X000A000F in all others.
    public ClientVersion ClientVersion { get; set; }

    /// <summary>
    /// Unknown. Always 1.
    /// </summary>
    /// Validation TODO: Always 1.
    public uint CompatVersion { get; set; }

    internal PackageVersion() { }
}

#pragma warning disable CS1591 // Missing XML comment

/// <summary>
/// Represents all known values for <see cref="PackageVersion.ClientVersion"/>.
/// </summary>
public enum ClientVersion : uint
{
    N100FPrototype = 0x00000001,
    N100FRelease = 0x00040006,
    Default = 0x000A000F
}
