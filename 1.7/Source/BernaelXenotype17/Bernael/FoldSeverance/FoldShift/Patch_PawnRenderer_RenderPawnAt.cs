using System.Collections.Generic;
using Verse;
using UnityEngine;
using HarmonyLib;

namespace Bernael_Xenotype
{
    [HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.RenderPawnAt))]
    public static class Patch_PawnRenderer_RenderPawnAt
    {
        private static readonly float NorthLayerOffset = PawnRenderUtility.AltitudeForLayer(-10f);
        private static readonly float OtherLayerOffset = PawnRenderUtility.AltitudeForLayer(90f);

        public static bool Prefix(Pawn ___pawn, Vector3 drawLoc, Rot4? rotOverride, bool neverAimWeapon)
        {
            if (FoldShiftVisuals.Capturing || !WorldComponent_FoldShiftVisual.TryGet(___pawn, out List<HediffComp_FoldShiftVisual> comps))
                return true;
            if (!___pawn.Spawned || ___pawn.Position.Fogged(___pawn.Map))
                return true;
            float traversalOpacity = ___pawn.Map.GetComponent<FoldGateMapComponent>().Opacity(___pawn, drawLoc);
            if (traversalOpacity < 0.01f) return false;
            for (int i = 0; i < comps.Count; i++)
            {
                HediffComp_FoldShiftVisual visualComp = comps[i];
                if (visualComp == null) continue;
                if (!visualComp.Draw(drawLoc, ___pawn, rotOverride, out bool hidesPawn)) return true;
                // While the shadow still rises over the pawn, the game draws the pawn beneath it.
                if (!hidesPawn) return true;
                // The captured render tree contains clothes, but held weapons and
                // worn extras are drawn separately by the game and must remain visible.
                Rot4 facing = rotOverride ?? ___pawn.Rotation;
                Vector3 equipmentPosition = drawLoc;
                equipmentPosition.y = FoldShiftVisuals.ShadowAltitudeAt(drawLoc) +
                    (facing == Rot4.North ? NorthLayerOffset : OtherLayerOffset);
                if (traversalOpacity > 0.95f)
                {
                    PawnRenderUtility.DrawEquipmentAndApparelExtras(
                        ___pawn,
                        equipmentPosition,
                        facing,
                        neverAimWeapon ? PawnRenderFlags.NeverAimWeapon : PawnRenderFlags.None
                    );
                }

                return false;
            }

            return true;
        }
    }
}
