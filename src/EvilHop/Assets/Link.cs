using EvilHop.Primitives;

namespace EvilHop.Assets;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

// TODO: I'll write these comments later.
// TODO: Should we enforce exactly 4 elements in Params?

public struct Link
{
    public short SourceEvent { get; set; }
    public short DestinationEvent { get; set; }
    public AssetId DestinationAssetId { get; set; }
    public Parameter[] Params { get; set; }
    public AssetId ParamWidgetAssetId { get; set; }
    public AssetId CheckAssetId { get; set; }
}
