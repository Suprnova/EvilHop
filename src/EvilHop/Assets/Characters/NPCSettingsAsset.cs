using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;

namespace EvilHop.Assets;

/// <summary>
/// A named set of NPC AI parameters, in the same format as <see cref="AssetType.ModelInfo"/>'s own
/// parameters, shared between NPCs that reference it.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/NPCS">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class NPCSettingsAsset() : Asset(AssetType.NPCSettings), IPhysicalNPCSettingsAsset
{
    /// <summary>Named parameters this asset's occupant NPC AI code reads by hash.</summary>
    public Collection<ModelInfoParameter> Parameters { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalNPCSettingsAsset Physical => this;

    private uint? _overriddenParameterCount;
    uint IPhysicalNPCSettingsAsset.ParameterCount
    {
        get => _overriddenParameterCount ?? (uint)Parameters.Count;
        set => _overriddenParameterCount = value == (uint)Parameters.Count ? null : value;
    }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.NPCSettings"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.Incredibles,
        GameVersion.ROTU,
    };

    internal static NPCSettingsAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new NPCSettingsAsset();
        AssetFields.Populate(asset, header, debug);

        uint parameterCount = reader.ReadUInt32();
        for (int i = 0; i < parameterCount; i++)
            asset.Parameters.Add(ModelInfoParameter.Read(reader, profile));

        asset.Physical.ParameterCount = (uint)asset.Parameters.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(NPCSettingsAsset asset, EndianWriter writer, FormatProfile profile)
    {
        writer.Write(asset.Physical.ParameterCount);

        foreach (var parameter in asset.Parameters)
            ModelInfoParameter.Write(parameter, writer, profile);

        writer.Write(asset.GetUnparsedTail());
    }
}

/// <summary>
/// An explicit interface used to interact with <see cref="NPCSettingsAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalNPCSettingsAsset : IPhysicalAsset
{
    /// <summary>
    /// The number of <see cref="NPCSettingsAsset.Parameters"/> stored for this asset, read directly
    /// from its leading count field.
    /// </summary>
    /// <remarks>
    /// When disagreements with <see cref="NPCSettingsAsset.Parameters"/>.Count exist, this field wins
    /// during serialization.
    /// </remarks>
    uint ParameterCount { get; set; }
}
