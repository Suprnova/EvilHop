using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public sealed partial class VolumeAsset
{
    public abstract partial class Bound
    {
        /// <summary>
        /// A vertical cylindrical <see cref="Bound"/>.
        /// </summary>
        public sealed class Cylinder : Bound
        {
            /// <summary>The cylinder's radius.</summary>
            public float Radius { get; set; }

            /// <summary>The cylinder's height.</summary>
            public float Height { get; set; }

            /// <inheritdoc/>
            public override ShapeKind Kind => ShapeKind.Cylinder;

            internal static new Cylinder Read(EndianReader reader, FormatProfile _) => new()
            {
                Radius = reader.ReadSingle(),
                Height = reader.ReadSingle(),
            };

            internal static void Write(Cylinder value, EndianWriter writer, FormatProfile _)
            {
                writer.Write(value.Radius);
                writer.Write(value.Height);
            }
        }
    }
}
