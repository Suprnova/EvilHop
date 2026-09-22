using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// Evaluates a comparison against a named game variable upon receiving the Evaluate event, then
/// fires a different event depending on whether the comparison is true or false.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/COND">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed partial class ConditionalAsset() : BaseAsset(AssetType.Conditional, baseType: 0x1F)
{
    /// <summary>
    /// The value <see cref="Variable"/> is compared against, using <see cref="Operation"/>.
    /// </summary>
    public uint EvaluationAmount { get; set; }

    /// <summary>
    /// The named game variable to check.
    /// </summary>
    public VariableKind Variable { get; set; }

    /// <summary>
    /// How <see cref="Variable"/> is compared against <see cref="EvaluationAmount"/>.
    /// </summary>
    public Operational Operation { get; set; }

    /// <summary>
    /// The asset <see cref="Variable"/> is evaluated on, for variables that query another object's
    /// state (e.g. a counter's value, or an object's enabled/visible state). Not present in
    /// <see cref="GameVersion.N100F"/>.
    /// </summary>
    public AssetId TargetId { get; set; }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.Conditional"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.N100F,
        GameVersion.BFBB,
        GameVersion.TSSM,
        GameVersion.Incredibles,
        GameVersion.ROTU,
        GameVersion.Ratatouille,
    };

    internal static ConditionalAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new ConditionalAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        asset.EvaluationAmount = reader.ReadUInt32();
        asset.Variable = (VariableKind)reader.ReadUInt32();
        asset.Operation = (Operational)reader.ReadUInt32();

        if (profile.Game is not GameVersion.N100F)
            asset.TargetId = reader.ReadAssetId();

        for (var i = 0; i < asset.Physical.LinkCount; i++)
            asset.Links.Add(Link.Read(reader, profile));
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(ConditionalAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);

        writer.Write(asset.EvaluationAmount);
        writer.Write((uint)asset.Variable);
        writer.Write((uint)asset.Operation);

        if (profile.Game is not GameVersion.N100F)
            writer.Write(asset.TargetId);

        foreach (var link in asset.Links)
            Link.Write(link, writer, profile);
        writer.Write(asset.GetUnparsedTail());
    }

    /// <summary>
    /// The comparison or evaluation operation used by a <see cref="ConditionalAsset"/> to test its variables.
    /// </summary>
    public enum Operational : uint
    {
        /// <summary>The variable must equal the evaluation amount.</summary>
        EqualTo = 0,
        /// <summary>The variable must be greater than the evaluation amount.</summary>
        GreaterThan = 1,
        /// <summary>The variable must be less than the evaluation amount.</summary>
        LessThan = 2,
        /// <summary>The variable must be greater than or equal to the evaluation amount.</summary>
        GreaterThanOrEqualTo = 3,
        /// <summary>The variable must be less than or equal to the evaluation amount.</summary>
        LessThanOrEqualTo = 4,
        /// <summary>The variable must not equal the evaluation amount.</summary>
        NotEqualTo = 5,
    }
}

