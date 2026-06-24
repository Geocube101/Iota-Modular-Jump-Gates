using IOTA.ModularJumpGates.Animation;
using IOTA.ModularJumpGates.CubeBlock;
using IOTA.ModularJumpGates.Util;
using IOTA.ModularJumpGates.Util.ConcurrentCollections;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;

namespace IOTA.ModularJumpGates.JumpGates
{
	internal partial class MyJumpGate
	{
		#region Private Variables
		/// <summary>
		/// Wether the jump handler is active
		/// </summary>
		private bool JumpHandlerActive = false;

		/// <summary>
		/// The last time this gate attempted an auto-jump
		/// </summary>
		private DateTime LastAutojumpAttemptTime = DateTime.MinValue;

		/// <summary>
		/// The time at which this gate passed an auto activation check
		/// </summary>
		private DateTime? AutoActivateStartTime = null;

		/// <summary>
		/// Notification for autoactivation status
		/// </summary>
		private readonly IMyHudNotification AutoActivationNotif = (MyNetworkInterface.IsDedicatedMultiplayerServer) ? null : MyAPIGateway.Utilities.CreateNotification("");

		/// <summary>
		/// Notification for whether blocks are within shear zone
		/// </summary>
		private readonly IMyHudNotification ShearBlocksWarning = (MyNetworkInterface.IsDedicatedMultiplayerServer) ? null : MyAPIGateway.Utilities.CreateNotification("!Warning! - Blocks detected in Damage Zone", font: "Red");

		/// <summary>
		/// The client-side network handler for jump responses<br />
		/// Always null on server
		/// </summary>
		private Action<MyNetworkInterface.Packet> OnNetworkJumpResponse = null;

		/// <summary>
		/// The client-side network handler for true endpoint updates<br />
		/// Always null on server
		/// </summary>
		private Action<MyNetworkInterface.Packet> OnNetworkUpdateJumpEndpoint = null;

		/// <summary>
		/// The client-side network handler for wormhole jumps<br />
		/// Always null on server
		/// </summary>
		private Action<MyNetworkInterface.Packet> OnNetworkWormholeJump = null;

		/// <summary>
		/// The response information from a jump<br />
		/// Always null on server
		/// </summary>
		private JumpGateInfo ServerJumpResponse = null;

		/// <summary>
		/// The list of entities arriving to this gate via wormhole
		/// </summary>
		private ConcurrentLinkedHashSet<MyEntity> ArrivingWormholeEntities = new ConcurrentLinkedHashSet<MyEntity>();
		#endregion

		#region Public Variables
		/// <summary>
		/// Whether the gate has an active sustained wormhole
		/// </summary>
		public bool IsWormholeActive => this.WormholeStartTime != null;

		/// <summary>
		/// The reason this gate's last jump failed or SUCCESS if successfull or NONE if unset and whether failure was during INIT phase
		/// </summary>
		public KeyValuePair<MyJumpFailReason, bool> JumpFailureReason { get; private set; } = new KeyValuePair<MyJumpFailReason, bool>(MyJumpFailReason.NONE, true);

		/// <summary>
		/// The time at which this gate opened as a wormhole
		/// </summary>
		public DateTime? WormholeStartTime { get; private set; } = null;

		/// <summary>
		/// A map of jump space entities and their children entities
		/// </summary>
		public ConcurrentDictionary<MyEntity, EntityBatch> EntityBatches { get; private set; } = new ConcurrentDictionary<MyEntity, EntityBatch>();
		#endregion

		#region Private Methods
		/// <summary>
		/// Callback triggered when this object recevies a gate jump request from server
		/// </summary>
		/// <param name="packet">The jump request packet</param>
		private void OnNetworkJumpGateJump(MyNetworkInterface.Packet packet)
		{
			if (packet == null) return;
			JumpGateInfo jump_request = packet.Payload<JumpGateInfo>();
			if (jump_request == null || jump_request.JumpGateID != JumpGateUUID.FromJumpGate(this)) return;

			if (MyNetworkInterface.IsMultiplayerServer && packet.PhaseFrame == 1)
			{
				if (this.Closed || this.MarkClosed)
				{
					packet.Payload(new JumpGateInfo
					{
						Type = JumpGateInfo.TypeEnum.CLOSED,
						CancelOverride = false,
						JumpGateID = JumpGateUUID.FromJumpGate(this),
						ResultMessage = "",
					});
					packet.TargetID = packet.SenderID;
					packet.Broadcast = false;
					packet.Send();
					return;
				}

				switch (jump_request.Type)
				{
					case JumpGateInfo.TypeEnum.JUMP_START:
						MyJumpGateController.MyControllerBlockSettingsStruct controller_settings = this.ControlObject?.BlockSettings;

						if (controller_settings == null)
						{
							packet = packet.Forward(packet.SenderID, false);
							packet.Payload(new JumpGateInfo
							{
								Type = JumpGateInfo.TypeEnum.JUMP_FAIL,
								JumpType = MyJumpTypeEnum.STANDARD,
								JumpGateID = JumpGateUUID.FromJumpGate(this),
								ResultMessage = MyJumpGate.GetFailureDescription(MyJumpFailReason.CONTROLLER_NOT_CONNECTED),
								CancelOverride = false,
							});
							packet.Send();
							Logger.Debug($"[{this.JumpGateGrid.CubeGridID}-{this.JumpGateID}]: Sent network server jump response; NULL_CONTROLLER_SETTINGS");
						}
						else this.JumpWrapper(controller_settings, null, null, MyJumpTypeEnum.STANDARD);

						break;
					case JumpGateInfo.TypeEnum.JUMP_FAIL:
						if (jump_request.CancelOverride) this.CancelJump();
						break;
				}
			}
			else if (MyNetworkInterface.IsStandaloneMultiplayerClient && packet.PhaseFrame <= 2)
			{
				switch (jump_request.Type)
				{
					case JumpGateInfo.TypeEnum.JUMP_START:
						this.AutoActivationNotif?.Hide();
						this.ExecuteJumpClient(jump_request.ControllerSettings, jump_request.TargetControllerSettings, jump_request.TrueEndpoint.Value, jump_request.JumpType);
						break;
					case JumpGateInfo.TypeEnum.WORMHOLE_START:
						this.AutoActivationNotif?.Hide();
						this.ExecuteWormholeClient(jump_request.ControllerSettings, jump_request.TargetControllerSettings, jump_request.TrueEndpoint.Value, jump_request.JumpType);
						break;
					case JumpGateInfo.TypeEnum.JUMP_FAIL:
					case JumpGateInfo.TypeEnum.WORMHOLE_FAIL:
						if (!this.JumpHandlerActive) this.SendHudMessage(jump_request.ResultMessage, font: "Red");
						break;
					case JumpGateInfo.TypeEnum.JUMP_SUCCESS:
						if (!this.JumpHandlerActive) this.SendHudMessage(jump_request.ResultMessage);
						break;
				}
			}
		}

