#:project ../../../../src/EvilHop/EvilHop.csproj
#pragma warning disable CA1303, CA1031, CA1305, CA1307, CA1308, CA1310, CA1812, CA1852, CA2000
// build-probe.cs — assemble the ZZ01 probe workshop into an extracted disc: skydome, tiled floor,
// and a numbered row of rigs that differ only in the field under test. Run from the repo root:
//
//     dotnet run .claude/skills/probing-field-behavior/scripts/build-probe.cs -- [game] [scene]
//
// [scene] builds the workshop under a shipped scene ID instead of ZZ01, for engine code keyed on the
// scene; the shipped archive it replaces is kept as .orig.
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
// Where external IndustrialPark-EditorFiles checkout lives. Everything else is repo-relative.
string editorFiles = Environment.GetEnvironmentVariable("INDUSTRIALPARK_EDITORFILES")
    ?? @"C:\Users\s1rba\Documents\Development\IndustrialPark-EditorFiles";

var setups = new Dictionary<string, Setup>(StringComparer.OrdinalIgnoreCase)
{
    ["bfbb"] = new(GameVersion.BFBB, Template.EditorFiles(@"BattleForBikiniBottom\GameCube\Utility"), @"dump\bfbb-gc",
        "sb.ini", "zz", "ZZ01",
        Floor: new(@"bc\bc01.HOP", "disco_floor_A_3m", "disco_floor.RW3"), FloorWidth: 3f,
        Sky: new(@"jf\jf02.HOP", "skydome_jf", "jf_sky_color.RW3"), SkyScale: 2.07f,
        ReferenceSurface: new(@"bb\bb03.HIP", "HAZARD_SURF")),

    ["tssm"] = new(GameVersion.TSSM, Template.EditorFiles(@"MovieGame\GameCube\Utility"), @"dump\tssm-gc",
        "SB04.ini", "ZZ", "ZZ01",
        Floor: new(@"TT\tt01.HOP", "disco_floor_A_3m", "disco_floor.RW3"), FloorWidth: 3f,
        Sky: new(@"BB\bb01.HOP", "skydome_jf", "jf_sky_color.RW3"), SkyScale: 2.07f,
        ReferenceSurface: new(@"BB\bb03.HIP", "DAMAGE_SURF")),

    // Stripped from NJ\nj03 donor level; in.ini maps ZZ01 to PLY2.
    ["incredibles"] = new(GameVersion.Incredibles,
        Template.Donor(@"NJ\nj03", "PLAYER", "Mr_I_ights", "START_CAM", "START_PRESET_CAM",
            "NEW_JOB_ENV", "lightkit_nj03", "SCENE PARAMETERS"),
        @"dump\incredibles-gc", "in.ini", "ZZ", "ZZ01",
        Floor: new(@"OM\om03.HOP", "electric_pad_on_e", "Concrete_blocks_gray_OM.RW3"), FloorWidth: 5.97f, FloorTop: 0.13f,
        Sky: new(@"NJ\nj03.HOP", "skydome_red_NJ", "sky_red2_NJ.RW3"), SkyScale: 1.5f,
        ReferenceSurface: new(@"NJ\nj03.HIP", "MRI_FAT_ROC_SURF_01"),
        IniLines: ["ScenePlayerMapping = ZZ01 PLY2"],
        SceneNames: @"MN\mnui_US.hip"),

    // N100F is single-archive (.HIP). Swaps START camera from b001 and encloses scene in floor slabs.
    ["n100f"] = new(GameVersion.N100F, Template.EditorFiles(@"Scooby\GameCube", "blank_level", "START"), @"dump\n100f-gc",
        "sd2.ini", "ZZ", "ZZ01",
        Floor: new(@"L0\l013.HIP", "gc_rfcl0003", "ocr00001.RW3"), FloorWidth: 2.7f, FloorTop: -0.03f,
        Sky: null, SkyScale: 1f,
        ReferenceSurface: new(@"E0\e003.HIP", "KILL SURFACE"),
        EmptyWorld: "EMPTYBSP",
        Extras: [new(@"B0\b001.HIP", "START")],
        SingleArchive: true),

    // Stripped from FP\fp01 donor level. Borrowed JSP from main menu satisfies xEnvLoadJSPList.
    ["rat"] = new(GameVersion.Ratatouille,
        Template.Donor(@"FP\fp01", "PLAYER_START", "STARTCAM", "ENVIRONMENT_WIDGET", "fp01_lights", "pl_lightkit"),
        @"dump\rat-gc", "rats.ini", "ZZ", "ZZ01",
        Floor: new(@"FP\fp01.HOP", "platform_4x4xp5", "4x4xp5_plat.RW3"), FloorWidth: 4f, FloorTop: 0.5f,
        FloorCentre: new(0f, 0f, -2f),
        Sky: new(@"FP\fp01.HOP", "skydome_fp", "skydome_fp.RW3"), SkyScale: 1f,
        ReferenceSurface: new(@"FP\fp01.HIP", "FOOTSTEP_ROCK_SURF"),
        IniLines: ["ScenePlayerMapping = ZZ01 PLYR"],
        SceneNames: @"MN\mnui_US.hip",
        Extras: [new(@"MN\mnus.HOP", "ui_temp_jsp0", "ui_temp_jsp")]),

    // Stripped from A2\A201 donor level with co-op heroes, rail camera, and single-quad nav mesh.
    ["rotu"] = new(GameVersion.ROTU,
        Template.Donor(@"A2\A201", "MRI_PLYR", "FRO_PLYR", "START_CAM", "ENV", "lightkit_A2",
            "W01_CAMT_01", "W01_CAMC_POI_01", "W01_CURVEP_01", "W01_CURVEC_01"),
        @"dump\rotu-gc", "in2.ini", "ZZ", "ZZ01",
        Floor: new(@"A1\a101.HOP", "electric_plate_aone", "electric_plate_aone.RW3"), FloorWidth: 1.96f, FloorTop: 0.07f,
        Sky: new(@"A2\A201.HOP", "skydome_bm", "skydome_pink_blue_ni.RW3"), SkyScale: 1f,
        ReferenceSurface: new(@"A2\A201.HIP", "FOOTSTEP_ROCK_SURF"),
        IniLines: ["ScenePlayerMapping = ZZ01 PLM1 PLF1"],
        NavMesh: new(@"MN\mnus.HOP", "MNUS_navMesh")),
};

