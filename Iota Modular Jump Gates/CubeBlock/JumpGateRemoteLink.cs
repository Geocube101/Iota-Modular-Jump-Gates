using IOTA.ModularJumpGates.Util;
using ProtoBuf;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
			public MyFactionDisplayType AllowedFactionConnections = MyFactionDisplayType.OWNED;

			[ProtoMember(3)]
			public uint ChannelID = 0;

			[ProtoMember(4)]
			public long AttachedRemoteLink = 0;


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

		#region Public Static Variables
		/// <summary>
		/// Max connection distance allowed between two remote links
		/// </summary>
		public static readonly double MaxLargeConnectionDistance = 1000;

		/// <summary>
		/// Max connection distance allowed between two remote links
		/// </summary>
		public static readonly double MaxSmallConnectionDistance = 500;
		#endregion

		#region Private Variables
		/// <summary>
		/// The collision detector for finding nearby links
		/// </summary>
		private MyEntity LinkDetector = null;

		/// <summary>
		/// The connection effect spline to show when a link is established
		/// </summary>
		private List<MyParticleEffect> ConnectionEffects = null;

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
		public bool IsLinkParent => this.BlockSettings.AttachedRemoteLink > 0;

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
		protected override void Clean()
		{
			base.Clean();
			this.LinkDetector?.Close();
			foreach (MyParticleEffect effect in this.ConnectionEffects) effect.Stop();
			this.ConnectionEffects.Clear();
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
			if (MyNetworkInterface.IsStandaloneMultiplayerClient || this.TerminalBlock?.CubeGrid?.Physics == null) return;
			this.LinkDetector = new MyEntity();
			this.LinkDetector.EntityId = 0;
			this.LinkDetector.Init(new StringBuilder($"JumpGateRemoteLink_{this.BlockID}"), null, (MyEntity) this.Entity, 1f);
			this.LinkDetector.Save = false;
			this.LinkDetector.Flags = EntityFlags.IsNotGamePrunningStructureObject | EntityFlags.NeedsWorldMatrix;
			PhysicsSettings settings = MyAPIGateway.Physics.CreateSettingsForDetector(this.LinkDetector, this.OnEntityCollision, MyJumpGateModSession.WorldMatrix, Vector3D.Zero, RigidBodyFlag.RBF_KINEMATIC, 15, true);
			MyAPIGateway.Physics.CreateSpherePhysics(settings, (float) MyJumpGateRemoteAntenna.MaxCollisionDistance);
			MyAPIGateway.Entities.AddEntity(this.LinkDetector);
			Logger.Debug($"CREATED_COLLIDER REMOTE_LINK={this.BlockID}, COLLIDER={this.LinkDetector.DisplayName}", 4);

			BoundingSphereD bounding_sphere = new BoundingSphereD(this.WorldMatrix.Translation, this.MaxConnectionDistance);
			List<MyEntity> entities = new List<MyEntity>();
			MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref bounding_sphere, entities);
			this.NearbyEntities.Clear();
			foreach (MyEntity entity in entities) this.OnEntityCollision(entity, true);
			this.SetDirty();
			if (this.BlockSettings.AttachedRemoteLink <= 0) return;
			MyJumpGateRemoteLink other = MyJumpGateModSession.Instance.GetJumpGateBlock<MyJumpGateRemoteLink>(this.BlockSettings.AttachedRemoteLink);

			if (other == null || (other.AttachedRemoteLink != null && other.AttachedRemoteLink != this) || other.BlockSettings.AttachedRemoteLink != -this.TerminalBlock.EntityId)
			{
				this.BlockSettings.AttachedRemoteLink = -1;
				return;
			}

			other.AttachedRemoteLink = this;
			this.AttachedRemoteLink = other;
			MyCubeGridGroups.Static.CreateLink(GridLinkTypeEnum.Logical, this.TerminalBlock.EntityId, (MyCubeGrid) this.TerminalBlock.CubeGrid, (MyCubeGrid) other.TerminalBlock.CubeGrid);
		}

		/// <summary>
		/// CubeBlockBase Method<br />
		/// Initializes the block's game logic
		/// </summary>
		/// <param name="object_builder">This entity's object builder</param>
		public override void Init(MyObjectBuilder_EntityBase object_builder)
		{
			bool is_large = ((IMyTerminalBlock) this.Entity).CubeGrid.GridSizeEnum == VRage.Game.MyCubeSize.Large;
			double max_power_draw = (is_large) ? 5000 : 2500;
			this.MaxConnectionDistance = (is_large) ? 1000 : 500;
			this.Init(object_builder, 0, 5000, MyJumpGateModSession.BlockComponentDataGUID, false);
			this.TerminalBlock.Synchronized = true;
			if (MyJumpGateModSession.Network.Registered) MyJumpGateModSession.Network.On(MyPacketTypeEnum.UPDATE_REMOTE_LINK, this.OnNetworkBlockUpdate);
			string blockdata;
			this.ResourceSink.SetMaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId, 5000f);

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

			if (MyJumpGateModSession.Network.Registered && MyNetworkInterface.IsStandaloneMultiplayerClient)
			{
				MyNetworkInterface.Packet request = new MyNetworkInterface.Packet {
					TargetID = 0,
					Broadcast = false,
					PacketType = MyPacketTypeEnum.UPDATE_REMOTE_LINK,
				};
				request.Payload<MySerializedJumpGateCapacitor>(null);
				request.Send();
			}
		}

		public override void UpdateAfterSimulation()
		{
			base.UpdateAfterSimulation();
			if (this.TerminalBlock.CubeGrid.Physics == null) return;
			bool working = this.IsWorking;

			if (this.AttachedRemoteLink != null)
			{
				double scan_distance = Math.Min(this.MaxConnectionDistance, this.AttachedRemoteLink.MaxConnectionDistance);
				if (!working || this.BlockSettings.ChannelID != this.AttachedRemoteLink.BlockSettings.ChannelID || !this.IsFactionRelationValid(this.AttachedRemoteLink.OwnerID) || Vector3D.DistanceSquared(this.WorldMatrix.Translation, this.AttachedRemoteLink.WorldMatrix.Translation) > scan_distance * scan_distance) this.BreakConnection();
			}
			else if (working && this.LocalGameTick % 10 == 0) this.UpdateNearbyLinks();

			if (MyJumpGateModSession.DebugMode && this.AttachedRemoteLink != null && this.IsLinkParent)
			{
				Vector4 color = Color.Magenta;
				MySimpleObjectDraw.DrawLine(this.WorldMatrix.Translation, this.AttachedRemoteLink.WorldMatrix.Translation, MyJumpGateModSession.MyMaterialsHolder.WeaponLaser, ref color, 0.25f);
			}

			this.CheckSendGlobalUpdate();
		}

		/// <summary>
		/// CubeBlockBase Method<br />
		/// Unregisters events and marks block for close
		/// </summary>
		public override void MarkForClose()
		{
			base.MarkForClose();
			if (MyJumpGateModSession.Network.Registered) MyJumpGateModSession.Network.Off(MyPacketTypeEnum.UPDATE_REMOTE_LINK, this.OnNetworkBlockUpdate);
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
		/// Callback triggered when this component recevies a block update request from server
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
			if (this.NearbyEntities == null || this.NearbyLinks == null || this.JumpGateGrid == null) return;
			List<long> checked_grids = new List<long>(this.JumpGateGrid.GetCubeGrids().Select((grid) => grid.EntityId));
			MyJumpGateConstruct construct = null;
			Vector3D this_pos = this.WorldMatrix.Translation;
			string name = this.JumpGateGrid.PrimaryCubeGridCustomName;

			lock (this.NearbyLinksMutex)
			{
				this.NearbyLinks?.Clear();

				if (this.IsWorking)
				{
					double distance = this.MaxConnectionDistance * this.MaxConnectionDistance;

					foreach (KeyValuePair<long, MyEntity> pair in this.NearbyEntities)
					{
						if (!(pair.Value is MyCubeGrid) || pair.Value.Physics == null) continue;
						MyCubeGrid grid = (MyCubeGrid) pair.Value;
						if (checked_grids.Contains(pair.Key) || !MyJumpGateModSession.Instance.IsJumpGateGridMultiplayerValid(construct = MyJumpGateModSession.Instance.GetDirectJumpGateGrid(grid))) continue;
						checked_grids.AddRange(construct.GetCubeGrids().Select((subgrid) => subgrid.EntityId));

						foreach (MyJumpGateRemoteLink link in construct.GetAttachedJumpGateRemoteLinks())
						{
							if (!link.IsWorking || Vector3D.DistanceSquared(link.WorldMatrix.Translation, this_pos) > distance) continue;
							lock (this.NearbyLinksMutex) this.NearbyLinks?.Add(link);
						}
					}
				}
			}
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Breaks the connection between this remote link and its attached remote link
		/// </summary>
		public void BreakConnection()
		{
			if (this.AttachedRemoteLink != null && !this.IsLinkParent)
			{
				this.AttachedRemoteLink.BreakConnection();
				return;
			}
			else if (!this.IsLinkParent || this.AttachedRemoteLink == null) return;

			MyCubeGridGroups.Static.BreakLink(GridLinkTypeEnum.Logical, this.TerminalBlock.EntityId, (MyCubeGrid) this.TerminalBlock.CubeGrid, (MyCubeGrid) this.AttachedRemoteLink.TerminalBlock.CubeGrid);
			this.AttachedRemoteLink.AttachedRemoteLink = null;
			this.AttachedRemoteLink.BlockSettings.AttachedRemoteLink = 0;
			this.AttachedRemoteLink = null;
			this.BlockSettings.AttachedRemoteLink = 0;
		}

		/// <summary>
		/// Connects two remote links together<br />
		/// Links must be within the max connection distance of both links
		/// </summary>
		/// <param name="link">The other remote link</param>
		/// <returns>True if successfull</returns>
		public bool Connect(MyJumpGateRemoteLink link)
		{
			if (link == null || this.AttachedRemoteLink == link || link == this || this.BlockSettings.ChannelID != link.BlockSettings.ChannelID || !this.IsFactionRelationValid(link.OwnerID)) return false;
			double scan_distance = Math.Min(this.MaxConnectionDistance, link.MaxConnectionDistance);
			if (Vector3D.DistanceSquared(this.WorldMatrix.Translation, link.WorldMatrix.Translation) > scan_distance * scan_distance) return false;
			this.BreakConnection();
			link.BreakConnection();
			this.BlockSettings.AttachedRemoteLink = link.TerminalBlock.EntityId;
			link.BlockSettings.AttachedRemoteLink = -this.TerminalBlock.EntityId;
			this.AttachedRemoteLink = link;
			link.AttachedRemoteLink = this;
			MyCubeGridGroups.Static.CreateLink(GridLinkTypeEnum.Logical, this.TerminalBlock.EntityId, (MyCubeGrid) this.TerminalBlock.CubeGrid, (MyCubeGrid) link.TerminalBlock.CubeGrid);
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
			return true;
		}

		public new MySerializedJumpGateRemoteLink ToSerialized(bool as_client_request)
		{
			if (as_client_request) return base.ToSerialized<MySerializedJumpGateRemoteLink>(true);
			MySerializedJumpGateRemoteLink serialized = base.ToSerialized<MySerializedJumpGateRemoteLink>(false);
			return serialized;
		}
		#endregion
	}

	[ProtoContract]
	internal class MySerializedJumpGateRemoteLink : MySerializedCubeBlockBase
	{
		/// <summary>
		/// The connected server's address
		/// </summary>
		[ProtoMember(20)]
		public uint ChannelID;

		[ProtoMember(21)]
		public long AttachedRemoteLink;
	}
}
