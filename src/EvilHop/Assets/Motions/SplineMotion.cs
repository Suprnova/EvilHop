using EvilHop.Common;
using EvilHop.Primitives;

namespace EvilHop.Assets;

/// <summary>
/// An <see cref="EntityMotion"/> that follows a <see cref="AssetType.Spline"/>.
/// </summary>
/// <remarks>
/// Does nothing in <see cref="GameVersion.N100F"/> and <see cref="GameVersion.BFBB"/>, which store
/// only <see cref="SplineId"/>'s slot.
/// </remarks>
public sealed class SplineMotion : EntityMotion
{
    /// <summary>
    /// The <see cref="AssetId"/> of the <see cref="AssetType.Spline"/> to follow. Named
    /// <c>unknown</c> in <see cref="GameVersion.N100F"/> and <see cref="GameVersion.BFBB"/>.
    /// </summary>
    public AssetId SplineId { get; set; }

    /// <summary>
    /// The speed to follow the spline at. Not present in <see cref="GameVersion.N100F"/> or
    /// <see cref="GameVersion.BFBB"/>.
    /// </summary>
    public float Speed { get; set; }

    /// <summary>
    /// Unknown. Always 0 in known files. Not present in <see cref="GameVersion.N100F"/> or
    /// <see cref="GameVersion.BFBB"/>.
    /// </summary>
    public float LeanModifier { get; set; }

    private protected override MotionType Type => MotionType.Spline;

    private protected override void ReadFields(EndianReader reader, GameVersion game)
    {
        SplineId = reader.ReadAssetId();
        if (IsBFBBOrEarlier(game)) return;

        Speed = reader.ReadSingle();
        LeanModifier = reader.ReadSingle();
    }

    private protected override void WriteFields(EndianWriter writer, GameVersion game)
    {
        writer.Write(SplineId);
        if (IsBFBBOrEarlier(game)) return;

        writer.Write(Speed);
        writer.Write(LeanModifier);
    }
}
