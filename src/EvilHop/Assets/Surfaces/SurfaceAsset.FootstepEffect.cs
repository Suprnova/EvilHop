using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public partial class SurfaceAsset
{
    /// <summary>
    /// The effects a <see cref="SurfaceAsset"/> gives the player's footsteps.
    /// </summary>
    public sealed class FootstepEffect
    {
        /// <summary>
        /// Unknown.
        /// </summary>
        public AssetId ParticleEmitter { get; set; }

        /// <summary>
        /// The sound group played for each footstep.
        /// </summary>
        public AssetId Sound { get; set; }

        /// <summary>
        /// The texture of the footprint decal each footstep leaves; <see cref="AssetId.None"/> leaves
        /// none.
        /// </summary>
        public AssetId Texture { get; set; }

        /// <summary>
        /// Unknown.
        /// </summary>
        /// <remarks>
        /// Footprint decals are only left when this is non-zero.
        /// </remarks>
        public float Duration { get; set; }

        internal static FootstepEffect Read(EndianReader reader, FormatProfile _) => new()
        {
            ParticleEmitter = reader.ReadAssetId(),
            Sound = reader.ReadAssetId(),
            Texture = reader.ReadAssetId(),
            Duration = reader.ReadSingle(),
        };

        internal static void Write(FootstepEffect footsteps, EndianWriter writer, FormatProfile _)
        {
            writer.Write(footsteps.ParticleEmitter);
            writer.Write(footsteps.Sound);
            writer.Write(footsteps.Texture);
            writer.Write(footsteps.Duration);
        }
    }
}
