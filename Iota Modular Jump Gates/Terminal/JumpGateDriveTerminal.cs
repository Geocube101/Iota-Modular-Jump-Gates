using IOTA.ModularJumpGates.Util;
using Sandbox.ModAPI.Interfaces.Terminal;
using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Game.ModAPI;

namespace IOTA.ModularJumpGates.Terminal
{
	internal static class MyJumpGateDriveTerminal
	{
		public static bool IsLoaded { get; private set; } = false;
		public static readonly string MODID_PREFIX = MyJumpGateModSession.MODID + ".JumpGateDrive.";

		private static void SetupJumpGateDriveTerminalControls()
		{
			
		}

		private static void SetupJumpGateDriveTerminalActions()
		{

		}

		private static void SetupJumpGateDriveTerminalProperties()
		{

		}

		public static void Load(IMyModContext context)
		{
			List<IMyTerminalControl> controls;
			MyAPIGateway.TerminalControls.GetControls<IMyUpgradeModule>(out controls);
			if (MyJumpGateDriveTerminal.IsLoaded || controls.Count == 0) return;
			MyJumpGateDriveTerminal.IsLoaded = true;
			MyJumpGateDriveTerminal.SetupJumpGateDriveTerminalControls();
			MyJumpGateDriveTerminal.SetupJumpGateDriveTerminalActions();
			MyJumpGateDriveTerminal.SetupJumpGateDriveTerminalProperties();
		}
	}
}
