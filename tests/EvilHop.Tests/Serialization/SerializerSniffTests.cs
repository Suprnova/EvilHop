using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Serialization;
using EvilHop.Serialization.Sniffing;
using System.Text;

namespace EvilHop.Tests.Serialization;

public class SerializerSniffTests
{
    private static string FourCC(AssetType type) => Encoding.ASCII.GetString(
        [(byte)((uint)type >> 24), (byte)((uint)type >> 16), (byte)((uint)type >> 8), (byte)type]);

    private static readonly string[] BFBBMarkers =
    [
        FourCC(AssetType.DestructibleObject), FourCC(AssetType.SoundFX), FourCC(AssetType.SimpleShadowTable),
        FourCC(AssetType.UI), FourCC(AssetType.UIFont), FourCC(AssetType.VillainProperties)
    ];

    private static readonly string[] IncrediblesMarkers =
    [
        FourCC(AssetType.AttackTable), FourCC(AssetType.DashTrack), FourCC(AssetType.Duplicator),
        FourCC(AssetType.GrassMesh), FourCC(AssetType.OneLiner), FourCC(AssetType.SlideProperty),
        FourCC(AssetType.SceneSettings), FourCC(AssetType.ZipLine)
    ];

    private static readonly string[] TSSMMarkers =
    [
        FourCC(AssetType.DiscoFloor), FourCC(AssetType.ElectricArcGenerator), FourCC(AssetType.JawDataTable),
        FourCC(AssetType.Pickup), FourCC(AssetType.ParticleEmitter), FourCC(AssetType.ParticleEmitterProperty),
        FourCC(AssetType.ParticleSystem)
    ];

    private static readonly string[] ROTUMarkers = [FourCC(AssetType.Hangable), FourCC(AssetType.Volume)];

    private static readonly string[] FourStringPlat = ["GC", "US", "NTSC", "Test"];
    private static readonly string[] FiveStringPlat = ["GC", "GameCube", "NTSC", "US Common", "Test"];

    [Fact]
    public void Sniff_ValidHipaAndPack_HasHipaMagicTrueRegardlessOfConfidence()
    {
        byte[] bytes = new SniffFixture().Build();

        var result = Serializer.Sniff(new MemoryStream(bytes));

        Assert.True(result.Signals.HasHipaMagic);
    }

    [Fact]
    public void Sniff_N100FRelease_ResolvesN100F()
    {
        byte[] bytes = new SniffFixture
        {
            ClientVersion = ClientVersion.N100FRelease,
            Created = new DateTimeOffset(2002, 1, 1, 0, 0, 0, TimeSpan.Zero)
        }.Build();

        var result = Serializer.Sniff(new MemoryStream(bytes));

        Assert.Equal(SniffConfidence.Resolved, result.Confidence);
        Assert.Equal(GameVersion.N100F, Assert.Single(result.Candidates).Game);
        Assert.Equal(GameVersion.N100F, result.Profile!.Game);
        Assert.Equal(PlatformFieldOrder.PlatformNameRegionLanguage, result.Profile.PlatformFieldOrder);
    }

    [Fact]
    public void Sniff_Bfbb_FiveStringPlatAndMarkers_ResolvesBFBB()
    {
        byte[] bytes = new SniffFixture
        {
            ClientVersion = ClientVersion.Default,
            PlatformStrings = FiveStringPlat,
            AssetTypes = BFBBMarkers,
            Created = new DateTimeOffset(2003, 9, 1, 0, 0, 0, TimeSpan.Zero)
        }.Build();

        var result = Serializer.Sniff(new MemoryStream(bytes));

        Assert.Equal(SniffConfidence.Resolved, result.Confidence);
        Assert.Equal(GameVersion.BFBB, Assert.Single(result.Candidates).Game);
        Assert.Equal(GameVersion.BFBB, result.Profile!.Game);
        Assert.True(result.Profile.EntityHasPadding);
    }

    [Fact]
    public void Sniff_Incredibles_FourStringPlatAndMarkers_ResolvesIncredibles()
    {
        byte[] bytes = new SniffFixture
        {
            ClientVersion = ClientVersion.Default,
            PlatformStrings = FourStringPlat,
            AssetTypes = IncrediblesMarkers,
            Created = new DateTimeOffset(2004, 8, 1, 0, 0, 0, TimeSpan.Zero)
        }.Build();

        var result = Serializer.Sniff(new MemoryStream(bytes));

        Assert.Equal(SniffConfidence.Resolved, result.Confidence);
        Assert.Equal(GameVersion.Incredibles, Assert.Single(result.Candidates).Game);
        Assert.Equal(GameVersion.Incredibles, result.Profile!.Game);
        Assert.Equal(PlatformFieldOrder.LanguageRegion, result.Profile.PlatformFieldOrder);
    }

    [Fact]
    public void Sniff_TSSM_FourStringPlatAndMarkers_ResolvesTSSM()
    {
        byte[] bytes = new SniffFixture
        {
            ClientVersion = ClientVersion.Default,
            PlatformStrings = FourStringPlat,
            AssetTypes = TSSMMarkers,
            Created = new DateTimeOffset(2004, 11, 15, 0, 0, 0, TimeSpan.Zero)
        }.Build();

        var result = Serializer.Sniff(new MemoryStream(bytes));

        Assert.Equal(SniffConfidence.Resolved, result.Confidence);
        Assert.Equal(GameVersion.TSSM, Assert.Single(result.Candidates).Game);
        Assert.Equal(GameVersion.TSSM, result.Profile!.Game);
    }

