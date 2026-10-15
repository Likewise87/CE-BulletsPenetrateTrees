using System.Runtime.CompilerServices;
using Verse;

namespace TSA.CE.BulletsPenetrateTrees
{
	/// <summary>
	/// Remaining AP fraction after tree passes (1 = unmodified). Keys are CE ProjectileCE instances (Thing).
	/// </summary>
	internal static class ProjectileAPTracker
	{
		private static readonly ConditionalWeakTable<Thing, Holder> Table = new();

		internal class Holder
		{
			public float RemainingFraction = 1f;
		}

		public static bool TryGetFraction(Thing proj, out float fraction)
		{
			fraction = 1f;
			if (proj == null)
				return false;
			if (!Table.TryGetValue(proj, out Holder? h) || h == null)
				return false;
			fraction = h.RemainingFraction;
			return true;
		}

		public static void SetFraction(Thing proj, float fraction)
		{
			if (proj == null)
				return;
			Table.GetOrCreateValue(proj).RemainingFraction = fraction < 0f ? 0f : fraction;
		}

		public static void MultiplyFraction(Thing proj, float factor)
		{
			if (proj == null)
				return;
			float current = TryGetFraction(proj, out float f) ? f : 1f;
			SetFraction(proj, current * factor);
		}

		public static void Clear(Thing proj)
		{
			if (proj == null)
				return;
			Table.Remove(proj);
		}
	}
}
