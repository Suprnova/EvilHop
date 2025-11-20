using EvilHop.Blocks;
using EvilHop.Primitives;

namespace EvilHop.Serialization;

public partial class V1Serializer
{
    protected virtual PackageVersion ReadPackageVersion(BinaryReader reader)
    {
        return new PackageVersion
        {
            SubVer = (PackageVersion.SubVersion)reader.ReadEvilInt(),
            ClientVer = (PackageVersion.ClientVersion)reader.ReadEvilInt(),
            CompatVer = (PackageVersion.CompatVersion)reader.ReadEvilInt()
        };
    }

    protected virtual void WritePackageVersion(BinaryWriter writer, PackageVersion version)
    {
        writer.WriteEvilInt((uint)version.SubVer);
        writer.WriteEvilInt((uint)version.ClientVer);
        writer.WriteEvilInt((uint)version.CompatVer);
    }

    protected virtual PackageFlags ReadPackageFlags(BinaryReader reader)
    {
        return new PackageFlags
        {
            Flags = (PackageFlags.PFLG_Flags)reader.ReadEvilInt()
        };
    }

    protected virtual void WritePackageFlags(BinaryWriter writer, PackageFlags flags)
    {
        writer.WriteEvilInt((uint)flags.Flags);
    }

    protected virtual PackageCount ReadPackageCount(BinaryReader reader)
    {
        return new PackageCount
        {
            AssetCount = reader.ReadEvilInt(),
            LayerCount = reader.ReadEvilInt(),
            MaxAssetSize = reader.ReadEvilInt(),
            MaxLayerSize = reader.ReadEvilInt(),
            MaxXFormAssetSize = reader.ReadEvilInt()
        };
    }

    protected virtual void WritePackageCount(BinaryWriter writer, PackageCount count)
    {
        writer.WriteEvilInt(count.AssetCount);
        writer.WriteEvilInt(count.LayerCount);
        writer.WriteEvilInt(count.MaxAssetSize);
        writer.WriteEvilInt(count.MaxLayerSize);
        writer.WriteEvilInt(count.MaxXFormAssetSize);
    }

    protected virtual PackageCreated ReadPackageCreated(BinaryReader reader)
    {
        return new PackageCreated
        {
            CreatedDate = DateTime.UnixEpoch.AddSeconds(reader.ReadEvilInt()),
            CreatedDateString = reader.ReadEvilString()
        };
    }

    protected virtual void WritePackageCreated(BinaryWriter writer, PackageCreated created)
    {
        // todo: implement timezone (UTC-7)
        // todo: implement fallback for Y2106 lol
        writer.WriteEvilInt(Convert.ToUInt32((created.CreatedDate - DateTime.UnixEpoch).TotalSeconds));
        writer.WriteEvilString(created.CreatedDateString.Last() == '\n' ? created.CreatedDateString : created.CreatedDateString + '\n');
    }

    protected virtual PackageModified ReadPackageModified(BinaryReader reader)
    {
        return new PackageModified
        {
            ModifiedDate = DateTime.UnixEpoch.AddSeconds(reader.ReadEvilInt())
        };
    }

    protected virtual void WritePackageModified(BinaryWriter writer, PackageModified modified)
    {
        // todo: implement timezone (UTC-7)
        writer.WriteEvilInt(Convert.ToUInt32((modified.ModifiedDate - DateTime.UnixEpoch).TotalSeconds));
    }

    protected virtual PackagePlatform ReadPackagePlatform(BinaryReader reader) => throw new InvalidOperationException();
    protected virtual void WritePackagePlatform(BinaryWriter writer, PackagePlatform platform) => throw new InvalidOperationException();
}

public partial class V2Serializer
{
    protected override void WritePackageCreated(BinaryWriter writer, PackageCreated created)
    {
        // todo: implement timezone (UTC-7)
        writer.WriteEvilInt(Convert.ToUInt32((created.CreatedDate - DateTime.UnixEpoch).TotalSeconds));
        writer.WriteEvilString(created.CreatedDateString);
    }

    protected override void WritePackagePlatform(BinaryWriter writer, PackagePlatform platform)
    {
        writer.WriteEvilString(platform.PlatformID);
        // todo: should not be null in V1
        writer.WriteEvilString(platform.PlatformName ?? "");
        writer.WriteEvilString(platform.Region);
        writer.WriteEvilString(platform.Language);
        writer.WriteEvilString(platform.GameName);
    }
}