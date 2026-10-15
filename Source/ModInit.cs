using System;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace TSA.CE.BulletsPenetrateTrees
{
	public enum SliderFormat { Fixed0, Fixed1, Fixed2, Multiplier, Percent, PercentDecimal }

	public class ModInit : Mod
	{
		public const string HarmonyId = "TSA.CE.BulletsPenetrateTrees";
		public static TreePenetrationSettings Settings;

		private static readonly Color SectionHeaderColor = new Color(0.55f, 0.85f, 1f);

		private Vector2 scrollPos = Vector2.zero;
		private bool generalExpanded = true;
		private bool bandsExpanded = true;
		private bool armorExpanded;
		private bool damageExpanded;

		public ModInit(ModContentPack content) : base(content)
		{
			Settings = GetSettings<TreePenetrationSettings>();
			new Harmony(HarmonyId).PatchAll();
			Log.Message("[TSA.TreePassThrough] Initialized Harmony patches.");
		}

		public override string SettingsCategory() => "CE Bullets Penetrate Trees";

		public override void DoSettingsWindowContents(Rect inRect)
		{
			float contentWidth = inRect.width - 24f;
			float viewHeight = EstimateScrollHeight();
			Rect viewRect = new Rect(0f, 0f, contentWidth, viewHeight);

			Widgets.BeginScrollView(inRect, ref scrollPos, viewRect);

			Listing_Standard l = new Listing_Standard();
			l.Begin(viewRect);
			var s = Settings;

			DrawMenuTopBar(l, "Restore defaults", () => s.ResetToDefaults(),
				() => { generalExpanded = bandsExpanded = armorExpanded = damageExpanded = true; },
				() => { generalExpanded = bandsExpanded = armorExpanded = damageExpanded = false; });

			if (DrawCollapsibleHeader(l, "General", ref generalExpanded, SectionHeaderColor))
			{
				s.ChanceToJustPassThrough = LabeledSlider(l,
					"Chance to just pass through",
					s.ChanceToJustPassThrough, 0f, 1f,
					"After Combat Extended decides a bullet hits a tree, this chance skips the solid hit entirely — no tree damage, no AP loss, bullet keeps flying. Useful to approximate canopy misses / thin foliage.",
					0.01f, SliderFormat.Percent, TreePenetrationSettings.DefChanceToJustPassThrough);

				DrawCheckbox(l, "Enable debug logging", ref s.EnableDebug,
					"Logs tree hit / pass / block decisions to the debug log.",
					defaultValue: TreePenetrationSettings.DefEnableDebug);
			}

			if (DrawCollapsibleHeader(l, "Tree penetration bands", ref bandsExpanded, SectionHeaderColor,
				"When a bullet does solid-hit a tree, AP vs tree armor picks a band. Penetration chance and AP loss on pass are per band."))
			{
				for (int i = 0; i < TreePenetrationSettings.BandCount; i++)
				{
					l.Gap(4f);
					Text.Font = GameFont.Tiny;
					Widgets.Label(l.GetRect(18f), $"<b>{TreePenetrationSettings.BandLabelShort(i)}</b>");
					Text.Font = GameFont.Small;

					float[] pair =
					{
						s.PenetrationChances[i],
						s.APLossFractions[i]
					};
					MultiColumnSlider(l,
						new[] { "Penetration", "AP loss on pass" },
						pair, new Vector2(0f, 1f),
						new[]
						{
							"Chance the bullet penetrates this tree (keeps flying) at this AP/armor band.",
							"If the bullet passes, immediately lose this fraction of Sharp Armor Penetration."
						},
						0.01f, SliderFormat.Percent, 44f,
						new[]
						{
							TreePenetrationSettings.DefPenetrationChances[i],
							TreePenetrationSettings.DefAPLossFractions[i]
						});
					s.PenetrationChances[i] = Mathf.Clamp01(pair[0]);
					s.APLossFractions[i] = Mathf.Clamp01(pair[1]);
				}
			}

			if (DrawCollapsibleHeader(l, "Tree armor calculation", ref armorExpanded, SectionHeaderColor))
			{
				l.Label("Formula: TreeArmor = max(MinArmor, ( (HP ÷ HpDivisor) + (Fill × FillMultiplier) ) × Lerp(MinGrowthFactor, 1, Growth) )");
				l.Gap(4f);

				s.HpDivisor = LabeledSlider(l, "HpDivisor", s.HpDivisor, 1f, 200f,
					"Higher → thinner armor from HP.", 0.5f, SliderFormat.Fixed1, TreePenetrationSettings.DefHpDivisor);
				s.FillMultiplier = LabeledSlider(l, "FillMultiplier", s.FillMultiplier, 0f, 50f,
					"Higher → thicker armor from fillPercent.", 0.1f, SliderFormat.Fixed1, TreePenetrationSettings.DefFillMultiplier);
				s.MinGrowthFactor = LabeledSlider(l, "MinGrowthFactor", s.MinGrowthFactor, 0f, 1f,
					"Sapling baseline growth factor (0–1).", 0.01f, SliderFormat.Percent, TreePenetrationSettings.DefMinGrowthFactor);
				s.MinArmor = LabeledSlider(l, "MinArmor", s.MinArmor, 0f, 50f,
					"Tree Sharp armor is never below this.", 0.1f, SliderFormat.Fixed1, TreePenetrationSettings.DefMinArmor);

				l.Gap(6f);
				DrawOakExample(l);
			}

			if (DrawCollapsibleHeader(l, "Tree damage", ref damageExpanded, SectionHeaderColor))
			{
				s.TreeDamageOnBlock = LabeledSlider(l, "Damage when blocked", s.TreeDamageOnBlock, 0f, 4f,
					"Damage applied to the tree when the bullet is stopped.", 0.05f, SliderFormat.Fixed2, TreePenetrationSettings.DefTreeDamageOnBlock);
				s.TreeDamageOnPass = LabeledSlider(l, "Damage when passing through", s.TreeDamageOnPass, 0f, 4f,
					"Damage applied to the tree when the bullet penetrates (band pass). Not applied on 'just pass through'.", 0.05f, SliderFormat.Fixed2, TreePenetrationSettings.DefTreeDamageOnPass);
			}

			l.End();
			Widgets.EndScrollView();
		}

		private float EstimateScrollHeight()
		{
			float h = 80f;
			if (generalExpanded) h += 90f;
			if (bandsExpanded) h += TreePenetrationSettings.BandCount * 78f + 40f;
			if (armorExpanded) h += 280f;
			if (damageExpanded) h += 90f;
			h += 4 * 50f;
			return Mathf.Max(h, 400f);
		}

		private static void DrawOakExample(Listing_Standard list)
		{
			var oakDef = DefDatabase<ThingDef>.GetNamedSilentFail("Plant_TreeOak");

			float hp = oakDef != null ? Mathf.Max(1f, oakDef.BaseMaxHitPoints) : 200f;
			float fill = oakDef != null ? Mathf.Max(0f, oakDef.fillPercent) : 0.25f;
			float growth = 1f;

			var S = Settings;

			float hpTerm = hp / Mathf.Max(0.001f, S.HpDivisor);
			float fillTerm = fill * S.FillMultiplier;
			float factor = Mathf.Lerp(Mathf.Clamp01(S.MinGrowthFactor), 1f, growth);
			float armor = Mathf.Max(S.MinArmor, (hpTerm + fillTerm) * factor);

			list.Gap(4f);
			list.Label("<b>Example (vanilla Oak, mature)</b>");
			list.Label($"HP={hp:0}  Fill={fill:0.##}  Growth={growth:P0}");
			list.Label($"HP term = {hp:0} ÷ {S.HpDivisor:0.###} = <b>{hpTerm:0.##}</b>");
			list.Label($"Fill term = {fill:0.##} × {S.FillMultiplier:0.###} = <b>{fillTerm:0.##}</b>");
			list.Label($"Growth factor = Lerp({S.MinGrowthFactor:0.##}, 1, {growth:0.##}) = <b>{factor:0.##}</b>");
			list.Label($"Tree armor (Sharp) = max({S.MinArmor:0.##}, ({hpTerm:0.##} + {fillTerm:0.##}) × {factor:0.##}) = <b>{armor:0.##}</b>");
		}

		// ----- WD-style helpers (kept in ModInit; no separate library file) -----

		private static float LabeledSlider(Listing_Standard l, string label, float val, float min, float max, string tooltip = null, float step = 0.1f, SliderFormat format = SliderFormat.Fixed1, float? defaultValue = null)
		{
			l.Gap(2f);
			Rect r = l.GetRect(24f);
			if (!tooltip.NullOrEmpty()) TooltipHandler.TipRegion(r, TooltipWithDefault(tooltip, defaultValue, format));
			Widgets.Label(r.LeftPart(0.5f), $"{label}: {FormatValue(val, format).Colorize(Color.cyan)}");
			return Widgets.HorizontalSlider(r.RightPart(0.5f), val, min, max, false, null, null, null, step);
		}

		private static void MultiColumnSlider(Listing_Standard l, string[] labels, float[] values, Vector2 minMax, string[] tooltips = null, float step = 0.1f, SliderFormat format = SliderFormat.Fixed1, float height = 44f, float[] defaultValues = null)
		{
			l.Gap(4f);
			Rect rowRect = l.GetRect(height);
			int count = labels.Length;
			float colWidth = rowRect.width / count;
			for (int i = 0; i < count; i++)
			{
				if (labels[i].NullOrEmpty()) continue;
				Rect colRect = new Rect(rowRect.x + (colWidth * i), rowRect.y, colWidth - 10f, rowRect.height);
				if (tooltips != null && i < tooltips.Length && !tooltips[i].NullOrEmpty())
				{
					float? def = defaultValues != null && i < defaultValues.Length ? defaultValues[i] : (float?)null;
					TooltipHandler.TipRegion(colRect, TooltipWithDefault(tooltips[i], def, format));
				}
				Widgets.Label(colRect.TopPart(0.6f), $"<b>{labels[i]}</b>: {FormatValue(values[i], format).Colorize(Color.cyan)}");
				values[i] = Widgets.HorizontalSlider(colRect.BottomPart(0.4f), values[i], minMax.x, minMax.y, false, null, null, null, step);
			}
		}

		private static bool DrawCollapsibleHeader(Listing_Standard l, string label, ref bool expanded, Color? color = null, string tip = null)
		{
			l.Gap(12f);
			Rect r = l.GetRect(26f);
			Widgets.DrawHighlightIfMouseover(r);
			if (!tip.NullOrEmpty()) TooltipHandler.TipRegion(r, tip);
			if (Widgets.ButtonInvisible(r)) expanded = !expanded;
			Color c = color ?? Color.white;
			string arrow = expanded ? "▼" : "▶";
			Widgets.Label(r, $"<b><color=#{ColorUtility.ToHtmlStringRGBA(c)}>{arrow}  {label}</color></b>");
			if (expanded) { l.GapLine(6f); l.Gap(4f); }
			else l.Gap(4f);
			return expanded;
		}

		private static void DrawMenuTopBar(Listing_Standard l, string resetLabel, Action onReset, Action expandAll, Action collapseAll)
		{
			if (onReset == null) return;
			l.Gap(4f);
			Rect row = l.GetRect(30f);
			float sideBtnW = 110f;
			float rightPairW = sideBtnW * 2f + 8f;
			float resetW = Mathf.Min(250f, row.width - rightPairW - 24f);
			if (resetW < 120f) resetW = Mathf.Max(100f, row.width * 0.35f);
			Rect resetRect = new Rect(row.x, row.y, resetW, row.height);
			Rect collapseRect = new Rect(row.xMax - sideBtnW, row.y, sideBtnW, row.height);
			Rect expandRect = new Rect(collapseRect.x - 8f - sideBtnW, row.y, sideBtnW, row.height);
			Color prev = GUI.color;
			GUI.color = new Color(1f, 0.5f, 0.5f);
			if (Widgets.ButtonText(resetRect, resetLabel)) onReset();
			GUI.color = prev;
			if (expandAll != null && Widgets.ButtonText(expandRect, "Expand all")) expandAll();
			if (collapseAll != null && Widgets.ButtonText(collapseRect, "Collapse all")) collapseAll();
			l.Gap(6f);
		}

		private static void DrawCheckbox(Listing_Standard l, string label, ref bool value, string tooltip = null, bool? defaultValue = null)
		{
			l.Gap(2f);
			Rect r = l.GetRect(24f);
			if (!tooltip.NullOrEmpty()) TooltipHandler.TipRegion(r, TooltipWithDefault(tooltip, defaultValue));
			Widgets.CheckboxLabeled(r, label, ref value);
		}

		private static string TooltipWithDefault(string tooltip, float? defaultValue, SliderFormat format = SliderFormat.Fixed1)
		{
			if (!defaultValue.HasValue) return tooltip;
			return AppendDefault(tooltip, FormatValue(defaultValue.Value, format));
		}

		private static string TooltipWithDefault(string tooltip, bool? defaultValue)
		{
			if (!defaultValue.HasValue) return tooltip;
			return AppendDefault(tooltip, defaultValue.Value ? "On" : "Off");
		}

		private static string AppendDefault(string tooltip, string def)
		{
			string line = $"Default: {def}";
			return tooltip.NullOrEmpty() ? line : tooltip + "\n" + line;
		}

		private static string FormatValue(float val, SliderFormat format) => format switch
		{
			SliderFormat.Fixed0 => val.ToString("F0"),
			SliderFormat.Fixed1 => val.ToString("F1"),
			SliderFormat.Fixed2 => val.ToString("F2"),
			SliderFormat.Multiplier => val.ToString("F2") + "x",
			SliderFormat.Percent => (val * 100f).ToString("F0") + " %",
			SliderFormat.PercentDecimal => (val * 100f).ToString("F1") + " %",
			_ => val.ToString()
		};
	}
}
