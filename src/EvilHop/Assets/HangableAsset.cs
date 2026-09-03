using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// An <see cref="EntityAsset"/> that hangs and swings from a pivot point above it, such as a
/// chandelier or vine. Not functional in <see cref="GameVersion.BFBB"/> - it displays its model but
/// does not swing or respond to being grabbed.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/HANG">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class HangableAsset : EntityAsset, IHasModel
{
    /// <summary>
    /// Behavior flags for this <see cref="HangableAsset"/>.
    /// </summary>
    public HangableFlags Flags { get; set; }

    /// <summary>
    /// The vertical offset from <see cref="EntityAsset.Position"/> up to the pivot
    /// the object swings from.
    /// </summary>
    public float PivotOffset { get; set; }

    /// <summary>
    /// The length of the arm connecting the pivot to the hanging object.
    /// </summary>
    public float LeverArm { get; set; }

    /// <summary>
    /// The strength of gravity pulling the object back towards its resting position.
    /// </summary>
    public float Gravity { get; set; }

    /// <summary>
    /// How quickly the object accelerates while swinging. Appears to be unused in-game.
    /// </summary>
    public float Accel { get; set; }

    /// <summary>
    /// How quickly the object's swing velocity decays over time. Appears to be unused in-game.
    /// </summary>
    public float Decay { get; set; }

    /// <summary>
    /// The delay, in seconds, before the object can be grabbed again after being released.
    /// Appears to be unused in-game.
    /// </summary>
    public float GrabDelay { get; set; }

    /// <summary>
    /// How quickly the object decelerates once it stops swinging. Appears to be unused in-game.
    /// </summary>
    public float StopDecel { get; set; }

    AssetId IHasModel.ModelId { get => Physical.ModelId; set => Physical.ModelId = value; }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.Hangable"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.N100F,
        GameVersion.ROTU,
    };

    internal HangableAsset() { }

    internal static HangableAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new HangableAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);
        EntityAssetPrefix.Read(asset, reader, profile.EntityHasPadding);

        asset.Flags = (HangableFlags)reader.ReadUInt32();
        asset.PivotOffset = reader.ReadSingle();
        asset.LeverArm = reader.ReadSingle();
        asset.Gravity = reader.ReadSingle();
        asset.Accel = reader.ReadSingle();
        asset.Decay = reader.ReadSingle();
        asset.GrabDelay = reader.ReadSingle();
        asset.StopDecel = reader.ReadSingle();

        LinkSerialization.Read(asset, reader, asset.Physical.LinkCount);
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(HangableAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);
        EntityAssetPrefix.Write(asset, writer, profile.EntityHasPadding);

        writer.Write((uint)asset.Flags);
        writer.Write(asset.PivotOffset);
        writer.Write(asset.LeverArm);
        writer.Write(asset.Gravity);
        writer.Write(asset.Accel);
        writer.Write(asset.Decay);
        writer.Write(asset.GrabDelay);
        writer.Write(asset.StopDecel);

        LinkSerialization.Write(asset, writer);
        writer.Write(asset.GetUnparsedTail());
    }
}

/// <summary>
/// Represents all known values for <see cref="HangableAsset.Flags"/>.
/// </summary>
[Flags]
public enum HangableFlags : uint
{
    /// <summary>
    /// No flags are set.
    /// </summary>
    None = 0,
}
