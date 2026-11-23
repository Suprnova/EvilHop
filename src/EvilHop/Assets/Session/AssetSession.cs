using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.Frozen;

namespace EvilHop.Assets;

/// <summary>
/// Owns an <see cref="Archive"/>'s assets for the duration of a scope. Opening one detaches the
/// blocks that describe assets from the block tree and parses them into <see cref="Layer"/>s of
/// <see cref="Asset"/>s; committing rebuilds those blocks and reattaches them.
/// </summary>
/// <remarks>
/// <para>The following blocks are locked and unavailable from <see cref="Archive"/> while open:</para>
/// <list type="bullet">
/// <item><see cref="Dictionary"/></item>
/// <item><see cref="AssetTable"/></item>
/// <item><see cref="LayerTable"/></item>
/// <item><see cref="AssetStream"/></item>
/// <item><see cref="StreamData"/></item>
/// </list>
/// </remarks>
public sealed class AssetSession : IDisposable
{
    /// <summary>The archive's <see cref="Layer"/>s, in the order they are listed on disk.</summary>
    public IReadOnlyList<Layer> Layers => _layers;
    private readonly List<Layer> _layers;

    /// <summary>
    /// Creates an empty <see cref="Layer"/> of the given <paramref name="type"/> and places it in
    /// <see cref="Layers"/>, directly after the last layer that shares its type, or at the end when
    /// there is none.
    /// </summary>
    /// <param name="type">The <see cref="LayerType"/> the new layer carries.</param>
    /// <returns>The new <see cref="Layer"/>, ready to <see cref="Layer.Add"/> assets to.</returns>
    /// <remarks>
    /// Placing rather than appending is what keeps same-type layers adjacent, which is how every
    /// N100F and Battle for Bikini Bottom archive stores them. Layers read from disk are never
    /// reordered - from The SpongeBob SquarePants Movie on, a run of <see cref="LayerType.BSP"/>
    /// layers is followed by the <see cref="LayerType.JSPInfo"/> describing it and that pairing
    /// repeats, so regrouping an archive by type would both break byte-exact round-tripping and
    /// separate a <c>JSPInfo</c> from the layers it belongs to.
    /// </remarks>
    public Layer CreateLayer(LayerType type)
    {
        var layer = new Layer { Type = type };
        int last = _layers.FindLastIndex(existing => existing.Type == type);

        if (last < 0) _layers.Add(layer);
        else _layers.Insert(last + 1, layer);

        return layer;
    }

    /// <summary>
    /// Problems encountered while opening. An asset that fails to parse degrades to its untyped form
    /// and is reported here rather than throwing.
    /// </summary>
    public IReadOnlyList<AssetDiagnostic> Diagnostics => _diagnostics;
    private readonly List<AssetDiagnostic> _diagnostics;

    /// <summary>
    /// The assets whose bytes differ from what they were at open, calculated via the session's checksum.
    /// </summary>
    public IReadOnlyList<AssetId> ChangedAssets { get; private set; } = [];

    private readonly Archive _archive;
    private readonly FormatProfile _profile;
    private readonly Platform _platform;
    private readonly Package _package;
    private readonly Dictionary _dictionary;
    private readonly AssetStream _stream;
    private readonly StreamData _streamData;
    private readonly uint _assetInfValue;
    private readonly uint _layerInfValue;
    private readonly byte _fillByte;
    private readonly bool _hadPaddingAmountField;
    private readonly List<AssetId> _originalAtocOrder;
    private readonly bool _originalAtocWasSorted;
    private readonly Dictionary<AssetId, uint> _openChecksums;
    private readonly Dictionary<(AssetId Before, AssetId After), byte[]> _capturedGaps;
    private readonly Dictionary<AssetId, uint> _originalZeroSizeOffsets;
    private HashSet<AssetId> _changedLookup = [];
    private bool _committed;

    /// <summary>The blocks this session was opened against.</summary>
    private readonly record struct SessionTarget(Archive Archive, FormatProfile Profile, Platform Platform, Package Package, Dictionary Dictionary, AssetStream Stream, StreamData StreamData);

    /// <summary>
    /// Scalars captured while opening, before the blocks that carried them are detached or cleared.
    /// </summary>
    private readonly record struct CapturedValues(uint AssetInfValue, uint LayerInfValue, byte FillByte, bool HadPaddingAmountField);

