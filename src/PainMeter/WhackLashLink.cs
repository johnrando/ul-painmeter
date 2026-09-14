using System;
using System.Reflection;
using HarmonyLib;

namespace PainMeter
{
	/// <summary>
	/// Reads WhackLash's focus meter, when that mod is installed, so the widget can draw it under
	/// the pain bar. WhackLash publishes four static methods on <c>WhackLash.FlavorInterop</c> -
	/// <c>FocusPoints(int) : float</c>, <c>FocusBreak() : float</c>, <c>FocusCap() : float</c> and
	/// <c>DamageBonusPercent(int) : float</c> - and this binds them by reflection into delegates at
	/// init, so neither mod references the other and either works with the other absent. Every
	/// failure degrades to a log line and an unwired link; a throw at read time retires the link
	/// for the session rather than repeating every frame. Read-only: nothing here writes to
	/// WhackLash or the game.
	/// </summary>
	internal static class WhackLashLink
	{
		private const string AssemblyName = "WhackLash";

		private const string TypeName = "WhackLash.FlavorInterop";

		/// <summary>Outcome of the lookup, as reported by <c>pm info</c>.</summary>
		internal static string Status = "not checked";

		/// <summary>Whether the assembly was there at all, as opposed to there but unusable.</summary>
		internal static bool Found { get; private set; }

		/// <summary>Whether every read is bound and a call will reach WhackLash.</summary>
		internal static bool Wired => points != null;

		private static Func<int, float> points;

		private static Func<float> breakPoints;

		private static Func<float> cap;

		private static Func<int, float> bonus;

		/// <summary>Called once from <see cref="Patches.Apply"/>. ModManager has loaded every mod
		/// assembly by then, so a missing assembly means WhackLash is not installed.</summary>
		internal static void Resolve()
		{
			Assembly assembly = UndeadLegacyInfo.FindAssembly(AssemblyName);
			Found = assembly != null;
			if (assembly == null)
			{
				Status = "not installed - no focus row";
				Log.Out(Patches.LogPrefix + "WhackLash is not installed; the pain bar is drawn on its own.");
				return;
			}

			try
			{
				Type type = assembly.GetType(TypeName, false);
				if (type == null)
				{
					Status = "installed, but has no " + TypeName;
					Log.Warning(Patches.LogPrefix + "WhackLash is installed but has no " + TypeName
						+ ", so its focus meter is not drawn. Both mods still work on their own.");
					return;
				}

				Func<int, float> readPoints = Bind<Func<int, float>>(type, "FocusPoints", typeof(int));
				Func<float> readBreak = Bind<Func<float>>(type, "FocusBreak");
				Func<float> readCap = Bind<Func<float>>(type, "FocusCap");
				Func<int, float> readBonus = Bind<Func<int, float>>(type, "DamageBonusPercent", typeof(int));
				if (readPoints == null || readBreak == null || readCap == null || readBonus == null)
				{
					Status = "installed, but its readout did not match";
					Log.Warning(Patches.LogPrefix + "WhackLash is installed but its focus readout could "
						+ "not be bound, so it is not drawn. Both mods still work; one of them is "
						+ "probably newer than the other.");
					return;
				}

				breakPoints = readBreak;
				cap = readCap;
				bonus = readBonus;
				points = readPoints;
				Status = "installed - focus row wired up";
				Log.Out(Patches.LogPrefix + "WhackLash detected: its focus meter is drawn under the pain "
					+ "bar. Toggle with 'pm focus'.");
			}
			catch (Exception e)
			{
				Status = "installed, but the lookup threw";
				Log.Warning(Patches.LogPrefix + "Could not bind WhackLash: " + e.Message);
			}
		}

		private static T Bind<T>(Type _type, string _name, params Type[] _parameters) where T : class
		{
			MethodInfo method = AccessTools.DeclaredMethod(_type, _name, _parameters);
			if (method == null || method.ReturnType != typeof(float))
			{
				return null;
			}
			return Delegate.CreateDelegate(typeof(T), method) as T;
		}

		/// <summary>
		/// This entity's focus meter as it stands now. False, with <c>default</c>, when WhackLash is
		/// absent, the focus row is switched off, or the meter reads zero, so the caller draws the
		/// pain bar on its own.
		/// </summary>
		internal static bool TryRead(int _entityId, out FocusState _state)
		{
			_state = default;
			if (points == null || !Settings.Focus)
			{
				return false;
			}

			try
			{
				float value = points(_entityId);
				if (!(value > 0f))
				{
					return false;
				}
				_state = new FocusState(value, breakPoints(), cap(), bonus(_entityId));
				return true;
			}
			catch (Exception e)
			{
				// One throw retires the link rather than repeating on every frame.
				points = null;
				Status = "installed, but a read threw - retired for this session";
				Log.Error(Patches.LogPrefix + "WhackLash threw when its focus meter was read; the focus "
					+ "row is off for the rest of this session. The pain bar is unaffected.");
				Log.Exception(e);
				return false;
			}
		}
	}
}
