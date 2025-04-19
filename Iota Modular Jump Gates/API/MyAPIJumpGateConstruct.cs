using IOTA.ModularJumpGates.API.CubeBlock;
using IOTA.ModularJumpGates.CubeBlock;
using IOTA.ModularJumpGates.Util;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;

namespace IOTA.ModularJumpGates.API
{
	public class MyAPIJumpGateConstruct : MyAPIInterface
	{
		internal MyJumpGateConstruct Construct;

		internal MyAPIJumpGateConstruct(MyJumpGateConstruct grid)
		{
			this.Construct = grid;
		}

		#region "object" Methods
		/// <summary>
		/// The hashcode for this object
		/// </summary>
		/// <returns>The hashcode of this construct's main cube grid</returns>
		public override int GetHashCode()
		{
			return this.Construct.GetHashCode();
		}
		#endregion

		#region Public API Variables
		/// <summary>
		/// Whether this construct is closed
		/// </summary>
		public bool Closed => this.Construct.Closed;

		/// <summary>
		/// Whether this construct is marked for close
		/// </summary>
		public bool MarkClosed => this.Construct.MarkClosed;

		/// <summary>
		/// Whether this construct is suspended from updates<br />
		/// Only used on muliplayer clients when grid is not in scene
		/// </summary>
		public bool IsSuspended => this.Construct.IsSuspended;

		/// <summary>
		/// Whether to update jump gate intersections on next tick<br />
		/// Gate drive intersections will be updated and gates reconstructed on next tick
		/// </summary>
		public bool MarkUpdateJumpGates => this.Construct.MarkUpdateJumpGates;

		/// <summary>
		/// Whether this construct should be synced<br/>
		/// If true, will be synced on next component tick
		/// </summary>
		public bool IsDirty => this.Construct.IsDirty;

		/// <summary>
		/// Whether this construct is fully initialized<br />
		/// True when all gates are constructed and resources initialized
		/// </summary>
		public bool FullyInitialized => this.Construct.FullyInitialized;

		/// <summary>
		/// The current ID of this construct's cube grid
		/// </summary>
		public long CubeGridID => this.Construct.CubeGridID;

		/// <summary>
		/// The custom name of this construct's primary cube grid
		/// </summary>
		public string PrimaryCubeGridCustomName => this.Construct.PrimaryCubeGridCustomName;

		/// <summary>
		/// The main cube grid for this construct
		/// </summary>
		public IMyCubeGrid CubeGrid => this.Construct.CubeGrid;
		#endregion

		#region Public API Methods
		/// <summary>
		/// Destroys all grids attached to this construct
		/// </summary>
		public void Destroy()
		{
			this.Construct.Destroy();
		}

		/// <summary>
		/// Marks this construct for gate reconstruction<br />
		/// Gate drive intersections will be updated and gates reconstructed on next tick
		/// </summary>
		public void MarkGatesForUpdate()
		{
			this.Construct.MarkGatesForUpdate();
		}

		/// <summary>
		/// Requests a jump grid update from the server<br />
		/// Does nothing if singleplayer or server<br />
		/// Does nothing if construct is suspended
		/// </summary>
		public void RequestGateUpdate()
		{
			this.Construct.RequestGateUpdate();
		}

		/// <summary>
		/// Marks this construct as dirty<br />
		/// It will be synced to clients on next tick
		/// </summary>
		public void SetDirty()
		{
			this.Construct.SetDirty();
		}

		/// <summary>
		/// Sets whether all parts of this construct should be static
		/// </summary>
		/// <param name="is_static">Staticness</param>
		public void SetConstructStaticness(bool is_static)
		{
			this.Construct.SetConstructStaticness(is_static);
		}

		/// <summary>
		/// Gets all jump gate controllers in this construct
		/// </summary>
		/// <param name="controllers">All attached controllers<br />List will not be cleared</param>
		/// <param name="filter">A predicate to match controllers against</param>
		public void GetAttachedJumpGateControllers(List<MyAPIJumpGateController> controllers, Func<MyAPIJumpGateController, bool> filter = null)
		{
			this.Construct.GetAttachedJumpGateControllers(controllers, MyAPISession.GetNewBlock, (block) => {
				MyAPIJumpGateController api = MyAPISession.GetNewBlock(block);
				try { return filter == null || filter(api); }
				finally { api.Release(); }
			});
		}

		/// <summary>
		/// Gets all jump gate controllers in this construct as the specified type
		/// </summary>
		/// <param name="controllers">All attached controllers<br />List will not be cleared</param>
		/// <param name="transformer">A transformer converting a controller to the specified type</param>
		/// <param name="filter">A predicate to match controllers against</param>
		public void GetAttachedJumpGateControllers<T>(List<T> controllers, Func<MyAPIJumpGateController, T> transformer, Func<MyAPIJumpGateController, bool> filter = null)
		{
			this.Construct.GetAttachedJumpGateControllers(controllers, (block) => {
				MyAPIJumpGateController api = MyAPISession.GetNewBlock(block);
				try { return transformer(api); }
				finally { api.Release(); }
			}, (block) => {
				MyAPIJumpGateController api = MyAPISession.GetNewBlock(block);
				try { return filter == null || filter(api); }
				finally { api.Release(); }
			});
		}

