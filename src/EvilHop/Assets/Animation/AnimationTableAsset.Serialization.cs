using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public sealed partial class AnimationTableAsset
{
    internal static AnimationTableAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile _)
    {
        var asset = new AnimationTableAsset();
        AssetFields.Populate(asset, header, debug);

        reader.ReadUInt32(); // Magic
        uint rawCount = reader.ReadUInt32();
        uint fileCount = reader.ReadUInt32();
        uint stateCount = reader.ReadUInt32();
        asset.ConstructFunc = reader.ReadUInt32();

        for (uint i = 0; i < rawCount; i++) asset.Raw.Add(reader.ReadAssetId());

        for (uint i = 0; i < fileCount; i++)
        {
            asset.Files.Add(new AnimationTableFile
            {
                FileFlags = (FileFlags)reader.ReadUInt32(),
                Duration = reader.ReadSingle(),
                TimeOffset = reader.ReadSingle(),
                NumAnimsX = reader.ReadUInt16(),
                NumAnimsY = reader.ReadUInt16(),
                RawDataOffset = reader.ReadUInt32(),
                Physics = reader.ReadInt32(),
                StartPose = reader.ReadInt32(),
                EndPose = reader.ReadInt32(),
            });
        }

        for (uint i = 0; i < stateCount; i++)
        {
            asset.States.Add(new AnimationTableState
            {
                StateId = reader.ReadUInt32(),
                FileIndex = reader.ReadUInt32(),
                EffectCount = reader.ReadUInt32(),
                EffectOffset = reader.ReadUInt32(),
                Speed = reader.ReadSingle(),
                SubStateId = reader.ReadUInt32(),
                SubStateCount = reader.ReadUInt32(),
            });
        }

        asset.Physical.RawCount = rawCount;
        asset.Physical.FileCount = fileCount;
        asset.Physical.StateCount = stateCount;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(AnimationTableAsset asset, EndianWriter writer, FormatProfile _)
    {
        writer.Write(0x4C425441u); // Magic
        writer.Write(asset.Physical.RawCount);
        writer.Write(asset.Physical.FileCount);
        writer.Write(asset.Physical.StateCount);
        writer.Write(asset.ConstructFunc);

        foreach (var id in asset.Raw) writer.Write(id);

        foreach (var file in asset.Files)
        {
            writer.Write((uint)file.FileFlags);
            writer.Write(file.Duration);
            writer.Write(file.TimeOffset);
            writer.Write(file.NumAnimsX);
            writer.Write(file.NumAnimsY);
            writer.Write(file.RawDataOffset);
            writer.Write(file.Physics);
            writer.Write(file.StartPose);
            writer.Write(file.EndPose);
        }

        foreach (var state in asset.States)
        {
            writer.Write(state.StateId);
            writer.Write(state.FileIndex);
            writer.Write(state.EffectCount);
            writer.Write(state.EffectOffset);
            writer.Write(state.Speed);
            writer.Write(state.SubStateId);
            writer.Write(state.SubStateCount);
        }

        writer.Write(asset.GetUnparsedTail());
    }
}
