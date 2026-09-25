using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// An entity that renders its model, optionally collidable, and optionally looping the
/// <see cref="AssetType.Animation"/> referenced by <see cref="IHasAnimList.AnimListId"/>.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/SIMP">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class SimpleObjectAsset() : EntityAsset(AssetType.SimpleObject, baseType: 0x0B), IHasModel, IHasAnimList, IHasSurface, Physical.ISimpleObjectAsset
{
    /// <summary>
    /// Whether the player, NPCs, and dynamic entities collide with this object.
    /// </summary>
    /// <remarks>
    /// In <see cref="GameVersion.N100F"/> and <see cref="GameVersion.BFBB"/>, only the player and
    /// NPCs do. Setting this to <see langword="true"/> stores <see cref="CollisionKind.Static"/>
    /// unless <see cref="Physical.ISimpleObjectAsset.Collision"/> already holds a collidable value.
    /// </remarks>
    public bool HasCollision
    {
        get => Physical.Collision is not CollisionKind.None;
        set
        {
            if (value != HasCollision)
                Physical.Collision = value ? CollisionKind.Static : CollisionKind.None;
        }
    }

    /// <summary>
    /// Whether this object turns to face the camera every frame.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Takes effect only on an object the engine updates every frame, such as one with an
    /// <see cref="IHasAnimList.AnimListId"/>, or one with 
    /// <see cref="CollisionFlags.AnimateCollision"/>, <see cref="CollisionFlags.Grabbable"/>,
    /// or <see cref="CollisionFlags.LedgeGrab"/> in <see cref="Physical.IEntity.CollisionFlags"/>.
    /// </para>
    /// <para>
    /// <see cref="FacePlayer"/> takes precedence when both are set. Ignored by
    /// <see cref="GameVersion.N100F"/> and <see cref="GameVersion.BFBB"/>.
    /// </para>
    /// </remarks>
    public bool FaceCamera
    {
        get => Physical.SimpleFlags.HasFlag(Behavior.FaceCamera);
        set => Physical.SimpleFlags = Physical.SimpleFlags.WithFlag(Behavior.FaceCamera, value);
    }

    /// <summary>
    /// Whether this object turns to face the player every frame.
    /// </summary>
    /// <remarks>
    /// Takes effect only on an object the engine updates every frame; see <see cref="FaceCamera"/>.
    /// Takes precedence over <see cref="FaceCamera"/> when both are set. Ignored by
    /// <see cref="GameVersion.N100F"/> and <see cref="GameVersion.BFBB"/>.
    /// </remarks>
    public bool FacePlayer
    {
        get => Physical.SimpleFlags.HasFlag(Behavior.FacePlayer);
        set => Physical.SimpleFlags = Physical.SimpleFlags.WithFlag(Behavior.FacePlayer, value);
    }

    /// <summary>
    /// Whether this object stays upright while facing the camera or player.
    /// </summary>
    /// <remarks>
    /// When set, the object turns about the vertical axis only instead of pointing straight at its
    /// target. Has no effect without <see cref="FaceCamera"/> or <see cref="FacePlayer"/>. Ignored by
    /// <see cref="GameVersion.N100F"/> and <see cref="GameVersion.BFBB"/>.
    /// </remarks>
    public bool Upright
    {
        get => Physical.SimpleFlags.HasFlag(Behavior.Upright);
        set => Physical.SimpleFlags = Physical.SimpleFlags.WithFlag(Behavior.Upright, value);
    }

    /// <inheritdoc cref="Asset.Physical"/>
    public override Physical.ISimpleObjectAsset Physical => this;

    private float _animationSpeed = 1f;
    float Physical.ISimpleObjectAsset.AnimationSpeed { get => _animationSpeed; set => _animationSpeed = value; }

    private uint _initialAnimationState;
    uint Physical.ISimpleObjectAsset.InitialAnimationState { get => _initialAnimationState; set => _initialAnimationState = value; }

    private CollisionKind _collision;
    CollisionKind Physical.ISimpleObjectAsset.Collision { get => _collision; set => _collision = value; }

    private Behavior _simpleFlags;
    Behavior Physical.ISimpleObjectAsset.SimpleFlags { get => _simpleFlags; set => _simpleFlags = value; }

    AssetId IHasModel.ModelId { get => Physical.ModelId; set => Physical.ModelId = value; }
    AssetId IHasAnimList.AnimListId { get => Physical.AnimListId; set => Physical.AnimListId = value; }
    AssetId IHasSurface.SurfaceId { get => Physical.SurfaceId; set => Physical.SurfaceId = value; }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.SimpleObject"/> is known to be read by.
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

    internal static SimpleObjectAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new SimpleObjectAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);
        EntityAssetPrefix.Read(asset, reader, profile);

        asset.Physical.AnimationSpeed = reader.ReadSingle();
        asset.Physical.InitialAnimationState = reader.ReadUInt32();
        asset.Physical.Collision = (CollisionKind)reader.ReadByte();
        asset.Physical.SimpleFlags = (Behavior)reader.ReadByte();
        reader.ReadInt16(); // padding, always zero

        for (var i = 0; i < asset.Physical.LinkCount; i++)
            asset.Links.Add(Link.Read(reader, profile));
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(SimpleObjectAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);
        EntityAssetPrefix.Write(asset, writer, profile);

        writer.Write(asset.Physical.AnimationSpeed);
        writer.Write(asset.Physical.InitialAnimationState);
        writer.Write((byte)asset.Physical.Collision);
        writer.Write((byte)asset.Physical.SimpleFlags);
        writer.Write((short)0); // padding

        foreach (var link in asset.Links)
            Link.Write(link, writer, profile);
        writer.Write(asset.GetUnparsedTail());
    }

    /// <summary>
    /// An entity collision type, shared by every entity type. Values combine as a bitmask.
    /// </summary>
    [Flags]
    public enum CollisionKind : byte
    {
        /// <summary>
        /// No collision type.
        /// </summary>
        None = 0,
        /// <summary>
        /// The collision type of a <see cref="AssetType.Trigger"/>.
        /// </summary>
        Trigger = 1 << 0,
        /// <summary>
        /// The collision type of a <see cref="AssetType.SimpleObject"/>.
        /// </summary>
        Static = 1 << 1,
        /// <summary>
        /// The collision type of moving entities such as a <see cref="AssetType.Platform"/>,
        /// <see cref="AssetType.Pendulum"/>, <see cref="AssetType.Hangable"/>, or
        /// <see cref="AssetType.Button"/>.
        /// </summary>
        Dynamic = 1 << 2,
        /// <summary>
        /// The collision type of a <see cref="AssetType.Villain"/>.
        /// </summary>
        NPC = 1 << 3,
        /// <summary>
        /// The collision type of the <see cref="AssetType.Player"/>.
        /// </summary>
        Player = 1 << 4,
        /// <summary>
        /// The collision type a collidable <see cref="SimpleObjectAsset"/> takes when
        /// <see cref="Behavior.EnvironmentCollision"/> is set.
        /// </summary>
        Environment = 1 << 5,
    }

    /// <summary>
    /// Per-object behavior switches.
    /// </summary>
    /// <remarks>
    /// Ignored by <see cref="GameVersion.N100F"/> and <see cref="GameVersion.BFBB"/>.
    /// </remarks>
    [Flags]
    public enum Behavior : byte
    {
        /// <summary>
        /// No behavior switches.
        /// </summary>
        None = 0,
        /// <summary>
        /// Pre-evaluates the animation into a per-frame matrix cache at load.
        /// </summary>
        BakeAnimation = 1 << 0,
        /// <summary>
        /// Turns to face the camera every frame.
        /// </summary>
        /// <remarks>
        /// Takes effect only on an updated object; see <see cref="SimpleObjectAsset.FaceCamera"/>.
        /// </remarks>
        FaceCamera = 1 << 1,
        /// <summary>
        /// Turns to face the player every frame.
        /// </summary>
        /// <remarks>
        /// Takes effect only on an updated object; see <see cref="SimpleObjectAsset.FaceCamera"/>.
        /// Takes precedence over <see cref="FaceCamera"/>.
        /// </remarks>
        FacePlayer = 1 << 2,
        /// <summary>
        /// Stays upright while facing the camera or player.
        /// </summary>
        /// <remarks>
        /// The object turns about the vertical axis only instead of pointing straight at its target.
        /// </remarks>
        Upright = 1 << 4,
        /// <summary>
        /// Gives a collidable object the <see cref="CollisionKind.Environment"/> collision type.
        /// </summary>
        /// <remarks>
        /// Replaces <see cref="Physical.ISimpleObjectAsset.Collision"/> when that is not
        /// <see cref="CollisionKind.None"/>. Ignored by every game except <see cref="GameVersion.ROTU"/>
        /// and <see cref="GameVersion.Ratatouille"/>.
        /// </remarks>
        EnvironmentCollision = 1 << 5,
    }
}

