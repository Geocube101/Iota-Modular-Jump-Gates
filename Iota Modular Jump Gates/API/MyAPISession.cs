using IOTA.ModularJumpGates.API.CubeBlock;
using IOTA.ModularJumpGates.CubeBlock;
using IOTA.ModularJumpGates.Util;
using Sandbox.ModAPI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using VRage.Game.ModAPI;
using VRageMath;

namespace IOTA.ModularJumpGates.API
{
	public class MyAPISession
	{
		#region Private Static Variables
		private static long NextInstanceID = 0;
		#endregion

		#region Private Static Variables
		private static ConcurrentDictionary<long, MyAPICubeBlockBase> CubeBlockAPIs = new ConcurrentDictionary<long, MyAPICubeBlockBase>();

		private static ConcurrentDictionary<long, MyAPIJumpGateConstruct> JumpGateGridAPIs = new ConcurrentDictionary<long, MyAPIJumpGateConstruct>();

		private static ConcurrentDictionary<JumpGateUUID, MyAPIJumpGate> JumpGateAPIs = new ConcurrentDictionary<JumpGateUUID, MyAPIJumpGate>();
		#endregion

		#region Public Static Variables
		/// <summary>
		/// Reference to the instance of this session component wrapper
		/// </summary>
		public static MyAPISession Instance { get; private set; }
		#endregion

		#region Public Static API Variables
		/// <summary>
		/// Whether to show debug draw
		/// </summary>
		public static bool DebugMode { get { return MyJumpGateModSession.DebugMode; } set { MyJumpGateModSession.DebugMode = value; } }

		/// <summary>
		/// Whether all entities in session are loaded<br />
		/// <i>Always false on client</i>
		/// </summary>
		public static bool AllSessionEntitiesLoaded => MyJumpGateModSession.Instance.AllSessionEntitiesLoaded;

		/// <summary>
		/// For server like: Whether all constructs have had at least one tick<br />
		/// For client like: Whether session has begun ticking
		/// </summary>
		public static bool ModFullyInitialized => MyJumpGateModSession.Instance.InitializationComplete;

		/// <summary>
		/// The current, session local game tick
		/// </summary>
		public static ulong GameTick => MyJumpGateModSession.GameTick;

		/// <summary>
		/// The current session component status
		/// </summary>
		public static MySessionStatusEnum SessionStatus => MyJumpGateModSession.SessionStatus;

		/// <summary>
		/// The world's world matrix
		/// </summary>
		public static readonly MatrixD WorldMatrix = MyJumpGateModSession.WorldMatrix;

		/// <summary>
		/// Configuration variables as loaded from file or recieved from server
		/// </summary>
		public static Configuration Configuration => MyJumpGateModSession.Configuration;
		#endregion

		#region Public Static API Methods
		/// <summary>
		/// Checks whether the block is a derivative of CubeBlockBase
		/// </summary>
		/// <param name="block">The block to check</param>
		/// <returns>Whether the "CubeBlockBase" game logic component is attached</returns>
		public static bool IsBlockCubeBlockBase(IMyTerminalBlock block)
		{
			return MyJumpGateModSession.IsBlockCubeBlockBase(block);
		}

		/// <summary>
		/// Checks whether the block is a jump gate controller
		/// </summary>
		/// <param name="block">The block to check</param>
		/// <returns>Whether the "JumpGateController" game logic component is attached</returns>
		public static bool IsBlockJumpGateController(IMyTerminalBlock block)
		{
			return MyJumpGateModSession.IsBlockJumpGateController(block);
		}

		/// <summary>
		/// Checks whether the block is a jump gate drive
		/// </summary>
		/// <param name="block">The block to check</param>
		/// <returns>Whether the "JumpGateDrive" game logic component is attached</returns>
		public static bool IsBlockJumpGateDrive(IMyTerminalBlock block)
		{
			return MyJumpGateModSession.IsBlockJumpGateDrive(block);
		}