		/// <summary>
		/// All jump gate drives in this construct
		/// </summary>
		/// <param name="drives">All attached drives<br />List will not be cleared</param>
		/// <param name="filter">A predicate to match jump gate drives against</param>
		public void GetAttachedJumpGateDrives(List<MyAPIJumpGateDrive> drives, Func<MyAPIJumpGateDrive, bool> filter = null)
		{
			this.Construct.GetAttachedJumpGateDrives(drives, MyAPISession.GetNewBlock, (block) => {
				MyAPIJumpGateDrive api = MyAPISession.GetNewBlock(block);
				try { return filter == null || filter(api); }
				finally { api.Release(); }
			});
		}

		/// <summary>
		/// All jump gate drives in this construct as the specified type
		/// </summary>
		/// <param name="drives">All attached drives<br />List will not be cleared</param>
		///	<param name="transformer">A transformer converting a drive to the specified type</param>
		/// <param name="filter">A predicate to match jump gate drives against</param>
		public void GetAttachedJumpGateDrives<T>(List<T> drives, Func<MyAPIJumpGateDrive, T> transformer, Func<MyAPIJumpGateDrive, bool> filter = null)
		{
			this.Construct.GetAttachedJumpGateDrives(drives, (block) => {
				MyAPIJumpGateDrive api = MyAPISession.GetNewBlock(block);
				try { return transformer(api); }
				finally { api.Release(); }
			}, (block) => {
				MyAPIJumpGateDrive api = MyAPISession.GetNewBlock(block);
				try { return filter == null || filter(api); }
				finally { api.Release(); }
			});
		}

		/// <summary>
		/// Gets all jump gate drives in this construct not bound to a jump gate
		/// </summary>
		/// <param name="drives">All attached unassociated drives<br />List will not be cleared</param>
		/// <param name="filter">A predicate to match jump gate drives against</param>
		public void GetAttachedUnassociatedJumpGateDrives(List<MyAPIJumpGateDrive> drives, Func<MyAPIJumpGateDrive, bool> filter = null)
		{
			this.Construct.GetAttachedUnassociatedJumpGateDrives(drives, MyAPISession.GetNewBlock, (block) => {
				MyAPIJumpGateDrive api = MyAPISession.GetNewBlock(block);
				try { return filter == null || filter(api); }
				finally { api.Release(); }
			});
		}

		/// <summary>
		/// All jump gate drives in this construct as the specified type
		/// </summary>
		/// <param name="drives">All attached unassociated drives<br />List will not be cleared</param>
		///	<param name="transformer">A transformer converting a drive to the specified type</param>
		/// <param name="filter">A predicate to match jump gate drives against</param>
		public void GetAttachedUnassociatedJumpGateDrives<T>(List<T> drives, Func<MyAPIJumpGateDrive, T> transformer, Func<MyAPIJumpGateDrive, bool> filter = null)
		{
			this.Construct.GetAttachedUnassociatedJumpGateDrives(drives, (block) => {
				MyAPIJumpGateDrive api = MyAPISession.GetNewBlock(block);
				try { return transformer(api); }
				finally { api.Release(); }
			}, (block) => {
				MyAPIJumpGateDrive api = MyAPISession.GetNewBlock(block);
				try { return filter == null || filter(api); }
				finally { api.Release(); }
			});
		}

		/// <summary>
		/// Gets all jump gate capacitors in this construct
		/// </summary>
		/// <param name="capacitors">All attached capacitors<br />List will not be cleared</param>
		/// <param name="filter">A predicate to match capacitors against</param>
		public void GetAttachedJumpGateCapacitors(List<MyAPIJumpGateCapacitor> capacitors, Func<MyAPIJumpGateCapacitor, bool> filter = null)
		{
			this.Construct.GetAttachedJumpGateCapacitors(capacitors, MyAPISession.GetNewBlock, (block) => {
				MyAPIJumpGateCapacitor api = MyAPISession.GetNewBlock(block);
				try { return filter == null || filter(api); }
				finally { api.Release(); }
			});
		}

