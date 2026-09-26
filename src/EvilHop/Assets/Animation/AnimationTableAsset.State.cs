using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public partial class AnimationTableAsset
{
    /// <summary>
    /// One <see cref="AnimationTableAsset"/> state: a named entry in the table's runtime state machine.
    /// </summary>
    public sealed class State
    {
        /// <summary>
        /// A hash identifying this state, matched against link/animation-table lookups by name.
        /// </summary>
        public uint StateId { get; set; }

        /// <summary>
        /// The index into the owning <see cref="AnimationTableAsset"/>'s <see cref="Files"/>
        /// this state plays.
        /// </summary>
        public uint FileIndex { get; set; }

        /// <summary>
        /// The number of playback effects (sounds, etc.) attached to this state, starting at
        /// <see cref="EffectOffset"/>. Not individually resolved; preserved via
        /// <see cref="Asset.GetUnparsedTail"/>.
        /// </summary>
        public uint EffectCount { get; set; }

        /// <summary>
        /// An internal byte offset, from the start of the owning <see cref="AnimationTableAsset"/>'s data,
        /// to this state's <see cref="EffectCount"/> effect records. Not individually resolved; preserved
        /// via <see cref="Asset.GetUnparsedTail"/>.
        /// </summary>
        public uint EffectOffset { get; set; }

        /// <summary>
        /// The playback speed multiplier applied while in this state.
        /// </summary>
        public float Speed { get; set; }

        /// <summary>
        /// The id of a "sub-state" this state's file is additionally registered under, if any - see
        /// <see cref="SubStateCount"/>.
        /// </summary>
        /// <remarks>
        /// Not present in <see cref="GameVersion.N100F"/>.
        /// </remarks>
        public uint SubStateId { get; set; }

        /// <summary>
        /// The number of files sharing <see cref="SubStateId"/>, chosen between at random when this state
        /// is entered.
        /// </summary>
        /// <remarks>
        /// Not present in <see cref="GameVersion.N100F"/>.
        /// </remarks>
        public uint SubStateCount { get; set; }

        internal static State Read(EndianReader reader, FormatProfile profile)
        {
            var state = new State
            {
                StateId = reader.ReadUInt32(),
                FileIndex = reader.ReadUInt32(),
                EffectCount = reader.ReadUInt32(),
                EffectOffset = reader.ReadUInt32(),
            };
            if (profile.AnimationTableHasStateSpeed)
                state.Speed = reader.ReadSingle();
            if (profile.Game != GameVersion.N100F)
            {
                state.SubStateId = reader.ReadUInt32();
                state.SubStateCount = reader.ReadUInt32();
            }
            return state;
        }

        internal static void Write(State value, EndianWriter writer, FormatProfile profile)
        {
            writer.Write(value.StateId);
            writer.Write(value.FileIndex);
            writer.Write(value.EffectCount);
            writer.Write(value.EffectOffset);
            if (profile.AnimationTableHasStateSpeed)
                writer.Write(value.Speed);
            if (profile.Game != GameVersion.N100F)
            {
                writer.Write(value.SubStateId);
                writer.Write(value.SubStateCount);
            }
        }
    }
}
