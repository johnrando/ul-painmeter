using System.Collections.Generic;
using UnityEngine;

namespace PainMeter
{
	/// <summary>
	/// One meter: a background, a fill that grows from the left, a white flash over it, and the
	/// decay timer underneath. <see cref="Settings.Scale"/> picks what the fill spans: 0 to 1,
	/// 0 to 3 with a tick at 1, or one segment per pain hit of the entity's class. With WhackLash
	/// installed a run of square pips starting at the focus anchor (the caller's choice: beside the
	/// bar) counts its focus meter, the fill takes the locked tint while it holds the pain meter
	/// down, and a label after the last pip prints the damage bonus. Built from NGUI sprites in code the way the game's own on-screen icons are (<c>XUiC_OnScreenIcons.OnScreenIcon.CreateObjects</c>): a plain white atlas
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

		/// <summary>The 1.0 mark on the <see cref="MeterScale.Full"/> bar.</summary>
		private UISprite tick;

		private readonly List<UISprite> pips = new List<UISprite>();

		/// <summary>Per-hit segments for <see cref="MeterScale.Hits"/>: a dark back and a fill each.</summary>
		private readonly List<UISprite> segmentBacks = new List<UISprite>();

		private readonly List<UISprite> segmentFills = new List<UISprite>();

		/// <summary>How many segments the current layout was computed for; 0 until the first.</summary>
		private int segmentsLaidOut;

		/// <summary>Never more segments than there is room to tell apart.</summary>
		private const int MaxSegments = 20;

		private XUi xui;

		private int depth;

		private int width = -1;

		private int height = -1;

		private int timerHeight;

		/// <summary>How many pips the current layout was computed for; 0 until the first.</summary>
		private int pipsLaidOut;

		/// <summary>WhackLash's focus meter, one pip per point: a dark back and a fill each.</summary>
		private readonly List<UISprite> focusBacks = new List<UISprite>();

		private readonly List<UISprite> focusFills = new List<UISprite>();

		/// <summary>A frame one pixel proud of the pip at which WhackLash locks the pain meter.</summary>
		private UISprite focusMark;

		/// <summary>"+15%": the damage bonus WhackLash gives the next hit.</summary>
		private UILabel bonusLabel;

		private int focusLaidOut;

		/// <summary>Left edge and vertical centre of the first focus pip, relative to the bar's centre.</summary>
		private Vector2 focusAnchor;

		/// <summary>Side of one square pip, set by the caller.</summary>
		private int pipSize;

		private const int FocusGap = 3;

		/// <summary>Centre the pip run on the bar (overhead) rather than start it at the anchor.</summary>
		private bool centreFocus;

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
			widget.tick = widget.MakeSprite("Tick", _depth + 3);
			widget.focusMark = widget.MakeSprite("FocusMark", _depth);
			widget.bonusLabel = widget.MakeLabel("Bonus", _depth + 3);
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

		/// <summary>Set up the way <c>XUiC_OnScreenIcons.OnScreenIcon.CreateObjects</c> builds its label.</summary>
		private UILabel MakeLabel(string _name, int _depth)
		{
			GameObject go = new GameObject(_name);
			go.layer = Layer;
			go.transform.SetParent(root.transform, worldPositionStays: false);
			UILabel label = go.AddComponent<UILabel>();
			label.font = xui.GetUIFontByName("ReferenceFont");
			label.pivot = UIWidget.Pivot.Left;
			label.overflowMethod = UILabel.Overflow.ResizeFreely;
			label.alignment = NGUIText.Alignment.Left;
			label.effectStyle = UILabel.Effect.Outline;
			label.effectColor = new Color32(0, 0, 0, byte.MaxValue);
			label.effectDistance = new Vector2(1f, 1f);
			label.color = Color.white;
			label.text = string.Empty;
			label.depth = _depth;
			label.enabled = false;
			return label;
		}

