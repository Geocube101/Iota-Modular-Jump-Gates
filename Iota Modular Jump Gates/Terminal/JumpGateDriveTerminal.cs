using Sandbox.ModAPI.Interfaces.Terminal;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;
using IOTA.ModularJumpGates.CubeBlock;
using VRage.ModAPI;
using IOTA.ModularJumpGates.API.CubeBlock;
using IOTA.ModularJumpGates.Util;
using IOTA.ModularJumpGates.ProgramScripting.CubeBlock;

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
			if (MyJumpGateDriveTerminal.IsLoaded) return;
			MyJumpGateDriveTerminal.IsLoaded = true;
			MyJumpGateDriveTerminal.SetupJumpGateDriveTerminalControls();
			MyJumpGateDriveTerminal.SetupJumpGateDriveTerminalActions();
			MyJumpGateDriveTerminal.SetupJumpGateDriveTerminalProperties();
			MyPBJumpGateDrive.SetupBlockTerminal();
		}
	}
}
