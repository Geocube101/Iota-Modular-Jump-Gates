using IOTA.ModularJumpGates.Terminal;
using IOTA.ModularJumpGates.Util;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace IOTA.ModularJumpGates.CubeBlock
{
	/// <summary>
	/// Game logic for jump gate server antennas
	/// </summary>
	[MyEntityComponentDescriptor(typeof(MyObjectBuilder_UpgradeModule), false, "IOTA.JumpGate.JumpGateRemoteLink.Large", "IOTA.JumpGate.JumpGateRemoteLink.Small")]
	internal class MyJumpGateRemoteLink : MyCubeBlockBase
	{
		/// <summary>
		/// Stores block data for this block type
		/// </summary>
		[ProtoContract]
		public sealed class MyRemoteLinkBlockSettingsStruct
		{
			[ProtoMember(1)]
			public bool DisplayConnectionEffect = true;

			[ProtoMember(2)]
			public bool IsPhysicallyConnected = false;

			[ProtoMember(3)]
			public MyFactionDisplayType AllowedFactionConnections = MyFactionDisplayType.OWNED;

			[ProtoMember(4)]
			public ushort ChannelID = 0;

			[ProtoMember(5)]
			public long AttachedRemoteLink = 0;

			[ProtoMember(6)]
			public Color ConnectionEffectColor = new Color(100, 255, 255, 255);

			public void AllowFactionConnection(MyFactionDisplayType setting, bool flag)
			{
				if (flag) this.AllowedFactionConnections |= setting;
				else this.AllowedFactionConnections &= ~setting;
			}

			public bool IsFactionConnectionAllowed(MyFactionDisplayType setting)
			{
				return (this.AllowedFactionConnections & setting) != 0;
			}
		}

		[ProtoContract]
		public sealed class MyLinkConnectionInfo
		{
			[ProtoMember(1)]
			public long ParentTerminalID;

			[ProtoMember(2)]
			public long ChildTerminalID;
		}

		#region Public Static Variables
		/// <summary>
		/// The spline size in meters
		/// </summary>
		public static double TargetConnectionEffectSize => 25;

		/// <summary>
		/// Max connection distance allowed between two remote links
		/// </summary>
		public static double MaxLargeConnectionDistance => 1000;

		/// <summary>
		/// Max connection distance allowed between two remote links
		/// </summary>
		public static double MaxSmallConnectionDistance => 500;
		#endregion

		#region Private Variables
		/// <summary>
		/// The time after a connection, in ticks, until gates are reconstructed
		/// </summary>
		private byte ReconstructionDelay = 0;

		/// <summary>
		/// The emissive animation for this block
		/// </summary>
		private byte[] EmissiveAnimation = new byte[2] { 0, 0 };

		/// <summary>
		/// The last game tick at which nearby links were updated
		/// </summary>
		private ulong LastNearbyLinksUpdateTime = 0;

		/// <summary>
		/// The world position of this block's particle emitter
		/// </summary>
		private Vector3D ParticleEmitterPos => MyJumpGateModSession.LocalVectorToWorldVectorP(this.TerminalBlock.WorldMatrix, this.LocalParticleEmitter?.Matrix.Translation ?? Vector3.Zero);

		/// <summary>
		/// This block's particle emitter dummy
		/// </summary>
		private IMyModelDummy LocalParticleEmitter;

		/// <summary>
		/// The collision detector for finding nearby links
		/// </summary>
		private MyEntity LinkDetector = null;

		/// <summary>
		/// The connection effect spline to show when a link is established
		/// </summary>
		private MyParticleEffect[] ConnectionEffects = new MyParticleEffect[6] { null, null, null, null, null, null };

		/// <summary>
		/// A list of all nearby links
		/// </summary>
		private List<MyJumpGateRemoteLink> NearbyLinks = new List<MyJumpGateRemoteLink>();

		/// <summary>
		/// A map of all topmost entities within search range
		/// </summary>
		private ConcurrentDictionary<long, MyEntity> NearbyEntities = new ConcurrentDictionary<long, MyEntity>();

		/// <summary>
		/// Mutex object for exclusive read-write operations on the NearbyLinks list
		/// </summary>
		private object NearbyLinksMutex = new object();
		#endregion

		#region Public Variables
		/// <summary>
		/// True if this remote link is the parent of a link connection
		/// </summary>
		public bool IsLinkParent => this.BlockSettings.IsPhysicallyConnected && this.BlockSettings.AttachedRemoteLink > 0;

		/// <summary>
		/// Max connection distance allowed between two remote links
		/// </summary>
		public double MaxConnectionDistance { get; private set; }

		/// <summary>
		/// The block data for this block
		/// </summary>
		public MyRemoteLinkBlockSettingsStruct BlockSettings { get; private set; }

		/// <summary>
		/// The attached remote link or null
		/// </summary>
		public MyJumpGateRemoteLink AttachedRemoteLink { get; private set; } = null;
		#endregion

		#region Constructors
		public MyJumpGateRemoteLink() : base() { }

		/// <summary>
		/// Creates a new null wrapper
		/// </summary>
		/// <param name="serialized">The serialized block data</param>
		/// <param name="parent">The containing grid or null to calculate</param>
		public MyJumpGateRemoteLink(MySerializedJumpGateRemoteLink serialized, MyJumpGateConstruct parent = null) : base(serialized, parent)
		{
			this.FromSerialized(serialized, parent);
		}
		#endregion

		#region "CubeBlockBase" Methods
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

				info.Append($"\n-=-=-=( {MyTexts.GetString("DisplayName_CubeBlock_JumpGateCapacitor")} )=-=-=-\n");
				info.Append($" {MyTexts.GetString("DetailedInfo_BlockBase_Input")}: {MyJumpGateModSession.AutoconvertMetricUnits(input_wattage * 1e6, "W/s", 4)}\n");
				info.Append($" {MyTexts.GetString("DetailedInfo_BlockBase_RequiredInput")}: {MyJumpGateModSession.AutoconvertMetricUnits(max_input * 1e6, "W/s", 4)}\n");
				info.Append($" {MyTexts.GetString("DetailedInfo_BlockBase_InputRatio")}: {((double.IsNaN(input_ratio)) ? "N/A" : $"{Math.Round(MathHelperD.Clamp(input_ratio, 0, 1) * 100, 2):#.00}%")}\n");
				info.Append($" {MyTexts.GetString("DetailedInfo_JumpGateRemoteLink_NearbyLinks")}: {this.GetNearbyLinks().Count()}\n");
				info.Append($" {MyTexts.GetString("DetailedInfo_JumpGateRemoteLink_AttachedLink")}: {(this.AttachedRemoteLink?.TerminalBlock?.CustomName ?? this.AttachedRemoteLink?.BlockID.ToString() ?? "N/A")}\n");
			}
		}

		protected override void Clean()
		{
			base.Clean();
			this.LinkDetector?.Close();

			for (byte i = 0; i < this.ConnectionEffects.Length; ++i)
			{
				this.ConnectionEffects[i]?.Stop();
				this.ConnectionEffects[i] = null;
			}

			this.NearbyEntities.Clear();
			lock (this.NearbyLinksMutex) this.NearbyLinks.Clear();
			this.NearbyEntities = null;
			this.NearbyLinks = null;
			this.ConnectionEffects = null;
			if (this.BlockSettings.AttachedRemoteLink <= 0 || this.AttachedRemoteLink == null) return;
			MyCubeGridGroups.Static.BreakLink(GridLinkTypeEnum.Logical, this.TerminalBlock.EntityId, (MyCubeGrid) this.TerminalBlock.CubeGrid, (MyCubeGrid) this.AttachedRemoteLink.TerminalBlock.CubeGrid);
			this.AttachedRemoteLink.AttachedRemoteLink = null;
			this.AttachedRemoteLink = null;
		}

		protected override void UpdateOnceAfterInit()
		{
			base.UpdateOnceAfterInit();

			// Create collider
			if (MyNetworkInterface.IsStandaloneMultiplayerClient || this.TerminalBlock?.CubeGrid?.Physics == null) return;
			this.LinkDetector = new MyEntity();
			this.LinkDetector.EntityId = 0;
			this.LinkDetector.Init(new StringBuilder($"JumpGateRemoteLink_{this.BlockID}"), null, (MyEntity) this.Entity, 1f);
			this.LinkDetector.Save = false;
			this.LinkDetector.Flags = EntityFlags.IsNotGamePrunningStructureObject | EntityFlags.NeedsWorldMatrix;
			PhysicsSettings settings = MyAPIGateway.Physics.CreateSettingsForDetector(this.LinkDetector, this.OnEntityCollision, MyJumpGateModSession.WorldMatrix, Vector3D.Zero, RigidBodyFlag.RBF_KINEMATIC, 15, true);
			MyAPIGateway.Physics.CreateSpherePhysics(settings, (float) this.MaxConnectionDistance);
			MyAPIGateway.Entities.AddEntity(this.LinkDetector);
			Logger.Debug($"CREATED_COLLIDER REMOTE_LINK={this.BlockID}, COLLIDER={this.LinkDetector.DisplayName}", 4);

			// Populate nearby links
			BoundingSphereD bounding_sphere = new BoundingSphereD(this.WorldMatrix.Translation, this.MaxConnectionDistance);
			List<MyEntity> entities = new List<MyEntity>();
			MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref bounding_sphere, entities);
			this.NearbyEntities.Clear();
			foreach (MyEntity entity in entities) this.OnEntityCollision(entity, true);
			this.SetDirty();

			// Check and reestablish saved connection
			if (!MyNetworkInterface.IsServerLike || !this.BlockSettings.IsPhysicallyConnected || this.BlockSettings.AttachedRemoteLink <= 0) return;
			MyJumpGateRemoteLink other = MyJumpGateModSession.Instance.GetJumpGateBlock<MyJumpGateRemoteLink>(this.BlockSettings.AttachedRemoteLink);

			if (other == null)
			{
				this.BlockSettings.AttachedRemoteLink = 0;
				this.BlockSettings.IsPhysicallyConnected = false;
				return;
			}
			else if (other.AttachedRemoteLink != null && other.AttachedRemoteLink != this)
			{
				MyJumpGateRemoteLink potential = MyCubeBlockBase.Instances.Where((pair) => pair.Value is MyJumpGateRemoteLink).Select((pair) => (MyJumpGateRemoteLink) pair.Value).FirstOrDefault((link) => link.AttachedRemoteLink == null && link.BlockSettings.AttachedRemoteLink == other.BlockSettings.AttachedRemoteLink);

				if (potential != null && potential != this && potential.AttachedRemoteLink == null && potential.BlockSettings.AttachedRemoteLink <= 0) other = potential;
				else
				{
					this.BlockSettings.IsPhysicallyConnected = false;
					this.BlockSettings.AttachedRemoteLink = 0;
					return;
				}
			}

			this.AttachedRemoteLink = other;
			this.BlockSettings.AttachedRemoteLink = other.TerminalBlock.EntityId;
			other.AttachedRemoteLink = this;
			other.BlockSettings.IsPhysicallyConnected = true;
			other.BlockSettings.AttachedRemoteLink = -this.TerminalBlock.EntityId;
			MyCubeGridGroups.Static.CreateLink(GridLinkTypeEnum.Logical, this.TerminalBlock.EntityId, (MyCubeGrid) this.TerminalBlock.CubeGrid, (MyCubeGrid) other.TerminalBlock.CubeGrid);
			this.ReconstructionDelay = 5;

			if (MyNetworkInterface.IsMultiplayerServer && other != null)
			{
				MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet {
					PacketType = MyPacketTypeEnum.LINK_CONNECTION,
					TargetID = 0,
					Broadcast = true,
				};
				packet.Payload(new MyLinkConnectionInfo {
					ParentTerminalID = this.TerminalBlock.EntityId,
					ChildTerminalID = other.TerminalBlock.EntityId,
				});
				packet.Send();
			}
		}

		public override void UpdateOnceBeforeFrame()
		{
			base.UpdateOnceBeforeFrame();
			if (!MyJumpGateRemoteLinkTerminal.IsLoaded) MyJumpGateRemoteLinkTerminal.Load(this.ModContext);
		}

		/// <summary>
		/// CubeBlockBase Method<br />
		/// Initializes the block's game logic
		/// </summary>
		/// <param name="object_builder">This entity's object builder</param>
		public override void Init(MyObjectBuilder_EntityBase object_builder)
		{
			bool is_large = ((IMyTerminalBlock) this.Entity).CubeGrid.GridSizeEnum == MyCubeSize.Large;
			this.MaxConnectionDistance = (is_large) ? 1000 : 500;
			this.Init(object_builder, 0, () => {
				if (this.AttachedRemoteLink == null) return (is_large) ? 0.05f : 0.02f;
				float base_power = (is_large) ? 0.1f : 0.25f;
				float max_power = (is_large) ? 0.5f : 0.75f;
				float ratio = (float) MathHelperD.Clamp(Vector3D.Distance(this.WorldMatrix.Translation, this.AttachedRemoteLink.WorldMatrix.Translation) / this.MaxConnectionDistance, 0, 1);
				return (max_power - base_power) * ratio + base_power;
			}, MyJumpGateModSession.BlockComponentDataGUID, false);
			this.TerminalBlock.Synchronized = true;
			string blockdata;
			this.ResourceSink.SetMaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId, 5000f);

			if (MyJumpGateModSession.Network.Registered)
			{
				MyJumpGateModSession.Network.On(MyPacketTypeEnum.UPDATE_REMOTE_LINK, this.OnNetworkBlockUpdate);
				MyJumpGateModSession.Network.On(MyPacketTypeEnum.LINK_CONNECTION, this.OnNetworkConnection);
			}

			if (this.ModStorageComponent.TryGetValue(MyJumpGateModSession.BlockComponentDataGUID, out blockdata) && blockdata.Length > 0)
			{
				try
				{
					MyRemoteLinkBlockSettingsStruct serialized = MyAPIGateway.Utilities.SerializeFromBinary<MyRemoteLinkBlockSettingsStruct>(Convert.FromBase64String(blockdata));
					this.BlockSettings = serialized;
				}
				catch (Exception e)
				{
					this.BlockSettings = new MyRemoteLinkBlockSettingsStruct();
					Logger.Error($"Failed to load block data: {this.GetType().Name}-{this.TerminalBlock.CustomName}\n{e}");
				}
			}
			else
			{
				this.BlockSettings = new MyRemoteLinkBlockSettingsStruct();
				this.ModStorageComponent.Add(MyJumpGateModSession.BlockComponentDataGUID, "");
			}

			Dictionary<string, IMyModelDummy> dummies = new Dictionary<string, IMyModelDummy>();
			this.TerminalBlock.Model.GetDummies(dummies);
			this.LocalParticleEmitter = dummies.GetValueOrDefault("particles1", null);

			if (MyJumpGateModSession.Network.Registered && MyNetworkInterface.IsStandaloneMultiplayerClient)
			{
				MyNetworkInterface.Packet request = new MyNetworkInterface.Packet
				{
					TargetID = 0,
					Broadcast = false,
					PacketType = MyPacketTypeEnum.UPDATE_REMOTE_LINK,
				};
				request.Payload(this.ToSerialized(true));
				request.Send();
			}
		}

		public override void UpdateAfterSimulation()
		{
			base.UpdateAfterSimulation();
			if (this.TerminalBlock.CubeGrid.Physics == null) return;

			// Update emissives
			bool working = this.IsWorking;
			if (working) this.TerminalBlock.SetEmissiveParts("Emissive0", Color.Lime, 1);
			else if (this.TerminalBlock.IsFunctional) this.TerminalBlock.SetEmissiveParts("Emissive0", Color.Red, 1);
			else this.TerminalBlock.SetEmissiveParts("Emissive0", Color.Black, 0);

			float lerp = ((float) this.EmissiveAnimation[0]) / 180;
			Color emissive_color = Color.Lerp(Color.Black, this.BlockSettings.ConnectionEffectColor, lerp);
			this.TerminalBlock.SetEmissiveParts("Emissive1", emissive_color, 1);

			if (this.EmissiveAnimation[0] < this.EmissiveAnimation[1]) ++this.EmissiveAnimation[0];
			else if (this.EmissiveAnimation[0] > this.EmissiveAnimation[1]) --this.EmissiveAnimation[0];

			// Update connection
			if (this.AttachedRemoteLink != null)
			{
				double scan_distance = Math.Min(this.MaxConnectionDistance, this.AttachedRemoteLink.MaxConnectionDistance);
				if (!working || this.BlockSettings.ChannelID != this.AttachedRemoteLink.BlockSettings.ChannelID || !this.IsFactionRelationValid(this.AttachedRemoteLink.OwnerID) || Vector3D.DistanceSquared(this.WorldMatrix.Translation, this.AttachedRemoteLink.WorldMatrix.Translation) > scan_distance * scan_distance) this.BreakConnection(false);
				else if (this.IsLinkParent && this.BlockSettings.DisplayConnectionEffect)
				{
					Vector3D local_emitter_pos = this.ParticleEmitterPos;
					Vector3D remote_emitter_pos = this.AttachedRemoteLink.ParticleEmitterPos;
					scan_distance = Vector3D.Distance(local_emitter_pos, remote_emitter_pos);
					float effect_scale = (float) (scan_distance / 25d);
					float inv_effect_scale = 1f / effect_scale;
					Vector3D delta = (remote_emitter_pos - local_emitter_pos);

					for (byte i = 0; i < this.ConnectionEffects.Length; ++i)
					{
						Vector3D particle_pos;
						MatrixD particle_matrix;
						string particle_name;

						if (i <= 1)
						{
							particle_pos = local_emitter_pos;
							particle_matrix = MatrixD.CreateWorld(particle_pos, delta.Normalized(), (delta + Vector3D.One).Normalized());
							particle_name = (i % 2 == 0) ? "IOTA.JumpGateLink.ColorableConnectionEffectEdge" : "IOTA.JumpGateLink.FixedConnectionEffectEdge";
						}
						else if (i >= 4)
						{
							particle_pos = remote_emitter_pos;
							particle_matrix = MatrixD.CreateWorld(particle_pos, -delta.Normalized(), (delta + Vector3D.One).Normalized());
							particle_name = (i % 2 == 0) ? "IOTA.JumpGateLink.ColorableConnectionEffectEdge" : "IOTA.JumpGateLink.FixedConnectionEffectEdge";
						}
						else
						{
							particle_pos = local_emitter_pos + delta / 2d;
							particle_matrix = MatrixD.CreateWorld(particle_pos, delta.Normalized(), (delta + Vector3D.One).Normalized());
							particle_name = (i % 2 == 0) ? "IOTA.JumpGateLink.ColorableConnectionEffectMiddle" : "IOTA.JumpGateLink.FixedConnectionEffectMiddle";
						}

						if (this.ConnectionEffects[i] != null || MyParticlesManager.TryCreateParticleEffect(particle_name, ref particle_matrix, ref particle_pos, uint.MaxValue, out this.ConnectionEffects[i]))
						{
							this.ConnectionEffects[i].UserScale = effect_scale;
							this.ConnectionEffects[i].UserRadiusMultiplier = inv_effect_scale;
							this.ConnectionEffects[i].WorldMatrix = particle_matrix;
							this.ConnectionEffects[i].UserColorMultiplier = (i % 2 == 0) ? this.BlockSettings.ConnectionEffectColor : Color.Lerp(this.BlockSettings.ConnectionEffectColor, Color.White, 0.125f);
							this.ConnectionEffects[i].UserBirthMultiplier = effect_scale;
							this.ConnectionEffects[i].UserVelocityMultiplier = inv_effect_scale;
							this.ConnectionEffects[i].Play();
						}
					}
				}
				else
				{
					foreach (MyParticleEffect effect in this.ConnectionEffects) effect?.StopEmitting();
				}

				if (this.ReconstructionDelay > 0)
				{
					--this.ReconstructionDelay;
					if (this.ReconstructionDelay == 0) this.JumpGateGrid?.MarkGatesForUpdate();
				}
			}
			else
			{
				if (MyNetworkInterface.IsServerLike && working && this.LocalGameTick % 10 == 0) this.UpdateNearbyLinks();
				foreach (MyParticleEffect effect in this.ConnectionEffects) effect?.StopEmitting();

				if (working && !this.BlockSettings.IsPhysicallyConnected && this.BlockSettings.AttachedRemoteLink > 0) this.Connect(MyJumpGateModSession.Instance.GetJumpGateBlock<MyJumpGateRemoteLink>(this.BlockSettings.AttachedRemoteLink));
				else if (working && !this.BlockSettings.IsPhysicallyConnected && this.BlockSettings.AttachedRemoteLink < 0)
				{
					MyJumpGateRemoteLink remote_link = MyJumpGateModSession.Instance.GetJumpGateBlock<MyJumpGateRemoteLink>(this.BlockSettings.AttachedRemoteLink);
					if (remote_link != null && remote_link.AttachedRemoteLink == null) remote_link.Connect(this);
					else this.BlockSettings.AttachedRemoteLink = 0;
				}
			}

			this.EmissiveAnimation[1] = (byte) ((this.AttachedRemoteLink == null) ? 0 : 180);

			// Debug draw
			if (MyJumpGateModSession.Instance.DebugMode && this.AttachedRemoteLink != null && this.IsLinkParent)
			{
				Vector4 color = Color.Magenta;
				MySimpleObjectDraw.DrawLine(this.ParticleEmitterPos, this.AttachedRemoteLink.ParticleEmitterPos, MyJumpGateModSession.MATERIALS.WeaponLaser, ref color, 0.25f);
			}

			//Network sync
			this.CheckSendGlobalUpdate();
		}

		/// <summary>
		/// CubeBlockBase Method<br />
		/// Unregisters events and marks block for close
		/// </summary>
		public override void MarkForClose()
		{
			base.MarkForClose();

			if (MyJumpGateModSession.Network.Registered)
			{
				MyJumpGateModSession.Network.Off(MyPacketTypeEnum.UPDATE_REMOTE_LINK, this.OnNetworkBlockUpdate);
				MyJumpGateModSession.Network.Off(MyPacketTypeEnum.LINK_CONNECTION, this.OnNetworkConnection);
			}

			this.LinkDetector?.Close();
			this.LinkDetector = null;
			this.NearbyEntities.Clear();
		}

		/// <summary>
		/// Serializes block for streaming and saving
		/// </summary>
		/// <returns>false</returns>
		public override bool IsSerialized()
		{
			bool res = base.IsSerialized();
			this.ModStorageComponent[MyJumpGateModSession.BlockComponentDataGUID] = Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(this.BlockSettings));
			return res;
		}
		#endregion

		#region Private Methods
		/// <summary>
		/// Callback triggered when this component receives a block update request from server
		/// </summary>
		/// <param name="packet">The update request packet</param>
		private void OnNetworkBlockUpdate(MyNetworkInterface.Packet packet)
		{
			if (packet == null) return;
			MySerializedJumpGateRemoteLink serialized = packet.Payload<MySerializedJumpGateRemoteLink>();
			if (serialized == null || serialized.UUID.GetBlock() != this.BlockID) return;

			if (MyNetworkInterface.IsMultiplayerServer && packet.PhaseFrame == 1 && packet.EpochTime > this.LastUpdateTime)
			{
				if (serialized.IsClientRequest)
				{
					packet.Payload(this.ToSerialized(false));
					packet.Forward(packet.SenderID, false).Send();

					MyNetworkInterface.Packet connection_packet = new MyNetworkInterface.Packet {
						PacketType = MyPacketTypeEnum.LINK_CONNECTION,
						TargetID = packet.SenderID,
						Broadcast = false,
					};
					connection_packet.Payload(new MyLinkConnectionInfo
					{
						ParentTerminalID = this.TerminalBlock.EntityId,
						ChildTerminalID = this.AttachedRemoteLink?.TerminalBlock.EntityId ?? 0,
					});
					connection_packet.Send();
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
		/// Callback triggered when this component receives a remote link connection request
		/// </summary>
		/// <param name="packet">The connection request packet</param>
		private void OnNetworkConnection(MyNetworkInterface.Packet packet)
		{
			if (packet == null) return;
			MyLinkConnectionInfo info = packet.Payload<MyLinkConnectionInfo>();
			if (info == null || info.ParentTerminalID != this.BlockID) return;

			if (MyNetworkInterface.IsMultiplayerServer && packet.PhaseFrame == 1)
			{
				MyJumpGateRemoteLink remote = this.AttachedRemoteLink;
				this.BreakConnection(true);
				remote?.SetDirty();
				if (info.ChildTerminalID != 0) this.Connect(MyJumpGateModSession.Instance.GetJumpGateBlock<MyJumpGateRemoteLink>(info.ChildTerminalID));
				this.SetDirty();
				this.AttachedRemoteLink?.SetDirty();
				packet.Forward(0, true).Send();
			}
			else if (MyNetworkInterface.IsStandaloneMultiplayerClient && (packet.PhaseFrame == 1 || packet.PhaseFrame == 2) && this.TerminalBlock?.CubeGrid != null)
			{
				MyJumpGateRemoteLink link;
				if (info.ChildTerminalID == 0) MyCubeGridGroups.Static.BreakLink(GridLinkTypeEnum.Logical, this.TerminalBlock.EntityId, (MyCubeGrid) this.TerminalBlock.CubeGrid, null);
				else if ((link = MyJumpGateModSession.Instance.GetJumpGateBlock<MyJumpGateRemoteLink>(info.ChildTerminalID))?.TerminalBlock?.CubeGrid != null) MyCubeGridGroups.Static.CreateLink(GridLinkTypeEnum.Logical, this.TerminalBlock.EntityId, (MyCubeGrid) this.TerminalBlock.CubeGrid, (MyCubeGrid) link.TerminalBlock.CubeGrid);
			}
		}

		/// <summary>
		/// Server only<br />
		/// Checks if this block should be updated and if true -<br />
		/// sends an update request for this block to all clients
		/// </summary>
		private void CheckSendGlobalUpdate()
		{
			if (MyJumpGateModSession.Network.Registered && ((MyNetworkInterface.IsMultiplayerServer && MyJumpGateModSession.Instance.GameTick % MyCubeBlockBase.ForceUpdateDelay == 0) || this.IsDirty))
			{
				MyNetworkInterface.Packet update_packet = new MyNetworkInterface.Packet {
					PacketType = MyPacketTypeEnum.UPDATE_REMOTE_LINK,
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
			if (entity == null || this.MarkedForClose || this.NearbyEntities == null || !(entity is MyCubeGrid)) return;
			else if (is_entering) this.NearbyEntities[entity.EntityId] = (MyEntity) entity;
			else this.NearbyEntities.Remove(entity.EntityId);
			this.SetDirty();
		}

		/// <summary>
		/// Updates the internal listing of nearby antennas
		/// </summary>
		private void UpdateNearbyLinks()
		{
			if (!MyNetworkInterface.IsServerLike || this.NearbyEntities == null || this.NearbyLinks == null || this.JumpGateGrid == null) return;
			MyJumpGateConstruct construct = null;
			Vector3D this_pos = this.WorldMatrix.Translation;
			string name = this.JumpGateGrid.PrimaryCubeGridCustomName;

			lock (this.NearbyLinksMutex)
			{
				this.NearbyLinks?.Clear();
				this.LastNearbyLinksUpdateTime = this.LocalGameTick;

				if (this.IsWorking)
				{
					int count = this.NearbyLinks?.Count ?? 0;
					double distance = this.MaxConnectionDistance * this.MaxConnectionDistance;

					foreach (KeyValuePair<long, MyEntity> pair in this.NearbyEntities)
					{
						if (!(pair.Value is MyCubeGrid) || pair.Value.Physics == null) continue;
						MyCubeGrid grid = (MyCubeGrid) pair.Value;
						if (!MyJumpGateModSession.Instance.IsJumpGateGridMultiplayerValid(construct = MyJumpGateModSession.Instance.GetDirectJumpGateGrid(grid))) continue;

						foreach (MyJumpGateRemoteLink link in construct.GetAttachedJumpGateRemoteLinks())
						{
							if (link.CubeGridID == this.CubeGridID || !link.IsWorking || Vector3D.DistanceSquared(link.WorldMatrix.Translation, this_pos) > distance) continue;
							this.NearbyLinks?.Add(link);
						}
					}

					if (count != (this.NearbyLinks?.Count ?? 0)) this.SetDirty();
				}
			}
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Breaks the connection between this remote link and its attached remote link
		/// </summary>
		public void BreakConnection(bool permanent)
		{
			if (!MyNetworkInterface.IsServerLike || this.AttachedRemoteLink == null) return;
			if (!this.IsLinkParent)
			{
				this.AttachedRemoteLink.BreakConnection(permanent);
				return;
			}

			MyCubeGridGroups.Static.BreakLink(GridLinkTypeEnum.Logical, this.TerminalBlock.EntityId, (MyCubeGrid) this.TerminalBlock.CubeGrid, null);

			if (permanent)
			{
				this.AttachedRemoteLink.BlockSettings.AttachedRemoteLink = 0;
				this.BlockSettings.AttachedRemoteLink = 0;
			}

			this.AttachedRemoteLink.AttachedRemoteLink = null;
			this.AttachedRemoteLink.BlockSettings.IsPhysicallyConnected = false;
			this.AttachedRemoteLink = null;
			this.BlockSettings.IsPhysicallyConnected = false;
		}

		/// <summary>
		/// Connects two remote links together<br />
		/// Links must be within the max connection distance of both links
		/// </summary>
		/// <param name="link">The other remote link</param>
		/// <returns>True if successfull</returns>
		public bool Connect(MyJumpGateRemoteLink link)
		{
			if (!MyNetworkInterface.IsServerLike || link == null || this.AttachedRemoteLink == link || link == this || this.BlockSettings.ChannelID != link.BlockSettings.ChannelID || this.CubeGridID == link.CubeGridID || !this.IsWorking || !link.IsWorking || !this.IsFactionRelationValid(link.OwnerID) || !link.IsFactionRelationValid(this.OwnerID)) return false;
			double scan_distance = Math.Min(this.MaxConnectionDistance, link.MaxConnectionDistance);
			if (Vector3D.DistanceSquared(this.WorldMatrix.Translation, link.WorldMatrix.Translation) > scan_distance * scan_distance) return false;
			this.BreakConnection(false);
			link.BreakConnection(false);
			this.BlockSettings.AttachedRemoteLink = link.TerminalBlock.EntityId;
			link.BlockSettings.AttachedRemoteLink = -this.TerminalBlock.EntityId;
			this.BlockSettings.IsPhysicallyConnected = true;
			link.BlockSettings.IsPhysicallyConnected = true;
			this.AttachedRemoteLink = link;
			link.AttachedRemoteLink = this;
			MyCubeGridGroups.Static.CreateLink(GridLinkTypeEnum.Logical, this.TerminalBlock.EntityId, (MyCubeGrid) this.TerminalBlock.CubeGrid, (MyCubeGrid) link.TerminalBlock.CubeGrid);
			this.ReconstructionDelay = 5;
			return true;
		}

		/// <summary>
		/// </summary>
		/// <param name="steam_id">The player's steam ID to check</param>
		/// <returns>True if the caller's faction can jump to this gate</returns>
		public bool IsFactionRelationValid(ulong steam_id)
		{
			long player_id = MyAPIGateway.Players.TryGetIdentityId(steam_id);
			if (player_id == 0) return false;
			MyRelationsBetweenPlayerAndBlock relation = this.TerminalBlock?.GetUserRelationToOwner(player_id) ?? MyJumpGateModSession.GetPlayerByID(this.OwnerID)?.GetRelationTo(player_id) ?? MyRelationsBetweenPlayerAndBlock.NoOwnership;
			MyFactionDisplayType faction;

			switch (relation)
			{
				case MyRelationsBetweenPlayerAndBlock.NoOwnership:
					faction = MyFactionDisplayType.UNOWNED;
					break;
				case MyRelationsBetweenPlayerAndBlock.Neutral:
					faction = MyFactionDisplayType.NEUTRAL;
					break;
				case MyRelationsBetweenPlayerAndBlock.Friends:
					faction = MyFactionDisplayType.FRIENDLY;
					break;
				case MyRelationsBetweenPlayerAndBlock.Enemies:
					faction = MyFactionDisplayType.ENEMY;
					break;
				case MyRelationsBetweenPlayerAndBlock.Owner:
				case MyRelationsBetweenPlayerAndBlock.FactionShare:
					faction = MyFactionDisplayType.OWNED;
					break;
				default:
					return false;
			}

			return this.BlockSettings.IsFactionConnectionAllowed(faction);
		}

		/// <summary>
		/// </summary>
		/// <param name="player_identity">The player's identiy to check</param>
		/// <returns>True if the caller's faction can jump to this gate</returns>
		public bool IsFactionRelationValid(long player_identity)
		{
			MyRelationsBetweenPlayerAndBlock relation = this.TerminalBlock?.GetUserRelationToOwner(player_identity) ?? MyJumpGateModSession.GetPlayerByID(this.OwnerID)?.GetRelationTo(player_identity) ?? MyRelationsBetweenPlayerAndBlock.NoOwnership;
			MyFactionDisplayType faction;

			switch (relation)
			{
				case MyRelationsBetweenPlayerAndBlock.NoOwnership:
					faction = MyFactionDisplayType.UNOWNED;
					break;
				case MyRelationsBetweenPlayerAndBlock.Neutral:
					faction = MyFactionDisplayType.NEUTRAL;
					break;
				case MyRelationsBetweenPlayerAndBlock.Friends:
					faction = MyFactionDisplayType.FRIENDLY;
					break;
				case MyRelationsBetweenPlayerAndBlock.Enemies:
					faction = MyFactionDisplayType.ENEMY;
					break;
				case MyRelationsBetweenPlayerAndBlock.Owner:
				case MyRelationsBetweenPlayerAndBlock.FactionShare:
					faction = MyFactionDisplayType.OWNED;
					break;
				default:
					return false;
			}

			return this.BlockSettings.IsFactionConnectionAllowed(faction);
		}

		public bool FromSerialized(MySerializedJumpGateRemoteLink link, MyJumpGateConstruct parent = null)
		{
			if (!base.FromSerialized(link, parent)) return false;
			
			if (!MyNetworkInterface.IsServerLike)
			{
				this.NearbyLinks?.Clear();
				if (link.SyncedNearbyLinks != null) this.NearbyLinks?.AddRange(link.SyncedNearbyLinks.Select(MyJumpGateModSession.Instance.GetJumpGateBlock<MyJumpGateRemoteLink>));
			}

			this.BlockSettings = MyAPIGateway.Utilities.SerializeFromBinary<MyRemoteLinkBlockSettingsStruct>(Convert.FromBase64String(link.BlockSettings));
			if (!MyNetworkInterface.IsServerLike) this.AttachedRemoteLink = MyJumpGateModSession.Instance.GetJumpGateBlock<MyJumpGateRemoteLink>(this.BlockSettings.AttachedRemoteLink);
			return true;
		}

		/// <summary>
		/// Gets all working links within range of this one
		/// </summary>
		/// <returns>An enumerable containing all nearby working links</returns>
		public IEnumerable<MyJumpGateRemoteLink> GetNearbyLinks()
		{
			if (this.LastNearbyLinksUpdateTime == 0 || (this.LocalGameTick - this.LastNearbyLinksUpdateTime) >= 60) this.UpdateNearbyLinks();
			return this.NearbyLinks?.Where((block) => block != null) ?? Enumerable.Empty<MyJumpGateRemoteLink>();
		}

		public new MySerializedJumpGateRemoteLink ToSerialized(bool as_client_request)
		{
			if (as_client_request) return base.ToSerialized<MySerializedJumpGateRemoteLink>(true);
			MySerializedJumpGateRemoteLink serialized = base.ToSerialized<MySerializedJumpGateRemoteLink>(false);
			serialized.SyncedNearbyLinks = this.GetNearbyLinks().Select((link) => link.BlockID).ToList();
			serialized.BlockSettings = Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(this.BlockSettings));
			return serialized;
		}
		#endregion
	}

	[ProtoContract]
	internal class MySerializedJumpGateRemoteLink : MySerializedCubeBlockBase
	{
		[ProtoMember(20)]
		public List<long> SyncedNearbyLinks;

		[ProtoMember(21)]
		public string BlockSettings;
	}
}
