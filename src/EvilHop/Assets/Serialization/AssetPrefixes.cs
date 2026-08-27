using EvilHop.Primitives;
using System.Buffers.Binary;
using System.Numerics;

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
    /// <summary>The prefix's length in bytes.</summary>
    public const int Length = 8;

    /// <summary>
    /// Reads the prefix from the start of <paramref name="data"/> into <paramref name="asset"/>.
    /// </summary>
    /// <returns>The offset immediately past the prefix.</returns>
    public static int Read(BaseAsset asset, ReadOnlySpan<byte> data)
    {
        asset.Physical.BaseId = new AssetId(BinaryPrimitives.ReadUInt32BigEndian(data));
        asset.Physical.BaseType = data[4];
        asset.Physical.LinkCount = data[5];
        asset.BaseFlags = (BaseAssetFlags)BinaryPrimitives.ReadInt16BigEndian(data[6..]);
        return Length;
    }

    /// <summary>
    /// Writes <paramref name="asset"/>'s prefix to <paramref name="writer"/>.
    /// </summary>
    public static void Write(BaseAsset asset, BinaryWriter writer)
    {
        writer.WriteEvilInt(asset.Physical.BaseId.Value);
        writer.Write(asset.Physical.BaseType);
        writer.Write(asset.Physical.LinkCount);
        writer.WriteBigEndian((short)asset.BaseFlags);
    }
}

/// <summary>
/// Reads and writes the shared prefix every <see cref="EntityAsset"/> carries after its
/// <see cref="BaseAssetPrefix"/>.
/// </summary>
internal static class EntityAssetPrefix
{
    /// <summary>
    /// The prefix's length in bytes, excluding <see cref="Common.GameVersion.BFBB"/>'s
    /// four padding bytes.
    /// </summary>
    public const int Length = 72;

    /// <summary>
    /// Reads the prefix from <paramref name="data"/> at <paramref name="offset"/> into
    /// <paramref name="asset"/>.
    /// </summary>
    /// <param name="asset">The <see cref="EntityAsset"/> to populate.</param>
    /// <param name="data">The asset's data.</param>
    /// <param name="offset">The offset to begin reading at.</param>
    /// <param name="hasPadding">
    /// Whether this build inserts four bytes of padding after the four flag bytes, from
    /// <see cref="EvilHop.Serialization.FormatProfile.EntityHasPadding"/>.
    /// </param>
    /// <returns>The offset immediately past the prefix.</returns>
    public static int Read(EntityAsset asset, ReadOnlySpan<byte> data, int offset, bool hasPadding)
    {
        asset.EntityFlags = (EntityFlags)data[offset];
        asset.Physical.Subtype = data[offset + 1];
        asset.Physical.PFlags = data[offset + 2];
        asset.Physical.CollisionFlags = (CollisionFlags)data[offset + 3];
        offset += 4;

        // Read and discarded, never modelled - it is always zero where it exists.
        if (hasPadding) offset += 4;

        asset.Physical.SurfaceId = ReadAssetId(data, ref offset);
        asset.Angle = ReadVector3(data, ref offset);
        asset.Position = ReadVector3(data, ref offset);
        asset.Scale = ReadVector3(data, ref offset);
        asset.RedMultiplier = ReadSingle(data, ref offset);
        asset.GreenMultiplier = ReadSingle(data, ref offset);
        asset.BlueMultiplier = ReadSingle(data, ref offset);
        asset.SeeThrough = ReadSingle(data, ref offset);
        asset.Physical.SeeThroughSpeed = ReadSingle(data, ref offset);
        asset.Physical.ModelId = ReadAssetId(data, ref offset);
        asset.Physical.AnimListId = ReadAssetId(data, ref offset);

        return offset;
    }

    /// <summary>
    /// Writes <paramref name="asset"/>'s prefix to <paramref name="writer"/>.
    /// </summary>
    /// <param name="asset">The <see cref="EntityAsset"/> to read from.</param>
    /// <param name="writer">The writer to write to.</param>
    /// <param name="hasPadding">
    /// Whether this build writes four bytes of padding after the four flag bytes. Written as zero
    /// where it applies.
    /// </param>
    public static void Write(EntityAsset asset, BinaryWriter writer, bool hasPadding)
    {
        writer.Write((byte)asset.EntityFlags);
        writer.Write(asset.Physical.Subtype);
        writer.Write(asset.Physical.PFlags);
        writer.Write((byte)asset.Physical.CollisionFlags);

        if (hasPadding) writer.Write(new byte[4]);

        writer.WriteEvilInt(asset.Physical.SurfaceId.Value);
        writer.WriteBigEndian(asset.Angle);
        writer.WriteBigEndian(asset.Position);
        writer.WriteBigEndian(asset.Scale);
        writer.WriteBigEndian(asset.RedMultiplier);
        writer.WriteBigEndian(asset.GreenMultiplier);
        writer.WriteBigEndian(asset.BlueMultiplier);
        writer.WriteBigEndian(asset.SeeThrough);
        writer.WriteBigEndian(asset.Physical.SeeThroughSpeed);
        writer.WriteEvilInt(asset.Physical.ModelId.Value);
        writer.WriteEvilInt(asset.Physical.AnimListId.Value);
    }

    private static AssetId ReadAssetId(ReadOnlySpan<byte> data, ref int offset)
    {
        var id = new AssetId(BinaryPrimitives.ReadUInt32BigEndian(data[offset..]));
        offset += 4;
        return id;
    }

    private static float ReadSingle(ReadOnlySpan<byte> data, ref int offset)
    {
        float value = BinaryPrimitives.ReadSingleBigEndian(data[offset..]);
        offset += 4;
        return value;
    }

    private static Vector3 ReadVector3(ReadOnlySpan<byte> data, ref int offset) =>
        new(ReadSingle(data, ref offset), ReadSingle(data, ref offset), ReadSingle(data, ref offset));
}

/// <summary>
/// Reads and writes the prefix every <see cref="DynaAsset"/> carries after its
/// <see cref="BaseAssetPrefix"/>.
/// </summary>
internal static class DynaAssetPrefix
{
    /// <summary>The prefix's length in bytes.</summary>
    public const int Length = 8;

    /// <summary>
    /// Reads the prefix from <paramref name="data"/> at <paramref name="offset"/> into
    /// <paramref name="asset"/>.
    /// </summary>
    /// <returns>The offset immediately past the prefix.</returns>
    public static int Read(DynaAsset asset, ReadOnlySpan<byte> data, int offset)
    {
        asset.DynaType = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        asset.Version = BinaryPrimitives.ReadInt16BigEndian(data[(offset + 4)..]);
        asset.Handle = BinaryPrimitives.ReadInt16BigEndian(data[(offset + 6)..]);
        return offset + Length;
    }

    /// <summary>
    /// Writes <paramref name="asset"/>'s prefix to <paramref name="writer"/>.
    /// </summary>
    public static void Write(DynaAsset asset, BinaryWriter writer)
    {
        writer.WriteEvilInt(asset.DynaType);
        writer.WriteBigEndian(asset.Version);
        writer.WriteBigEndian(asset.Handle);
    }
}