    /// <summary>
    /// State captured at open: what changed by the time of commit, and values with no computable
    /// rule that must simply be replayed.
    /// </summary>
    private readonly record struct ChangeTracking(
        List<AssetId> OriginalAtocOrder,
        Dictionary<AssetId, uint> OpenChecksums,
        Dictionary<(AssetId Before, AssetId After), byte[]> CapturedGaps,
        Dictionary<AssetId, uint> OriginalZeroSizeOffsets);

    private AssetSession(SessionTarget target, CapturedValues captured, AssetRead read, ChangeTracking changeTracking)
    {
        _layers = read.Layers;
        _diagnostics = read.Diagnostics;
        _archive = target.Archive;
        _profile = target.Profile;
        _platform = target.Platform;
        _package = target.Package;
        _dictionary = target.Dictionary;
        _stream = target.Stream;
        _streamData = target.StreamData;
        _assetInfValue = captured.AssetInfValue;
        _layerInfValue = captured.LayerInfValue;
        _fillByte = captured.FillByte;
        _hadPaddingAmountField = captured.HadPaddingAmountField;
        _originalAtocOrder = changeTracking.OriginalAtocOrder;
        _originalAtocWasSorted = IsSorted(_originalAtocOrder);
        _openChecksums = changeTracking.OpenChecksums;
        _capturedGaps = changeTracking.CapturedGaps;
        _originalZeroSizeOffsets = changeTracking.OriginalZeroSizeOffsets;
    }

    /// <summary>
    /// The fill byte assumed when an archive carries no padding to sample one from.
    /// </summary>
    private const byte DefaultFillByte = 0x33;

    /// <summary>
    /// The alignment assumed for an asset that declares a non-positive one, except the types in
    /// <see cref="WideAlignmentTypes"/> and <see cref="StreamingAlignmentTypes"/> - see
    /// <see cref="DefaultAlignmentFor"/>.
    /// </summary>
    private const uint DefaultAlignment = 16;

    /// <summary>
    /// The alignment <see cref="WideAlignmentTypes"/> default to on every platform, and
    /// <see cref="StreamingAlignmentTypes"/> default to off <see cref="Platform.GameCube"/>.
    /// </summary>
    private const uint WideDefaultAlignment = 2048;

    /// <summary>
    /// The alignment <see cref="StreamingAlignmentTypes"/> default to on <see cref="Platform.GameCube"/>.
    /// </summary>
    private const uint GameCubeStreamingAlignment = 32;

    /// <summary>
    /// Types that default to <see cref="WideDefaultAlignment"/> on every platform when their own
    /// declared alignment is non-positive - the wiki puts them at "at least 128" instead.
    /// </summary>
    private static readonly FrozenSet<AssetType> WideAlignmentTypes =
        [AssetType.ReactiveAnimation, AssetType.PickupTypes, AssetType.ThrowableTable];

    /// <summary>
    /// Types that default to <see cref="GameCubeStreamingAlignment"/> on GameCube and
    /// <see cref="WideDefaultAlignment"/> elsewhere when their own declared alignment is non-positive -
    /// plausibly for optical-disc sector-aligned streaming reads, which GameCube discs don't need. The
    /// wiki puts them all at 32 regardless of platform.
    /// </summary>
    private static readonly FrozenSet<AssetType> StreamingAlignmentTypes =
        [AssetType.BinkVideo, AssetType.CutsceneTable, AssetType.StreamingTexture, AssetType.Wireframe];

    /// <summary>
    /// The alignment to use for <paramref name="type"/> when its own declared alignment is
    /// non-positive, on <paramref name="platform"/>.
    /// </summary>
    private static uint DefaultAlignmentFor(AssetType type, Platform platform)
    {
        if (WideAlignmentTypes.Contains(type)) return WideDefaultAlignment;
        if (StreamingAlignmentTypes.Contains(type))
            return platform == Platform.GameCube ? GameCubeStreamingAlignment : WideDefaultAlignment;
        return DefaultAlignment;
    }

    /// <summary>
    /// The boundary <see cref="StreamData"/>'s data begins on.
    /// </summary>
    private static uint DataAlignmentFor(Platform platform) => platform == Platform.GameCube ? 32u : 2048u;

