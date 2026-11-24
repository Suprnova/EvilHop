namespace EvilHop.Assets;

/// <summary>
/// Which surface of an asset holds a field's value.
/// </summary>
public enum FieldSurface
{
    /// <summary>A public property of the asset or of a type nested in it.</summary>
    Logical,

    /// <summary>A member of an <c>asset.Physical</c> surface.</summary>
    Physical,

    /// <summary>No member: bytes the codec skips or never reads.</summary>
    Unmodeled
}

/// <summary>
/// How a field's bytes are read, in the byte order of the view's <see cref="AssetView.Profile"/>.
/// </summary>
/// <remarks>
/// An enum or flags field is the integer kind it is read as.
/// </remarks>
public enum FieldKind
{
    /// <summary>An unsigned byte.</summary>
    U8,

    /// <summary>A signed byte.</summary>
    S8,

    /// <summary>A signed 16-bit integer.</summary>
    S16,

    /// <summary>An unsigned 16-bit integer.</summary>
    U16,

    /// <summary>A signed 32-bit integer.</summary>
    S32,

    /// <summary>An unsigned 32-bit integer.</summary>
    U32,

    /// <summary>A 32-bit float.</summary>
    F32,

    /// <summary>Two consecutive <see cref="F32"/>s, a <see cref="System.Numerics.Vector2"/>.</summary>
    Vector2,

    /// <summary>Three consecutive <see cref="F32"/>s, a <see cref="System.Numerics.Vector3"/>.</summary>
    Vector3,

    /// <summary>Three consecutive <see cref="F32"/>s, an <see cref="Primitives.Rgb"/>.</summary>
    Rgb,

    /// <summary>Three consecutive bytes, each a channel scaled from 0-255, an <see cref="Primitives.Rgb"/>.</summary>
    Rgb24,

    /// <summary>Four consecutive <see cref="F32"/>s, an <see cref="Primitives.Rgba"/>.</summary>
    Rgba,

    /// <summary>Four consecutive bytes, each a channel scaled from 0-255, an <see cref="Primitives.Rgba"/>.</summary>
    Rgba32,

    /// <summary>An <see cref="Common.AssetId"/>.</summary>
    AssetId,

    /// <summary>Bytes read as they are.</summary>
    Bytes
}

/// <summary>
/// One field of an asset's bytes.
/// </summary>
/// <param name="Offset">Where the field starts, from the asset's first byte.</param>
/// <param name="Size">The field's length in bytes.</param>
/// <param name="Path">
/// The member the field is read into, as a path from the asset: <c>ScaleFactor</c>,
/// <c>Physical.Loop</c>, <c>Events[3].WidgetId</c>. Padding a codec skips is named for the field
/// before it, <c>Physical.CollisionFlags.Padding</c>. <see cref="FieldMap.Unnamed"/>,
/// <see cref="FieldMap.Unread"/> and <see cref="FieldMap.UnparsedTail"/> name bytes with no member path.
/// </param>
/// <param name="Surface">Which surface of the asset holds the field.</param>
/// <param name="Kind">How the field's bytes are read.</param>
public sealed record FieldEntry(uint Offset, uint Size, string Path, FieldSurface Surface, FieldKind Kind)
{
    /// <summary>The offset just past the field's last byte.</summary>
    public uint End => Offset + Size;
}

/// <summary>
/// The field at one offset of an asset's bytes.
/// </summary>
/// <param name="Field">The field holding the offset.</param>
/// <param name="OffsetInField">How far into <paramref name="Field"/> the offset is.</param>
public readonly record struct FieldLocation(FieldEntry Field, uint OffsetInField);

/// <summary>
/// The fields of one asset's bytes as its codec reads them, in offset order.
/// </summary>
/// <remarks>
/// <para>
/// The map comes from the codec's own reads, so it says what the reader consumes. Its fields cover
/// the asset's bytes exactly, with no overlap: bytes read twice belong to the last read of them,
/// bytes the codec passes over without reading are <see cref="Unread"/>, and the bytes it leaves at
/// the end are <see cref="UnparsedTail"/>.
/// </para>
/// <para>
/// A field a profile switch gates is present or absent in that profile's map. An asset whose bytes
/// fail to parse maps as one <see cref="UnparsedTail"/>, as it reads.
/// </para>
/// </remarks>
public sealed class FieldMap
{
    /// <summary>
    /// The last segment of the <see cref="FieldEntry.Path"/> of a read that doesn't yet name its
    /// member: <c>Unnamed</c>, or <c>Events[3].Unnamed</c> inside an element whose other reads are named.
    /// </summary>
    public const string Unnamed = "Unnamed";

    /// <summary>The <see cref="FieldEntry.Path"/> of bytes the codec passes over without reading.</summary>
    public const string Unread = "Unread";

    /// <summary>The <see cref="FieldEntry.Path"/> of the bytes at the end that the codec doesn't parse.</summary>
    public const string UnparsedTail = nameof(Asset.UnparsedTail);

    internal FieldMap(IReadOnlyList<FieldEntry> fields, uint length)
    {
        Fields = fields;
        Length = length;
    }

    /// <summary>The asset's length in bytes, the length of <see cref="AssetView.GetBytes"/>.</summary>
    public uint Length { get; }

    /// <summary>The asset's fields, in offset order.</summary>
    public IReadOnlyList<FieldEntry> Fields { get; }

    /// <summary>
    /// The field at <paramref name="offset"/> of the asset's bytes.
    /// </summary>
    /// <param name="offset">An offset from the asset's first byte.</param>
    /// <returns>The location, or <see langword="null"/> when <paramref name="offset"/> is past the asset's end.</returns>
    public FieldLocation? Locate(uint offset)
    {
        if (offset >= Length) return null;

        int low = 0, high = Fields.Count - 1;
        while (low < high)
        {
            int middle = (low + high + 1) / 2;
            if (Fields[middle].Offset <= offset) low = middle;
            else high = middle - 1;
        }
        return new FieldLocation(Fields[low], offset - Fields[low].Offset);
    }
}
