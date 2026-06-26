using IOTA.ModularJumpGates.CubeBlock;
using IOTA.ModularJumpGates.JumpGates;
using IOTA.ModularJumpGates.ModConfiguration;
using IOTA.ModularJumpGates.Session;
using IOTA.ModularJumpGates.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI;
using VRageMath;

namespace IOTA.ModularJumpGates.JumpGateConstruct
{
	internal partial class MyJumpGateConstruct
	{
		#region Private Variables
		/// <summary>
		/// The next jump gate ID
		/// </summary>
		private long NextJumpGateID = 0;

		/// <summary>
		/// A queue of all IDs from closed gates
		/// </summary>
		private Queue<long> ClosedJumpGateIDs = new Queue<long>();

		/// <summary>
		/// A list of all jump gate drive combinations<br />
		/// List is non-repeating
		/// </summary>
		private List<KeyValuePair<MyJumpGateDrive, MyJumpGateDrive>> DriveCombinations = null;

		/// <summary>
		/// The master map of jump gates this construct has
		/// </summary>
		private ConcurrentDictionary<long, MyJumpGate> JumpGates = new ConcurrentDictionary<long, MyJumpGate>();
		#endregion

		#region Public Variables
		/// <summary>
		/// Whether to update jump gate intersections on next tick<br />
		/// Gate drive intersections will be updated and gates reconstructed on next tick
		/// </summary>
		public bool MarkUpdateJumpGates { get; private set; } = true;

		/// <summary>
		/// The gate this construct is currently being jumped by or null
		/// </summary>
		public MyJumpGate BatchingGate = null;
		#endregion

		#region Private Methods
		/// <summary>
		/// Updates the max raycast distances for all drives
		/// </summary>
		private void UpdateDriveRaycastDistances()
		{
			if (!this.UpdateDriveMaxRaycastDistances || this.LocalGameTick % 60 != 0) return;
			List<Vector3I> raycast_cells = new List<Vector3I>();
			this.UpdateDriveMaxRaycastDistances = false;

			foreach (MyJumpGateDrive drive in this.GetAttachedJumpGateDrives())
			{
				double closest_distance = drive.Configuration.DriveRaycastDistance;
				Vector3D raycast_start = drive.GetDriveRaycastStartpoint();
				Vector3D raycast_end = drive.GetDriveRaycastEndpoint(closest_distance);

				foreach (KeyValuePair<long, IMyCubeGrid> grid in this.CubeGrids)
				{
					if (grid.Value.MarkedForClose) continue;
					grid.Value.RayCastCells(raycast_start, raycast_end, raycast_cells);

					foreach (Vector3I cell in raycast_cells)
					{
						IMySlimBlock block = grid.Value.GetCubeBlock(cell);
						if (block == null || block.FatBlock == drive.TerminalBlock) continue;
						closest_distance = Math.Min(closest_distance, Vector3D.Distance(grid.Value.GridIntegerToWorld(cell), raycast_start));
					}

					raycast_cells.Clear();
				}

				this.MarkUpdateJumpGates = this.MarkUpdateJumpGates || drive.MaxRaycastDistance != closest_distance;
				drive.MaxRaycastDistance = closest_distance;
			}
		}

		/// <summary>
		/// Rebuilds all jump gates in this construct based on drive intersections
		/// </summary>
		private void RebuildJumpGates()
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
				Vector3D midpoint;

				double angle_a = Vector3D.Angle(side_b, -side_c);
				double angle_b = Vector3D.Angle(side_a, side_c);
				double ico_angle = angle_a + angle_b;
				double angle_c = Math.PI - ico_angle;
				double c_ratio = (side_c.Length() / Math.Sin(angle_c));

				if (ico_angle == 0)
				{
					midpoint = (drive1_pos2 + drive2_pos2) / 2d;
					MyModConfigurationV1.MyLocalDriveConfiguration drive_configuration = drive1.Configuration;
					if (Vector3D.Distance(drive1_pos1, midpoint) > drive_configuration.DriveRaycastWidth || Vector3D.Distance(drive2_pos1, midpoint) > drive_configuration.DriveRaycastWidth) continue;
				}
				else if (ico_angle < Math.PI)
				{
					double side_a_len = Math.Sin(angle_a) * c_ratio;
					double side_b_len = Math.Sin(angle_b) * c_ratio;
					if (side_a_len > drive2.MaxRaycastDistance || side_b_len > drive1.MaxRaycastDistance) continue;
					Vector3D _side_a = drive1.GetDriveRaycastEndpoint(side_a_len);
					Vector3D _side_b = drive2.GetDriveRaycastEndpoint(side_b_len);
					midpoint = (_side_a + _side_b) / 2d;
					MyModConfigurationV1.MyLocalDriveConfiguration drive_configuration = drive1.Configuration;
					if (Vector3D.Distance(_side_a, midpoint) > drive_configuration.DriveRaycastWidth || Vector3D.Distance(_side_b, midpoint) > drive_configuration.DriveRaycastWidth) continue;
				}
				else continue;

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
						foreach (MyJumpGateDrive other_drive in _intersection.IntersectingDrives) intersecting_drives[other_drive] = intersection;
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

