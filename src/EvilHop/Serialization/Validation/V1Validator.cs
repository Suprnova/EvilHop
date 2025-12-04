using EvilHop.Assets;
using EvilHop.Blocks;

namespace EvilHop.Serialization.Validation;

public abstract partial class V1Validator : IFormatValidator
{
    protected internal V1Validator()
    {
    }

    public IEnumerable<ValidationIssue> ValidateBlock(Block block)
    {
        foreach (var issue in ValidateBlockData(block)) yield return issue;

        foreach (var child in block.Children)
        {
            foreach (var issue in ValidateBlock(child)) yield return issue;
        }
    }

    public IEnumerable<ValidationIssue> ValidateArchive(HipFile hipFile)
    {
        foreach (var issue in ValidateBlockData(hipFile.HIPA)) yield return issue;
        foreach (var issue in ValidateBlockData(hipFile.Package)) yield return issue;
        foreach (var issue in ValidateBlockData(hipFile.Dictionary)) yield return issue;
        foreach (var issue in ValidateBlockData(hipFile.AssetStream)) yield return issue;
        // todo: validate PCNT fields against AHDR, LHDR, and DPAK

        // todo: validate AHDR against STRM (?)

        // todo: validate ADBG checksum against STRM

        // todo: validate no assetheaders overlap
    }

    protected IEnumerable<ValidationIssue> ValidateBlockData(Block block)
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
            Dictionary dictionary => ValidateDictionary(dictionary),
            AssetTable table => ValidateAssetTable(table),
            AssetInf inf => ValidateAssetInf(inf),
            AssetHeader header => ValidateAssetHeader(header),
            AssetDebug debug => ValidateAssetDebug(debug),
            LayerTable table => ValidateLayerTable(table),
            LayerInf inf => ValidateLayerInf(inf),
            LayerHeader header => ValidateLayerHeader(header),
            LayerDebug debug => ValidateLayerDebug(debug),
            AssetStream stream => ValidateAssetStream(stream),
            StreamHeader header => ValidateStreamHeader(header),
            StreamData data => ValidateStreamData(data),
            _ => throw new NotImplementedException()
        };
    }

    protected static IEnumerable<ValidationIssue> ValidateChildCount(Block block, int expectedCount)
    {
        if (block.Children.Count != expectedCount)
        {
            yield return new ValidationIssue
            {
                Severity = ValidationSeverity.Error,
                Message = $"Block type {block.GetType().Name} expects {expectedCount} children, found {block.Children.Count}.",
                Context = block
            };
        }
    }

    public IEnumerable<ValidationIssue> ValidateAsset(Asset asset)
    {
        throw new NotImplementedException();
    }
}

public partial class ScoobyPrototypeValidator : V1Validator
{
}

public partial class ScoobyValidator : V1Validator
{
}

public partial class BattleV1Validator : V1Validator
{
}
