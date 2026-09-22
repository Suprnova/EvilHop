using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public partial class OneLinerAsset
{
    /// <summary>
    /// One <see cref="OneLinerAsset"/> entry: a voice line played from <see cref="SoundGroupId"/> when
    /// <see cref="EventType"/> occurs.
    /// </summary>
    public sealed class Entry
    {
        /// <summary>The <see cref="AssetType.SoundGroup"/> played when this entry triggers.</summary>
        public AssetId SoundGroupId { get; set; }

        /// <summary>How long, in seconds, to wait after triggering before <see cref="SoundGroupId"/> actually plays.</summary>
        public float SoundStartDelay { get; set; }

        /// <summary>
        /// A time window, in seconds, associated with this entry's play-count tracking.
        /// </summary>
        public float TimeSpan { get; set; }

        /// <summary>
        /// The time this entry last played, used to enforce <see cref="DelayBetweenPlays"/>.
        /// </summary>
        public float TimeLastPlayed { get; set; }

        /// <summary>
        /// The number of times this entry has played so far.
        /// </summary>
        public uint NumPlays { get; set; }

        /// <summary>The minimum time, in seconds, between two plays of this entry.</summary>
        public float DelayBetweenPlays { get; set; }

        /// <summary>The chance, from 0 to 1, that this entry plays when triggered.</summary>
        public float Probability { get; set; }

        /// <summary>This entry's default duration, in seconds.</summary>
        public float DefaultDuration { get; set; }

        /// <summary>
        /// The duration, in seconds, of this entry's last play.
        /// </summary>
        public float LastDuration { get; set; }

        /// <summary>
        /// The maximum number of times this entry may play.
        /// </summary>
        public uint MaxPlays { get; set; }

        /// <summary>The game event that triggers this entry.</summary>
        public short EventType { get; set; }

        /// <summary>Whether this entry plays through the music channel rather than the sound effect channel.</summary>
        public bool PlaysInMusicChannel { get; set; }

        /// <summary>Which condition, if any, gates this entry playing.</summary>
        public PlayerKind Kind { get; set; }

        /// <summary>
        /// The first parameter passed to <see cref="Kind"/>'s condition, if applicable.
        /// </summary>
        public int FirstParam { get; set; }

        /// <summary>
        /// The second parameter passed to <see cref="Kind"/>'s condition, if applicable.
        /// </summary>
        public float SecondParam { get; set; }

        internal static Entry Read(EndianReader reader, FormatProfile _)
        {
            var entry = new Entry
            {
                SoundGroupId = reader.ReadAssetId(),
                SoundStartDelay = reader.ReadSingle(),
                TimeSpan = reader.ReadSingle(),
                TimeLastPlayed = reader.ReadSingle(),
                NumPlays = reader.ReadUInt32(),
                DelayBetweenPlays = reader.ReadSingle(),
                Probability = reader.ReadSingle(),
                DefaultDuration = reader.ReadSingle(),
                LastDuration = reader.ReadSingle(),
                MaxPlays = reader.ReadUInt32(),
            };
            reader.ReadUInt32(); // m_soundGroupHandle, runtime-resolved, always zero
            reader.ReadUInt32(); // m_pOLManager, runtime-resolved back-pointer, always zero
            entry.EventType = reader.ReadInt16();
            entry.PlaysInMusicChannel = reader.ReadInt16() != 0;
            reader.ReadUInt32(); // m_pData, runtime-resolved per-PlayerType data pointer, always zero
            entry.Kind = (PlayerKind)reader.ReadInt32();
            entry.FirstParam = reader.ReadInt32();
            entry.SecondParam = reader.ReadSingle();
            return entry;
        }

        internal static void Write(Entry entry, EndianWriter writer, FormatProfile _)
        {
            writer.Write(entry.SoundGroupId);
            writer.Write(entry.SoundStartDelay);
            writer.Write(entry.TimeSpan);
            writer.Write(entry.TimeLastPlayed);
            writer.Write(entry.NumPlays);
            writer.Write(entry.DelayBetweenPlays);
            writer.Write(entry.Probability);
            writer.Write(entry.DefaultDuration);
            writer.Write(entry.LastDuration);
            writer.Write(entry.MaxPlays);
            writer.Write(0u); // m_soundGroupHandle
            writer.Write(0u); // m_pOLManager
            writer.Write(entry.EventType);
            writer.Write((short)(entry.PlaysInMusicChannel ? 1 : 0));
            writer.Write(0u); // m_pData
            writer.Write((int)entry.Kind);
            writer.Write(entry.FirstParam);
            writer.Write(entry.SecondParam);
        }
    }
}