		/// <summary>
		/// Wrapper which executes the jump animation depending on multiplayer status
		/// </summary>
		/// <param name="controller_settings">The controller settings to use for the jump</param>
		/// <param name="target_controller_settings">The controller settings of the target gate's controller</param>
		/// <param name="client_true_endpoint">The true endpoint for use on clients or null on server</param>
		/// <param name="jump_type">The type of this jump for use on clients</param>
		private void JumpWrapper(MyJumpGateController.MyControllerBlockSettingsStruct controller_settings, MyJumpGateController.MyControllerBlockSettingsStruct target_controller_settings, Vector3D? client_true_endpoint, MyJumpTypeEnum jump_type)
		{
			controller_settings = controller_settings ?? this.ControlObject?.BlockSettings;
			if (MyNetworkInterface.IsServerLike && controller_settings.DoSustainedWormhole()) this.ExecuteWormholeServer(controller_settings, target_controller_settings);
			else if (MyNetworkInterface.IsServerLike) this.ExecuteJumpServer(controller_settings, target_controller_settings);
			else if (MyNetworkInterface.IsMultiplayerClient && controller_settings.DoSustainedWormhole()) this.ExecuteWormholeClient(controller_settings, target_controller_settings, client_true_endpoint.Value, jump_type);
			else if (MyNetworkInterface.IsMultiplayerClient) this.ExecuteJumpClient(controller_settings, target_controller_settings, client_true_endpoint.Value, jump_type);
		}

		/// <summary>
		/// Executes the gate jump for multiplayer client sessons
		/// </summary>
		/// <param name="controller_settings">The controller settings to use for the jump</param>
		private void ExecuteJumpClient(MyJumpGateController.MyControllerBlockSettingsStruct controller_settings, MyJumpGateController.MyControllerBlockSettingsStruct target_controller_settings, Vector3D true_endpoint, MyJumpTypeEnum jump_type)
		{
			if (this.Closed) return;
			Logger.Debug($"[{this.JumpGateGrid?.CubeGridID ?? -1}]-{this.JumpGateID} BEGIN_JUMP CLIENT PRE_CHECK", 3);
			MyJumpGate target_gate = null;
			MyJumpGateAnimation gate_animation = null;
			MyJumpGateWaypoint dst = controller_settings.SelectedWaypoint();
			this.ServerJumpResponse = null;
			Action<string, bool> SendJumpResponse = null;
			Vector3D jump_node = this.WorldJumpNode;
			IMyHudNotification hud_notification = null;
			JumpGateUUID this_id = JumpGateUUID.FromJumpGate(this);
			this.AutoActivateStartTime = null;
			this.AutoActivationNotif.Hide();

			Action<MyNetworkInterface.Packet> OnNetworkJumpResponse = (packet) => {
				if (packet == null || packet.PhaseFrame != 1) return;
				JumpGateInfo jump_gate_info = packet.Payload<JumpGateInfo>();

				if (MyNetworkInterface.IsStandaloneMultiplayerClient && jump_gate_info.Type != JumpGateInfo.TypeEnum.JUMP_START && jump_gate_info.Type != JumpGateInfo.TypeEnum.WORMHOLE_OPEN && jump_gate_info.JumpGateID == JumpGateUUID.FromJumpGate(this))
				{
					Logger.Debug($"[{this.JumpGateGrid.CubeGridID}-{this.JumpGateID}]: Got network server jump response");
					this.ServerJumpResponse = jump_gate_info;
					this.Phase = MyJumpGatePhase.JUMPING;

					if (jump_gate_info.EntityBatches != null)
					{
						foreach (string serialized_batch in jump_gate_info.EntityBatches)
						{
							EntityBatch batch = new EntityBatch(serialized_batch);
							if (batch.Batch.Count == 0) continue;
							this.EntityBatches[batch.Parent] = batch;
						}
					}

					if (jump_gate_info.EntityWarps != null)
					{
						foreach (string serialized_warp in jump_gate_info.EntityWarps)
						{
							EntityWarpInfo warp = EntityWarpInfo.FromSerialized(serialized_warp, null);
							MyJumpGateModSession.Instance.WarpEntityBatchOverTime(warp);
						}
					}

					switch (this.ServerJumpResponse.Type)
					{
						case JumpGateInfo.TypeEnum.JUMP_FAIL:
						case JumpGateInfo.TypeEnum.CLOSED:
							this.Status = (this.ServerJumpResponse.CancelOverride) ? MyJumpGateStatus.CANCELLED : this.Status;
							SendJumpResponse(this.ServerJumpResponse.ResultMessage, false);
							break;
						case JumpGateInfo.TypeEnum.JUMP_SUCCESS:
							SendJumpResponse(this.ServerJumpResponse.ResultMessage, true);
							//MyJumpGateModSession.Instance.RequestGridsDownload();
							break;
					}
				}
			};

			Action<MyNetworkInterface.Packet> OnNetworkUpdateJumpEndpoint = (packet) => {
				if (packet == null || packet.PhaseFrame != 1) return;
				KeyValuePair<JumpGateUUID, Vector3D> payload = packet.Payload<KeyValuePair<JumpGateUUID, Vector3D>>();
				this.TrueEndpoint = (payload.Key == this_id && this.JumpHandlerActive) ? payload.Value : this.TrueEndpoint;
			};

			SendJumpResponse = (message, result_status) => {
				this.Status = MyJumpGateStatus.SWITCHING;
				this.Phase = MyJumpGatePhase.RESETTING;

				if (target_gate != null)
				{
					target_gate.Status = MyJumpGateStatus.SWITCHING;
					target_gate.Phase = MyJumpGatePhase.RESETTING;
				}

				MyJumpGateModSession.Instance.Network.Off(MyPacketTypeEnum.JUMP_GATE_JUMP, this.OnNetworkJumpResponse);
				MyJumpGateModSession.Instance.Network.Off(MyPacketTypeEnum.UPDATE_JUMP_ENDPOINT, this.OnNetworkUpdateJumpEndpoint);
				this.JumpHandlerActive = false;

				Action<Exception> onend = (error) => {
					gate_animation?.Clean();
					this.TrueEndpoint = null;
					this.EntityBatches?.Clear();
					if (this.ServerJumpResponse != null && this.ServerJumpResponse.Type == JumpGateInfo.TypeEnum.CLOSED) this.Dispose();
					else if (!this.Closed) this.Reset();
					if (target_gate != null && !target_gate.Closed) target_gate.Reset();
					this.ServerJumpResponse = null;
					this.OnNetworkJumpResponse = null;
					this.OnNetworkUpdateJumpEndpoint = null;
				};

				if (gate_animation != null) MyJumpGateModSession.Instance.PlayAnimation(gate_animation, (result_status) ? MyJumpGateAnimation.AnimationTypeEnum.JUMPED : MyJumpGateAnimation.AnimationTypeEnum.FAILED, null, onend);
				else onend(null);
				hud_notification?.Hide();
				this.HasBlocksWithinShearZone = false;
				this.ShearBlocksWarning.Hide();
				this.SendHudMessage(message, 3000, (result_status) ? "White" : "Red");
				if (target_gate != null && !target_gate.Closed && !target_gate.MarkClosed) target_gate.Reset();
				Logger.Debug($"[{this.JumpGateGrid.CubeGridID}-{this.JumpGateID}] END_JUMP {result_status}/{message}; ANIMATION={gate_animation}", 3);
			};

			this.OnNetworkJumpResponse = this.OnNetworkJumpResponse ?? OnNetworkJumpResponse;
			this.OnNetworkUpdateJumpEndpoint = this.OnNetworkUpdateJumpEndpoint ?? OnNetworkUpdateJumpEndpoint;
			MyJumpGateModSession.Instance.Network.On(MyPacketTypeEnum.JUMP_GATE_JUMP, this.OnNetworkJumpResponse);
			MyJumpGateModSession.Instance.Network.On(MyPacketTypeEnum.UPDATE_JUMP_ENDPOINT, this.OnNetworkUpdateJumpEndpoint);
			this.JumpHandlerActive = true;

			try
			{
				// Get target gate if applicable
				if (dst.WaypointType == MyWaypointType.JUMP_GATE)
				{
					MyJumpGateConstruct grid = MyJumpGateModSession.Instance.GetJumpGateGrid(dst.JumpGate.GetJumpGateGrid());
					target_gate = grid?.GetJumpGate(dst.JumpGate.GetJumpGate());
				}

				// Setup endpoint
				Vector3D? _endpoint = dst.GetEndpoint();

				if (_endpoint == null)
				{
					SendJumpResponse(MyJumpGate.GetFailureDescription(MyJumpFailReason.DESTINATION_UNAVAILABLE), false);
					return;
				}

				// Setup gate animation
				string gate_animation_name = controller_settings.JumpEffectAnimationName();
				gate_animation = MyAnimationHandler.GetAnimation(gate_animation_name, MyAPIGateway.Session.Player, this, target_gate, controller_settings, target_controller_settings, jump_type);

				if (gate_animation == null)
				{
					SendJumpResponse(MyJumpGate.GetFailureDescription(MyJumpFailReason.NULL_ANIMATION), false);
					return;
				}

				// Final setup
				this.TrueEndpoint = true_endpoint;
				double tick_duration = 1000d / 60d;
				double time_to_jump_ms = (gate_animation == null) ? 0 : gate_animation.Durations()[0] * tick_duration;
				hud_notification = MyAPIGateway.Utilities.CreateNotification(MyTexts.GetString("Notification_JumpGate_GateActivationIn").Replace("{%0}", "--.-s"), (int) Math.Round(time_to_jump_ms), "White");

				if (!MyNetworkInterface.IsMultiplayerServer)
				{
					hud_notification.Show();
					this.ShearBlocksWarning.AliveTime = (int) Math.Round(time_to_jump_ms);
				}

				// Update gate status
				foreach (MyJumpGateDrive drive in this.GetJumpGateDrives()) drive.InvalidatePowerCheck();
				this.Status = MyJumpGateStatus.OUTBOUND;
				this.Phase = MyJumpGatePhase.CHARGING;
				MyJumpGateModSession.Instance.RedrawAllTerminalControls();

				// Play animation and check for invalidation of jump event
				MyJumpGateModSession.Instance.PlayAnimation(gate_animation, MyJumpGateAnimation.AnimationTypeEnum.JUMPING, () => {
					time_to_jump_ms -= tick_duration;
					hud_notification.Text = MyTexts.GetString("Notification_JumpGate_GateActivationIn").Replace("{%0}", MyJumpGateModSession.AutoconvertMetricUnits(Math.Max(0, time_to_jump_ms / 1000d), "s", 1));
					hud_notification.Hide();
					Vector3D character_pos = MyAPIGateway.Session.LocalHumanPlayer?.GetPosition() ?? Vector3D.PositiveInfinity;

					if (character_pos != null && this.IsPointInHudBroadcastRange(ref character_pos))
					{
						hud_notification.Show();
						if (this.ShearBlocks.Count == 0) this.ShearBlocksWarning.Hide();
						else this.ShearBlocksWarning.Show();
					}

					return this.ServerJumpResponse == null;
				}, (Exception err) => {
					hud_notification.Hide();
					if (err != null) SendJumpResponse(MyJumpGate.GetFailureDescription(MyJumpFailReason.UNKNOWN_ERROR), false);
				});
			}
			catch (Exception)
			{
				SendJumpResponse(MyJumpGate.GetFailureDescription(MyJumpFailReason.UNKNOWN_ERROR), false);
			}
		}

