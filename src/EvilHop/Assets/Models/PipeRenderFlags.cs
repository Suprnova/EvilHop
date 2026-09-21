namespace EvilHop.Assets;

/// <summary>
/// Packed rendering flags for a <see cref="PipeInfoEntry"/>. Wraps the raw 32-bit value so bits with
/// no known meaning round-trip untouched alongside the named fields.
/// </summary>
public readonly record struct PipeRenderFlags(uint Value)
{
    /// <summary>
    /// The minimum alpha (0-255) a pixel must have to be rendered; atomics with transparent textures
    /// discard any pixel at or below this value. 0 renders every pixel regardless of alpha.
    /// </summary>
    public byte AlphaCompare => (byte)(Value >> 24);

    /// <summary>If set, the selected atomics are rendered without fog.</summary>
    public bool IgnoreFog => (Value & (1u << 16)) != 0;

    /// <summary>The destination <see cref="RwBlendFunction"/> used when blending the selected atomics.</summary>
    public RwBlendFunction DestinationBlend => (RwBlendFunction)((Value >> 12) & 0xF);

    /// <summary>The source <see cref="RwBlendFunction"/> used when blending the selected atomics.</summary>
    public RwBlendFunction SourceBlend => (RwBlendFunction)((Value >> 8) & 0xF);

    /// <summary>How the selected atomics are lit.</summary>
    public PipeLightingMode LightingMode => (PipeLightingMode)((Value >> 6) & 0x3);

    /// <summary>Which faces of the selected atomics are culled.</summary>
    public PipeCullMode CullMode => (PipeCullMode)((Value >> 4) & 0x3);

    /// <summary>How the selected atomics write to the z-buffer.</summary>
    public PipeZWriteMode ZWriteMode => (PipeZWriteMode)((Value >> 2) & 0x3);

    /// <summary>Returns a copy with <see cref="AlphaCompare"/> replaced, every other bit unchanged.</summary>
    public PipeRenderFlags WithAlphaCompare(byte value) => new((Value & 0x00FFFFFFu) | ((uint)value << 24));

    /// <summary>Returns a copy with <see cref="IgnoreFog"/> replaced, every other bit unchanged.</summary>
    public PipeRenderFlags WithIgnoreFog(bool value) => new(value ? Value | (1u << 16) : Value & ~(1u << 16));

    /// <summary>Returns a copy with <see cref="DestinationBlend"/> replaced, every other bit unchanged.</summary>
    public PipeRenderFlags WithDestinationBlend(RwBlendFunction value) => new((Value & ~(0xFu << 12)) | ((uint)value << 12));

    /// <summary>Returns a copy with <see cref="SourceBlend"/> replaced, every other bit unchanged.</summary>
    public PipeRenderFlags WithSourceBlend(RwBlendFunction value) => new((Value & ~(0xFu << 8)) | ((uint)value << 8));

    /// <summary>Returns a copy with <see cref="LightingMode"/> replaced, every other bit unchanged.</summary>
    public PipeRenderFlags WithLightingMode(PipeLightingMode value) => new((Value & ~(0x3u << 6)) | ((uint)value << 6));

    /// <summary>Returns a copy with <see cref="CullMode"/> replaced, every other bit unchanged.</summary>
    public PipeRenderFlags WithCullMode(PipeCullMode value) => new((Value & ~(0x3u << 4)) | ((uint)value << 4));

    /// <summary>Returns a copy with <see cref="ZWriteMode"/> replaced, every other bit unchanged.</summary>
    public PipeRenderFlags WithZWriteMode(PipeZWriteMode value) => new((Value & ~(0x3u << 2)) | ((uint)value << 2));
}

/// <summary>
/// Defines the RenderWare blend function applied to source or destination pixels.
/// </summary>
public enum RwBlendFunction : byte
{
    /// <summary>No blending.</summary>
    None = 0,
    /// <summary>Zero.</summary>
    Zero = 1,
    /// <summary>One.</summary>
    One = 2,
    /// <summary>Source color.</summary>
    SourceColor = 3,
    /// <summary>Inverse source color.</summary>
    InverseSourceColor = 4,
    /// <summary>Source alpha.</summary>
    SourceAlpha = 5,
    /// <summary>Inverse source alpha.</summary>
    InverseSourceAlpha = 6,
    /// <summary>Destination alpha.</summary>
    DestinationAlpha = 7,
    /// <summary>Inverse destination alpha.</summary>
    InverseDestinationAlpha = 8,
    /// <summary>Destination color.</summary>
    DestinationColor = 9,
    /// <summary>Inverse destination color.</summary>
    InverseDestinationColor = 10,
    /// <summary>Source alpha, saturated.</summary>
    SourceAlphaSaturated = 11,
}

/// <summary>
/// Defines the lighting mode applied to a model's selected atomics.
/// </summary>
public enum PipeLightingMode : byte
{
    /// <summary>Lit by the level's light kit only.</summary>
    LightKitOnly = 0,
    /// <summary>Lit by prebaked vertex lighting only.</summary>
    PrelightOnly = 1,
    /// <summary>Lit by both the level's light kit and prebaked vertex lighting.</summary>
    LightKitAndPrelight = 2,
    /// <summary>Unknown.</summary>
    Unknown = 3,
}

/// <summary>
/// Defines the face culling mode applied to a model's selected atomics.
/// </summary>
public enum PipeCullMode : byte
{
    /// <summary>Unknown.</summary>
    Unknown = 0,
    /// <summary>No culling; both front and back faces are rendered.</summary>
    None = 1,
    /// <summary>Back-face culling.</summary>
    Back = 2,
    /// <summary>Rendered twice: once with front-face culling, then once with back-face culling.</summary>
    Dual = 3,
}

/// <summary>
/// Defines the depth-buffer write behavior applied to a model's selected atomics.
/// </summary>
public enum PipeZWriteMode : byte
{
    /// <summary>Z-write is enabled.</summary>
    Enabled = 0,
    /// <summary>Z-write is disabled.</summary>
    Disabled = 1,
    /// <summary>Rendered twice: once with z-write disabled, then once with z-write enabled.</summary>
    Dual = 2,
    /// <summary>Unknown.</summary>
    Unknown = 3,
}
