using HarmonyLib;

namespace PainMeter
{
	/// <summary>
	/// The Undead Legacy patches: the row under UL's target health bar. Only referenced from
	/// <see cref="Patches.Apply"/> after UL is confirmed present - this is one of the two classes
	/// that mention UL types, so it must not be JIT-compiled without them.
	/// </summary>
	internal static class UlPatches
	{
		internal static void Install(Harmony _harmony)
		{
			if (!HealthBarRow.Resolve())
			{
				Patches.HealthBarInitStatus = "NOT APPLIED - XUiC_ULM_TargetHealthBar.target not found";
				Patches.HealthBarUpdateStatus = Patches.HealthBarInitStatus;
				Log.Error(Patches.LogPrefix + "Health-bar row NOT applied: UL's target health bar no "
					+ "longer keeps its target where this mod expects, so 'pm bar' will do nothing.");
				return;
			}

			Patches.HealthBarInitStatus = Patches.Postfix(_harmony, typeof(XUiC_ULM_TargetHealthBar), "Init", null,
				typeof(HealthBarRow), nameof(HealthBarRow.AfterInit),
				"a pain row is built under UL's target health bar");
			Patches.HealthBarUpdateStatus = Patches.Postfix(_harmony, typeof(XUiC_ULM_TargetHealthBar), "Update", null,
				typeof(HealthBarRow), nameof(HealthBarRow.AfterUpdate),
				"the pain row follows the health bar's target");
		}
	}
}
