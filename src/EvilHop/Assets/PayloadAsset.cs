namespace EvilHop.Assets;

/// <summary>
/// An <see cref="Asset"/> whose body is a file embedded verbatim in the archive, such as
/// a RenderWare stream, a Bink video, or an audio stream.
/// </summary>
public abstract class PayloadAsset : Asset
{
    internal byte[] Data { get; set; } = [];

    /// <summary>
    /// Writes this <see cref="PayloadAsset"/>'s embedded file to <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    public void SaveTo(Stream stream) => stream.Write(Data);

    /// <summary>
    /// Replaces this <see cref="PayloadAsset"/>'s embedded file with the contents of
    /// <paramref name="stream"/>, read to its end.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    public void LoadFrom(Stream stream)
    {
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        Data = buffer.ToArray();
    }

    /// <summary>
    /// Writes this <see cref="PayloadAsset"/>'s embedded file to <paramref name="path"/>.
    /// </summary>
    /// <param name="path">The path to write the file to.</param>
    public void SaveToFile(string path)
    {
        using var file = File.Create(path);
        SaveTo(file);
    }

    /// <summary>
    /// Replaces this <see cref="PayloadAsset"/>'s embedded file with the contents of the file at
    /// <paramref name="path"/>.
    /// </summary>
    /// <param name="path">The path to read the file from.</param>
    public void LoadFromFile(string path)
    {
        using var file = File.OpenRead(path);
        LoadFrom(file);
    }

    /// <summary>
    /// Not supported. A <see cref="PayloadAsset"/> has no unparsed region - its whole body is the
    /// embedded file, replaced through <see cref="LoadFromFile"/>.
    /// </summary>
    /// <param name="bytes">Unused.</param>
    /// <exception cref="NotSupportedException">Always.</exception>
    public override void SetUnparsedTail(byte[] bytes) =>
        throw new NotSupportedException(
            $"A {nameof(PayloadAsset)}'s body is an embedded file, not a parsed record with an " +
            $"unparsed remainder, so it has nowhere to put these bytes. Use " +
            $"{nameof(LoadFromFile)} to replace the file instead.");
}
