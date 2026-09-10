using EvilHop.Common;
using EvilHop.Serialization;

namespace EvilHop.Blocks;

/// <summary>
/// An unofficial root <see cref="Block"/> appended after every other root block by HipHopFile, one
/// of the community's first HIP-parsing libraries.
/// </summary>
/// <remarks>
/// No official archive carries one, and EvilHop never writes one on its own initiative - it exists
/// purely so archives that were edited by a HipHopFile-based tool round-trip byte-exactly.
/// <see cref="Version"/> gates which of the fields below are present, both on disk and on this
/// block; each field's remarks note the version it was introduced in. Nothing else in an archive
/// depends on this block's content, so a malformed one - an unrecognized version, a truncated or
/// corrupt field - degrades instead of failing the whole archive to load; see
/// <see cref="Serializer.ReadHIPB"/>.
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/HIP_(File_Format)">Heavy Iron Modding documentation</seealso>
/// </remarks>
/// Validation TODO: No children.
public class HIPB : Block
{
    /// <inheritdoc/>
    protected internal override string Tag => "HIPB";

    /// <summary>
    /// The version of this block's layout, as understood by the last HipHopFile release EvilHop has
    /// tracked (currently 3). A higher version is read on a best-effort basis rather than rejected.
    /// </summary>
    public uint Version { get; set; } = 3;

    /// <summary>
    /// Whether the archive's editor should treat the archive as having no layers, flattening every
    /// asset into a single implicit layer instead of respecting <c>LTOC</c>. Present from version 1.
    /// </summary>
    public uint HasNoLayers { get; set; }

    /// <summary>
    /// The platform this archive targets. Present from version 2.
    /// </summary>
    public HIPBPlatform Platform { get; set; }

    /// <summary>
    /// Custom names for layers, keyed by their index into <c>LTOC</c>. Present from version 2.
    /// </summary>
    public Dictionary<int, string> LayerNames { get; } = [];

    /// <summary>
    /// The game this archive targets. Present from version 3.
    /// </summary>
    public HIPBGame Game { get; set; }

    internal HIPB() { }
}

#pragma warning disable CS1591 // Missing XML comment

/// <summary>
/// The platform identifier <see cref="HIPB.Platform"/> stores. A distinct value set from
/// <see cref="Platform"/> - not to be confused with it.
/// </summary>
public enum HIPBPlatform
{
    Unknown,
    PlayStation2,
    GameCube,
    Xbox
}

/// <summary>
/// The game identifier <see cref="HIPB.Game"/> stores. A distinct value set from
/// <see cref="GameVersion"/> - not to be confused with it.
/// </summary>
public enum HIPBGame
{
    Unknown,
    N100F,
    BFBB,
    Incredibles,
    ROTU,
    Ratatouille
}
