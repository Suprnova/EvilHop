namespace EvilHop.Assets;

/// <summary>
/// Marks an <see cref="Common.AssetId"/> member that holds the asset's own ID, stored again in its
/// data, rather than a reference to another asset.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
internal sealed class OwnAssetIdAttribute : Attribute;
