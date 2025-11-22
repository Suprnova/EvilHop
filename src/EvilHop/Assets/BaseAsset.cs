namespace EvilHop.Assets;

public abstract class Asset
{
    public byte[] BinaryData { get; set; } = [];
}

// todo: determine validation for an unknown flag being set
[Flags]
public enum BaseFlags : ushort
{
    None = 0,
    Enabled = 1 << 0,
    Persistent = 1 << 1,
    Valid = 1 << 2,
    VisibleInCutscenes = 1 << 3,
    ReceiveShadows = 1 << 4
}

public abstract class BaseAsset : Asset
{
    public uint Id { get; set; }
    public byte BaseType { get; set; }
    public byte LinkCount { get; set; }
    public BaseFlags BaseFlags { get; set; }
}
