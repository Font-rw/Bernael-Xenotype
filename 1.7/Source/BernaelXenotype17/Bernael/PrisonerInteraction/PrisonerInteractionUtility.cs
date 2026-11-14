using Verse;

namespace Bernael_Xenotype
{
    public class PrisonerInteractionModeExtension : DefModExtension
    {
        public bool hideIfNoSoulfeeders;
        public RecipeDef autoSurgeryRecipe;
    }

    public static class PrisonerInteractionUtility
    {
        public static bool ColonyHasAnySoulFeeder(Map map)
        {
            if (!ModsConfig.BiotechActive) return false;
            foreach (Pawn item in map.mapPawns.FreeColonistsAndPrisonersSpawned)
            {
                if (SoulUtility.IsSoulFeeder(item))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
