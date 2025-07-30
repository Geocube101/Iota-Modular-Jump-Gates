using IOTA.ModularJumpGates.API.ModAPI.Util;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI;
using VRageMath;

namespace IOTA.ModularJumpGates.API.ModAPI
{
	public class MyModAPISession : MyModAPIObjectBase
	{
		public static readonly long ModAPIID = 3313236685;
		public static readonly int[] ModAPIVersion = new int[2] { 1, 3 };
		public static MyModAPISession Instance { get; private set; } = null;

		/// <summary>
		/// Initializes the Mod API Session
		/// </summary>
		/// <param name="context">Your mod context</param>
		/// <returns>Whether the API was initialized</returns>
		public static bool Init(IMyModContext context)
		{
			bool result = false;
			MyAPIGateway.Utilities.SendModMessage(MyModAPISession.ModAPIID, new Dictionary<string, object>() {
				["Type"] = "modapi",
				["Callback"] = (Action<Dictionary<string, object>>) ((attributes) => {
					if (result = attributes != null) MyModAPISession.Instance = new MyModAPISession(attributes);
				}),
				["Version"] = MyModAPISession.ModAPIVersion,
				["ModContext"] = context,
			});
			return result;
		}

		private MyModAPISession(Dictionary<string, object> attributes) : base(attributes) { }

		/// <summary>
		/// The current, session local game tick
		/// </summary>
		public ulong GameTick => this.GetAttribute<ulong>("GameTick");

		/// <summary>
		/// Whether to show debug draw
		/// </summary>
		public bool DebugMode => this.GetAttribute<bool>("DebugMode");

		/// <summary>
		/// The current session component status<br />
		/// 0 - OFFLINE, 1 - LOADING, 2 - RUNNING, 3 - UNLOADING
		/// </summary>
		public byte SessionStatus => this.GetAttribute<byte>("SessionStatus");

		/// <summary>
		/// The world's world matrix
		/// </summary>
		public MatrixD WorldMatrix => this.GetAttribute<MatrixD>("WorldMatrix");

		/// <summary>
		/// Whether all entities in session are loaded<br />
		/// <i>Always false on client</i>
		/// </summary>
		public bool AllSessionEntitiesLoaded => this.GetAttribute<bool>("AllSessionEntitiesLoaded");

		/// <summary>
		/// Whether the session has loaded completely
		/// </summary>
		public bool InitializationComplete => this.GetAttribute<bool>("InitializationComplete");

		/// <summary>
		/// Requests a download of the specified construct from server<br />
		/// Does nothing if not standalone multiplayer client
		/// </summary>
		/// <param name="grid_id">The grid ID to download</param>
		public void RequestGridDownload(long grid_id)
		{
			this.GetMethod<Action<long>>("RequestGridDownload")(grid_id);
		}

		/// <summary>
		/// Requests a download of all constructs from server<br />
		/// Does nothing if not standalone multiplayer client
		/// </summary>
		public void RequestGridsDownload()
		{
			this.GetMethod<Action>("RequestGridsDownload")();
		}

		/// <summary>
		/// </summary>
		/// <returns>Whether all store grids had at least one update</returns>
		public bool AllFirstTickComplete()
		{
			return this.GetMethod<Func<bool>>("AllFirstTickComplete")();
		}

		/// <summary>
		/// Checks if a construct exists with the given ID in the internal grid map
		/// </summary>
		/// <param name="grid_id">The grid's ID</param>
		/// <returns>Existedness</returns>
		public bool HasCubeGrid(long grid_id)
		{
			return this.GetMethod<Func<long, bool>>("HasCubeGrid")(grid_id);
		}

		/// <summary>
		/// Whether the grid should be considered valid in multiplayer context<br />
		/// For server: Returns MyJumpGateConstruct::IsValid()<br />
		/// For client: Returns whether said grid is stored in the map
		/// </summary>
		/// <param name="grid">The grid to check</param>
		/// <returns>True if this grid should be considered valid</returns>
		public bool IsJumpGateGridMultiplayerValid(MyModAPIJumpGateConstruct grid)
		{
			return (grid == null) ? false : this.GetMethod<Func<long, bool>>("IsJumpGateGridMultiplayerValid")(grid.CubeGridID);
		}

