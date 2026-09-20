using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets.Serialization;

/// <summary>
/// Reads and writes the fixed 8-byte header every <see cref="BaseAsset"/> begins with.
/// </summary>
/// <remarks>
/// Deliberately does not touch <see cref="BaseAsset.Links"/>. Links do not sit at a fixed offset -
/// a concrete type's own fields can precede and follow them - so locating them is a codec's job,
/// not this helper's.
/// </remarks>
internal static class BaseAssetPrefix
{
    /// <summary>
    /// Reads the prefix from <paramref name="reader"/>'s current position into <paramref name="asset"/>.
    /// </summary>
    public static void Read(BaseAsset asset, EndianReader reader)
    {
        asset.Physical.BaseId = reader.ReadAssetId();
        asset.Physical.BaseType = reader.ReadByte();
        asset.Physical.LinkCount = reader.ReadByte();
        asset.BaseFlags = (BaseAssetFlags)reader.ReadInt16();
    }

    /// <summary>
    /// Writes <paramref name="asset"/>'s prefix to <paramref name="writer"/>.
    /// </summary>
    public static void Write(BaseAsset asset, EndianWriter writer)
    {
        writer.Write(asset.Physical.BaseId);
        writer.Write(asset.Physical.BaseType);
        writer.Write(asset.Physical.LinkCount);
        writer.Write((short)asset.BaseFlags);
    }
}

/// <summary>
/// Reads and writes the shared prefix every <see cref="EntityAsset"/> carries after its
/// <see cref="BaseAssetPrefix"/>.
/// </summary>
internal static class EntityAssetPrefix
{
    /// <summary>
    /// Reads the prefix from <paramref name="reader"/>'s current position into <paramref name="asset"/>.
    /// </summary>
    /// <param name="asset">The <see cref="EntityAsset"/> to populate.</param>
    /// <param name="reader">The reader to read from.</param>
    /// <param name="profile">
    /// The active <see cref="FormatProfile"/>, whose <see cref="FormatProfile.EntityHasPadding"/> and
    /// <see cref="FormatProfile.EntityHasExtendedFields"/> control this build's exact layout.
    /// </param>
    public static void Read(EntityAsset asset, EndianReader reader, FormatProfile profile)
    {
        asset.EntityFlags = (EntityFlags)reader.ReadByte();
        asset.Physical.Subtype = reader.ReadByte();
        asset.Physical.PFlags = reader.ReadByte();
        asset.Physical.CollisionFlags = (CollisionFlags)reader.ReadByte();

        // Read and discarded, never modelled - it is always zero where it exists.
        if (profile.EntityHasPadding) reader.ReadBytes(4);

        if (profile.EntityHasExtendedFields) asset.Physical.SurfaceId = reader.ReadAssetId();
        asset.Angle = reader.ReadVector3();
        asset.Position = reader.ReadVector3();
        asset.Scale = reader.ReadVector3();
        if (profile.EntityHasExtendedFields)
        {
            asset.ColorMultiplier = reader.ReadRgba();
            asset.Physical.SeeThroughSpeed = reader.ReadSingle();
        }
        asset.Physical.ModelId = reader.ReadAssetId();
        if (profile.EntityHasExtendedFields) asset.Physical.AnimListId = reader.ReadAssetId();
    }

    /// <summary>
    /// Writes <paramref name="asset"/>'s prefix to <paramref name="writer"/>.
    /// </summary>
    /// <param name="asset">The <see cref="EntityAsset"/> to read from.</param>
    /// <param name="writer">The writer to write to.</param>
    /// <param name="profile">
    /// The active <see cref="FormatProfile"/>, whose <see cref="FormatProfile.EntityHasPadding"/> and
    /// <see cref="FormatProfile.EntityHasExtendedFields"/> control this build's exact layout. Padding
    /// is written as zero where it applies.
    /// </param>
    public static void Write(EntityAsset asset, EndianWriter writer, FormatProfile profile)
    {
        writer.Write((byte)asset.EntityFlags);
        writer.Write(asset.Physical.Subtype);
        writer.Write(asset.Physical.PFlags);
        writer.Write((byte)asset.Physical.CollisionFlags);

        if (profile.EntityHasPadding) writer.Write(new byte[4]);

        if (profile.EntityHasExtendedFields) writer.Write(asset.Physical.SurfaceId);
        writer.Write(asset.Angle);
        writer.Write(asset.Position);
        writer.Write(asset.Scale);
        if (profile.EntityHasExtendedFields)
        {
            writer.Write(asset.ColorMultiplier);
            writer.Write(asset.Physical.SeeThroughSpeed);
        }
        writer.Write(asset.Physical.ModelId);
        if (profile.EntityHasExtendedFields) writer.Write(asset.Physical.AnimListId);
    }
}

/// <summary>
/// Reads and writes the prefix every <see cref="DynaAsset"/> carries after its
/// <see cref="BaseAssetPrefix"/>.
/// </summary>
internal static class DynaAssetPrefix
{
    /// <summary>
    /// Reads the prefix from <paramref name="reader"/>'s current position into <paramref name="asset"/>.
    /// </summary>
    public static void Read(DynaAsset asset, EndianReader reader)
    {
        asset.Physical.DynaType = reader.ReadUInt32();
        asset.Physical.Version = reader.ReadInt16();
        asset.Physical.Handle = reader.ReadInt16();
    }

    /// <summary>
    /// Writes <paramref name="asset"/>'s prefix to <paramref name="writer"/>.
    /// </summary>
    public static void Write(DynaAsset asset, EndianWriter writer)
    {
        writer.Write(asset.Physical.DynaType);
        writer.Write(asset.Physical.Version);
        writer.Write(asset.Physical.Handle);
    }
}
