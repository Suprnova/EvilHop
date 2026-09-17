using EvilHop.Common;
using System.Diagnostics.CodeAnalysis;

namespace EvilHop.Assets;

/// <summary>
/// One named parameter read by an NPC's AI code by hashing the parameter's name and matching it
/// against <see cref="HashId"/>. Defined by <see cref="AssetType.ModelInfo"/>, and reused verbatim by
/// <see cref="AssetType.NPCSettings"/>.
/// </summary>
public sealed class ModelInfoParameter
{
    /// <summary>A BKDR hash of this parameter's name.</summary>
    public uint HashId { get; set; }

    /// <summary>
    /// This parameter's value, usually a floating-point number or a vector written as text (for
    /// example <c>"1.0"</c> or <c>"{ 3.0, 3.0, 3.0 }"</c>).
    /// </summary>
    public string Value { get; set; } = "";

    /// <summary>
    /// Trailing padding bytes between the null terminator and the 4-byte boundary.
    /// Preserved directly to reproduce uninitialized memory on round-trip.
    /// </summary>
    [SuppressMessage("Design", "CA1819:Properties should not return arrays", Justification = "Raw padding bytes with no field structure of its own; a byte[] is the natural representation.")]
    public byte[] Padding { get; set; } = [];
}
