namespace EvilHop.Common;

#pragma warning disable CS1591 // Missing XML comment

/// <summary>
/// Specifies the type of a <c>Layer</c>.
/// </summary>
/// <remarks>
/// Backed by a uint field that maps to <see cref="Blocks.LayerHeader.Type"/>.
/// </remarks>
/// TODO: wrong mapping types for N100F (no TextureStream, no JSPInfo) and BFBB (no TextureStream).
public enum LayerType : uint
{
    Default = 0,
    Texture,
    TextureStream,
    BSP,
    Model,
    Animation,
    VRAM,
    SRAM,
    SoundTable,
    Cutscene,
    CutsceneTable,
    JSPInfo
}
