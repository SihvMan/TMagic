﻿using System.Linq;
using RimWorld;
using Verse;
using AbilityUser;
using UnityEngine;

namespace TorannMagic
{
	class Projectile_Snowball : Projectile_AbilityBase
	{

        private int verVal;
        private int pwrVal;

		protected override void Impact(Thing hitThing)
		{
			Map map = base.Map;
			base.Impact(hitThing);
			ThingDef def = this.def;
            
            Pawn pawn = this.launcher as Pawn;
            CompAbilityUserMagic comp = pawn.GetComp<CompAbilityUserMagic>();
            MagicPowerSkill pwr = pawn.GetComp<CompAbilityUserMagic>().MagicData.MagicPowerSkill_Snowball.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Snowball_pwr");
            MagicPowerSkill ver = pawn.GetComp<CompAbilityUserMagic>().MagicData.MagicPowerSkill_Snowball.FirstOrDefault((MagicPowerSkill x) => x.label == "TM_Snowball_ver");
            ModOptions.SettingsRef settingsRef = new ModOptions.SettingsRef();
            pwrVal = pwr.level;
            verVal = ver.level;
            if(settingsRef.AIHardMode && !pawn.IsColonistPlayerControlled)
            {
                pwrVal = 3;
                verVal = 3;
            }
            GenExplosion.DoExplosion(base.Position, map, this.def.projectile.explosionRadius + (0.7f * (float)pwrVal), TMDamageDefOf.DamageDefOf.Snowball, this.launcher, this.def.projectile.damageAmountBase, SoundDefOf.Crunch, def, this.equipmentDef, null, 0f, 1, false, null, 0f, 1, 0f, true);
            CellRect cellRect = CellRect.CenteredOn(base.Position, 3 + (verVal * 1));
			cellRect.ClipInsideMap(map);
			for (int i = 0; i < verVal * 5; i++)
			{
				IntVec3 randomCell = cellRect.RandomCell;
				this.IceExplosion(pwrVal, randomCell, map, (float)verVal * 0.4f);
				Vector3 loc = randomCell.ToVector3Shifted();
				MoteMaker.ThrowSmoke(loc, map, 4.2f);
				MoteMaker.ThrowSmoke(loc, map, 0.6f * (float)pwrVal);
				MoteMaker.ThrowAirPuffUp(loc, map);
				MoteMaker.ThrowDustPuff(loc, map, 1.0f * (float)pwrVal);
				AddSnowRadial(randomCell, map, this.def.projectile.explosionRadius, (this.def.projectile.explosionRadius / 2 )+ (0.7f * (float)verVal));
			}
			AddSnowRadial(base.Position, map, this.def.projectile.explosionRadius, this.def.projectile.explosionRadius + (0.7f * (float)verVal));
		}

		protected void IceExplosion(int pwr, IntVec3 pos, Map map, float radius)
		{
			ThingDef def = this.def;
			Explosion(pwr, pos, map, radius, TMDamageDefOf.DamageDefOf.Snowball, this.launcher, SoundDefOf.Crunch, def, this.equipmentDef, ThingDefOf.Mote_Smoke, 1.2f, 1, false, null, 0f, 1);
            Explosion(pwr, pos, map, radius, DamageDefOf.Extinguish, this.launcher, this.def.projectile.soundExplode, def, this.equipmentDef, ThingDefOf.Mote_WaterSplash, 1.8f, 1, false, null, 0f, 1);
        }

		public static void Explosion(int pwr, IntVec3 center, Map map, float radius, DamageDef damType, Thing instigator, SoundDef explosionSound, ThingDef projectile = null, ThingDef source = null, ThingDef postExplosionSpawnThingDef = null, float postExplosionSpawnChance = 0f, int postExplosionSpawnThingCount = 1, bool applyDamageToExplosionCellsNeighbors = false, ThingDef preExplosionSpawnThingDef = null, float preExplosionSpawnChance = 0f, int preExplosionSpawnThingCount = 1)
		{
			System.Random rnd = new System.Random();
			int modDamAmountRand = pwr * GenMath.RoundRandom(rnd.Next(1, projectile.projectile.damageAmountBase * pwr)); //7
			if (map == null)
			{
				Log.Warning("Tried to do explosion in a null map.");
				return;
			}
            Explosion explosion = (Explosion)GenSpawn.Spawn(ThingDefOf.Explosion, center, map);
            explosion.dealMoreDamageAtCenter = false;
            explosion.chanceToStartFire = 0.0f;
            explosion.Position = center;
			explosion.radius = radius;
			explosion.damType = damType;
			explosion.instigator = instigator;
			explosion.damAmount = ((projectile == null) ? GenMath.RoundRandom((float)damType.explosionDamage) : modDamAmountRand);
			explosion.weapon = source;
			explosion.preExplosionSpawnThingDef = preExplosionSpawnThingDef;
			explosion.preExplosionSpawnChance = preExplosionSpawnChance;
			explosion.preExplosionSpawnThingCount = preExplosionSpawnThingCount;
			explosion.postExplosionSpawnThingDef = postExplosionSpawnThingDef;
			explosion.postExplosionSpawnChance = postExplosionSpawnChance;
			explosion.postExplosionSpawnThingCount = postExplosionSpawnThingCount;
			explosion.applyDamageToExplosionCellsNeighbors = applyDamageToExplosionCellsNeighbors;
            explosion.StartExplosion(explosionSound);
		}

		public static void AddSnowRadial(IntVec3 center, Map map, float radius, float depth)
		{
			int num = GenRadial.NumCellsInRadius(radius);
			for (int i = 0; i < num; i++)
			{
				IntVec3 intVec = center + GenRadial.RadialPattern[i];
				if (intVec.InBounds(map))
				{
					float lengthHorizontal = (center - intVec).LengthHorizontal;
					float num2 = 1f - lengthHorizontal / radius;
					map.snowGrid.AddDepth(intVec, num2 * depth);
                    
				}
			}
		}

	}
}