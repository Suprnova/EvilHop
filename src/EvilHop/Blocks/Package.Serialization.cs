using EvilHop.Blocks;
using EvilHop.Primitives;

namespace EvilHop.Serialization.Serializers;

public abstract partial class V1Serializer
{
    protected virtual Package InitPackage()
    {
        return new Package(NewBlock<PackageVersion>(), NewBlock<PackageFlags>(), NewBlock<PackageCount>(), NewBlock<PackageCreated>(), NewBlock<PackageModified>());
    }

    protected abstract PackageVersion InitPackageVersion();

    protected virtual PackageVersion ReadPackageVersion(BinaryReader reader)
    {
        return new PackageVersion(
            reader.ReadEvilInt(),
            (ClientVersion)reader.ReadEvilInt(),
            reader.ReadEvilInt()
            );
    }

    protected virtual void WritePackageVersion(BinaryWriter writer, PackageVersion version)
    {
        writer.WriteEvilInt(version.SubVersion);
        writer.WriteEvilInt((uint)version.ClientVersion);
        writer.WriteEvilInt(version.CompatVersion);
    }

    protected virtual PackageFlags InitPackageFlags()
    {
        return new PackageFlags(PackFlags.Default);
    }

    protected virtual PackageFlags ReadPackageFlags(BinaryReader reader)
    {
        return new PackageFlags(
            (PackFlags)reader.ReadEvilInt()
        );
    }

    protected virtual void WritePackageFlags(BinaryWriter writer, PackageFlags flags)
    {
        writer.WriteEvilInt((uint)flags.Flags);
    }

    protected virtual PackageCount ReadPackageCount(BinaryReader reader)
    {
        return new PackageCount(
            reader.ReadEvilInt(),
            reader.ReadEvilInt(),
            reader.ReadEvilInt(),
            reader.ReadEvilInt(),
            reader.ReadEvilInt()
        );
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
        return new PackageCreated(
            DateTime.UnixEpoch.AddSeconds(reader.ReadEvilInt()),
            reader.ReadEvilString()
        );
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
        return new PackageModified(
            DateTime.UnixEpoch.AddSeconds(reader.ReadEvilInt())
        );
    }

    protected virtual void WritePackageModified(BinaryWriter writer, PackageModified modified)
    {
        // todo: implement timezone (UTC-7)
        writer.WriteEvilInt(Convert.ToUInt32((modified.ModifiedDate - DateTime.UnixEpoch).TotalSeconds));
    }
}

public partial class V2Serializer
{
    protected override PackageVersion InitPackageVersion() => new(ClientVersion.Default);

    protected override void WritePackageCreated(BinaryWriter writer, PackageCreated created)
    {
        // todo: implement timezone (UTC-7)
        writer.WriteEvilInt(Convert.ToUInt32((created.CreatedDate - DateTime.UnixEpoch).TotalSeconds));
        writer.WriteEvilString(created.CreatedDateString);
    }

    protected abstract PackagePlatform InitPackagePlatform();

    protected virtual PackagePlatform ReadPackagePlatform(BinaryReader reader)
    {
        return new PackagePlatform(
            reader.ReadEvilString(),
            reader.ReadEvilString(),
            reader.ReadEvilString(),
            reader.ReadEvilString(),
            reader.ReadEvilString()
        );
    }

    protected virtual void WritePackagePlatform(BinaryWriter writer, PackagePlatform platform)
    {
        writer.WriteEvilString(platform.PlatformID);
        // will produce an invalid block in v2 if null, but that's by design. user explicitly chose to do this,
        // validation will warn them but i won't stop them.
        if (platform.PlatformName is not null) writer.WriteEvilString(platform.PlatformName);
        writer.WriteEvilString(platform.Region);
        writer.WriteEvilString(platform.Language);
        writer.WriteEvilString(platform.GameName);
    }
}

public partial class V3Serializer
{
    protected override PackagePlatform ReadPackagePlatform(BinaryReader reader)
    {
        return new PackagePlatform
        {
            PlatformID = reader.ReadEvilString(),
            Language = reader.ReadEvilString(),
            Region = reader.ReadEvilString(),
            GameName = reader.ReadEvilString(),
            PlatformName = null
        };
    }

    protected override void WritePackagePlatform(BinaryWriter writer, PackagePlatform platform)
    {
        writer.WriteEvilString(platform.PlatformID);
        // will be invalid in v3 if not null by design
        if (platform.PlatformName is not null) writer.WriteEvilString(platform.PlatformName);
        writer.WriteEvilString(platform.Language);
        writer.WriteEvilString(platform.Region);
        writer.WriteEvilString(platform.GameName);
    }
}
