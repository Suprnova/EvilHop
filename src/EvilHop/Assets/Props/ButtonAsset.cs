using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// An <see cref="EntityAsset"/> that is pressed down by the player, or by whatever else its
/// <see cref="ActivatedBy"/> allows, such as a switch or a pressure plate.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/BUTN">Heavy Iron Modding documentation</seealso>
/// </remarks>
// TODO: Partial implementation - N100F stores a shorter, differently laid out format and is not yet modelled
public sealed partial class ButtonAsset() : EntityAsset(AssetType.Button, baseType: 0x18), IHasModel, IHasSurface, Physical.IButtonAsset
{
    /// <summary>
    /// How this button behaves once pressed.
    /// </summary>
    public ButtonKind Kind { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the <see cref="AssetType.Model"/> shown once the button is fully
    /// pressed, or <see cref="AssetId.None"/> to keep showing its own model.
    /// </summary>
    public AssetId PressedModelId { get; set; }

    /// <summary>
    /// Whether the button pops back up by itself, <see cref="ResetDelay"/> after being pressed. Only
    /// applies to a <see cref="ButtonKind.Latching"/> button.
    /// </summary>
    public bool AutoReset { get; set; }

    /// <summary>
    /// The time, in seconds, the button stays pressed before popping back up, when
    /// <see cref="AutoReset"/> is set. Only applies to a <see cref="ButtonKind.Latching"/> button.
    /// </summary>
    public float ResetDelay { get; set; }

    /// <summary>
    /// What can press this button.
    /// </summary>
    /// <remarks>
    /// <see cref="GameVersion.Incredibles"/> presses with <see cref="Activators.BubbleSpin"/> or
    /// <see cref="Activators.BubbleBounce"/> for its own combat hits, depending on the kind of hit.
    /// </remarks>
    public Activators ActivatedBy { get; set; }

    /// <summary>
    /// How this button moves as it's pressed and released.
    /// </summary>
    public EntityMotion.Mechanism Motion { get; set; } = new();

    /// <inheritdoc cref="Asset.Physical"/>
    public override Physical.IButtonAsset Physical => this;

    private int _initialState;
    int Physical.IButtonAsset.InitialState { get => _initialState; set => _initialState = value; }

    AssetId IHasModel.ModelId { get => Physical.ModelId; set => Physical.ModelId = value; }
    AssetId IHasSurface.SurfaceId { get => Physical.SurfaceId; set => Physical.SurfaceId = value; }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.Button"/> is known to be read by.
    /// </summary>
    /// <remarks>
    /// <see cref="GameVersion.N100F"/> stores a shorter, differently laid out <c>BUTN</c> and is not
    /// yet modelled.
    /// </remarks>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.BFBB,
        GameVersion.TSSM,
        GameVersion.Incredibles,
    };

    internal static ButtonAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new ButtonAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);
        EntityAssetPrefix.Read(asset, reader, profile);

        asset.PressedModelId = reader.ReadAssetId();
        asset.Kind = (ButtonKind)reader.ReadUInt32();
        asset.Physical.InitialState = reader.ReadInt32();
        asset.AutoReset = reader.ReadInt32() != 0;
        asset.ResetDelay = reader.ReadSingle();
        asset.ActivatedBy = (Activators)reader.ReadUInt32();
        asset.Motion = (EntityMotion.Mechanism)EntityMotion.Read(reader, profile);

        for (var i = 0; i < asset.Physical.LinkCount; i++)
            asset.Links.Add(Link.Read(reader, profile));
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(ButtonAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);
        EntityAssetPrefix.Write(asset, writer, profile);

        writer.Write(asset.PressedModelId);
        writer.Write((uint)asset.Kind);
        writer.Write(asset.Physical.InitialState);
        writer.Write(asset.AutoReset ? 1 : 0);
        writer.Write(asset.ResetDelay);
        writer.Write((uint)asset.ActivatedBy);
        EntityMotion.Write(asset.Motion, writer, profile);

        foreach (var link in asset.Links)
            Link.Write(link, writer, profile);
        writer.Write(asset.GetUnparsedTail());
    }

    /// <summary>
    /// Defines how a <see cref="ButtonAsset"/> behaves once pressed.
    /// </summary>
    public enum ButtonKind : uint
    {
        /// <summary>
        /// Stays pressed once hit, until it resets or is sent an <b>Unpress</b> event. Pulses while
        /// it can still be pressed.
        /// </summary>
        Latching = 0,
        /// <summary>
        /// Stays pressed only while something is resting on it, popping back up as soon as it's
        /// left alone.
        /// </summary>
        PressurePlate = 1,
    }
}

public static partial class Physical
{
    /// <summary>
    /// An explicit interface used to interact with <see cref="ButtonAsset"/>'s underlying values.
    /// </summary>
    public interface IButtonAsset : IEntityAsset
    {
        /// <summary>
        /// Unused. A button always starts unpressed.
        /// </summary>
        int InitialState { get; set; }
    }
}
