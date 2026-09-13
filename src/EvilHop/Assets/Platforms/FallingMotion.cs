using EvilHop.Common;
using EvilHop.Primitives;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="PlatformMotion"/> for a falling platform.
/// </summary>
public sealed class FallingMotion : PlatformMotion
{
    /// <summary>Unknown.</summary>
    public float Speed { get; set; }

    /// <summary>An unknown <see cref="AssetType.Model"/> <see cref="AssetId"/>.</summary>
    public AssetId BustModelId { get; set; }

    internal override PlatformType PlatformType => PlatformType.Falling;

    private protected override void ReadFields(EndianReader reader, GameVersion _)
    {
        Speed = reader.ReadSingle();
        BustModelId = reader.ReadAssetId();
    }

    private protected override void WriteFields(EndianWriter writer, GameVersion _)
    {
        writer.Write(Speed);
        writer.Write(BustModelId);
    }
}
