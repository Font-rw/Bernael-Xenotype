using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace Bernael_Xenotype
{
    /// <summary>
    /// WorldComponent that maintains an O(1) lookup dictionary mapping active Pawns 
    /// directly to their active HediffComp_ImmuneWithoutWeakDamage instance.
    /// </summary>
    public class WorldComponent_ImmuneWithoutWeakDamage : WorldComponent
    {
        private const int CleanupIntervalTicks = 30000; // 12 in-game hours (12 * 2500 ticks)

        private static readonly Dictionary<Pawn, List<HediffComp_ImmuneWithoutWeakDamage>> activeComps = new();
        private static readonly List<Pawn> staleKeys = new();

        public WorldComponent_ImmuneWithoutWeakDamage(World world) : base(world) { }

        public override void WorldComponentTick()
        {
            if (Find.TickManager.TicksGame % CleanupIntervalTicks == 0)
            {
                if (activeComps.Count == 0) return;
                staleKeys.Clear();
                foreach (var kvp in activeComps)
                {
                    Pawn pawn = kvp.Key;
                    if (pawn == null || pawn.Destroyed || pawn.Dead || kvp.Value == null || kvp.Value.Count == 0)
                    {
                        staleKeys.Add(pawn);
                    }
                }
                for (int i = 0; i < staleKeys.Count; i++)
                {
                    activeComps.Remove(staleKeys[i]);
                }
                staleKeys.Clear();
            }
        }

        public override void FinalizeInit(bool fromLoad)
        {
            activeComps.Clear();
            staleKeys.Clear();
        }

        public static void Register(Pawn pawn, HediffComp_ImmuneWithoutWeakDamage comp)
        {
            if (pawn == null || comp == null) return;
            if (!activeComps.TryGetValue(pawn, out var list))
            {
                list = new List<HediffComp_ImmuneWithoutWeakDamage>(2);
                activeComps[pawn] = list;
            }
            if (!list.Contains(comp))
            {
                list.Add(comp);
            }
        }

        public static void Unregister(Pawn pawn, HediffComp_ImmuneWithoutWeakDamage comp)
        {
            if (pawn == null || comp == null) return;

            if (activeComps.TryGetValue(pawn, out var list))
            {
                list.Remove(comp);
                if (list.Count == 0)
                {
                    activeComps.Remove(pawn);
                }
            }
        }

        public static bool TryGet(Pawn pawn, out List<HediffComp_ImmuneWithoutWeakDamage> comps)
        {
            if (pawn == null)
            {
                comps = null;
                return false;
            }
            return activeComps.TryGetValue(pawn, out comps);
        }
    }
}
