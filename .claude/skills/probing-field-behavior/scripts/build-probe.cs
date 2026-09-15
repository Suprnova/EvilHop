#:project ../../../../src/EvilHop/EvilHop.csproj
#pragma warning disable CA1303, CA1031, CA1305, CA1307, CA1308, CA1310, CA1812, CA1852, CA2000
// build-probe.cs — assemble the ZZ01 probe workshop into an extracted disc: skydome, tiled floor,
// and a numbered row of rigs that differ only in the field under test. Run from the repo root:
//
//     dotnet run .claude/skills/probing-field-behavior/scripts/build-probe.cs -- [game]
//
// Everything above "THE PROBE" is workshop scaffolding shared by every game. Everything below it is
// the rig for one specific question and is meant to be rewritten per probe. See SKILL.md.
//
// HUMAN WARNING: SLOP AHEAD! convenience tooling, not library-quality code.
using EvilHop;
using EvilHop.Assets;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;
using System.Text.RegularExpressions;

// ===================== local configuration =====================
// Where your IndustrialPark-EditorFiles checkout lives. Everything else is repo-relative.
string editorFiles = Environment.GetEnvironmentVariable("INDUSTRIALPARK_EDITORFILES")
    ?? @"C:\Users\s1rba\Documents\Development\IndustrialPark-EditorFiles";

var setups = new Dictionary<string, Setup>(StringComparer.OrdinalIgnoreCase)
{
    ["bfbb"] = new(GameVersion.BFBB, @"BattleForBikiniBottom\GameCube\Utility", @"dump\bfbb-gc",
        "sb.ini", "zz", "ZZ01",
        FloorFrom: @"bc\bc01.HOP", SkyFrom: @"jf\jf02.HOP",
        ReferenceSurfaceArchive: @"bb\bb03.HIP", ReferenceSurfaceName: "HAZARD_SURF"),

    ["tssm"] = new(GameVersion.TSSM, @"MovieGame\GameCube\Utility", @"dump\tssm-gc",
        "SB04.ini", "ZZ", "ZZ01",
        FloorFrom: @"TT\tt01.HOP", SkyFrom: @"BB\bb01.HOP",
        ReferenceSurfaceArchive: @"BB\bb03.HIP", ReferenceSurfaceName: "DAMAGE_SURF"),
};

var setup = setups[args.Length > 0 ? args[0] : "bfbb"];
string files = Path.Combine(setup.Disc, "files");
Console.WriteLine($"=== {setup.Game} -> {files}\\{setup.SlotDir}\\{setup.Slot.ToLowerInvariant()}");

// ===================== scaffolding =====================

FormatProfile DefaultProfile(GameVersion game) => game switch
{
    GameVersion.N100F => N100FSerializer.DefaultProfile,
    GameVersion.BFBB => BFBBSerializer.DefaultProfile,
    GameVersion.TSSM => TSSMSerializer.DefaultProfile,
    GameVersion.Incredibles => IncrediblesSerializer.DefaultProfile,
    GameVersion.ROTU => ROTUSerializer.DefaultProfile,
    _ => RatatouilleSerializer.DefaultProfile
};

/// <summary>
/// Opens an archive under <paramref name="setup"/>'s game rather than a sniffed one - sniffing
/// cannot tell TSSM, The Incredibles and Ratatouille apart, since they share a format version.
/// IndustrialPark archives additionally omit <c>DPAK</c>'s padding-amount field, and reading one
/// under the official profile shifts every asset offset four bytes and silently degrades whatever
/// falls outside the <c>DPAK</c>.
/// </summary>
(Archive Archive, AssetSession Session) Open(string path, bool communityBuilt = false, GameVersion? asGame = null)
{
    byte[] bytes = File.ReadAllBytes(path);
    var profile = DefaultProfile(asGame ?? setup.Game) with { StreamDataHasPaddingField = !communityBuilt };

    var archive = Archive.Load(new MemoryStream(bytes), Serializer.Create(profile));
    var session = archive.OpenAssets();
    foreach (var diagnostic in session.Diagnostics.Take(3))
        Console.WriteLine($"  !! {Path.GetFileName(path)}: {diagnostic.Message}");
    return (archive, session);
}

