using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.Immutable;

namespace EvilHop.Assets;

/// <summary>
/// A fixed list of up to 10 <see cref="AssetType.Animation"/>s, referenced by an
/// <see cref="IHasAnimList"/>-implementing asset to drive its animation table.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/ALST">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class AnimationListAsset() : Asset(AssetType.AnimationList), IPhysicalAnimationListAsset
{
    /// <summary>
    /// The list's 10 animation slots. An unused slot is <see cref="AssetId.None"/>.
    /// </summary>
    /// <exception cref="ArgumentException">The assigned value's length isn't 10.</exception>
    public ImmutableArray<AssetId> Ids
    {
        get;
        set => field = value.Length == 10
            ? value
            : throw new ArgumentException($"{nameof(Ids)} must contain exactly 10 elements.", nameof(value));
    } = [.. new AssetId[10]];

    /// <summary>
    /// Whether each of <see cref="Ids"/>' slots drives ragdoll/physics-based animation. 
    /// Only present in <see cref="GameVersion.ROTU"/> and <see cref="GameVersion.Ratatouille"/>.
    /// </summary>
    /// <exception cref="ArgumentException">The assigned value's length isn't 10.</exception>
    public ImmutableArray<bool> HasPhysics
    {
        get;
        set => field = value.Length == 10
            ? value
            : throw new ArgumentException($"{nameof(HasPhysics)} must contain exactly 10 elements.", nameof(value));
    } = [.. new bool[10]];

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalAnimationListAsset Physical => this;

    private ImmutableArray<uint> _stateHashes = [.. new uint[10]];
    ImmutableArray<uint> IPhysicalAnimationListAsset.StateHashes
    {
        get => _stateHashes;
        set => _stateHashes = value.Length == 10
            ? value
            : throw new ArgumentException($"{nameof(IPhysicalAnimationListAsset.StateHashes)} must contain exactly 10 elements.");
    }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.AnimationList"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.N100F,
        GameVersion.BFBB,
        GameVersion.TSSM,
        GameVersion.Incredibles,
        GameVersion.ROTU,
        GameVersion.Ratatouille,
    };

    internal static AnimationListAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new AnimationListAsset();
        AssetFields.Populate(asset, header, debug);

        asset.Ids = [.. Enumerable.Range(0, 10).Select(_ => reader.ReadAssetId())];

        if (profile.Game is GameVersion.ROTU or GameVersion.Ratatouille)
        {
            asset.Physical.StateHashes = [.. Enumerable.Range(0, 10).Select(_ => reader.ReadUInt32())];
            asset.HasPhysics = [.. Enumerable.Range(0, 10).Select(_ => reader.ReadByte() != 0)];
            reader.ReadInt16(); // padding, always zero
        }

        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(AnimationListAsset asset, EndianWriter writer, FormatProfile profile)
    {
        foreach (var id in asset.Ids) writer.Write(id);

        if (profile.Game is GameVersion.ROTU or GameVersion.Ratatouille)
        {
            foreach (uint hash in asset.Physical.StateHashes) writer.Write(hash);
            foreach (bool hasPhysics in asset.HasPhysics) writer.Write((byte)(hasPhysics ? 1 : 0));
            writer.Write((short)0); // padding
        }

        writer.Write(asset.GetUnparsedTail());
    }
}

/// <summary>
/// An explicit interface used to interact with <see cref="AnimationListAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalAnimationListAsset : IPhysicalAsset
{
    /// <summary>
    /// An unknown hash paired with each of <see cref="AnimationListAsset.Ids"/>' slots.
    /// Only present in <see cref="GameVersion.ROTU"/> and <see cref="GameVersion.Ratatouille"/>.
    /// </summary>
    /// <exception cref="ArgumentException">The assigned value's length isn't 10.</exception>
    ImmutableArray<uint> StateHashes { get; set; }
}
