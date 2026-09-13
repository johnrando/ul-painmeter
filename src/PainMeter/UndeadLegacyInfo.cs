using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace PainMeter
{
	/// <summary>
	/// Detects whether Undead Legacy is loaded and which build it is. <see cref="Present"/> gates
	/// the health-bar row, which patches a UL controller; the overhead bars need only vanilla.
	///
	/// Of UL's version markers only the ones on <c>H_UndeadLegacy</c> are trustworthy: the
	/// <c>[BepInPlugin]</c> attribute and the <c>pluginVersion</c> literal. UL's assembly version is
	/// hardcoded 1.0.0.0 and its ModInfo.xml lags reality.
	/// </summary>
	internal static class UndeadLegacyInfo
	{
		private const string AssemblyName = "UndeadLegacy";

		internal static bool Present;

		internal static string Version = "not detected";

		internal static string DetectedSource = "none";

		/// <summary>One-line summary for the <c>ds</c> console command.</summary>
		internal static string Status = "not checked";

		internal static void Report()
		{
			Assembly assembly = FindAssembly(AssemblyName);
			Present = assembly != null;
			if (!Present)
			{
				Status = "not installed";
				Log.Out(Patches.LogPrefix + "Undead Legacy is not installed. The overhead bars work "
					+ "the same; there is no UL target health bar to put a row under, so 'pm bar' "
					+ "is inert.");
				return;
			}

			Type plugin = assembly.GetType("H_UndeadLegacy", false);
			string raw = plugin == null
				? null
				: ReadFromBepInPluginAttribute(plugin) ?? ReadFromVersionConstant(plugin);

			if (raw == null)
			{
				Version = "unknown";
				Status = "installed, version unknown";
				Log.Out(Patches.LogPrefix + "Undead Legacy is installed but its version could not be "
					+ "read. The health-bar row is still installed.");
				return;
			}

			Version = raw;
			Status = raw;
			Log.Out(Patches.LogPrefix + "Undead Legacy " + raw + " detected (from " + DetectedSource
				+ "). The row under UL's target health bar is available.");
		}

		/// <summary>
		/// Reads the third argument of <c>[BepInPlugin(guid, name, version)]</c> via
		/// <see cref="CustomAttributeData"/>, so this assembly needs no reference to BepInEx.
		/// </summary>
		private static string ReadFromBepInPluginAttribute(Type _plugin)
		{
			try
			{
				foreach (CustomAttributeData attribute in CustomAttributeData.GetCustomAttributes(_plugin))
				{
					if (attribute.Constructor?.DeclaringType?.Name != "BepInPlugin")
					{
						continue;
					}
					IList<CustomAttributeTypedArgument> args = attribute.ConstructorArguments;
					if (args.Count >= 3 && args[2].Value is string version && version.Length > 0)
					{
						DetectedSource = "[BepInPlugin] attribute";
						return version;
					}
				}
			}
			catch (Exception e)
			{
				Log.Warning(Patches.LogPrefix + "Could not read Undead Legacy's [BepInPlugin] attribute: "
					+ e.Message);
			}
			return null;
		}

		private static string ReadFromVersionConstant(Type _plugin)
		{
			try
			{
				FieldInfo field = AccessTools.Field(_plugin, "pluginVersion");
				if (field != null && field.IsLiteral && field.GetRawConstantValue() is string version
					&& version.Length > 0)
				{
					DetectedSource = "pluginVersion constant";
					return version;
				}
			}
			catch (Exception e)
			{
				Log.Warning(Patches.LogPrefix + "Could not read Undead Legacy's pluginVersion constant: "
					+ e.Message);
			}
			return null;
		}

		/// <summary>A loaded assembly by simple name, or null. Kept for parity with the sibling mods.</summary>
		internal static Assembly FindAssembly(string _simpleName)
		{
			Assembly[] loaded = AppDomain.CurrentDomain.GetAssemblies();
			for (int i = 0; i < loaded.Length; i++)
			{
				if (string.Equals(loaded[i].GetName().Name, _simpleName, StringComparison.OrdinalIgnoreCase))
				{
					return loaded[i];
				}
			}
			return null;
		}
	}
}
