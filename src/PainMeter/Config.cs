using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace PainMeter
{
	/// <summary>
	/// Reads <see cref="Settings"/> back at startup and writes it out whenever a <c>pm</c> command
	/// changes something. The file lives in the game's user data folder rather than in the mod
	/// folder, so it survives a mod update. Plain <c>key = value</c> text; every line names the
	/// console command that writes it. Nothing here can stop the mod working: any failure degrades
	/// to a log line and the defaults.
	/// </summary>
	internal static class Config
	{
		private const string FolderName = "PainMeter";

		private const string FileName = "settings.txt";

		/// <summary>What the last load or save did, as reported by <c>pm info</c>.</summary>
		internal static string Status = "not loaded - mod init has not run";

		/// <summary>Where the file is, once resolved. Null means it never was.</summary>
		private static string filePath;

		/// <summary>
		/// Called once from <see cref="ModApi.InitMod"/>, before the patches go in. A missing file
		/// is a first run: writing the defaults out is what makes the file discoverable.
		/// </summary>
		internal static void Load()
		{
			if (!Resolve())
			{
				return;
			}

			if (!File.Exists(filePath))
			{
				Save();
				return;
			}

			try
			{
				int applied = 0;
				int rejected = 0;
				foreach (string line in File.ReadAllLines(filePath))
				{
					switch (Parse(line))
					{
					case LineResult.Applied:
						applied++;
						break;
					case LineResult.Rejected:
						rejected++;
						break;
					}
				}

				Status = applied + " settings loaded"
					+ (rejected > 0 ? ", " + rejected + " line(s) ignored" : "")
					+ " - " + filePath;
				Log.Out(Patches.LogPrefix + "Settings loaded from " + filePath + ".");
			}
			catch (Exception e)
			{
				Status = "NOT LOADED - " + e.Message;
				Log.Warning(Patches.LogPrefix + "Could not read " + filePath + ", so the built-in "
					+ "defaults are in force: " + e.Message);
			}
		}

		/// <summary>
		/// Writes the whole file, which is what keeps the comments and ordering intact. Called by
		/// every <c>pm</c> command that changes a setting.
		/// </summary>
		internal static void Save()
		{
			if (!Resolve())
			{
				return;
			}

			try
			{
				Directory.CreateDirectory(Path.GetDirectoryName(filePath));
				File.WriteAllText(filePath, Compose());
				Status = "saved - " + filePath;
			}
			catch (Exception e)
			{
				Status = "NOT SAVED - " + e.Message;
				Log.Warning(Patches.LogPrefix + "Could not write " + filePath + ", so this change "
					+ "will not survive a restart: " + e.Message);
			}
		}

		/// <summary>Works out where the file goes, once.</summary>
		private static bool Resolve()
		{
			if (filePath != null)
			{
				return true;
			}

			try
			{
				string dir = GameIO.GetUserGameDataDir();
				if (string.IsNullOrEmpty(dir))
				{
					Status = "unavailable - the game reported no user data folder";
					return false;
				}
				filePath = Path.Combine(Path.Combine(dir, FolderName), FileName);
				return true;
			}
			catch (Exception e)
			{
				Status = "unavailable - " + e.Message;
				Log.Warning(Patches.LogPrefix + "Could not work out where to keep settings, so they "
					+ "will not persist: " + e.Message);
				return false;
			}
		}

		private static string Compose()
		{
			StringBuilder text = new StringBuilder();
			text.AppendLine("# PainMeter settings.");
			text.AppendLine("#");
			text.AppendLine("# Read once when the game starts and rewritten whenever a 'pm' command changes");
			text.AppendLine("# something, so edit this with the game closed. Every line names the command that");
			text.AppendLine("# sets it; anything after a '#' is a comment, and a line that will not parse is");
			text.AppendLine("# ignored rather than fatal.");
			text.AppendLine();
			Setting(text, "enabled", OnOff(Settings.Enabled), "pm on|off");
			Setting(text, "bar", OnOff(Settings.HealthBarRow), "pm bar");
			Setting(text, "overhead.target", OnOff(Settings.OverheadTarget), "pm target");
			Setting(text, "overhead.all", OnOff(Settings.OverheadAll), "pm all");
			Setting(text, "range", Number(Settings.Range), "pm range {m}");
			Setting(text, "hidezero", OnOff(Settings.HideZero), "pm hidezero");
			Setting(text, "flash", OnOff(Settings.Flash), "pm flash");
			Setting(text, "scale", Settings.Scale.ToString().ToLowerInvariant(), "pm scale - threshold, full or hits");
			Setting(text, "timer", Settings.Timer.ToString().ToLowerInvariant(), "pm timer - bar, pips or off");
			Setting(text, "timer.pips", Settings.TimerPips.ToString(), "pm pips {n}");
			Setting(text, "size.width", Settings.Width.ToString(), "pm size {w} {h}");
			Setting(text, "size.height", Settings.Height.ToString(), "pm size {w} {h}");
			Setting(text, "offset", Number(Settings.HeadOffset), "pm offset {m}");
			Setting(text, "opacity", Number(Settings.Opacity), "pm opacity {0-1}");
			Setting(text, "colour.low", BarColour.Format(Settings.ColourLow), "pm colour {low} {high} {locked}");
			Setting(text, "colour.high", BarColour.Format(Settings.ColourHigh), "pm colour {low} {high} {locked}");
			Setting(text, "animals", OnOff(Settings.Animals), "pm animals");
			Setting(text, "focus", OnOff(Settings.Focus), "pm focus - WhackLash focus pips, locked tint and bonus label");
			Setting(text, "focus.x", Settings.FocusX.ToString(), "pm focuspos {x} {y} - pips right of / above the bar's end");
			Setting(text, "focus.y", Settings.FocusY.ToString(), "pm focuspos {x} {y}");
			Setting(text, "colour.locked", BarColour.Format(Settings.ColourLocked), "pm colour {low} {high} {locked}");
			Setting(text, "pip.low", BarColour.Format(Settings.PipLow), "pm pipcolour {low} {mid} {high} - focus pips by meter level");
			Setting(text, "pip.mid", BarColour.Format(Settings.PipMid), "pm pipcolour {low} {mid} {high}");
			Setting(text, "pip.high", BarColour.Format(Settings.PipHigh), "pm pipcolour {low} {mid} {high}");
			Setting(text, "colour.timer", BarColour.Format(Settings.ColourTimer), "pm accents {timer} {flash} {mark}");
			Setting(text, "colour.flash", BarColour.Format(Settings.ColourFlash), "pm accents {timer} {flash} {mark}");
			Setting(text, "colour.mark", BarColour.Format(Settings.ColourMark), "pm accents {timer} {flash} {mark} - break frame, +N% before break, tick at 1");
			return text.ToString();
		}

		/// <summary>One setting, padded so the values and the commands each share a column.</summary>
		private static void Setting(StringBuilder _text, string _key, string _value, string _command)
		{
			_text.AppendLine(_key.PadRight(16) + "= " + _value.PadRight(12) + " # " + _command);
		}

		private enum LineResult
		{
			/// <summary>Blank or a comment.</summary>
			Skipped,

			Applied,

			Rejected
		}

		/// <summary>
		/// One line of the file. An unknown key is a warning rather than an error: that is what a
		/// file written by a newer version of the mod looks like to an older one.
		/// </summary>
		private static LineResult Parse(string _line)
		{
			int comment = _line.IndexOf('#');
			string text = (comment >= 0 ? _line.Substring(0, comment) : _line).Trim();
			if (text.Length == 0)
			{
				return LineResult.Skipped;
			}

			int split = text.IndexOf('=');
			if (split <= 0)
			{
				Log.Warning(Patches.LogPrefix + "Ignoring a settings line that is not 'key = value': "
					+ _line.Trim());
				return LineResult.Rejected;
			}

			string key = text.Substring(0, split).Trim().ToLowerInvariant();
			string value = text.Substring(split + 1).Trim();
			if (Apply(key, value))
			{
				return LineResult.Applied;
			}

			Log.Warning(Patches.LogPrefix + "Ignoring settings line '" + _line.Trim()
				+ "' - unknown setting or unusable value.");
			return LineResult.Rejected;
		}

		private static bool Apply(string _key, string _value)
		{
			switch (_key)
			{
			case "enabled":
				return TryBool(_value, ref Settings.Enabled);
			case "bar":
				return TryBool(_value, ref Settings.HealthBarRow);
			case "overhead.target":
				return TryBool(_value, ref Settings.OverheadTarget);
			case "overhead.all":
				return TryBool(_value, ref Settings.OverheadAll);
			case "range":
				return LoadMeasure(_value, ref Settings.Range);
			case "hidezero":
				return TryBool(_value, ref Settings.HideZero);
			case "flash":
				return TryBool(_value, ref Settings.Flash);
			case "scale":
				return TryScale(_value, ref Settings.Scale);
			case "timer":
				return TryTimer(_value, ref Settings.Timer);
			case "timer.pips":
				return LoadCount(_value, ref Settings.TimerPips, 1);
			case "size.width":
				return LoadCount(_value, ref Settings.Width, 1);
			case "size.height":
				return LoadCount(_value, ref Settings.Height, 1);
			case "offset":
				return LoadSigned(_value, ref Settings.HeadOffset);
			case "opacity":
				return LoadUnit(_value, ref Settings.Opacity);
			case "colour.low":
			case "color.low":
				return LoadColour(_value, ref Settings.ColourLow);
			case "colour.high":
			case "color.high":
				return LoadColour(_value, ref Settings.ColourHigh);
			case "animals":
				return TryBool(_value, ref Settings.Animals);
			case "focus":
				return TryBool(_value, ref Settings.Focus);
			case "focus.x":
				return LoadPixels(_value, ref Settings.FocusX);
			case "focus.y":
				return LoadPixels(_value, ref Settings.FocusY);
			case "colour.locked":
			case "color.locked":
				return LoadColour(_value, ref Settings.ColourLocked);
			case "pip.low":
				return LoadColour(_value, ref Settings.PipLow);
			case "pip.mid":
				return LoadColour(_value, ref Settings.PipMid);
			case "pip.high":
				return LoadColour(_value, ref Settings.PipHigh);
			case "colour.timer":
			case "color.timer":
				return LoadColour(_value, ref Settings.ColourTimer);
			case "colour.flash":
			case "color.flash":
				return LoadColour(_value, ref Settings.ColourFlash);
			case "colour.mark":
			case "color.mark":
				return LoadColour(_value, ref Settings.ColourMark);
			default:
				return false;
			}
		}

		/// <summary>Accepts what the file writes plus the obvious hand-edit synonyms.</summary>
		private static bool TryBool(string _value, ref bool _target)
		{
			switch (_value.ToLowerInvariant())
			{
			case "on":
			case "true":
			case "yes":
			case "1":
				_target = true;
				return true;
			case "off":
			case "false":
			case "no":
			case "0":
				_target = false;
				return true;
			default:
				return false;
			}
		}

		/// <summary>Whole numbers at or above a floor. Shared with the console command.</summary>
		internal static bool TryCount(string _value, int _min, out int _parsed)
		{
			return int.TryParse(_value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _parsed)
				&& _parsed >= _min;
		}

		private static bool LoadCount(string _value, ref int _target, int _min)
		{
			if (!TryCount(_value, _min, out int parsed))
			{
				return false;
			}
			_target = parsed;
			return true;
		}

		/// <summary>A signed pixel offset, <see cref="MaxPixels"/> either way. Shared with the console command.</summary>
		internal static bool TryPixels(string _value, out int _parsed)
		{
			return int.TryParse(_value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _parsed)
				&& _parsed >= -MaxPixels && _parsed <= MaxPixels;
		}

		internal const int MaxPixels = 200;

		private static bool LoadPixels(string _value, ref int _target)
		{
			if (!TryPixels(_value, out int parsed))
			{
				return false;
			}
			_target = parsed;
			return true;
		}

		/// <summary>
		/// Metres, zero or more, against the invariant culture so a file written on one machine
		/// means the same on one whose decimal separator is a comma. Shared with the console
		/// command. <c>!(x &gt;= 0)</c> rather than <c>x &lt; 0</c> so NaN is rejected too.
		/// </summary>
		internal static bool TryMeasure(string _value, out float _parsed)
		{
			return float.TryParse(_value, NumberStyles.Float, CultureInfo.InvariantCulture, out _parsed)
				&& _parsed >= 0f && !float.IsInfinity(_parsed);
		}

		private static bool LoadMeasure(string _value, ref float _target)
		{
			if (!TryMeasure(_value, out float parsed))
			{
				return false;
			}
			_target = parsed;
			return true;
		}

		/// <summary>A finite number of either sign: the head offset may go below the head.</summary>
		internal static bool TrySigned(string _value, out float _parsed)
		{
			return float.TryParse(_value, NumberStyles.Float, CultureInfo.InvariantCulture, out _parsed)
				&& !float.IsNaN(_parsed) && !float.IsInfinity(_parsed);
		}

		private static bool LoadSigned(string _value, ref float _target)
		{
			if (!TrySigned(_value, out float parsed))
			{
				return false;
			}
			_target = parsed;
			return true;
		}

		/// <summary>0 to 1 inclusive, for the opacity.</summary>
		internal static bool TryUnit(string _value, out float _parsed)
		{
			return TryMeasure(_value, out _parsed) && _parsed <= 1f;
		}

		private static bool LoadUnit(string _value, ref float _target)
		{
			if (!TryUnit(_value, out float parsed))
			{
				return false;
			}
			_target = parsed;
			return true;
		}

		internal static bool TryScale(string _value, ref MeterScale _target)
		{
			switch (_value.ToLowerInvariant())
			{
			case "threshold":
				_target = MeterScale.Threshold;
				return true;
			case "full":
				_target = MeterScale.Full;
				return true;
			case "hits":
				_target = MeterScale.Hits;
				return true;
			default:
				return false;
			}
		}

		internal static bool TryTimer(string _value, ref TimerStyle _target)
		{
			switch (_value.ToLowerInvariant())
			{
			case "off":
				_target = TimerStyle.Off;
				return true;
			case "bar":
				_target = TimerStyle.Bar;
				return true;
			case "pips":
				_target = TimerStyle.Pips;
				return true;
			default:
				return false;
			}
		}

		private static bool LoadColour(string _value, ref Color32 _target)
		{
			if (!BarColour.TryParse(_value, out Color32 parsed))
			{
				return false;
			}
			_target = parsed;
			return true;
		}

		private static string OnOff(bool _on)
		{
			return _on ? "on" : "off";
		}

		/// <summary>Written the way it is parsed, so a reported value can be typed back in.</summary>
		internal static string Number(float _value)
		{
			return _value.ToString(CultureInfo.InvariantCulture);
		}
	}
}
