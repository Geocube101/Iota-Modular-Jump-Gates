using IOTA.ModularJumpGates.Terminal;
using IOTA.ModularJumpGates.Util;
using IOTA.ModularJumpGates.Util.ConcurrentCollections;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace IOTA.ModularJumpGates.CubeBlock
{
	public enum MyAllowedRemoteSettings : ushort {
		NAME = 1, DESTINATIONS = 2, ANIMATIONS = 4, ROUTING = 8, ENTITY_FILTER = 16, AUTO_ACTIVATE = 32, COMM_LINKAGE = 64, JUMPSPACE = 128, COLOR_OVERRIDE = 256, VECTOR_OVERRIDE = 512, JUMP = 1024, NOJUMP = 2048,
		ALL = 4095, NONE = 0,
	}

	/// <summary>
	/// Game logic for jump gate remote antennas
	/// </summary>
	[MyEntityComponentDescriptor(typeof(MyObjectBuilder_UpgradeModule), false, "IOTA.JumpGate.JumpGateRemoteAntenna.Large", "IOTA.JumpGate.JumpGateRemoteAntenna.Small")]
	internal class MyJumpGateRemoteAntenna : MyCubeBlockBase
	{
		/// <summary>
		/// Stores block data for this block type
		/// </summary>
		[ProtoContract]
		public sealed class MyRemoteAntennaBlockSettingsStruct
		{
			[ProtoMember(1)]
			public long[] InboundConnectionJumpGates;
			[ProtoMember(2)]
			public long[] OutboundConnectionControllers;
			[ProtoMember(3)]
			public MyAllowedRemoteSettings AllowedRemoteSettings = MyAllowedRemoteSettings.ALL;
			[ProtoMember(4)]
			public double AntennaRange = MyJumpGateRemoteAntenna.MaxCollisionDistance;

			public MyRemoteAntennaBlockSettingsStruct() { }

			public void AllowSetting(MyAllowedRemoteSettings setting, bool flag)
			{
				if (flag) this.AllowedRemoteSettings |= setting;
				else this.AllowedRemoteSettings &= ~setting;
			}
		}

		#region Public Static Variables
		/// <summary>
		/// The number of available antenna channels
		/// </summary>
		public const byte ChannelCount = 3;

		/// <summary>
		/// The maximum distance (in meters) to scan for nearby antennas
		/// </summary>
		public const double MaxCollisionDistance = 1500;
		#endregion

		#region Private Variables
		/// <summary>
		/// The current channel being shown on emissives
		/// </summary>
		private byte ConnectionEmissiveIndex = 0;

		/// <summary>
		/// The list of jump gates listening from this antenna
		/// </summary>
		private readonly long[] InboundConnectionJumpGates = new long[MyJumpGateRemoteAntenna.ChannelCount] { -1, -1, -1 };

		/// <summary>
		/// The list of controllers listening to this antenna
		/// </summary>
		private readonly long[] OutboundConnectionControllers = new long[MyJumpGateRemoteAntenna.ChannelCount] { -1, -1, -1 };

		/// <summary>
		/// The list of active antenna connections
		/// </summary>
		private readonly MyJumpGateRemoteAntenna[] ConnectionThreads = new MyJumpGateRemoteAntenna[MyJumpGateRemoteAntenna.ChannelCount] { null, null, null };

		/// <summary>
		/// The collision detector for finding nearby antennas
		/// </summary>
		private MyEntity AntennaDetector = null;

		/// <summary>
		/// Queue of pairings that cannot be completed
		/// </summary>
		private KeyValuePair<JumpGateUUID, JumpGateUUID>?[] UninitializedPairings = new KeyValuePair<JumpGateUUID, JumpGateUUID>?[3] { null, null, null };

		/// <summary>
		/// A collection of all topmost entities within search range
		/// </summary>
		private ConcurrentLinkedHashSet<long> NearbyEntities = new ConcurrentLinkedHashSet<long>();
		#endregion

		#region Public Variables
		/// <summary>
		/// The block data for this block
		/// </summary>
		public MyRemoteAntennaBlockSettingsStruct BlockSettings { get; private set; } = new MyRemoteAntennaBlockSettingsStruct();

		/// <summary>
		/// Callbacks for when an antenna entered range<br />
		/// Callback takes the form "Action(this_antenna, other_antenna, is_entering)"
		/// </summary>
		public event Action<MyJumpGateRemoteAntenna, MyJumpGateRemoteAntenna, bool> OnAntennaEnteredRange;

		/// <summary>
		/// Callbacks for when an antenna connection is established<br />
		/// Callback takes the form "Action(this_antenna, connected_controller, connected_gate, channel, connection_formed)"
		/// </summary>
		public event Action<MyJumpGateRemoteAntenna, MyJumpGateController, MyJumpGate, byte, bool> OnAntennaConnection;
		#endregion

		#region Constructors
		public MyJumpGateRemoteAntenna() : base() { }

		/// <summary>
		/// Creates a new null wrapper
		/// </summary>
		/// <param name="serialized">The serialized block data</param>
		/// <param name="parent">The containing grid or null to calculate</param>
		public MyJumpGateRemoteAntenna(MySerializedJumpGateRemoteAntenna serialized, MyJumpGateConstruct parent = null) : base(serialized, parent)
		{
			this.FromSerialized(serialized, parent);
		}
		#endregion

		#region "CubeBlockBase" Methods
		protected override void Clean()
		{
			base.Clean();
			this.NearbyEntities.Clear();
			this.NearbyEntities = null;
		}

		protected override void UpdateOnceAfterInit()
		{
			base.UpdateOnceAfterInit();
			if (!MyJumpGateRemoteAntennaTerminal.IsLoaded) MyJumpGateRemoteAntennaTerminal.Load(this.ModContext);
			if (MyNetworkInterface.IsStandaloneMultiplayerClient) return;
			this.AntennaDetector = new MyEntity();
			this.AntennaDetector.EntityId = 0;
			this.AntennaDetector.Init(new StringBuilder($"JumpGateRemoteAntenna_{this.BlockID}"), null, (MyEntity) this.Entity, 1f);
			this.AntennaDetector.Save = false;
			this.AntennaDetector.Flags = EntityFlags.IsNotGamePrunningStructureObject | EntityFlags.NeedsWorldMatrix;
			PhysicsSettings settings = MyAPIGateway.Physics.CreateSettingsForDetector(this.AntennaDetector, this.OnEntityCollision, MyJumpGateModSession.WorldMatrix, Vector3D.Zero, RigidBodyFlag.RBF_KINEMATIC, 15, true);
			MyAPIGateway.Physics.CreateSpherePhysics(settings, (float) MyJumpGateRemoteAntenna.MaxCollisionDistance);
			MyAPIGateway.Entities.AddEntity(this.AntennaDetector);
			Logger.Debug($"CREATED_COLLIDER REMOTE_ANTENNA={this.BlockID}, COLLIDER={this.AntennaDetector.DisplayName}", 4);

			BoundingSphereD bounding_sphere = new BoundingSphereD(this.WorldMatrix.Translation, MyJumpGateRemoteAntenna.MaxCollisionDistance);
			List<MyEntity> entities = new List<MyEntity>();
			MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref bounding_sphere, entities);
			this.NearbyEntities.Clear();
			foreach (MyEntity entity in entities) this.OnEntityCollision(entity, true);
			Logger.Critical($"[{this.BlockID}]: INIT_GRIDS={this.NearbyEntities?.Select(MyJumpGateModSession.Instance.GetJumpGateGrid).Where((grid) => grid != this.JumpGateGrid && MyJumpGateModSession.Instance.IsJumpGateGridMultiplayerValid(grid)).Distinct().Count()}");

			for (byte i = 0; i < MyJumpGateRemoteAntenna.ChannelCount; ++i)
			{
				long gateid = this.InboundConnectionJumpGates[i];
				if (gateid == -1) continue;
				MyJumpGate jump_gate = this.JumpGateGrid.GetJumpGate(gateid);
				if (jump_gate != null && jump_gate.RemoteAntenna == null)
				{
					jump_gate.RemoteAntenna = this;
					jump_gate.SetDirty();
				}
				else if (jump_gate != null) this.InboundConnectionJumpGates[i] = -1;
			}

			this.SetDirty();
		}

		/// <summary>
		/// CubeBlockBase Method<br />
		/// Appends info to the detailed info screen
		/// </summary>
		/// <param name="info">The string builder to append to</param>
		protected override void AppendCustomInfo(StringBuilder info)
		{
			if (this.ResourceSink != null)
			{
				double input_wattage = this.ResourceSink.CurrentInputByType(MyResourceDistributorComponent.ElectricityId);
				double max_input = this.ResourceSink.RequiredInputByType(MyResourceDistributorComponent.ElectricityId);
				double input_ratio = input_wattage / max_input;

				info.Append($"\n-=-=( {MyTexts.GetString("DisplayName_CubeBlock_JumpGateRemoteAntenna")} )=-=-\n");
				info.Append($" {MyTexts.GetString("DetailedInfo_BlockBase_Input")}: {MyJumpGateModSession.AutoconvertMetricUnits(input_wattage * 1e6, "W/s", 4)}\n");
				info.Append($" {MyTexts.GetString("DetailedInfo_BlockBase_RequiredInput")}: {MyJumpGateModSession.AutoconvertMetricUnits(max_input * 1e6, "W/s", 4)}\n");
				info.Append($" {MyTexts.GetString("DetailedInfo_BlockBase_InputRatio")}: {((double.IsNaN(input_ratio)) ? "N/A" : $"{Math.Round(MathHelperD.Clamp(input_ratio, 0, 1) * 100, 2):#.00}%")}\n");
				info.Append($" {MyTexts.GetString("DetailedInfo_JumpGateRemoteAntenna_NearbyAntennas")}: {this.GetNearbyAntennas().Count()}\n");

				for (byte i = 0; i < MyJumpGateRemoteAntenna.ChannelCount; ++i)
				{
					MyJumpGateRemoteAntenna antenna = this.ConnectionThreads[i];
					MyJumpGateController outbound_controller = this.GetOutboundControlController(i);
					MyJumpGateController inbound_controller = antenna?.GetOutboundControlController(i);
					MyJumpGate inbound_controllee = antenna?.GetInboundControlGate(i);
					MyJumpGate outbound_controllee = this.GetInboundControlGate(i);
					info.Append($"\n {MyTexts.GetString("DetailedInfo_JumpGateRemoteAntenna_Channel")} {i}:\n");
					info.Append($" ... {MyTexts.GetString("DetailedInfo_JumpGateRemoteAntenna_IsConnected")}: {antenna != null}\n");
					info.Append($" ... {MyTexts.GetString("DetailedInfo_JumpGateRemoteAntenna_HasOutboundController")}: {outbound_controller != null}\n");
					info.Append($" ... {MyTexts.GetString("DetailedInfo_JumpGateRemoteAntenna_JumpGateConnected")}: {inbound_controllee != null || outbound_controllee != null}\n");
					info.Append($" ... {MyTexts.GetString("DetailedInfo_JumpGateRemoteAntenna_HasInboundController")}: {inbound_controller != null}\n");
				}
			}
		}

		/// <summary>
		/// CubeBlockBase Method<br />
		/// Initializes the block's game logic
		/// </summary>
		/// <param name="object_builder">This entity's object builder</param>
		public override void Init(MyObjectBuilder_EntityBase object_builder)
		{
			this.Init(object_builder, 0, () => (this.TerminalBlock.IsWorking) ? (float) (0.1 + this.ConnectionThreads.Count((other) => other != null) * 0.05) : 0, MyJumpGateModSession.BlockComponentDataGUID, false);
			this.TerminalBlock.Synchronized = true;
			if (MyJumpGateModSession.Network.Registered) MyJumpGateModSession.Network.On(MyPacketTypeEnum.UPDATE_REMOTE_ANTENNA, this.OnNetworkBlockUpdate);
			string blockdata;
			this.ResourceSink.SetMaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId, 250f);

			if (this.ModStorageComponent.TryGetValue(MyJumpGateModSession.BlockComponentDataGUID, out blockdata) && blockdata.Length > 0)
			{
				try
				{
					MyRemoteAntennaBlockSettingsStruct serialized = MyAPIGateway.Utilities.SerializeFromBinary<MyRemoteAntennaBlockSettingsStruct>(Convert.FromBase64String(blockdata));
					for (byte i = 0; i < this.InboundConnectionJumpGates.Length; ++i) this.InboundConnectionJumpGates[i] = serialized.InboundConnectionJumpGates[i];
					for (byte i = 0; i < this.OutboundConnectionControllers.Length; ++i) this.OutboundConnectionControllers[i] = serialized.OutboundConnectionControllers[i];
					this.BlockSettings = serialized;
				}
				catch (Exception e)
				{
					Logger.Error($"Failed to load block data: {this.GetType().Name}-{this.TerminalBlock.CustomName}\n{e}");
				}
			}
			else
			{
				this.ModStorageComponent.Add(MyJumpGateModSession.BlockComponentDataGUID, "");
			}

			if (MyJumpGateModSession.Network.Registered && MyNetworkInterface.IsStandaloneMultiplayerClient)
			{
				MyNetworkInterface.Packet request = new MyNetworkInterface.Packet {
					TargetID = 0,
					Broadcast = false,
					PacketType = MyPacketTypeEnum.UPDATE_REMOTE_ANTENNA,
				};
				request.Payload<MySerializedJumpGateCapacitor>(null);
				request.Send();
			}
		}

		/// <summary>
		/// CubeBlockBase Method<br />
		/// Called once every tick after simulation<br />
		/// Updates power usage and emissives
		/// </summary>
		public override void UpdateAfterSimulation()
		{
			base.UpdateAfterSimulation();

			// Update Emmissives
			bool is_working;
			if (this.TerminalBlock?.CubeGrid?.Physics == null || this.TerminalBlock.MarkedForClose) return;
			else if (is_working = this.IsWorking) this.TerminalBlock.SetEmissiveParts("Emissive0", Color.Lime, 1);
			else if (this.TerminalBlock.IsFunctional) this.TerminalBlock.SetEmissiveParts("Emissive0", Color.Red, 1);
			else this.TerminalBlock.SetEmissiveParts("Emissive0", Color.Black, 1);

			// Update gate connections and nearby antennas
			if (MyNetworkInterface.IsServerLike && this.IsInitFrameCalled)
			{
				for (byte i = 0; i < MyJumpGateRemoteAntenna.ChannelCount; ++i)
				{
					MyJumpGateController outbound_controller = this.GetOutboundControlController(i);
					MyJumpGate inbound_jump_gate = this.GetInboundControlGate(i);

					if (outbound_controller != null && (outbound_controller.RemoteAntenna != this || outbound_controller.JumpGateGrid != this.JumpGateGrid))
					{
						outbound_controller.AttachedRemoteJumpGate(null);
						outbound_controller.SetDirty();
						this.OutboundConnectionControllers[i] = -1;
						continue;
					}

					if (inbound_jump_gate != null && (inbound_jump_gate.RemoteAntenna != this || inbound_jump_gate.JumpGateGrid != this.JumpGateGrid))
					{
						this.InboundConnectionJumpGates[i] = -1;
						continue;
					}

					MyJumpGateRemoteAntenna antenna = this.ConnectionThreads[i];
					if (antenna == null || outbound_controller == null) continue;
					MyJumpGate connected_gate = this.GetConnectedControlledJumpGate(i);
					MyJumpGate attached_gate = outbound_controller.AttachedJumpGate();
					double distance = Math.Min(this.BlockSettings.AntennaRange, antenna.BlockSettings.AntennaRange);
					if (connected_gate == attached_gate && connected_gate != null && !connected_gate.Closed && is_working && antenna.IsWorking && Vector3D.DistanceSquared(this.WorldMatrix.Translation, antenna.WorldMatrix.Translation) <= distance * distance) continue;
					this.ConnectionThreads[i] = null;
					antenna.ConnectionThreads[i] = null;
					outbound_controller?.AttachedRemoteJumpGate(null);
					if (connected_gate != null) connected_gate.Controller = null;
					if (attached_gate != null) attached_gate.Controller = null;
					outbound_controller?.SetDirty();
					connected_gate?.SetDirty();
					attached_gate?.SetDirty();
					this.SetDirty();
					antenna.SetDirty();
					this.OnAntennaConnection?.Invoke(this, outbound_controller, inbound_jump_gate, i, false);
				}

				if (is_working)
				{
					foreach (MyJumpGateRemoteAntenna antenna in this.GetNearbyAntennas().OrderBy((antenna) => Vector3D.DistanceSquared(antenna.WorldMatrix.Translation, this.WorldMatrix.Translation)))
					{
						for (byte i = 0; i < MyJumpGateRemoteAntenna.ChannelCount; ++i)
						{
							MyJumpGateController outbound_controller = this.GetOutboundControlController(i);
							MyJumpGate inbound_controllee = antenna.GetInboundControlGate(i);
							if (outbound_controller == null || outbound_controller.Closed || inbound_controllee == null || inbound_controllee.Closed || this.ConnectionThreads[i] != null || antenna.ConnectionThreads[i] != null || inbound_controllee.Controller != null && !inbound_controllee.Closed) continue;
							this.ConnectionThreads[i] = antenna;
							antenna.ConnectionThreads[i] = this;
							this.SetDirty();
							antenna.SetDirty();
							outbound_controller.AttachedRemoteJumpGate(inbound_controllee);
							outbound_controller.SetDirty();
							inbound_controllee.SetDirty();
							this.OnAntennaConnection?.Invoke(this, outbound_controller, inbound_controllee, i, true);
						}
					}
				}
			}
			else if (MyNetworkInterface.IsStandaloneMultiplayerClient)
			{
				for (byte i = 0; i < MyJumpGateRemoteAntenna.ChannelCount; ++i)
				{
					KeyValuePair<JumpGateUUID, JumpGateUUID>? null_pair = this.UninitializedPairings[i];
					if (null_pair == null) continue;
					KeyValuePair<JumpGateUUID, JumpGateUUID> pair = null_pair.Value;
					if (pair.Key == JumpGateUUID.Empty || pair.Value == JumpGateUUID.Empty) continue;
					MyJumpGateController controller = this.JumpGateGrid.GetController(pair.Key.GetBlock());
					MyJumpGate remote_gate = MyJumpGateModSession.Instance.GetJumpGate(pair.Value);

					if (controller == null || remote_gate == null)
					{
						this.UninitializedPairings[i] = pair;
						continue;
					}

					controller.AttachedRemoteJumpGate(remote_gate);
					remote_gate.RemoteAntenna = this.ConnectionThreads[i];
					this.UninitializedPairings[i] = null;
				}
			}

			// Update connection emissives
			ulong tick_offset = this.LocalGameTick % 30;
			this.ConnectionEmissiveIndex = (tick_offset == 0) ? (byte) ((this.ConnectionEmissiveIndex + 1) % MyJumpGateRemoteAntenna.ChannelCount) : this.ConnectionEmissiveIndex;
			byte connections = (byte) this.ConnectionThreads.Count((other) => other != null);
			if (connections == 0) this.TerminalBlock.SetEmissiveParts("Emissive1", Color.Red, 1);
			else if (connections == MyJumpGateRemoteAntenna.ChannelCount) this.TerminalBlock.SetEmissiveParts("Emissive1", Color.Lime, 1);
			else if (tick_offset >= 25) this.TerminalBlock.SetEmissiveParts("Emissive1", Color.Black, 1);
			else this.TerminalBlock.SetEmissiveParts("Emissive1", (this.ConnectionThreads[this.ConnectionEmissiveIndex] != null) ? Color.Blue : Color.Gold, 1);

			this.CheckSendGlobalUpdate();
		}

		/// <summary>
		/// CubeBlockBase Method<br />
		/// Unregisters events and marks block for close
		/// </summary>
		public override void MarkForClose()
		{
			base.MarkForClose();
			if (MyJumpGateModSession.Network.Registered) MyJumpGateModSession.Network.Off(MyPacketTypeEnum.UPDATE_REMOTE_ANTENNA, this.OnNetworkBlockUpdate);
			if (this.AntennaDetector == null) return;
			MyAPIGateway.Entities.RemoveEntity(this.AntennaDetector);
			this.AntennaDetector = null;
			this.NearbyEntities.Clear();
		}

		/// <summary>
		/// Serializes block for streaming and saving
		/// </summary>
		/// <returns>false</returns>
		public override bool IsSerialized()
		{
			bool res = base.IsSerialized();
			this.BlockSettings.InboundConnectionJumpGates = this.InboundConnectionJumpGates;
			this.BlockSettings.OutboundConnectionControllers = this.OutboundConnectionControllers;
			this.ModStorageComponent[MyJumpGateModSession.BlockComponentDataGUID] = Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(this.BlockSettings));
			return res;
		}
		#endregion

		#region Private Methods
		/// <summary>
		/// Callback triggered when this component recevies a block update request from server
		/// </summary>
		/// <param name="packet">The update request packet</param>
		private void OnNetworkBlockUpdate(MyNetworkInterface.Packet packet)
		{
			if (packet == null || packet.EpochTime <= this.LastUpdateTime) return;
			MySerializedJumpGateRemoteAntenna serialized = packet.Payload<MySerializedJumpGateRemoteAntenna>();
			if (serialized == null || serialized.UUID.GetBlock() != this.BlockID) return;

			if (MyNetworkInterface.IsMultiplayerServer && packet.PhaseFrame == 1)
			{
				if (serialized.IsClientRequest)
				{
					packet.Payload(this.ToSerialized(false));
					packet.Send();
				}
				else if (this.LastUpdateDateTimeUTC < packet.EpochDateTimeUTC && this.FromSerialized(serialized))
				{
					this.LastUpdateTime = packet.EpochTime;
					this.IsDirty = false;
					packet.Forward(0, true).Send();
				}
			}
			else if (MyNetworkInterface.IsStandaloneMultiplayerClient && (packet.PhaseFrame == 1 || packet.PhaseFrame == 2) && this.FromSerialized(serialized))
			{
				this.LastUpdateTime = packet.EpochTime;
				this.IsDirty = false;
			}
		}

		/// <summary>
		/// Server only<br />
		/// Checks if this block should be updated and if true -<br />
		/// sends an update request for this block to all clients
		/// </summary>
		private void CheckSendGlobalUpdate()
		{
			if (MyJumpGateModSession.Network.Registered && ((MyNetworkInterface.IsMultiplayerServer && MyJumpGateModSession.GameTick % MyCubeBlockBase.ForceUpdateDelay == 0) || this.IsDirty))
			{
				MyNetworkInterface.Packet update_packet = new MyNetworkInterface.Packet {
					PacketType = MyPacketTypeEnum.UPDATE_REMOTE_ANTENNA,
					TargetID = 0,
					Broadcast = MyNetworkInterface.IsMultiplayerServer,
				};

				update_packet.Payload(this.ToSerialized(false));
				update_packet.Send();
				if (MyNetworkInterface.IsMultiplayerServer) this.LastUpdateTime = update_packet.EpochTime;
				this.IsDirty = false;
			}
		}

		private void OnEntityCollision(IMyEntity entity, bool is_entering)
		{
			if (entity == null || entity.Physics == null || !(entity is MyCubeGrid) || this.MarkedForClose) return;
			else if (is_entering) this.NearbyEntities?.Add(entity.EntityId);
			else this.NearbyEntities?.Remove(entity.EntityId);
			this.SetDirty();
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Sets the channel to a specified jump gate or clears the channel if the gate is null or closed
		/// </summary>
		/// <param name="channel">The channel or 255 for next available</param>
		/// <param name="gate">The gate to bind</param>
		public void SetGateForInboundControl(byte channel, MyJumpGate gate)
		{
			if (channel >= MyJumpGateRemoteAntenna.ChannelCount && (gate == null || channel != 0xFF) || (gate != null && gate.JumpGateGrid != this.JumpGateGrid)) return;
			else if (channel == 0xFF && this.InboundConnectionJumpGates.Contains(gate.JumpGateID)) channel = (byte) Array.IndexOf(this.InboundConnectionJumpGates, gate.JumpGateID);
			else if (channel == 0xFF)
			{
				for (byte i = 0; i < MyJumpGateRemoteAntenna.ChannelCount; ++i)
				{
					if (this.InboundConnectionJumpGates[i] == -1)
					{
						channel = i;
						break;
					}
				}

				if (channel == 0xFF) return;
			}

			if (gate != null && this.InboundConnectionJumpGates.Contains(gate.JumpGateID)) this.SetGateForInboundControl((byte) Array.IndexOf(this.InboundConnectionJumpGates, gate.JumpGateID), null);
			MyJumpGate old_gate = this.GetInboundControlGate(channel);
			this.InboundConnectionJumpGates[channel] = (gate == null || gate.Closed) ? -1 : gate.JumpGateID;
			if (gate != null) gate.RemoteAntenna = this;
			if (old_gate == null || old_gate == gate) return;
			old_gate.Controller?.AttachedRemoteJumpGate(null);
			old_gate.Controller = null;
			old_gate.RemoteAntenna = null;
		}

		/// <summary>
		/// Sets the channel to a specified jump gate controller or clears the channel if the controller is null or closed
		/// </summary>
		/// <param name="channel">The channel or 255 for next available</param>
		/// <param name="controller">The controller to bind</param>
		public void SetControllerForOutboundControl(byte channel, MyJumpGateController controller)
		{
			if (channel >= MyJumpGateRemoteAntenna.ChannelCount && (channel != 0xFF || controller == null) || (controller != null && controller.JumpGateGrid != this.JumpGateGrid)) return;
			else if (channel == 0xFF && this.OutboundConnectionControllers.Contains(controller.BlockID)) channel = (byte) Array.IndexOf(this.OutboundConnectionControllers, controller.BlockID);
			else if (channel == 0xFF)
			{
				for (byte i = 0; i < MyJumpGateRemoteAntenna.ChannelCount; ++i)
				{
					if (this.OutboundConnectionControllers[i] == -1)
					{
						channel = i;
						break;
					}
				}

				if (channel == 0xFF) return;
			}

			if (controller != null && this.OutboundConnectionControllers.Contains(controller.BlockID)) this.SetControllerForOutboundControl((byte) Array.IndexOf(this.OutboundConnectionControllers, controller.BlockID), null);
			MyJumpGateController old_controller = this.GetOutboundControlController(channel);
			if (old_controller == controller) return;
			old_controller?.AttachedRemoteJumpGate(null);
			this.OutboundConnectionControllers[channel] = (controller == null) ? -1 : controller.BlockID;
			controller?.BlockSettings.RemoteAntennaChannel(channel);
			controller?.BlockSettings.RemoteAntennaID(this.BlockID);
		}

		public bool FromSerialized(MySerializedJumpGateRemoteAntenna antenna, MyJumpGateConstruct parent = null)
		{
			if (!base.FromSerialized(antenna, parent)) return false;
			else if (this.JumpGateGrid == null) return true;
			
			if (antenna.InboundControlJumpGates != null)
			{
				for (byte i = 0; i < this.InboundConnectionJumpGates.Length; ++i)
				{
					MyJumpGate jump_gate = this.JumpGateGrid.GetJumpGate(antenna.InboundControlJumpGates[i]);
					this.SetGateForInboundControl(i, jump_gate);
				}
			}
			
			if (antenna.OutboundControlControllers != null)
			{
				for (byte i = 0; i < this.OutboundConnectionControllers.Length; ++i)
				{
					MyJumpGateController controller = this.JumpGateGrid.GetController(antenna.OutboundControlControllers[i]);
					this.SetControllerForOutboundControl(i, controller);
				}
			}
			
			if (MyNetworkInterface.IsStandaloneMultiplayerClient)
			{
				if (antenna.RemoteConnectionThreads != null)
				{
					for (byte i = 0; i < this.ConnectionThreads.Length; ++i)
					{
						long blockid = antenna.RemoteConnectionThreads[i];
						if (blockid == -1) continue;
						IMyEntity entity = MyAPIGateway.Entities.GetEntityById(blockid);
						if (entity == null || !(entity is IMyTerminalBlock)) continue;
						this.ConnectionThreads[i] = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna((IMyTerminalBlock) entity);
					}
				}
				
				this.NearbyEntities.Clear();
				if (antenna.NearbyEntities != null) this.NearbyEntities.AddRange(antenna.NearbyEntities);
				
				if (antenna.ControllerGatePairings != null)
				{
					for (byte i = 0; i < MyJumpGateRemoteAntenna.ChannelCount; ++i)
					{
						KeyValuePair<JumpGateUUID, JumpGateUUID> pair = antenna.ControllerGatePairings[i];
						if (pair.Key == JumpGateUUID.Empty ||  pair.Value == JumpGateUUID.Empty) continue;
						MyJumpGateController controller = this.JumpGateGrid.GetController(pair.Key.GetBlock());
						MyJumpGate remote_gate = MyJumpGateModSession.Instance.GetJumpGate(pair.Value);

						if (controller == null || remote_gate == null)
						{
							this.UninitializedPairings[i] = pair;
							continue;
						}

						controller.AttachedRemoteJumpGate(remote_gate);
						remote_gate.RemoteAntenna = this.ConnectionThreads[i];
					}
				}
			}
			
			this.BlockSettings = MyAPIGateway.Utilities.SerializeFromBinary<MyRemoteAntennaBlockSettingsStruct>(antenna.BlockSettings);
			return true;
		}

		/// <summary>
		/// Gets the control channel this jump gate is bound to
		/// </summary>
		/// <param name="gate">The gate to check</param>
		/// <returns>The control channel or 255 if not registered</returns>
		public byte GetJumpGateInboundControlChannel(MyJumpGate gate)
		{
			return (byte) ((gate != null && gate.JumpGateGrid == this.JumpGateGrid && this.InboundConnectionJumpGates.Contains(gate.JumpGateID)) ? Array.IndexOf(this.InboundConnectionJumpGates, gate.JumpGateID) : 0xFF);
		}

		/// <summary>
		/// Gets the control channel this jump gate controller is bound to
		/// </summary>
		/// <param name="controller">The controller to check</param>
		/// <returns>The control channel or 255 if not registered</returns>
		public byte GetControllerOutboundControlChannel(MyJumpGateController controller)
		{
			return (byte) ((controller != null && controller.JumpGateGrid == this.JumpGateGrid && this.OutboundConnectionControllers.Contains(controller.BlockID)) ? Array.IndexOf(this.OutboundConnectionControllers, controller.BlockID) : 0xFF);
		}

		/// <summary>
		/// </summary>
		/// <param name="channel">The channel to check</param>
		/// <returns>The gate listening on the specified inbound channel or null</returns>
		public MyJumpGate GetInboundControlGate(byte channel)
		{
			return (channel > this.InboundConnectionJumpGates.Length) ? null : this.JumpGateGrid?.GetJumpGate(this.InboundConnectionJumpGates[channel]);
		}

		/// <summary>
		/// Gets the jump gate on the other end of this antenna's outbound connection or null
		/// </summary>
		/// <param name="channel">The channel to check</param>
		/// <returns>The connected gate</returns>
		public MyJumpGate GetConnectedControlledJumpGate(byte channel)
		{
			if (channel >= this.ConnectionThreads.Length) return null;
			MyJumpGateRemoteAntenna antenna = this.ConnectionThreads[channel];
			return antenna?.GetInboundControlGate(channel);
		}

		/// <summary>
		/// </summary>
		/// <param name="channel">The channel to check</param>
		/// <returns>The controller listening on the specified outbound channel or null</returns>
		public MyJumpGateController GetOutboundControlController(byte channel)
		{
			return (channel > this.OutboundConnectionControllers.Length) ? null : this.JumpGateGrid?.GetController(this.OutboundConnectionControllers[channel]);
		}

		/// <summary>
		/// Gets the antenna on the other end of this connection for the specified channel
		/// </summary>
		/// <param name="channel">The channel to check</param>
		/// <returns>The connected antenna or null</returns>
		public MyJumpGateRemoteAntenna GetConnectedRemoteAntenna(byte channel)
		{
			return (channel < MyJumpGateRemoteAntenna.ChannelCount) ? this.ConnectionThreads[channel] : null;
		}

		/// <summary>
		/// </summary>
		/// <returns>Gets the IDs of all inbound registered jump gates</returns>
		public IEnumerable<long> RegisteredInboundControlGateIDs()
		{
			return this.InboundConnectionJumpGates?.AsEnumerable() ?? Enumerable.Empty<long>();
		}

		/// <summary>
		/// </summary>
		/// <returns>Gets the IDs of all outbound registered jump gate controllers</returns>
		public IEnumerable<long> RegisteredOutboundControlControllerIDs()
		{
			return this.OutboundConnectionControllers?.AsEnumerable() ?? Enumerable.Empty<long>();
		}

		/// <summary>
		/// </summary>
		/// <returns>Gets all inbound registered jump gates</returns>
		public IEnumerable<MyJumpGate> RegisteredInboundControlGates()
		{
			return this.InboundConnectionJumpGates?.Select((id) => this.JumpGateGrid?.GetJumpGate(id)).Where((gate) => gate != null) ?? Enumerable.Empty<MyJumpGate>();
		}

		/// <summary>
		/// </summary>
		/// <returns>Gets all inbound registered jump gate controllers</returns>
		public IEnumerable<MyJumpGateController> RegisteredOutboundControlControllers()
		{
			return this.OutboundConnectionControllers?.Select((id) => this.JumpGateGrid?.GetController(id)).Where((controller) => controller != null) ?? Enumerable.Empty<MyJumpGateController>();
		}

		/// <summary>
		/// Gets all working antennas within range of this one
		/// </summary>
		/// <returns>An enumerable containing all nearby working antennas</returns>
		public IEnumerable<MyJumpGateRemoteAntenna> GetNearbyAntennas()
		{
			return this.NearbyEntities?.Select(MyJumpGateModSession.Instance.GetJumpGateGrid).Where((grid) => grid != this.JumpGateGrid && MyJumpGateModSession.Instance.IsJumpGateGridMultiplayerValid(grid)).Distinct().SelectMany((grid) => grid.GetAttachedJumpGateRemoteAntennas()).Where((antenna) => {
				double distance = Math.Min(this.BlockSettings.AntennaRange, antenna.BlockSettings.AntennaRange);
				return antenna.IsWorking && Vector3D.DistanceSquared(antenna.WorldMatrix.Translation, this.WorldMatrix.Translation) <= distance * distance;
			}) ?? Enumerable.Empty<MyJumpGateRemoteAntenna>();
		}

		public new MySerializedJumpGateRemoteAntenna ToSerialized(bool as_client_request)
		{
			if (as_client_request) return base.ToSerialized<MySerializedJumpGateRemoteAntenna>(true);
			KeyValuePair<JumpGateUUID, JumpGateUUID>[] pairings = new KeyValuePair<JumpGateUUID, JumpGateUUID>[MyJumpGateRemoteAntenna.ChannelCount];

			for (byte i = 0; i < MyJumpGateRemoteAntenna.ChannelCount; ++i)
			{
				MyJumpGateController outbound_controller = this.JumpGateGrid.GetController(this.OutboundConnectionControllers[i]);
				MyJumpGate inbound_controlee = this.GetConnectedControlledJumpGate(i);
				if (outbound_controller == null || inbound_controlee == null || outbound_controller.AttachedJumpGate() != inbound_controlee || inbound_controlee.Controller != outbound_controller) pairings[i] = new KeyValuePair<JumpGateUUID, JumpGateUUID>(JumpGateUUID.Empty, JumpGateUUID.Empty);
				else pairings[i] = new KeyValuePair<JumpGateUUID, JumpGateUUID>(JumpGateUUID.FromBlock(outbound_controller), JumpGateUUID.FromJumpGate(inbound_controlee));
			}

			MySerializedJumpGateRemoteAntenna serialized = base.ToSerialized<MySerializedJumpGateRemoteAntenna>(false);
			serialized.InboundControlJumpGates = this.InboundConnectionJumpGates;
			serialized.OutboundControlControllers = this.OutboundConnectionControllers;
			serialized.RemoteConnectionThreads = this.ConnectionThreads.Select((antenna) => antenna?.BlockID ?? -1).ToArray();
			serialized.NearbyEntities = this.NearbyEntities.ToList();
			serialized.BlockSettings = MyAPIGateway.Utilities.SerializeToBinary(this.BlockSettings);
			serialized.ControllerGatePairings = pairings;
			return serialized;
		}
		#endregion
	}

	[ProtoContract]
	internal class MySerializedJumpGateRemoteAntenna : MySerializedCubeBlockBase
	{
		/// <summary>
		/// The connected jump gates
		/// </summary>
		[ProtoMember(20)]
		public long[] InboundControlJumpGates;
		[ProtoMember(21)]
		public long[] OutboundControlControllers;
		[ProtoMember(22)]
		public long[] RemoteConnectionThreads;
		[ProtoMember(23)]
		public List<long> NearbyEntities;
		[ProtoMember(24)]
		public byte[] BlockSettings;
		[ProtoMember(25)]
		public KeyValuePair<JumpGateUUID, JumpGateUUID>[] ControllerGatePairings;
	}
}
