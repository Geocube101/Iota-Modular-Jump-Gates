using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRageMath;
using IOTA.ModularJumpGates.API.ModAPI.Util;

namespace IOTA.ModularJumpGates.API.ModAPI
{
	public class MyModAPIJumpGateConstruct : MyModAPIObjectBase
	{
		internal static MyModAPIJumpGateConstruct New(Dictionary<string, object> attributes)
		{
			return MyModAPIObjectBase.GetObjectOrNew<MyModAPIJumpGateConstruct>(attributes, () => new MyModAPIJumpGateConstruct(attributes));
		}

		private MyModAPIJumpGateConstruct(Dictionary<string, object> attributes) : base(attributes) { }

		/// <summary>
		/// Whether this construct is closed
		/// </summary>
		public bool Closed => this.GetAttribute<bool>("Closed");

		/// <summary>
		/// Whether this construct is marked for close
		/// </summary>
		public bool MarkClosed => this.GetAttribute<bool>("MarkClosed");

		/// <summary>
		/// Whether this construct is suspended from updates<br />
		/// Only used on muliplayer clients when grid is not in scene
		/// </summary>
		public bool IsSuspended => this.GetAttribute<bool>("IsSuspended");

		/// <summary>
		/// Whether to update jump gate intersections on next tick<br />
		/// Gate drive intersections will be updated and gates reconstructed on next tick
		/// </summary>
		public bool MarkUpdateJumpGates => this.GetAttribute<bool>("MarkUpdateJumpGates");

		/// <summary>
		/// Whether this construct should be synced<br/>
		/// If true, will be synced on next component tick
		/// </summary>
		public bool IsDirty => this.GetAttribute<bool>("IsDirty");

		/// <summary>
		/// Whether this construct is fully initialized<br />
		/// True when all gates are constructed and resources initialized
		/// </summary>
		public bool FullyInitialized => this.GetAttribute<bool>("FullyInitialized");

		/// <summary>
		/// The current ID of this construct's cube grid
		/// </summary>
		public long CubeGridID => this.GetAttribute<long>("CubeGridID");

		/// <summary>
		/// The longest time it took for this grid to update
		/// </summary>
		public double LongestUpdateTime => this.GetAttribute<double>("LongestUpdateTime");

		/// <summary>
		/// The custom name of this construct's primary cube grid
		/// </summary>
		public string PrimaryCubeGridCustomName => this.GetAttribute<string>("PrimaryCubeGridCustomName");

		/// <summary>
		/// The UTC date time representation of MyJumpGateConstruct.LastUpdateTime
		/// </summary>
		public DateTime LastUpdateDateTimeUTC
		{
			get
			{
				return this.GetAttribute<DateTime>("LastUpdateDateTimeUTC");
			}

			set
			{
				this.SetAttribute<DateTime>("LastUpdateDateTimeUTC", value);
			}
		}

		/// <summary>
		/// The main cube grid for this construct
		/// </summary>
		public IMyCubeGrid CubeGrid => this.GetAttribute<IMyCubeGrid>("CubeGrid");

		/// <summary>
		/// The gate this construct is currently being jumped by or null
		/// </summary>
		public MyModAPIJumpGate BatchingGate => MyModAPIJumpGate.New(this.GetAttribute<Dictionary<string, object>>("BatchingGate"));

		/// <summary>
		/// Gets all big owners for all grids in this construct
		/// </summary>
		public List<ulong> BigOwners => this.GetAttribute<List<ulong>>("BigOwners");

		/// <summary>
		/// Gets all small owners for all grids in this construct
		/// </summary>
		public List<ulong> SmallOwners => this.GetAttribute<List<ulong>>("SmallOwners");

		/// <summary>
		/// Remaps all jump gate IDs on this construct<br />
		/// Ids will be compressed such that the highest ID is the number of jump gates - 1
		/// </summary>
		public void RemapJumpGateIDs()
		{
			this.GetMethod<Action>("RemapJumpGateIDs")();
		}

