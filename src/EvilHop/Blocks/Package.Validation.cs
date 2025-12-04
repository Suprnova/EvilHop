using EvilHop.Blocks;

namespace EvilHop.Serialization.Validation;

public partial class V1Validator
{
    protected virtual IEnumerable<ValidationIssue> ValidatePackage(Package package)
    {
        foreach (var issue in ValidateChildCount(package, GetExpectedChildCount(package)))
            yield return issue;

        if (package.GetChild<PackageVersion>() == null)
            yield return ValidationIssue.MissingChild<Package, PackageVersion>(package);

        if (package.GetChild<PackageFlags>() == null)
            yield return ValidationIssue.MissingChild<Package, PackageFlags>(package);

        if (package.GetChild<PackageCount>() == null)
            yield return ValidationIssue.MissingChild<Package, PackageCount>(package);

        if (package.GetChild<PackageCreated>() == null)
            yield return ValidationIssue.MissingChild<Package, PackageCreated>(package);

        if (package.GetChild<PackageModified>() == null)
            yield return ValidationIssue.MissingChild<Package, PackageModified>(package);
    }

    protected virtual IEnumerable<ValidationIssue> ValidatePackageVersion(PackageVersion version)
    {
        foreach (var issue in ValidateChildCount(version, GetExpectedChildCount(version)))
            yield return issue;

        if (version.SubVersion != 0x00000002)
            yield return ValidationIssue.UnknownValue(nameof(version.SubVersion), version.SubVersion, version);

        if (!Enum.IsDefined(version.ClientVersion))
            yield return ValidationIssue.UnknownValue(nameof(version.ClientVersion), (uint)version.ClientVersion, version);
        else
            foreach (var issue in ValidateClientVersion(version))
                yield return issue;

        if (version.CompatVersion != 0x00000001)
            yield return ValidationIssue.UnknownValue(nameof(version.CompatVersion), version.CompatVersion, version);
    }

    protected abstract IEnumerable<ValidationIssue> ValidateClientVersion(PackageVersion version);

    protected virtual IEnumerable<ValidationIssue> ValidatePackageFlags(PackageFlags flags)
    {
        foreach (var issue in ValidateChildCount(flags, GetExpectedChildCount(flags)))
            yield return issue;

        PackFlags allPackFlags = Enum.GetValues<PackFlags>().Aggregate((a, b) => a | b);
        if ((flags.Flags & ~allPackFlags) != 0)
            yield return ValidationIssue.UnknownValue(nameof(flags.Flags), (uint)flags.Flags, flags);
    }

    protected virtual IEnumerable<ValidationIssue> ValidatePackageCount(PackageCount count)
    {
        foreach (var issue in ValidateChildCount(count, GetExpectedChildCount(count)))
            yield return issue;

        // most of this validation (ensuring counts line up) is done on the root (HipFile) level
    }

    protected virtual IEnumerable<ValidationIssue> ValidatePackageCreated(PackageCreated created)
    {
        foreach (var issue in ValidateChildCount(created, GetExpectedChildCount(created)))
            yield return issue;
    }

    protected virtual IEnumerable<ValidationIssue> ValidatePackageModified(PackageModified modified)
    {
        foreach (var issue in ValidateChildCount(modified, GetExpectedChildCount(modified)))
            yield return issue;
    }

    protected virtual IEnumerable<ValidationIssue> ValidatePackagePlatform(PackagePlatform platform)
    {
        yield return new ValidationIssue
        {
            Severity = ValidationSeverity.Error,
            Message = $"Block Type {typeof(PackagePlatform).Name} is not valid in HIP Version 1.",
            Context = platform
        };
    }

    // helper methods to get expected child counts, can be overridden by derived validators
    protected virtual int GetExpectedChildCount(Package package) => 5;
    protected virtual int GetExpectedChildCount(PackageVersion version) => 0;
    protected virtual int GetExpectedChildCount(PackageFlags flags) => 0;
    protected virtual int GetExpectedChildCount(PackageCount count) => 0;
    protected virtual int GetExpectedChildCount(PackageCreated created) => 0;
    protected virtual int GetExpectedChildCount(PackageModified modified) => 0;
    protected virtual int GetExpectedChildCount(PackagePlatform platform) => 0;
}

public partial class ScoobyPrototypeValidator
{
    protected override IEnumerable<ValidationIssue> ValidateClientVersion(PackageVersion version)
    {
        if (version.ClientVersion != ClientVersion.N100FPrototype)
        {
            yield return new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Message = $"ClientVersion '{version.ClientVersion}' is not valid for Scooby Prototype, expected '{ClientVersion.N100FPrototype}'",
                Context = version
            };
        }
    }
}

public partial class ScoobyValidator
{
    protected override IEnumerable<ValidationIssue> ValidateClientVersion(PackageVersion version)
    {
        if (version.ClientVersion != ClientVersion.N100FRelease)
        {
            yield return new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Message = $"ClientVersion '{version.ClientVersion}' is not valid for Scooby, expected '{ClientVersion.N100FRelease}'.",
                Context = version
            };
        }
    }
}

public partial class BattleV1Validator
{
    protected override IEnumerable<ValidationIssue> ValidateClientVersion(PackageVersion version)
    {
        if (version.ClientVersion != ClientVersion.Default)
        {
            yield return new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Message = $"ClientVersion '{version.ClientVersion}' is not valid for Battle V1, expected '{ClientVersion.Default}'.",
                Context = version
            };
        }
    }
}

public partial class V2Validator
{
    protected override IEnumerable<ValidationIssue> ValidatePackage(Package package)
    {
        foreach (var issue in base.ValidatePackage(package))
            yield return issue;

        if (package.GetChild<PackagePlatform>() == null)
            yield return ValidationIssue.MissingChild<Package, PackagePlatform>(package);
    }

    // add Platform block to expected child count
    protected override int GetExpectedChildCount(Package package) => base.GetExpectedChildCount(package) + 1;

    protected override IEnumerable<ValidationIssue> ValidateClientVersion(PackageVersion version)
    {
        if (version.ClientVersion != ClientVersion.Default)
        {
            yield return new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Message = $"ClientVersion '{version.ClientVersion}' is not valid for non-Scooby games, expected '{ClientVersion.Default}'.",
                Context = version
            };
        }
    }

    protected override IEnumerable<ValidationIssue> ValidatePackagePlatform(PackagePlatform platform)
    {
        foreach (var issue in ValidateChildCount(platform, GetExpectedChildCount(platform)))
            yield return issue;

        if (platform.PlatformName is null)
            yield return ValidationIssue.MissingValue(nameof(platform.PlatformName), platform);

        // todo: add validation for game fields to game validators
    }
}

public partial class V3Validator
{
    protected override IEnumerable<ValidationIssue> ValidatePackagePlatform(PackagePlatform platform)
    {
        foreach (var issue in ValidateChildCount(platform, GetExpectedChildCount(platform)))
            yield return issue;

        if (platform.PlatformName is not null)
        {
            yield return new ValidationIssue
            {
                Severity = ValidationSeverity.Error,
                Message = $"Unexpected non-null value '{platform.PlatformName}' in field '{nameof(platform.PlatformName)}'.",
                Context = platform
            };
        }
    }
}
