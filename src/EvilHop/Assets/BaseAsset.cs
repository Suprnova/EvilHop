using EvilHop.Primitives;

namespace EvilHop.Assets;

/// <summary>
/// 
/// </summary>
public abstract class BaseAsset : Asset
{
    /// <summary>
    /// 
    /// </summary>
    public AssetId BaseId { get; set; }
    /// <summary>
    /// 
    /// </summary>
    public byte BaseType { get; set; }
    /// <summary>
    /// 
    /// </summary>
    public BaseAssetFlags BaseFlags { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public byte LinkCount { get; set; }
    /// <summary>
    /// 
    /// </summary>
    public List<Link> Links { get; } = [];
}

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

/// <summary>
/// 
/// </summary>
[Flags]
public enum BaseAssetFlags : short
{
    None = 0,
    Enabled = 1 << 0,
    Persistent = 1 << 1,
    Valid = 1 << 2,
    VisibleDuringCutscenes = 1 << 3,
    ReceiveShadows = 1 << 4,
}
