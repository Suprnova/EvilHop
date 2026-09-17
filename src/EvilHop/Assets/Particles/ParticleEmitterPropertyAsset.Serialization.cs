using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public sealed partial class ParticleEmitterPropertyAsset
{
    internal static ParticleEmitterPropertyAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile _)
    {
        var asset = new ParticleEmitterPropertyAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        asset.ParSysId = reader.ReadAssetId();
        asset.Rate = ReadInterpolation(reader);
        asset.Life = ReadInterpolation(reader);
        asset.SizeBirth = ReadInterpolation(reader);
        asset.SizeDeath = ReadInterpolation(reader);
        asset.ColorBirth = ReadColorInterpolation(reader);
        asset.ColorDeath = ReadColorInterpolation(reader);
        asset.VelocityScale = ReadInterpolation(reader);
        asset.VelocityAngle = ReadInterpolation(reader);
        asset.Velocity = reader.ReadVector3();
        asset.EmitLimit = reader.ReadInt32();
        asset.EmitLimitResetTime = reader.ReadSingle();

        LinkSerialization.Read(asset, reader, asset.Physical.LinkCount);
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(ParticleEmitterPropertyAsset asset, EndianWriter writer, FormatProfile _)
    {
        BaseAssetPrefix.Write(asset, writer);

        writer.Write(asset.ParSysId);
        WriteInterpolation(writer, asset.Rate);
        WriteInterpolation(writer, asset.Life);
        WriteInterpolation(writer, asset.SizeBirth);
        WriteInterpolation(writer, asset.SizeDeath);
        WriteColorInterpolation(writer, asset.ColorBirth);
        WriteColorInterpolation(writer, asset.ColorDeath);
        WriteInterpolation(writer, asset.VelocityScale);
        WriteInterpolation(writer, asset.VelocityAngle);
        writer.Write(asset.Velocity);
        writer.Write(asset.EmitLimit);
        writer.Write(asset.EmitLimitResetTime);

        LinkSerialization.Write(asset, writer);
        writer.Write(asset.GetUnparsedTail());
    }

    private static ParticleInterpolation ReadInterpolation(EndianReader reader) => new()
    {
        Start = reader.ReadSingle(),
        End = reader.ReadSingle(),
        Mode = (ParticleInterpolationMode)reader.ReadUInt32(),
        Frequency = reader.ReadSingle(),
        InverseFrequency = reader.ReadSingle(),
    };

    private static void WriteInterpolation(EndianWriter writer, ParticleInterpolation interpolation)
    {
        writer.Write(interpolation.Start);
        writer.Write(interpolation.End);
        writer.Write((uint)interpolation.Mode);
        writer.Write(interpolation.Frequency);
        writer.Write(interpolation.InverseFrequency);
    }

    private static ParticleColorInterpolation ReadColorInterpolation(EndianReader reader) => new()
    {
        Red = ReadInterpolation(reader),
        Green = ReadInterpolation(reader),
        Blue = ReadInterpolation(reader),
        Alpha = ReadInterpolation(reader),
    };

    private static void WriteColorInterpolation(EndianWriter writer, ParticleColorInterpolation color)
    {
        WriteInterpolation(writer, color.Red);
        WriteInterpolation(writer, color.Green);
        WriteInterpolation(writer, color.Blue);
        WriteInterpolation(writer, color.Alpha);
    }
}
