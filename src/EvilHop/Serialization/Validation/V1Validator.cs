using EvilHop.Blocks;

namespace EvilHop.Serialization.Validation;

public partial class V1Validator : IFormatValidator
{
    public IEnumerable<ValidationIssue> Validate(Block block)
    {
        foreach (var issue in ValidateBlockData(block)) yield return issue;

        foreach (var child in block.Children)
        {
            foreach (var issue in ValidateBlockData(child)) yield return issue;
        }
    }

    public IEnumerable<ValidationIssue> ValidateArchive(HipFile hipFile)
    {
        foreach (var issue in ValidateBlockData(hipFile.HIPA)) yield return issue;
        foreach (var issue in ValidateBlockData(hipFile.Package)) yield return issue;
        //foreach (var issue in ValidateBlockData(hipFile.Dictionary)) yield return issue;
        //foreach (var issue in ValidateBlockData(hipFile.AssetDataStream)) yield return issue;
        // todo: perform cross-referential validation here, probably call to protected virtual methods for each one
        // i.e. validate PackageCount against AHDR, LHDR, and DPAK
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
