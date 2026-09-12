using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;

namespace EvilHop.Assets;

/// <summary>
/// Maps models to the meshes used in their place for collision detection by other objects and the
/// camera.
/// </summary>
/// <remarks>
/// <para>
/// A model with no entry here uses itself as its own collision mesh against other objects, and has no
/// collision mesh against the camera.
/// </para>
/// <seealso href="https://heavyironmodding.org/wiki/COLL">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class CollisionTableAsset() : Asset(AssetType.CollisionTable)
{
    /// <summary>
    /// The table's entries.
    /// </summary>
    public Collection<CollisionTableEntry> Entries { get; } = [];

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.CollisionTable"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.BFBB,
        GameVersion.TSSM,
        GameVersion.Incredibles,
        GameVersion.ROTU,
        GameVersion.Ratatouille,
    };

    internal static CollisionTableAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile _)
    {
        var asset = new CollisionTableAsset();
        AssetFields.Populate(asset, header, debug);

        var count = reader.ReadUInt32();
        for (var i = 0; i < count; i++)
        {
            asset.Entries.Add(new CollisionTableEntry
            {
                ModelId = reader.ReadAssetId(),
                CollisionModelId = reader.ReadAssetId(),
                CameraCollisionModelId = reader.ReadAssetId(),
            });
        }

        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(CollisionTableAsset asset, EndianWriter writer, FormatProfile _)
    {
        writer.Write((uint)asset.Entries.Count);
        foreach (var entry in asset.Entries)
        {
            writer.Write(entry.ModelId);
            writer.Write(entry.CollisionModelId);
            writer.Write(entry.CameraCollisionModelId);
        }

        writer.Write(asset.GetUnparsedTail());
    }
}

/// <summary>
/// One <see cref="CollisionTableAsset"/> entry, overriding the collision meshes used against a single
/// <see cref="AssetType.Model"/>.
/// </summary>
public record struct CollisionTableEntry
{
    /// <summary>
    /// The <see cref="AssetId"/> of the <see cref="AssetType.Model"/> this entry applies to.
    /// </summary>
    public AssetId ModelId { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the <see cref="AssetType.Model"/> used for collision detection by
    /// other objects. If <see cref="AssetId.None"/>, <see cref="ModelId"/> is used instead.
    /// </summary>
    public AssetId CollisionModelId { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the <see cref="AssetType.Model"/> used for collision detection by
    /// the camera. If <see cref="AssetId.None"/>, the camera has no collision mesh against
    /// <see cref="ModelId"/> and can pass through it.
    /// </summary>
    public AssetId CameraCollisionModelId { get; set; }
}
