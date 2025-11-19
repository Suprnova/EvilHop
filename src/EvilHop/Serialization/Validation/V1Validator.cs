using EvilHop.Blocks;
using System;
using System.Collections.Generic;
using System.Text;

namespace EvilHop.Serialization.Validation;

public partial class V1Validator : IFormatValidator
{
    public IEnumerable<ValidationIssue> Validate(Block block)
    {
        foreach (var issue in ValidateBlockData(block)) yield return issue;
    }

    protected virtual IEnumerable<ValidationIssue> ValidateBlockData(Block block)
    {
        return block switch
        {
            HIPA hipa => ValidateHIPA(hipa),
            Package package => ValidatePackage(package),
            PackageVersion version => ValidatePackageVersion(version),
            PackageFlags flags => ValidatePackageFlags(flags),
            PackageCount count => ValidatePackageCount(count),
            PackageCreated created => ValidatePackageCreated(created),
            PackageModified modified => ValidatePackageModified(modified),
            PackagePlatform platform => ValidatePackagePlatform(platform),
            _ => throw new NotImplementedException()
        };
    }
}
