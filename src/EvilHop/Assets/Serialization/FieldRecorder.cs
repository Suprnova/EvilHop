using EvilHop.Primitives;

namespace EvilHop.Assets.Serialization;

/// <summary>
/// Records each read an <see cref="EndianReader"/> makes from <paramref name="stream"/> as a
/// <see cref="FieldEntry"/>, to build an asset's <see cref="FieldMap"/>.
/// </summary>
/// <remarks>
/// Only the outermost read is a field: a <see cref="EndianReader.ReadVector3"/> is one
/// <see cref="FieldKind.Vector3"/>, not three <see cref="FieldKind.F32"/>s. A field's path is the
/// open scopes joined with its name, and a path through <c>Physical</c> is on the
/// <see cref="FieldSurface.Physical"/> surface.
/// </remarks>
internal sealed class FieldRecorder(Stream stream)
{
    private const string Padding = nameof(Padding);

    private readonly List<FieldEntry> _fields = [];
    private readonly List<string> _scopes = [];
    private int _depth;
    private (string Path, FieldSurface Surface)? _next;
    private string? _lastPath;

    /// <summary>Names the next read's field <paramref name="name"/>, under the open scopes.</summary>
    public void Name(string name)
    {
        string path = PathOf(name);
        _next = (path, SurfaceOf(path));
    }

    /// <summary>Names the next read's field <paramref name="name"/>, under the open scopes, as bytes no surface models.</summary>
    public void NameUnmodeled(string name) => _next = (PathOf(name), FieldSurface.Unmodeled);

    /// <summary>Names the next read's field as padding after the last field read.</summary>
    public void NamePadding() =>
        _next = (_lastPath is null ? PathOf(Padding) : $"{_lastPath}.{Padding}", FieldSurface.Unmodeled);

    /// <summary>Opens the scope <paramref name="segment"/>: a member, or an element <c>name[index]</c>.</summary>
    public void Push(string segment) => _scopes.Add(segment);

    /// <summary>Closes the innermost scope.</summary>
    public void Pop() => _scopes.RemoveAt(_scopes.Count - 1);

    /// <summary>Starts a read, returning the position it starts at.</summary>
    public long Enter()
    {
        _depth++;
        return stream.Position;
    }

    /// <summary>Ends the read that started at <paramref name="start"/>, recording it if it is outermost.</summary>
    public void Exit(long start, FieldKind kind)
    {
        if (--_depth > 0) return;

        var (path, surface) = _next ?? (PathOf(FieldMap.Unnamed), FieldSurface.Logical);
        _next = null;
        if (stream.Position <= start) return;

        Add(new FieldEntry((uint)start, (uint)(stream.Position - start), path, surface, kind));
        _lastPath = path;
    }

    /// <summary>
    /// The map of a <paramref name="length"/>-byte asset from the reads recorded so far, with the
    /// bytes between them <see cref="FieldMap.Unread"/> and the bytes after them
    /// <see cref="FieldMap.UnparsedTail"/>.
    /// </summary>
    public FieldMap Build(uint length)
    {
        var fields = new List<FieldEntry>(_fields.Count + 1);
        uint end = 0;
        foreach (var field in _fields)
        {
            if (field.Offset > end) fields.Add(Unmodeled(end, field.Offset, FieldMap.Unread));
            fields.Add(field);
            end = field.End;
        }
        if (length > end) fields.Add(Unmodeled(end, length, FieldMap.UnparsedTail));
        return new FieldMap(fields, length);
    }

    private string PathOf(string name) => _scopes.Count == 0 ? name : $"{string.Join('.', _scopes)}.{name}";

    private static FieldSurface SurfaceOf(string path) =>
        path.Split('.').Contains(nameof(Physical)) ? FieldSurface.Physical : FieldSurface.Logical;

    /// <summary>
    /// Keeps <see cref="_fields"/> in offset order without overlap: a read of bytes already read
    /// replaces the fields it overlaps.
    /// </summary>
    private void Add(FieldEntry field)
    {
        int index = _fields.Count;
        while (index > 0 && _fields[index - 1].End > field.Offset) index--;

        int overlapping = 0;
        while (index + overlapping < _fields.Count && _fields[index + overlapping].Offset < field.End) overlapping++;

        _fields.RemoveRange(index, overlapping);
        _fields.Insert(index, field);
    }

    private static FieldEntry Unmodeled(uint start, uint end, string path) =>
        new(start, end - start, path, FieldSurface.Unmodeled, FieldKind.Bytes);
}

/// <summary>
/// One read in progress on a recorded <see cref="EndianReader"/>; a no-op when none is recorded.
/// </summary>
internal readonly ref struct FieldRead
{
    private readonly FieldRecorder? _recorder;
    private readonly long _start;
    private readonly FieldKind _kind;

    public FieldRead(FieldRecorder? recorder, FieldKind kind)
    {
        _recorder = recorder;
        _kind = kind;
        _start = recorder?.Enter() ?? 0;
    }

    public void Dispose() => _recorder?.Exit(_start, _kind);
}

/// <summary>
/// A scope opened by a <c>Field</c> read of <see cref="FieldReads"/>, closed on dispose; a no-op when none is recorded.
/// </summary>
internal readonly ref struct FieldScope(FieldRecorder? recorder)
{
    public void Dispose() => recorder?.Pop();
}
