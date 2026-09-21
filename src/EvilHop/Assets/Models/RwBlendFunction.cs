namespace EvilHop.Assets;

/// <summary>
/// Defines the RenderWare blend function applied to source or destination pixels.
/// </summary>
/// <remarks>
/// Shared by <see cref="PipeInfoTableAsset.PipeRenderFlags"/> and
/// <see cref="ParticleSystemAsset"/>, so it stays top-level rather than nesting into either.
/// </remarks>
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