var setup = setups[args.Length > 0 ? args[0] : "bfbb"];
if (args.Length > 1) setup = setup with { Slot = args[1].ToUpperInvariant(), SlotDir = args[1][..2].ToUpperInvariant() };
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
/// Opens an archive under <paramref name="setup"/>'s game profile, with padding handling for community archives.
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

/// <summary>Moves named assets out of one archive's session and into another's, each into a
/// layer of the same type it came from.</summary>
static void Borrow(AssetSession from, AssetSession into, params string[] names)
{
    foreach (var source in from.Layers.SelectMany(layer => layer.Assets).ToList())
    {
        if (!names.Contains(source.Name, StringComparer.OrdinalIgnoreCase)) continue;
        var layerType = source.Layer!.Type;
        source.Layer.Remove(source);
        LayerOf(into, layerType).Add(source);
        Console.WriteLine($"  borrowed {source.Type} '{source.Name}' {source.Id}");
    }
}

/// <summary>Points the boot INI at the workshop, skipping the menu while preserving line endings.
/// The original is kept alongside as <c>.orig</c>.</summary>
void PointBootIni()
{
    string path = Path.Combine(files, setup.Ini);
    if (!File.Exists(path + ".orig")) File.Copy(path, path + ".orig");

    string text = File.ReadAllText(path + ".orig");
    text = SetIniKey(text, "BOOT", setup.Slot);
    text = SetIniKey(text, "ShowMenuOnBoot", "0");
    // Normalize lines to avoid duplicate entries when matching existing configuration.
    var present = text.Split('\n').Select(NormalizeIniLine).ToHashSet(StringComparer.OrdinalIgnoreCase);
    foreach (string line in setup.IniLines ?? [])
        if (!present.Contains(NormalizeIniLine(line))) text = AppendIniLine(text, line);
    File.WriteAllText(path, text);

    Console.WriteLine($"  {setup.Ini}: BOOT={setup.Slot}, ShowMenuOnBoot=0{string.Concat((setup.IniLines ?? []).Select(l => $", {l}"))}");
}

static string SetIniKey(string text, string key, string value)
{
    var pattern = new Regex($@"(?m)^(\s*{key}\s*=\s*)\S+");
    return pattern.IsMatch(text) ? pattern.Replace(text, $"${{1}}{value}") : AppendIniLine(text, $"{key} = {value}");
}

static string NormalizeIniLine(string line) => Regex.Replace(line.Trim(), @"\s+", " ");

static string AppendIniLine(string text, string line) =>
    (text.EndsWith('\n') ? text : text + "\r\n") + line + "\r\n";

string SlotPath(string extension) =>
    Path.Combine(files, setup.SlotDir, setup.Slot.ToLowerInvariant() + extension);

/// <summary>Opens a template archive, stripping unneeded assets from donor levels.</summary>
(Archive Archive, AssetSession Session) OpenTemplate(string extension)
{
    var template = setup.Template;
    var (archive, session) = template.Keep is null
        ? Open(Path.Combine(editorFiles, template.Path, template.FileName + extension), communityBuilt: true)
        : Open(Path.Combine(files, template.Path + extension));

    foreach (var asset in session.Layers.SelectMany(l => l.Assets).ToList())
        if (template.Keep is { } keep
            ? !keep.Contains(asset.Name, StringComparer.OrdinalIgnoreCase)
            : (template.Drop ?? []).Contains(asset.Name, StringComparer.OrdinalIgnoreCase))
            asset.Layer!.Remove(asset);
    return (archive, session);
}

Directory.CreateDirectory(Path.Combine(files, setup.SlotDir));

// ---------------- the HOP: every model and texture the level needs ----------------
// N100F keeps a whole level in one HIP; there the "HOP" below is the HIP itself.
var (hop, hopSession) = OpenTemplate(setup.SingleArchive ? ".HIP" : ".HOP");

// Borrow models and textures from the target game's disc.
foreach (var prop in new[] { setup.Floor, setup.Sky }.OfType<Prop>())
{
    var (_, propSession) = Open(Path.Combine(files, prop.Archive));
    Borrow(propSession, hopSession, prop.Model, prop.Texture);
}

if (setup.NavMesh is { } navSource)
{
    var (_, navSession) = Open(Path.Combine(files, navSource.Archive));
    Borrow(navSession, hopSession, navSource.Name);
}