		/// <summary>
		/// Checks whether the block is a jump gate capacitor
		/// </summary>
		/// <param name="block">The block to check</param>
		/// <returns>Whether the "JumpGateCapacitor" game logic component is attached</returns>
		public static bool IsBlockJumpGateCapacitor(IMyTerminalBlock block)
		{
			return MyJumpGateModSession.IsBlockJumpGateCapacitor(block);
		}

		/// <summary>
		/// Checks whether the block is a jump gate server antenna
		/// </summary>
		/// <param name="block">The block to check</param>
		/// <returns>Whether the "JumpGateServerAntenna" game logic component is attached</returns>
		public static bool IsBlockJumpGateServerAntenna(IMyTerminalBlock block)
		{
			return MyJumpGateModSession.IsBlockJumpGateServerAntenna(block);
		}

		/// <summary>
		/// Checks if the position is both inside a cube grid's bounding box and whether that position is between at least two walls
		/// </summary>
		/// <param name="grid">The grid to check in</param>
		/// <param name="position">The world coordinate</param>
		/// <returns>Insideness</returns>
		public static bool IsPositionInsideGrid(IMyCubeGrid grid, Vector3D position)
		{
			return MyJumpGateModSession.IsPositionInsideGrid(grid, position);
		}

		/// <summary>
		/// Converts a world position vector to a local position vector
		/// </summary>
		/// <param name="world_matrix">The world matrix</param>
		/// <param name="world_pos">The world vector to convert</param>
		/// <returns>The local vector</returns>
		public static Vector3D WorldVectorToLocalVectorP(MatrixD world_matrix, Vector3D world_pos)
		{
			return MyJumpGateModSession.WorldVectorToLocalVectorP(ref world_matrix, world_pos);
		}

		/// <summary>
		/// Converts a world position vector to a local position vector
		/// </summary>
		/// <param name="world_matrix">The world matrix</param>
		/// <param name="world_pos">The world vector to convert</param>
		/// <returns>The local vector</returns>
		public static Vector3D WorldVectorToLocalVectorP(ref MatrixD world_matrix, Vector3D world_pos)
		{
			return MyJumpGateModSession.WorldVectorToLocalVectorP(ref world_matrix, world_pos);
		}

		/// <summary>
		/// Converts a local position vector to a world position vector
		/// </summary>
		/// <param name="world_matrix">The world matrix</param>
		/// <param name="world_pos">The local vector to convert</param>
		/// <returns>The world vector</returns>
		public static Vector3D LocalVectorToWorldVectorP(MatrixD world_matrix, Vector3D local_pos)
		{
			return MyJumpGateModSession.LocalVectorToWorldVectorP(ref world_matrix, local_pos);
		}

		/// <summary>
		/// Converts a local position vector to a world position vector
		/// </summary>
		/// <param name="world_matrix">The world matrix</param>
		/// <param name="world_pos">The local vector to convert</param>
		/// <returns>The world vector</returns>
		public static Vector3D LocalVectorToWorldVectorP(ref MatrixD world_matrix, Vector3D local_pos)
		{
			return MyJumpGateModSession.LocalVectorToWorldVectorP(ref world_matrix, local_pos);
		}

		/// <summary>
		/// Converts a world direction vector to a local direction vector
		/// </summary>
		/// <param name="world_matrix">The world matrix</param>
		/// <param name="world_pos">The world vector to convert</param>
		/// <returns>The local vector</returns>
		public static Vector3D WorldVectorToLocalVectorD(MatrixD world_matrix, Vector3D world_direction)
		{
			return MyJumpGateModSession.WorldVectorToLocalVectorD(ref world_matrix, world_direction);
		}

		/// <summary>
		/// Converts a world direction vector to a local direction vector
		/// </summary>
		/// <param name="world_matrix">The world matrix</param>
		/// <param name="world_pos">The world vector to convert</param>
		/// <returns>The local vector</returns>
		public static Vector3D WorldVectorToLocalVectorD(ref MatrixD world_matrix, Vector3D world_direction)
		{
			return MyJumpGateModSession.WorldVectorToLocalVectorD(ref world_matrix, world_direction);
		}

