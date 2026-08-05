using EvilHop.Common;
using EvilHop.Serialization;
using EvilHop.Serialization.Validation;

namespace EvilHop.Blocks;

public enum LayerType : uint
{
    Default = 0,
    Texture,
    TextureStream,
    BSP,
    Model,
    Animation,
    VRAM,
    SRAM,
    SoundTable,
    Cutscene,
    CutsceneTable,
    JSPInfo
}

[Flags]
public enum AssetFlags : uint
{
    None = 0U,
    SourceFile = 1U << 0,
    SourceVirtual = 1U << 1,
    ReadTransform = 1U << 2,
    WriteTransform = 1U << 3,
    UnknownScooby = 1U << 31
}

[ExpectedChildCount(2)]
[RequiredChild(typeof(AssetTable))]
[RequiredChild(typeof(LayerTable))]
public class Dictionary : Block
{
    protected internal override string Id => "DICT";

    public AssetTable AssetTable
    {
        get => GetRequiredChild<AssetTable>();
        set => SetChild(value);
    }

    public LayerTable LayerTable
    {
        get => GetRequiredChild<LayerTable>();
        set => SetChild(value);
    }

    internal Dictionary()
    {
    }

    public Dictionary(AssetTable assetTable, LayerTable layerTable)
    {
        Children.AddRange([
            assetTable,
            layerTable
        ]);
    }
}

[RequiredChild(typeof(AssetInf))]
public class AssetTable : Block
{
    protected internal override string Id => "ATOC";

    public AssetInf AssetInf
    {
        get => GetRequiredChild<AssetInf>();
        set => SetChild(value);
    }

    public IEnumerable<AssetHeader> AssetHeaders
    {
        get => GetVariableChildren<AssetHeader>();
        set => SetVariableChildren(value);
    }

    internal AssetTable()
    {
    }

    public AssetTable(AssetInf assetInf, IEnumerable<AssetHeader> assetHeaders)
    {
        Children.Add(assetInf);
        Children.AddRange(assetHeaders);
    }
}

[ExpectedChildCount(0)]
public class AssetInf(uint value) : Block
{
    protected internal override string Id => "AINF";

    [ExpectedValue(0x00000000)]
    public uint Value { get; set; } = value;

    internal AssetInf() : this(0)
    {
    }
}

[ExpectedChildCount(1)]
[RequiredChild(typeof(AssetDebug))]
public class AssetHeader : Block
{
    protected internal override string Id => "AHDR";

    public AssetDebug Debug
    {
        get => GetRequiredChild<AssetDebug>();
        set => SetChild(value);
    }

    // todo: Asset abstraction should handle this binding to Debug.Name
    public uint AssetId { get; set; }
    public AssetType Type { get; set; }
    public uint Offset { get; set; }
    public uint Size { get; set; }
    public uint Padding { get; set; }
    public AssetFlags Flags { get; set; }

    internal AssetHeader()
    {
    }

    internal AssetHeader(AssetDebug debug)
    {
        Children.Add(debug);
    }
}

[ExpectedChildCount(0)]
public class AssetDebug(uint alignment, string name, string fileName, uint checksum) : Block
{
    protected internal override string Id => "ADBG";

    public uint Alignment { get; set; } = alignment;
    public string Name { get; set; } = name;
    public string FileName { get; set; } = fileName;
    // todo: asset abstraction should handle binding this to the checksum of the asset's data
    public uint Checksum { get; set; } = checksum;

    internal AssetDebug() : this(0, "", "", 0)
    {
    }
}

[RequiredChild(typeof(LayerInf))]
public class LayerTable : Block
{
    protected internal override string Id => "LTOC";

    public LayerInf LayerInf
    {
        get => GetRequiredChild<LayerInf>();
        set => SetChild(value);
    }

    public IEnumerable<LayerHeader> LayerHeaders
    {
        get => GetVariableChildren<LayerHeader>();
        set => SetVariableChildren(value);
    }

    internal LayerTable()
    {
    }

    public LayerTable(LayerInf layerInf, IEnumerable<LayerHeader> layerHeaders)
    {
        Children.Add(layerInf);
        Children.AddRange(layerHeaders);
    }
}

[ExpectedChildCount(0)]
public class LayerInf(uint value) : Block
{
    protected internal override string Id => "LINF";

    [ExpectedValue(0x00000000)]
    public uint Value { get; set; } = value;

    internal LayerInf() : this(0)
    {
    }
}

[ExpectedChildCount(1)]
[RequiredChild(typeof(LayerDebug))]
public class LayerHeader : Block
{
    protected internal override string Id => "LHDR";

    public LayerDebug LayerDebug
    {
        get => GetRequiredChild<LayerDebug>();
        set => SetChild(value);
    }

    public LayerType Type { get; set; }
    // todo: asset abstraction should handle this binding to AssetIds.Count()
    public uint AssetCount { get; set; }
    public IEnumerable<uint> AssetIds { get; set; } = [];

    internal LayerHeader()
    {
    }

    internal LayerHeader(LayerDebug layerDebug)
    {
        Children.Add(layerDebug);
    }
}

[ExpectedChildCount(0)]
public class LayerDebug(uint value) : Block
{
    protected internal override string Id => "LDBG";

    [ExpectedValue(0xFFFFFFFF)]
    public uint Value { get; set; } = value;

    internal LayerDebug() : this(0)
    {
    }
}