    /// <summary>
    /// Opens <paramref name="archive"/>'s assets under its serializer's profile, with
    /// <paramref name="supplied"/> filling in a platform the profile doesn't state.
    /// </summary>
    internal static AssetSession Open(Archive archive, Platform? supplied)
    {
        var profile = AssetReader.ResolveProfile(archive, supplied);
        var read = AssetReader.Read(archive, profile);

        var package = archive.Roots.OfType<Package>().Single();
        var dictionary = archive.Roots.OfType<Dictionary>().Single();
        var stream = archive.Roots.OfType<AssetStream>().Single();
        var assetTable = dictionary.AssetTable;
        var layerTable = dictionary.LayerTable;
        var streamData = stream.Data;

        var session = new AssetSession(
            new SessionTarget(archive, profile, profile.Platform!.Value, package, dictionary, stream, streamData),
            new CapturedValues(
                assetTable.Inf.Value,
                layerTable.Inf.Value,
                streamData.Padding.Length > 0 ? streamData.Padding[0] : DefaultFillByte,
                // The reader always tries to parse PaddingAmount when 4+ bytes are available, so a
                // no-field archive's fill bytes get parsed as one anyway. Comparing the parsed value
                // against the padding actually read is what tells a real field from a false one.
                streamData.PaddingAmount == (uint?)streamData.Padding.Length),
            read,
            new ChangeTracking(
                [.. read.Placements.Select(p => p.Id)],
                read.Checksums,
                CaptureGaps(read.Placements, read.Data, read.DataStart),
                CaptureZeroSizeOffsets(read.Placements)));

        assetTable.LockFields();
        layerTable.LockFields();

        dictionary.AssetTable = null!;
        dictionary.LayerTable = null!;
        dictionary.LockFields();

        streamData.Data = [];
        stream.LockFields();

        return session;
    }

    /// <summary>
    /// Captures every zero-size asset's <c>Offset</c> as read, for <see cref="BuildData"/> to replay.
    /// </summary>
    private static Dictionary<AssetId, uint> CaptureZeroSizeOffsets(List<AssetPlacement> placements)
    {
        var offsets = new Dictionary<AssetId, uint>();
        foreach (var placement in placements.Where(p => p.Size == 0))
            offsets[placement.Id] = placement.Offset;
        return offsets;
    }

    /// <summary>
    /// Captures the bytes between each pair of assets adjacent in the data, for
    /// <see cref="GapBytesFor"/> to replay.
    /// </summary>
    private static Dictionary<(AssetId Before, AssetId After), byte[]> CaptureGaps(
        List<AssetPlacement> placements, byte[] data, long dataStart)
    {
        var gaps = new Dictionary<(AssetId Before, AssetId After), byte[]>();
        var ordered = placements.OrderBy(p => p.Offset).ToList();
        for (int i = 0; i < ordered.Count - 1; i++)
        {
            uint gapStart = ordered[i].Offset + ordered[i].Size;
            if (ordered[i + 1].Offset <= gapStart) continue;

            uint gapSize = ordered[i + 1].Offset - gapStart;
            if (!AssetReader.TryGetRange(dataStart, data.Length, gapStart, gapSize, out var range)) continue;

            gaps[(ordered[i].Id, ordered[i + 1].Id)] = data[range];
        }

        return gaps;
    }

    /// <summary>
    /// Rebuilds <see cref="AssetTable"/>, <see cref="LayerTable"/>, and <see cref="StreamData"/>
    /// from the current assets and reattaches them. Calling this twice is a no-op the second time.
    /// </summary>
    public void Commit()
    {
        if (_committed) return;
        _committed = true;

        var ordered = _layers.SelectMany(layer => layer.Assets).ToList();
        var serialized = new Dictionary<AssetId, byte[]>();
        var checksums = new Dictionary<AssetId, uint>();

        foreach (var asset in ordered)
        {
            using var buffer = new MemoryStream();
            using (var writer = new EndianWriter(buffer, _profile.Endianness, leaveOpen: true))
                AssetCodecs.Write(asset, writer, _profile);

            byte[] bytes = buffer.ToArray();
            serialized[asset.Id] = bytes;
            checksums[asset.Id] = Crc32Mpeg2.Compute(bytes);
        }

        ChangedAssets = [.. checksums
            .Where(pair => !_openChecksums.TryGetValue(pair.Key, out uint open) || open != pair.Value)
            .Select(pair => pair.Key)];
        _changedLookup = [.. ChangedAssets];

        // Only the derived baseline moves. An asset carrying an override keeps writing it, which is
        // how an archive that shipped a wrong checksum reproduces one.
        foreach (var asset in ordered)
            asset.ComputedChecksum = checksums[asset.Id];

        var headers = BuildHeaders(ordered, serialized);
        var assetTable = BuildAssetTable(headers);
        var layerTable = BuildLayerTable();

        _dictionary.UnlockFields();
        _dictionary.AssetTable = assetTable;
        _dictionary.LayerTable = layerTable;

        _stream.UnlockFields();
        _streamData.PaddingAmount = null;
        _streamData.Padding = [];
        _streamData.Data = [];

        // An empty archive usually omits PaddingAmount entirely; a minority keep it anyway, so
        // replay whichever convention this one was opened with.
        if (ordered.Count == 0 && !_hadPaddingAmountField)
        {
            var (_, emptyPadding) = MeasureDataStart();
            _streamData.Data = FillBytes(emptyPadding);
        }
        else
        {
            _streamData.PaddingAmount = 0;
            var (dataStart, paddingAmount) = MeasureDataStart();

            _streamData.PaddingAmount = (uint)paddingAmount;
            _streamData.Padding = FillBytes(paddingAmount);
            dataStart += paddingAmount;

            _streamData.Data = BuildData(ordered, serialized, headers, dataStart);
        }

        // todo: we should be locking this block if we aren't already
        UpdatePackageCounts(headers);
    }

