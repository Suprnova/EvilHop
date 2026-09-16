using EvilHop.Common;

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
}
