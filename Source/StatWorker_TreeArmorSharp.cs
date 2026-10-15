using Verse;
using RimWorld;
using UnityEngine;

namespace TSA.CE.BulletsPenetrateTrees
{
    // Appears in the Info Card's "Basics" section. Shows for trees.
    public class StatWorker_TreeArmorSharp : StatWorker
    {
        public override bool ShouldShowFor(StatRequest req)
        {
            if (req.HasThing && req.Thing is Plant p) return p.def?.plant?.IsTree == true;
            if (req.Def is ThingDef td && td.plant != null) return td.plant.IsTree;
            return false;
        }

        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            if (req.HasThing && req.Thing is Plant p && p.def?.plant?.IsTree == true)
                return TreeArmorUtil.ComputeTreeArmorSharp(p);

            if (req.Def is ThingDef td && td.plant?.IsTree == true)
            {
                var S = ModInit.Settings;
                float hpTerm = Mathf.Max(1f, td.BaseMaxHitPoints) / Mathf.Max(0.001f, S.HpDivisor);
                float fillTerm = Mathf.Max(0f, td.fillPercent) * S.FillMultiplier;
                float armor = (hpTerm + fillTerm) * 1f; // growthFactor = 1 (mature)
                return Mathf.Max(S.MinArmor, armor);
            }

            return 0f;
        }

        public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            var S = ModInit.Settings;

            if (req.HasThing && req.Thing is Plant p && p.def?.plant?.IsTree == true)
            {
                float hpTerm = Mathf.Max(1f, p.MaxHitPoints) / Mathf.Max(0.001f, S.HpDivisor);
                float fillTerm = Mathf.Max(0f, p.def.fillPercent) * S.FillMultiplier;
                float growth = Mathf.Clamp01(p.Growth);
                float factor = Mathf.Lerp(Mathf.Clamp01(S.MinGrowthFactor), 1f, growth);
                float total = Mathf.Max(S.MinArmor, (hpTerm + fillTerm) * factor);

                return $"HP term: {hpTerm:0.##} (HP ÷ {S.HpDivisor:0.###})\n" +
                       $"Fill term: {fillTerm:0.##} (fill × {S.FillMultiplier:0.###})\n" +
                       $"Growth: {growth:P0} → factor {factor:0.##}\n" +
                       $"Min armor clamp: {S.MinArmor:0.##}\n" +
                       $"= {total:0.##}";
            }

            if (req.Def is ThingDef td && td.plant?.IsTree == true)
            {
                float hpTerm = Mathf.Max(1f, td.BaseMaxHitPoints) / Mathf.Max(0.001f, S.HpDivisor);
                float fillTerm = Mathf.Max(0f, td.fillPercent) * S.FillMultiplier;
                float total = Mathf.Max(S.MinArmor, (hpTerm + fillTerm)); // mature factor = 1

                return $"(Estimated at mature growth)\n" +
                       $"HP term: {hpTerm:0.##} (HP ÷ {S.HpDivisor:0.###})\n" +
                       $"Fill term: {fillTerm:0.##} (fill × {S.FillMultiplier:0.###})\n" +
                       $"Min armor clamp: {S.MinArmor:0.##}\n" +
                       $"= {total:0.##}";
            }

            return null;
        }
    }
}