		/// <summary>
		/// All jump gate capacitors in this construct as the specified type
		/// </summary>
		/// <param name="capacitors">All attached capacitors<br />List will not be cleared</param>
		///	<param name="transformer">A transformer converting a capacitor to the specified type</param>
		/// <param name="filter">A predicate to match jump gate capacitors against</param>
		public void GetAttachedJumpGateCapacitors<T>(List<T> capacitors, Func<MyAPIJumpGateCapacitor, T> transformer, Func<MyAPIJumpGateCapacitor, bool> filter = null)
		{
			this.Construct.GetAttachedJumpGateCapacitors(capacitors, (block) => {
				MyAPIJumpGateCapacitor api = MyAPISession.GetNewBlock(block);
				try { return transformer(api); }
				finally { api.Release(); }
			}, (block) => {
				MyAPIJumpGateCapacitor api = MyAPISession.GetNewBlock(block);
				try { return filter == null || filter(api); }
				finally { api.Release(); }
			});
		}

		/// <summary>
		/// Gets all laser antennas in this construct
		/// </summary>
		/// <param name="laser_antennas">All attached laser antennas<br />List will not be cleared</param>
		/// <param name="filter">A predicate to match laser antennas against</param>
		public void GetAttachedLaserAntennas(List<IMyLaserAntenna> laser_antennas, Func<IMyLaserAntenna, bool> filter = null)
		{
			this.Construct.GetAttachedLaserAntennas(laser_antennas, filter);
		}

		/// <summary>
		/// Gets all laser antennas in this construct as the specified type
		/// </summary>
		/// <param name="laser_antennas">All attached laser antennas<br />List will not be cleared</param>
		/// <param name="transformer">A transformer converting a laser antenna to the specified type</param>
		/// <param name="filter">A predicate to match laser antennas against</param>
		public void GetAttachedLaserAntennas<T>(List<T> laser_antennas, Func<IMyLaserAntenna, T> transformer, Func<IMyLaserAntenna, bool> filter = null)
		{
			this.Construct.GetAttachedLaserAntennas(laser_antennas, transformer, filter);
		}

		/// <summary>
		/// Gets all radio antennas in this construct
		/// </summary>
		/// <param name="radio_antennas">All attached radio antennas<br />List will not be cleared</param>
		/// <param name="filter">A predicate to match radio antennas against</param>
		public void GetAttachedRadioAntennas(List<IMyRadioAntenna> radio_antennas, Func<IMyRadioAntenna, bool> filter = null)
		{
			this.Construct.GetAttachedRadioAntennas(radio_antennas, filter);
		}

		/// <summary>
		/// Gets all radio antennas in this construct as the specified type
		/// </summary>
		/// <param name="radio_antennas">All attached radio antennas<br />List will not be cleared</param>
		/// <param name="transformer">A transformer converting a radio antenna to the specified type</param>
		/// <param name="filter">A predicate to match radio antennas against</param>
		public void GetAttachedRadioAntennas<T>(List<T> radio_antennas, Func<IMyRadioAntenna, T> transformer, Func<IMyRadioAntenna, bool> filter = null)
		{
			this.Construct.GetAttachedRadioAntennas(radio_antennas, transformer, filter);
		}

		/// <summary>
		/// Gets all beacons in this construct
		/// </summary>
		/// <param name="beacons">All attached beacons<br />List will not be cleared</param>
		/// <param name="filter">A predicate to match beacons against</param>
		public void GetAttachedBeacons(List<IMyBeacon> beacons, Func<IMyBeacon, bool> filter = null)
		{
			this.Construct.GetAttachedBeacons(beacons, filter);
		}

		/// <summary>
		/// Gets all beacons in this construct as the specified type
		/// </summary>
		/// <param name="beacons">All attached beacons<br />List will not be cleared</param>
		/// <param name="transformer">A transformer converting a beacons to the specified type</param>
		/// <param name="filter">A predicate to match beacons against</param>
		public void GetAttachedBeacons<T>(List<T> beacons, Func<IMyBeacon, T> transformer, Func<IMyBeacon, bool> filter = null)
		{
			this.Construct.GetAttachedBeacons(beacons, transformer, filter);
		}

		/// <summary>
		/// Gets all jump gates in this construct
		/// </summary>
		/// <param name="jump_gates">All attached jump gates<br />List will not be cleared</param>
		/// <param name="filter">A predicate to match jump gates against</param>
		public void GetJumpGates(List<MyAPIJumpGate> jump_gates, Func<MyAPIJumpGate, bool> filter = null)
		{
			this.Construct.GetJumpGates(jump_gates, MyAPISession.GetNewJumpGate, (gate) => {
				MyAPIJumpGate api = MyAPISession.GetNewJumpGate(gate);
				try { return filter == null || filter(api); }
				finally { api.Release(); }
			});
		}

		/// <summary>
		/// Gets all jump gates in this construct as the specified type
		/// </summary>
		/// <param name="jump_gates">All attached jump gates<br />List will not be cleared</param>
		/// <param name="transformer">A transformer converting a jump gate to the specified type</param>
		/// <param name="filter">A predicate to match jump gates against</param>
		public void GetJumpGates<T>(List<T> jump_gates, Func<MyAPIJumpGate, T> transformer, Func<MyAPIJumpGate, bool> filter = null)
		{
			this.Construct.GetJumpGates(jump_gates, (gate) => {
				MyAPIJumpGate api = MyAPISession.GetNewJumpGate(gate);
				try { return transformer(api); }
				finally { api.Release(); }
			}, (gate) => {
				MyAPIJumpGate api = MyAPISession.GetNewJumpGate(gate);
				try { return filter == null || filter(api); }
				finally { api.Release(); }
			});
		}