		/// <summary>
		/// Converts a local direction vector to a world direction vector
		/// </summary>
		/// <param name="world_matrix">The world matrix</param>
		/// <param name="world_pos">The local vector to convert</param>
		/// <returns>The world vector</returns>
		public static Vector3D LocalVectorToWorldVectorD(MatrixD world_matrix, Vector3D local_direction)
		{
			return MyJumpGateModSession.LocalVectorToWorldVectorD(ref world_matrix, local_direction);
		}

		/// <summary>
		/// Converts a local direction vector to a world direction vector
		/// </summary>
		/// <param name="world_matrix">The world matrix</param>
		/// <param name="world_pos">The local vector to convert</param>
		/// <returns>The world vector</returns>
		public static Vector3D LocalVectorToWorldVectorD(ref MatrixD world_matrix, Vector3D local_direction)
		{
			return MyJumpGateModSession.LocalVectorToWorldVectorD(ref world_matrix, local_direction);
		}

		/// <summary>
		/// Converts a value to its simplist metric unit<br />
		/// Example: 1000 -> 1 K<br />
		/// Example: 1000000 -> 1 M<br />
		/// </summary>
		/// <param name="value">The value to convert</param>
		/// <param name="unit">The base unit</param>
		/// <param name="places">The number of places to round to</param>
		/// <returns>The resulting value</returns>
		public static string AutoconvertMetricUnits(double value, string unit, int places)
		{
			return MyJumpGateModSession.AutoconvertMetricUnits(value, unit, places);
		}

		/// <summary>
		/// Converts a value to base 10 scientific notation
		/// </summary>
		/// <param name="value">The value to convert</param>
		/// <param name="places">The number of places to round to</param>
		/// <param name="e">The value used in place of 'E'</param>
		/// <returns>The resulting value in scientific notation</returns>
		public static string AutoconvertSciNotUnits(double value, int places, string e = " E ")
		{
			return MyJumpGateModSession.AutoconvertSciNotUnits(value, places, e);
		}

		/// <summary>
		/// Gets the block as a CubeBlockBase instance or null if not a derivative of CubeBlockBase
		/// </summary>
		/// <param name="block">The block to convert</param>
		/// <returns>The "CubeBlockBase" game logic component or null</returns>
		public static MyAPICubeBlockBase GetBlockAsCubeBlockBase(IMyTerminalBlock block)
		{
			return MyAPISession.GetNewBlock(MyJumpGateModSession.GetBlockAsCubeBlockBase(block));
		}

		/// <summary>
		/// Gets the block as a jump gate controller or null if not a jump gate controller
		/// </summary>
		/// <param name="block">The block to convert</param>
		/// <returns>The "JumpGateController" game logic component or null</returns>
		public static MyAPIJumpGateController GetBlockAsJumpGateController(IMyTerminalBlock block)
		{
			return MyAPISession.GetNewBlock(MyJumpGateModSession.GetBlockAsJumpGateController(block));
		}

		/// <summary>
		/// Gets the block as a jump gate drive or null if not a jump gate drive
		/// </summary>
		/// <param name="block">The block to convert</param>
		/// <returns>The "JumpGateDrive" game logic component or null</returns>
		public static MyAPIJumpGateDrive GetBlockAsJumpGateDrive(IMyTerminalBlock block)
		{
			return MyAPISession.GetNewBlock(MyJumpGateModSession.GetBlockAsJumpGateDrive(block));
		}

		/// <summary>
		/// Gets the block as a jump gate capacitor of null if not a jump gate capacitor
		/// </summary>
		/// <param name="block">The block to convert</param>
		/// <returns>The "JumpGateCapacitor" game logic component or null</returns>
		public static MyAPIJumpGateCapacitor GetBlockAsJumpGateCapacitor(IMyTerminalBlock block)
		{
			return MyAPISession.GetNewBlock(MyJumpGateModSession.GetBlockAsJumpGateCapacitor(block));
		}

