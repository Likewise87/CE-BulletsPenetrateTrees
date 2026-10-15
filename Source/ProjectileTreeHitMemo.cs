using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Verse;

namespace TSA.CE.BulletsPenetrateTrees
{
	internal static class ProjectileTreeHitMemo
	{
		private class Holder
		{
			public HashSet<int> ProcessedPlants = new();
		}

		private static readonly ConditionalWeakTable<Thing, Holder> Table = new();

		public static bool WasProcessed(Thing proj, int plantId)
		{
			if (proj == null)
				return false;
			return Table.TryGetValue(proj, out Holder? h) && h != null && h.ProcessedPlants.Contains(plantId);
		}

		public static void MarkProcessed(Thing proj, int plantId)
		{
			if (proj == null)
				return;
			Table.GetOrCreateValue(proj).ProcessedPlants.Add(plantId);
		}

		public static void Clear(Thing proj)
		{
			if (proj == null)
				return;
			Table.Remove(proj);
		}
	}
}
