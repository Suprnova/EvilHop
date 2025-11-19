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
        if (!Enum.IsDefined(version.SubVer))
        {
            yield return new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Message = $"SubVersion {(int)version.SubVer} is unknown.",
                Context = version
            };
        }
        if (!Enum.IsDefined(version.ClientVer))
        {
            yield return new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Message = $"ClientVersion {(int)version.ClientVer} is unknown.",
                Context = version
            };
        }
        if (!Enum.IsDefined(version.CompatVer))
        {
            yield return new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Message = $"CompatVersion {(int)version.CompatVer} is unknown.",
                Context = version
            };
        }
    }

    protected virtual IEnumerable<ValidationIssue> ValidatePackageFlags(PackageFlags flags)
    {
        if (!Enum.IsDefined(flags.Flags)) {
            yield return new ValidationIssue
            {
                Severity = ValidationSeverity.None,
                Message = $"PackageFlags value {(int)flags.Flags} is unknown, likely undocumented.",
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
        // todo: maybe ensure CreatedDate.ToString() == CreatedDateString() ?
        yield break;
    }

    protected virtual IEnumerable<ValidationIssue> ValidatePackageModified(PackageModified modified)
    {
        yield break;
    }

    protected virtual IEnumerable<ValidationIssue> ValidatePackagePlatform(PackagePlatform platform)
    {
        // todo: should be an error of None? we should still let this happen, since the serializer can just not write it
        yield return new ValidationIssue
        {
            Severity = ValidationSeverity.Error,
            Message = $"Block Type {typeof(PackagePlatform).Name} is not valid in HIP Version 1.",
            Context = platform
        };
    }
}

public partial class V2Validator
{
    protected virtual IEnumerable<ValidationIssue> ValidatePackagePlatform(PackagePlatform platform)
    {
        yield break;
    }
}