		/// <summary>
		/// Gets the block as a jump gate server antenna of null if not a jump gate server antenna
		/// </summary>
		/// <param name="block">The block to convert</param>
		/// <returns>The "JumpGateServerAntenna" game logic component or null</returns>
		public static MyAPIJumpGateServerAntenna GetBlockAsJumpGateServerAntenna(IMyTerminalBlock block)
		{
			return MyAPISession.GetNewBlock(MyJumpGateModSession.GetBlockAsJumpGateServerAntenna(block));
		}
		#endregion

		#region Internal Static Methods
		internal static void Init()
		{
			MyAPISession.Instance = new MyAPISession();
		}

		internal static void Dispose()
		{
			MyAPISession.Instance = null;

			foreach (KeyValuePair<long, MyAPICubeBlockBase> api in MyAPISession.CubeBlockAPIs)
			{
				api.Value.ObjectInterfaceID = -1;
				api.Value.ReferenceCounter = 0;
				api.Value.CubeBlock = null;
			}

			foreach (KeyValuePair<long, MyAPIJumpGateConstruct> api in MyAPISession.JumpGateGridAPIs)
			{
				api.Value.ObjectInterfaceID = -1;
				api.Value.ReferenceCounter = 0;
				api.Value.Construct = null;
			}

			foreach (KeyValuePair<JumpGateUUID, MyAPIJumpGate> api in MyAPISession.JumpGateAPIs)
			{
				api.Value.ObjectInterfaceID = -1;
				api.Value.ReferenceCounter = 0;
				api.Value.JumpGate = null;
			}

			MyAPISession.CubeBlockAPIs.Clear();
			MyAPISession.JumpGateGridAPIs.Clear();
			MyAPISession.JumpGateAPIs.Clear();

			MyAPISession.CubeBlockAPIs = null;
			MyAPISession.JumpGateGridAPIs = null;
			MyAPISession.JumpGateAPIs = null;
		}

		internal static void ReleaseInterface(MyAPIInterface iface)
		{
			if (iface == null || iface.ObjectInterfaceID == -1) return;
			iface.ObjectInterfaceID = -1;
			iface.ReferenceCounter = 0;

			if (iface is MyAPICubeBlockBase)
			{
				MyAPICubeBlockBase api = (MyAPICubeBlockBase) iface;
				MyAPISession.CubeBlockAPIs.Remove(api.CubeBlock.TerminalBlock.EntityId);
				api.CubeBlock = null;
			}
			else if (iface is MyAPIJumpGateConstruct)
			{
				MyAPIJumpGateConstruct api = (MyAPIJumpGateConstruct) iface;
				MyAPISession.JumpGateGridAPIs.Remove(api.Construct.CubeGridID);
				api.Construct = null;
			}
			else if (iface is MyAPIJumpGate)
			{
				MyAPIJumpGate api = (MyAPIJumpGate) iface;
				MyAPISession.JumpGateAPIs.Remove(JumpGateUUID.FromJumpGate(api.JumpGate));
				api.JumpGate = null;
			}
		}

		internal static MyAPICubeBlockBase GetNewBlock(MyCubeBlockBase block)
		{
			if (block == null) return null;
			MyAPICubeBlockBase block_base = new MyAPICubeBlockBase(block);
			block_base = MyAPISession.CubeBlockAPIs.AddOrUpdate(block.TerminalBlock.EntityId, block_base, (_, old_block) => {
				block_base.CubeBlock = null;
				++old_block.ReferenceCounter;
				return old_block;
			});

			if (block_base.ObjectInterfaceID == -1) block_base.ObjectInterfaceID = MyAPISession.NextInstanceID++;
			return block_base;
		}

