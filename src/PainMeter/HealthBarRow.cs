using HarmonyLib;
using UnityEngine;

namespace PainMeter
{
	/// <summary>
	/// The row under Undead Legacy's target health bar. One widget, parented under UL's
	/// controller rect and sized to its bar (456 wide, centred at x 249 inside a 476 by 30 rect
	/// - see UL's windows.xml), shown for exactly the target UL is showing. The one place that
	/// knows how UL's bar is put together.
	/// </summary>
	internal static class HealthBarRow
	{
		/// <summary>Matches UL's bar content width.</summary>
		private const int Width = 456;

		private const int Height = 8;

		/// <summary>UL's bar centre is at x 249; the bar rect is 30 tall, so the row sits just under it.</summary>
		private static readonly Vector3 RowPosition = new Vector3(249f, -36f, 0f);

		/// <summary>UL's own sprites use 2 and 3.</summary>
		private const int Depth = 5;

		private static AccessTools.FieldRef<XUiC_ULM_TargetHealthBar, EntityAlive> targetField;

		private static XUiC_ULM_TargetHealthBar owner;

		private static PainWidget row;

		/// <summary>Binds the private <c>target</c> field. False if UL has moved it.</summary>
		internal static bool Resolve()
		{
			try
			{
				targetField = AccessTools.FieldRefAccess<XUiC_ULM_TargetHealthBar, EntityAlive>("target");
			}
			catch
			{
				targetField = null;
			}
			return targetField != null;
		}

		/// <summary>Postfix on <c>XUiC_ULM_TargetHealthBar.Init</c>.</summary>
		internal static void AfterInit(XUiC_ULM_TargetHealthBar __instance)
		{
			Build(__instance);
		}

		private static void Build(XUiC_ULM_TargetHealthBar _instance)
		{
			if (row != null)
			{
				row.Destroy();
				row = null;
			}
			owner = _instance;
			XUiView view = _instance.ViewComponent;
			if (view == null || view.UiTransform == null || _instance.xui == null)
			{
				return;
			}
			row = PainWidget.Create(view.UiTransform, _instance.xui, Depth);
			row.SetLocalPosition(RowPosition);
			row.Hide();
		}

		/// <summary>Postfix on <c>XUiC_ULM_TargetHealthBar.Update</c>.</summary>
		internal static void AfterUpdate(XUiC_ULM_TargetHealthBar __instance)
		{
			if (owner != __instance || row == null || !row.Alive)
			{
				Build(__instance);
				if (row == null)
				{
					return;
				}
			}
			Counters.BarRowUpdates++;

			if (!Settings.Enabled || !Settings.HealthBarRow || !ULM_Settings.ShowTargetHealth)
			{
				HideRow("hidden");
				return;
			}
			XUiView view = __instance.ViewComponent;
			if (view == null || !view.IsVisible)
			{
				HideRow("hidden");
				return;
			}
			EntityPlayerLocal player = __instance.xui?.playerUI?.entityPlayer;
			if (player == null || player.AttachedToEntity is EntityVehicle)
			{
				HideRow("hidden");
				return;
			}
			EntityAlive target = targetField(__instance);
			if (!TargetFinder.Qualifies(target))
			{
				HideRow("hidden");
				return;
			}

			PainState state = PainState.Read(target);
			Counters.TrackSeen(target.entityId, state);
			if (Settings.HideZero && state.IsZero)
			{
				HideRow("hidden - " + Name(target) + " reads zero");
				return;
			}
			row.Apply(state, Width, Height, Settings.Opacity);
			row.Show();
			Counters.BarRowShowing = "showing " + Name(target) + " at " + state.Pain.ToString("0.00");
		}

		private static void HideRow(string _why)
		{
			row.Hide();
			Counters.BarRowShowing = _why;
		}

		private static string Name(EntityAlive _entity)
		{
			EntityClass entityClass = EntityClass.list[_entity.entityClass];
			return entityClass != null ? entityClass.entityClassName : "entity " + _entity.entityId;
		}
	}
}
