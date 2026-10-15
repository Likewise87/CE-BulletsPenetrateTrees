using Verse;
using UnityEngine;

namespace TSA.CE.BulletsPenetrateTrees
{
	public class TreePenetrationSettings : ModSettings
	{
		public const int BandCount = 9;

		public const float DefChanceToJustPassThrough = 0f;
		public const float DefHpDivisor = 25f;
		public const float DefFillMultiplier = 10f;
		public const float DefMinGrowthFactor = 0.30f;
		public const float DefMinArmor = 1.0f;
		public const float DefTreeDamageOnBlock = 1.5f;
		public const float DefTreeDamageOnPass = 1.0f;
		public const bool DefEnableDebug = false;

		public static readonly float[] DefPenetrationChances = { 0.00f, 0.10f, 0.25f, 0.50f, 1.00f, 1.00f, 1.00f, 1.00f, 1.00f };
		public static readonly float[] DefAPLossFractions = { 1.00f, 0.90f, 0.80f, 0.50f, 0.40f, 0.30f, 0.20f, 0.15f, 0.10f };

		public float[] PenetrationChances = new float[BandCount];
		public float[] StopChances = new float[BandCount]; // legacy; kept for save-compat (unused)
		public float[] APLossFractions = new float[BandCount];

		/// <summary>After CE decides a tree collide, chance to skip solid hit entirely (no damage, no AP loss).</summary>
		public float ChanceToJustPassThrough = DefChanceToJustPassThrough;

		public float HpDivisor = DefHpDivisor;
		public float FillMultiplier = DefFillMultiplier;
		public float MinGrowthFactor = DefMinGrowthFactor;
		public float MinArmor = DefMinArmor;

		public float TreeDamageOnBlock = DefTreeDamageOnBlock;
		public float TreeDamageOnPass = DefTreeDamageOnPass;

		public bool EnableDebug = DefEnableDebug;

		public TreePenetrationSettings() => ResetToDefaults();

		public override void ExposeData()
		{
			for (int i = 0; i < BandCount; i++)
			{
				Scribe_Values.Look(ref PenetrationChances[i], $"PenetrationChance{i}", PenetrationChances[i]);
				Scribe_Values.Look(ref StopChances[i], $"StopChance{i}", StopChances[i]);
				Scribe_Values.Look(ref APLossFractions[i], $"APLossFraction{i}", APLossFractions[i]);
			}

			Scribe_Values.Look(ref ChanceToJustPassThrough, "ChanceToJustPassThrough", DefChanceToJustPassThrough);

			Scribe_Values.Look(ref HpDivisor, "HpDivisor", DefHpDivisor);
			Scribe_Values.Look(ref FillMultiplier, "FillMultiplier", DefFillMultiplier);
			Scribe_Values.Look(ref MinGrowthFactor, "MinGrowthFactor", DefMinGrowthFactor);
			Scribe_Values.Look(ref MinArmor, "MinArmor", DefMinArmor);

			Scribe_Values.Look(ref TreeDamageOnBlock, "TreeDamageOnBlock", DefTreeDamageOnBlock);
			Scribe_Values.Look(ref TreeDamageOnPass, "TreeDamageOnPass", DefTreeDamageOnPass);

			Scribe_Values.Look(ref EnableDebug, "EnableDebug", DefEnableDebug);
		}

		public void ResetToDefaults()
		{
			for (int i = 0; i < BandCount; i++)
			{
				PenetrationChances[i] = Mathf.Clamp01(DefPenetrationChances[i]);
				StopChances[i] = 0f;
				APLossFractions[i] = Mathf.Clamp01(DefAPLossFractions[i]);
			}

			ChanceToJustPassThrough = DefChanceToJustPassThrough;

			HpDivisor = DefHpDivisor;
			FillMultiplier = DefFillMultiplier;
			MinGrowthFactor = DefMinGrowthFactor;
			MinArmor = DefMinArmor;

			TreeDamageOnBlock = DefTreeDamageOnBlock;
			TreeDamageOnPass = DefTreeDamageOnPass;

			EnableDebug = DefEnableDebug;
		}

		public static string BandLabelShort(int index) => index switch
		{
			0 => "Sharp Pen is 0–24% of tree armor",
			1 => "Sharp Pen is 25–49% of tree armor",
			2 => "Sharp Pen is 50–74% of tree armor",
			3 => "Sharp Pen is 75–99% of tree armor",
			4 => "Sharp Pen is 100–124% of tree armor",
			5 => "Sharp Pen is 125–149% of tree armor",
			6 => "Sharp Pen is 150–174% of tree armor",
			7 => "Sharp Pen is 175–199% of tree armor",
			8 => "Sharp Pen is 200%+ of tree armor",
			_ => "?"
		};
	}
}