		internal static MyAPIJumpGateCapacitor GetNewBlock(MyJumpGateCapacitor block)
		{
			if (block == null) return null;
			MyAPIJumpGateCapacitor block_base = new MyAPIJumpGateCapacitor(block);
			block_base = (MyAPIJumpGateCapacitor) MyAPISession.CubeBlockAPIs.AddOrUpdate(block.TerminalBlock.EntityId, block_base, (_, old_block) => {
				block_base.CubeBlock = null;
				block_base.BlockSettings = null;
				++old_block.ReferenceCounter;
				return old_block;
			});

			if (block_base.ObjectInterfaceID == -1) block_base.ObjectInterfaceID = MyAPISession.NextInstanceID++;
			return block_base;
		}

		internal static MyAPIJumpGateController GetNewBlock(MyJumpGateController block)
		{
			if (block == null) return null;
			MyAPIJumpGateController block_base = new MyAPIJumpGateController(block);
			block_base = (MyAPIJumpGateController) MyAPISession.CubeBlockAPIs.AddOrUpdate(block.TerminalBlock.EntityId, block_base, (_, old_block) => {
				block_base.CubeBlock = null;
				block_base.BlockSettings = null;
				++old_block.ReferenceCounter;
				return old_block;
			});

			if (block_base.ObjectInterfaceID == -1) block_base.ObjectInterfaceID = MyAPISession.NextInstanceID++;
			return block_base;
		}

		internal static MyAPIJumpGateDrive GetNewBlock(MyJumpGateDrive block)
		{
			if (block == null) return null;
			MyAPIJumpGateDrive block_base = new MyAPIJumpGateDrive(block);
			block_base = (MyAPIJumpGateDrive) MyAPISession.CubeBlockAPIs.AddOrUpdate(block.TerminalBlock.EntityId, block_base, (_, old_block) => {
				block_base.CubeBlock = null;
				++old_block.ReferenceCounter;
				return old_block;
			});

			if (block_base.ObjectInterfaceID == -1) block_base.ObjectInterfaceID = MyAPISession.NextInstanceID++;
			return block_base;
		}

		internal static MyAPIJumpGateServerAntenna GetNewBlock(MyJumpGateServerAntenna block)
		{
			if (block == null) return null;
			MyAPIJumpGateServerAntenna block_base = new MyAPIJumpGateServerAntenna(block);
			block_base = (MyAPIJumpGateServerAntenna) MyAPISession.CubeBlockAPIs.AddOrUpdate(block.TerminalBlock.EntityId, block_base, (_, old_block) => {
				block_base.CubeBlock = null;
				++old_block.ReferenceCounter;
				return old_block;
			});

			if (block_base.ObjectInterfaceID == -1) block_base.ObjectInterfaceID = MyAPISession.NextInstanceID++;
			return block_base;
		}

		internal static MyAPIJumpGateConstruct GetNewConstruct(MyJumpGateConstruct grid)
		{
			if (grid == null) return null;
			MyAPIJumpGateConstruct grid_base = new MyAPIJumpGateConstruct(grid);
			grid_base = MyAPISession.JumpGateGridAPIs.AddOrUpdate(grid.CubeGridID, grid_base, (_, old_grid) => {
				grid_base.Construct = null;
				++grid_base.ReferenceCounter;
				return old_grid;
			});

			if (grid_base.ObjectInterfaceID == -1) grid_base.ObjectInterfaceID = MyAPISession.NextInstanceID++;
			return grid_base;
		}

		internal static MyAPIJumpGate GetNewJumpGate(MyJumpGate gate)
		{
			if (gate == null) return null;
			JumpGateUUID uuid = JumpGateUUID.FromJumpGate(gate);
			MyAPIJumpGate gate_base = new MyAPIJumpGate(gate);
			gate_base = MyAPISession.JumpGateAPIs.AddOrUpdate(uuid, gate_base, (_, old_gate) => {
				gate_base.JumpGate = null;
				++gate_base.ReferenceCounter;
				return old_gate;
			});

			if (gate_base.ObjectInterfaceID == -1) gate_base.ObjectInterfaceID = MyAPISession.NextInstanceID++;
			return gate_base;
		}
		#endregion

