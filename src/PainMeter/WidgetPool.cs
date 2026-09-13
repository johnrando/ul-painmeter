using System.Collections.Generic;
using UnityEngine;

namespace PainMeter
{
	/// <summary>
	/// The overhead widgets, one per entity drawn this frame. Widgets are reused rather than
	/// rebuilt: an entity that leaves the screen hands its widget to the free list and the next
	/// arrival takes it. Everything is destroyed when the owning window is torn down.
	/// </summary>
	internal static class WidgetPool
	{
		private static readonly Dictionary<int, PainWidget> live = new Dictionary<int, PainWidget>();

		private static readonly Stack<PainWidget> free = new Stack<PainWidget>();

		private static readonly HashSet<int> touched = new HashSet<int>();

		private static readonly List<int> stale = new List<int>();

		private static Transform parent;

		/// <summary>Drops everything if the window this pool draws under has been replaced.</summary>
		internal static void BindParent(Transform _parent)
		{
			if (parent != _parent)
			{
				DestroyAll();
				parent = _parent;
			}
		}

		internal static void BeginFrame()
		{
			touched.Clear();
		}

		internal static PainWidget Acquire(int _entityId, XUi _xui, int _depth)
		{
			touched.Add(_entityId);
			if (live.TryGetValue(_entityId, out PainWidget widget) && widget.Alive)
			{
				return widget;
			}
			while (free.Count > 0)
			{
				widget = free.Pop();
				if (widget.Alive)
				{
					live[_entityId] = widget;
					return widget;
				}
			}
			widget = PainWidget.Create(parent, _xui, _depth);
			live[_entityId] = widget;
			if (live.Count > Counters.PeakLive)
			{
				Counters.PeakLive = live.Count;
			}
			return widget;
		}

		/// <summary>Hides and frees the widgets of entities not drawn this frame.</summary>
		internal static void EndFrame()
		{
			stale.Clear();
			foreach (KeyValuePair<int, PainWidget> entry in live)
			{
				if (!touched.Contains(entry.Key))
				{
					stale.Add(entry.Key);
				}
			}
			for (int i = 0; i < stale.Count; i++)
			{
				Release(stale[i]);
			}
			Counters.OverheadLive = live.Count;
		}

		private static void Release(int _entityId)
		{
			if (live.TryGetValue(_entityId, out PainWidget widget))
			{
				live.Remove(_entityId);
				widget.Hide();
				if (widget.Alive)
				{
					free.Push(widget);
				}
			}
		}

		internal static void HideAll()
		{
			if (live.Count == 0)
			{
				return;
			}
			stale.Clear();
			stale.AddRange(live.Keys);
			for (int i = 0; i < stale.Count; i++)
			{
				Release(stale[i]);
			}
			Counters.OverheadLive = 0;
		}

		internal static void DestroyAll()
		{
			foreach (PainWidget widget in live.Values)
			{
				widget.Destroy();
			}
			live.Clear();
			while (free.Count > 0)
			{
				free.Pop().Destroy();
			}
			touched.Clear();
			Counters.OverheadLive = 0;
			parent = null;
		}
	}
}
