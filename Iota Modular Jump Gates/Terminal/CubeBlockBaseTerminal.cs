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
	internal static class MyCubeBlockTerminal
	{
		public static bool IsLoaded { get; private set; } = false;
		public static readonly string MODID_PREFIX = MyJumpGateModSession.MODID + ".BlockBase.";

		private static void SetupJumpGateBlockTerminalControls()
		{
			
		}

		private static void SetupJumpGateBlockTerminalActions()
		{

		}

		private static void SetupJumpGateBlockTerminalProperties()
		{
			
		}

		public static void Load(IMyModContext context)
		{
			if (MyCubeBlockTerminal.IsLoaded) return;
			MyCubeBlockTerminal.IsLoaded = true;
			MyCubeBlockTerminal.SetupJumpGateBlockTerminalControls();
			MyCubeBlockTerminal.SetupJumpGateBlockTerminalActions();
			MyCubeBlockTerminal.SetupJumpGateBlockTerminalProperties();
			MyPBCubeBlockBase.SetupBlockTerminal();
		}
	}
}
