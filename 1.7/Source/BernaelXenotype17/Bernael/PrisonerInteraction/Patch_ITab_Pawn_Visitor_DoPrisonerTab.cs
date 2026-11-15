using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Bernael_Xenotype
{
    [HarmonyPatch(typeof(ITab_Pawn_Visitor), "DoPrisonerTab")]
    public static class Patch_ITab_Pawn_Visitor_DoPrisonerTab
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo allDefsGetter = AccessTools.PropertyGetter(typeof(DefDatabase<PrisonerInteractionModeDef>), nameof(DefDatabase<PrisonerInteractionModeDef>.AllDefs));
            MethodInfo filterMethod = AccessTools.Method(typeof(Patch_ITab_Pawn_Visitor_DoPrisonerTab), nameof(FilterSoulfeederModes));

            foreach (var code in instructions)
            {
                yield return code;
                if (code.Calls(allDefsGetter))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, filterMethod);
                }
            }
        }

        private static IEnumerable<PrisonerInteractionModeDef> FilterSoulfeederModes(IEnumerable<PrisonerInteractionModeDef> modes, ITab_Pawn_Visitor tab)
        {
            Pawn pawn = Traverse.Create(tab).Property("SelPawn").GetValue<Pawn>();
            foreach (var mode in modes)
            {
                var ext = mode.GetModExtension<PrisonerInteractionModeExtension>();
                if (ext != null && ext.hideIfNoSoulfeeders && pawn?.MapHeld != null && !PrisonerInteractionUtility.ColonyHasAnySoulFeeder(pawn.MapHeld))
                {
                    continue;
                }
                yield return mode;
            }
        }
    }
}
