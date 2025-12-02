using EvilHop.Blocks;

namespace EvilHop.Serialization.Validation;

public partial class V1Validator
{
    protected virtual IEnumerable<ValidationIssue> ValidatePackage(Package package, int expectedChildrenCount = 5)
    {
        foreach (var issue in ValidateChildCount(package, expectedChildrenCount))
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

    protected virtual IEnumerable<ValidationIssue> ValidatePackageVersion(PackageVersion version, int expectedChildrenCount = 0)
    {
        foreach (var issue in ValidateChildCount(version, expectedChildrenCount))
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

    protected virtual IEnumerable<ValidationIssue> ValidatePackageFlags(PackageFlags flags, int expectedChildrenCount = 0)
    {
        foreach (var issue in ValidateChildCount(flags, expectedChildrenCount))
            yield return issue;

        // todo: this should check for undefined flags, combinations should be alright
        // or we could check for both i dunno
        if (!Enum.IsDefined(flags.Flags))
            yield return ValidationIssue.UnknownValue(nameof(flags.Flags), (uint)flags.Flags, flags);
    }

    protected virtual IEnumerable<ValidationIssue> ValidatePackageCount(PackageCount count, int expectedChildrenCount = 0)
    {
        foreach (var issue in ValidateChildCount(count, expectedChildrenCount))
            yield return issue;

        // most of this validation (ensuring counts line up) is done on the root (HipFile) level
    }

    protected virtual IEnumerable<ValidationIssue> ValidatePackageCreated(PackageCreated created, int expectedChildrenCount = 0)
    {
        foreach (var issue in ValidateChildCount(created, expectedChildrenCount))
            yield return issue;
    }

    protected virtual IEnumerable<ValidationIssue> ValidatePackageModified(PackageModified modified, int expectedChildrenCount = 0)
    {
        foreach (var issue in ValidateChildCount(modified, expectedChildrenCount))
            yield return issue;
    }

    protected virtual IEnumerable<ValidationIssue> ValidatePackagePlatform(PackagePlatform platform, int expectedChildrenCount = 0)
    {
        yield return new ValidationIssue
        {
            Severity = ValidationSeverity.Error,
            Message = $"Block Type {typeof(PackagePlatform).Name} is not valid in HIP Version 1.",
            Context = platform
        };
    }
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

public partial class V2Validator
{
    protected override IEnumerable<ValidationIssue> ValidatePackage(Package package, int expectedChildCount = 6)
    {
        foreach (var issue in base.ValidatePackage(package, expectedChildCount))
            yield return issue;

        if (package.GetChild<PackagePlatform>() == null)
            yield return ValidationIssue.MissingChild<Package, PackagePlatform>(package);
    }

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

    protected override IEnumerable<ValidationIssue> ValidatePackagePlatform(PackagePlatform platform, int expectedChildrenCount = 0)
    {
        foreach (var issue in ValidateChildCount(platform, expectedChildrenCount))
            yield return issue;
    }
}
