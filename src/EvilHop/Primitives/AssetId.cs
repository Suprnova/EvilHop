using EvilHop.Common;

namespace EvilHop.Primitives;

/// <summary>
/// Represents a reference to an <see cref="Assets.Asset"/>.
/// </summary>
/// <param name="Value">The underlying Asset ID uint.</param>
/// TODO: extension on Asset for .CalculateId()?
public readonly record struct AssetId(uint Value)
{
    /// <summary>
    /// Constructs an <see cref="AssetId"/> from the provided <paramref name="name"/>.
    /// </summary>
    /// <param name="name">The <see cref="string"/> to calculate from.</param>
    /// <returns>An <see cref="AssetId"/> pointing to an asset named <paramref name="name"/>.</returns>
    /// <remarks>
    /// This does not apply special logic used to accurately construct an asset's ID based on its
    /// type. For that functionality, use <see cref="FromName(string, AssetType)"/>.
    /// </remarks>
    public static AssetId FromName(string name) => new(BKDRHash.Calculate(name));

    /// <summary>
    /// Constructs an <see cref="AssetId"/> of the provided <paramref name="type"/> from the
    /// provided <paramref name="name"/>.
    /// </summary>
    /// <param name="name">The <see cref="string"/> to calculate from.</param>
    /// <param name="type">The <see cref="AssetType"/> to calculate for.</param>
    /// <returns>An <see cref="AssetId"/> pointing to an asset of type <paramref name="type"/> named <paramref name="name"/>.</returns>
    public static AssetId FromName(string name, AssetType type) =>
        new(BKDRHash.Calculate(TransformName(name, type)));

    private static string TransformName(string name, AssetType type) => type switch
    {
        AssetType.Animation => Path.ChangeExtension(name, ".anm"),
        AssetType.DestructibleAsset => name + ".dff_destruct",
        AssetType.MorphTarget => Path.ChangeExtension(name, ".mph"),
        _ => name
    };

    /// <summary>
    /// Represents an <see cref="AssetId"/> that does not point to an asset.
    /// </summary>
    public static readonly AssetId None = new(0);

    /// <inheritdoc/>
    public override string ToString() => $"0x{Value:X8}";

    /// <inheritdoc/>
    public static explicit operator uint(AssetId id) => id.Value;
    /// <inheritdoc/>
    public static explicit operator AssetId(uint value) => new(value);
}
