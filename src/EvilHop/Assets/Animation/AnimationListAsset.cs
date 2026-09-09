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
public sealed class AnimationListAsset : Asset
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
    /// A hash paired with each of <see cref="Ids"/>' slots. Unverified purpose - not present under
    /// <see cref="GameVersion.N100F"/>, <see cref="GameVersion.BFBB"/>, <see cref="GameVersion.TSSM"/>,
    /// or <see cref="GameVersion.Incredibles"/>.
    /// </summary>
    /// <exception cref="ArgumentException">The assigned value's length isn't 10.</exception>
    public ImmutableArray<uint> StateHashes
    {
        get;
        set => field = value.Length == 10
            ? value
            : throw new ArgumentException($"{nameof(StateHashes)} must contain exactly 10 elements.", nameof(value));
    } = [.. new uint[10]];

    /// <summary>
    /// Whether each of <see cref="Ids"/>' slots drives ragdoll/physics-based animation. Not present
    /// under <see cref="GameVersion.N100F"/>, <see cref="GameVersion.BFBB"/>, <see cref="GameVersion.TSSM"/>,
    /// or <see cref="GameVersion.Incredibles"/>.
    /// </summary>
    /// <exception cref="ArgumentException">The assigned value's length isn't 10.</exception>
    public ImmutableArray<bool> HasPhysics
    {
        get;
        set => field = value.Length == 10
            ? value
            : throw new ArgumentException($"{nameof(HasPhysics)} must contain exactly 10 elements.", nameof(value));
    } = [.. new bool[10]];

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

    internal AnimationListAsset() { }

    internal static AnimationListAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new AnimationListAsset();
        AssetFields.Populate(asset, header, debug);

        asset.Ids = [.. Enumerable.Range(0, 10).Select(_ => reader.ReadAssetId())];

        if (HasExtraFields(profile))
        {
            asset.StateHashes = [.. Enumerable.Range(0, 10).Select(_ => reader.ReadUInt32())];
            asset.HasPhysics = [.. Enumerable.Range(0, 10).Select(_ => reader.ReadByte() != 0)];
            reader.ReadInt16(); // padding, always zero - aligns the trailing bool[10] to 4 bytes
        }

        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(AnimationListAsset asset, EndianWriter writer, FormatProfile profile)
    {
        foreach (var id in asset.Ids) writer.Write(id);

        if (HasExtraFields(profile))
        {
            foreach (uint hash in asset.StateHashes) writer.Write(hash);
            foreach (bool hasPhysics in asset.HasPhysics) writer.Write((byte)(hasPhysics ? 1 : 0));
            writer.Write((short)0); // padding
        }

        writer.Write(asset.GetUnparsedTail());
    }

    private static bool HasExtraFields(FormatProfile profile) => profile.Game is GameVersion.ROTU or GameVersion.Ratatouille;
}