			uint gate_count = MyJumpGateModSession.Instance.Configuration.ConstructConfiguration.MaxTotalGatesPerConstruct.Value;
			uint large_gate_count = MyJumpGateModSession.Instance.Configuration.ConstructConfiguration.MaxLargeGatesPerConstruct.Value;
			uint small_gate_count = MyJumpGateModSession.Instance.Configuration.ConstructConfiguration.MaxSmallGatesPerConstruct.Value;
			List<MyJumpGateDrive> unmapped_drives = this.GetAttachedJumpGateDrives().ToList();
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
				MyJumpGate jump_gate = null;

				if (this.JumpGates.TryGetValue(primary_id, out jump_gate) && !jump_gate.MarkClosed)
				{
					jump_gate.ConstructMatrix = this.CubeGrid.WorldMatrix;
					jump_gate.WorldJumpNode = jump_node;
					jump_gate.UpdateDriveIntersectNodes(node_group);
					jump_gate.SetJumpSpaceEllipsoidDirty();
					foreach (MyJumpGateDrive drive in drive_group) drive.SetAttachedJumpGate(jump_gate);
				}
				else if (jump_gate != null && jump_gate.MarkClosed && !jump_gate.Closed) MyJumpGateModSession.Instance.CloseGate(jump_gate);
				else if (jump_gate == null)
				{
					this.JumpGates[primary_id] = (jump_gate = new MyJumpGate(this, primary_id, ref jump_node, node_group));
					foreach (MyJumpGateDrive drive in drive_group) drive.SetAttachedJumpGate(jump_gate);
				}
			}

			foreach (MyJumpGateDrive unmapped in unmapped_drives) unmapped.SetAttachedJumpGate(null);
			foreach (KeyValuePair<long, MyJumpGate> pair in this.JumpGates) if (!mapped_gate_ids.Contains(pair.Key)) pair.Value.Dispose();
			this.SetDirty();
		}

		/// <summary>
		/// Ticks all jump gates in this construct
		/// </summary>
		/// <param name="full_gate_update">Whether to do a full gate update</param>
		/// <param name="gate_entity_update">Whether to update all gate jump space entities</param>
		private void TickUpdateJumpGates(bool full_gate_update, bool gate_entity_update)
		{
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

				jump_gate.Update(this.LocalGameTick, full_gate_update, gate_entity_update);
				if (!jump_gate.MarkClosed && (!jump_gate.IsValid() || jump_gate.JumpGateGrid != this)) jump_gate.Dispose();
			}
		}
		#endregion

		#region Public Methods
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
				foreach (MyJumpGateDrive drive in this.GetAttachedJumpGateDrives()) if (drive.JumpGateID == old_uid.GetJumpGate()) drive.SetAttachedJumpGate(gate);
				foreach (MyJumpGateController controller in this.GetAttachedJumpGateControllers()) if (controller.BlockSettings.JumpGateID() == old_uid.GetJumpGate()) controller.AttachedJumpGate(gate);

				foreach (MyJumpGate source in all_gates)
				{
					MyJumpGateWaypoint dest = source.Controller?.BlockSettings.SelectedWaypoint();
					if (source.Controller == null || dest == null || dest.WaypointType != MyWaypointType.JUMP_GATE || dest.JumpGate != old_uid) continue;
					source.Controller.BlockSettings.SelectedWaypoint(new MyJumpGateWaypoint(gate));
				}
			}
		}

		/// <summary>
		/// Marks this construct for gate reconstruction<br />
		/// Gate drive intersections will be updated and gates reconstructed on next tick
		/// </summary>
		/// <param name="update_drive_combinations">Whether to recalculate drive combinations</param>
		public void MarkGatesForUpdate(bool update_drive_combinations = false)
		{
			if (MyNetworkInterface.IsStandaloneMultiplayerClient) return;
			this.MarkUpdateJumpGates = !this.Closed;

			if (update_drive_combinations)
			{
				this.DriveCombinations = null;
				this.UpdateDriveCombinations(ref update_drive_combinations);
			}

			Logger.Debug($"[{this.CubeGridID}] Marked all gates for reconstruct", 5);
		}

		/// <summary>
		/// Requests a jump grid update from the server<br />
		/// Does nothing if singleplayer or server<br />
		/// Does nothing if construct is suspended
		/// </summary>
		public void RequestGateUpdate()
		{
			if (this.Closed || MyNetworkInterface.IsServerLike || this.IsSuspended) return;
			MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet
			{
				PacketType = MyPacketTypeEnum.UPDATE_GRID,
				Broadcast = false,
				TargetID = 0,
			};
			packet.Payload<MySerializedJumpGateConstruct>(null);
			packet.Send();
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
		/// Gets all jump gates in this construct
		/// </summary>
		/// <returns>An IEnumerable referencing all jump gates</returns>
		public IEnumerable<MyJumpGate> GetJumpGates()
		{
			return this.JumpGates?.Values.AsEnumerable() ?? Enumerable.Empty<MyJumpGate>();
		}
		#endregion
	}
}
