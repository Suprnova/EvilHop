namespace EvilHop.Assets;

/// <summary>
/// How much of an asset EvilHop models, which says whether its fields can be trusted.
/// </summary>
public enum SupportState
{
    /// <summary>
    /// The asset's fields are modeled and read and written one by one. A partial model keeps the
    /// bytes it doesn't cover in <see cref="Asset.GetUnparsedTail"/>.
    /// </summary>
    Typed,

    /// <summary>
    /// The asset wraps an embedded file, such as a RenderWare model, a Bink video or a sound, that
    /// is read and written whole: see <see cref="PayloadAsset"/>.
    /// </summary>
    Payload,

    /// <summary>
    /// No codec models the asset's fields. It keeps what its shape shares (the base fields and
    /// links, or the entity header), and the rest is in <see cref="Asset.GetUnparsedTail"/>.
    /// </summary>
    Untyped
}
