namespace EvilHop.Blocks;

public class AssetStream : Block
{
    protected internal override string Id => "STRM";
    protected internal override uint DataLength => 0;

    public StreamHeader Header
    {
        get => GetRequiredChild<StreamHeader>();
        set => SetChild(value);
    }

    public StreamData Data
    {
        get => GetRequiredChild<StreamData>();
        set => SetChild(value);
    }
}

public class StreamHeader : Block
{
    protected internal override string Id => "DHDR";
    protected internal override uint DataLength => sizeof(uint);

    internal uint Value { get; set; } = 0xFFFFFFFF;
}

public class StreamData : Block
{
    protected internal override string Id => "DPAK";
    // todo: dynamic
    protected internal override uint DataLength => 0;

    internal uint PaddingAmount { get; set; }
    internal byte[] Data { get; set; } = [];
}
