using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public partial class AttackTableAsset
{
    /// <summary>
    /// One <see cref="AttackTableAsset"/> transition: an allowed switch between two of the owning table's
    /// <see cref="States"/>.
    /// </summary>
    public sealed class Transition
    {
        /// <summary>
        /// A hash identifying the <see cref="States"/> entry this transition starts from.
        /// </summary>
        public uint SourceState { get; set; }

        /// <summary>
        /// A hash identifying the <see cref="States"/> entry this transition ends at.
        /// </summary>
        public uint DestinationState { get; set; }

        /// <summary>
        /// The time, in seconds into <see cref="SourceState"/>'s animation, at which this transition
        /// becomes available.
        /// </summary>
        public float SourceTime { get; set; }

        /// <summary>
        /// The time, in seconds, this transition's blend takes to complete.
        /// </summary>
        public float ThroughTime { get; set; }

        /// <summary>
        /// The time, in seconds into <see cref="DestinationState"/>'s animation, playback resumes at.
        /// </summary>
        public float DestinationTime { get; set; }

        /// <summary>
        /// The time, in seconds, the blend between animations takes.
        /// </summary>
        public float BlendTime { get; set; }

        /// <summary>
        /// Unknown flags. Always 0.
        /// </summary>
        public uint Flags { get; set; }

        internal static Transition Read(EndianReader reader, FormatProfile _) => new()
        {
            SourceState = reader.ReadUInt32(),
            DestinationState = reader.ReadUInt32(),
            SourceTime = reader.ReadSingle(),
            ThroughTime = reader.ReadSingle(),
            DestinationTime = reader.ReadSingle(),
            BlendTime = reader.ReadSingle(),
            Flags = reader.ReadUInt32(),
        };

        internal static void Write(Transition transition, EndianWriter writer, FormatProfile _)
        {
            writer.Write(transition.SourceState);
            writer.Write(transition.DestinationState);
            writer.Write(transition.SourceTime);
            writer.Write(transition.ThroughTime);
            writer.Write(transition.DestinationTime);
            writer.Write(transition.BlendTime);
            writer.Write(transition.Flags);
        }
    }
}
