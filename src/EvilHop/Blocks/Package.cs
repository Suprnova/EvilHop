namespace EvilHop.Blocks;

public enum SubVersion : uint
{
    Default = 0x00000002
}

public enum ClientVersion : uint
{
    N100FPrototype = 0x00000001,
    N100FRelease = 0x00040006,
    Default = 0x000A000F
}

public enum CompatVersion : uint
{
    Default = 0x00000001
}

public class Package : Block
{
    protected internal override string Id => "PACK";
    protected internal override uint DataLength => 0;

    public PackageVersion Versions
    {
        get => GetRequiredChild<PackageVersion>();
        set => SetChild(value);
    }

    public PackageFlags Flags
    {
        get => GetRequiredChild<PackageFlags>();
        set => SetChild(value);
    }

    public PackageCount Counts
    {
        get => GetRequiredChild<PackageCount>();
        set => SetChild(value);
    }

    public PackageCreated Created
    {
        get => GetRequiredChild<PackageCreated>();
        set => SetChild(value);
    }

    public PackageModified Modified
    {
        get => GetRequiredChild<PackageModified>();
        set => SetChild(value);
    }

    public PackagePlatform? Platform
    {
        get => GetChild<PackagePlatform>();
        set => SetChild(value);
    }

    // todo: implement public default based on file version
    internal Package()
    {
    }

    public Package(PackageVersion packageVersion, PackageFlags packageFlags, PackageCount packageCount, PackageCreated packageCreated, PackageModified packageModified,
        PackagePlatform? packagePlatform = null
        )
    {
        Children.AddRange([
            packageVersion,
            packageFlags,
            packageCount,
            packageCreated,
            packageModified
        ]);
        if (packagePlatform != null) Children.Add(packagePlatform);
    }
}
public class PackageVersion : Block
{
    protected internal override string Id => "PVER";
    protected internal override uint DataLength => sizeof(uint) * 3;

    public SubVersion SubVer { get; set; } = SubVersion.Default;

    public ClientVersion ClientVer { get; set; } = ClientVersion.Default;

    public CompatVersion CompatVer { get; set; } = CompatVersion.Default;

    public PackageVersion()
    {
    }

    public PackageVersion(SubVersion subVer, ClientVersion clientVer, CompatVersion compatVer)
    {
        SubVer = subVer;
        ClientVer = clientVer;
        CompatVer = compatVer;
    }
}

public class PackageFlags : Block
{
    protected internal override string Id => "PFLG";
    protected internal override uint DataLength => sizeof(uint);

    // TODO: validate
    [Flags]
    public enum PFLG_Flags : uint
    {
        None = 0U,
        Unknown1 = 1U << 0,
        Unknown2 = 1U << 1,
        Unknown3 = 1U << 2,
        Unknown4 = 1U << 3,
        Unknown5 = 1U << 4,
        Unknown6 = 1U << 5,
        Unknown7 = 1U << 6,
        Unknown8 = 1U << 7,
        Unknown9 = 1U << 8,
        Unknown10 = 1U << 9,
        Unknown11 = 1U << 10,
        Unknown12 = 1U << 11,
        Unknown13 = 1U << 12,
        Unknown14 = 1U << 13,
        Unknown15 = 1U << 14,
        Unknown16 = 1U << 15,
        Unknown17 = 1U << 16,
        Unknown18 = 1U << 17,
        Unknown19 = 1U << 18,
        Unknown20 = 1U << 19,
        Unknown21 = 1U << 20,
        Unknown22 = 1U << 21,
        Unknown23 = 1U << 22,
        Unknown24 = 1U << 23,
        Unknown25 = 1U << 24,
        Unknown26 = 1U << 25,
        Unknown27 = 1U << 26,
        Unknown28 = 1U << 27,
        Unknown29 = 1U << 28,
        Unknown30 = 1U << 29,
        Unknown31 = 1U << 30,
        Unknown32 = 1U << 31,
        // todo: these are additive, probably should create values for their sums
        Default = Unknown2 | Unknown3 | Unknown4 | Unknown6,
        DE_PS2_BFBB = Unknown21 | Unknown19,
        US_GC_BFBB = Unknown22 | Unknown20 | Unknown17,
        US_XBOX_BFBB = Unknown22 | Unknown20 | Unknown18,
        US_PS2_BFBB = Unknown22 | Unknown20 | Unknown19,
        GC_MNPAL_BFBB = Unknown23 | Unknown21 | Unknown17,
        US_BFBB = Unknown26,
        DE_PS2_BFBB_2 = Unknown26 | Unknown25
    }