		/// <summary>
		/// </summary>
		/// <param name="grid">The grid construct</param>
		/// <returns>True if this grid has a duplicate construct</returns>
		public bool HasDuplicateGrid(MyModAPIJumpGateConstruct grid)
		{
			return (grid == null) ? false : this.GetMethod<Func<long, bool>>("HasDuplicateGrid")(grid.CubeGridID);
		}

		/// <summary>
		/// Moves the specified construct position in the internal grid map<br />
		/// New ID must be an available spot and a subgrid ID of the specified construct
		/// </summary>
		/// <param name="grid">The unclosed construct to move</param>
		/// <param name="new_id">The subgrid ID to move to</param>
		/// <returns>Whether the grid was moved</returns>
		public bool MoveGrid(MyModAPIJumpGateConstruct grid, long new_id)
		{
			return (grid == null) ? false : this.GetMethod<Func<long, long, bool>>("MoveGrid")(grid.CubeGridID, new_id);
		}

		/// <summary>
		/// Checks whether the block is a derivative of CubeBlockBase
		/// </summary>
		/// <param name="block">The block to check</param>
		/// <returns>Whether the "CubeBlockBase" game logic component is attached</returns>
		public bool IsBlockCubeBlockBase(IMyTerminalBlock block)
		{
			return this.GetMethod<Func<IMyTerminalBlock, bool>>("IsBlockCubeBlockBase")(block);
		}

		/// <summary>
		/// Checks whether the block is a jump gate controller
		/// </summary>
		/// <param name="block">The block to check</param>
		/// <returns>Whether the "JumpGateController" game logic component is attached</returns>
		public bool IsBlockJumpGateController(IMyTerminalBlock block)
		{
			return this.GetMethod<Func<IMyTerminalBlock, bool>>("IsBlockJumpGateController")(block);
		}

		/// <summary>
		/// Checks whether the block is a jump gate drive
		/// </summary>
		/// <param name="block">The block to check</param>
		/// <returns>Whether the "JumpGateDrive" game logic component is attached</returns>
		public bool IsBlockJumpGateDrive(IMyTerminalBlock block)
		{
			return this.GetMethod<Func<IMyTerminalBlock, bool>>("IsBlockJumpGateDrive")(block);
		}

		/// <summary>
		/// Checks whether the block is a jump gate capacitor
		/// </summary>
		/// <param name="block">The block to check</param>
		/// <returns>Whether the "JumpGateCapacitor" game logic component is attached</returns>
		public bool IsBlockJumpGateCapacitor(IMyTerminalBlock block)
		{
			return this.GetMethod<Func<IMyTerminalBlock, bool>>("IsBlockJumpGateCapacitor")(block);
		}

		/// <summary>
		/// Checks whether the block is a jump gate remote antenna
		/// </summary>
		/// <param name="block">The block to check</param>
		/// <returns>Whether the "JumpGateRemoteAntenna" game logic component is attached</returns>
		public bool IsBlockJumpGateRemoteAntenna(IMyTerminalBlock block)
		{
			return this.GetMethod<Func<IMyTerminalBlock, bool>>("IsBlockJumpGateRemoteAntenna")(block);
		}

		/// <summary>
		/// Checks whether the block is a jump gate remote link
		/// </summary>
		/// <param name="block">The block to check</param>
		/// <returns>Whether the "JumpGateRemoteLink" game logic component is attached</returns>
		public bool IsBlockJumpGateRemoteLink(IMyTerminalBlock block)
		{
			return this.GetMethod<Func<IMyTerminalBlock, bool>>("IsBlockJumpGateRemoteLink")(block);
		}

