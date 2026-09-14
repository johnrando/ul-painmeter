using UnityEngine;

namespace PainMeter
{
	/// <summary>How the "time until the meter drops back below 1" indicator is drawn.</summary>
	internal enum TimerStyle
	{
		Off,

		/// <summary>A thin bar under the meter that drains as the seconds run out.</summary>
		Bar,

		/// <summary>One pip per second remaining.</summary>
		Pips
	}

	/// <summary>What range the main bar spans.</summary>
	internal enum MeterScale
	{
		/// <summary>0 to 1: full is the point at which hits stop staggering.</summary>
		Threshold,

		/// <summary>0 to 3, the whole number, with a tick where 1 falls.</summary>
		Full,

		/// <summary>One segment per pain hit of the entity's class, so the bar counts hits.</summary>
		Hits
	}

	/// <summary>
	/// Runtime knobs, all switchable from the <c>pm</c> console command. The values here are the
	/// built-in defaults; <see cref="Config"/> reads the player's file over them at startup and
	/// writes it back whenever a command changes one. Every one of these is a display preference
	/// for this client: nothing here touches the game state.
	/// </summary>
	internal static class Settings
	{
		/// <summary>Master switch. When off, both renderers return immediately.</summary>
		internal static bool Enabled = true;

		/// <summary>A row under Undead Legacy's target health bar. Needs UL. See <see cref="HealthBarRow"/>.</summary>
		internal static bool HealthBarRow = true;

		/// <summary>A bar over the head of the zombie under the crosshair. See <see cref="OverheadRenderer"/>.</summary>
		internal static bool OverheadTarget;

		/// <summary>Bars over every zombie within <see cref="Range"/>. Includes the crosshair target.</summary>
		internal static bool OverheadAll;

		/// <summary>Metres, for <see cref="OverheadAll"/>.</summary>
		internal static float Range = 15f;

		/// <summary>Skip a meter that reads zero with no stagger lockout running, so an idle
		/// zombie carries no bar.</summary>
		internal static bool HideZero = true;

		/// <summary>Flash the bar while the 0.5 s stagger lockout runs. See <see cref="PainState.Staggered"/>.</summary>
		internal static bool Flash = true;

		/// <summary>What the main bar spans. See <see cref="MeterScale"/>.</summary>
		internal static MeterScale Scale = MeterScale.Threshold;

		/// <summary>How the decay-to-threshold time is shown once the meter is full.</summary>
		internal static TimerStyle Timer = TimerStyle.Bar;

		/// <summary>Pip count for <see cref="TimerStyle.Pips"/>, one per second. 10 covers the cap.</summary>
		internal static int TimerPips = 10;

		/// <summary>Overhead bar size in XUi pixels; scales with the game's UI scale option.</summary>
		internal static int Width = 60;

		internal static int Height = 6;

		/// <summary>Metres above the head bone the overhead bar floats.</summary>
		internal static float HeadOffset = 0.25f;

		/// <summary>0 to 1.</summary>
		internal static float Opacity = 0.85f;

		/// <summary>Fill tint at an empty meter.</summary>
		internal static Color32 ColourLow = new Color32(60, 200, 60, 255);

		/// <summary>Fill tint at a full meter. In between is a straight blend.</summary>
		internal static Color32 ColourHigh = new Color32(220, 50, 50, 255);

		/// <summary>Also meter hostile animals (dogs, vultures, bears). Off: zombies and bandits only.</summary>
		internal static bool Animals;

		/// <summary>
		/// With WhackLash installed, draw its focus meter as a row of pips under the bar, tint the
		/// bar <see cref="ColourLocked"/> while it holds the pain meter down, and print the damage
		/// bonus beside it. Does nothing without WhackLash. See <see cref="WhackLashLink"/>.
		/// </summary>
		internal static bool Focus = true;

		/// <summary>Where the focus pips start on the health-bar row: pixels right of the bar's right
		/// end and above the strip's centreline. +12 lands them on UL's bar centre; 22 clears the
		/// bar texture's edge. Overhead bars ignore these and trail their pips on the centreline.</summary>
		internal static int FocusX = 22;

		internal static int FocusY = 12;

		/// <summary>Fill tint while WhackLash has the zombie broken: the pain meter is pinned, so the
		/// low/high blend would sit at full and mean nothing.</summary>
		internal static Color32 ColourLocked = new Color32(70, 130, 220, 255);

		/// <summary>Focus pip tint by meter level: every lit pip shares one colour that blends from
		/// <see cref="PipLow"/> through <see cref="PipMid"/> to <see cref="PipHigh"/> as the meter fills
		/// towards its cap.</summary>
		internal static Color32 PipLow = new Color32(255, 215, 0, 255);

		internal static Color32 PipMid = new Color32(255, 130, 0, 255);

		internal static Color32 PipHigh = new Color32(225, 40, 40, 255);

		/// <summary>The decay timer bar and its pips.</summary>
		internal static Color32 ColourTimer = new Color32(255, 255, 255, 255);

		/// <summary>The stagger-lockout pulse over the bar.</summary>
		internal static Color32 ColourFlash = new Color32(255, 255, 255, 255);

		/// <summary>The break-point frame and the bonus label before the zombie is broken.</summary>
		internal static Color32 ColourMark = new Color32(255, 255, 255, 255);
	}
}
