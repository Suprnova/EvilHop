using EvilHop.Assets;
using EvilHop.Common;
using EvilHop.Primitives;

namespace EvilHop.Assets
{
    public class AnimationList(uint[] assetIds, uint[]? stateHashes, bool[]? hasPhysics) : Asset
    {
        public override AssetType Type => AssetType.AnimationList;

        // capacity of all fields: 10
        public uint[] AssetIds { get; set; } = assetIds;
        public uint[]? StateHashes { get; set; } = stateHashes;
        public bool[]? HasPhysics { get; set; } = hasPhysics;

        internal AnimationList(uint[] assetIds) : this(assetIds, null, null)
        {
        }
    }
}

namespace EvilHop.Serialization.Serializers
{
    public abstract partial class V1Serializer
    {
        protected virtual AnimationList InitAnimationListAsset()
        {
            return new AnimationList(new uint[10]);
        }

        protected virtual AnimationList ReadAnimationListAsset(BinaryReader reader)
        {
            uint[] assetIds = new uint[10];

            for (int i = 0; i < 10; i++)
                assetIds[i] = reader.ReadEvilInt();

            return new AnimationList(assetIds);
        }

        protected virtual void WriteAnimationListAsset(BinaryWriter writer, AnimationList animList)
        {
            foreach (uint assetId in animList.AssetIds)
                writer.WriteEvilInt(assetId);
        }
    }

    public abstract partial class V4Serializer
    {
        protected override AnimationList InitAnimationListAsset()
        {
            return new AnimationList(new uint[10], new uint[10], new bool[10]);
        }

        protected override AnimationList ReadAnimationListAsset(BinaryReader reader)
        {
            var animationList = base.ReadAnimationListAsset(reader);
            animationList.StateHashes = new uint[10];
            animationList.HasPhysics = new bool[10];

            for (int i = 0; i < 10; i++)
                animationList.StateHashes[i] = reader.ReadEvilInt();

            for (int i = 0; i < 10; i++)
                animationList.HasPhysics[i] = reader.ReadBoolean(); // TODO: is this right?

            return animationList;
        }

        protected override void WriteAnimationListAsset(BinaryWriter writer, AnimationList animList)
        {
            base.WriteAnimationListAsset(writer, animList);

            for (int i = 0; i < 10 && animList.StateHashes != null; i++)
                writer.WriteEvilInt(animList.StateHashes[i]);

            for (int i = 0; i < 10 && animList.HasPhysics != null; i++)
                writer.Write(animList.HasPhysics[i]);
        }
    }
}

namespace EvilHop.Serialization.Validation
{
    public partial class V1Validator
    {
        protected virtual IEnumerable<ValidationIssue> ValidateAnimationListAsset(AnimationList animList)
        {
            // todo validate array sizes?
            // todo: maybe have all known assetIds stored in validator, to allow validating AssetIds field?
            if (animList.StateHashes != null)
            {
                yield return new ValidationIssue
                {
                    Severity = ValidationSeverity.Warning,
                    Message =
                        $"{GetType().Name} does not support {nameof(animList.StateHashes)} being populated in " +
                        $"{typeof(AnimationList).Name} asset '{animList.Name}'; data loss may occur when writing.",
                    Context = null
                };
            }

            if (animList.HasPhysics != null)
            {
                yield return new ValidationIssue
                {
                    Severity = ValidationSeverity.Warning,
                    Message =
                        $"{GetType().Name} does not support {nameof(animList.HasPhysics)} being populated in " +
                        $"{typeof(AnimationList).Name} asset '{animList.Name}'; data loss may occur when writing.",
                    Context = null
                };
            }
        }
    }

    public partial class V4Validator
    {
        protected override IEnumerable<ValidationIssue> ValidateAnimationListAsset(AnimationList animList)
        {
            if (animList.StateHashes == null)
            {
                yield return new ValidationIssue
                {
                    Severity = ValidationSeverity.Error,
                    Message =
                        $"{GetType().Name} expects {nameof(animList.StateHashes)} to be populated in " +
                        $"{typeof(AnimationList).Name} asset '{animList.Name}'.",
                    Context = null
                };
            }

            if (animList.HasPhysics == null)
            {
                yield return new ValidationIssue
                {
                    Severity = ValidationSeverity.Error,
                    Message =
                        $"{GetType().Name} expects {nameof(animList.HasPhysics)} to be populated in " +
                        $"{typeof(AnimationList).Name} asset '{animList.Name}'.",
                    Context = null
                };
            }
        }
    }
}
