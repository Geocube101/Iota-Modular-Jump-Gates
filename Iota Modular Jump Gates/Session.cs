using IOTA.ModularJumpGates.API;
using IOTA.ModularJumpGates.Commands;
using IOTA.ModularJumpGates.CubeBlock;
using IOTA.ModularJumpGates.ISC;
using IOTA.ModularJumpGates.Terminal;
using IOTA.ModularJumpGates.Util;
using IOTA.ModularJumpGates.Util.ConcurrentCollections;
using Sandbox.Engine.Utils;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace IOTA.ModularJumpGates
{
	public enum MySessionStatusEnum : byte { OFFLINE, LOADING, RUNNING, UNLOADING };

	[MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation | MyUpdateOrder.BeforeSimulation)]
	internal class MyJumpGateModSession : MySessionComponentBase
	{
		public static class MyMaterialsHolder
		{
			public static readonly MyStringId WeaponLaser = MyStringId.GetOrCompute("WeaponLaser");
			public static readonly MyStringId GizmoDrawLine = MyStringId.GetOrCompute("GizmoDrawLine");
			public static readonly MyStringId EnabledEntityMarker = MyStringId.GetOrCompute("IOTA.JumpGateControllerIcon.EntityMarker");
			public static readonly MyStringId DisabledEntityMarker = MyStringId.GetOrCompute("IOTA.JumpGateControllerIcon.EntityMarker");
			public static readonly MyStringId GateOfflineControllerIcon = MyStringId.GetOrCompute("IOTA.JumpGateControllerIcon.GateOffline");
			public static readonly MyStringId GateDisconnectedControllerIcon = MyStringId.GetOrCompute("IOTA.JumpGateControllerIcon.NoGateConnected");
			public static readonly MyStringId GateAntennaDisconnectedControllerIcon = MyStringId.GetOrCompute("IOTA.JumpGateControllerIcon.NoAntennaConnection");
		}

		#region Public Static Variables
		/// <summary>
		/// The current, session local game tick
		/// </summary>
		public static ulong GameTick { get; private set; } = 0;

		/// <summary>
		/// The Guid used to store information in mod storage components
		/// </summary>
		public static readonly Guid BlockComponentDataGUID = new Guid("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF");

		/// <summary>
		/// Whether to show debug draw
		/// </summary>
		public static bool DebugMode = false;

		/// <summary>
		/// The current session component status
		/// </summary>
		public static MySessionStatusEnum SessionStatus { get; private set; } = MySessionStatusEnum.OFFLINE;

		/// <summary>
		/// The world's world matrix
		/// </summary>
		public static readonly MatrixD WorldMatrix = MatrixD.CreateWorld(Vector3D.Zero, new Vector3D(0, 0, -1), new Vector3D(0, 1, 0));

		/// <summary>
		/// The Mod ID string used for terminal controls
		/// </summary>
		public static readonly string MODID = "IOTA.ModularJumpGatesMod";

		/// <summary>
		/// The display name used for chat messages
		/// </summary>
		public static readonly string DISPLAYNAME = "[ IMJG ]";

		/// <summary>
		/// Configuration variables as loaded from file or recieved from server
		/// </summary>
		public static Configuration Configuration { get; private set; } = null;

		/// <summary>
		/// Reference to the instance of this session component
		/// </summary>
		public static MyJumpGateModSession Instance { get; private set; } = null;

		/// <summary>
		/// Reference to the instane of this session's network interface
		/// </summary>
		public static MyNetworkInterface Network { get; private set; } = null;
		#endregion

		#region Private Static Variables
		/// <summary>
		/// The time in game ticks used to detect entity load completed
		/// </summary>
		private static ushort InitialEntityLoadedDelayTicks = 300;
		#endregion

		#region Private Variables
		/// <summary>
		/// Whether to force a redraw of all terminal controls
		/// </summary>
		private bool __RedrawAllTerminalControls = false;

		/// <summary>
		/// The number of current active grid update threads
		/// </summary>
		private byte ActiveGridUpdateThreads = 0;

		/// <summary>
		/// The time in game ticks after session start to update grids
		/// </summary>
		private ushort FirstUpdateTimeTicks = 300;

		/// <summary>
		/// The time in game ticks after which all grids are sent from server to all clients
		/// </summary>
		private ushort GridNetworkUpdateDelay = 30 * 60;

		/// <summary>
		/// The gameplay frame counter's value 1 tick ago
		/// </summary>
		private int LastGameplayFrameCounter = 0;

		/// <summary>
		/// Stores the next index for queued animations
		/// </summary>
		private ulong AnimationQueueIndex = 0;

		/// <summary>
		/// Stores the active terminal block
		/// </summary>
		private long InteractedBlock = -1;

		/// <summary>
		/// Stores the last opened terminal page
		/// </summary>
		private MyTerminalPageEnum LastTerminalPage = MyTerminalPageEnum.None;

		/// <summary>
		/// A list of the last 60 update times
		/// </summary>
		private ConcurrentSpinQueue<double> GridUpdateTimeTicks = new ConcurrentSpinQueue<double>(60);

		/// <summary>
		/// A list of the last 60 update times
		/// </summary>
		private ConcurrentSpinQueue<double> SessionUpdateTimeTicks = new ConcurrentSpinQueue<double>(60);

		/// <summary>
		/// Used to store all grids requesting closure
		/// </summary>
		private ConcurrentQueue<KeyValuePair<MyJumpGateConstruct, bool>> GridCloseRequests = new ConcurrentQueue<KeyValuePair<MyJumpGateConstruct, bool>>();

		/// <summary>
		/// Used to store all jump gates requesting closure
		/// </summary>
		private ConcurrentQueue<MyJumpGate> GateCloseRequests = new ConcurrentQueue<MyJumpGate>();

		/// <summary>
		/// Used to store all constructs that are partially loaded
		/// </summary>
		private ConcurrentDictionary<long, byte> PartialSuspendedGridsQueue = new ConcurrentDictionary<long, byte>();

		/// <summary>
		/// Master map for storing grid constructs
		/// </summary>
		private ConcurrentDictionary<long, MyJumpGateConstruct> GridMap = new ConcurrentDictionary<long, MyJumpGateConstruct>();

		/// <summary>
		/// Master map for storing in-progress animations
		/// </summary>
		private ConcurrentDictionary<ulong, AnimationInfo> JumpGateAnimations = new ConcurrentDictionary<ulong, AnimationInfo>();

		/// <summary>
		/// Master map for storing entity warps
		/// </summary>
		private ConcurrentDictionary<long, EntityWarpInfo> EntityWarps = new ConcurrentDictionary<long, EntityWarpInfo>();
		#endregion

		#region Internal Variables
		/// <summary>
		/// The time in game ticks used to detect entity load completed
		/// </summary>
		internal ushort EntityLoadedDelayTicks { get; private set; } = MyJumpGateModSession.InitialEntityLoadedDelayTicks;

		/// <summary>
		/// The number of currently outbound jump gates
		/// </summary>
		internal uint ConcurrentJumpsCounter = 0;
		#endregion

		#region Public Variables
		/// <summary>
		/// Whether all entities in session are loaded<br />
		/// <i>Always false on client</i>
		/// </summary>
		public bool AllSessionEntitiesLoaded => MyNetworkInterface.IsServerLike && this.EntityLoadedDelayTicks == 0;

		/// <summary>
		/// Whether the session has loaded completely
		/// </summary>
		public bool InitializationComplete { get; private set; } = false;

		/// <summary>
		/// Interface for handling API requests
		/// </summary>
		public MyAPIInterface ModAPIInterface { get; private set; } = new MyAPIInterface();
		#endregion

		#region Public Static Methods
		public static void DrawTransparentLine(Vector3D start, Vector3D end, MyStringId? material, ref Vector4 color, float thickness, MyBillboard.BlendTypeEnum blendtype = MyBillboard.BlendTypeEnum.Standard)
		{
			Vector3D dir = end - start;
			float len = (float) dir.Length();
			MyTransparentGeometry.AddLineBillboard(material ?? MyJumpGateModSession.MyMaterialsHolder.GizmoDrawLine, color, start, dir.Normalized(), len, thickness, blendtype);
		}

		/// <summary>
		/// Checks whether the block is a derivative of CubeBlockBase
		/// </summary>
		/// <param name="block">The block to check</param>
		/// <returns>Whether the "CubeBlockBase" game logic component is attached</returns>
		public static bool IsBlockCubeBlockBase(IMyTerminalBlock block)
		{
			return MyJumpGateModSession.GetBlockAsCubeBlockBase(block) != null;
		}

		/// <summary>
		/// Checks whether the block is a jump gate controller
		/// </summary>
		/// <param name="block">The block to check</param>
		/// <returns>Whether the "JumpGateController" game logic component is attached</returns>
		public static bool IsBlockJumpGateController(IMyTerminalBlock block)
		{
			return MyJumpGateModSession.GetBlockAsJumpGateController(block) != null;
		}

		/// <summary>
		/// Checks whether the block is a jump gate drive
		/// </summary>
		/// <param name="block">The block to check</param>
		/// <returns>Whether the "JumpGateDrive" game logic component is attached</returns>
		public static bool IsBlockJumpGateDrive(IMyTerminalBlock block)
		{
			return MyJumpGateModSession.GetBlockAsJumpGateDrive(block) != null;
		}

		/// <summary>
		/// Checks whether the block is a jump gate capacitor
		/// </summary>
		/// <param name="block">The block to check</param>
		/// <returns>Whether the "JumpGateCapacitor" game logic component is attached</returns>
		public static bool IsBlockJumpGateCapacitor(IMyTerminalBlock block)
		{
			return MyJumpGateModSession.GetBlockAsJumpGateCapacitor(block) != null;
		}

		/// <summary>
		/// Checks whether the block is a jump gate remote antenna
		/// </summary>
		/// <param name="block">The block to check</param>
		/// <returns>Whether the "JumpGateRemoteAntenna" game logic component is attached</returns>
		public static bool IsBlockJumpGateRemoteAntenna(IMyTerminalBlock block)
		{
			return MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block) != null;
		}

		/// <summary>
		/// Checks whether the block is a jump gate server antenna
		/// </summary>
		/// <param name="block">The block to check</param>
		/// <returns>Whether the "JumpGateServerAntenna" game logic component is attached</returns>
		public static bool IsBlockJumpGateServerAntenna(IMyTerminalBlock block)
		{
			return MyJumpGateModSession.GetBlockAsJumpGateServerAntenna(block) != null;
		}

		/// <summary>
		/// Checks whether the block is a jump gate remote link
		/// </summary>
		/// <param name="block">The block to check</param>
		/// <returns>Whether the "JumpGateRemoteLink" game logic component is attached</returns>
		public static bool IsBlockJumpGateRemoteLink(IMyTerminalBlock block)
		{
			return MyJumpGateModSession.GetBlockAsJumpGateRemoteLink(block) != null;
		}

		/// <summary>
		/// Checks if the position is both inside a cube grid's bounding box and whether that position is between at least two walls
		/// </summary>
		/// <param name="grid">The grid to check in</param>
		/// <param name="position">The world coordinate</param>
		/// <returns>Insideness</returns>
		public static bool IsPositionInsideGrid(IMyCubeGrid grid, Vector3D position)
		{
			BoundingBoxD aabb = grid.WorldAABB;
			MatrixD world_matrix = grid.WorldMatrix;
			Vector3D vector1, vector2;
			
			vector1 = position - grid.WorldMatrix.Forward * aabb.Extents.Z;
			vector2 = position + grid.WorldMatrix.Forward * aabb.Extents.Z;
			if (grid.RayCastBlocks(vector1, position) != null && grid.RayCastBlocks(vector2, position) != null) return true;

			vector1 = position + grid.WorldMatrix.Up * aabb.Extents.Y;
			vector2 = position - grid.WorldMatrix.Up * aabb.Extents.Y;
			if (grid.RayCastBlocks(vector1, position) != null && grid.RayCastBlocks(vector2, position) != null) return true;

			vector1 = position - grid.WorldMatrix.Left * aabb.Extents.X;
			vector2 = position + grid.WorldMatrix.Left * aabb.Extents.X;
			if (grid.RayCastBlocks(vector1, position) != null && grid.RayCastBlocks(vector2, position) != null) return true;

			return false;
		}

		/// <summary>
		/// Converts a world position vector to a local position vector
		/// </summary>
		/// <param name="world_matrix">The world matrix</param>
		/// <param name="world_pos">The world vector to convert</param>
		/// <returns>The local vector</returns>
		public static Vector3D WorldVectorToLocalVectorP(MatrixD world_matrix, Vector3D world_pos)
		{
			return Vector3D.TransformNormal(world_pos - world_matrix.Translation, MatrixD.Transpose(world_matrix));
		}

		/// <summary>
		/// Converts a world position vector to a local position vector
		/// </summary>
		/// <param name="world_matrix">The world matrix</param>
		/// <param name="world_pos">The world vector to convert</param>
		/// <returns>The local vector</returns>
		public static Vector3D WorldVectorToLocalVectorP(ref MatrixD world_matrix, Vector3D world_pos)
		{
			MatrixD transposed;
			MatrixD.Transpose(ref world_matrix, out transposed);
			return Vector3D.TransformNormal(world_pos - world_matrix.Translation, ref transposed);
		}

		/// <summary>
		/// Converts a local position vector to a world position vector
		/// </summary>
		/// <param name="world_matrix">The world matrix</param>
		/// <param name="world_pos">The local vector to convert</param>
		/// <returns>The world vector</returns>
		public static Vector3D LocalVectorToWorldVectorP(MatrixD world_matrix, Vector3D local_pos)
		{
			return Vector3D.Transform(local_pos, ref world_matrix);
		}

		/// <summary>
		/// Converts a local position vector to a world position vector
		/// </summary>
		/// <param name="world_matrix">The world matrix</param>
		/// <param name="world_pos">The local vector to convert</param>
		/// <returns>The world vector</returns>
		public static Vector3D LocalVectorToWorldVectorP(ref MatrixD world_matrix, Vector3D local_pos)
		{
			return Vector3D.Transform(local_pos, ref world_matrix);
		}

		/// <summary>
		/// Converts a world direction vector to a local direction vector
		/// </summary>
		/// <param name="world_matrix">The world matrix</param>
		/// <param name="world_pos">The world vector to convert</param>
		/// <returns>The local vector</returns>
		public static Vector3D WorldVectorToLocalVectorD(MatrixD world_matrix, Vector3D world_direction)
		{
			return Vector3D.TransformNormal(world_direction, MatrixD.Transpose(world_matrix));
		}

		/// <summary>
		/// Converts a world direction vector to a local direction vector
		/// </summary>
		/// <param name="world_matrix">The world matrix</param>
		/// <param name="world_pos">The world vector to convert</param>
		/// <returns>The local vector</returns>
		public static Vector3D WorldVectorToLocalVectorD(ref MatrixD world_matrix, Vector3D world_direction)
		{
			MatrixD transposed;
			MatrixD.Transpose(ref world_matrix, out transposed);
			return Vector3D.TransformNormal(world_direction, ref transposed);
		}

		/// <summary>
		/// Converts a local direction vector to a world direction vector
		/// </summary>
		/// <param name="world_matrix">The world matrix</param>
		/// <param name="world_pos">The local vector to convert</param>
		/// <returns>The world vector</returns>
		public static Vector3D LocalVectorToWorldVectorD(MatrixD world_matrix, Vector3D local_direction)
		{
			return Vector3D.TransformNormal(local_direction, ref world_matrix);
		}

		/// <summary>
		/// Converts a local direction vector to a world direction vector
		/// </summary>
		/// <param name="world_matrix">The world matrix</param>
		/// <param name="world_pos">The local vector to convert</param>
		/// <returns>The world vector</returns>
		public static Vector3D LocalVectorToWorldVectorD(ref MatrixD world_matrix, Vector3D local_direction)
		{
			return Vector3D.TransformNormal(local_direction, ref world_matrix);
		}

		/// <summary>
		/// Gets a player by player ID
		/// </summary>
		/// <param name="player_id">The player ID</param>
		/// <returns>The specified player or null</returns>
		public static IMyPlayer GetPlayerByID(long player_id)
		{
			List<IMyPlayer> players = new List<IMyPlayer>();
			MyAPIGateway.Players.GetPlayers(players, (player) => player.IdentityId == player_id);
			return players.FirstOrDefault();
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
			if (double.IsPositiveInfinity(value)) return "INFINITE";
			else if (double.IsNegativeInfinity(value)) return "-INFINITE";
			else if (double.IsNaN(value)) return "NaN";
			else if (value == 0) return $"0 {unit}";
			string[] prefixes_up = { "", "K", "M", "G", "T", "P", "E", "Z", "Y", "R", "Q" };
			string[] prefixes_down = { "", "m", "μ", "n", "p", "f", "a", "z", "y", "r", "q" };
			int index = (int) Math.Log(value, 1000);
			index = MathHelper.Clamp(index, -10, 10);
			return $"{Math.Round(value / Math.Pow(1000, index), places)} {((index > 0) ? prefixes_up[index] : prefixes_down[-index])}{unit}";
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
			if (double.IsPositiveInfinity(value)) return "INFINITE";
			else if (double.IsNegativeInfinity(value)) return "-INFINITE";
			else if (double.IsNaN(value)) return "NaN";
			else if (value == 0) return "0";
			int l10 = (int) Math.Floor(Math.Log10(Math.Abs(value)));
			string sign = (value < 0) ? "-" : "";
			value = value / Math.Pow(10, l10);
			return $"{sign}{Math.Round(value, places)}{e}{l10}";
		}

		/// <summary>
		/// Gets the block as a CubeBlockBase instance or null if not a derivative of CubeBlockBase
		/// </summary>
		/// <param name="block">The block to convert</param>
		/// <returns>The "CubeBlockBase" game logic component or null</returns>
		public static MyCubeBlockBase GetBlockAsCubeBlockBase(IMyTerminalBlock block)
		{
			return block?.GameLogic?.GetAs<MyCubeBlockBase>();
		}

		/// <summary>
		/// Gets the block as a jump gate controller or null if not a jump gate controller
		/// </summary>
		/// <param name="block">The block to convert</param>
		/// <returns>The "JumpGateController" game logic component or null</returns>
		public static MyJumpGateController GetBlockAsJumpGateController(IMyTerminalBlock block)
		{
			return block?.GameLogic?.GetAs<MyJumpGateController>();
		}

		/// <summary>
		/// Gets the block as a jump gate drive or null if not a jump gate drive
		/// </summary>
		/// <param name="block">The block to convert</param>
		/// <returns>The "JumpGateDrive" game logic component or null</returns>
		public static MyJumpGateDrive GetBlockAsJumpGateDrive(IMyTerminalBlock block)
		{
			return block?.GameLogic?.GetAs<MyJumpGateDrive>();
		}

		/// <summary>
		/// Gets the block as a jump gate capacitor or null if not a jump gate capacitor
		/// </summary>
		/// <param name="block">The block to convert</param>
		/// <returns>The "JumpGateCapacitor" game logic component or null</returns>
		public static MyJumpGateCapacitor GetBlockAsJumpGateCapacitor(IMyTerminalBlock block)
		{
			return block?.GameLogic?.GetAs<MyJumpGateCapacitor>();
		}

		/// <summary>
		/// Gets the block as a jump gate remote antenna or null if not a jump gate remote antenna
		/// </summary>
		/// <param name="block">The block to convert</param>
		/// <returns>The "JumpGateRemoteAntenna" game logic component or null</returns>
		public static MyJumpGateRemoteAntenna GetBlockAsJumpGateRemoteAntenna(IMyTerminalBlock block)
		{
			return block?.GameLogic?.GetAs<MyJumpGateRemoteAntenna>();
		}

		/// <summary>
		/// Gets the block as a jump gate server antenna or null if not a jump gate server antenna
		/// </summary>
		/// <param name="block">The block to convert</param>
		/// <returns>The "JumpGateServerAntenna" game logic component or null</returns>
		public static MyJumpGateServerAntenna GetBlockAsJumpGateServerAntenna(IMyTerminalBlock block)
		{
			return block?.GameLogic?.GetAs<MyJumpGateServerAntenna>();
		}

		/// <summary>
		/// Gets the block as a jump gate remote link or null if not a jump gate remote link
		/// </summary>
		/// <param name="block">The block to convert</param>
		/// <returns>The "MyJumpGateRemoteLink" game logic component or null</returns>
		public static MyJumpGateRemoteLink GetBlockAsJumpGateRemoteLink(IMyTerminalBlock block)
		{
			return block?.GameLogic?.GetAs<MyJumpGateRemoteLink>();
		}
		#endregion

		#region "MySessionComponentBase" Methods
		/// <summary>
		/// MySessionComponentBase method<br />
		/// Called when session is unloaded<br />
		/// Saves and unloads all relevant mod data and configurations
		/// </summary>
		protected override void UnloadData()
		{
			MyJumpGateModSession.SessionStatus = MySessionStatusEnum.UNLOADING;
			Logger.Log("Closing...");
			base.UnloadData();
			this.ModAPIInterface.Close();
			MyAnimationHandler.Unload();
			MyChatCommandHandler.Close();

			if (MyJumpGateModSession.Network != null && MyJumpGateModSession.Network.Registered)
			{
				MyJumpGateModSession.Network.Off(MyPacketTypeEnum.UPDATE_GRIDS, this.OnNetworkGridsUpdate);
				MyJumpGateModSession.Network.Off(MyPacketTypeEnum.CLOSE_GRID, this.OnNetworkGridClose);
				MyJumpGateModSession.Network.Off(MyPacketTypeEnum.DOWNLOAD_GRID, this.OnNetworkGridDownload);
				MyJumpGateModSession.Network.Off(MyPacketTypeEnum.COMM_LINKED, this.OnNetworkCommLinkedUpdate);
				MyJumpGateModSession.Network.Off(MyPacketTypeEnum.BEACON_LINKED, this.OnNetworkBeaconLinkedUpdate);
				MyJumpGateModSession.Network.Off(MyPacketTypeEnum.UPDATE_CONFIG, this.OnConfigurationUpdate);
				MyJumpGateModSession.Network.Unregister();
			}

			int closed_grids_count = 0;

			foreach (KeyValuePair<long, MyJumpGateConstruct> pair in this.GridMap)
			{
				if (!pair.Value.Closed)
				{
					pair.Value.Dispose();
					++closed_grids_count;
				}
			}

			this.FlushClosureQueues();
			Logger.Log($"Closed {closed_grids_count} Grids");
			foreach (KeyValuePair<ulong, AnimationInfo> pair in this.JumpGateAnimations) pair.Value.Animation.Clean();
			foreach (KeyValuePair<long, EntityWarpInfo> pair in this.EntityWarps) pair.Value.Close();

			this.GridUpdateTimeTicks.Clear();
			this.SessionUpdateTimeTicks.Clear();
			this.GridMap.Clear();
			this.JumpGateAnimations.Clear();
			this.EntityWarps.Clear();
			this.PartialSuspendedGridsQueue.Clear();

			this.ModAPIInterface = null;
			this.GridUpdateTimeTicks = null;
			this.SessionUpdateTimeTicks = null;
			this.GridCloseRequests = null;
			this.GateCloseRequests = null;
			this.GridMap = null;
			this.JumpGateAnimations = null;
			this.EntityWarps = null;
			this.PartialSuspendedGridsQueue = null;

			MyAPIGateway.Entities.OnEntityAdd -= this.OnEntityAdd;
			MyAPIGateway.Entities.OnEntityRemove -= this.OnEntityRemove;
			MyAPIGateway.TerminalControls.CustomControlGetter -= this.OnTerminalSelector;

			MyCubeBlockBase.Instances.Clear();

			MyCubeBlockTerminal.Unload();
			MyJumpGateCapacitorTerminal.Unload();
			MyJumpGateControllerTerminal.Unload();
			MyJumpGateDriveTerminal.Unload();
			MyJumpGateRemoteAntennaTerminal.Unload();
			MyJumpGateRemoteLinkTerminal.Unload();

			MyJumpGateModSession.Configuration.Save();
			MyJumpGateModSession.Configuration = null;
			MyJumpGateModSession.Network = null;
			MyJumpGateModSession.Instance = null;
			MyJumpGateModSession.SessionStatus = MySessionStatusEnum.OFFLINE;

			Logger.Log("Closed.");
			Logger.Flush();
			Logger.Close();
		}

		/// <summary>
		/// MySessionComponentBase method<br />
		/// Called when session is loaded<br />
		/// Loads all relevant mod data and configurations
		/// </summary>
		public override void LoadData()
		{
			// amogst the earliest execution points, but not everything is available at this point.

			// These can be used anywhere, not just in this method/class:
			// MyAPIGateway. - main entry point for the API
			// MyDefinitionManager.Static. - reading/editing definitions
			// MyGamePruningStructure. - fast way of finding entities in an area
			// MyTransparentGeometry. and MySimpleObjectDraw. - to draw sprites (from TransparentMaterials.sbc) in world (they usually live a single tick)
			// MyVisualScriptLogicProvider. - mainly designed for VST but has its uses, use as a last resort.
			// System.Diagnostics.Stopwatch - for measuring code execution time.
			// ...and many more things, ask in #programming-modding in keen's discord for what you want to do to be pointed at the available things to use.

			MyJumpGateModSession.Instance = this;
			Logger.Init();
			Logger.Log("PREINIT - Loading Data...");
			MyJumpGateModSession.SessionStatus = MySessionStatusEnum.LOADING;
			MyJumpGateModSession.Configuration = Configuration.Load();
			MyJumpGateModSession.Network = new MyNetworkInterface(0xFFFF, this.ModContext.ModId);
			if (MyNetworkInterface.IsServerLike && !MyAPIGateway.Utilities.GetVariable($"{MyJumpGateModSession.MODID}.DebugMode", out MyJumpGateModSession.DebugMode)) MyJumpGateModSession.DebugMode = false;
			this.UpdateOnPause = true;

			if (!MyNetworkInterface.IsSingleplayer)
			{
				MyJumpGateModSession.Network.Register();
				MyJumpGateModSession.Network.On(MyPacketTypeEnum.UPDATE_GRIDS, this.OnNetworkGridsUpdate);
				MyJumpGateModSession.Network.On(MyPacketTypeEnum.CLOSE_GRID, this.OnNetworkGridClose);
				MyJumpGateModSession.Network.On(MyPacketTypeEnum.DOWNLOAD_GRID, this.OnNetworkGridDownload);
				MyJumpGateModSession.Network.On(MyPacketTypeEnum.UPDATE_CONFIG, this.OnConfigurationUpdate);
			}

			if (MyNetworkInterface.IsMultiplayerServer)
			{
				MyJumpGateModSession.Network.On(MyPacketTypeEnum.COMM_LINKED, this.OnNetworkCommLinkedUpdate);
				MyJumpGateModSession.Network.On(MyPacketTypeEnum.BEACON_LINKED, this.OnNetworkBeaconLinkedUpdate);
			}

			if (MyNetworkInterface.IsDedicatedMultiplayerServer) MyInterServerCommunication.Register(0, 0, null);
			MyAPIGateway.Entities.OnEntityAdd += this.OnEntityAdd;
			MyAPIGateway.Entities.OnEntityRemove += this.OnEntityRemove;
			this.ModAPIInterface.Init();
			Logger.Log("PREINIT - Loaded.");
		}

		/// <summary>
		/// MySessionComponentBase method<br />
		/// Called before session start<br />
		/// Updates animations and loads controls
		/// </summary>
		public override void BeforeStart()
		{
			base.BeforeStart();
			Logger.Log("INIT - Loading Data...");
			MyAnimationHandler.Load();
			if (MyJumpGateModSession.Network.Registered && MyNetworkInterface.IsStandaloneMultiplayerClient) this.RequestGridsDownload();
			if (!MyNetworkInterface.IsDedicatedMultiplayerServer) MyAPIGateway.Utilities.ShowMessage(MyJumpGateModSession.DISPLAYNAME, "Initializing Constructs...");
			MyAPIGateway.TerminalControls.CustomControlGetter += this.OnTerminalSelector;
			MyChatCommandHandler.Init();
			Logger.Log("INIT - Loaded.");
		}

		/// <summary>
		/// MySessionComponentBase method<br />
		/// Called every tick before simulation<br />
		/// </summary>
		public override void UpdateBeforeSimulation()
		{
			Stopwatch sw = new Stopwatch();
			sw.Start();
			
			try
			{
				base.UpdateBeforeSimulation();
				this.FlushClosureQueues();
				if (MyNetworkInterface.IsServerLike) return;

				foreach (KeyValuePair<long, byte> partial_grid in this.PartialSuspendedGridsQueue)
				{
					byte new_time = (byte) (partial_grid.Value - 1);

					if (new_time > 0)
					{
						this.PartialSuspendedGridsQueue[partial_grid.Key] = new_time;
						continue;
					}

					this.PartialSuspendedGridsQueue.Remove(partial_grid.Key);
					this.RequestGridDownload(partial_grid.Key);
				}
			}
			finally
			{
				sw.Stop();
				this.SessionUpdateTimeTicks.Enqueue(sw.Elapsed.TotalMilliseconds);
			}
		}

		/// <summary>
		/// MySessionComponentBase method<br />
		/// Called every tick after simulation<br />
		/// Primary logic executor; ticks grids, animations, teleport requests, terminal controls, and sound emitters
		/// </summary>
		public override void UpdateAfterSimulation()
		{
			Stopwatch sw = new Stopwatch();
			sw.Start();

			try
			{
				MyJumpGateModSession.SessionStatus = MySessionStatusEnum.RUNNING;
				++MyJumpGateModSession.GameTick;
				MyJumpGateModSession.GameTick = (MyJumpGateModSession.GameTick >= 0xFFFFFFFFFFFFFFF0) ? 0 : MyJumpGateModSession.GameTick;
				this.EntityLoadedDelayTicks -= (this.EntityLoadedDelayTicks > 0) ? (ushort) 1 : (ushort) 0;

				// Update grids
				if (MyNetworkInterface.IsStandaloneMultiplayerClient && MyJumpGateModSession.GameTick == this.FirstUpdateTimeTicks)
				{
					this.RequestGridsDownload();
					Logger.Debug($"INITAL_GRID_UPDATE", 2);
				}

				// Check initialization
				if (MyNetworkInterface.IsServerLike && !this.AllSessionEntitiesLoaded) return;

				if (!this.InitializationComplete && ((MyNetworkInterface.IsServerLike && this.AllFirstTickComplete()) || (MyNetworkInterface.IsStandaloneMultiplayerClient && MyJumpGateModSession.GameTick == this.FirstUpdateTimeTicks)))
				{
					this.InitializationComplete = true;
					MyAPIGateway.Utilities.ShowMessage(MyJumpGateModSession.DISPLAYNAME, "Initialization Complete!");
				}
				
				if (this.ActiveGridUpdateThreads < MyJumpGateModSession.Configuration.GeneralConfiguration.ConcurrentGridUpdateThreads)
				{
					++this.ActiveGridUpdateThreads;
					MyAPIGateway.Parallel.StartBackground(this.TickUpdateGrids);
				}

				// Redraw Terminal Controls
				if (MyNetworkInterface.IsClientLike && this.InitializationComplete && (MyJumpGateModSession.GameTick % 60 == 0 || this.__RedrawAllTerminalControls))
				{
					MyCubeBlockTerminal.UpdateRedrawControls();
					MyJumpGateCapacitorTerminal.UpdateRedrawControls();
					MyJumpGateControllerTerminal.UpdateRedrawControls();
					MyJumpGateDriveTerminal.UpdateRedrawControls();
					MyJumpGateRemoteAntennaTerminal.UpdateRedrawControls();
					MyJumpGateRemoteLinkTerminal.UpdateRedrawControls();
					this.__RedrawAllTerminalControls = false;
				}
				if (MyAPIGateway.Gui.GetCurrentScreen != MyTerminalPageEnum.ControlPanel)
				{
					MyJumpGateControllerTerminal.ResetSearchInputs();
					MyJumpGateRemoteAntennaTerminal.ResetSearchInputs();
				}

				// Update grid non-threadable
				foreach (KeyValuePair<long, MyJumpGateConstruct> pair in this.GridMap)
				{
					MyJumpGateConstruct grid = pair.Value;

					if (!grid.MarkClosed && !grid.IsSuspended && grid.FullyInitialized)
					{
						try
						{
							grid.UpdateNonThreadable();
						}
						catch (Exception e)
						{
							Logger.Error($"Error during construct no-thread tick - {grid.CubeGrid?.EntityId.ToString() ?? "N/A"} ({pair.Key})\n  ...\n[ {e} ]: {e.Message}\n{e.StackTrace}\n{e.InnerException}");
						}
					}
				}

				bool paused = MyNetworkInterface.IsSingleplayer && MyAPIGateway.Session.GameplayFrameCounter == this.LastGameplayFrameCounter;
				this.LastGameplayFrameCounter = MyAPIGateway.Session.GameplayFrameCounter;

				if (!paused)
				{
					// Tick queued entity warps
					foreach (KeyValuePair<long, EntityWarpInfo> pair in this.EntityWarps)
					{
						if (pair.Value.Update())
						{
							pair.Value.Close();
							this.EntityWarps.Remove(pair.Key);
						}
					}

					// Tick queued animations
					Particle.Render();
					foreach (KeyValuePair<ulong, AnimationInfo> pair in this.JumpGateAnimations)
					{
						AnimationInfo animation_info = pair.Value;
						string animation_name = animation_info.Animation.FullAnimationName;
						MyJumpGateAnimation animation = animation_info.Animation;
						animation.Tick(animation_info.AnimationIndex);

						if (animation.Stopped(animation_info.AnimationIndex))
						{
							if (animation_info.CompletionCallback != null) animation_info.CompletionCallback(animation_info.IterCallbackException);
							this.JumpGateAnimations.Remove(pair.Key);
						}
						else if (animation_info.IterCallback != null)
						{
							bool stop;

							try
							{
								stop = !animation_info.IterCallback();
							}
							catch (Exception e)
							{
								stop = true;
								animation_info.IterCallbackException = e;
								Logger.Error($"Error during animation iteration callback - {animation_name}\n  ...\n[ {e} ]: {e.Message}\n{e.StackTrace}\n{e.InnerException}");
							}

							if (stop) animation.Stop();
						}
						else if (animation.JumpGate.Closed) animation.Stop();
					}
				}

				// Update Log
				if (MyJumpGateModSession.GameTick % 300 == 0) Logger.Flush();
			}
			finally
			{
				sw.Stop();
				this.SessionUpdateTimeTicks[-1] += sw.Elapsed.TotalMilliseconds;
			}
		}

		/// <summary>
		/// MySessionComponentBase method<br />
		/// Updates terminal block scrolling for detailed info
		/// </summary>
		public override void HandleInput()
		{
			base.HandleInput();
			if (this.LastTerminalPage == MyTerminalPageEnum.ControlPanel && MyAPIGateway.Gui.GetCurrentScreen != MyTerminalPageEnum.ControlPanel)
				foreach (KeyValuePair<long, MyCubeBlockBase> pair in MyCubeBlockBase.Instances) pair.Value.SetScroll(0);
			this.LastTerminalPage = MyAPIGateway.Gui.GetCurrentScreen;
			if (!MyAPIGateway.Gui.IsCursorVisible || MyAPIGateway.Gui.ChatEntryVisible || MyAPIGateway.Gui.GetCurrentScreen != MyTerminalPageEnum.ControlPanel) return;

			MyCubeBlockBase interacted_cube_block;
			if ((interacted_cube_block = MyCubeBlockBase.Instances.GetValueOrDefault(this.InteractedBlock, null)) == null) return;

			Vector2 area = MyAPIGateway.Input.GetMouseAreaSize();
			Vector2 position = MyAPIGateway.Input.GetMousePosition();
			Vector2 percent_pos = position / area;
			if (percent_pos.X < 0.6273438f || percent_pos.Y < 0.5972222 || percent_pos.X > 0.8605469f || percent_pos.Y > 0.88125f) return;
			
			if (MyAPIGateway.Input.DeltaMouseScrollWheelValue() > 0) interacted_cube_block.Scroll(3);
			else if (MyAPIGateway.Input.DeltaMouseScrollWheelValue() < 0) interacted_cube_block.Scroll(-3);
		}

		/// <summary>
		/// MySessionComponentBase method<br />
		/// Called every tick<br />
		/// Displays debug draw lines
		/// </summary>
		public override void Draw()
		{
			base.Draw();

			// Debug draw
			if (MyJumpGateModSession.DebugMode && !MyNetworkInterface.IsDedicatedMultiplayerServer)
			{
				Vector4 color4;
				Color color;
				Vector3D this_position = MyAPIGateway.Session.Camera.Position;
				MyStringId line_material = MyMaterialsHolder.WeaponLaser;
				List<MyJumpGate> jump_gates = new List<MyJumpGate>();
				List<MyJumpGateDrive> jump_drives = new List<MyJumpGateDrive>();

				foreach (KeyValuePair<long, MyJumpGateConstruct> pair in this.GridMap)
				{
					MyJumpGateConstruct jump_grid = pair.Value;
					if (jump_grid.IsSuspended || jump_grid.CubeGrid == null || !jump_grid.IsValid() || !jump_grid.AtLeastOneUpdate()) continue;
					jump_gates.AddRange(jump_grid.GetJumpGates());
					jump_drives.AddRange(jump_grid.GetAttachedJumpGateDrives());
					color4 = Color.Violet.ToVector4() * 20;
					foreach (MyJumpGate gate in jump_gates) if (gate.TrueEndpoint != null) MySimpleObjectDraw.DrawLine(gate.WorldJumpNode, gate.TrueEndpoint.Value, line_material, ref color4, 10);

					try
					{
						if (Vector3D.Distance(this_position, jump_grid.CubeGrid.WorldMatrix.Translation) > MyJumpGateModSession.Configuration.GeneralConfiguration.DrawSyncDistance) continue;
						MatrixD grid_matrix = jump_grid.CubeGrid.WorldMatrix;
						color4 = Color.Gold.ToVector4();

						foreach (MyJumpGateDrive drive in jump_drives)
						{
							if (drive.IsClosed) continue;
							Vector3D start = drive.GetDriveRaycastStartpoint();
							Vector3D end = drive.GetDriveRaycastEndpoint(drive.MaxRaycastDistance);
							MySimpleObjectDraw.DrawLine(start, end, line_material, ref color4, 0.25f);
						}

						foreach (MyJumpGate gate in jump_gates)
						{
							if (!gate.IsValid()) continue;
							Vector3D world_node = gate.WorldJumpNode;
							MatrixD gate_matrix = gate.GetWorldMatrix(true, true);
							BoundingEllipsoidD jump_ellipse = gate.JumpEllipse;
							bool complete = gate.IsComplete();
							color = Color.Aqua;

							// Display drive intersections
							if (Vector3D.Distance(this_position, world_node) <= 100)
							{
								foreach (Vector3D node in gate.WorldDriveIntersectNodes)
								{
									BoundingBoxD node_box = BoundingBoxD.CreateFromSphere(new BoundingSphereD(MyJumpGateModSession.WorldVectorToLocalVectorP(ref grid_matrix, node), 1));
									MySimpleObjectDraw.DrawTransparentBox(ref grid_matrix, ref node_box, ref color, MySimpleObjectRasterizer.Wireframe, 1, 0.1f, null, line_material);
								}
							}

							// Display gate ellipsoid
							jump_ellipse.Draw((complete) ? Color.Lime : Color.Red, 90, 0.1f, line_material);
							BoundingEllipsoidD effective_ellipse = gate.GetEffectiveJumpEllipse();
							if (complete && effective_ellipse != jump_ellipse) effective_ellipse.Draw(Color.BlueViolet, 90, 0.1f, line_material);
							if (complete && !MyJumpGateModSession.Configuration.GeneralConfiguration.LenientJumps) gate.ShearEllipse.Draw(Color.Red, 90, 0.1f, line_material);
							new BoundingEllipsoidD(jump_ellipse.Radii.Max() * 2, ref jump_ellipse.WorldMatrix).Draw(Color.LightSkyBlue, 90, 0.1f, line_material);
							new BoundingEllipsoidD(jump_ellipse.Radii.Max(), ref jump_ellipse.WorldMatrix).Draw(Color.GhostWhite, 90, 0.1f, line_material);

							// Display gate ellipsoid bounds
							BoundingBoxD ellipse_aabb = BoundingBox.CreateFromHalfExtent(Vector3.Zero, jump_ellipse.Radii);
							Vector3D jump_node = gate.WorldJumpNode;
							color = Color.AliceBlue;
							MySimpleObjectDraw.DrawTransparentBox(ref jump_ellipse.WorldMatrix, ref ellipse_aabb, ref color, MySimpleObjectRasterizer.Wireframe, 1, 0.01f, null, line_material);

							// Display gate normal override
							MyJumpGateControlObject control_object = gate.ControlObject;

							if (control_object != null && control_object.BlockSettings.HasVectorNormalOverride())
							{
								color4 = Color.Magenta;
								Vector3D normal = gate.GetWorldMatrix(false, true).Forward;
								MySimpleObjectDraw.DrawLine(jump_node, jump_node + normal * 500d, line_material, ref color4, 1);
							}

							// Display jump gate endpoint aligned axes
							color4 = Color.Magenta;
							Vector3D axis = MyJumpGateModSession.LocalVectorToWorldVectorP(ref gate_matrix, new Vector3D(5, 0, 0));
							MySimpleObjectDraw.DrawLine(axis, jump_node, line_material, ref color4, 1f);
							color4 = Color.Yellow;
							axis = MyJumpGateModSession.LocalVectorToWorldVectorP(ref gate_matrix, new Vector3D(0, 5, 0));
							MySimpleObjectDraw.DrawLine(axis, jump_node, line_material, ref color4, 1f);
							color4 = Color.Cyan;
							axis = MyJumpGateModSession.LocalVectorToWorldVectorP(ref gate_matrix, new Vector3D(0, 0, 5));
							MySimpleObjectDraw.DrawLine(axis, jump_node, line_material, ref color4, 1f);

							// Display jump gate axes
							gate_matrix = jump_ellipse.WorldMatrix;
							color4 = Color.Red;
							axis = MyJumpGateModSession.LocalVectorToWorldVectorP(ref gate_matrix, new Vector3D(7.5, 0, 0));
							MySimpleObjectDraw.DrawLine(axis, jump_node, line_material, ref color4, 0.5f);
							color4 = Color.Green;
							axis = MyJumpGateModSession.LocalVectorToWorldVectorP(ref gate_matrix, new Vector3D(0, 7.5, 0));
							MySimpleObjectDraw.DrawLine(axis, jump_node, line_material, ref color4, 0.5f);
							color4 = Color.Blue;
							axis = MyJumpGateModSession.LocalVectorToWorldVectorP(ref gate_matrix, new Vector3D(0, 0, 7.5));
							MySimpleObjectDraw.DrawLine(axis, jump_node, line_material, ref color4, 0.5f);
						}
					}
					finally
					{
						jump_gates.Clear();
						jump_drives.Clear();
					}
				}
			}
		}

		/// <summary>
		/// MySessionComponentBase method<br />
		/// Called after world saved<br />
		/// If singleplayer or multiplayer server: Saves configuration data to file
		/// </summary>
		public override void SaveData()
		{
			base.SaveData();
			if (MyNetworkInterface.IsMultiplayerClient) return;
			MyJumpGateModSession.Configuration?.Save();
			MyAPIGateway.Utilities.SetVariable($"{MyJumpGateModSession.MODID}.DebugMode", MyJumpGateModSession.DebugMode);
		}
		#endregion

		#region Private Methods
		/// <summary>
		/// Event handler for when entity added to session
		/// </summary>
		/// <param name="entity">The added entity</param>
		private void OnEntityAdd(IMyEntity entity)
		{
			IMyCubeGrid cube_grid = entity as IMyCubeGrid;
			if (cube_grid?.Physics == null) return;
			Logger.Debug("ENTITY-ADD", 5);

			if (MyNetworkInterface.IsStandaloneMultiplayerClient && MyJumpGateModSession.GameTick > this.FirstUpdateTimeTicks) this.RequestGridDownload(cube_grid.EntityId);
			else if (MyNetworkInterface.IsServerLike) this.AddCubeGridToSession(cube_grid);
		}

		/// <summary>
		/// Event handler for when entity removed from session
		/// </summary>
		/// <param name="entity">The removed entity</param>
		private void OnEntityRemove(IMyEntity entity)
		{
			IMyCubeGrid cube_grid = entity as IMyCubeGrid;
			if (cube_grid?.Physics == null) return;
			Logger.Debug("ENTITY-REMOVE", 5);
			MyJumpGateConstruct parent_grid = this.GridMap.GetValueOrDefault(cube_grid.EntityId, null);
			if (parent_grid?.Closed ?? true) return;
			if (MyNetworkInterface.IsStandaloneMultiplayerClient) parent_grid.Suspend(cube_grid.EntityId);
			else parent_grid.Dispose();
		}

		/// <summary>
		/// Event handler for when a block's terminal controls are iterated
		/// </summary>
		/// <param name="block">The block</param>
		/// <param name="controls">The block's controls</param>
		private void OnTerminalSelector(IMyTerminalBlock block, List<IMyTerminalControl> controls)
		{
			this.InteractedBlock = (MyJumpGateModSession.IsBlockCubeBlockBase(block)) ? block.EntityId : -1;
		}

		/// <summary>
		/// Event handler for grid update packet<br />
		/// If server: Sends current grids to client<br />
		/// If client: Updates internal grid map from server
		/// </summary>
		/// <param name="packet">The received packet</param>
		private void OnNetworkGridsUpdate(MyNetworkInterface.Packet packet)
		{
			if (packet == null) return;

			if (MyNetworkInterface.IsMultiplayerServer && packet.PhaseFrame == 1)
			{
				List<MySerializedJumpGateConstruct> serialized_grids = this.GridMap.Where((pair) => pair.Value.AtLeastOneUpdate() && !pair.Value.MarkClosed).Select((pair) => pair.Value.ToSerialized(false)).ToList();
				MyNetworkInterface.Packet grids_packet = new MyNetworkInterface.Packet {
					PacketType = MyPacketTypeEnum.UPDATE_GRIDS,
					TargetID = packet.SenderID,
					Broadcast = false,
				};
				grids_packet.Payload(serialized_grids);
				grids_packet.Send();
				Logger.Debug($"Got client grid update request - Sent grids: {string.Join(", ", serialized_grids.Select((grid) => grid.UUID.GetJumpGateGrid().ToString()))}", 2);
			}
			else if (MyNetworkInterface.IsStandaloneMultiplayerClient && (packet.PhaseFrame == 1 || packet.PhaseFrame == 2))
			{
				Logger.Debug($"Got server grid update packet", 2);
				List<MySerializedJumpGateConstruct> serialized_grids = packet.Payload<List<MySerializedJumpGateConstruct>>() ?? new List<MySerializedJumpGateConstruct>();
				List<long> closed = new List<long>(this.GridMap.Keys);

				foreach (MySerializedJumpGateConstruct serialized in serialized_grids)
				{
					MyJumpGateConstruct new_grid = this.StoreSerializedGrid(serialized);
					if (new_grid == null) continue;
					new_grid.LastUpdateDateTimeUTC = packet.EpochDateTimeUTC;
					closed.Remove(new_grid.CubeGridID);
				}

				foreach (long gridid in closed) this.CloseGrid(this.GridMap.GetValueOrDefault(gridid, null));
			}
		}

		/// <summary>
		/// Event handler for grid download packet<br />
		/// If server: Sends the specified grid to client<br />
		/// If client: Updates the specified grid from server
		/// </summary>
		/// <param name="packet">The received packet</param>
		private void OnNetworkGridDownload(MyNetworkInterface.Packet packet)
		{
			if (packet == null) return;

			if (MyNetworkInterface.IsMultiplayerServer && packet.PhaseFrame == 1)
			{
				long grid_id = packet.Payload<long>();
				packet = packet.Forward(packet.SenderID, false);
				packet.Payload(this.GridMap.GetValueOrDefault(grid_id, null)?.ToSerialized(false));
				packet.Send();
				Logger.Debug($"Got client grid download request - Sent grid: {grid_id}", 2);
			}
			else if (MyNetworkInterface.IsStandaloneMultiplayerClient && packet.PhaseFrame == 2)
			{
				MyJumpGateConstruct new_grid = this.StoreSerializedGrid(packet.Payload<MySerializedJumpGateConstruct>());
				if (new_grid == null) return;
				new_grid.LastUpdateDateTimeUTC = packet.EpochDateTimeUTC;
				Logger.Debug($"Grid '{new_grid.CubeGridID}' NETWORK_DOWNLOAD", 2);
			}
		}

		/// <summary>
		/// Callback triggered when this object receives a comm-linked grids update request
		/// </summary>
		/// <param name="packet">The update request packet</param>
		private void OnNetworkCommLinkedUpdate(MyNetworkInterface.Packet packet)
		{
			if (packet == null || !MyNetworkInterface.IsMultiplayerServer || packet.PhaseFrame != 1) return;
			long construct_id = packet.Payload<long>();
			MyJumpGateConstruct target = this.GridMap.GetValueOrDefault(construct_id, null);
			List<MyJumpGateConstruct> comm_linked = target?.GetCommLinkedJumpGateGrids().ToList();
			packet = packet.Forward(packet.SenderID, false);
			packet.Payload(new KeyValuePair<long, List<long>>(construct_id, comm_linked?.Select((grid) => grid.CubeGridID)?.ToList()));
			packet.Send();
			Logger.Debug($"Got client comm-link request for \"{construct_id}\", sent grids: {string.Join(", ", comm_linked?.Select((grid) => grid.CubeGridID.ToString()).ToList() ?? new List<string>() { "N/A" })}", 3);
		}

		/// <summary>
		/// Callback triggered when this object receives a beacon-linked grids update request
		/// </summary>
		/// <param name="packet">The update request packet</param>
		private void OnNetworkBeaconLinkedUpdate(MyNetworkInterface.Packet packet)
		{
			if (packet == null || !MyNetworkInterface.IsMultiplayerServer || packet.PhaseFrame != 1) return;
			long construct_id = packet.Payload<long>();
			MyJumpGateConstruct target = this.GridMap.GetValueOrDefault(construct_id, null);
			List<MyBeaconLinkWrapper> beacon_linked = target?.GetBeaconsWithinReverseBroadcastSphere().ToList();
			packet = packet.Forward(packet.SenderID, false);
			packet.Payload(new KeyValuePair<long, List<MyBeaconLinkWrapper>>(construct_id, beacon_linked));
			packet.Send();
			Logger.Debug($"Got client beacon-link request for \"{construct_id}\", sent beacons: {string.Join(", ", beacon_linked?.Select((beacon) => beacon.BeaconID.ToString()).ToList() ?? new List<string>() { "N/A" })}", 3);
		}

		/// <summary>
		/// Event handler for grid closed packet
		/// If client: Closes the associated MyJumpGateConstruct indicated from server
		/// </summary>
		/// <param name="packet">The received packet</param>
		private void OnNetworkGridClose(MyNetworkInterface.Packet packet)
		{
			if (packet == null || MyNetworkInterface.IsServerLike) return;
			long gridid = packet.Payload<long>();
			this.GridMap.GetValueOrDefault(gridid, null)?.Dispose();
			Logger.Debug($"Grid '{gridid}' PURGED", 2);
		}

		/// <summary>
		/// Event handler for configuration update
		/// </summary>
		/// <param name="packet">The received packet</param>
		private void OnConfigurationUpdate(MyNetworkInterface.Packet packet)
		{
			if (packet == null || packet.PhaseFrame != 1) return;
			else if (MyNetworkInterface.IsServerLike) this.ReloadConfigurations(Configuration.Load());
			else
			{
				Configuration config = packet.Payload<Configuration>();
				if (config == null) return;
				MyJumpGateModSession.Configuration = config;
				foreach (KeyValuePair<long, MyJumpGateConstruct> pair in this.GridMap) pair.Value.ReloadConfigurations();
				MyAPIGateway.Utilities.ShowMessage(MyJumpGateModSession.DISPLAYNAME, MyTexts.GetString("Session_OnConfigReload"));
			}
		}

		/// <summary>
		/// Updates all stored grids and sends grid update packet every second
		/// </summary>
		private void TickUpdateGrids()
		{
			Stopwatch sw = new Stopwatch();
			sw.Start();

			try
			{
				foreach (KeyValuePair<long, MyJumpGateConstruct> pair in this.GridMap)
				{
					long gridid = pair.Key;
					MyJumpGateConstruct grid = pair.Value;

					if (grid.Closed)
					{
						this.GridMap.Remove(gridid);
						Logger.Log($"Grid {gridid} FULL_CLOSE");
						continue;
					}

					try
					{
						grid.Update();
					}
					catch (Exception e)
					{
						Logger.Error($"Error during construct thread tick - {grid.CubeGrid?.EntityId.ToString() ?? "N/A"} ({gridid})\n  ...\n[ {e} ]: {e.Message}\n{e.StackTrace}\n{e.InnerException}");
					}
				}

				// Force network update grid every "x" seconds
				if (MyJumpGateModSession.Network.Registered && MyNetworkInterface.IsMultiplayerServer && MyJumpGateModSession.GameTick % this.GridNetworkUpdateDelay == 0)
				{
					MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet
					{
						PacketType = MyPacketTypeEnum.UPDATE_GRIDS,
						Broadcast = true,
						TargetID = 0,
					};

					List<MySerializedJumpGateConstruct> serialized_grids = this.GridMap.Where((pair) => !pair.Value.Closed).Select((pair) => pair.Value.ToSerialized(false)).ToList();
					packet.Payload(serialized_grids);
					packet.Send();
				}
			}
			finally
			{
				sw.Stop();
				this.GridUpdateTimeTicks.Enqueue(sw.Elapsed.TotalMilliseconds);
				--this.ActiveGridUpdateThreads;
			}
		}

		/// <summary>
		/// Closes all pending grids and gates
		/// </summary>
		private void FlushClosureQueues()
		{
			KeyValuePair<MyJumpGateConstruct, bool> pair;
			while (this.GridCloseRequests.TryDequeue(out pair)) if (!pair.Key.Closed) pair.Key.Close(pair.Value);
			MyJumpGate gate;
			while (this.GateCloseRequests.TryDequeue(out gate)) if (!gate.Closed) gate.Close();
		}

		/// <summary>
		/// Deserializes and stores a MyJumpGateConstruct if applicable
		/// </summary>
		/// <param name="serialized">The serialized jump gate grid</param>
		/// <returns>The new jump gate grid or null if the source grid could not be found</returns>
		private MyJumpGateConstruct StoreSerializedGrid(MySerializedJumpGateConstruct serialized)
		{
			if (serialized == null) return null;
			long gridid = serialized.UUID.GetJumpGateGrid();
			IMyCubeGrid cube_grid = MyAPIGateway.Entities.GetEntityById(gridid) as IMyCubeGrid;
			MyJumpGateConstruct grid = new MyJumpGateConstruct(cube_grid, gridid);
			grid = this.GridMap.AddOrUpdate(gridid, grid, (_, old_grid) => {
				if (old_grid.Closed) return grid;
				grid.Release();
				return old_grid;
			});
			bool deserialized = (grid.IsSuspended && cube_grid != null) ? grid.Persist(serialized) : grid.FromSerialized(serialized);
			return (deserialized) ? grid : null;
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Reloads the configurations of all constructs in this session<br />
		/// New config will be sent to all clients<br />
		/// Does nothing if standalone multiplayer client
		/// </summary>
		public void ReloadConfigurations(Configuration config)
		{
			if (MyNetworkInterface.IsStandaloneMultiplayerClient) return;
			config.Validate();
			MyJumpGateModSession.Configuration = config;
			foreach (KeyValuePair<long, MyJumpGateConstruct> pair in this.GridMap) pair.Value.ReloadConfigurations();

			MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet {
				PacketType = MyPacketTypeEnum.UPDATE_CONFIG,
				Broadcast = true,
				TargetID = 0,
			};

			packet.Payload(config);
			packet.Send();
			Logger.Log("Global mod configuration was modified");
		}

		/// <summary>
		/// Plays an animation on this session's game thread
		/// </summary>
		/// <param name="animation">The animation to play</param>
		/// <param name="animation_type">The animation type to play</param>
		/// <param name="callback">A callback called every animation tick; Iteration will stop if this callback returns false</param>
		/// <param name="on_complete">A callback called once the animation is stopped (either from an exception, the previous parameter returning false, or the animation stopped normally)<br/>The callback will be passed the exception that caused the animation to stop, or null if no exception occured</param>
		public void PlayAnimation(MyJumpGateAnimation animation, MyJumpGateAnimation.AnimationTypeEnum animation_type, Func<bool> callback = null, Action<Exception> on_complete = null)
		{
			if (animation == null) return;
			animation.Stop();
			animation.Restart((byte) animation_type);
			AnimationInfo animation_info = new AnimationInfo(animation, animation_type, callback, on_complete);
			this.JumpGateAnimations.AddOrUpdate(this.AnimationQueueIndex++, animation_info, (_1, _2) => animation_info);
		}

		/// <summary>
		/// Stops an animation currently playing on this session's game thread
		/// </summary>
		/// <param name="animation">The animation to stop</param>
		public void StopAnimation(MyJumpGateAnimation animation)
		{
			foreach (KeyValuePair<ulong, AnimationInfo> pair in this.JumpGateAnimations) if (pair.Value.Animation == animation) pair.Value.Animation.Stop();
		}

		/// <summary>
		/// Marks this session to redraw JumpGateController terminal controls
		/// </summary>
		public void RedrawAllTerminalControls()
		{
			this.__RedrawAllTerminalControls = true;
		}

		/// <summary>
		/// Queues a grid for closure on main game thread
		/// </summary>
		/// <param name="grid">The grid to close</param>
		/// <param name="override_client">Whether to force closure on client</param>
		public void CloseGrid(MyJumpGateConstruct grid, bool override_client = false)
		{
			if (grid == null || grid.Closed || !this.GridMap.ContainsKey(grid.CubeGridID)) return;
			this.GridCloseRequests.Enqueue(new KeyValuePair<MyJumpGateConstruct, bool>(grid, override_client));
		}

		/// <summary>
		/// Queues a gate for closure on main game thread
		/// </summary>
		/// <param name="gate">The gate to close</param>
		public void CloseGate(MyJumpGate gate)
		{
			if (gate == null || gate.Closed) return;
			this.GateCloseRequests.Enqueue(gate);
		}

		/// <summary>
		/// Gets all animations playing from the specified jump gate
		/// </summary>
		/// <param name="gate">The gate to poll</param>
		/// <param name="animations">All animations playing for the specified gate<br />List will not be cleared</param>
		public void GetGateAnimationsPlaying(List<MyJumpGateAnimation> animations, MyJumpGate gate, Func<MyJumpGateConstruct, bool> filter = null)
		{
			if (gate == null) return;
			foreach (KeyValuePair<ulong, AnimationInfo> pair in this.JumpGateAnimations) if (pair.Value.Animation.JumpGate == gate) animations.Add(pair.Value.Animation);
		}

		/// <summary>
		/// Requests a download of the specified construct from server<br />
		/// Does nothing if not standalone multiplayer client
		/// </summary>
		/// <param name="grid_id">The grid ID to download</param>
		public void RequestGridDownload(long grid_id)
		{
			if (MyNetworkInterface.IsServerLike) return;

			MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet {
				PacketType = MyPacketTypeEnum.DOWNLOAD_GRID,
				TargetID = 0,
				Broadcast = false,
			};

			packet.Payload(grid_id);
			packet.Send();
		}

		/// <summary>
		/// Requests a download of all constructs from server<br />
		/// Does nothing if not standalone multiplayer client
		/// </summary>
		public void RequestGridsDownload()
		{
			if (MyNetworkInterface.IsServerLike) return;

			MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet
			{
				PacketType = MyPacketTypeEnum.UPDATE_GRIDS,
				TargetID = 0,
				Broadcast = false,
			};

			packet.Send();
		}

		/// <summary>
		/// Queues an entity warp<br />
		/// This will move the specified entity over time to the targeted position and rotation<br />
		/// Does nothing on standalone multiplayer client
		/// </summary>
		/// <param name="jump_gate">The calling jump gate</param>
		/// <param name="entity_batch">The batch of entities to move</param>
		/// <param name="source_matrix">The starting location of the batch's parent</param>
		/// <param name="dest_matrix">The ending location of the batch's parent</param>
		/// <param name="end_position">The end position to warp to<br />"dest_matrix" will be the endposition applied after completion of warp</param>
		/// <param name="time">The duration in game ticks</param>
		/// <param name="max_safe_speed">The temporary speed to clamp entities to after warp</param>
		/// <param name="callback">A callback called when the warp is complete</param>
		public void WarpEntityBatchOverTime(MyJumpGate jump_gate, List<MyEntity> entity_batch, ref MatrixD source_matrix, ref MatrixD dest_matrix, ref Vector3D end_position, ushort time, double max_safe_speed, Action<List<MyEntity>> callback = null)
		{
			if (MyNetworkInterface.IsStandaloneMultiplayerClient) return;
			this.EntityWarps.TryAdd(entity_batch[0].EntityId, new EntityWarpInfo(jump_gate, ref source_matrix, ref end_position, ref dest_matrix, entity_batch, time, max_safe_speed, callback));
		}

		/// <summary>
		/// Queues an entity warp<br />
		/// This will move the specified entity over time to the targeted position and rotation
		/// </summary>
		/// <param name="warp">The entity warp</param>
		public void WarpEntityBatchOverTime(EntityWarpInfo warp)
		{
			if (warp == null || warp.Parent == null || warp.Parent.Closed) return;
			this.EntityWarps.TryAdd(warp.Parent.EntityId, warp);
		}

		/// <summary>
		/// Gets all active entity batch warps for the specified jump gate<br />
		/// Does nothing on standalone multiplayer client
		/// </summary>
		/// <param name="jump_gate">The jump gate who's batch warps to get</param>
		/// <param name="batch_warps">A list of batch warps<br />List will not be cleared</param>
		public void GetEntityBatchWarpsForGate(MyJumpGate jump_gate, List<EntityWarpInfo> batch_warps)
		{
			if (MyNetworkInterface.IsStandaloneMultiplayerClient) return;
			foreach (KeyValuePair<long, EntityWarpInfo> pair in this.EntityWarps) if (pair.Value.JumpGate == jump_gate) batch_warps.Add(pair.Value);
		}

		/// <summary>
		/// Queues a construct for redownload after 1 second
		/// </summary>
		/// <param name="main_grid_id">The id of the construct's main grid</param>
		public void QueuePartialConstructForReload(long main_grid_id)
		{
			this.PartialSuspendedGridsQueue[main_grid_id] = 60;
		}

		/// <summary>
		/// </summary>
		/// <returns>Whether all stored grids had at least one update</returns>
		public bool AllFirstTickComplete()
		{
			foreach (KeyValuePair<long, MyJumpGateConstruct> pair in this.GridMap)
				if (!pair.Value.MarkClosed && !pair.Value.Closed && !pair.Value.IsSuspended && !pair.Value.FullyInitialized)
					return false;
			return true;
		}

		/// <summary>
		/// Checks if a construct exists with the given ID in the internal grid map
		/// </summary>
		/// <param name="grid_id">The grid's ID</param>
		/// <returns>Existedness</returns>
		public bool HasCubeGrid(long grid_id)
		{
			return this.GridMap.ContainsKey(grid_id);
		}

		/// <summary>
		/// Whether the grid should be considered valid in multiplayer context<br />
		/// For server: Returns MyJumpGateConstruct::IsValid()<br />
		/// For client: Returns whether said grid is stored in the map
		/// </summary>
		/// <param name="grid">The grid to check</param>
		/// <returns>True if this grid should be considered valid</returns>
		public bool IsJumpGateGridMultiplayerValid(MyJumpGateConstruct grid)
		{
			return grid != null && ((MyNetworkInterface.IsServerLike) ? grid.IsValid() : this.GridMap.ContainsKey(grid.CubeGridID));
		}

		/// <summary>
		/// </summary>
		/// <param name="grid">The grid construct</param>
		/// <returns>True if this grid has a duplicate construct</returns>
		public bool HasDuplicateGrid(MyJumpGateConstruct grid)
		{
			if (grid == null) return false;
			foreach (IMyCubeGrid subgrid in grid.GetCubeGrids()) if (subgrid.EntityId != grid.CubeGridID && this.GridMap.ContainsKey(subgrid.EntityId)) return true;
			return false;
		}

		/// <summary>
		/// Moves the specified construct position in the internal grid map<br />
		/// New ID must be an available spot and a subgrid ID of the specified construct
		/// </summary>
		/// <param name="grid">The unclosed construct to move</param>
		/// <param name="new_id">The subgrid ID to move to</param>
		/// <returns>Whether the grid was moved</returns>
		public bool MoveGrid(MyJumpGateConstruct grid, long new_id)
		{
			if (grid == null || grid.MarkClosed || !grid.HasCubeGrid(new_id)) return false;
			else if (grid.CubeGridID == new_id) return true;
			MyJumpGateConstruct oldgrid = this.GridMap.GetValueOrDefault(new_id, null);
			if (oldgrid != null && !oldgrid.MarkClosed) return false;
			else if (oldgrid != null && !oldgrid.Closed) this.CloseGrid(oldgrid);
			this.GridMap[new_id] = grid;
			this.GridMap.Remove(grid.CubeGridID);
			return true;
		}

		/// <summary>
		/// </summary>
		/// <returns>The average of all construct update times</returns>
		public double AverageGridUpdateTime60()
		{
			try { return (this.GridUpdateTimeTicks.Count == 0) ? -1 : this.GridUpdateTimeTicks.Average(); }
			catch (InvalidOperationException) { return -1; }
		}

		/// <summary>
		/// </summary>
		/// <returns>Returns the longest construct update time within the 60 tick buffer</returns>
		public double LocalLongestGridUpdateTime60()
		{
			try { return (this.GridUpdateTimeTicks.Count == 0) ? -1 : this.GridUpdateTimeTicks.Max(); }
			catch (InvalidOperationException) { return -1; }
		}

		/// <summary>
		/// </summary>
		/// <returns>The average of all session update times</returns>
		public double AverageSessionUpdateTime60()
		{
			try { return (this.SessionUpdateTimeTicks.Count == 0) ? -1 : this.SessionUpdateTimeTicks.Average(); }
			catch (InvalidOperationException) { return -1; }
		}

		/// <summary>
		/// </summary>
		/// <returns>Returns the longest session update time within the 60 tick buffer</returns>
		public double LocalLongestSessionUpdateTime60()
		{
			try { return (this.SessionUpdateTimeTicks.Count == 0) ? -1 : this.SessionUpdateTimeTicks.Max(); }
			catch (InvalidOperationException) { return -1; }
		}

		/// <summary>
		/// Adds a new cube grid to this session's internal map
		/// </summary>
		/// <param name="cube_grid">The grid to add</param>
		public MyJumpGateConstruct AddCubeGridToSession(IMyCubeGrid cube_grid)
		{
			MyJumpGateConstruct grid = this.GetJumpGateGrid(cube_grid);
			if (grid != null) return grid;
			grid = new MyJumpGateConstruct(cube_grid, cube_grid.EntityId);
			MyJumpGateConstruct existing = this.GridMap.GetValueOrDefault(cube_grid.EntityId, null);

			if (existing == null)
			{
				this.GridMap[cube_grid.EntityId] = grid;
				Logger.Debug($"Grid {cube_grid.EntityId} added to session", 1);
				return grid;
			}
			else if (existing.Closed)
			{
				this.GridMap[cube_grid.EntityId] = grid;
				this.CloseGrid(existing, true);
				Logger.Debug($"Grid {cube_grid.EntityId} added to session; CLOSED_EXISTING", 1);
				return grid;
			}
			else
			{
				grid.Release();
				Logger.Debug($"Grid {cube_grid.EntityId} skipped session add", 1);
				return null;
			}
		}

		/// <summary>
		/// Gets the equivilent MyJumpGateConstruct given a cube grid ID
		/// </summary>
		/// <param name="id">The cube grid ID</param>
		/// <returns>The matching MyJumpGateConstruct or null if not found</returns>
		public MyJumpGateConstruct GetJumpGateGrid(long id)
		{
			return this.GridMap.GetValueOrDefault(id, null) ?? this.GetJumpGateGrid(MyAPIGateway.Entities.GetEntityById(id) as IMyCubeGrid);
		}

		/// <summary>
		/// Gets the equivilent MyJumpGateConstruct given a cube grid
		/// </summary>
		/// <param name="cube_grid">The cube grid</param>
		/// <returns>The matching MyJumpGateConstruct or null if not found</returns>
		public MyJumpGateConstruct GetJumpGateGrid(IMyCubeGrid cube_grid)
		{
			if (cube_grid == null || cube_grid.Closed || cube_grid.MarkedForClose) return null;
			MyJumpGateConstruct jump_gate_grid;
			if (this.GridMap.TryGetValue(cube_grid.EntityId, out jump_gate_grid)) return jump_gate_grid;
			List<IMyCubeGrid> connected_grids = new List<IMyCubeGrid>();
			cube_grid.GetGridGroup(GridLinkTypeEnum.Logical).GetGrids(connected_grids);

			foreach (IMyCubeGrid grid in connected_grids)
			{
				if (this.GridMap.TryGetValue(grid.EntityId, out jump_gate_grid)) return jump_gate_grid;
			}

			return null;
		}

		/// <summary>
		/// Gets the equivilent MyJumpGateConstruct given a JumpGateUUID
		/// </summary>
		/// <param name="uuid">The cube grid UUID</param>
		/// <returns>The matching MyJumpGateConstruct or null if not found</returns>
		public MyJumpGateConstruct GetJumpGateGrid(JumpGateUUID uuid)
		{
			if (uuid == null) return null;
			return this.GetJumpGateGrid(uuid.GetJumpGateGrid());
		}

		/// <summary>
		/// Gets the equivilent MyJumpGateConstruct given a cube grid ID
		/// </summary>
		/// <param name="id">The cube grid ID</param>
		/// <returns>The matching MyJumpGateConstruct or null if not found or marked closed</returns>
		public MyJumpGateConstruct GetUnclosedJumpGateGrid(long id)
		{
			MyJumpGateConstruct jump_gate_grid = this.GridMap.GetValueOrDefault(id, null);
			if (jump_gate_grid != null && !jump_gate_grid.MarkClosed) return jump_gate_grid;
			return this.GetUnclosedJumpGateGrid(MyAPIGateway.Entities.GetEntityById(id) as IMyCubeGrid);
		}

		/// <summary>
		/// Gets the equivilent MyJumpGateConstruct given a cube grid
		/// </summary>
		/// <param name="cube_grid">The cube grid</param>
		/// <returns>The matching MyJumpGateConstruct or null if not found or marked closed</returns>
		public MyJumpGateConstruct GetUnclosedJumpGateGrid(IMyCubeGrid cube_grid)
		{
			if (cube_grid == null || cube_grid.Closed || cube_grid.MarkedForClose) return null;
			MyJumpGateConstruct jump_gate_grid;
			if (this.GridMap.TryGetValue(cube_grid.EntityId, out jump_gate_grid) && !jump_gate_grid.MarkClosed) return jump_gate_grid;
			List<IMyCubeGrid> connected_grids = new List<IMyCubeGrid>();
			cube_grid.GetGridGroup(GridLinkTypeEnum.Logical).GetGrids(connected_grids);
			foreach (IMyCubeGrid grid in connected_grids) if (this.GridMap.TryGetValue(grid.EntityId, out jump_gate_grid) && !jump_gate_grid.MarkClosed) return jump_gate_grid;
			return null;
		}

		/// <summary>
		/// Gets the equivilent MyJumpGateConstruct given a JumpGateUUID
		/// </summary>
		/// <param name="uuid">The cube grid UUID</param>
		/// <returns>The matching MyJumpGateConstruct or null if not found or marked closed</returns>
		public MyJumpGateConstruct GetUnclosedJumpGateGrid(JumpGateUUID uuid)
		{
			if (uuid == null) return null;
			return this.GetUnclosedJumpGateGrid(uuid.GetJumpGateGrid());
		}

		/// <summary>
		/// Gets the equivilent MyJumpGateConstruct given a cube grid ID<br />
		/// This will not check subgrids
		/// </summary>
		/// <param name="id">The cube grid ID</param>
		/// <returns>The matching MyJumpGateConstruct or null if not found</returns>
		public MyJumpGateConstruct GetDirectJumpGateGrid(long id)
		{
			return this.GridMap.GetValueOrDefault(id, null);
		}

		/// <summary>
		/// Gets the equivilent MyJumpGateConstruct given a cube grid<br />
		/// This will not check subgrids
		/// </summary>
		/// <param name="cube_grid">The cube grid</param>
		/// <returns>The matching MyJumpGateConstruct or null if not found</returns>
		public MyJumpGateConstruct GetDirectJumpGateGrid(IMyCubeGrid cube_grid)
		{
			return (cube_grid == null || cube_grid.Closed || cube_grid.MarkedForClose) ? null : this.GridMap.GetValueOrDefault(cube_grid.EntityId, null);
		}

		/// <summary>
		/// Gets the equivilent MyJumpGateConstruct given a JumpGateUUID<br />
		/// This will not check subgrids
		/// </summary>
		/// <param name="uuid">The cube grid UUID</param>
		/// <returns>The matching MyJumpGateConstruct or null if not found</returns>
		public MyJumpGateConstruct GetDirectJumpGateGrid(JumpGateUUID uuid)
		{
			if (uuid == null) return null;
			return this.GetDirectJumpGateGrid(uuid.GetJumpGateGrid());
		}

		/// <summary>
		/// Gets the equivilent MyJumpGate given a JumpGateUUID
		/// </summary>
		/// <param name="uuid">The jump gate's JumpGateUUID</param>
		/// <returns>The matching MyJumpGate or null if not found</returns>
		public MyJumpGate GetJumpGate(JumpGateUUID uuid)
		{
			if (uuid == null) return null;
			MyJumpGateConstruct grid = this.GetJumpGateGrid(uuid);
			return grid?.GetJumpGate(uuid.GetJumpGate());
		}

		/// <summary>
		/// Gets all valid jump gates stored by all grids in this session
		/// </summary>
		public IEnumerable<MyJumpGate> GetAllJumpGates()
		{
			IEnumerable<MyJumpGate> all_jump_gates = Enumerable.Empty<MyJumpGate>();

			foreach (KeyValuePair<long, MyJumpGateConstruct> pair in this.GridMap)
			{
				if (!this.IsJumpGateGridMultiplayerValid(pair.Value)) continue;
				all_jump_gates = all_jump_gates.Concat(pair.Value.GetJumpGates().Where((gate) => gate.IsValid()));
			}

			return all_jump_gates;
		}

		/// <summary>
		/// Gets all valid jump gate grids stored in this session
		/// </summary>
		/// <param name="filter">A predicate to match jump gate grids against</param>
		/// <param name="grids">All jump gate grid constructs<br />List will be cleared</param>
		public IEnumerable<MyJumpGateConstruct> GetAllJumpGateGrids()
		{
			return this.GridMap?.Select((pair) => pair.Value).Where(this.IsJumpGateGridMultiplayerValid) ?? Enumerable.Empty<MyJumpGateConstruct>();
		}

		/// <summary>
		/// Gets a block from its UUID
		/// </summary>
		/// <typeparam name="T">The block type to get</typeparam>
		/// <param name="uuid">The block's UUID</param>
		/// <returns>The cube block component or null</returns>
		public T GetJumpGateBlock<T>(JumpGateUUID uuid) where T : MyCubeBlockBase
		{
			if (uuid == null) return null;
			MyJumpGateConstruct construct = this.GetJumpGateGrid(uuid);
			MyCubeBlockBase block = construct?.GetCubeBlock(uuid.GetBlock());
			return (block is T) ? (T) block : null;
		}

		/// <summary>
		/// Gets a block from its UUID
		/// </summary>
		/// <typeparam name="T">The block type to get</typeparam>
		/// <param name="block_id">The block's entity ID</param>
		/// <returns>The cube block component or null</returns>
		public T GetJumpGateBlock<T>(long block_id) where T : MyCubeBlockBase
		{
			if (block_id <= 0) return null;
			IMyEntity entity = MyAPIGateway.Entities.GetEntityById(block_id);
			return (entity == null || !(entity is IMyTerminalBlock)) ? null : entity.GameLogic?.GetAs<T>();
		}
		#endregion
	}
}
