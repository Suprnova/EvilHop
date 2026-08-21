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
    /// Writes the fields of a <see cref="PackageVersion"/> (PVER) block.
    /// </summary>
    protected static void WritePackageVersion(BinaryWriter writer, PackageVersion block)
    {
        writer.WriteEvilInt(block.SubVersion);
        writer.WriteEvilInt((uint)block.ClientVersion);
        writer.WriteEvilInt(block.CompatVersion);
    }

    /// <summary>
    /// Reads the fields of a <see cref="PackageFlags"/> (PFLG) block.
    /// </summary>
    protected static void ReadPackageFlags(BinaryReader reader, PackageFlags block, uint size)
    {
        block.Flags = (PackFlags)reader.ReadEvilInt();
    }

    /// <summary>
    /// Writes the fields of a <see cref="PackageFlags"/> (PFLG) block.
    /// </summary>
    protected static void WritePackageFlags(BinaryWriter writer, PackageFlags block) =>
        writer.WriteEvilInt((uint)block.Flags);

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
    /// Writes the fields of a <see cref="PackageCount"/> (PCNT) block.
    /// </summary>
    protected static void WritePackageCount(BinaryWriter writer, PackageCount block)
    {
        writer.WriteEvilInt(block.AssetCount);
        writer.WriteEvilInt(block.LayerCount);
        writer.WriteEvilInt(block.MaxAssetSize);
        writer.WriteEvilInt(block.MaxLayerSize);
        writer.WriteEvilInt(block.MaxXFormAssetSize);
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
    /// Writes the fields of a <see cref="PackageCreated"/> (PCRT) block.
    /// </summary>
    protected static void WritePackageCreated(BinaryWriter writer, PackageCreated block)
    {
        writer.WriteEvilInt((uint)block.CreatedDate.ToUnixTimeSeconds());
        writer.WriteEvilString(block.CreatedDateString);
    }

    /// <summary>
    /// Reads the fields of a <see cref="PackageModified"/> (PMOD) block.
    /// </summary>
    protected static void ReadPackageModified(BinaryReader reader, PackageModified block, uint size)
    {
        uint rawModifiedDate = reader.ReadEvilInt();
        block.ModifiedDate = DateTimeOffset.FromUnixTimeSeconds(rawModifiedDate);
    }

    /// <summary>
    /// Writes the fields of a <see cref="PackageModified"/> (PMOD) block.
    /// </summary>
    protected static void WritePackageModified(BinaryWriter writer, PackageModified block) =>
        writer.WriteEvilInt((uint)block.ModifiedDate.ToUnixTimeSeconds());

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

    private static readonly Func<PackagePlatform, string?>[] PlatformNameRegionLanguageWriteSlots =
    [
        b => b.PlatformId,
        b => b.PlatformName,
        b => b.Region,
        b => b.Language,
        b => b.GameName
    ];

    private static readonly Func<PackagePlatform, string?>[] LanguageRegionWriteSlots =
    [
        b => b.PlatformId,
        b => b.Language,
        b => b.Region,
        b => b.GameName
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

    /// <summary>
    /// Writes the fields of a <see cref="PackagePlatform"/> (PLAT) block: a run of <c>EvilString</c>s
    /// whose field mapping is governed by <see cref="FormatProfile.PlatformFieldOrder"/>. Always
    /// writes the active layout's full field count, substituting <c>""</c> for any unset field -
    /// including a <see langword="null"/> <see cref="PackagePlatform.PlatformName"/> - since nothing
    /// in the model records how many fields a truncated <c>PLAT</c> originally had.
    /// </summary>
    protected void WritePackagePlatform(BinaryWriter writer, PackagePlatform block)
    {
        var slots = Profile.PlatformFieldOrder switch
        {
            PlatformFieldOrder.PlatformNameRegionLanguage => PlatformNameRegionLanguageWriteSlots,
            PlatformFieldOrder.LanguageRegion => LanguageRegionWriteSlots,
            _ => throw new UnreachableException()
        };

        foreach (var extract in slots)
            writer.WriteEvilString(extract(block) ?? "");
    }
}