		/// <summary>
		/// Executes the gate wormhole jump for multiplayer server and singleplayer sessons
		/// </summary>
		/// <param name="controller_settings">The controller settings to use for the jump</param>
		/// <param name="target_controller_settings">The controller settings of the target gate's controller</param>
		private void ExecuteWormholeClient(MyJumpGateController.MyControllerBlockSettingsStruct controller_settings, MyJumpGateController.MyControllerBlockSettingsStruct target_controller_settings, Vector3D true_endpoint, MyJumpTypeEnum jump_type, bool bypass_activation = false)
		{
			if (this.Closed) return;
			Logger.Debug($"[{this.JumpGateGrid?.CubeGridID ?? -1}]-{this.JumpGateID} BEGIN_WORMHOLE_JUMP CLIENT PRE_CHECK", 3);
			MyJumpGate target_gate = null;
			MyJumpGateAnimation gate_animation = null;
			MyJumpGateWaypoint dst = controller_settings.SelectedWaypoint();
			JumpGateWormholeJumpInfo wormhole_jump_info = null;
			this.ServerJumpResponse = null;
			Action<string, bool, bool> SendJumpResponse = null;
			Vector3D jump_node = this.WorldJumpNode;
			IMyHudNotification hud_notification = null;
			JumpGateUUID this_id = JumpGateUUID.FromJumpGate(this);
			string gate_animation_name = controller_settings.JumpEffectAnimationName();
			this.AutoActivateStartTime = null;
			this.AutoActivationNotif.Hide();

			Action WormholeLoop = () => {
				Vector3D endpoint = this.TrueEndpoint.Value;
				MyParticleEffect energy_release_effect;
				MyEntity3DSoundEmitter energy_release_sound = new MyEntity3DSoundEmitter(null);
				MatrixD energy_release_matrix = ParticleOrientationDef.GetJumpGateMatrix(this, target_gate, true, ref endpoint, null);
				MyParticlesManager.TryCreateParticleEffect("IOTA.WormholeEnergyRelease", ref energy_release_matrix, ref jump_node, uint.MaxValue, out energy_release_effect);
				energy_release_effect?.StopEmitting();
				energy_release_effect?.StopLights();
				double energy_jump_cutoff_min = 1e4;
				double energy_jump_cutoff_max = 1e6;

				Action<float> UpdateEnergyReleaseEffects = (float alpha) => {
					alpha = MathHelper.Clamp(alpha, 0, 1);

					if (alpha > 0.1f)
					{
						if (energy_release_effect != null)
						{
							if (energy_release_effect.IsEmittingStopped) energy_release_effect.Play();
							energy_release_effect.UserScale = (float) this.JumpEllipse.Radii.Max();
							energy_release_effect.UserColorMultiplier = new Vector4(1f, 1f, 1f, alpha);
							energy_release_effect.WorldMatrix = ParticleOrientationDef.GetJumpGateMatrix(this, target_gate, true, ref endpoint, null);
						}

						if (!energy_release_sound.IsPlaying) energy_release_sound.PlaySound(new MySoundPair("IOTA.EnergyRelease"));
						energy_release_sound.VolumeMultiplier = alpha;
						energy_release_sound.SetPosition(target_gate.WorldJumpNode);
					}
					else
					{
						if (energy_release_effect != null && !energy_release_effect.IsEmittingStopped)
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
					if (wormhole_jump_info != null)
					{
						float alpha = MathHelper.Clamp((float) ((wormhole_jump_info.ExplosionBuffer - energy_jump_cutoff_min) / energy_jump_cutoff_max), 0, 1);
						UpdateEnergyReleaseEffects(alpha);

						if (wormhole_jump_info.TargetGate != JumpGateUUID.Empty)
						{
							MyJumpGate new_target_gate = MyJumpGateModSession.Instance.GetJumpGate(wormhole_jump_info.TargetGate);
							Logger.Debug($"CLIENT_WORMHOLE_JUMP: EXPLOSION_POWER={wormhole_jump_info.ExplosionBuffer}, TARGET={new_target_gate?.GetPrintableName()}", 2);
							new_target_gate.SenderGate = this;
							new_target_gate.Status = MyJumpGateStatus.INBOUND;
							new_target_gate.Phase = MyJumpGatePhase.JUMPING;
							gate_animation.Pause();
							new_target_gate.TrueEndpoint = new_target_gate.WorldJumpNode + (new_target_gate.WorldJumpNode - jump_node);
							target_gate.TrueEndpoint = endpoint + (endpoint - jump_node);
							float open_duration = gate_animation.Durations()[(byte) MyJumpGateAnimation.AnimationTypeEnum.WORMHOLE_OPEN];
							ushort transition_tick = 0;

							MyJumpGate close_gate = target_gate;
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
								UpdateEnergyReleaseEffects(0);
								new_target_gate.WormholeStartTime = this.WormholeStartTime;
								new_target_gate.TrueEndpoint = null;
								this.JumpSpaceExplosions?.Clear();
								this.TrueEndpoint = new_target_gate.WorldJumpNode;
								target_gate = new_target_gate;
								target_controller_settings = wormhole_jump_info.TargetControllerSettings;
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
								wormhole_jump_info = null;
							});

							return true;
						}
					}

					endpoint = this.TrueEndpoint.Value;
					return this.IsWormholeActive;
				}, (Exception e) => {
					this.WormholeStartTime = null;
					if (target_gate != null) target_gate.WormholeStartTime = null;
					if (e == null) return;
					int drive_count = this.GetJumpGateDrives().Count();
					int working_drive_count = this.GetWorkingJumpGateDrives().Count();
					Logger.Error($"Error during jump gate wormhole jump:\n\tPHASE=POSTANIM\n\tGATE_CUBE_GRID={this.JumpGateGrid?.CubeGrid?.EntityId.ToString() ?? "N/A"}\n\tGATE_ID={this.JumpGateID}\n\tGATE_SIZE={drive_count}\n\tGATE_SIZE_WORKING={working_drive_count}\nERROR:\n\t{e}\n\tINNER:\n{e.InnerException}");
				});
			};