		/// <summary>
		/// Checks whether the block is a jump gate server antenna
		/// </summary>
		/// <param name="block">The block to check</param>
		/// <returns>Whether the "JumpGateServerAntenna" game logic component is attached</returns>
		public bool IsBlockJumpGateServerAntenna(IMyTerminalBlock block)
		{
			return this.GetMethod<Func<IMyTerminalBlock, bool>>("IsBlockJumpGateServerAntenna")(block);
		}

		/// <summary>
		/// </summary>
		/// <returns>The average of all construct update times</returns>
		public double AverageGridUpdateTime60()
		{
			return this.GetMethod<Func<double>>("AverageGridUpdateTime60")();
		}

		/// <summary>
		/// </summary>
		/// <returns>Returns the longest construct update time within the 60 tick buffer</returns>
		public double LocalLongestGridUpdateTime60()
		{
			return this.GetMethod<Func<double>>("LocalLongestGridUpdateTime60")();
		}

		/// <summary>
		/// </summary>
		/// <returns>The average of all session update times</returns>
		public double AverageSessionUpdateTime60()
		{
			return this.GetMethod<Func<double>>("AverageSessionUpdateTime60")();
		}

		/// <summary>
		/// </summary>
		/// <returns>Returns the longest session update time within the 60 tick buffer</returns>
		public double LocalLongestSessionUpdateTime60()
		{
			return this.GetMethod<Func<double>>("LocalLongestSessionUpdateTime60")();
		}

		/// <summary>
		/// Gets the block as a CubeBlockBase instance or null if not a derivative of CubeBlockBase
		/// </summary>
		/// <param name="block">The block to convert</param>
		/// <returns>The "CubeBlockBase" game logic component or null</returns>
		public MyModAPICubeBlockBase GetBlockAsCubeBlockBase(IMyTerminalBlock block)
		{
			return MyModAPICubeBlockBase.New(this.GetMethod<Func<IMyTerminalBlock, Dictionary<string, object>>>("GetBlockAsCubeBlockBase")(block));
		}

		/// <summary>
		/// Gets the block as a jump gate controller or null if not a jump gate controller
		/// </summary>
		/// <param name="block">The block to convert</param>
		/// <returns>The "JumpGateController" game logic component or null</returns>
		public MyModAPIJumpGateController GetBlockAsJumpGateController(IMyTerminalBlock block)
		{
			return MyModAPIJumpGateController.New(this.GetMethod<Func<IMyTerminalBlock, Dictionary<string, object>>>("GetBlockAsJumpGateController")(block));
		}

		/// <summary>
		/// Gets the block as a jump gate drive or null if not a jump gate drive
		/// </summary>
		/// <param name="block">The block to convert</param>
		/// <returns>The "JumpGateDrive" game logic component or null</returns>
		public MyModAPIJumpGateDrive GetBlockAsJumpGateDrive(IMyTerminalBlock block)
		{
			return MyModAPIJumpGateDrive.New(this.GetMethod<Func<IMyTerminalBlock, Dictionary<string, object>>>("GetBlockAsJumpGateDrive")(block));
		}

		/// <summary>
		/// Gets the block as a jump gate capacitor of null if not a jump gate capacitor
		/// </summary>
		/// <param name="block">The block to convert</param>
		/// <returns>The "JumpGateCapacitor" game logic component or null</returns>
		public MyModAPIJumpGateCapacitor GetBlockAsJumpGateCapacitor(IMyTerminalBlock block)
		{
			return MyModAPIJumpGateCapacitor.New(this.GetMethod<Func<IMyTerminalBlock, Dictionary<string, object>>>("GetBlockAsJumpGateCapacitor")(block));
		}

		/// <summary>
		/// Gets the block as a jump gate remote antenna of null if not a jump gate remote antenna
		/// </summary>
		/// <param name="block">The block to convert</param>
		/// <returns>The "JumpGateRemoteAntenna" game logic component or null</returns>
		public MyModAPIJumpGateRemoteAntenna GetBlockAsJumpGateRemoteAntenna(IMyTerminalBlock block)
		{
			return MyModAPIJumpGateRemoteAntenna.New(this.GetMethod<Func<IMyTerminalBlock, Dictionary<string, object>>>("GetBlockAsJumpGateRemoteAntenna")(block));
		}