		#region Public API Methods
		/// <summary>
		/// Gets all valid jump gates stored by all grids in this session
		/// </summary>
		/// <param name="filter">A predicate to match jump gates against</param>
		/// <param name="jump_gates">All attached jump gates<br />List will not be cleared</param>
		public void GetAllJumpGates(List<MyAPIJumpGate> jump_gates, Func<MyAPIJumpGate, bool> filter = null)
		{
			List<MyJumpGate> _jump_gates = new List<MyJumpGate>(jump_gates.Capacity);
			MyJumpGateModSession.Instance.GetAllJumpGates(_jump_gates);

			foreach (MyJumpGate gate in _jump_gates)
			{
				MyAPIJumpGate api = MyAPISession.GetNewJumpGate(gate);
				bool keep = false;
				try { if (keep = (filter == null || filter(api))) jump_gates.Add(api); }
				finally { if (!keep) api.Release(); };
			}
		}

		/// <summary>
		/// Gets all valid jump gate grids stored in this session
		/// </summary>
		/// <param name="filter">A predicate to match jump gate grids against</param>
		/// <param name="grids">All jump gate grid constructs<br />List will be cleared</param>
		public void GetAllJumpGateGrids(List<MyAPIJumpGateConstruct> grids, Func<MyAPIJumpGateConstruct, bool> filter = null)
		{
			List<MyJumpGateConstruct> _grids = new List<MyJumpGateConstruct>(grids.Capacity);
			MyJumpGateModSession.Instance.GetAllJumpGateGrids(_grids);

			foreach (MyJumpGateConstruct grid in _grids)
			{
				MyAPIJumpGateConstruct api = MyAPISession.GetNewConstruct(grid);
				bool keep = false;
				try { if (keep = (filter == null || filter(api))) grids.Add(api); }
				finally { if (!keep) api.Release(); };
			}
		}

		/// <summary>
		/// </summary>
		/// <returns>Whether all store grids had at least one update</returns>
		public bool AllFirstTickComplete()
		{
			return MyJumpGateModSession.Instance.AllFirstTickComplete();
		}

		/// <summary>
		/// Checks if a construct exists with the given ID in the internal grid map
		/// </summary>
		/// <param name="grid_id">The grid's ID</param>
		/// <returns>Existedness</returns>
		public bool HasCubeGrid(long grid_id)
		{
			return MyJumpGateModSession.Instance.HasCubeGrid(grid_id);
		}

		/// <summary>
		/// Whether the grid should be considered valid in multiplayer context<br />
		/// For server: Returns MyJumpGateConstruct::isValid()<br />
		/// For client: Returns whether said grid is stored in the map
		/// </summary>
		/// <param name="grid">The grid to check</param>
		/// <returns>True if this grid should be considered valid</returns>
		public bool IsJumpGateGridMultiplayerValid(MyAPIJumpGateConstruct grid)
		{
			return MyJumpGateModSession.Instance.IsJumpGateGridMultiplayerValid(grid.Construct);
		}

		/// <summary>
		/// </summary>
		/// <param name="grid">The grid construct</param>
		/// <returns>True if this grid has a duplicate construct</returns>
		public bool HasDuplicateGrid(MyAPIJumpGateConstruct grid)
		{
			return MyJumpGateModSession.Instance.HasDuplicateGrid(grid.Construct);
		}

		/// <summary>
		/// </summary>
		/// <returns>The average of all construct update times</returns>
		public double AverageGridUpdateTime60()
		{
			return MyJumpGateModSession.Instance.AverageGridUpdateTime60();
		}

		/// <summary>
		/// </summary>
		/// <returns>Returns the longest construct update time within the 60 tick buffer</returns>
		public double LocalLongestGridUpdateTime60()
		{
			return MyJumpGateModSession.Instance.LocalLongestGridUpdateTime60();
		}

