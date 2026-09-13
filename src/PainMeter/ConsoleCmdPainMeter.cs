using System.Collections.Generic;
using UnityEngine;

namespace PainMeter
{
	/// <summary>
	/// <c>pm</c> (or <c>painmeter</c>). The bare command prints the settings block and changes
	/// nothing; every line of the block names the command that changes it, so it doubles as the
	/// menu. <c>pm info</c> adds the diagnostics and counters that answer "is this thing working".
	///
	/// Runs on the client, unlike the sibling mods' commands: every setting here is a display
	/// preference for the machine drawing the HUD, and the widgets live in that machine's XUi.
	/// Executed on the server, a client's <c>pm</c> would change nothing it could see.
	/// </summary>
	public class ConsoleCmdPainMeter : ConsoleCmdAbstract
	{
		public override bool IsExecuteOnClient => true;

		public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
		{
			string command = _params.Count > 0 ? _params[0].ToLower() : string.Empty;

			switch (command)
			{
			case "":
				OutputMenu("PainMeter is " + OnOff(Settings.Enabled));
				return;

			case "on":
			case "off":
				SetEnabled(command == "on");
				return;

			case "bar":
				Settings.HealthBarRow = !Settings.HealthBarRow;
				Config.Save();
				Output("Health-bar row " + OnOff(Settings.HealthBarRow) + ".");
				if (Settings.HealthBarRow && !UndeadLegacyInfo.Present)
				{
					Output("Saved, but Undead Legacy is not installed, so there is no target health "
						+ "bar to add a row to. 'pm target' and 'pm all' work without it.");
				}
				return;

			case "target":
				Settings.OverheadTarget = !Settings.OverheadTarget;
				Config.Save();
				Output("Overhead bar over the crosshair target " + OnOff(Settings.OverheadTarget)
					+ (Settings.OverheadTarget && Settings.OverheadAll
						? " - 'pm all' is on too, which already covers it." : "."));
				return;

			case "all":
				Settings.OverheadAll = !Settings.OverheadAll;
				Config.Save();
				Output("Overhead bars over every zombie within " + Config.Number(Settings.Range) + " m "
					+ OnOff(Settings.OverheadAll) + ".");
				return;

			case "range":
				SetRange(_params);
				return;

			case "hidezero":
				Settings.HideZero = !Settings.HideZero;
				Config.Save();
				Output(Settings.HideZero
					? "A meter that reads zero is now hidden."
					: "A meter that reads zero is now drawn empty.");
				return;

			case "flash":
				Settings.Flash = !Settings.Flash;
				Config.Save();
				Output("Stagger flash " + OnOff(Settings.Flash) + ".");
				return;

			case "timer":
				Settings.Timer = Settings.Timer == TimerStyle.Bar ? TimerStyle.Pips
					: Settings.Timer == TimerStyle.Pips ? TimerStyle.Off : TimerStyle.Bar;
				Config.Save();
				Output("Decay timer: " + TimerLine());
				return;

			case "pips":
				SetPips(_params);
				return;

			case "size":
				SetSize(_params);
				return;

			case "offset":
				SetOffset(_params);
				return;

			case "opacity":
				SetOpacity(_params);
				return;

			case "colour":
			case "color":
				SetColour(_params);
				return;

			case "animals":
				Settings.Animals = !Settings.Animals;
				Config.Save();
				Output(Settings.Animals
					? "Hostile animals are metered too."
					: "Only zombies and bandits are metered.");
				return;

			case "info":
				OutputInfo();
				return;

			case "reset":
				Counters.Reset();
				Output("Counters reset.");
				return;

			default:
				Output("Unknown option '" + _params[0]
					+ "'. Try: pm [on|off|bar|target|all|range|hidezero|flash|timer|pips|size|offset"
					+ "|opacity|colour|animals|info|reset]");
				return;
			}
		}

