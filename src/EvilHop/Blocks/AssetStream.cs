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

    internal AssetStream()
    {
    }
}

public class StreamHeader(uint value) : Block
{
    protected internal override string Id => "DHDR";
    protected internal override uint DataLength => sizeof(uint);

    internal uint Value { get; set; } = value;

    internal StreamHeader() : this(0xFFFFFFFF)
    {
    }
}

public class StreamData(uint paddingAmount, byte[] data) : Block
{
    protected internal override string Id => "DPAK";
    protected internal override uint DataLength
    {
        get
        {
            return (uint)(sizeof(uint) + PaddingAmount + Data.Length);
        }
    }

    internal uint PaddingAmount { get; set; } = paddingAmount;
    internal byte[] Data { get; set; } = data;

    internal StreamData() : this(0, [])
    {
    }
}
