using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>
/// A skeletal animation, storing a sparse set of keyframes per bone in the SKB format.
/// </summary>
/// <remarks>
/// <para>
/// An animation plays back over a series of <see cref="Times"/> (in seconds). At each time except the
/// last, <see cref="Offsets"/> maps every bone to the index within <see cref="Keys"/> its next
/// keyframe starts at - the run of keys for a bone at a given time ends where the next time's offset
/// for that bone begins.
/// </para>
/// <seealso href="https://heavyironmodding.org/wiki/ANIM">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class AnimationAsset() : Asset(AssetType.Animation), IPhysicalAnimationAsset
{
    /// <summary>
    /// A four-character tag identifying this as the SKB format.
    /// </summary>
    /// <remarks>
    /// Normally <c>0x31424B53</c> ("SKB1" stored reversed), but not every game agrees, so it's read
    /// and written back verbatim rather than assumed.
    /// </remarks>
    public uint Magic { get; set; } = 0x31424B53;

    /// <summary>
    /// Unknown. Observed values vary and are not always meaningful (at least one archive stores an
    /// apparent uninitialized-memory pattern here).
    /// </summary>
    public uint Flags { get; set; }

    /// <summary>
    /// The number of bones this animation drives, and the row length of <see cref="Offsets"/>.
    /// </summary>
    public ushort BoneCount { get; set; }

    /// <summary>
    /// A per-axis scale applied when decoding <see cref="AnimationKey.Quat"/>/<see cref="AnimationKey.Tran"/>'s
    /// fixed-point values back to real rotation/translation units.
    /// </summary>
    public Vector3 Scale { get; set; }

    /// <summary>
    /// The animation's keyframes, indexed into by <see cref="Offsets"/>.
    /// </summary>
    public Collection<AnimationKey> Keys { get; } = [];

    /// <summary>
    /// The times (in seconds) this animation has a frame at. The last entry is the animation's
    /// duration.
    /// </summary>
    public Collection<float> Times { get; } = [];

    /// <summary>
    /// For every time except the last, one <see cref="Keys"/> start index per bone - row-major, one
    /// row of <see cref="BoneCount"/> entries per time. Index as
    /// <c>Offsets[timeIndex * BoneCount + boneIndex]</c>.
    /// </summary>
    public Collection<ushort> Offsets { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalAnimationAsset Physical => this;

    private uint? _overriddenKeyCount;
    uint IPhysicalAnimationAsset.KeyCount
    {
        get => _overriddenKeyCount ?? (uint)Keys.Count;
        set => _overriddenKeyCount = value == (uint)Keys.Count ? null : value;
    }

    private ushort? _overriddenTimeCount;
    ushort IPhysicalAnimationAsset.TimeCount
    {
        get => _overriddenTimeCount ?? (ushort)Times.Count;
        set => _overriddenTimeCount = value == (ushort)Times.Count ? null : value;
    }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.Animation"/> is known to be read by.
    /// </summary>
    /// <remarks>
    /// <see cref="GameVersion.ROTU"/> and <see cref="GameVersion.Ratatouille"/> use a revised SKB
    /// layout not modeled here; both degrade to the generic shape.
    /// </remarks>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.N100F,
        GameVersion.BFBB,
        GameVersion.TSSM,
        GameVersion.Incredibles,
    };

    internal static AnimationAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile _)
    {
        var asset = new AnimationAsset();
        AssetFields.Populate(asset, header, debug);

        asset.Magic = reader.ReadUInt32();
        asset.Flags = reader.ReadUInt32();
        asset.BoneCount = (ushort)reader.ReadInt16();
        int timeCount = (ushort)reader.ReadInt16();
        int keyCount = (int)reader.ReadUInt32();
        asset.Scale = reader.ReadVector3();

        for (int i = 0; i < keyCount; i++)
        {
            asset.Keys.Add(new AnimationKey
            {
                TimeIndex = (ushort)reader.ReadInt16(),
                Quat = new Vector4(reader.ReadInt16(), reader.ReadInt16(), reader.ReadInt16(), reader.ReadInt16()),
                Tran = new Vector3(reader.ReadInt16(), reader.ReadInt16(), reader.ReadInt16()),
            });
        }

        for (int i = 0; i < timeCount; i++) asset.Times.Add(reader.ReadSingle());

        int offsetCount = Math.Max(timeCount - 1, 0) * asset.BoneCount;
        for (int i = 0; i < offsetCount; i++) asset.Offsets.Add((ushort)reader.ReadInt16());

        asset.Physical.KeyCount = (uint)asset.Keys.Count;
        asset.Physical.TimeCount = (ushort)asset.Times.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(AnimationAsset asset, EndianWriter writer, FormatProfile _)
    {
        writer.Write(asset.Magic);
        writer.Write(asset.Flags);
        writer.Write((short)asset.BoneCount);
        writer.Write((short)asset.Physical.TimeCount);
        writer.Write(asset.Physical.KeyCount);
        writer.Write(asset.Scale);

        foreach (var key in asset.Keys)
        {
            writer.Write((short)key.TimeIndex);
            writer.Write((short)key.Quat.X); writer.Write((short)key.Quat.Y); writer.Write((short)key.Quat.Z); writer.Write((short)key.Quat.W);
            writer.Write((short)key.Tran.X); writer.Write((short)key.Tran.Y); writer.Write((short)key.Tran.Z);
        }

        foreach (float time in asset.Times) writer.Write(time);
        foreach (ushort offset in asset.Offsets) writer.Write((short)offset);

        writer.Write(asset.GetUnparsedTail());
    }
}

/// <summary>
/// An explicit interface used to interact with <see cref="AnimationAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalAnimationAsset : IPhysicalAsset
{
    /// <summary>
    /// The number of <see cref="AnimationAsset.Keys"/> stored for this asset, read directly from its
    /// header.
    /// </summary>
    /// <remarks>
    /// When disagreements with <see cref="AnimationAsset.Keys"/>.Count exist, this field wins during
    /// serialization.
    /// </remarks>
    uint KeyCount { get; set; }

    /// <summary>
    /// The number of <see cref="AnimationAsset.Times"/> stored for this asset, read directly from its
    /// header.
    /// </summary>
    /// <remarks>
    /// When disagreements with <see cref="AnimationAsset.Times"/>.Count exist, this field wins during
    /// serialization.
    /// </remarks>
    ushort TimeCount { get; set; }
}

/// <summary>
/// One <see cref="AnimationAsset"/> keyframe: a bone's rotation and translation at a given time.
/// </summary>
public sealed class AnimationKey
{
    /// <summary>
    /// The index into the owning <see cref="AnimationAsset.Times"/> this keyframe applies at.
    /// </summary>
    public ushort TimeIndex { get; set; }

    /// <summary>
    /// The bone's rotation at <see cref="TimeIndex"/>, as a fixed-point quaternion (X, Y, Z, W).
    /// </summary>
    /// <remarks>
    /// Each component is a raw <see cref="short"/> widened losslessly to <see cref="float"/>, not yet
    /// decoded to real rotation units; see <see cref="AnimationAsset.Scale"/>.
    /// </remarks>
    public Vector4 Quat { get; set; }

    /// <summary>
    /// The bone's translation offset at <see cref="TimeIndex"/>, as a fixed-point vector (X, Y, Z).
    /// </summary>
    /// <remarks>
    /// Each component is a raw <see cref="short"/> widened losslessly to <see cref="float"/>, not yet
    /// decoded to real translation units; see <see cref="AnimationAsset.Scale"/>.
    /// </remarks>
    public Vector3 Tran { get; set; }
}
