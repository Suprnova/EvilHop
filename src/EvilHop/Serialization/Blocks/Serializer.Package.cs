using EvilHop.Blocks;
using EvilHop.Primitives;
using System.Diagnostics;

namespace EvilHop.Serialization;

public partial class Serializer
{
    /// <summary>
    /// Reads the fields of a <see cref="PackageVersion"/> (PVER) block.
    /// </summary>
    protected static void ReadPackageVersion(EndianReader reader, PackageVersion block, uint size)
    {
        block.SubVersion = reader.ReadUInt32();
        block.ClientVersion = (ClientVersion)reader.ReadUInt32();
        block.CompatVersion = reader.ReadUInt32();
    }

    /// <summary>
    /// Writes the fields of a <see cref="PackageVersion"/> (PVER) block.
    /// </summary>
    protected static void WritePackageVersion(EndianWriter writer, PackageVersion block)
    {
        writer.Write(block.SubVersion);
        writer.Write((uint)block.ClientVersion);
        writer.Write(block.CompatVersion);
    }

    /// <summary>
    /// Reads the fields of a <see cref="PackageFlags"/> (PFLG) block.
    /// </summary>
    protected static void ReadPackageFlags(EndianReader reader, PackageFlags block, uint size)
    {
        block.Flags = (PackFlags)reader.ReadUInt32();
    }

    /// <summary>
    /// Writes the fields of a <see cref="PackageFlags"/> (PFLG) block.
    /// </summary>
    protected static void WritePackageFlags(EndianWriter writer, PackageFlags block) =>
        writer.Write((uint)block.Flags);

    /// <summary>
    /// Reads the fields of a <see cref="PackageCount"/> (PCNT) block.
    /// </summary>
    protected static void ReadPackageCount(EndianReader reader, PackageCount block, uint size)
    {
        block.AssetCount = reader.ReadUInt32();
        block.LayerCount = reader.ReadUInt32();
        block.MaxAssetSize = reader.ReadUInt32();
        block.MaxLayerSize = reader.ReadUInt32();
        block.MaxXFormAssetSize = reader.ReadUInt32();
    }

    /// <summary>
    /// Writes the fields of a <see cref="PackageCount"/> (PCNT) block.
    /// </summary>
    protected static void WritePackageCount(EndianWriter writer, PackageCount block)
    {
        writer.Write(block.AssetCount);
        writer.Write(block.LayerCount);
        writer.Write(block.MaxAssetSize);
        writer.Write(block.MaxLayerSize);
        writer.Write(block.MaxXFormAssetSize);
    }

    /// <summary>
    /// Reads the fields of a <see cref="PackageCreated"/> (PCRT) block.
    /// </summary>
    protected static void ReadPackageCreated(EndianReader reader, PackageCreated block, uint size)
    {
        uint rawCreatedDate = reader.ReadUInt32();
        block.CreatedDate = DateTimeOffset.FromUnixTimeSeconds(rawCreatedDate);
        block.CreatedDateString = reader.ReadEvilString();
    }

    /// <summary>
    /// Writes the fields of a <see cref="PackageCreated"/> (PCRT) block.
    /// </summary>
    protected static void WritePackageCreated(EndianWriter writer, PackageCreated block)
    {
        writer.Write((uint)block.CreatedDate.ToUnixTimeSeconds());
        writer.WriteEvilString(block.CreatedDateString);
    }

    /// <summary>
    /// Reads the fields of a <see cref="PackageModified"/> (PMOD) block.
    /// </summary>
    protected static void ReadPackageModified(EndianReader reader, PackageModified block, uint size)
    {
        uint rawModifiedDate = reader.ReadUInt32();
        block.ModifiedDate = DateTimeOffset.FromUnixTimeSeconds(rawModifiedDate);
    }

    /// <summary>
    /// Writes the fields of a <see cref="PackageModified"/> (PMOD) block.
    /// </summary>
    protected static void WritePackageModified(EndianWriter writer, PackageModified block) =>
        writer.Write((uint)block.ModifiedDate.ToUnixTimeSeconds());

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
    /// Reads the fields of a <see cref="PackagePlatform"/> (PLAT) block.
    /// </summary>
    protected void ReadPackagePlatform(EndianReader reader, PackagePlatform block, uint size)
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
    /// Writes the fields of a <see cref="PackagePlatform"/> (PLAT) block.
    /// </summary>
    protected void WritePackagePlatform(EndianWriter writer, PackagePlatform block)
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
