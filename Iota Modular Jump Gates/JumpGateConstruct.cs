using IOTA.ModularJumpGates.CubeBlock;
using IOTA.ModularJumpGates.Util;
using IOTA.ModularJumpGates.Util.ConcurrentCollections;
using ProtoBuf;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;

namespace IOTA.ModularJumpGates
{
	public enum MyGridInvalidationReason : byte { NONE, CLOSED, NULL_GRID, INSUFFICIENT_GRIDS, NULL_PHYSICS };

	/// <summary>
	/// Class representing a grid-group construct
	/// </summary>
	internal class MyJumpGateConstruct
    {
		#region Private Static Variables
		/// <summary>
		/// The delay in milliseconds after which the next call to comm linked grids should update the internal list
		/// </summary>
		private static readonly ushort CommLinkedUpdateDelay = (ushort) ((MyNetworkInterface.IsStandaloneMultiplayerClient) ? 3000u : 500u);
		#endregion

		#region Private Variables
		/// <summary>
		/// Whether this grid has an outgoing unfulfilled comm linked grids request
		/// </summary>
		private bool CommLinkedClientUpdate = false;

		/// <summary>
		/// Whether this grid has an outgoing unfulfilled beacon linked grids request
		/// </summary>
		private bool BeaconLinkedClientUpdate = false;

		/// <summary>
		/// Whether drive max raycast distances should be updated on next tick
		/// </summary>
		private bool UpdateDriveMaxRaycastDistances = false;

		/// <summary>
		/// Whether to update comm linked grids on next tick
		/// </summary>
		private bool UpdateCommLinkedGrids = true;

		/// <summary>
		/// Whether to update reachable beacons on next tick
		/// </summary>
		private bool UpdateBeaconLinkedBeacons = true;

		/// <summary>
		/// The next jump gate ID
		/// </summary>
		private long NextJumpGateID = 0;

        /// <summary>
        /// The ID of the CubeGrid this construct is bound to when suspended
        /// </summary>
        private long SuspendedCubeGridID;

        /// <summary>
        /// The local game tick for this object
        /// </summary>
		private ulong LocalGameTick = 0;

		/// <summary>
		/// The last time this grid was updated (in timer ticks ~100ns)
		/// </summary>
		private ulong LastUpdateTime = 0;

		/// <summary>
		/// The last time at which comm linked grids were updated
		/// </summary>
		private DateTime LastCommLinkUpdate;

		/// <summary>
		/// The last time at which beacon links were updated
		/// </summary>
		private DateTime LastBeaconLinkUpdate;

		/// <summary>
		/// The mutex lock for exclusive construct updates
		/// </summary>
		private object UpdateLock = new object();

		/// <summary>
		/// The mutex lock for exclusive comm-linked updates
		/// </summary>
		private IOTA.ModularJumpGates.Util.ReaderWriterLock CommLinkedLock = new IOTA.ModularJumpGates.Util.ReaderWriterLock();

		/// <summary>
		/// The mutex lock for exclusive beacon-linked updates
		/// </summary>
		private IOTA.ModularJumpGates.Util.ReaderWriterLock BeaconLinkedLock = new IOTA.ModularJumpGates.Util.ReaderWriterLock();

		/// <summary>
		/// A queue of all IDs from closed gates
		/// </summary>
		private Queue<long> ClosedJumpGateIDs = new Queue<long>();

		/// <summary>
		/// A list of all grids reachable from this grid by antenna
		/// </summary>
		private List<MyJumpGateConstruct> CommLinkedGrids = null;

		/// <summary>
		/// A list of all beacons that this grid can see
		/// </summary>
		private List<MyBeaconLinkWrapper> BeaconLinks = null;

		/// <summary>
		/// A list of all jump gate drive combinations<br />
		/// List is non-repeating
		/// </summary>
		private List<KeyValuePair<MyJumpGateDrive, MyJumpGateDrive>> DriveCombinations = null;

		/// <summary>
		/// A collection for hopefully faster iterating of grid blocks
		/// </summary>
		private ConcurrentLinkedHashSet<IMySlimBlock> GridBlocks = new ConcurrentLinkedHashSet<IMySlimBlock>();

        /// <summary>
        /// A list of the last 60 update times
        /// </summary>
		private ConcurrentSpinQueue<double> UpdateTimeTicks = new ConcurrentSpinQueue<double>(60);

        /// <summary>
        /// The master map of jump gates this construct has
        /// </summary>
        private ConcurrentDictionary<long, MyJumpGate> JumpGates = new ConcurrentDictionary<long, MyJumpGate>();

        /// <summary>
        /// The master map of CubeGrids in this construct
        /// </summary>
		private ConcurrentDictionary<long, IMyCubeGrid> CubeGrids = new ConcurrentDictionary<long, IMyCubeGrid>();

        /// <summary>
        /// The master map of jump gate drives this construct has
        /// </summary>
		private ConcurrentDictionary<long, MyJumpGateDrive> JumpGateDrives = new ConcurrentDictionary<long, MyJumpGateDrive>();

		/// <summary>
		/// The master map of jump gate controllers this construct has
		/// </summary>
		private ConcurrentDictionary<long, MyJumpGateController> JumpGateControllers = new ConcurrentDictionary<long, MyJumpGateController>();

		/// <summary>
		/// The master map of jump gate capacitors this construct has
		/// </summary>
		private ConcurrentDictionary<long, MyJumpGateCapacitor> JumpGateCapacitors = new ConcurrentDictionary<long, MyJumpGateCapacitor>();

		/// <summary>
		/// The master map of jump gate server antennas this construct has
		/// </summary>
		private ConcurrentDictionary<long, MyJumpGateServerAntenna> JumpGateServerAntennas = new ConcurrentDictionary<long, MyJumpGateServerAntenna>();

		/// <summary>
		/// The master map of laser antennas this construct has
		/// </summary>
		private ConcurrentDictionary<long, IMyLaserAntenna> LaserAntennas = new ConcurrentDictionary<long, IMyLaserAntenna>();

		/// <summary>
		/// The master map of radio antennas this construct has
		/// </summary>
		private ConcurrentDictionary<long, IMyRadioAntenna> RadioAntennas = new ConcurrentDictionary<long, IMyRadioAntenna>();

		/// <summary>
		/// The master map of beacons this construct has
		/// </summary>
		private ConcurrentDictionary<long, IMyBeacon> BeaconAntennas = new ConcurrentDictionary<long, IMyBeacon>();
		#endregion

		#region Public Variables
		/// <summary>
		/// Whether this construct is closed
		/// </summary>
		public bool Closed { get; private set; } = false;

        /// <summary>
        /// Whether this construct is marked for close
        /// </summary>
		public bool MarkClosed { get; private set; } = false;

		/// <summary>
		/// Whether this construct is suspended from updates<br />
		/// Only used on muliplayer clients when grid is not in scene
		/// </summary>
		public bool IsSuspended { get; private set; } = false;

		/// <summary>
		/// Whether to update jump gate intersections on next tick<br />
		/// Gate drive intersections will be updated and gates reconstructed on next tick
		/// </summary>
		public bool MarkUpdateJumpGates { get; private set; } = true;

		/// <summary>
		/// Whether this construct should be synced<br/>
		/// If true, will be synced on next component tick
		/// </summary>
		public bool IsDirty { get; private set; } = false;

		/// <summary>
		/// Whether this construct is fully initialized<br />
		/// True when all gates are constructed and resources initialized
		/// </summary>
		public bool FullyInitialized { get; private set; } = false;

		/// <summary>
		/// The current ID of this construct's cube grid
		/// </summary>
		public long CubeGridID
        {
            get
            {
                return (this.IsSuspended || this.CubeGrid == null) ? this.SuspendedCubeGridID : this.CubeGrid.EntityId;
            }
		}

		/// <summary>
		/// The longest time it took for this grid to update
		/// </summary>
		public double LongestUpdateTime { get; private set; } = -1;

		/// <summary>
		/// The custom name of this construct's primary cube grid
		/// </summary>
		public string PrimaryCubeGridCustomName { get; private set; }