		/// <summary>
		/// Destroys all grids attached to this construct
		/// </summary>
		public void Destroy()
		{
			this.GetMethod<Action>("Destroy")();
		}

		/// <summary>
		/// Clears all timing information for this construct
		/// </summary>
		public void ClearResetTimes()
		{
			this.GetMethod<Action>("ClearResetTimes")();
		}

		/// <summary>
		/// Marks this construct for gate reconstruction<br />
		/// Gate drive intersections will be updated and gates reconstructed on next tick
		/// </summary>
		public void MarkGatesForUpdate()
		{
			this.GetMethod<Action>("MarkGatesForUpdate")();
		}

		/// <summary>
		/// Requests a jump grid update from the server<br />
		/// Does nothing if singleplayer or server<br />
		/// Does nothing if construct is suspended
		/// </summary>
		public void RequestGateUpdate()
		{
			this.GetMethod<Action>("RequestGateUpdate")();
		}

		/// <summary>
		/// Marks this construct as dirty<br />
		/// It will be synced to clients on next tick
		/// </summary>
		public void SetDirty()
		{
			this.GetMethod<Action>("SetDirty")();
		}

		/// <summary>
		/// Sets whether all parts of this construct should be static
		/// </summary>
		/// <param name="is_static">Staticness</param>
		public void SetConstructStaticness(bool is_static)
		{
			this.GetMethod<Action<bool>>("SetConstructStaticness")(is_static);
		}

		/// <summary>
		/// Gets a list of blocks from this construct that intersect the specified bounding ellipsoid
		/// </summary>
		/// <param name="ellipsoid">The bounding ellipsoid</param>
		/// <param name="contained_blocks">An optional collection to populate containing all blocks within the ellipsoid</param>
		/// <param name="uncontained_blocks">An optional collection to populate containing all blocks outside the ellipsoid</param>
		public void GetBlocksIntersectingEllipsoid(ref BoundingEllipsoidD ellipsoid, ICollection<IMySlimBlock> contained_blocks = null, ICollection<IMySlimBlock> uncontained_blocks = null)
		{
			this.GetMethod<Action<byte[], ICollection<IMySlimBlock>, ICollection<IMySlimBlock>>>("GetBlocksIntersectingEllipsoid")(ellipsoid.ToSerialized(), contained_blocks, uncontained_blocks);
		}

		/// <summary>
		/// Whether this grid is valid
		/// </summary>
		/// <returns>True if valid</returns>
		public bool IsValid()
		{
			return this.GetMethod<Func<bool>>("IsValid")();
		}

		/// <summary>
		/// Checks construct staticness
		/// </summary>
		/// <returns>Whether at least one grid is static</returns>
		public bool IsStatic()
		{
			return this.GetMethod<Func<bool>>("IsStatic")();
		}

		/// <summary>
		/// </summary>
		/// <returns>True if any working antennas exist on this construct</returns>
		public bool HasCommLink()
		{
			return this.GetMethod<Func<bool>>("HasCommLink")();
		}

		/// <summary>
		/// </summary>
		/// <returns>True if this construct completed at least one tick</returns>
		public bool AtLeastOneUpdate()
		{
			return this.GetMethod<Func<bool>>("AtLeastOneUpdate")();
		}

		/// <summary>
		/// </summary>
		/// <param name="grid_id">The sub-grid ID</param>
		/// <returns>True if the grid ID is part of this construct</returns>
		public bool HasCubeGrid(long grid_id)
		{
			return this.GetMethod<Func<long, bool>>("HasCubeGridByID")(grid_id);
		}

		/// <summary>
		/// </summary>
		/// <param name="grid">The sub-grid cube grid</param>
		/// <returns>True if the grid is part of this construct</returns>
		public bool HasCubeGrid(IMyCubeGrid grid)
		{
			return this.GetMethod<Func<IMyCubeGrid, bool>>("HasCubeGrid")(grid);
		}

