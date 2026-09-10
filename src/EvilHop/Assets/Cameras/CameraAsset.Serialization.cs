using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public abstract partial class CameraAsset
{
    /// <exception cref="InvalidDataException">The stored <see cref="Kind"/> byte is not a known <see cref="CameraKind"/>.</exception>
    internal static CameraAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        // Cam Type - the discriminator selecting which of these fields apply - is stored after the
        // type-specific block it selects, so the concrete instance can't be constructed until
        // everything up to and including that byte has been read.
        var baseId = reader.ReadAssetId();
        var baseType = reader.ReadByte();
        var linkCount = reader.ReadByte();
        var baseFlags = (BaseAssetFlags)reader.ReadInt16();

        var position = reader.ReadVector3();
        var forward = reader.ReadVector3();
        var up = reader.ReadVector3();
        var left = reader.ReadVector3();
        var viewOffset = reader.ReadVector3();
        var offsetStartFrames = reader.ReadInt16();
        var offsetEndFrames = reader.ReadInt16();
        var fov = reader.ReadSingle();
        var transitionTime = reader.ReadSingle();
        var transitionType = (CameraTransitionType)reader.ReadInt32();
        var cameraFlags = reader.ReadUInt32();
        var fadeUp = reader.ReadSingle();
        var fadeDown = reader.ReadSingle();

        var typeData = reader.ReadBytes(TypeDataSize);

        var validFlags = reader.ReadUInt32();
        var markerId1 = reader.ReadAssetId();
        var markerId2 = reader.ReadAssetId();
        var kind = (CameraKind)reader.ReadByte();
        reader.ReadBytes(3); // padding, always zero

        CameraAsset asset = kind switch
        {
            CameraKind.Follow => new FollowCameraAsset(),
            CameraKind.Shoulder => new ShoulderCameraAsset(),
            CameraKind.Static => new StaticCameraAsset(),
            CameraKind.Path => new PathCameraAsset(),
            CameraKind.StaticFollow => new StaticFollowCameraAsset(),
            _ => throw new InvalidDataException($"Unknown Cam Type 0x{(byte)kind:X2}."),
        };

        AssetFields.Populate(asset, header, debug);
        asset.Physical.BaseId = baseId;
        asset.Physical.BaseType = baseType;
        asset.Physical.LinkCount = linkCount;
        asset.BaseFlags = baseFlags;

        asset.Position = position;
        asset.Forward = forward;
        asset.Up = up;
        asset.Left = left;
        asset.ViewOffset = viewOffset;
        asset.OffsetStartFrames = offsetStartFrames;
        asset.OffsetEndFrames = offsetEndFrames;
        asset.Fov = fov;
        asset.TransitionTime = transitionTime;
        asset.TransitionType = transitionType;
        asset.Physical.CameraFlags = cameraFlags;
        asset.FadeUp = fadeUp;
        asset.FadeDown = fadeDown;

        using (var typeReader = new EndianReader(new MemoryStream(typeData), profile.Endianness))
            asset.ReadTypeFields(typeReader);

        asset.Physical.ValidFlags = validFlags;
        asset.MarkerId1 = markerId1;
        asset.MarkerId2 = markerId2;

        LinkSerialization.Read(asset, reader, linkCount);
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(CameraAsset asset, EndianWriter writer, FormatProfile _)
    {
        BaseAssetPrefix.Write(asset, writer);

        writer.Write(asset.Position);
        writer.Write(asset.Forward);
        writer.Write(asset.Up);
        writer.Write(asset.Left);
        writer.Write(asset.ViewOffset);
        writer.Write(asset.OffsetStartFrames);
        writer.Write(asset.OffsetEndFrames);
        writer.Write(asset.Fov);
        writer.Write(asset.TransitionTime);
        writer.Write((int)asset.TransitionType);
        writer.Write(asset.Physical.CameraFlags);
        writer.Write(asset.FadeUp);
        writer.Write(asset.FadeDown);

        asset.WriteTypeFields(writer);

        writer.Write(asset.Physical.ValidFlags);
        writer.Write(asset.MarkerId1);
        writer.Write(asset.MarkerId2);
        writer.Write((byte)asset.Kind);
        writer.Write(new byte[3]); // padding

        LinkSerialization.Write(asset, writer);
        writer.Write(asset.GetUnparsedTail());
    }
}
