using RimWorld;
using Verse;
using HarmonyLib;

namespace Bernael_Xenotype
{
    [HarmonyPatch(typeof(Pawn_GuestTracker), nameof(Pawn_GuestTracker.GuestTrackerTickInterval))]
    public static class Patch_Pawn_GuestTracker_GuestTrackerTickInterval
    {
        public static void Postfix(Pawn ___pawn, int delta)
        {
            if (___pawn.IsHashIntervalTick(5000, delta) && ___pawn.IsPrisonerOfColony && ___pawn.guest.IsInteractionEnabled(BernaelDefOf.BX_SoulExecution))
            {
                var ext = BernaelDefOf.BX_SoulExecution.GetModExtension<PrisonerInteractionModeExtension>();
                if (ext == null || ext.autoSurgeryRecipe == null) return;
                HealthCardUtility.CreateSurgeryBill(___pawn, ext.autoSurgeryRecipe, null, null, sendMessages: false);
            }
        }
    }
}
