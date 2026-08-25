namespace EvilHop.Assets;

/// <summary>
/// An <see cref="Asset"/> whose body is a file embedded verbatim in the archive - a RenderWare
/// stream, a Bink video, an audio stream - rather than a <see cref="BaseAsset"/>-shaped game-object
/// record.
/// </summary>
public abstract class PayloadAsset : Asset
{
    internal byte[] Data { get; set; } = [];

    /// <summary>
    /// Writes this <see cref="PayloadAsset"/>'s embedded file to <paramref name="path"/>.
    /// </summary>
    /// <param name="path">The path to write the file to.</param>
    public void SaveToFile(string path) => File.WriteAllBytes(path, Data);

    /// <summary>
    /// Replaces this <see cref="PayloadAsset"/>'s embedded file with the contents of the file at
    /// <paramref name="path"/>.
    /// </summary>
    /// <param name="path">The path to read the file from.</param>
    public void LoadFromFile(string path) => Data = File.ReadAllBytes(path);
}
