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
public sealed partial class ButtonAsset() : EntityAsset(AssetType.Button, baseType: 0x18), IHasModel, IHasSurface, Physical.IButtonAsset
{
    /// <summary>
    /// How this button behaves once pressed.
    /// </summary>
    /// <remarks>
    /// Not present in <see cref="GameVersion.N100F"/>.
    /// </remarks>
    public ButtonKind Kind { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the <see cref="AssetType.Model"/> shown once the button is fully
    /// pressed, or <see cref="AssetId.None"/> to keep showing its own model.
    /// </summary>
    /// <remarks>
    /// Not present in <see cref="GameVersion.N100F"/>.
    /// </remarks>
    public AssetId PressedModelId { get; set; }

    /// <summary>
    /// Whether the <see cref="ButtonKind.Latching"/> button pops back up by itself some time after
    /// being pressed.
    /// </summary>
    /// <remarks>
    /// The button stays pressed for <see cref="ResetDelay"/>, except in
    /// <see cref="GameVersion.N100F"/>, where it stays pressed for 80 frames.
    /// </remarks>
    public bool AutoReset { get; set; }

    /// <summary>
    /// The time, in seconds, the <see cref="ButtonKind.Latching"/> button stays pressed before
    /// popping back up, when <see cref="AutoReset"/> is set.
    /// </summary>
    /// <remarks>
    /// Not present in <see cref="GameVersion.N100F"/>.
    /// </remarks>
    public float ResetDelay { get; set; }

    /// <summary>
    /// What can press this button.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="GameVersion.Incredibles"/> presses with <see cref="Activators.BubbleSpin"/> or
    /// <see cref="Activators.BubbleBounce"/> for its own combat hits, depending on the kind of hit.
    /// </para>
    /// <para>
    /// Not present in <see cref="GameVersion.N100F"/>, which uses <see cref="Activation"/> instead.
    /// </para>
    /// </remarks>
    public Activators ActivatedBy { get; set; }

    /// <summary>
    /// The move of the player's that presses this button.
    /// </summary>
    /// <remarks>
    /// Only present in <see cref="GameVersion.N100F"/>, in place of <see cref="ActivatedBy"/>.
    /// </remarks>
    public ActivationMethod Activation { get; set; }

    /// <summary>
    /// How this button moves as it's pressed and released.
    /// </summary>
    public EntityMotion.Mechanism Motion { get; set; } = new();

    /// <inheritdoc cref="Asset.Physical"/>
    public override Physical.IButtonAsset Physical => this;

    private int _initialState;
    int Physical.IButtonAsset.InitialState { get => _initialState; set => _initialState = value; }

    private int _springButtBounce;
    int Physical.IButtonAsset.SpringButtBounce { get => _springButtBounce; set => _springButtBounce = value; }

    private byte _buttonFlags;
    byte Physical.IButtonAsset.ButtonFlags { get => _buttonFlags; set => _buttonFlags = value; }

    AssetId IHasModel.ModelId { get => Physical.ModelId; set => Physical.ModelId = value; }
    AssetId IHasSurface.SurfaceId { get => Physical.SurfaceId; set => Physical.SurfaceId = value; }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.Button"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.N100F,
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

        if (profile.Game == GameVersion.N100F)
        {
            asset.Activation = (ActivationMethod)reader.ReadUInt32();
            asset.Physical.InitialState = reader.ReadInt32();
            asset.AutoReset = reader.ReadInt32() != 0;
            asset.Physical.SpringButtBounce = reader.ReadInt32();
            asset.Physical.ButtonFlags = reader.ReadByte();
            reader.ReadBytes(3); // padding, always zero
        }
        else
        {
            asset.PressedModelId = reader.ReadAssetId();
            asset.Kind = (ButtonKind)reader.ReadUInt32();
            asset.Physical.InitialState = reader.ReadInt32();
            asset.AutoReset = reader.ReadInt32() != 0;
            asset.ResetDelay = reader.ReadSingle();
            asset.ActivatedBy = (Activators)reader.ReadUInt32();
        }
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

        if (profile.Game == GameVersion.N100F)
        {
            writer.Write((uint)asset.Activation);
            writer.Write(asset.Physical.InitialState);
            writer.Write(asset.AutoReset ? 1 : 0);
            writer.Write(asset.Physical.SpringButtBounce);
            writer.Write(asset.Physical.ButtonFlags);
            writer.Write(new byte[3]); // padding
        }
        else
        {
            writer.Write(asset.PressedModelId);
            writer.Write((uint)asset.Kind);
            writer.Write(asset.Physical.InitialState);
            writer.Write(asset.AutoReset ? 1 : 0);
            writer.Write(asset.ResetDelay);
            writer.Write((uint)asset.ActivatedBy);
        }
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

    /// <summary>
    /// Defines which of the player's moves presses a <see cref="ButtonAsset"/>.
    /// </summary>
    public enum ActivationMethod : uint
    {
        /// <summary>
        /// The player standing on the button.
        /// </summary>
        Stand = 0,
        /// <summary>
        /// The player landing a butt bounce on the button.
        /// </summary>
        ButtBounce = 1,
        /// <summary>
        /// The player headbutting the button.
        /// </summary>
        Headbutt = 2,
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

        /// <summary>
        /// Unknown.
        /// </summary>
        /// <remarks>
        /// Only present in <see cref="GameVersion.N100F"/>.
        /// </remarks>
        int SpringButtBounce { get; set; }

        /// <summary>
        /// Unknown.
        /// </summary>
        /// <remarks>
        /// Only present in <see cref="GameVersion.N100F"/>.
        /// </remarks>
        byte ButtonFlags { get; set; }
    }
}
