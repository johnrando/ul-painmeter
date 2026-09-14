using UnityEngine;

namespace PainMeter
{
	/// <summary>
	/// WhackLash's focus meter for one entity, read through <see cref="WhackLashLink"/> and turned
	/// into what the widget draws under the pain bar. <c>default</c> is "nothing to show": no
	/// WhackLash, or a meter at zero.
	///
	/// The meter builds per hit the player lands (1 melee, 0.5 archery, 0.25 gun by WhackLash's
	/// defaults), drains at a fixed rate and holds at most <see cref="Cap"/> points. Once it has
	/// reached <see cref="Break"/> WhackLash pins the game's pain meter just under 1, so the zombie
	/// cannot attack through hits - that is the bar's locked tint. Every point also buys a damage
	/// bonus, which is the label.
	/// </summary>
	internal readonly struct FocusState
	{
		/// <summary>Never more pips than there is room to tell apart.</summary>
		internal const int MaxPips = 10;

		internal readonly float Points;

		/// <summary>Points at which WhackLash locks the pain meter; below zero when that is switched off.</summary>
		internal readonly float Break;

		internal readonly float Cap;

		/// <summary>Damage bonus on the next hit, percent.</summary>
		internal readonly float BonusPercent;

		internal FocusState(float _points, float _break, float _cap, float _bonusPercent)
		{
			Points = _points > 0f ? _points : 0f;
			Break = _break;
			Cap = _cap > 0f ? _cap : 1f;
			BonusPercent = _bonusPercent > 0f ? _bonusPercent : 0f;
		}

		/// <summary>
		/// WhackLash is holding this entity's pain meter down. A zero meter is never broken: the
		/// <c>default</c> state (no WhackLash, focus off, nothing built) has Break and Points both
		/// at 0, and 0 >= 0 must not read as locked or every bar would tint <see cref="Settings.ColourLocked"/>.
		/// </summary>
		internal bool Broken => Points > 0f && Break >= 0f && Points >= Break;

		/// <summary>The meter reads zero: nothing to draw.</summary>
		internal bool IsZero => Points <= 0f;

		/// <summary>One pip per point of the cap, so the row counts hits the way the meter does.</summary>
		internal int PipCount => Mathf.Clamp(Mathf.CeilToInt(Cap - 0.0001f), 1, MaxPips);

		/// <summary>The pip that carries the break mark, or -1 when there is none to mark.</summary>
		internal int BreakPip
		{
			get
			{
				if (Break < 0f)
				{
					return -1;
				}
				if (Break <= 0f)
				{
					return 0;
				}
				return Mathf.Min(Mathf.CeilToInt(Break - 0.0001f) - 1, PipCount - 1);
			}
		}

		/// <summary>0 to 1: how much of pip <paramref name="_index"/> is lit.</summary>
		internal float PipFill(int _index)
		{
			return Mathf.Clamp01(Points - _index);
		}
	}
}
