using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// A physics-driven rolling/bouncing sphere, such as a boulder thrown by a cannon.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/BOUL">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class BoulderAsset : EntityAsset, IHasModel, IHasAnimList
{
    /// <summary>
    /// The downward acceleration applied to this boulder.
    /// </summary>
    public float Gravity { get; set; }

    /// <summary>
    /// This boulder's mass, used to scale forces applied to it. Forced to 1 if not positive.
    /// </summary>
    public float Mass { get; set; }

    /// <summary>
    /// How much of this boulder's velocity is preserved along the collision normal when it bounces
    /// off a surface.
    /// </summary>
    public float Bounce { get; set; }

    /// <summary>
    /// How quickly this boulder's velocity decays while rolling against a surface.
    /// </summary>
    public float Friction { get; set; }

    /// <summary>
    /// If this boulder's downward velocity is below this threshold when it hits the ground, that
    /// velocity is reset to 0. <see cref="GameVersion.BFBB"/> only.
    /// </summary>
    public float StaticFriction { get; set; }

    /// <summary>
    /// The maximum speed this boulder can move at.
    /// </summary>
    public float MaxVelocity { get; set; }

    /// <summary>
    /// The maximum speed this boulder can rotate at.
    /// </summary>
    public float MaxAngularVelocity { get; set; }

    /// <summary>
    /// How strongly this boulder's rotation axis follows its direction of travel, versus staying
    /// where it was.
    /// </summary>
    public float Stickiness { get; set; }

    /// <summary>
    /// The downward velocity threshold, on landing, below which <see cref="Bounce"/> is not applied.
    /// </summary>
    public float BounceDamping { get; set; }

    /// <summary>
    /// This boulder's behavior flags.
    /// </summary>
    public BoulderFlags Flags { get; set; }

    /// <summary>
    /// The lifetime, in seconds, before this boulder is destroyed, if
    /// <see cref="BoulderFlags.DieAfterKillTimer"/> is set. If 0, the lifetime is infinite.
    /// </summary>
    public float KillTimer { get; set; }

    /// <summary>
    /// The number of hits this boulder can take from a damaging surface (see
    /// <see cref="BoulderFlags.DieOnDamagingSurface"/>) before it is destroyed.
    /// </summary>
    public uint Hitpoints { get; set; }

    /// <summary>
    /// The sound played when this boulder bounces off a surface, if any. References an
    /// <see cref="AssetType.Sound"/> in <see cref="GameVersion.BFBB"/>, or an
    /// <see cref="AssetType.SoundGroup"/> in every other supported game.
    /// </summary>
    public AssetId BounceSoundId { get; set; }

    /// <summary>
    /// The volume <see cref="BounceSoundId"/> plays at. <see cref="GameVersion.BFBB"/> only.
    /// </summary>
    public float Volume { get; set; }

    /// <summary>
    /// The downward velocity below which <see cref="BounceSoundId"/> does not play.
    /// </summary>
    public float MinSoundVelocity { get; set; }

    /// <summary>
    /// The downward velocity at and above which <see cref="BounceSoundId"/> plays at full
    /// <see cref="Volume"/>.
    /// </summary>
    public float MaxSoundVelocity { get; set; }

    /// <summary>
    /// The distance from this boulder at which <see cref="BounceSoundId"/> is at full volume.
    /// <see cref="GameVersion.BFBB"/> only.
    /// </summary>
    public float InnerRadius { get; set; }

    /// <summary>
    /// The distance from this boulder beyond which <see cref="BounceSoundId"/> is inaudible.
    /// <see cref="GameVersion.BFBB"/> only.
    /// </summary>
    public float OuterRadius { get; set; }

    /// <summary>
    /// The distance from this boulder at which <see cref="BounceSoundId"/> falls off to silent. Not
    /// present in <see cref="GameVersion.BFBB"/>.
    /// </summary>
    public float SoundRadius { get; set; }

    /// <summary>
    /// The skeleton bone <see cref="BounceSoundId"/> is played from. Not present in
    /// <see cref="GameVersion.BFBB"/>.
    /// </summary>
    public byte BoneIndex { get; set; }

    /// <summary>
    /// The time, in seconds after spawning, before this boulder can collide with anything.
    /// <see cref="GameVersion.ROTU"/> only.
    /// </summary>
    public float InitialNonCollideTime { get; set; }

    AssetId IHasModel.ModelId { get => Physical.ModelId; set => Physical.ModelId = value; }
    AssetId IHasAnimList.AnimListId { get => Physical.AnimListId; set => Physical.AnimListId = value; }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.Boulder"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.BFBB,
        GameVersion.TSSM,
        GameVersion.Incredibles,
        GameVersion.ROTU,
    };

    internal BoulderAsset() { }

    internal static BoulderAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new BoulderAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);
        EntityAssetPrefix.Read(asset, reader, profile.EntityHasPadding);

        asset.Gravity = reader.ReadSingle();
        asset.Mass = reader.ReadSingle();
        asset.Bounce = reader.ReadSingle();
        asset.Friction = reader.ReadSingle();
        if (profile.Game is GameVersion.BFBB) asset.StaticFriction = reader.ReadSingle();
        asset.MaxVelocity = reader.ReadSingle();
        asset.MaxAngularVelocity = reader.ReadSingle();
        asset.Stickiness = reader.ReadSingle();
        asset.BounceDamping = reader.ReadSingle();
        asset.Flags = (BoulderFlags)reader.ReadUInt32();
        asset.KillTimer = reader.ReadSingle();
        asset.Hitpoints = reader.ReadUInt32();
        asset.BounceSoundId = reader.ReadAssetId();
        if (profile.Game is GameVersion.BFBB) asset.Volume = reader.ReadSingle();
        asset.MinSoundVelocity = reader.ReadSingle();
        asset.MaxSoundVelocity = reader.ReadSingle();

        if (profile.Game is GameVersion.BFBB)
        {
            asset.InnerRadius = reader.ReadSingle();
            asset.OuterRadius = reader.ReadSingle();
        }
        else
        {
            asset.SoundRadius = reader.ReadSingle();
            reader.ReadByte(); // pad0, always zero
            reader.ReadByte(); // pad1, always zero
            reader.ReadByte(); // pad2, always zero
            asset.BoneIndex = reader.ReadByte();
            if (profile.Game is GameVersion.ROTU) asset.InitialNonCollideTime = reader.ReadSingle();
        }

        LinkSerialization.Read(asset, reader, asset.Physical.LinkCount);
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(BoulderAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);
        EntityAssetPrefix.Write(asset, writer, profile.EntityHasPadding);

        writer.Write(asset.Gravity);
        writer.Write(asset.Mass);
        writer.Write(asset.Bounce);
        writer.Write(asset.Friction);
        if (profile.Game is GameVersion.BFBB) writer.Write(asset.StaticFriction);
        writer.Write(asset.MaxVelocity);
        writer.Write(asset.MaxAngularVelocity);
        writer.Write(asset.Stickiness);
        writer.Write(asset.BounceDamping);
        writer.Write((uint)asset.Flags);
        writer.Write(asset.KillTimer);
        writer.Write(asset.Hitpoints);
        writer.Write(asset.BounceSoundId);
        if (profile.Game is GameVersion.BFBB) writer.Write(asset.Volume);
        writer.Write(asset.MinSoundVelocity);
        writer.Write(asset.MaxSoundVelocity);

        if (profile.Game is GameVersion.BFBB)
        {
            writer.Write(asset.InnerRadius);
            writer.Write(asset.OuterRadius);
        }
        else
        {
            writer.Write(asset.SoundRadius);
            writer.Write((byte)0); // pad0
            writer.Write((byte)0); // pad1
            writer.Write((byte)0); // pad2
            writer.Write(asset.BoneIndex);
            if (profile.Game is GameVersion.ROTU) writer.Write(asset.InitialNonCollideTime);
        }

        LinkSerialization.Write(asset, writer);
        writer.Write(asset.GetUnparsedTail());
    }
}

