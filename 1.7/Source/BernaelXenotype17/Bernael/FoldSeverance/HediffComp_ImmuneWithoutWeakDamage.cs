using Verse;

namespace Bernael_Xenotype
{
    public class HediffCompProperties_ImmuneWithoutWeakDamage : HediffCompProperties
    {
        public DamageDef weakDamageDef;

        public HediffCompProperties_ImmuneWithoutWeakDamage()
        {
            compClass = typeof(HediffComp_ImmuneWithoutWeakDamage);
        }
    }

    public class HediffComp_ImmuneWithoutWeakDamage : HediffComp
    {
        public HediffCompProperties_ImmuneWithoutWeakDamage Props => (HediffCompProperties_ImmuneWithoutWeakDamage)props;

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            WorldComponent_ImmuneWithoutWeakDamage.Register(Pawn, this);
        }

        public override void CompPostPostRemoved()
        {
            WorldComponent_ImmuneWithoutWeakDamage.Unregister(Pawn, this);
        }

        public virtual bool CheckPreApplyDamage(ref DamageInfo dinfo)
        {
            return Props.weakDamageDef != null && dinfo.Def != Props.weakDamageDef;
        }
    }
}
