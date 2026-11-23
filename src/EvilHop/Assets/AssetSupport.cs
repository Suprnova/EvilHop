using EvilHop.Assets.Serialization;
using EvilHop.Common;

namespace EvilHop.Assets;

/// <summary>
/// How much of an asset type or a dynamic kind EvilHop models, asked before reading any asset.
/// </summary>
/// <remarks>
/// The answer is what a read under the same game resolves to, so an asset read from a well-formed
/// archive reports the same <see cref="Asset.Support"/>. An asset whose parse fails reports
/// <see cref="SupportState.Untyped"/> whatever its type's state.
/// </remarks>
public static class AssetSupport
{
    /// <summary>
    /// How much of an asset of <paramref name="type"/> is modeled under <paramref name="game"/>.
    /// </summary>
    /// <param name="type">The asset type.</param>
    /// <param name="game">The game the asset is read under.</param>
    /// <returns>
    /// <see cref="SupportState.Typed"/> when a codec models the type for <paramref name="game"/>,
    /// <see cref="SupportState.Payload"/> for a type that wraps an embedded file, and
    /// <see cref="SupportState.Untyped"/> otherwise. <see cref="AssetType.Dynamic"/> answers
    /// <see cref="SupportState.Untyped"/>: a dynamic's state is its kind's, which
    /// <see cref="Of(DynamicKind, GameVersion, short)"/> gives.
    /// </returns>
    public static SupportState Of(AssetType type, GameVersion game) =>
        type is AssetType.Dynamic ? SupportState.Untyped : AssetCodecs.SupportOf(type, game);

    /// <summary>
    /// How much of a dynamic of <paramref name="kind"/> and <paramref name="version"/> is modeled
    /// under <paramref name="game"/>.
    /// </summary>
    /// <param name="kind">The dynamic's kind.</param>
    /// <param name="game">The game the dynamic is read under.</param>
    /// <param name="version">The dynamic's version, from its header.</param>
    /// <returns><see cref="SupportState.Typed"/> when a model covers that layout, <see cref="SupportState.Untyped"/> otherwise.</returns>
    public static SupportState Of(DynamicKind kind, GameVersion game, short version) =>
        DynamicCodecs.IsModeled(kind, version, game) ? SupportState.Typed : SupportState.Untyped;
}
