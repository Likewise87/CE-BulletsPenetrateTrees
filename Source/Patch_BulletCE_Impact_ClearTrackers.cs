using System.Reflection;
using HarmonyLib;
using Verse;

namespace TSA.CE.BulletsPenetrateTrees
{
	/// <summary>Clears per-projectile trackers when a CE bullet finishes impacting.</summary>
	[HarmonyPatch]
	internal static class Patch_BulletCE_Impact_ClearTrackers
	{
		static MethodBase? TargetMethod()
		{
			var bulletType = AccessTools.TypeByName("CombatExtended.BulletCE");
			return bulletType == null ? null : AccessTools.Method(bulletType, "Impact", new[] { typeof(Thing) });
		}

		static void Finalizer(Thing __instance)
		{
			ProjectileAPTracker.Clear(__instance);
			ProjectileTreeHitMemo.Clear(__instance);
		}
	}
}
