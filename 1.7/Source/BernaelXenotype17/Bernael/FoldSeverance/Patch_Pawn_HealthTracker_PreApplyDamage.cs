using System.Collections.Generic;
using Verse;
using HarmonyLib;

namespace Bernael_Xenotype
{
    [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.PreApplyDamage))]
    public static class Patch_Pawn_HealthTracker_PreApplyDamage
    {
        public static bool Prefix(Pawn ___pawn, ref DamageInfo dinfo, out bool absorbed)
        {
            absorbed = false;
            if (!WorldComponent_ImmuneWithoutWeakDamage.TryGet(___pawn, out List<HediffComp_ImmuneWithoutWeakDamage> comps))
                return true;

            for (int i = 0; i < comps.Count; i++)
            {
                if (comps[i].CheckPreApplyDamage(ref dinfo))
                {
                    absorbed = true;
                    return false;
                }
            }
            return true;
        }
    }
}
