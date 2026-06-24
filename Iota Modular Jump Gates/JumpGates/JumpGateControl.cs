using IOTA.ModularJumpGates.Animation;
using IOTA.ModularJumpGates.CubeBlock;
using IOTA.ModularJumpGates.Extensions;
using IOTA.ModularJumpGates.ModConfiguration;
using IOTA.ModularJumpGates.Util;
using Sandbox.Game;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace IOTA.ModularJumpGates.JumpGates
{
	internal partial class MyJumpGate
	{
		#region Private Variables
		/// <summary>
		/// This gate's collider status synced from server or NONE if server
		/// </summary>
		private MyGateColliderStatus MPGateColliderStatus;

		/// <summary>
		/// The last time this grid was updated (in timer ticks ~100ns)
		/// </summary>
		private ulong LastUpdateTime = 0;

		/// <summary>
		/// The last name assigned to this jump gate
		/// </summary>
		private string LastStoredName = null;

		/// <summary>
		/// The mutex lock for exclusive jump gate updates
		/// </summary>
		private readonly object UpdateLock = new object();

		/// <summary>
		/// The grid size of this gate
		/// </summary>
		private MyCubeSize? DriveGridSize = null;

		/// <summary>
		/// The syphon for syphoning grid power over some length of time
		/// </summary>
		private GridPowerSyphon PowerSyphon;
		#endregion

		#region Public Variables
		/// <summary>
		/// Whether this gate is marked for close
		/// </summary>
		public bool MarkClosed { get; private set; } = false;

		/// <summary>
		/// Whether this gate is closed
		/// </summary>
		public bool Closed { get; private set; } = false;

		/// <summary>
		/// Whether this gate is closing
		/// </summary>
		public bool IsClosing = false;

		/// <summary>
		/// Whether this jump gate should be synced<br/>
		/// If true, will be synced on next component tick
		/// </summary>
		public bool IsDirty = false;

		/// <summary>
		/// The channel this jump gate's remote antenna is listening to or 255
		/// </summary>
		public byte RemoteAntennaChannel = 0xFF;

		/// <summary>
		/// The status of this gate
		/// </summary>
		public MyJumpGateStatus Status { get; private set; } = MyJumpGateStatus.NONE;

		/// <summary>
		/// The jump phase of this gate
		/// </summary>
		public MyJumpGatePhase Phase { get; private set; } = MyJumpGatePhase.NONE;

		/// <summary>
		/// This gate's ID
		/// </summary>
		public long JumpGateID = -1;

		/// <summary>
		/// The world matrix of this gate's jump gate grid's main cube grid
		/// </summary>
		public MatrixD ConstructMatrix;

		/// <summary>
		/// The inverted world matrix of this gate's jump gate grid's main cube grid
		/// </summary>
		public MatrixD ConstructMatrixInv;

		/// <summary>
		/// The UTC date time representation of MyJumpGate.LastUpdateTime
		/// </summary>
		public DateTime LastUpdateDateTimeUTC
		{
			get
			{
				return (DateTime.MinValue.ToUniversalTime() + new TimeSpan((long) this.LastUpdateTime));
			}

			set
			{
				if (this.Closed) return;
				this.LastUpdateTime = (ulong) (value.ToUniversalTime() - DateTime.MinValue).Ticks;
			}
		}

		/// <summary>
		/// Gets the attached control object (either controller or remote antenna) for this gate
		/// </summary>
		public MyJumpGateControlObject ControlObject => (this.Controller == null && this.RemoteAntenna == null) ? null : ((this.Controller == null) ? new MyJumpGateControlObject(this.RemoteAntenna, this.RemoteAntennaChannel) : new MyJumpGateControlObject(this.Controller));

		/// <summary>
		/// The gate configuration variables for this gate
		/// </summary>
		public MyModConfigurationV1.MyLocalJumpGateConfiguration JumpGateConfiguration => MyJumpGateModSession.Instance.Configuration?.GetJumpGateConfigurationForGate(this);

		/// <summary>
		/// This gate's attached controller or null
		/// </summary>
		public MyJumpGateController Controller = null;

		/// <summary>
		/// This gate's attached remote anntena or null
		/// </summary>
		public MyJumpGateRemoteAntenna RemoteAntenna = null;

		/// <summary>
		/// This gate's attached server antenna or null
		/// </summary>
		public MyJumpGateServerAntenna ServerAntenna = null;

		/// <summary>
		/// The gate jumping to this one or null
		/// </summary>
		public MyJumpGate SenderGate = null;

		/// <summary>
		/// This gate's MyJumpGateConstruct construct
		/// </summary>
		public MyJumpGateConstruct JumpGateGrid { get; private set; } = null;
		#endregion

		#region Private Methods
		/// <summary>
		/// Callback triggered when this object recevies a jump gate update request from server
		/// </summary>
		/// <param name="packet">The update request packet</param>
		private void OnNetworkJumpGateUpdate(MyNetworkInterface.Packet packet)
		{
			if (packet == null) return;
			MySerializedJumpGate serialized = packet.Payload<MySerializedJumpGate>();
			if (serialized == null || serialized.UUID != JumpGateUUID.FromJumpGate(this)) return;

			if (MyNetworkInterface.IsMultiplayerServer && packet.PhaseFrame == 1)
			{
				if (serialized.IsClientRequest)
				{
					packet = packet.Forward(packet.SenderID, false);
					packet.Payload(this.ToSerialized(false));
					packet.Send();
					Logger.Debug($"CLIENT_GATE_REQUEST -> {this.JumpGateGrid.CubeGridID}-{this.JumpGateID}", 3);
				}
				else if (packet.EpochTime > this.LastUpdateTime && this.FromSerialized(serialized))
				{
					packet = packet.Forward(0, true);
					packet.Payload(this.ToSerialized(false));
					packet.Send();
					this.LastUpdateTime = packet.EpochTime;
					Logger.Debug($"CLIENT_GATE_UPDATE -> {this.JumpGateGrid.CubeGridID}-{this.JumpGateID}", 3);
				}
			}
			else if (MyNetworkInterface.IsMultiplayerClient && (packet.PhaseFrame == 1 || packet.PhaseFrame == 2) && this.FromSerialized(serialized))
			{
				this.LastUpdateTime = packet.EpochTime;
				this.IsDirty = false;
				Logger.Debug($"SERVER_GATE_UPDATE -> {this.JumpGateGrid.CubeGridID}-{this.JumpGateID}", 3);
			}
		}

		/// <summary>
		/// Callback triggered when this object recevies a gate jump poll event
		/// </summary>
		/// <param name="packet">The jump poll request packet</param>
		private void OnNetworkJumpGateJumpPoll(MyNetworkInterface.Packet packet)
		{
			if (packet == null) return;
			JumpGatePollingInfo polling_info = packet.Payload<JumpGatePollingInfo>();
			JumpGateUUID this_id = JumpGateUUID.FromJumpGate(this);

			if (polling_info == null || polling_info.ThisGate != this_id) return;
			else if (MyNetworkInterface.IsMultiplayerServer && packet.PhaseFrame == 1 && !this.MarkClosed)
			{
				JumpGateUUID? target_id = this.ControlObject?.BlockSettings.SelectedWaypoint()?.JumpGate;
				MyJumpGate target = (target_id == null) ? null : MyJumpGateModSession.Instance.GetJumpGate(target_id.Value);
				packet = packet.Forward(0, true);
				packet.Payload(new JumpGatePollingInfo
				{
					ControllerSettings = this.ControlObject?.BlockSettings,
					TargetControllerSettings = target?.ControlObject?.BlockSettings,
					WormholeStartTime = this.WormholeStartTime,
					ThisGate = this_id,
					TargetGate = target_id ?? JumpGateUUID.Empty,
					TrueEndpoint = this.TrueEndpoint ?? Vector3D.Zero,
				});
				packet.Send();
			}
			else if (MyNetworkInterface.IsStandaloneMultiplayerClient && polling_info.WormholeStartTime != null && packet.PhaseFrame == 2)
			{
				this.ExecuteWormholeClient(polling_info.ControllerSettings, polling_info.TargetControllerSettings, polling_info.TrueEndpoint, MyJumpTypeEnum.STANDARD, true);
				this.WormholeStartTime = polling_info.WormholeStartTime;
			}
		}

		/// <summary>
		/// Callback triggered when server sends auto activation alert
		/// </summary>
		/// <param name="packet">The alert packet</param>
		private void OnNetworkAutoActivationAlert(MyNetworkInterface.Packet packet)
		{
			if (packet == null || MyNetworkInterface.IsServerLike || packet.PhaseFrame != 1) return;
			KeyValuePair<JumpGateUUID, DateTime?> payload = packet.Payload<KeyValuePair<JumpGateUUID, DateTime?>>();
			if (payload.Key != JumpGateUUID.FromJumpGate(this)) return;
			this.AutoActivateStartTime = payload.Value;
		}

		/// <summary>
		/// Callback triggered when server sends block damage alert
		/// </summary>
		/// <param name="packet">The alert packet</param>
		private void OnNetworkShearBlockAlert(MyNetworkInterface.Packet packet)
		{
			if (packet == null || MyNetworkInterface.IsServerLike || packet.PhaseFrame != 1) return;
			KeyValuePair<JumpGateUUID, int?> payload = packet.Payload<KeyValuePair<JumpGateUUID, int?>>();
			if (payload.Key != JumpGateUUID.FromJumpGate(this)) return;
			if (this.HasBlocksWithinShearZone = payload.Value.HasValue) this.ShearBlocksWarning.AliveTime = payload.Value.Value;
		}

		/// <summary>
		/// Callback triggered when this object receives a gate debug event from clients
		/// </summary>
		/// <param name="packet">The debug packet</param>
		private void OnGateDebug(MyNetworkInterface.Packet packet)
		{
			if (packet == null || packet.PhaseFrame != 1 || !MyNetworkInterface.IsMultiplayerServer) return;
			JumpGateDebugPayload payload = packet.Payload<JumpGateDebugPayload>();
			if (payload == null || (payload.JumpGateUUID != JumpGateUUID.Full && payload.JumpGateUUID != JumpGateUUID.FromJumpGate(this))) return;

			switch (payload.DebugType)
			{
				case 0:
					this.SetColliderDirty();
					break;
				case 1:
				{
					MyTuple<MyJumpGateController.MyControllerBlockSettingsStruct, double, float> arguments = payload.GetMetaData<MyTuple<MyJumpGateController.MyControllerBlockSettingsStruct, double, float>>();
					this.JumpToVoid(arguments.Item1, arguments.Item2, arguments.Item3);
					break;
				}
				case 2:
				{
					KeyValuePair<MyJumpGateController.MyControllerBlockSettingsStruct, List<MyPrefabInfo>> arguments = payload.GetMetaData<KeyValuePair<MyJumpGateController.MyControllerBlockSettingsStruct, List<MyPrefabInfo>>>();
					this.JumpFromVoid(arguments.Key, arguments.Value, null);
					break;
				}
			}
		}

		/// <summary>
		/// If server: Transmits this jump gate to all clients
		/// </summary>
		private void SendNetworkJumpGateUpdate()
		{
			if (!MyNetworkInterface.IsMultiplayerServer) return;

			MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet
			{
				PacketType = MyPacketTypeEnum.UPDATE_JUMP_GATE,
				TargetID = 0,
				Broadcast = true,
			};

			packet.Payload(this.ToSerialized(false));
			packet.Send();
			this.IsDirty = false;
			this.LastUpdateTime = packet.EpochTime;
			Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID}: Sent Network Gate Update", 4);
		}

		/// <summary>
		/// Sends a message to this player's hud if within range of this gate or it's controller
		/// </summary>
		/// <param name="msg">The message</param>
		/// <param name="delay">The message duration in ms</param>
		/// <param name="font">The font</param>
		private void SendHudMessage(string msg, int delay = 1000, string font = "White")
		{
			if (MyAPIGateway.Session.LocalHumanPlayer == null) return;
			Vector3D character_pos = MyAPIGateway.Session.LocalHumanPlayer.GetPosition();
			if (!MyNetworkInterface.IsDedicatedMultiplayerServer && ((character_pos != null && this.IsPointInHudBroadcastRange(ref character_pos)) || this?.Controller?.JumpGateGrid?.GetRadioAntenna(MyAPIGateway.Gui.InteractedEntity?.EntityId ?? -1) != null)) MyAPIGateway.Utilities.ShowNotification(msg, delay, font);
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Closes this gate, unregisters handlers, and frees all resources<br />
		/// To be called only by session
		/// </summary>
		public void Close()
		{
			lock (this.UpdateLock)
			{
				if (this.Closed) return;
				MyGateInvalidationReason reason = this.GetInvalidationReason();
				JumpGateUUID uuid = JumpGateUUID.FromJumpGate(this);
				string name = this.GetPrintableName();
				List<MyJumpGateDrive> drives = this.GetJumpGateDrives().ToList();
				foreach (MyJumpGateDrive drive in drives) drive.SetAttachedJumpGate(null);
				foreach (KeyValuePair<ulong, KeyValuePair<Vector3D?, MySoundEmitter3D>> pair in this.SoundEmitters) pair.Value.Value.Dispose(true);
				this.Controller?.AttachedJumpGate(null);
				this.Release();
				lock (this.DriveIntersectNodesMutex) this.InnerDriveIntersectNodes.Clear();
				this.Closed = true;

				foreach (KeyValuePair<long, KeyValuePair<float, MyEntity>> pair in this.JumpSpaceEntities)
				{
					MyEntity entity = pair.Value.Value ?? (MyEntity) MyAPIGateway.Entities.GetEntityById(pair.Key);
					if (entity != null) this.OnEntityJumpSpaceLeave(entity);
				}

				if (this.JumpSpaceCollisionDetector != null)
				{
					this.JumpSpaceCollisionDetector.Close();
					this.JumpSpaceCollisionDetector = null;
				}

				if (MyJumpGateModSession.Instance != null && MyJumpGateModSession.Instance.Network.Registered && MyNetworkInterface.IsMultiplayerServer)
				{
					MyNetworkInterface.Packet update_packet = new MyNetworkInterface.Packet
					{
						PacketType = MyPacketTypeEnum.UPDATE_JUMP_GATE,
						TargetID = 0,
						Broadcast = true,
					};
					update_packet.Payload(this.ToSerialized(false));
					update_packet.Send();
				}

				this.SoundEmitters.Clear();
				this.JumpSpaceEntities.Clear();
				this.ShearBlocks.Clear();
				this.EntityBatches.Clear();
				this.JumpSpaceColliderEntities.Clear();
				this.DriveIntersectionPlanes.Clear();
				this.JumpSpaceExplosions.Clear();

				this.Status = MyJumpGateStatus.NONE;
				this.Phase = MyJumpGatePhase.NONE;
				this.JumpGateID = -1;
				this.LocalJumpNode = Vector3D.Zero;
				this.TrueLocalJumpEllipse = BoundingEllipsoidD.Zero;
				this.TrueWorldJumpEllipse = BoundingEllipsoidD.Zero;

				this.Controller = null;
				this.JumpGateGrid = null;
				this.InnerDriveIntersectNodes = null;
				this.JumpSpaceEntities = null;
				this.ShearBlocks = null;
				this.JumpSpaceColliderEntities = null;
				this.DriveIntersectionPlanes = null;
				this.SoundEmitters = null;
				this.EntityBatches = null;
				this.PowerSyphon = null;
				this.JumpSpaceExplosions = null;

				this.MarkClosed = true;
				this.IsClosing = false;

				if (MyJumpGateModSession.Instance != null) Logger.Debug($"Jump Gate \"{name}\" ({uuid.GetJumpGateGrid()}-{uuid.GetJumpGate()}) Closed - {reason}", 1);
			}
		}

		/// <summary>
		/// Resets this gate's status and phase after jump
		/// </summary>
		public void Reset(bool reset_sender = true)
		{
			if (this.Closed) return;
			bool valid = this.IsValid();
			this.Phase = (valid) ? MyJumpGatePhase.IDLE : MyJumpGatePhase.NONE;
			this.Status = (valid) ? MyJumpGateStatus.IDLE : MyJumpGateStatus.NONE;
			if (reset_sender && this.SenderGate != null && !this.SenderGate.Closed) this.SenderGate.Reset();
			this.SenderGate = null;
			this.TrueEndpoint = null;
			this.WormholeStartTime = null;
		}

		/// <summary>
		/// Unregisteres resources for partially initialized gates<br />
		/// For fully initialized gates: Call MyJumpGate::Dispose()
		/// </summary>
		public void Release()
		{
			if (MyJumpGateModSession.Instance == null) return;
			else if (MyJumpGateModSession.Instance.Network.Registered)
			{
				MyJumpGateModSession.Instance.Network.Off(MyPacketTypeEnum.JUMP_GATE_JUMP, this.OnNetworkJumpGateJump);
				MyJumpGateModSession.Instance.Network.Off(MyPacketTypeEnum.JUMP_GATE_JUMP_POLL, this.OnNetworkJumpGateJumpPoll);
				MyJumpGateModSession.Instance.Network.Off(MyPacketTypeEnum.UPDATE_JUMP_GATE, this.OnNetworkJumpGateUpdate);
				MyJumpGateModSession.Instance.Network.Off(MyPacketTypeEnum.AUTOACTIVATE, this.OnNetworkAutoActivationAlert);
				MyJumpGateModSession.Instance.Network.Off(MyPacketTypeEnum.SHEARWARNING, this.OnNetworkShearBlockAlert);
				MyJumpGateModSession.Instance.Network.Off(MyPacketTypeEnum.GATE_DEBUG, this.OnGateDebug);
				MyJumpGateModSession.Instance.Network.Off(MyPacketTypeEnum.GATE_DETONATE, this.OnGateDetonate);
			}

			if (MyNetworkInterface.IsServerLike) MyExplosions.OnExplosion -= this.OnExplosion;
		}

		/// <summary>
		/// Requests a jump gate update from the server<br />
		/// Does nothing if singleplayer or server<br />
		/// Does nothing if parent construct is suspended or closed
		/// </summary>
		public void RequestGateUpdate()
		{
			if (this.Closed || MyNetworkInterface.IsServerLike || this.JumpGateGrid.IsSuspended) return;
			MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet
			{
				PacketType = MyPacketTypeEnum.UPDATE_JUMP_GATE,
				Broadcast = false,
				TargetID = 0,
			};
			packet.Payload<MySerializedJumpGate>(this.ToSerialized(true));
			packet.Send();
		}

		/// <summary>
		/// Does one update of this gate's non-threadable attributes<br />
		/// This will tick this gate's power syphon, physics collider, and sound emitters<br />
		/// <b>Must be called from game thread</b>
		/// </summary>
		public void UpdateNonThreadable()
		{
			if (this.Closed || this.IsClosing || this.MarkClosed) return;
			bool suspended = this.JumpGateGrid == null || this.JumpGateGrid.IsSuspended || this.JumpGateGrid.CubeGrid == null;

			// Update physics collider
			if (this.JumpSpaceCollisionDetector != null && this.JumpSpaceCollisionDetector.Closed) this.JumpSpaceCollisionDetector = null;
			else if (this.JumpSpaceCollisionDetector != null) this.JumpSpaceCollisionDetector.WorldMatrix = MatrixD.Normalize(this.TrueWorldJumpEllipse.WorldMatrix);
			else if (this.ForceUpdateJumpSpaceDetector && this.JumpSpaceCollisionDetector != null && !this.JumpSpaceCollisionDetector.MarkedForClose) this.JumpSpaceCollisionDetector.Close();

			// Update jump space detector
			if (MyNetworkInterface.IsServerLike && this.JumpSpaceCollisionDetector == null && this.TrueLocalJumpEllipse != BoundingEllipsoidD.Zero && MyJumpGateModSession.Instance.AllSessionEntitiesLoaded && MyJumpGateModSession.Instance.AllFirstTickComplete())
			{
				MatrixD world_matrix = MatrixD.Normalize(this.TrueWorldJumpEllipse.WorldMatrix);
				this.JumpSpaceCollisionDetector = new MyEntity() {
					EntityId = 0,
					Save = false,
					Flags = EntityFlags.IsNotGamePrunningStructureObject,
					WorldMatrix = world_matrix,
				};
				this.JumpSpaceCollisionDetector.Init(new StringBuilder($"JumpGate_{this.JumpGateGrid.CubeGrid.EntityId}_{this.JumpGateID}"), null, null, 1f);
				this.JumpSpaceCollisionDetector.OnClosing += (entity) => Logger.Debug($"CLOSED_COLLIDER JUMP_GATE={this.GetPrintableName()}", 4);
				this.JumpSpaceColliderEntities.Clear();
				this.JumpSpaceEntities.Clear();
				float radius = (float) MyJumpGateModSession.Instance.MaxJumpGateColliderRadius;
				this.PhysicalDetector = new MyPhysicalDetector(this.JumpSpaceCollisionDetector, radius, this.OnEntityCollision);
				MyAPIGateway.Entities.AddEntity(this.JumpSpaceCollisionDetector);
				Logger.Debug($"CREATED_COLLIDER JUMP_GATE={this.GetPrintableName()}, COLLIDER={this.JumpSpaceCollisionDetector.DisplayName}, RADIUS={radius}", 4);
			}

			this.ForceUpdateJumpSpaceDetector = false;

			// Update power syphon
			this.PowerSyphon.Tick();

			// Update sound emitters
			if (MyNetworkInterface.IsClientLike)
			{
				foreach (KeyValuePair<ulong, KeyValuePair<Vector3D?, MySoundEmitter3D>> pair in this.SoundEmitters)
				{
					MySoundEmitter3D sound_emitter = pair.Value.Value;
					if (sound_emitter.Closed) this.SoundEmitters.Remove(pair.Key);
					else if (!sound_emitter.IsPlaying()) sound_emitter.Dispose();
					else sound_emitter.Update(pair.Value.Key ?? this.WorldJumpNode);
				}
			}

			Vector3D camera_pos = MyAPIGateway.Session.Camera.Position;
			double distance = Vector3D.Distance(camera_pos, this.WorldJumpNode);

			// Draw shear blocks
			if (!suspended && MyJumpGateModSession.Instance.Configuration.GeneralConfiguration.HighlightShearBlocks.Value && this.Phase == MyJumpGatePhase.JUMPING && this.Status == MyJumpGateStatus.OUTBOUND && distance <= MyJumpGateModSession.Instance.Configuration.GeneralConfiguration.DrawSyncDistance)
			{
				BoundingBoxD localbox;
				Color warn_color = Color.Red;
				MyStringId warn_material = MyStringId.GetOrCompute("IOTA.SpecialEffects.BlockWarningOutline");
				MyStringId line_material = MyStringId.GetOrCompute("WeaponLaser");

				foreach (IMySlimBlock block in this.ShearBlocks)
				{
					MatrixD world_matrix = block.CubeGrid.WorldMatrix;
					BoundingSphereD sphere = new BoundingSphereD(block.Position * block.CubeGrid.GridSize, block.CubeGrid.GridSize / 2f * 1.025d);
					BoundingBoxD.CreateFromSphere(ref sphere, out localbox);
					MySimpleObjectDraw.DrawTransparentBox(ref world_matrix, ref localbox, ref warn_color, MySimpleObjectRasterizer.SolidAndWireframe, 1, 0.01f, warn_material, line_material, intensity: 10);
				}
			}

			// Display notifications
			if (!suspended && MyNetworkInterface.IsClientLike && this.IsPointInHudBroadcastRange(ref camera_pos))
			{
				if (this.AutoActivateStartTime != null && this.AutoActivationNotif != null && this.Controller != null)
				{
					double seconds = (DateTime.UtcNow - this.AutoActivateStartTime.Value).TotalSeconds;
					float auto_activation_delay = this.Controller.BlockSettings.AutoActivationDelay();
					this.AutoActivationNotif.Hide();
					this.AutoActivationNotif.ResetAliveTime();
					//if (seconds >= 0) this.AutoActivationNotif.Text = MyTexts.GetString("Notification_JumpGate_GateAutoActivationIn").Replace("{%0}", $"{Math.Max(0, Math.Round(auto_activation_delay - seconds, 2))}s");
					//else this.AutoActivationNotif.Text = MyTexts.GetString("Notification_JumpGate_GateRetryAutoActivationIn").Replace("{%0}", $"{Math.Max(0, Math.Round(-seconds, 2))}s");
					if (seconds >= 0) this.AutoActivationNotif.Text = MyTexts.GetString("Notification_JumpGate_GateAutoActivationIn").Replace("{%0}", MyJumpGateModSession.AutoconvertTimeHHMMSS(auto_activation_delay - seconds));
					else this.AutoActivationNotif.Text = MyTexts.GetString("Notification_JumpGate_GateRetryAutoActivationIn").Replace("{%0}", MyJumpGateModSession.AutoconvertTimeHHMMSS(-seconds));
					this.AutoActivationNotif.Show();
				}
				else this.AutoActivationNotif?.Hide();

				if (this.HasBlocksWithinShearZone && this.Phase == MyJumpGatePhase.JUMPING && this.Status == MyJumpGateStatus.OUTBOUND) this.ShearBlocksWarning.Show();
				else this.ShearBlocksWarning.Hide();
			}
		}

		/// <summary>
		/// Does one update of this gate<br />
		/// This will close this gate if:<br />
		///  ... The session is not running<br />
		///  ... This gate is not valid<br />
		/// </summary>
		public void Update(ulong grid_local_tick, bool update_ellipses = false, bool update_entities = false)
		{
			// Check update validity
			if (this.Closed || this.IsClosing || this.MarkClosed || this.JumpGateGrid == null || this.JumpGateGrid.IsSuspended || this.JumpGateGrid.CubeGrid == null || !Monitor.TryEnter(this.UpdateLock)) return;

			try
			{
				bool grid_invalid = this.JumpGateGrid == null || this.MarkClosed || (!this.JumpGateGrid.IsSuspended && !this.JumpGateGrid.IsValid());

				if (MyJumpGateModSession.Instance.SessionStatus != MySessionStatusEnum.RUNNING || (grid_invalid && this.Status != MyJumpGateStatus.OUTBOUND && this.Status != MyJumpGateStatus.SWITCHING && this.Status != MyJumpGateStatus.CANCELLED))
				{
					foreach (MyJumpGateAnimation animation in MyJumpGateModSession.Instance.GetGateAnimationsPlaying(this)) MyJumpGateModSession.Instance.StopAnimation(animation);
					MyJumpGateModSession.Instance.CloseGate(this);
					return;
				}
				else if (grid_invalid && (this.Status == MyJumpGateStatus.OUTBOUND || this.Status == MyJumpGateStatus.SWITCHING || this.Status == MyJumpGateStatus.CANCELLED)) return;
				if (this.Controller?.IsClosed ?? false) this.Controller = null;

				if (grid_local_tick % 10 == 0 || update_ellipses)
				{
					this.ConstructMatrix = this.JumpGateGrid.CubeGrid?.WorldMatrix ?? this.ConstructMatrix;
					this.ConstructMatrixInv = this.JumpGateGrid.CubeGrid?.WorldMatrixInvScaled ?? this.ConstructMatrixInv;
					this.TrueLocalJumpEllipse.Transform(ref this.ConstructMatrix, out this.TrueWorldJumpEllipse);
				}

				// Update jump space ellipsoid
				if (MyNetworkInterface.IsServerLike && update_ellipses || this.ForceUpdateJumpEllipsoid || this.TrueLocalJumpEllipse == BoundingEllipsoidD.Zero) this.UpdateJumpSpaceEllipsoid();

				// Update jump space entities
				if (MyNetworkInterface.IsServerLike && update_entities && !this.IsWormholeActive && this.IsControlled() && !this.MarkClosed) this.UpdateJumpSpaceEntities();

				// Check auto activation
				if (MyNetworkInterface.IsServerLike && this.Controller != null && !this.Controller.MarkedForClose && this.Controller.BlockSettings.CanAutoActivate() && this.Phase == MyJumpGatePhase.IDLE && this.Controller.BlockSettings.SelectedWaypoint() != null)
				{
					double total_mass = this.JumpSpaceEntities.Sum((pair) => (double) pair.Value.Key);
					double total_power = this.CalculateTotalRequiredPower(mass: total_mass);
					KeyValuePair<float, float> required_mass = this.Controller.BlockSettings.AutoActivateMass();
					KeyValuePair<double, double> required_power = this.Controller.BlockSettings.AutoActivatePower();
					bool check_pass = total_mass > required_mass.Key && total_mass < required_mass.Value && total_power > required_power.Key && total_power < required_power.Value;
					float activation_delay = this.Controller.BlockSettings.AutoActivationDelay();
					DateTime now = DateTime.UtcNow;

					if (total_mass == 0 || this.JumpSpaceEntities.Count == 0 || !check_pass)
					{
						this.AutoActivateStartTime = null;

						if (MyJumpGateModSession.Instance.Network.Registered)
						{
							MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet
							{
								PacketType = MyPacketTypeEnum.AUTOACTIVATE,
								TargetID = 0,
								Broadcast = true,
							};

							packet.Payload(new KeyValuePair<JumpGateUUID, DateTime?>(JumpGateUUID.FromJumpGate(this), this.AutoActivateStartTime));
							packet.Send();
						}
					}
					else if (this.AutoActivateStartTime == null && activation_delay == 0) this.CheckExecuteAutoJump();
					else if (this.AutoActivateStartTime == null)
					{
						this.AutoActivateStartTime = now;

						if (MyJumpGateModSession.Instance.Network.Registered)
						{
							MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet
							{
								PacketType = MyPacketTypeEnum.AUTOACTIVATE,
								TargetID = 0,
								Broadcast = true,
							};

							packet.Payload(new KeyValuePair<JumpGateUUID, DateTime?>(JumpGateUUID.FromJumpGate(this), this.AutoActivateStartTime));
							packet.Send();
						}
					}
					else if (this.AutoActivateStartTime != null && (now - this.AutoActivateStartTime.Value).TotalSeconds >= activation_delay) this.CheckExecuteAutoJump();
				}
				else if (MyNetworkInterface.IsServerLike && this.AutoActivateStartTime != null)
				{
					this.AutoActivateStartTime = null;

					if (MyJumpGateModSession.Instance.Network.Registered)
					{
						MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet
						{
							PacketType = MyPacketTypeEnum.AUTOACTIVATE,
							TargetID = 0,
							Broadcast = true,
						};

						packet.Payload(new KeyValuePair<JumpGateUUID, DateTime?>(JumpGateUUID.FromJumpGate(this), this.AutoActivateStartTime));
						packet.Send();
					}
				}

				// Check lenient jump blocks
				if (this.Controller != null && !MyJumpGateModSession.Instance.Configuration.GeneralConfiguration.LenientJumps.Value && grid_local_tick % 60 == 0)
				{
					this.ShearBlocks.Clear();
					BoundingEllipsoidD jump_ellipse = this.JumpEllipse;
					BoundingEllipsoidD shear_ellipse = this.ShearEllipse;

					foreach (KeyValuePair<long, KeyValuePair<float, MyEntity>> pair in this.JumpSpaceEntities)
					{
						MyJumpGateConstruct grid = MyJumpGateModSession.Instance.GetJumpGateGrid(pair.Key);
						if (grid == null) continue;
						IEnumerator<IMySlimBlock> iterator = grid.GetConstructBlocks();

						while (iterator.MoveNext())
						{
							IMySlimBlock block = iterator.Current;
							Vector3D pos = block.CubeGrid.GridIntegerToWorld(block.Position);
							if (shear_ellipse.IsPointInEllipse(pos) && !jump_ellipse.IsPointInEllipse(pos)) this.ShearBlocks.Add(block);
						}
					}
				}

				// Update Jump Node Velocity
				this.OldPosition = this.NewPosition;
				this.NewPosition = this.WorldJumpNode;
				DateTime new_time = DateTime.UtcNow;
				double time_delta = (new_time - this.LastScanTime).TotalSeconds;
				this.JumpNodeVelocity = (this.NewPosition - this.OldPosition) / time_delta;
				this.LastScanTime = new_time;
				MyGateInvalidationReason invalid_reason = this.GetInvalidationReason();

				// Clean Invalid
				if (invalid_reason != MyGateInvalidationReason.NONE)
				{
					string main_grid = this.JumpGateGrid?.CubeGrid?.EntityId.ToString() ?? "N/A";
					Logger.Debug($"Invalid Jump Gate {main_grid}::{this.JumpGateID} - {invalid_reason}", 2);

					switch (invalid_reason)
					{
						case MyGateInvalidationReason.CLOSED:
							Logger.Debug($" ... Closure Info - {main_grid}::{this.JumpGateID} - CLOSED={this.Closed}, MARKED_CLOSED={this.MarkClosed}", 2);
							break;
						case MyGateInvalidationReason.INSUFFICIENT_DRIVES:
						{
							Logger.Debug($" ... Closure Info - {main_grid}::{this.JumpGateID} - DRIVE_COUNT={this.GetJumpGateDrives().Count()}, INTERSECTIONS={this.InnerDriveIntersectNodes.Count}, GRID_DRIVE_INFO=\n[{string.Join("\n ... ", this.JumpGateGrid?.GetAttachedJumpGateDrives().Select((drive) => $"GRID={drive.CubeGridID}, GATE={drive.JumpGateID}, CLOSED={drive.IsClosed}"))}]", 2);
							break;
						}
					}

					this.Status = MyJumpGateStatus.NONE;
					this.Phase = MyJumpGatePhase.NONE;
					this.Dispose();
				}
				else if (this.Controller == null)
				{
					this.Status = MyJumpGateStatus.IDLE;
					this.Phase = MyJumpGatePhase.IDLE;
				}

				// Update gate explosion
				if (MyNetworkInterface.IsServerLike && MyJumpGateModSession.Instance.Configuration.JumpGateConfiguration.JumpGateExplosionConfiguration.EnableGateExplosions.Value && !this.IsDetonating && this.IsJumping())
				{
					float percentage = 1 - this.GetFunctionalDrivePercentage();
					MyJumpGateDrive initiator = this.GetJumpGateDrives().FirstOrDefault((drive) => drive.TerminalBlock != null && !drive.TerminalBlock.IsFunctional);
					if (initiator != null && (percentage >= this.JumpGateConfiguration.ExplosionDamagePercent || !this.GetJumpGateDrives().Where((drive) => drive.TerminalBlock != null && drive.TerminalBlock.IsFunctional).AtLeast(2))) this.Detonate(initiator);
				}

				// Self Destruct
				if (MyNetworkInterface.IsServerLike && this.ManualDetonationTimeout == 0) this.Detonate();
				else if (this.ManualDetonationTimeout > 0) --this.ManualDetonationTimeout;

				// Kick Unauthorized Controllers
				if (MyNetworkInterface.IsServerLike && MyJumpGateModSession.Instance.GameTick % 30 == 0)
				{
					float ratio = this.JumpGateConfiguration.MinimumControlOwnerFactionRatio;
					List<IMyPlayer> players = new List<IMyPlayer>();
					MyAPIGateway.Players.GetPlayers(players);
					IMyPlayer controller = players.FirstOrDefault((player) => player.IdentityId == this.Controller?.OwnerID);

					if (controller != null && this.GetFactionControlRatio(controller) < ratio)
					{
						if (this.RemoteAntenna != null && this.RemoteAntenna.GetInboundControlGate(this.RemoteAntennaChannel) == this)
						{
							this.RemoteAntenna.SetGateForInboundControl(this.RemoteAntennaChannel, null);
							this.RemoteAntenna = null;
						}
						else if (this.Controller != null && this.Controller.AttachedJumpGate() == this)
						{
							this.Controller.AttachedJumpGate(null);
							this.Controller = null;
						}
					}
				}

				if (this.IsDirty && MyJumpGateModSession.Instance.Network.Registered) this.SendNetworkJumpGateUpdate();
			}
			finally
			{
				if (Monitor.IsEntered(this.UpdateLock)) Monitor.Exit(this.UpdateLock);
			}
		}

		/// <summary>
		/// Markes this construct for close
		/// </summary>
		public void Dispose()
		{
			if (this.Closed || this.MarkClosed) return;
			this.MarkClosed = true;
			if (MyJumpGateModSession.Instance.SessionStatus == MySessionStatusEnum.UNLOADING) MyJumpGateModSession.Instance.CloseGate(this);
		}

		/// <summary>
		/// Marks this jump gate as dirty<br />
		/// It will be synced to clients on next tick
		/// </summary>
		public void SetDirty()
		{
			if (this.Closed) return;
			this.IsDirty = true;
			this.LastUpdateDateTimeUTC = DateTime.UtcNow;
		}

		/// <summary>
		/// Sends a request to get gate wormhole status from server<br />
		/// <i>Does nothing if not standalone MP client</i>
		/// </summary>
		public void PollGateWormholeStatus()
		{
			if (!MyNetworkInterface.IsStandaloneMultiplayerClient || !MyJumpGateModSession.Instance.Network.Registered) return;

			MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet
			{
				PacketType = MyPacketTypeEnum.JUMP_GATE_JUMP_POLL,
				TargetID = 0,
				Broadcast = false,
			};

			packet.Payload(new JumpGatePollingInfo
			{
				ThisGate = JumpGateUUID.FromJumpGate(this),
			});

			packet.Send();
		}

		/// <summary>
		/// Syphons power from the parent MyJumpGateConstruct
		/// </summary>
		/// <param name="power_mw">The power to draw (will be split amongst all working drives for this gate and across the time specified)</param>
		/// <param name="ticks">The duration of the test in game ticks</param>
		/// <param name="callback">The callback to pass the result to, accepting a single bool representing whether all power was draw successfully</param>
		/// <param name="syphon_grid_only">If true, skips drive and capacitor power syphon</param>
		public void CanDoSyphonGridPower(double power_mw, ulong ticks, Action<bool> callback, bool syphon_grid_only = false)
		{
			if (this.Closed) return;
			this.PowerSyphon.DoSyphonPower(power_mw, ticks, callback, syphon_grid_only);
		}

		/// <summary>
		/// Marks this gate as inbound, receiving a connection from the specified gate
		/// </summary>
		/// <param name="source">Null to reset this gate's status or the gate connecting to this one</param>
		/// <exception cref="InvalidOperationException">If this gate is not IDLE</exception>
		public void ConnectAsInbound(MyJumpGate source)
		{
			if (this.Closed || this.MarkClosed) return;
			else if (source == null || !source.IsValid())
			{
				this.Reset();
				this.SenderGate = null;
			}
			else if (this.IsIdle())
			{
				this.Status = MyJumpGateStatus.INBOUND;
				this.Phase = MyJumpGatePhase.CHARGING;
				this.SenderGate = source;
			}
			else throw new InvalidOperationException("Failed to set inbound");
		}

		/// <summary>
		/// Checks if a point is within twice the distance of the largest jump ellipse radius or within 500 meters of the attached controller
		/// </summary>
		/// <param name="pos">The world position to check</param>
		/// <returns>True if in range</returns>
		public bool IsPointInHudBroadcastRange(ref Vector3D pos)
		{
			if (this.Closed) return false;
			BoundingEllipsoidD jump_ellipse = this.JumpEllipse;
			return new BoundingSphereD(jump_ellipse.WorldMatrix.Translation, jump_ellipse.Radii.Max() * 2).Contains(pos) == ContainmentType.Contains || (this.Controller != null && new BoundingSphereD(this.Controller.WorldMatrix.Translation, 50).Contains(pos) == ContainmentType.Contains);
		}

		/// <param name="entity">The entity to check</param>
		/// <returns>Whether entity is a functional part of this gate</returns>
		public bool IsEntityPartOfJumpGate(MyEntity entity)
		{
			return entity != null && entity != this.JumpSpaceCollisionDetector;
		}

		/// <summary>
		/// Whether this grid is valid
		/// </summary>
		/// <returns>True if valid</returns>
		public bool IsValid()
		{
			return this.GetInvalidationReason() == MyGateInvalidationReason.NONE;
		}

		/// <summary>
		/// Whether this grid is valid and a controller attached
		/// </summary>
		/// <returns>True if controlled</returns>
		public bool IsControlled()
		{
			return this.IsValid() && this.Controller != null;
		}

		/// <summary>
		/// Whether this grid is valid and either a controller or remote antenna attached
		/// </summary>
		/// <returns>True if complete</returns>
		public bool IsComplete()
		{
			return this.IsValid() && (this.Controller != null || this.RemoteAntenna != null);
		}

		/// <summary>
		/// </summary>
		/// <returns>True if this gate is idle</returns>
		public bool IsIdle()
		{
			return !this.Closed && this.Status == MyJumpGateStatus.IDLE && this.Phase == MyJumpGatePhase.IDLE;
		}

		/// <summary>
		/// </summary>
		/// <returns>True if this gate is large grid</returns>
		public bool IsLargeGrid()
		{
			if (this.Closed) return false;
			else if (this.DriveGridSize == null) return this.GetJumpGateDrives().Any((drive) => drive.IsLargeGrid);
			else return this.DriveGridSize.Value == MyCubeSize.Large;
		}

		/// <summary>
		/// </summary>
		/// <returns>True if this gate is small grid</returns>
		public bool IsSmallGrid()
		{
			if (this.Closed) return false;
			else if (this.DriveGridSize == null) return this.GetJumpGateDrives().Any((drive) => drive.IsSmallGrid);
			else return this.DriveGridSize.Value == MyCubeSize.Small;
		}

		/// <summary>
		/// Checks whether this jump gate is suspended<br />
		/// Gate is suspended if parent construct is suspended or at least one drive is a null wrapper<br />
		/// Always false on server or singleplayer
		/// </summary>
		/// <returns>Suspendedness</returns>
		public bool IsSuspended()
		{
			return (this.JumpGateGrid?.IsSuspended ?? false) || this.GetJumpGateDrives().Any((drive) => drive.IsNullWrapper);
		}

		/// <summary>
		/// </summary>
		/// <returns>Whether this jump gate can be safely closed</returns>
		public bool CanFinalizeClosure()
		{
			return (this.Closed || this.JumpGateGrid == null || MyJumpGateModSession.Instance.SessionStatus != MySessionStatusEnum.RUNNING || ((this.Status == MyJumpGateStatus.NONE || this.Status == MyJumpGateStatus.IDLE) && (this.Phase == MyJumpGatePhase.NONE || this.Phase == MyJumpGatePhase.IDLE))) && MyJumpGateModSession.Instance.GetGateDetonationInfo(JumpGateUUID.FromJumpGate(this)) == null;
		}

		/// <summary>
		/// Checks if this MyJumpGate equals another
		/// </summary>
		/// <param name="other">The MyJumpGate to check</param>
		/// <returns>Equality</returns>
		public bool Equals(MyJumpGate other)
		{
			return other != null && this.JumpGateGrid == other.JumpGateGrid && this.JumpGateID == other.JumpGateID;
		}

		/// <summary>
		/// Gets the reason this gate is invalid
		/// </summary>
		/// <returns>The invalidation reason or InvalidationReason.NONE</returns>
		public MyGateInvalidationReason GetInvalidationReason()
		{
			if (this.Closed) return MyGateInvalidationReason.CLOSED;
			else if (this.JumpGateGrid == null) return MyGateInvalidationReason.NULL_GRID;
			else if (this.Status == MyJumpGateStatus.NONE) return MyGateInvalidationReason.NULL_STATUS;
			else if (this.Phase == MyJumpGatePhase.NONE) return MyGateInvalidationReason.NULL_PHASE;
			else if (this.JumpGateID < 0) return MyGateInvalidationReason.INVALID_ID;
			else if (this.InnerDriveIntersectNodes.Count < 1) return MyGateInvalidationReason.INSUFFICIENT_NODES;
			else if (this.MarkClosed) return MyGateInvalidationReason.CLOSED;
			else if (!this.GetJumpGateDrives().AtLeast(2)) return MyGateInvalidationReason.INSUFFICIENT_DRIVES;
			else return MyGateInvalidationReason.NONE;
		}

		/// <summary>
		/// Gets the number of functional drives in this jump gate out of the total number of drives<br />
		/// If this is not server like, returns -1
		/// </summary>
		/// <returns>The percentage of functional drives or -1 if not server like</returns>
		public float GetFunctionalDrivePercentage()
		{
			if (!MyNetworkInterface.IsServerLike) return -1;
			int total = 0;
			int functional = 0;

			foreach (MyJumpGateDrive drive in this.GetJumpGateDrives())
			{
				++total;
				if (drive.TerminalBlock.IsFunctional) ++functional;
			}

			return ((float) functional) / total;
		}

		/// <summary>
		/// Gets the percentage of drives owned by the player or player's faction
		/// </summary>
		/// <param name="player">The player</param>
		/// <returns>Percentage of owned drives</returns>
		public float GetFactionControlRatio(IMyPlayer player)
		{
			if (player == null || this.Closed) return 0;
			IMyFaction player_faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(player.IdentityId);
			uint total = 0;
			uint owned = 0;

			foreach (MyJumpGateDrive drive in this.GetJumpGateDrives())
			{
				if (drive.OwnerID == 0) continue;
				++total;
				IMyFaction drive_faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(drive.OwnerID);
				if (player_faction != null && drive_faction != null && player_faction.FactionId == drive_faction.FactionId) ++owned;
				else if (player_faction == null && drive_faction == null && drive.OwnerID == player.IdentityId) ++owned;
			}

			return (float) ((double) owned / (double) total);
		}

		/// <summary>
		/// Calculates the extra power multiplier caused from exceeding the wormhole time limit
		/// </summary>
		/// <returns>The overrun multiplier</returns>
		public float CalculatePowerOverrunMultiplier()
		{
			if (this.Closed || !this.IsWormholeActive) return 0;
			DateTime now = DateTime.UtcNow;
			double overflow = (now - this.WormholeStartTime.Value).TotalSeconds;
			return (overflow <= this.JumpGateConfiguration.MaxWormholeDurationSeconds) ? 1 : (float) (1 + ((overflow - this.JumpGateConfiguration.MaxWormholeDurationSeconds) * this.JumpGateConfiguration.OverrunPowerMultiplierPerSecond));
		}

		/// <summary>
		/// Calculates the extra power multiplier caused from exceeding the wormhole time limit<br />
		/// This version rounds overrun time to the nearest place
		/// </summary>
		/// <param name="places">The number of places to round the overrun time to</param>
		/// <returns>The overrun multiplier</returns>
		public float CalculateSteppedPowerOverrunMultiplier(int places)
		{
			if (this.Closed || !this.IsWormholeActive) return 0;
			DateTime now = DateTime.UtcNow;
			double overflow = Math.Round((now - this.WormholeStartTime.Value).TotalSeconds, places);
			return (overflow <= this.JumpGateConfiguration.MaxWormholeDurationSeconds) ? 1 : (float) (1 + ((overflow - this.JumpGateConfiguration.MaxWormholeDurationSeconds) * this.JumpGateConfiguration.OverrunPowerMultiplierPerSecond));
		}

		/// <summary>
		/// Attemps to drain the specified power from all working drives attached to this gate
		/// </summary>
		/// <param name="power_mw">The amount of drain to syphon in MegaWatts</param>
		/// <returns>The remaining power after syphon</returns>
		public double SyphonGridDrivePower(double power_mw)
		{
			if (this.Closed || power_mw <= 0) return 0;
			List<MyJumpGateDrive> drives = this.GetWorkingJumpGateDrives().Where((drive) => drive.StoredChargeMW > 0).ToList();
			double power_per = power_mw / drives.Count;
			power_mw = 0;
			foreach (MyJumpGateDrive drive in drives) power_mw += drive.DrainStoredCharge(power_per);
			return power_mw;
		}

		/// <summary>
		/// Calculates the distance ratio given a target endpoint
		/// </summary>
		/// <param name="endpoint">The target endpont</param>
		/// <returns>A ratio between this gate's maximum distance and minimum distance</returns>
		public double CalculateDistanceRatio(ref Vector3D endpoint)
		{
			if (this.Closed) return 0;
			double distance = Vector3D.Distance(this.WorldJumpNode, endpoint);
			double max_distance = this.CalculateMaxGateDistance();
			double min_distance = this.JumpGateConfiguration.MinimumJumpDistance;
			return (distance - min_distance) / (max_distance - min_distance);
		}

		/// <summary>
		/// Calculates the power multiplier given a distance ratio
		/// </summary>
		/// <param name="ratio">The distance ratio</param>
		/// <returns>The power factor</returns>
		public double CalculatePowerFactorFromDistanceRatio(double ratio)
		{
			if (this.Closed) return 0;
			double z = this.JumpGateConfiguration.GatePowerFalloffFactor;
			double s = this.JumpGateConfiguration.GatePowerFactorExponent;
			double x = Math.Max(ratio, 0);
			double power_factor = Math.Pow(z * x - z, s) + 1;
			return double.IsNaN(power_factor) ? double.PositiveInfinity : power_factor;
		}

		/// <summary>
		/// Calculates the power density (in Kw/Kg) required for a single kilogram to jump
		/// </summary>
		/// <param name="endpoint">The target endpoint</param>
		/// <returns>The required power density</returns>
		public double CalculateUnitPowerRequiredForJump(ref Vector3D endpoint)
		{
			if (this.Closed || !this.IsValid()) return -1;
			double ratio = this.CalculateDistanceRatio(ref endpoint);
			double factor = this.CalculatePowerFactorFromDistanceRatio(ratio);
			return this.JumpGateConfiguration.GateKilowattPerKilogram * factor;
		}

		/// <summary>
		/// Calculates the maximum distance this gate can jump such that the power factor does not exceed 1
		/// </summary>
		/// <returns>The max reasonable distance in meters</returns>
		public double CalculateMaxGateDistance()
		{
			if (this.Closed) return 0;
			int drive_count = this.GetWorkingJumpGateDrives().Count();
			return Math.Pow(drive_count, this.JumpGateConfiguration.GateDistanceScaleExponent) * 1000d + this.JumpGateConfiguration.MinimumJumpDistance;
		}

		/// <summary>
		/// Calculates the total required power to jump some amount of mass a to the specified coordinate
		/// </summary>
		/// <param name="endpoint">The target world coordinate</param>
		/// <param name="has_target_gate">Whether to treat the calculation as if a target gate is present</param>
		/// <param name="mass">The mass in kilograms</param>
		/// <returns>The required power in megawatts</returns>
		public double CalculateTotalRequiredPower(Vector3D? endpoint = null, bool? has_target_gate = null, double? mass = null)
		{
			if (this.Closed) return 0;
			MyJumpGateWaypoint waypoint = this.Controller?.BlockSettings.SelectedWaypoint();
			endpoint = endpoint ?? (waypoint?.GetEndpoint());
			if (endpoint == null) return double.NaN;
			bool _has_target_gate = has_target_gate ?? (waypoint?.WaypointType == MyWaypointType.JUMP_GATE);
			double total_mass_kg = mass ?? this.JumpSpaceEntities.Sum((pair) => (double) pair.Value.Key);
			double power_scaler = (_has_target_gate) ? 1 : this.JumpGateConfiguration.UntetheredJumpEnergyCost;
			Vector3D end = endpoint.Value;
			return Math.Round(total_mass_kg * this.CalculateUnitPowerRequiredForJump(ref end), 8) * power_scaler / 1000;
		}

		/// <summary>
		/// Calculates the total available power stored within construct capacitors and drives for the specified jump gate
		/// </summary>
		/// <returns>Total available instant power in Mega-Watts</returns>
		public double CalculateTotalAvailableInstantPower()
		{
			return this.JumpGateGrid?.CalculateTotalAvailableInstantPower(this.JumpGateID) ?? 0;
		}

		/// <summary>
		/// Calculates the total possible power stored within construct capacitors and drives for the specified jump gate
		/// </summary>
		/// <returns>Total possible instant power in Mega-Watts</returns>
		public double CalculateTotalPossibleInstantPower()
		{
			return this.JumpGateGrid?.CalculateTotalPossibleInstantPower(this.JumpGateID) ?? 0;
		}

		/// <summary>
		/// Calculates the total possible power stored within construct capacitors and drives for the specified jump gate<br />
		/// This method ignores block settings and whether block is working
		/// </summary>
		/// <returns>Total max possible instant power in Mega-Watts</returns>
		public double CalculateTotalMaxPossibleInstantPower()
		{
			return this.JumpGateGrid?.CalculateTotalMaxPossibleInstantPower(this.JumpGateID) ?? 0;
		}

		/// <summary>
		/// Calculates the total explosive power currently within this gate's jump space
		/// </summary>
		/// <returns>The total explosive power or 0 if closed</returns>
		public double CalculateExplosivePowerWithinJumpSpace()
		{
			return this.JumpSpaceExplosions?.Sum((explosion) => explosion.CurrentExplosionPower) ?? 0;
		}

		/// <summary>
		/// Calculates the minimum number of drives needed to achieve a jump of some distance such that the power factor does not exceed 1
		/// </summary>
		/// <param name="distance">The jump distance in meters</param>
		/// <returns>The number of drives required</returns>
		public int CalculateDrivesRequiredForDistance(double distance)
		{
			if (this.Closed) return 0;
			double drive_count = Math.Pow((distance - this.JumpGateConfiguration.MinimumJumpDistance) / 1000d, 1d / this.JumpGateConfiguration.GateDistanceScaleExponent);
			return (int) Math.Ceiling(drive_count);
		}

		/// <summary>
		/// Gets the ID of the person with the most drives on this gate<br />
		/// If a control object is attached, owner of controller is returned
		/// </summary>
		/// <returns>Primary owner ID or -1 if closed</returns>
		public long GetPrimaryOwnerID()
		{
			if (this.Closed) return -1;
			else if (this.ControlObject == null) return this.GetJumpGateDrives().Select((drive) => drive.OwnerID).GroupBy(i => i).OrderByDescending(grp => grp.Count()).Select(grp => grp.Key).FirstOrDefault();
			else if (this.ControlObject.IsController) return this.ControlObject.Controller.OwnerID;
			else return this.ControlObject.RemoteAntenna.OwnerID;
		}

		/// <summary>
		/// Gets the steam ID of the person with the most drives on this gate<br />
		/// If a control object is attached, owner of controller is returned
		/// </summary>
		/// <returns>Primary owner steam ID or 0 if closed</returns>
		public ulong GetPrimaryOwnerSteamID()
		{
			if (this.Closed) return 0;
			else if (this.ControlObject == null) return this.GetJumpGateDrives().Select((drive) => drive.OwnerSteamID).GroupBy(i => i).OrderByDescending(grp => grp.Count()).Select(grp => grp.Key).FirstOrDefault();
			else if (this.ControlObject.IsController) return this.ControlObject.Controller.OwnerSteamID;
			else return this.ControlObject.RemoteAntenna.OwnerSteamID;
		}

		/// <summary>
		/// Gets the player with the most drives on this gate<br />
		/// If a control object is attached, owner of controller is returned
		/// </summary>
		/// <returns>Primary owner player or null if closed</returns>
		public IMyPlayer GetPrimaryOwner()
		{
			ulong steam_id = this.GetPrimaryOwnerSteamID();
			if (steam_id == 0) return null;
			List<IMyPlayer> players = new List<IMyPlayer>();
			MyAPIGateway.Players.GetPlayers(players);
			return players.FirstOrDefault((player) => player.SteamUserId == steam_id);
		}

		/// <summary>
		/// </summary>
		/// <returns>The cube grid size of this gate</returns>
		public MyCubeSize CubeGridSize()
		{
			if (this.Closed) return MyCubeSize.Large;
			this.DriveGridSize = this.GetJumpGateDrives().FirstOrDefault()?.CubeGridSize ?? this.DriveGridSize;
			return this.DriveGridSize.Value;
		}

		/// <summary>
		/// Gets the world matrix for this gate<br />
		/// Resulting matrix may be affected by the normal override and endpoint
		/// </summary>
		/// <param name="use_endpoint">Whether to calculate the world matrix using this gate's endpoint</param>
		/// <param name="use_normal_override">Whether to calculate the world matrix using this gate's normal override</param>
		/// <param name="normalized">Whether to normalize the world matrix</param>
		/// <returns>This gate's world matrix</returns>
		public MatrixD GetWorldMatrix(bool use_endpoint, bool use_normal_override, bool normalized = true)
		{
			if (this.Closed) return MatrixD.Identity;
			MyJumpGateControlObject control_object = this.ControlObject;
			Vector3D? endpoint = control_object?.BlockSettings?.SelectedWaypoint()?.GetEndpoint();
			Vector3D? normal_override = control_object?.BlockSettings?.VectorNormalOverride();
			Vector3D world_node = this.WorldJumpNode;
			MatrixD jump_ellipse_matrix = (normalized) ? MatrixD.Normalize(this.TrueWorldJumpEllipse.WorldMatrix) : this.TrueWorldJumpEllipse.WorldMatrix;

			if (use_normal_override && normal_override != null)
			{
				double distance = (endpoint == null) ? 10d : Vector3D.Distance(endpoint.Value, world_node);
				Vector3D vector = jump_ellipse_matrix.Up.Rotate(jump_ellipse_matrix.Forward, normal_override.Value.X).Rotate(jump_ellipse_matrix.Up, normal_override.Value.Y).Rotate(jump_ellipse_matrix.Left, normal_override.Value.Z);
				Vector3D second = ((Vector3D.Angle(vector, jump_ellipse_matrix.Forward) % Math.PI) == 0) ? jump_ellipse_matrix.Left : jump_ellipse_matrix.Forward;
				return MatrixD.CreateWorld(world_node, vector, Vector3D.Cross(vector, second));
			}
			else if (endpoint == null || !use_endpoint) return jump_ellipse_matrix;
			else
			{
				MatrixD control_object_matrix = control_object.WorldMatrix;
				Vector3D forward = (endpoint.Value - world_node).Normalized();
				Vector3D second = ((Vector3D.Angle(forward, control_object_matrix.Up) % Math.PI) == 0) ? control_object_matrix.Left : control_object_matrix.Up;
				return MatrixD.CreateWorld(world_node, forward, Vector3D.Cross(forward, second).Normalized());
			}
		}

		/// <summary>
		/// Gets the world matrix for this gate<br />
		/// Resulting matrix may be affected by the normal override and endpoint
		/// </summary>
		/// <param name="use_endpoint">Whether to calculate the world matrix using this gate's endpoint</param>
		/// <param name="use_normal_override">Whether to calculate the world matrix using this gate's normal override</param>
		/// <param name="normalized">Whether to normalize the world matrix</param>
		/// <returns>This gate's world matrix</returns>
		public void GetWorldMatrix(out MatrixD world_matrix, bool use_endpoint, bool use_normal_override, bool normalized = true)
		{
			if (this.Closed)
			{
				world_matrix = MatrixD.Identity;
				return;
			}

			MyJumpGateControlObject control_object = this.ControlObject;
			Vector3D? endpoint = control_object?.BlockSettings?.SelectedWaypoint()?.GetEndpoint();
			Vector3D? normal_override = control_object?.BlockSettings?.VectorNormalOverride();
			Vector3D world_node = this.WorldJumpNode;
			MatrixD jump_ellipse_matrix = (normalized) ? MatrixD.Normalize(this.TrueWorldJumpEllipse.WorldMatrix) : this.TrueWorldJumpEllipse.WorldMatrix;

			if (use_normal_override && normal_override != null)
			{
				double distance = (endpoint == null) ? 10d : Vector3D.Distance(endpoint.Value, world_node);
				Vector3D vector = jump_ellipse_matrix.Up.Rotate(jump_ellipse_matrix.Forward, normal_override.Value.X).Rotate(jump_ellipse_matrix.Up, normal_override.Value.Y).Rotate(jump_ellipse_matrix.Left, normal_override.Value.Z);
				Vector3D second = ((Vector3D.Angle(vector, jump_ellipse_matrix.Forward) % Math.PI) == 0) ? jump_ellipse_matrix.Left : jump_ellipse_matrix.Forward;
				Vector3D.Cross(ref vector, ref second, out second);
				MatrixD.CreateWorld(ref world_node, ref vector, ref second, out world_matrix);
			}
			else if (endpoint == null || !use_endpoint) world_matrix = jump_ellipse_matrix;
			else
			{
				MatrixD control_object_matrix = control_object.WorldMatrix;
				Vector3D forward = (endpoint.Value - world_node).Normalized();
				Vector3D second = ((Vector3D.Angle(forward, control_object_matrix.Up) % Math.PI) == 0) ? control_object_matrix.Left : control_object_matrix.Up;
				Vector3D.Cross(ref forward, ref second, out second);
				second.Normalize();
				MatrixD.CreateWorld(ref world_node, ref forward, ref second, out world_matrix);
			}
		}

		/// <summary>
		/// Gets this gate's name
		/// </summary>
		/// <returns>The name from the attached controller or the last name if no controller attached</returns>
		public string GetName()
		{
			if (this.Closed) return null;
			string name = this.Controller?.BlockSettings?.JumpGateName() ?? this.RemoteAntenna?.GetJumpGateName(this.RemoteAntennaChannel);
			this.LastStoredName = name ?? this.LastStoredName;
			return name;
		}

		/// <summary>
		/// </summary>
		/// <returns>The printable name for this gate</returns>
		public string GetPrintableName()
		{
			if (this.Closed) return null;
			return this.GetName() ?? this.LastStoredName ?? "�Unnamed�";
		}

		/// <summary>
		/// Gets the entity batch containing the specified entity
		/// </summary>
		/// <param name="entity">The entity who's batch to get</param>
		/// <returns>The entity's batch or null if not batched</returns>
		public EntityBatch GetEntityBatchFromEntity(MyEntity entity)
		{
			if (this.Closed || entity == null) return null;
			return this.EntityBatches.GetValueOrDefault(entity, this.EntityBatches.FirstOrDefault((pair) => pair.Value.IsEntityInBatch(entity)).Value);
		}

		/// <summary>
		/// Gets a list of all drives attached to this gate
		/// </summary>
		/// <returns>An IEnumerable referencing all attached drives</returns>
		public IEnumerable<MyJumpGateDrive> GetJumpGateDrives()
		{
			return this.JumpGateGrid?.GetAttachedJumpGateDrives().Where((drive) => drive.JumpGateID == this.JumpGateID && !drive.IsClosed) ?? Enumerable.Empty<MyJumpGateDrive>();
		}

		/// <summary>
		/// Gets a list of all working drives attached to this gate
		/// </summary>
		/// <returns>An IEnumerable referencing all attached, working drives</returns>
		public IEnumerable<MyJumpGateDrive> GetWorkingJumpGateDrives()
		{
			return this.JumpGateGrid?.GetAttachedJumpGateDrives().Where((drive) => drive.JumpGateID == this.JumpGateID && drive.IsWorking) ?? Enumerable.Empty<MyJumpGateDrive>();
		}
		#endregion
	}
}