/// <summary>
/// Represents all known values for <see cref="BoulderAsset.Flags"/>.
/// </summary>
[Flags]
public enum BoulderFlags : uint
{
    /// <summary>
    /// No flags are set.
    /// </summary>
    None = 0,
    /// <summary>
    /// If unset, this boulder is destroyed when it hits a wall.
    /// </summary>
    CanHitWalls = 1 << 0,
    /// <summary>
    /// This boulder damages the player on contact.
    /// </summary>
    DamagePlayer = 1 << 1,
    /// <summary>
    /// This boulder damages destructible objects on contact.
    /// </summary>
    DamageDestructibleObjects = 1 << 2,
    /// <summary>
    /// This boulder damages NPCs on contact.
    /// </summary>
    DamageNPCs = 1 << 3,
    /// <summary>
    /// This boulder is destroyed (after <see cref="BoulderAsset.Hitpoints"/> hits) when it touches a
    /// surface with a damaging surface type. Per decompiled source.
    /// </summary>
    DieOnDamagingSurface = 1 << 4,
    /// <summary>
    /// This boulder is destroyed when it touches an out-of-bounds surface.
    /// </summary>
    DieOnOutOfBoundsSurfaces = 1 << 5,
    /// <summary>
    /// This boulder is destroyed when it touches goo. Per decompiled source.
    /// </summary>
    DieInGoo = 1 << 6,
    /// <summary>
    /// This boulder reacts to being hit by the player's attack.
    /// </summary>
    DieOnPlayerAttack = 1 << 8,
    /// <summary>
    /// If set, this boulder is destroyed after <see cref="BoulderAsset.KillTimer"/> seconds. If
    /// unset, this has the same effect as a <see cref="BoulderAsset.KillTimer"/> of 0 (infinite
    /// lifetime).
    /// </summary>
    DieAfterKillTimer = 1 << 9,
}
