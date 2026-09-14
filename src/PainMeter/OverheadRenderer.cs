using System.Collections.Generic;
using UnityEngine;

namespace PainMeter
{
	/// <summary>
	/// The overhead bars. Runs after <c>XUiC_OnScreenIcons.Update</c> every frame - the window
	/// the game's own quest markers and nav icons are drawn in - and parents the widgets under
	/// that window, so they scale, layer and hide with the rest of the HUD. Positions go through
	/// the same world-to-screen-to-XUi path the icons use.
	/// </summary>
	internal static class OverheadRenderer
	{
		/// <summary>Icons draw at 300 and up; these sit above them.</summary>
		private const int BaseDepth = 400;

		/// <summary>Whether the icons window is being drawn, as reported by <c>pm info</c>.</summary>
		internal static string WindowStatus = "not seen yet";

		private static readonly List<EntityAlive> targets = new List<EntityAlive>();

		/// <summary>Postfix on <c>XUiC_OnScreenIcons.Update(float)</c>.</summary>
		internal static void AfterUpdate(XUiC_OnScreenIcons __instance)
		{
			if (!Settings.Enabled || (!Settings.OverheadTarget && !Settings.OverheadAll))
			{
				WidgetPool.HideAll();
				return;
			}
			XUiView view = __instance.ViewComponent;
			XUi xui = __instance.xui;
			if (view == null || view.UiTransform == null || xui == null)
			{
				return;
			}
			WindowStatus = view.IsVisible ? "visible" : "hidden (Undead Legacy's on-screen icons option?)";
			WidgetPool.BindParent(view.UiTransform);

			EntityPlayerLocal player = xui.playerUI?.entityPlayer;
			if (player == null || player.IsDead() || player.finalCamera == null || player.cameraTransform == null)
			{
				WidgetPool.HideAll();
				return;
			}

			Counters.FramesRendered++;
			WidgetPool.BeginFrame();

			if (Settings.OverheadAll)
			{
				TargetFinder.Nearby(player.world, player, Settings.Range, targets);
			}
			else
			{
				targets.Clear();
				EntityAlive target = TargetFinder.CrosshairTarget(player);
				if (target != null)
				{
					targets.Add(target);
				}
			}

			Vector3 cameraPosition = player.cameraTransform.position;
			Vector3 cameraForward = player.cameraTransform.forward;
			for (int i = 0; i < targets.Count; i++)
			{
				Draw(targets[i], player, xui, cameraPosition, cameraForward);
			}
			targets.Clear();

			WidgetPool.EndFrame();
		}

		private static void Draw(EntityAlive _entity, EntityPlayerLocal _player, XUi _xui,
			Vector3 _cameraPosition, Vector3 _cameraForward)
		{
			PainState state = PainState.Read(_entity);
			Counters.TrackSeen(_entity.entityId, state);
			WhackLashLink.TryRead(_entity.entityId, out FocusState focus);
			if (Settings.HideZero && state.IsZero && focus.IsZero)
			{
				return;
			}

			Vector3 head = HeadPosition(_entity) + Vector3.up * Settings.HeadOffset;
			if (Vector3.Dot(head - _cameraPosition, _cameraForward) <= 0f)
			{
				return;
			}
			Vector3 screen = _player.finalCamera.WorldToScreenPoint(head);
			if (screen.z <= 0f || screen.x < 0f || screen.y < 0f
				|| screen.x > Screen.width || screen.y > Screen.height)
			{
				return;
			}
			Vector3 local = _xui.TranslateScreenVectorToXuiVector(screen);
			local.z = 0f;

			PainWidget widget = WidgetPool.Acquire(_entity.entityId, _xui, BaseDepth);
			// Pips ride above the bar, centred on it, one gap clear of its top edge.
			int pipSize = Settings.Height + 4;
			const float gap = 2f;
			widget.Apply(state, focus, Settings.Width, Settings.Height, Settings.Opacity,
				new Vector2(0f, Settings.Height * 0.5f + gap + pipSize * 0.5f), pipSize, _centreFocus: true);
			widget.SetLocalPosition(local);
			widget.Show();
		}

		/// <summary>
		/// The head bone in Unity space, which is what the camera works in. The fallback is the
		/// game's world-space head position shifted by the world origin.
		/// </summary>
		private static Vector3 HeadPosition(EntityAlive _entity)
		{
			Transform head = _entity.emodel != null ? _entity.emodel.GetHeadTransform() : null;
			if (head != null)
			{
				return head.position;
			}
			return _entity.getHeadPosition() - Origin.position;
		}

		/// <summary>Postfix on <c>XUiC_OnScreenIcons.Cleanup</c>: the window is going away.</summary>
		internal static void AfterCleanup()
		{
			WidgetPool.DestroyAll();
			WindowStatus = "not seen yet";
		}

		/// <summary>Postfix on <c>XUiC_OnScreenIcons.OnClose</c>.</summary>
		internal static void AfterClose()
		{
			WidgetPool.HideAll();
		}
	}
}
