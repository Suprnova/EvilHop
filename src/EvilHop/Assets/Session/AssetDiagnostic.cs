using EvilHop.Common;

namespace EvilHop.Assets;

/// <summary>
/// A problem encountered while opening an <see cref="AssetSession"/>, attributed to the asset that
/// caused it.
/// </summary>
/// <param name="AssetId">The asset the problem was encountered for.</param>
/// <param name="Message">A human-readable description of the problem.</param>
public readonly record struct AssetDiagnostic(AssetId AssetId, string Message)
{
    /// <inheritdoc/>
    public override string ToString() => $"{AssetId}: {Message}";
}