		/// <summary>
		/// Gets all cube grids in this construct
		/// </summary>
		/// <param name="grids">All attached cube grids<br />List will not be cleared</param>
		/// <param name="filter">A predicate to match cube grids against</param>
		public void GetCubeGrids(List<IMyCubeGrid> grids, Func<IMyCubeGrid, bool> filter = null)
		{
			this.Construct.GetCubeGrids(grids, filter);
		}

		/// <summary>
		/// Gets all cube grids in this construct as the specified type
		/// </summary>
		/// <param name="grids">All attached cube grids<br />List will not be cleared</param>
		/// <param name="transformer">A transformer converting a cube grid to the specified type</param>
		/// <param name="filter">A predicate to matchcube grids against</param>
		public void GetCubeGrids<T>(List<T> grids, Func<IMyCubeGrid, T> transformer, Func<IMyCubeGrid, bool> filter = null)
		{
			this.Construct.GetCubeGrids(grids, transformer, filter);
		}

		/// <summary>
		/// Does a BFS search of all constructs accessible to this one via antennas
		/// </summary>
		/// <param name="comm_linked_grids">A list of grids to populate or null to update only<br />List will not be cleared</param>
		/// <param name="filter">A predicate to match jump gate constructs against</param>
		public void GetCommLinkedJumpGateGrids(List<MyAPIJumpGateConstruct> comm_linked_grids, Func<MyAPIJumpGateConstruct, bool> filter = null)
		{
			List<MyJumpGateConstruct> grids = new List<MyJumpGateConstruct>(comm_linked_grids.Capacity);
			this.Construct.GetCommLinkedJumpGateGrids(grids);

			foreach (MyJumpGateConstruct grid in grids)
			{
				MyAPIJumpGateConstruct api = MyAPISession.GetNewConstruct(grid);
				bool keep = false;
				try { if (keep = (filter == null || filter(api))) comm_linked_grids.Add(api); }
				finally { if (!keep) api.Release(); }
			}
		}

		/// <summary>
		/// Does a search for all beacons that this grid can see
		/// </summary>
		/// <param name="beacons">A list of beacons to populate or null to update only<br />List will not be cleared</param>
		/// <param name="filter">A predicate to match beacons against</param>
		public void GetBeaconsWithinReverseBroadcastSphere(List<MyBeaconLinkWrapper> beacons, Func<MyBeaconLinkWrapper, bool> filter = null)
		{
			this.Construct.GetBeaconsWithinReverseBroadcastSphere(beacons, filter);
		}

		/// <summary>
		/// Gets a list of blocks from this construct that intersect the specified bounding ellipsoid
		/// </summary>
		/// <param name="ellipsoid">The bounding ellipsoid</param>
		/// <param name="contained_blocks">An optional collection to populate containing all blocks within the ellipsoid</param>
		/// <param name="uncontained_blocks">An optional collection to populate containing all blocks outside the ellipsoid</param>
		public void GetBlocksIntersectingEllipsoid(ref BoundingEllipsoidD ellipsoid, ICollection<IMySlimBlock> contained_blocks = null, ICollection<IMySlimBlock> uncontained_blocks = null)
		{
			this.Construct.GetBlocksIntersectingEllipsoid(ref ellipsoid, contained_blocks, uncontained_blocks);
		}

		/// <summary>
		/// Whether this grid is valid
		/// </summary>
		/// <returns>True if valid</returns>
		public bool IsValid()
		{
			return this.Construct.IsValid();
		}

		/// <summary>
		/// Checks construct staticness
		/// </summary>
		/// <returns>Whether at least one grid is static</returns>
		public bool IsStatic()
		{
			return this.Construct.IsStatic();
		}

		/// <summary>
		/// </summary>
		/// <returns>True if any working antennas exist on this construct</returns>
		public bool HasCommLink()
		{
			return this.Construct.HasCommLink();
		}

		/// <summary>
		/// </summary>
		/// <returns>True if this construct completed at least one tick</returns>
		public bool AtLeastOneUpdate()
		{
			return this.Construct.AtLeastOneUpdate();
		}

		/// <summary>
		/// </summary>
		/// <param name="grid_id">The sub-grid ID</param>
		/// <returns>True if the grid ID is part of this construct</returns>
		public bool HasCubeGrid(long grid_id)
		{
			return this.Construct.HasCubeGrid(grid_id);
		}