static Layer LayerOf(AssetSession session, LayerType type) =>
    session.Layers.FirstOrDefault(layer => layer.Type == type) ?? session.CreateLayer(type);

/// <summary>Moves named assets out of one archive's session and into another's.</summary>
static void Borrow(AssetSession from, AssetSession into, params string[] names)
{
    foreach (var source in from.Layers.SelectMany(layer => layer.Assets).ToList())
    {
        if (!names.Contains(source.Name, StringComparer.OrdinalIgnoreCase)) continue;
        source.Layer!.Remove(source);
        LayerOf(into, source.Type switch
        {
            AssetType.Texture => LayerType.Texture,
            AssetType.Model => LayerType.Model,
            _ => LayerType.Default
        }).Add(source);
        Console.WriteLine($"  borrowed {source.Type} '{source.Name}' {source.Id}");
    }
}

/// <summary>Points the boot ini at the workshop and skips the menu, leaving CRLF line endings and
/// every other byte untouched. The original is kept alongside as <c>.orig</c>.</summary>
void PointBootIni()
{
    string path = Path.Combine(files, setup.Ini);
    if (!File.Exists(path + ".orig")) File.Copy(path, path + ".orig");

    string text = File.ReadAllText(path + ".orig");
    text = Regex.Replace(text, @"(?m)^(\s*BOOT\s*=\s*)\S+", $"${{1}}{setup.Slot}");
    text = Regex.Replace(text, @"(?m)^(\s*ShowMenuOnBoot\s*=\s*)\S+", "${1}0");
    File.WriteAllText(path, text);

    Console.WriteLine($"  {setup.Ini}: BOOT={setup.Slot}, ShowMenuOnBoot=0");
}

string SlotPath(string extension) =>
    Path.Combine(files, setup.SlotDir, setup.Slot.ToLowerInvariant() + extension);

Directory.CreateDirectory(Path.Combine(files, setup.SlotDir));

// ---------------- the HOP: every model and texture the level needs ----------------
var (hop, hopSession) = Open(Path.Combine(editorFiles, setup.Template, "blank.HOP"), communityBuilt: true);

// Every model and texture comes from the target game's own disc. They are NOT interchangeable
// between games even where the name and asset ID match: TSSM re-exported them under a different
// RenderWare version - 'disco_floor_A_3m' is 816 bytes in BFBB and 736 in TSSM - and loading
// BFBB's copies under TSSM crashes the game.
var (_, floorSession) = Open(Path.Combine(files, setup.FloorFrom));
Borrow(floorSession, hopSession, "disco_floor_A_3m", "disco_floor.RW3");

var (_, skySession) = Open(Path.Combine(files, setup.SkyFrom));
Borrow(skySession, hopSession, "skydome_jf", "jf_sky_color.RW3");

