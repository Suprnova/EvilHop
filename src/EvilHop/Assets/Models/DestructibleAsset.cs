using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace EvilHop.Assets;

/// <summary>
/// Defines the states and effects of a destructible model. A <see cref="AssetType.Model"/> only
/// breaks apart when its name ends in ".dff_destruct" and it is paired with one of these.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/DEST">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class DestructibleAsset() : Asset(AssetType.DestructibleAsset), IPhysicalDestructibleAsset
{
    /// <summary>
    /// The <see cref="AssetType.ModelInfo"/> this destructible is associated with.
    /// </summary>
    public AssetId ModelInfoId { get; set; }

    /// <summary>
    /// The number of hits this destructible can take before it is destroyed.
    /// </summary>
    public uint HitPoints { get; set; }

    /// <summary>
    /// A number of health points. Only present in <see cref="GameVersion.ROTU"/> and
    /// <see cref="GameVersion.Ratatouille"/>.
    /// </summary>
    public uint HealthPoints { get; set; }

    /// <summary>
    /// A number of experience points. Only present in <see cref="GameVersion.ROTU"/> and
    /// <see cref="GameVersion.Ratatouille"/>.
    /// </summary>
    public uint ExperiencePoints { get; set; }

    /// <summary>
    /// A chance, from 0 to 100, tied to <see cref="HealthPoints"/>. Only present in
    /// <see cref="GameVersion.ROTU"/> and <see cref="GameVersion.Ratatouille"/>.
    /// </summary>
    public float HealthChance { get; set; }

    /// <summary>
    /// A chance, from 0 to 100, tied to <see cref="ExperiencePoints"/>. Only present in
    /// <see cref="GameVersion.ROTU"/> and <see cref="GameVersion.Ratatouille"/>.
    /// </summary>
    public float ExperienceChance { get; set; }

    /// <summary>
    /// The <see cref="AssetType.SoundGroup"/> played idly while this destructible is intact.
    /// </summary>
    public AssetId IdleSoundGroupId { get; set; }

    /// <summary>
    /// The delay, in seconds, before this destructible respawns after being destroyed.
    /// </summary>
    public float RespawnTime { get; set; }

    /// <summary>
    /// This destructible's priority as an AI targeting candidate.
    /// </summary>
    public byte TargetPriority { get; set; }

    /// <summary>
    /// This destructible's states, ordered from least to most damaged.
    /// </summary>
    public Collection<DestructibleAssetState> States { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalDestructibleAsset Physical => this;

    private uint? _overriddenStateCount;
    uint IPhysicalDestructibleAsset.StateCount
    {
        get => _overriddenStateCount ?? (uint)States.Count;
        set => _overriddenStateCount = value == (uint)States.Count ? null : value;
    }

    private byte[] _padding = new byte[PaddingSize];
    byte[] IPhysicalDestructibleAsset.Padding { get => _padding; set => _padding = value; }

    private const int PaddingSize = 3;

    private uint _hitFilter;
    uint IPhysicalDestructibleAsset.HitFilter { get => _hitFilter; set => _hitFilter = value; }

    private uint _excludedHitFilter;
    uint IPhysicalDestructibleAsset.ExcludedHitFilter { get => _excludedHitFilter; set => _excludedHitFilter = value; }

    private uint _launchFlag;
    uint IPhysicalDestructibleAsset.LaunchFlag { get => _launchFlag; set => _launchFlag = value; }

    private uint _behaviour;
    uint IPhysicalDestructibleAsset.Behaviour { get => _behaviour; set => _behaviour = value; }

    private uint _unknownFlags;
    uint IPhysicalDestructibleAsset.DestructibleFlags { get => _unknownFlags; set => _unknownFlags = value; }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.DestructibleAsset"/> is known to be read
    /// by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.TSSM,
        GameVersion.Incredibles,
        GameVersion.ROTU,
        GameVersion.Ratatouille,
    };

    internal static DestructibleAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new DestructibleAsset();
        AssetFields.Populate(asset, header, debug);

        asset.ModelInfoId = reader.ReadAssetId();
        uint stateCount = reader.ReadUInt32();
        asset.HitPoints = reader.ReadUInt32();
        asset.Physical.HitFilter = reader.ReadUInt32();

        if (profile.Game is GameVersion.ROTU or GameVersion.Ratatouille)
        {
            asset.Physical.ExcludedHitFilter = reader.ReadUInt32();
            asset.HealthPoints = reader.ReadUInt32();
            asset.ExperiencePoints = reader.ReadUInt32();
            asset.HealthChance = reader.ReadSingle();
            asset.ExperienceChance = reader.ReadSingle();
        }

        asset.Physical.LaunchFlag = reader.ReadUInt32();
        asset.Physical.Behaviour = reader.ReadUInt32();
        asset.Physical.DestructibleFlags = reader.ReadUInt32();
        asset.IdleSoundGroupId = reader.ReadAssetId();
        asset.RespawnTime = reader.ReadSingle();
        asset.TargetPriority = reader.ReadByte();
        asset.Physical.Padding = reader.ReadBytes(PaddingSize);

        for (int i = 0; i < stateCount; i++)
            asset.States.Add(DestructibleAssetState.Read(reader, profile));

        asset.Physical.StateCount = (uint)asset.States.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(DestructibleAsset asset, EndianWriter writer, FormatProfile profile)
    {
        writer.Write(asset.ModelInfoId);
        writer.Write(asset.Physical.StateCount);
        writer.Write(asset.HitPoints);
        writer.Write(asset.Physical.HitFilter);

        if (profile.Game is GameVersion.ROTU or GameVersion.Ratatouille)
        {
            writer.Write(asset.Physical.ExcludedHitFilter);
            writer.Write(asset.HealthPoints);
            writer.Write(asset.ExperiencePoints);
            writer.Write(asset.HealthChance);
            writer.Write(asset.ExperienceChance);
        }

        writer.Write(asset.Physical.LaunchFlag);
        writer.Write(asset.Physical.Behaviour);
        writer.Write(asset.Physical.DestructibleFlags);
        writer.Write(asset.IdleSoundGroupId);
        writer.Write(asset.RespawnTime);
        writer.Write(asset.TargetPriority);
        writer.Write(asset.Physical.Padding);

        foreach (var state in asset.States)
            DestructibleAssetState.Write(state, writer, profile);

        writer.Write(asset.GetUnparsedTail());
    }
}

