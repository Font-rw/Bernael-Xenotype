using RimWorld;
using Verse;
using UnityEngine;
using HarmonyLib;

namespace Bernael_Xenotype
{
    [HarmonyPatch(typeof(PawnHeadOverlays), "RenderStatusOverlays")]
    public static class Patch_PawnHeadOverlays_RenderStatusOverlays
    {
        public static void Postfix(Pawn ___pawn, Vector3 offset, Quaternion quat, Mesh headMesh)
        {
            MentalStateDef mentalStateDef = ___pawn.mindState?.mentalStateHandler?.CurStateDef;
            if (mentalStateDef != null && mentalStateDef == BernaelDefOf.BX_Terror)
            {
                Vector3 vector = ___pawn.DrawPos + offset + new Vector3(0f, 0f, 0.32f);
                if (ThingDefOf.Mote_TerrorHalo?.graphicData?.Graphic != null)
                {
                    ThingDefOf.Mote_TerrorHalo.graphicData.Graphic.DrawWorker(vector, ___pawn.Rotation, null, null, 0f);
                }
            }
        }
    }
}