		/// <summary>Draws one reading. Sizes are re-applied only when they change.
		/// <paramref name="_focus"/> is <c>default</c> without WhackLash; <paramref name="_focusAnchor"/>
		/// is where its pips start, relative to the bar's centre, and <paramref name="_pipSize"/> their side.
		/// With <paramref name="_centreFocus"/> the anchor's x is ignored and the run is centred over
		/// the bar's own centre instead.</summary>
		internal void Apply(in PainState _state, in FocusState _focus, int _width, int _height, float _opacity,
			Vector2 _focusAnchor, int _pipSize, bool _centreFocus)
		{
			if (!Alive)
			{
				return;
			}
			if (_width != width || _height != height)
			{
				Resize(_width, _height);
			}
			if (_focusAnchor != focusAnchor || _pipSize != pipSize || _centreFocus != centreFocus)
			{
				focusAnchor = _focusAnchor;
				pipSize = _pipSize;
				centreFocus = _centreFocus;
				if (focusLaidOut > 0)
				{
					LayoutFocus(focusLaidOut);
				}
			}

			MeterScale scale = Settings.Scale;
			int segments = scale == MeterScale.Hits ? Mathf.Min(_state.HitsTo(PainState.MaxPain), MaxSegments) : 0;
			if (segments <= 0)
			{
				// Hits scale on a class that never takes pain: nothing to count, draw the plain bar.
				scale = MeterScale.Threshold;
			}

			// Broken: WhackLash pins the pain number just under 1, so the blend would sit at the
			// high colour and the flash would never stop. One tint says it all.
			bool locked = _focus.Broken;

			bool plain = scale != MeterScale.Hits;
			background.enabled = plain;
			fill.enabled = plain;
			if (plain)
			{
				background.color = new Color(0f, 0f, 0f, 1f);
				background.alpha = _opacity * 0.6f;
				fill.fillAmount = scale == MeterScale.Full
					? Mathf.Clamp01(_state.Pain / PainState.MaxPain) : _state.Fill;
				fill.color = locked ? (Color)Settings.ColourLocked : _state.Colour(1f);
				fill.alpha = _opacity;
			}

			bool showTick = scale == MeterScale.Full;
			tick.enabled = showTick;
			if (showTick)
			{
				tick.color = Settings.ColourMark;
				tick.alpha = _opacity * 0.9f;
			}

			ApplySegments(_state, segments, locked, _opacity);

			bool flashing = Settings.Flash && _state.Staggered && !locked;
			flash.enabled = flashing;
			if (flashing)
			{
				flash.color = Settings.ColourFlash;
				flash.alpha = _opacity * (0.35f + 0.35f * Mathf.PingPong(Time.unscaledTime * 6f, 1f));
			}

			ApplyTimer(_state, _opacity);
			ApplyFocus(_focus, _opacity);
		}

