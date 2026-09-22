using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;

namespace EvilHop.Assets;

/// <summary>
/// The end-credits sequence played after completing the game: a set of independently-scrolling
/// <see cref="Section"/>s, optionally encrypted on disk.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/CRDT">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed partial class CreditsAsset() : Asset(AssetType.Credits), Physical.ICreditsAsset
{
    /// <summary>
    /// The key an encrypted <see cref="CreditsAsset"/>'s body is XORed against.
    /// </summary>
    internal const string CipherKey = "xCMChunkHand";

    internal const int HeaderSize = 24;
    internal const int SectionHeaderSize = 56;
    internal const int PresetSize = 12 + 2 * 32;
    internal const int HunkHeaderSize = 24;

    /// <summary>
    /// Whether this <see cref="CreditsAsset"/> is stored encrypted on disk.
    /// </summary>
    public bool IsEncrypted
    {
        get => Physical.State == State.Encrypted;
        set => Physical.State = value ? State.Encrypted : State.NotEncrypted;
    }

    /// <summary>
    /// The credits sequence's total duration, in seconds. Loops back to the start once elapsed.
    /// </summary>
    public float Duration { get; set; }

    /// <summary>
    /// This <see cref="CreditsAsset"/>'s independently-scrolling sections, shown one after another.
    /// </summary>
    public Collection<Section> Sections { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override Physical.ICreditsAsset Physical => this;

    private uint _magic = 0xBEEEEEEF;
    uint Physical.ICreditsAsset.Magic { get => _magic; set => _magic = value; }

    private uint _version;
    uint Physical.ICreditsAsset.Version { get => _version; set => _version = value; }

    private AssetId _creditsId;
    AssetId Physical.ICreditsAsset.CreditsId { get => _creditsId; set => _creditsId = value; }

    private State _state;
    State Physical.ICreditsAsset.State { get => _state; set => _state = value; }

    private uint? _overriddenTotalSize;
    uint Physical.ICreditsAsset.TotalSize
    {
        get => _overriddenTotalSize ?? ComputedTotalSize;
        set => _overriddenTotalSize = value == ComputedTotalSize ? null : value;
    }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.Credits"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.BFBB,
        GameVersion.TSSM,
        GameVersion.Incredibles,
        GameVersion.ROTU,
        GameVersion.Ratatouille,
    };

    private uint ComputedTotalSize =>
        (uint)(HeaderSize + Sections.Sum(Section.SectionByteLength) + GetUnparsedTail().Length);

    internal static CreditsAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new CreditsAsset();
        AssetFields.Populate(asset, header, debug);

        asset.Physical.Magic = reader.ReadUInt32();
        asset.Physical.Version = reader.ReadUInt32();
        asset.Physical.CreditsId = reader.ReadAssetId();
        asset.Physical.State = (State)reader.ReadUInt32();
        asset.Duration = reader.ReadSingle();
        asset.Physical.TotalSize = reader.ReadUInt32();

        byte[] body = reader.ReadRemainingBytes();
        if (asset.Physical.State is State.Encrypted)
            Decrypt(body);

        using var bodyReader = new EndianReader(new MemoryStream(body), profile.Endianness);
        while (body.Length - bodyReader.BaseStream.Position >= SectionHeaderSize)
            asset.Sections.Add(Section.Read(bodyReader, profile));

        asset.SetUnparsedTail(body[(int)bodyReader.BaseStream.Position..]);
        asset.Physical.TotalSize = asset.ComputedTotalSize;
        return asset;
    }

    internal static void Write(CreditsAsset asset, EndianWriter writer, FormatProfile profile)
    {
        writer.Write(asset.Physical.Magic);
        writer.Write(asset.Physical.Version);
        writer.Write(asset.Physical.CreditsId);
        writer.Write((uint)asset.Physical.State);
        writer.Write(asset.Duration);
        writer.Write(asset.Physical.TotalSize);

        using var bodyStream = new MemoryStream();
        using (var bodyWriter = new EndianWriter(bodyStream, profile.Endianness, leaveOpen: true))
        {
            foreach (var section in asset.Sections)
                Section.Write(section, bodyWriter, profile);
            bodyWriter.Write(asset.GetUnparsedTail());
        }

        byte[] body = bodyStream.ToArray();
        if (asset.Physical.State is State.Encrypted)
            Encrypt(body);

        writer.Write(body);
    }

    private static void Decrypt(byte[] body)
    {
        byte last = 0;
        for (int i = 0; i < body.Length; i++)
        {
            last = (byte)(body[i] ^ last ^ CipherKey[i % CipherKey.Length]);
            body[i] = last;
        }
    }

    private static void Encrypt(byte[] body)
    {
        byte previousPlaintext = 0;
        for (int i = 0; i < body.Length; i++)
        {
            byte plaintext = body[i];
            body[i] = (byte)(plaintext ^ previousPlaintext ^ CipherKey[i % CipherKey.Length]);
            previousPlaintext = plaintext;
        }
    }

    /// <summary>
    /// Defines the playback and display state of a credits entry.
    /// </summary>
    public enum State : uint
    {
        /// <summary>The body following the header is stored as-is.</summary>
        NotEncrypted = 1,
        /// <summary>The body following the header is encrypted and must be decrypted before use.</summary>
        Encrypted = 3,
    }

    // TODO: names for <see cref="Center"/>, <see cref="Left"/>, <see cref="Right"/>, and
    // <see cref="Inner"/> are inferred from a dead-stripped debug string list's declaration order
    // (<c>CM_ALIGN_CENTER</c>, <c>CM_ALIGN_LEFT</c>, <c>CM_ALIGN_RIGHT</c>, <c>CM_ALIGN_INNER</c>,
    // <c>CM_ALIGN_TEXTURE</c>), not confirmed against their numeric values directly. Only
    // <see cref="Center"/> and <see cref="Texture"/> are confirmed by the render switch itself; only
    // <see cref="Center"/>, <see cref="Inner"/>, and <see cref="Texture"/> are ever observed in the
    // corpus.

    /// <summary>
    /// Specifies text alignment and layout positioning for credits lines.
    /// </summary>
    public enum Alignment : ushort
    {
        /// <summary>A single, centered <see cref="Textbox"/>.</summary>
        Center = 0,
        /// <summary>Two <see cref="Textbox"/>s, both left-aligned.</summary>
        Left = 1,
        /// <summary>Two <see cref="Textbox"/>s, both right-aligned.</summary>
        Right = 2,
        /// <summary>Two <see cref="Textbox"/>s, facing each other across the gap between them.</summary>
        Inner = 3,
        /// <summary>A single <see cref="CreditsAsset.Texture"/>.</summary>
        Texture = 4,
    }
}

public static partial class Physical
{
    /// <summary>
    /// An explicit interface used to interact with <see cref="CreditsAsset"/>'s underlying values.
    /// </summary>
    public interface ICreditsAsset : IAsset
    {
        /// <summary>
        /// A magic number used to validate the payload.
        /// </summary>
        uint Magic { get; set; }

        /// <summary>
        /// The credits format version. 256 (1.0) in <see cref="GameVersion.BFBB"/> and
        /// <see cref="GameVersion.Incredibles"/>; 512 (2.0) from <see cref="GameVersion.TSSM"/> onward.
        /// </summary>
        uint Version { get; set; }

        /// <summary>
        /// Unknown.
        /// </summary>
        AssetId CreditsId { get; set; }

        /// <summary>
        /// Whether the body following this header is encrypted.
        /// </summary>
        CreditsAsset.State State { get; set; }

        /// <summary>
        /// The total size, in bytes, of this <see cref="CreditsAsset"/>'s entire on-disk representation,
        /// including this header.
        /// </summary>
        /// <remarks>
        /// When disagreements with the actual encoded size exist, this field wins during serialization.
        /// </remarks>
        uint TotalSize { get; set; }
    }
}
