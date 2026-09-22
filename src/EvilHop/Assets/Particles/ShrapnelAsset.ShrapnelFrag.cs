using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace EvilHop.Assets;

public partial class ShrapnelAsset
{
    /// <summary>
    /// A single fragment entry within a <see cref="ShrapnelAsset"/>.
    /// </summary>
    public sealed class ShrapnelFrag
    {
        /// <summary>The type of effect or object spawned by this fragment.</summary>
        public ShrapnelFragType Type { get; set; }

        /// <summary>This fragment's asset ID.</summary>
        public AssetId Id { get; set; }

        /// <summary>The asset ID of this fragment's first parent, if any.</summary>
        public AssetId ParentId0 { get; set; }

        /// <summary>The asset ID of this fragment's second parent, if any.</summary>
        public AssetId ParentId1 { get; set; }

        /// <summary>The lifetime of this fragment, in seconds.</summary>
        public float Lifetime { get; set; }

        /// <summary>The delay before this fragment spawns, in seconds.</summary>
        public float Delay { get; set; }

        /// <summary>
        /// The type-specific raw data payload following the 24-byte fragment header.
        /// </summary>
        [SuppressMessage("Design", "CA1819:Properties should not return arrays", Justification = "Raw fragment data payload; a byte[] is the natural representation.")]
        public byte[] Data { get; set; } = [];

        internal static ShrapnelFrag Read(EndianReader reader, FormatProfile profile)
        {
            var type = (ShrapnelFragType)reader.ReadUInt32();
            var id = reader.ReadAssetId();
            var parentId0 = reader.ReadAssetId();
            var parentId1 = reader.ReadAssetId();
            float lifetime = reader.ReadSingle();
            float delay = reader.ReadSingle();

            int totalFragSize = type == ShrapnelFragType.Inactive
                ? GetInactiveFragSize(profile.Game, id.Value)
                : GetFragSize(profile.Game, type, profile.ShrapnelHasExtendedFragFields, profile.ShrapnelSoundHasExtendedFields, profile.ShrapnelProjectileHasIntermediateFields);
            if (totalFragSize < 24)
            {
                throw new InvalidDataException($"Unknown or unsupported fragment type {(uint)type} under {profile.Game}.");
            }

            return new ShrapnelFrag
            {
                Type = type,
                Id = id,
                ParentId0 = parentId0,
                ParentId1 = parentId1,
                Lifetime = lifetime,
                Delay = delay,
                Data = reader.ReadBytes(totalFragSize - 24),
            };
        }

        internal static void Write(ShrapnelFrag frag, EndianWriter writer, FormatProfile _)
        {
            writer.Write((uint)frag.Type);
            writer.Write(frag.Id);
            writer.Write(frag.ParentId0);
            writer.Write(frag.ParentId1);
            writer.Write(frag.Lifetime);
            writer.Write(frag.Delay);
            if (frag.Data is not null)
                writer.Write(frag.Data);
        }
    }
}