		/// <summary>
		/// The hits scale: segment i spans pain i*PerHit to (i+1)*PerHit, the last one cut at the
		/// cap. A segment whose completion puts the entity at or past 1 is in the high colour, so
		/// the first high segment reads as "one more hit and it stops staggering".
		/// </summary>
		private void ApplySegments(in PainState _state, int _count, bool _locked, float _opacity)
		{
			if (_count > 0 && _count != segmentsLaidOut)
			{
				LayoutSegments(_count);
			}
			int firstHigh = _state.HitsTo(PainState.Threshold) - 1;
			Color low = Settings.ColourLow;
			Color high = Settings.ColourHigh;
			for (int i = 0; i < segmentBacks.Count; i++)
			{
				bool on = i < _count;
				segmentBacks[i].enabled = on;
				segmentFills[i].enabled = on;
				if (!on)
				{
					continue;
				}
				float start = i * _state.PerHit;
				float end = Mathf.Min(start + _state.PerHit, PainState.MaxPain);
				segmentBacks[i].color = new Color(0f, 0f, 0f, 1f);
				segmentBacks[i].alpha = _opacity * 0.6f;
				segmentFills[i].fillAmount = Mathf.Clamp01((_state.Pain - start) / (end - start));
				segmentFills[i].color = _locked ? (Color)Settings.ColourLocked : i >= firstHigh ? high : low;
				segmentFills[i].alpha = _opacity;
			}
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
				timerBar.color = Settings.ColourTimer;
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
					pips[i].color = Settings.ColourTimer;
					pips[i].alpha = _opacity;
				}
			}
		}

		/// <summary>
		/// The focus row: pip i lights as the meter passes i+1 points, the last lit one partially so
		/// the drain is visible, and the pip at the break point wears a white frame that turns the
		/// locked colour once the zombie is broken. The label prints the damage bonus the next hit
		/// gets, rounded to a whole percent.
		/// </summary>
		private void ApplyFocus(in FocusState _focus, float _opacity)
		{
			int count = _focus.IsZero ? 0 : _focus.PipCount;
			if (count > 0 && count != focusLaidOut)
			{
				LayoutFocus(count);
			}
			// Every lit pip shares one build-up tint that follows the meter's level towards its cap;
			// once broken every pip is the locked colour, the same signal as the bar.
			Color pipTint = _focus.Broken ? (Color)Settings.ColourLocked : BuildUpColour(_focus.Points / _focus.Cap);
			for (int i = 0; i < focusBacks.Count; i++)
			{
				bool on = i < count;
				focusBacks[i].enabled = on;
				focusFills[i].enabled = on;
				if (!on)
				{
					continue;
				}
				focusBacks[i].color = new Color(0f, 0f, 0f, 1f);
				focusBacks[i].alpha = _opacity * 0.6f;
				focusFills[i].fillAmount = _focus.PipFill(i);
				focusFills[i].color = pipTint;
				focusFills[i].alpha = _opacity;
			}
			Color lit = _focus.Broken ? (Color)Settings.ColourLocked : (Color)Settings.ColourMark;

			int breakPip = count > 0 ? _focus.BreakPip : -1;
			focusMark.enabled = breakPip >= 0;
			if (breakPip >= 0)
			{
				focusMark.transform.localPosition = FocusPipOrigin(breakPip) + new Vector3(-1f, 0f, 0f);
				focusMark.color = lit;
				focusMark.alpha = _opacity * 0.9f;
			}

			int percent = Mathf.FloorToInt(_focus.BonusPercent + 0.5f);
			bool showBonus = count > 0 && percent > 0;
			bonusLabel.enabled = showBonus;
			if (showBonus)
			{
				bonusLabel.text = "+" + percent + "%";
				bonusLabel.color = lit;
				bonusLabel.alpha = _opacity;
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
			tick.SetDimensions(1, _height + 2);
			tick.transform.localPosition = origin
				+ new Vector3(Mathf.Round(_width * PainState.Threshold / PainState.MaxPain) - 0.5f, 0f, 0f);
			if (pipsLaidOut > 0)
			{
				LayoutPips(pipsLaidOut);
			}
			if (segmentsLaidOut > 0)
			{
				LayoutSegments(segmentsLaidOut);
			}
			if (focusLaidOut > 0)
			{
				LayoutFocus(focusLaidOut);
			}
		}

		private Vector3 TimerOrigin()
		{
			return new Vector3(-width * 0.5f, -(height * 0.5f + timerHeight * 0.5f + 1f), 0f);
		}

		/// <summary><paramref name="_level"/> 0 to 1 of the cap: <see cref="Settings.PipLow"/> at
		/// empty, <see cref="Settings.PipMid"/> midway, <see cref="Settings.PipHigh"/> at the cap.</summary>
		private static Color BuildUpColour(float _level)
		{
			float t = Mathf.Clamp01(_level);
			return t < 0.5f
				? Color.Lerp(Settings.PipLow, Settings.PipMid, t * 2f)
				: Color.Lerp(Settings.PipMid, Settings.PipHigh, (t - 0.5f) * 2f);
		}

		private int FocusPipSize()
		{
			return Mathf.Max(1, pipSize);
		}

		/// <summary>Left edge of pip <paramref name="_index"/>; the pivot is Left, so y is its centre.
		/// Uses the laid-out count when centring, so call after <see cref="LayoutFocus"/> has set it.</summary>
		private Vector3 FocusPipOrigin(int _index)
		{
			int step = FocusPipSize() + FocusGap;
			float left = centreFocus
				? -(focusLaidOut * step - FocusGap) * 0.5f
				: focusAnchor.x;
			return new Vector3(left + _index * step, focusAnchor.y, 0f);
		}

		/// <summary>Lays <paramref name="_count"/> square focus pips rightward from the anchor, the
		/// bonus label after the last one.</summary>
		private void LayoutFocus(int _count)
		{
			while (focusBacks.Count < _count)
			{
				focusBacks.Add(MakeSprite("FocusBack" + focusBacks.Count, depth + 1));
				UISprite focusFill = MakeSprite("FocusFill" + focusFills.Count, depth + 2);
				focusFill.type = UIBasicSprite.Type.Filled;
				focusFill.fillDirection = UIBasicSprite.FillDirection.Horizontal;
				focusFills.Add(focusFill);
			}
			focusLaidOut = _count;
			int pip = FocusPipSize();
			for (int i = 0; i < focusBacks.Count; i++)
			{
				Vector3 at = FocusPipOrigin(i);
				focusBacks[i].SetDimensions(pip, pip);
				focusBacks[i].transform.localPosition = at;
				focusFills[i].SetDimensions(pip, pip);
				focusFills[i].transform.localPosition = at;
			}
			focusMark.SetDimensions(pip + 2, pip + 2);
			bonusLabel.fontSize = Mathf.Clamp(pip + 2, 10, 16);
			bonusLabel.transform.localPosition = FocusPipOrigin(_count) + new Vector3(1f, 0f, 0f);
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
			}
		}

		/// <summary>Lays <paramref name="_count"/> segments across the bar's width with a one-pixel gap.</summary>
		private void LayoutSegments(int _count)
		{
			while (segmentBacks.Count < _count)
			{
				segmentBacks.Add(MakeSprite("SegmentBack" + segmentBacks.Count, depth));
				UISprite segmentFill = MakeSprite("SegmentFill" + segmentFills.Count, depth + 1);
				segmentFill.type = UIBasicSprite.Type.Filled;
				segmentFill.fillDirection = UIBasicSprite.FillDirection.Horizontal;
				segmentFills.Add(segmentFill);
			}
			segmentsLaidOut = _count;
			int gap = 1;
			int segmentWidth = Mathf.Max(1, (width - gap * (_count - 1)) / _count);
			Vector3 origin = new Vector3(-width * 0.5f, 0f, 0f);
			for (int i = 0; i < segmentBacks.Count; i++)
			{
				Vector3 at = origin + new Vector3(i * (segmentWidth + gap), 0f, 0f);
				segmentBacks[i].SetDimensions(segmentWidth, height);
				segmentBacks[i].transform.localPosition = at;
				segmentFills[i].SetDimensions(segmentWidth, height);
				segmentFills[i].transform.localPosition = at;
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
			segmentBacks.Clear();
			segmentFills.Clear();
			focusBacks.Clear();
			focusFills.Clear();
		}
	}
}