foreach (var extra in setup.Extras ?? [])
{
    var (_, extraSession) = Open(Path.Combine(files, extra.Archive));
    Borrow(extraSession, hopSession, extra.Names);
}

// Queries hopSession live so newly borrowed models can be resolved immediately.
AssetId Model(params string[] candidates) =>
    candidates.Select(name => hopSession.Layers.SelectMany(l => l.Assets)
            .FirstOrDefault(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.Id ?? default)
        .FirstOrDefault(id => id != default);

// Shipped SURF reference for copying base settings.
SurfaceAsset? reference = null;
if (setup.ReferenceSurface is { } referenceSource)
{
    var (_, referenceSession) = Open(Path.Combine(files, referenceSource.Archive));
    reference = referenceSession.Layers.SelectMany(l => l.Assets).OfType<SurfaceAsset>()
        .First(s => s.Name.Equals(referenceSource.Name, StringComparison.OrdinalIgnoreCase));
    Console.WriteLine($"  reference SURF '{reference.Name}'");
}

// ---------------- the HIP: the level ----------------
var (hip, hipSession) = setup.SingleArchive ? (hop, hopSession) : OpenTemplate(".HIP");
var layer = LayerOf(hipSession, LayerType.Default);

var players = hipSession.Layers.SelectMany(l => l.Assets).OfType<EntityAsset>()
    .Where(a => a.Type == AssetType.Player).ToList();
var spawn = players[0].Position;

// Every player starts facing +X down the variant row. Yaw (Angle.X) turns +Z toward +X.
foreach (var player in players)
    player.Angle = new Vector3(MathF.PI / 2, 0f, 0f);

int count = 0;

/// <summary>Creates and places a <see cref="SimpleObjectAsset"/> carrying <paramref name="model"/>.</summary>
SimpleObjectAsset Place(string name, AssetId model, Vector3 position, float scale,
    bool collidable, AssetId surface = default, Rgba? tint = null, float yaw = 0f)
{
    var asset = new SimpleObjectAsset
    {
        Name = name,
        EntityFlags = EntityFlags.Visible,
        Position = position,
        Angle = new Vector3(yaw, 0f, 0f),
        Scale = new Vector3(scale, scale, scale),
        ColorMultiplier = new Rgba(1f, 1f, 1f, 1f),
        HasCollision = collidable
    };

    if (tint is { } colour) asset.ColorMultiplier = colour;

    asset.CalculateId();
    asset.Physical.BaseFlags = BaseAssetFlags.Enabled | BaseAssetFlags.Valid
        | BaseAssetFlags.VisibleDuringCutscenes | BaseAssetFlags.ReceiveShadows;
    asset.Physical.BaseType = 11;
    asset.Physical.CollisionFlags = !collidable
        ? CollisionFlags.None : CollisionFlags.PreciseCollision;
    asset.Physical.SeeThroughSpeed = 255f;
    asset.Physical.ModelId = model;
    asset.Physical.SurfaceId = surface;
    asset.Physical.Flags = AssetFlags.SourceVirtual;

    layer.Add(asset);
    count++;
    return asset;
}

// Skydome entity: registers via SetasSkydome on ScenePrepare. Placed far below floor to prevent static batching artifacts.
if (setup.Sky is { } skyProp)
{
    const short ScenePrepare = 0x57, SetasSkydome = 0x132;
    var sky = Place("zz_skydome", Model(skyProp.Model), new Vector3(spawn.X, spawn.Y - 2000f, spawn.Z),
        setup.SkyScale, collidable: false);
    sky.Links.Add(new Link
    {
        SourceEvent = ScenePrepare,
        DestinationEvent = SetasSkydome,
        DestinationAssetId = sky.Id,
        Params = [new FloatParameter(0f), new FloatParameter(1f), new FloatParameter(1f), new FloatParameter(0f)]
    });
}

// Every tile is sized by the width it should span.
var tileModel = Model(setup.Floor.Model);
float TileScale(float width) => width / setup.FloorWidth;

/// <summary>Places the floor model scaled to span <paramref name="width"/>, with top surface centred at <paramref name="top"/>.</summary>
SimpleObjectAsset Tile(string name, Vector3 top, float width, bool collidable,
    AssetId surface = default, Rgba? tint = null) =>
    Place(name, tileModel, top - ((setup.FloorCentre + new Vector3(0f, setup.FloorTop, 0f)) * TileScale(width)),
        TileScale(width), collidable, surface, tint);

// Floor placed 1 unit below spawn, tiled on TileStep increments.
float floorY = spawn.Y - 1f;
const float TileStep = 12f;
const float FloorMinX = -36f, FloorMaxX = 228f, FloorHalfZ = 48f;
for (float x = FloorMinX; x <= FloorMaxX; x += TileStep)
    for (float z = -FloorHalfZ; z <= FloorHalfZ; z += TileStep)
        Tile($"zz_floor_{count:D3}", new Vector3(spawn.X + x, floorY, spawn.Z + z), TileStep, collidable: true);

// For games without a skydome (N100F), enclose the arena in non-colliding floor slabs.
if (setup.Sky is null)
{
    const float SlabWidth = 450f, Clearance = 10f;
    var centre = new Vector3(spawn.X + ((FloorMinX + FloorMaxX) / 2), floorY, spawn.Z);
    var halfX = ((FloorMaxX - FloorMinX) / 2) + (TileStep / 2) + Clearance;
    var halfZ = FloorHalfZ + (TileStep / 2) + Clearance;
    (string Name, Vector3 Offset, Vector3 Angle)[] slabs =
    [
        ("under", new(0f, -60f, 0f), Vector3.Zero),
        ("ceiling", new(0f, 160f, 0f), Vector3.Zero),
        ("west", new(-halfX, 0f, 0f), new(0f, 0f, MathF.PI / 2)),
        ("east", new(halfX, 0f, 0f), new(0f, 0f, MathF.PI / 2)),
        ("north", new(0f, 0f, -halfZ), new(0f, MathF.PI / 2, 0f)),
        ("south", new(0f, 0f, halfZ), new(0f, MathF.PI / 2, 0f)),
    ];
    foreach (var (name, offset, angle) in slabs)
        Place($"zz_backdrop_{name}", tileModel, centre + offset, TileScale(SlabWidth), collidable: false).Angle = angle;
}

// Stretch the single-quad nav mesh across the floor bounds.
if (setup.NavMesh is { } navMeshSource)
{
    var navMesh = hopSession.Layers.SelectMany(l => l.Assets).OfType<NavigationMeshAsset>()
        .Single(m => m.Name.Equals(navMeshSource.Name, StringComparison.OrdinalIgnoreCase));
    var min = new Vector3(spawn.X + FloorMinX - (TileStep / 2), floorY, spawn.Z - FloorHalfZ - (TileStep / 2));
    var max = new Vector3(spawn.X + FloorMaxX + (TileStep / 2), min.Y, spawn.Z + FloorHalfZ + (TileStep / 2));
    var vertices = navMesh.SubMeshes.Single().Vertices;
    for (int i = 0; i < vertices.Count; i++)
        vertices[i] = new Vector3(float.Lerp(min.X, max.X, vertices[i].X + 0.5f), min.Y, float.Lerp(min.Z, max.Z, vertices[i].Z + 0.5f));
    Console.WriteLine($"  nav mesh '{navMesh.Name}' stretched over x[{min.X:F0},{max.X:F0}] z[{min.Z:F0},{max.Z:F0}] at y={min.Y:F2}");
}

// Widen empty BSP atomic sector bounding box so xPartitionWorld bounds encompass the workshop.
if (setup.EmptyWorld is { } emptyWorld)
{
    const float Reach = 1000f;
    const int WorldChunk = 0x0B, AtomicSectorChunk = 0x09;
    var bsp = hipSession.Layers.SelectMany(l => l.Assets).OfType<PayloadAsset>()
        .Single(a => a.Name.Equals(emptyWorld, StringComparison.OrdinalIgnoreCase));
    using var buffer = new MemoryStream();
    bsp.SaveTo(buffer);
    byte[] world = buffer.ToArray();

    int Child(int parent, int type)
    {
        int end = parent + 12 + BitConverter.ToInt32(world, parent + 4);
        for (int at = parent + 12; at < end; at += 12 + BitConverter.ToInt32(world, at + 4))
            if (BitConverter.ToInt32(world, at) == type) return at;
        throw new InvalidDataException($"{emptyWorld}: no chunk 0x{type:X} under 0x{parent:X}");
    }

    if (BitConverter.ToInt32(world, 0) != WorldChunk) throw new InvalidDataException($"{emptyWorld} is not a world");
    int header = Child(Child(0, AtomicSectorChunk), 0x01) + 12;
    if (BitConverter.ToInt32(world, header + 4) != 0) throw new InvalidDataException($"{emptyWorld} is not empty");
    float[] box = [spawn.X - Reach, spawn.Y - Reach, spawn.Z - Reach, spawn.X + Reach, spawn.Y + Reach, spawn.Z + Reach];
    for (int i = 0; i < box.Length; i++)
        BitConverter.TryWriteBytes(world.AsSpan(header + 12 + (i * 4)), box[i]);

    bsp.LoadFrom(new MemoryStream(world));
    Console.WriteLine($"  {emptyWorld}: world bounds widened to {Reach} around the spawn");
}

// Relay ROTU's camera curve splines along the variant row and trigger the transition via script.
{
    const short ScenePrepare = 0x57, Run = 0x12;
    const uint CameraTransitionBegin = 0x343;
    const float CameraBack = 35f, CameraUp = 10f;
    var templateAssets = hipSession.Layers.SelectMany(l => l.Assets).ToList();

    void Lay(AssetId splineId, Vector3 offset)
    {
        var spline = templateAssets.OfType<SplineAsset>().Single(s => s.Id == splineId);
        var from = new Vector3(spawn.X - 30f, spawn.Y, spawn.Z) + offset;
        var to = new Vector3(spawn.X + 225f, spawn.Y, spawn.Z) + offset;
        for (int i = 0; i < spline.ControlPoints.Count; i++)
            spline.ControlPoints[i] = Vector3.Lerp(from, to, i / (spline.ControlPoints.Count - 1f));
    }

    foreach (var curve in templateAssets.OfType<CameraCurveAsset>())
    {
        Lay(curve.CurveId1, Vector3.Zero);
        Lay(curve.CurveId2, new Vector3(0f, CameraUp, CameraBack));
        foreach (var bead in curve.Beads)
            (bead.DistanceAdjust, bead.PitchOffset) = (0.9f, -12f);
    }

    var transitions = templateAssets.OfType<DynamicAsset>().Where(d => d.Kind is DynamicKind.CameraTransitionTime).ToList();
    if (transitions.Count > 0)
    {
        var script = new ScriptAsset
        {
            Name = "zz_camera_script",
            ScaleFactor = 1f
        };
        script.Physical.BaseFlags = BaseAssetFlags.Enabled | BaseAssetFlags.Valid
            | BaseAssetFlags.VisibleDuringCutscenes | BaseAssetFlags.ReceiveShadows;
        script.Links.Add(new Link { SourceEvent = ScenePrepare, DestinationEvent = Run, DestinationAssetId = default });
        foreach (var transition in transitions)
            script.Events.Add(new ScriptAsset.ScriptEvent { Time = 0.5f, WidgetId = transition.Id, ParamEvent = CameraTransitionBegin });
        script.CalculateId();
        script.Links[0].DestinationAssetId = script.Id;
        script.Physical.Flags = AssetFlags.SourceVirtual;
        layer.Add(script);
        Console.WriteLine($"  camera: {string.Join(", ", transitions.Select(t => t.Name))} fired at 0.5s; trails from +Z");
    }
}

// ======================= THE PROBE — rewrite below this line =======================
// BaseAsset: BaseFlags bits, BaseId and LinkCount. Four stations along +X; see probes/base-asset.md.

const BaseAssetFlags Shipped = BaseAssetFlags.Enabled | BaseAssetFlags.Valid
    | BaseAssetFlags.VisibleDuringCutscenes | BaseAssetFlags.ReceiveShadows;

// Event IDs (Rat, TSSM: DWARF enumerators; BFBB: xEvent.h; N100F: shipped links and zEntEvent; ROTU, Incredibles: the constants zPlayer::CollideTrigger,
// zEntEvent and ZDSP_elcb_event compare against), and a shipped trigger to copy every non-geometric
// field from. N100F, BFBB and TSSM have no ForceSceneReset; their scene resets only when the player loses
// a life.
var (events, triggerSource) = setup.Game switch
{
    GameVersion.N100F => (new Events(Enable: 1, Invisible: 4, Enter: 5, ForceSceneReset: null),
        new Borrowed(@"E0\e003.HIP", "OUTOFBOUNDS")),
    GameVersion.BFBB => (new Events(Enable: 1, Invisible: 4, Enter: 5, ForceSceneReset: null),
        new Borrowed(@"bb\bb01.HIP", "EXIT_TO_BB02")),
    GameVersion.TSSM => (new Events(Enable: 1, Invisible: 4, Enter: 5, ForceSceneReset: null),
        new Borrowed(@"BB\bb01.HIP", "LAPCOUNTER_TRIG_01")),
    GameVersion.Incredibles => (new Events(Enable: 1, Invisible: 4, Enter: 5, ForceSceneReset: 632),
        new Borrowed(@"NJ\nj03.HIP", "TO_NEXT_TRIG")),
    GameVersion.Ratatouille => (new Events(Enable: 1, Invisible: 4, Enter: 881, ForceSceneReset: 632),
        new Borrowed(@"FP\fp01.HIP", "TUTORIAL_INTRO_TRIG")),
    GameVersion.ROTU => (new Events(Enable: 1, Invisible: 4, Enter: 881, ForceSceneReset: 632),
        new Borrowed(@"A2\A201.HIP", "WAVE_01_GO_TRIGGER")),
    _ => throw new NotSupportedException($"Add {setup.Game}'s event IDs and reference trigger to the probe.")
};

var (_, triggerSession) = Open(Path.Combine(files, triggerSource.Archive));
var shippedTrigger = triggerSession.Layers.SelectMany(l => l.Assets).OfType<TriggerAsset>()
    .Single(t => t.Name == triggerSource.Name);

const float PadLift = 0.35f;
float x0 = spawn.X, z0 = spawn.Z;
Vector3 OnFloor(float x, float z, float lift = 0f) => new(x0 + x, floorY + lift, z0 + z);

/// <summary>Numbers <paramref name="position"/> with a row of small tiles beside it.</summary>
void Tally(int number, Vector3 position)
{
    for (int t = 0; t < number; t++)
        Tile($"zz_tally_{number:D2}_{t:D2}",
            position + new Vector3(-2f + (t % 5 * 1.25f), 0f, 4f + (t / 5 * 1.5f)), 1f, collidable: false);
}

// A floor model whose top sits at its origin is a flat plane, which no Y scale gives height to.
bool flatFloor = setup.FloorTop <= 0f;

/// <summary>A non-colliding floor-tile box of <paramref name="size"/> whose top surface centres on
/// <paramref name="top"/>, kept off the static batch by <see cref="CollisionFlags.LedgeGrab"/> so it
/// is drawn and updated individually. A flat floor model ignores <paramref name="size"/>'s height.</summary>
SimpleObjectAsset Unbatched(string name, Vector3 top, Vector3 size, Rgba tint, BaseAssetFlags flags)
{
    var scale = new Vector3(size.X / setup.FloorWidth, flatFloor ? 1f : size.Y / setup.FloorTop, size.Z / setup.FloorWidth);
    var simp = Place(name, tileModel, top - ((setup.FloorCentre + new Vector3(0f, setup.FloorTop, 0f)) * scale),
        1f, collidable: false, tint: tint);
    simp.Scale = scale;
    simp.Physical.BaseFlags = flags;
    simp.Physical.CollisionFlags = CollisionFlags.LedgeGrab;
    return simp;
}

/// <summary>A pillar standing on the floor at <paramref name="x"/>, <paramref name="z"/>, or a raised
/// slab where the floor model is flat.</summary>
SimpleObjectAsset Pillar(int number, float x, float z, Rgba tint, BaseAssetFlags flags = Shipped)
{
    var pillar = flatFloor
        ? Unbatched($"zz_pillar_{number:D2}", OnFloor(x, z, 0.5f), new Vector3(3f, 0f, 3f), tint, flags)
        : Unbatched($"zz_pillar_{number:D2}", OnFloor(x, z, 3f), new Vector3(2f, 3f, 2f), tint, flags);
    Tally(number, OnFloor(x, z));
    return pillar;
}

/// <summary>A box trigger standing on a tinted step-pad at <paramref name="x"/>.</summary>
TriggerAsset Trigger(string name, float x, Rgba tint, BaseAssetFlags flags = Shipped)
{
    const float Half = 3f, Height = 6f;
    var centre = OnFloor(x, 0f);
    Tile($"{name}_pad", centre + new Vector3(0f, 0.05f, 0f), Half * 2, collidable: false, tint: tint);

    var trigger = new TriggerAsset
    {
        Name = name,
        EntityFlags = shippedTrigger.EntityFlags,
        Position = centre,
        Scale = shippedTrigger.Scale,
        ColorMultiplier = shippedTrigger.ColorMultiplier,
        Kind = TriggerAsset.Shape.Box,
        TriggerPosition0 = centre - new Vector3(Half, 1f, Half),
        TriggerPosition1 = centre + new Vector3(Half, Height, Half),
        Direction = shippedTrigger.Direction,
        Flags = shippedTrigger.Flags,
    };
    trigger.CalculateId();
    trigger.Physical.BaseFlags = flags;
    trigger.Physical.BaseType = shippedTrigger.Physical.BaseType;
    trigger.Physical.ModelId = shippedTrigger.Physical.ModelId;
    trigger.Physical.TriggerPosition2 = centre;
    trigger.Physical.TriggerPosition3 = centre;
    trigger.Physical.Flags = AssetFlags.SourceVirtual;
    layer.Add(trigger);
    count++;
    return trigger;
}

static Link Send(short sourceEvent, short destinationEvent, AssetId destination) =>
    new() { SourceEvent = sourceEvent, DestinationEvent = destinationEvent, DestinationAssetId = destination };

Rgba grey = new(0.8f, 0.8f, 0.8f, 1f), blue = new(0.3f, 0.4f, 1f, 1f), green = new(0.3f, 1f, 0.3f, 1f),
    red = new(1f, 0.2f, 0.2f, 1f), yellow = new(0.9f, 0.9f, 0.3f, 1f), purple = new(0.6f, 0.2f, 0.8f, 1f),
    orange = new(0.9f, 0.6f, 0.1f, 1f), cyan = new(0.1f, 0.8f, 0.8f, 1f);

Console.WriteLine($"  spawn {spawn}; stations run +X:");

// Station 1: raised pads to stand on.
(string Label, BaseAssetFlags Flags, Rgba Tint)[] pads =
[
    ("0x1D shipped (control)",           Shipped,                                  blue),
    ("0x0D ReceiveShadows cleared",      Shipped & ~BaseAssetFlags.ReceiveShadows, yellow),
    ("0x19 Valid cleared",               Shipped & ~BaseAssetFlags.Valid,          cyan),
    ("0x1C Enabled cleared",             Shipped & ~BaseAssetFlags.Enabled,        red),
];
for (int i = 0; i < pads.Length; i++)
{
    var (label, flags, tint) = pads[i];
    var position = OnFloor((i + 1) * 12f, 0f, PadLift);
    Tile($"zz_pad_{i + 1:D2}", position, 9f, collidable: true, tint: tint).Physical.BaseFlags = flags;
    Tally(i + 1, position with { Z = position.Z - 12f });
    Console.WriteLine($"    #{i + 1} pad  x={position.X,6:F1}  {label}");
}

// Station 2: FacePlayer + Upright bars, a readout for whether the SIMP is updated.
(string Label, BaseAssetFlags Flags, Rgba Tint)[] bars =
[
    ("0x1D shipped (control)",             Shipped,                           blue),
    ("0x1C Enabled cleared",               Shipped & ~BaseAssetFlags.Enabled, red),
    ("0x9D shipped + bit 7 (0x80)",        Shipped | BaseAssetFlags.NeverUpdateCulled,        purple),
];
for (int i = 0; i < bars.Length; i++)
{
    var (label, flags, tint) = bars[i];
    int number = pads.Length + i + 1;
    var position = OnFloor(number * 12f, 0f, 1.5f);
    var bar = Unbatched($"zz_bar_{number:D2}", position, new Vector3(1f, 0.5f, 8f), tint, flags);
    bar.FacePlayer = true;
    bar.Upright = true;
    Tally(number, position with { Y = floorY, Z = position.Z - 12f });
    Console.WriteLine($"    #{number} bar  x={position.X,6:F1}  {label}");
}

// Station 3: one trigger drives pillars that differ only in the field under test. Pillars stand in a
// row along -Z of the step-pad, numbered away from the pad.
int next = pads.Length + bars.Length + 1;
float stationX = next * 12f + 12f;
var mainTrigger = Trigger("zz_trig_main", stationX, green);
Console.WriteLine($"    main trigger x={x0 + stationX,6:F1} (green pad):");

SimpleObjectAsset StationPillar(string label, Rgba tint, BaseAssetFlags flags = Shipped)
{
    int number = next++;
    var pillar = Pillar(number, stationX - 15f + ((number - pads.Length - bars.Length - 1) * 6f), -12f, tint, flags);
    Console.WriteLine($"      #{number} pillar  {label}");
    return pillar;
}

var control = StationPillar("shipped, sent Invisible (control)", blue);
var disabled = StationPillar("Enabled cleared, sent Invisible", red, Shipped & ~BaseAssetFlags.Enabled);
var reenabled = StationPillar("Enabled cleared, sent Enable then Invisible", orange, Shipped & ~BaseAssetFlags.Enabled);
var persistent = StationPillar("Persistent, sent Invisible", purple, Shipped | BaseAssetFlags.Persistent);
var byAtocId = StationPillar("BaseId changed, sent Invisible at its ATOC id", yellow);
var byBaseId = StationPillar("BaseId changed, sent Invisible at its BaseId", cyan);

byAtocId.Physical.BaseId = AssetId.FromName("zz_other_id_a");
byBaseId.Physical.BaseId = AssetId.FromName("zz_other_id_b");

mainTrigger.Links.Add(Send(events.Enter, events.Invisible, control.Id));
mainTrigger.Links.Add(Send(events.Enter, events.Invisible, disabled.Id));
mainTrigger.Links.Add(Send(events.Enter, events.Enable, reenabled.Id));
mainTrigger.Links.Add(Send(events.Enter, events.Invisible, reenabled.Id));
mainTrigger.Links.Add(Send(events.Enter, events.Invisible, persistent.Id));
mainTrigger.Links.Add(Send(events.Enter, events.Invisible, byAtocId.Id));
mainTrigger.Links.Add(Send(events.Enter, events.Invisible, byBaseId.Physical.BaseId));

// Station 4: a disabled trigger.
stationX += 30f;
var disabledTrigger = Trigger("zz_trig_disabled", stationX, red, Shipped & ~BaseAssetFlags.Enabled);
Console.WriteLine($"    disabled trigger x={x0 + stationX,6:F1} (red pad):");
var disabledTarget = Pillar(next, stationX, -12f, blue);
Console.WriteLine($"      #{next++} pillar  shipped, sent Invisible by the disabled trigger");
disabledTrigger.Links.Add(Send(events.Enter, events.Invisible, disabledTarget.Id));

// Station 5: a trigger with two links and LinkCount 1.
stationX += 20f;
var shortTrigger = Trigger("zz_trig_linkcount", stationX, yellow);
Console.WriteLine($"    LinkCount=1 trigger x={x0 + stationX,6:F1} (yellow pad):");
var firstTarget = Pillar(next, stationX - 4f, -12f, blue);
Console.WriteLine($"      #{next++} pillar  sent Invisible by link 1");
var secondTarget = Pillar(next, stationX + 4f, -12f, blue);
Console.WriteLine($"      #{next++} pillar  sent Invisible by link 2");
shortTrigger.Links.Add(Send(events.Enter, events.Invisible, firstTarget.Id));
shortTrigger.Links.Add(Send(events.Enter, events.Invisible, secondTarget.Id));
shortTrigger.Physical.LinkCount = 1;

// Station 6: scene reset, through a dispatcher where the game has ForceSceneReset, and otherwise by
// dying on a fatal surface, since losing a life resets the scene.
stationX += 20f;
if (events.ForceSceneReset is { } forceSceneReset)
{
    var resetTrigger = Trigger("zz_trig_reset", stationX, purple);
    var dispatcher = new DispatcherAsset { Name = "zz_reset_dispatcher" };
    dispatcher.CalculateId();
    dispatcher.Physical.BaseFlags = Shipped;
    dispatcher.Physical.Flags = AssetFlags.SourceVirtual;
    layer.Add(dispatcher);
    resetTrigger.Links.Add(Send(events.Enter, forceSceneReset, dispatcher.Id));
    Console.WriteLine($"    scene reset trigger x={x0 + stationX,6:F1} (purple pad)");
}
else
{
    // The shipped reference surface itself, so every game-specific field keeps a shipped value.
    var fatal = reference!;
    fatal.Layer!.Remove(fatal);
    fatal.Name = "zz_death_surf";
    fatal.CalculateId();
    fatal.Damage = SurfaceAsset.DamageKind.FatalDeathPlane;
    fatal.DamagePassthrough = false;
    layer.Add(fatal);
    Tile("zz_death_pad", OnFloor(stationX, 0f, PadLift), 9f, collidable: true, fatal.Id);
    Console.WriteLine($"    death pad x={x0 + stationX,6:F1} (resets the scene on death)");
}

// Station 7 (BFBB): bit 7 against update culling. Two shipped always-spinning mechanisms, re-modelled
// as big floor tiles, placed past the 70-unit default cull radius; only #18 carries 0x80. A culled
// platform isn't updated, so #17 should hang still until the camera comes within range.
if (setup.Game is GameVersion.BFBB)
{
    var (_, spinnerSession) = Open(Path.Combine(files, @"b2\b201.HIP"));
    var spinners = spinnerSession.Layers.SelectMany(l => l.Assets).OfType<PlatformAsset>()
        .Where(p => p.Name is "ROLLER_MECH 01" or "ROLLER_MECH 02").ToList();
    const float SpinnerX = 205f, SpinnerSide = 24f, SpinnerLift = 10f, SpinnerWidth = 12f;
    Console.WriteLine($"    spinners x={x0 + SpinnerX,6:F1}, {SpinnerLift} up:");
    for (int i = 0; i < spinners.Count; i++)
    {
        int number = next++;
        bool exempt = i == 1;
        var spinner = spinners[i];
        spinner.Layer!.Remove(spinner);
        spinner.Name = $"zz_spinner_{number:D2}";
        spinner.CalculateId();
        spinner.Physical.ModelId = tileModel;
        spinner.Scale = new Vector3(TileScale(SpinnerWidth));
        spinner.Angle = Vector3.Zero;
        spinner.Position = OnFloor(SpinnerX, (i * 2 - 1) * SpinnerSide, SpinnerLift);
        spinner.ColorMultiplier = exempt ? purple : blue;
        spinner.Physical.BaseFlags = exempt ? Shipped | BaseAssetFlags.NeverUpdateCulled : Shipped;
        layer.Add(spinner);
        count++;
        Tally(number, OnFloor(SpinnerX, (i * 2 - 1) * SpinnerSide));
        Console.WriteLine($"      #{number} spinner z={spinner.Position.Z,6:F1}  {(exempt ? "0x9D, bit 7 set" : "0x1D (control)")}");
    }
}

// ======================= end of probe =======================

void SaveArchive(Archive archive, AssetSession session, string path)
{
    if (args.Length > 1 && File.Exists(path) && !File.Exists(path + ".orig")) File.Copy(path, path + ".orig");
    session.Commit();
    using var stream = File.Create(path);
    archive.Save(stream);
}

if (!setup.SingleArchive) SaveArchive(hop, hopSession, SlotPath(".HOP"));
SaveArchive(hip, hipSession, SlotPath(".HIP"));

// Write an empty locale archive ({scene}_US.hip) if donor has one.
if (setup.Template.Keep is not null && File.Exists(Path.Combine(files, setup.Template.Path + "_US.hip")))
{
    var (locale, localeSession) = OpenTemplate("_US.hip");
    SaveArchive(locale, localeSession, SlotPath("_US.hip"));
    Console.WriteLine($"  wrote empty {setup.Slot.ToLowerInvariant()}_US.hip");
}

// Register scene name UI text in the UI locale archive.
if (setup.SceneNames is { } sceneNames)
{
    string path = Path.Combine(files, sceneNames);
    if (!File.Exists(path + ".orig")) File.Copy(path, path + ".orig");

    var (names, namesSession) = Open(path + ".orig");
    var shipped = namesSession.Layers.SelectMany(l => l.Assets).OfType<TextAsset>()
        .First(t => t.Name.StartsWith("UI TXT SCENE ", StringComparison.Ordinal));
    var name = new TextAsset { Name = $"UI TXT SCENE {setup.Slot}", Text = "Probe Workshop" };
    name.CalculateId();
    name.Physical.Flags = shipped.Physical.Flags;
    shipped.Layer!.Add(name);
    SaveArchive(names, namesSession, path);
    Console.WriteLine($"  {sceneNames}: added '{name.Name}'");
}

PointBootIni();
Console.WriteLine($"  {count} assets; wrote {setup.Slot}.HIP{(setup.SingleArchive ? "" : $" and {setup.Slot}.HOP")}");

/// <summary>Defines the donor/template source and disc destination for one game's probe workshop.</summary>
record Setup(
    GameVersion Game,
    Template Template,
    string Disc,                   // Extracted disc path relative to repo root
    string Ini,                    // Boot INI inside files/
    string SlotDir,                // Level folder to create
    string Slot,                   // Four-character scene ID
    Prop Floor, float FloorWidth,  // Flat floor tile model and width at scale 1
    Prop? Sky, float SkyScale,     // Skydome model (null for N100F)
    Borrowed? ReferenceSurface,    // Shipped SURF reference for copying base settings
    float FloorTop = 0f,           // Height offset of top surface at scale 1
    Vector3 FloorCentre = default, // Footprint center offset at scale 1
    string[]? IniLines = null,     // Lines to ensure in boot INI
    NavMeshSource? NavMesh = null, // Single-quad nav mesh to stretch
    string? SceneNames = null,     // UI locale archive to register scene name into
    Extra[]? Extras = null,        // Shipped assets to borrow unchanged into HOP
    bool SingleArchive = false,    // True if level is single HIP without HOP (N100F)
    string? EmptyWorld = null);    // Name of empty BSP to widen world bounds

/// <summary>The game-specific event IDs the probe's links use.</summary>
record Events(short Enable, short Invisible, short Enter, short? ForceSceneReset);

/// <summary>One named asset in a shipped archive.</summary>
record Borrowed(string Archive, string Name);

/// <summary>Shipped assets borrowed unchanged into the workshop's HOP.</summary>
record Extra(string Archive, params string[] Names);

/// <summary>A shipped nav mesh made of a single unit quad centred on the origin.</summary>
record NavMeshSource(string Archive, string Name);

/// <summary>Workshop starting template: IndustrialPark blank archive or stripped shipped level.</summary>
record Template(string Path, string[]? Keep, string FileName = "blank", string[]? Drop = null)
{
    public static Template EditorFiles(string directory, string fileName = "blank", params string[] drop) =>
        new(directory, null, fileName, drop);
    public static Template Donor(string level, params string[] keep) => new(level, keep);
}

/// <summary>A model borrowed from a shipped archive, with the texture it needs to render.</summary>
record Prop(string Archive, string Model, string Texture);
