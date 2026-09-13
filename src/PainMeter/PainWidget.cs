using System.Collections.Generic;
using UnityEngine;

namespace PainMeter
{
	/// <summary>
	/// One meter: a background, a fill that grows from the left, a white flash over it, and the
	/// decay timer underneath. Built from NGUI sprites in code the way the game's own on-screen
	/// icons are (<c>XUiC_OnScreenIcons.OnScreenIcon.CreateObjects</c>): a plain white atlas
	/// sprite tinted through its colour, parented under an existing XUi window so it is drawn and
	/// scaled with the rest of the HUD. The anchor is the bar's centre; XUi y runs upward.
	/// </summary>
	internal sealed class PainWidget
	{
		private const string Atlas = "UIAtlas";

		private const string WhiteSprite = "menu_empty";

		private const int Layer = 12;

		private GameObject root;

		private UISprite background;

		private UISprite fill;

		private UISprite flash;

		private UISprite timerBar;

		private readonly List<UISprite> pips = new List<UISprite>();

		private XUi xui;

		private int depth;

		private int width = -1;

		private int height = -1;

		private int timerHeight;

		/// <summary>How many pips the current layout was computed for; 0 until the first.</summary>
		private int pipsLaidOut;

		/// <summary>False once Unity has destroyed the objects underneath (a window torn down).</summary>
		internal bool Alive => root != null;

		internal static PainWidget Create(Transform _parent, XUi _xui, int _depth)
		{
			PainWidget widget = new PainWidget();
			widget.xui = _xui;
			widget.depth = _depth;
			widget.root = new GameObject("PainMeter");
			widget.root.layer = Layer;
			widget.root.transform.SetParent(_parent, worldPositionStays: false);
			widget.background = widget.MakeSprite("Background", _depth);
			widget.fill = widget.MakeSprite("Fill", _depth + 1);
			widget.fill.type = UIBasicSprite.Type.Filled;
			widget.fill.fillDirection = UIBasicSprite.FillDirection.Horizontal;
			widget.flash = widget.MakeSprite("Flash", _depth + 2);
			widget.timerBar = widget.MakeSprite("Timer", _depth + 1);
			widget.timerBar.type = UIBasicSprite.Type.Filled;
			widget.timerBar.fillDirection = UIBasicSprite.FillDirection.Horizontal;
			Counters.WidgetsCreated++;
			return widget;
		}

		private UISprite MakeSprite(string _name, int _depth)
		{
			GameObject go = new GameObject(_name);
			go.layer = Layer;
			go.transform.SetParent(root.transform, worldPositionStays: false);
			UISprite sprite = go.AddComponent<UISprite>();
			sprite.atlas = xui.GetAtlasByName(Atlas, WhiteSprite);
			sprite.spriteName = WhiteSprite;
			sprite.pivot = UIWidget.Pivot.Left;
			sprite.depth = _depth;
			sprite.color = Color.white;
			return sprite;
		}

		/// <summary>Draws one reading. Sizes are re-applied only when they change.</summary>
		internal void Apply(in PainState _state, int _width, int _height, float _opacity)
		{
			if (!Alive)
			{
				return;
			}
			if (_width != width || _height != height)
			{
				Resize(_width, _height);
			}

			background.color = new Color(0f, 0f, 0f, 1f);
			background.alpha = _opacity * 0.6f;

			fill.fillAmount = _state.Fill;
			Color colour = _state.Colour(1f);
			fill.color = colour;
			fill.alpha = _opacity;

			bool flashing = Settings.Flash && _state.Staggered;
			flash.enabled = flashing;
			if (flashing)
			{
				flash.alpha = _opacity * (0.35f + 0.35f * Mathf.PingPong(Time.unscaledTime * 6f, 1f));
			}

			ApplyTimer(_state, _opacity);
		}

		private void ApplyTimer(in PainState _state, float _opacity)
		{
			bool show = _state.OverThreshold && Settings.Timer != TimerStyle.Off;
			float fraction = Mathf.Clamp01(_state.SecondsUntilBelow / PainState.MaxSecondsOver);

			bool bar = show && Settings.Timer == TimerStyle.Bar;
			timerBar.enabled = bar;
			if (bar)
			{
				timerBar.fillAmount = fraction;
				timerBar.alpha = _opacity;
			}

			int wanted = show && Settings.Timer == TimerStyle.Pips ? Settings.TimerPips : 0;
			if (wanted > 0 && wanted != pipsLaidOut)
			{
				LayoutPips(wanted);
			}
			int lit = wanted > 0 ? Mathf.CeilToInt(_state.SecondsUntilBelow) : 0;
			for (int i = 0; i < pips.Count; i++)
			{
				bool on = i < wanted && i < lit;
				pips[i].enabled = on;
				if (on)
				{
					pips[i].alpha = _opacity;
				}
			}
		}

		private void Resize(int _width, int _height)
		{
			width = _width;
			height = _height;
			timerHeight = Mathf.Max(2, _height / 2);
			Vector3 origin = new Vector3(-_width * 0.5f, 0f, 0f);
			background.SetDimensions(_width, _height);
			background.transform.localPosition = origin;
			fill.SetDimensions(_width, _height);
			fill.transform.localPosition = origin;
			flash.SetDimensions(_width, _height);
			flash.transform.localPosition = origin;
			timerBar.SetDimensions(_width, timerHeight);
			timerBar.transform.localPosition = TimerOrigin();
			if (pipsLaidOut > 0)
			{
				LayoutPips(pipsLaidOut);
			}
		}

		private Vector3 TimerOrigin()
		{
			return new Vector3(-width * 0.5f, -(height * 0.5f + timerHeight * 0.5f + 1f), 0f);
		}

		/// <summary>Lays <paramref name="_count"/> pips across the bar's width with a one-pixel gap.</summary>
		private void LayoutPips(int _count)
		{
			while (pips.Count < _count)
			{
				pips.Add(MakeSprite("Pip" + pips.Count, depth + 1));
			}
			pipsLaidOut = _count;
			int gap = 1;
			int pipWidth = Mathf.Max(1, (width - gap * (_count - 1)) / _count);
			Vector3 origin = TimerOrigin();
			for (int i = 0; i < pips.Count; i++)
			{
				pips[i].SetDimensions(pipWidth, timerHeight);
				pips[i].transform.localPosition = origin + new Vector3(i * (pipWidth + gap), 0f, 0f);
				pips[i].color = Color.white;
			}
		}

		internal void SetLocalPosition(Vector3 _position)
		{
			if (Alive)
			{
				root.transform.localPosition = _position;
			}
		}

		internal void Show()
		{
			if (Alive && !root.activeSelf)
			{
				root.SetActive(true);
			}
		}

		internal void Hide()
		{
			if (Alive && root.activeSelf)
			{
				root.SetActive(false);
			}
		}

		internal void Destroy()
		{
			if (root != null)
			{
				Object.Destroy(root);
			}
			root = null;
			pips.Clear();
		}
	}
}