		/// <summary>
		/// </summary>
		/// <param name="grid">The sub-grid cube grid</param>
		/// <returns>True if the grid is part of this construct</returns>
		public bool HasCubeGrid(IMyCubeGrid grid)
		{
			return this.Construct.HasCubeGrid(grid);
		}

		/// <summary>
		/// Checks if the specified grid is reachable from this one via antennas
		/// </summary>
		/// <param name="grid">The target grid</param>
		/// <param name="update">If true, requests update to internal list</param>
		/// <returns>Whether target grid is comm linked</returns>
		public bool IsConstructCommLinked(MyAPIJumpGateConstruct grid, bool update)
		{
			return this.Construct.IsConstructCommLinked(grid.Construct, update);
		}

		/// <summary>
		/// Checks if the specified beacon is seen from this grid
		/// This method will not update the internal beacon-linked list before checking
		/// </summary>
		/// <param name="beacon">The target beacon</param>
		/// <param name="update">If true, requests update to internal list</param>
		/// <returns>Whether target beacon is beacon linked</returns>
		public bool IsBeaconWithinReverseBroadcastSphere(MyBeaconLinkWrapper beacon, bool update)
		{
			return this.Construct.IsBeaconWithinReverseBroadcastSphere(beacon, update);
		}

		/// <summary>
		/// Gets the number of controllers matching the specified predicate<br />
		/// Gets the number of all controllers if predicate is null
		/// </summary>
		/// <param name="predicate">The predicate to filter controllers by</param>
		/// <returns>The number of matching controllers</returns>
		public int GetControllerCount(Func<MyAPIJumpGateController, bool> predicate = null)
		{
			return (predicate == null) ? this.Construct.GetControllerCount() : this.Construct.GetControllerCount((block) => {
				MyAPIJumpGateController api = MyAPISession.GetNewBlock(block);
				try { return predicate(api); }
				finally { api.Release(); }
			});
		}

		/// <summary>
		/// Gets the number of drives matching the specified predicate<br />
		/// Gets the number of all drives if predicate is null
		/// </summary>
		/// <param name="predicate">The predicate to filter drives by</param>
		/// <returns>The number of matching drives</returns>
		public int GetDriveCount(Func<MyAPIJumpGateDrive, bool> predicate = null)
		{
			return (predicate == null) ? this.Construct.GetDriveCount() : this.Construct.GetDriveCount((block) => {
				MyAPIJumpGateDrive api = MyAPISession.GetNewBlock(block);
				try { return predicate(api); }
				finally { api.Release(); }
			});
		}

		/// <summary>
		/// Gets the number of capacitors matching the specified predicate<br />
		/// Gets the number of all capacitors if predicate is null
		/// </summary>
		/// <param name="predicate">The predicate to filter capacitors by</param>
		/// <returns>The number of matching capacitors</returns>
		public int GetCapacitorCount(Func<MyAPIJumpGateCapacitor, bool> predicate = null)
		{
			return (predicate == null) ? this.Construct.GetCapacitorCount() : this.Construct.GetCapacitorCount((block) => {
				MyAPIJumpGateCapacitor api = MyAPISession.GetNewBlock(block);
				try { return predicate(api); }
				finally { api.Release(); }
			});
		}

		/// <summary>
		/// Gets the number of laser antennas matching the specified predicate<br />
		/// Gets the number of all laser antennas if predicate is null
		/// </summary>
		/// <param name="predicate">The predicate to filter laser antennas by</param>
		/// <returns>The number of matching laser antennas</returns>
		public int GetLaserAntennaCount(Func<IMyLaserAntenna, bool> predicate = null)
		{
			return this.Construct.GetLaserAntennaCount(predicate);
		}

		/// <summary>
		/// Gets the number of radio antennas matching the specified predicate<br />
		/// Gets the number of all radio antennas if predicate is null
		/// </summary>
		/// <param name="predicate">The predicate to filter radio antennas by</param>
		/// <returns>The number of matching radio antennas</returns>
		public int GetRadioAntennaCount(Func<IMyRadioAntenna, bool> predicate = null)
		{
			return this.Construct.GetRadioAntennaCount(predicate);
		}

		/// <summary>
		/// Gets the number of beacons matching the specified predicate<br />
		/// Gets the number of all beacons if predicate is null
		/// </summary>
		/// <param name="predicate">The predicate to filter beacons by</param>
		/// <returns>The number of matching beacons</returns>
		public int GetBeaconCount(Func<IMyBeacon, bool> predicate = null)
		{
			return this.Construct.GetBeaconCount(predicate);
		}

		/// <summary>
		/// Gets the number of cube grids matching the specified predicate<br />
		/// Gets the number of all cube grids if predicate is null
		/// </summary>
		/// <param name="predicate">The predicate to filter cube grids by</param>
		/// <returns>The number of matching cube grids</returns>
		public int GetCubeGridCount(Func<IMyCubeGrid, bool> predicate = null)
		{
			return this.Construct.GetCubeGridCount(predicate);
		}

