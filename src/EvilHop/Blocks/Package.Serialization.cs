using EvilHop.Blocks;
using EvilHop.Primitives;

namespace EvilHop.Serialization;

public partial class SerializerV1
{
    /// <summary>
    /// Reads the fields of a <see cref="PackageVersion"/> (PVER) block.
    /// </summary>
    protected virtual void ReadPackageVersion(BinaryReader reader, PackageVersion block, uint size)
    {
        block.SubVersion = reader.ReadEvilInt();
        block.ClientVersion = (ClientVersion)reader.ReadEvilInt();
        block.CompatVersion = reader.ReadEvilInt();
    }

    /// <summary>
    /// Reads the fields of a <see cref="PackageFlags"/> (PFLG) block.
    /// </summary>
    protected virtual void ReadPackageFlags(BinaryReader reader, PackageFlags block, uint size)
    {
        block.Flags = (PackFlags)reader.ReadEvilInt();
    }

    /// <summary>
    /// Reads the fields of a <see cref="PackageCount"/> (PCNT) block.
    /// </summary>
    protected virtual void ReadPackageCount(BinaryReader reader, PackageCount block, uint size)
    {
        block.AssetCount = reader.ReadEvilInt();
        block.LayerCount = reader.ReadEvilInt();
        block.MaxAssetSize = reader.ReadEvilInt();
        block.MaxLayerSize = reader.ReadEvilInt();
        block.MaxXFormAssetSize = reader.ReadEvilInt();
    }

    /// <summary>
    /// Reads the fields of a <see cref="PackageCreated"/> (PCRT) block. <see cref="PackageCreated.CreatedDate"/>
    /// is converted from a raw UTC Unix timestamp to a UTC-7:00 (Pacific Time) display offset.
    /// </summary>
    protected virtual void ReadPackageCreated(BinaryReader reader, PackageCreated block, uint size)
    {
        uint rawCreatedDate = reader.ReadEvilInt();
        block.CreatedDate = DateTimeOffset.FromUnixTimeSeconds(rawCreatedDate).ToOffset(TimeSpan.FromHours(-7));
        block.CreatedDateString = reader.ReadEvilString();
    }

    /// <summary>
    /// Reads the fields of a <see cref="PackageModified"/> (PMOD) block. <see cref="PackageModified.ModifiedDate"/>
    /// is converted from a raw UTC Unix timestamp to a UTC-7:00 (Pacific Time) display offset.
    /// </summary>
    protected virtual void ReadPackageModified(BinaryReader reader, PackageModified block, uint size)
    {
        uint rawModifiedDate = reader.ReadEvilInt();
        block.ModifiedDate = DateTimeOffset.FromUnixTimeSeconds(rawModifiedDate).ToOffset(TimeSpan.FromHours(-7));
    }
}
