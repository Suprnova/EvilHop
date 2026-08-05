using EvilHop.Serialization;
using EvilHop.Serialization.Validation;

namespace EvilHop.Blocks;

[ExpectedChildCount(2)]
[RequiredChild(typeof(StreamHeader))]
[RequiredChild(typeof(StreamData))]
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

    public AssetStream(StreamHeader header, StreamData data)
    {
        Children.AddRange([
            header,
            data
        ]);
    }
}

[ExpectedChildCount(0)]
public class StreamHeader(uint value) : Block
{
    protected internal override string Id => "DHDR";

    [ExpectedValue(0xFFFFFFFF)]
    internal uint Value { get; set; } = value;

    internal StreamHeader() : this(0xFFFFFFFF)
    {
    }
}

[ExpectedChildCount(0)]
public class StreamData(uint paddingAmount, byte[] data) : Block
{
    protected internal override string Id => "DPAK";

    internal uint PaddingAmount { get; set; } = paddingAmount;
    internal byte[] Data { get; set; } = data;

    internal StreamData() : this(0, [])
    {
    }
}