		/// <summary>
		/// Gets the number of jump gates matching the specified predicate<br />
		/// Gets the number of all jump gates if predicate is null
		/// </summary>
		/// <param name="predicate">The predicate to filter jump gates by</param>
		/// <returns>The number of matching jump gates</returns>
		public int GetJumpGateCount(Func<MyAPIJumpGate, bool> predicate = null)
		{
			return (predicate == null) ? this.Construct.GetJumpGateCount() : this.Construct.GetJumpGateCount((gate) => {
				MyAPIJumpGate api = MyAPISession.GetNewJumpGate(gate);
				try { return predicate(api); }
				finally { api.Release(); }
			});
		}

		/// <summary>
		/// Gets the number of blocks matching the specified predicate<br />
		/// Gets the number of all blocks if predicate is null
		/// </summary>
		/// <param name="predicate">The predicate to filter blocks by</param>
		/// <returns>The number of matching blocks</returns>
		public int GetConstructBlockCount(Func<IMySlimBlock, bool> predicate = null)
		{
			return this.Construct.GetConstructBlockCount(predicate);
		}

		/// <summary>
		/// Gets the reason this construct is invalid
		/// </summary>
		/// <returns>The invalidation reason or InvalidationReason.NONE</returns>
		public GridInvalidationReason GetInvalidationReason()
		{
			return this.Construct.GetInvalidationReason();
		}

		/// <summary>
		/// Gets the mass of this construct in kilograms<br />
		/// This equals the total mass of each grid<br />
		/// <i>Will be 0 if grid is suspended</i>
		/// </summary>
		/// <returns>The mass of this construct</returns>
		public double ConstructMass()
		{
			return this.Construct.ConstructMass();
		}

		/// <summary>
		/// Attemps to drain the specified power from all on-construct capacitors
		/// </summary>
		/// <param name="power_mw">The amount of drain to syphon in MegaWatts</param>
		/// <returns>The remaining power after syphon</returns>
		public double SyphonConstructCapacitorPower(double power_mw)
		{
			return this.Construct.SyphonConstructCapacitorPower(power_mw);
		}

		/// <summary>
		/// </summary>
		/// <returns>The average of all update times</returns>
		public double AverageUpdateTime60()
		{
			return this.Construct.AverageUpdateTime60();
		}

		/// <summary>
		/// </summary>
		/// <returns>Returns the longest update time within the 60 tick buffer</returns>
		public double LocalLongestUpdateTime60()
		{
			return this.Construct.LocalLongestUpdateTime60();
		}

		/// <summary>
		/// Gets the center of volume of this construct<br />
		/// Returns a zero vector if suspended
		/// </summary>
		/// <returns>This construct's center of volume</returns>
		public Vector3D ConstructVolumeCenter()
		{
			return this.Construct.ConstructVolumeCenter();
		}

		/// <summary>
		/// Gets the center of mass of this construct<br />
		/// Returns a zero vector if suspended
		/// </summary>
		/// <returns>This construct's center of volume</returns>
		public Vector3D ConstructMassCenter()
		{
			return this.Construct.ConstructMassCenter();
		}

		/// <summary>
		/// Checks if the specified world coordinate is within at least one grid of this construct<br />
		/// Point must be within at least two walls of the same grid
		/// </summary>
		/// <param name="world_position">The world position</param>
		/// <returns>The containing grid or null if not inside any subgrid</returns>
		public IMyCubeGrid IsPositionInsideAnySubgrid(Vector3D world_position)
		{
			return this.Construct.IsPositionInsideAnySubgrid(world_position);
		}

		/// <summary>
		/// </summary>
		/// <returns>This construct's main cube grid</returns>
		public IMyCubeGrid GetMainCubeGrid()
		{
			return this.Construct.GetMainCubeGrid();
		}

		/// <summary>
		/// Gets a cube grid by it's terminal block entity ID
		/// </summary>
		/// <param name="id">The cube grid's ID</param>
		/// <returns>The cube grid</returns>
		public IMyCubeGrid GetCubeGrid(long id)
		{
			return this.Construct.GetCubeGrid(id);
		}

		/// <summary>
		/// Gets the first cube grid to match the specified predicate<br />
		/// Gets the first cube grid if predicate is null
		/// </summary>
		/// <param name="predicate">The predicate to filter cube grids by</param>
		/// <param name="default_">The default value if no cube grid matches</param>
		/// <returns>The first matching cube grid</returns>
		public IMyCubeGrid GetFirstCubeGrid(Func<IMyCubeGrid, bool> predicate = null, IMyCubeGrid default_ = default(IMyCubeGrid))
		{
			return this.GetFirstCubeGrid(predicate, default_);
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
			return this.Construct.SplitGrid(grid, blocks_to_discard);
		}

