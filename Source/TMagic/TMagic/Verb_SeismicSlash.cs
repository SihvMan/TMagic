﻿using Verse;
using AbilityUser;
using UnityEngine;
using RimWorld;
using System.Linq;
using System.Collections.Generic;

namespace TorannMagic
{
    [StaticConstructorOnStartup]
    public class Verb_SeismicSlash : Verb_UseAbility
    {
        MightPowerSkill pwr;
        MightPowerSkill ver;
        MightPowerSkill str;

        float weaponDPS = 0;
        float dmgMultiplier = 1;
        float pawnDPS = 0;
        float skillMultiplier = 1;
        ThingWithComps weaponComp;
        int dmgNum = 0;
        bool flag10;

        protected Vector3 origin;
        protected Vector3 destination;
        protected int ticksToImpact;

        private static readonly Color cleaveColor = new Color(160f, 160f, 160f);
        private static readonly Material bladeMat = MaterialPool.MatFrom("Spells/cleave", false);

        bool validTarg;
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ.Thing != null && targ.Thing == this.caster)
            {
                return this.verbProps.targetParams.canTargetSelf;
            }
            if (targ.IsValid && targ.CenterVector3.InBounds(base.CasterPawn.Map) && !targ.Cell.Fogged(base.CasterPawn.Map) && targ.Cell.Walkable(base.CasterPawn.Map))
            {
                if ((root - targ.Cell).LengthHorizontal < this.verbProps.range)
                {
                    ShootLine shootLine;
                    validTarg = this.TryFindShootLineFromTo(root, targ, out shootLine);
                }
                else
                {
                    validTarg = false;
                }
            }
            else
            {
                validTarg = false;
            }
            return validTarg;
        }

        protected int StartingTicksToImpact
        {
            get
            {
                int num = Mathf.RoundToInt((this.origin - this.destination).magnitude);
                bool flag = num < 1;
                if (flag)
                {
                    num = 1;
                }
                return num;
            }
        }

        public virtual Vector3 ExactPosition
        {
            get
            {
                Vector3 b = (this.destination - this.origin) * (1f - (float)this.ticksToImpact / (float)this.StartingTicksToImpact);
                return this.origin + b + Vector3.up * 2;
            }
        }

        public virtual Quaternion ExactRotation
        {
            get
            {
                return Quaternion.LookRotation(this.destination - this.origin);
            }
        }

        protected override bool TryCastShot()
        {

            CellRect cellRect = CellRect.CenteredOn(base.CasterPawn.Position, 1);
            Map map = base.CasterPawn.Map;
            cellRect.ClipInsideMap(map);

            IntVec3 centerCell = cellRect.CenterCell;
            CompAbilityUserMight comp = this.CasterPawn.GetComp<CompAbilityUserMight>();
            pwr = comp.MightData.MightPowerSkill_SeismicSlash.FirstOrDefault((MightPowerSkill x) => x.label == "TM_SeismicSlash_pwr");
            ver = comp.MightData.MightPowerSkill_SeismicSlash.FirstOrDefault((MightPowerSkill x) => x.label == "TM_SeismicSlash_ver");
            str = comp.MightData.MightPowerSkill_global_strength.FirstOrDefault((MightPowerSkill x) => x.label == "TM_global_strength_pwr");
            this.origin = base.CasterPawn.Position.ToVector3();
            this.destination = this.currentTarget.Cell.ToVector3Shifted();
            this.ticksToImpact = Mathf.RoundToInt((this.origin - this.destination).magnitude);

            if (this.CasterPawn.equipment.Primary != null && !this.CasterPawn.equipment.Primary.def.IsRangedWeapon)
            {

                weaponComp = base.CasterPawn.equipment.Primary;
                weaponDPS = weaponComp.GetStatValue(StatDefOf.MeleeWeapon_AverageDPS, false) * .7f;
                dmgMultiplier = weaponComp.GetStatValue(StatDefOf.MeleeWeapon_DamageMultiplier, false);
                pawnDPS = base.CasterPawn.GetStatValue(StatDefOf.MeleeDPS, false);
                skillMultiplier = (.7f + (.07f * pwr.level) + (.025f * str.level));
                dmgNum = Mathf.RoundToInt(skillMultiplier * dmgMultiplier * (pawnDPS + weaponDPS));
                ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
                if (!this.CasterPawn.IsColonistPlayerControlled && settingsRef.AIHardMode)
                {
                    dmgNum += 10;
                }

                Vector3 strikeVec = this.origin;
                DrawBlade(strikeVec, 0);
                for (int i = 0; i < this.StartingTicksToImpact; i++)
                {
                    strikeVec = this.ExactPosition;
                    Pawn victim = strikeVec.ToIntVec3().GetFirstPawn(map);
                    if (victim != null && victim.Faction != base.CasterPawn.Faction)
                    {
                        DrawStrike(strikeVec.ToIntVec3(), strikeVec, map);
                        damageEntities(victim, null, dmgNum, DamageDefOf.Cut);
                    }
                    MoteMaker.ThrowTornadoDustPuff(strikeVec, map, .6f, Color.white);
                    for (int j = 0; j < 2+(2*ver.level); j++)
                    {
                        IntVec3 searchCell = strikeVec.ToIntVec3() + GenAdj.AdjacentCells8WayRandomized()[j];
                        MoteMaker.ThrowTornadoDustPuff(searchCell.ToVector3(), map, .1f, Color.gray);
                        victim = searchCell.GetFirstPawn(map);
                        if (victim != null && victim.Faction != base.CasterPawn.Faction)
                        {
                            DrawStrike(searchCell, searchCell.ToVector3(), map);
                            damageEntities(victim, null, dmgNum, DamageDefOf.Cut);
                        }
                    }
                    this.ticksToImpact--;
                }
            }
            else
            {
                Messages.Message("MustHaveMeleeWeapon".Translate(new object[]
                {
                    this.CasterPawn.LabelCap
                }), MessageTypeDefOf.RejectInput);
                return false;
            }

            this.burstShotsLeft = 0;
            this.PostCastShot(flag10, out flag10);
            return flag10;
        }

        private void DrawBlade(Vector3 center, float magnitude)
        {
            Graphics.DrawMesh(MeshPool.plane10, center, this.ExactRotation, Verb_SeismicSlash.bladeMat, 0);           
        }

        public void DrawStrike(IntVec3 center, Vector3 strikePos, Map map)
        {
            TM_MoteMaker.ThrowMultiStrike(strikePos, map, .5f);
            TM_MoteMaker.ThrowBloodSquirt(strikePos, map, 1.2f);
        }        

        public void damageEntities(Pawn victim, BodyPartRecord hitPart, int amt, DamageDef type)
        {
            DamageInfo dinfo;
            amt = (int)((float)amt * Rand.Range(.7f, 1.3f));
            dinfo = new DamageInfo(type, amt, (float)-1, this.CasterPawn, hitPart, null, DamageInfo.SourceCategory.ThingOrUnknown);            
            dinfo.SetAllowDamagePropagation(false);
            victim.TakeDamage(dinfo);
        }
    }
}