		/// <summary>
		/// </summary>
		/// <returns>The average of all session update times</returns>
		public double AverageSessionUpdateTime60()
		{
			return MyJumpGateModSession.Instance.AverageSessionUpdateTime60();
		}

		/// <summary>
		/// </summary>
		/// <returns>Returns the longest session update time within the 60 tick buffer</returns>
		public double LocalLongestSessionUpdateTime60()
		{
			return MyJumpGateModSession.Instance.LocalLongestSessionUpdateTime60();
		}

		/// <summary>
		/// Gets the equivilent MyJumpGateConstruct given a cube grid ID
		/// </summary>
		/// <param name="id">The cube grid ID</param>
		/// <returns>The matching MyJumpGateConstruct or null if not found</returns>
		public MyAPIJumpGateConstruct GetJumpGateGrid(long id)
		{
			return MyAPISession.GetNewConstruct(MyJumpGateModSession.Instance.GetJumpGateGrid(id));
		}

		/// <summary>
		/// Gets the equivilent MyJumpGateConstruct given a cube grid
		/// </summary>
		/// <param name="cube_grid">The cube grid</param>
		/// <returns>The matching MyJumpGateConstruct or null if not found</returns>
		public MyAPIJumpGateConstruct GetJumpGateGrid(IMyCubeGrid cube_grid)
		{
			return MyAPISession.GetNewConstruct(MyJumpGateModSession.Instance.GetJumpGateGrid(cube_grid));
		}

		/// <summary>
		/// Gets the equivilent MyJumpGateConstruct given a JumpGateUUID
		/// </summary>
		/// <param name="uuid">The cube grid UUID</param>
		/// <returns>The matching MyJumpGateConstruct or null if not found</returns>
		public MyAPIJumpGateConstruct GetJumpGateGrid(JumpGateUUID uuid)
		{
			return MyAPISession.GetNewConstruct(MyJumpGateModSession.Instance.GetJumpGateGrid(uuid));
		}

		/// <summary>
		/// Gets the equivilent MyJumpGateConstruct given a cube grid ID
		/// </summary>
		/// <param name="id">The cube grid ID</param>
		/// <returns>The matching MyJumpGateConstruct or null if not found or marked closed</returns>
		public MyAPIJumpGateConstruct GetUnclosedJumpGateGrid(long id)
		{
			return MyAPISession.GetNewConstruct(MyJumpGateModSession.Instance.GetUnclosedJumpGateGrid(id));
		}

		/// <summary>
		/// Gets the equivilent MyJumpGateConstruct given a cube grid
		/// </summary>
		/// <param name="cube_grid">The cube grid</param>
		/// <returns>The matching MyJumpGateConstruct or null if not found or marked closed</returns>
		public MyAPIJumpGateConstruct GetUnclosedJumpGateGrid(IMyCubeGrid cube_grid)
		{
			return MyAPISession.GetNewConstruct(MyJumpGateModSession.Instance.GetUnclosedJumpGateGrid(cube_grid));
		}

		/// <summary>
		/// Gets the equivilent MyJumpGateConstruct given a JumpGateUUID
		/// </summary>
		/// <param name="uuid">The cube grid UUID</param>
		/// <returns>The matching MyJumpGateConstruct or null if not found or marked closed</returns>
		public MyAPIJumpGateConstruct GetUnclosedJumpGateGrid(JumpGateUUID uuid)
		{
			return MyAPISession.GetNewConstruct(MyJumpGateModSession.Instance.GetUnclosedJumpGateGrid(uuid));
		}

		/// <summary>
		/// Gets the equivilent MyJumpGate given a JumpGateUUID
		/// </summary>
		/// <param name="uuid">The jump gate's JumpGateUUID</param>
		/// <returns>The matching MyJumpGate or null if not found</returns>
		public MyAPIJumpGate GetJumpGate(JumpGateUUID uuid)
		{
			return MyAPISession.GetNewJumpGate(MyJumpGateModSession.Instance.GetJumpGate(uuid));
		}
		#endregion
	}
}
