using IOTA.ModularJumpGates.CubeBlock;
using IOTA.ModularJumpGates.Terminal;
using IOTA.ModularJumpGates.Util;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using VRageMath;

namespace IOTA.ModularJumpGates.ProgramScripting.CubeBlock
{
	internal static class MyPBJumpGateDrive
	{
		private static void SetupMaxRaycastDistanceTerminalAction()
		{
			IMyTerminalControlProperty<double> property = MyAPIGateway.TerminalControls.CreateProperty<double, IMyUpgradeModule>(MyJumpGateDriveTerminal.MODID_PREFIX + "MaxRaycastDistance");
			property.Getter = (block) => {
				MyJumpGateDrive drive = MyJumpGateModSession.GetBlockAsJumpGateDrive(block);
				if (drive == null) throw new InvalidBlockTypeException("Specified block is not a jump gate drive");
				return drive.MaxRaycastDistance;
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupWattageSinkOverrideTerminalAction()
		{
			IMyTerminalControlProperty<double> property = MyAPIGateway.TerminalControls.CreateProperty<double, IMyUpgradeModule>(MyJumpGateDriveTerminal.MODID_PREFIX + "WattageSinkOverride");
			property.Getter = (block) => {
				MyJumpGateDrive drive = MyJumpGateModSession.GetBlockAsJumpGateDrive(block);
				if (drive == null) throw new InvalidBlockTypeException("Specified block is not a jump gate drive");
				return drive.GetWattageSinkOverride();
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupCurrentWattageSinkInputTerminalAction()
		{
			IMyTerminalControlProperty<double> property = MyAPIGateway.TerminalControls.CreateProperty<double, IMyUpgradeModule>(MyJumpGateDriveTerminal.MODID_PREFIX + "CurrentWattageSinkInput");
			property.Getter = (block) => {
				MyJumpGateDrive drive = MyJumpGateModSession.GetBlockAsJumpGateDrive(block);
				if (drive == null) throw new InvalidBlockTypeException("Specified block is not a jump gate drive");
				return drive.GetCurrentWattageSinkInput();
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupJumpGateIDTerminalAction()
		{
			IMyTerminalControlProperty<long> property = MyAPIGateway.TerminalControls.CreateProperty<long, IMyUpgradeModule>(MyJumpGateDriveTerminal.MODID_PREFIX + "JumpGateID");
			property.Getter = (block) => {
				MyJumpGateDrive drive = MyJumpGateModSession.GetBlockAsJumpGateDrive(block);
				if (drive == null) throw new InvalidBlockTypeException("Specified block is not a jump gate drive");
				return drive.JumpGateID;
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupDriveEmitterColorTerminalAction()
		{
			IMyTerminalControlProperty<Color> property = MyAPIGateway.TerminalControls.CreateProperty<Color, IMyUpgradeModule>(MyJumpGateDriveTerminal.MODID_PREFIX + "DriveEmitterColor");
			property.Getter = (block) => {
				MyJumpGateDrive drive = MyJumpGateModSession.GetBlockAsJumpGateDrive(block);
				if (drive == null) throw new InvalidBlockTypeException("Specified block is not a jump gate drive");
				return drive.DriveEmitterColor;
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupDriveRaycastEndpointTerminalAction()
		{
			IMyTerminalControlProperty<Func<double, Vector3D>> property = MyAPIGateway.TerminalControls.CreateProperty<Func<double, Vector3D>, IMyUpgradeModule>(MyJumpGateDriveTerminal.MODID_PREFIX + "RaycastEndpoint");
			property.Getter = (block) => {
				MyJumpGateDrive drive = MyJumpGateModSession.GetBlockAsJumpGateDrive(block);
				if (drive == null) throw new InvalidBlockTypeException("Specified block is not a jump gate drive");
				return drive.GetDriveRaycastEndpoint;
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupDriveRaycastStartpointTerminalAction()
		{
			IMyTerminalControlProperty<Func<Vector3D>> property = MyAPIGateway.TerminalControls.CreateProperty<Func<Vector3D>, IMyUpgradeModule>(MyJumpGateDriveTerminal.MODID_PREFIX + "RaycastStartpoint");
			property.Getter = (block) => {
				MyJumpGateDrive drive = MyJumpGateModSession.GetBlockAsJumpGateDrive(block);
				if (drive == null) throw new InvalidBlockTypeException("Specified block is not a jump gate drive");
				return drive.GetDriveRaycastStartpoint;
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		public static void SetupBlockTerminal()
		{
			MyPBJumpGateDrive.SetupMaxRaycastDistanceTerminalAction();
			MyPBJumpGateDrive.SetupWattageSinkOverrideTerminalAction();
			MyPBJumpGateDrive.SetupCurrentWattageSinkInputTerminalAction();
			MyPBJumpGateDrive.SetupJumpGateIDTerminalAction();
			MyPBJumpGateDrive.SetupDriveEmitterColorTerminalAction();
			MyPBJumpGateDrive.SetupDriveRaycastEndpointTerminalAction();
			MyPBJumpGateDrive.SetupDriveRaycastStartpointTerminalAction();
		}
	}
}