    /// <summary>
    /// Recomputes <see cref="PackageCount"/> from the headers just serialized: how many assets and
    /// layers there are, and the largest asset, layer, and transform-asset sizes among them.
    /// </summary>
    private void UpdatePackageCounts(Dictionary<AssetId, AssetHeader> headers)
    {
        var counts = _package.Counts;
        var values = headers.Values.ToList();

        counts.AssetCount = (uint)values.Count;
        counts.LayerCount = (uint)_layers.Count;
        counts.MaxAssetSize = values.Count == 0 ? 0 : values.Max(h => h.Size);

        var transformed = values.Where(h => h.Flags.HasFlag(AssetFlags.ReadTransform)).ToList();
        counts.MaxXFormAssetSize = transformed.Count == 0 ? 0 : transformed.Max(h => h.Size);

        long calculateHeaderSize(Asset asset) => (long)headers[asset.Id].Size + headers[asset.Id].Plus;

        counts.MaxLayerSize = _layers.Count == 0 ? 0 : (uint)_layers.Max(layer => layer.Assets.Sum(calculateHeaderSize));
    }

    private Dictionary<AssetId, AssetHeader> BuildHeaders(List<Asset> ordered, Dictionary<AssetId, byte[]> serialized)
    {
        var headers = new Dictionary<AssetId, AssetHeader>();

        foreach (var asset in ordered)
        {
            var header = _archive.Serializer.CreateBlock<AssetHeader>();
            var debug = _archive.Serializer.CreateBlock<AssetDebug>();
            AssetFields.Apply(asset, header, debug);

            header.Size = (uint)serialized[asset.Id].Length;
            header.Debug = debug;

            headers[asset.Id] = header;
        }

        return headers;
    }

    private AssetTable BuildAssetTable(Dictionary<AssetId, AssetHeader> headers)
    {
        var table = _archive.Serializer.CreateBlock<AssetTable>();
        var inf = _archive.Serializer.CreateBlock<AssetInf>();
        inf.Value = _assetInfValue;
        table.Inf = inf;

        foreach (var id in ReplayAtocOrder(headers.Keys))
            table.Children.Add(headers[id]);

        return table;
    }

    /// <summary>
    /// Rebuilds <see cref="AssetTable"/>'s ordering: surviving assets keep their captured relative
    /// order, removed ones simply drop out, and assets added during the session land at their
    /// sorted position when the original table was sorted, or at the end when it was not.
    /// </summary>
    private List<AssetId> ReplayAtocOrder(IEnumerable<AssetId> currentIds)
    {
        var current = currentIds.ToList();
        var present = current.ToHashSet();
        var replayed = _originalAtocOrder.Where(present.Contains).ToList();
        var replayedSet = replayed.ToHashSet();
        var added = current.Where(id => !replayedSet.Contains(id)).ToList();

        if (added.Count == 0)
            return replayed;

        if (_originalAtocWasSorted)
            return [.. current.OrderBy(id => id.Value)];

        return [.. replayed, .. added];
    }

    private static bool IsSorted(List<AssetId> order)
    {
        for (int i = 1; i < order.Count; i++)
            if (order[i].Value < order[i - 1].Value)
                return false;
        return true;
    }