public static partial class Physical
{
    /// <summary>
    /// An explicit interface used to interact with <see cref="SimpleObjectAsset"/>'s underlying values.
    /// </summary>
    public interface ISimpleObjectAsset : IEntityAsset
    {
        /// <summary>
        /// Unknown.
        /// </summary>
        float AnimationSpeed { get; set; }

        /// <summary>
        /// Unknown.
        /// </summary>
        uint InitialAnimationState { get; set; }

        /// <summary>
        /// This object's collision type.
        /// </summary>
        /// <remarks>
        /// Projected by <see cref="SimpleObjectAsset.HasCollision"/>. In
        /// <see cref="GameVersion.N100F"/> and <see cref="GameVersion.BFBB"/>, only
        /// <see cref="SimpleObjectAsset.CollisionKind.Static"/> is read.
        /// </remarks>
        SimpleObjectAsset.CollisionKind Collision { get; set; }

        /// <summary>
        /// This object's behavior switches.
        /// </summary>
        /// <remarks>
        /// Projected by <see cref="SimpleObjectAsset.FaceCamera"/>,
        /// <see cref="SimpleObjectAsset.FacePlayer"/>, and <see cref="SimpleObjectAsset.Upright"/>.
        /// Ignored by <see cref="GameVersion.N100F"/> and <see cref="GameVersion.BFBB"/>.
        /// </remarks>
        SimpleObjectAsset.Behavior SimpleFlags { get; set; }
    }
}
