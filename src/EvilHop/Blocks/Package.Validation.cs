using EvilHop.Blocks;

namespace EvilHop.Serialization.Validation;

public partial class V1Validator
{
    protected virtual IEnumerable<ValidationIssue> ValidatePackage(Package package)
    {
        yield break;
    }

    protected virtual IEnumerable<ValidationIssue> ValidatePackageVersion(PackageVersion version)
    {
        yield break;
    }

    protected virtual IEnumerable<ValidationIssue> ValidatePackageFlags(PackageFlags flags)
    {
        PackFlags allPackFlags = Enum.GetValues<PackFlags>().Aggregate((a, b) => a | b);
        if ((flags.Flags & ~allPackFlags) != 0)
            yield return ValidationIssue.UnknownValue(nameof(flags.Flags), (uint)flags.Flags, flags);
    }

    protected virtual IEnumerable<ValidationIssue> ValidatePackageCount(PackageCount count)
    {
        yield break;
    }

    protected virtual IEnumerable<ValidationIssue> ValidatePackageCreated(PackageCreated created)
    {
        yield break;
    }

    protected virtual IEnumerable<ValidationIssue> ValidatePackageModified(PackageModified modified)
    {
        yield break;
    }

    protected virtual IEnumerable<ValidationIssue> ValidatePackagePlatform(PackagePlatform platform)
    {
        yield break;
    }
}

public partial class V2Validator
{
    protected override IEnumerable<ValidationIssue> ValidatePackagePlatform(PackagePlatform platform)
    {
        if (platform.PlatformName is null)
            yield return ValidationIssue.MissingValue(nameof(platform.PlatformName), platform);

        // todo: add validation for game fields to game validators
    }
}