    private LayerTable BuildLayerTable()
    {
        var table = _archive.Serializer.CreateBlock<LayerTable>();
        var inf = _archive.Serializer.CreateBlock<LayerInf>();
        inf.Value = _layerInfValue;
        table.Inf = inf;

        foreach (var layer in _layers)
        {
            var header = _archive.Serializer.CreateBlock<LayerHeader>();
            var debug = _archive.Serializer.CreateBlock<LayerDebug>();
            debug.Value = layer.DebugValue;

            header.Type = layer.Type;
            header.AssetCount = (uint)layer.Assets.Count;
            header.AssetIds = [.. layer.Assets.Select(asset => asset.Id.Value)];
            header.Debug = debug;

            table.Children.Add(header);
        }

        return table;
    }

    /// <summary>
    /// Lays every asset out in layer order, assigning <see cref="AssetHeader.Offset"/> and
    /// <see cref="AssetHeader.Plus"/> as it goes, and returns the resulting
    /// <see cref="StreamData"/> data.
    /// </summary>
    /// <remarks>
    /// Each <c>Layer</c> pads its own end up to the platform's data alignment, so every <c>Layer</c> -
    /// and the archive itself - starts and ends on that boundary. That padding belongs to the
    /// <c>Layer</c>, not its last <c>Asset</c>, so <see cref="AssetHeader.Plus"/> stays 0 there.
    /// </remarks>
    private byte[] BuildData(
        List<Asset> ordered,
        Dictionary<AssetId, byte[]> serialized,
        Dictionary<AssetId, AssetHeader> headers,
        long dataStart)
    {
        var lastInLayer = _layers
            .Where(layer => layer.Assets.Count > 0)
            .Select(layer => layer.Assets[^1])
            .ToHashSet();

        uint layerAlignment = DataAlignmentFor(_platform);

        using var data = new MemoryStream();
        long position = dataStart;

        for (int i = 0; i < ordered.Count; i++)
        {
            var asset = ordered[i];
            byte[] bytes = serialized[asset.Id];
            var header = headers[asset.Id];

            // A zero-size asset points at nothing real, and real archives don't agree on what to put
            // there (0, a shared address, even something platform-dependent) - so replay whatever an
            // asset that was already zero-size at open originally had, rather than guessing a rule.
            header.Offset = bytes.Length == 0 && _originalZeroSizeOffsets.TryGetValue(asset.Id, out uint originalOffset)
                ? originalOffset
                : (uint)position;
            data.Write(bytes);
            position += bytes.Length;

            if (i == ordered.Count - 1)
            {
                header.Plus = 0;
                data.Write(FillBytes((int)((layerAlignment - position % layerAlignment) % layerAlignment)));
                continue;
            }

            var next = ordered[i + 1];

            if (lastInLayer.Contains(asset))
            {
                header.Plus = 0;

                uint layerPadding = (uint)((layerAlignment - position % layerAlignment) % layerAlignment);
                data.Write(GapBytesFor(asset.Id, next.Id, (int)layerPadding));
                position += layerPadding;
                continue;
            }

            var nextHeader = headers[next.Id];
            int nextAlignment = nextHeader.Debug.Alignment;
            uint alignment = nextAlignment > 0
                ? (uint)nextAlignment
                : DefaultAlignmentFor(nextHeader.Type, _platform);
            uint plus = (uint)((alignment - position % alignment) % alignment);

            header.Plus = plus;
            data.Write(GapBytesFor(asset.Id, next.Id, (int)plus));
            position += plus;
        }

        return data.ToArray();
    }

    /// <summary>
    /// The bytes filling the gap between two adjacent assets: the originals if both ends are
    /// unchanged and still adjacent (real gaps aren't always flat fill), otherwise flat fill.
    /// </summary>
    private byte[] GapBytesFor(AssetId before, AssetId after, int expectedLength)
    {
        bool unchanged = !_changedLookup.Contains(before) && !_changedLookup.Contains(after);

        return unchanged
            && _capturedGaps.TryGetValue((before, after), out byte[]? captured)
            && captured.Length == expectedLength
                ? captured
                : FillBytes(expectedLength);
    }

    private byte[] FillBytes(int length)
    {
        var bytes = new byte[length];
        Array.Fill(bytes, _fillByte);
        return bytes;
    }

    /// <summary>
    /// Where <see cref="StreamData.Data"/> would start if serialized right now, and how much fill
    /// that needs after it to reach the platform's data alignment.
    /// </summary>
    private (long DataStart, int PaddingAmount) MeasureDataStart()
    {
        long dataStart = AssetReader.MeasureThrough(_archive, _stream);
        long dataAlignment = DataAlignmentFor(_platform);
        return (dataStart, (int)((dataAlignment - dataStart % dataAlignment) % dataAlignment));
    }

    /// <inheritdoc/>
    public void Dispose() => Commit();
}
