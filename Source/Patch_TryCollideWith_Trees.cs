using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TSA.CE.BulletsPenetrateTrees
{
	[HarmonyPatch]
	public static class Patch_TryCollideWith_Trees
	{
		private const string FleckDefName1 = "DustPuff";
		private const float FleckScale1 = 1f;
		private const string FleckDefName2 = "DirtBits";
		private const float FleckScale2 = 0.6f;

		static System.Reflection.MethodBase TargetMethod()
		{
			var ceType = AccessTools.TypeByName("CombatExtended.ProjectileCE");
			return AccessTools.Method(ceType, "TryCollideWith", new[] { typeof(Thing) });
		}

		static bool Prefix(Thing __instance, Thing thing, ref bool __result)
		{
			if (thing is not Plant plant || plant.def?.plant?.IsTree != true)
				return true;

			if (__instance == null || __instance.Destroyed)
				return true;

			// Explosives: leave entirely to CE (before memoizing).
			if (TreeArmorUtil.IsExplosiveProjectile(__instance, __instance.def))
			{
				if (ModInit.Settings.EnableDebug)
					Log.Message($"[TSA.TreePassThrough] CE-handled explosive: {__instance.def?.defName ?? "null"}");
				return true;
			}

			if (ProjectileTreeHitMemo.WasProcessed(__instance, plant.thingIDNumber))
			{
				if (ModInit.Settings.EnableDebug)
					Log.Message($"[TSA.TreePassThrough] SKIP duplicate plant: proj={__instance.def.defName} plant={plant.def.defName}");
				__result = false;
				return false;
			}

			var S = ModInit.Settings;

			// Optional early miss: treat as canopy / thin foliage — no damage, no AP loss.
			if (S.ChanceToJustPassThrough > 0f && Rand.Chance(S.ChanceToJustPassThrough))
			{
				ProjectileTreeHitMemo.MarkProcessed(__instance, plant.thingIDNumber);
				if (S.EnableDebug)
					Log.Message($"[TSA.TreePassThrough] JUST PASS THROUGH (chance {S.ChanceToJustPassThrough:P0}): proj={__instance.def.defName} plant={plant.def.defName}");
				__result = false;
				return false;
			}

			float armor = TreeArmorUtil.ComputeTreeArmorSharp(plant);

			float apBefore;
			string apSource;
			if (TreeArmorUtil.TryGetEffectiveAP(__instance, out float ap))
			{
				apBefore = ap;
				apSource = "PenetrationAmount";
			}
			else
			{
				apBefore = 0f;
				apSource = "unknown";
			}

			float ratio = armor <= 0f ? 999f : (apBefore / armor);
			int band = GetBandIndex(ratio);

			float penChance = Mathf.Clamp01(S.PenetrationChances[band]);
			float apLossFrac = Mathf.Clamp01(S.APLossFractions[band]);

			if (S.EnableDebug)
			{
				Log.Message($"[TSA.TreePassThrough] TREE-HIT proj={__instance.def.defName} AP={apBefore:0.###} ({apSource}), armor={armor:0.###}, ratio={ratio:0.##}x → band {band} | Pass={penChance:P0} | APLoss={apLossFrac:P0}");
			}

			ProjectileTreeHitMemo.MarkProcessed(__instance, plant.thingIDNumber);

			if (Rand.Chance(penChance))
			{
				float keepFrac = 1f - apLossFrac;
				ProjectileAPTracker.MultiplyFraction(__instance, keepFrac);

				if (S.EnableDebug)
				{
					ProjectileAPTracker.TryGetFraction(__instance, out float frac);
					Log.Message($"[TSA.TreePassThrough] PASS (band {band}). AP fraction now {frac:0.###}");
				}

				SpawnImpactFleck(plant);
				ApplyTreeDamage(plant, __instance, S.TreeDamageOnPass);

				__result = false;
				return false;
			}

			if (S.EnableDebug)
				Log.Message($"[TSA.TreePassThrough] BLOCK (band {band}).");

			SpawnImpactFleck(plant);
			ApplyTreeDamage(plant, __instance, S.TreeDamageOnBlock);
			ProjectileAPTracker.Clear(__instance);
			ProjectileTreeHitMemo.Clear(__instance);
			__instance.Destroy(DestroyMode.Vanish);

			__result = true;
			return false;
		}

		private static int GetBandIndex(float ratio)
		{
			if (ratio < 0.25f) return 0;
			if (ratio < 0.5f) return 1;
			if (ratio < 0.75f) return 2;
			if (ratio < 1.0f) return 3;
			if (ratio < 1.25f) return 4;
			if (ratio < 1.5f) return 5;
			if (ratio < 1.75f) return 6;
			if (ratio < 2.0f) return 7;
			return 8;
		}

		private static void SpawnImpactFleck(Plant tree)
		{
			try
			{
				if (tree?.Map == null)
					return;

				float sizeFactor = tree.def.fillPercent * Mathf.Lerp(0.3f, 1.0f, Mathf.Clamp01(tree.Growth));
				float scale1 = Mathf.Max(0.1f, FleckScale1 * sizeFactor);
				float scale2 = Mathf.Max(0.1f, FleckScale2 * sizeFactor);

				var fleck1 = DefDatabase<FleckDef>.GetNamedSilentFail(FleckDefName1);
				var fleck2 = DefDatabase<FleckDef>.GetNamedSilentFail(FleckDefName2);

				if (fleck1 != null)
					FleckMaker.Static(tree.DrawPos, tree.Map, fleck1, scale1);
				if (fleck2 != null)
					FleckMaker.Static(tree.DrawPos, tree.Map, fleck2, scale2);
			}
			catch { }
		}

		private static void ApplyTreeDamage(Plant tree, Thing proj, float multiplier)
		{
			if (tree == null || proj == null)
				return;

			try
			{
				multiplier = Mathf.Clamp(multiplier, 0f, 4f);
				int baseDmg = TreeArmorUtil.GetProjectileBaseDamage(proj);
				if (baseDmg <= 0)
					return;

				int amount = Mathf.RoundToInt(baseDmg * multiplier);
				if (amount <= 0)
					return;

				var dinfo = new DamageInfo(DamageDefOf.Bullet, amount, armorPenetration: 0f, angle: -1f,
					instigator: null, hitPart: null, weapon: proj.def);
				tree.TakeDamage(dinfo);

				if (ModInit.Settings.EnableDebug)
					Log.Message($"[TSA.TreePassThrough] TREE DAMAGE: {tree.def.defName} took {amount} dmg (x{multiplier:0.##}) from {proj.def.defName}");
			}
			catch { }
		}
	}
}
