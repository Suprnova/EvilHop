using System.Diagnostics.CodeAnalysis;

namespace EvilHop.Blocks;

/// <summary>
/// A child <see cref="Block"/> of <see cref="AssetStream"/> containing the data for
/// all <c>Assets</c>.
/// </summary>
/// <remarks>
/// <see cref="Padding"/> exists to start <see cref="Data"/> on the platform's data alignment
/// boundary (32 bytes on GameCube, 2048 on PlayStation 2 and Xbox). A no-assets archive usually
/// omits <see cref="PaddingAmount"/> entirely - there's no <see cref="Data"/> to align - but a
/// minority keep the field anyway.
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/HIP_(File_Format)#DPAK">Heavy Iron Modding documentation</seealso>
/// </remarks>
/// Validation TODO: No children.
public class StreamData : Block
{
    /// <inheritdoc/>
    protected internal override string Tag => "DPAK";

    /// <summary>
    /// The amount of padding in bytes.
    /// </summary>
    /// <remarks>
    /// Nullable because the field is absent entirely from a no-assets block, whose whole content is
    /// <see cref="Padding"/>.
    /// </remarks>
    /// Validation TODO: Equal to Padding's size.
    public uint? PaddingAmount { get; set; }

    /// <summary>
    /// The actual padding bytes.
    /// </summary>
    /// Validation TODO: All 0x00 on N100F proto, 0x33 otherwise.
    [SuppressMessage("Design", "CA1819:Properties should not return arrays", Justification = "Written and read as a raw byte buffer.")]
    public byte[] Padding { get; set; } = [];

    /// <summary>
    /// The <c>Asset</c> data of the <see cref="StreamData"/>, grouped into <c>Layers</c>.
    /// </summary>
    /// <remarks>
    /// Zeroed while the archive is set to <c>Asset Mode</c>.
    /// </remarks>
    [SuppressMessage("Design", "CA1819:Properties should not return arrays", Justification = "Written and read as a raw byte buffer.")]
    public byte[] Data
    {
        get => GetManagedBlockField(ref field);
        set => SetManagedBlockField(ref field, value);
    } = [];

    internal StreamData() { }
}
