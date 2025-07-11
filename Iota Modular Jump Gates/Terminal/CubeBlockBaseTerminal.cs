﻿using VRage.Game.ModAPI;

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

		private static void SetupBasicBlockTerminalControls()
		{

		}

		public static void Load(IMyModContext context)
		{
			if (MyCubeBlockTerminal.IsLoaded) return;
			MyCubeBlockTerminal.IsLoaded = true;
			MyCubeBlockTerminal.SetupJumpGateBlockTerminalControls();
			MyCubeBlockTerminal.SetupJumpGateBlockTerminalActions();
			MyCubeBlockTerminal.SetupJumpGateBlockTerminalProperties();
		}

		public static void Unload()
		{
			if (!MyCubeBlockTerminal.IsLoaded) return;
			MyCubeBlockTerminal.IsLoaded = false;
		}

		public static void UpdateRedrawControls()
		{

		}
	}
}