    public PFLG_Flags Flags { get; set; } = PFLG_Flags.Default;

    public PackageFlags()
    {
    }

    public PackageFlags(PFLG_Flags flags)
    {
        Flags = flags;
    }
}

public class PackageCount : Block
{
    protected internal override string Id => "PCNT";
    protected internal override uint DataLength => sizeof(uint) * 5;

    /// <summary>
    /// The number of assets present in the archive, and by conjunction the number of <see cref="AHDR"/> blocks present.
    /// </summary>
    internal uint AssetCount { get; set; } = 0;
    /// <summary>
    /// The number of layers present in the archive, and by conjunction the number of <see cref="LHDR"/> blocks present.
    /// </summary>
    internal uint LayerCount { get; set; } = 0;
    /// <summary>
    /// The largest asset size among all of the assets in the archive, in bytes.
    /// </summary>
    internal uint MaxAssetSize { get; set; } = 0;
    /// <summary>
    /// The largest layer size, excluding padding, among all of the layers in the archive, in bytes.
    /// </summary>
    internal uint MaxLayerSize { get; set; } = 0;
    /// <summary>
    /// The largest asset size among all of the assets in the archive with the <see cref="ADHR.ADHR_Flags.READ_TRANSFORM"/> flag set, in bytes.
    /// </summary>
    internal uint MaxXFormAssetSize { get; set; } = 0;

    public PackageCount()
    {
    }

    public PackageCount(uint assetCount, uint layerCount, uint maxAssetSize, uint maxLayerSize, uint maxXFormAssetSize)
    {
        AssetCount = assetCount;
        LayerCount = layerCount;
        MaxAssetSize = maxAssetSize;
        MaxLayerSize = maxLayerSize;
        MaxXFormAssetSize = maxXFormAssetSize;
    }
}

public class PackageCreated : Block
{
    protected internal override string Id => "PCRT";
    // Always 26 for string size, even in n100f where CreatedDateString ends in '\n', due to EvilString handling
    protected internal override uint DataLength => sizeof(uint) + 26;

    // TODO: should be in UTC-7
    public DateTime CreatedDate
    {
        get;
        set
        {
            field = value;
            CreatedDateString = CreatedDate.ToString("ddd MMM dd HH:mm:ss yyyy");
        }
    }
    // TODO: should end with newline in n100f
    internal string CreatedDateString { get; set; }

    public PackageCreated()
    {
        CreatedDate = DateTime.Now;
        CreatedDateString = CreatedDate.ToString("ddd MMM dd HH:mm:ss yyyy");
    }

    public PackageCreated(DateTime createdDate)
    {
        CreatedDate = createdDate;
        CreatedDateString = CreatedDate.ToString("ddd MMM dd HH:mm:ss yyyy");
    }
}

public class PackageModified : Block
{
    protected internal override string Id => "PMOD";
    protected internal override uint DataLength => sizeof(uint);

    // TODO: should be in UTC-7
    public DateTime ModifiedDate { get; set; }

    public PackageModified()
    {
        ModifiedDate = DateTime.Now;
    }

    public PackageModified(DateTime modifiedDate)
    {
        ModifiedDate = modifiedDate;
    }
}

public class PackagePlatform : Block
{
    protected internal override string Id => "PLAT";
    // not accurate, must be determined by serializer
    protected internal override uint DataLength => 0;

    public string PlatformID { get; set; } = "";
    public string? PlatformName { get; set; }
    public string Region { get; set; } = "";
    public string Language { get; set; } = "";
    public string GameName { get; set; } = "";
}