			Action<MyNetworkInterface.Packet> OnNetworkJumpResponse = (packet) => {
				if (packet == null || packet.PhaseFrame != 1) return;
				JumpGateInfo jump_gate_info = packet.Payload<JumpGateInfo>();
				if (!MyNetworkInterface.IsStandaloneMultiplayerClient || jump_gate_info.JumpGateID != this_id) return;
				Logger.Debug($"[{this.JumpGateGrid?.CubeGridID}-{this.JumpGateID}]: Got network server wormhole jump response");
				this.ServerJumpResponse = jump_gate_info;
				MyJumpGateAnimation batch_animation;
				List<MyEntity> active_batch;

				switch (jump_gate_info.Type)
				{
					case JumpGateInfo.TypeEnum.WORMHOLE_OPEN:
						DateTime now = DateTime.UtcNow;
						this.Status = MyJumpGateStatus.OUTBOUND;
						this.Phase = MyJumpGatePhase.JUMPING;
						this.WormholeStartTime = now;
						//this.TrueEndpoint = jump_gate_info.TrueEndpoint;
						target_gate.Status = MyJumpGateStatus.INBOUND;
						target_gate.Phase = MyJumpGatePhase.JUMPING;
						target_gate.WormholeStartTime = now;
						WormholeLoop();
						Logger.Debug($"[{this.JumpGateGrid?.CubeGridID ?? -1}]-{this.JumpGateID} BEGIN_CLIENT_WORMHOLE_LOOP", 3);
						break;
					case JumpGateInfo.TypeEnum.WORMHOLE_FAIL:
						this.Status = MyJumpGateStatus.SWITCHING;
						this.Phase = MyJumpGatePhase.RESETTING;

						if (target_gate != null)
						{
							target_gate.Status = MyJumpGateStatus.SWITCHING;
							target_gate.Phase = MyJumpGatePhase.RESETTING;
						}

						MyJumpGateModSession.Instance.Network.Off(MyPacketTypeEnum.JUMP_GATE_JUMP, this.OnNetworkJumpResponse);
						MyJumpGateModSession.Instance.Network.Off(MyPacketTypeEnum.UPDATE_JUMP_ENDPOINT, this.OnNetworkUpdateJumpEndpoint);
						MyJumpGateModSession.Instance.Network.Off(MyPacketTypeEnum.JUMP_GATE_WORMHOLE_JUMP, this.OnNetworkWormholeJump);
						this.JumpHandlerActive = false;

						Action<Exception> onend = (error) => {
							gate_animation?.Clean();
							this.TrueEndpoint = null;
							this.EntityBatches?.Clear();
							if (this.ServerJumpResponse != null && this.ServerJumpResponse.Type == JumpGateInfo.TypeEnum.CLOSED) this.Dispose();
							else if (!this.Closed) this.Reset();
							if (target_gate != null && !target_gate.Closed) target_gate.Reset();
							this.ServerJumpResponse = null;
							this.OnNetworkJumpResponse = null;
							this.OnNetworkUpdateJumpEndpoint = null;
							this.OnNetworkWormholeJump = null;
						};

						if (gate_animation != null) MyJumpGateModSession.Instance.PlayAnimation(gate_animation, MyJumpGateAnimation.AnimationTypeEnum.WORMHOLE_CLOSE, null, onend);
						else onend(null);
						hud_notification?.Hide();
						this.HasBlocksWithinShearZone = false;
						this.ShearBlocksWarning.Hide();
						this.SendHudMessage(jump_gate_info.ResultMessage, 3000, "Red");
						Logger.Debug($"[{this.JumpGateGrid?.CubeGridID ?? -1}]-{this.JumpGateID} END_WORMHOLE_JUMP {jump_gate_info.ResultMessage}", 3);
						break;
					case JumpGateInfo.TypeEnum.WORMHOLE_ENTITY_PREJUMP:
						active_batch = jump_gate_info.EntityIds.Select((id) => (MyEntity) MyAPIGateway.Entities.GetEntityById(id)).Where((entity) => entity != null).ToList();
						batch_animation = MyAnimationHandler.GetAnimation(gate_animation_name, MyAPIGateway.Session.Player, this, target_gate, controller_settings, target_controller_settings, MyJumpTypeEnum.STANDARD, active_batch);
						MyJumpGateModSession.Instance.PlayAnimation(batch_animation, MyJumpGateAnimation.AnimationTypeEnum.JUMPING, () => this.IsWormholeActive);
						break;
					case JumpGateInfo.TypeEnum.WORMHOLE_ANTI_ENTITY_PREJUMP:
						active_batch = jump_gate_info.EntityIds.Select((id) => (MyEntity) MyAPIGateway.Entities.GetEntityById(id)).Where((entity) => entity != null).ToList();
						batch_animation = MyAnimationHandler.GetAnimation(gate_animation_name, MyAPIGateway.Session.Player, target_gate, this, controller_settings, target_controller_settings, MyJumpTypeEnum.STANDARD, active_batch);
						MyJumpGateModSession.Instance.PlayAnimation(batch_animation, MyJumpGateAnimation.AnimationTypeEnum.JUMPING, () => this.IsWormholeActive);
						break;
					case JumpGateInfo.TypeEnum.WORMHOLE_ENTITY_JUMP_SUCCESS:
						if (jump_gate_info.EntityBatches != null)
						{
							foreach (string serialized_batch in jump_gate_info.EntityBatches)
							{
								EntityBatch batch = new EntityBatch(serialized_batch);
								if (batch.Batch.Count == 0) continue;
								this.EntityBatches[batch.Parent] = batch;
							}
						}

						if (jump_gate_info.EntityWarps != null)
						{
							foreach (string serialized_warp in jump_gate_info.EntityWarps)
							{
								EntityWarpInfo warp = EntityWarpInfo.FromSerialized(serialized_warp, null);
								MyJumpGateModSession.Instance.WarpEntityBatchOverTime(warp);
							}
						}

						SendJumpResponse(this.ServerJumpResponse.ResultMessage, true, false);
						active_batch = jump_gate_info.EntityIds.Select((id) => (MyEntity) MyAPIGateway.Entities.GetEntityById(id)).Where((entity) => entity != null).ToList();
						batch_animation = MyAnimationHandler.GetAnimation(gate_animation_name, MyAPIGateway.Session.Player, this, target_gate, controller_settings, target_controller_settings, MyJumpTypeEnum.STANDARD, active_batch);
						MyJumpGateModSession.Instance.PlayAnimation(batch_animation, MyJumpGateAnimation.AnimationTypeEnum.JUMPED, () => this.IsWormholeActive);
						//MyJumpGateModSession.Instance.RequestGridsDownload();
						break;
					case JumpGateInfo.TypeEnum.WORMHOLE_ANTI_ENTITY_JUMP_SUCCESS:
						if (jump_gate_info.EntityBatches != null)
						{
							foreach (string serialized_batch in jump_gate_info.EntityBatches)
							{
								EntityBatch batch = new EntityBatch(serialized_batch);
								if (batch.Batch.Count == 0) continue;
								target_gate.EntityBatches[batch.Parent] = batch;
							}
						}

						if (jump_gate_info.EntityWarps != null)
						{
							foreach (string serialized_warp in jump_gate_info.EntityWarps)
							{
								EntityWarpInfo warp = EntityWarpInfo.FromSerialized(serialized_warp, null);
								MyJumpGateModSession.Instance.WarpEntityBatchOverTime(warp);
							}
						}

						SendJumpResponse(this.ServerJumpResponse.ResultMessage, true, true);
						active_batch = jump_gate_info.EntityIds.Select((id) => (MyEntity) MyAPIGateway.Entities.GetEntityById(id)).Where((entity) => entity != null).ToList();
						batch_animation = MyAnimationHandler.GetAnimation(gate_animation_name, MyAPIGateway.Session.Player, target_gate, this, controller_settings, target_controller_settings, MyJumpTypeEnum.STANDARD, active_batch);
						MyJumpGateModSession.Instance.PlayAnimation(batch_animation, MyJumpGateAnimation.AnimationTypeEnum.JUMPED, () => this.IsWormholeActive);
						//MyJumpGateModSession.Instance.RequestGridsDownload();
						break;
					case JumpGateInfo.TypeEnum.WORMHOLE_ENTITY_JUMP_FAIL:
						SendJumpResponse(this.ServerJumpResponse.ResultMessage, false, false);
						active_batch = jump_gate_info.EntityIds.Select((id) => (MyEntity) MyAPIGateway.Entities.GetEntityById(id)).Where((entity) => entity != null).ToList();
						batch_animation = MyAnimationHandler.GetAnimation(gate_animation_name, MyAPIGateway.Session.Player, this, target_gate, controller_settings, target_controller_settings, MyJumpTypeEnum.STANDARD, active_batch);
						MyJumpGateModSession.Instance.PlayAnimation(batch_animation, MyJumpGateAnimation.AnimationTypeEnum.FAILED, () => this.IsWormholeActive);
						break;
					case JumpGateInfo.TypeEnum.WORMHOLE_ANTI_ENTITY_JUMP_FAIL:
						SendJumpResponse(this.ServerJumpResponse.ResultMessage, false, true);
						active_batch = jump_gate_info.EntityIds.Select((id) => (MyEntity) MyAPIGateway.Entities.GetEntityById(id)).Where((entity) => entity != null).ToList();
						batch_animation = MyAnimationHandler.GetAnimation(gate_animation_name, MyAPIGateway.Session.Player, target_gate, this, controller_settings, target_controller_settings, MyJumpTypeEnum.STANDARD, active_batch);
						MyJumpGateModSession.Instance.PlayAnimation(batch_animation, MyJumpGateAnimation.AnimationTypeEnum.FAILED, () => this.IsWormholeActive);
						break;
				}
			};

