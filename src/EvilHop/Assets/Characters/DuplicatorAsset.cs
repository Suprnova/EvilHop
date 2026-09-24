using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="BaseAsset"/> that spawns copies of an embedded <see cref="Villain"/> over time, drawn
/// from a fixed pool of <see cref="MaxInGame"/> clones.
/// </summary>
/// <remarks>
/// <para>
/// Fires <b>NPCSpawn</b> for each clone it spawns, <b>SpawnedNPCKilled</b> or
/// <b>SpawnedNPCNoHealth</b> as each one dies or loses its health, and <b>SpawnedNPCsAllKilled</b> or
/// <b>SpawnedNPCsAllNoHealth</b> once it has spawned <see cref="MaxToSpawn"/> clones and none remain.
/// </para>
/// <para>
/// <b>DuplicatorActivate</b> and <b>DuplicatorDeactivate</b> resume and pause spawning;
/// <b>JumpOnSpawn</b> and <b>RandomJumpOnSpawn</b> send each new clone to a movepoint. Every other
/// event is forwarded to every living clone.
/// </para>
/// </remarks>
public sealed partial class DuplicatorAsset() : BaseAsset(AssetType.Duplicator, baseType: 0x42), Physical.IDuplicatorAsset
{
    /// <summary>
    /// The number of clones spawned at once whenever the duplicator resets.
    /// </summary>
    public ushort InitialSpawn { get; set; }

    /// <summary>
    /// The number of clones in the duplicator's pool, and so the most that can be alive at once.
    /// </summary>
    public ushort MaxInGame { get; set; }

    /// <summary>
    /// The total number of clones the duplicator spawns before it stops, or 0 to spawn indefinitely.
    /// </summary>
    public ushort MaxToSpawn { get; set; }

    /// <summary>
    /// The time, in seconds, between spawns.
    /// </summary>
    public float SpawnRate { get; set; }

    /// <summary>
    /// The villain every clone is a copy of.
    /// </summary>
    public Villain Template { get; set; } = new();

    /// <inheritdoc cref="Asset.Physical"/>
    public override Physical.IDuplicatorAsset Physical => this;

    private Avatar _avatar = new();
    Avatar Physical.IDuplicatorAsset.Avatar { get => _avatar; set => _avatar = value; }

    private AssetId? _overriddenTemplateBaseId;
    AssetId Physical.IDuplicatorAsset.TemplateBaseId
    {
        get => _overriddenTemplateBaseId ?? Physical.BaseId;
        set => _overriddenTemplateBaseId = value == Physical.BaseId ? null : value;
    }

    private byte _templateBaseType = 0x2B;
    byte Physical.IDuplicatorAsset.TemplateBaseType { get => _templateBaseType; set => _templateBaseType = value; }

    private byte? _overriddenTemplateLinkCount;
    byte Physical.IDuplicatorAsset.TemplateLinkCount
    {
        get => _overriddenTemplateLinkCount ?? Physical.LinkCount;
        set => _overriddenTemplateLinkCount = value == Physical.LinkCount ? null : value;
    }

