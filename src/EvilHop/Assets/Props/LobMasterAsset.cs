using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>
/// A launcher that lobs a <see cref="AssetType.Projectile"/> along an arc, optionally firing several
/// at once as a salvo.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/LOBM">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class LobMasterAsset() : BaseAsset(AssetType.LobMaster, baseType: 0x23)
{
    /// <summary>This launcher's type.</summary>
    public int LobMasterType { get; set; }

    /// <summary>The <see cref="AssetType.Projectile"/> this launcher lobs.</summary>
    public AssetId ProjectileId { get; set; }

    /// <summary>The position a projectile is launched from.</summary>
    public Vector3 LaunchPosition { get; set; }

    /// <summary>The rotation a projectile is launched along.</summary>
    public Vector3 LaunchRotation { get; set; }

    /// <summary>The speed a projectile is launched at.</summary>
    public float LaunchSpeed { get; set; }

    /// <summary>How much, as a percentage, <see cref="LaunchSpeed"/> randomly varies per shot.</summary>
    public float LaunchSpeedVariance { get; set; }

    /// <summary>The scale applied to the projectile's model.</summary>
    public Vector3 ModelScale { get; set; }

    /// <summary>Toggles this launcher's optional behaviors.</summary>
    public int Enablers { get; set; }

    /// <summary>How long, in seconds, a launched projectile lives before expiring.</summary>
    public float MaxLifetime { get; set; }

    /// <summary>The maximum distance a launched projectile may travel before expiring.</summary>
    public float MaxDistance { get; set; }

    /// <summary>
    /// The <see cref="AssetType.MovePoint"/> this launcher's projectiles travel toward, if any.
    /// </summary>
    public AssetId MovePointId { get; set; }

    /// <summary>The number of projectiles launched together in one salvo.</summary>
    public int SalvoCount { get; set; }

    /// <summary>
    /// The number of salvos this launcher may fire before running out, or <c>-1</c> for unlimited
    /// ammo.
    /// </summary>
    public int AmmoCount { get; set; }

    /// <summary>The coefficient shaping a launched projectile's arc.</summary>
    public float ArcCoefficient { get; set; }

    /// <summary>The angle of the cone debris scatters within when a projectile is destroyed.</summary>
    public int DebrisConeAngle { get; set; }

    /// <summary>The number of times a launched projectile may bounce before coming to rest.</summary>
    public int BounceCount { get; set; }

    /// <summary>Which powerup, if any, a launched projectile drops.</summary>
    public int PowerupType { get; set; }

    /// <summary>Scales how heavily a launched projectile behaves once at rest.</summary>
    public float HeavyFactor { get; set; }

    /// <summary>The rotation a launched projectile tumbles through while in flight.</summary>
    public Vector3 TumbleRotation { get; set; }

    /// <summary>How long, in seconds, a projectile waits after colliding before it may collide again.</summary>
    public float CollideDelay { get; set; }

    /// <summary>How long, in seconds, a projectile remains at rest before expiring.</summary>
    public float AtRestPeriod { get; set; }

    /// <summary>This launcher's mode.</summary>
    public uint Mode { get; set; }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.LobMaster"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.N100F,
    };

    internal static LobMasterAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new LobMasterAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        asset.LobMasterType = reader.ReadInt32();
        asset.ProjectileId = reader.ReadAssetId();
        asset.LaunchPosition = reader.ReadVector3();
        asset.LaunchRotation = reader.ReadVector3();
        asset.LaunchSpeed = reader.ReadSingle();
        asset.LaunchSpeedVariance = reader.ReadSingle();
        asset.ModelScale = reader.ReadVector3();
        asset.Enablers = reader.ReadInt32();
        asset.MaxLifetime = reader.ReadSingle();
        asset.MaxDistance = reader.ReadSingle();
        asset.MovePointId = reader.ReadAssetId();
        asset.SalvoCount = reader.ReadInt32();
        asset.AmmoCount = reader.ReadInt32();
        asset.ArcCoefficient = reader.ReadSingle();
        asset.DebrisConeAngle = reader.ReadInt32();
        asset.BounceCount = reader.ReadInt32();
        asset.PowerupType = reader.ReadInt32();
        asset.HeavyFactor = reader.ReadSingle();
        asset.TumbleRotation = reader.ReadVector3();
        asset.CollideDelay = reader.ReadSingle();
        asset.AtRestPeriod = reader.ReadSingle();
        asset.Mode = reader.ReadUInt32();

        for (var i = 0; i < asset.Physical.LinkCount; i++)
            asset.Links.Add(Link.Read(reader, profile));
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(LobMasterAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);

        writer.Write(asset.LobMasterType);
        writer.Write(asset.ProjectileId);
        writer.Write(asset.LaunchPosition);
        writer.Write(asset.LaunchRotation);
        writer.Write(asset.LaunchSpeed);
        writer.Write(asset.LaunchSpeedVariance);
        writer.Write(asset.ModelScale);
        writer.Write(asset.Enablers);
        writer.Write(asset.MaxLifetime);
        writer.Write(asset.MaxDistance);
        writer.Write(asset.MovePointId);
        writer.Write(asset.SalvoCount);
        writer.Write(asset.AmmoCount);
        writer.Write(asset.ArcCoefficient);
        writer.Write(asset.DebrisConeAngle);
        writer.Write(asset.BounceCount);
        writer.Write(asset.PowerupType);
        writer.Write(asset.HeavyFactor);
        writer.Write(asset.TumbleRotation);
        writer.Write(asset.CollideDelay);
        writer.Write(asset.AtRestPeriod);
        writer.Write(asset.Mode);

        foreach (var link in asset.Links)
            Link.Write(link, writer, profile);
        writer.Write(asset.GetUnparsedTail());
    }
}
