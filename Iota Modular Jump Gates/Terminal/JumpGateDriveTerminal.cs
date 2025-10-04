using VRage.Game.ModAPI;

namespace IOTA.ModularJumpGates.Terminal
{
	internal static class MyJumpGateDriveTerminal
	{
		public static bool IsLoaded { get; private set; } = false;
		public static string MODID_PREFIX { get; private set; } = MyJumpGateModSession.MODID + ".JumpGateDrive.";

		private static void SetupJumpGateDriveTerminalControls()
		{
			
		}

		private static void SetupJumpGateDriveTerminalActions()
		{

		}

		private static void SetupJumpGateDriveTerminalProperties()
		{

		}

		public static void Load()
		{
			if (MyJumpGateDriveTerminal.IsLoaded) return;
			MyJumpGateDriveTerminal.IsLoaded = true;
			MyJumpGateDriveTerminal.SetupJumpGateDriveTerminalControls();
			MyJumpGateDriveTerminal.SetupJumpGateDriveTerminalActions();
			MyJumpGateDriveTerminal.SetupJumpGateDriveTerminalProperties();
		}

		public static void Unload()
		{
			if (!MyJumpGateDriveTerminal.IsLoaded) return;
			MyJumpGateDriveTerminal.IsLoaded = false;
			MyJumpGateDriveTerminal.MODID_PREFIX = null;
		}

		public static void UpdateRedrawControls()
		{

		}
	}
}
