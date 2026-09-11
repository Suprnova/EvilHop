using EvilHop.Common;
using EvilHop.Primitives;
using System.Collections.Immutable;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="PlatformMotion"/> that rotates between a set of orientations when hit.
/// </summary>
/// <remarks>
/// Not present in <see cref="GameVersion.N100F"/>, whose type-specific block is too small to hold
/// one; written there as an empty block.
/// </remarks>
public sealed class PaddleMotion : PlatformMotion
{
    /// <summary>
    /// The most <see cref="Orientations"/> a paddle can have.
    /// </summary>
    public const int MaxOrientations = 6;

    /// <summary>
    /// The index into <see cref="Orientations"/> the paddle starts at.
    /// </summary>
    public int StartOrientation { get; set; }

    /// <summary>
    /// The yaw rotations, in degrees, the paddle can rest at. At most <see cref="MaxOrientations"/>
    /// elements.
    /// </summary>
    /// <exception cref="ArgumentException">The assigned value has more than <see cref="MaxOrientations"/> elements.</exception>
    public ImmutableArray<float> Orientations
    {
        get;
        set => field = value.Length <= MaxOrientations
            ? value
            : throw new ArgumentException($"{nameof(Orientations)} must contain at most {MaxOrientations} elements.", nameof(value));
    } = [];

    /// <summary>
    /// The orientation, in degrees, that stands in for the first of <see cref="Orientations"/> when
    /// wrapping around from the last, such as 360.
    /// </summary>
    public float OrientationLoop { get; set; }

    /// <summary>
    /// This paddle's flags.
    /// </summary>
    public PaddleFlags PaddleFlags { get; set; }

    /// <summary>The paddle's rotation speed.</summary>
    public float RotateSpeed { get; set; }

    /// <summary>The time, in seconds, to ease into a rotation.</summary>
    public float AccelTime { get; set; }

    /// <summary>The time, in seconds, to ease out of a rotation.</summary>
    public float DecelTime { get; set; }

    /// <summary>The radius of the paddle's hub.</summary>
    public float HubRadius { get; set; }

    internal override PlatformType PlatformType => PlatformType.Paddle;

    private protected override void ReadFields(EndianReader reader, GameVersion game)
    {
        if (game is GameVersion.N100F)
            throw new InvalidDataException($"{nameof(GameVersion.N100F)} has no paddle platforms.");

        StartOrientation = reader.ReadInt32();
        int count = reader.ReadInt32();
        OrientationLoop = reader.ReadSingle();
        float[] slots = [.. Enumerable.Range(0, MaxOrientations).Select(_ => reader.ReadSingle())];
        if (count is < 0 or > MaxOrientations)
            throw new InvalidDataException($"Paddle orientation count {count} is outside 0-{MaxOrientations}.");

        Orientations = [.. slots.Take(count)]; // unused slots are always zero
        PaddleFlags = (PaddleFlags)reader.ReadUInt32();
        RotateSpeed = reader.ReadSingle();
        AccelTime = reader.ReadSingle();
        DecelTime = reader.ReadSingle();
        HubRadius = reader.ReadSingle();
    }

    private protected override void WriteFields(EndianWriter writer, GameVersion game)
    {
        if (game is GameVersion.N100F) return;

        writer.Write(StartOrientation);
        writer.Write(Orientations.Length);
        writer.Write(OrientationLoop);
        foreach (float orientation in Orientations) writer.Write(orientation);
        writer.Write(new byte[sizeof(float) * (MaxOrientations - Orientations.Length)]);
        writer.Write((uint)PaddleFlags);
        writer.Write(RotateSpeed);
        writer.Write(AccelTime);
        writer.Write(DecelTime);
        writer.Write(HubRadius);
    }
}

/// <summary>
/// Represents all known values for <see cref="PaddleMotion.PaddleFlags"/>.
/// </summary>
[Flags]
public enum PaddleFlags : uint
{
    /// <summary>
    /// No flags are set.
    /// </summary>
    None = 0,
    /// <summary>
    /// The paddle can rotate forward, toward its next orientation. Per decompiled source.
    /// </summary>
    RotatesForward = 1 << 0,
    /// <summary>
    /// The paddle can rotate backward, toward its previous orientation. Per decompiled source.
    /// </summary>
    RotatesBackward = 1 << 1,
    /// <summary>
    /// The paddle wraps around between its last and first orientations. Per decompiled source.
    /// </summary>
    Wraps = 1 << 2,
    /// <summary>
    /// The Cruise Bubble can rotate the paddle. Per decompiled source.
    /// </summary>
    HitByCruiseBubble = 1 << 5,
}