    [Fact]
    public void Sniff_ROTU_FourStringPlatAndMarkers_ResolvesROTU()
    {
        byte[] bytes = new SniffFixture
        {
            ClientVersion = ClientVersion.Default,
            PlatformStrings = FourStringPlat,
            AssetTypes = ROTUMarkers,
            Created = new DateTimeOffset(2005, 10, 1, 0, 0, 0, TimeSpan.Zero)
        }.Build();

        var result = Serializer.Sniff(new MemoryStream(bytes));

        Assert.Equal(SniffConfidence.Resolved, result.Confidence);
        Assert.Equal(GameVersion.ROTU, Assert.Single(result.Candidates).Game);
        Assert.Equal(GameVersion.ROTU, result.Profile!.Game);
    }

    [Fact]
    public void Sniff_Ratatouille_FourStringPlatAndOwnDateRange_ResolvesRatatouille()
    {
        byte[] bytes = new SniffFixture
        {
            ClientVersion = ClientVersion.Default,
            PlatformStrings = FourStringPlat,
            Created = new DateTimeOffset(2006, 1, 11, 0, 0, 0, TimeSpan.Zero)
        }.Build();

        var result = Serializer.Sniff(new MemoryStream(bytes));

        Assert.Equal(SniffConfidence.Resolved, result.Confidence);
        Assert.Equal(GameVersion.Ratatouille, Assert.Single(result.Candidates).Game);
        Assert.Equal(GameVersion.Ratatouille, result.Profile!.Game);
    }

    [Fact]
    public void Sniff_N100FPrototype_NoDpakPaddingObserved_StreamDataHasPaddingFieldIsFalse()
    {
        byte[] bytes = new SniffFixture
        {
            ClientVersion = ClientVersion.N100FPrototype,
            Created = new DateTimeOffset(2001, 6, 11, 0, 0, 0, TimeSpan.Zero),
            DpakPaddingPresent = false
        }.Build();

        var result = Serializer.Sniff(new MemoryStream(bytes));

        Assert.Null(result.Signals.DpakPaddingObserved);
        Assert.False(result.Profile!.StreamDataHasPaddingField);
    }

    [Fact]
    public void Sniff_FourWayGenericSignals_AllFourMarkersPresent_IsAmbiguousWithEqualScores()
    {
        // Every marker from the three marker-having candidates is present at once, so each of the
        // four non-N100F/BFBB candidates scores a perfect 1.0 - Incredibles/TSSM/ROTU on a full
        // marker-fraction match, Ratatouille on having no marker dimension to fall short on at all.
        byte[] bytes = new SniffFixture
        {
            ClientVersion = ClientVersion.Default,
            PlatformStrings = FourStringPlat,
            AssetTypes = [.. IncrediblesMarkers, .. TSSMMarkers, .. ROTUMarkers]
        }.Build();

        var result = Serializer.Sniff(new MemoryStream(bytes));

        Assert.Equal(SniffConfidence.Ambiguous, result.Confidence);
        Assert.Equal(
            [GameVersion.Incredibles, GameVersion.TSSM, GameVersion.ROTU, GameVersion.Ratatouille],
            result.Candidates.Select(c => c.Game));
        Assert.All(result.Candidates, c => Assert.Equal(result.Candidates[0].Score, c.Score, precision: 9));
    }

    [Fact]
    public void Sniff_GarbageBytes_IsUnrecognized()
    {
        byte[] garbage = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

        var result = Serializer.Sniff(new MemoryStream(garbage));

        Assert.Equal(SniffConfidence.Unrecognized, result.Confidence);
        Assert.Null(result.Profile);
        Assert.Empty(result.Candidates);
        Assert.False(result.Signals.HasHipaMagic);
    }

    [Fact]
    public void Sniff_WrongMagic_IsUnrecognized()
    {
        byte[] bytes = BlockBytes.Build("HIPZ", []);

        var result = Serializer.Sniff(new MemoryStream(bytes));

        Assert.Equal(SniffConfidence.Unrecognized, result.Confidence);
        Assert.Null(result.Profile);
        Assert.Empty(result.Candidates);
    }

    [Fact]
    public void Sniff_TruncatedPastGate_DoesNotThrowAndScoresPartialSignals()
    {
        byte[] hipa = BlockBytes.Build("HIPA", []);
        // A PVER envelope claiming a huge declared size, with no content behind it at all - PACK's
        // own size correctly covers just the 8-byte envelope header, but reading PVER's fields hits
        // end-of-stream immediately.
        byte[] truncatedPver = [.. Encoding.ASCII.GetBytes("PVER"), 0xFF, 0xFF, 0xFF, 0xFF];
        byte[] pack = BlockBytes.Build("PACK", truncatedPver);

        SniffResult? result = null;
        var exception = Record.Exception(() => result = Serializer.Sniff(new MemoryStream([.. hipa, .. pack])));

        Assert.Null(exception);
        Assert.True(result!.Signals.HasHipaMagic);
    }
}
