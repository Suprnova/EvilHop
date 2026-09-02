namespace EvilHop.Assets;

/// <summary>
/// Tracks a single integer value that can be incremented, decremented, reset, and queried by other
/// assets via links.
/// </summary>
/// <remarks>
/// <para>
/// A counter can be in a normal or expired state. In the normal state, its value can be freely
/// changed; if a change ever sets it to 0, it becomes expired, and stays that way - ignoring further
/// changes - until explicitly reset back to <see cref="InitialValue"/>. None of this runtime state is
/// stored in the archive.
/// </para>
/// <seealso href="https://heavyironmodding.org/wiki/CNTR">Heavy Iron Modding documentation</seealso>
/// Validation TODO: Physical.BaseType is always 0x16.
/// </remarks>
public sealed class CounterAsset : BaseAsset
{
    /// <summary>
    /// The counter's value when the level loads.
    /// </summary>
    public short InitialValue { get; set; }
}