		private static void OutputMenu(string _header)
		{
			Output(_header);
			Switch("pm on|off", EnabledChoices(), "show how close a zombie is to attacking through hits");
			Switch("pm bar", OnOffChoices(Settings.HealthBarRow), "row under Undead Legacy's target health bar");
			Switch("pm target", OnOffChoices(Settings.OverheadTarget), "bar over the head of the zombie under the crosshair");
			Switch("pm all", OnOffChoices(Settings.OverheadAll), "bars over every zombie in range");
			Line("pm range {m}", RangeLine());
			Switch("pm hidezero", OnOffChoices(Settings.HideZero), "hide a meter that reads zero");
			Switch("pm flash", OnOffChoices(Settings.Flash), "flash while the 0.5 s stagger lockout runs");
			Switch("pm timer", TimerChoices(), "how long until the meter drops back below 1");
			Line("pm pips {n}", PipsLine());
			Line("pm size {w} {h}", SizeLine());
			Line("pm offset {m}", OffsetLine());
			Line("pm opacity {0-1}", Config.Number(Settings.Opacity));
			Line("pm colour {low} {high}", ColourLine());
			Switch("pm animals", OnOffChoices(Settings.Animals), "hostile animals too, not just zombies");
		}

		/// <summary>The header says whether anything moved: typing the state you were already in
		/// should not read like a change.</summary>
		private static void SetEnabled(bool _on)
		{
			bool changed = Settings.Enabled != _on;
			Settings.Enabled = _on;
			if (changed)
			{
				Config.Save();
			}
			OutputMenu("PainMeter is " + (changed ? "now " : "already ") + OnOff(_on));
		}

		private static string OnOff(bool _on)
		{
			return _on ? "ON" : "OFF";
		}

		/// <summary>The menu, with the read-only lines appended in the same column.</summary>
		private static void OutputInfo()
		{
			OutputMenu("PainMeter is " + OnOff(Settings.Enabled));
			Line("settings file", Config.Status);
			Line("Undead Legacy", UndeadLegacyInfo.Status);
			Line("overhead frame hook", Patches.OverheadUpdateStatus);
			Line("overhead cleanup hook", Patches.OverheadCleanupStatus);
			Line("health-bar row (init)", Patches.HealthBarInitStatus);
			Line("health-bar row (tick)", Patches.HealthBarUpdateStatus);
			Line("icons window", OverheadRenderer.WindowStatus);
			Line("frames rendered", Counters.FramesRendered + " (" + Counters.OverheadLive
				+ " overhead bars live now)");
			Line("widgets created", Counters.WidgetsCreated + " (peak " + Counters.PeakLive + " live)");
			Line("zombies seen in pain", Counters.SeenInPain.Count + " distinct, highest "
				+ Counters.HighestPain.ToString("0.00") + ", " + Counters.CrossedThreshold
				+ " crossed 1.0");
			Line("health-bar row", Counters.BarRowUpdates + " updates, currently " + Counters.BarRowShowing);

			if (Counters.FramesRendered == 0)
			{
				Output("Note: the overhead hook has not drawn a frame yet. It only runs in a world with");
				Output("'pm target' or 'pm all' on - if both are on and this stays at zero, the hook is");
				Output("not live.");
			}
		}

		/// <summary>Labels padded to the longest one ("pm colour {low} {high}") so the block shares a column.</summary>
		private static void Line(string _label, string _value)
		{
			Output("  " + _label.PadRight(22) + ": " + _value);
		}

		/// <summary>A switch line: the choices padded to the widest set, then what the switch is for.</summary>
		private static void Switch(string _label, string _choices, string _note)
		{
			Line(_label, _choices.PadRight(26) + " - " + _note);
		}

		private static void SetRange(List<string> _params)
		{
			if (_params.Count != 2)
			{
				Output("Usage: pm range {m} - currently: " + RangeLine());
				return;
			}

			if (!TryMeasure(_params[1], "range", out float range))
			{
				return;
			}

			Settings.Range = range;
			Config.Save();
			Output("Range: " + RangeLine());
		}

		private static void SetPips(List<string> _params)
		{
			if (_params.Count != 2)
			{
				Output("Usage: pm pips {n} - currently: " + PipsLine());
				return;
			}

			if (!TryCount(_params[1], "pip count", 1, out int pips))
			{
				return;
			}

			Settings.TimerPips = pips;
			Config.Save();
			Output("Timer pips: " + PipsLine());
		}

		private static void SetSize(List<string> _params)
		{
			if (_params.Count != 3)
			{
				Output("Usage: pm size {w} {h} - currently: " + SizeLine());
				return;
			}

			if (!TryCount(_params[1], "width", 1, out int width)
				|| !TryCount(_params[2], "height", 1, out int height))
			{
				return;
			}

			Settings.Width = width;
			Settings.Height = height;
			Config.Save();
			Output("Overhead bar size: " + SizeLine());
		}

