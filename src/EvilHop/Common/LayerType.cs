namespace EvilHop.Common;

#pragma warning disable CS1591 // Missing XML comment

/// <summary>
/// Specifies the type of a <c>Layer</c>.
/// </summary>
/// <remarks>
/// Backed by a uint field that maps to <see cref="Blocks.LayerHeader.Type"/>.
/// </remarks>
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
