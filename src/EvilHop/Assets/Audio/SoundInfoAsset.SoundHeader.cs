using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public partial class SoundInfoAsset
{
    /// <summary>
    /// One sound's platform-specific audio header, linking it to its <see cref="AssetType.Sound"/>,
    /// <see cref="AssetType.StreamingSound"/>, or <see cref="AssetType.CutsceneStreamingSound"/> asset.
    /// </summary>
    /// <remarks>
    /// Each platform stores a different header: <see cref="DspHeader"/> on <see cref="Platform.GameCube"/>,
    /// <see cref="WaveHeader"/> on <see cref="Platform.Xbox"/>, and <see cref="VagHeader"/> on
    /// <see cref="Platform.PlayStation2"/>. Writing a header to a platform it doesn't belong to throws.
    /// </remarks>
    public abstract class SoundHeader
    {
        /// <summary>The sound's sample rate, in Hz.</summary>
        public uint SampleRate { get; set; }

        /// <summary>
        /// The <see cref="AssetType.Sound"/>, <see cref="AssetType.StreamingSound"/>, or
        /// <see cref="AssetType.CutsceneStreamingSound"/> asset this header describes.
        /// </summary>
        public AssetId SoundAssetId { get; set; }

        internal static SoundHeader Read(EndianReader reader, FormatProfile profile) => profile.Platform switch
        {
            Platform.GameCube => DspHeader.Read(reader, profile),
            Platform.Xbox => WaveHeader.Read(reader, profile),
            Platform.PlayStation2 => VagHeader.Read(reader, profile),
            _ => throw new NotSupportedException($"{nameof(SoundHeader)}s can't be read on {profile.Platform}."),
        };

        internal static void Write(SoundHeader value, EndianWriter writer, FormatProfile profile)
        {
            switch (value, profile.Platform)
            {
                case (DspHeader dsp, Platform.GameCube): DspHeader.Write(dsp, writer, profile); break;
                case (WaveHeader wave, Platform.Xbox): WaveHeader.Write(wave, writer, profile); break;
                case (VagHeader vag, Platform.PlayStation2): VagHeader.Write(vag, writer, profile); break;
                default: throw new InvalidOperationException($"A {value.GetType().Name} can't be written on {profile.Platform}.");
            }
        }
    }
}