		private static void SetOffset(List<string> _params)
		{
			if (_params.Count != 2)
			{
				Output("Usage: pm offset {m} - currently: " + OffsetLine());
				return;
			}

			if (!Config.TrySigned(_params[1], out float offset))
			{
				Output("'" + _params[1] + "' is not a valid offset - a number of metres, like 0.25 or -0.1.");
				return;
			}

			Settings.HeadOffset = offset;
			Config.Save();
			Output("Head offset: " + OffsetLine());
		}

		private static void SetOpacity(List<string> _params)
		{
			if (_params.Count != 2)
			{
				Output("Usage: pm opacity {0-1} - currently: " + Config.Number(Settings.Opacity));
				return;
			}

			if (!Config.TryUnit(_params[1], out float opacity))
			{
				Output("'" + _params[1] + "' is not a valid opacity - numbers from 0 to 1, like 0.85.");
				return;
			}

			Settings.Opacity = opacity;
			Config.Save();
			Output("Opacity: " + Config.Number(Settings.Opacity));
		}

		private static void SetColour(List<string> _params)
		{
			if (_params.Count != 3)
			{
				Output("Usage: pm colour {low} {high} - currently: " + ColourLine());
				return;
			}

			if (!TryColour(_params[1], "low colour", out Color32 low)
				|| !TryColour(_params[2], "high colour", out Color32 high))
			{
				return;
			}

			Settings.ColourLow = low;
			Settings.ColourHigh = high;
			Config.Save();
			Output("Colours: " + ColourLine());
		}

		private static bool TryCount(string _value, string _what, int _min, out int _parsed)
		{
			if (Config.TryCount(_value, _min, out _parsed))
			{
				return true;
			}
			Output("'" + _value + "' is not a valid " + _what + " - whole numbers from " + _min + " up.");
			return false;
		}

		private static bool TryMeasure(string _value, string _what, out float _parsed)
		{
			if (Config.TryMeasure(_value, out _parsed))
			{
				return true;
			}
			Output("'" + _value + "' is not a valid " + _what + " - numbers from 0 up, like 12.5.");
			return false;
		}

		private static bool TryColour(string _value, string _what, out Color32 _parsed)
		{
			if (BarColour.TryParse(_value, out _parsed))
			{
				return true;
			}
			Output("'" + _value + "' is not a valid " + _what + " - r,g,b with each from 0 to 255, like 220,50,50.");
			return false;
		}

		/// <summary>The choice list for a cycle or toggle, with the live value marked.</summary>
		private static string Choices(params string[] _options)
		{
			return "[ " + string.Join(" | ", _options) + " ]";
		}

		private static string Mark(string _option, bool _live)
		{
			return _live ? ">" + _option + "<" : _option;
		}

		private static string EnabledChoices()
		{
			return Choices(Mark("on", Settings.Enabled), Mark("off", !Settings.Enabled));
		}

		private static string OnOffChoices(bool _on)
		{
			return Choices(Mark("on", _on), Mark("off", !_on));
		}

		private static string TimerChoices()
		{
			return Choices(
				Mark("bar", Settings.Timer == TimerStyle.Bar),
				Mark("pips", Settings.Timer == TimerStyle.Pips),
				Mark("off", Settings.Timer == TimerStyle.Off));
		}

		private static string RangeLine()
		{
			return Config.Number(Settings.Range) + " m reach for 'pm all'";
		}

		private static string PipsLine()
		{
			return Settings.TimerPips + " pips, one per second (with 'pm timer' on pips)";
		}

		private static string SizeLine()
		{
			return Settings.Width + " x " + Settings.Height + " px overhead bar";
		}

		private static string OffsetLine()
		{
			return Config.Number(Settings.HeadOffset) + " m above the head";
		}

		private static string TimerLine()
		{
			switch (Settings.Timer)
			{
			case TimerStyle.Bar:
				return "a thin bar under the meter that drains as the seconds run out";
			case TimerStyle.Pips:
				return "one pip per second remaining (" + Settings.TimerPips + " pips)";
			default:
				return "off";
			}
		}

		private static string ColourLine()
		{
			return BarColour.Format(Settings.ColourLow) + " at empty / "
				+ BarColour.Format(Settings.ColourHigh) + " at full (r,g,b)";
		}

		private static void Output(string _line)
		{
			SdtdConsole.Instance.Output(_line);
		}

		public override string[] getCommands()
		{
			return new string[2] { "pm", "painmeter" };
		}

