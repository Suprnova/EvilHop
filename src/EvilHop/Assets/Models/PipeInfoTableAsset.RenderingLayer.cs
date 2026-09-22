namespace EvilHop.Assets;

public partial class PipeInfoTableAsset
{
    /// <summary>
    /// Defines the rendering stage when a model's selected atomics are drawn relative to other
    /// transparent geometry, from earliest to latest.
    /// </summary>
    public enum RenderingLayer : byte
    {
        /// <summary>Draw first.</summary>
        First = 0,
        /// <summary>Draw before pickups.</summary>
        PrePickup = 1,
        /// <summary>Draw just after pickups.</summary>
        PostPickup = 2,
        /// <summary>Draw before the out-of-bounds object.</summary>
        PreOob = 3,
        /// <summary>Draw just after the out-of-bounds object.</summary>
        PostOob = 4,
        /// <summary>Draw before cutscene objects.</summary>
        PreCutscene = 5,
        /// <summary>Draw just after cutscene objects.</summary>
        PostCutscene = 6,
        /// <summary>Draw before NPC objects.</summary>
        PreNpc = 7,
        /// <summary>Draw just after NPC objects.</summary>
        PostNpc = 8,
        /// <summary>Draw before shadows.</summary>
        PreShadow = 9,
        /// <summary>Draw just after shadows.</summary>
        PostShadow = 10,
        /// <summary>Draw before lightning, glares, etc.</summary>
        PreFx = 11,
        /// <summary>Draw after glares, etc.</summary>
        PostFx = 12,
        /// <summary>Draw before particles.</summary>
        PreParticles = 13,
        /// <summary>Draw after particles.</summary>
        PostParticles = 14,
        /// <summary>Draw before normal transparencies (4 stages).</summary>
        PreNormal4 = 15,
        /// <summary>Draw before normal transparencies (3 stages).</summary>
        PreNormal3 = 16,
        /// <summary>Draw before normal transparencies (2 stages).</summary>
        PreNormal2 = 17,
        /// <summary>Draw before normal transparencies.</summary>
        PreNormal = 18,
        /// <summary>Draw in the normal position.</summary>
        Normal = 19,
        /// <summary>Draw directly after transparencies.</summary>
        PostNormal = 20,
        /// <summary>Draw directly after transparencies (2 stages).</summary>
        PostNormal2 = 21,
        /// <summary>Draw directly after transparencies (3 stages).</summary>
        PostNormal3 = 22,
        /// <summary>Draw directly after transparencies (4 stages).</summary>
        PostNormal4 = 23,
        /// <summary>Draw before ptank effects.</summary>
        PrePtank = 24,
        /// <summary>Draw after ptank effects.</summary>
        PostPtank = 25,
        /// <summary>Draw before decals.</summary>
        PreDecal = 26,
        /// <summary>Draw after decals.</summary>
        PostDecal = 27,
        /// <summary>Draw before lasers, etc.</summary>
        PreLastFx = 28,
        /// <summary>Draw directly after lasers, etc.</summary>
        PostLastFx = 29,
        /// <summary>Draw before last.</summary>
        PreLast = 30,
        /// <summary>Draw last.</summary>
        Last = 31,
    }
}
