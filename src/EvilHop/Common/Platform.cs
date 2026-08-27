namespace EvilHop.Common;

/// <summary>
/// The console family a HIP archive's assets were built for.
/// </summary>
public enum Platform
{
    /// <summary>Big-endian PowerPC.</summary>
    GameCube,

    /// <summary>Little-endian x86.</summary>
    Xbox,

    /// <summary>Little-endian MIPS.</summary>
    PlayStation2
}
