using System;
using System.Reflection;
using System.Text;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TSA.CE.BulletsPenetrateTrees
{
	internal static class TreeArmorUtil
	{
		private static readonly PropertyInfo? PenetrationAmountProp;
		private static readonly Type? ProjectileCEType;
		private static readonly Type? ProjectileCEExplosiveType;
		private static readonly FieldInfo? DamageAmountBaseField;
		private static bool _loggedPenetrationMissing;

		static TreeArmorUtil()
		{
			ProjectileCEType = AccessTools.TypeByName("CombatExtended.ProjectileCE");
			ProjectileCEExplosiveType = AccessTools.TypeByName("CombatExtended.ProjectileCE_Explosive");
			if (ProjectileCEType != null)
				PenetrationAmountProp = AccessTools.Property(ProjectileCEType, "PenetrationAmount");

			var propsType = AccessTools.TypeByName("CombatExtended.ProjectilePropertiesCE")
				?? typeof(ProjectileProperties);
			DamageAmountBaseField = AccessTools.Field(propsType, "damageAmountBase");
		}

		public static float ComputeTreeArmorSharp(Plant plant)
		{
			if (plant?.def == null)
				return ModInit.Settings.MinArmor;

			var S = ModInit.Settings;
			float hp = Mathf.Max(1f, plant.MaxHitPoints);
			float fill = Mathf.Clamp01(plant.def.fillPercent);
			float growth = Mathf.Clamp01(plant.Growth);

			float hpTerm = hp / Mathf.Max(0.001f, S.HpDivisor);
			float fillTerm = fill * S.FillMultiplier;
			float factor = Mathf.Lerp(Mathf.Clamp01(S.MinGrowthFactor), 1f, growth);
			return Mathf.Max(S.MinArmor, (hpTerm + fillTerm) * factor);
		}

		/// <summary>CE effective AP (PenetrationAmount), already reduced by prior tree-pass fraction via Harmony postfix.</summary>
		public static bool TryGetEffectiveAP(Thing proj, out float ap)
		{
			ap = 0f;
			if (proj == null)
				return false;

			if (PenetrationAmountProp != null && ProjectileCEType != null && ProjectileCEType.IsInstanceOfType(proj))
			{
				try
				{
					ap = Mathf.Max(0f, (float)PenetrationAmountProp.GetValue(proj));
					return true;
				}
				catch
				{
					if (!_loggedPenetrationMissing)
					{
						_loggedPenetrationMissing = true;
						Log.Warning("[TSA.TreePassThrough] Failed to read PenetrationAmount.");
					}
				}
			}

			return false;
		}

		public static bool IsExplosiveProjectile(Thing proj, ThingDef? def)
		{
			if (proj != null && ProjectileCEExplosiveType != null && ProjectileCEExplosiveType.IsInstanceOfType(proj))
				return true;

			if (def == null)
				return false;

			if (def.thingClass != null)
			{
				string cn = def.thingClass.Name ?? string.Empty;
				if (cn.IndexOf("Explosive", StringComparison.OrdinalIgnoreCase) >= 0)
					return true;
			}

			var props = def.projectile;
			return props != null && props.explosionRadius > 0f;
		}

		public static int GetProjectileBaseDamage(Thing proj)
		{
			try
			{
				var props = proj?.def?.projectile;
				if (props == null)
					return 0;

				if (DamageAmountBaseField != null)
				{
					object? val = DamageAmountBaseField.GetValue(props);
					if (val is int i && i > 0)
						return i;
					if (val is float f && f > 0f)
						return Mathf.RoundToInt(f);
				}

				try
				{
					return Mathf.Max(0, props.GetDamageAmount((Thing)null!, (StringBuilder)null!));
				}
				catch { }
			}
			catch { }

			return 0;
		}
	}
}
