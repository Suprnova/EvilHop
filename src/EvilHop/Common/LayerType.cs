namespace EvilHop.Common;

#pragma warning disable CS1591 // Missing XML comment

/// <summary>
/// Specifies the type of a <c>Layer</c>.
/// </summary>
/// <remarks>
/// Backed by a uint field that maps to <see cref="Blocks.LayerHeader.Type"/>. These values are the
/// on-disk numbering for every game from TSSM on. N100F and BFBB have special handling for these
/// values in <see cref="Serialization.Serializer"/>.
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
