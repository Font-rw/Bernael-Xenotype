using RimWorld;
using Verse;

namespace Bernael_Xenotype
{
    /// <summary>
    /// A powerful psycaster arrives at the settlement, drawn by the light of the Gaze, and offers to join.
    /// </summary>
    public class IncidentWorker_PsycasterPilgrimage : IncidentWorker_WandererJoin
    {
        public override Pawn GeneratePawn(Map map)
        {
            Pawn pawn = base.GeneratePawn(map);

            int targetLevel = Rand.RangeInclusive(3, 6);

            Hediff_Psylink psylink = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PsychicAmplifier) as Hediff_Psylink;
            if (psylink == null)
            {
                psylink = (Hediff_Psylink)HediffMaker.MakeHediff(HediffDefOf.PsychicAmplifier, pawn);
                pawn.health.AddHediff(psylink);
            }

            int currentLevel = psylink.level;
            if (targetLevel > currentLevel)
            {
                psylink.ChangeLevel(targetLevel - currentLevel, sendLetter: false);
            }

            return pawn;
        }
    }
}
