using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>
/// An electric arc generated from this asset's position to an <see cref="AssetType.MovePoint"/>.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EGEN">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class ElectricArcGeneratorAsset() : EntityAsset(AssetType.ElectricArcGenerator, baseType: 0x29), IHasModel, IHasSurface
{
    /// <summary>
    /// The position, relative to this asset's own transform, the arc originates from.
    /// </summary>
    public Vector3 SourceOffset { get; set; }

    /// <summary>
    /// The type of damage dealt by the arc's contact.
    /// </summary>
    public byte DamageType { get; set; }

    /// <summary>
    /// Behavior flags for this <see cref="ElectricArcGeneratorAsset"/>.
    /// </summary>
    public ElectricArcGeneratorFlags Flags { get; set; }

    /// <summary>
    /// How long the arc stays active, in seconds, once turned on.
    /// </summary>
    public float ActiveTime { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the <see cref="AssetType.Animation"/> played while the arc is
    /// active, if any.
    /// </summary>
    public AssetId OnAnimationId { get; set; }

    AssetId IHasModel.ModelId { get => Physical.ModelId; set => Physical.ModelId = value; }
    AssetId IHasSurface.SurfaceId { get => Physical.SurfaceId; set => Physical.SurfaceId = value; }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.ElectricArcGenerator"/> is known to be
    /// read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.N100F,
        GameVersion.BFBB,
        GameVersion.TSSM,
    };

    internal static ElectricArcGeneratorAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new ElectricArcGeneratorAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);
        EntityAssetPrefix.Read(asset, reader, profile);

        asset.SourceOffset = reader.ReadVector3();
        asset.DamageType = reader.ReadByte();
        asset.Flags = (ElectricArcGeneratorFlags)reader.ReadByte();
        reader.ReadInt16(); // 2 bytes of padding, always zero
        asset.ActiveTime = reader.ReadSingle();
        asset.OnAnimationId = reader.ReadAssetId();

        for (var i = 0; i < asset.Physical.LinkCount; i++)
            asset.Links.Add(Link.Read(reader, profile));
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(ElectricArcGeneratorAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);
        EntityAssetPrefix.Write(asset, writer, profile);

        writer.Write(asset.SourceOffset);
        writer.Write(asset.DamageType);
        writer.Write((byte)asset.Flags);
        writer.Write((short)0); // padding
        writer.Write(asset.ActiveTime);
        writer.Write(asset.OnAnimationId);

        foreach (var link in asset.Links)
            Link.Write(link, writer, profile);
        writer.Write(asset.GetUnparsedTail());
    }

    /// <summary>
    /// Flags controlling the activation, visual style, and damage behavior of an electric arc generator.
    /// </summary>
    [Flags]
    public enum ElectricArcGeneratorFlags : byte
    {
        /// <summary>
        /// No flags are set.
        /// </summary>
        None = 0,
        /// <summary>
        /// The arc starts active instead of waiting for an <b>On</b> event.
        /// </summary>
        StartsOn = 1 << 0,
    }
}