		/// <summary>
		/// Checks if the specified grid is reachable from this one via antennas
		/// </summary>
		/// <param name="grid">The target grid</param>
		/// <param name="update">If true, requests update to internal list</param>
		/// <returns>Whether target grid is comm linked</returns>
		public bool IsConstructCommLinked(MyModAPIJumpGateConstruct grid)
		{
			return (grid == null) ? false : this.GetMethod<Func<long, bool>>("IsConstructCommLinked")(grid.CubeGridID);
		}

		/// <summary>
		/// Checks if the specified beacon is seen from this grid
		/// This method will not update the internal beacon-linked list before checking
		/// </summary>
		/// <param name="beacon">The target beacon</param>
		/// <param name="update">If true, requests update to internal list</param>
		/// <returns>Whether target beacon is beacon linked</returns>
		public bool IsBeaconWithinReverseBroadcastSphere(IMyBeacon beacon)
		{
			return this.GetMethod<Func<IMyBeacon, bool>>("IsBeaconWithinReverseBroadcastSphere")(beacon);
		}

		/// <summary>
		/// Checks if the specified beacon is seen from this grid
		/// This method will not update the internal beacon-linked list before checking
		/// </summary>
		/// <param name="beacon">The target beacon</param>
		/// <param name="update">If true, requests update to internal list</param>
		/// <returns>Whether target beacon is beacon linked</returns>
		public bool IsBeaconWithinReverseBroadcastSphere(MyAPIBeaconLinkWrapper beacon)
		{
			return this.GetMethod<Func<byte[], bool>>("IsBeaconWrapperWithinReverseBroadcastSphere")(MyAPIGateway.Utilities.SerializeToBinary(beacon));
		}

		/// <summary>
		/// Checks whether thie construct intersects the specified ellipsoid
		/// </summary>
		/// <param name="ellipsoid">The ellipsoid to check against</param>
		/// <returns>Whether at least 4 bounding box vertices are contained</returns>
		public bool IsConstructWithinBoundingEllipsoid(ref BoundingEllipsoidD ellipsoid)
		{
			return this.GetMethod<Func<byte[], bool>>("IsConstructWithinBoundingEllipsoid")(ellipsoid.ToSerialized());
		}

