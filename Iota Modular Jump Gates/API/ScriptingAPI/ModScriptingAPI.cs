using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;

namespace IOTA.ModularJumpGates.API.ScriptingAPI
{
	public class ModScriptingAPI
	{
		public static bool IsInitialized => ModScriptingAPI.Session != null;
		public static readonly int[] APIVersion = new int[2] { 1, 0 };
		public static Dictionary<string, object> Session { get; private set; } = null;

		public static bool Init(IMyProgrammableBlock block, out bool version_ok)
		{
			Func<int[], Action<Dictionary<string, object>>, KeyValuePair<bool, bool>> initializer = block.GetValue<Func<int[], Action<Dictionary<string, object>>, KeyValuePair<bool, bool>>>("IMGJScriptingAPI");
			KeyValuePair<bool, bool> result = initializer(ModScriptingAPI.APIVersion, (attributes) => ModScriptingAPI.Session = attributes);
			version_ok = result.Value;
			return result.Key;
		}
	}
}
