using IOTA.ModularJumpGates.Animation;
using IOTA.ModularJumpGates.CubeBlock;
using IOTA.ModularJumpGates.Extensions;
using IOTA.ModularJumpGates.JumpGateConstruct;
using IOTA.ModularJumpGates.Session;
using IOTA.ModularJumpGates.Util;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace IOTA.ModularJumpGates.JumpGates
{
	internal partial class MyJumpGate
	{
		#region Private Methods
		/// <summary>
		/// Executes the gate jump for multiplayer server and singleplayer sessons
		/// </summary>
		/// <param name="controller_settings">The controller settings to use for the jump</param>
		/// <param name="target_controller_settings">The controller settings of the target gate's controller</param>
		private void ExecuteJumpServer(MyJumpGateController.MyControllerBlockSettingsStruct controller_settings, MyJumpGateController.MyControllerBlockSettingsStruct target_controller_settings)
		{
			if (this.Closed) return;
			controller_settings = controller_settings ?? this.ControlObject?.BlockSettings;
			MyJumpGate target_gate = null;
			MyJumpGateAnimation gate_animation = null;
			MyJumpGateWaypoint dst = controller_settings.SelectedWaypoint();
			List<MyEntity> entities_to_jump = null;
			List<MyJumpGate> other_gates = new List<MyJumpGate>();
			Func<MyJumpGateDrive, bool> working_src_drive_filter = (drive) => drive.JumpGateID == this.JumpGateID && drive.IsWorking;
			Func<MyJumpGateDrive, bool> working_dst_drive_filter = (drive) => target_gate != null && drive.JumpGateID == target_gate.JumpGateID && drive.IsWorking;
			MyJumpFailReason result = MyJumpFailReason.NONE;
			this.JumpFailureReason = new KeyValuePair<MyJumpFailReason, bool>(MyJumpFailReason.IN_PROGRESS, true);
			this.AutoActivateStartTime = DateTime.UtcNow + new TimeSpan(0, 0, 30);
			bool is_init = true;
			Vector3D jump_node = this.WorldJumpNode;
			JumpGateUUID this_id = JumpGateUUID.FromJumpGate(this);
			BoundingEllipsoidD jump_ellipse = this.JumpEllipse;

			Action<MyJumpFailReason, bool, string> SendJumpResponse = (reason, result_status, override_message) => {
				this.Status = MyJumpGateStatus.SWITCHING;
				this.Phase = MyJumpGatePhase.RESETTING;
				this.JumpFailureReason = new KeyValuePair<MyJumpFailReason, bool>((result_status) ? MyJumpFailReason.SUCCESS : reason, is_init);
				string message = override_message ?? MyJumpGate.GetFailureDescription(this.JumpFailureReason.Key);

				if (target_gate != null)
				{
					target_gate.Status = MyJumpGateStatus.SWITCHING;
					target_gate.Phase = MyJumpGatePhase.RESETTING;
				}

				// Is server, broadcast jump results
				if (MyNetworkInterface.IsMultiplayerServer)
				{
					List<EntityWarpInfo> batch_warps = new List<EntityWarpInfo>();
					MyJumpGateModSession.Instance.GetEntityBatchWarpsForGate(this, batch_warps);

					MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet
					{
						PacketType = MyPacketTypeEnum.JUMP_GATE_JUMP,
						TargetID = 0,
						Broadcast = true,
					};

					packet.Payload(new JumpGateInfo
					{
						Type = (result_status) ? JumpGateInfo.TypeEnum.JUMP_SUCCESS : JumpGateInfo.TypeEnum.JUMP_FAIL,
						JumpType = MyJumpTypeEnum.STANDARD,
						JumpGateID = (this.JumpGateGrid == null) ? this_id : JumpGateUUID.FromJumpGate(this),
						ControllerSettings = controller_settings,
						ResultMessage = message,
						CancelOverride = this.Status == MyJumpGateStatus.CANCELLED,
						EntityBatches = this.EntityBatches?.Select((pair) => pair.Value.ToSerialized()).ToList(),
						EntityWarps = batch_warps.Select((warp) => warp.ToSerialized()).ToList(),
					});
					packet.Send();
					Logger.Debug($"[{this.JumpGateGrid.CubeGridID}-{this.JumpGateID}]: Sent network server jump response");
				}

				Action<Exception> onend = (error) => {
					gate_animation?.Clean();
					this.TrueEndpoint = null;
					this.EntityBatches?.Clear();
					if (target_gate != null && !target_gate.Closed && !target_gate.MarkClosed) target_gate.Reset();
					else if (!this.Closed) this.Reset();
				};

				if (gate_animation != null) MyJumpGateModSession.Instance.PlayAnimation(gate_animation, (result_status) ? MyJumpGateAnimation.AnimationTypeEnum.JUMPED : MyJumpGateAnimation.AnimationTypeEnum.FAILED, null, onend);
				else onend(null);
				--MyJumpGateModSession.Instance.ConcurrentJumpsCounter;
				this.SendHudMessage(message, 3000, (result_status) ? "White" : "Red");
				Logger.Debug($"[{this.JumpGateGrid?.CubeGridID ?? -1}]-{this.JumpGateID} END_JUMP {result_status}/{message}", 3);
			};

			Action<Vector3D> SendUpdatedJumpEndpoint = (new_endpoint) => {
				MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet
				{
					PacketType = MyPacketTypeEnum.UPDATE_JUMP_ENDPOINT,
					Broadcast = true,
					TargetID = 0,
				};
				packet.Payload(new KeyValuePair<JumpGateUUID, Vector3D>(this_id, new_endpoint));
				packet.Send();
			};

			try
			{
				// Check this gate
				Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} JUMP_PRECHECK", 3);
				++MyJumpGateModSession.Instance.ConcurrentJumpsCounter;

				if (!this.IsValid()) result = MyJumpFailReason.SRC_INVALID;
				else if (!this.IsControlled() || !this.Controller.IsWorking) result = MyJumpFailReason.CONTROLLER_NOT_CONNECTED;
				else if (this.JumpGateGrid.GetAttachedJumpGateDrives().Count(working_src_drive_filter) < 2) result = MyJumpFailReason.SRC_DISABLED;
				else if (this.Status == MyJumpGateStatus.NONE) result = MyJumpFailReason.SRC_NOT_CONFIGURED;
				else if (!this.IsIdle()) result = MyJumpFailReason.SRC_BUSY;
				else if (!controller_settings.CanBeInbound() && !controller_settings.CanBeOutbound()) result = MyJumpFailReason.SRC_ROUTING_DISABLED;
				else if (!controller_settings.CanBeOutbound()) result = MyJumpFailReason.SRC_INBOUND_ONLY;
				else if (dst == null || dst.WaypointType == MyWaypointType.NONE) result = MyJumpFailReason.NULL_DESTINATION;
				else if (MyJumpGateModSession.Instance.Configuration.GeneralConfiguration.MaxConcurrentJumps.Value > 0 && MyJumpGateModSession.Instance.ConcurrentJumpsCounter > MyJumpGateModSession.Instance.Configuration.GeneralConfiguration.MaxConcurrentJumps) result = MyJumpFailReason.SUBSPACE_BUSY;
				else if (MyJumpGateModSession.Instance.Configuration.JumpGateConfiguration.JumpGateExplosionConfiguration.EnableGateExplosions.Value && 1 - this.GetFunctionalDrivePercentage() > this.JumpGateConfiguration.ExplosionDamagePercent) result = MyJumpFailReason.SRC_DAMAGED;

				// Check target gate if applicable
				if (result == MyJumpFailReason.NONE && dst.WaypointType == MyWaypointType.JUMP_GATE)
				{
					MyJumpGateConstruct grid = MyJumpGateModSession.Instance.GetJumpGateGrid(dst.JumpGate.GetJumpGateGrid());
					target_gate = grid?.GetJumpGate(dst.JumpGate.GetJumpGate());
					MyJumpGateControlObject control_object = target_gate?.ControlObject;
					target_controller_settings = target_controller_settings ?? control_object?.BlockSettings;
					if (MyJumpGateModSession.Instance.Configuration.ConstructConfiguration.RequireGridCommLink.Value && !this.JumpGateGrid.IsConstructCommLinked(grid)) result = MyJumpFailReason.RADIO_LINK_FAILED;
					else if (target_gate == null || !target_gate.IsComplete() || control_object == null || !control_object.IsWorking || target_controller_settings == null) result = MyJumpFailReason.DST_UNAVAILABLE;
					else if (!target_controller_settings.CanBeInbound() && !target_controller_settings.CanBeOutbound()) result = MyJumpFailReason.DST_ROUTING_DISABLED;
					else if (!target_controller_settings.CanBeInbound()) result = MyJumpFailReason.DST_OUTBOUND_ONLY;
					else if (target_controller_settings.DoSustainedWormhole()) result = MyJumpFailReason.DST_UNAVAILABLE;
					else if (control_object != null && !control_object.IsFactionRelationValid(this.Controller.OwnerSteamID)) result = MyJumpFailReason.DST_FORBIDDEN;
					else if (target_gate.JumpGateGrid.GetAttachedJumpGateDrives().Count(working_dst_drive_filter) < 2) result = MyJumpFailReason.DST_DISABLED;
					else if (target_gate.Status == MyJumpGateStatus.NONE) result = MyJumpFailReason.DST_NOT_CONFIGURED;
					else if (!target_gate.IsIdle()) result = MyJumpFailReason.DST_BUSY;
					else if (MyJumpGateModSession.Instance.Configuration.JumpGateConfiguration.JumpGateExplosionConfiguration.EnableGateExplosions.Value && 1 - target_gate.GetFunctionalDrivePercentage() > target_gate.JumpGateConfiguration.ExplosionDamagePercent) result = MyJumpFailReason.DST_DAMAGED;
					else target_gate.ConnectAsInbound(this);
					if (result != MyJumpFailReason.NONE) target_gate = null;
				}

				// Check target beacon if applicable
				else if (result == MyJumpFailReason.NONE && dst.WaypointType == MyWaypointType.BEACON && (dst.Beacon.Beacon == null || !dst.Beacon.Beacon.IsWorking || !this.JumpGateGrid.IsBeaconWithinReverseBroadcastSphere(dst.Beacon)))
				{
					result = MyJumpFailReason.BEACON_LINK_FAILED;
				}

				// Check gate overlap
				if (result == MyJumpFailReason.NONE)
				{
					other_gates.AddRange(MyJumpGateModSession.Instance.GetAllJumpGates());

					foreach (MyJumpGate other_gate in other_gates)
					{
						if (other_gate != this && (other_gate.Status == MyJumpGateStatus.OUTBOUND || other_gate == target_gate) && jump_ellipse.Intersects(other_gate.JumpEllipse))
						{
							result = MyJumpFailReason.JUMP_SPACE_TRANSPOSED;
							break;
						}
					}

					other_gates.Clear();
				}

				// Send jump-failure if failed
				if (result != MyJumpFailReason.NONE)
				{
					SendJumpResponse(result, false, null);
					return;
				}

				// Setup endpoint
				Vector3D? _endpoint = dst.GetEndpoint();

				if (_endpoint == null)
				{
					SendJumpResponse(MyJumpFailReason.DESTINATION_UNAVAILABLE, false, null);
					return;
				}

				Vector3D endpoint = _endpoint.Value;
				Vector3D default_endpoint = endpoint;
				double distance_to_endpoint = Vector3D.Distance(endpoint, jump_node);
				this.TrueEndpoint = endpoint;

				// Setup gate animation
				string gate_animation_name = controller_settings.JumpEffectAnimationName();
				gate_animation = MyAnimationHandler.GetAnimation(gate_animation_name, MyAPIGateway.Session.Player, this, target_gate, controller_settings, target_controller_settings, MyJumpTypeEnum.STANDARD);

				if (gate_animation == null)
				{
					SendJumpResponse(MyJumpFailReason.NULL_ANIMATION, false, null);
					return;
				}

				// Final setup
				double tick_duration = 1000d / 60d;
				double time_to_jump_ms = (gate_animation == null) ? 0 : gate_animation.Durations()[0] * tick_duration;
				IMyHudNotification hud_notification = MyAPIGateway.Utilities.CreateNotification(MyTexts.GetString("Notification_JumpGate_GateActivationIn").Replace("{%0}", "--.-s"), (int) Math.Round(time_to_jump_ms), "White");
				IMyHudNotification cancel_request_notificiation = null;

				if (!MyNetworkInterface.IsDedicatedMultiplayerServer)
				{
					hud_notification.Show();
					this.ShearBlocksWarning.AliveTime = (int) Math.Round(time_to_jump_ms);
				}

				// If server, broadcast jump start
				if (MyNetworkInterface.IsMultiplayerServer)
				{
					MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet
					{
						PacketType = MyPacketTypeEnum.JUMP_GATE_JUMP,
						TargetID = 0,
						Broadcast = true,
					};

					packet.Payload(new JumpGateInfo
					{
						Type = JumpGateInfo.TypeEnum.JUMP_START,
						JumpGateID = JumpGateUUID.FromJumpGate(this),
						ControllerSettings = controller_settings,
						TargetControllerSettings = target_controller_settings,
						TrueEndpoint = endpoint,
						JumpType = MyJumpTypeEnum.STANDARD,
					});

					packet.Send();
				}

				// Update gate status
				foreach (MyJumpGateDrive drive in this.GetJumpGateDrives()) drive.InvalidatePowerCheck();
				this.Status = MyJumpGateStatus.OUTBOUND;
				this.Phase = MyJumpGatePhase.CHARGING;
				is_init = false;
				MyJumpGateModSession.Instance.RedrawAllTerminalControls();
				Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} JUMP_CHARGE, TARGET_TYPE={dst.WaypointType}, ENDPOINT={endpoint}, DISTANCE={distance_to_endpoint}", 3);

				// Play animation and check for invalidation of jump event
				MyJumpGateModSession.Instance.PlayAnimation(gate_animation, MyJumpGateAnimation.AnimationTypeEnum.JUMPING, () => {
					if (!this.IsValid()) result = MyJumpFailReason.SRC_INVALID;
					else if (!this.IsControlled() || !this.Controller.IsWorking) result = MyJumpFailReason.CONTROLLER_NOT_CONNECTED;
					else if (this.JumpGateGrid.GetAttachedJumpGateDrives().Count(working_src_drive_filter) < 2) result = MyJumpFailReason.SRC_DISABLED;
					else if (this.Status == MyJumpGateStatus.NONE) result = MyJumpFailReason.SRC_NOT_CONFIGURED;
					else if (!controller_settings.CanBeInbound() && !controller_settings.CanBeOutbound()) result = MyJumpFailReason.SRC_ROUTING_CHANGED;
					else if (!controller_settings.CanBeOutbound()) result = MyJumpFailReason.SRC_ROUTING_CHANGED;
					else if (dst == null || dst.WaypointType == MyWaypointType.NONE) result = MyJumpFailReason.NULL_DESTINATION;
					else if (target_gate != null)
					{
						MyJumpGateConstruct grid = MyJumpGateModSession.Instance.GetJumpGateGrid(dst.JumpGate.GetJumpGateGrid());
						MyJumpGateControlObject control_object = target_gate.ControlObject;
						if (MyJumpGateModSession.Instance.Configuration.ConstructConfiguration.RequireGridCommLink.Value && !this.JumpGateGrid.IsConstructCommLinked(grid)) result = MyJumpFailReason.DST_RADIO_CONNECTION_INTERRUPTED;
						else if (target_gate.Closed || control_object == null || !control_object.IsWorking) result = MyJumpFailReason.DST_UNAVAILABLE;
						else if (!target_controller_settings.CanBeInbound()) result = MyJumpFailReason.DST_ROUTING_CHANGED;
						else if (control_object != null && !control_object.IsFactionRelationValid(this.Controller.OwnerSteamID)) result = MyJumpFailReason.DST_FORBIDDEN;
						else if (target_gate.JumpGateGrid.GetAttachedJumpGateDrives().Count(working_dst_drive_filter) < 2) result = MyJumpFailReason.DST_DISABLED;
						else if (target_gate.Status == MyJumpGateStatus.NONE) result = MyJumpFailReason.DST_NOT_CONFIGURED;
						else if (target_gate.SenderGate != this) result = MyJumpFailReason.DST_VOIDED;
						else
						{
							endpoint = target_gate.WorldJumpNode;
							if (MyJumpGateModSession.Instance.Network.Registered && endpoint != this.TrueEndpoint) SendUpdatedJumpEndpoint(endpoint);
							this.TrueEndpoint = endpoint;
						}
					}
					else if (dst.WaypointType == MyWaypointType.BEACON)
					{
						if (dst.Beacon.Beacon == null || !dst.Beacon.Beacon.IsWorking || !this.JumpGateGrid.IsBeaconWithinReverseBroadcastSphere(dst.Beacon)) result = MyJumpFailReason.DST_BEACON_CONNECTION_INTERRUPTED;
						endpoint = dst.Beacon.BeaconPosition;
						if (MyJumpGateModSession.Instance.Network.Registered && endpoint != this.TrueEndpoint) SendUpdatedJumpEndpoint(endpoint);
						this.TrueEndpoint = endpoint;
					}

					if (MyJumpGateModSession.Instance.GameTick % 30 == 0)
					{
						other_gates.AddRange(MyJumpGateModSession.Instance.GetAllJumpGates());

						foreach (MyJumpGate other_gate in other_gates)
						{
							if (other_gate != this && other_gate.Status == MyJumpGateStatus.OUTBOUND && jump_ellipse.Intersects(other_gate.JumpEllipse))
							{
								result = MyJumpFailReason.JUMP_SPACE_TRANSPOSED;
								break;
							}
						}

						other_gates.Clear();
					}

					time_to_jump_ms -= tick_duration;
					hud_notification.Text = MyTexts.GetString("Notification_JumpGate_GateActivationIn").Replace("{%0}", MyJumpGateModSession.AutoconvertTimeHHMMSS(time_to_jump_ms / 1000d));
					hud_notification.Hide();
					cancel_request_notificiation?.Hide();
					Vector3D character_pos = MyAPIGateway.Session.LocalHumanPlayer?.GetPosition() ?? Vector3D.PositiveInfinity;
					bool show_notifs = character_pos != null && this.IsPointInHudBroadcastRange(ref character_pos);

					if (show_notifs)
					{
						hud_notification.Show();
						cancel_request_notificiation?.Show();
						if (this.ShearBlocks.Count == 0) this.ShearBlocksWarning?.Hide();
						else this.ShearBlocksWarning.Show();
					}

					if (this.Closed || this.JumpGateGrid == null || this.JumpGateGrid.Closed) result = MyJumpFailReason.SRC_CLOSED;
					else if (this.Status == MyJumpGateStatus.CANCELLED && gate_animation.ImmediateCancel) result = MyJumpFailReason.CANCELLED;
					else if (this.Status == MyJumpGateStatus.CANCELLED && cancel_request_notificiation == null && show_notifs)
					{
						cancel_request_notificiation = MyAPIGateway.Utilities.CreateNotification(MyTexts.GetString("Notification_JumpGate_JumpCancelling"), (int) Math.Round(time_to_jump_ms), "Red");
						cancel_request_notificiation.Show();
					}

					List<MyJumpGateDrive> working_drives = new List<MyJumpGateDrive>();
					working_drives.AddRange(this.GetWorkingJumpGateDrives());

					foreach (MyJumpGateDrive drive in working_drives)
					{
						if (drive.IsNullWrapper) continue;
						Vector3D force_dir = (drive.TerminalBlock.WorldMatrix.Translation - this.WorldJumpNode).Normalized() * this.JumpGateConfiguration.ChargingDriveEffectorForceN;
						drive.TerminalBlock.GetTopMostParent().Physics.AddForce(VRage.Game.Components.MyPhysicsForceType.APPLY_WORLD_FORCE, force_dir, drive.TerminalBlock.WorldMatrix.Translation, null);
					}

					return result == MyJumpFailReason.NONE;
				}, (Exception err) => {

					try
					{
						// Confirm no errors occured
						Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} JUMP_POST_CHECK", 3);
						if (err != null) result = MyJumpFailReason.UNKNOWN_ERROR;
						else if (this.Status == MyJumpGateStatus.CANCELLED) result = MyJumpFailReason.CANCELLED;
						hud_notification.Hide();
						cancel_request_notificiation?.Hide();
						this.ShearBlocksWarning?.Hide();

						if (result != MyJumpFailReason.NONE)
						{
							SendJumpResponse(result, false, null);
							return;
						}

						this.Phase = MyJumpGatePhase.JUMPING;
						this.LastAutojumpAttemptTime = DateTime.MinValue;
						if (target_gate != null) target_gate.Phase = MyJumpGatePhase.JUMPING;
						entities_to_jump = this.GetEntitiesInJumpSpace(true).Select((pair) => pair.Key).ToList();
						Random prng = new Random();

						if (entities_to_jump.Count == 0)
						{
							SendJumpResponse(MyJumpFailReason.NO_ENTITIES, false, null);
							return;
						}

						if (target_gate == null)
							this.JumpEntitiesToEndpoint(entities_to_jump, ref jump_node, endpoint, ref default_endpoint, dst.WaypointType, controller_settings, gate_animation, true, SendJumpResponse);
						else
							this.JumpEntitiesToEndpoint(entities_to_jump, ref jump_node, target_gate, ref default_endpoint, dst.WaypointType, controller_settings, gate_animation, true, SendJumpResponse);
					}
					catch (Exception e)
					{
						SendJumpResponse(MyJumpFailReason.UNKNOWN_ERROR, false, null);
						int drive_count = this.GetJumpGateDrives().Count();
						int working_drive_count = this.GetWorkingJumpGateDrives().Count();
						Logger.Error($"Error during jump gate jump:\n\tPHASE=POSTANIM\n\tGATE_CUBE_GRID={this.JumpGateGrid?.CubeGrid?.EntityId.ToString() ?? "N/A"}\n\tGATE_ID={this.JumpGateID}\n\tGATE_SIZE={drive_count}\n\tGATE_SIZE_WORKING={working_drive_count}\nMESSAGE={e.Message}\n\tSTACKTRACE:\n{e.StackTrace}\n\tINNER:\n{e.InnerException}");
					}

				});
			}
			catch (Exception e)
			{
				SendJumpResponse(MyJumpFailReason.UNKNOWN_ERROR, false, null);
				int drive_count = this.GetJumpGateDrives().Count();
				int working_drive_count = this.GetWorkingJumpGateDrives().Count();
				Logger.Error($"Error during jump gate jump:\n\tPHASE=PREANIM\n\tGATE_CUBE_GRID={this.JumpGateGrid?.CubeGrid?.EntityId.ToString() ?? "N/A"}\n\tGATE_ID={this.JumpGateID}\n\tGATE_SIZE={drive_count}\n\tGATE_SIZE_WORKING={working_drive_count}\nMESSAGE={e.Message}\n\tSTACKTRACE:\n{e.StackTrace}\n\tINNER:\n{e.InnerException}");
			}
		}

		/// <summary>
		/// Executes the gate wormhole jump for multiplayer server and singleplayer sessons
		/// </summary>
		/// <param name="controller_settings">The controller settings to use for the jump</param>
		/// <param name="target_controller_settings">The controller settings of the target gate's controller</param>
		private void ExecuteWormholeServer(MyJumpGateController.MyControllerBlockSettingsStruct controller_settings, MyJumpGateController.MyControllerBlockSettingsStruct target_controller_settings)
		{
			if (this.Closed) return;
			controller_settings = controller_settings ?? this.ControlObject?.BlockSettings;
			MyJumpGate target_gate = null;
			MyJumpGateAnimation gate_animation = null;
			MyJumpGateWaypoint dst = controller_settings.SelectedWaypoint();
			List<MyJumpGate> other_gates = new List<MyJumpGate>();
			Func<MyJumpGateDrive, bool> working_src_drive_filter = (drive) => drive.JumpGateID == this.JumpGateID && drive.IsWorking;
			Func<MyJumpGateDrive, bool> working_dst_drive_filter = (drive) => target_gate != null && drive.JumpGateID == target_gate.JumpGateID && drive.IsWorking;
			MyJumpFailReason result = MyJumpFailReason.NONE;
			this.JumpFailureReason = new KeyValuePair<MyJumpFailReason, bool>(MyJumpFailReason.IN_PROGRESS, true);
			this.AutoActivateStartTime = DateTime.UtcNow + new TimeSpan(0, 0, 30);
			bool is_init = true;
			Vector3D jump_node = this.WorldJumpNode;
			JumpGateUUID this_id = JumpGateUUID.FromJumpGate(this);
			BoundingEllipsoidD jump_ellipse = this.JumpEllipse;
			BoundingEllipsoidD target_ellipse;

			Action<MyJumpFailReason, bool, string> SendWormholeJumpResponse = (reason, result_status, override_message) => {
				string message = override_message ?? MyJumpGate.GetFailureDescription(reason);

				if (result_status)
				{
					DateTime now = DateTime.UtcNow;
					this.Status = MyJumpGateStatus.OUTBOUND;
					this.Phase = MyJumpGatePhase.JUMPING;
					this.WormholeStartTime = now;
					target_gate.Status = MyJumpGateStatus.INBOUND;
					target_gate.Phase = MyJumpGatePhase.JUMPING;
					target_gate.WormholeStartTime = now;
					++MyJumpGateModSession.Instance.ConcurrentJumpsCounter;
					Logger.Debug($"[{this.JumpGateGrid?.CubeGridID ?? -1}]-{this.JumpGateID} BEGIN_WORMHOLE_LOOP", 3);
				}
				else
				{
					this.Status = MyJumpGateStatus.SWITCHING;
					this.Phase = MyJumpGatePhase.RESETTING;
					this.JumpFailureReason = new KeyValuePair<MyJumpFailReason, bool>(reason, is_init);
					this.WormholeStartTime = null;

					if (target_gate != null)
					{
						target_gate.Status = MyJumpGateStatus.SWITCHING;
						target_gate.Phase = MyJumpGatePhase.RESETTING;
						target_gate.WormholeStartTime = null;
					}

					Action<Exception> onend = (error) => {
						gate_animation?.Clean();
						this.TrueEndpoint = null;
						this.EntityBatches?.Clear();

						if (target_gate != null && !target_gate.Closed && !target_gate.MarkClosed)
						{
							target_gate.Reset();
							target_gate.SetDirty();
						}

						if (!this.Closed)
						{
							this.Reset();
							this.SetDirty();
						}
					};

					if (gate_animation != null) MyJumpGateModSession.Instance.PlayAnimation(gate_animation, MyJumpGateAnimation.AnimationTypeEnum.WORMHOLE_CLOSE, null, onend);
					else onend(null);
					--MyJumpGateModSession.Instance.ConcurrentJumpsCounter;
					this.SendHudMessage(message, 3000, "Red");
					Logger.Debug($"[{this.JumpGateGrid?.CubeGridID ?? -1}]-{this.JumpGateID} END_WORMHOLE_JUMP {result_status}/{message}", 3);
				}

				// Is server, broadcast jump results
				if (MyNetworkInterface.IsMultiplayerServer)
				{
					MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet
					{
						PacketType = MyPacketTypeEnum.JUMP_GATE_JUMP,
						TargetID = 0,
						Broadcast = true,
					};

					packet.Payload(new JumpGateInfo
					{
						Type = (result_status) ? JumpGateInfo.TypeEnum.WORMHOLE_OPEN : JumpGateInfo.TypeEnum.WORMHOLE_FAIL,
						JumpType = MyJumpTypeEnum.STANDARD,
						JumpGateID = (this.JumpGateGrid == null) ? this_id : JumpGateUUID.FromJumpGate(this),
						ResultMessage = message,
						CancelOverride = this.Status == MyJumpGateStatus.CANCELLED,
						TrueEndpoint = this.TrueEndpoint,
					});
					packet.Send();
					Logger.Debug($"[{this.JumpGateGrid.CubeGridID}-{this.JumpGateID}]: Sent network server wormhole jump response");
				}
			};

			Action<MyJumpFailReason, bool, string, IEnumerable<MyEntity>, bool> SendJumpResponse = (reason, result_status, override_message, active_entity_batch, is_anti_gate) => {
				string message = override_message ?? MyJumpGate.GetFailureDescription(reason);
				this.SendHudMessage(message, 3000, (result_status) ? "White" : "Red");

				// Is server, broadcast jump results
				if (MyNetworkInterface.IsMultiplayerServer)
				{
					List<EntityWarpInfo> batch_warps = new List<EntityWarpInfo>();
					MyJumpGateModSession.Instance.GetEntityBatchWarpsForGate((is_anti_gate) ? target_gate : this, batch_warps);

					MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet {
						PacketType = MyPacketTypeEnum.JUMP_GATE_JUMP,
						TargetID = 0,
						Broadcast = true,
					};

					packet.Payload(new JumpGateInfo {
						Type = (is_anti_gate) ? ((result_status) ? JumpGateInfo.TypeEnum.WORMHOLE_ANTI_ENTITY_JUMP_SUCCESS : JumpGateInfo.TypeEnum.WORMHOLE_ANTI_ENTITY_JUMP_FAIL) : ((result_status) ? JumpGateInfo.TypeEnum.WORMHOLE_ENTITY_JUMP_SUCCESS : JumpGateInfo.TypeEnum.WORMHOLE_ENTITY_JUMP_FAIL),
						JumpType = MyJumpTypeEnum.STANDARD,
						JumpGateID = (this.JumpGateGrid == null) ? this_id : JumpGateUUID.FromJumpGate(this),
						ResultMessage = message,
						CancelOverride = this.Status == MyJumpGateStatus.CANCELLED,
						EntityBatches = this.EntityBatches?.Select((pair) => pair.Value.ToSerialized()).ToList(),
						EntityWarps = batch_warps.Select((warp) => warp.ToSerialized()).ToList(),
						EntityIds = active_entity_batch?.Select((entity) => entity.EntityId)?.ToList(),
					});
					packet.Send();
					Logger.Debug($"[{this.JumpGateGrid.CubeGridID}-{this.JumpGateID}]: Sent network server wormhole jump response");
				}
			};

			Action<Vector3D> SendUpdatedJumpEndpoint = (new_endpoint) => {
				MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet
				{
					PacketType = MyPacketTypeEnum.UPDATE_JUMP_ENDPOINT,
					Broadcast = true,
					TargetID = 0,
				};
				packet.Payload(new KeyValuePair<JumpGateUUID, Vector3D>(this_id, new_endpoint));
				packet.Send();
			};

			try
			{
				// Check this gate
				Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} WORMHOLE_JUMP_PRECHECK", 3);
				++MyJumpGateModSession.Instance.ConcurrentJumpsCounter;

				if (!this.JumpGateConfiguration.AllowWormholeActivation) result = MyJumpFailReason.DESTINATION_UNAVAILABLE;
				else if (!this.IsValid()) result = MyJumpFailReason.SRC_INVALID;
				else if (!this.IsControlled() || !this.Controller.IsWorking) result = MyJumpFailReason.CONTROLLER_NOT_CONNECTED;
				else if (this.JumpGateGrid.GetAttachedJumpGateDrives().Count(working_src_drive_filter) < 2) result = MyJumpFailReason.SRC_DISABLED;
				else if (this.Status == MyJumpGateStatus.NONE) result = MyJumpFailReason.SRC_NOT_CONFIGURED;
				else if (!this.IsIdle()) result = MyJumpFailReason.SRC_BUSY;
				else if (!controller_settings.CanBeInbound() && !controller_settings.CanBeOutbound()) result = MyJumpFailReason.SRC_ROUTING_DISABLED;
				else if (!controller_settings.CanBeOutbound()) result = MyJumpFailReason.SRC_INBOUND_ONLY;
				else if (dst == null || dst.WaypointType != MyWaypointType.JUMP_GATE) result = MyJumpFailReason.NULL_DESTINATION;
				else if (MyJumpGateModSession.Instance.Configuration.GeneralConfiguration.MaxConcurrentJumps.Value > 0 && MyJumpGateModSession.Instance.ConcurrentJumpsCounter > MyJumpGateModSession.Instance.Configuration.GeneralConfiguration.MaxConcurrentJumps.Value) result = MyJumpFailReason.SUBSPACE_BUSY;
				else if (MyJumpGateModSession.Instance.Configuration.JumpGateConfiguration.JumpGateExplosionConfiguration.EnableGateExplosions.Value && 1 - this.GetFunctionalDrivePercentage() > this.JumpGateConfiguration.ExplosionDamagePercent) result = MyJumpFailReason.SRC_DAMAGED;

				// Send jump-failure if failed
				if (result != MyJumpFailReason.NONE)
				{
					SendWormholeJumpResponse(result, false, null);
					return;
				}

				// Check target gate
				MyJumpGateConstruct grid = MyJumpGateModSession.Instance.GetJumpGateGrid(dst.JumpGate.GetJumpGateGrid());
				target_gate = grid?.GetJumpGate(dst.JumpGate.GetJumpGate());
				MyJumpGateControlObject control_object = target_gate?.ControlObject;
				target_controller_settings = target_controller_settings ?? control_object?.BlockSettings;

				if (MyJumpGateModSession.Instance.Configuration.ConstructConfiguration.RequireGridCommLink.Value && !this.JumpGateGrid.IsConstructCommLinked(grid)) result = MyJumpFailReason.RADIO_LINK_FAILED;
				else if (target_gate == null || !target_gate.IsComplete() || control_object == null || !control_object.IsWorking || target_controller_settings == null) result = MyJumpFailReason.DST_UNAVAILABLE;
				else if (!target_controller_settings.CanBeInbound() && !target_controller_settings.CanBeOutbound()) result = MyJumpFailReason.DST_ROUTING_DISABLED;
				else if (!target_controller_settings.CanBeInbound()) result = MyJumpFailReason.DST_OUTBOUND_ONLY;
				else if (!target_controller_settings.DoSustainedWormhole()) result = MyJumpFailReason.DESTINATION_UNAVAILABLE;
				else if (control_object != null && !control_object.IsFactionRelationValid(this.Controller.OwnerSteamID)) result = MyJumpFailReason.DST_FORBIDDEN;
				else if (target_gate.JumpGateGrid.GetAttachedJumpGateDrives().Count(working_dst_drive_filter) < 2) result = MyJumpFailReason.DST_DISABLED;
				else if (target_gate.Status == MyJumpGateStatus.NONE) result = MyJumpFailReason.DST_NOT_CONFIGURED;
				else if (!target_gate.IsIdle()) result = MyJumpFailReason.DST_BUSY;
				else if (MyJumpGateModSession.Instance.Configuration.JumpGateConfiguration.JumpGateExplosionConfiguration.EnableGateExplosions.Value && 1 - target_gate.GetFunctionalDrivePercentage() > target_gate.JumpGateConfiguration.ExplosionDamagePercent) result = MyJumpFailReason.DST_DAMAGED;
				else target_gate.ConnectAsInbound(this);

				// Check gate overlap
				if (result == MyJumpFailReason.NONE)
				{
					other_gates.AddRange(MyJumpGateModSession.Instance.GetAllJumpGates());

					foreach (MyJumpGate other_gate in other_gates)
					{
						if (other_gate != this && (other_gate.Status == MyJumpGateStatus.OUTBOUND || other_gate == target_gate) && jump_ellipse.Intersects(other_gate.JumpEllipse))
						{
							result = MyJumpFailReason.JUMP_SPACE_TRANSPOSED;
							break;
						}
					}

					other_gates.Clear();
				}

				// Send jump-failure if failed
				if (result != MyJumpFailReason.NONE)
				{
					SendWormholeJumpResponse(result, false, null);
					return;
				}

				// Setup endpoint
				Vector3D? _endpoint = dst.GetEndpoint();

				if (_endpoint == null)
				{
					SendWormholeJumpResponse(MyJumpFailReason.DESTINATION_UNAVAILABLE, false, null);
					return;
				}

				Vector3D endpoint = _endpoint.Value;
				Vector3D default_endpoint = endpoint;
				double distance_to_endpoint = Vector3D.Distance(endpoint, jump_node);
				this.TrueEndpoint = endpoint;

				// Setup gate animation
				string gate_animation_name = controller_settings.JumpEffectAnimationName();
				gate_animation = MyAnimationHandler.GetAnimation(gate_animation_name, MyAPIGateway.Session.Player, this, target_gate, controller_settings, target_controller_settings, MyJumpTypeEnum.STANDARD);

				if (gate_animation == null)
				{
					SendWormholeJumpResponse(MyJumpFailReason.NULL_ANIMATION, false, null);
					return;
				}

				// Final setup
				double tick_duration = 1000d / 60d;
				double time_to_jump_ms = gate_animation.Durations()[3] * tick_duration;
				IMyHudNotification hud_notification = MyAPIGateway.Utilities.CreateNotification(MyTexts.GetString("Notification_JumpGate_GateActivationIn").Replace("{%0}", "--.-s"), (int) Math.Round(time_to_jump_ms), "White");
				IMyHudNotification cancel_request_notificiation = null;

				if (!MyNetworkInterface.IsDedicatedMultiplayerServer)
				{
					hud_notification.Show();
					this.ShearBlocksWarning.AliveTime = (int) Math.Round(time_to_jump_ms);
				}

				// If server, broadcast jump start
				if (MyNetworkInterface.IsMultiplayerServer)
				{
					MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet
					{
						PacketType = MyPacketTypeEnum.JUMP_GATE_JUMP,
						TargetID = 0,
						Broadcast = true,
					};

					packet.Payload(new JumpGateInfo
					{
						Type = JumpGateInfo.TypeEnum.WORMHOLE_START,
						JumpGateID = JumpGateUUID.FromJumpGate(this),
						ControllerSettings = controller_settings,
						TargetControllerSettings = target_controller_settings,
						TrueEndpoint = endpoint,
						JumpType = MyJumpTypeEnum.STANDARD,
					});

					packet.Send();
				}

				// Update gate status
				foreach (MyJumpGateDrive drive in this.GetJumpGateDrives()) drive.InvalidatePowerCheck();
				this.Status = MyJumpGateStatus.OUTBOUND;
				this.Phase = MyJumpGatePhase.CHARGING;
				is_init = false;
				MyJumpGateModSession.Instance.RedrawAllTerminalControls();
				Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} WORMHOLE_JUMP_CHARGE, TARGET_TYPE={dst.WaypointType}, ENDPOINT={endpoint}, DISTANCE={distance_to_endpoint}", 3);

				// Play animation and check for invalidation of jump event
				MyJumpGateModSession.Instance.PlayAnimation(gate_animation, MyJumpGateAnimation.AnimationTypeEnum.WORMHOLE_OPEN, () => {
					time_to_jump_ms -= tick_duration;
					hud_notification.Text = MyTexts.GetString("Notification_JumpGate_GateActivationIn").Replace("{%0}", MyJumpGateModSession.AutoconvertTimeHHMMSS(time_to_jump_ms / 1000d));
					hud_notification.Hide();
					cancel_request_notificiation?.Hide();
					Vector3D character_pos = MyAPIGateway.Session.LocalHumanPlayer?.GetPosition() ?? Vector3D.PositiveInfinity;
					bool show_notifs = character_pos != null && this.IsPointInHudBroadcastRange(ref character_pos);

					if (show_notifs)
					{
						hud_notification.Show();
						cancel_request_notificiation?.Show();
						if (this.ShearBlocks.Count == 0) this.ShearBlocksWarning?.Hide();
						else this.ShearBlocksWarning.Show();
					}

					if (this.Closed || this.JumpGateGrid == null || this.JumpGateGrid.Closed) result = MyJumpFailReason.SRC_CLOSED;
					else if (this.Status == MyJumpGateStatus.CANCELLED && gate_animation.ImmediateCancel) result = MyJumpFailReason.CANCELLED;
					else if (this.Status == MyJumpGateStatus.CANCELLED && cancel_request_notificiation == null && show_notifs)
					{
						cancel_request_notificiation = MyAPIGateway.Utilities.CreateNotification(MyTexts.GetString("Notification_JumpGate_JumpCancelling"), (int) Math.Round(time_to_jump_ms), "Red");
						cancel_request_notificiation.Show();
					}

					List<MyJumpGateDrive> working_drives = new List<MyJumpGateDrive>();
					working_drives.AddRange(this.GetWorkingJumpGateDrives());

					foreach (MyJumpGateDrive drive in working_drives)
					{
						if (drive.IsNullWrapper) continue;
						Vector3D force_dir = (drive.TerminalBlock.WorldMatrix.Translation - this.WorldJumpNode).Normalized() * this.JumpGateConfiguration.ChargingDriveEffectorForceN;
						drive.TerminalBlock.GetTopMostParent().Physics.AddForce(VRage.Game.Components.MyPhysicsForceType.APPLY_WORLD_FORCE, force_dir, drive.TerminalBlock.WorldMatrix.Translation, null);
					}

					return result == MyJumpFailReason.NONE;
				}, (Exception err) => {

					try
					{
						// Confirm no errors occured
						Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} WORMHOLE_JUMP_POST_CHECK", 3);
						if (err != null) result = MyJumpFailReason.UNKNOWN_ERROR;
						else if (this.Status == MyJumpGateStatus.CANCELLED) result = MyJumpFailReason.CANCELLED;
						hud_notification.Hide();
						cancel_request_notificiation?.Hide();
						this.ShearBlocksWarning?.Hide();

						if (result != MyJumpFailReason.NONE)
						{
							SendWormholeJumpResponse(result, false, null);
							return;
						}
						else if (dst.WaypointType == MyWaypointType.SERVER)
						{
							SendWormholeJumpResponse(MyJumpFailReason.CROSS_SERVER_JUMP, false, null);
							return;
						}

						SendWormholeJumpResponse(MyJumpFailReason.SUCCESS, true, null);
						ulong wormhole_tick = 0;
						float max_wormhole_duration = controller_settings.GateWormholeAutoCloseTime();
						max_wormhole_duration = (max_wormhole_duration == 0) ? this.JumpGateConfiguration.MaxWormholeDurationSeconds : Math.Min(this.JumpGateConfiguration.MaxWormholeDurationSeconds, max_wormhole_duration);
						List<MyEntity> src_entities_to_jump = new List<MyEntity>();
						List<MyEntity> dst_entities_to_jump = new List<MyEntity>();
						List<int> closed_src_jump_entities = new List<int>();
						List<int> closed_dst_jump_entities = new List<int>();
						List<KeyValuePair<List<MyEntity>, MyJumpGateAnimation>> src_jump_entities = new List<KeyValuePair<List<MyEntity>, MyJumpGateAnimation>>();
						List<KeyValuePair<List<MyEntity>, MyJumpGateAnimation>> dst_jump_entities = new List<KeyValuePair<List<MyEntity>, MyJumpGateAnimation>>();
						List<KeyValuePair<List<MyEntity>, MyJumpGateAnimation>> entity_animation_batches = new List<KeyValuePair<List<MyEntity>, MyJumpGateAnimation>>();
						MyParticleEffect energy_release_effect;
						MyEntity3DSoundEmitter energy_release_sound = new MyEntity3DSoundEmitter(target_gate.JumpSpaceCollisionDetector);
						MatrixD energy_release_matrix = ParticleOrientationDef.GetJumpGateMatrix(this, target_gate, true, ref endpoint, null);
						MyParticlesManager.TryCreateParticleEffect("IOTA.WormholeEnergyRelease", ref energy_release_matrix, ref jump_node, uint.MaxValue, out energy_release_effect);
						energy_release_effect?.StopEmitting();
						energy_release_effect?.StopLights();
						Vector3D remote_gravity_direction = MyAPIGateway.GravityProviderSystem.CalculateNaturalGravityInPoint(target_gate.WorldJumpNode);
						Vector3D this_gravity_direction = MyAPIGateway.GravityProviderSystem.CalculateNaturalGravityInPoint(this.WorldJumpNode);
						const double gravity = 9.80665;
						double remote_gravity_strength = remote_gravity_direction.Length() / gravity * this.JumpGateConfiguration.GravityPassthroughMultiplier;
						double this_gravity_strength = this_gravity_direction.Length() / gravity * target_gate.JumpGateConfiguration.GravityPassthroughMultiplier;
						double lower_gravity_limit = 0.05;
						double r1, r2;
						float energy_buffer = 0;
						float last_energy_buffer = 0;
						float energy_jump_cutoff_min = 1e4f;
						float energy_jump_cutoff_max = 1e6f;
						float energy_buffer_dissipation_percent = 0.1f;

						IMySphericalNaturalGravityComponent remote_gravity, this_gravity;
						r1 = jump_ellipse.Radii.Max();
						r2 = r1 / Math.Sqrt(lower_gravity_limit / remote_gravity_strength);
						remote_gravity = MyAPIGateway.GravityProviderSystem.AddNaturalGravityToEntity(this.JumpSpaceCollisionDetector, r1, r2, 2, remote_gravity_strength);

						r1 = target_gate.JumpEllipse.Radii.Max();
						r2 = r1 / Math.Sqrt(lower_gravity_limit / this_gravity_strength);
						this_gravity = MyAPIGateway.GravityProviderSystem.AddNaturalGravityToEntity(target_gate.JumpSpaceCollisionDetector, r1, r2, 2, this_gravity_strength);

						Action<float> UpdateEnergyReleaseEffects = (float alpha) => {
							if (!MyNetworkInterface.IsDedicatedMultiplayerServer && alpha > 0.1f)
							{
								if (energy_release_effect.IsEmittingStopped) energy_release_effect.Play();
								if (!energy_release_sound.IsPlaying) energy_release_sound.PlaySound(new MySoundPair("IOTA.EnergyRelease"));
								energy_release_effect.UserScale = (float) jump_ellipse.Radii.Max();
								energy_release_effect.UserColorMultiplier = new Vector4(1f, 1f, 1f, alpha);
								energy_release_effect.WorldMatrix = ParticleOrientationDef.GetJumpGateMatrix(this, target_gate, true, ref endpoint, null);
								energy_release_sound.VolumeMultiplier = alpha;
							}
							else if (!MyNetworkInterface.IsDedicatedMultiplayerServer)
							{
								if (!energy_release_effect.IsEmittingStopped)
								{
									energy_release_effect.StopEmitting();
									energy_release_effect.StopLights();
								}

								if (energy_release_sound.IsPlaying)
								{
									energy_release_sound.StopSound(true);
								}
							}
						};

						MyJumpGateModSession.Instance.PlayAnimation(gate_animation, MyJumpGateAnimation.AnimationTypeEnum.WORMHOLE_LOOP, () => {
							if (wormhole_tick++ % 15 != 0) return true;

							// Check validity
							{
								grid = MyJumpGateModSession.Instance.GetJumpGateGrid(dst.JumpGate.GetJumpGateGrid());
								control_object = target_gate.ControlObject;
								result = MyJumpFailReason.NONE;

								if (!this.IsValid()) result = MyJumpFailReason.SRC_INVALID;
								else if (!this.IsControlled() || !this.Controller.IsWorking) result = MyJumpFailReason.CONTROLLER_NOT_CONNECTED;
								else if (!this.JumpGateGrid.GetAttachedJumpGateDrives().Where(working_src_drive_filter).AtLeast(2)) result = MyJumpFailReason.SRC_DISABLED;
								else if (this.Status == MyJumpGateStatus.NONE) result = MyJumpFailReason.SRC_NOT_CONFIGURED;
								else if (this.Status == MyJumpGateStatus.CANCELLED) result = MyJumpFailReason.CANCELLED;
								else if (!controller_settings.CanBeInbound() && !controller_settings.CanBeOutbound()) result = MyJumpFailReason.SRC_ROUTING_CHANGED;
								else if (!controller_settings.CanBeOutbound()) result = MyJumpFailReason.SRC_ROUTING_CHANGED;
								else if (dst == null || dst.WaypointType == MyWaypointType.NONE) result = MyJumpFailReason.NULL_DESTINATION;
								else if (MyJumpGateModSession.Instance.Configuration.ConstructConfiguration.RequireGridCommLink.Value && !this.JumpGateGrid.IsConstructCommLinked(grid)) result = MyJumpFailReason.DST_RADIO_CONNECTION_INTERRUPTED;
								else if (target_gate.Closed || control_object == null || !control_object.IsWorking) result = MyJumpFailReason.DST_UNAVAILABLE;
								else if (!target_controller_settings.CanBeInbound()) result = MyJumpFailReason.DST_ROUTING_CHANGED;
								else if (control_object != null && !control_object.IsFactionRelationValid(this.Controller.OwnerSteamID)) result = MyJumpFailReason.DST_FORBIDDEN;
								else if (!target_gate.JumpGateGrid.GetAttachedJumpGateDrives().Where(working_dst_drive_filter).AtLeast(2)) result = MyJumpFailReason.DST_DISABLED;
								else if (target_gate.Status == MyJumpGateStatus.NONE) result = MyJumpFailReason.DST_NOT_CONFIGURED;
								else if (target_gate.SenderGate != this) result = MyJumpFailReason.DST_VOIDED;
								else if (!controller_settings.DoAdminWormholeBypass() && this.JumpGateConfiguration.OverrunPowerMultiplierPerSecond == 0 && (DateTime.UtcNow - this.WormholeStartTime.Value).TotalSeconds >= max_wormhole_duration) result = MyJumpFailReason.WORMHOLE_TIMEOUT;
								else
								{
									jump_ellipse = this.JumpEllipse;
									target_ellipse = target_gate.JumpEllipse;
									endpoint = target_ellipse.WorldMatrix.Translation;
									jump_node = jump_ellipse.WorldMatrix.Translation;
									if (MyJumpGateModSession.Instance.Network.Registered && endpoint != this.TrueEndpoint) SendUpdatedJumpEndpoint(endpoint);
									this.TrueEndpoint = endpoint;
								}
							}

							// Check gate overlap and check wormhole jumping
							{
								other_gates.AddRange(MyJumpGateModSession.Instance.GetAllJumpGates());
								MyJumpGate new_target_gate = null;
								this.JumpSpaceExplosions.RemoveAll((explosion) => explosion.Tick(15));
								float total_explosive = (float) this.CalculateExplosivePowerWithinJumpSpace();
								last_energy_buffer = energy_buffer;
								energy_buffer += total_explosive;
								energy_buffer = (float) Math.Round(energy_buffer * (1 - energy_buffer_dissipation_percent), 4);
								double max_jump_distance = energy_buffer * this.JumpGateConfiguration.JumpDistancePerExplosionPower;
								float alpha = MathHelper.Clamp((float) ((energy_buffer - energy_jump_cutoff_min) / energy_jump_cutoff_max), 0, 1);
								UpdateEnergyReleaseEffects(alpha);

								foreach (MyJumpGate other_gate in other_gates)
								{
									if (other_gate.MarkClosed || other_gate.Closed || other_gate == this) continue;
									else if (other_gate.Status == MyJumpGateStatus.OUTBOUND && jump_ellipse.Intersects(other_gate.JumpEllipse))
									{
										result = MyJumpFailReason.JUMP_SPACE_TRANSPOSED;
										break;
									}
									else if (max_jump_distance > this.JumpGateConfiguration.MinimumJumpDistance && new_target_gate == null && other_gate != target_gate && this.IsGateValidForWormholeJump(other_gate, max_jump_distance))
									{
										new_target_gate = other_gate;
										break;
									}
								}

								other_gates.Clear();

								if ((new_target_gate != null || energy_buffer != last_energy_buffer) && MyJumpGateModSession.Instance.Network.Registered)
								{
									MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet
									{
										PacketType = MyPacketTypeEnum.JUMP_GATE_WORMHOLE_JUMP,
										Broadcast = true,
										TargetID = 0,
									};
									packet.Payload(new JumpGateWormholeJumpInfo
									{
										ThisGate = this_id,
										TargetGate = (new_target_gate == null) ? JumpGateUUID.Empty : JumpGateUUID.FromJumpGate(new_target_gate),
										TargetControllerSettings = new_target_gate?.ControlObject?.BlockSettings,
										ExplosionBuffer = energy_buffer,
									});
									packet.Send();
								}

								if (new_target_gate != null)
								{
									Logger.Debug($"WORMHOLE_JUMP: EXPLOSION_POWER={total_explosive}, MAX_JUMP_DISTANCE={max_jump_distance}, TARGET={new_target_gate?.GetPrintableName()}", 2);
									new_target_gate.SenderGate = this;
									new_target_gate.Status = MyJumpGateStatus.INBOUND;
									new_target_gate.Phase = MyJumpGatePhase.JUMPING;
									remote_gravity.Intensity = 0;
									this_gravity.Intensity = 0;
									gate_animation.Pause();
									new_target_gate.TrueEndpoint = new_target_gate.WorldJumpNode + (new_target_gate.WorldJumpNode - jump_node);
									target_gate.TrueEndpoint = endpoint + (endpoint - jump_node);
									float open_duration = gate_animation.Durations()[(byte) MyJumpGateAnimation.AnimationTypeEnum.WORMHOLE_OPEN];
									ushort transition_tick = 0;

									MyJumpGate close_gate = target_gate;
									MyAPIGateway.GravityProviderSystem.RemoveNaturalGravityFromEntity(target_gate.JumpSpaceCollisionDetector);
									MyJumpGateController.MyControllerBlockSettingsStruct new_target_settings = new_target_gate.ControlObject?.BlockSettings;
									MyJumpGateAnimation swap_open_animation = MyAnimationHandler.GetAnimation(gate_animation_name, MyAPIGateway.Session.Player, new_target_gate, new_target_gate, controller_settings, controller_settings, MyJumpTypeEnum.INBOUND_VOID);
									MyJumpGateAnimation swap_close_animation = MyAnimationHandler.GetAnimation(gate_animation_name, MyAPIGateway.Session.Player, target_gate, target_gate, controller_settings, controller_settings, MyJumpTypeEnum.INBOUND_VOID);
									MyJumpGateAnimation transition_animation = MyAnimationHandler.GetAnimation(gate_animation_name, MyAPIGateway.Session.Player, this, null, controller_settings, null, MyJumpTypeEnum.OUTBOUND_VOID);

									MyJumpGateModSession.Instance.PlayAnimation(swap_close_animation, MyJumpGateAnimation.AnimationTypeEnum.WORMHOLE_CLOSE, on_complete: (Exception e) => {
										if (close_gate.MarkClosed || close_gate.Closed) return;
										close_gate.Reset(false);
										close_gate.TrueEndpoint = null;
									});

									MyJumpGateModSession.Instance.PlayAnimation(transition_animation, MyJumpGateAnimation.AnimationTypeEnum.WORMHOLE_LOOP, () => {
										float ratio = transition_tick++ / open_duration;
										this.TrueEndpoint = Vector3D.Lerp(target_gate.WorldJumpNode, new_target_gate.WorldJumpNode, ratio);
										UpdateEnergyReleaseEffects(1 - ratio);
										return ratio < 1;
									});

									MyJumpGateModSession.Instance.PlayAnimation(swap_open_animation, MyJumpGateAnimation.AnimationTypeEnum.WORMHOLE_OPEN, on_complete: (Exception exception) => {
										new_target_gate.WormholeStartTime = this.WormholeStartTime;
										new_target_gate.TrueEndpoint = null;
										this.JumpSpaceExplosions?.Clear();
										this.TrueEndpoint = new_target_gate.WorldJumpNode;
										energy_buffer = 0;
										target_gate = new_target_gate;
										target_controller_settings = new_target_settings;
										this_gravity = MyAPIGateway.GravityProviderSystem.AddNaturalGravityToEntity(target_gate.JumpSpaceCollisionDetector, r1, r2, 2, this_gravity_strength);
										swap_open_animation.Clean();

										if (energy_release_effect != null && !energy_release_effect.IsEmittingStopped)
										{
											energy_release_effect.StopEmitting();
											energy_release_effect.StopLights();
										}

										energy_release_sound.Entity = target_gate.JumpSpaceCollisionDetector;
										energy_release_sound.StopSound(true);
										MyJumpGateModSession.Instance.StopAnimation(transition_animation);
										gate_animation.SetTargetGate(target_gate, target_controller_settings);
										gate_animation.Resume();
									});

									return true;
								}
							}

							// Check power
							foreach (MyJumpGateDrive drive in this.GetWorkingJumpGateDrives())
							{
								if (drive.IsPowered) continue;
								result = MyJumpFailReason.INSUFFICIENT_POWER;
								break;
							}

							// Update gravity
							{
								remote_gravity.Intensity = 0;
								this_gravity.Intensity = 0;
								remote_gravity_direction = MyAPIGateway.GravityProviderSystem.CalculateNaturalGravityInPoint(endpoint);
								this_gravity_direction = MyAPIGateway.GravityProviderSystem.CalculateNaturalGravityInPoint(jump_node);

								remote_gravity_strength = remote_gravity_direction.Length() / gravity * this.JumpGateConfiguration.GravityPassthroughMultiplier;
								this_gravity_strength = this_gravity_direction.Length() / gravity * target_gate.JumpGateConfiguration.GravityPassthroughMultiplier;

								r1 = jump_ellipse.Radii.Max();
								r2 = r1 / Math.Sqrt(lower_gravity_limit / remote_gravity_strength);
								remote_gravity.MinRadius = r1;
								remote_gravity.MaxRadius = r2;
								remote_gravity.Intensity = remote_gravity_strength;

								r1 = target_gate.JumpEllipse.Radii.Max();
								r2 = r1 / Math.Sqrt(lower_gravity_limit / this_gravity_strength);
								this_gravity.MinRadius = r1;
								this_gravity.MaxRadius = r2;
								this_gravity.Intensity = this_gravity_strength;
							}

							// Jump entities
							{
								this.UpdateJumpSpaceEntities();
								target_gate.UpdateJumpSpaceEntities();
								src_entities_to_jump.AddRange(this.GetEntitiesReadyForJump(true)
									.Select((pair) => pair.Key)
									.Where((entity) => !src_jump_entities.Any((pair) => pair.Key.Contains(entity)) && !entity_animation_batches.Any((pair) => pair.Key.Contains(entity)))
									.Take(128));
								dst_entities_to_jump.AddRange(target_gate.GetEntitiesReadyForJump(true)
									.Select((pair) => pair.Key)
									.Where((entity) => !dst_jump_entities.Any((pair) => pair.Key.Contains(entity)) && !entity_animation_batches.Any((pair) => pair.Key.Contains(entity)))
									.Take(128));

								if (src_entities_to_jump.Count > 0 && entity_animation_batches.Count < 16)
								{
									List<MyEntity> active_batch = new List<MyEntity>(src_entities_to_jump.Distinct().Where((entity) => entity?.Physics != null && !entity.MarkedForClose));
									MyJumpGateAnimation batch_animation = MyAnimationHandler.GetAnimation(gate_animation_name, MyAPIGateway.Session.Player, this, target_gate, controller_settings, target_controller_settings, MyJumpTypeEnum.STANDARD, active_batch);
									KeyValuePair<List<MyEntity>, MyJumpGateAnimation> batch_pair = new KeyValuePair<List<MyEntity>, MyJumpGateAnimation>(active_batch, batch_animation);
									entity_animation_batches.Add(batch_pair);
									src_entities_to_jump.Clear();
									MyJumpGateModSession.Instance.PlayAnimation(batch_animation, MyJumpGateAnimation.AnimationTypeEnum.JUMPING, () => {
										return this.IsWormholeActive && active_batch.All((entity) => entity != null && entity.Physics != null && !entity.MarkedForClose);
									}, (Exception e) => {
										if (!this.IsWormholeActive) return;
										src_jump_entities.Add(batch_pair);
										entity_animation_batches.Remove(batch_pair);
									});

									if (MyJumpGateModSession.Instance.Network.Registered)
									{
										MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet {
											PacketType = MyPacketTypeEnum.JUMP_GATE_JUMP,
											Broadcast = true,
											TargetID = 0,
										};
										packet.Payload(new JumpGateInfo {
											ControllerSettings = controller_settings,
											TargetControllerSettings = target_controller_settings,
											JumpGateID = (this.JumpGateGrid == null) ? this_id : JumpGateUUID.FromJumpGate(this),
											Type = JumpGateInfo.TypeEnum.WORMHOLE_ENTITY_PREJUMP,
											EntityIds = active_batch.Select((entity) => entity.EntityId).ToList(),
										});
										packet.Send();
									}
								}

								if (dst_entities_to_jump.Count > 0 && entity_animation_batches.Count < 16)
								{
									List<MyEntity> active_batch = new List<MyEntity>(dst_entities_to_jump.Distinct().Where((entity) => entity?.Physics != null && !entity.MarkedForClose));
									MyJumpGateAnimation batch_animation = MyAnimationHandler.GetAnimation(gate_animation_name, MyAPIGateway.Session.Player, target_gate, this, controller_settings, target_controller_settings, MyJumpTypeEnum.STANDARD, active_batch);
									KeyValuePair<List<MyEntity>, MyJumpGateAnimation> batch_pair = new KeyValuePair<List<MyEntity>, MyJumpGateAnimation>(active_batch, batch_animation);
									entity_animation_batches.Add(batch_pair);
									dst_entities_to_jump.Clear();
									MyJumpGateModSession.Instance.PlayAnimation(batch_animation, MyJumpGateAnimation.AnimationTypeEnum.JUMPING, () => {
										return this.IsWormholeActive && active_batch.All((entity) => entity != null && entity.Physics != null && !entity.MarkedForClose);
									}, (Exception e) => {
										if (!this.IsWormholeActive) return;
										dst_jump_entities.Add(batch_pair);
										entity_animation_batches.Remove(batch_pair);
									});
									
									if (MyJumpGateModSession.Instance.Network.Registered)
									{
										MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet {
											PacketType = MyPacketTypeEnum.JUMP_GATE_JUMP,
											Broadcast = true,
											TargetID = 0,
										};
										packet.Payload(new JumpGateInfo {
											ControllerSettings = controller_settings,
											TargetControllerSettings = target_controller_settings,
											JumpGateID = (this.JumpGateGrid == null) ? this_id : JumpGateUUID.FromJumpGate(this),
											Type = JumpGateInfo.TypeEnum.WORMHOLE_ANTI_ENTITY_PREJUMP,
											EntityIds = active_batch.Select((entity) => entity.EntityId).ToList(),
										});
										packet.Send();
									}
								}

								for (int i = 0; i < src_jump_entities.Count; ++i)
								{
									KeyValuePair<List<MyEntity>, MyJumpGateAnimation> pair = src_jump_entities[i];
									pair.Key.RemoveAll((entity) => entity == null || entity.Physics == null || entity.MarkedForClose);
									if (pair.Key.Count == 0) continue;
									this.JumpEntitiesToEndpoint(pair.Key, ref jump_node, target_gate, ref default_endpoint, MyWaypointType.JUMP_GATE, controller_settings, gate_animation, false, (batch_reason, batch_result, batch_message) => {
										if (batch_result) target_gate.ArrivingWormholeEntities.AddRange(pair.Key);
										MyJumpGateModSession.Instance.PlayAnimation(pair.Value, (batch_result) ? MyJumpGateAnimation.AnimationTypeEnum.JUMPED : MyJumpGateAnimation.AnimationTypeEnum.FAILED);
										SendJumpResponse(batch_reason, batch_result, batch_message, pair.Key, false);
										closed_src_jump_entities.Add(i);
									});
								}

								for (int i = 0; i < dst_jump_entities.Count; ++i)
								{
									KeyValuePair<List<MyEntity>, MyJumpGateAnimation> pair = dst_jump_entities[i];
									pair.Key.RemoveAll((entity) => entity == null || entity.Physics == null || entity.MarkedForClose);
									if (pair.Key.Count == 0) continue;
									target_gate.JumpEntitiesToEndpoint(pair.Key, ref endpoint, this, ref jump_node, MyWaypointType.JUMP_GATE, controller_settings, gate_animation, false, (batch_reason, batch_result, batch_message) => {
										if (batch_result) this.ArrivingWormholeEntities.AddRange(pair.Key);
										MyJumpGateModSession.Instance.PlayAnimation(pair.Value, (batch_result) ? MyJumpGateAnimation.AnimationTypeEnum.JUMPED : MyJumpGateAnimation.AnimationTypeEnum.FAILED);
										SendJumpResponse(batch_reason, batch_result, batch_message, pair.Key, true);
										closed_dst_jump_entities.Add(i);
									});
								}

								src_jump_entities.RemoveIndices(closed_src_jump_entities);
								dst_jump_entities.RemoveIndices(closed_dst_jump_entities);
								closed_src_jump_entities.Clear();
								closed_dst_jump_entities.Clear();
							}

							if (result != MyJumpFailReason.NONE) SendWormholeJumpResponse(result, false, null);
							return result == MyJumpFailReason.NONE;
						}, (Exception e) => {
							this.JumpSpaceExplosions?.Clear();
							energy_release_effect?.Stop();
							energy_release_sound.StopSound(true);
							if (this.JumpSpaceCollisionDetector != null) MyAPIGateway.GravityProviderSystem.RemoveNaturalGravityFromEntity(this.JumpSpaceCollisionDetector);
							if (target_gate.JumpSpaceCollisionDetector != null) MyAPIGateway.GravityProviderSystem.RemoveNaturalGravityFromEntity(target_gate.JumpSpaceCollisionDetector);
							if (result != MyJumpFailReason.NONE || !this.IsWormholeActive) return;
							int drive_count = this.GetJumpGateDrives().Count();
							int working_drive_count = this.GetWorkingJumpGateDrives().Count();
							Logger.Error($"Error during jump gate wormhole jump:\n\tPHASE=LOOP\n\tGATE_CUBE_GRID={this.JumpGateGrid?.CubeGrid?.EntityId.ToString() ?? "N/A"}\n\tGATE_ID={this.JumpGateID}\n\tGATE_SIZE={drive_count}\n\tGATE_SIZE_WORKING={working_drive_count}\nERROR:\n\t{e}\n\tINNER:\n{e.InnerException}");
							SendWormholeJumpResponse(MyJumpFailReason.UNKNOWN_ERROR, false, null);
						});
					}
					catch (Exception e)
					{
						SendWormholeJumpResponse(MyJumpFailReason.UNKNOWN_ERROR, false, null);
						int drive_count = this.GetJumpGateDrives().Count();
						int working_drive_count = this.GetWorkingJumpGateDrives().Count();
						Logger.Error($"Error during jump gate wormhole jump:\n\tPHASE=POSTANIM\n\tGATE_CUBE_GRID={this.JumpGateGrid?.CubeGrid?.EntityId.ToString() ?? "N/A"}\n\tGATE_ID={this.JumpGateID}\n\tGATE_SIZE={drive_count}\n\tGATE_SIZE_WORKING={working_drive_count}\nERROR:\n\t{e}\n\tINNER:\n{e.InnerException}");
					}

				});
			}
			catch (Exception e)
			{
				SendWormholeJumpResponse(MyJumpFailReason.UNKNOWN_ERROR, false, null);
				int drive_count = this.GetJumpGateDrives().Count();
				int working_drive_count = this.GetWorkingJumpGateDrives().Count();
				Logger.Error($"Error during jump gate wormhole jump:\n\tPHASE=PREANIM\n\tGATE_CUBE_GRID={this.JumpGateGrid?.CubeGrid?.EntityId.ToString() ?? "N/A"}\n\tGATE_ID={this.JumpGateID}\n\tGATE_SIZE={drive_count}\n\tGATE_SIZE_WORKING={working_drive_count}\nERROR:\n\t{e}\n\tINNER:\n{e.InnerException}");
			}
		}

		/// <summary>
		/// Jumps a set of entities from this gate to the tageted endpoint
		/// </summary>
		/// <param name="entities">The entities to jump</param>
		/// <param name="jump_node">This gate's jump node</param>
		/// <param name="target_endpoint">The target endpoint vector or target jump gate</param>
		/// <param name="default_endpoint">The calculated default endpoint (before randomness)</param>
		/// <param name="waypoint_type">The type of waypoint being jumped to</param>
		/// <param name="controller_settings">This jump gate's controller settings</param>
		/// <param name="animation">The jump gate animation</param>
		/// <param name="allow_power_syphon">Whether to allow the power syphon to engage</param>
		/// <param name="on_jump_callback">Callback used to receive jump results</param>
		private void JumpEntitiesToEndpoint(IEnumerable<MyEntity> entities, ref Vector3D jump_node, object target_endpoint, ref Vector3D default_endpoint, MyWaypointType waypoint_type, MyJumpGateController.MyControllerBlockSettingsStruct controller_settings, MyJumpGateAnimation animation, bool allow_power_syphon, Action<MyJumpFailReason, bool, string> on_jump_callback)
		{
			Random prng = new Random();
			MyJumpGate target_gate = target_endpoint as MyJumpGate;
			Vector3D endpoint = target_gate?.TrueWorldJumpEllipse.WorldMatrix.Translation ?? (Vector3D) target_endpoint;
			BoundingEllipsoidD jump_ellipse = this.JumpEllipse;
			bool compute_orientation = !controller_settings.BypassComputedEntityOrientation();

			allow_power_syphon = MyJumpGateModSession.Instance.Configuration.GeneralConfiguration.SyphonReactorPower.Value && allow_power_syphon;
			ushort jump_duration = animation?.GateJumpedAnimationDef?.BeamPulse?.TravelTime ?? animation?.GateJumpedAnimationDef?.TravelTime ?? animation?.GateJumpedAnimationDef?.Duration ?? 0;
			int skipped_entities_count = 0;
			int entity_count = 0;
			double syphon_power = 0;
			double distance_to_endpoint;
			Vector3D.Distance(ref jump_node, ref endpoint, out distance_to_endpoint);

			MatrixD this_matrix = MatrixD.Normalize(this.TrueWorldJumpEllipse.WorldMatrix);
			MatrixD target_matrix = MatrixD.Normalize(target_gate?.TrueWorldJumpEllipse.WorldMatrix ?? MatrixD.CreateWorld(endpoint, this_matrix.Forward, this_matrix.Up));
			Vector3D src_gate_velocity = this.JumpNodeVelocity;
			Vector3D dst_gate_velocity = (target_gate == null) ? Vector3D.Zero : target_gate.JumpNodeVelocity;
			MyGravityAlignmentType alignment = controller_settings.GravityAlignmentType();

			List<MyEntity> obstructing = new List<MyEntity>();
			List<MyVoxelBase> terrain = new List<MyVoxelBase>();
			List<MyEntity> syphon_entities = new List<MyEntity>();
			List<IMySlimBlock> destroyed = new List<IMySlimBlock>();
			List<IMyCubeGrid> grids = new List<IMyCubeGrid>();

			// Find available beacon jump point
			if (waypoint_type == MyWaypointType.BEACON)
			{
				BoundingSphereD endsphere = new BoundingSphereD(endpoint, Math.Max(jump_ellipse.Radii.Max() * 2, 1000));
				BoundingSphereD largest_sphere = new BoundingSphereD(jump_node, jump_ellipse.Radii.Max());
				List<MyEntity> topmost = new List<MyEntity>();
				bool found = false;

				for (int i = 0; i < 10; ++i)
				{
					double rand1 = prng.NextDouble();
					double rand2 = prng.NextDouble();
					double rand3 = prng.NextDouble();
					largest_sphere.Center = endsphere.RandomToUniformPointInSphere(rand1, rand2, rand3);
					MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref largest_sphere, topmost);

					if (!topmost.Any((entity) => entity is MyCubeGrid))
					{
						topmost.Clear();
						endpoint = largest_sphere.Center;
						found = true;
						break;
					}

					topmost.Clear();
				}

				if (!found)
				{
					on_jump_callback(MyJumpFailReason.BEACON_BLOCKED, false, null);
					return;
				}
			}

			// Apply Randomness to endpoint if untethered
			if (!MyJumpGateModSession.Instance.Configuration.GeneralConfiguration.SafeJumps.Value && target_gate == null)
			{
				// Bend jump path around gravity
				byte distance_multiplier = 1;
				uint per_meter = 1000u * distance_multiplier;
				ulong segments = (ulong) Math.Round(distance_to_endpoint / per_meter);
				Vector3D direction = (endpoint - jump_node).Normalized() * per_meter;
				Vector3D startpos = jump_node;

				for (ulong i = 0; i < segments; ++i)
				{
					float _;
					Vector3D gravity_direction = MyAPIGateway.Physics.CalculateNaturalGravityAt(startpos, out _);
					double g_effector = this.JumpGateConfiguration.GateKilometerOffsetPerUnitG * gravity_direction.Length() * distance_multiplier;
					direction += gravity_direction * g_effector;
					startpos += direction;
				}

				distance_to_endpoint = Vector3D.Distance(startpos, jump_node);
				Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} GRAVITY_ENDPOINT_OFFSET - OLD={default_endpoint}; NEW={startpos}; OFFSET={Vector3D.Distance(startpos, default_endpoint)}", 5);

				// Apply random offset to endpoint
				if (this.JumpGateConfiguration.ConfineUntetheredSpread)
				{
					double max_offset = distance_to_endpoint * this.JumpGateConfiguration.GateRandomOffsetPerKilometer;
					double rand1 = prng.NextDouble();
					double rand2 = prng.NextDouble();
					double rand3 = prng.NextDouble();
					endpoint = new BoundingSphereD(startpos, max_offset).RandomToUniformPointInSphere(rand1, rand2, rand3);
					distance_to_endpoint = Vector3D.Distance(endpoint, jump_node);
					Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} RANDOM_ENDPOINT_OFFSET - OLD={default_endpoint}; NEW={endpoint}; OFFSET={Vector3D.Distance(endpoint, default_endpoint)}", 5);
				}
				else
				{
					endpoint = startpos;
					distance_to_endpoint = Vector3D.Distance(endpoint, jump_node);
				}
			}

			// Jump entities
			if (waypoint_type == MyWaypointType.SERVER)
			{
				on_jump_callback(MyJumpFailReason.CROSS_SERVER_JUMP, false, null);
				return;
			}

			Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} JUMP_ENTITY_TRANSIT - Assembling batches...", 3);

			// Assemble batches
			foreach (MyEntity root in entities)
			{
				if (root == null || root.Physics == null || !root.Physics.Enabled) continue;
				++entity_count;
				MyEntity entity = root;
				MatrixD entity_final_matrix;
				BoundingBoxD obstruction_aabb;
				MyJumpGateConstruct parent = (entity is MyCubeGrid) ? MyJumpGateModSession.Instance.GetUnclosedJumpGateGrid(entity.EntityId) : null;
				entity = (parent == null) ? entity : (MyCubeGrid) parent.CubeGrid;
				List<MyEntity> batch = new List<MyEntity>() { entity };
				MatrixD orientation_matrix = (parent != null && parent != this.JumpGateGrid) ? parent.GetConstructCalculatedOrienation() : entity.WorldMatrix;
				Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} BATCHING - BATCH_PARENT={entity.EntityId}; ISGRID={parent != null}", 5);

				// Calculate end position
				Vector3D position = MyJumpGateModSession.WorldVectorToLocalVectorP(ref this_matrix, orientation_matrix.Translation);
				position = MyJumpGateModSession.LocalVectorToWorldVectorP(ref target_matrix, position);
				Vector3D forward_dir, up_dir;

				if (compute_orientation)
				{
					forward_dir = MyJumpGateModSession.WorldVectorToLocalVectorD(ref this_matrix, orientation_matrix.Forward);
					forward_dir = MyJumpGateModSession.LocalVectorToWorldVectorD(ref target_matrix, forward_dir);
					up_dir = MyJumpGateModSession.WorldVectorToLocalVectorD(ref this_matrix, orientation_matrix.Up);
					up_dir = MyJumpGateModSession.LocalVectorToWorldVectorD(ref target_matrix, up_dir);
				}
				else
				{
					forward_dir = entity.WorldMatrix.Forward;
					up_dir = entity.WorldMatrix.Up;
				}

				MatrixD.CreateWorld(ref position, ref forward_dir, ref up_dir, out entity_final_matrix);
				Vector3D entity_target_position = target_matrix.Translation + (orientation_matrix.Translation - this_matrix.Translation);
				Vector3D gravity_vector = Vector3D.Zero;

				// Apply randomness to end position of applicable
				if (target_gate == null && !this.JumpGateConfiguration.ConfineUntetheredSpread)
				{
					double max_offset = distance_to_endpoint * this.JumpGateConfiguration.GateRandomOffsetPerKilometer;
					double rand1 = prng.NextDouble();
					double rand2 = prng.NextDouble();
					double rand3 = prng.NextDouble();
					entity_final_matrix.Translation = position = new BoundingSphereD(endpoint, max_offset).RandomToUniformPointInSphere(rand1, rand2, rand3);
					Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... RANDOM_DIST_ENDPOINT_OFFSET_LOCAL @ {entity.EntityId} - OLD={endpoint}; NEW={position}; OFFSET={Vector3D.Distance(position, endpoint)}", 5);
				}

				if (target_gate == null && this.JumpGateConfiguration.GateRandomDisplacementRadius > 0)
				{
					double rand1 = prng.NextDouble();
					double rand2 = prng.NextDouble();
					double rand3 = prng.NextDouble();
					entity_final_matrix.Translation = position = new BoundingSphereD(position, this.JumpGateConfiguration.GateRandomDisplacementRadius).RandomToUniformPointInSphere(rand1, rand2, rand3);
					Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... RANDOM_ENDPOINT_OFFSET_LOCAL @ {entity.EntityId} - OLD={endpoint}; NEW={position}; OFFSET={Vector3D.Distance(position, endpoint)}", 5);
				}

				// Align entity to gravity
				if (alignment != MyGravityAlignmentType.NONE && compute_orientation)
				{
					//Vector3D gravity_vector = Vector3D.Zero;
					float gravity_strength;

					switch (alignment)
					{
						case MyGravityAlignmentType.NATURAL:
							gravity_vector = MyAPIGateway.Physics.CalculateNaturalGravityAt(target_matrix.Translation, out gravity_strength);
							break;
						case MyGravityAlignmentType.ARTIFICIAL:
							MyAPIGateway.Physics.CalculateNaturalGravityAt(target_matrix.Translation, out gravity_strength);
							gravity_vector = MyAPIGateway.Physics.CalculateArtificialGravityAt(target_matrix.Translation, gravity_strength);
							break;
						case MyGravityAlignmentType.BOTH:
							gravity_vector = MyAPIGateway.Physics.CalculateNaturalGravityAt(target_matrix.Translation, out gravity_strength);
							gravity_vector += MyAPIGateway.Physics.CalculateArtificialGravityAt(target_matrix.Translation, gravity_strength);
							break;
					}

					if (gravity_vector != Vector3D.Zero)
					{
						double angle = Vector3D.Angle(gravity_vector, entity_final_matrix.Down);
						Logger.Log($"RESULT-{entity.DisplayName} = O1 {angle * (180d / Math.PI)}");

						if (angle > 0.0017453292519943296)
						{
							Vector3D axis = ((angle == Math.PI) ? Vector3D.CalculatePerpendicularVector(gravity_vector) : Vector3D.Cross(gravity_vector, entity_final_matrix.Down)).Normalized();
							QuaternionD transform = QuaternionD.CreateFromAxisAngle(axis, -angle);
							entity_final_matrix = Extensions.Extensions.Transform(ref entity_final_matrix, ref transform).Round(8);
							entity_final_matrix.Translation = position;
						}

						Logger.Log($"RESULT-{entity.DisplayName} = O2 {Vector3D.Angle(entity_final_matrix.Down, gravity_vector) * (180d / Math.PI)}, {Vector3D.Angle(entity_final_matrix.Down, entity_final_matrix.Up) * (180d / Math.PI)}");
					}
				}

				// Construct specific checks
				if (parent == this.JumpGateGrid) continue;
				else if (parent != null)
				{
					// Check cube grid obstructions
					BoundingBoxD construct_aabb = parent.GetCombinedAABB();
					obstruction_aabb = construct_aabb;
					obstruction_aabb.TransformFast(ref entity_final_matrix, ref obstruction_aabb);
					MyGamePruningStructure.GetTopmostEntitiesInBox(ref obstruction_aabb, obstructing);

					obstructing.RemoveAll((obstruct) => {
						if (!(obstruct is MyCubeGrid)) return true;
						MyJumpGateConstruct obgrid = MyJumpGateModSession.Instance.GetJumpGateGrid(obstruct.EntityId);
						if (obgrid == null || obgrid.MarkClosed) return true;
						IEnumerator<IMySlimBlock> enumerator1 = parent.GetConstructBlocks();
						IEnumerator<IMySlimBlock> enumerator2 = obgrid.GetConstructBlocks();

						while (enumerator1.MoveNext())
						{
							IMySlimBlock block1 = enumerator1.Current;
							Vector3D block_pos_1 = block1.CubeGrid.GridIntegerToWorld(block1.Position);
							block_pos_1 = MyJumpGateModSession.WorldVectorToLocalVectorP(ref this_matrix, block_pos_1);
							block_pos_1 = MyJumpGateModSession.LocalVectorToWorldVectorP(ref target_matrix, block_pos_1);

							while (enumerator2.MoveNext())
							{
								IMySlimBlock block2 = enumerator2.Current;
								BoundingBoxD block2_bounds;
								block2.GetWorldBoundingBox(out block2_bounds);
								if (block2_bounds.Contains(block_pos_1) != ContainmentType.Disjoint) return false;
							}

							enumerator2.Reset();
						}

						return true;
					});

					if (obstructing.Count > 0)
					{
						++skipped_entities_count;
						obstructing.Clear();
						Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... BATCH_SKIP_FAIL_OBSTRUCTED_CONSTRUCT", 4);
						continue;
					}

					obstructing.Clear();

					// Terrain Check
					MyGamePruningStructure.GetAllVoxelMapsInBox(ref obstruction_aabb, terrain);
					bool terrain_obstruct = false;

					foreach (IMyCubeGrid subgrid in parent.GetCubeGrids())
					{
						Vector3 extents = subgrid.LocalAABB.Extents * 0.75f;
						BoundingBoxD local_aabb = new BoundingBoxD(subgrid.LocalAABB.Center - extents, subgrid.LocalAABB.Center + extents);
						MatrixD world_matrix = entity_final_matrix;
						Vector3D relation = subgrid.WorldMatrix.Translation - parent.CubeGrid.WorldMatrix.Translation;
						relation = MyJumpGateModSession.WorldVectorToLocalVectorD(parent.CubeGrid.WorldMatrix, relation);
						world_matrix.Translation = entity_final_matrix.Translation + MyJumpGateModSession.LocalVectorToWorldVectorD(ref entity_final_matrix, relation);
						if (terrain_obstruct = terrain.Any((voxel) => voxel.GetVoxelContentInBoundingBox_Fast(local_aabb, world_matrix, true).Item2 > 0)) break;
					}

					terrain.Clear();

					if (terrain_obstruct)
					{
						++skipped_entities_count;
						Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... BATCH_SKIP_FAIL_OBSTRUCTED_TERRAIN", 4);
						continue;
					}

					// Gather contained child entities
					MyGamePruningStructure.GetTopmostEntitiesInBox(ref construct_aabb, obstructing);
					MyJumpGateConstruct subparent;

					foreach (MyEntity sub_entity in obstructing)
					{
						subparent = (sub_entity is MyCubeGrid) ? MyJumpGateModSession.Instance.GetUnclosedJumpGateGrid(sub_entity.EntityId) : null;
						if (sub_entity == entity || subparent == parent || subparent == this.JumpGateGrid || this.JumpGateGrid.HasCubeGrid(sub_entity.EntityId) || parent.IsPositionInsideAnySubgrid(sub_entity.WorldMatrix.Translation) == null) continue;
						batch.Add(sub_entity);
						Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... ... BATCH_CHILD={sub_entity.EntityId}", 4);
					}

					obstructing.Clear();

					// Gather attached child grids
					foreach (IMyCubeGrid subgrid in parent.GetCubeGrids())
					{
						subgrid.GetGridGroup(GridLinkTypeEnum.Physical).GetGrids(grids);
						batch.AddRange(grids.Select((child) => (MyEntity) child).Where((child) => !batch.Contains(child)));
						grids.Clear();
					}
				}
				else
				{
					obstruction_aabb = ((IMyEntity) entity).WorldAABB;
					obstruction_aabb.TransformFast(ref entity_final_matrix, ref obstruction_aabb);
				}

				// Merge batches
				List<MyEntity> mergers = new List<MyEntity>();

				for (int i = 0; i < batch.Count; ++i)
				{
					MyEntity child = batch[i];
					EntityBatch child_batch = this.EntityBatches.GetValueOrDefault(child, null);

					if (child_batch != null)
					{
						batch.AddRange(child_batch.Batch.Where((child_) => !batch.Contains(child_)));
						this.EntityBatches.Remove(child);
						mergers.Remove(child);
						Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... BATCH_MERGED - {child.EntityId} --> {entity.EntityId}", 4);
					}

					foreach (KeyValuePair<MyEntity, EntityBatch> batch_pair in this.EntityBatches)
					{
						if (!batch_pair.Value.Batch.Contains(child)) continue;
						mergers.Add(batch_pair.Key);
						break;
					}
				}

				if (mergers.Count == 1)
				{
					EntityBatch target = this.EntityBatches[mergers[0]];
					target.Batch.AddRange(batch.Where((child) => !target.Batch.Contains(child)));
					BoundingBoxD.CreateMerged(ref target.ObstructAABB, ref obstruction_aabb, out target.ObstructAABB);
					Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... BATCH_MERGED - {entity.EntityId} --> {target.Parent.EntityId}", 4);
				}
				else
				{
					foreach (MyEntity merger in mergers.Distinct()) batch.RemoveAll(this.EntityBatches[merger].Batch.Contains);
					this.EntityBatches[entity] = new EntityBatch(batch.Distinct().ToList(), ref entity_target_position, ref obstruction_aabb, ref entity_final_matrix, ref gravity_vector);
					Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... BATCH_ADDED_{entity.EntityId}", 4);
				}
			}

			// Finalize batches (remove duplicate constructs)
			foreach (KeyValuePair<MyEntity, EntityBatch> pair in this.EntityBatches)
			{
				MyJumpGateConstruct construct;
				HashSet<MyJumpGateConstruct> seen_constructs = new HashSet<MyJumpGateConstruct>();
				int count = pair.Value.Batch.Count;
				pair.Value.Batch.RemoveAll((child) => {
					construct = MyJumpGateModSession.Instance.GetJumpGateGrid(child as MyCubeGrid);
					if (construct == null) return false;
					if (seen_constructs.Contains(construct)) return true;
					seen_constructs.Add(construct);
					return false;
				});
				Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} JUMP_ENTITY_TRANSIT - Removed {count - pair.Value.Batch.Count} duplicate subgrid(s) from batch", 4);
			}

			if (this.EntityBatches.Count == 0)
			{
				on_jump_callback(MyJumpFailReason.NO_ENTITIES, false, null);
				return;
			}

			// Teleport batches
			Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} JUMP_ENTITY_TRANSIT - Teleporting batches...", 3);

			foreach (KeyValuePair<MyEntity, EntityBatch> pair in this.EntityBatches)
			{
				EntityBatch batch = pair.Value;
				MyEntity entity = pair.Key;
				MyJumpGateConstruct parent = (entity is MyCubeGrid) ? MyJumpGateModSession.Instance.GetUnclosedJumpGateGrid(entity.EntityId) : null;
				Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} TELEPORTING - BATCH_PARENT={entity.EntityId}; ISGRID={parent != null}; BATCH_SIZE={batch.Batch.Count}", 4);

				// Destroy neccessary construct blocks
				if (parent != null)
				{
					grids.AddRange(parent.GetCubeGrids());

					foreach (IMyCubeGrid grid in grids)
					{
						foreach (IMyShipConnector connector in grid.GetFatBlocks<IMyShipConnector>())
						{
							if (connector.IsWorking && connector.Status == Sandbox.ModAPI.Ingame.MyShipConnectorStatus.Connected && this.JumpGateGrid.HasCubeGrid(connector.OtherConnector.CubeGrid))
							{
								destroyed.Add(connector.SlimBlock);
							}
						}

						foreach (IMyLandingGear gear in grid.GetFatBlocks<IMyLandingGear>())
						{
							IMyEntity attached_entity;
							if (gear.IsWorking && gear.IsLocked && (attached_entity = gear.GetAttachedEntity()) is IMyCubeGrid && this.JumpGateGrid.HasCubeGrid(attached_entity as IMyCubeGrid))
							{
								destroyed.Add(gear.SlimBlock);
							}
						}

						if (destroyed.Count > 0 && this.JumpGateConfiguration.IgnoreDockedGrids)
						{
							skipped_entities_count += pair.Value.Batch.Count;
							grids.Clear();
							destroyed.Clear();
							Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... TP_FAIL_DOCKED", 4);
							continue;
						}
						else
						{
							foreach (IMySlimBlock block in destroyed) grid.RemoveDestroyedBlock(block);
						}

						destroyed.Clear();

						// Shear grid
						if (!MyJumpGateModSession.Instance.Configuration.GeneralConfiguration.LenientJumps.Value)
						{
							MyCubeGrid cubegrid = entity as MyCubeGrid;
							foreach (IMySlimBlock block in this.ShearBlocks) if (block.CubeGrid == grid) grid.RemoveDestroyedBlock(block);
							destroyed.AddRange(cubegrid.CubeBlocks.Where((block) => !jump_ellipse.IsPointInEllipse(grid.GridIntegerToWorld(((IMySlimBlock) block).Position))));
							parent.SplitGrid(grid, destroyed);
							destroyed.Clear();
						}
					}

					grids.Clear();
				}

				// Calculate power
				double batch_mass = batch.BatchMass;
				double available_power = this.CalculateTotalAvailableInstantPower();
				double power_scaler = (target_gate == null) ? this.JumpGateConfiguration.UntetheredJumpEnergyCost : 1;
				double required_power = Math.Round(batch_mass * this.CalculateUnitPowerRequiredForJump(ref default_endpoint), 8) * power_scaler / 1000;
				Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... INSTANT_POWER_SYPHON - BATCH_MASS={batch_mass} Kg; AVAILABLE_INSTANT_POWER={available_power} MW; MAX_AVAILABLE_INSTANT_POWER={this.CalculateTotalPossibleInstantPower()}", 4);
				Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... ... INITIAL - REQUIRED_POWER={required_power} Mw", 4);

				if (available_power < required_power)
				{
					syphon_entities.Add(entity);
					syphon_power += required_power;
					Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... TP_DEFER_INSUFFICIENT_POWER", 4);
					continue;
				}

				required_power = Math.Round(this.SyphonGridDrivePower(required_power), 8);
				Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... ... POST_DRIVES - REQUIRED_POWER={required_power} Mw", 4);
				if (required_power > 0) required_power = Math.Round(this.JumpGateGrid.SyphonConstructCapacitorPower(required_power), 8);
				Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... ... POST_CAPACITORS - REQUIRED_POWER={required_power} Mw", 4);

				if (required_power > 0)
				{
					syphon_entities.Add(entity);
					syphon_power += required_power;
					Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... TP_DEFER_INSUFFICIENT_POWER", 4);
					continue;
				}

				// Teleport batch
				// Calculate end position and velocity
				MatrixD entity_matrix = entity.WorldMatrix;
				Vector3D velocity = entity.Physics.LinearVelocity - this.JumpNodeVelocity;
				velocity = MyJumpGateModSession.WorldVectorToLocalVectorD(ref this_matrix, velocity);
				velocity = MyJumpGateModSession.LocalVectorToWorldVectorD(ref target_matrix, -velocity);
				velocity += target_gate?.JumpNodeVelocity ?? Vector3D.Zero;

				MyJumpGateModSession.Instance.WarpEntityBatchOverTime(this, batch.Batch, ref entity_matrix, ref batch.ParentFinalMatrix, ref batch.ParentTargetPosition, jump_duration, velocity.Length(), (batch_) => {
					MatrixD relation;
					Vector3? orientation;

					if (controller_settings.BypassComputedEntityOrientation())
					{
						relation = MatrixD.Identity;
					}
					else if ((orientation = controller_settings.EntityOrientationOverride()).HasValue)
					{
						relation = MatrixD.Identity;
						QuaternionD rotation = QuaternionD.CreateFromYawPitchRoll(orientation.Value.X, orientation.Value.Y, orientation.Value.Z);
						Extensions.Extensions.Transform(ref relation, ref rotation, ref relation);
					}
					else
					{
						MatrixD new_target_matrix = MatrixD.Normalize(target_gate?.TrueWorldJumpEllipse.WorldMatrix ?? target_matrix);
						relation = new_target_matrix * MatrixD.Invert(target_matrix);
					}

					foreach (MyEntity child in batch_.EntityBatch)
					{
						if (child != null && !child.MarkedForClose && child.Physics != null && child.Physics.Enabled)
						{
							child.Physics.LinearVelocity = velocity;
							MatrixD new_final_pos = relation * child.WorldMatrix;
							if (child is IMyCubeGrid) MyJumpGateModSession.TeleportCubeGrid(child as MyCubeGrid, ref new_final_pos);
							else child.Teleport(new_final_pos);
						}
					}

					this.EntityBatches?.Remove(entity);
				});

				foreach (MyEntity child in batch.Batch) this.OnEntityCollision(child, false);
				Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... TP_SUCCESS_BATCH_{entity.EntityId} - ENDPOINT={batch.ParentFinalMatrix.Translation}", 4);
			}

			// Check power syphon availability
			if (!allow_power_syphon && syphon_entities.Count == entity_count)
			{
				on_jump_callback(MyJumpFailReason.INSUFFICIENT_POWER, false, null);
				return;
			}
			else if (!allow_power_syphon && syphon_entities.Count > 0)
			{
				int final_count = entity_count - skipped_entities_count - syphon_entities.Count;
				string message = MyTexts.GetString("Notification_JumpGate_Jumped").Replace("{%0}", final_count.ToString()).Replace("{%1}", entity_count.ToString());
				on_jump_callback((final_count > 0) ? MyJumpFailReason.SUCCESS : MyJumpFailReason.NO_ENTITIES_JUMPED, final_count > 0, message);
			}

			// Handle power syphon
			Action<bool> power_syphon_callback = (power_syphoned) => {
				int final_count;
				string message;

				if (!power_syphoned && syphon_entities.Count == entity_count)
				{
					on_jump_callback(MyJumpFailReason.INSUFFICIENT_POWER, false, null);
					return;
				}
				else if (!power_syphoned)
				{
					final_count = entity_count - skipped_entities_count - syphon_entities.Count;
					message = MyTexts.GetString("Notification_JumpGate_Jumped").Replace("{%0}", final_count.ToString()).Replace("{%1}", entity_count.ToString());
					on_jump_callback((final_count > 0) ? MyJumpFailReason.SUCCESS : MyJumpFailReason.NO_ENTITIES_JUMPED, final_count > 0, message);
					return;
				}

				// Recheck syphon entities
				Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} JUMP_ENTITY_TRANSIT - Reevaluate syphon batches...", 3);

				foreach (MyEntity entity in syphon_entities)
				{
					EntityBatch batch = this.EntityBatches[entity];
					MyJumpGateConstruct parent = (entity is MyCubeGrid) ? MyJumpGateModSession.Instance.GetUnclosedJumpGateGrid(entity.EntityId) : null;
					Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} TELEPORTING - BATCH_PARENT={entity.EntityId}; ISGRID={parent != null}", 5);

					// Construct specific checks
					if (parent != null)
					{
						// Check cube grid obstructions
						MyGamePruningStructure.GetTopmostEntitiesInBox(ref batch.ObstructAABB, obstructing);

						obstructing.RemoveAll((obstruct) => {
							if (!(obstruct is MyCubeGrid)) return true;
							MyJumpGateConstruct obgrid = MyJumpGateModSession.Instance.GetJumpGateGrid(obstruct.EntityId);
							if (obgrid == null || obgrid.MarkClosed) return true;
							IEnumerator<IMySlimBlock> enumerator1 = parent.GetConstructBlocks();
							IEnumerator<IMySlimBlock> enumerator2 = obgrid.GetConstructBlocks();

							while (enumerator1.MoveNext())
							{
								IMySlimBlock block1 = enumerator1.Current;
								Vector3D block_pos_1 = block1.CubeGrid.GridIntegerToWorld(block1.Position);
								block_pos_1 = MyJumpGateModSession.WorldVectorToLocalVectorP(ref this_matrix, block_pos_1);
								block_pos_1 = MyJumpGateModSession.LocalVectorToWorldVectorP(ref target_matrix, block_pos_1);

								while (enumerator2.MoveNext())
								{
									IMySlimBlock block2 = enumerator2.Current;
									BoundingBoxD block2_bounds;
									block2.GetWorldBoundingBox(out block2_bounds);
									if (block2_bounds.Contains(block_pos_1) != ContainmentType.Disjoint) return false;

								}

								enumerator2.Reset();
							}

							return true;
						});

						if (obstructing.Count > 0)
						{
							++skipped_entities_count;
							obstructing.Clear();
							Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... BATCH_SKIP_FAIL_OBSTRUCTED_CONSTRUCT", 4);
							continue;
						}

						obstructing.Clear();
					}

					target_matrix = MatrixD.Normalize(target_gate?.TrueWorldJumpEllipse.WorldMatrix ?? MatrixD.CreateWorld(endpoint, this_matrix.Forward, this_matrix.Up));
					src_gate_velocity = this.JumpNodeVelocity;
					dst_gate_velocity = (target_gate == null) ? Vector3D.Zero : target_gate.JumpNodeVelocity;

					// Teleport batch
					if (jump_duration == 0)
					{
						foreach (MyEntity child in batch.Batch)
						{
							child.Teleport(batch.ParentFinalMatrix);
							Vector3D velocity = MyJumpGateModSession.WorldVectorToLocalVectorD(ref this_matrix, child.Physics.LinearVelocity);
							child.Physics.LinearVelocity = MyJumpGateModSession.LocalVectorToWorldVectorD(ref target_matrix, velocity);
							Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... ... TELEPORT_CHILD_{child.EntityId} - ENDPOINT={batch.ParentFinalMatrix.Translation}", 5);
						}

						Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... TP_SUCCESS", 4);
					}
					else
					{
						// Calculate end position and velocity
						MatrixD entity_matrix = entity.WorldMatrix;
						Vector3D velocity = MyJumpGateModSession.WorldVectorToLocalVectorD(ref this_matrix, entity.Physics.LinearVelocity);
						velocity = MyJumpGateModSession.LocalVectorToWorldVectorD(ref target_matrix, velocity);

						MyJumpGateModSession.Instance.WarpEntityBatchOverTime(this, batch.Batch, ref entity_matrix, ref batch.ParentFinalMatrix, ref batch.ParentTargetPosition, jump_duration, velocity.Length(), (batch_) => {
							MatrixD relation;
							Vector3? orientation;

							if (controller_settings.BypassComputedEntityOrientation())
							{
								relation = MatrixD.Identity;
							}
							else if ((orientation = controller_settings.EntityOrientationOverride()).HasValue)
							{
								relation = MatrixD.Identity;
								QuaternionD rotation = QuaternionD.CreateFromYawPitchRoll(orientation.Value.X, orientation.Value.Y, orientation.Value.Z);
								Extensions.Extensions.Transform(ref relation, ref rotation, ref relation);
							}
							else
							{
								MatrixD new_target_matrix = MatrixD.Normalize(target_gate?.TrueWorldJumpEllipse.WorldMatrix ?? target_matrix);
								relation = new_target_matrix * MatrixD.Invert(target_matrix);
							}

							foreach (MyEntity child in batch_.EntityBatch)
							{
								if (child != null && !child.MarkedForClose && child.Physics != null && child.Physics.Enabled)
								{
									child.Physics.LinearVelocity = velocity;
									MatrixD new_final_pos = relation * child.WorldMatrix;
									if (child is IMyCubeGrid) MyJumpGateModSession.TeleportCubeGrid(child as MyCubeGrid, ref new_final_pos);
									else child.Teleport(new_final_pos);
								}
							}

							this.EntityBatches?.Remove(entity);
						});
						Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... TP_SUCCESS_BATCH_{entity.EntityId} - ENDPOINT={batch.ParentFinalMatrix.Translation}", 4);
					}
				}

				final_count = entity_count - skipped_entities_count;
				message = MyTexts.GetString("Notification_JumpGate_Jumped").Replace("{%0}", final_count.ToString()).Replace("{%1}", entity_count.ToString());
				on_jump_callback((final_count > 0) ? MyJumpFailReason.SUCCESS : MyJumpFailReason.NO_ENTITIES_JUMPED, final_count > 0, message);
			};

			this.CanDoSyphonGridPower(syphon_power, 180, power_syphon_callback, false);
		}

		/// <summary>
		/// Begins a standard activation sequence for this gate<br />
		/// Destination is the void; <b>All jump entities will be destroyed</b><br />
		/// This method must be called on singleplayer or multiplayer server
		/// </summary>
		/// <param name="controller_settings">The controller settings to use for the jump</param>
		/// <param name="distance">The distance to use for power calculations</param>
		private void ExecuteJumpToVoid(MyJumpGateController.MyControllerBlockSettingsStruct controller_settings, double distance)
		{
			if (this.Closed || this.MarkClosed || !MyNetworkInterface.IsServerLike) return;
			controller_settings = controller_settings ?? this.ControlObject?.BlockSettings;
			Vector3D endpoint = this.WorldJumpNode + this.GetWorldMatrix(false, true).Forward * distance;
			Vector3D animation_endpoint = this.WorldJumpNode + this.GetWorldMatrix(false, true).Forward * 1e6;
			this.TrueEndpoint = endpoint;
			MyJumpGateAnimation gate_animation = null;
			List<KeyValuePair<MyEntity, bool>> entities_to_jump = null;
			List<MyJumpGate> other_gates = new List<MyJumpGate>();
			Func<MyJumpGateDrive, bool> working_src_drive_filter = (drive) => drive.JumpGateID == this.JumpGateID && drive.IsWorking;
			MyJumpFailReason result = MyJumpFailReason.NONE;
			this.JumpFailureReason = new KeyValuePair<MyJumpFailReason, bool>(MyJumpFailReason.IN_PROGRESS, true);
			bool is_init = true;
			Vector3D jump_node = this.WorldJumpNode;
			JumpGateUUID this_id = JumpGateUUID.FromJumpGate(this);
			BoundingEllipsoidD jump_ellipse = this.JumpEllipse;

			Action<MyJumpFailReason, bool, string> SendJumpResponse = (reason, result_status, override_message) => {
				this.JumpFailureReason = new KeyValuePair<MyJumpFailReason, bool>((result_status) ? MyJumpFailReason.SUCCESS : reason, is_init);
				string message = override_message ?? MyJumpGate.GetFailureDescription(this.JumpFailureReason.Key);
				this.Status = MyJumpGateStatus.SWITCHING;
				this.Phase = MyJumpGatePhase.RESETTING;

				// Is server, broadcast jump results
				if (MyNetworkInterface.IsMultiplayerServer)
				{
					MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet
					{
						PacketType = MyPacketTypeEnum.JUMP_GATE_JUMP,
						TargetID = 0,
						Broadcast = true,
					};

					packet.Payload(new JumpGateInfo
					{
						Type = (result_status) ? JumpGateInfo.TypeEnum.JUMP_SUCCESS : JumpGateInfo.TypeEnum.JUMP_FAIL,
						JumpGateID = JumpGateUUID.FromJumpGate(this),
						ControllerSettings = controller_settings,
						ResultMessage = message,
						CancelOverride = this.Status == MyJumpGateStatus.CANCELLED,
					});
					packet.Send();
				}

				Action<Exception> onend = (error) => {
					gate_animation?.Clean();
					this.TrueEndpoint = null;
					if (!this.Closed) this.Reset();
					if (entities_to_jump != null) foreach (KeyValuePair<MyEntity, bool> entity in entities_to_jump) entity.Key.Render.Visible = entity.Value;
				};

				if (gate_animation != null) MyJumpGateModSession.Instance.PlayAnimation(gate_animation, (result_status) ? MyJumpGateAnimation.AnimationTypeEnum.JUMPED : MyJumpGateAnimation.AnimationTypeEnum.FAILED, null, onend);
				else onend(null);
				--MyJumpGateModSession.Instance.ConcurrentJumpsCounter;
				this.SendHudMessage(message, 3000, (result_status) ? "White" : "Red");
				Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} END_JUMP {result_status}/{message}", 3);
			};

			try
			{
				// Check this gate
				Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} VOIDJUMP_PRECHECK", 3);
				++MyJumpGateModSession.Instance.ConcurrentJumpsCounter;

				if (!this.IsValid()) result = MyJumpFailReason.SRC_INVALID;
				else if (!this.IsControlled() || !this.Controller.IsWorking) result = MyJumpFailReason.CONTROLLER_NOT_CONNECTED;
				else if (this.JumpGateGrid.GetAttachedJumpGateDrives().Count(working_src_drive_filter) < 2) result = MyJumpFailReason.SRC_DISABLED;
				else if (this.Status == MyJumpGateStatus.NONE) result = MyJumpFailReason.SRC_NOT_CONFIGURED;
				else if (!this.IsIdle()) result = MyJumpFailReason.SRC_BUSY;
				else if (!controller_settings.CanBeInbound() && !controller_settings.CanBeOutbound()) result = MyJumpFailReason.SRC_ROUTING_DISABLED;
				else if (!controller_settings.CanBeOutbound()) result = MyJumpFailReason.SRC_INBOUND_ONLY;
				else if (MyJumpGateModSession.Instance.Configuration.GeneralConfiguration.MaxConcurrentJumps.Value > 0 && MyJumpGateModSession.Instance.ConcurrentJumpsCounter > MyJumpGateModSession.Instance.Configuration.GeneralConfiguration.MaxConcurrentJumps.Value) result = MyJumpFailReason.SUBSPACE_BUSY;

				// Check gate overlap
				if (result == MyJumpFailReason.NONE)
				{
					other_gates.AddRange(MyJumpGateModSession.Instance.GetAllJumpGates());

					foreach (MyJumpGate other_gate in other_gates)
					{
						if (other_gate != this && (other_gate.Status == MyJumpGateStatus.OUTBOUND) && jump_ellipse.Intersects(other_gate.JumpEllipse))
						{
							result = MyJumpFailReason.JUMP_SPACE_TRANSPOSED;
							break;
						}
					}

					other_gates.Clear();
				}

				// Send jump-failure if failed
				if (result != MyJumpFailReason.NONE)
				{
					SendJumpResponse(result, false, null);
					return;
				}

				// Setup gate animation
				string gate_animation_name = controller_settings.JumpEffectAnimationName();
				gate_animation = MyAnimationHandler.GetAnimation(gate_animation_name, MyAPIGateway.Session.Player, this, null, controller_settings, null, MyJumpTypeEnum.OUTBOUND_VOID);

				if (gate_animation == null)
				{
					SendJumpResponse(MyJumpFailReason.NULL_ANIMATION, false, null);
					return;
				}

				// Final setup
				double tick_duration = 1000d / 60d;
				double time_to_jump_ms = (gate_animation == null) ? 0 : gate_animation.Durations()[0] * tick_duration;
				IMyHudNotification hud_notification = MyAPIGateway.Utilities.CreateNotification(MyTexts.GetString("Notification_JumpGate_GateActivationIn").Replace("{%0}", "--.-s"), (int) Math.Round(time_to_jump_ms), "White");
				IMyHudNotification cancel_request_notificiation = null;

				if (!MyNetworkInterface.IsDedicatedMultiplayerServer)
				{
					hud_notification.Show();
					this.ShearBlocksWarning.AliveTime = (int) Math.Round(time_to_jump_ms);
				}

				// If server, broadcast jump start
				if (MyNetworkInterface.IsMultiplayerServer)
				{
					MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet
					{
						PacketType = MyPacketTypeEnum.JUMP_GATE_JUMP,
						TargetID = 0,
						Broadcast = true,
					};

					packet.Payload(new JumpGateInfo
					{
						Type = JumpGateInfo.TypeEnum.JUMP_START,
						JumpType = MyJumpTypeEnum.OUTBOUND_VOID,
						JumpGateID = JumpGateUUID.FromJumpGate(this),
						ControllerSettings = controller_settings,
						TargetControllerSettings = null,
						TrueEndpoint = endpoint,
					});

					packet.Send();
				}

				// Update gate status
				foreach (MyJumpGateDrive drive in this.GetJumpGateDrives()) drive.InvalidatePowerCheck();
				this.Status = MyJumpGateStatus.OUTBOUND;
				this.Phase = MyJumpGatePhase.CHARGING;
				is_init = false;
				MyJumpGateModSession.Instance.RedrawAllTerminalControls();
				Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} VOIDJUMP_CHARGE", 3);

				// Play animation and check for invalidation of jump event
				MyJumpGateModSession.Instance.PlayAnimation(gate_animation, MyJumpGateAnimation.AnimationTypeEnum.JUMPING, () => {
					if (!this.IsValid()) result = MyJumpFailReason.SRC_INVALID;
					else if (!this.IsControlled() || !this.Controller.IsWorking) result = MyJumpFailReason.CONTROLLER_NOT_CONNECTED;
					else if (this.JumpGateGrid.GetAttachedJumpGateDrives().Count(working_src_drive_filter) < 2) result = MyJumpFailReason.SRC_DISABLED;
					else if (this.Status == MyJumpGateStatus.NONE) result = MyJumpFailReason.SRC_NOT_CONFIGURED;
					else if (!controller_settings.CanBeInbound() && !controller_settings.CanBeOutbound()) result = MyJumpFailReason.SRC_ROUTING_CHANGED;
					else if (!controller_settings.CanBeOutbound()) result = MyJumpFailReason.SRC_ROUTING_CHANGED;

					if (MyJumpGateModSession.Instance.GameTick % 30 == 0)
					{
						other_gates.AddRange(MyJumpGateModSession.Instance.GetAllJumpGates());

						foreach (MyJumpGate other_gate in other_gates)
						{
							if (other_gate != this && other_gate.Status == MyJumpGateStatus.OUTBOUND && jump_ellipse.Intersects(other_gate.JumpEllipse))
							{
								result = MyJumpFailReason.JUMP_SPACE_TRANSPOSED;
								break;
							}
						}

						other_gates.Clear();
					}

					time_to_jump_ms -= tick_duration;
					hud_notification.Text = MyTexts.GetString("Notification_JumpGate_GateActivationIn").Replace("{%0}", MyJumpGateModSession.AutoconvertMetricUnits(Math.Max(0, time_to_jump_ms / 1000d), "s", 1));
					hud_notification.Hide();
					cancel_request_notificiation?.Hide();
					Vector3D character_pos = MyAPIGateway.Session.LocalHumanPlayer?.GetPosition() ?? Vector3D.PositiveInfinity;
					bool show_notifs = character_pos != null && this.IsPointInHudBroadcastRange(ref character_pos);

					if (show_notifs)
					{
						hud_notification.Show();
						cancel_request_notificiation?.Show();
						if (this.ShearBlocks.Count == 0) this.ShearBlocksWarning?.Hide();
						else this.ShearBlocksWarning.Show();
					}

					if (this.Closed || this.JumpGateGrid == null || this.JumpGateGrid.Closed) result = MyJumpFailReason.SRC_CLOSED;
					else if (this.Status == MyJumpGateStatus.CANCELLED && gate_animation.ImmediateCancel) result = MyJumpFailReason.CANCELLED;
					else if (this.Status == MyJumpGateStatus.CANCELLED && cancel_request_notificiation == null && show_notifs)
					{
						cancel_request_notificiation = MyAPIGateway.Utilities.CreateNotification(MyTexts.GetString("Notification_JumpGate_JumpCancelling"), (int) Math.Round(time_to_jump_ms), "Red");
						cancel_request_notificiation.Show();
					}

					foreach (MyJumpGateDrive drive in this.GetWorkingJumpGateDrives())
					{
						if (drive.IsNullWrapper) continue;
						Vector3D force_dir = (drive.TerminalBlock.WorldMatrix.Translation - this.WorldJumpNode).Normalized() * this.JumpGateConfiguration.ChargingDriveEffectorForceN;
						drive.TerminalBlock.GetTopMostParent().Physics.AddForce(VRage.Game.Components.MyPhysicsForceType.APPLY_WORLD_FORCE, force_dir, drive.TerminalBlock.WorldMatrix.Translation, null);
					}

					return result == MyJumpFailReason.NONE;
				}, (Exception err) => {

					try
					{
						// Confirm no errors occured
						Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} VOIDJUMP_POSTCHECK", 3);
						if (err != null) result = MyJumpFailReason.UNKNOWN_ERROR;
						else if (this.Status == MyJumpGateStatus.CANCELLED) result = MyJumpFailReason.CANCELLED;
						hud_notification.Hide();
						cancel_request_notificiation?.Hide();
						this.ShearBlocksWarning?.Hide();
						this.Phase = MyJumpGatePhase.JUMPING;

						if (result != MyJumpFailReason.NONE)
						{
							SendJumpResponse(result, false, null);
							return;
						}

						entities_to_jump = this.GetEntitiesInJumpSpace(true).Select((pair) => new KeyValuePair<MyEntity, bool>(pair.Key, pair.Key.Render.Visible)).ToList();

						if (entities_to_jump.Count == 0)
						{
							SendJumpResponse(MyJumpFailReason.NO_ENTITIES, false, null);
							return;
						}

						// Calculate Power
						double total_mass_kg = entities_to_jump.Sum((entity) => (double) entity.Key.Physics.Mass);
						double power_scaler = this.JumpGateConfiguration.UntetheredJumpEnergyCost;
						double remaining_power_mw = Math.Round(total_mass_kg * this.CalculateUnitPowerRequiredForJump(ref endpoint), 8) * power_scaler / 1000;
						Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... INSTANT_POWER_SYPHON - TOTAL_MASS={total_mass_kg} Kg; AVAILABLE_INSTANT_POWER={this.CalculateTotalAvailableInstantPower()} MW; MAX_AVAILABLE_INSTANT_POWER={this.CalculateTotalPossibleInstantPower()}", 4);
						Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... ... INITIAL - REQUIRED_POWER={remaining_power_mw} Mw", 4);
						remaining_power_mw = Math.Round(this.SyphonGridDrivePower(remaining_power_mw), 8);
						Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... ... POST_DRIVES - REQUIRED_POWER={remaining_power_mw} Mw", 4);
						if (remaining_power_mw > 0) remaining_power_mw = Math.Round(this.JumpGateGrid.SyphonConstructCapacitorPower(remaining_power_mw), 8);
						Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... ... POST_CAPACITORS - REQUIRED_POWER={remaining_power_mw} Mw", 4);

						if (!MyJumpGateModSession.Instance.Configuration.GeneralConfiguration.SyphonReactorPower.Value && remaining_power_mw > 0)
						{
							SendJumpResponse(MyJumpFailReason.INSUFFICIENT_POWER, false, null);
							return;
						}

						Action<bool> power_syphon_callback = (power_syphoned) => {
							if (!power_syphoned)
							{
								SendJumpResponse(MyJumpFailReason.INSUFFICIENT_POWER, false, null);
								return;
							}

							List<MyEntity> subentities = new List<MyEntity>();
							List<IMySlimBlock> destroyed = new List<IMySlimBlock>();
							List<IMyCubeGrid> grids = new List<IMyCubeGrid>();
							MatrixD this_matrix = this.TrueWorldJumpEllipse.WorldMatrix;

							// Jump entities
							Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} VOIDJUMP_ENTITY_TRANSIT - Teleporting batches...", 3);
							int skipped_entities_count = 0;
							BoundingEllipsoidD effective_target_ellipse = this.GetEffectiveJumpEllipse();

							for (int i = 0; i < entities_to_jump.Count; ++i)
							{
								MyEntity entity = entities_to_jump[i].Key;
								MyJumpGateConstruct parent = null;
								Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... TELEPORTING - ENTITY={entity.EntityId}; ISGRID={parent != null}", 4);

								if (entity is MyCubeGrid)
								{
									parent = MyJumpGateModSession.Instance.GetUnclosedJumpGateGrid(entity.EntityId);
									if (parent == null) continue;
									grids.AddRange(parent.GetCubeGrids());

									foreach (IMyCubeGrid grid in grids)
									{
										// Shear constraints attaching this cube grid to this jump gate
										BoundingBoxD box = grid.WorldAABB;
										MyGamePruningStructure.GetTopmostEntitiesInBox(ref box, subentities);
										MyJumpGateConstruct subparent;

										foreach (MyEntity sub_entity in subentities)
										{
											subparent = (sub_entity is MyCubeGrid) ? MyJumpGateModSession.Instance.GetUnclosedJumpGateGrid(sub_entity.EntityId) : null;
											if (sub_entity != entity && subparent != parent && MyJumpGateModSession.IsPositionInsideGrid(grid, sub_entity.WorldMatrix.Translation)) entities_to_jump.Add(new KeyValuePair<MyEntity, bool>(sub_entity, sub_entity.Render.Visible));
										}

										subentities.Clear();

										foreach (IMyShipConnector connector in grid.GetFatBlocks<IMyShipConnector>())
										{
											if (connector.IsWorking && connector.Status == Sandbox.ModAPI.Ingame.MyShipConnectorStatus.Connected && this.JumpGateGrid.HasCubeGrid(connector.OtherConnector.CubeGrid))
											{
												destroyed.Add(connector.SlimBlock);
											}
										}

										foreach (IMyLandingGear gear in grid.GetFatBlocks<IMyLandingGear>())
										{
											IMyEntity attached_entity;
											if (gear.IsWorking && gear.IsLocked && (attached_entity = gear.GetAttachedEntity()) is IMyCubeGrid && this.JumpGateGrid.HasCubeGrid(attached_entity as IMyCubeGrid))
											{
												destroyed.Add(gear.SlimBlock);
											}
										}

										if (destroyed.Count > 0 && this.JumpGateConfiguration.IgnoreDockedGrids)
										{
											++skipped_entities_count;
											Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... TP_FAIL_DOCKED", 4);
											continue;
										}
										else
										{
											foreach (IMySlimBlock block in destroyed) grid.RemoveDestroyedBlock(block);
										}

										destroyed.Clear();

										// Shear grid
										if (!MyJumpGateModSession.Instance.Configuration.GeneralConfiguration.LenientJumps.Value)
										{
											MyCubeGrid cubegrid = entity as MyCubeGrid;
											foreach (IMySlimBlock block in this.ShearBlocks) if (block.CubeGrid == grid) grid.RemoveDestroyedBlock(block);
											destroyed.AddRange(cubegrid.CubeBlocks.Where((block) => !jump_ellipse.IsPointInEllipse(grid.GridIntegerToWorld(((IMySlimBlock) block).Position))));
											parent.SplitGrid(grid, destroyed);
											destroyed.Clear();
										}
									}
								}

								// Teleport to void
								if (entity is IMyCharacter) ((IMyCharacter) entity).Die();
								else if (parent == null) entity.Close();
								else parent.Destroy();
							}

							int final_count = entities_to_jump.Count - skipped_entities_count;
							string message = MyTexts.GetString("Notification_JumpGate_Jumped").Replace("{%0}", final_count.ToString()).Replace("{%1}", entities_to_jump.Count.ToString());
							SendJumpResponse((final_count > 0) ? MyJumpFailReason.SUCCESS : MyJumpFailReason.NO_ENTITIES_JUMPED, final_count > 0, message);
						};

						this.CanDoSyphonGridPower(remaining_power_mw, 180, power_syphon_callback, true);
					}
					catch (Exception e)
					{
						SendJumpResponse(MyJumpFailReason.UNKNOWN_ERROR, false, null);
						int drive_count = this.GetJumpGateDrives().Count();
						int working_drive_count = this.GetWorkingJumpGateDrives().Count();
						Logger.Error($"Error during jump gate jump:\n\tPHASE=POSTANIM\n\tGATE_CUBE_GRID={this.JumpGateGrid?.CubeGrid?.EntityId.ToString() ?? "N/A"}\n\tGATE_ID={this.JumpGateID}\n\tGATE_SIZE={drive_count}\n\tGATE_SIZE_WORKING={working_drive_count}\n\n\tSTACKTRACE:\n{e}");
					}

				});
			}
			catch (Exception e)
			{
				SendJumpResponse(MyJumpFailReason.UNKNOWN_ERROR, false, null);
				int drive_count = this.GetJumpGateDrives().Count();
				int working_drive_count = this.GetWorkingJumpGateDrives().Count();
				Logger.Error($"Error during jump gate jump:\n\tPHASE=PREANIM\n\tGATE_CUBE_GRID={this.JumpGateGrid?.CubeGrid?.EntityId.ToString() ?? "N/A"}\n\tGATE_ID={this.JumpGateID}\n\tGATE_SIZE={drive_count}\n\tGATE_SIZE_WORKING={working_drive_count}\n\n\tSTACKTRACE:\n{e}");
			}
		}

		/// <summary>
		/// Begins a standard activation sequence for this gate<br />
		/// Destination is the void; <b>All jump entities will be destroyed</b><br />
		/// This method must be called on singleplayer or multiplayer server
		/// </summary>
		/// <param name="controller_settings">The controller settings to use for the jump</param>
		/// <param name="distance">The distance to use for power calculations</param>
		/// <param name="gravity_strength">The gravity to apply to this gate's jump node</param>
		private void ExecuteWormholeJumpToVoid(MyJumpGateController.MyControllerBlockSettingsStruct controller_settings, double distance, float gravity_strength)
		{
			if (this.Closed || this.MarkClosed || !MyNetworkInterface.IsServerLike) return;
			controller_settings = controller_settings ?? this.ControlObject?.BlockSettings;
			MyJumpGateAnimation gate_animation = null;
			MyJumpGateWaypoint dst = controller_settings.SelectedWaypoint();
			List<MyJumpGate> other_gates = new List<MyJumpGate>();
			Func<MyJumpGateDrive, bool> working_src_drive_filter = (drive) => drive.JumpGateID == this.JumpGateID && drive.IsWorking;
			MyJumpFailReason result = MyJumpFailReason.NONE;
			this.JumpFailureReason = new KeyValuePair<MyJumpFailReason, bool>(MyJumpFailReason.IN_PROGRESS, true);
			this.AutoActivateStartTime = DateTime.UtcNow + new TimeSpan(0, 0, 30);
			bool is_init = true;
			Vector3D jump_node = this.WorldJumpNode;
			JumpGateUUID this_id = JumpGateUUID.FromJumpGate(this);
			BoundingEllipsoidD jump_ellipse = this.JumpEllipse;

			Vector3D endpoint = this.WorldJumpNode + this.GetWorldMatrix(false, true).Forward * distance;
			Vector3D animation_endpoint = this.WorldJumpNode + this.GetWorldMatrix(false, true).Forward * 1e6;
			double distance_to_endpoint = Vector3D.Distance(endpoint, jump_node);
			this.TrueEndpoint = endpoint;

			Action<MyJumpFailReason, bool, string> SendWormholeJumpResponse = (reason, result_status, override_message) => {
				string message = override_message ?? MyJumpGate.GetFailureDescription(reason);

				if (result_status)
				{
					DateTime now = DateTime.UtcNow;
					this.Status = MyJumpGateStatus.OUTBOUND;
					this.Phase = MyJumpGatePhase.JUMPING;
					this.WormholeStartTime = now;
					++MyJumpGateModSession.Instance.ConcurrentJumpsCounter;
					Logger.Debug($"[{this.JumpGateGrid?.CubeGridID ?? -1}]-{this.JumpGateID} BEGIN_WORMHOLE_LOOP", 3);
				}
				else
				{
					this.Status = MyJumpGateStatus.SWITCHING;
					this.Phase = MyJumpGatePhase.RESETTING;
					this.JumpFailureReason = new KeyValuePair<MyJumpFailReason, bool>(reason, is_init);

					Action<Exception> onend = (error) => {
						gate_animation?.Clean();
						this.TrueEndpoint = null;
						this.EntityBatches?.Clear();
						if (!this.Closed) this.Reset();
					};

					if (gate_animation != null) MyJumpGateModSession.Instance.PlayAnimation(gate_animation, MyJumpGateAnimation.AnimationTypeEnum.WORMHOLE_CLOSE, null, onend);
					else onend(null);
					--MyJumpGateModSession.Instance.ConcurrentJumpsCounter;
					this.SendHudMessage(message, 3000, "Red");
					Logger.Debug($"[{this.JumpGateGrid?.CubeGridID ?? -1}]-{this.JumpGateID} END_WORMHOLE_JUMP {result_status}/{message}", 3);
				}

				// Is server, broadcast jump results
				if (MyNetworkInterface.IsMultiplayerServer)
				{
					MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet
					{
						PacketType = MyPacketTypeEnum.JUMP_GATE_JUMP,
						TargetID = 0,
						Broadcast = true,
					};

					packet.Payload(new JumpGateInfo
					{
						Type = (result_status) ? JumpGateInfo.TypeEnum.WORMHOLE_OPEN : JumpGateInfo.TypeEnum.WORMHOLE_FAIL,
						JumpType = MyJumpTypeEnum.OUTBOUND_VOID,
						JumpGateID = (this.JumpGateGrid == null) ? this_id : JumpGateUUID.FromJumpGate(this),
						ResultMessage = message,
						CancelOverride = this.Status == MyJumpGateStatus.CANCELLED,
					});
					packet.Send();
					Logger.Debug($"[{this.JumpGateGrid.CubeGridID}-{this.JumpGateID}]: Sent network server wormhole jump response");
				}
			};

			Action<MyJumpFailReason, bool, string, IEnumerable<MyEntity>> SendJumpResponse = (reason, result_status, override_message, active_entity_batch) => {
				string message = override_message ?? MyJumpGate.GetFailureDescription(reason);
				this.SendHudMessage(message, 3000, (result_status) ? "White" : "Red");

				// Is server, broadcast jump results
				if (MyNetworkInterface.IsMultiplayerServer)
				{
					List<EntityWarpInfo> batch_warps = new List<EntityWarpInfo>();
					MyJumpGateModSession.Instance.GetEntityBatchWarpsForGate(this, batch_warps);

					MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet
					{
						PacketType = MyPacketTypeEnum.JUMP_GATE_JUMP,
						TargetID = 0,
						Broadcast = true,
					};

					packet.Payload(new JumpGateInfo
					{
						Type = (result_status) ? JumpGateInfo.TypeEnum.WORMHOLE_ENTITY_JUMP_SUCCESS : JumpGateInfo.TypeEnum.WORMHOLE_ENTITY_JUMP_FAIL,
						JumpType = MyJumpTypeEnum.OUTBOUND_VOID,
						JumpGateID = (this.JumpGateGrid == null) ? this_id : JumpGateUUID.FromJumpGate(this),
						ResultMessage = message,
						CancelOverride = this.Status == MyJumpGateStatus.CANCELLED,
						EntityBatches = this.EntityBatches?.Select((pair) => pair.Value.ToSerialized()).ToList(),
						EntityWarps = batch_warps.Select((warp) => warp.ToSerialized()).ToList(),
						EntityIds = active_entity_batch?.Select((entity) => entity.EntityId)?.ToList(),
					});
					packet.Send();
					Logger.Debug($"[{this.JumpGateGrid.CubeGridID}-{this.JumpGateID}]: Sent network server wormhole jump response");
				}
			};

			try
			{
				// Check this gate
				Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} WORMHOLE_VOID_JUMP_PRECHECK", 3);
				++MyJumpGateModSession.Instance.ConcurrentJumpsCounter;

				if (!this.JumpGateConfiguration.AllowWormholeActivation) result = MyJumpFailReason.DESTINATION_UNAVAILABLE;
				else if (!this.IsValid()) result = MyJumpFailReason.SRC_INVALID;
				else if (!this.IsControlled() || !this.Controller.IsWorking) result = MyJumpFailReason.CONTROLLER_NOT_CONNECTED;
				else if (this.JumpGateGrid.GetAttachedJumpGateDrives().Count(working_src_drive_filter) < 2) result = MyJumpFailReason.SRC_DISABLED;
				else if (this.Status == MyJumpGateStatus.NONE) result = MyJumpFailReason.SRC_NOT_CONFIGURED;
				else if (!this.IsIdle()) result = MyJumpFailReason.SRC_BUSY;
				else if (!controller_settings.CanBeInbound() && !controller_settings.CanBeOutbound()) result = MyJumpFailReason.SRC_ROUTING_DISABLED;
				else if (!controller_settings.CanBeOutbound()) result = MyJumpFailReason.SRC_INBOUND_ONLY;
				else if (dst == null || dst.WaypointType != MyWaypointType.JUMP_GATE) result = MyJumpFailReason.NULL_DESTINATION;
				else if (MyJumpGateModSession.Instance.Configuration.GeneralConfiguration.MaxConcurrentJumps.Value > 0 && MyJumpGateModSession.Instance.ConcurrentJumpsCounter > MyJumpGateModSession.Instance.Configuration.GeneralConfiguration.MaxConcurrentJumps.Value) result = MyJumpFailReason.SUBSPACE_BUSY;
				else if (MyJumpGateModSession.Instance.Configuration.JumpGateConfiguration.JumpGateExplosionConfiguration.EnableGateExplosions.Value && 1 - this.GetFunctionalDrivePercentage() > this.JumpGateConfiguration.ExplosionDamagePercent) result = MyJumpFailReason.SRC_DAMAGED;

				// Check gate overlap
				if (result == MyJumpFailReason.NONE)
				{
					other_gates.AddRange(MyJumpGateModSession.Instance.GetAllJumpGates());

					foreach (MyJumpGate other_gate in other_gates)
					{
						if (other_gate != this && (other_gate.Status == MyJumpGateStatus.OUTBOUND) && jump_ellipse.Intersects(other_gate.JumpEllipse))
						{
							result = MyJumpFailReason.JUMP_SPACE_TRANSPOSED;
							break;
						}
					}

					other_gates.Clear();
				}

				// Send jump-failure if failed
				if (result != MyJumpFailReason.NONE)
				{
					SendWormholeJumpResponse(result, false, null);
					return;
				}

				// Setup gate animation
				string gate_animation_name = controller_settings.JumpEffectAnimationName();
				gate_animation = MyAnimationHandler.GetAnimation(gate_animation_name, MyAPIGateway.Session.Player, this, null, controller_settings, controller_settings, MyJumpTypeEnum.OUTBOUND_VOID);

				if (gate_animation == null)
				{
					SendWormholeJumpResponse(MyJumpFailReason.NULL_ANIMATION, false, null);
					return;
				}

				// Final setup
				double tick_duration = 1000d / 60d;
				double time_to_jump_ms = gate_animation.Durations()[3] * tick_duration;
				IMyHudNotification hud_notification = MyAPIGateway.Utilities.CreateNotification(MyTexts.GetString("Notification_JumpGate_GateActivationIn").Replace("{%0}", "--.-s"), (int) Math.Round(time_to_jump_ms), "White");
				IMyHudNotification cancel_request_notificiation = null;

				if (!MyNetworkInterface.IsDedicatedMultiplayerServer)
				{
					hud_notification.Show();
					this.ShearBlocksWarning.AliveTime = (int) Math.Round(time_to_jump_ms);
				}

				// If server, broadcast jump start
				if (MyNetworkInterface.IsMultiplayerServer)
				{
					MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet
					{
						PacketType = MyPacketTypeEnum.JUMP_GATE_JUMP,
						TargetID = 0,
						Broadcast = true,
					};

					packet.Payload(new JumpGateInfo
					{
						Type = JumpGateInfo.TypeEnum.WORMHOLE_START,
						JumpType = MyJumpTypeEnum.OUTBOUND_VOID,
						JumpGateID = JumpGateUUID.FromJumpGate(this),
						ControllerSettings = controller_settings,
						TargetControllerSettings = null,
						TrueEndpoint = endpoint,
					});

					packet.Send();
				}

				// Update gate status
				foreach (MyJumpGateDrive drive in this.GetJumpGateDrives()) drive.InvalidatePowerCheck();
				this.Status = MyJumpGateStatus.OUTBOUND;
				this.Phase = MyJumpGatePhase.CHARGING;
				is_init = false;
				MyJumpGateModSession.Instance.RedrawAllTerminalControls();
				Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} WORMHOLE_JUMP_CHARGE, TARGET_TYPE={dst.WaypointType}, ENDPOINT={endpoint}, DISTANCE={distance_to_endpoint}", 3);

				// Play animation and check for invalidation of jump event
				MyJumpGateModSession.Instance.PlayAnimation(gate_animation, MyJumpGateAnimation.AnimationTypeEnum.WORMHOLE_OPEN, () => {
					time_to_jump_ms -= tick_duration;
					hud_notification.Text = MyTexts.GetString("Notification_JumpGate_GateActivationIn").Replace("{%0}", MyJumpGateModSession.AutoconvertTimeHHMMSS(time_to_jump_ms / 1000d));
					hud_notification.Hide();
					cancel_request_notificiation?.Hide();
					Vector3D character_pos = MyAPIGateway.Session.LocalHumanPlayer?.GetPosition() ?? Vector3D.PositiveInfinity;
					bool show_notifs = character_pos != null && this.IsPointInHudBroadcastRange(ref character_pos);

					if (show_notifs)
					{
						hud_notification.Show();
						cancel_request_notificiation?.Show();
						if (this.ShearBlocks.Count == 0) this.ShearBlocksWarning?.Hide();
						else this.ShearBlocksWarning.Show();
					}

					if (this.Closed || this.JumpGateGrid == null || this.JumpGateGrid.Closed) result = MyJumpFailReason.SRC_CLOSED;
					else if (this.Status == MyJumpGateStatus.CANCELLED && gate_animation.ImmediateCancel) result = MyJumpFailReason.CANCELLED;
					else if (this.Status == MyJumpGateStatus.CANCELLED && cancel_request_notificiation == null && show_notifs)
					{
						cancel_request_notificiation = MyAPIGateway.Utilities.CreateNotification(MyTexts.GetString("Notification_JumpGate_JumpCancelling"), (int) Math.Round(time_to_jump_ms), "Red");
						cancel_request_notificiation.Show();
					}

					List<MyJumpGateDrive> working_drives = new List<MyJumpGateDrive>();
					working_drives.AddRange(this.GetWorkingJumpGateDrives());

					foreach (MyJumpGateDrive drive in working_drives)
					{
						if (drive.IsNullWrapper) continue;
						Vector3D force_dir = (drive.TerminalBlock.WorldMatrix.Translation - this.WorldJumpNode).Normalized() * this.JumpGateConfiguration.ChargingDriveEffectorForceN;
						drive.TerminalBlock.GetTopMostParent().Physics.AddForce(VRage.Game.Components.MyPhysicsForceType.APPLY_WORLD_FORCE, force_dir, drive.TerminalBlock.WorldMatrix.Translation, null);
					}

					return result == MyJumpFailReason.NONE;
				}, (Exception err) => {

					try
					{
						// Confirm no errors occured
						Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} WORMHOLE_JUMP_POST_CHECK", 3);
						if (err != null) result = MyJumpFailReason.UNKNOWN_ERROR;
						else if (this.Status == MyJumpGateStatus.CANCELLED) result = MyJumpFailReason.CANCELLED;
						hud_notification.Hide();
						cancel_request_notificiation?.Hide();
						this.ShearBlocksWarning?.Hide();

						if (result != MyJumpFailReason.NONE)
						{
							SendWormholeJumpResponse(result, false, null);
							return;
						}
						else if (dst.WaypointType == MyWaypointType.SERVER)
						{
							SendWormholeJumpResponse(MyJumpFailReason.CROSS_SERVER_JUMP, false, null);
							return;
						}

						SendWormholeJumpResponse(MyJumpFailReason.SUCCESS, true, null);
						ulong wormhole_tick = 0;
						float max_wormhole_duration = controller_settings.GateWormholeAutoCloseTime();
						max_wormhole_duration = (max_wormhole_duration == 0) ? this.JumpGateConfiguration.MaxWormholeDurationSeconds : Math.Min(this.JumpGateConfiguration.MaxWormholeDurationSeconds, max_wormhole_duration);
						List<MyEntity> entities_to_jump = new List<MyEntity>();
						List<KeyValuePair<List<MyEntity>, MyJumpGateAnimation>> entity_animation_batches = new List<KeyValuePair<List<MyEntity>, MyJumpGateAnimation>>();
						const double gravity = 9.80665;
						double remote_gravity_strength = gravity_strength / gravity * this.JumpGateConfiguration.GravityPassthroughMultiplier;
						double lower_gravity_limit = 0.05;
						double r1 = jump_ellipse.Radii.Max();
						double r2 = r1 / Math.Sqrt(lower_gravity_limit / remote_gravity_strength);
						IMySphericalNaturalGravityComponent remote_gravity = MyAPIGateway.GravityProviderSystem.AddNaturalGravityToEntity(this.JumpSpaceCollisionDetector, r1, r2, 2, remote_gravity_strength);

						MyJumpGateModSession.Instance.PlayAnimation(gate_animation, MyJumpGateAnimation.AnimationTypeEnum.WORMHOLE_LOOP, () => {
							if (wormhole_tick++ % 15 != 0) return true;

							// Check validity
							result = MyJumpFailReason.NONE;
							if (!this.IsValid()) result = MyJumpFailReason.SRC_INVALID;
							else if (!this.IsControlled() || !this.Controller.IsWorking) result = MyJumpFailReason.CONTROLLER_NOT_CONNECTED;
							else if (!this.JumpGateGrid.GetAttachedJumpGateDrives().Where(working_src_drive_filter).AtLeast(2)) result = MyJumpFailReason.SRC_DISABLED;
							else if (this.Status == MyJumpGateStatus.NONE) result = MyJumpFailReason.SRC_NOT_CONFIGURED;
							else if (this.Status == MyJumpGateStatus.CANCELLED) result = MyJumpFailReason.CANCELLED;
							else if (!controller_settings.CanBeInbound() && !controller_settings.CanBeOutbound()) result = MyJumpFailReason.SRC_ROUTING_CHANGED;
							else if (!controller_settings.CanBeOutbound()) result = MyJumpFailReason.SRC_ROUTING_CHANGED;
							else if (dst == null || dst.WaypointType == MyWaypointType.NONE) result = MyJumpFailReason.NULL_DESTINATION;
							else if (!controller_settings.DoAdminWormholeBypass() && this.JumpGateConfiguration.OverrunPowerMultiplierPerSecond == 0 && (DateTime.UtcNow - this.WormholeStartTime.Value).TotalSeconds >= max_wormhole_duration) result = MyJumpFailReason.WORMHOLE_TIMEOUT;

							// Check gate overlap
							if (MyJumpGateModSession.Instance.GameTick % 30 == 0)
							{
								other_gates.AddRange(MyJumpGateModSession.Instance.GetAllJumpGates());

								foreach (MyJumpGate other_gate in other_gates)
								{
									if (other_gate != this && other_gate.Status == MyJumpGateStatus.OUTBOUND && jump_ellipse.Intersects(other_gate.JumpEllipse))
									{
										result = MyJumpFailReason.JUMP_SPACE_TRANSPOSED;
										break;
									}
								}

								other_gates.Clear();
							}

							// Check power
							foreach (MyJumpGateDrive drive in this.GetWorkingJumpGateDrives())
							{
								if (drive.IsPowered) continue;
								result = MyJumpFailReason.INSUFFICIENT_POWER;
								break;
							}

							// Update gravity
							r1 = this.JumpEllipse.Radii.Max();
							r2 = r1 / Math.Sqrt(lower_gravity_limit / remote_gravity_strength);
							remote_gravity.MinRadius = r1;
							remote_gravity.MaxRadius = r2;

							// Jump entities
							this.UpdateJumpSpaceEntities();
							entities_to_jump.AddRange(this.GetEntitiesReadyForJump(true)
								.Select((pair) => pair.Key)
								.Where((entity) => !entity_animation_batches.Any((pair) => pair.Key.Contains(entity)))
								.Take(128));

							if (entities_to_jump.Count > 0 && entity_animation_batches.Count < 16)
							{
								List<MyEntity> active_batch = new List<MyEntity>(entities_to_jump.Distinct().Where((entity) => entity?.Physics != null && !entity.MarkedForClose));
								MyJumpGateAnimation batch_animation = MyAnimationHandler.GetAnimation(gate_animation_name, MyAPIGateway.Session.Player, this, null, controller_settings, null, MyJumpTypeEnum.STANDARD, active_batch);
								KeyValuePair<List<MyEntity>, MyJumpGateAnimation> batch_pair = new KeyValuePair<List<MyEntity>, MyJumpGateAnimation>(active_batch, batch_animation);
								entity_animation_batches.Add(batch_pair);
								entities_to_jump.Clear();
								MyJumpGateModSession.Instance.PlayAnimation(batch_animation, MyJumpGateAnimation.AnimationTypeEnum.JUMPING, () => {
									return this.IsWormholeActive && active_batch.All((entity) => entity != null && entity.Physics != null && !entity.MarkedForClose);
								}, (Exception e) => {
									if (!this.IsWormholeActive) return;

									foreach (MyEntity entity in active_batch)
									{
										MyJumpGateConstruct parent = MyJumpGateModSession.Instance.GetUnclosedJumpGateGrid(entity.EntityId);
										if (entity is IMyCharacter) ((IMyCharacter) entity).Die();
										else if (parent == null) entity.Close();
										else parent.Destroy();
									}

									string message = MyTexts.GetString("Notification_JumpGate_Jumped").Replace("{%0}", active_batch.Count.ToString()).Replace("{%1}", active_batch.Count.ToString());
									SendJumpResponse((active_batch.Count > 0) ? MyJumpFailReason.SUCCESS : MyJumpFailReason.NO_ENTITIES_JUMPED, active_batch.Count > 0, message, active_batch);
								});

								if (MyJumpGateModSession.Instance.Network.Registered)
								{
									MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet
									{
										PacketType = MyPacketTypeEnum.JUMP_GATE_JUMP,
										Broadcast = true,
										TargetID = 0,
									};
									packet.Payload(new JumpGateInfo
									{
										ControllerSettings = controller_settings,
										TargetControllerSettings = null,
										JumpGateID = (this.JumpGateGrid == null) ? this_id : JumpGateUUID.FromJumpGate(this),
										Type = JumpGateInfo.TypeEnum.WORMHOLE_ENTITY_PREJUMP,
										EntityIds = active_batch.Select((entity) => entity.EntityId).ToList(),
									});
									packet.Send();
								}
							}

							if (result != MyJumpFailReason.NONE) SendWormholeJumpResponse(result, false, null);
							return result == MyJumpFailReason.NONE;
						}, (Exception e) => {
							if (this.JumpSpaceCollisionDetector != null) MyAPIGateway.GravityProviderSystem.RemoveNaturalGravityFromEntity(this.JumpSpaceCollisionDetector);
							if (result != MyJumpFailReason.NONE || !this.IsWormholeActive) return;
							SendWormholeJumpResponse(MyJumpFailReason.UNKNOWN_ERROR, false, null);
						});
					}
					catch (Exception e)
					{
						SendWormholeJumpResponse(MyJumpFailReason.UNKNOWN_ERROR, false, null);
						int drive_count = this.GetJumpGateDrives().Count();
						int working_drive_count = this.GetWorkingJumpGateDrives().Count();
						Logger.Error($"Error during jump gate wormhole jump:\n\tPHASE=POSTANIM\n\tGATE_CUBE_GRID={this.JumpGateGrid?.CubeGrid?.EntityId.ToString() ?? "N/A"}\n\tGATE_ID={this.JumpGateID}\n\tGATE_SIZE={drive_count}\n\tGATE_SIZE_WORKING={working_drive_count}\nMESSAGE={e.Message}\n\tSTACKTRACE:\n{e.StackTrace}");
					}

				});
			}
			catch (Exception e)
			{
				SendWormholeJumpResponse(MyJumpFailReason.UNKNOWN_ERROR, false, null);
				int drive_count = this.GetJumpGateDrives().Count();
				int working_drive_count = this.GetWorkingJumpGateDrives().Count();
				Logger.Error($"Error during jump gate wormhole jump:\n\tPHASE=PREANIM\n\tGATE_CUBE_GRID={this.JumpGateGrid?.CubeGrid?.EntityId.ToString() ?? "N/A"}\n\tGATE_ID={this.JumpGateID}\n\tGATE_SIZE={drive_count}\n\tGATE_SIZE_WORKING={working_drive_count}\nMESSAGE={e.Message}\n\tSTACKTRACE:\n{e.StackTrace}");
			}
		}
		#endregion
	}
}
