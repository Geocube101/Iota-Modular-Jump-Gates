using IOTA.ModularJumpGates.CubeBlock;
using IOTA.ModularJumpGates.Terminal;
using IOTA.ModularJumpGates.Util;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;

namespace IOTA.ModularJumpGates.ProgramScripting.CubeBlock
{
	internal static class MyPBJumpGateController
	{
		#region Controller Actions
		private static void SetupSelectedWaypointTerminalAction()
		{
			IMyTerminalControlProperty<KeyValuePair<object, int>> property = MyAPIGateway.TerminalControls.CreateProperty<KeyValuePair<object, int>, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "SelectedWaypoint");
			property.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				MyJumpGateWaypoint waypoint = controller.BlockSettings.SelectedWaypoint();
				if (waypoint == null) return new KeyValuePair<object, int>(null, (int) MyWaypointType.NONE);
				int type = (int) waypoint.WaypointType;
				MyJumpGate target_gate;
				Vector3D? endpoint = waypoint.GetEndpoint(out target_gate);

				switch (waypoint.WaypointType)
				{
					case MyWaypointType.NONE:
						return new KeyValuePair<object, int>(null, type);
					case MyWaypointType.JUMP_GATE:
						return new KeyValuePair<object, int>(new long[] { target_gate.JumpGateGrid.CubeGridID, target_gate.JumpGateID }, type);
					case MyWaypointType.GPS:
						return new KeyValuePair<object, int>(endpoint.Value, type);
					case MyWaypointType.BEACON:
						return new KeyValuePair<object, int>(new long[] { waypoint.Beacon.Beacon.CubeGrid.EntityId, waypoint.Beacon.BeaconID }, type);
					case MyWaypointType.SERVER:
						return new KeyValuePair<object, int>(null, type);
					default:
						throw new InvalidOperationException($"Invalid jump gate waypoint type - {type} ");
				}
			};
			property.Setter = (block, value) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				MyJumpGateWaypoint waypoint = null;

				switch ((MyWaypointType) value.Value)
				{
					case MyWaypointType.NONE:
						break;
					case MyWaypointType.JUMP_GATE:
					{
						long[] buffer = (long[]) value.Key;
						if (buffer.Length != 2) throw new InvalidOperationException("Guid is malformed");
						else if (buffer[0] == 0 && buffer[1] == 0) break;
						MyJumpGateConstruct grid = MyJumpGateModSession.Instance.GetUnclosedJumpGateGrid(buffer[0]);
						MyJumpGate target = grid?.GetJumpGate(buffer[1]);
						if (target == null) throw new InvalidGuuidException($"Invalid Jump Gate UUID - {buffer[0]}/{buffer[1]}");
						waypoint = new MyJumpGateWaypoint(target);
						break;
					}
					case MyWaypointType.GPS:
					{
						IMyGps gps = MyAPIGateway.Session.GPS.Create("PB-Local", "Programmable API Temporary Waypoint", (Vector3D) value.Key, false);
						waypoint = new MyJumpGateWaypoint(gps);
						break;
					}
					case MyWaypointType.BEACON:
					{
						long[] buffer = (long[]) value.Key;
						if (buffer.Length != 2) throw new InvalidOperationException("Guid is malformed");
						else if (buffer[0] == 0 && buffer[1] == 0) break;
						MyJumpGateConstruct grid = MyJumpGateModSession.Instance.GetJumpGateGrid(buffer[0]);
						IMyBeacon beacon = grid?.GetBeacon(buffer[1]);
						if (beacon == null) throw new InvalidGuuidException($"Invalid Beacon Block ID - {buffer[0]}/{buffer[1]}");
						MyBeaconLinkWrapper wrapper = new MyBeaconLinkWrapper(beacon);
						if (controller.JumpGateGrid.IsBeaconWithinReverseBroadcastSphere(wrapper, false)) waypoint = new MyJumpGateWaypoint(wrapper);
						break;
					}
					case MyWaypointType.SERVER:
						break;
				}

