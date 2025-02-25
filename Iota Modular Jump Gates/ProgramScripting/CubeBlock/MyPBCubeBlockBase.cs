using IOTA.ModularJumpGates.Terminal;
using Sandbox.ModAPI.Interfaces.Terminal;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IOTA.ModularJumpGates.CubeBlock;
using IOTA.ModularJumpGates.Util;
using VRageMath;

namespace IOTA.ModularJumpGates.ProgramScripting.CubeBlock
{
	internal static class MyPBCubeBlockBase
	{
		#region Type Check Variables
		private static void SetupIsBlockBaseTerminalAction()
		{
			IMyTerminalControlProperty<bool> property = MyAPIGateway.TerminalControls.CreateProperty<bool, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "IsJumpGateBlock");
			property.Getter = MyJumpGateModSession.IsBlockCubeBlockBase;
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);

			Vector3D pos = Vector3D.Zero;
			MyAPIGateway.Session.Camera.WorldToScreen(ref pos);
		}
		private static void SetupIsControllerTerminalAction()
		{
			IMyTerminalControlProperty<bool> property = MyAPIGateway.TerminalControls.CreateProperty<bool, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "IsJumpGateController");
			property.Getter = MyJumpGateModSession.IsBlockJumpGateController;
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}
		private static void SetupIsDriveTerminalAction()
		{
			IMyTerminalControlProperty<bool> property = MyAPIGateway.TerminalControls.CreateProperty<bool, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "IsJumpGateDrive");
			property.Getter = MyJumpGateModSession.IsBlockJumpGateDrive;
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}
		private static void SetupIsCapacitorTerminalAction()
		{
			IMyTerminalControlProperty<bool> property = MyAPIGateway.TerminalControls.CreateProperty<bool, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "IsJumpGateCapacitor");
			property.Getter = MyJumpGateModSession.IsBlockJumpGateCapacitor;
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}
		#endregion

		#region Common Variables
		private static void SetupIsDirtyTerminalAction()
		{
			IMyTerminalControlProperty<bool> property = MyAPIGateway.TerminalControls.CreateProperty<bool, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "IsDirty");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return block_base.IsDirty;
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}
		private static void SetupIsMarkedForCloseTerminalAction()
		{
			IMyTerminalControlProperty<bool> property = MyAPIGateway.TerminalControls.CreateProperty<bool, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "IsMarkedForClose");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return block_base.MarkedForClose;
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}
		private static void SetupIsClosedTerminalAction()
		{
			IMyTerminalControlProperty<bool> property = MyAPIGateway.TerminalControls.CreateProperty<bool, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "IsClosed");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return block_base.IsClosed();
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}
		private static void SetupSetDirtyTerminalAction()
		{
			IMyTerminalControlProperty<Action> property = MyAPIGateway.TerminalControls.CreateProperty<Action, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "SetDirty");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return block_base.SetDirty;
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}
		private static void SetupBlockSettingsTerminalAction()
		{
			IMyTerminalControlProperty<Dictionary<string, object>> property = MyAPIGateway.TerminalControls.CreateProperty<Dictionary<string, object>, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "BlockSettings");
			property.Getter = (block) => {
				MyJumpGateController controller;
				MyJumpGateCapacitor capacitor;
				if ((controller = MyJumpGateModSession.GetBlockAsJumpGateController(block)) != null) return controller.BlockSettings.ToDictionary();
				else if ((capacitor = MyJumpGateModSession.GetBlockAsJumpGateCapacitor(block)) != null) return capacitor.BlockSettings.ToDictionary();
				else throw new InvalidBlockTypeException("Specified block is not a jump gate controller or capacitor");
			};
			property.Setter = (block, value) => {
				MyJumpGateController controller;
				MyJumpGateCapacitor capacitor;
				if ((controller = MyJumpGateModSession.GetBlockAsJumpGateController(block)) != null) controller.BlockSettings.FromDictionary(controller, value);
				else if ((capacitor = MyJumpGateModSession.GetBlockAsJumpGateCapacitor(block)) != null) capacitor.BlockSettings.FromDictionary(value);
				else throw new InvalidBlockTypeException("Specified block is not a jump gate controller or capacitor");
			};
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}
		private static void SetupConfigurationTerminalAction()
		{
			IMyTerminalControlProperty<Dictionary<string, object>> property = MyAPIGateway.TerminalControls.CreateProperty<Dictionary<string, object>, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "Configuration");
			property.Getter = (block) => {
				MyJumpGateDrive drive;
				MyJumpGateCapacitor capacitor;
				if ((drive = MyJumpGateModSession.GetBlockAsJumpGateDrive(block)) != null) return drive.DriveConfiguration.ToDictionary();
				else if ((capacitor = MyJumpGateModSession.GetBlockAsJumpGateCapacitor(block)) != null) return capacitor.CapacitorConfiguration.ToDictionary();
				else throw new InvalidBlockTypeException("Specified block is not a jump gate drive or capacitor");
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}
		#endregion

		#region Chargable Methods
		private static void SetupDrainStoredChargeTerminalAction()
		{
			IMyTerminalControlProperty<Func<double, double>> property = MyAPIGateway.TerminalControls.CreateProperty<Func<double, double>, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "DrainStoredCharge");
			property.Getter = (block) => {
				MyJumpGateDrive drive;
				MyJumpGateCapacitor capacitor;
				if ((drive = MyJumpGateModSession.GetBlockAsJumpGateDrive(block)) != null) return drive.DrainStoredCharge;
				else if ((capacitor = MyJumpGateModSession.GetBlockAsJumpGateCapacitor(block)) != null) return capacitor.DrainStoredCharge;
				else throw new InvalidBlockTypeException("Specified block is not a jump gate drive or capacitor");
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}
		private static void SetupStoredChargeTerminalAction()
		{
			IMyTerminalControlProperty<double> property = MyAPIGateway.TerminalControls.CreateProperty<double, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "StoredChargeMW");
			property.Getter = (block) => {
				MyJumpGateDrive drive;
				MyJumpGateCapacitor capacitor;
				if ((drive = MyJumpGateModSession.GetBlockAsJumpGateDrive(block)) != null) return drive.StoredChargeMW;
				else if ((capacitor = MyJumpGateModSession.GetBlockAsJumpGateCapacitor(block)) != null) return capacitor.StoredChargeMW;
				else throw new InvalidBlockTypeException("Specified block is not a jump gate drive or capacitor");
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}
		#endregion

		#region Construct Variables
		private static void SetupConstructDirtyTerminalAction()
		{
			IMyTerminalControlProperty<bool> property = MyAPIGateway.TerminalControls.CreateProperty<bool, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "IsConstructDirty");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return block_base.JumpGateGrid.IsDirty;
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructMainGridTerminalAction()
		{
			IMyTerminalControlProperty<VRage.Game.ModAPI.Ingame.IMyCubeGrid> property = MyAPIGateway.TerminalControls.CreateProperty<VRage.Game.ModAPI.Ingame.IMyCubeGrid, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "GetConstructMainGrid");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return block_base.JumpGateGrid.CubeGrid;
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructIsValidTerminalAction()
		{
			IMyTerminalControlProperty<bool> property = MyAPIGateway.TerminalControls.CreateProperty<bool, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "IsConstructValid");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return block_base.JumpGateGrid.IsValid();
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructIsStaticTerminalAction()
		{
			IMyTerminalControlProperty<bool> property = MyAPIGateway.TerminalControls.CreateProperty<bool, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "IsConstructStatic");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return block_base.JumpGateGrid.IsStatic();
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructHasCommLinkTerminalAction()
		{
			IMyTerminalControlProperty<bool> property = MyAPIGateway.TerminalControls.CreateProperty<bool, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "ConstructHasCommLink");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return block_base.JumpGateGrid.HasCommLink();
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructAtLeastOneUpdateTerminalAction()
		{
			IMyTerminalControlProperty<bool> property = MyAPIGateway.TerminalControls.CreateProperty<bool, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "ConstructHasOneUpdate");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return block_base.JumpGateGrid.AtLeastOneUpdate();
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructMassUpdateTerminalAction()
		{
			IMyTerminalControlProperty<double> property = MyAPIGateway.TerminalControls.CreateProperty<double, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "GetConstructMass");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return block_base.JumpGateGrid.ConstructMass();
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructVolumeCenterUpdateTerminalAction()
		{
			IMyTerminalControlProperty<Vector3D> property = MyAPIGateway.TerminalControls.CreateProperty<Vector3D, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "GetConstructCenterOfVolume");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return block_base.JumpGateGrid.ConstructVolumeCenter();
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructMassCenterUpdateTerminalAction()
		{
			IMyTerminalControlProperty<Vector3D> property = MyAPIGateway.TerminalControls.CreateProperty<Vector3D, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "GetConstructCenterOfMass");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return block_base.JumpGateGrid.ConstructMassCenter();
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}
		#endregion

		#region Construct Methods
		private static void SetupConstructMarkGatesTerminalAction()
		{
			IMyTerminalControlProperty<Action> property = MyAPIGateway.TerminalControls.CreateProperty<Action, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "MarkConstructGatesForUpdate");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return block_base.JumpGateGrid.MarkGatesForUpdate;
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructSetDirtyTerminalAction()
		{
			IMyTerminalControlProperty<Action> property = MyAPIGateway.TerminalControls.CreateProperty<Action, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "SetConstructDirty");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return block_base.JumpGateGrid.SetDirty;
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructSetStaticnessTerminalAction()
		{
			IMyTerminalControlProperty<Action<bool>> property = MyAPIGateway.TerminalControls.CreateProperty<Action<bool>, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "SetConstructStaticness");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return block_base.JumpGateGrid.SetConstructStaticness;
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructGetControllersTerminalAction()
		{
			IMyTerminalControlProperty<Action<List<Sandbox.ModAPI.Ingame.IMyUpgradeModule>, Func<Sandbox.ModAPI.Ingame.IMyUpgradeModule, bool>>> property = MyAPIGateway.TerminalControls.CreateProperty<Action<List<Sandbox.ModAPI.Ingame.IMyUpgradeModule>, Func<Sandbox.ModAPI.Ingame.IMyUpgradeModule, bool>>, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "GetConstructControllers");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return (blocks, filter) => block_base.JumpGateGrid.GetAttachedJumpGateControllers(blocks, (_block) => _block.TerminalBlock, (_block) => filter == null || filter(_block.TerminalBlock));
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructGetDrivesTerminalAction()
		{
			IMyTerminalControlProperty<Action<List<Sandbox.ModAPI.Ingame.IMyUpgradeModule>, Func<Sandbox.ModAPI.Ingame.IMyUpgradeModule, bool>>> property = MyAPIGateway.TerminalControls.CreateProperty<Action<List<Sandbox.ModAPI.Ingame.IMyUpgradeModule>, Func<Sandbox.ModAPI.Ingame.IMyUpgradeModule, bool>>, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "GetConstructDrives");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return (blocks, filter) => block_base.JumpGateGrid.GetAttachedJumpGateDrives(blocks, (_block) => _block.TerminalBlock, (_block) => filter == null || filter(_block.TerminalBlock));
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructGetUnassociatedDrivesTerminalAction()
		{
			IMyTerminalControlProperty<Action<List<Sandbox.ModAPI.Ingame.IMyUpgradeModule>, Func<Sandbox.ModAPI.Ingame.IMyUpgradeModule, bool>>> property = MyAPIGateway.TerminalControls.CreateProperty<Action<List<Sandbox.ModAPI.Ingame.IMyUpgradeModule>, Func<Sandbox.ModAPI.Ingame.IMyUpgradeModule, bool>>, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "GetUnassociatedConstructDrives");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return (blocks, filter) => block_base.JumpGateGrid.GetAttachedUnassociatedJumpGateDrives(blocks, (_block) => _block.TerminalBlock, (_block) => filter == null || filter(_block.TerminalBlock));
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructGetCapacitorsTerminalAction()
		{
			IMyTerminalControlProperty<Action<List<Sandbox.ModAPI.Ingame.IMyUpgradeModule>, Func<Sandbox.ModAPI.Ingame.IMyUpgradeModule, bool>>> property = MyAPIGateway.TerminalControls.CreateProperty<Action<List<Sandbox.ModAPI.Ingame.IMyUpgradeModule>, Func<Sandbox.ModAPI.Ingame.IMyUpgradeModule, bool>>, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "GetConstructCapacitors");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return (blocks, filter) => block_base.JumpGateGrid.GetAttachedJumpGateCapacitors(blocks, (_block) => _block.TerminalBlock, (_block) => filter == null || filter(_block.TerminalBlock));
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructGetLaserAntennasTerminalAction()
		{
			IMyTerminalControlProperty<Action<List<Sandbox.ModAPI.Ingame.IMyLaserAntenna>, Func<Sandbox.ModAPI.Ingame.IMyLaserAntenna, bool>>> property = MyAPIGateway.TerminalControls.CreateProperty<Action<List<Sandbox.ModAPI.Ingame.IMyLaserAntenna>, Func<Sandbox.ModAPI.Ingame.IMyLaserAntenna, bool>>, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "GetConstructLaserAntennas");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return (blocks, filter) => block_base.JumpGateGrid.GetAttachedLaserAntennas(blocks, (_block) => _block, (_block) => filter == null || filter(_block));
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructGetRadioAntennasTerminalAction()
		{
			IMyTerminalControlProperty<Action<List<Sandbox.ModAPI.Ingame.IMyRadioAntenna>, Func<Sandbox.ModAPI.Ingame.IMyRadioAntenna, bool>>> property = MyAPIGateway.TerminalControls.CreateProperty<Action<List<Sandbox.ModAPI.Ingame.IMyRadioAntenna>, Func<Sandbox.ModAPI.Ingame.IMyRadioAntenna, bool>>, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "GetConstructRadioAntennas");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return (blocks, filter) => block_base.JumpGateGrid.GetAttachedRadioAntennas(blocks, (_block) => _block, (_block) => filter == null || filter(_block));
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructGetBeaconsTerminalAction()
		{
			IMyTerminalControlProperty<Action<List<Sandbox.ModAPI.Ingame.IMyBeacon>, Func<Sandbox.ModAPI.Ingame.IMyBeacon, bool>>> property = MyAPIGateway.TerminalControls.CreateProperty<Action<List<Sandbox.ModAPI.Ingame.IMyBeacon>, Func<Sandbox.ModAPI.Ingame.IMyBeacon, bool>>, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "GetConstructBeacons");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return (blocks, filter) => block_base.JumpGateGrid.GetAttachedBeacons(blocks, (_block) => _block, (_block) => filter == null || filter(_block));
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructGetJumpGatesTerminalAction()
		{
			IMyTerminalControlProperty<Action<List<long>>> property = MyAPIGateway.TerminalControls.CreateProperty<Action<List<long>>, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "GetConstructJumpGates");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return (gates) => block_base.JumpGateGrid.GetJumpGates(gates, (gate) => gate.JumpGateID);
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructGetCubeGridsTerminalAction()
		{
			IMyTerminalControlProperty<Action<List<VRage.Game.ModAPI.Ingame.IMyCubeGrid>, Func<VRage.Game.ModAPI.Ingame.IMyCubeGrid, bool>>> property = MyAPIGateway.TerminalControls.CreateProperty<Action<List<VRage.Game.ModAPI.Ingame.IMyCubeGrid>, Func<VRage.Game.ModAPI.Ingame.IMyCubeGrid, bool>>, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "GetConstructCubeGrids");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return (grids, filter) => block_base.JumpGateGrid.GetCubeGrids(grids, (grid) => grid, (grid) => filter == null || filter(grid));
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructHasCubeGridTerminalAction()
		{
			IMyTerminalControlProperty<Func<VRage.Game.ModAPI.Ingame.IMyCubeGrid, bool>> property = MyAPIGateway.TerminalControls.CreateProperty<Func<VRage.Game.ModAPI.Ingame.IMyCubeGrid, bool>, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "DoesConstructHaveGrid");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return (grid) => block_base.JumpGateGrid.HasCubeGrid(grid.EntityId);
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructIsCommLinkedTerminalAction()
		{
			IMyTerminalControlProperty<Func<VRage.Game.ModAPI.Ingame.IMyCubeGrid, bool, bool>> property = MyAPIGateway.TerminalControls.CreateProperty<Func<VRage.Game.ModAPI.Ingame.IMyCubeGrid, bool, bool>, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "IsGridCommLinkedWithConstruct");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return (grid, update_listing) => block_base.JumpGateGrid.IsConstructCommLinked(MyJumpGateModSession.Instance.GetJumpGateGrid(grid.EntityId), update_listing);
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructIsBeaconWithinRBSTerminalAction()
		{
			IMyTerminalControlProperty<Func<Sandbox.ModAPI.Ingame.IMyBeacon, bool, bool>> property = MyAPIGateway.TerminalControls.CreateProperty<Func<Sandbox.ModAPI.Ingame.IMyBeacon, bool, bool>, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "IsBeaconWithinConstructReverseBroadcastSphere");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return (beacon, update_listing) => block_base.JumpGateGrid.IsBeaconWithinReverseBroadcastSphere(new MyBeaconLinkWrapper((IMyBeacon) beacon), update_listing);
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructControllerCountTerminalAction()
		{
			IMyTerminalControlProperty<Func<Func<Sandbox.ModAPI.Ingame.IMyUpgradeModule, bool>, int>> property = MyAPIGateway.TerminalControls.CreateProperty<Func<Func<Sandbox.ModAPI.Ingame.IMyUpgradeModule, bool>, int>, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "GetConstructControllerCount");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return (filter) => block_base.JumpGateGrid.GetControllerCount((_block) => filter == null || filter(_block.TerminalBlock));
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructDriveCountTerminalAction()
		{
			IMyTerminalControlProperty<Func<Func<Sandbox.ModAPI.Ingame.IMyUpgradeModule, bool>, int>> property = MyAPIGateway.TerminalControls.CreateProperty<Func<Func<Sandbox.ModAPI.Ingame.IMyUpgradeModule, bool>, int>, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "GetConstructDriveCount");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return (filter) => block_base.JumpGateGrid.GetDriveCount((_block) => filter == null || filter(_block.TerminalBlock));
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructCapacitorCountTerminalAction()
		{
			IMyTerminalControlProperty<Func<Func<Sandbox.ModAPI.Ingame.IMyUpgradeModule, bool>, int>> property = MyAPIGateway.TerminalControls.CreateProperty<Func<Func<Sandbox.ModAPI.Ingame.IMyUpgradeModule, bool>, int>, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "GetConstructCapacitorCount");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return (filter) => block_base.JumpGateGrid.GetCapacitorCount((_block) => filter == null || filter(_block.TerminalBlock));
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructLaserAntennaCountTerminalAction()
		{
			IMyTerminalControlProperty<Func<Func<Sandbox.ModAPI.Ingame.IMyLaserAntenna, bool>, int>> property = MyAPIGateway.TerminalControls.CreateProperty<Func<Func<Sandbox.ModAPI.Ingame.IMyLaserAntenna, bool>, int>, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "GetConstructLaserAntennaCount");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return (filter) => block_base.JumpGateGrid.GetLaserAntennaCount((_block) => filter == null || filter(_block));
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructRadioAntennaCountTerminalAction()
		{
			IMyTerminalControlProperty<Func<Func<Sandbox.ModAPI.Ingame.IMyRadioAntenna, bool>, int>> property = MyAPIGateway.TerminalControls.CreateProperty<Func<Func<Sandbox.ModAPI.Ingame.IMyRadioAntenna, bool>, int>, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "GetConstructRadioAntennaCount");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return (filter) => block_base.JumpGateGrid.GetRadioAntennaCount((_block) => filter == null || filter(_block));
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructeBeaconAntennaCountTerminalAction()
		{
			IMyTerminalControlProperty<Func<Func<Sandbox.ModAPI.Ingame.IMyBeacon, bool>, int>> property = MyAPIGateway.TerminalControls.CreateProperty<Func<Func<Sandbox.ModAPI.Ingame.IMyBeacon, bool>, int>, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "GetConstructBeaconCount");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return (filter) => block_base.JumpGateGrid.GetBeaconCount((_block) => filter == null || filter(_block));
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructCubeGridCountTerminalAction()
		{
			IMyTerminalControlProperty<Func<Func<VRage.Game.ModAPI.Ingame.IMyCubeGrid, bool>, int>> property = MyAPIGateway.TerminalControls.CreateProperty<Func<Func<VRage.Game.ModAPI.Ingame.IMyCubeGrid, bool>, int>, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "GetConstructCubeGridCount");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return (filter) => block_base.JumpGateGrid.GetCubeGridCount((grid) => filter == null || filter(grid));
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructJumpGateCountTerminalAction()
		{
			IMyTerminalControlProperty<Func<Func<long, bool>, int>> property = MyAPIGateway.TerminalControls.CreateProperty<Func<Func<long, bool>, int>, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "GetConstructJumpGateCount");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return (filter) => block_base.JumpGateGrid.GetJumpGateCount((gate) => filter == null || filter(gate.JumpGateID));
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructSyphonPowerTerminalAction()
		{
			IMyTerminalControlProperty<Func<double, double>> property = MyAPIGateway.TerminalControls.CreateProperty<Func<double, double>, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "SyphonConstructCapacitorPower");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return block_base.JumpGateGrid.SyphonConstructCapacitorPower;
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructGetLaserAntennaTerminalAction()
		{
			IMyTerminalControlProperty<Func<long, Sandbox.ModAPI.Ingame.IMyLaserAntenna>> property = MyAPIGateway.TerminalControls.CreateProperty<Func<long, Sandbox.ModAPI.Ingame.IMyLaserAntenna>, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "GetConstructLaserAntenna");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return block_base.JumpGateGrid.GetLaserAntenna;
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructGetFirstLaserAntennaTerminalAction()
		{
			IMyTerminalControlProperty<Func<Func<Sandbox.ModAPI.Ingame.IMyLaserAntenna, bool>, Sandbox.ModAPI.Ingame.IMyLaserAntenna>> property = MyAPIGateway.TerminalControls.CreateProperty<Func<Func<Sandbox.ModAPI.Ingame.IMyLaserAntenna, bool>, Sandbox.ModAPI.Ingame.IMyLaserAntenna>, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "GetConstructFirstLaserAntenna");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return (filter) => block_base.JumpGateGrid.GetFirstLaserAntenna((_block) => filter == null || filter(_block));
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructGetRadioAntennaTerminalAction()
		{
			IMyTerminalControlProperty<Func<long, Sandbox.ModAPI.Ingame.IMyRadioAntenna>> property = MyAPIGateway.TerminalControls.CreateProperty<Func<long, Sandbox.ModAPI.Ingame.IMyRadioAntenna>, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "GetConstructRadioAntenna");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return block_base.JumpGateGrid.GetRadioAntenna;
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructGetFirstRadioAntennaTerminalAction()
		{
			IMyTerminalControlProperty<Func<Func<Sandbox.ModAPI.Ingame.IMyRadioAntenna, bool>, Sandbox.ModAPI.Ingame.IMyRadioAntenna>> property = MyAPIGateway.TerminalControls.CreateProperty<Func<Func<Sandbox.ModAPI.Ingame.IMyRadioAntenna, bool>, Sandbox.ModAPI.Ingame.IMyRadioAntenna>, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "GetConstructFirstRadioAntenna");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return (filter) => block_base.JumpGateGrid.GetFirstRadioAntenna((_block) => filter == null || filter(_block));
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructGetBeaconTerminalAction()
		{
			IMyTerminalControlProperty<Func<long, Sandbox.ModAPI.Ingame.IMyBeacon>> property = MyAPIGateway.TerminalControls.CreateProperty<Func<long, Sandbox.ModAPI.Ingame.IMyBeacon>, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "GetConstructBeacon");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return block_base.JumpGateGrid.GetBeacon;
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructGetFirstBeaconTerminalAction()
		{
			IMyTerminalControlProperty<Func<Func<Sandbox.ModAPI.Ingame.IMyBeacon, bool>, Sandbox.ModAPI.Ingame.IMyBeacon>> property = MyAPIGateway.TerminalControls.CreateProperty<Func<Func<Sandbox.ModAPI.Ingame.IMyBeacon, bool>, Sandbox.ModAPI.Ingame.IMyBeacon>, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "GetConstructFirstBeacon");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return (filter) => block_base.JumpGateGrid.GetFirstBeacon((_block) => filter == null || filter(_block));
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructGetFirstJumpGateTerminalAction()
		{
			IMyTerminalControlProperty<Func<Func<long, bool>, long>> property = MyAPIGateway.TerminalControls.CreateProperty<Func<Func<long, bool>, long>, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "GetConstructFirstJumpGate");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return (filter) => block_base.JumpGateGrid.GetFirstJumpGate((_gate) => filter == null || filter(_gate.JumpGateID))?.JumpGateID ?? -1;
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructGetCapacitorTerminalAction()
		{
			IMyTerminalControlProperty<Func<long, Sandbox.ModAPI.Ingame.IMyUpgradeModule>> property = MyAPIGateway.TerminalControls.CreateProperty<Func<long, Sandbox.ModAPI.Ingame.IMyUpgradeModule>, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "GetConstructCapacitor");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return (id) => block_base.JumpGateGrid.GetCapacitor(id)?.TerminalBlock;
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructGetFirstCapacitorTerminalAction()
		{
			IMyTerminalControlProperty<Func<Func<Sandbox.ModAPI.Ingame.IMyUpgradeModule, bool>, Sandbox.ModAPI.Ingame.IMyUpgradeModule>> property = MyAPIGateway.TerminalControls.CreateProperty<Func<Func<Sandbox.ModAPI.Ingame.IMyUpgradeModule, bool>, Sandbox.ModAPI.Ingame.IMyUpgradeModule>, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "GetConstructFirstCapacitor");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return (filter) => block_base.JumpGateGrid.GetFirstCapacitor((_block) => filter == null || filter(_block.TerminalBlock))?.TerminalBlock;
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructGetDriveTerminalAction()
		{
			IMyTerminalControlProperty<Func<long, Sandbox.ModAPI.Ingame.IMyUpgradeModule>> property = MyAPIGateway.TerminalControls.CreateProperty<Func<long, Sandbox.ModAPI.Ingame.IMyUpgradeModule>, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "GetConstructDrive");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return (id) => block_base.JumpGateGrid.GetDrive(id)?.TerminalBlock;
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructGetFirstDriveTerminalAction()
		{
			IMyTerminalControlProperty<Func<Func<Sandbox.ModAPI.Ingame.IMyUpgradeModule, bool>, Sandbox.ModAPI.Ingame.IMyUpgradeModule>> property = MyAPIGateway.TerminalControls.CreateProperty<Func<Func<Sandbox.ModAPI.Ingame.IMyUpgradeModule, bool>, Sandbox.ModAPI.Ingame.IMyUpgradeModule>, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "GetConstructFirstDrive");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return (filter) => block_base.JumpGateGrid.GetFirstDrive((_block) => filter == null || filter(_block.TerminalBlock))?.TerminalBlock;
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructGetControllerTerminalAction()
		{
			IMyTerminalControlProperty<Func<long, Sandbox.ModAPI.Ingame.IMyUpgradeModule>> property = MyAPIGateway.TerminalControls.CreateProperty<Func<long, Sandbox.ModAPI.Ingame.IMyUpgradeModule>, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "GetConstructController");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return (id) => block_base.JumpGateGrid.GetController(id)?.TerminalBlock;
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructGetFirstControllerTerminalAction()
		{
			IMyTerminalControlProperty<Func<Func<Sandbox.ModAPI.Ingame.IMyUpgradeModule, bool>, Sandbox.ModAPI.Ingame.IMyUpgradeModule>> property = MyAPIGateway.TerminalControls.CreateProperty<Func<Func<Sandbox.ModAPI.Ingame.IMyUpgradeModule, bool>, Sandbox.ModAPI.Ingame.IMyUpgradeModule>, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "GetConstructFirstController");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return (filter) => block_base.JumpGateGrid.GetFirstController((_block) => filter == null || filter(_block.TerminalBlock))?.TerminalBlock;
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructGetCubeGridTerminalAction()
		{
			IMyTerminalControlProperty<Func<long, VRage.Game.ModAPI.Ingame.IMyCubeGrid>> property = MyAPIGateway.TerminalControls.CreateProperty<Func<long, VRage.Game.ModAPI.Ingame.IMyCubeGrid>, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "GetConstructCubeGrid");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return block_base.JumpGateGrid.GetCubeGrid;
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructGetFirstCubeGridTerminalAction()
		{
			IMyTerminalControlProperty<Func<Func<VRage.Game.ModAPI.Ingame.IMyCubeGrid, bool>, VRage.Game.ModAPI.Ingame.IMyCubeGrid>> property = MyAPIGateway.TerminalControls.CreateProperty<Func<Func<VRage.Game.ModAPI.Ingame.IMyCubeGrid, bool>, VRage.Game.ModAPI.Ingame.IMyCubeGrid>, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "GetConstructFirstCubeGrid");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return (filter) => block_base.JumpGateGrid.GetFirstCubeGrid((grid) => filter == null || filter(grid));
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructCalculateInstantPowerTerminalAction()
		{
			IMyTerminalControlProperty<Func<long, double>> property = MyAPIGateway.TerminalControls.CreateProperty<Func<long, double>, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "CalculateConstructGateTotalAvailableInstantPower");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return block_base.JumpGateGrid.CalculateTotalAvailableInstantPower;
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructCalculatePossibleInstantPowerTerminalAction()
		{
			IMyTerminalControlProperty<Func<long, double>> property = MyAPIGateway.TerminalControls.CreateProperty<Func<long, double>, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "CalculateConstructGateTotalPossibleInstantPower");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return block_base.JumpGateGrid.CalculateTotalPossibleInstantPower;
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupConstructCalculateMaxInstantPowerTerminalAction()
		{
			IMyTerminalControlProperty<Func<long, double>> property = MyAPIGateway.TerminalControls.CreateProperty<Func<long, double>, IMyUpgradeModule>(MyCubeBlockTerminal.MODID_PREFIX + "CalculateConstructGateTotalMaxPossibleInstantPower");
			property.Getter = (block) => {
				MyCubeBlockBase block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block);
				if (block_base == null) throw new InvalidBlockTypeException("Specified block is not a jump gate block");
				return block_base.JumpGateGrid.CalculateTotalMaxPossibleInstantPower;
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}
		#endregion

		public static void SetupBlockTerminal()
		{
			MyPBCubeBlockBase.SetupIsBlockBaseTerminalAction();
			MyPBCubeBlockBase.SetupIsControllerTerminalAction();
			MyPBCubeBlockBase.SetupIsDriveTerminalAction();
			MyPBCubeBlockBase.SetupIsCapacitorTerminalAction();

			MyPBCubeBlockBase.SetupIsDirtyTerminalAction();
			MyPBCubeBlockBase.SetupIsMarkedForCloseTerminalAction();
			MyPBCubeBlockBase.SetupIsClosedTerminalAction();
			MyPBCubeBlockBase.SetupSetDirtyTerminalAction();
			MyPBCubeBlockBase.SetupBlockSettingsTerminalAction();
			MyPBCubeBlockBase.SetupConfigurationTerminalAction();

			MyPBCubeBlockBase.SetupDrainStoredChargeTerminalAction();
			MyPBCubeBlockBase.SetupStoredChargeTerminalAction();

			MyPBCubeBlockBase.SetupConstructDirtyTerminalAction();
			MyPBCubeBlockBase.SetupConstructMainGridTerminalAction();
			MyPBCubeBlockBase.SetupConstructIsValidTerminalAction();
			MyPBCubeBlockBase.SetupConstructIsStaticTerminalAction();
			MyPBCubeBlockBase.SetupConstructHasCommLinkTerminalAction();
			MyPBCubeBlockBase.SetupConstructAtLeastOneUpdateTerminalAction();
			MyPBCubeBlockBase.SetupConstructMassUpdateTerminalAction();
			MyPBCubeBlockBase.SetupConstructVolumeCenterUpdateTerminalAction();
			MyPBCubeBlockBase.SetupConstructMassCenterUpdateTerminalAction();

			MyPBCubeBlockBase.SetupConstructMarkGatesTerminalAction();
			MyPBCubeBlockBase.SetupConstructSetDirtyTerminalAction();
			MyPBCubeBlockBase.SetupConstructSetStaticnessTerminalAction();
			MyPBCubeBlockBase.SetupConstructGetControllersTerminalAction();
			MyPBCubeBlockBase.SetupConstructGetDrivesTerminalAction();
			MyPBCubeBlockBase.SetupConstructGetUnassociatedDrivesTerminalAction();
			MyPBCubeBlockBase.SetupConstructGetCapacitorsTerminalAction();
			MyPBCubeBlockBase.SetupConstructGetLaserAntennasTerminalAction();
			MyPBCubeBlockBase.SetupConstructGetRadioAntennasTerminalAction();
			MyPBCubeBlockBase.SetupConstructGetBeaconsTerminalAction();
			MyPBCubeBlockBase.SetupConstructGetJumpGatesTerminalAction();
			MyPBCubeBlockBase.SetupConstructGetCubeGridsTerminalAction();
			MyPBCubeBlockBase.SetupConstructHasCubeGridTerminalAction();
			MyPBCubeBlockBase.SetupConstructIsCommLinkedTerminalAction();
			MyPBCubeBlockBase.SetupConstructIsBeaconWithinRBSTerminalAction();
			MyPBCubeBlockBase.SetupConstructControllerCountTerminalAction();
			MyPBCubeBlockBase.SetupConstructDriveCountTerminalAction();
			MyPBCubeBlockBase.SetupConstructCapacitorCountTerminalAction();
			MyPBCubeBlockBase.SetupConstructLaserAntennaCountTerminalAction();
			MyPBCubeBlockBase.SetupConstructRadioAntennaCountTerminalAction();
			MyPBCubeBlockBase.SetupConstructeBeaconAntennaCountTerminalAction();
			MyPBCubeBlockBase.SetupConstructCubeGridCountTerminalAction();
			MyPBCubeBlockBase.SetupConstructJumpGateCountTerminalAction();
			MyPBCubeBlockBase.SetupConstructSyphonPowerTerminalAction();
			MyPBCubeBlockBase.SetupConstructGetLaserAntennaTerminalAction();
			MyPBCubeBlockBase.SetupConstructGetFirstLaserAntennaTerminalAction();
			MyPBCubeBlockBase.SetupConstructGetRadioAntennaTerminalAction();
			MyPBCubeBlockBase.SetupConstructGetFirstRadioAntennaTerminalAction();
			MyPBCubeBlockBase.SetupConstructGetBeaconTerminalAction();
			MyPBCubeBlockBase.SetupConstructGetFirstBeaconTerminalAction();
			MyPBCubeBlockBase.SetupConstructGetFirstJumpGateTerminalAction();
			MyPBCubeBlockBase.SetupConstructGetCapacitorTerminalAction();
			MyPBCubeBlockBase.SetupConstructGetFirstCapacitorTerminalAction();
			MyPBCubeBlockBase.SetupConstructGetDriveTerminalAction();
			MyPBCubeBlockBase.SetupConstructGetFirstDriveTerminalAction();
			MyPBCubeBlockBase.SetupConstructGetControllerTerminalAction();
			MyPBCubeBlockBase.SetupConstructGetFirstControllerTerminalAction();
			MyPBCubeBlockBase.SetupConstructGetCubeGridTerminalAction();
			MyPBCubeBlockBase.SetupConstructGetFirstCubeGridTerminalAction();
			MyPBCubeBlockBase.SetupConstructCalculateInstantPowerTerminalAction();
			MyPBCubeBlockBase.SetupConstructCalculatePossibleInstantPowerTerminalAction();
			MyPBCubeBlockBase.SetupConstructCalculateMaxInstantPowerTerminalAction();
		}
	}
}
