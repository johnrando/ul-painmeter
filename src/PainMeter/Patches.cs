using System;
using System.Reflection;
using HarmonyLib;

namespace PainMeter
{
	/// <summary>
	/// Installs the Harmony patches. The overhead bars hook vanilla's on-screen icons window and
	/// go in on every install; the health-bar row hooks an Undead Legacy controller and goes in
	/// only once <see cref="UndeadLegacyInfo.Report"/> has confirmed UL is loaded. Everything that
	/// names a UL type lives in <see cref="UlPatches"/> and <see cref="HealthBarRow"/>, which are
	/// never JIT-compiled otherwise, so a plain install gets a log line rather than a
	/// TypeLoadException. Each target is resolved late and gated on its own prerequisites, so a
	/// game update that moves something degrades to a log line rather than an exception at init.
	///
	/// No load order needs declaring: UL applies its patches as a BepInEx plugin before any
	/// IModApi.InitMod runs, and ModManager loads every mod assembly before calling any InitMod.
	/// </summary>
	internal static class Patches
	{
		internal const string LogPrefix = "[PainMeter] ";

		private const string HarmonyId = "PainMeter";

		internal const string NotRunYet = "not applied - mod init has not run";

		/// <summary>Outcome of each patch, as reported by <c>pm info</c>.</summary>
		internal static string OverheadUpdateStatus = NotRunYet;

		internal static string OverheadCleanupStatus = NotRunYet;

		internal static string HealthBarInitStatus = NotRunYet;

		internal static string HealthBarUpdateStatus = NotRunYet;

		private static bool applied;

		internal static void Apply()
		{
			if (applied)
			{
				return;
			}
			applied = true;
			try
			{
				ApplyPatches();
			}
			catch (Exception e)
			{
				Log.Error(LogPrefix + "Failed to apply patches; no meters will be drawn.");
				Log.Exception(e);
			}
		}

		private static void ApplyPatches()
		{
			UndeadLegacyInfo.Report();

			Harmony harmony = new Harmony(HarmonyId);
			ApplyOverheadHooks(harmony);

			if (!UndeadLegacyInfo.Present)
			{
				HealthBarInitStatus = "NOT APPLIED - Undead Legacy is not installed";
				HealthBarUpdateStatus = HealthBarInitStatus;
				return;
			}
			UlPatches.Install(harmony);
		}

		/// <summary>
		/// The frame hook that draws the overhead bars, and the two that clear them when the HUD
		/// closes or is torn down. UL's subclass of the icons window overrides none of the three,
		/// so patching the base covers both.
		/// </summary>
		private static void ApplyOverheadHooks(Harmony _harmony)
		{
			OverheadUpdateStatus = Postfix(_harmony, typeof(XUiC_OnScreenIcons), "Update", new[] { typeof(float) },
				typeof(OverheadRenderer), nameof(OverheadRenderer.AfterUpdate),
				"overhead bars are drawn each frame");
			OverheadCleanupStatus = Postfix(_harmony, typeof(XUiC_OnScreenIcons), "Cleanup", Type.EmptyTypes,
				typeof(OverheadRenderer), nameof(OverheadRenderer.AfterCleanup),
				"overhead bars are destroyed with the HUD");
			if (!OverheadCleanupStatus.StartsWith("NOT APPLIED"))
			{
				string closeStatus = Postfix(_harmony, typeof(XUiC_OnScreenIcons), "OnClose", Type.EmptyTypes,
					typeof(OverheadRenderer), nameof(OverheadRenderer.AfterClose),
					"overhead bars hide with the HUD");
				OverheadCleanupStatus = closeStatus.StartsWith("NOT APPLIED")
					? OverheadCleanupStatus + "; OnClose " + closeStatus
					: OverheadCleanupStatus + " and OnClose";
			}
		}

		/// <summary>
		/// One postfix. Resolves the target by name, reports and logs either way. Shared with
		/// <see cref="UlPatches"/>.
		/// </summary>
		internal static string Postfix(Harmony _harmony, Type _targetType, string _targetName, Type[] _parameters,
			Type _patchType, string _patchName, string _effect)
		{
			string site = _targetType.Name + "." + _targetName;
			MethodInfo target = _parameters == null
				? AccessTools.DeclaredMethod(_targetType, _targetName)
				: AccessTools.DeclaredMethod(_targetType, _targetName, _parameters);
			if (target == null)
			{
				Log.Error(LogPrefix + "Patch NOT applied: " + site + " could not be found, so "
					+ _effect + " will not work.");
				return "NOT APPLIED - " + site + " not found";
			}

			MethodInfo patch = AccessTools.DeclaredMethod(_patchType, _patchName);
			if (patch == null)
			{
				Log.Error(LogPrefix + "Patch NOT applied: own method " + _patchType.Name + "."
					+ _patchName + " is missing - this is a build error in the mod.");
				return "NOT APPLIED - patch method missing";
			}

			try
			{
				_harmony.Patch(target, postfix: new HarmonyMethod(patch));
			}
			catch (Exception e)
			{
				Log.Error(LogPrefix + "Patch NOT applied on " + site + ": " + e.Message);
				return "NOT APPLIED - " + e.GetType().Name + " on " + site;
			}

			Log.Out(LogPrefix + "Patched " + site + ": " + _effect + ".");
			return "applied - postfix on " + site;
		}
	}
}
