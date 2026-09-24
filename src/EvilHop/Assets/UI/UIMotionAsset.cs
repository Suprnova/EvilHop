using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;

namespace EvilHop.Assets;

/// <summary>
/// A sequence of animated changes - movement, scaling, rotation, opacity, brightness, color, and UV
/// scrolling - applied to a <see cref="AssetType.UI"/> over time, referenced by that asset's own
/// selected/unselected motion fields.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/UIM">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class UIMotionAsset() : BaseAsset(AssetType.UIMotion, baseType: 0x53), Physical.IUIMotionAsset
{
    /// <summary>
    /// How long, in seconds, the motion lasts. The motion ends at this point, so any
    /// <see cref="Commands"/> scheduled after it never play.
    /// </summary>
    public float TotalTime { get; set; }

    /// <summary>
    /// The time offset, in seconds, the motion loops back to instead of 0 once it reaches
    /// <see cref="TotalTime"/>.
    /// </summary>
    public float LoopTime { get; set; }

    /// <summary>
    /// The commands that make up this motion.
    /// </summary>
    public Collection<UIMotionCommand> Commands { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override Physical.IUIMotionAsset Physical => this;

    private byte? _overriddenCommandCount;
    byte Physical.IUIMotionAsset.CommandCount
    {
        get => _overriddenCommandCount ?? (byte)Commands.Count;
        set => _overriddenCommandCount = value == (byte)Commands.Count ? null : value;
    }

    private uint? _overriddenCommandsSize;
    uint Physical.IUIMotionAsset.CommandsSize
    {
        get => _overriddenCommandsSize ?? ComputedCommandsSize;
        set => _overriddenCommandsSize = value == ComputedCommandsSize ? null : value;
    }

    private uint ComputedCommandsSize
    {
        get
        {
            uint size = 0;
            foreach (var command in Commands) size += (uint)command.SerializedSize;
            return size;
        }
    }

    private byte _inFlag;
    byte Physical.IUIMotionAsset.InFlag { get => _inFlag; set => _inFlag = value; }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.UIMotion"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.Incredibles,
        GameVersion.TSSM,
        GameVersion.ROTU,
        GameVersion.Ratatouille,
    };

    internal static UIMotionAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new UIMotionAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        asset.Physical.CommandCount = reader.ReadByte();
        asset.Physical.InFlag = reader.ReadByte();
        reader.ReadBytes(2); // padding, always zero
        asset.Physical.CommandsSize = reader.ReadUInt32();
        asset.TotalTime = reader.ReadSingle();
        asset.LoopTime = reader.ReadSingle();

        byte commandCount = asset.Physical.CommandCount;
        for (int i = 0; i < commandCount; i++)
            asset.Commands.Add(UIMotionCommand.Read(reader, profile));

        asset.Physical.CommandCount = (byte)asset.Commands.Count;
        asset.Physical.CommandsSize = asset.ComputedCommandsSize;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(UIMotionAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);

        writer.Write(asset.Physical.CommandCount);
        writer.Write(asset.Physical.InFlag);
        writer.Write(new byte[2]); // padding
        writer.Write(asset.Physical.CommandsSize);
        writer.Write(asset.TotalTime);
        writer.Write(asset.LoopTime);

        foreach (var command in asset.Commands)
            UIMotionCommand.Write(command, writer, profile);

        writer.Write(asset.GetUnparsedTail());
    }
}

public static partial class Physical
{
    /// <summary>
    /// An explicit interface used to interact with <see cref="UIMotionAsset"/>'s underlying values.
    /// </summary>
    public interface IUIMotionAsset : IBaseAsset
    {
        /// <summary>
        /// The number of <see cref="UIMotionAsset.Commands"/> stored for this asset, read directly from
        /// its stored field.
        /// </summary>
        /// <remarks>
        /// When disagreements with <see cref="UIMotionAsset.Commands"/>.Count exist, this field wins
        /// during serialization.
        /// </remarks>
        byte CommandCount { get; set; }

        /// <summary>
        /// The combined serialized size, in bytes, of <see cref="UIMotionAsset.Commands"/>, read directly
        /// from its stored field.
        /// </summary>
        /// <remarks>
        /// When disagreements with <see cref="UIMotionAsset.Commands"/>' actual serialized size exist,
        /// this field wins during serialization.
        /// </remarks>
        uint CommandsSize { get; set; }

        /// <summary>
        /// Unknown.
        /// </summary>
        byte InFlag { get; set; }
    }
}