				controller.BlockSettings.SelectedWaypoint(waypoint);
			};
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupIsFactionRelationValidTerminalAction()
		{
			IMyTerminalControlProperty<Func<ulong, bool>> property1 = MyAPIGateway.TerminalControls.CreateProperty<Func<ulong, bool>, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "IsSteamFactionRelationValid");
			property1.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				return controller.IsFactionRelationValid;
			};
			property1.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property1);

			IMyTerminalControlProperty<Func<long, bool>> property2 = MyAPIGateway.TerminalControls.CreateProperty<Func<long, bool>, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "IsPlayerFactionRelationValid");
			property2.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				return controller.IsFactionRelationValid;
			};
			property2.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property2);
		}

		private static void SetupAttachedJumpGateTerminalAction()
		{
			IMyTerminalControlProperty<long> property = MyAPIGateway.TerminalControls.CreateProperty<long, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "AttachedJumpGate");
			property.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				return controller.AttachedJumpGate()?.JumpGateID ?? -1;
			};
			property.Setter = (block, value) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				MyJumpGate new_gate = controller.JumpGateGrid.GetJumpGate(value);
				controller.AttachedJumpGate(new_gate);
			};
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}
		#endregion

		#region Gate Static Methods
		private static void SetupFailureDescriptionTerminalAction()
		{
			IMyTerminalControlProperty<Func<int, string>> property = MyAPIGateway.TerminalControls.CreateProperty<Func<int, string>, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "FailureDescriptionFor");
			property.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				return (reason) => MyJumpGate.GetFailureDescription((MyJumpFailReason) reason);
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupFailureSoundTerminalAction()
		{
			IMyTerminalControlProperty<Func<int, bool, string>> property = MyAPIGateway.TerminalControls.CreateProperty<Func<int, bool, string>, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "FailureSoundNameFor");
			property.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				return (reason, is_init) => MyJumpGate.GetFailureSound((MyJumpFailReason) reason, is_init);
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}
		#endregion

		#region Gate Variables
		private static void SetupGateMarkedForCloseTerminalAction()
		{
			IMyTerminalControlProperty<bool> property = MyAPIGateway.TerminalControls.CreateProperty<bool, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "GateMarkedForClose");
			property.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				MyJumpGate jump_gate = controller?.AttachedJumpGate();
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				else if (jump_gate == null) throw new InvalidOperationException("No jump gate attached");
				return jump_gate.MarkClosed;
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupGateClosedTerminalAction()
		{
			IMyTerminalControlProperty<bool> property = MyAPIGateway.TerminalControls.CreateProperty<bool, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "GateClosed");
			property.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				MyJumpGate jump_gate = controller?.AttachedJumpGate();
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				else if (jump_gate == null) throw new InvalidOperationException("No jump gate attached");
				return jump_gate.Closed;
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupGateIsDirtyTerminalAction()
		{
			IMyTerminalControlProperty<bool> property = MyAPIGateway.TerminalControls.CreateProperty<bool, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "GateDirty");
			property.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				MyJumpGate jump_gate = controller?.AttachedJumpGate();
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				else if (jump_gate == null) throw new InvalidOperationException("No jump gate attached");
				return jump_gate.IsDirty;
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupGateStatusTerminalAction()
		{
			IMyTerminalControlProperty<int> property = MyAPIGateway.TerminalControls.CreateProperty<int, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "GateStatus");
			property.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				MyJumpGate jump_gate = controller?.AttachedJumpGate();
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				else if (jump_gate == null) throw new InvalidOperationException("No jump gate attached");
				return (int) jump_gate.Status;
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupGatePhaseTerminalAction()
		{
			IMyTerminalControlProperty<int> property = MyAPIGateway.TerminalControls.CreateProperty<int, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "GatePhase");
			property.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				MyJumpGate jump_gate = controller?.AttachedJumpGate();
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				else if (jump_gate == null) throw new InvalidOperationException("No jump gate attached");
				return (int) jump_gate.Phase;
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupGateLastFailureReasonTerminalAction()
		{
			IMyTerminalControlProperty<KeyValuePair<int, bool>> property = MyAPIGateway.TerminalControls.CreateProperty<KeyValuePair<int, bool>, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "GateLastFailureReason");
			property.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				MyJumpGate jump_gate = controller?.AttachedJumpGate();
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				else if (jump_gate == null) throw new InvalidOperationException("No jump gate attached");
				KeyValuePair<int, bool> reason = new KeyValuePair<int, bool>((int) jump_gate.JumpFailureReason.Key, jump_gate.JumpFailureReason.Value);
				return reason;
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupGateLocalJumpNodeTerminalAction()
		{
			IMyTerminalControlProperty<Vector3D> property = MyAPIGateway.TerminalControls.CreateProperty<Vector3D, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "GateLocalJumpNode");
			property.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				MyJumpGate jump_gate = controller?.AttachedJumpGate();
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				else if (jump_gate == null) throw new InvalidOperationException("No jump gate attached");
				return jump_gate.LocalJumpNode;
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupGateWorldJumpNodeTerminalAction()
		{
			IMyTerminalControlProperty<Vector3D> property = MyAPIGateway.TerminalControls.CreateProperty<Vector3D, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "GateWorldJumpNode");
			property.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				MyJumpGate jump_gate = controller?.AttachedJumpGate();
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				else if (jump_gate == null) throw new InvalidOperationException("No jump gate attached");
				return jump_gate.WorldJumpNode;
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupGateTrueEndpointTerminalAction()
		{
			IMyTerminalControlProperty<Vector3D?> property = MyAPIGateway.TerminalControls.CreateProperty<Vector3D?, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "GateTrueEndpoint");
			property.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				MyJumpGate jump_gate = controller?.AttachedJumpGate();
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				else if (jump_gate == null) throw new InvalidOperationException("No jump gate attached");
				return jump_gate.TrueEndpoint;
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupGateNodeVelocityTerminalAction()
		{
			IMyTerminalControlProperty<Vector3D> property = MyAPIGateway.TerminalControls.CreateProperty<Vector3D, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "GateNodeVelocity");
			property.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				MyJumpGate jump_gate = controller?.AttachedJumpGate();
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				else if (jump_gate == null) throw new InvalidOperationException("No jump gate attached");
				return jump_gate.JumpNodeVelocity;
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupGatePrimaryPlaneTerminalAction()
		{
			IMyTerminalControlProperty<PlaneD?> property = MyAPIGateway.TerminalControls.CreateProperty<PlaneD?, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "GatePrimaryPlane");
			property.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				MyJumpGate jump_gate = controller?.AttachedJumpGate();
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				else if (jump_gate == null) throw new InvalidOperationException("No jump gate attached");
				return jump_gate.PrimaryDrivePlane;
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupGateConstructMatrixTerminalAction()
		{
			IMyTerminalControlProperty<MatrixD> property = MyAPIGateway.TerminalControls.CreateProperty<MatrixD, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "GateConstructMatrix");
			property.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				MyJumpGate jump_gate = controller?.AttachedJumpGate();
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				else if (jump_gate == null) throw new InvalidOperationException("No jump gate attached");
				return jump_gate.ConstructMatrix;
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupGateSenderGateTerminalAction()
		{
			IMyTerminalControlProperty<KeyValuePair<VRage.Game.ModAPI.Ingame.IMyCubeGrid, long>> property = MyAPIGateway.TerminalControls.CreateProperty<KeyValuePair<VRage.Game.ModAPI.Ingame.IMyCubeGrid, long>, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "GateSenderGate");
			property.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				MyJumpGate jump_gate = controller?.AttachedJumpGate();
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				else if (jump_gate == null) throw new InvalidOperationException("No jump gate attached");
				jump_gate = jump_gate.SenderGate;
				return new KeyValuePair<VRage.Game.ModAPI.Ingame.IMyCubeGrid, long>(jump_gate?.JumpGateGrid.CubeGrid, jump_gate?.JumpGateID ?? -1);
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupGateConfigurationTerminalAction()
		{
			IMyTerminalControlProperty<Dictionary<string, object>> property = MyAPIGateway.TerminalControls.CreateProperty<Dictionary<string, object>, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "GateConfiguration");
			property.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				MyJumpGate jump_gate = controller?.AttachedJumpGate();
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				else if (jump_gate == null) throw new InvalidOperationException("No jump gate attached");
				return jump_gate.JumpGateConfiguration.ToDictionary();
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupGateJumpEllipseTerminalAction()
		{
			IMyTerminalControlProperty<KeyValuePair<MatrixD, Vector3D>> property = MyAPIGateway.TerminalControls.CreateProperty<KeyValuePair<MatrixD, Vector3D>, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "GateJumpEllipse");
			property.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				MyJumpGate jump_gate = controller?.AttachedJumpGate();
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				else if (jump_gate == null) throw new InvalidOperationException("No jump gate attached");
				BoundingEllipsoidD ellipse = jump_gate.JumpEllipse;
				return new KeyValuePair<MatrixD, Vector3D>(ellipse.WorldMatrix, ellipse.Radii);
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupGateShearEllipseTerminalAction()
		{
			IMyTerminalControlProperty<KeyValuePair<MatrixD, Vector3D>> property = MyAPIGateway.TerminalControls.CreateProperty<KeyValuePair<MatrixD, Vector3D>, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "GateShearEllipse");
			property.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				MyJumpGate jump_gate = controller?.AttachedJumpGate();
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				else if (jump_gate == null) throw new InvalidOperationException("No jump gate attached");
				BoundingEllipsoidD ellipse = jump_gate.ShearEllipse;
				return new KeyValuePair<MatrixD, Vector3D>(ellipse.WorldMatrix, ellipse.Radii);
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupGateIsValidTerminalAction()
		{
			IMyTerminalControlProperty<bool> property = MyAPIGateway.TerminalControls.CreateProperty<bool, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "GateIsValid");
			property.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				MyJumpGate jump_gate = controller?.AttachedJumpGate();
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				else if (jump_gate == null) throw new InvalidOperationException("No jump gate attached");
				return jump_gate.IsValid();
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupGateIsIdleTerminalAction()
		{
			IMyTerminalControlProperty<bool> property = MyAPIGateway.TerminalControls.CreateProperty<bool, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "GateIsIdle");
			property.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				MyJumpGate jump_gate = controller?.AttachedJumpGate();
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				else if (jump_gate == null) throw new InvalidOperationException("No jump gate attached");
				return jump_gate.IsIdle();
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupGateIsJumpingTerminalAction()
		{
			IMyTerminalControlProperty<bool> property = MyAPIGateway.TerminalControls.CreateProperty<bool, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "GateIsJumping");
			property.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				MyJumpGate jump_gate = controller?.AttachedJumpGate();
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				else if (jump_gate == null) throw new InvalidOperationException("No jump gate attached");
				return jump_gate.IsJumping();
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupGateIsLargeGridTerminalAction()
		{
			IMyTerminalControlProperty<bool> property = MyAPIGateway.TerminalControls.CreateProperty<bool, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "GateIsLargeGrid");
			property.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				MyJumpGate jump_gate = controller?.AttachedJumpGate();
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				else if (jump_gate == null) throw new InvalidOperationException("No jump gate attached");
				return jump_gate.IsLargeGrid();
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupGateIsSmallGridTerminalAction()
		{
			IMyTerminalControlProperty<bool> property = MyAPIGateway.TerminalControls.CreateProperty<bool, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "GateIsSmallGrid");
			property.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				MyJumpGate jump_gate = controller?.AttachedJumpGate();
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				else if (jump_gate == null) throw new InvalidOperationException("No jump gate attached");
				return jump_gate.IsSmallGrid();
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupGateMaxReasonableDistanceTerminalAction()
		{
			IMyTerminalControlProperty<double> property = MyAPIGateway.TerminalControls.CreateProperty<double, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "GateMaxReasonableDistance");
			property.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				MyJumpGate jump_gate = controller?.AttachedJumpGate();
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				else if (jump_gate == null) throw new InvalidOperationException("No jump gate attached");
				return jump_gate.CalculateMaxGateDistance();
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupGateNodeRadiusTerminalAction()
		{
			IMyTerminalControlProperty<double> property = MyAPIGateway.TerminalControls.CreateProperty<double, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "GateNodeRadius");
			property.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				MyJumpGate jump_gate = controller?.AttachedJumpGate();
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				else if (jump_gate == null) throw new InvalidOperationException("No jump gate attached");
				return jump_gate.JumpNodeRadius();
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupGateGridSizeTerminalAction()
		{
			IMyTerminalControlProperty<MyCubeSize> property = MyAPIGateway.TerminalControls.CreateProperty<MyCubeSize, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "GateGridSize");
			property.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				MyJumpGate jump_gate = controller?.AttachedJumpGate();
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				else if (jump_gate == null) throw new InvalidOperationException("No jump gate attached");
				return jump_gate.CubeGridSize();
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupGateEffectiveEllipseTerminalAction()
		{
			IMyTerminalControlProperty<KeyValuePair<MatrixD, Vector3D>> property = MyAPIGateway.TerminalControls.CreateProperty<KeyValuePair<MatrixD, Vector3D>, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "GateEffectiveEllipse");
			property.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				MyJumpGate jump_gate = controller?.AttachedJumpGate();
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				else if (jump_gate == null) throw new InvalidOperationException("No jump gate attached");
				BoundingEllipsoidD ellipse = jump_gate.GetEffectiveJumpEllipse();
				return new KeyValuePair<MatrixD, Vector3D>(ellipse.WorldMatrix, ellipse.Radii);
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupGateNameTerminalAction()
		{
			IMyTerminalControlProperty<string> property = MyAPIGateway.TerminalControls.CreateProperty<string, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "GateName");
			property.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				MyJumpGate jump_gate = controller?.AttachedJumpGate();
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				else if (jump_gate == null) throw new InvalidOperationException("No jump gate attached");
				return jump_gate.GetName();
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupGatePrintableNameTerminalAction()
		{
			IMyTerminalControlProperty<string> property = MyAPIGateway.TerminalControls.CreateProperty<string, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "GatePrintableName");
			property.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				MyJumpGate jump_gate = controller?.AttachedJumpGate();
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				else if (jump_gate == null) throw new InvalidOperationException("No jump gate attached");
				return jump_gate.GetPrintableName();
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}
		#endregion

		#region Gate Methods
		private static void SetupGateJumpTerminalAction()
		{
			IMyTerminalControlProperty<Action> property = MyAPIGateway.TerminalControls.CreateProperty<Action, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "GateJump");
			property.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				MyJumpGate jump_gate = controller?.AttachedJumpGate();
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				else if (jump_gate == null) throw new InvalidOperationException("No jump gate attached");
				return () => jump_gate.Jump(controller.BlockSettings);
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupGateSetDirtyTerminalAction()
		{
			IMyTerminalControlProperty<Action> property = MyAPIGateway.TerminalControls.CreateProperty<Action, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "GateSetDirty");
			property.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				MyJumpGate jump_gate = controller?.AttachedJumpGate();
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				else if (jump_gate == null) throw new InvalidOperationException("No jump gate attached");
				return () => jump_gate.IsDirty = true;
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupGateCancelJumpTerminalAction()
		{
			IMyTerminalControlProperty<Action> property = MyAPIGateway.TerminalControls.CreateProperty<Action, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "GateCancelJump");
			property.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				MyJumpGate jump_gate = controller?.AttachedJumpGate();
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				else if (jump_gate == null) throw new InvalidOperationException("No jump gate attached");
				return jump_gate.CancelJump;
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupGateJumpGateDrivesTerminalAction()
		{
			IMyTerminalControlProperty<Action<List<Sandbox.ModAPI.Ingame.IMyUpgradeModule>, Func<Sandbox.ModAPI.Ingame.IMyUpgradeModule, bool>>> property = MyAPIGateway.TerminalControls.CreateProperty<Action<List<Sandbox.ModAPI.Ingame.IMyUpgradeModule>, Func<Sandbox.ModAPI.Ingame.IMyUpgradeModule, bool>>, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "GetGateDrives");
			property.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				MyJumpGate jump_gate = controller?.AttachedJumpGate();
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				else if (jump_gate == null) throw new InvalidOperationException("No jump gate attached");
				return (drives, filter) => jump_gate.GetJumpGateDrives(drives, (drive) => drive.TerminalBlock, (drive) => filter == null || filter(drive.TerminalBlock));
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupGateJumpGateWorkingDrivesTerminalAction()
		{
			IMyTerminalControlProperty<Action<List<Sandbox.ModAPI.Ingame.IMyUpgradeModule>, Func<Sandbox.ModAPI.Ingame.IMyUpgradeModule, bool>>> property = MyAPIGateway.TerminalControls.CreateProperty<Action<List<Sandbox.ModAPI.Ingame.IMyUpgradeModule>, Func<Sandbox.ModAPI.Ingame.IMyUpgradeModule, bool>>, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "GetWorkingGateDrives");
			property.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				MyJumpGate jump_gate = controller?.AttachedJumpGate();
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				else if (jump_gate == null) throw new InvalidOperationException("No jump gate attached");
				return (drives, filter) => jump_gate.GetWorkingJumpGateDrives(drives, (drive) => drive.TerminalBlock, (drive) => filter == null || filter(drive.TerminalBlock));
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupGateEntitiesTerminalAction()
		{
			IMyTerminalControlProperty<Action<Dictionary<VRage.Game.ModAPI.Ingame.IMyEntity, float>, bool>> property = MyAPIGateway.TerminalControls.CreateProperty<Action<Dictionary<VRage.Game.ModAPI.Ingame.IMyEntity, float>, bool>, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "GetGateJumpSpaceEntities");
			property.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				MyJumpGate jump_gate = controller?.AttachedJumpGate();
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				else if (jump_gate == null) throw new InvalidOperationException("No jump gate attached");
				return (entities, filtered) => {
					Dictionary<MyEntity, float> temp = new Dictionary<MyEntity, float>(entities.Count);
					jump_gate.GetEntitiesInJumpSpace(temp, filtered);
					foreach (KeyValuePair<MyEntity, float> pair in temp) entities.Add(pair.Key, pair.Value);
				};
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupGateEntityValidForJumpTerminalAction()
		{
			IMyTerminalControlProperty<Func<VRage.Game.ModAPI.Ingame.IMyEntity, bool>> property = MyAPIGateway.TerminalControls.CreateProperty<Func<VRage.Game.ModAPI.Ingame.IMyEntity, bool>, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "GateEntityValidForJump");
			property.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				MyJumpGate jump_gate = controller?.AttachedJumpGate();
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				else if (jump_gate == null) throw new InvalidOperationException("No jump gate attached");
				return (entity) => jump_gate.IsEntityValidForJumpSpace((MyEntity) entity);
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupGateCalculateDistanceRatioTerminalAction()
		{
			IMyTerminalControlProperty<Func<Vector3D, double>> property = MyAPIGateway.TerminalControls.CreateProperty<Func<Vector3D, double>, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "CalculateGateDistanceRatio");
			property.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				MyJumpGate jump_gate = controller?.AttachedJumpGate();
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				else if (jump_gate == null) throw new InvalidOperationException("No jump gate attached");
				return (endpoint) => jump_gate.CalculateDistanceRatio(ref endpoint);
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupGateCalculatePowerFactorDistanceRatioTerminalAction()
		{
			IMyTerminalControlProperty<Func<double, double>> property = MyAPIGateway.TerminalControls.CreateProperty<Func<double, double>, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "CalculateGatePowerFactor");
			property.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				MyJumpGate jump_gate = controller?.AttachedJumpGate();
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				else if (jump_gate == null) throw new InvalidOperationException("No jump gate attached");
				return (ratio) => jump_gate.CalculatePowerFactorFromDistanceRatio(ratio);
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupGateCalculateUnitPowerTerminalAction()
		{
			IMyTerminalControlProperty<Func<Vector3D, double>> property = MyAPIGateway.TerminalControls.CreateProperty<Func<Vector3D, double>, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "CalculateGateRequiredUnitPower");
			property.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				MyJumpGate jump_gate = controller?.AttachedJumpGate();
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				else if (jump_gate == null) throw new InvalidOperationException("No jump gate attached");
				return (endpoint) => jump_gate.CalculateUnitPowerRequiredForJump(ref endpoint);
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupGateCalculateTotalRequiredPowerAction()
		{
			IMyTerminalControlProperty<Func<Vector3D?, bool?, double?, double>> property = MyAPIGateway.TerminalControls.CreateProperty<Func<Vector3D?, bool?, double?, double>, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "CalculateGateTotalRequiredPower");
			property.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				MyJumpGate jump_gate = controller?.AttachedJumpGate();
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				else if (jump_gate == null) throw new InvalidOperationException("No jump gate attached");
				return jump_gate.CalculateTotalRequiredPower;
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupGateCalculateRequiredDrivesTerminalAction()
		{
			IMyTerminalControlProperty<Func<double, int>> property = MyAPIGateway.TerminalControls.CreateProperty<Func<double, int>, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "CalculateGateRequiredDrives");
			property.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				MyJumpGate jump_gate = controller?.AttachedJumpGate();
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				else if (jump_gate == null) throw new InvalidOperationException("No jump gate attached");
				return jump_gate.CalculateDrivesRequiredForDistance;
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupGateCalculateInstantPowerTerminalAction()
		{
			IMyTerminalControlProperty<Func<double>> property = MyAPIGateway.TerminalControls.CreateProperty<Func<double>, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "CalculateGateTotalAvailableInstantPower");
			property.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				MyJumpGate jump_gate = controller?.AttachedJumpGate();
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				else if (jump_gate == null) throw new InvalidOperationException("No jump gate attached");
				return jump_gate.CalculateTotalAvailableInstantPower;
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupGateCalculatePossibleInstantPowerTerminalAction()
		{
			IMyTerminalControlProperty<Func<double>> property = MyAPIGateway.TerminalControls.CreateProperty<Func<double>, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "CalculateGateTotalPossibleInstantPower");
			property.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				MyJumpGate jump_gate = controller?.AttachedJumpGate();
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				else if (jump_gate == null) throw new InvalidOperationException("No jump gate attached");
				return jump_gate.CalculateTotalPossibleInstantPower;
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupGateCalculateMaxInstantPowerTerminalAction()
		{
			IMyTerminalControlProperty<Func<double>> property = MyAPIGateway.TerminalControls.CreateProperty<Func<double>, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "CalculateGateTotalMaxPossibleInstantPower");
			property.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				MyJumpGate jump_gate = controller?.AttachedJumpGate();
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				else if (jump_gate == null) throw new InvalidOperationException("No jump gate attached");
				return jump_gate.CalculateTotalMaxPossibleInstantPower;
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}

		private static void SetupGateMatrixTerminalAction()
		{
			IMyTerminalControlProperty<Func<bool, bool, MatrixD>> property = MyAPIGateway.TerminalControls.CreateProperty<Func<bool, bool, MatrixD>, IMyUpgradeModule>(MyJumpGateControllerTerminal.MODID_PREFIX + "GateMatrix");
			property.Getter = (block) => {
				MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
				MyJumpGate jump_gate = controller?.AttachedJumpGate();
				if (controller == null) throw new InvalidBlockTypeException("Specified block is not a jump gate controller");
				else if (jump_gate == null) throw new InvalidOperationException("No jump gate attached");
				return jump_gate.GetWorldMatrix;
			};
			property.Setter = (block, value) => { throw new InvalidOperationException("Specified property is readonly"); };
			MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(property);
		}
		#endregion

		public static void SetupBlockTerminal()
		{
			MyPBJumpGateController.SetupSelectedWaypointTerminalAction();
			MyPBJumpGateController.SetupIsFactionRelationValidTerminalAction();

			MyPBJumpGateController.SetupFailureDescriptionTerminalAction();
			MyPBJumpGateController.SetupFailureSoundTerminalAction();

			MyPBJumpGateController.SetupAttachedJumpGateTerminalAction();
			MyPBJumpGateController.SetupGateMarkedForCloseTerminalAction();
			MyPBJumpGateController.SetupGateClosedTerminalAction();
			MyPBJumpGateController.SetupGateIsDirtyTerminalAction();
			MyPBJumpGateController.SetupGateStatusTerminalAction();
			MyPBJumpGateController.SetupGatePhaseTerminalAction();
			MyPBJumpGateController.SetupGateLastFailureReasonTerminalAction();
			MyPBJumpGateController.SetupGateLocalJumpNodeTerminalAction();
			MyPBJumpGateController.SetupGateWorldJumpNodeTerminalAction();
			MyPBJumpGateController.SetupGateTrueEndpointTerminalAction();
			MyPBJumpGateController.SetupGateNodeVelocityTerminalAction();
			MyPBJumpGateController.SetupGatePrimaryPlaneTerminalAction();
			MyPBJumpGateController.SetupGateConstructMatrixTerminalAction();
			MyPBJumpGateController.SetupGateSenderGateTerminalAction();
			MyPBJumpGateController.SetupGateConfigurationTerminalAction();
			MyPBJumpGateController.SetupGateJumpEllipseTerminalAction();
			MyPBJumpGateController.SetupGateShearEllipseTerminalAction();
			MyPBJumpGateController.SetupGateIsValidTerminalAction();
			MyPBJumpGateController.SetupGateIsJumpingTerminalAction();
			MyPBJumpGateController.SetupGateIsLargeGridTerminalAction();
			MyPBJumpGateController.SetupGateIsSmallGridTerminalAction();
			MyPBJumpGateController.SetupGateIsIdleTerminalAction();
			MyPBJumpGateController.SetupGateMaxReasonableDistanceTerminalAction();
			MyPBJumpGateController.SetupGateNodeRadiusTerminalAction();
			MyPBJumpGateController.SetupGateGridSizeTerminalAction();
			MyPBJumpGateController.SetupGateEffectiveEllipseTerminalAction();
			MyPBJumpGateController.SetupGateNameTerminalAction();
			MyPBJumpGateController.SetupGatePrintableNameTerminalAction();

			MyPBJumpGateController.SetupGateJumpTerminalAction();
			MyPBJumpGateController.SetupGateSetDirtyTerminalAction();
			MyPBJumpGateController.SetupGateCancelJumpTerminalAction();
			MyPBJumpGateController.SetupGateJumpGateDrivesTerminalAction();
			MyPBJumpGateController.SetupGateJumpGateWorkingDrivesTerminalAction();
			MyPBJumpGateController.SetupGateEntitiesTerminalAction();
			MyPBJumpGateController.SetupGateEntityValidForJumpTerminalAction();
			MyPBJumpGateController.SetupGateCalculateDistanceRatioTerminalAction();
			MyPBJumpGateController.SetupGateCalculatePowerFactorDistanceRatioTerminalAction();
			MyPBJumpGateController.SetupGateCalculateUnitPowerTerminalAction();
			MyPBJumpGateController.SetupGateCalculateTotalRequiredPowerAction();
			MyPBJumpGateController.SetupGateCalculateRequiredDrivesTerminalAction();
			MyPBJumpGateController.SetupGateCalculateInstantPowerTerminalAction();
			MyPBJumpGateController.SetupGateCalculatePossibleInstantPowerTerminalAction();
			MyPBJumpGateController.SetupGateCalculateMaxInstantPowerTerminalAction();
			MyPBJumpGateController.SetupGateMatrixTerminalAction();
		}
	}
}
