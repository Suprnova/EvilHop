using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.Immutable;

namespace EvilHop.Assets;

public partial class ProgressScriptAsset
{
    /// <summary>
    /// One <see cref="ProgressScriptAsset"/> event - sends <see cref="ParamEvent"/> to
    /// <see cref="WidgetId"/>, alongside <see cref="Param"/>, once playback reaches <see cref="Percent"/>.
    /// </summary>
    /// <remarks>
    /// Shaped like a <see cref="Link"/>, but not one: it has no source event of its own (playback past
    /// <see cref="Percent"/> is what triggers it) and no <see cref="Link.CheckAssetId"/>.
    /// </remarks>
    public sealed class ProgressEvent()
    {
        /// <summary>
        /// The percentage, from 0 to 100, of playback this event fires at.
        /// </summary>
        public float Percent { get; set; }

        /// <summary>
        /// This event's flags.
        /// </summary>
        public Behavior Flags { get; set; }

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

        private static ImmutableArray<Parameter> ZeroedParams() =>
            [new RawParameter(new byte[4]), new RawParameter(new byte[4]), new RawParameter(new byte[4]), new RawParameter(new byte[4])];

        internal static ProgressEvent Read(EndianReader reader, FormatProfile _) => new()
        {
            Percent = reader.ReadSingle(),
            Flags = (Behavior)reader.ReadInt32(),
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

        internal static void Write(ProgressEvent value, EndianWriter writer, FormatProfile _)
        {
            writer.Write(value.Percent);
            writer.Write((int)value.Flags);
            writer.Write(value.WidgetId);
            writer.Write(value.ParamEvent);
            foreach (var param in value.Param) param.WriteTo(writer);
            writer.Write(value.ParamWidgetId);
        }
    }

    /// <summary>
    /// Flags controlling playback and dispatch behavior for a progress script event.
    /// </summary>
    [Flags]
    public enum Behavior
    {
        /// <summary>No flags are set.</summary>
        None = 0,

        /// <summary>
        /// This event fires only once per <see cref="ProgressScriptAsset"/> reset, rather than every time
        /// playback crosses <see cref="ProgressEvent.Percent"/>.
        /// </summary>
        Once = 1 << 0,
    }
}
