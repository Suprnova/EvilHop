namespace EvilHop.Assets;

/// <summary>
/// One <see cref="AnimationTableAsset"/> file: one or more of the table's <see cref="AnimationTableAsset.Raw"/>
/// animations, optionally blended together across a bilinear grid.
/// </summary>
public sealed class AnimationTableFile
{
    /// <summary>
    /// Playback and format flags for this file.
    /// </summary>
    public FileFlags FileFlags { get; set; }

    /// <summary>
    /// The playback duration, in seconds.
    /// </summary>
    public float Duration { get; set; }

    /// <summary>
    /// The time, in seconds, playback starts at within the underlying raw animation(s).
    /// </summary>
    public float TimeOffset { get; set; }

    /// <summary>
    /// The bilinear blend grid's width, in raw animations. 1 for a non-blended file.
    /// </summary>
    public ushort NumAnimsX { get; set; }

    /// <summary>
    /// The bilinear blend grid's height, in raw animations. 1 for a non-blended file.
    /// </summary>
    public ushort NumAnimsY { get; set; }

    /// <summary>
    /// An internal byte offset, from the start of the owning <see cref="AnimationTableAsset"/>'s data,
    /// to this file's <see cref="NumAnimsX"/>*<see cref="NumAnimsY"/> indices into
    /// <see cref="AnimationTableAsset.Raw"/>. Not individually resolved; preserved via
    /// <see cref="Asset.GetUnparsedTail"/>.
    /// </summary>
    public uint RawDataOffset { get; set; }

    /// <summary>
    /// Unknown. Usually -1.
    /// </summary>
    public int Physics { get; set; }

    /// <summary>
    /// Unknown. Usually -1.
    /// </summary>
    public int StartPose { get; set; }

    /// <summary>
    /// Unknown. Usually -1.
    /// </summary>
    public int EndPose { get; set; }
}

/// <summary>
/// Flags governing the playback and blending of an <see cref="AnimationTableFile"/>.
/// </summary>
[Flags]
public enum FileFlags : uint
{
    /// <summary>No flags are set.</summary>
    None = 0,

    /// <summary>Plays the animation in reverse.</summary>
    Reverse = 1 << 12,

    /// <summary>Doubles the duration and plays the second half in reverse.</summary>
    ReverseSecondHalf = 1 << 13,

    /// <summary>Marks this file's blend dimensions as an active bilinear blend grid.</summary>
    Bilinear = 1 << 14,

    /// <summary>Marks this as vertex/morph animation data rather than skeletal.</summary>
    Morph = 1 << 15,
}
