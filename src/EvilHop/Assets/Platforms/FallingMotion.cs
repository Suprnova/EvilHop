using EvilHop.Common;
using EvilHop.Primitives;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="PlatformMotion"/> for a falling platform.
/// </summary>
/// <remarks>
/// No known file uses this motion, and <see cref="GameVersion.BFBB"/>'s code never reads it.
/// </remarks>
public sealed class FallingMotion : PlatformMotion
{
    /// <summary>Unknown.</summary>
    public float Speed { get; set; }

    /// <summary>The <see cref="AssetId"/> of a <see cref="AssetType.Model"/>, named <c>bustModelID</c> in source.</summary>
    public AssetId BustModelId { get; set; }

    internal override PlatformType PlatformType => PlatformType.Falling;

    private protected override void ReadFields(EndianReader reader, GameVersion game)
    {
        Speed = reader.ReadSingle();
        BustModelId = reader.ReadAssetId();
    }

    private protected override void WriteFields(EndianWriter writer, GameVersion game)
    {
        writer.Write(Speed);
        writer.Write(BustModelId);
    }
}
