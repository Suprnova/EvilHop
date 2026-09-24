using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Assets;

public sealed partial class VolumeAsset
{
    public abstract partial class Bound
    {
        /// <summary>
        /// An axis-aligned box <see cref="Bound"/>, before <see cref="Rotation"/> is applied.
        /// </summary>
        public sealed class Box : Bound
        {
            /// <summary>The box's maximum corner.</summary>
            public Vector3 Upper { get; set; }

            /// <summary>The box's minimum corner.</summary>
            public Vector3 Lower { get; set; }

            /// <inheritdoc/>
            public override ShapeKind Kind => ShapeKind.Box;

            internal static new Box Read(EndianReader reader, FormatProfile _) => new()
            {
                Upper = reader.ReadVector3(),
                Lower = reader.ReadVector3(),
            };

            internal static void Write(Box value, EndianWriter writer, FormatProfile _)
            {
                writer.Write(value.Upper);
                writer.Write(value.Lower);
            }
        }
    }
}