// Queries hopSession live rather than a snapshot, so a probe can Borrow() more models partway
// through and still resolve them - the HOP isn't committed/saved until every borrow is done, below.
AssetId Model(params string[] candidates) =>
    candidates.Select(name => hopSession.Layers.SelectMany(l => l.Assets)
            .FirstOrDefault(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.Id ?? default)
        .FirstOrDefault(id => id != default);

// A shipped SURF of the kind under test, for values this probe should not be inventing - notably
// TSSM onward, where every surface carries ~140 bytes of ExtendedData whose layout is unknown.
var (_, referenceSession) = Open(Path.Combine(files, setup.ReferenceSurfaceArchive));
var reference = referenceSession.Layers.SelectMany(l => l.Assets).OfType<SurfaceAsset>()
    .First(s => s.Name.Equals(setup.ReferenceSurfaceName, StringComparison.OrdinalIgnoreCase));
Console.WriteLine($"  reference SURF '{reference.Name}': ExtendedData={reference.ExtendedData.Length}B");

// ---------------- the HIP: the level ----------------
var (hip, hipSession) = Open(Path.Combine(editorFiles, setup.Template, "blank.HIP"), communityBuilt: true);
var layer = LayerOf(hipSession, LayerType.Default);

var spawn = hipSession.Layers.SelectMany(l => l.Assets).OfType<EntityAsset>()
    .First(a => a.Type == AssetType.Player).Position;

int count = 0;

/// <summary>A <see cref="SimpleObjectAsset"/> carrying <paramref name="model"/>, with field values
/// copied from a shipped SIMP rather than guessed.</summary>
SimpleObjectAsset Place(string name, AssetId model, Vector3 position, float scale,
    SimpleObjectCollisionType collision, AssetId surface = default, Rgba? tint = null, float yaw = 0f)
{
    var asset = new SimpleObjectAsset
    {
        Name = name,
        BaseFlags = BaseAssetFlags.Enabled | BaseAssetFlags.Valid
            | BaseAssetFlags.VisibleDuringCutscenes | BaseAssetFlags.ReceiveShadows,
        EntityFlags = EntityFlags.Visible,
        Position = position,
        Angle = new Vector3(yaw, 0f, 0f),
        Scale = new Vector3(scale, scale, scale),
        ColorMultiplier = new Rgba(1f, 1f, 1f, 1f),
        AnimationSpeed = 1f,
        CollisionType = collision
    };

    if (tint is { } colour) asset.ColorMultiplier = colour;

    asset.CalculateId();
    asset.Physical.BaseType = 11;
    asset.Physical.CollisionFlags = collision == SimpleObjectCollisionType.None
        ? CollisionFlags.None : CollisionFlags.PreciseCollision;
    asset.Physical.SeeThroughSpeed = 255f;
    asset.Physical.ModelId = model;
    asset.Physical.SurfaceId = surface;
    asset.Physical.Flags = AssetFlags.SourceVirtual;

    layer.Add(asset);
    count++;
    return asset;
}

/// <summary>Like <see cref="Place"/>, but rotated by a full <see cref="Vector3"/> of radians rather
/// than just a yaw - for a tilted ramp, where which component is pitch is exactly what's under
/// test.</summary>
SimpleObjectAsset PlaceTilted(string name, AssetId model, Vector3 position, float scale,
    Vector3 angle, AssetId surface, Rgba tint)
{
    var asset = Place(name, model, position, scale, SimpleObjectCollisionType.Static, surface, tint);
    asset.Angle = angle;
    return asset;
}

// Skydome, collision off so it can never interfere with a measurement. Without it the framebuffer
// is never cleared and the level renders as hall-of-mirrors.
Place("zz_skydome", Model("skydome_jf"), Vector3.Zero, 2.07f, SimpleObjectCollisionType.None);

// Floor, one unit below the spawn so the player always lands on it rather than inside it -
// templates do not agree on spawn height. Collision scales with Scale, so a large arena is cheap.
float floorY = spawn.Y - 1f;
const float TileStep = 3f * 4f;
for (float x = -30f; x <= 225f; x += TileStep)
    for (float z = -42f; z <= 42f; z += TileStep)
        Place($"zz_floor_{count:D3}", Model("disco_floor_A_3m"),
            new Vector3(spawn.X + x, floorY, spawn.Z + z), 4f, SimpleObjectCollisionType.Static);

// ======================= THE PROBE — rewrite below this line =======================
// Round three. Pads 1-7 are settled (see probes/surf-physicsflags.md) and kept only as a rig sanity
// check: bits 2 (Step, null), 3 (PreventStanding), 4 (OutOfBounds) and OutOfBoundsDelay (0.5/2/20,
// all exact) are confirmed. This round replaces the broken dynamic-platform teeter rig with static
// tilted ramps - see the block below.

/// <summary>A SURF matching the shipped damage reference except for PhysFlags/GameDamageType.
/// <paramref name="outOfBoundsDelay"/> defaults to the reference's own -1 ("never trigger", per
/// <c>zSurfaceGetOutOfBoundsDelay</c>'s no-surface fallback) - every bit-4 pad needs a real, finite
/// override or the flag would look dead no matter what it does. BFBB's own 'OUTOFBOUNDS_SURF' uses
/// 2 seconds. <paramref name="slideAngles"/> defaults to the reference's own 20/10.</summary>
SurfaceAsset Surface(string name, byte physFlags, byte damageType, float? outOfBoundsDelay = null,
    (byte Start, byte Stop)? slideAngles = null)
{
    var surface = new SurfaceAsset
    {
        Name = name,
        BaseFlags = reference.BaseFlags,
        GameDamageType = (SurfaceGameDamageType)damageType,
        PhysFlags = (SurfacePhysicsFlags)physFlags,
        Friction = reference.Friction,
        SlideStartAngle = slideAngles?.Start ?? reference.SlideStartAngle,
        SlideStopAngle = slideAngles?.Stop ?? reference.SlideStopAngle,
        OutOfBoundsDelay = outOfBoundsDelay ?? reference.OutOfBoundsDelay,
        WallJumpScaleXZ = reference.WallJumpScaleXZ,
        WallJumpScaleY = reference.WallJumpScaleY,
        IsEnabled = true,
        ExtendedData = reference.ExtendedData
    };

    surface.CalculateId();
    surface.Physical.BaseType = 26;
    surface.Physical.Flags = AssetFlags.SourceVirtual;

    layer.Add(surface);
    count++;
    return surface;
}

// Round two adds pads 6-7: bit 4 was confirmed to gate a delayed reset in round one, but the delay
// didn't match OutOfBoundsDelay (set to 2, matching BFBB's own OUTOFBOUNDS_SURF; actual wait was 9
// seconds). Two clearly different values, both far from 2 and from each other, tell us whether the
// wait tracks the field at all.
(string Label, byte PhysFlags, byte DamageType, float? OobDelay, Rgba Tint)[] padVariants =
[
    ("flags=0  (control)",                    0, 0, null, new Rgba(0.3f, 0.4f, 1f, 1f)),
    ("flags=4  Step",                         4, 0, null, new Rgba(0.6f, 0.2f, 0.8f, 1f)),
    ("flags=8  PreventStanding",              8, 0, null, new Rgba(0.9f, 0.6f, 0.1f, 1f)),
    ("flags=16 OutOfBounds delay=2",         16, 0, 2f,   new Rgba(0.1f, 0.8f, 0.8f, 1f)),
    ("flags=0  damage=1 (control+)",          0, 1, null, new Rgba(1f, 0.2f, 0.2f, 1f)),
    ("flags=16 OutOfBounds delay=0.5",       16, 0, 0.5f, new Rgba(0.2f, 1f, 0.4f, 1f)),
    ("flags=16 OutOfBounds delay=20",        16, 0, 20f,  new Rgba(0.1f, 0.3f, 0.15f, 1f)),
];

// Pads sit just above the floor so the player unambiguously contacts the pad's surface rather than
// the untouched floor beneath it.
const float PadStep = 12f;
const float PadLift = 0.35f;
Console.WriteLine($"  spawn {spawn}; pad row runs +X, {PadStep} apart:");

for (int i = 0; i < padVariants.Length; i++)
{
    var (label, physFlags, damageType, oobDelay, tint) = padVariants[i];
    var surface = Surface($"zz_surf_{i + 1:D2}", physFlags, damageType, oobDelay);
    var position = new Vector3(spawn.X + ((i + 1) * PadStep), floorY + PadLift, spawn.Z);
    Place($"zz_pad_{i + 1:D2}", Model("disco_floor_A_3m"), position, 3f,
        SimpleObjectCollisionType.Static, surface.Id, tint);

    // Tally markers beside each pad, in rows of five, so its number can be read off at a glance.
    // Tint alone is not enough: TSSM ignores a SimpleObject's colour multiplier and renders every
    // pad the same shade. Collision off so they cannot affect what the pad is measuring.
    for (int t = 0; t <= i; t++)
        Place($"zz_tally_{i + 1:D2}_{t:D2}", Model("disco_floor_A_3m"),
            new Vector3(position.X - 4f + (t % 5 * 2f), floorY + PadLift, position.Z - 8f - (t / 5 * 2.5f)),
            0.5f, SimpleObjectCollisionType.None);

    Console.WriteLine($"    #{i + 1} x={position.X,6:F1}  {label}");
}

// ---- tilted ramps: bits 0 (Slide) and 1 (MatchOrient), and whether slide angles affect
// PreventStanding ----
// The dynamic TeeterTotterMotion rig (see probes/surf-physicsflags.md) never worked - no tilt, no
// difference between PhysFlags values, cause not found in the asset's own data. This drops the
// dynamic platform entirely: a plain SimpleObject, statically tilted via Angle, needs nothing but a
// rotation, and PreciseCollision already handles rotated static geometry correctly (the wall-jump
// probe's walls rely on the same thing).
//
// Angle's three components are confirmed, empirically, to be (yaw, roll, pitch) - round one of this
// ramp rig guessed X for pitch (wrong: it's yaw, so those ramps were flat) and included Y/Z as a
// fallback, which is how this was found. Pitch is Z.
//
// Run in both games, not just BFBB: no TSSM SURF in the corpus sets bits 0/1, but that says nothing
// about whether the engine still implements them there - same reasoning as every field probed so
// far that turned out to be constant in shipped content without being dead.
{
    const float TiltAngle = 0.5236f; // 30 degrees: clearly past both SlideStartAngle (20) and
                                      // SlideStopAngle (10), so full sliding is unambiguous if Slide works.
    const float RampScale = 4f;
    const float RampStep = 12f;
    int rampBase = padVariants.Length;
    Console.WriteLine($"  ramp row continues +X, {RampStep} apart, pitched {TiltAngle:F2} rad (Z):");

    (string Label, byte PhysFlags, (byte, byte)? SlideAngles, Vector3 Angle, Rgba Tint)[] ramps =
    [
        ("flags=0  neither",                     0, null,    new Vector3(0, 0, TiltAngle), new Rgba(0.3f, 0.4f, 1f, 1f)),
        ("flags=1  Slide only",                   1, null,    new Vector3(0, 0, TiltAngle), new Rgba(0.9f, 0.9f, 0.3f, 1f)),
        ("flags=2  MatchOrient only",             2, null,    new Vector3(0, 0, TiltAngle), new Rgba(0.3f, 0.9f, 0.9f, 1f)),
        ("flags=3  both (shipped value)",         3, null,    new Vector3(0, 0, TiltAngle), new Rgba(0.3f, 1f, 0.3f, 1f)),
        ("flags=8  PreventStanding, slide=20/10", 8, (20, 10), new Vector3(0, 0, TiltAngle), new Rgba(0.9f, 0.6f, 0.1f, 1f)),
        ("flags=8  PreventStanding, slide=1/1",   8, (1, 1),   new Vector3(0, 0, TiltAngle), new Rgba(0.6f, 0.3f, 0f, 1f)),
    ];

    for (int i = 0; i < ramps.Length; i++)
    {
        var (label, physFlags, slideAngles, angle, tint) = ramps[i];
        int num = rampBase + i + 1;
        var surface = Surface($"zz_ramp_{num:D2}_surf", physFlags, 0, slideAngles: slideAngles);
        var position = new Vector3(spawn.X + (num * RampStep), floorY + 1f, spawn.Z);
        PlaceTilted($"zz_ramp_{num:D2}", Model("disco_floor_A_3m"), position, RampScale, angle, surface.Id, tint);

        for (int t = 0; t <= i; t++)
            Place($"zz_tally_{num:D2}_{t:D2}", Model("disco_floor_A_3m"),
                new Vector3(position.X - 4f + (t % 5 * 2f), floorY + PadLift, position.Z - 8f - (t / 5 * 2.5f)),
                0.5f, SimpleObjectCollisionType.None);

        Console.WriteLine($"    #{num} x={position.X,6:F1}  {label}");
    }
}

// ======================= end of probe =======================

hopSession.Commit();
using (var stream = File.Create(SlotPath(".HOP"))) hop.Save(stream);
hipSession.Commit();
using (var stream = File.Create(SlotPath(".HIP"))) hip.Save(stream);
PointBootIni();
Console.WriteLine($"  {count} assets; wrote {setup.Slot}.HIP and {setup.Slot}.HOP");

/// <summary>Where one game's workshop comes from and goes to.</summary>
record Setup(
    GameVersion Game,
    string Template,     // IndustrialPark blank.HIP/.HOP directory, relative to editorFiles
    string Disc,         // extracted disc, relative to the repo root
    string Ini,          // boot ini inside the disc's files/
    string SlotDir,      // level folder to create; case matters on the disc
    string Slot,         // four-character scene id
    string FloorFrom, string SkyFrom,
    string ReferenceSurfaceArchive, string ReferenceSurfaceName);
