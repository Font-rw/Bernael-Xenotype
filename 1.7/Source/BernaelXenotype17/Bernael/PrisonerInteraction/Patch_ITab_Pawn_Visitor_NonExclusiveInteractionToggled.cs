using System.Linq;
using RimWorld;
using Verse;
using HarmonyLib;

namespace Bernael_Xenotype
{
    [HarmonyPatch(typeof(ITab_Pawn_Visitor), nameof(ITab_Pawn_Visitor.NonExclusiveInteractionToggled))]
    public static class Patch_ITab_Pawn_Visitor_NonExclusiveInteractionToggled
    {
        public static void Prefix(PrisonerInteractionModeDef mode, bool enabled, ITab_Pawn_Visitor __instance)
        {
            if (ModsConfig.BiotechActive && mode == BernaelDefOf.BX_SoulExecution)
            {
                Pawn selPawn = Traverse.Create(__instance).Property("SelPawn").GetValue<Pawn>();
                if (selPawn == null) return;
                var ext = mode.GetModExtension<PrisonerInteractionModeExtension>();
                if (ext == null || ext.autoSurgeryRecipe == null) return;
                Bill bill = selPawn.BillStack?.Bills?.FirstOrDefault(x => x.recipe == ext.autoSurgeryRecipe);
                if (enabled)
                {
                    if (bill == null) HealthCardUtility.CreateSurgeryBill(selPawn, ext.autoSurgeryRecipe, null);
                }
                else
                {
                    if (bill != null) selPawn.BillStack.Bills.Remove(bill);
                }
            }
        }
    }
}