		/// <summary>
		/// The UTC date time representation of MyJumpGateConstruct.LastUpdateTime
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
				foreach (KeyValuePair<long, MyJumpGate> pair in this.JumpGates) if (!pair.Value.Closed) pair.Value.LastUpdateDateTimeUTC = value;
			}
		}

		/// <summary>
		/// The main cube grid for this construct
		/// </summary>
		public IMyCubeGrid CubeGrid { get; private set; }

		/// <summary>
		/// The gate this construct is currently being jumped by or null
		/// </summary>
		public MyJumpGate BatchingGate = null;
		#endregion

		#region Constructors
		/// <summary>
		/// Creates a new construct from a CubeGrid
		/// </summary>
		/// <param name="source">The main cube grid</param>
		public MyJumpGateConstruct(IMyCubeGrid source, long fallback_id)
        {
            this.CubeGrid = source;
			this.PrimaryCubeGridCustomName = source?.CustomName;

            if (MyJumpGateModSession.Network.Registered)
			{
				MyJumpGateModSession.Network.On(MyPacketTypeEnum.UPDATE_GRID, this.OnNetworkJumpGateGridUpdate);
				MyJumpGateModSession.Network.On(MyPacketTypeEnum.STATICIFY_CONSTRUCT, this.OnStaticifyConstruct);
				MyJumpGateModSession.Network.On(MyPacketTypeEnum.COMM_LINKED, this.OnNetworkCommLinkedUpdate);
				MyJumpGateModSession.Network.On(MyPacketTypeEnum.BEACON_LINKED, this.OnNetworkBeaconLinkedUpdate);
			}

			if (source == null) this.Suspend(fallback_id);
			else this.SetupConstruct();
        }

		/// <summary>
		/// Creates a new construct from a serialized grid
		/// </summary>
		/// <param name="serialized">The serialized grid data</param>
		public MyJumpGateConstruct(MySerializedJumpGateConstruct serialized)
        {
            this.FromSerialized(serialized);

			if (MyJumpGateModSession.Network.Registered)
			{
				MyJumpGateModSession.Network.On(MyPacketTypeEnum.UPDATE_GRID, this.OnNetworkJumpGateGridUpdate);
				MyJumpGateModSession.Network.On(MyPacketTypeEnum.STATICIFY_CONSTRUCT, this.OnStaticifyConstruct);
				MyJumpGateModSession.Network.On(MyPacketTypeEnum.COMM_LINKED, this.OnNetworkCommLinkedUpdate);
				MyJumpGateModSession.Network.On(MyPacketTypeEnum.BEACON_LINKED, this.OnNetworkBeaconLinkedUpdate);
			}
        }
		#endregion

		#region "object" Methods
		public override bool Equals(object other)
		{
			return other != null && other is MyJumpGateConstruct && this.CubeGridID == ((MyJumpGateConstruct) other).CubeGridID;
		}

		/// <summary>
		/// The hashcode for this object
		/// </summary>
		/// <returns>The hashcode of this construct's main cube grid</returns>
		public override int GetHashCode()
		{
			return this.CubeGrid.GetHashCode();
		}
		#endregion

		#region Private Methods
		/// <summary>
		/// Callback triggered when this object receives a grid update packet
		/// </summary>
		/// <param name="packet">The update request packet</param>
		private void OnNetworkJumpGateGridUpdate(MyNetworkInterface.Packet packet)
		{
			if (packet == null || packet.EpochTime <= this.LastUpdateTime || !this.AtLeastOneUpdate()) return;
			MySerializedJumpGateConstruct serialized = packet.Payload<MySerializedJumpGateConstruct>();
			if (serialized == null || JumpGateUUID.FromGuid(serialized.UUID) != JumpGateUUID.FromJumpGateGrid(this)) return;

			if (MyNetworkInterface.IsMultiplayerServer && packet.PhaseFrame == 1)
            {
				if (serialized.IsClientRequest)
				{
					packet = packet.Forward(packet.SenderID, false);
					packet.Payload(this.ToSerialized(false));
					packet.Send();
				}
				else if (this.LastUpdateDateTimeUTC < packet.EpochDateTimeUTC && this.FromSerialized(serialized))
				{
					this.LastUpdateTime = packet.EpochTime;
					this.IsDirty = false;
					packet = packet.Forward(packet.SenderID, true);
					packet.Payload(serialized);
					packet.Send();
				}
			}
            else if (MyNetworkInterface.IsMultiplayerClient && (packet.PhaseFrame == 1 || packet.PhaseFrame == 2) && this.FromSerialized(serialized))
            {
                this.LastUpdateTime = packet.EpochTime;
                this.IsDirty = false;
				Logger.Debug($"SERVER_GRID_UPDATE -> {this.CubeGridID}", 3);
			}
        }

		/// <summary>
		/// Callback triggered when this object receives a comm-linked grids update request
		/// </summary>
		/// <param name="packet">The update request packet</param>
		private void OnNetworkCommLinkedUpdate(MyNetworkInterface.Packet packet)
		{
			if (packet == null || MyNetworkInterface.IsMultiplayerServer || packet.PhaseFrame != 2) return;
			KeyValuePair<long, List<long>> info = packet.Payload<KeyValuePair<long, List<long>>>();
			if (info.Key != this.CubeGridID) return;
			this.CommLinkedLock.AcquireWriter();

			try
			{
				if (this.CommLinkedGrids == null) this.CommLinkedGrids = new List<MyJumpGateConstruct>();
				else this.CommLinkedGrids.Clear();
				if (info.Value == null) return;

				foreach (long id in info.Value)
				{
					MyJumpGateConstruct grid = MyJumpGateModSession.Instance.GetJumpGateGrid(id);

					if (grid == null || grid.Closed || grid.MarkClosed)
					{
						MyJumpGateModSession.Instance.RequestGridDownload(id);
						Logger.Debug($" ... Got Comm-Linked grid - {grid}; NEEDS_DOWNLOAD", 5);
					}
					else
					{
						this.CommLinkedGrids.Add(grid);
						Logger.Debug($" ... Got Comm-Linked grid - {grid}; OK", 5);
					}
				}

				this.LastCommLinkUpdate = DateTime.Now;
			}
			finally
			{
				this.CommLinkedClientUpdate = false;
				this.CommLinkedLock.ReleaseWriter();
				Logger.Debug($"COMMLINKED_UPDATE -> {this.CubeGridID}; Got {this.CommLinkedGrids.Count} grid(s)", 3);
			}
		}

		/// <summary>
		/// Callback triggered when this object receives a beacon-linked grids update request
		/// </summary>
		/// <param name="packet">The update request packet</param>
		private void OnNetworkBeaconLinkedUpdate(MyNetworkInterface.Packet packet)
		{
			if (packet == null || MyNetworkInterface.IsMultiplayerServer || packet.PhaseFrame != 2) return;
			KeyValuePair<long, List<MyBeaconLinkWrapper>> info = packet.Payload<KeyValuePair<long, List<MyBeaconLinkWrapper>>>();
			if (info.Key != this.CubeGridID) return;
			this.BeaconLinkedLock.AcquireWriter();

			try
			{
				if (this.BeaconLinks == null) this.BeaconLinks = new List<MyBeaconLinkWrapper>();
				else this.BeaconLinks.Clear();
				if (info.Value == null) return;

				foreach (MyBeaconLinkWrapper beacon_link in info.Value)
				{
					IMyBeacon beacon = (IMyBeacon) MyAPIGateway.Entities.GetEntityById(beacon_link.BeaconID);
					if (beacon != null) this.BeaconLinks.Add(new MyBeaconLinkWrapper(beacon));
					else this.BeaconLinks.Add(beacon_link);
					Logger.Debug($"{beacon_link.BroadcastName}, {beacon_link.BeaconPosition}");
				}

				this.LastBeaconLinkUpdate = DateTime.Now;
			}
			finally
			{
				this.BeaconLinkedClientUpdate = false;
				this.BeaconLinkedLock.ReleaseWriter();
				Logger.Debug($"BEACONLINKED_UPDATE -> {this.CubeGridID}; Got {this.BeaconLinks.Count} beacon(s)", 3);
			}
		}

		/// <summary>
		/// Callback triggered when this object receives a grid staticify request
		/// </summary>
		/// <param name="packet">The network packet</param>
		private void OnStaticifyConstruct(MyNetworkInterface.Packet packet)
		{
			if (packet == null || this.Closed || this.IsSuspended || !this.AtLeastOneUpdate()) return;
			KeyValuePair<long, bool> payload = packet.Payload<KeyValuePair<long, bool>>();
			if (payload.Key != this.CubeGridID) return;

			if (MyNetworkInterface.IsMultiplayerServer && packet.PhaseFrame == 1)
			{
				if (payload.Value && this.CubeGrids.Any((pair) => pair.Value.Speed > 1e-3 || pair.Value.Physics.AngularVelocity.Length() > 1e-3)) return;
				this.SetConstructStaticness(payload.Value);
				MyNetworkInterface.Packet response = packet.Forward(packet.SenderID, true);
				packet.Send();
			}
			else if (MyNetworkInterface.IsMultiplayerClient && (packet.PhaseFrame == 1 || packet.PhaseFrame == 2) && !this.Closed)
			{
				this.SetConstructStaticness(payload.Value);
			}
		}

        /// <summary>
        /// If server: Transmits this grid to all clients
        /// </summary>
        private void SendNetworkGridUpdate(bool closed = false)
        {
            if (!MyNetworkInterface.IsMultiplayerServer || this.CubeGrid == null) return;

            if (closed || this.Closed)
            {
				MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet {
					PacketType = MyPacketTypeEnum.CLOSE_GRID,
					TargetID = 0,
					Broadcast = true,
				};

				packet.Payload(this.CubeGrid.EntityId);
				packet.Send();
			}
            else
            {
				MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet {
					PacketType = MyPacketTypeEnum.UPDATE_GRID,
					TargetID = 0,
					Broadcast = true,
				};

				packet.Payload(this.ToSerialized(false));
				packet.Send();
			}

            this.IsDirty = false;
            if (this.JumpGates != null) foreach (KeyValuePair<long, MyJumpGate> pair in this.JumpGates) pair.Value.IsDirty = false;
			Logger.Debug($"[{this.CubeGridID}]: Sent Network Grid Update", 4);
		}

        /// <summary>
        /// Callback for when a block is added to this construct
        /// </summary>
        /// <param name="block">The added block</param>
        private void OnBlockAdded(IMySlimBlock block)
		{
			if (this.MarkClosed || this.Closed) return;
			this.GridBlocks?.Add(block);
			this.UpdateDriveMaxRaycastDistances = true;
			if (block.FatBlock == null || block.FatBlock.MarkedForClose || block.FatBlock.Closed) return;
			
			if (block.FatBlock is IMyRadioAntenna)
            {
				IMyRadioAntenna fat_block = block.FatBlock as IMyRadioAntenna;
                if (!this.RadioAntennas.TryAdd(fat_block.EntityId, fat_block)) this.RadioAntennas[fat_block.EntityId] = fat_block;
				this.SetDirty();
			}
			else if (block.FatBlock is IMyLaserAntenna)
			{
				IMyLaserAntenna fat_block = block.FatBlock as IMyLaserAntenna;
				if (!this.LaserAntennas.TryAdd(fat_block.EntityId, fat_block)) this.LaserAntennas[fat_block.EntityId] = fat_block;
				this.SetDirty();
			}
			else if (block.FatBlock is IMyBeacon)
			{
				IMyBeacon fat_block = block.FatBlock as IMyBeacon;
				if (!this.BeaconAntennas.TryAdd(fat_block.EntityId, fat_block)) this.BeaconAntennas[fat_block.EntityId] = fat_block;
				this.SetDirty();
			}
			else if (block.FatBlock is IMyUpgradeModule)
            {
				IMyUpgradeModule fat_block = block.FatBlock as IMyUpgradeModule;
				MyCubeBlockBase block_base;
                if ((block_base = MyJumpGateModSession.GetBlockAsJumpGateCapacitor(fat_block)) != null)
                {
                    MyJumpGateCapacitor capacitor = block_base as MyJumpGateCapacitor;
					if (!this.JumpGateCapacitors.TryAdd(fat_block.EntityId, capacitor)) this.JumpGateCapacitors[fat_block.EntityId] = capacitor;
				}
				else if ((block_base = MyJumpGateModSession.GetBlockAsJumpGateController(fat_block)) != null)
                {
					MyJumpGateController controller = block_base as MyJumpGateController;
					if (!this.JumpGateControllers.TryAdd(fat_block.EntityId, controller)) this.JumpGateControllers[fat_block.EntityId] = controller;
				}
				else if ((block_base = MyJumpGateModSession.GetBlockAsJumpGateDrive(fat_block)) != null)
                {
					MyJumpGateDrive drive = block_base as MyJumpGateDrive;

					if (!this.JumpGateDrives.TryAdd(fat_block.EntityId, drive))
					{
						MyJumpGateDrive old_drive = this.JumpGateDrives[fat_block.EntityId];
						lock (this.UpdateLock) this.DriveCombinations?.RemoveAll((pair) => pair.Key == old_drive || pair.Value == old_drive);
						this.JumpGateDrives[fat_block.EntityId] = drive;
					}

					lock (this.UpdateLock)
					{
						foreach (KeyValuePair<long, MyJumpGateDrive> pair in this.JumpGateDrives)
						{
							if (drive != pair.Value)
							{
								this.DriveCombinations?.Add(new KeyValuePair<MyJumpGateDrive, MyJumpGateDrive>(drive, pair.Value));
							}
						}
					}

					this.MarkUpdateJumpGates = true;
				}
				else if ((block_base = MyJumpGateModSession.GetBlockAsJumpGateServerAntenna(fat_block)) != null)
                {
					MyJumpGateServerAntenna antenna = block_base as MyJumpGateServerAntenna;
					if (!this.JumpGateServerAntennas.TryAdd(fat_block.EntityId, antenna)) this.JumpGateServerAntennas[fat_block.EntityId] = antenna;
				}

                if (MyJumpGateModSession.IsBlockCubeBlockBase(fat_block)) this.SetDirty();
			}
		}

		/// <summary>
		/// Callback for when a block is removed from this construct
		/// </summary>
		/// <param name="block">The removed block</param>
		private void OnBlockRemoved(IMySlimBlock block)
		{
			this.UpdateDriveMaxRaycastDistances = true;
			if (block.FatBlock == null || this.MarkClosed || this.Closed) return;
            this.RadioAntennas.Remove(block.FatBlock.EntityId);
            this.LaserAntennas.Remove(block.FatBlock.EntityId);
			this.BeaconAntennas.Remove(block.FatBlock.EntityId);
			this.GridBlocks.Remove(block);

			if (block.FatBlock is IMyUpgradeModule)
            {
				MyJumpGateDrive drive;
                this.JumpGateCapacitors.Remove(block.FatBlock.EntityId);
				this.JumpGateControllers.Remove(block.FatBlock.EntityId);
				this.JumpGateServerAntennas.Remove(block.FatBlock.EntityId);

                if (this.JumpGateDrives.TryRemove(block.FatBlock.EntityId, out drive))
				{
					this.MarkUpdateJumpGates = true;
					lock (this.UpdateLock) this.DriveCombinations?.RemoveAll((pair) => pair.Key == drive || pair.Value == drive);
				}

				this.SetDirty();
			}
        }

        /// <summary>
        /// Callback for when a grid on this grid is merged with another grid
        /// </summary>
        /// <param name="_1">The first grid</param>
        /// <param name="_2">The second grid</param>
        private void OnGridMerged(IMyCubeGrid _1, IMyCubeGrid _2)
        {
            if (this.MarkClosed) return;
            this.SetupConstruct();
			this.SetDirty();
		}

		/// <summary>
		/// Callback for when a grid on this grid is split into another grid
		/// </summary>
		/// <param name="_1">The first grid</param>
		/// <param name="_2">The second grid</param>
		private void OnGridSplit(IMyCubeGrid _1, IMyCubeGrid _2)
		{
			if (this.MarkClosed) return;
            this.SetupConstruct();
			this.SetDirty();
		}

        /// <summary>
        /// Callback for when a grid is added to this construct
        /// </summary>
        /// <param name="grid">The added grid</param>
        private void OnGridAdded(IMyCubeGrid grid)
		{
			if (grid == null) return;
			grid.OnBlockAdded += this.OnBlockAdded;
            grid.OnBlockRemoved += this.OnBlockRemoved;
            grid.OnGridMerge += this.OnGridMerged;
            grid.OnGridSplit += this.OnGridSplit;
			this.SetDirty();
        }

		/// <summary>
		/// Callback for when a grid is removed from this construct
		/// </summary>
		/// <param name="grid">The removed grid</param>
		private void OnGridRemoved(IMyCubeGrid grid)
        {
            if (grid == null) return;
			grid.OnBlockAdded -= this.OnBlockAdded;
			grid.OnBlockRemoved -= this.OnBlockRemoved;
			grid.OnGridMerge -= this.OnGridMerged;
			grid.OnGridSplit -= this.OnGridSplit;

            if (!this.Closed)
			{
				foreach (KeyValuePair<long, IMyRadioAntenna> pair in this.RadioAntennas) if (pair.Value.CubeGrid == grid) this.RadioAntennas.Remove(pair.Value.EntityId);
				foreach (KeyValuePair<long, IMyLaserAntenna> pair in this.LaserAntennas) if (pair.Value.CubeGrid == grid) this.LaserAntennas.Remove(pair.Value.EntityId);
				foreach (KeyValuePair<long, MyJumpGateCapacitor> pair in this.JumpGateCapacitors) if (pair.Value.CubeGridID == grid.EntityId) this.JumpGateCapacitors.Remove(pair.Value.BlockID);
				foreach (KeyValuePair<long, MyJumpGateController> pair in this.JumpGateControllers) if (pair.Value.CubeGridID == grid.EntityId) this.JumpGateControllers.Remove(pair.Value.BlockID);
				foreach (KeyValuePair<long, MyJumpGateDrive> pair in this.JumpGateDrives) if (pair.Value.CubeGridID == grid.EntityId) this.JumpGateDrives.Remove(pair.Value.BlockID);
				foreach (KeyValuePair<long, MyJumpGateServerAntenna> pair in this.JumpGateServerAntennas) if (pair.Value.CubeGridID == grid.EntityId) this.JumpGateServerAntennas.Remove(pair.Value.BlockID);
			}

            MyJumpGateConstruct parent = MyJumpGateModSession.Instance.GetJumpGateGrid(grid);
            if (parent == null && (MyJumpGateModSession.SessionStatus == MySessionStatusEnum.LOADING || MyJumpGateModSession.SessionStatus == MySessionStatusEnum.RUNNING)) parent = MyJumpGateModSession.Instance.AddCubeGridToSession(grid);
			this.SetDirty();
		}

        /// <summary>
        /// Sets up the new construct<br />
        /// Checks grids within this group, updates connected blocks, and updates the main controller
        /// </summary>
        /// <param name="grids">The cube grids to use instead of the list from this grid group</param>
        private void SetupConstruct(IEnumerable<IMyCubeGrid> grids = null)
        {
			if (this.Closed || this.MarkClosed) return;
			List<IMyCubeGrid> construct = (grids == null) ? new List<IMyCubeGrid>() : new List<IMyCubeGrid>(grids);
			
			if (this.CubeGrid == null || this.CubeGrid.MarkedForClose || this.CubeGrid.Closed)
            {
				this.CubeGrid = null;
                if (this.CubeGrids == null) return;
				foreach (KeyValuePair<long, IMyCubeGrid> pair in this.CubeGrids) this.OnGridRemoved(pair.Value);
				foreach (KeyValuePair<long, MyJumpGate> pair in this.JumpGates) pair.Value.Dispose();
				this.CubeGrids.Clear();
				this.MarkClosed = true;
				return;
            }
			
			if (construct.Count == 0) this.CubeGrid.GetGridGroup(GridLinkTypeEnum.Logical)?.GetGrids(construct);

			if (construct.Count == 0)
			{
				this.CubeGrid = null;
				this.CubeGrids.Clear();
				this.MarkClosed = true;
				foreach (KeyValuePair<long, MyJumpGate> pair in this.JumpGates) pair.Value.Dispose();
				return;
			}

            bool grids_updated = false;
			
			foreach (IMyCubeGrid grid in construct)
            {
				if (this.CubeGrids.TryAdd(grid.EntityId, grid))
                {
					this.OnGridAdded(grid);
                    grids_updated = true;
				}
			}
			
			foreach (KeyValuePair<long, IMyCubeGrid> closed in this.CubeGrids)
            {
				if (construct.Contains(closed.Value)) continue;
                this.OnGridRemoved(closed.Value);
                this.CubeGrids.Remove(closed.Key);
                grids_updated = true;
			}
			
			if (grids_updated)
            {
				// Update connected blocks
				this.UpdateDriveMaxRaycastDistances = true;
				int drive_count = this.JumpGateDrives.Count;
				this.GridBlocks.Clear();
				
				foreach (KeyValuePair<long, MyJumpGateController> pair in this.JumpGateControllers) if (pair.Value.IsClosed() || !this.CubeGrids.ContainsKey(pair.Value.CubeGridID)) this.JumpGateControllers.Remove(pair.Key);
				foreach (KeyValuePair<long, MyJumpGateCapacitor> pair in this.JumpGateCapacitors) if (pair.Value.IsClosed() || !this.CubeGrids.ContainsKey(pair.Value.CubeGridID)) this.JumpGateCapacitors.Remove(pair.Key);
				foreach (KeyValuePair<long, MyJumpGateDrive> pair in this.JumpGateDrives) if (pair.Value.IsClosed() || !this.CubeGrids.ContainsKey(pair.Value.CubeGridID)) this.JumpGateDrives.Remove(pair.Key);
				foreach (KeyValuePair<long, MyJumpGateServerAntenna> pair in this.JumpGateServerAntennas) if (pair.Value.IsClosed() || !this.CubeGrids.ContainsKey(pair.Value.CubeGridID)) this.JumpGateServerAntennas.Remove(pair.Key);
				foreach (KeyValuePair<long, IMyLaserAntenna> pair in this.LaserAntennas) if (pair.Value.Closed || pair.Value.MarkedForClose || !this.CubeGrids.ContainsKey(pair.Value.CubeGrid.EntityId)) this.LaserAntennas.Remove(pair.Key);
				foreach (KeyValuePair<long, IMyRadioAntenna> pair in this.RadioAntennas) if (pair.Value.Closed || pair.Value.MarkedForClose || !this.CubeGrids.ContainsKey(pair.Value.CubeGrid.EntityId)) this.RadioAntennas.Remove(pair.Key);
				foreach (KeyValuePair<long, IMyBeacon> pair in this.BeaconAntennas) if (pair.Value.Closed || pair.Value.MarkedForClose || !this.CubeGrids.ContainsKey(pair.Value.CubeGrid.EntityId)) this.BeaconAntennas.Remove(pair.Key);
				
				foreach (KeyValuePair<long, IMyCubeGrid> pair in this.CubeGrids)
				{
					foreach (IMyUpgradeModule block in pair.Value.GetFatBlocks<IMyUpgradeModule>())
					{
						if (block.MarkedForClose || block.Closed) continue;
						MyCubeBlockBase block_base;
						if ((block_base = MyJumpGateModSession.GetBlockAsJumpGateCapacitor(block)) != null) this.JumpGateCapacitors.AddOrUpdate(block.EntityId, block_base as MyJumpGateCapacitor, (_1, _2) => block_base as MyJumpGateCapacitor);
						else if ((block_base = MyJumpGateModSession.GetBlockAsJumpGateController(block)) != null) this.JumpGateControllers.AddOrUpdate(block.EntityId, block_base as MyJumpGateController, (_1, _2) => block_base as MyJumpGateController);
						else if ((block_base = MyJumpGateModSession.GetBlockAsJumpGateDrive(block)) != null) this.JumpGateDrives.AddOrUpdate(block.EntityId, block_base as MyJumpGateDrive, (_1, _2) => block_base as MyJumpGateDrive);
						else if ((block_base = MyJumpGateModSession.GetBlockAsJumpGateServerAntenna(block)) != null) this.JumpGateServerAntennas.AddOrUpdate(block.EntityId, block_base as MyJumpGateServerAntenna, (_1, _2) => block_base as MyJumpGateServerAntenna);
					}
					
					foreach (IMyLaserAntenna antenna in pair.Value.GetFatBlocks<IMyLaserAntenna>()) if (!antenna.MarkedForClose && !antenna.Closed) this.LaserAntennas.AddOrUpdate(antenna.EntityId, antenna, (_1, _2) => antenna);
					foreach (IMyRadioAntenna antenna in pair.Value.GetFatBlocks<IMyRadioAntenna>()) if (!antenna.MarkedForClose && !antenna.Closed) this.RadioAntennas.AddOrUpdate(antenna.EntityId, antenna, (_1, _2) => antenna);
					foreach (IMyBeacon antenna in pair.Value.GetFatBlocks<IMyBeacon>()) if (!antenna.MarkedForClose && !antenna.Closed) this.BeaconAntennas.AddOrUpdate(antenna.EntityId, antenna, (_1, _2) => antenna);
					
					this.GridBlocks.AddRange(((MyCubeGrid) pair.Value).CubeBlocks);	
				}

				if (drive_count != this.JumpGateDrives.Count || this.JumpGateDrives.Any((pair) => pair.Value.JumpGateGrid != this))
				{
					this.MarkUpdateJumpGates = true;
					this.DriveCombinations = null;
				}
				
				this.SetDirty();
			}
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Closes this construct, unregisters handlers, and frees all resources<br />
		/// If client: Does nothing<br />
		/// To be called only by session
		/// </summary>
		/// <param name="override_client">If true, will close grid on clients instead of doing nothing</param>
		public void Close(bool override_client = false)
		{
			if (this.Closed || (!override_client && MyNetworkInterface.IsStandaloneMultiplayerClient && this.CubeGrid != null && MyJumpGateModSession.Instance.HasCubeGrid(this.CubeGridID))) return;
			MyGridInvalidationReason reason = this.GetInvalidationReason();
			this.SendNetworkGridUpdate(true);
			string name = this.CubeGrid?.CustomName ?? "N/A";

			foreach (KeyValuePair<long, MyJumpGate> pair in this.JumpGates)
			{
				if (!pair.Value.Closed)
				{
					pair.Value.Dispose();
					MyJumpGateModSession.Instance.CloseGate(pair.Value);
				}
			}

			foreach (KeyValuePair<long, IMyCubeGrid> pair in this.CubeGrids) this.OnGridRemoved(pair.Value);
			this.NextJumpGateID = 0;
			this.Release();

			this.ClosedJumpGateIDs.Clear();
			this.JumpGates.Clear();
			this.JumpGateDrives.Clear();
			this.JumpGateCapacitors.Clear();
			this.JumpGateControllers.Clear();
			this.JumpGateServerAntennas.Clear();
			this.CubeGrids.Clear();
			this.DriveCombinations?.Clear();
			this.GridBlocks.Clear();

			this.ClosedJumpGateIDs = null;
			this.JumpGates = null;
			this.JumpGateDrives = null;
			this.JumpGateCapacitors = null;
			this.JumpGateControllers = null;
			this.UpdateTimeTicks = null;
			this.CubeGrids = null;
			this.DriveCombinations = null;
			this.GridBlocks = null;

			this.BatchingGate = null;
			this.CubeGrid = null;
			this.MarkClosed = true;
			this.Closed = true;

			Logger.Debug($"Jump Gate Grid \"{name}\" ({this.CubeGridID}) Closed - {reason}; SESSION_STATUS={MyJumpGateModSession.SessionStatus}", 1);
		}

		/// <summary>
		/// Does one update of this construct, ticking all associated jump gates<br />
		/// This will close this construct if:<br />
		///  ... This construct is marked for close<br />
		///  ... The session is not running<br />
		///  ... This construct is not valid<br />
		///  ... This construct's main grid is not the true main grid
		/// </summary>
		public void Update()
		{
			if (this.Closed) return;
			MyGridInvalidationReason reason;

			// Check update validity
			if (this.IsSuspended && this.CubeGrid != null)
			{
				this.CubeGrids?.Clear();
				this.CubeGrid = null;
				return;
			}
			
			if (this.IsSuspended || this.CubeGrid == null || !Monitor.TryEnter(this.UpdateLock)) return;
			string grid_id = this.CubeGridID.ToString();
			//Logger.Debug($"[{grid_id}] - UPDATE_START", 4);
			Stopwatch sw = new Stopwatch();
			sw.Start();
			
			try
			{
				//Logger.Debug($"[{grid_id}] - IN_SCENE={this.CubeGrid?.InScene ?? false}", 1);
				if (MyJumpGateModSession.SessionStatus != MySessionStatusEnum.RUNNING)
				{
					MyJumpGateModSession.Instance.CloseGrid(this);
					this.SendNetworkGridUpdate();
					Logger.Debug($"[{grid_id}]] - Session is not running ({MyJumpGateModSession.SessionStatus}); CLOSED", 2);
					return;
				}
				else if ((reason = this.GetInvalidationReason()) != MyGridInvalidationReason.NONE)
				{
					MyJumpGateModSession.Instance.CloseGrid(this);
					this.SendNetworkGridUpdate();
					Logger.Debug($"[{grid_id}]] - Grid is not valid ({reason}); CLOSED", 2);
					return;
				}
				
				// Main Grid Check
				{
					this.SetupConstruct();
					IMyCubeGrid main_grid = this.GetMainCubeGrid();

					if (main_grid != this.CubeGrid && !MyJumpGateModSession.Instance.HasCubeGrid(main_grid.EntityId))
					{
						if (MyJumpGateModSession.Instance.MoveGrid(this, main_grid.EntityId))
						{
							this.CubeGrid = main_grid;
							this.SuspendedCubeGridID = main_grid.EntityId;
							this.MarkUpdateJumpGates = true;
							this.SetupConstruct();
							Logger.Debug($"[{grid_id}]] - Main grid changed -> {main_grid.EntityId}", 2);
							grid_id = main_grid.EntityId.ToString();
						}
						else
						{
							MyJumpGateModSession.Instance.CloseGrid(this, true);
							Logger.Debug($"[{grid_id}]] - Grid is not Main Grid @ {main_grid.EntityId}; CLOSED", 2);
							return;
						}
					}
					else if (main_grid != this.CubeGrid)
					{
						MyJumpGateModSession.Instance.CloseGrid(this, true);
						this.SendNetworkGridUpdate();
						Logger.Debug($"[{grid_id}]] - Grid is not Main Grid @ {main_grid.EntityId}; CLOSED", 2);
						return;
					}
					else if (MyJumpGateModSession.Instance.HasDuplicateGrid(this))
					{
						Logger.Debug($"[{grid_id}]] - Grid duplicate exists; UPDATE_SKIPPED", 2);
						return;
					}
				}
				
				// Initialize update
				bool first_update = this.LocalGameTick == 0;
				this.LocalGameTick = (this.LocalGameTick + 1) % 0xFFFFFFFFFFFFFFF0;
				bool full_gate_update = this.MarkUpdateJumpGates || first_update;
				bool gate_entity_update = this.LocalGameTick % 30 == 0;
				this.MarkUpdateJumpGates = false;
				bool is_dirty = this.IsDirty || full_gate_update;
				
				// Update drive combinations
				if (this.DriveCombinations == null)
				{
					List<MyJumpGateDrive> indexable_large_drives = new List<MyJumpGateDrive>(this.JumpGateDrives.Select((pair) => pair.Value).Where((drive) => drive.IsLargeGrid));
					List<MyJumpGateDrive> indexable_small_drives = new List<MyJumpGateDrive>(this.JumpGateDrives.Select((pair) => pair.Value).Where((drive) => drive.IsSmallGrid));
					this.DriveCombinations = new List<KeyValuePair<MyJumpGateDrive, MyJumpGateDrive>>();

					for (int i = 0; i < indexable_large_drives.Count - 1; ++i)
					{
						for (int j = i + 1; j < indexable_large_drives.Count; ++j)
						{
							this.DriveCombinations.Add(new KeyValuePair<MyJumpGateDrive, MyJumpGateDrive>(indexable_large_drives[i], indexable_large_drives[j]));
						}
					}

					for (int i = 0; i < indexable_small_drives.Count - 1; ++i)
					{
						for (int j = i + 1; j < indexable_small_drives.Count; ++j)
						{
							this.DriveCombinations.Add(new KeyValuePair<MyJumpGateDrive, MyJumpGateDrive>(indexable_small_drives[i], indexable_small_drives[j]));
						}
					}
				}
				
				// Update drive max raycast distances
				if (this.UpdateDriveMaxRaycastDistances && this.LocalGameTick % 30 == 0)
				{
					List<Vector3I> raycast_cells = new List<Vector3I>();
					this.UpdateDriveMaxRaycastDistances = false;

					foreach (KeyValuePair<long, MyJumpGateDrive> drive in this.JumpGateDrives)
					{
						double closest_distance = drive.Value.DriveConfiguration.DriveRaycastDistance;
						Vector3D raycast_start = drive.Value.GetDriveRaycastStartpoint();
						Vector3D raycast_end = drive.Value.GetDriveRaycastEndpoint(closest_distance);

						foreach (KeyValuePair<long, IMyCubeGrid> grid in this.CubeGrids)
						{
							if (grid.Value.MarkedForClose) continue;
							grid.Value.RayCastCells(raycast_start, raycast_end, raycast_cells);

							foreach (Vector3I cell in raycast_cells)
							{
								IMySlimBlock block = grid.Value.GetCubeBlock(cell);
								if (block == null || block.FatBlock == drive.Value.TerminalBlock) continue;
								closest_distance = Math.Min(closest_distance, Vector3D.Distance(grid.Value.GridIntegerToWorld(cell), raycast_start));
							}

							raycast_cells.Clear();
						}

						this.MarkUpdateJumpGates = this.MarkUpdateJumpGates || drive.Value.MaxRaycastDistance != closest_distance;
						drive.Value.MaxRaycastDistance = closest_distance;
					}
				}

				// Rebuild all jump gates
				if (MyNetworkInterface.IsServerLike && full_gate_update)
				{
					Dictionary<MyJumpGateDrive, IntersectionInfo> intersecting_drives = new Dictionary<MyJumpGateDrive, IntersectionInfo>();

					foreach (KeyValuePair<MyJumpGateDrive, MyJumpGateDrive> combination in this.DriveCombinations)
					{
						MyJumpGateDrive drive1 = combination.Key;
						MyJumpGateDrive drive2 = combination.Value;

						Vector3D drive1_pos1 = drive1.GetDriveRaycastStartpoint();
						Vector3D drive1_pos2 = drive1.GetDriveRaycastEndpoint(drive1.MaxRaycastDistance);
						Vector3D side_a = drive1_pos2 - drive1_pos1;

						Vector3D drive2_pos1 = drive2.GetDriveRaycastStartpoint();
						Vector3D drive2_pos2 = drive2.GetDriveRaycastEndpoint(drive2.MaxRaycastDistance);
						Vector3D side_b = drive2_pos2 - drive2_pos1;
						Vector3 side_c = drive2_pos1 - drive1_pos1;

						double angle_a = Vector3D.Angle(side_b, -side_c);
						double angle_b = Vector3D.Angle(side_a, side_c);
						double ico_angle = angle_a + angle_b;
						double angle_c = Math.PI - ico_angle;
						double c_ratio = (side_c.Length() / Math.Sin(angle_c));

						if (ico_angle == 0)
						{
							Vector3D midpoint = (drive1_pos2 + drive2_pos2) / 2d;
							if (Vector3D.Distance(drive1_pos1, midpoint) > drive1.DriveConfiguration.DriveRaycastWidth || Vector3D.Distance(drive2_pos1, midpoint) > drive1.DriveConfiguration.DriveRaycastWidth) continue;

							IntersectionInfo intersection;
							bool drive1_contained = intersecting_drives.ContainsKey(drive1);
							bool drive2_contained = intersecting_drives.ContainsKey(drive2);

							if (drive1_contained && drive2_contained)
							{
								intersection = intersecting_drives[drive1];
								IntersectionInfo _intersection = intersecting_drives[drive2];

								if (intersection != _intersection)
								{
									intersection.IntersectingDrives.AddList(_intersection.IntersectingDrives);
									intersection.IntersectNodes.AddList(_intersection.IntersectNodes);
									intersection.IntersectingDrives = intersection.IntersectingDrives.Distinct().ToList();
									intersection.IntersectNodes = intersection.IntersectNodes.Distinct().ToList();
									intersecting_drives[drive2] = intersection;
								}
							}
							else if (drive1_contained)
							{
								intersection = intersecting_drives[drive1];
								intersecting_drives.Add(drive2, intersection);
							}
							else if (drive2_contained)
							{
								intersection = intersecting_drives[drive2];
								intersecting_drives.Add(drive1, intersection);
							}
							else
							{
								intersection = new IntersectionInfo();
								intersecting_drives.Add(drive1, intersection);
								intersecting_drives.Add(drive2, intersection);
							}

							if (!intersection.IntersectingDrives.Contains(drive1)) intersection.IntersectingDrives.Add(drive1);
							if (!intersection.IntersectingDrives.Contains(drive2)) intersection.IntersectingDrives.Add(drive2);
							if (!intersection.IntersectNodes.Contains(midpoint)) intersection.IntersectNodes.Add(midpoint);
						}
						else if (ico_angle < Math.PI)
						{
							double side_a_len = Math.Sin(angle_a) * c_ratio;
							double side_b_len = Math.Sin(angle_b) * c_ratio;
							if (side_a_len > drive2.MaxRaycastDistance || side_b_len > drive1.MaxRaycastDistance) continue;
							Vector3D _side_a = drive1.GetDriveRaycastEndpoint(side_a_len);
							Vector3D _side_b = drive2.GetDriveRaycastEndpoint(side_b_len);
							Vector3D midpoint = (_side_a + _side_b) / 2d;
							if (Vector3D.Distance(_side_a, midpoint) > drive1.DriveConfiguration.DriveRaycastWidth || Vector3D.Distance(_side_b, midpoint) > drive1.DriveConfiguration.DriveRaycastWidth) continue;

							IntersectionInfo intersection;
							bool drive1_contained = intersecting_drives.ContainsKey(drive1);
							bool drive2_contained = intersecting_drives.ContainsKey(drive2);

							if (drive1_contained && drive2_contained)
							{
								intersection = intersecting_drives[drive1];
								IntersectionInfo _intersection = intersecting_drives[drive2];

								if (intersection != _intersection)
								{
									intersection.IntersectingDrives.AddList(_intersection.IntersectingDrives);
									intersection.IntersectNodes.AddList(_intersection.IntersectNodes);
									intersection.IntersectingDrives = intersection.IntersectingDrives.Distinct().ToList();
									intersection.IntersectNodes = intersection.IntersectNodes.Distinct().ToList();
									intersecting_drives[drive2] = intersection;
								}
							}
							else if (drive1_contained)
							{
								intersection = intersecting_drives[drive1];
								intersecting_drives.Add(drive2, intersection);
							}
							else if (drive2_contained)
							{
								intersection = intersecting_drives[drive2];
								intersecting_drives.Add(drive1, intersection);
							}
							else
							{
								intersection = new IntersectionInfo();
								intersecting_drives.Add(drive1, intersection);
								intersecting_drives.Add(drive2, intersection);
							}

							if (!intersection.IntersectingDrives.Contains(drive1)) intersection.IntersectingDrives.Add(drive1);
							if (!intersection.IntersectingDrives.Contains(drive2)) intersection.IntersectingDrives.Add(drive2);
							if (!intersection.IntersectNodes.Contains(midpoint)) intersection.IntersectNodes.Add(midpoint);
						}
					}

					uint gate_count = MyJumpGateModSession.Configuration.ConstructConfiguration.MaxTotalGatesPerConstruct;
					uint large_gate_count = MyJumpGateModSession.Configuration.ConstructConfiguration.MaxLargeGatesPerConstruct;
					uint small_gate_count = MyJumpGateModSession.Configuration.ConstructConfiguration.MaxSmallGatesPerConstruct;
					List<MyJumpGateDrive> unmapped_drives = new List<MyJumpGateDrive>(this.JumpGateDrives.Select((pair) => pair.Value));
					List<long> mapped_gate_ids = new List<long>(this.JumpGates.Count);

					foreach (IntersectionInfo intersection_info in intersecting_drives.Select((pair) => pair.Value).Distinct())
					{
						List<MyJumpGateDrive> drive_group = intersection_info.IntersectingDrives.Distinct().ToList();
						if (drive_group.Count < 2) continue;
						bool is_large_grid = drive_group.First().IsLargeGrid;
						List<Vector3D> node_group = intersection_info.IntersectNodes.Distinct().ToList();
						unmapped_drives.RemoveAll(drive_group.Contains);
						Vector3D jump_node = Vector3D.Zero;
						foreach (Vector3D node in node_group) jump_node += node;
						jump_node /= node_group.Count;
						IEnumerable<long> primary_ids = drive_group.Select((drive) => drive.JumpGateID).Where((id) => id >= 0 && !mapped_gate_ids.Contains(id)).GroupBy(id => id).OrderByDescending(grp => grp.Count()).Select(grp => grp.Key);
						long primary_id = (primary_ids.Any()) ? primary_ids.First() : ((this.ClosedJumpGateIDs.Count > 0) ? this.ClosedJumpGateIDs.Dequeue() : this.NextJumpGateID++);
						mapped_gate_ids.Add(primary_id);
						if ((gate_count > 0 && primary_id >= gate_count) || (large_gate_count > 0 && is_large_grid && primary_id >= large_gate_count) || (small_gate_count > 0 && !is_large_grid && primary_id >= small_gate_count)) continue;
						MyJumpGate jump_gate;

						if (this.JumpGates.TryGetValue(primary_id, out jump_gate) && !jump_gate.MarkClosed)
						{
							jump_gate.ConstructMatrix = this.CubeGrid.WorldMatrix;
							jump_gate.WorldJumpNode = jump_node;
							jump_gate.UpdateDriveIntersectNodes(node_group);
							jump_gate.SetColliderDirty();
							jump_gate.SetJumpSpaceEllipsoidDirty();
						}
						else
						{
							jump_gate = new MyJumpGate(this, primary_id, jump_node, node_group);
							this.JumpGates.TryAdd(primary_id, jump_gate);
						}

						foreach (MyJumpGateDrive drive in drive_group) drive.SetAttachedJumpGate(jump_gate);
						jump_gate.Init();
					}

					foreach (MyJumpGateDrive unmapped in unmapped_drives) unmapped.SetAttachedJumpGate(null);
					this.SetDirty();
				}
				
				// Update jump gates
				foreach (KeyValuePair<long, MyJumpGate> pair in this.JumpGates)
				{
					long jump_gate_id = pair.Key;
					MyJumpGate jump_gate = pair.Value;

					if (jump_gate.Closed)
					{
						this.ClosedJumpGateIDs.Enqueue(jump_gate_id);
						this.JumpGates.Remove(jump_gate_id);
						continue;
					}
					else if (jump_gate.MarkClosed)
					{
						MyJumpGateModSession.Instance.CloseGate(jump_gate);
						continue;
					}

						jump_gate.Update(full_gate_update, gate_entity_update);
					if (!jump_gate.MarkClosed && (!jump_gate.IsValid() || jump_gate.JumpGateGrid != this)) jump_gate.Dispose();
				}
				
				// Update comm linked grids
				if (this.UpdateCommLinkedGrids && MyNetworkInterface.IsServerLike)
				{
					using (this.CommLinkedLock.WithWriter())
					{
						int list_index = 0;
						if (this.CommLinkedGrids == null) this.CommLinkedGrids = new List<MyJumpGateConstruct>();
						else this.CommLinkedGrids.Clear();
						this.CommLinkedGrids.Add(this);
						List<MyEntity> broadcast_entities = new List<MyEntity>();

						while (list_index < this.CommLinkedGrids.Count)
						{
							MyJumpGateConstruct grid = this.CommLinkedGrids[list_index++];
							if (grid.Closed || grid.MarkClosed || (grid.LaserAntennas.Count == 0 && grid.RadioAntennas.Count == 0)) continue;

							foreach (KeyValuePair<long, IMyLaserAntenna> pair in grid.LaserAntennas)
							{
								if (pair.Value.MarkedForClose || !pair.Value.IsWorking || pair.Value.Status != Sandbox.ModAPI.Ingame.MyLaserAntennaStatus.Connected) continue;
								MyJumpGateConstruct other = MyJumpGateModSession.Instance.GetJumpGateGrid(pair.Value.Other.CubeGrid);
								if (other != null && !this.CommLinkedGrids.Contains(other)) this.CommLinkedGrids.Add(other);
							}

							if (grid.RadioAntennas.Count == 0 || grid.RadioAntennas.All((pair) => pair.Value.MarkedForClose || !pair.Value.IsWorking || !pair.Value.IsBroadcasting)) continue;
							BoundingBoxD world_aabb = grid.GetCombinedAABB();
							BoundingSphereD broadcast_sphere = new BoundingSphereD(world_aabb.Center, 50000 + world_aabb.Extents.Max());
							MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref broadcast_sphere, broadcast_entities);

							foreach (KeyValuePair<long, IMyRadioAntenna> pair in grid.RadioAntennas)
							{
								if (pair.Value.MarkedForClose || !pair.Value.IsWorking || !pair.Value.IsBroadcasting) continue;

								foreach (MyEntity entity in broadcast_entities)
								{
									IMyCubeGrid target = (entity is IMyCubeGrid) ? (MyCubeGrid) entity : null;
									MyJumpGateConstruct target_grid = (target == null) ? null : MyJumpGateModSession.Instance.GetJumpGateGrid(target);
									if (entity.Physics == null || target_grid == null || target_grid == this || this.CommLinkedGrids.Contains(target_grid)) continue;

									foreach (KeyValuePair<long, IMyRadioAntenna> target_pair in grid.RadioAntennas)
									{
										if (target_pair.Value.IsWorking && target_pair.Value.IsBroadcasting && Vector3D.Distance(pair.Value.WorldMatrix.Translation, target_pair.Value.WorldMatrix.Translation) < Math.Min(pair.Value.Radius, target_pair.Value.Radius))
										{
											this.CommLinkedGrids.Add(target_grid);
											break;
										}
									}
								}
							}

							broadcast_entities.Clear();
						}

						this.LastCommLinkUpdate = DateTime.Now;
						this.UpdateCommLinkedGrids = false;
					}
				}

				// Update beacon linked beacons
				if (this.UpdateBeaconLinkedBeacons && MyNetworkInterface.IsServerLike)
				{
					using (this.BeaconLinkedLock.WithWriter())
					{
						if (this.RadioAntennas.Count > 0 && this.RadioAntennas.Any((pair) => !pair.Value.MarkedForClose && pair.Value.IsWorking && pair.Value.IsBroadcasting))
						{
							if (this.BeaconLinks == null) this.BeaconLinks = new List<MyBeaconLinkWrapper>();
							else this.BeaconLinks.Clear();
							List<MyEntity> broadcast_entities = new List<MyEntity>();
							BoundingBoxD world_aabb = this.GetCombinedAABB();
							BoundingSphereD broadcast_sphere = new BoundingSphereD(world_aabb.Center, 200000 + world_aabb.Extents.Max());
							MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref broadcast_sphere, broadcast_entities);

							foreach (MyEntity entity in broadcast_entities)
							{
								MyJumpGateConstruct target_grid;
								if (!(entity is IMyCubeGrid) || entity.Physics == null || (target_grid = MyJumpGateModSession.Instance.GetJumpGateGrid(entity.EntityId)) == null || target_grid == this) continue;

								foreach (KeyValuePair<long, IMyRadioAntenna> antenna_pair in this.RadioAntennas)
								{
									foreach (KeyValuePair<long, IMyBeacon> beacon_pair in target_grid.BeaconAntennas)
									{
										MyBeaconLinkWrapper wrapper;
										if (beacon_pair.Value.MarkedForClose || !beacon_pair.Value.IsWorking || Vector3D.Distance(beacon_pair.Value.WorldMatrix.Translation, antenna_pair.Value.WorldMatrix.Translation) > beacon_pair.Value.Radius || this.BeaconLinks.Contains(wrapper = new MyBeaconLinkWrapper(beacon_pair.Value))) continue;
										this.BeaconLinks.Add(wrapper);
									}
								}
							}

							this.LastCommLinkUpdate = DateTime.Now;
							this.UpdateBeaconLinkedBeacons = false;
						}
					}
				}

				// Finalize update
				if (MyJumpGateModSession.Network.Registered && (is_dirty || first_update)) this.SendNetworkGridUpdate();
				if (first_update) Logger.Debug($"[{grid_id}] (MAIN.{this.GetMainCubeGrid()?.EntityId ?? -1})] - FIRST_UPDATE", 1);
				this.FullyInitialized = this.FullyInitialized || first_update;
			}
			finally
			{
				sw.Stop();
				this.UpdateTimeTicks?.Enqueue(sw.Elapsed.TotalMilliseconds);
				this.LongestUpdateTime = Math.Max(this.LongestUpdateTime, sw.Elapsed.TotalMilliseconds);
				if (Monitor.IsEntered(this.UpdateLock)) Monitor.Exit(this.UpdateLock);
				//Logger.Debug($"[{grid_id}] - UPDATE_END", 4);
			}
		}

		/// <summary>
		/// Does one update of this construct's non-threadable attributes<br />
		/// This will tick all jump gate power syphons, physics colliders, and sound emitters<br />
		/// <b>Must be called from game thread</b>
		/// </summary>
		public void UpdateNonThreadable()
		{
			if (this.Closed) return;

			foreach (KeyValuePair<long, MyJumpGate> pair in this.JumpGates)
			{
				if (!pair.Value.Closed)
				{
					pair.Value.UpdateNonThreadable();
				}
			}
		}

		/// <summary>
		/// Remaps all jump gate IDs on this construct<br />
		/// Ids will be compressed such that the highest ID is the number of jump gates - 1
		/// </summary>
		public void RemapJumpGateIDs()
		{
			if (this.Closed) return;
			long remapped_id = 0;
			List<MyJumpGate> remapped_gates = new List<MyJumpGate>(this.JumpGates.Values);
			List<MyJumpGate> all_gates = MyJumpGateModSession.Instance.GetAllJumpGates().ToList();
			this.JumpGates.Clear();

			foreach (MyJumpGate gate in remapped_gates)
			{
				JumpGateUUID old_uid = JumpGateUUID.FromJumpGate(gate);
				gate.JumpGateID = remapped_id;
				this.JumpGates[remapped_id++] = gate;
				foreach (KeyValuePair<long, MyJumpGateDrive> pair in this.JumpGateDrives) if (pair.Value.JumpGateID == old_uid.GetJumpGate()) pair.Value.SetAttachedJumpGate(gate);
				foreach (KeyValuePair<long, MyJumpGateController> pair in this.JumpGateControllers) if (pair.Value.BlockSettings.JumpGateID() == old_uid.GetJumpGate()) pair.Value.AttachedJumpGate(gate);

				foreach (MyJumpGate source in all_gates)
				{
					MyJumpGateWaypoint dest = source.Controller?.BlockSettings.SelectedWaypoint();
					if (source.Controller == null || dest == null || dest.WaypointType != MyWaypointType.JUMP_GATE || JumpGateUUID.FromGuid(dest.JumpGate) != old_uid) continue;
					source.Controller.BlockSettings.SelectedWaypoint(new MyJumpGateWaypoint(gate));
				}
			}
		}

		/// <summary>
		/// Markes this construct for close or closes immediatly if session is not running
		/// </summary>
		public void Dispose()
		{
			if (this.Closed || this.MarkClosed || MyNetworkInterface.IsStandaloneMultiplayerClient) return;
			this.MarkClosed = true;
			foreach (KeyValuePair<long, MyJumpGate> pair in this.JumpGates) pair.Value.Dispose();
			if (MyJumpGateModSession.SessionStatus == MySessionStatusEnum.UNLOADING) MyJumpGateModSession.Instance.CloseGrid(this);
		}

		/// <summary>
		/// Destroys all grids attached to this construct
		/// </summary>
		public void Destroy()
		{
			if (this.Closed) return;
			foreach (KeyValuePair<long, IMyCubeGrid> pair in this.CubeGrids) pair.Value.Close();
		}

		/// <summary>
		/// Clears all timing information for this construct
		/// </summary>
		public void ClearResetTimes()
		{
			if (this.Closed) return;
			this.LongestUpdateTime = -1;
			this.UpdateTimeTicks.Clear();
		}

		/// <summary>
		/// Marks this construct for gate reconstruction<br />
		/// Gate drive intersections will be updated and gates reconstructed on next tick
		/// </summary>
		public void MarkGatesForUpdate()
		{
			this.MarkUpdateJumpGates = !this.Closed;
		}

		/// <summary>
		/// Unregisters resources for partially initialized constructs<br />
		/// For fully initialized constructs: Call MyJumpGateConstruct::Dispose()
		/// </summary>
		public void Release()
		{
			if (MyJumpGateModSession.Network.Registered)
			{
				MyJumpGateModSession.Network.Off(MyPacketTypeEnum.UPDATE_GRID, this.OnNetworkJumpGateGridUpdate);
				MyJumpGateModSession.Network.Off(MyPacketTypeEnum.STATICIFY_CONSTRUCT, this.OnStaticifyConstruct);
				MyJumpGateModSession.Network.Off(MyPacketTypeEnum.COMM_LINKED, this.OnNetworkCommLinkedUpdate);
				MyJumpGateModSession.Network.Off(MyPacketTypeEnum.BEACON_LINKED, this.OnNetworkBeaconLinkedUpdate);
			}
		}

		/// <summary>
		/// Requests a jump grid update from the server<br />
		/// Does nothing if singleplayer or server<br />
		/// Does nothing if construct is suspended
		/// </summary>
		public void RequestGateUpdate()
		{
			if (this.Closed || MyNetworkInterface.IsServerLike || this.IsSuspended) return;
			MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet {
				PacketType = MyPacketTypeEnum.UPDATE_GRID,
				Broadcast = false,
				TargetID = 0,
			};
			packet.Payload<MySerializedJumpGateConstruct>(null);
			packet.Send();
		}

		/// <summary>
		/// Marks this construct as suspended<br />
		/// Suspended grids do not receive update ticks<br />
		/// <i>Has no effect on server</i>
		/// </summary>
		public void Suspend(long fallback_id)
		{
			if (this.Closed || MyNetworkInterface.IsMultiplayerServer) return;
			this.IsSuspended = true;
			this.SuspendedCubeGridID = this.CubeGrid?.EntityId ?? fallback_id;
		}

		/// <summary>
		/// Marks this construct as dirty<br />
		/// It will be synced to clients on next tick
		/// </summary>
		public void SetDirty()
		{
			if (this.Closed) return;
			this.IsDirty = true;
			this.LastUpdateDateTimeUTC = DateTime.UtcNow;
		}

		/// <summary>
		/// Sets whether all parts of this construct should be static
		/// </summary>
		/// <param name="is_static">Staticness</param>
		public void SetConstructStaticness(bool is_static)
		{
			if (this.Closed) return;

			foreach (KeyValuePair<long, IMyCubeGrid> pair in this.CubeGrids)
			{
				pair.Value.Physics.AngularVelocity = Vector3.Zero;
				pair.Value.Physics.LinearVelocity = Vector3.Zero;
				pair.Value.IsStatic = is_static;
			}
		}

		/// <summary>
		/// Gets a list of blocks from this construct that intersect the specified bounding ellipsoid
		/// </summary>
		/// <param name="ellipsoid">The bounding ellipsoid</param>
		/// <param name="contained_blocks">An optional collection to populate containing all blocks within the ellipsoid</param>
		/// <param name="uncontained_blocks">An optional collection to populate containing all blocks outside the ellipsoid</param>
		public void GetBlocksIntersectingEllipsoid(ref BoundingEllipsoidD ellipsoid, ICollection<IMySlimBlock> contained_blocks = null, ICollection<IMySlimBlock> uncontained_blocks = null)
		{
			if (this.Closed || contained_blocks == null && uncontained_blocks == null) return;
			Vector3D position;

			foreach (IMySlimBlock block in this.GridBlocks)
			{
				if (block == null || (block.FatBlock?.MarkedForClose ?? false)) continue;
				position = block.CubeGrid.GridIntegerToWorld(block.Position);
				if (contained_blocks != null && ellipsoid.IsPointInEllipse(position)) contained_blocks.Add(block);
				else if (uncontained_blocks != null) uncontained_blocks.Add(block);
			}
		}

		/// <summary>
		/// Clears this construct from suspension<br />
		/// Construct will continue updates in next tick
		/// </summary>
		/// <param name="serialized">The serialized construct to update from</param>
		/// <param name="update_time">The new optional last update time for this construct</param>
		/// <returns>Whether this grid was resumed</returns>
		public bool Persist(MySerializedJumpGateConstruct serialized, DateTime? update_time = null)
		{
			if (this.Closed || !this.IsSuspended) return true;
			bool resumed = this.FromSerialized(serialized);
			this.IsSuspended = !resumed;
			if (resumed) this.LastUpdateDateTimeUTC = update_time ?? DateTime.UtcNow;
			return resumed;
		}

		/// <summary>
		/// Updates this construct data from a serialized controller
		/// </summary>
		/// <param name="controller">The serialized grid data</param>
		/// <returns>Whether this construct was updated</returns>
		public bool FromSerialized(MySerializedJumpGateConstruct grid)
		{
			if (this.Closed || grid == null || grid.IsClientRequest) return false;
			bool initial_dirty = this.IsDirty;
			long gridid = JumpGateUUID.FromGuid(grid.UUID).GetJumpGateGrid();
			if (gridid != this.CubeGridID) return false;

			lock (this.UpdateLock)
			{
				IMyCubeGrid new_grid = MyAPIGateway.Entities.GetEntityById(gridid) as IMyCubeGrid;
				Logger.Debug($"CUBE_GRID_SETUP::{gridid}, NULLGRID={new_grid == null}", 2);

				if (this.IsSuspended && new_grid != null && (grid.CubeGrids?.Any((subgrid_id) => ((IMyCubeGrid) MyAPIGateway.Entities.GetEntityById(subgrid_id)) == null) ?? true))
				{
					MyJumpGateModSession.Instance.QueuePartialConstructForReload(gridid);
					Logger.Debug($" ... PARTIAL_GRID >> MARKED_FOR_SINGLE_UPDATE_60");
					return true;
				}

				this.MarkClosed = false;
				this.PrimaryCubeGridCustomName = new_grid?.CustomName ?? "";
				List<MySerializedJumpGate> jump_gates = grid.JumpGates ?? new List<MySerializedJumpGate>();
				List<MySerializedJumpGateDrive> drives = grid.JumpGateDrives ?? new List<MySerializedJumpGateDrive>();
				List<MySerializedJumpGateCapacitor> capacitors = grid.JumpGateCapacitors ?? new List<MySerializedJumpGateCapacitor>();
				List<MySerializedJumpGateController> controllers = grid.JumpGateControllers ?? new List<MySerializedJumpGateController>();
				List<MySerializedJumpGateServerAntenna> server_antennas = grid.JumpGateServerAntennas ?? new List<MySerializedJumpGateServerAntenna>();
				List<long> laser_antennas = grid.LaserAntennas ?? new List<long>();
				List<long> radio_antennas = grid.RadioAntennas ?? new List<long>();
				List<long> new_gates = new List<long>();

				if (new_grid != null && MyNetworkInterface.IsStandaloneMultiplayerClient)
				{
					this.CubeGrid = new_grid;
					this.SetupConstruct(grid.CubeGrids.Select((subgrid) => (IMyCubeGrid) MyAPIGateway.Entities.GetEntityById(subgrid)).Where((subgrid) => subgrid != null));
				}
				
				foreach (MySerializedJumpGateDrive serialized in drives)
				{
					JumpGateUUID uuid = JumpGateUUID.FromGuid(serialized.UUID);
					MyJumpGateDrive component = MyJumpGateModSession.GetBlockAsJumpGateDrive(MyAPIGateway.Entities.GetEntityById(uuid.GetBlock()) as IMyTerminalBlock) ?? new MyJumpGateDrive(serialized);

					this.JumpGateDrives.AddOrUpdate(component.BlockID, component, (_1, block) => {
						block.FromSerialized(serialized);
						return block;
					});
				}
				
				foreach (MySerializedJumpGateCapacitor serialized in capacitors)
				{
					JumpGateUUID uuid = JumpGateUUID.FromGuid(serialized.UUID);
					MyJumpGateCapacitor component = MyJumpGateModSession.GetBlockAsJumpGateCapacitor(MyAPIGateway.Entities.GetEntityById(uuid.GetBlock()) as IMyTerminalBlock) ?? new MyJumpGateCapacitor(serialized);

					this.JumpGateCapacitors.AddOrUpdate(component.BlockID, component, (_1, block) => {
						block.FromSerialized(serialized);
						return block;
					});
				}
				
				foreach (MySerializedJumpGateController serialized in controllers)
				{
					JumpGateUUID uuid = JumpGateUUID.FromGuid(serialized.UUID);
					MyJumpGateController component = MyJumpGateModSession.GetBlockAsJumpGateController(MyAPIGateway.Entities.GetEntityById(uuid.GetBlock()) as IMyTerminalBlock) ?? new MyJumpGateController(serialized);
					
					this.JumpGateControllers.AddOrUpdate(component.BlockID, component, (_1, block) => {
						block.FromSerialized(serialized);
						return block;
					});
				}
				
				foreach (MySerializedJumpGateServerAntenna serialized in server_antennas)
				{
					JumpGateUUID uuid = JumpGateUUID.FromGuid(serialized.UUID);
					MyJumpGateServerAntenna component = MyJumpGateModSession.GetBlockAsJumpGateServerAntenna(MyAPIGateway.Entities.GetEntityById(uuid.GetBlock()) as IMyUpgradeModule) ?? new MyJumpGateServerAntenna(serialized);

					this.JumpGateServerAntennas.AddOrUpdate(component.BlockID, component, (_1, block) => {
						block.FromSerialized(serialized);
						return block;
					});
				}
				
				foreach (long laser_antenna in laser_antennas)
				{
					IMyLaserAntenna block = MyAPIGateway.Entities.GetEntityById(laser_antenna) as IMyLaserAntenna;
					if (block != null) this.LaserAntennas.AddOrUpdate(block.EntityId, block, (_1, _2) => block);
				}

				foreach (long radio_antenna in radio_antennas)
				{
					IMyRadioAntenna block = MyAPIGateway.Entities.GetEntityById(radio_antenna) as IMyRadioAntenna;
					if (block != null) this.RadioAntennas.AddOrUpdate(block.EntityId, block, (_1, _2) => block);
				}

				if (this.JumpGateDrives.Count > 0 || MyNetworkInterface.IsStandaloneMultiplayerClient)
				{
					foreach (MySerializedJumpGate serialized in jump_gates)
					{
						JumpGateUUID uuid = JumpGateUUID.FromGuid(serialized.UUID);
						MyJumpGate new_gate = new MyJumpGate(serialized);
						new_gate = this.JumpGates.AddOrUpdate(uuid.GetJumpGate(), new_gate, (_, gate) => {
							if (gate.MarkClosed) return new_gate;
							gate.FromSerialized(serialized);
							new_gate.Release();
							return gate;
						});
						new_gate.Init();
						new_gates.Add(new_gate.JumpGateID);
					}
				}

				foreach (KeyValuePair<long, MyJumpGate> pair in this.JumpGates)
				{
					if (!new_gates.Contains(pair.Key))
					{
						pair.Value.Dispose();
						MyJumpGateModSession.Instance.CloseGate(pair.Value);
					}
				}

				if (MyNetworkInterface.IsMultiplayerServer) this.SetDirty();
				else this.IsDirty = initial_dirty;
				if (new_grid == null) this.Suspend(gridid);
				return true;
			}
		}

		/// <summary>
		/// Whether this grid is valid
		/// </summary>
		/// <returns>True if valid</returns>
		public bool IsValid()
        {
            return this.GetInvalidationReason() == MyGridInvalidationReason.NONE;
        }

		/// <summary>
		/// Checks construct staticness
		/// </summary>
		/// <returns>Whether at least one grid is static</returns>
		public bool IsStatic()
		{
			if (this.Closed) return false;
			foreach (KeyValuePair<long, IMyCubeGrid> pair in this.CubeGrids) if (pair.Value.IsStatic) return true;
			return false;
		}

        /// <summary>
        /// </summary>
        /// <returns>True if any working antennas exist on this construct</returns>
        public bool HasCommLink()
        {
			if (this.Closed) return false;
			foreach (KeyValuePair<long, IMyRadioAntenna> pair in this.RadioAntennas) if (pair.Value.IsWorking) return true;
			foreach (KeyValuePair<long, IMyLaserAntenna> pair in this.LaserAntennas) if (pair.Value.IsWorking) return true;
			return false;
        }

        /// <summary>
        /// </summary>
        /// <returns>True if this construct completed at least one tick</returns>
        public bool AtLeastOneUpdate()
        {
            return (this.UpdateTimeTicks?.Count ?? 0) > 0;
        }

        /// <summary>
        /// </summary>
        /// <param name="grid_id">The sub-grid ID</param>
        /// <returns>True if the grid ID is part of this construct</returns>
		public bool HasCubeGrid(long grid_id)
		{
			return !this.Closed && this.CubeGrids.ContainsKey(grid_id);
		}

		/// <summary>
		/// </summary>
		/// <param name="grid">The sub-grid cube grid</param>
		/// <returns>True if the grid is part of this construct</returns>
		public bool HasCubeGrid(IMyCubeGrid grid)
        {
            return !this.Closed && this.CubeGrids.ContainsKey(grid.EntityId);
        }

		/// <summary>
		/// Checks if the specified grid is reachable from this one via antennas
		/// </summary>
		/// <param name="grid">The target grid</param>
		/// <param name="update">If true, requests update to internal list</param>
		/// <returns>Whether target grid is comm linked</returns>
		public bool IsConstructCommLinked(MyJumpGateConstruct grid)
		{
			if (this.Closed || grid == null) return false;
			this.CommLinkedLock.AcquireReader();
			try { return this.CommLinkedGrids?.Contains(grid) ?? false; }
			finally { this.CommLinkedLock.ReleaseReader(); }
		}

		/// <summary>
		/// Checks if the specified beacon is seen from this grid
		/// This method will not update the internal beacon-linked list before checking
		/// </summary>
		/// <param name="beacon">The target beacon</param>
		/// <param name="update">If true, requests update to internal list</param>
		/// <returns>Whether target beacon is beacon linked</returns>
		public bool IsBeaconWithinReverseBroadcastSphere(MyBeaconLinkWrapper beacon)
		{
			if (this.Closed || beacon == null) return false;
			this.BeaconLinkedLock.AcquireReader();
			try { return this.BeaconLinks?.Contains(beacon) ?? false; }
			finally { this.BeaconLinkedLock.ReleaseReader(); }
		}

		/// <summary>
		/// Checks whether thie construct intersects the specified ellipsoid
		/// </summary>
		/// <param name="ellipsoid">The ellipsoid to check against</param>
		/// <returns>Whether at least 4 bounding box vertices are contained</returns>
		public bool IsConstructWithinBoundingEllipsoid(ref BoundingEllipsoidD ellipsoid)
		{
			if (this.Closed) return false;
			foreach (IMySlimBlock block in this.GridBlocks) if (ellipsoid.IsPointInEllipse(MyJumpGateModSession.LocalVectorToWorldVectorP(block.CubeGrid.WorldMatrix, block.Position * block.CubeGrid.GridSize))) return true;
			return false;
		}

		/// <summary>
		/// Gets the number of blocks matching the specified predicate<br />
		/// Gets the number of all blocks if predicate is null
		/// </summary>
		/// <param name="predicate">The predicate to filter blocks by</param>
		/// <returns>The number of matching blocks</returns>
		public int GetConstructBlockCount(Func<IMySlimBlock, bool> predicate = null)
		{
			if (this.Closed) return 0;
			return (predicate == null) ? this.GridBlocks.Count : this.GridBlocks.Count(predicate);
		}

		/// <summary>
		/// Gets the reason this construct is invalid
		/// </summary>
		/// <returns>The invalidation reason or InvalidationReason.NONE</returns>
		public MyGridInvalidationReason GetInvalidationReason()
        {
            if (this.Closed || this.MarkClosed) return MyGridInvalidationReason.CLOSED;
            else if (this.CubeGrid == null) return (this.IsSuspended) ? MyGridInvalidationReason.NONE : MyGridInvalidationReason.NULL_GRID;
            else if (this.CubeGrids.Count == 0) return MyGridInvalidationReason.INSUFFICIENT_GRIDS;
            else if (this.GetCubeGridPhysics() == null) return MyGridInvalidationReason.NULL_PHYSICS;
            else return MyGridInvalidationReason.NONE;
		}

		/// <summary>
		/// Gets the mass of this construct in kilograms<br />
		/// This equals the total mass of each grid<br />
		/// <i>Will be 0 if grid is suspended</i>
		/// </summary>
		/// <returns>The mass of this construct</returns>
		public double ConstructMass()
		{
			if (this.Closed || this.IsSuspended) return 0;
			double mass = 0;
			foreach (KeyValuePair<long, IMyCubeGrid> pair in this.CubeGrids) mass += (double) (pair.Value.Physics?.Mass ?? 0);
			return mass;
		}

		/// <summary>
		/// Attemps to drain the specified power from all on-construct capacitors
		/// </summary>
		/// <param name="power_mw">The amount of drain to syphon in MegaWatts</param>
		/// <returns>The remaining power after syphon</returns>
		public double SyphonConstructCapacitorPower(double power_mw)
		{
			if (this.Closed || power_mw <= 0) return 0;
			List<MyJumpGateCapacitor> capacitors = new List<MyJumpGateCapacitor>();
			foreach (KeyValuePair<long, MyJumpGateCapacitor> pair in this.JumpGateCapacitors) if (pair.Value.IsWorking() && pair.Value.StoredChargeMW > 0 && pair.Value.BlockSettings.RechargeEnabled) capacitors.Add(pair.Value);
			if (capacitors.Count == 0) return power_mw;
			double power_per = power_mw / capacitors.Count;
			power_mw = 0;
			foreach (MyJumpGateCapacitor capacitor in capacitors) power_mw += capacitor.DrainStoredCharge(power_per);
			return power_mw;
		}

		/// <summary>
		/// </summary>
		/// <returns>The average of all update times</returns>
		public double AverageUpdateTime60()
		{
			if (this.Closed) return -1;
			try { return (this.UpdateTimeTicks.Count == 0) ? -1 : this.UpdateTimeTicks.Average(); }
			catch (InvalidOperationException) { return -1; }
		}

		/// <summary>
		/// </summary>
		/// <returns>Returns the longest update time within the 60 tick buffer</returns>
		public double LocalLongestUpdateTime60()
		{
			if (this.Closed) return -1;
			try { return (this.UpdateTimeTicks.Count == 0) ? -1 : this.UpdateTimeTicks.Max(); }
			catch (InvalidOperationException) { return -1; }
		}

		/// <summary>
		/// Calculates the total available power stored within construct capacitors and drives for the specified jump gate
		/// </summary>
		/// <param name="jump_gate_id">The jump gate to calculate for</param>
		/// <returns>Total available instant power in Mega-Watts</returns>
		public double CalculateTotalAvailableInstantPower(long jump_gate_id)
		{
			if (this.Closed) return 0;
			double total_power = 0;
			foreach (KeyValuePair<long, MyJumpGateDrive> pair in this.JumpGateDrives) if (pair.Value.JumpGateID == jump_gate_id && pair.Value.IsWorking()) total_power += pair.Value.StoredChargeMW;
			foreach (KeyValuePair<long, MyJumpGateCapacitor> pair in this.JumpGateCapacitors) if (pair.Value.IsWorking() && pair.Value.BlockSettings.RechargeEnabled) total_power += pair.Value.StoredChargeMW;
			return total_power;
		}

		/// <summary>
		/// Calculates the total possible power stored within construct capacitors and drives for the specified jump gate
		/// </summary>
		/// <param name="jump_gate_id">The jump gate to calculate for</param>
		/// <returns>Total possible instant power in Mega-Watts</returns>
		public double CalculateTotalPossibleInstantPower(long jump_gate_id)
		{
			if (this.Closed) return 0;
			double total_power = 0;
			foreach (KeyValuePair<long, MyJumpGateDrive> pair in this.JumpGateDrives) if (pair.Value.JumpGateID == jump_gate_id && pair.Value.IsWorking()) total_power += pair.Value.DriveConfiguration.MaxDriveChargeMW;
			foreach (KeyValuePair<long, MyJumpGateCapacitor> pair in this.JumpGateCapacitors) if (pair.Value.IsWorking() && pair.Value.BlockSettings.RechargeEnabled) total_power += pair.Value.CapacitorConfiguration.MaxCapacitorChargeMW;
			return total_power;
		}

		/// <summary>
		/// Calculates the total possible power stored within construct capacitors and drives for the specified jump gate<br />
		/// This method ignores block settings and whether block is working
		/// </summary>
		/// <param name="jump_gate_id">The jump gate to calculate for</param>
		/// <returns>Total max possible instant power in Mega-Watts</returns>
		public double CalculateTotalMaxPossibleInstantPower(long jump_gate_id)
		{
			if (this.Closed) return 0;
			double total_power = 0;
			foreach (KeyValuePair<long, MyJumpGateDrive> pair in this.JumpGateDrives) if (pair.Value.JumpGateID == jump_gate_id) total_power += pair.Value.DriveConfiguration.MaxDriveChargeMW;
			foreach (KeyValuePair<long, MyJumpGateCapacitor> pair in this.JumpGateCapacitors) total_power += pair.Value.CapacitorConfiguration.MaxCapacitorChargeMW;
			return total_power;
		}

		/// <summary>
		/// Gets the center of volume of this construct<br />
		/// Returns a zero vector if suspended or closed
		/// </summary>
		/// <returns>This construct's center of volume</returns>
		public Vector3D ConstructVolumeCenter()
		{
			if (this.Closed) return Vector3D.Zero;
			return this.GetCombinedAABB().Center;
		}

		/// <summary>
		/// Gets the center of mass of this construct<br />
		/// Returns a zero vector if suspended or closed
		/// </summary>
		/// <returns>This construct's center of volume</returns>
		public Vector3D ConstructMassCenter()
		{
			if (this.Closed || this.IsSuspended) return Vector3D.Zero;
			Vector3D sum = Vector3D.Zero;
			foreach (KeyValuePair<long, IMyCubeGrid> pair in this.CubeGrids) sum += pair.Value.Physics.CenterOfMassWorld;
			return sum / this.CubeGrids.Count;
		}

		/// <summary>
		/// Checks if the specified world coordinate is within at least one grid of this construct<br />
		/// Point must be within at least two walls of the same grid
		/// </summary>
		/// <param name="world_position">The world position</param>
		/// <returns>The containing grid or null if not inside any subgrid</returns>
		public IMyCubeGrid IsPositionInsideAnySubgrid(Vector3D world_position)
		{
			if (this.Closed) return null;
			foreach (KeyValuePair<long, IMyCubeGrid> pair in this.CubeGrids) if (MyJumpGateModSession.IsPositionInsideGrid(pair.Value, world_position)) return pair.Value;
			return null;
		}

		/// <summary>
		/// </summary>
		/// <returns>This construct's main cube grid</returns>
		public IMyCubeGrid GetMainCubeGrid()
        {
			if (this.Closed) return null;
			int highest = 0;
			IMyCubeGrid largest_grid = null;

			foreach (KeyValuePair<long, IMyCubeGrid> pair in this.CubeGrids)
			{
				int count = ((MyCubeGrid) pair.Value).BlocksCount;
				if (count == 0 || count < highest || (count == highest && largest_grid.EntityId < pair.Value.EntityId)) continue;
				highest = count;
				largest_grid = pair.Value;
			}

            return largest_grid;
		}

		/// <summary>
		/// Gets a cube grid by it's terminal block entity ID
		/// </summary>
		/// <param name="id">The cube grid's ID</param>
		/// <returns>The cube grid</returns>
		public IMyCubeGrid GetCubeGrid(long id)
		{
			return this.CubeGrids?.GetValueOrDefault(id, null);
		}

		/// <summary>
		/// Splits a grid of this construct<br />
		/// The section to discard will be returned and the section to keep will update this construct<br />
		/// If any argument is null, this is not server, or the grid is not contained, result will be null
		/// </summary>
		/// <param name="grid">The grid to split</param>
		/// <param name="blocks_to_discard">The blocks to keep in this construct</param>
		/// <returns>The resulting grid or null</returns>
		public IMyCubeGrid SplitGrid(IMyCubeGrid grid, List<IMySlimBlock> blocks_to_discard)
		{
			if (this.Closed || !MyNetworkInterface.IsServerLike || grid == null || blocks_to_discard == null || blocks_to_discard.Count == 0 || !this.CubeGrids.ContainsKey(grid.EntityId)) return null;
			return grid.Split(blocks_to_discard);
		}

		/// <summary>
		/// Gets a laser antenna by it's terminal block entity ID
		/// </summary>
		/// <param name="id">The laser antenna's ID</param>
		/// <returns>The laser antenna</returns>
		public IMyLaserAntenna GetLaserAntenna(long id)
        {
			return this.LaserAntennas?.GetValueOrDefault(id, null);
		}

		/// <summary>
		/// Gets a radio antenna by it's terminal block entity ID
		/// </summary>
		/// <param name="id">The radio antenna's ID</param>
		/// <returns>The radio antenna</returns>
		public IMyRadioAntenna GetRadioAntenna(long id)
        {
			return this.RadioAntennas?.GetValueOrDefault(id, null);
		}

		/// <summary>
		/// Gets a beacon by it's terminal block entity ID
		/// </summary>
		/// <param name="id">The beacon's ID</param>
		/// <returns>The beacon</returns>
		public IMyBeacon GetBeacon(long id)
		{
			return this.BeaconAntennas?.GetValueOrDefault(id, null);
		}

		/// <summary>
		/// Gets an enumerator to all grid blocks within this construct
		/// </summary>
		/// <returns>An enumerator for all blocks</returns>
		public IEnumerator<IMySlimBlock> GetConstructBlocks()
		{
			return this.GridBlocks?.GetEnumerator();
		}

		/// <summary>
		/// Creates a bounding box containing all sub grids' bounding box
		/// </summary>
		/// <returns>This construct's combined AABB or an invalid AABB if suspended or closed</returns>
		public BoundingBoxD GetCombinedAABB()
		{
			if (this.Closed || this.IsSuspended) return BoundingBoxD.CreateInvalid();
			List<Vector3D> points = new List<Vector3D>(8 * this.CubeGrids.Count);
			foreach (KeyValuePair<long, IMyCubeGrid> pair in this.CubeGrids) for (int i = 0; i < 8; ++i) points.Add(pair.Value.WorldAABB.GetCorner(i));
			return BoundingBoxD.CreateFromPoints(points);
		}

		/// <summary>
		/// Gets a jump gate by it's terminal block entity ID
		/// </summary>
		/// <param name="id">The jump gate's ID</param>
		/// <returns>The jump gate</returns>
		public MyJumpGate GetJumpGate(long id)
        {
			return this.JumpGates?.GetValueOrDefault(id, null);
		}

		/// <summary>
		/// Gets a capacitor by it's terminal block entity ID
		/// </summary>
		/// <param name="id">The capacitor's ID</param>
		/// <returns>The capacitor</returns>
		public MyJumpGateCapacitor GetCapacitor(long id)
		{
			return this.JumpGateCapacitors?.GetValueOrDefault(id, null);
		}

		/// <summary>
		/// Gets a drive by it's terminal block entity ID
		/// </summary>
		/// <param name="id">The drive's ID</param>
		/// <returns>The drive</returns>
		public MyJumpGateDrive GetDrive(long id)
		{
			return this.JumpGateDrives?.GetValueOrDefault(id, null);
		}

		/// <summary>
		/// Gets a server antenna by it's terminal block entity ID
		/// </summary>
		/// <param name="id">The server antenna's ID</param>
		/// <returns>The server antenna</returns>
		public MyJumpGateServerAntenna GetServerAntenna(long id)
		{
			return this.JumpGateServerAntennas?.GetValueOrDefault(id, null);
		}

		/// <summary>
		/// Gets a controller by it's terminal block entity ID
		/// </summary>
		/// <param name="id">The controller's ID</param>
		/// <returns>The controller</returns>
		public MyJumpGateController GetController(long id)
		{
			return this.JumpGateControllers?.GetValueOrDefault(id, null);
		}

		/// <summary>
		/// Gets the first physics component from this construct's cube grids
		/// </summary>
		/// <returns></returns>
		public MyPhysicsComponentBase GetCubeGridPhysics()
		{
			if (this.Closed) return null;
			foreach (KeyValuePair<long, IMyCubeGrid> pair in this.CubeGrids) if (pair.Value.Physics != null) return pair.Value.Physics;
			return null;
		}

		/// <summary>
		/// Gets all blocks inheriting from the specified type
		/// </summary>
		/// <typeparam name="T">The block type</typeparam>
		/// <returns>An IEnumerable referencing all matching blocks</returns>
		public IEnumerable<T> GetAllSlimBlocksOfType<T>() where T : IMySlimBlock
		{
			return (this.Closed) ? Enumerable.Empty<T>() : this.GridBlocks.Where((block) => block is T).Select((block) => (T) block);
		}

		/// <summary>
		/// Gets all blocks inheriting from the specified type
		/// </summary>
		/// <typeparam name="T">The block type</typeparam>
		/// <returns>An IEnumerable referencing all matching blocks</returns>
		public IEnumerable<T> GetAllFatBlocksOfType<T>() where T : IMyCubeBlock
		{
			return (this.Closed) ? Enumerable.Empty<T>() : this.GridBlocks.Where((block) => block.FatBlock != null && block.FatBlock is T).Select((block) => (T) block.FatBlock);
		}

		/// <summary>
		/// Gets all jump gate controllers in this construct
		/// </summary>
		/// <returns>An IEnumerable referencing all controllers</returns>
		public IEnumerable<MyJumpGateController> GetAttachedJumpGateControllers()
		{
			return this.JumpGateControllers?.Values.AsEnumerable() ?? Enumerable.Empty<MyJumpGateController>();
		}

		/// <summary>
		/// All jump gate drives in this construct
		/// </summary>
		/// <returns>An IEnumerable referencing all drives</returns>
		public IEnumerable<MyJumpGateDrive> GetAttachedJumpGateDrives()
		{
			return this.JumpGateDrives?.Values.AsEnumerable() ?? Enumerable.Empty<MyJumpGateDrive>();
		}

		/// <summary>
		/// Gets all jump gate drives in this construct not bound to a jump gate
		/// </summary>
		/// <returns>An IEnumerable referencing all unassociated drives</returns>
		public IEnumerable<MyJumpGateDrive> GetAttachedUnassociatedJumpGateDrives()
		{
			return this.JumpGateDrives?.Values.Where((drive) => drive.JumpGateID == -1) ?? Enumerable.Empty<MyJumpGateDrive>();
		}

		/// <summary>
		/// Gets all jump gate capacitors in this construct
		/// </summary>
		/// <returns>An IEnumerable referencing all capacitors</returns>
		public IEnumerable<MyJumpGateCapacitor> GetAttachedJumpGateCapacitors()
		{
			return this.JumpGateCapacitors?.Values.AsEnumerable() ?? Enumerable.Empty<MyJumpGateCapacitor>();
		}

		/// <summary>
		/// Gets all jump gate server antennas in this construct
		/// </summary>
		/// <returns>An IEnumerable referencing all server antennas</returns>
		public IEnumerable<MyJumpGateServerAntenna> GetAttachedJumpGateServerAntennas()
		{
			return this.JumpGateServerAntennas?.Values.AsEnumerable() ?? Enumerable.Empty<MyJumpGateServerAntenna>();
		}

		/// <summary>
		/// Gets all laser antennas in this construct
		/// </summary>
		/// <returns>An IEnumerable referencing all laser antennas</returns>
		public IEnumerable<IMyLaserAntenna> GetAttachedLaserAntennas()
		{
			return this.LaserAntennas?.Values.AsEnumerable() ?? Enumerable.Empty<IMyLaserAntenna>();
		}

		/// <summary>
		/// Gets all radio antennas in this construct
		/// </summary>
		/// <returns>An IEnumerable referencing all radio antennas</returns>
		public IEnumerable<IMyRadioAntenna> GetAttachedRadioAntennas()
		{
			return this.RadioAntennas?.Values.AsEnumerable() ?? Enumerable.Empty<IMyRadioAntenna>();
		}

		/// <summary>
		/// Gets all beacons in this construct
		/// </summary>
		/// <returns>An IEnumerable referencing all beacons</returns>
		public IEnumerable<IMyBeacon> GetAttachedBeacons()
		{
			return this.BeaconAntennas?.Values.AsEnumerable() ?? Enumerable.Empty<IMyBeacon>();
		}

		/// <summary>
		/// Gets all jump gates in this construct
		/// </summary>
		/// <returns>An IEnumerable referencing all jump gates</returns>
		public IEnumerable<MyJumpGate> GetJumpGates()
		{
			return this.JumpGates?.Values.AsEnumerable() ?? Enumerable.Empty<MyJumpGate>();
		}

		/// <summary>
		/// Gets all cube grids in this construct
		/// </summary>
		/// <returns>An IEnumerable referencing all grids</returns>
		public IEnumerable<IMyCubeGrid> GetCubeGrids()
		{
			return this.CubeGrids?.Values.AsEnumerable() ?? Enumerable.Empty<IMyCubeGrid>();
		}

		/// <summary>
		/// Does a BFS search of all constructs accessible to this one via antennas
		/// </summary>
		/// <returns>An IEnumerable referencing all comm-linked constructs</returns>
		public IEnumerable<MyJumpGateConstruct> GetCommLinkedJumpGateGrids()
		{
			if (this.Closed) return Enumerable.Empty<MyJumpGateConstruct>();
			bool timedout = this.CommLinkedGrids == null || (DateTime.Now - this.LastCommLinkUpdate).TotalMilliseconds >= MyJumpGateConstruct.CommLinkedUpdateDelay;
			IEnumerable<MyJumpGateConstruct> comm_linked_grids = Enumerable.Empty<MyJumpGateConstruct>();

			if (this.CommLinkedGrids != null)
			{
				using (this.CommLinkedLock.WithReader())
				{
					comm_linked_grids = this.CommLinkedGrids.AsEnumerable();
				}
			}

			if (timedout && MyNetworkInterface.IsServerLike) this.UpdateCommLinkedGrids = true;
			else if (timedout && MyNetworkInterface.IsStandaloneMultiplayerClient && !this.CommLinkedClientUpdate)
			{
				this.CommLinkedClientUpdate = true;
				MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet {
					PacketType = MyPacketTypeEnum.COMM_LINKED,
					TargetID = 0,
					Broadcast = false,
				};
				packet.Payload(this.CubeGridID);
				packet.Send();
			}

			return comm_linked_grids;
		}

		/// <summary>
		/// Does a search for all beacons that this grid can see
		/// </summary>
		/// <returns>An IEnumerable referencing all beacon-linked beacons</returns>
		public IEnumerable<MyBeaconLinkWrapper> GetBeaconsWithinReverseBroadcastSphere()
		{
			if (this.Closed) return Enumerable.Empty<MyBeaconLinkWrapper>();
			bool timedout = this.BeaconLinks == null || (DateTime.Now - this.LastBeaconLinkUpdate).TotalMilliseconds >= MyJumpGateConstruct.CommLinkedUpdateDelay;
			IEnumerable<MyBeaconLinkWrapper> beacon_links = Enumerable.Empty<MyBeaconLinkWrapper>();

			if (this.BeaconLinks != null)
			{
				using (this.BeaconLinkedLock.WithReader())
				{
					beacon_links = this.BeaconLinks.AsEnumerable();
				}
			}

			if (timedout && MyNetworkInterface.IsServerLike) this.UpdateBeaconLinkedBeacons = true;
			else if (timedout && MyNetworkInterface.IsStandaloneMultiplayerClient && !this.BeaconLinkedClientUpdate)
			{
				this.BeaconLinkedClientUpdate = true;
				MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet {
					PacketType = MyPacketTypeEnum.BEACON_LINKED,
					TargetID = 0,
					Broadcast = false,
				};
				packet.Payload(this.CubeGridID);
				packet.Send();
			}

			return beacon_links;
		}

		/// <summary>
		/// Serializes this construct's data
		/// </summary>
		/// <param name="as_client_request">If true, this will be an update request to server</param>
		/// <returns>The serialized grid data</returns>
		public MySerializedJumpGateConstruct ToSerialized(bool as_client_request)
        {
			if (as_client_request)
			{
				return new MySerializedJumpGateConstruct {
					UUID = JumpGateUUID.FromJumpGateGrid(this).ToGuid(),
					IsClientRequest = true,
				};
			}
			else
			{
				return new MySerializedJumpGateConstruct
				{
					UUID = JumpGateUUID.FromJumpGateGrid(this).ToGuid(),
					JumpGates = (this.Closed) ? null : this.JumpGates.Select((pair) => pair.Value.ToSerialized(false)).ToList(),
					JumpGateDrives = (this.Closed) ? null : this.JumpGateDrives.Select((pair) => pair.Value.ToSerialized(false)).ToList(),
					JumpGateControllers = (this.Closed) ? null : this.JumpGateControllers.Select((pair) => pair.Value.ToSerialized(false)).ToList(),
					JumpGateCapacitors = (this.Closed) ? null : this.JumpGateCapacitors.Select((pair) => pair.Value.ToSerialized(false)).ToList(),
					JumpGateServerAntennas = (this.Closed) ? null : this.JumpGateServerAntennas.Select((pair) => pair.Value.ToSerialized(false)).ToList(),
					LaserAntennas = (this.Closed) ? null : this.LaserAntennas.Select((pair) => pair.Value.EntityId).ToList(),
					RadioAntennas = (this.Closed) ? null : this.RadioAntennas.Select((pair) => pair.Value.EntityId).ToList(),
					CubeGrids = (this.Closed) ? null : this.CubeGrids.Select((pair) => pair.Value.EntityId).ToList(),
					ConstructName = (this.Closed) ? null : this.PrimaryCubeGridCustomName,
					IsClientRequest = false,
				};
			}
        }
		#endregion
	}

	/// <summary>
	/// Class for holding serialized MyJumpGateConstruct data
	/// </summary>
	[ProtoContract]
    internal class MySerializedJumpGateConstruct
    {
		/// <summary>
		/// The construct's JumpGateUUID as a Guid
		/// </summary>
		[ProtoMember(1)]
        public Guid UUID;

        /// <summary>
        /// The serialized jump gates belonging to this construct
        /// </summary>
        [ProtoMember(2)]
        public List<MySerializedJumpGate> JumpGates;

		/// <summary>
		/// The serialized jump gate drives belonging to this construct
		/// </summary>
		[ProtoMember(3)]
        public List<MySerializedJumpGateDrive> JumpGateDrives;

		/// <summary>
		/// The serialized jump gate controllers belonging to this construct
		/// </summary>
		[ProtoMember(4)]
        public List<MySerializedJumpGateController> JumpGateControllers;

		/// <summary>
		/// The serialized jump gate capacitors belonging to this construct
		/// </summary>
		[ProtoMember(5)]
        public List<MySerializedJumpGateCapacitor> JumpGateCapacitors;

		/// <summary>
		/// The serialized jump gate server antennas belonging to this construct
		/// </summary>
		[ProtoMember(6)]
        public List<MySerializedJumpGateServerAntenna> JumpGateServerAntennas;

		/// <summary>
		/// The list of laser antenna IDs belonging to this construct
		/// </summary>
		[ProtoMember(7)]
        public List<long> LaserAntennas;

		/// <summary>
		/// The list of radio antenna IDs belonging to this construct
		/// </summary>
		[ProtoMember(8)]
        public List<long> RadioAntennas;

		/// <summary>
		/// The list of cube grid IDs belonging to this construct
		/// </summary>
		[ProtoMember(9)]
        public List<long> CubeGrids;

		/// <summary>
		/// The name of this construct's primary cube grid
		/// </summary>
		[ProtoMember(10)]
		public string ConstructName;

		/// <summary>
		/// If true, this data should be used by server to identify grid and send updated data
		/// </summary>
		[ProtoMember(11)]
		public bool IsClientRequest;
    }
}