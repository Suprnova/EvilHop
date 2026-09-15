namespace EvilHop.Primitives;

/// <summary>
/// An RGB color with normalized (0–1) red, green, and blue channels.
/// </summary>
/// <param name="R">The red channel.</param>
/// <param name="G">The green channel.</param>
/// <param name="B">The blue channel.</param>
public readonly record struct Rgb(float R, float G, float B);
