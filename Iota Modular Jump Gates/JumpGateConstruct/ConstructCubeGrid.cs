using IOTA.ModularJumpGates.CubeBlock;
using IOTA.ModularJumpGates.Extensions;
using IOTA.ModularJumpGates.JumpGates;
using IOTA.ModularJumpGates.Session;
using IOTA.ModularJumpGates.Util;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;

namespace IOTA.ModularJumpGates.JumpGateConstruct
{
	internal partial class MyJumpGateConstruct
	{
		#region Private Variables
		/// <summary>
		/// The master map of CubeGrids in this construct
		/// </summary>
		private ConcurrentDictionary<long, IMyCubeGrid> CubeGrids = new ConcurrentDictionary<long, IMyCubeGrid>();
		#endregion

		#region Public Variables
		/// <summary>
		/// The current ID of this construct's cube grid
		/// </summary>
		public long CubeGridID
		{
			get
			{
				if (this.IsSuspended || this.CubeGrid == null) return this.SuspendedCubeGridID;
				return (this.SuspendedCubeGridID = this.CubeGrid.EntityId);
			}
		}

		/// <summary>
		/// The custom name of this construct's primary cube grid
		/// </summary>
		public string PrimaryCubeGridCustomName { get; private set; }

		/// <summary>
		/// The main cube grid for this construct
		/// </summary>
		public IMyCubeGrid CubeGrid { get; private set; }

		/// <summary>
		/// Gets all big owners for all grids in this construct
		/// </summary>
		public List<ulong> BigOwners => this.CubeGrids?.Where((pair) => pair.Value != null && !pair.Value.Closed).SelectMany((pair) => pair.Value.BigOwners).Select(MyAPIGateway.Players.TryGetSteamId).Where((id) => id != 0).Distinct().ToList();

		/// <summary>
		/// Gets all small owners for all grids in this construct
		/// </summary>
		public List<ulong> SmallOwners => this.CubeGrids?.Where((pair) => pair.Value != null && !pair.Value.Closed).SelectMany((pair) => pair.Value.SmallOwners).Select(MyAPIGateway.Players.TryGetSteamId).Where((id) => id != 0).Distinct().ToList();
		#endregion

		#region Private Methods
		/// <summary>
		/// Callback for when a grid is added to this construct
		/// </summary>
		/// <param name="grid">The added grid</param>
		private void OnGridAdded(IMyCubeGrid grid)
		{
			if (grid == null) return;
			grid.OnBlockAdded += this.OnBlockAdded;
			grid.OnBlockRemoved += this.OnBlockRemoved;
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

			if (!this.Closed)
			{
				foreach (KeyValuePair<long, IMyTerminalBlock> pair in this.CommBlocks) if (pair.Value.CubeGrid == grid) this.CommBlocks.Remove(pair.Value.EntityId);
				foreach (KeyValuePair<long, MyCubeBlockBase> pair in this.JumpGateBlocks) if (pair.Value.CubeGridID == grid.EntityId) this.JumpGateBlocks.Remove(pair.Value.BlockID);
			}

			MyJumpGateConstruct parent = MyJumpGateModSession.Instance.GetJumpGateGrid(grid);
			if (parent == null && (MyJumpGateModSession.Instance.SessionStatus == MySessionStatusEnum.LOADING || MyJumpGateModSession.Instance.SessionStatus == MySessionStatusEnum.RUNNING)) parent = MyJumpGateModSession.Instance.AddCubeGridToSession(grid);
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
				MyJumpGateConstruct closed_parent = MyJumpGateModSession.Instance.GetUnclosedJumpGateGrid(closed.Key);
				if (closed_parent == null) MyJumpGateModSession.Instance.AddCubeGridToSession(closed.Value);
				grids_updated = true;
			}

