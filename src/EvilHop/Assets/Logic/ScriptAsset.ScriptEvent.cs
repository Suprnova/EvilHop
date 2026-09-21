using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.Immutable;

namespace EvilHop.Assets;

public partial class ScriptAsset
{
    /// <summary>
    /// One <see cref="ScriptAsset"/> event - sends <see cref="ParamEvent"/> to <see cref="WidgetId"/>,
    /// alongside <see cref="Param"/>, once <see cref="Time"/> seconds have passed since the script
    /// started running.
    /// </summary>
    /// <remarks>
    /// Shaped like a <see cref="Link"/>, but not one: it has no source event of its own (elapsed time
    /// reaching <see cref="Time"/> is what triggers it) and no <see cref="Link.CheckAssetId"/>.
    /// </remarks>
    public sealed class ScriptEvent()
    {

        /// <summary>
        /// The time, in seconds, this event fires at, relative to when the script starts running.
        /// </summary>
        public float Time { get; set; }

        /// <summary>
        /// The <see cref="AssetId"/> this event sends <see cref="ParamEvent"/> to.
        /// </summary>
        public AssetId WidgetId { get; set; }

        /// <summary>
        /// The event sent to <see cref="WidgetId"/>.
        /// </summary>
        public uint ParamEvent { get; set; }

        /// <summary>
        /// The four parameter slots passed alongside <see cref="ParamEvent"/>. Always exactly 4
        /// elements, in order.
        /// </summary>
        /// <exception cref="ArgumentException">The assigned value's length isn't 4.</exception>
        public ImmutableArray<Parameter> Param
        {
            get;
            set => field = value.Length == 4
                ? value
                : throw new ArgumentException($"{nameof(Param)} must contain exactly 4 elements.", nameof(value));
        } = ZeroedParams();

        /// <summary>
        /// A supplemental <see cref="AssetId"/> parameter for <see cref="ParamEvent"/>.
        /// </summary>
        public AssetId ParamWidgetId { get; set; }

        /// <summary>
        /// Whether this event fires at all.
        /// </summary>
        /// <remarks>
        /// Only present in <see cref="GameVersion.ROTU"/> and <see cref="GameVersion.Ratatouille"/>.
        /// </remarks>
        public bool Enabled { get; set; } = true;

        private static ImmutableArray<Parameter> ZeroedParams() =>
            [new RawParameter(new byte[4]), new RawParameter(new byte[4]), new RawParameter(new byte[4]), new RawParameter(new byte[4])];

        internal static ScriptEvent Read(EndianReader reader, FormatProfile profile)
        {
            var evt = new ScriptEvent
            {
                Time = reader.ReadSingle(),
                WidgetId = reader.ReadAssetId(),
                ParamEvent = reader.ReadUInt32(),
                Param =
                [
                    new RawParameter(reader.ReadBytes(4)),
                    new RawParameter(reader.ReadBytes(4)),
                    new RawParameter(reader.ReadBytes(4)),
                    new RawParameter(reader.ReadBytes(4)),
                ],
                ParamWidgetId = reader.ReadAssetId(),
            };

            bool hasEnabled = profile.Game is GameVersion.ROTU or GameVersion.Ratatouille;
            if (hasEnabled)
            {
                evt.Enabled = reader.ReadByte() != 0;
                reader.ReadBytes(3); // padding, always zero
            }

            return evt;
        }

        internal static void Write(ScriptEvent value, EndianWriter writer, FormatProfile profile)
        {
            writer.Write(value.Time);
            writer.Write(value.WidgetId);
            writer.Write(value.ParamEvent);
            foreach (var param in value.Param) param.WriteTo(writer);
            writer.Write(value.ParamWidgetId);

            bool hasEnabled = profile.Game is GameVersion.ROTU or GameVersion.Ratatouille;
            if (hasEnabled)
            {
                writer.Write((byte)(value.Enabled ? 1 : 0));
                writer.Write(new byte[3]); // padding
            }
        }
    }
}