		public override string getDescription()
		{
			return "Reports PainMeter's settings; 'pm on' and 'pm off' switch it.";
		}

		public override string getHelp()
		{
			return "Usage: pm [on|off|bar|target|all|range {m}|hidezero|flash|timer|pips {n}"
				+ "|size {w} {h}|offset {m}|opacity {0-1}|colour {low} {high}|animals|info|reset]"
				+ "\r\n\r\nEvery zombie carries a hidden pain meter. Each hit that makes it flinch "
				+ "adds to it - about 0.55 for an ordinary zombie, 0.7 feral, 0.9 radiated - and it "
				+ "drains at 0.2 a second. Below 1, every such hit staggers the zombie and for half "
				+ "a second it can neither attack nor move at more than a tenth of its speed. At 1 or "
				+ "more it shrugs your hits off and attacks straight through them. Two hits on almost "
				+ "any zombie take it past 1, and the meter tops out at 3, which is ten seconds of "
				+ "draining before it staggers again. PainMeter draws that number so you can see "
				+ "it coming: a bar that fills from empty to full at 1, shifting from the 'low' "
				+ "colour to the 'high' one on the way, flashing while the half-second lockout "
				+ "runs, and once full showing how long until it drops back below 1. It reads the "
				+ "game's own numbers on this machine and changes nothing."
				+ "\r\n\r\n'pm' on its own prints the settings and changes nothing - it is the "
				+ "status read, so it is safe to type when you only want to look. Each line names "
				+ "the command that changes it, so the settings block is also the menu."
				+ "\r\n\r\n'pm on' and 'pm off' are the master switch. With it off nothing is drawn "
				+ "and every other setting here is inert."
				+ "\r\n\r\n'pm bar' toggles a row under Undead Legacy's target health bar, on by "
				+ "default. It follows UL's bar exactly: same target, same visibility, and it "
				+ "disappears with UL's own 'Target HP' option. Without Undead Legacy there is no "
				+ "bar to sit under, so the setting is saved but does nothing."
				+ "\r\n\r\n'pm target' toggles a bar floating over the head of the zombie under your "
				+ "crosshair, and 'pm all' one over every zombie within 'pm range' metres of you "
				+ "(15 by default); both off by default. They work with or without Undead Legacy, "
				+ "but they live in the game's on-screen icons layer, so UL's 'On-screen icons' "
				+ "option hides them too. 'pm size' sets the bar's width and height in UI pixels "
				+ "(60 by 6), 'pm offset' how far above the head it floats in metres (0.25, "
				+ "negative allowed), and 'pm opacity' how solid it is (0.85)."
				+ "\r\n\r\n'pm hidezero', on by default, hides any meter that reads zero, so an "
				+ "untouched zombie carries no bar; off draws it empty. 'pm flash' toggles the "
				+ "flash during the half-second lockout. 'pm timer' cycles how the time-to-drop is "
				+ "shown once the meter is full: a thin draining bar under the meter, one pip per "
				+ "second remaining ('pm pips' sets how many pips there are, 10 by default, which "
				+ "covers the cap), or off. 'pm colour' takes the empty and full tints as r,g,b."
				+ "\r\n\r\n'pm animals' extends all of it to hostile animals - dogs, vultures, bears "
				+ "- which run on the same rules. Off by default: zombies and bandits only."
				+ "\r\n\r\nEvery setting here takes effect immediately and is written straight to a "
				+ "settings file, so it survives a restart - and survives updating the mod, because "
				+ "the file lives in the game's user data folder next to Saves rather than in Mods. "
				+ "'pm info' prints its full path. It is plain 'key = value' text and can be edited "
				+ "by hand with the game closed; a line that will not parse is ignored rather than "
				+ "fatal, and deleting the file goes back to the built-in defaults in Settings.cs."
				+ "\r\n\r\n'pm info' prints the same block with the patch state and the counters "
				+ "added. 'frames rendered' proves the overhead hook is being reached (it only "
				+ "counts with an overhead mode on), and 'zombies seen in pain' proves the numbers "
				+ "are being read. 'pm reset' zeroes the counters so one fight can be measured on its "
				+ "own."
				+ "\r\n\r\nUnlike most console commands this one runs on the machine that typed it, "
				+ "because every setting is about what that machine draws. 'painmeter' is an alias "
				+ "for 'pm'.";
		}
	}
}