/// <summary>
/// An explicit interface used to interact with <see cref="DestructibleAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalDestructibleAsset : IPhysicalAsset
{
    /// <summary>
    /// The number of <see cref="DestructibleAsset.States"/> stored for this asset, read directly
    /// from its leading count field.
    /// </summary>
    /// <remarks>
    /// When disagreements with <see cref="DestructibleAsset.States"/>.Count exist, this field wins
    /// during serialization.
    /// </remarks>
    uint StateCount { get; set; }

    /// <summary>
    /// The 3 bytes following <see cref="DestructibleAsset.TargetPriority"/>, reserved for struct
    /// alignment. Observed non-zero in some builds, so it is preserved rather than assumed zero.
    /// </summary>
    [SuppressMessage("Design", "CA1819:Properties should not return arrays", Justification = "Fixed-size raw padding with no field structure of its own; a byte[] is the natural representation.")]
    byte[] Padding { get; set; }

    /// <summary>
    /// Unknown.
    /// </summary>
    uint HitFilter { get; set; }

    /// <summary>
    /// Unknown. Only present in <see cref="GameVersion.ROTU"/> and
    /// <see cref="GameVersion.Ratatouille"/>.
    /// </summary>
    uint ExcludedHitFilter { get; set; }

    /// <summary>
    /// Unknown.
    /// </summary>
    uint LaunchFlag { get; set; }

    /// <summary>
    /// Unknown.
    /// </summary>
    uint Behaviour { get; set; }

    /// <summary>
    /// Unknown.
    /// </summary>
    uint DestructibleFlags { get; set; }
}
