using IOTA.ModularJumpGates.Session;
using IOTA.ModularJumpGates.Util;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;

namespace IOTA.ModularJumpGates.JumpGateConstruct
{
	internal partial class MyJumpGateConstruct
	{
		#region Private Static Variables
		/// <summary>
		/// The delay in milliseconds after which the next call to comm linked grids should update the internal list
		/// </summary>
		private static ushort CommLinkedUpdateDelay => (ushort) ((MyNetworkInterface.IsStandaloneMultiplayerClient) ? 3000u : 500u);
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
		/// Whether to update comm linked grids on next tick
		/// </summary>
		private bool UpdateCommLinkedGrids = true;

		/// <summary>
		/// Whether to update reachable beacons on next tick
		/// </summary>
		private bool UpdateBeaconLinkedBeacons = true;

		/// <summary>
		/// The last time at which comm linked grids were updated
		/// </summary>
		private DateTime LastCommLinkUpdate;

		/// <summary>
		/// The last time at which beacon links were updated
		/// </summary>
		private DateTime LastBeaconLinkUpdate;

		/// <summary>
		/// The mutex lock for exclusive comm-linked updates
		/// </summary>
		private readonly Util.ReaderWriterLock CommLinkedLock = new Util.ReaderWriterLock();

		/// <summary>
		/// The mutex lock for exclusive beacon-linked updates
		/// </summary>
		private readonly Util.ReaderWriterLock BeaconLinkedLock = new Util.ReaderWriterLock();

		/// <summary>
		/// Temporary list of comm-linked grids used to update actual listing
		/// </summary>
		private Queue<MyJumpGateConstruct> CommLinkedGridsPoll = new Queue<MyJumpGateConstruct>();

		/// <summary>
		/// A list of all grids reachable from this grid by antenna
		/// </summary>
		private List<MyJumpGateConstruct> CommLinkedGrids = null;

		/// <summary>
		/// A list of all beacons that this grid can see
		/// </summary>
		private List<MyBeaconLinkWrapper> BeaconLinks = null;
		#endregion

		#region Private Methods
		/// <summary>
		/// Does a BFS of antenna connections to find all comm-linked grids
		/// </summary>
		private void SearchUpdateCommLinkedGrids()
		{
			if (!this.UpdateCommLinkedGrids || !MyNetworkInterface.IsServerLike) return;
			this.CommLinkedGridsPoll.Clear();
			this.CommLinkedGridsPoll.Enqueue(this);
			List<MyEntity> broadcast_entities = new List<MyEntity>();
			List<MyJumpGateConstruct> new_comm_linked = new List<MyJumpGateConstruct>(this.CommLinkedGrids?.Count ?? 0);
			List<IMyLaserAntenna> laser_antennas = new List<IMyLaserAntenna>();
			List<IMyRadioAntenna> radio_antennas = new List<IMyRadioAntenna>();

			while (this.CommLinkedGridsPoll.Count > 0)
			{
				MyJumpGateConstruct grid = this.CommLinkedGridsPoll.Dequeue();
				if (grid == null || grid.Closed || grid.MarkClosed) continue;
				laser_antennas.Clear();
				radio_antennas.Clear();
				laser_antennas.AddRange(grid.GetAttachedLaserAntennas());
				radio_antennas.AddRange(grid.GetAttachedRadioAntennas());
				if (laser_antennas.Count == 0 && radio_antennas.Count == 0) continue;

				foreach (IMyLaserAntenna antenna in laser_antennas)
				{
					if (antenna.MarkedForClose || !antenna.IsWorking || antenna.Status != Sandbox.ModAPI.Ingame.MyLaserAntennaStatus.Connected) continue;
					MyJumpGateConstruct other = MyJumpGateModSession.Instance.GetUnclosedJumpGateGrid(antenna.Other.CubeGrid);
					if (other == null || new_comm_linked.Contains(other)) continue;
					new_comm_linked.Add(other);
					this.CommLinkedGridsPoll.Enqueue(other);
				}

				if (radio_antennas.Count == 0 || radio_antennas.All((radio) => radio.MarkedForClose || !radio.IsWorking || !radio.IsBroadcasting)) continue;
				float max_range = radio_antennas.Max((antenna) => antenna.Radius);
				BoundingBoxD world_aabb = grid.GetCombinedAABB();
				BoundingSphereD broadcast_sphere = new BoundingSphereD(world_aabb.Center, max_range + world_aabb.Extents.Max());
				MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref broadcast_sphere, broadcast_entities);

				foreach (IMyRadioAntenna antenna in radio_antennas)
				{
					if (antenna.MarkedForClose || !antenna.IsWorking || !antenna.IsBroadcasting) continue;

					foreach (MyJumpGateConstruct target_grid in broadcast_entities.Where((entity) => entity is IMyCubeGrid && entity.Physics != null).Cast<IMyCubeGrid>().Select(MyJumpGateModSession.Instance.GetUnclosedJumpGateGrid).Distinct())
					{
						if (target_grid == null || target_grid == this || new_comm_linked.Contains(target_grid)) continue;

						foreach (IMyRadioAntenna target_antenna in grid.GetAttachedRadioAntennas())
						{
							if (target_antenna.IsWorking && target_antenna.IsBroadcasting && Vector3D.Distance(antenna.WorldMatrix.Translation, target_antenna.WorldMatrix.Translation) < Math.Min(antenna.Radius, target_antenna.Radius))
							{
								new_comm_linked.Add(target_grid);
								this.CommLinkedGridsPoll.Enqueue(target_grid);
								break;
							}
						}
					}
				}

				broadcast_entities.Clear();
			}

