using EvilHop.Common;

namespace EvilHop.Assets;

public partial class PipeInfoTableAsset
{
    /// <summary>
    /// Packed rendering flags for a <see cref="Entry"/>. Wraps the raw 32-bit value so bits with
    /// no known meaning round-trip untouched alongside the named fields.
    /// </summary>
    public readonly record struct RenderBehavior(uint Value)
    {
        /// <summary>
        /// The minimum alpha (0-255) a pixel must have to be rendered; atomics with transparent textures
        /// discard any pixel at or below this value. 0 renders every pixel regardless of alpha.
        /// </summary>
        /// <remarks>
        /// Only read by <see cref="GameVersion.BFBB"/>. Later games don't read this byte as an alpha
        /// threshold, and its lowest bit is <see cref="DualUVTransform"/>.
        /// </remarks>
        public byte AlphaCompare => (byte)(Value >> 24);

        /// <summary>
        /// If set, the selected atomics' materials are given RenderWare's dual-pass UV-transform
        /// material effect when the scene loads, for <see cref="SurfaceAsset"/> UV animation that
        /// also drives a second texture pass. Takes precedence over <see cref="UVTransform"/>.
        /// </summary>
        /// <remarks>
        /// Read from <see cref="GameVersion.TSSM"/> onward.
        /// </remarks>
        public bool DualUVTransform => (Value & (1u << 24)) != 0;

        /// <summary>
        /// If set, the selected atomics' materials are given RenderWare's UV-transform material effect
        /// when the scene loads. A <see cref="SurfaceAsset"/>'s <see cref="SurfaceAsset.Uvfxs"/> only
        /// animates materials that have this effect (or <see cref="DualUVTransform"/>'s), so a model
        /// without either shows no UV animation.
        /// </summary>
        /// <remarks>
        /// Read from <see cref="GameVersion.TSSM"/> onward. In <see cref="GameVersion.BFBB"/>, this bit
        /// belongs to the alpha rendering layer instead.
        /// </remarks>
        public bool UVTransform => (Value & (1u << 23)) != 0;

        /// <summary>If set, the selected atomics are rendered without fog.</summary>
        public bool IgnoreFog => (Value & (1u << 16)) != 0;

        /// <summary>The destination <see cref="RwBlendFunction"/> used when blending the selected atomics.</summary>
        public RwBlendFunction DestinationBlend => (RwBlendFunction)((Value >> 12) & 0xF);

        /// <summary>The source <see cref="RwBlendFunction"/> used when blending the selected atomics.</summary>
        public RwBlendFunction SourceBlend => (RwBlendFunction)((Value >> 8) & 0xF);

        /// <summary>How the selected atomics are lit.</summary>
        public LightingMode LightingMode => (LightingMode)((Value >> 6) & 0x3);

        /// <summary>Which faces of the selected atomics are culled.</summary>
        public CullMode CullMode => (CullMode)((Value >> 4) & 0x3);

        /// <summary>How the selected atomics write to the z-buffer.</summary>
        public ZWriteMode ZWriteMode => (ZWriteMode)((Value >> 2) & 0x3);

        /// <summary>Returns a copy with <see cref="AlphaCompare"/> replaced, every other bit unchanged.</summary>
        public RenderBehavior WithAlphaCompare(byte value) => new((Value & 0x00FFFFFFu) | ((uint)value << 24));

        /// <summary>Returns a copy with <see cref="DualUVTransform"/> replaced, every other bit unchanged.</summary>
        public RenderBehavior WithDualUVTransform(bool value) => new(value ? Value | (1u << 24) : Value & ~(1u << 24));

        /// <summary>Returns a copy with <see cref="UVTransform"/> replaced, every other bit unchanged.</summary>
        public RenderBehavior WithUVTransform(bool value) => new(value ? Value | (1u << 23) : Value & ~(1u << 23));

        /// <summary>Returns a copy with <see cref="IgnoreFog"/> replaced, every other bit unchanged.</summary>
        public RenderBehavior WithIgnoreFog(bool value) => new(value ? Value | (1u << 16) : Value & ~(1u << 16));

        /// <summary>Returns a copy with <see cref="DestinationBlend"/> replaced, every other bit unchanged.</summary>
        public RenderBehavior WithDestinationBlend(RwBlendFunction value) => new((Value & ~(0xFu << 12)) | ((uint)value << 12));

        /// <summary>Returns a copy with <see cref="SourceBlend"/> replaced, every other bit unchanged.</summary>
        public RenderBehavior WithSourceBlend(RwBlendFunction value) => new((Value & ~(0xFu << 8)) | ((uint)value << 8));

        /// <summary>Returns a copy with <see cref="LightingMode"/> replaced, every other bit unchanged.</summary>
        public RenderBehavior WithLightingMode(LightingMode value) => new((Value & ~(0x3u << 6)) | ((uint)value << 6));

        /// <summary>Returns a copy with <see cref="CullMode"/> replaced, every other bit unchanged.</summary>
        public RenderBehavior WithCullMode(CullMode value) => new((Value & ~(0x3u << 4)) | ((uint)value << 4));

        /// <summary>Returns a copy with <see cref="ZWriteMode"/> replaced, every other bit unchanged.</summary>
        public RenderBehavior WithZWriteMode(ZWriteMode value) => new((Value & ~(0x3u << 2)) | ((uint)value << 2));
    }
}
