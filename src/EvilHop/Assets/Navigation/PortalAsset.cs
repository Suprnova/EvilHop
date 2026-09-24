using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Buffers.Binary;
using System.Text;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="BaseAsset"/> that, when sent a <b>TeleportPlayer</b> event, moves the player to
/// <see cref="MarkerId"/> in the scene <see cref="SceneId"/> names, loading that scene first if the
/// player isn't already in it.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/PORT">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class PortalAsset() : BaseAsset(AssetType.Portal, baseType: 0x10)
{
    /// <summary>
    /// The <see cref="AssetId"/> of the <see cref="AssetType.Camera"/> the player's view starts from on
    /// arrival, if it can be found.
    /// </summary>
    public AssetId CameraId { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the asset whose position the player arrives at - a
    /// <see cref="AssetType.Marker"/>, or from <see cref="GameVersion.TSSM"/> onward usually a
    /// <see cref="AssetType.Dynamic"/>. If it can't be found, the player isn't moved.
    /// </summary>
    public AssetId MarkerId { get; set; }

    /// <summary>
    /// The direction, in degrees, the player faces on arrival. 1000 or more instead keeps the
    /// player's current facing, and moves the camera along with them.
    /// </summary>
    /// <remarks>
    /// In <see cref="GameVersion.ROTU"/> and <see cref="GameVersion.Ratatouille"/>, most portals store
    /// uninitialized memory (<c>0xCDCDCDCA</c>) here.
    /// </remarks>
    public float Angle { get; set; }

    /// <summary>
    /// The four-character ID of the scene to warp to, such as <c>HB01</c>.
    /// </summary>
    /// <remarks>
    /// When it matches the current scene's ID, the player is teleported without a loading screen. The
    /// comparison is case-sensitive while the scene itself loads case-insensitively, so a lowercase ID
    /// (<c>hb01</c>) reloads the current scene instead.
    /// </remarks>
    public string SceneId { get; set; } = "HB01";

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.Portal"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.N100F,
        GameVersion.BFBB,
        GameVersion.TSSM,
        GameVersion.Incredibles,
        GameVersion.ROTU,
        GameVersion.Ratatouille,
    };

    internal static PortalAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new PortalAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        asset.CameraId = reader.ReadAssetId();
        asset.MarkerId = reader.ReadAssetId();
        asset.Angle = reader.ReadSingle();
        asset.SceneId = ReadSceneId(reader, profile);

        for (var i = 0; i < asset.Physical.LinkCount; i++)
            asset.Links.Add(Link.Read(reader, profile));
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    /// <exception cref="ArgumentException"><see cref="SceneId"/> isn't exactly four characters.</exception>
    internal static void Write(PortalAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);

        writer.Write(asset.CameraId);
        writer.Write(asset.MarkerId);
        writer.Write(asset.Angle);
        WriteSceneId(asset.SceneId, writer, profile);

        foreach (var link in asset.Links)
            Link.Write(link, writer, profile);
        writer.Write(asset.GetUnparsedTail());
    }

    /// <summary>
    /// Reads the scene ID's integer and spells it out as four characters.
    /// </summary>
    private static string ReadSceneId(EndianReader reader, FormatProfile profile)
    {
        uint value = reader.ReadUInt32();
        // N100F and BFBB store the ID byte-reversed; the game swaps it back whenever its last
        // character isn't a digit.
        if (StoresSceneIdReversed(profile)) value = BinaryPrimitives.ReverseEndianness(value);

        byte[] characters = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(characters, value);
        return Encoding.Latin1.GetString(characters);
    }

    /// <exception cref="ArgumentException"><paramref name="sceneId"/> isn't exactly four characters.</exception>
    private static void WriteSceneId(string sceneId, EndianWriter writer, FormatProfile profile)
    {
        byte[] characters = Encoding.Latin1.GetBytes(sceneId);
        if (characters.Length != 4)
            throw new ArgumentException($"Must be exactly 4 characters, but was '{sceneId}'.", nameof(sceneId));

        uint value = BinaryPrimitives.ReadUInt32BigEndian(characters);
        if (StoresSceneIdReversed(profile)) value = BinaryPrimitives.ReverseEndianness(value);
        writer.Write(value);
    }

    private static bool StoresSceneIdReversed(FormatProfile profile) =>
        profile.Game is GameVersion.N100F or GameVersion.BFBB;
}
