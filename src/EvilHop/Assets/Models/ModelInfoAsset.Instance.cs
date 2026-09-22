using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Assets;

public partial class ModelInfoAsset
{
    /// <summary>
    /// One <see cref="ModelInfoAsset"/> model instance, attached to <see cref="Parent"/>'s
    /// <see cref="Bone"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="Flags"/>, <see cref="Parent"/>, and <see cref="Bone"/> are only read for every instance
    /// after the first - the root instance (index 0) ignores them entirely.
    /// </remarks>
    public sealed class Instance
    {
        /// <summary>The <see cref="AssetType.Model"/> - or nested <see cref="AssetType.ModelInfo"/> - this instance displays.</summary>
        public AssetId ModelId { get; set; }

        /// <summary>Flags applied when attaching this instance to <see cref="Parent"/>. Unused for the root instance.</summary>
        public ushort Flags { get; set; }

        /// <summary>
        /// The index into <see cref="ModelInstances"/> this instance attaches to. Unused
        /// for the root instance.
        /// </summary>
        public byte Parent { get; set; }

        /// <summary>The bone on <see cref="Parent"/> this instance attaches to. Unused for the root instance.</summary>
        public byte Bone { get; set; }

        /// <summary>The right vector of this instance's orientation relative to <see cref="Parent"/>, usually (1, 0, 0).</summary>
        public Vector3 Right { get; set; }

        /// <summary>The up vector of this instance's orientation relative to <see cref="Parent"/>, usually (0, 1, 0).</summary>
        public Vector3 Up { get; set; }

        /// <summary>The forward vector of this instance's orientation relative to <see cref="Parent"/>, usually (0, 0, 1).</summary>
        public Vector3 At { get; set; }

        /// <summary>This instance's position relative to <see cref="Parent"/>, usually zero.</summary>
        public Vector3 Position { get; set; }

        internal static Instance Read(EndianReader reader, FormatProfile _) => new()
        {
            ModelId = reader.ReadAssetId(),
            Flags = reader.ReadUInt16(),
            Parent = reader.ReadByte(),
            Bone = reader.ReadByte(),
            Right = reader.ReadVector3(),
            Up = reader.ReadVector3(),
            At = reader.ReadVector3(),
            Position = reader.ReadVector3(),
        };

        internal static void Write(Instance value, EndianWriter writer, FormatProfile _)
        {
            writer.Write(value.ModelId);
            writer.Write(value.Flags);
            writer.Write(value.Parent);
            writer.Write(value.Bone);
            writer.Write(value.Right);
            writer.Write(value.Up);
            writer.Write(value.At);
            writer.Write(value.Position);
        }
    }
}
