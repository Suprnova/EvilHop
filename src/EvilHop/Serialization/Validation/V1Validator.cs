using EvilHop.Assets;
using EvilHop.Blocks;
using EvilHop.Serialization;
using System.Reflection;

namespace EvilHop.Serialization.Validation;

public abstract partial class V1Validator : IFormatValidator
{
    protected internal abstract FileFormatVersion Version { get; }
    protected internal V1Validator()
    {
    }

    public IEnumerable<ValidationIssue> ValidateBlock(Block block)
    {
        foreach (var issue in ValidateBlockAttributes(block)) yield return issue;

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

    protected IEnumerable<ValidationIssue> ValidateBlockAttributes(Block block)
    {
        var type = block.GetType();
        var attributes = ValidationAttributesCache.GetAttributes(type);

        var applicableChildCounts = attributes.ChildCounts.Where(attr => attr.AppliesTo(Version)).ToList();
        foreach (var issue in ValidateChildCountAttributes(block, applicableChildCounts))
            yield return issue;

        var applicableRequiredChildren = attributes.RequiredChildren.Where(attr => attr.AppliesTo(Version)).ToList();
        foreach (var issue in ValidateRequiredChildAttributes(block, applicableRequiredChildren))
            yield return issue;

        foreach (var issue in ValidateFieldAttributes(block, attributes.Fields))
            yield return issue;
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

    protected IEnumerable<ValidationIssue> ValidateFieldAttributes(object obj, IReadOnlyDictionary<PropertyInfo, FieldValidationAttributes> attributes)
    {
        foreach (var kvp in attributes)
        {
            var property = kvp.Key;
            var fieldAttributes = kvp.Value;
            var value = property.GetValue(obj);

            var applicableExpectedValues = fieldAttributes.ExpectedValues.Where(attr => attr.AppliesTo(Version)).ToList();
            foreach (var issue in ValidateExpectedValueAttributes(property, value, applicableExpectedValues, obj))
                yield return issue;

            var applicableAllowedValues = fieldAttributes.AllowedValues.Where(attr => attr.AppliesTo(Version)).ToList();
            foreach (var issue in ValidateAllowedValuesAttributes(property, value, applicableAllowedValues, obj))
                yield return issue;

            // VersionRange is special: if attributes exist but none apply, report an issue
            if (fieldAttributes.VersionRanges.Count > 0)
            {
                var applicableVersionRanges = fieldAttributes.VersionRanges.Where(attr => attr.AppliesTo(Version)).ToList();
                if (applicableVersionRanges.Count == 0)
                {
                    var range = fieldAttributes.VersionRanges.First();
                    if (obj is Block block)
                    {
                        yield return new ValidationIssue
                        {
                            Severity = range.Severity,
                            Message = $"{property.Name} in block type {block.GetType().Name} is not valid for version {Version}.",
                            Context = block
                        };
                    }
                    else
                    {
                        yield return new ValidationIssue
                        {
                            Severity = range.Severity,
                            Message = $"{property.Name} is not valid for version {Version}.",
                            Context = null
                        };
                    }
                }
            }
        }
    }

    protected IEnumerable<ValidationIssue> ValidateChildCountAttributes(Block block, IReadOnlyList<ExpectedChildCountAttribute> applicableAttributes)
    {
        if (applicableAttributes.Count == 0) yield break;

        var expected = applicableAttributes.First();

        if (block.Children.Count != expected.Count)
        {
            yield return new ValidationIssue
            {
                Severity = expected.Severity,
                Message = $"Block type {block.GetType().Name} expects {expected.Count} children, found {block.Children.Count}.",
                Context = block
            };
        }
    }

    protected IEnumerable<ValidationIssue> ValidateRequiredChildAttributes(Block block, IReadOnlyList<RequiredChildAttribute> applicableAttributes)
    {
        if (applicableAttributes.Count == 0) yield break;

        foreach (var attr in applicableAttributes)
        {
            var hasChild = block.Children.Any(child => attr.ChildType.IsInstanceOfType(child));
            if (!hasChild)
            {
                yield return new ValidationIssue
                {
                    Severity = attr.Severity,
                    Message = $"Block type {block.GetType().Name} is missing required child of type {attr.ChildType.Name}.",
                    Context = block
                };
            }
        }
    }

    protected IEnumerable<ValidationIssue> ValidateExpectedValueAttributes(PropertyInfo property, object? value, IReadOnlyList<ExpectedValueAttribute> applicableAttributes, object? context)
    {
        if (applicableAttributes.Count == 0) yield break;

        var expected = applicableAttributes.First();

        // Use proper equality comparison that handles boxing
        bool valuesMatch = AreValuesEqual(value, expected.Value);

        if (!valuesMatch)
        {
            if (context is Block block)
            {
                yield return new ValidationIssue
                {
                    Severity = expected.Severity,
                    Message = $"{property.Name} in block type {block.GetType().Name} has unknown value {value ?? "null"} (expected {expected.Value}).",
                    Context = block
                };
            }
            else
            {
                yield return new ValidationIssue
                {
                    Severity = expected.Severity,
                    Message = $"{property.Name} has unexpected value {value ?? "null"} (expected {expected.Value}).",
                    Context = null
                };
            }
        }
    }

    // todo: there's gotta be a better way to do this man
    private static bool AreValuesEqual(object? value1, object? value2)
    {
        if (value1 == null && value2 == null) return true;
        if (value1 == null || value2 == null) return false;

        Type type1 = value1.GetType();
        Type type2 = value2.GetType();

        // If types are the same, use Equals
        if (type1 == type2)
            return value1.Equals(value2);

        // For numeric types, convert to common type and compare
        if (IsNumericType(type1) && IsNumericType(type2))
        {
            try
            {
                decimal d1 = Convert.ToDecimal(value1);
                decimal d2 = Convert.ToDecimal(value2);
                return d1 == d2;
            }
            catch
            {
                return false;
            }
        }

        // For enums, compare their underlying values
        if (type1.IsEnum && type2.IsEnum)
        {
            return Convert.ToInt64(value1) == Convert.ToInt64(value2);
        }

        // Fallback to string comparison for other types
        return value1.ToString() == value2.ToString();
    }

    private static bool IsNumericType(Type type)
    {
        return type == typeof(sbyte) || type == typeof(byte) ||
               type == typeof(short) || type == typeof(ushort) ||
               type == typeof(int) || type == typeof(uint) ||
               type == typeof(long) || type == typeof(ulong) ||
               type == typeof(float) || type == typeof(double) ||
               type == typeof(decimal);
    }

    protected IEnumerable<ValidationIssue> ValidateAllowedValuesAttributes(PropertyInfo property, object? value, IReadOnlyList<AllowedValuesAttribute> applicableAttributes, object? context)
    {
        if (applicableAttributes.Count == 0) yield break;

        var allowed = applicableAttributes.First();

        if (!allowed.Allowed.Contains(value))
        {
            if (context is Block block)
            {
                yield return new ValidationIssue
                {
                    Severity = allowed.Severity,
                    Message = $"{property.Name} in block type {block.GetType().Name} has value {value ?? "null"} which is not in the allowed values [{string.Join(", ", allowed.Allowed)}].",
                    Context = block
                };
            }
            else
            {
                yield return new ValidationIssue
                {
                    Severity = allowed.Severity,
                    Message = $"{property.Name} has value {value ?? "null"} which is not in the allowed values [{string.Join(", ", allowed.Allowed)}].",
                    Context = null
                };
            }
        }
    }

    public IEnumerable<ValidationIssue> ValidateAsset(Asset asset)
    {
        throw new NotImplementedException();
    }
}

public partial class ScoobyPrototypeValidator : V1Validator
{
    protected internal override FileFormatVersion Version => FileFormatVersion.ScoobyPrototype;
}

public partial class ScoobyValidator : V1Validator
{
    protected internal override FileFormatVersion Version => FileFormatVersion.Scooby;
}

public partial class BattleV1Validator : V1Validator
{
    protected internal override FileFormatVersion Version => FileFormatVersion.BattleV1;
}
