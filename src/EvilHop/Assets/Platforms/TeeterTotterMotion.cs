using EvilHop.Common;
using EvilHop.Primitives;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="PlatformMotion"/> that tilts under the player's weight.
/// </summary>
public sealed class TeeterTotterMotion : PlatformMotion
{
    /// <summary>The platform's initial tilt, in radians.</summary>
    public float InitialTilt { get; set; }

    /// <summary>The platform's maximum tilt, in radians.</summary>
    public float MaxTilt { get; set; }

    /// <summary>Determines how quickly the platform tilts.</summary>
    public float InverseMass { get; set; }

    /// <summary>
    /// Unknown; its values look like an <see cref="AssetId"/>. <see cref="GameVersion.ROTU"/> only.
    /// </summary>
    public uint Unknown { get; set; }

    internal override PlatformType PlatformType => PlatformType.TeeterTotter;

    private protected override void ReadFields(EndianReader reader, GameVersion game)
    {
        InitialTilt = reader.ReadSingle();
        MaxTilt = reader.ReadSingle();
        InverseMass = reader.ReadSingle();
        if (game is GameVersion.ROTU) Unknown = reader.ReadUInt32();
    }

    private protected override void WriteFields(EndianWriter writer, GameVersion game)
    {
        writer.Write(InitialTilt);
        writer.Write(MaxTilt);
        writer.Write(InverseMass);
        if (game is GameVersion.ROTU) writer.Write(Unknown);
    }
}
