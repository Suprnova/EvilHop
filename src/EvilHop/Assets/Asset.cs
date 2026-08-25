using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;

namespace EvilHop.Assets;

/// <summary>
/// An individual resource or object located in a level's archive.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/Assets">Heavy Iron Modding documentation</seealso>
/// </remarks>
/// TODO: review if we need pointers to the blocks in comments, seems irrelevant to consumers
/// seems like a better idea to have this info in Block remarks (like AssetType)
public abstract class Asset
{
    /// <summary>
    /// The <see cref="Asset"/>'s ID, retrieved from <see cref="AssetHeader.Id"/>.
    /// </summary>
    public AssetId Id { get; set; }
    /// <summary>
    /// The <see cref="Asset"/>'s type, retrieved from <see cref="AssetHeader.Type"/>.
    /// </summary>
    public AssetType Type { get; internal set; }
    /// <summary>
    /// The <see cref="Asset"/>'s name, retrieved from <see cref="AssetDebug.Name"/>.
    /// </summary>
    public string Name { get; set; } = String.Empty;
    /// <summary>
    /// The <see cref="Asset"/>'s alignment, retrieved from <see cref="AssetDebug.Alignment"/>.
    /// </summary>
    public int Alignment { get; set; }
    /// <summary>
    /// The <see cref="Asset"/>'s <see cref="AssetFlags"/>, retrieved from <see cref="AssetHeader.Flags"/>.
    /// </summary>
    public AssetFlags Flags { get; set; }
    /// <summary>
    /// The <see cref="Asset"/>'s filename, retrieved from <see cref="AssetDebug.FileName"/>.
    /// </summary>
    public string FileName { get; set; } = String.Empty;

    /// <summary>
    /// The <see cref="Layer"/> that this <see cref="Asset"/> belongs to.
    /// </summary>
    public Layer? Layer { get; internal set; }

    internal byte[] UnparsedTail { get; set; } = [];

    /// <summary>
    /// Returns the bytes that this <see cref="Asset"/> was unable to parse from its
    /// slice of <see cref="StreamData"/>.
    /// </summary>
    /// <returns>The unparsed bytes for this <see cref="Asset"/>.</returns>
    public Span<byte> GetUnparsedTail() => UnparsedTail;

    /// <summary>
    /// Overwrites the existing <see cref="UnparsedTail"/>, if any, with the provided bytes.
    /// </summary>
    /// <param name="bytes">The bytes to append at the end of this <see cref="Asset"/>'s data.</param>
    public void SetUnparsedTail(byte[] bytes) => UnparsedTail = bytes;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    protected Asset? Resolve(AssetId id) => throw new NotImplementedException();

}
