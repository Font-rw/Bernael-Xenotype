using System.Collections.Generic;
using Verse;

namespace Bernael_Xenotype
{
    public class HediffCompProperties_NoPathCost : HediffCompProperties
    {
        public HediffCompProperties_NoPathCost()
        {
            compClass = typeof(HediffComp_NoPathCost);
        }
    }

    public class HediffComp_NoPathCost : HediffComp
    {
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            WorldComponent_NoPathCost.Add(Pawn);
        }

        public override void CompPostPostRemoved()
        {
            if (Remaining() == null)
            {
                WorldComponent_NoPathCost.Remove(Pawn);
            }
        }

        private HediffComp_NoPathCost Remaining()
        {
            List<Hediff> hediffs = Pawn.health?.hediffSet?.hediffs;
            if (hediffs == null) return null;
            for (int i = 0; i < hediffs.Count; i++)
                if (hediffs[i] != parent && hediffs[i] is HediffWithComps withComps && withComps.comps != null)
                    for (int j = 0; j < withComps.comps.Count; j++)
                        if (withComps.comps[j] is HediffComp_NoPathCost comp) return comp;
            return null;
        }
    }
}
