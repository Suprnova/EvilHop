namespace EvilHop.Blocks;

public enum ClientVersion : uint
{
    N100FPrototype = 0x00000001,
    N100FRelease = 0x00040006,
    Default = 0x000A000F
}

// TODO: validate
[Flags]
public enum PFLG_Flags : uint
{
    Unknown2 = 1U << 1,
    Unknown3 = 1U << 2,
    Unknown4 = 1U << 3,
    Unknown6 = 1U << 5,
    Unknown17 = 1U << 16,
    Unknown18 = 1U << 17,
    Unknown19 = 1U << 18,
    Unknown20 = 1U << 19,
    Unknown21 = 1U << 20,
    Unknown22 = 1U << 21,
    Unknown23 = 1U << 22,
    Unknown25 = 1U << 24,
    Unknown26 = 1U << 25,
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

public class Package : Block
{
    protected internal override string Id => "PACK";

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

public class PackageVersion(uint subVersion, ClientVersion clientVersion, uint compatVersion) : Block
{
    protected internal override string Id => "PVER";

    public uint SubVersion { get; set; } = subVersion;

    public ClientVersion ClientVersion { get; set; } = clientVersion;

    public uint CompatVersion { get; set; } = compatVersion;

    public PackageVersion(ClientVersion clientVersion) : this(2, clientVersion, 1)
    {
    }
}

public class PackageFlags(PFLG_Flags flags) : Block
{
    protected internal override string Id => "PFLG";

    public PFLG_Flags Flags { get; set; } = flags;
}

public class PackageCount(uint assetCount, uint layerCount, uint maxAssetSize, uint maxLayerSize, uint maxXFormAssetSize) : Block
{
    protected internal override string Id => "PCNT";

    /// <summary>
    /// The number of assets present in the archive, and by conjunction the number of <see cref="AHDR"/> blocks present.
    /// </summary>
    public uint AssetCount { get; set; } = assetCount;
    /// <summary>
    /// The number of layers present in the archive, and by conjunction the number of <see cref="LHDR"/> blocks present.
    /// </summary>
    public uint LayerCount { get; set; } = layerCount;
    /// <summary>
    /// The largest asset size among all of the assets in the archive, in bytes.
    /// </summary>
    public uint MaxAssetSize { get; set; } = maxAssetSize;
    /// <summary>
    /// The largest layer size, excluding padding, among all of the layers in the archive, in bytes.
    /// </summary>
    public uint MaxLayerSize { get; set; } = maxLayerSize;
    /// <summary>
    /// The largest asset size among all of the assets in the archive with the <see cref="ADHR.ADHR_Flags.READ_TRANSFORM"/> flag set, in bytes.
    /// </summary>
    public uint MaxXFormAssetSize { get; set; } = maxXFormAssetSize;

    internal PackageCount() : this(0, 0, 0, 0, 0)
    {
    }
}

public class PackageCreated(DateTime createdDate, string createdDateString) : Block
{
    private static readonly String _dateTimeFormat = "ddd MMM dd HH:mm:ss yyyy";

    protected internal override string Id => "PCRT";

    // TODO: should be in UTC-7
    public DateTime CreatedDate { get; set; } = createdDate;
    public string CreatedDateString { get; set; } = createdDateString;

    internal PackageCreated() : this(DateTime.Now, DateTime.Now.ToString(_dateTimeFormat))
    {
    }

    public PackageCreated(DateTime createdDate) : this(createdDate, createdDate.ToString(_dateTimeFormat))
    {
    }
}

public class PackageModified(DateTime modifiedDate) : Block
{
    protected internal override string Id => "PMOD";

    // TODO: should be in UTC-7
    public DateTime ModifiedDate { get; set; } = modifiedDate;

    internal PackageModified() : this(DateTime.Now)
    {
    }
}

public class PackagePlatform(string platformId, string region, string language, string gameName, string? platformName = null) : Block
{
    protected internal override string Id => "PLAT";

    public string PlatformID { get; set; } = platformId;
    public string? PlatformName { get; set; } = platformName;
    public string Region { get; set; } = region;
    public string Language { get; set; } = language;
    public string GameName { get; set; } = gameName;
}