			Action<MyNetworkInterface.Packet> OnNetworkUpdateJumpEndpoint = (packet) => {
				if (packet == null || packet.PhaseFrame != 1) return;
				KeyValuePair<JumpGateUUID, Vector3D> payload = packet.Payload<KeyValuePair<JumpGateUUID, Vector3D>>();
				this.TrueEndpoint = (payload.Key == this_id && this.JumpHandlerActive) ? payload.Value : this.TrueEndpoint;
			};

			Action<MyNetworkInterface.Packet> OnNetworkWormholeJump = (packet) => {
				if (packet == null || packet.PhaseFrame != 1) return;
				wormhole_jump_info = packet.Payload<JumpGateWormholeJumpInfo>();
				if (JumpGateUUID.FromJumpGate(this) == wormhole_jump_info.ThisGate) return;
				wormhole_jump_info = null;
			};

			SendJumpResponse = (message, result_status, is_anti_gate) => {
				MyJumpGate gate = (is_anti_gate) ? target_gate : this;
				gate.SendHudMessage(message, 3000, (result_status) ? "White" : "Red");
			};

			this.OnNetworkJumpResponse = this.OnNetworkJumpResponse ?? OnNetworkJumpResponse;
			this.OnNetworkUpdateJumpEndpoint = this.OnNetworkUpdateJumpEndpoint ?? OnNetworkUpdateJumpEndpoint;
			this.OnNetworkWormholeJump = this.OnNetworkWormholeJump ?? OnNetworkWormholeJump;
			MyJumpGateModSession.Instance.Network.On(MyPacketTypeEnum.JUMP_GATE_JUMP, this.OnNetworkJumpResponse);
			MyJumpGateModSession.Instance.Network.On(MyPacketTypeEnum.UPDATE_JUMP_ENDPOINT, this.OnNetworkUpdateJumpEndpoint);
			MyJumpGateModSession.Instance.Network.On(MyPacketTypeEnum.JUMP_GATE_WORMHOLE_JUMP, this.OnNetworkWormholeJump);
			this.JumpHandlerActive = true;

