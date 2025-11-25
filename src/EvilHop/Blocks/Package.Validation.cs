using EvilHop.Blocks;

namespace EvilHop.Serialization.Validation;

public partial class V1Validator
{
    protected virtual IEnumerable<ValidationIssue> ValidatePackage(Package package)
    {
        if (package.GetChild<PackageVersion>() == null)
        {
            yield return new ValidationIssue
            {
                Severity = ValidationSeverity.Error,
                Message = $"Block Type {typeof(PackageVersion).Name} is missing from {typeof(Package).Name}.",
                Context = package
            };
        }
        if (package.GetChild<PackageFlags>() == null)
        {
            yield return new ValidationIssue
            {
                Severity = ValidationSeverity.Error,
                Message = $"Block Type {typeof(PackageFlags).Name} is missing from {typeof(Package).Name}.",
                Context = package
            };
        }
        if (package.GetChild<PackageCount>() == null)
        {
            yield return new ValidationIssue
            {
                Severity = ValidationSeverity.Error,
                Message = $"Block Type {typeof(PackageCount).Name} is missing from {typeof(Package).Name}.",
                Context = package
            };
        }
        if (package.GetChild<PackageCreated>() == null)
        {
            yield return new ValidationIssue
            {
                Severity = ValidationSeverity.Error,
                Message = $"Block Type {typeof(PackageCreated).Name} is missing from {typeof(Package).Name}.",
                Context = package
            };
        }
        if (package.GetChild<PackageModified>() == null)
        {
            yield return new ValidationIssue
            {
                Severity = ValidationSeverity.Error,
                Message = $"Block Type {typeof(PackageModified).Name} is missing from {typeof(Package).Name}.",
                Context = package
            };
        }
    }

    protected virtual IEnumerable<ValidationIssue> ValidatePackageVersion(PackageVersion version)
    {
        if (version.SubVersion != 0x00000002)
        {
            yield return new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Message = $"SubVersion '{version.SubVersion}' is unknown.",
                Context = version
            };
        }

        if (!Enum.IsDefined(version.ClientVersion))
        {
            yield return new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Message = $"ClientVersion '{(uint)version.ClientVersion}' is unknown.",
                Context = version
            };
        }
        else
            foreach (var issue in ValidateClientVersion(version))
                yield return issue;

        if (version.CompatVersion != 0x00000001)
        {
            yield return new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Message = $"CompatVersion '{version.CompatVersion}' is unknown.",
                Context = version
            };
        }
    }

    protected abstract IEnumerable<ValidationIssue> ValidateClientVersion(PackageVersion version);

    protected virtual IEnumerable<ValidationIssue> ValidatePackageFlags(PackageFlags flags)
    {
        // todo: this should check for undefined flags, combinations should be alright
        // or we could check for both i dunno
        if (!Enum.IsDefined(flags.Flags))
        {
            yield return new ValidationIssue
            {
                Severity = ValidationSeverity.None,
                Message = $"PackageFlags value {(uint)flags.Flags} is unknown, likely undocumented.",
                Context = flags
            };
        }
    }

    protected virtual IEnumerable<ValidationIssue> ValidatePackageCount(PackageCount count)
    {
        // most of this validation (ensuring counts line up) is done on the root (HipFile) level
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
        yield break;
    }
}
