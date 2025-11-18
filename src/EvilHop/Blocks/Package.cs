using EvilHop.Exceptions;
using EvilHop.Extensions;

namespace EvilHop.Blocks;

public class Package : Block
{
    public Package() : base("PACK", 0, [])
    {
        Children.AddRange([
            new PackageVersion(),
            new PackageFlags(),
            new PackageCount(),
            new PackageCreated(),
            new PackageModified(),
            // new PackagePlatform()
        ]);
        Length = (uint)Children.Sum(child => child.Length);
    }

    public Package(PackageVersion packageVersion, PackageFlags packageFlags, PackageCount packageCount, PackageCreated packageCreated, PackageModified packageModified
        //, PackagePlatform packagePlatform
        ) : base("PACK", 0, [])
    {
        Children.AddRange([
            packageVersion,
            packageFlags,
            packageCount,
            packageCreated,
            packageModified,
            // packagePlatform
        ]);
        Length = (uint)Children.Sum(child => child.Length);
    }

    public Package(BinaryReader reader) : base(reader.ReadEvilInt(), reader.ReadEvilInt(), [])
    {
        if (!this.Id.Equals("PACK")) throw new ArgumentException("Invalid magic number; not a Package block.");
        Children.AddRange([
            reader.ReadPVER(),
            reader.ReadPFLG(),
            reader.ReadPCNT(),
            reader.ReadPCRT(),
            reader.ReadPMOD(),
            // reader.ReadPLAT()
        ]);
        // TODO: validate length is valid, else throw. add to all constructors
        // implement in Block class, accept argument for known data size
    }

    public class PackageVersion : Block
    {
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

        public PackageVersion() : base("PVER", 12, [])
        {
            SubVer = SubVersion.Default;
            ClientVer = ClientVersion.Default;
            CompatVer = CompatVersion.Default;
        }

        public PackageVersion(SubVersion subVer, ClientVersion clientVer, CompatVersion compatVer) : base("PVER", 12, [])
        {
            SubVer = subVer;
            ClientVer = clientVer;
            CompatVer = compatVer;
        }

        public PackageVersion(BinaryReader reader) : base(reader.ReadEvilInt(), reader.ReadEvilInt(), [])
        {
            if (!this.Id.Equals("PVER")) throw new ArgumentException("Invalid magic number; not a PackageVersion block.");
            SubVer = (SubVersion)reader.ReadEvilInt();
            ClientVer = (ClientVersion)reader.ReadEvilInt();
            CompatVer = (CompatVersion)reader.ReadEvilInt();
        }
    }

    public class PackageFlags : Block
    {
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
            // todo: wiki is confusing, do these replace the other flags or get added to them
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

        public PackageFlags() : base("PFLG", 4, [])
        {
            Flags = PFLG_Flags.Default;
        }

        public PackageFlags(PFLG_Flags flags) : base("PFLG", 4, [])
        {
            Flags = flags;
        }

        public PackageFlags(BinaryReader reader) : base(reader.ReadEvilInt(), reader.ReadEvilInt(), [])
        {
            if (!this.Id.Equals("PFLG")) throw new ArgumentException("Invalid magic number; not a PackageFlags block.");
            Flags = (PFLG_Flags)reader.ReadEvilInt();
        }
    }

    public class PackageCount : Block
    {
        /// <summary>
        /// The number of assets present in the archive, and by conjunction the number of <see cref="AHDR"/> blocks present.
        /// </summary>
        public uint AssetCount { get; set; }
        /// <summary>
        /// The number of layers present in the archive, and by conjunction the number of <see cref="LHDR"/> blocks present.
        /// </summary>
        public uint LayerCount { get; set; }
        /// <summary>
        /// The largest asset size among all of the assets in the archive, in bytes.
        /// </summary>
        public uint MaxAssetSize { get; set; }
        /// <summary>
        /// The largest layer size, excluding padding, among all of the layers in the archive, in bytes.
        /// </summary>
        public uint MaxLayerSize { get; set; }
        /// <summary>
        /// The largest asset size among all of the assets in the archive with the <see cref="ADHR.ADHR_Flags.READ_TRANSFORM"/> flag set, in bytes.
        /// </summary>
        public uint MaxXFormAssetSize { get; set; }

        public PackageCount() : base("PCRT", 20, [])
        {
            // todo: determine reasonable defaults
            AssetCount = 0;
            LayerCount = 0;
            MaxAssetSize = 0;
            MaxLayerSize = 0;
            MaxXFormAssetSize = 0;
        }

        public PackageCount(uint assetCount, uint layerCount, uint maxAssetSize, uint maxLayerSize, uint maxXFormAssetSize)
            : base("PCRT", 20, [])
        {
            AssetCount = assetCount;
            LayerCount = layerCount;
            MaxAssetSize = maxAssetSize;
            MaxLayerSize = maxLayerSize;
            MaxXFormAssetSize = maxXFormAssetSize;
        }

        public PackageCount(BinaryReader reader) : base(reader.ReadEvilInt(), reader.ReadEvilInt(), [])
        {
            if (!this.Id.Equals("PCNT")) throw new ArgumentException("Invalid magic number; not a PackageCount block.");
            AssetCount = reader.ReadEvilInt();
            LayerCount = reader.ReadEvilInt();
            MaxAssetSize = reader.ReadEvilInt();
            MaxLayerSize = reader.ReadEvilInt();
            MaxXFormAssetSize = reader.ReadEvilInt();
        }
    }

    public class PackageCreated : Block
    {
        // TODO: should be in UTC-7
        public DateTime CreatedDate { get; set; }
        // TODO: should end with newline in n100f
        public string CreatedDateString { get => CreatedDate.ToString("ddd MMM dd HH:mm:ss yyyy"); }
        
        public PackageCreated() : base("PCRT", 30, [])
        {
            CreatedDate = DateTime.Now;
        }

        public PackageCreated(DateTime createdDate) : base("PCRT", 30, [])
        {
            CreatedDate = createdDate;
        }

        public PackageCreated(BinaryReader reader) : base(reader.ReadEvilInt(), reader.ReadEvilInt(), [])
        {
            if (!this.Id.Equals("PCRT")) throw new ArgumentException("Invalid magic number; not a PackageCreated block.");
            CreatedDate = DateTime.UnixEpoch.AddSeconds(reader.ReadEvilInt());
            reader.ReadEvilString();
        }
    }

    public class PackageModified : Block
    {
        // TODO: should be in UTC-7
        public DateTime ModifiedDate { get; set; }

        public PackageModified() : base("PMOD", 4, [])
        {
            ModifiedDate = DateTime.Now;
        }

        public PackageModified(DateTime modifiedDate) : base("PMOD", 4, [])
        {
            ModifiedDate = modifiedDate;
        }

        public PackageModified(BinaryReader reader) : base(reader.ReadEvilInt(), reader.ReadEvilInt(), [])
        {
            if (!this.Id.Equals("PMOD")) throw new ArgumentException("Invalid magic number; not a PackageModified block.");
            ModifiedDate = DateTime.MinValue.AddSeconds(reader.ReadEvilInt());
        }
    }
}

