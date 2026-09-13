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
        FloorFrom: @"bc\bc01.HOP", SkyFrom: @"jf\jf02.HOP", RigFrom: @"bc\bc02.HOP",
        RigModel: "wall_jump_high", RigTexture: "walljump_symbol.RW3", RigScale: 2f,
        ReferenceSurfaceArchive: @"bc\bc02.HIP", ReferenceSurfaceName: "WALLJUMP_SURFACE"),

    ["tssm"] = new(GameVersion.TSSM, @"MovieGame\GameCube\Utility", @"dump\tssm-gc",
        "SB04.ini", "ZZ", "ZZ01",
        FloorFrom: @"TT\tt01.HOP", SkyFrom: @"BB\bb01.HOP", RigFrom: @"PT\pt01.HOP",
        RigModel: "pt_wall_jump_vert", RigTexture: "pt_sign_walljump.RW3", RigScale: 1.5f,
        ReferenceSurfaceArchive: @"BB\bb02.HIP", ReferenceSurfaceName: "WALLJUMP_SURF_01"),
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

// Rig models: whatever the shipped rig for this mechanic uses.
var (_, rigSession) = Open(Path.Combine(files, setup.RigFrom));
Borrow(rigSession, hopSession, setup.RigModel, setup.RigTexture);

var models = hopSession.Layers.SelectMany(l => l.Assets)
    .ToDictionary(a => a.Name, a => a.Id, StringComparer.OrdinalIgnoreCase);

AssetId Model(params string[] candidates) =>
    candidates.Select(name => models.TryGetValue(name, out var id) ? id : default)
        .FirstOrDefault(id => id != default);

hopSession.Commit();
using (var stream = File.Create(SlotPath(".HOP"))) hop.Save(stream);

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
    SimpleObjectCollisionType collision, AssetId surface = default, RgbaColor? tint = null, float yaw = 0f)
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
        ColorMultiplier = new RgbaColor(1f, 1f, 1f, 1f),
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

// Skydome, collision off so it can never interfere with a measurement. Without it the framebuffer
// is never cleared and the level renders as hall-of-mirrors.
Place("zz_skydome", Model("skydome_jf"), Vector3.Zero, 2.07f, SimpleObjectCollisionType.None);

// Floor, one unit below the spawn so the player always lands on it rather than inside it -
// templates do not agree on spawn height. Collision scales with Scale, so a large arena is cheap.
float floorY = spawn.Y - 1f;
const float TileStep = 3f * 4f;
for (float x = -30f; x <= 120f; x += TileStep)
    for (float z = -42f; z <= 42f; z += TileStep)
        Place($"zz_floor_{count:D3}", Model("disco_floor_A_3m"),
            new Vector3(spawn.X + x, floorY, spawn.Z + z), 4f, SimpleObjectCollisionType.Static);

// ======================= THE PROBE — rewrite below this line =======================
// One row of rigs running +X from the spawn, numbered west to east so the tester can report by
// position. Variant 1 is the control: the value every shipped asset uses. Include a positive
// control varying a field known to work, or a null result proves nothing.

/// <summary>A SURF matching the shipped reference except for the fields under test.</summary>
SurfaceAsset Surface(string name, float scaleXZ, float scaleY, SurfacePhysicsFlags physFlags)
{
    var surface = new SurfaceAsset
    {
        Name = name,
        BaseFlags = reference.BaseFlags,
        PhysFlags = physFlags,
        Friction = reference.Friction,
        SlideStartAngle = reference.SlideStartAngle,
        SlideStopAngle = reference.SlideStopAngle,
        OutOfBoundsDelay = reference.OutOfBoundsDelay,
        WallJumpScaleXZ = scaleXZ,
        WallJumpScaleY = scaleY,
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

const SurfacePhysicsFlags WallJump = SurfacePhysicsFlags.WallJump;

(string Label, float XZ, float Y, RgbaColor Tint, SurfacePhysicsFlags Flags)[] variants =
[
    ("XZ=1 (control)",  1f,  1f, new RgbaColor(1f, 1f, 1f, 1f), WallJump),
    ("XZ=0",            0f,  1f, new RgbaColor(1f, 0.3f, 0.3f, 1f), WallJump),
    ("XZ=3",            3f,  1f, new RgbaColor(0.3f, 1f, 0.3f, 1f), WallJump),
    ("XZ=10",          10f,  1f, new RgbaColor(0.3f, 0.4f, 1f, 1f), WallJump),
    ("XZ=-3",          -3f,  1f, new RgbaColor(1f, 1f, 0.3f, 1f), WallJump),
    ("Y=5 (positive control, shipped)", 1f,  5f, new RgbaColor(1f, 0.3f, 1f, 1f), WallJump),
    ("Y=15 (positive control)",         1f, 15f, new RgbaColor(0.3f, 1f, 1f, 1f), WallJump),
    ("XZ=10, PhysFlags=None (gate)",   10f,  1f, new RgbaColor(1f, 0.55f, 0.1f, 1f), SurfacePhysicsFlags.None),
];

var rigModel = Model(setup.RigModel);
const float WallStep = 18f;
Console.WriteLine($"  spawn {spawn}; probe row runs +X, {WallStep} apart:");

for (int i = 0; i < variants.Length; i++)
{
    var (label, scaleXZ, scaleY, tint, physFlags) = variants[i];
    var surface = Surface($"zz_surf_{i + 1:D2}", scaleXZ, scaleY, physFlags);
    var position = new Vector3(spawn.X + (i * WallStep), floorY, spawn.Z + 12f);
    Place($"zz_wall_{i + 1:D2}", rigModel, position, setup.RigScale, SimpleObjectCollisionType.Static, surface.Id, tint);
    Console.WriteLine($"    #{i + 1} x={position.X,6:F1}  {label}");
}

// ======================= end of probe =======================

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
    string FloorFrom, string SkyFrom, string RigFrom,
    string RigModel, string RigTexture, float RigScale,
    string ReferenceSurfaceArchive, string ReferenceSurfaceName);