		/// <summary>
		/// Gets a laser antenna by it's terminal block entity ID
		/// </summary>
		/// <param name="id">The laser antenna's ID</param>
		/// <returns>The laser antenna</returns>
		public IMyLaserAntenna GetLaserAntenna(long id)
		{
			return this.Construct.GetLaserAntenna(id);
		}

		/// <summary>
		/// Gets the first laser antenna to match the specified predicate<br />
		/// Gets the first laser antenna if predicate is null
		/// </summary>
		/// <param name="predicate">The predicate to filter laser antennas by</param>
		/// <param name="default_">The default value if no laser antenna matches</param>
		/// <returns>The first matching laser antenna</returns>
		public IMyLaserAntenna GetFirstLaserAntenna(Func<IMyLaserAntenna, bool> predicate = null, IMyLaserAntenna default_ = default(IMyLaserAntenna))
		{
			return this.Construct.GetFirstLaserAntenna(predicate, default_);
		}

		/// <summary>
		/// Gets a radio antenna by it's terminal block entity ID
		/// </summary>
		/// <param name="id">The radio antenna's ID</param>
		/// <returns>The radio antenna</returns>
		public IMyRadioAntenna GetRadioAntenna(long id)
		{
			return this.Construct.GetRadioAntenna(id);
		}

		/// <summary>
		/// Gets the first radio antenna to match the specified predicate<br />
		/// Gets the first radio antenna if predicate is null
		/// </summary>
		/// <param name="predicate">The predicate to filter radio antennas by</param>
		/// <param name="default_">The default value if no radio antenna matches</param>
		/// <returns>The first matching radio antenna</returns>
		public IMyRadioAntenna GetFirstRadioAntenna(Func<IMyRadioAntenna, bool> predicate = null, IMyRadioAntenna default_ = default(IMyRadioAntenna))
		{
			return this.Construct.GetFirstRadioAntenna(predicate, default_);
		}

		/// <summary>
		/// Gets a beacon by it's terminal block entity ID
		/// </summary>
		/// <param name="id">The beacon's ID</param>
		/// <returns>The beacon</returns>
		public IMyBeacon GetBeacon(long id)
		{
			return this.Construct.GetBeacon(id);
		}

		/// <summary>
		/// Gets the first beacon to match the specified predicate<br />
		/// Gets the first beacon if predicate is null
		/// </summary>
		/// <param name="predicate">The predicate to filter beacon by</param>
		/// <param name="default_">The default value if no beacon matches</param>
		/// <returns>The first matching beacon</returns>
		public IMyBeacon GetFirstBeacon(Func<IMyBeacon, bool> predicate = null, IMyBeacon default_ = default(IMyBeacon))
		{
			return this.Construct.GetFirstBeacon(predicate, default_);
		}

		/// <summary>
		/// Gets an enumerator to all grid blocks within this construct
		/// </summary>
		/// <returns>An enumerator for all blocks</returns>
		public IEnumerator<IMySlimBlock> GetConstructBlocks()
		{
			return this.Construct.GetConstructBlocks();
		}

		/// <summary>
		/// Creates a bounding box containing all sub grids' bounding box
		/// </summary>
		/// <returns>This construct's combined AABB or an invalid AABB if suspended</returns>
		public BoundingBoxD GetCombinedAABB()
		{
			return this.Construct.GetCombinedAABB();
		}

		/// <summary>
		/// Gets a jump gate by it's terminal block entity ID
		/// </summary>
		/// <param name="id">The jump gate's ID</param>
		/// <returns>The jump gate</returns>
		public MyAPIJumpGate GetJumpGate(long id)
		{
			return MyAPISession.GetNewJumpGate(this.Construct.GetJumpGate(id));
		}

		/// <summary>
		/// Gets the first jump gate to match the specified predicate<br />
		/// Gets the first jump gate if predicate is null
		/// </summary>
		/// <param name="predicate">The predicate to filter jump gates by</param>
		/// <param name="default_">The default value if no jump gate matches</param>
		/// <returns>The first matching jump gate</returns>
		public MyAPIJumpGate GetFirstJumpGate(Func<MyAPIJumpGate, bool> predicate = null, MyAPIJumpGate default_ = default(MyAPIJumpGate))
		{
			return MyAPISession.GetNewJumpGate((predicate == null) ? this.Construct.GetFirstJumpGate() : this.Construct.GetFirstJumpGate((gate) => {
				MyAPIJumpGate api = MyAPISession.GetNewJumpGate(gate);
				try { return predicate(api); }
				finally { api.Release(); }
			}, default_?.JumpGate));
		}

		/// <summary>
		/// Gets a capacitor by it's terminal block entity ID
		/// </summary>
		/// <param name="id">The capacitor's ID</param>
		/// <returns>The capacitor</returns>
		public MyAPIJumpGateCapacitor GetCapacitor(long id)
		{
			return MyAPISession.GetNewBlock(this.Construct.GetCapacitor(id));
		}

