using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public sealed partial class DuplicatorAsset
{
    /// <summary>
    /// The NPC fields of a <see cref="DuplicatorAsset"/>'s avatar - see
    /// <see cref="Physical.IDuplicatorAsset.Avatar"/>.
    /// </summary>
    public sealed class Avatar : IVillain
    {
        /// <inheritdoc cref="VillainAsset.NpcFlags"/>
        public int NpcFlags { get; set; }

        /// <inheritdoc cref="VillainAsset.NpcModelId"/>
        public AssetId NpcModelId { get; set; }

        /// <inheritdoc cref="VillainAsset.NpcSettingsId"/>
        public AssetId NpcSettingsId { get; set; }

        /// <inheritdoc cref="VillainAsset.MovePointId"/>
        public AssetId MovePointId { get; set; }

        /// <inheritdoc cref="VillainAsset.TaskWidgetPrimeId"/>
        public AssetId TaskWidgetPrimeId { get; set; }

        /// <inheritdoc cref="VillainAsset.TaskWidgetSecondId"/>
        public AssetId TaskWidgetSecondId { get; set; }

        /// <inheritdoc cref="VillainAsset.NavigationMeshId"/>
        public AssetId NavigationMeshId { get; set; }

        /// <inheritdoc cref="VillainAsset.SettingsId"/>
        public AssetId SettingsId { get; set; }

        internal static Avatar Read(EndianReader reader, FormatProfile profile)
        {
            var value = new Avatar();
            IVillain.ReadVillain(reader, profile, value);
            return value;
        }

        internal static void Write(Avatar value, EndianWriter writer, FormatProfile profile) =>
            IVillain.WriteVillain(value, writer, profile);
    }
}
