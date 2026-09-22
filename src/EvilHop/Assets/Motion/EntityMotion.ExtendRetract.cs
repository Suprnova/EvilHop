using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Assets;

public partial class EntityMotion
{
    /// <summary>
    /// An <see cref="EntityMotion"/> that moves out to one position and back again, waiting at each end.
    /// </summary>
    public sealed class ExtendRetract() : EntityMotion
    {
        /// <summary>The position the entity starts at, and retracts back to.</summary>
        public Vector3 RetractPosition { get; set; }

        /// <summary>The offset from <see cref="RetractPosition"/> the entity extends to.</summary>
        public Vector3 ExtendOffset { get; set; }

        /// <summary>The time, in seconds, the entity takes to extend.</summary>
        public float ExtendTime { get; set; }

        /// <summary>The time, in seconds, the entity waits once extended.</summary>
        public float ExtendWaitTime { get; set; }

        /// <summary>The time, in seconds, the entity takes to retract.</summary>
        public float RetractTime { get; set; }

        /// <summary>The time, in seconds, the entity waits once retracted.</summary>
        public float RetractWaitTime { get; set; }

        private protected override Kind Type => Kind.ExtendRetract;

        internal static new ExtendRetract Read(EndianReader reader, FormatProfile _) => new()
        {
            RetractPosition = reader.ReadVector3(),
            ExtendOffset = reader.ReadVector3(),
            ExtendTime = reader.ReadSingle(),
            ExtendWaitTime = reader.ReadSingle(),
            RetractTime = reader.ReadSingle(),
            RetractWaitTime = reader.ReadSingle(),
        };

        internal static void Write(ExtendRetract value, EndianWriter writer, FormatProfile _)
        {
            writer.Write(value.RetractPosition);
            writer.Write(value.ExtendOffset);
            writer.Write(value.ExtendTime);
            writer.Write(value.ExtendWaitTime);
            writer.Write(value.RetractTime);
            writer.Write(value.RetractWaitTime);
        }
    }
}
