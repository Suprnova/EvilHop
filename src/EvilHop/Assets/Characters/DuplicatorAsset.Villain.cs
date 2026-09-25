using EvilHop.Assets.Serialization;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Assets;

public sealed partial class DuplicatorAsset
{
    /// <summary>
    /// The villain a <see cref="DuplicatorAsset"/> embeds as its <see cref="Template"/>: a
    /// <see cref="VillainAsset"/>'s entity and NPC fields, without the <see cref="Asset"/> around them.
    /// </summary>
    public sealed class Villain : IEntity, IVillain, IHasModel, IGrabbable, Physical.IEntity
    {
        /// <inheritdoc cref="EntityAsset.EntityFlags"/>
        public EntityFlags EntityFlags { get; set; }

        /// <inheritdoc cref="EntityAsset.Angle"/>
        public Vector3 Angle { get; set; }

        /// <inheritdoc cref="EntityAsset.Position"/>
        public Vector3 Position { get; set; }

        /// <inheritdoc cref="EntityAsset.Scale"/>
        public Vector3 Scale { get; set; }

        /// <inheritdoc cref="EntityAsset.ColorMultiplier"/>
        public Rgba ColorMultiplier { get; set; }

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

        /// <inheritdoc cref="EntityAsset.Physical"/>
        public Physical.IEntity Physical => this;

        private byte _subtype;
        byte Physical.IEntity.Subtype { get => _subtype; set => _subtype = value; }

        private byte _pFlags;
        byte Physical.IEntity.PFlags { get => _pFlags; set => _pFlags = value; }

        private CollisionFlags _collisionFlags;
        CollisionFlags Physical.IEntity.CollisionFlags { get => _collisionFlags; set => _collisionFlags = value; }

        private AssetId _surfaceId;
        AssetId Physical.IEntity.SurfaceId { get => _surfaceId; set => _surfaceId = value; }

        private AssetId _modelId;
        AssetId Physical.IEntity.ModelId { get => _modelId; set => _modelId = value; }

        private AssetId _animListId;
        AssetId Physical.IEntity.AnimListId { get => _animListId; set => _animListId = value; }

        private float _seeThroughSpeed;
        float Physical.IEntity.SeeThroughSpeed { get => _seeThroughSpeed; set => _seeThroughSpeed = value; }

        AssetId IHasModel.ModelId { get => Physical.ModelId; set => Physical.ModelId = value; }

        bool IGrabbable.IsGrabbable
        {
            get => _collisionFlags.HasFlag(CollisionFlags.Grabbable);
            set => _collisionFlags = _collisionFlags.WithFlag(CollisionFlags.Grabbable, value);
        }

        internal static Villain Read(EndianReader reader, FormatProfile profile)
        {
            var value = new Villain();
            EntityAssetPrefix.Read(value, reader, profile);
            IVillain.ReadVillain(reader, profile, value);
            return value;
        }

        internal static void Write(Villain value, EndianWriter writer, FormatProfile profile)
        {
            EntityAssetPrefix.Write(value, writer, profile);
            IVillain.WriteVillain(value, writer, profile);
        }
    }
}