			if (grids_updated)
			{
				// Update connected blocks
				this.UpdateDriveMaxRaycastDistances = true;
				int drive_count = this.GetAttachedJumpGateDrives().Count();
				this.GridBlocks.Clear();

				foreach (KeyValuePair<long, MyCubeBlockBase> pair in this.JumpGateBlocks) if (pair.Value.IsClosed || !this.CubeGrids.ContainsKey(pair.Value.CubeGridID)) this.JumpGateBlocks.Remove(pair.Key);
				foreach (KeyValuePair<long, IMyTerminalBlock> pair in this.CommBlocks) if (pair.Value.Closed || pair.Value.MarkedForClose || !this.CubeGrids.ContainsKey(pair.Value.CubeGrid.EntityId)) this.CommBlocks.Remove(pair.Key);

				foreach (KeyValuePair<long, IMyCubeGrid> pair in this.CubeGrids)
				{
					foreach (IMyUpgradeModule block in pair.Value.GetFatBlocks<IMyUpgradeModule>())
					{
						MyCubeBlockBase block_base;
						if (block.MarkedForClose || block.Closed || (block_base = MyJumpGateModSession.GetBlockAsCubeBlockBase(block)) == null) continue;
						this.JumpGateBlocks[block.EntityId] = block_base;
					}

					foreach (IMyLaserAntenna antenna in pair.Value.GetFatBlocks<IMyLaserAntenna>()) if (!antenna.MarkedForClose && !antenna.Closed) this.CommBlocks[antenna.EntityId] = antenna;
					foreach (IMyRadioAntenna antenna in pair.Value.GetFatBlocks<IMyRadioAntenna>()) if (!antenna.MarkedForClose && !antenna.Closed) this.CommBlocks[antenna.EntityId] = antenna;

					foreach (IMyBeacon antenna in pair.Value.GetFatBlocks<IMyBeacon>()) if (!antenna.MarkedForClose && !antenna.Closed)
						{
							this.CommBlocks[antenna.EntityId] = antenna;
							MyJumpGateModSession.Instance.UpdateMaxBeaconBroadcastRadius(antenna);
						}

					this.GridBlocks.AddRange(((MyCubeGrid) pair.Value).CubeBlocks);
				}

				if (this.GetAttachedJumpGateDrives().Count() != drive_count)
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
		/// <param name="grid_id">The sub-grid ID</param>
		/// <returns>True if the grid ID is part of this construct</returns>
		public bool HasCubeGrid(long grid_id)
		{
			return !this.Closed && ((this.IsSuspended && this.SuspendedCubeGridIDs.Contains(grid_id)) || (!this.IsSuspended && this.CubeGrids.ContainsKey(grid_id)));
		}

		/// <summary>
		/// </summary>
		/// <param name="grid">The sub-grid cube grid</param>
		/// <returns>True if the grid is part of this construct</returns>
		public bool HasCubeGrid(IMyCubeGrid grid)
		{
			return !this.Closed && grid != null && ((this.IsSuspended && this.SuspendedCubeGridIDs.Contains(grid.EntityId)) || (!this.IsSuspended && this.CubeGrids.ContainsKey(grid.EntityId)));
		}

		/// <param name="entity">The entity to check</param>
		/// <returns>Whether entity is a functional part of this construct</returns>
		public bool IsEntityPartOfConstruct(MyEntity entity)
		{
			if (entity == null) return false;
			else if (entity is MyCubeGrid) return this.HasCubeGrid(entity.EntityId);
			else return this.JumpGates.All((pair) => !pair.Value.IsEntityPartOfJumpGate(entity));
		}

		/// <summary>
		/// Checks whether thie construct intersects the specified ellipsoid
		/// </summary>
		/// <param name="ellipsoid">The ellipsoid to check against</param>
		/// <returns>Whether at least 4 bounding box vertices are contained</returns>
		public bool IsConstructWithinBoundingEllipsoid(ref BoundingEllipsoidD ellipsoid)
		{
			if (this.Closed) return false;
			foreach (KeyValuePair<long, IMyCubeGrid> pair in this.CubeGrids) if (ellipsoid.IsPointInEllipse(pair.Value.WorldMatrix.Translation)) return true;
			foreach (IMySlimBlock block in this.GridBlocks) if (ellipsoid.IsPointInEllipse(block.CubeGrid.GridIntegerToWorld(block.Position))) return true;
			return false;
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
		/// Calculates this construct's orientation based on the main cockpit or other blocks<br />
		/// If no main cockpit is set, orientation will be calculated from the most common block orientation from wheels, cockpits, and doors
		/// </summary>
		/// <returns>This construct's orientation</returns>
		public MatrixD GetConstructCalculatedOrienation()
		{
			List<IMyCockpit> cockpits = new List<IMyCockpit>();
			List<MatrixD> matrices = new List<MatrixD>();
			bool main_cockpit_found = false;

			foreach (KeyValuePair<long, IMyCubeGrid> pair in this.CubeGrids)
			{
				if (pair.Value == null || pair.Value.MarkedForClose || pair.Value.Physics == null) continue;
				cockpits.AddRange(pair.Value.GetFatBlocks<IMyCockpit>());
				IMyCockpit cockpit = cockpits.FirstOrDefault((block) => block.IsMainCockpit);

				if (cockpit != null && main_cockpit_found)
				{
					matrices.Add(cockpit.WorldMatrix.Round(6));
					continue;
				}
				else if (cockpit != null)
				{
					main_cockpit_found = true;
					matrices.Clear();
					matrices.Add(cockpit.WorldMatrix.Round(6));
					continue;
				}
				else if (main_cockpit_found) continue;

				matrices.AddRange(cockpits.Select((cp) => cp.WorldMatrix.Round(6)));
				matrices.AddRange(pair.Value.GetFatBlocks<IMyDoor>().Select((door) => door.WorldMatrix.Round(6)));
				matrices.AddRange(pair.Value.GetFatBlocks<IMyMotorSuspension>().Select((wheel) => wheel.WorldMatrix.Round(6)));
				cockpits.Clear();
			}

			return (matrices.Count == 0) ? this.CubeGrid.WorldMatrix : matrices.GroupBy(i => i).OrderByDescending(grp => grp.Count()).Select(grp => grp.Key).First();
		}

		/// <summary>
		/// Gets the first physics component from this construct's cube grids
		/// </summary>
		/// <returns></returns>
		public MyPhysicsComponentBase GetCubeGridPhysics()
		{
			if (this.Closed) return null;
			else if (this.CubeGrid.Physics != null) return this.CubeGrid.Physics;
			foreach (KeyValuePair<long, IMyCubeGrid> pair in this.CubeGrids) if (pair.Value.Physics != null) return pair.Value.Physics;
			return null;
		}

		/// <summary>
		/// Gets all cube grids in this construct
		/// </summary>
		/// <returns>An IEnumerable referencing all grids</returns>
		public IEnumerable<IMyCubeGrid> GetCubeGrids()
		{
			return this.CubeGrids?.Select((pair) => pair.Value) ?? Enumerable.Empty<IMyCubeGrid>();
		}
		#endregion
	}
}
