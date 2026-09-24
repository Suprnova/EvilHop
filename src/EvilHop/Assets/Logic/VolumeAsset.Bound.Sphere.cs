using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public sealed partial class VolumeAsset
{
    public abstract partial class Bound
    {
        /// <summary>
        /// A spherical <see cref="Bound"/>.
        /// </summary>
        public sealed class Sphere : Bound
        {
            /// <summary>The sphere's radius.</summary>
            public float Radius { get; set; }

            /// <inheritdoc/>
            public override ShapeKind Kind => ShapeKind.Sphere;

            internal static new Sphere Read(EndianReader reader, FormatProfile _) => new() { Radius = reader.ReadSingle() };

            internal static void Write(Sphere value, EndianWriter writer, FormatProfile _) => writer.Write(value.Radius);
        }
    }
}