			this.LastCommLinkUpdate = DateTime.Now;
			this.UpdateCommLinkedGrids = false;

			using (this.CommLinkedLock.WithWriter())
			{
				if (this.CommLinkedGrids == null) this.CommLinkedGrids = new List<MyJumpGateConstruct>();
				else this.CommLinkedGrids.Clear();
				this.CommLinkedGrids.AddList(new_comm_linked);
			}
		}

		/// <summary>
		/// Does a pruning call to find all beacons within this construct's broadcast sphere
		/// </summary>
		private void SearchUpdateBeaconLinkedBeacons()
		{
			if (!this.UpdateBeaconLinkedBeacons || !MyNetworkInterface.IsServerLike) return;

			using (this.BeaconLinkedLock.WithWriter())
			{
				if (this.GetAttachedRadioAntennas().Any((antenna) => !antenna.MarkedForClose && antenna.IsWorking && antenna.IsBroadcasting))
				{
					if (this.BeaconLinks == null) this.BeaconLinks = new List<MyBeaconLinkWrapper>();
					else this.BeaconLinks.Clear();
					List<MyEntity> broadcast_entities = new List<MyEntity>();
					BoundingBoxD world_aabb = this.GetCombinedAABB();
					BoundingSphereD broadcast_sphere = new BoundingSphereD(world_aabb.Center, MyJumpGateModSession.Instance.MaxBeaconBroadcastRadius + world_aabb.Extents.Max());
					MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref broadcast_sphere, broadcast_entities);

					foreach (MyJumpGateConstruct target_grid in broadcast_entities.Where((entity) => entity is IMyCubeGrid && entity.Physics != null).Cast<IMyCubeGrid>().Select(MyJumpGateModSession.Instance.GetUnclosedJumpGateGrid).Distinct())
					{
						if (target_grid == null || target_grid == this) continue;

						foreach (IMyRadioAntenna antenna in this.GetAttachedRadioAntennas())
						{
							foreach (IMyBeacon beacon in target_grid.GetAttachedBeacons())
							{
								MyBeaconLinkWrapper wrapper;
								if (beacon.MarkedForClose || !beacon.IsWorking || Vector3D.DistanceSquared(beacon.WorldMatrix.Translation, antenna.WorldMatrix.Translation) > beacon.Radius * beacon.Radius || this.BeaconLinks.Contains(wrapper = new MyBeaconLinkWrapper(beacon))) continue;
								this.BeaconLinks.Add(wrapper);
							}
						}
					}

					this.LastCommLinkUpdate = DateTime.Now;
					this.UpdateBeaconLinkedBeacons = false;
				}
			}
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// </summary>
		/// <returns>True if any working antennas exist on this construct</returns>
		public bool HasCommLink()
		{
			if (this.Closed) return false;
			foreach (KeyValuePair<long, IMyTerminalBlock> block in this.CommBlocks) if ((block.Value is IMyRadioAntenna || block.Value is IMyLaserAntenna) && block.Value.IsWorking) return true;
			return false;
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
				MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet
				{
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
				MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet
				{
					PacketType = MyPacketTypeEnum.BEACON_LINKED,
					TargetID = 0,
					Broadcast = false,
				};
				packet.Payload(this.CubeGridID);
				packet.Send();
			}

			return beacon_links;
		}
		#endregion
	}
}
