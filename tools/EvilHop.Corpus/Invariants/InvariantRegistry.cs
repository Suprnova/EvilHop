namespace EvilHop.Corpus.Invariants;

/// <summary>
/// Every invariant checked over the corpus.
/// </summary>
internal static class InvariantRegistry
{
    /// <summary>Creates one fresh instance of every registered invariant.</summary>
    public static IReadOnlyList<IInvariant> CreateAll() =>
    [
        new StructuralInvariant(),
        new AssetIdMatchesNameHashInvariant(),
        new AssetIdsUniqueInvariant(),
        new AssetChecksumMatchesDataInvariant(),
        new AssetOffsetsInBoundsInvariant(),
        new LastAssetInLayerHasZeroPlusInvariant(),
        new PlusMatchesAlignmentInvariant(),
        new SourceFlagsExclusiveInvariant(),
        new FileNameSetWhenSourceFileInvariant(),
        new LayerAssetIdsResolveInvariant(),
        new LayerAssetCountsSumWithinTotalInvariant(),
        new PackageCountsMatchTreeInvariant(),
        new PackageMaxSizesMatchTreeInvariant(),
        new CreatedDateStringMatchesTimestampInvariant(),
        new CreatedDateStringTrailingWhitespaceInvariant(),
        new PaddingIsHomogeneousInvariant(),
        new PackageFlagsDefaultAlwaysPresentInvariant(),
        new PackageFlagsUpperWordInvariant()
    ];
}
