using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

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
    internal static FlyAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new FlyAsset();
        AssetFields.Populate(asset, header, debug);

        reader = LittleEndianReader(reader);
        while (reader.BaseStream.Length - reader.BaseStream.Position >= KeySize)
            asset.Keys.Add(FlyKey.Read(reader, profile));

        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "LittleEndianWriter's result never owns a resource worth disposing - see LittleEndianReader's remarks.")]
    internal static void Write(FlyAsset asset, EndianWriter writer, FormatProfile profile)
    {
        writer = LittleEndianWriter(writer);
        foreach (var key in asset.Keys)
            FlyKey.Write(key, writer, profile);

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
