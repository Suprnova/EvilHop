using EvilHop.Common;
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
public sealed partial class UIMotionAsset() : BaseAsset(AssetType.UIMotion), IPhysicalUIMotionAsset
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
    public override IPhysicalUIMotionAsset Physical => this;

    private byte? _overriddenCommandCount;
    byte IPhysicalUIMotionAsset.CommandCount
    {
        get => _overriddenCommandCount ?? (byte)Commands.Count;
        set => _overriddenCommandCount = value == (byte)Commands.Count ? null : value;
    }

    private uint? _overriddenCommandsSize;
    uint IPhysicalUIMotionAsset.CommandsSize
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
    byte IPhysicalUIMotionAsset.InFlag { get => _inFlag; set => _inFlag = value; }

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
}

/// <summary>
/// An explicit interface used to interact with <see cref="UIMotionAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalUIMotionAsset : IPhysicalBaseAsset
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
    /// TODO: validate against decompiled source
    byte InFlag { get; set; }
}
