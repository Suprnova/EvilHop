namespace EvilHop.Primitives;

/// <summary>
/// An RGBA color with normalized (0–1) red, green, blue, and alpha channels.
/// </summary>
/// <param name="R">The red channel.</param>
/// <param name="G">The green channel.</param>
/// <param name="B">The blue channel.</param>
/// <param name="A">The alpha channel.</param>
public readonly record struct Rgba(float R, float G, float B, float A);
