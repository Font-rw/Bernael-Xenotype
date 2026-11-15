using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.Sound;
using VEF.Abilities;

namespace Bernael_Xenotype
{
    // Brands the targeted pawn. With psychic set on the props,
    // Psycast already turns away targets without psychic sensitivity and shows each target's sensitivity while aiming.
    public sealed class AbilityExtension_MaliciousSeal : AbilityExtension_AbilityMod
    {
        public SoundDef brandSound;

        public override void Cast(GlobalTargetInfo[] targets, VEF.Abilities.Ability ability)
        {
            base.Cast(targets, ability);
            if (targets == null || targets.Length == 0) return;

            Pawn victim = targets[0].Pawn;
            MaliciousSealUtility.Brand(victim, ability.CasterPawn, ability.CasterPawn.Faction);
            FleckMaker.Static(victim.DrawPos, victim.Map, FleckDefOf.PsycastSkipFlashEntry, 1.4f);
            brandSound?.PlayOneShot(new TargetInfo(victim.Position, victim.Map));
        }

        public override bool Valid(GlobalTargetInfo[] targets, VEF.Abilities.Ability ability, bool throwMessages = false)
        {
            if (targets == null || targets.Length == 0) return false;

            Pawn pawn = targets[0].Pawn;
            return pawn != null && pawn != ability.CasterPawn && MaliciousSealUtility.CanSeal(pawn) && base.Valid(targets, ability, throwMessages);
        }
    }
}
