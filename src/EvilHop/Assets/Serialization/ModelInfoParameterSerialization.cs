using EvilHop.Common;
using EvilHop.Primitives;
using System.Text;

namespace EvilHop.Assets.Serialization;

/// <summary>
/// Reads and writes a single <see cref="ModelInfoParameter"/> entry, shared verbatim between
/// <see cref="AssetType.ModelInfo"/> and <see cref="AssetType.NPCSettings"/>.
/// </summary>
internal static class ModelInfoParameterSerialization
{
    /// <summary>
    /// Reads one entry from <paramref name="reader"/>'s current position.
    /// </summary>
    public static ModelInfoParameter Read(EndianReader reader)
    {
        uint hashId = reader.ReadUInt32();
        byte wordLength = reader.ReadByte();
        int stringAreaLength = (wordLength + 1) * 4 - 1;
        byte[] stringArea = reader.ReadBytes(stringAreaLength);
        int nullIndex = Array.IndexOf(stringArea, (byte)0);
        string value = Encoding.Latin1.GetString(stringArea, 0, nullIndex >= 0 ? nullIndex : stringArea.Length);
        return new ModelInfoParameter { HashId = hashId, Value = value };
    }

    /// <summary>
    /// Writes <paramref name="parameter"/> to <paramref name="writer"/>.
    /// </summary>
    public static void Write(EndianWriter writer, ModelInfoParameter parameter)
    {
        byte[] valueBytes = Encoding.Latin1.GetBytes(parameter.Value);
        int stringBytesWithNull = valueBytes.Length + 1;
        int totalRegion = ((1 + stringBytesWithNull + 3) / 4) * 4;
        byte wordLength = (byte)(totalRegion / 4 - 1);

        writer.Write(parameter.HashId);
        writer.Write(wordLength);
        writer.Write(valueBytes);
        writer.Write((byte)0);
        for (int i = 1 + stringBytesWithNull; i < totalRegion; i++)
            writer.Write((byte)0);
    }
}
