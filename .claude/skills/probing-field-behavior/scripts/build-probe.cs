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
        ReferenceSurface: null,
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

// Shipped SURF reference for copying ExtendedData (TSSM onward) and base settings.
SurfaceAsset? reference = null;
if (setup.ReferenceSurface is { } referenceSource)
{
    var (_, referenceSession) = Open(Path.Combine(files, referenceSource.Archive));
    reference = referenceSession.Layers.SelectMany(l => l.Assets).OfType<SurfaceAsset>()
        .First(s => s.Name.Equals(referenceSource.Name, StringComparison.OrdinalIgnoreCase));
    Console.WriteLine($"  reference SURF '{reference.Name}': ExtendedData={reference.ExtendedData.Length}B");
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
        BaseFlags = BaseAssetFlags.Enabled | BaseAssetFlags.Valid
            | BaseAssetFlags.VisibleDuringCutscenes | BaseAssetFlags.ReceiveShadows,
        EntityFlags = EntityFlags.Visible,
        Position = position,
        Angle = new Vector3(yaw, 0f, 0f),
        Scale = new Vector3(scale, scale, scale),
        ColorMultiplier = new Rgba(1f, 1f, 1f, 1f),
        HasCollision = collidable
    };

    if (tint is { } colour) asset.ColorMultiplier = colour;

    asset.CalculateId();
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
            BaseFlags = BaseAssetFlags.Enabled | BaseAssetFlags.Valid
                | BaseAssetFlags.VisibleDuringCutscenes | BaseAssetFlags.ReceiveShadows,
            ScaleFactor = 1f
        };
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

/// <summary>Creates a SurfaceAsset variant based on the reference surface.</summary>
AssetId Surface(string name, byte physFlags, byte damageType, float? outOfBoundsDelay = null,
    (byte Start, byte Stop)? slideAngles = null)
{
    if (reference is null) return default;

    var surface = new SurfaceAsset
    {
        Name = name,
        BaseFlags = reference.BaseFlags,
        Damage = (SurfaceAsset.DamageKind)damageType,
        PhysFlags = (SurfaceAsset.PhysicsBehavior)physFlags,
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
    return surface.Id;
}

// Variant pads placed along +X.
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

const float PadStep = 12f;
const float PadLift = 0.35f;
Console.WriteLine($"  spawn {spawn}; pad row runs +X, {PadStep} apart:");

for (int i = 0; i < padVariants.Length; i++)
{
    var (label, physFlags, damageType, oobDelay, tint) = padVariants[i];
    var surface = Surface($"zz_surf_{i + 1:D2}", physFlags, damageType, oobDelay);
    var position = new Vector3(spawn.X + ((i + 1) * PadStep), floorY + PadLift, spawn.Z);
    Tile($"zz_pad_{i + 1:D2}", position, 9f, collidable: true, surface, tint);

    // Tally markers beside each pad in rows of five.
    for (int t = 0; t <= i; t++)
        Tile($"zz_tally_{i + 1:D2}_{t:D2}",
            new Vector3(position.X - 4f + (t % 5 * 2f), floorY + PadLift, position.Z - 8f - (t / 5 * 2.5f)),
            1.5f, collidable: false);

    Console.WriteLine($"    #{i + 1} x={position.X,6:F1}  {label}");
}

// Tilted ramps testing slide and orientation matching (pitch turns about Z).
{
    const float TiltAngle = 0.5236f; // 30 degrees
    const float RampWidth = 12f;
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
        Tile($"zz_ramp_{num:D2}", position, RampWidth, collidable: true, surface, tint).Angle = angle;

        for (int t = 0; t <= i; t++)
            Tile($"zz_tally_{num:D2}_{t:D2}",
                new Vector3(position.X - 4f + (t % 5 * 2f), floorY + PadLift, position.Z - 8f - (t / 5 * 2.5f)),
                1.5f, collidable: false);

        Console.WriteLine($"    #{num} x={position.X,6:F1}  {label}");
    }
}

// ======================= end of probe =======================

/// <summary>Commits the session and saves the archive with ATOC headers sorted in ascending unsigned asset ID order.</summary>
void SaveSorted(Archive archive, AssetSession session, string path)
{
    session.Commit();
    var table = archive.Roots.OfType<EvilHop.Blocks.Dictionary>().Single().AssetTable;
    table.Headers = [.. table.Headers.OrderBy(header => header.Id)];
    using var stream = File.Create(path);
    archive.Save(stream);
}

if (!setup.SingleArchive) SaveSorted(hop, hopSession, SlotPath(".HOP"));
SaveSorted(hip, hipSession, SlotPath(".HIP"));

// Write an empty locale archive ({scene}_US.hip) if donor has one.
if (setup.Template.Keep is not null && File.Exists(Path.Combine(files, setup.Template.Path + "_US.hip")))
{
    var (locale, localeSession) = OpenTemplate("_US.hip");
    SaveSorted(locale, localeSession, SlotPath("_US.hip"));
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
    SaveSorted(names, namesSession, path);
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
    Borrowed? ReferenceSurface,    // Shipped SURF reference for copying ExtendedData
    float FloorTop = 0f,           // Height offset of top surface at scale 1
    Vector3 FloorCentre = default, // Footprint center offset at scale 1
    string[]? IniLines = null,     // Lines to ensure in boot INI
    NavMeshSource? NavMesh = null, // Single-quad nav mesh to stretch
    string? SceneNames = null,     // UI locale archive to register scene name into
    Extra[]? Extras = null,        // Shipped assets to borrow unchanged into HOP
    bool SingleArchive = false,    // True if level is single HIP without HOP (N100F)
    string? EmptyWorld = null);    // Name of empty BSP to widen world bounds

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
