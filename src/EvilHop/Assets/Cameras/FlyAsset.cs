using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>
/// A recorded camera path made up of one <see cref="FlyKey"/> per frame at 30 FPS, each carrying the
/// camera's transform, aperture, and focal length. Drives <c>game_object:Flythrough</c>, the in-game
/// object that overrides the active camera to play the recording back.
/// </summary>
/// <remarks>
/// Has no header of its own - the payload is nothing but contiguous <see cref="FlyKey"/> entries, and
/// its <see cref="Keys"/> count derives entirely from the asset's size.
/// <seealso href="https://heavyironmodding.org/wiki/FLY">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class FlyAsset() : Asset(AssetType.Fly)
{
    /// <summary>The recorded keyframes, one per frame of the flythrough.</summary>
    public Collection<FlyKey> Keys { get; } = [];

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.Fly"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.BFBB,
        GameVersion.TSSM,
        GameVersion.Incredibles,
        GameVersion.Ratatouille,
    };

    /// <summary>The on-disk size, in bytes, of one <see cref="FlyKey"/>.</summary>
    private const int KeySize = 64;

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "LittleEndianReader's result never owns a resource worth disposing - see its remarks.")]
    internal static FlyAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile _)
    {
        var asset = new FlyAsset();
        AssetFields.Populate(asset, header, debug);

        reader = LittleEndianReader(reader);
        while (reader.BaseStream.Length - reader.BaseStream.Position >= KeySize)
        {
            asset.Keys.Add(new FlyKey
            {
                Frame = reader.ReadInt32(),
                Right = reader.ReadVector3(),
                Up = reader.ReadVector3(),
                At = reader.ReadVector3(),
                Position = reader.ReadVector3(),
                Aperture = new Vector2(reader.ReadSingle(), reader.ReadSingle()),
                FocalLength = reader.ReadSingle(),
            });
        }

        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "LittleEndianWriter's result never owns a resource worth disposing - see LittleEndianReader's remarks.")]
    internal static void Write(FlyAsset asset, EndianWriter writer, FormatProfile _)
    {
        writer = LittleEndianWriter(writer);
        foreach (var key in asset.Keys)
        {
            writer.Write(key.Frame);
            writer.Write(key.Right);
            writer.Write(key.Up);
            writer.Write(key.At);
            writer.Write(key.Position);
            writer.Write(key.Aperture.X);
            writer.Write(key.Aperture.Y);
            writer.Write(key.FocalLength);
        }

        writer.Write(asset.GetUnparsedTail());
    }

    /// <summary>
    /// Wraps <paramref name="reader"/> to force <see cref="Endianness.Little"/> - unlike every other
    /// asset type, a <see cref="FlyAsset"/>'s <see cref="FlyKey"/> entries are written little-endian on
    /// every platform, including GameCube.
    /// </summary>
    /// <remarks>
    /// Never disposed: it shares <paramref name="reader"/>'s underlying stream with
    /// <c>leaveOpen: true</c> and owns nothing else, so there is nothing for a missing
    /// <see cref="IDisposable.Dispose"/> call to leak.
    /// </remarks>
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Thin, non-owning wrapper over the caller's own stream - see remarks.")]
    private static EndianReader LittleEndianReader(EndianReader reader) =>
        reader.Endianness == Endianness.Little
            ? reader
            : new EndianReader(reader.BaseStream, Endianness.Little, leaveOpen: true);

    /// <summary>See <see cref="LittleEndianReader"/>.</summary>
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Thin, non-owning wrapper over the caller's own stream - see LittleEndianReader.")]
    private static EndianWriter LittleEndianWriter(EndianWriter writer) =>
        writer.Endianness == Endianness.Little
            ? writer
            : new EndianWriter(writer.BaseStream, Endianness.Little, leaveOpen: true);
}

/// <summary>
/// One frame of a <see cref="FlyAsset"/>: the camera's transform, aperture, and focal length at a
/// given frame.
/// </summary>
public sealed class FlyKey
{
    /// <summary>The frame number this key applies at, at 30 FPS.</summary>
    public int Frame { get; set; }

    /// <summary>The camera's normalized right vector.</summary>
    public Vector3 Right { get; set; }

    /// <summary>The camera's normalized up vector.</summary>
    public Vector3 Up { get; set; }

    /// <summary>The camera's normalized forward vector.</summary>
    public Vector3 At { get; set; }

    /// <summary>The camera's position.</summary>
    public Vector3 Position { get; set; }

    /// <summary>The camera's aperture (view window half-extents).</summary>
    public Vector2 Aperture { get; set; }

    /// <summary>The camera's focal length.</summary>
    public float FocalLength { get; set; }
}
