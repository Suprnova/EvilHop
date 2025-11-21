using EvilHop.Helpers;
using EvilHop.Primitives;

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
    JSPInfo,
    Unknown = uint.MaxValue
}

[Flags]
public enum AssetFlags : uint
{
    None = 0U,
    SourceFile = 1U << 0,
    SourceVirtual = 1U << 1,
    ReadTransform = 1U << 2,
    WriteTransform = 1U << 3
}

public class Dictionary : Block
{
    protected internal override string Id => "DICT";
    protected internal override uint DataLength => 0;

    public AssetTable Table
    {
        get => GetRequiredChild<AssetTable>();
        set => SetChild(value);
    }

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
}

public class AssetTable : Block
{
    protected internal override string Id => "ATOC";
    protected internal override uint DataLength => 0;

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
}

public class AssetInf : Block
{
    protected internal override string Id => "AINF";
    protected internal override uint DataLength => sizeof(uint);

    internal uint Value { get; set; } = 0;
}

public class AssetHeader : Block
{
    protected internal override string Id => "AHDR";
    protected internal override uint DataLength => sizeof(uint) * 6;

    public AssetDebug Debug
    {
        get => GetRequiredChild<AssetDebug>();
        set => SetChild(value);
    }

    // todo: Asset abstraction should handle this binding to Debug.Name
    internal uint AssetId { get; set; }
    internal string Type { get; set; } = ""; // todo: make enum
    internal uint Offset { get; set; }
    internal uint Size { get; set; }
    internal uint Padding { get; set; }
    internal AssetFlags Flags { get; set; } // todo: make enum
}

public class AssetDebug : Block
{
    protected internal override string Id => "ADBG";
    protected internal override uint DataLength
    {
        get
        {
            return sizeof(uint) + Name.GetEvilStringLength() + FileName.GetEvilStringLength() + sizeof(uint);
        }
    }

    internal uint Alignment { get; set; }
    internal string Name { get; set; } = "";
    internal string FileName { get; set; } = "";
    // todo: asset abstraction should handle binding this to the checksum of the asset's data
    internal uint Checksum { get; set; }

}

public class LayerTable : Block
{
    protected internal override string Id => "LTOC";
    protected internal override uint DataLength => 0;

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
}

public class LayerInf : Block
{
    protected internal override string Id => "LINF";
    protected internal override uint DataLength => sizeof(uint);

    internal uint Value { get; set; }
}

public class LayerHeader : Block
{
    protected internal override string Id => "LHDR";
    protected internal override uint DataLength
    {
        get
        {
            return sizeof(uint) + sizeof(uint) + AssetCount * sizeof(uint);
        }
    }

    public LayerDebug LayerDebug
    {
        get => GetRequiredChild<LayerDebug>();
        set => SetChild(value);
    }

    internal LayerType Type { get; set; }
    // todo: asset abstraction should handle this binding to AssetIds.Count()
    internal uint AssetCount { get; set; }
    internal IEnumerable<uint> AssetIds { get; set; } = [];
}

public class LayerDebug : Block
{
    protected internal override string Id => "LDBG";
    protected internal override uint DataLength => sizeof(uint);

    internal uint Value { get; set; }
}