			try
			{
				// Get target gate
				MyJumpGateConstruct grid = MyJumpGateModSession.Instance.GetJumpGateGrid(dst.JumpGate.GetJumpGateGrid());
				target_gate = grid?.GetJumpGate(dst.JumpGate.GetJumpGate());

				// Setup endpoint
				Vector3D? _endpoint = dst.GetEndpoint();

				if (_endpoint == null)
				{
					SendJumpResponse(MyJumpGate.GetFailureDescription(MyJumpFailReason.DESTINATION_UNAVAILABLE), false, false);
					return;
				}

				// Setup gate animation
				gate_animation = MyAnimationHandler.GetAnimation(gate_animation_name, MyAPIGateway.Session.Player, this, target_gate, controller_settings, target_controller_settings, jump_type);

				if (gate_animation == null)
				{
					SendJumpResponse(MyJumpGate.GetFailureDescription(MyJumpFailReason.NULL_ANIMATION), false, false);
					return;
				}

				// Final setup
				this.TrueEndpoint = true_endpoint;
				double tick_duration = 1000d / 60d;
				double time_to_jump_ms = (gate_animation == null) ? 0 : gate_animation.Durations()[3] * tick_duration;
				hud_notification = MyAPIGateway.Utilities.CreateNotification(MyTexts.GetString("Notification_JumpGate_GateActivationIn").Replace("{%0}", "--.-s"), (int) Math.Round(time_to_jump_ms), "White");

				if (!MyNetworkInterface.IsMultiplayerServer)
				{
					hud_notification.Show();
					this.ShearBlocksWarning.AliveTime = (int) Math.Round(time_to_jump_ms);
				}

				// Update gate status
				foreach (MyJumpGateDrive drive in this.GetJumpGateDrives()) drive.InvalidatePowerCheck();
				this.Status = MyJumpGateStatus.OUTBOUND;
				this.Phase = MyJumpGatePhase.CHARGING;
				MyJumpGateModSession.Instance.RedrawAllTerminalControls();

				if (bypass_activation)
				{
					DateTime now = DateTime.UtcNow;
					this.Status = MyJumpGateStatus.OUTBOUND;
					this.Phase = MyJumpGatePhase.JUMPING;
					this.WormholeStartTime = now;
					target_gate.Status = MyJumpGateStatus.INBOUND;
					target_gate.Phase = MyJumpGatePhase.JUMPING;
					target_gate.WormholeStartTime = now;
					WormholeLoop();
					Logger.Debug($"[{this.JumpGateGrid?.CubeGridID ?? -1}]-{this.JumpGateID} BEGIN_CLIENT_WORMHOLE_LOOP", 3);
				}
				else
				{
					// Play animation and check for invalidation of jump event
					MyJumpGateModSession.Instance.PlayAnimation(gate_animation, MyJumpGateAnimation.AnimationTypeEnum.WORMHOLE_OPEN, () => {
						time_to_jump_ms -= tick_duration;
						hud_notification.Text = MyTexts.GetString("Notification_JumpGate_GateActivationIn").Replace("{%0}", MyJumpGateModSession.AutoconvertTimeHHMMSS(time_to_jump_ms / 1000d));
						hud_notification.Hide();
						Vector3D character_pos = MyAPIGateway.Session.LocalHumanPlayer?.GetPosition() ?? Vector3D.PositiveInfinity;

						if (character_pos != null && this.IsPointInHudBroadcastRange(ref character_pos))
						{
							hud_notification.Show();
							if (this.ShearBlocks.Count == 0) this.ShearBlocksWarning.Hide();
							else this.ShearBlocksWarning.Show();
						}

						return this.ServerJumpResponse == null;
					}, (Exception err) => {
						hud_notification.Hide();
						if (err != null) SendJumpResponse(MyJumpGate.GetFailureDescription(MyJumpFailReason.UNKNOWN_ERROR), false, false);
					});
				}
			}
			catch (Exception)
			{
				SendJumpResponse(MyJumpGate.GetFailureDescription(MyJumpFailReason.UNKNOWN_ERROR), false, false);
			}
		}

		/// <summary>
		/// Attempts to execute an auto-activated jump<br />
		/// Will not execute if time since successfull jump was less than 30 seconds
		/// </summary>
		private void CheckExecuteAutoJump()
		{
			DateTime now = DateTime.UtcNow;
			if ((now - this.LastAutojumpAttemptTime).TotalSeconds <= 30 || this.Controller == null) return;
			this.LastAutojumpAttemptTime = now;
			this.JumpWrapper(this.Controller.BlockSettings, null, null, MyJumpTypeEnum.STANDARD);
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Begins the activation sequence for this gate<br />
		/// For singleplayer: Begins the jump<br />
		/// For servers: Begins the jump, broadcasts jump request to all clients<br />
		/// For clients: Sends a jump request to server
		/// </summary>
		/// <param name="controller_settings">The controller settings to use for the jump</param>
		public void Jump(MyJumpGateController.MyControllerBlockSettingsStruct controller_settings)
		{
			if (this.Closed || this.MarkClosed || controller_settings == null) return;
			else if (MyNetworkInterface.IsStandaloneMultiplayerClient)
			{
				MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet
				{
					PacketType = MyPacketTypeEnum.JUMP_GATE_JUMP,
					TargetID = 0,
					Broadcast = false,
				};
				packet.Payload(new JumpGateInfo
				{
					CancelOverride = false,
					Type = JumpGateInfo.TypeEnum.JUMP_START,
					JumpGateID = JumpGateUUID.FromJumpGate(this),
				});
				packet.Send();
			}
			else if (MyNetworkInterface.IsServerLike) this.JumpWrapper(controller_settings, null, null, MyJumpTypeEnum.STANDARD);
		}

		/// <summary>
		/// Begins the activation sequence for this gate<br />
		/// Destination is the void; <b>All jump entities will be destroyed</b><br />
		/// This method must be called on singleplayer or multiplayer server
		/// </summary>
		/// <param name="controller_settings">The controller settings to use for the jump</param>
		/// <param name="distance">The distance to use for power calculations</param>
		/// <param name="gravity_strength">The gravity to apply to this gate's jump node for wormhole jumps</param>
		public void JumpToVoid(MyJumpGateController.MyControllerBlockSettingsStruct controller_settings, double distance, float gravity_strength)
		{
			if (controller_settings.DoSustainedWormhole()) this.ExecuteWormholeJumpToVoid(controller_settings, distance, gravity_strength);
			else this.ExecuteJumpToVoid(controller_settings, distance);
		}

		/// <summary>
		/// Begins the activation sequence for this gate<br />
		/// Source is the void; Allows spawning of prefab entities<br />
		/// This method must be called on singleplayer or multiplayer server
		/// </summary>
		/// <param name="controller_settings">The controller settings to use for the jump</param>
		/// <param name="spawn_prefabs">A list of prefabs to spawn<br />Jump will fail if empty</param>
		/// <param name="spawned_grids">A list containing the list of spawned grids per prefab</param>
		public void JumpFromVoid(MyJumpGateController.MyControllerBlockSettingsStruct controller_settings, List<MyPrefabInfo> spawn_prefabs, List<List<IMyCubeGrid>> spawned_grids)
		{
			if (this.Closed || this.MarkClosed || !MyNetworkInterface.IsServerLike) return;
			Vector3D endpoint = this.WorldJumpNode + this.GetWorldMatrix(false, true).Forward * 1e6;
			this.TrueEndpoint = endpoint;
			MyJumpGateAnimation gate_animation = null;
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

				if (!this.IsValid() || !this.IsControlled() || !this.Controller.IsWorking) result = MyJumpFailReason.DST_UNAVAILABLE;
				else if (this.JumpGateGrid.GetAttachedJumpGateDrives().Count(working_src_drive_filter) < 2) result = MyJumpFailReason.DST_DISABLED;
				else if (this.Status == MyJumpGateStatus.NONE) result = MyJumpFailReason.DST_NOT_CONFIGURED;
				else if (!this.IsIdle()) result = MyJumpFailReason.DST_BUSY;
				else if (!controller_settings.CanBeInbound() && !controller_settings.CanBeOutbound()) result = MyJumpFailReason.DST_ROUTING_DISABLED;
				else if (!controller_settings.CanBeInbound()) result = MyJumpFailReason.DST_OUTBOUND_ONLY;
				else if (MyJumpGateModSession.Instance.Configuration.GeneralConfiguration.MaxConcurrentJumps.Value > 0 && MyJumpGateModSession.Instance.ConcurrentJumpsCounter > MyJumpGateModSession.Instance.Configuration.GeneralConfiguration.MaxConcurrentJumps.Value) result = MyJumpFailReason.SUBSPACE_BUSY;
				else if (controller_settings.DoSustainedWormhole()) result = MyJumpFailReason.DESTINATION_UNAVAILABLE;

				// Send jump-failure if failed
				if (result != MyJumpFailReason.NONE)
				{
					SendJumpResponse(result, false, null);
					return;
				}

				// Setup gate animation
				string gate_animation_name = controller_settings.JumpEffectAnimationName();
				gate_animation = MyAnimationHandler.GetAnimation(gate_animation_name, MyAPIGateway.Session.Player, this, this, controller_settings, controller_settings, MyJumpTypeEnum.INBOUND_VOID);

				if (gate_animation == null)
				{
					SendJumpResponse(MyJumpFailReason.NULL_ANIMATION, false, null);
					return;
				}

				// Final setup
				double tick_duration = 1000d / 60d;
				double time_to_jump_ms = gate_animation.Durations()[0] * tick_duration;
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
						JumpType = MyJumpTypeEnum.INBOUND_VOID,
						JumpGateID = JumpGateUUID.FromJumpGate(this),
						ControllerSettings = controller_settings,
						TargetControllerSettings = null,
						TrueEndpoint = endpoint,
					});

					packet.Send();
				}

				// Update gate status
				foreach (MyJumpGateDrive drive in this.GetJumpGateDrives()) drive.InvalidatePowerCheck();
				this.Status = MyJumpGateStatus.INBOUND;
				this.Phase = MyJumpGatePhase.CHARGING;
				is_init = false;
				MyJumpGateModSession.Instance.RedrawAllTerminalControls();
				Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} VOIDJUMP_CHARGE", 3);

				// Play animation and check for invalidation of jump event
				MyJumpGateModSession.Instance.PlayAnimation(gate_animation, MyJumpGateAnimation.AnimationTypeEnum.JUMPING, () => {
					if (!this.IsValid() || !this.IsControlled() || !this.Controller.IsWorking) result = MyJumpFailReason.DST_UNAVAILABLE;
					else if (this.JumpGateGrid.GetAttachedJumpGateDrives().Count(working_src_drive_filter) < 2) result = MyJumpFailReason.DST_DISABLED;
					else if (this.Status == MyJumpGateStatus.NONE) result = MyJumpFailReason.DST_NOT_CONFIGURED;
					else if (!controller_settings.CanBeInbound() && !controller_settings.CanBeOutbound()) result = MyJumpFailReason.DST_ROUTING_CHANGED;
					else if (!controller_settings.CanBeInbound()) result = MyJumpFailReason.DST_ROUTING_CHANGED;

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

					if (this.Closed || this.JumpGateGrid == null || this.JumpGateGrid.Closed) result = MyJumpFailReason.DST_VOIDED;

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
						hud_notification.Hide();
						cancel_request_notificiation?.Hide();
						this.ShearBlocksWarning?.Hide();

						if (result != MyJumpFailReason.NONE)
						{
							SendJumpResponse(result, false, null);
							return;
						}
						else if (spawn_prefabs == null || spawn_prefabs.Count == 0)
						{
							SendJumpResponse(MyJumpFailReason.NO_ENTITIES, false, null);
							return;
						}
						else if (this.JumpSpaceEntities.Count > 0)
						{
							SendJumpResponse(MyJumpFailReason.NO_ENTITIES_JUMPED, false, MyTexts.GetString("Notification_JumpGate_JumpSpaceObstructed"));
							return;
						}

						// Spawn prefabs
						this.Phase = MyJumpGatePhase.JUMPING;
						int skipped_entities_count = 0;
						MatrixD gate_matrix = jump_ellipse.WorldMatrix;

						for (int i = 0; i < spawn_prefabs.Count; ++i)
						{
							MyPrefabInfo prefab = spawn_prefabs[i];
							List<IMyCubeGrid> grids = (i >= spawned_grids.Count) ? new List<IMyCubeGrid>() : spawned_grids[i];
							Vector3D world_pos = MyJumpGateModSession.LocalVectorToWorldVectorP(ref gate_matrix, prefab.Position);

							if (!jump_ellipse.IsPointInEllipse(world_pos))
							{
								++skipped_entities_count;
								continue;
							}

							Vector3D forward = MyJumpGateModSession.LocalVectorToWorldVectorD(ref gate_matrix, prefab.Forward);
							Vector3D up = MyJumpGateModSession.LocalVectorToWorldVectorD(ref gate_matrix, prefab.Up);
							MyAPIGateway.PrefabManager.SpawnPrefab(grids, prefab.PrefabName, world_pos, forward, up, prefab.InitialLinearVelocity, prefab.InitialAngularVelocity, prefab.BeaconName, prefab.SpawningOptions, prefab.UpdateSync, prefab.Callback);
						}

						int final_count = spawn_prefabs.Count - skipped_entities_count;
						string message = MyTexts.GetString("Notification_JumpGate_Jumped").Replace("{%0}", final_count.ToString()).Replace("{%1}", spawn_prefabs.Count.ToString());
						SendJumpResponse((final_count > 0) ? MyJumpFailReason.SUCCESS : MyJumpFailReason.NO_ENTITIES_JUMPED, final_count > 0, message);
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
		/// Cancels the gate's jump<br />
		/// For servers and singleplayer: Cancels the jump<br />
		/// For clients: Sends a cancel jump request to server
		/// </summary>
		public void CancelJump()
		{
			if (this.Closed) return;
			if (MyNetworkInterface.IsServerLike && this.Status != MyJumpGateStatus.OUTBOUND) this.SendHudMessage(MyTexts.GetString("Notification_JumpGate_JumpCancelFailed"), 3000, "Red");
			else if (MyNetworkInterface.IsServerLike) this.Status = MyJumpGateStatus.CANCELLED;
			else if (MyNetworkInterface.IsStandaloneMultiplayerClient)
			{
				MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet
				{
					PacketType = MyPacketTypeEnum.JUMP_GATE_JUMP,
					TargetID = 0,
					Broadcast = false,
				};

				packet.Payload(new JumpGateInfo
				{
					CancelOverride = true,
					Type = JumpGateInfo.TypeEnum.JUMP_FAIL,
					JumpGateID = JumpGateUUID.FromJumpGate(this),
					ControllerSettings = this.Controller?.BlockSettings,
				});
				packet.Send();
			}
		}

		/// <summary>
		/// </summary>
		/// <returns>True if this gate is inbound or outbound</returns>
		public bool IsJumping()
		{
			return !this.Closed && (this.Phase == MyJumpGatePhase.CHARGING || this.Phase == MyJumpGatePhase.JUMPING) && (this.Status == MyJumpGateStatus.OUTBOUND || this.Status == MyJumpGateStatus.INBOUND);
		}

		/// <summary>
		/// Checks if the specified gate is a valid wormhole jump target for this gate based on distance and faction settings
		/// </summary>
		/// <param name="other">The other gate</param>
		/// <param name="max_distance">The maximum jump distance</param>
		/// <returns>Validity</returns>
		public bool IsGateValidForWormholeJump(MyJumpGate other, double max_distance)
		{
			if (other == null || other.MarkClosed) return false;
			double max_distance_squared = max_distance * max_distance;
			IMyFaction this_faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(this.GetPrimaryOwnerID());
			IMyFaction other_faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(other.GetPrimaryOwnerID());
			MyJumpGateController.MyControllerBlockSettingsStruct target_settings = other.ControlObject?.BlockSettings;

			return target_settings != null && other.IsIdle()
				&& max_distance >= this.JumpGateConfiguration.MinimumJumpDistance && Vector3D.DistanceSquared(this.WorldJumpNode, other.WorldJumpNode) <= max_distance_squared
				&& (this.JumpGateConfiguration.AllowWormholeJumpsOutsideFaction || this_faction == other_faction || (this_faction != null && other_faction != null && MyAPIGateway.Session.Factions.GetRelationBetweenFactions(this_faction.FactionId, other_faction.FactionId) == MyRelationsBetweenFactions.Friends))
				&& (this.JumpGateConfiguration.AllowWormholeJumpsToStandardGate || target_settings.DoSustainedWormhole())
				;
		}

		/// <summary>
		/// Checks if the specified entity is within this gate's arrival queue
		/// </summary>
		/// <param name="entity">The entity to check</param>
		/// <returns>Whether entity is arriving and has not exited jump space</returns>
		public bool IsEntityInArrivalQueue(MyEntity entity)
		{
			MyJumpGateConstruct construct;
			if (entity is MyCubeGrid && (construct = MyJumpGateModSession.Instance.GetUnclosedJumpGateGrid((MyCubeGrid) entity)) != null) entity = (MyCubeGrid) construct.GetMainCubeGrid();
			return this.ArrivingWormholeEntities.Contains(entity);
		}
		#endregion
	}
}