		public override bool Equals(object other)
		{
			return other != null && other is MyModAPIJumpGateConstruct && base.Equals(((MyModAPIJumpGateConstruct) other).CubeGridID);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		/// <summary>
		/// Gets the number of blocks matching the specified predicate<br />
		/// Gets the number of all blocks if predicate is null
		/// </summary>
		/// <param name="predicate">The predicate to filter blocks by</param>
		/// <returns>The number of matching blocks</returns>
		public int GetConstructBlockCount(Func<IMySlimBlock, bool> predicate = null)
		{
			return this.GetMethod<Func<Func<IMySlimBlock, bool>, int>>("GetConstructBlockCount")(predicate);
		}

		/// <summary>
		/// Gets the reason this construct is invalid
		/// </summary>
		/// <returns>The invalidation reason or InvalidationReason.NONE</returns>
		public MyAPIGridInvalidationReason GetInvalidationReason()
		{
			return (MyAPIGridInvalidationReason) this.GetMethod<Func<byte>>("GetInvalidationReason")();
		}

		/// <summary>
		/// Gets the mass of this construct in kilograms<br />
		/// This equals the total mass of each grid<br />
		/// <i>Will be 0 if grid is suspended</i>
		/// </summary>
		/// <returns>The mass of this construct</returns>
		public double ConstructMass()
		{
			return this.GetMethod<Func<double>>("ConstructMass")();
		}

		/// <summary>
		/// Attemps to drain the specified power from all on-construct capacitors
		/// </summary>
		/// <param name="power_mw">The amount of drain to syphon in MegaWatts</param>
		/// <returns>The remaining power after syphon</returns>
		public double SyphonConstructCapacitorPower(double power_mw)
		{
			return this.GetMethod<Func<double, double>>("SyphonConstructCapacitorPower")(power_mw);
		}

		/// <summary>
		/// </summary>
		/// <returns>The average of all update times</returns>
		public double AverageUpdateTime60()
		{
			return this.GetMethod<Func<double>>("AverageUpdateTime60")();
		}

		/// <summary>
		/// </summary>
		/// <returns>Returns the longest update time within the 60 tick buffer</returns>
		public double LocalLongestUpdateTime60()
		{
			return this.GetMethod<Func<double>>("LocalLongestUpdateTime60")();
		}

		/// <summary>
		/// Calculates the total available power stored within construct capacitors and drives for the specified jump gate
		/// </summary>
		/// <param name="jump_gate_id">The jump gate to calculate for</param>
		/// <returns>Total available instant power in Mega-Watts</returns>
		public double CalculateTotalAvailableInstantPower(long jump_gate_id)
		{
			return this.GetMethod<Func<long, double>>("CalculateTotalAvailableInstantPower")(jump_gate_id);
		}

		/// <summary>
		/// Calculates the total possible power stored within construct capacitors and drives for the specified jump gate
		/// </summary>
		/// <param name="jump_gate_id">The jump gate to calculate for</param>
		/// <returns>Total possible instant power in Mega-Watts</returns>
		public double CalculateTotalPossibleInstantPower(long jump_gate_id)
		{
			return this.GetMethod<Func<long, double>>("CalculateTotalPossibleInstantPower")(jump_gate_id);
		}

		/// <summary>
		/// Calculates the total possible power stored within construct capacitors and drives for the specified jump gate<br />
		/// This method ignores block settings and whether block is working
		/// </summary>
		/// <param name="jump_gate_id">The jump gate to calculate for</param>
		/// <returns>Total max possible instant power in Mega-Watts</returns>
		public double CalculateTotalMaxPossibleInstantPower(long jump_gate_id)
		{
			return this.GetMethod<Func<long, double>>("CalculateTotalMaxPossibleInstantPower")(jump_gate_id);
		}

		/// <summary>
		/// Gets the center of volume of this construct<br />
		/// Returns a zero vector if suspended or closed
		/// </summary>
		/// <returns>This construct's center of volume</returns>
		public Vector3D ConstructVolumeCenter()
		{
			return this.GetMethod<Func<Vector3D>>("ConstructVolumeCenter")();
		}

		/// <summary>
		/// Gets the center of mass of this construct<br />
		/// Returns a zero vector if suspended or closed
		/// </summary>
		/// <returns>This construct's center of volume</returns>
		public Vector3D ConstructMassCenter()
		{
			return this.GetMethod<Func<Vector3D>>("ConstructMassCenter")();
		}

		/// <summary>
		/// Checks if the specified world coordinate is within at least one grid of this construct<br />
		/// Point must be within at least two walls of the same grid
		/// </summary>
		/// <param name="world_position">The world position</param>
		/// <returns>The containing grid or null if not inside any subgrid</returns>
		public IMyCubeGrid IsPositionInsideAnySubgrid(Vector3D world_position)
		{
			return this.GetMethod<Func<Vector3D, IMyCubeGrid>>("IsPositionInsideAnySubgrid")(world_position);
		}

		/// <summary>
		/// </summary>
		/// <returns>This construct's main cube grid</returns>
		public IMyCubeGrid GetMainCubeGrid()
		{
			return this.GetMethod<Func<IMyCubeGrid>>("GetMainCubeGrid")();
		}

		/// <summary>
		/// Gets a cube grid by it's terminal block entity ID
		/// </summary>
		/// <param name="id">The cube grid's ID</param>
		/// <returns>The cube grid</returns>
		public IMyCubeGrid GetCubeGrid(long id)
		{
			return this.GetMethod<Func<long, IMyCubeGrid>>("GetCubeGrid")(id);
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
			return this.GetMethod<Func<IMyCubeGrid, List<IMySlimBlock>, IMyCubeGrid>>("SplitGrid")(grid, blocks_to_discard);
		}

		/// <summary>
		/// Gets a laser antenna by it's terminal block entity ID
		/// </summary>
		/// <param name="id">The laser antenna's ID</param>
		/// <returns>The laser antenna</returns>
		public IMyLaserAntenna GetLaserAntenna(long id)
		{
			return this.GetMethod<Func<long, IMyLaserAntenna>>("GetLaserAntenna")(id);
		}

		/// <summary>
		/// Gets a radio antenna by it's terminal block entity ID
		/// </summary>
		/// <param name="id">The radio antenna's ID</param>
		/// <returns>The radio antenna</returns>
		public IMyRadioAntenna GetRadioAntenna(long id)
		{
			return this.GetMethod<Func<long, IMyRadioAntenna>>("GetRadioAntenna")(id);
		}

		/// <summary>
		/// Gets a beacon by it's terminal block entity ID
		/// </summary>
		/// <param name="id">The beacon's ID</param>
		/// <returns>The beacon</returns>
		public IMyBeacon GetBeacon(long id)
		{
			return this.GetMethod<Func<long, IMyBeacon>>("GetBeacon")(id);
		}

		/// <summary>
		/// Gets an enumerator to all grid blocks within this construct
		/// </summary>
		/// <returns>An enumerator for all blocks</returns>
		public IEnumerator<IMySlimBlock> GetConstructBlocks()
		{
			return this.GetMethod<Func<IEnumerator<IMySlimBlock>>>("GetConstructBlocks")();
		}

		/// <summary>
		/// Creates a bounding box containing all sub grids' bounding box
		/// </summary>
		/// <returns>This construct's combined AABB or an invalid AABB if suspended or closed</returns>
		public BoundingBoxD GetCombinedAABB()
		{
			return this.GetMethod<Func<BoundingBoxD>>("GetCombinedAABB")();
		}

		/// <summary>
		/// Gets a jump gate by it's terminal block entity ID
		/// </summary>
		/// <param name="id">The jump gate's ID</param>
		/// <returns>The jump gate</returns>
		public MyModAPIJumpGate GetJumpGate(long id)
		{
			return MyModAPIJumpGate.New(this.GetMethod<Func<long, Dictionary<string, object>>>("GetJumpGate")(id));
		}

		/// <summary>
		/// Gets a cube block by it's terminal block entity ID
		/// </summary>
		/// <param name="id">The block's ID</param>
		/// <returns>The block</returns>
		public MyModAPICubeBlockBase GetCubeBlock(long id)
		{
			return MyModAPICubeBlockBase.New(this.GetMethod<Func<long, Dictionary<string, object>>>("GetCubeBlock")(id));
		}

		/// <summary>
		/// Gets a capacitor by it's terminal block entity ID
		/// </summary>
		/// <param name="id">The capacitor's ID</param>
		/// <returns>The capacitor</returns>
		public MyModAPIJumpGateCapacitor GetCapacitor(long id)
		{
			return MyModAPIJumpGateCapacitor.New(this.GetMethod<Func<long, Dictionary<string, object>>>("GetCapacitor")(id));
		}

		/// <summary>
		/// Gets a drive by it's terminal block entity ID
		/// </summary>
		/// <param name="id">The drive's ID</param>
		/// <returns>The drive</returns>
		public MyModAPIJumpGateDrive GetDrive(long id)
		{
			return MyModAPIJumpGateDrive.New(this.GetMethod<Func<long, Dictionary<string, object>>>("GetDrive")(id));
		}

		/// <summary>
		/// Gets a remote antenna by it's terminal block entity ID
		/// </summary>
		/// <param name="id">The remote antenna's ID</param>
		/// <returns>The remote antenna</returns>
		public MyModAPIJumpGateRemoteAntenna GetRemoteAntenna(long id)
		{
			return MyModAPIJumpGateRemoteAntenna.New(this.GetMethod<Func<long, Dictionary<string, object>>>("GetRemoteAntenna")(id));
		}

		/// <summary>
		/// Gets a remote link by it's terminal block entity ID
		/// </summary>
		/// <param name="id">The remote link's ID</param>
		/// <returns>The remote link</returns>
		public MyModAPIJumpGateRemoteLink GetRemoteLink(long id)
		{
			return MyModAPIJumpGateRemoteLink.New(this.GetMethod<Func<long, Dictionary<string, object>>>("GetRemoteLink")(id));
		}

		/// <summary>
		/// Gets a server antenna by it's terminal block entity ID
		/// </summary>
		/// <param name="id">The server antenna's ID</param>
		/// <returns>The server antenna</returns>
		public MyModAPIJumpGateServerAntenna GetServerAntenna(long id)
		{
			return MyModAPIJumpGateServerAntenna.New(this.GetMethod<Func<long, Dictionary<string, object>>>("GetServerAntenna")(id));
		}

		/// <summary>
		/// Gets a controller by it's terminal block entity ID
		/// </summary>
		/// <param name="id">The controller's ID</param>
		/// <returns>The controller</returns>
		public MyModAPIJumpGateController GetController(long id)
		{
			return MyModAPIJumpGateController.New(this.GetMethod<Func<long, Dictionary<string, object>>>("GetController")(id));
		}

		/// <summary>
		/// Gets the first physics component from this construct's cube grids
		/// </summary>
		/// <returns></returns>
		public MyPhysicsComponentBase GetCubeGridPhysics()
		{
			return this.GetMethod<Func<MyPhysicsComponentBase>>("GetCubeGridPhysics")();
		}

		/// <summary>
		/// Gets all jump gate controllers in this construct
		/// </summary>
		/// <returns>An IEnumerable referencing all controllers</returns>
		public IEnumerable<MyModAPIJumpGateController> GetAttachedJumpGateControllers()
		{
			return this.GetMethod<Func<IEnumerable<Dictionary<string, object>>>>("GetAttachedJumpGateControllers")().Select(MyModAPIJumpGateController.New);
		}

		/// <summary>
		/// All jump gate drives in this construct
		/// </summary>
		/// <returns>An IEnumerable referencing all drives</returns>
		public IEnumerable<MyModAPIJumpGateDrive> GetAttachedJumpGateDrives()
		{
			return this.GetMethod<Func<IEnumerable<Dictionary<string, object>>>>("GetAttachedJumpGateDrives")().Select(MyModAPIJumpGateDrive.New);
		}

		/// <summary>
		/// Gets all jump gate drives in this construct not bound to a jump gate
		/// </summary>
		/// <returns>An IEnumerable referencing all unassociated drives</returns>
		public IEnumerable<MyModAPIJumpGateDrive> GetAttachedUnassociatedJumpGateDrives()
		{
			return this.GetMethod<Func<IEnumerable<Dictionary<string, object>>>>("GetAttachedUnassociatedJumpGateDrives")().Select(MyModAPIJumpGateDrive.New);
		}

		/// <summary>
		/// Gets all jump gate capacitors in this construct
		/// </summary>
		/// <returns>An IEnumerable referencing all capacitors</returns>
		public IEnumerable<MyModAPIJumpGateCapacitor> GetAttachedJumpGateCapacitors()
		{
			return this.GetMethod<Func<IEnumerable<Dictionary<string, object>>>>("GetAttachedJumpGateCapacitors")().Select(MyModAPIJumpGateCapacitor.New);
		}

		/// <summary>
		/// Gets all jump gate remote antennas in this construct
		/// </summary>
		/// <returns>An IEnumerable referencing all remote antennas</returns>
		public IEnumerable<MyModAPIJumpGateRemoteAntenna> GetAttachedJumpGateRemoteAntennas()
		{
			return this.GetMethod<Func<IEnumerable<Dictionary<string, object>>>>("GetAttachedJumpGateRemoteAntennas")().Select(MyModAPIJumpGateRemoteAntenna.New);
		}

		/// <summary>
		/// Gets all jump gate remote links in this construct
		/// </summary>
		/// <returns>An IEnumerable referencing all remote links</returns>
		public IEnumerable<MyModAPIJumpGateRemoteLink> GetAttachedJumpGateRemoteLinks()
		{
			return this.GetMethod<Func<IEnumerable<Dictionary<string, object>>>>("GetAttachedJumpGateRemoteLinks")().Select(MyModAPIJumpGateRemoteLink.New);
		}

		/// <summary>
		/// Gets all jump gate server antennas in this construct
		/// </summary>
		/// <returns>An IEnumerable referencing all server antennas</returns>
		public IEnumerable<MyModAPIJumpGateServerAntenna> GetAttachedJumpGateServerAntennas()
		{
			return this.GetMethod<Func<IEnumerable<Dictionary<string, object>>>>("GetAttachedJumpGateServerAntennas")().Select(MyModAPIJumpGateServerAntenna.New);
		}

		/// <summary>
		/// Gets all laser antennas in this construct
		/// </summary>
		/// <returns>An IEnumerable referencing all laser antennas</returns>
		public IEnumerable<IMyLaserAntenna> GetAttachedLaserAntennas()
		{
			return this.GetMethod<Func<IEnumerable<IMyLaserAntenna>>>("GetAttachedLaserAntennas")();
		}

		/// <summary>
		/// Gets all radio antennas in this construct
		/// </summary>
		/// <returns>An IEnumerable referencing all radio antennas</returns>
		public IEnumerable<IMyRadioAntenna> GetAttachedRadioAntennas()
		{
			return this.GetMethod<Func<IEnumerable<IMyRadioAntenna>>>("GetAttachedRadioAntennas")();
		}

		/// <summary>
		/// Gets all beacons in this construct
		/// </summary>
		/// <returns>An IEnumerable referencing all beacons</returns>
		public IEnumerable<IMyBeacon> GetAttachedBeacons()
		{
			return this.GetMethod<Func<IEnumerable<IMyBeacon>>>("GetAttachedBeacons")();
		}

		/// <summary>
		/// Gets all jump gates in this construct
		/// </summary>
		/// <returns>An IEnumerable referencing all jump gates</returns>
		public IEnumerable<MyModAPIJumpGate> GetJumpGates()
		{
			return this.GetMethod<Func<IEnumerable<Dictionary<string, object>>>>("GetJumpGates")().Select(MyModAPIJumpGate.New);
		}

		/// <summary>
		/// Gets all cube grids in this construct
		/// </summary>
		/// <returns>An IEnumerable referencing all grids</returns>
		public IEnumerable<IMyCubeGrid> GetCubeGrids()
		{
			return this.GetMethod<Func<IEnumerable<IMyCubeGrid>>>("GetCubeGrids")();
		}

		/// <summary>
		/// Does a BFS search of all constructs accessible to this one via antennas
		/// </summary>
		/// <returns>An IEnumerable referencing all comm-linked constructs</returns>
		public IEnumerable<MyModAPIJumpGateConstruct> GetCommLinkedJumpGateGrids()
		{
			return this.GetMethod<Func<IEnumerable<Dictionary<string, object>>>>("GetCommLinkedJumpGateGrids")().Select(MyModAPIJumpGateConstruct.New);
		}

		/// <summary>
		/// Does a search for all beacons that this grid can see
		/// </summary>
		/// <returns>An IEnumerable referencing all beacon-linked beacons</returns>
		public IEnumerable<MyAPIBeaconLinkWrapper> GetBeaconsWithinReverseBroadcastSphere()
		{
			return this.GetMethod<Func<IEnumerable<byte[]>>>("GetBeaconsWithinReverseBroadcastSphere")().Select(MyAPIGateway.Utilities.SerializeFromBinary<MyAPIBeaconLinkWrapper>);
		}
	}
}
