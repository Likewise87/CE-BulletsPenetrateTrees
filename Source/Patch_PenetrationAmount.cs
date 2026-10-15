using System.Reflection;
using HarmonyLib;
using Verse;

namespace TSA.CE.BulletsPenetrateTrees
{
	/// <summary>
	/// Scales CE PenetrationAmount by the remaining AP fraction after tree passes.
	/// Does not mutate shared ammo def properties.
	/// </summary>
	[HarmonyPatch]
	internal static class Patch_PenetrationAmount
	{
		static MethodBase? TargetMethod()
		{
			var ceType = AccessTools.TypeByName("CombatExtended.ProjectileCE");
			return ceType == null ? null : AccessTools.PropertyGetter(ceType, "PenetrationAmount");
		}

		static void Postfix(Thing __instance, ref float __result)
		{
			if (__instance == null)
				return;
			if (!ProjectileAPTracker.TryGetFraction(__instance, out float fraction))
				return;
			if (fraction >= 0.9999f)
				return;
			__result *= fraction;
		}
	}
}
