namespace EvilHop.Blocks;

public class AssetStream : Block
{
    protected internal override string Id => "STRM";

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

    internal AssetStream()
    {
    }
}

public class StreamHeader(uint value) : Block
{
    protected internal override string Id => "DHDR";

    internal uint Value { get; set; } = value;

    internal StreamHeader() : this(0xFFFFFFFF)
    {
    }
}

public class StreamData(uint paddingAmount, byte[] data) : Block
{
    protected internal override string Id => "DPAK";

    internal uint PaddingAmount { get; set; } = paddingAmount;
    internal byte[] Data { get; set; } = data;

    internal StreamData() : this(0, [])
    {
    }
}