		/// <summary>
		/// Gets the first capacitor to match the specified predicate<br />
		/// Gets the first capacitor if predicate is null
		/// </summary>
		/// <param name="predicate">The predicate to filter capacitors by</param>
		/// <param name="default_">The default value if no capacitor matches</param>
		/// <returns>The first matching capacitor</returns>
		public MyAPIJumpGateCapacitor GetFirstCapacitor(Func<MyAPIJumpGateCapacitor, bool> predicate = null, MyAPIJumpGateCapacitor default_ = default(MyAPIJumpGateCapacitor))
		{
			return MyAPISession.GetNewBlock((predicate == null) ? this.Construct.GetFirstCapacitor() : this.Construct.GetFirstCapacitor((block) => {
				MyAPIJumpGateCapacitor api = MyAPISession.GetNewBlock(block);
				try { return predicate(api); }
				finally { api.Release(); }
			}, default_?.CubeBlock));
		}

		/// <summary>
		/// Gets a drive by it's terminal block entity ID
		/// </summary>
		/// <param name="id">The drive's ID</param>
		/// <returns>The drive</returns>
		public MyAPIJumpGateDrive GetDrive(long id)
		{
			return MyAPISession.GetNewBlock(this.Construct.GetDrive(id));
		}

		/// <summary>
		/// Gets the first drive to match the specified predicate<br />
		/// Gets the first drive if predicate is null
		/// </summary>
		/// <param name="predicate">The predicate to filter drives by</param>
		/// <param name="default_">The default value if no drive matches</param>
		/// <returns>The first matching drive</returns>
		public MyAPIJumpGateDrive GetFirstDrive(Func<MyAPIJumpGateDrive, bool> predicate = null, MyAPIJumpGateDrive default_ = default(MyAPIJumpGateDrive))
		{
			return MyAPISession.GetNewBlock((predicate == null) ? this.Construct.GetFirstDrive() : this.Construct.GetFirstDrive((block) => {
				MyAPIJumpGateDrive api = MyAPISession.GetNewBlock(block);
				try { return predicate(api); }
				finally { api.Release(); }
			}, default_?.CubeBlock));
		}

		/// <summary>
		/// Gets a server antenna by it's terminal block entity ID
		/// </summary>
		/// <param name="id">The server antenna's ID</param>
		/// <returns>The server antenna</returns>
		public MyAPIJumpGateServerAntenna GetServerAntenna(long id)
		{
			return MyAPISession.GetNewBlock(this.Construct.GetServerAntenna(id));
		}

		/// <summary>
		/// Gets the first jump gate to match the specified predicate<br />
		/// Gets the first jump gate if predicate is null
		/// </summary>
		/// <param name="predicate">The predicate to filter jump gates by</param>
		/// <param name="default_">The default value if no jump gate matches</param>
		/// <returns>The first matching jump gate</returns>
		public MyAPIJumpGateServerAntenna GetFirstServerAntenna(Func<MyAPIJumpGateServerAntenna, bool> predicate = null, MyAPIJumpGateServerAntenna default_ = default(MyAPIJumpGateServerAntenna))
		{
			return MyAPISession.GetNewBlock((predicate == null) ? this.Construct.GetFirstServerAntenna() : this.Construct.GetFirstServerAntenna((block) => {
				MyAPIJumpGateServerAntenna api = MyAPISession.GetNewBlock(block);
				try { return predicate(api); }
				finally { api.Release(); }
			}, default_?.CubeBlock));
		}

		/// <summary>
		/// Gets a controller by it's terminal block entity ID
		/// </summary>
		/// <param name="id">The controller's ID</param>
		/// <returns>The controller</returns>
		public MyAPIJumpGateController GetController(long id)
		{
			return MyAPISession.GetNewBlock(this.Construct.GetController(id));
		}

		/// <summary>
		/// Gets the first jump gate to match the specified predicate<br />
		/// Gets the first jump gate if predicate is null
		/// </summary>
		/// <param name="predicate">The predicate to filter jump gates by</param>
		/// <param name="default_">The default value if no jump gate matches</param>
		/// <returns>The first matching jump gate</returns>
		public MyAPIJumpGateController GetFirstController(Func<MyAPIJumpGateController, bool> predicate = null, MyAPIJumpGateController default_ = default(MyAPIJumpGateController))
		{
			return MyAPISession.GetNewBlock((predicate == null) ? this.Construct.GetFirstController() : this.Construct.GetFirstController((block) => {
				MyAPIJumpGateController api = MyAPISession.GetNewBlock(block);
				try { return predicate(api); }
				finally { api.Release(); }
			}, default_?.CubeBlock));
		}

		/// <summary>
		/// Gets the first physics component from this construct's cube grids
		/// </summary>
		/// <returns></returns>
		public MyPhysicsComponentBase GetCubeGridPhysics()
		{
			return this.Construct.GetCubeGridPhysics();
		}
		#endregion
	}
}
