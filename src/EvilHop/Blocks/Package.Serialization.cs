using EvilHop.Blocks;
using EvilHop.Primitives;
using System.Diagnostics;

namespace EvilHop.Serialization;

public partial class Serializer
{
    /// <summary>
    /// Reads the fields of a <see cref="PackageVersion"/> (PVER) block.
    /// </summary>
    protected static void ReadPackageVersion(BinaryReader reader, PackageVersion block, uint size)
    {
        block.SubVersion = reader.ReadEvilInt();
        block.ClientVersion = (ClientVersion)reader.ReadEvilInt();
        block.CompatVersion = reader.ReadEvilInt();
    }

    /// <summary>
    /// Reads the fields of a <see cref="PackageFlags"/> (PFLG) block.
    /// </summary>
    protected static void ReadPackageFlags(BinaryReader reader, PackageFlags block, uint size)
    {
        block.Flags = (PackFlags)reader.ReadEvilInt();
    }

    /// <summary>
    /// Reads the fields of a <see cref="PackageCount"/> (PCNT) block.
    /// </summary>
    protected static void ReadPackageCount(BinaryReader reader, PackageCount block, uint size)
    {
        block.AssetCount = reader.ReadEvilInt();
        block.LayerCount = reader.ReadEvilInt();
        block.MaxAssetSize = reader.ReadEvilInt();
        block.MaxLayerSize = reader.ReadEvilInt();
        block.MaxXFormAssetSize = reader.ReadEvilInt();
    }

    /// <summary>
    /// Reads the fields of a <see cref="PackageCreated"/> (PCRT) block.
    /// </summary>
    protected static void ReadPackageCreated(BinaryReader reader, PackageCreated block, uint size)
    {
        uint rawCreatedDate = reader.ReadEvilInt();
        block.CreatedDate = DateTimeOffset.FromUnixTimeSeconds(rawCreatedDate);
        block.CreatedDateString = reader.ReadEvilString();
    }

    /// <summary>
    /// Reads the fields of a <see cref="PackageModified"/> (PMOD) block.
    /// </summary>
    protected static void ReadPackageModified(BinaryReader reader, PackageModified block, uint size)
    {
        uint rawModifiedDate = reader.ReadEvilInt();
        block.ModifiedDate = DateTimeOffset.FromUnixTimeSeconds(rawModifiedDate);
    }

    private static readonly Action<PackagePlatform, string>[] PlatformNameRegionLanguageSlots =
    [
        (b, v) => b.PlatformId = v,
        (b, v) => b.PlatformName = v,
        (b, v) => b.Region = v,
        (b, v) => b.Language = v,
        (b, v) => b.GameName = v
    ];

    private static readonly Action<PackagePlatform, string>[] LanguageRegionSlots =
    [
        (b, v) => b.PlatformId = v,
        (b, v) => b.Language = v,
        (b, v) => b.Region = v,
        (b, v) => b.GameName = v
    ];

    /// <summary>
    /// Reads the fields of a <see cref="PackagePlatform"/> (PLAT) block: a run of <c>EvilString</c>s
    /// whose field mapping is governed by <see cref="FormatProfile.PlatformFieldOrder"/>. A run
    /// shorter than the mapped layout leaves the trailing fields at their initializer values,
    /// bounded by <paramref name="size"/>.
    /// </summary>
    protected void ReadPackagePlatform(BinaryReader reader, PackagePlatform block, uint size)
    {
        long end = reader.BaseStream.Position + size;
        var slots = Profile.PlatformFieldOrder switch
        {
            PlatformFieldOrder.PlatformNameRegionLanguage => PlatformNameRegionLanguageSlots,
            PlatformFieldOrder.LanguageRegion => LanguageRegionSlots,
            _ => throw new UnreachableException()
        };

        foreach (var assign in slots)
        {
            if (reader.BaseStream.Position >= end) return;
            assign(block, reader.ReadEvilString());
        }
    }
}
