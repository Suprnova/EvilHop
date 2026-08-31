using EvilHop.Validation;

namespace EvilHop.Blocks;

/// <summary>
/// A no-data <see cref="Block"/> that serves as the root parent for all <c>Asset</c> data
/// related blocks.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/HIP_(File_Format)#STRM">Heavy Iron Modding documentation</seealso>
/// </remarks>
public class AssetStream : Block
{
    /// <inheritdoc/>
    protected internal override string Tag => "STRM";

    /// <summary>
    /// The child <see cref="StreamHeader"/> of the <see cref="AssetStream"/>.
    /// </summary>
    [RequiredChild]
    public StreamHeader Header
    {
        get => GetRequiredChild<StreamHeader>();
        set => SetChild(value);
    }

    /// <summary>
    /// The child <see cref="StreamData"/> of the <see cref="AssetStream"/>.
    /// </summary>
    [RequiredChild]
    public StreamData Data
    {
        get => GetRequiredChild<StreamData>();
        set => SetChild(value);
    }

    internal AssetStream() { }
}

/// <summary>
/// A child <see cref="Block"/> of <see cref="AssetStream"/> with unknown use.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/HIP_(File_Format)#DHDR">Heavy Iron Modding documentation</seealso>
/// </remarks>
[NoChildren]
public class StreamHeader : Block
{
    /// <inheritdoc/>
    protected internal override string Tag => "DHDR";

    /// <summary>
    /// Unknown. Always 0xFFFFFFFF.
    /// </summary>
    [ConstantValue(0xFFFFFFFFu)]
    public uint Value { get; set; }

    internal StreamHeader() { }
}

/// <summary>
/// A child <see cref="Block"/> of <see cref="AssetStream"/> containing the data for
/// all <c>Assets</c>.
/// </summary>
/// <remarks>
/// <see cref="Padding"/> exists to start <see cref="Data"/> on a 32-byte boundary, which holds for
/// every archive observed. In archives without any assets (<see cref="PackageCount.AssetCount"/> = 0)
/// there is no <see cref="Data"/> to align and no <see cref="PaddingAmount"/> field is written at
/// all - the block holds only the fill needed to bring the archive itself to a 32-byte boundary,
/// which is an empty block when it already ends aligned.
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/HIP_(File_Format)#DPAK">Heavy Iron Modding documentation</seealso>
/// </remarks>
[NoChildren]
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
    public byte[] Padding { get; set; } = [];

    /// <summary>
    /// The <c>Asset</c> data of the <see cref="StreamData"/>, grouped into <c>Layers</c>.
    /// </summary>
    /// <remarks>
    /// Zeroed while the archive is set to <c>Asset Mode</c>.
    /// </remarks>
    public byte[] Data
    {
        get => GetManagedBlockField(ref field);
        set => SetManagedBlockField(ref field, value);
    } = [];

    internal StreamData() { }
}
