using EvilHop.Common;

namespace EvilHop.Assets;

public partial class ShrapnelAsset
{
    /// <summary>
    /// The kind of effect or object spawned by a <see cref="Fragment"/>.
    /// </summary>
    public enum FragmentKind : uint
    {
        /// <summary>
        /// Inactive fragment slot. Its <see cref="Fragment.Id"/> is repurposed as a marker for
        /// the deactivated fragment's on-disk size - see <see cref="GetInactiveFragSize"/>.
        /// </summary>
        Inactive = 0,

        /// <summary>Group fragment.</summary>
        Group = 1,

        /// <summary>Nested shrapnel spawner referencing another <see cref="AssetType.Shrapnel"/>.</summary>
        Shrapnel = 2,

        /// <summary>Particle emitter fragment.</summary>
        Particle = 3,

        /// <summary>Physical projectile fragment with bounce, gravity, and model.</summary>
        Projectile = 4,

        /// <summary>Lightning arc fragment.</summary>
        Lightning = 5,

        /// <summary>Sound effect fragment.</summary>
        Sound = 6,

        /// <summary>Expanding shockwave ring fragment.</summary>
        Shockwave = 7,

        /// <summary>Explosion effect fragment.</summary>
        Explosion = 8,

        /// <summary>Screen distortion fragment.</summary>
        Distortion = 9,

        /// <summary>Fire effect fragment.</summary>
        Fire = 10,

        /// <summary>Dynamic light source fragment.</summary>
        Light = 11,

        /// <summary>Smoke plume fragment.</summary>
        Smoke = 12,

        /// <summary>Goo splat fragment.</summary>
        Goo = 13,
    }
}
