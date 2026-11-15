using RimWorld;
using RimWorld.Planet;
using Verse;
using VEF.Abilities;

namespace Bernael_Xenotype
{
    public sealed class AbilityExtension_DarkMirage : AbilityExtension_AbilityMod
    {
        public ThingDef mirageDef;

        public override void Cast(GlobalTargetInfo[] targets, VEF.Abilities.Ability ability)
        {
            base.Cast(targets, ability);
            if (targets == null || targets.Length == 0) return;

            MirageUtility.Spawn(ability.CasterPawn, targets[0].Cell, mirageDef);
        }

        public override bool Valid(GlobalTargetInfo[] targets, VEF.Abilities.Ability ability, bool throwMessages = false)
        {
            if (targets == null || targets.Length == 0) return false;
            
            if (!MirageVisuals.ShaderReady(mirageDef))
            {
                if (throwMessages) Messages.Message("BX_MirageShaderMissing".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }
            bool valid = ability.CasterPawn.Spawned && ability.CasterPawn.Faction != null &&
                MirageUtility.CanSpawnAt(targets[0].Cell, targets[0].Map ?? ability.CasterPawn?.Map);
            if (!valid && throwMessages)
                Messages.Message("BX_MirageBlocked".Translate(), MessageTypeDefOf.RejectInput, false);
            return valid;
        }
    }
}
