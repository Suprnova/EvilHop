using EvilHop.Exceptions;
using EvilHop.Extensions;

namespace EvilHop.Blocks;

public class PackageVersion : Block
{
    protected internal override string Id => "PVER";
    protected override uint DataLength => 12;

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

    public SubVersion SubVer
    {
        get;
        set => field = Enum.IsDefined(value)
            ? value
            : throw new UnrecognizedEnumValueException<SubVersion>(nameof(value), value);
    }

    public ClientVersion ClientVer
    {
        get;
        set => field = Enum.IsDefined(value)
            ? value
            : throw new UnrecognizedEnumValueException<ClientVersion>(nameof(value), value);
    }

    public CompatVersion CompatVer
    {
        get;
        set => field = Enum.IsDefined(value)
            ? value
            : throw new UnrecognizedEnumValueException<CompatVersion>(nameof(value), value);
    }

    public PackageVersion()
    {
        SubVer = SubVersion.Default;
        ClientVer = ClientVersion.Default;
        CompatVer = CompatVersion.Default;
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
    protected override uint DataLength => 4;

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

    public PFLG_Flags Flags { get; set; }

    public PackageFlags()
    {
        Flags = PFLG_Flags.Default;
    }

    public PackageFlags(PFLG_Flags flags)
    {
        Flags = flags;
    }
}

public class PackageCount : Block
{
    protected internal override string Id => "PCNT";
    protected override uint DataLength => 20;

    /// <summary>
    /// The number of assets present in the archive, and by conjunction the number of <see cref="AHDR"/> blocks present.
    /// </summary>
    public uint AssetCount { get; internal set; }
    /// <summary>
    /// The number of layers present in the archive, and by conjunction the number of <see cref="LHDR"/> blocks present.
    /// </summary>
    public uint LayerCount { get; internal set; }
    /// <summary>
    /// The largest asset size among all of the assets in the archive, in bytes.
    /// </summary>
    public uint MaxAssetSize { get; internal set; }
    /// <summary>
    /// The largest layer size, excluding padding, among all of the layers in the archive, in bytes.
    /// </summary>
    public uint MaxLayerSize { get; internal set; }
    /// <summary>
    /// The largest asset size among all of the assets in the archive with the <see cref="ADHR.ADHR_Flags.READ_TRANSFORM"/> flag set, in bytes.
    /// </summary>
    public uint MaxXFormAssetSize { get; internal set; }

    public PackageCount()
    {
        // todo: determine reasonable defaults
        AssetCount = 0;
        LayerCount = 0;
        MaxAssetSize = 0;
        MaxLayerSize = 0;
        MaxXFormAssetSize = 0;
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
    // Always 30, even in n100f where CreatedDateString ends in '\n', due to EvilString handling
    protected override uint DataLength => 30;

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

    //public PackageCreated(BinaryReader reader)
    //{
    //    if (!this.Id.Equals("PCRT")) throw new ArgumentException("Invalid magic number; not a PackageCreated block.");
    //    CreatedDate = DateTime.UnixEpoch.AddSeconds(reader.ReadEvilInt());
    //    reader.ReadEvilString();
    //}
}

public class PackageModified : Block
{
    protected internal override string Id => "PMOD";
    protected override uint DataLength => 4;

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

    //public PackageModified(BinaryReader reader)
    //{
    //    if (!this.Id.Equals("PMOD")) throw new ArgumentException("Invalid magic number; not a PackageModified block.");
    //    ModifiedDate = DateTime.MinValue.AddSeconds(reader.ReadEvilInt());
    //}
}

public class PackagePlatform(string platformId, string? platformName, string region, string language, string gameName) : Block
{
    protected internal override string Id => "PLAT";
    protected override uint DataLength
    {
        get
        {
            return
                PlatformID.GetEvilStringLength() + (PlatformName?.GetEvilStringLength() ?? 0) +
                Region.GetEvilStringLength() + Language.GetEvilStringLength() + GameName.GetEvilStringLength();
        }
    }

    public string PlatformID { get; set; } = platformId; // todo: make enum
    public string? PlatformName { get; set; } = platformName;
    public string Region { get; set; } = region; // make enum
    public string Language { get; set; } = language; // make enum
    public string GameName { get; set; } = gameName; // make enum

    public PackagePlatform() : this("GC", null, "NTSC", "US", "Incredibles")
    {
    }
}

public class Package() : Block
{
    protected internal override string Id => "PACK";
    protected override uint DataLength => 0;

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

    public Package(PackageVersion packageVersion, PackageFlags packageFlags, PackageCount packageCount, PackageCreated packageCreated, PackageModified packageModified,
        PackagePlatform? packagePlatform
        ) : this()
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

    //public Package(BinaryReader reader) : base(reader.ReadEvilInt(), reader.ReadEvilInt(), [])
    //{
    //    if (!this.Id.Equals("PACK")) throw new ArgumentException("Invalid magic number; not a Package block.");
    //    Children.AddRange([
    //        reader.ReadPVER(),
    //        reader.ReadPFLG(),
    //        reader.ReadPCNT(),
    //        reader.ReadPCRT(),
    //        reader.ReadPMOD(),
    //        // reader.ReadPLAT()
    //    ]);
    //    // TODO: validate length is valid, else throw. add to all constructors
    //    // implement in Block class, accept argument for known data size
    //}










}