		/// <summary>
		/// Gets the block as a jump gate remote link of null if not a jump gate remote link
		/// </summary>
		/// <param name="block">The block to convert</param>
		/// <returns>The "JumpGateRemoteLink" game logic component or null</returns>
		public MyModAPIJumpGateRemoteLink GetBlockAsJumpGateRemoteLink(IMyTerminalBlock block)
		{
			return MyModAPIJumpGateRemoteLink.New(this.GetMethod<Func<IMyTerminalBlock, Dictionary<string, object>>>("GetBlockAsJumpGateRemoteLink")(block));
		}

		/// <summary>
		/// Gets the block as a jump gate server antenna of null if not a jump gate server antenna
		/// </summary>
		/// <param name="block">The block to convert</param>
		/// <returns>The "JumpGateServerAntenna" game logic component or null</returns>
		public MyModAPIJumpGateServerAntenna GetBlockAsJumpGateServerAntenna(IMyTerminalBlock block)
		{
			return MyModAPIJumpGateServerAntenna.New(this.GetMethod<Func<IMyTerminalBlock, Dictionary<string, object>>>("GetBlockAsJumpGateServerAntenna")(block));
		}

		/// <summary>
		/// Gets the equivilent MyJumpGateConstruct given a cube grid ID
		/// </summary>
		/// <param name="id">The cube grid ID</param>
		/// <returns>The matching MyJumpGateConstruct or null if not found</returns>
		public MyModAPIJumpGateConstruct GetJumpGateGrid(long id)
		{
			return MyModAPIJumpGateConstruct.New(this.GetMethod<Func<long, Dictionary<string, object>>>("GetJumpGateGridL")(id));
		}

		/// <summary>
		/// Gets the equivilent MyJumpGateConstruct given a cube grid
		/// </summary>
		/// <param name="cube_grid">The cube grid</param>
		/// <returns>The matching MyJumpGateConstruct or null if not found</returns>
		public MyModAPIJumpGateConstruct GetJumpGateGrid(IMyCubeGrid cube_grid)
		{
			return MyModAPIJumpGateConstruct.New(this.GetMethod<Func<IMyCubeGrid, Dictionary<string, object>>>("GetJumpGateGridC")(cube_grid));
		}

		/// <summary>
		/// Gets the equivilent MyJumpGateConstruct given a JumpGateUUID
		/// </summary>
		/// <param name="uuid">The cube grid UUID</param>
		/// <returns>The matching MyJumpGateConstruct or null if not found</returns>
		public MyModAPIJumpGateConstruct GetJumpGateGrid(JumpGateUUID uuid)
		{
			return MyModAPIJumpGateConstruct.New(this.GetMethod<Func<long[], Dictionary<string, object>>>("GetJumpGateGridG")(uuid.Packed()));
		}

		/// <summary>
		/// Gets the equivilent MyJumpGateConstruct given a cube grid ID
		/// </summary>
		/// <param name="id">The cube grid ID</param>
		/// <returns>The matching MyJumpGateConstruct or null if not found or marked closed</returns>
		public MyModAPIJumpGateConstruct GetUnclosedJumpGateGrid(long id)
		{
			return MyModAPIJumpGateConstruct.New(this.GetMethod<Func<long, Dictionary<string, object>>>("GetUnclosedJumpGateGridL")(id));
		}

		/// <summary>
		/// Gets the equivilent MyJumpGateConstruct given a cube grid
		/// </summary>
		/// <param name="cube_grid">The cube grid</param>
		/// <returns>The matching MyJumpGateConstruct or null if not found or marked closed</returns>
		public MyModAPIJumpGateConstruct GetUnclosedJumpGateGrid(IMyCubeGrid cube_grid)
		{
			return MyModAPIJumpGateConstruct.New(this.GetMethod<Func<IMyCubeGrid, Dictionary<string, object>>>("GetUnclosedJumpGateGridC")(cube_grid));
		}

