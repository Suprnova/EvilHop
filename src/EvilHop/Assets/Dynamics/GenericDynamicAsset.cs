namespace EvilHop.Assets;

/// <summary>
/// A <see cref="DynamicAsset"/> whose <see cref="DynamicAsset.Kind"/>, game, and version have no
/// typed model. Its <see cref="BaseAsset.Links"/> are still parsed; its own fields are preserved,
/// unparsed, in <see cref="Asset.GetUnparsedTail"/>.
/// </summary>
/// <param name="kind">Which kind of object this dynamic is.</param>
public sealed class GenericDynamicAsset(DynamicKind kind) : DynamicAsset
{
    /// <inheritdoc/>
    public override DynamicKind Kind { get; } = kind;
}
