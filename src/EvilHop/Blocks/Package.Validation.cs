using EvilHop.Blocks;
using System;
using System.Collections.Generic;
using System.Text;

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
        yield break;
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
