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
/// Reads and writes the shared prefix every <see cref="IEntity"/> carries after its
/// <see cref="BaseAssetPrefix"/>.
/// </summary>
internal static class EntityAssetPrefix
{
    /// <summary>
    /// Reads the prefix from <paramref name="reader"/>'s current position into <paramref name="entity"/>.
    /// </summary>
    /// <param name="entity">The <see cref="IEntity"/> to populate.</param>
    /// <param name="reader">The reader to read from.</param>
    /// <param name="profile">
    /// The active <see cref="FormatProfile"/>, whose <see cref="FormatProfile.EntityHasPadding"/> and
    /// <see cref="FormatProfile.EntityHasExtendedFields"/> control this build's exact layout.
    /// </param>
    public static void Read(IEntity entity, EndianReader reader, FormatProfile profile)
    {
        entity.EntityFlags = (EntityFlags)reader.ReadByte();
        entity.Physical.Subtype = reader.ReadByte();
        entity.Physical.PFlags = reader.ReadByte();
        entity.Physical.CollisionFlags = (CollisionFlags)reader.ReadByte();

        // Read and discarded, never modelled - it is always zero where it exists.
        if (profile.EntityHasPadding) reader.ReadBytes(4);

        if (profile.EntityHasExtendedFields) entity.Physical.SurfaceId = reader.ReadAssetId();
        entity.Angle = reader.ReadVector3();
        entity.Position = reader.ReadVector3();
        entity.Scale = reader.ReadVector3();
        if (profile.EntityHasExtendedFields)
        {
            entity.ColorMultiplier = reader.ReadRgba();
            entity.Physical.SeeThroughSpeed = reader.ReadSingle();
        }
        entity.Physical.ModelId = reader.ReadAssetId();
        if (profile.EntityHasExtendedFields) entity.Physical.AnimListId = reader.ReadAssetId();
    }

    /// <summary>
    /// Writes <paramref name="entity"/>'s prefix to <paramref name="writer"/>.
    /// </summary>
    /// <param name="entity">The <see cref="IEntity"/> to read from.</param>
    /// <param name="writer">The writer to write to.</param>
    /// <param name="profile">
    /// The active <see cref="FormatProfile"/>, whose <see cref="FormatProfile.EntityHasPadding"/> and
    /// <see cref="FormatProfile.EntityHasExtendedFields"/> control this build's exact layout. Padding
    /// is written as zero where it applies.
    /// </param>
    public static void Write(IEntity entity, EndianWriter writer, FormatProfile profile)
    {
        writer.Write((byte)entity.EntityFlags);
        writer.Write(entity.Physical.Subtype);
        writer.Write(entity.Physical.PFlags);
        writer.Write((byte)entity.Physical.CollisionFlags);

        if (profile.EntityHasPadding) writer.Write(new byte[4]);

        if (profile.EntityHasExtendedFields) writer.Write(entity.Physical.SurfaceId);
        writer.Write(entity.Angle);
        writer.Write(entity.Position);
        writer.Write(entity.Scale);
        if (profile.EntityHasExtendedFields)
        {
            writer.Write(entity.ColorMultiplier);
            writer.Write(entity.Physical.SeeThroughSpeed);
        }
        writer.Write(entity.Physical.ModelId);
        if (profile.EntityHasExtendedFields) writer.Write(entity.Physical.AnimListId);
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
