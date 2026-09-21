using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;

namespace EvilHop.Assets;

public partial class DestructibleAsset
{
    /// <summary>
    /// One state of a <see cref="DestructibleAsset"/>: a model, plus the effects and sounds used while
    /// it is active.
    /// </summary>
    public sealed class DestructibleAssetState
    {
        /// <summary>
        /// Unknown. A percentage value.
        /// </summary>
        public uint Percent { get; set; }

        /// <summary>
        /// The <see cref="AssetType.Model"/> used while in this state.
        /// </summary>
        public AssetId ModelId { get; set; }

        /// <summary>
        /// Unknown. A <see cref="AssetType.Shrapnel"/> reference.
        /// </summary>
        public AssetId ShrapnelId { get; set; }

        /// <summary>
        /// Unknown. A <see cref="AssetType.Shrapnel"/> reference, for this state's "hit" variant.
        /// </summary>
        public AssetId HitShrapnelId { get; set; }

        /// <summary>
        /// Unknown. A <see cref="AssetType.SoundGroup"/> reference, for this state's "idle" variant.
        /// </summary>
        public AssetId IdleSoundGroupId { get; set; }

        /// <summary>
        /// Unknown. A <see cref="AssetType.SoundGroup"/> reference, for this state's "fx" variant.
        /// </summary>
        public AssetId FxSoundGroupId { get; set; }

        /// <summary>
        /// Unknown. A <see cref="AssetType.SoundGroup"/> reference, for this state's "hit" variant.
        /// </summary>
        public AssetId HitSoundGroupId { get; set; }

        /// <summary>
        /// Unknown. A <see cref="AssetType.SoundGroup"/> reference, for this state's "switch fx" variant.
        /// </summary>
        public AssetId SwitchFxSoundGroupId { get; set; }

        /// <summary>
        /// Unknown. A <see cref="AssetType.SoundGroup"/> reference, for this state's "switch hit" variant.
        /// </summary>
        public AssetId SwitchHitSoundGroupId { get; set; }

        /// <summary>
        /// Unknown. A <see cref="AssetType.Dynamic"/> rumble effect reference, for this state's "hit"
        /// variant.
        /// </summary>
        public AssetId HitRumbleId { get; set; }

        /// <summary>
        /// Unknown. A <see cref="AssetType.Dynamic"/> rumble effect reference, for this state's "switch"
        /// variant.
        /// </summary>
        public AssetId SwitchRumbleId { get; set; }

        /// <summary>
        /// Unknown.
        /// </summary>
        public uint FxFlags { get; set; }

        /// <summary>
        /// The <see cref="AssetType.Animation"/>s attached to this state.
        /// </summary>
        public Collection<AssetId> AnimationIds { get; } = [];

        internal static DestructibleAssetState Read(EndianReader reader, FormatProfile _)
        {
            var state = new DestructibleAssetState
            {
                Percent = reader.ReadUInt32(),
                ModelId = reader.ReadAssetId(),
                ShrapnelId = reader.ReadAssetId(),
                HitShrapnelId = reader.ReadAssetId(),
                IdleSoundGroupId = reader.ReadAssetId(),
                FxSoundGroupId = reader.ReadAssetId(),
                HitSoundGroupId = reader.ReadAssetId(),
                SwitchFxSoundGroupId = reader.ReadAssetId(),
                SwitchHitSoundGroupId = reader.ReadAssetId(),
                HitRumbleId = reader.ReadAssetId(),
                SwitchRumbleId = reader.ReadAssetId(),
                FxFlags = reader.ReadUInt32(),
            };

            uint animationCount = reader.ReadUInt32();
            for (int j = 0; j < animationCount; j++)
                state.AnimationIds.Add(reader.ReadAssetId());

            return state;
        }

        internal static void Write(DestructibleAssetState value, EndianWriter writer, FormatProfile _)
        {
            writer.Write(value.Percent);
            writer.Write(value.ModelId);
            writer.Write(value.ShrapnelId);
            writer.Write(value.HitShrapnelId);
            writer.Write(value.IdleSoundGroupId);
            writer.Write(value.FxSoundGroupId);
            writer.Write(value.HitSoundGroupId);
            writer.Write(value.SwitchFxSoundGroupId);
            writer.Write(value.SwitchHitSoundGroupId);
            writer.Write(value.HitRumbleId);
            writer.Write(value.SwitchRumbleId);
            writer.Write(value.FxFlags);

            writer.Write((uint)value.AnimationIds.Count);
            foreach (var animationId in value.AnimationIds) writer.Write(animationId);
        }
    }
}
