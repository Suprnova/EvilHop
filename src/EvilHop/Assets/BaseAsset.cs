namespace EvilHop.Assets;

public abstract class Asset
{
    public string? Name { get; set; } // todo: make not nullable once constructors are set up
}

[Flags]
public enum BaseFlags : ushort
{
    Enabled = 1 << 0,
    Persistent = 1 << 1,
    Valid = 1 << 2,
    VisibleDuringCutscenes = 1 << 3,
    ReceiveShadows = 1 << 4,
}

abstract class BaseAsset : Asset
{
    public uint Id { get; set; }
    public byte BaseType { get; set; }
    public byte LinkCount { get; set; }
    public BaseFlags Flags { get; set; }
}