    private BaseAssetFlags? _overriddenTemplateBaseFlags;
    BaseAssetFlags Physical.IDuplicatorAsset.TemplateBaseFlags
    {
        get => _overriddenTemplateBaseFlags ?? BaseFlags;
        set => _overriddenTemplateBaseFlags = value == BaseFlags ? null : value;
    }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.Duplicator"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.Incredibles,
    };

    internal static DuplicatorAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new DuplicatorAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        asset.InitialSpawn = reader.ReadUInt16();
        asset.MaxInGame = reader.ReadUInt16();
        asset.MaxToSpawn = reader.ReadUInt16();
        reader.ReadUInt16(); // padding, always zero
        asset.SpawnRate = reader.ReadSingle();
        asset.Physical.Avatar = Avatar.Read(reader, profile);

        // TemplateLinkCount derives from LinkCount, which derives from Links - reassigned once both are read.
        asset.Physical.TemplateBaseId = reader.ReadAssetId();
        asset.Physical.TemplateBaseType = reader.ReadByte();
        byte templateLinkCount = reader.ReadByte();
        asset.Physical.TemplateBaseFlags = (BaseAssetFlags)reader.ReadInt16();
        asset.Template = Villain.Read(reader, profile);

        for (var i = 0; i < asset.Physical.LinkCount; i++)
            asset.Links.Add(Link.Read(reader, profile));
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.Physical.TemplateLinkCount = templateLinkCount;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(DuplicatorAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);

        writer.Write(asset.InitialSpawn);
        writer.Write(asset.MaxInGame);
        writer.Write(asset.MaxToSpawn);
        writer.Write((ushort)0); // padding
        writer.Write(asset.SpawnRate);
        Avatar.Write(asset.Physical.Avatar, writer, profile);

        writer.Write(asset.Physical.TemplateBaseId);
        writer.Write(asset.Physical.TemplateBaseType);
        writer.Write(asset.Physical.TemplateLinkCount);
        writer.Write((short)asset.Physical.TemplateBaseFlags);
        Villain.Write(asset.Template, writer, profile);

        foreach (var link in asset.Links)
            Link.Write(link, writer, profile);
        writer.Write(asset.GetUnparsedTail());
    }
}

public static partial class Physical
{
    /// <summary>
    /// An explicit interface used to interact with <see cref="DuplicatorAsset"/>'s underlying values.
    /// </summary>
    public interface IDuplicatorAsset : IBaseAsset
    {
        /// <summary>
        /// A standalone NPC the duplicator creates alongside its pool, whose position every clone then
        /// spawns at: a copy of <see cref="DuplicatorAsset.Template"/>'s entity with these NPC fields,
        /// modelled with <see cref="DuplicatorAsset.Avatar.NpcModelId"/>. Only created when
        /// <see cref="DuplicatorAsset.Avatar.NpcModelId"/> is set.
        /// </summary>
        DuplicatorAsset.Avatar Avatar { get; set; }

        /// <summary>
        /// The <see cref="AssetId"/> stored in <see cref="DuplicatorAsset.Template"/>'s own base
        /// header. Follows <see cref="IBaseAsset.BaseId"/>.
        /// </summary>
        /// <remarks>
        /// When disagreements with <see cref="IBaseAsset.BaseId"/> exist, this field wins during
        /// serialization.
        /// </remarks>
        AssetId TemplateBaseId { get; set; }

        /// <summary>
        /// The base type stored in <see cref="DuplicatorAsset.Template"/>'s own base header - a
        /// <see cref="VillainAsset"/>'s.
        /// </summary>
        byte TemplateBaseType { get; set; }

        /// <summary>
        /// The link count stored in <see cref="DuplicatorAsset.Template"/>'s own base header, which
        /// the game reads the duplicator's <see cref="BaseAsset.Links"/> with. Follows
        /// <see cref="IBaseAsset.LinkCount"/>.
        /// </summary>
        /// <remarks>
        /// When disagreements with <see cref="IBaseAsset.LinkCount"/> exist, this field wins during
        /// serialization.
        /// </remarks>
        byte TemplateLinkCount { get; set; }

        /// <summary>
        /// The <see cref="BaseAssetFlags"/> stored in <see cref="DuplicatorAsset.Template"/>'s own
        /// base header, which the game reads in place of <see cref="BaseAsset.BaseFlags"/>. Follows
        /// <see cref="BaseAsset.BaseFlags"/>.
        /// </summary>
        /// <remarks>
        /// When disagreements with <see cref="BaseAsset.BaseFlags"/> exist, this field wins during
        /// serialization.
        /// </remarks>
        BaseAssetFlags TemplateBaseFlags { get; set; }
    }
}
