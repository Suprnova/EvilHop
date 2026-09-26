using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Assets;

public abstract partial class CameraAsset
{
    /// <exception cref="InvalidDataException">The stored <see cref="Kind"/> byte is not a known <see cref="CameraKind"/>.</exception>
    internal static CameraAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        // store fields for later, until we have a concrete Camera subclass instance after reading
        // kind.
        var baseId = reader.ReadAssetId();
        var baseType = reader.ReadByte();
        var linkCount = reader.ReadByte();
        var baseFlags = (BaseAssetFlags)reader.ReadInt16();

        var position = reader.ReadVector3();
        var forward = reader.ReadVector3();
        var up = reader.ReadVector3();
        var left = reader.ReadVector3();
        Vector3 viewOffset = default;
        short offsetStartFrames = 0, offsetEndFrames = 0;
        if (profile.CameraHasExtendedFields)
        {
            viewOffset = reader.ReadVector3();
            offsetStartFrames = reader.ReadInt16();
            offsetEndFrames = reader.ReadInt16();
        }
        var fov = reader.ReadSingle();
        // TODO: the 2001-06-11 prototype's five fields here are zero in every sample; their order is assumed from the retail layout
        var transitionTime = reader.ReadSingle();
        var transitionType = (CameraTransitionKind)reader.ReadInt32();
        var cameraFlags = reader.ReadUInt32();
        var fadeUp = reader.ReadSingle();
        var fadeDown = reader.ReadSingle();

        // a shorter type-specific region is zero-extended so every kind reads its full TypeDataSize
        byte[] typeData = [.. reader.ReadBytes(TypeDataSizeFor(profile)), .. new byte[TypeDataSize - TypeDataSizeFor(profile)]];

        var validFlags = reader.ReadUInt32();
        AssetId markerId1 = default, markerId2 = default;
        if (profile.CameraHasExtendedFields)
        {
            markerId1 = reader.ReadAssetId();
            markerId2 = reader.ReadAssetId();
        }
        var kind = (CameraKind)reader.ReadByte();
        reader.ReadBytes(3); // padding, always zero

        using var typeReader = new EndianReader(new MemoryStream(typeData), profile.Endianness);
        CameraAsset asset = kind switch
        {
            CameraKind.Follow => FollowCameraAsset.Read(typeReader, profile),
            CameraKind.FollowWithOffset => FollowWithOffsetCameraAsset.Read(typeReader, profile),
            CameraKind.Shoulder => ShoulderCameraAsset.Read(typeReader, profile),
            CameraKind.Static => StaticCameraAsset.Read(typeReader, profile),
            CameraKind.Path => PathCameraAsset.Read(typeReader, profile),
            CameraKind.StaticFollow => StaticFollowCameraAsset.Read(typeReader, profile),
            _ => throw new InvalidDataException($"Unknown Cam Type 0x{(byte)kind:X2}."),
        };

        AssetFields.Populate(asset, header, debug);
        asset.Physical.BaseId = baseId;
        asset.Physical.BaseType = baseType;
        asset.Physical.LinkCount = linkCount;
        asset.Physical.BaseFlags = baseFlags;

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

        asset.Physical.ValidFlags = validFlags;
        asset.MarkerId1 = markerId1;
        asset.MarkerId2 = markerId2;

        for (var i = 0; i < asset.Physical.LinkCount; i++)
            asset.Links.Add(Link.Read(reader, profile));
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(CameraAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);

        writer.Write(asset.Position);
        writer.Write(asset.Forward);
        writer.Write(asset.Up);
        writer.Write(asset.Left);
        if (profile.CameraHasExtendedFields)
        {
            writer.Write(asset.ViewOffset);
            writer.Write(asset.OffsetStartFrames);
            writer.Write(asset.OffsetEndFrames);
        }
        writer.Write(asset.Fov);
        writer.Write(asset.TransitionTime);
        writer.Write((int)asset.TransitionType);
        writer.Write(asset.Physical.CameraFlags);
        writer.Write(asset.FadeUp);
        writer.Write(asset.FadeDown);

        using var typeStream = new MemoryStream();
        using (var typeWriter = new EndianWriter(typeStream, profile.Endianness, leaveOpen: true))
        {
            switch (asset)
            {
                case FollowCameraAsset c: FollowCameraAsset.Write(c, typeWriter, profile); break;
                case ShoulderCameraAsset c: ShoulderCameraAsset.Write(c, typeWriter, profile); break;
                case StaticCameraAsset c: StaticCameraAsset.Write(c, typeWriter, profile); break;
                case PathCameraAsset c: PathCameraAsset.Write(c, typeWriter, profile); break;
                case StaticFollowCameraAsset c: StaticFollowCameraAsset.Write(c, typeWriter, profile); break;
            }
        }
        writer.Write(typeStream.GetBuffer().AsSpan(0, TypeDataSizeFor(profile)));

        writer.Write(asset.Physical.ValidFlags);
        if (profile.CameraHasExtendedFields)
        {
            writer.Write(asset.MarkerId1);
            writer.Write(asset.MarkerId2);
        }
        writer.Write((byte)asset.Kind);
        writer.Write(new byte[3]); // padding

        foreach (var link in asset.Links)
            Link.Write(link, writer, profile);
        writer.Write(asset.GetUnparsedTail());
    }
}