		/// <summary>
		/// Gets the equivilent MyJumpGateConstruct given a JumpGateUUID
		/// </summary>
		/// <param name="uuid">The cube grid UUID</param>
		/// <returns>The matching MyJumpGateConstruct or null if not found or marked closed</returns>
		public MyModAPIJumpGateConstruct GetUnclosedJumpGateGrid(JumpGateUUID uuid)
		{
			return MyModAPIJumpGateConstruct.New(this.GetMethod<Func<long[], Dictionary<string, object>>>("GetUnclosedJumpGateGridG")(uuid.Packed()));
		}

		/// <summary>
		/// Gets the equivilent MyJumpGateConstruct given a cube grid ID<br />
		/// This will not check subgrids
		/// </summary>
		/// <param name="id">The cube grid ID</param>
		/// <returns>The matching MyJumpGateConstruct or null if not found or marked closed</returns>
		public MyModAPIJumpGateConstruct GetDirectJumpGateGrid(long id)
		{
			return MyModAPIJumpGateConstruct.New(this.GetMethod<Func<long, Dictionary<string, object>>>("GetDirectJumpGateGridL")(id));
		}

		/// <summary>
		/// Gets the equivilent MyJumpGateConstruct given a cube grid<br />
		/// This will not check subgrids
		/// </summary>
		/// <param name="cube_grid">The cube grid</param>
		/// <returns>The matching MyJumpGateConstruct or null if not found or marked closed</returns>
		public MyModAPIJumpGateConstruct GetDirectJumpGateGrid(IMyCubeGrid cube_grid)
		{
			return MyModAPIJumpGateConstruct.New(this.GetMethod<Func<IMyCubeGrid, Dictionary<string, object>>>("GetDirectJumpGateGridC")(cube_grid));
		}

		/// <summary>
		/// Gets the equivilent MyJumpGateConstruct given a JumpGateUUID<br />
		/// This will not check subgrids
		/// </summary>
		/// <param name="uuid">The cube grid UUID</param>
		/// <returns>The matching MyJumpGateConstruct or null if not found or marked closed</returns>
		public MyModAPIJumpGateConstruct GetDirectJumpGateGrid(JumpGateUUID uuid)
		{
			return MyModAPIJumpGateConstruct.New(this.GetMethod<Func<long[], Dictionary<string, object>>>("GetDirectJumpGateGridG")(uuid.Packed()));
		}

		/// <summary>
		/// Gets the equivilent MyJumpGate given a JumpGateUUID
		/// </summary>
		/// <param name="uuid">The jump gate's JumpGateUUID</param>
		/// <returns>The matching MyJumpGate or null if not found</returns>
		public MyModAPIJumpGate GetJumpGate(JumpGateUUID uuid)
		{
			return MyModAPIJumpGate.New(this.GetMethod<Func<long[], Dictionary<string, object>>>("GetJumpGate")(uuid.Packed()));
		}

		/// <summary>
		/// Gets all valid jump gates stored by all grids in this session
		/// </summary>
		public IEnumerable<MyModAPIJumpGate> GetAllJumpGates()
		{
			return this.GetMethod<Func<IEnumerable<Dictionary<string, object>>>>("GetAllJumpGates")().Select(MyModAPIJumpGate.New);
		}

		/// <summary>
		/// Gets all valid jump gate grids stored in this session
		/// </summary>
		/// <param name="filter">A predicate to match jump gate grids against</param>
		/// <param name="grids">All jump gate grid constructs<br />List will be cleared</param>
		public IEnumerable<MyModAPIJumpGateConstruct> GetAllJumpGateGrids()
		{
			return this.GetMethod<Func<IEnumerable<Dictionary<string, object>>>>("GetAllJumpGateGrids")().Select(MyModAPIJumpGateConstruct.New);
		}
	}
}
