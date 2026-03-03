using IOTA.ModularJumpGates.API;
using IOTA.ModularJumpGates.Commands;
using IOTA.ModularJumpGates.CubeBlock;
using IOTA.ModularJumpGates.EventController.EventComponents;
using IOTA.ModularJumpGates.ISC;
using IOTA.ModularJumpGates.Terminal;
using IOTA.ModularJumpGates.Util;
using IOTA.ModularJumpGates.Util.ConcurrentCollections;
using ProtoBuf;
using Sandbox.Engine.Utils;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
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
		public sealed class MyMaterialsHolder
		{
			public readonly MyStringId WeaponLaser = MyStringId.GetOrCompute("WeaponLaser");
			public readonly MyStringId GizmoDrawLine = MyStringId.GetOrCompute("GizmoDrawLine");
			public readonly MyStringId EnabledEntityMarker = MyStringId.GetOrCompute("IOTA.JumpGateControllerIcon.EntityMarker");
			public readonly MyStringId DisabledEntityMarker = MyStringId.GetOrCompute("IOTA.JumpGateControllerIcon.EntityMarker");
			public readonly MyStringId GateOfflineControllerIcon = MyStringId.GetOrCompute("IOTA.JumpGateControllerIcon.GateOffline");
			public readonly MyStringId GateDisconnectedControllerIcon = MyStringId.GetOrCompute("IOTA.JumpGateControllerIcon.NoGateConnected");
			public readonly MyStringId GateAntennaDisconnectedControllerIcon = MyStringId.GetOrCompute("IOTA.JumpGateControllerIcon.NoAntennaConnection");
			public readonly MyStringId SpacialMarkerJumpNode = MyStringId.GetOrCompute("IOTA.HUD.SpacialMarker.JumpNode");
			public readonly MyStringId SpacialMarkerJumpPoint = MyStringId.GetOrCompute("IOTA.HUD.SpacialMarker.JumpPoint");
			public readonly MyStringId SpacialMarkerDamagePoint = MyStringId.GetOrCompute("IOTA.HUD.SpacialMarker.DamagePoint");
			public readonly MyStringId SpacialMarkerDeletePoint = MyStringId.GetOrCompute("IOTA.HUD.SpacialMarker.DeletePoint");
			public readonly MyStringId WhiteDot = MyStringId.GetOrCompute("WhiteDot");
		}

		public sealed class MyThreadedUpdateInfo
		{
			private readonly object ReadBufferLock = new object();
			private readonly object WriteBufferLock = new object();
			private List<long> GridBuffer1 = new List<long>();
			private List<long> GridBuffer2 = new List<long>();
			private List<long> ReadBuffer;
			private List<long> WriteBuffer;

			public bool IsFinished = true;

			public MyThreadedUpdateInfo()
			{
				this.ReadBuffer = this.GridBuffer1;
				this.WriteBuffer = this.GridBuffer2;
			}

			public void Dispose()
			{
				lock (this.ReadBufferLock)
				{
					lock (this.WriteBufferLock)
					{
						this.IsFinished = true;
						this.GridBuffer1?.Clear();
						this.GridBuffer2?.Clear();
						this.GridBuffer1 = null;
						this.GridBuffer2 = null;
						this.ReadBuffer = null;
						this.WriteBuffer = null;
					}
				}
			}

			public void Swap()
			{
				lock (this.ReadBufferLock)
				{
					lock (this.WriteBufferLock)
					{
						List<long> temp = this.ReadBuffer;
						this.ReadBuffer = this.WriteBuffer;
						this.WriteBuffer = temp;
					}
				}
			}

			public void EnqueueID(long id)
			{
				lock (this.WriteBufferLock) this.WriteBuffer?.Add(id);
			}

			public IEnumerable<long> ReadEnqueuedIDs()
			{
				lock (this.ReadBufferLock)
				{
					if (this.ReadBuffer != null)
					{
						foreach (long id in this.ReadBuffer) yield return id;
						this.ReadBuffer.Clear();
					}
				}
			}
		}

		public sealed class MyExternalModInfo
		{
			public readonly bool RealSolarSystemsEnabled;
			public readonly ImmutableHashSet<ulong> LoadedModIDs;

			public MyExternalModInfo(IMyModContext context)
			{
				this.LoadedModIDs = MyAPIGateway.Session.Mods.Select((mod) => mod.GetWorkshopId().Id).ToImmutableHashSet();
				this.RealSolarSystemsEnabled = this.LoadedModIDs.Contains(3351055036);
			}
		}

		[ProtoContract]
		public sealed class MyModLogFileInfo
		{
			[ProtoMember(1)]
			public readonly string Filename;
			[ProtoMember(2)]
			public long ModificationTime;

			public MyModLogFileInfo() { }
			public MyModLogFileInfo(string filename)
			{
				this.Filename = filename;
				this.ModificationTime = DateTime.UtcNow.Ticks;
			}

			public void UpdateTime()
			{
				this.ModificationTime = DateTime.UtcNow.Ticks;
			}
		}

		[ProtoContract]
		public sealed class MyModDataInfo
		{
			[ProtoMember(1)]
			public Vector3I ModVersion;

			[ProtoMember(2)]
			public uint MaxModSpecificLogFiles;

			[ProtoMember(3)]
			public List<MyModLogFileInfo> ModLogFiles;
		}

		[ProtoContract]
		public sealed class MyCockpitInfo : IEquatable<MyCockpitInfo>
		{
			[ProtoMember(1)]
			public long CockpitId;

			[ProtoMember(2)]
			public bool DisplayHudMarkers = true;

			[ProtoMember(3)]
			public bool DisplayForAttachedGates = false;

			[ProtoMember(4)]
			public long LastUpdateTimeEpoch;

			public IMyCockpit Cockpit => MyAPIGateway.Entities.GetEntityById(this.CockpitId) as IMyCockpit;

			public DateTime LastUpdateTime
			{
				get { return new DateTime(this.LastUpdateTimeEpoch); }
				set { this.LastUpdateTimeEpoch = value.Ticks; }
			}

			public static bool operator== (MyCockpitInfo a, MyCockpitInfo b)
			{
				return object.ReferenceEquals(a, b)
					|| (!object.ReferenceEquals(a, null) && !object.ReferenceEquals(b, null) && a.CockpitId == b.CockpitId && a.DisplayHudMarkers == b.DisplayHudMarkers && a.DisplayForAttachedGates == b.DisplayForAttachedGates);
			}

			public static bool operator!= (MyCockpitInfo a, MyCockpitInfo b)
			{
				return !(a == b);
			}

			public MyCockpitInfo() { }

			public MyCockpitInfo(IMyCockpit cockpit)
			{
				this.CockpitId = cockpit.EntityId;
			}

			public bool IsDefault()
			{
				return this.SettingsEqual(new MyCockpitInfo());
			}

			public bool SettingsEqual(MyCockpitInfo other)
			{
				return other != null && other.DisplayHudMarkers == this.DisplayHudMarkers && other.DisplayForAttachedGates == this.DisplayForAttachedGates;
			}

			public bool Equals(MyCockpitInfo other)
			{
				return this == other;
			}

			public override bool Equals(object obj)
			{
				return this == (obj as MyCockpitInfo);
			}

			public override int GetHashCode()
			{
				return this.CockpitId.GetHashCode();
			}
		}

		#region Public Static Variables
		/// <summary>
		/// The maximum jump gate collider radius allowed based on config values
		/// </summary>
		public static double MaxJumpGateColliderRadius => 2 * Math.Max(MyJumpGateModSession.Configuration.DriveConfiguration.LargeDriveRaycastDistance, MyJumpGateModSession.Configuration.DriveConfiguration.SmallDriveRaycastDistance);

		/// <summary>
		/// The current mod version (major, minor, patch)
		/// </summary>
		public static Vector3I ModVersion => new Vector3I(1, 3, 1);

		/// <summary>
		/// The Guid used to store information in mod storage components
		/// </summary>
		public static Guid BlockComponentDataGUID => new Guid("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF");

		/// <summary>
		/// The world's world matrix
		/// </summary>
		public static MatrixD WorldMatrix => MatrixD.CreateWorld(Vector3D.Zero, new Vector3D(0, 0, -1), new Vector3D(0, 1, 0));

		/// <summary>
		/// The Mod ID string used for terminal controls
		/// </summary>
		public static string MODID { get; private set; } = "IOTA.ModularJumpGatesMod";

		/// <summary>
		/// The display name used for chat messages
		/// </summary>
		public static string DISPLAYNAME { get; private set; } = "[ IMJG ]";

		/// <summary>
		/// The materials holder for common materials
		/// </summary>
		public static MyMaterialsHolder MATERIALS { get; private set; } = new MyMaterialsHolder();

		/// <summary>
		/// External mod information
		/// </summary>
		public static MyExternalModInfo MODSLIST { get; private set; } = null;

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
		private static ushort InitialEntityLoadedDelayTicks => 300;
		#endregion

		#region Private Variables
		/// <summary>
		/// Whether to force a redraw of all terminal controls
		/// </summary>
		private bool __RedrawAllTerminalControls = false;

		/// <summary>
		/// The time in game ticks after session start to update grids
		/// </summary>
		private readonly ushort FirstUpdateTimeTicks = 300;

		/// <summary>
		/// The time in game ticks after which all grids are sent from server to all clients
		/// </summary>
		private readonly ushort GridNetworkUpdateDelay = 18000;

		/// <summary>
		/// The gameplay frame counter's value 1 tick ago
		/// </summary>
		private int LastGameplayFrameCounter = 0;

		/// <summary>
		/// The fallback maximum number of mod-specific log files that can be stored
		/// </summary>
		private uint FallbackMaxModSpecificLogFiles = 0;

		/// <summary>
		/// Stores the next index for queued animations
		/// </summary>
		private ulong AnimationQueueIndex = 0;

		/// <summary>
		/// Stores the active terminal block
		/// </summary>
		private long InteractedBlock = -1;

		/// <summary>
		/// The variable name to access the mod log file list
		/// </summary>
		private string ModLogSettingsFile = "ModLogStorage.dat";

		/// <summary>
		/// The variable name to access the mod log file list
		/// </summary>
		private string ModDataFile = "ModData.dat";

		/// <summary>
		/// Stores the last opened terminal page
		/// </summary>
		private MyTerminalPageEnum LastTerminalPage = MyTerminalPageEnum.None;

		/// <summary>
		/// The error that occured loading the mod log file list
		/// </summary>
		private ModFileLoadingException ModDataLoadError = null;

		/// <summary>
		/// Mod data as loaded from file
		/// </summary>
		private MyModDataInfo ModData = null;

		/// <summary>
		/// Master map of threaded construct update queues
		/// </summary>
		private List<MyThreadedUpdateInfo> GridUpdateThreads = new List<MyThreadedUpdateInfo>();

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
		/// Used to store all constructs awaiting session add
		/// </summary>
		private ConcurrentDictionary<IMyCubeGrid, Action<MyJumpGateConstruct>> GridAddRequests = new ConcurrentDictionary<IMyCubeGrid, Action<MyJumpGateConstruct>>();

		/// <summary>
		/// Used to store all constructs that are partially loaded
		/// </summary>
		private ConcurrentDictionary<long, byte> PartialSuspendedGridsQueue = new ConcurrentDictionary<long, byte>();

		/// <summary>
		/// Master map for storing grid constructs
		/// </summary>
		private ConcurrentDictionary<long, MyJumpGateConstruct> GridMap = new ConcurrentDictionary<long, MyJumpGateConstruct>();

		/// <summary>
		/// Master map for storing grid construct reinitialization counts
		/// </summary>
		private ConcurrentDictionary<long, byte> GridReinitCounts = new ConcurrentDictionary<long, byte>();

		/// <summary>
		/// Master map for storing in-progress animations
		/// </summary>
		private ConcurrentDictionary<ulong, AnimationInfo> JumpGateAnimations = new ConcurrentDictionary<ulong, AnimationInfo>();

		/// <summary>
		/// Master map for storing entity warps
		/// </summary>
		private ConcurrentDictionary<long, EntityWarpInfo> EntityWarps = new ConcurrentDictionary<long, EntityWarpInfo>();

		/// <summary>
		/// Master map for storing gate detonations
		/// </summary>
		private ConcurrentDictionary<JumpGateUUID, GateDetonationInfo> GateDetonations = new ConcurrentDictionary<JumpGateUUID, GateDetonationInfo>();

		/// <summary>
		/// Master map for storing cockpit terminal settings
		/// </summary>
		private ConcurrentDictionary<IMyCockpit, MyCockpitInfo> CockpitBlockSettings = new ConcurrentDictionary<IMyCockpit, MyCockpitInfo>();
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
		/// The current, session local game tick
		/// </summary>
		public ulong GameTick { get; private set; } = 0;

		/// <summary>
		/// Whether to show debug draw
		/// </summary>
		public bool DebugMode = false;

		/// <summary>
		/// The maximum number of mod-specific log files that can be stored
		/// </summary>
		public uint MaxModSpecificLogFiles => MyJumpGateModSession.Configuration?.GeneralConfiguration?.MaxStoredModSpecificLogFiles ?? this.FallbackMaxModSpecificLogFiles;

		/// <summary>
		/// The current number of mod-specific log files
		/// </summary>
		public int ModSpecificLogFileCount => this.ModData?.ModLogFiles?.Count ?? 0;

		/// <summary>
		/// The current session component status
		/// </summary>
		public MySessionStatusEnum SessionStatus { get; private set; } = MySessionStatusEnum.OFFLINE;

		/// <summary>
		/// Interface for handling API requests
		/// </summary>
		public MyAPIInterface ModAPIInterface { get; private set; } = new MyAPIInterface();

		/// <summary>
		/// The currently active mod-specific log file name
		/// </summary>
		public string ActiveModLogFile { get; private set; }
		#endregion

		#region Public Static Methods
		public static void DrawTransparentLine(Vector3D start, Vector3D end, MyStringId? material, ref Vector4 color, float thickness, MyBillboard.BlendTypeEnum blendtype = MyBillboard.BlendTypeEnum.Standard)
		{
			Vector3D dir = end - start;
			float len = (float) dir.Length();
			MyTransparentGeometry.AddLineBillboard(material ?? MyJumpGateModSession.MATERIALS.GizmoDrawLine, color, start, dir.Normalized(), len, thickness, blendtype);
		}

		/// <summary>
		/// Teleports a cube grid applying both position and rotation to parent and children
		/// </summary>
		/// <param name="parent">The grid to teleport</param>
		/// <param name="world_matrix">The world matrix to teleport to</param>
		public static void TeleportCubeGrid(MyCubeGrid parent, ref MatrixD world_matrix)
		{
			if (parent == null) return;
			parent.Teleport(world_matrix, parent.Parent);
			MatrixD parent_matrix = MatrixD.Invert(parent.WorldMatrix);
			parent.PositionComp.SetWorldMatrix(ref world_matrix, parent.Parent, true, true, true, true, true);
			List<IMyCubeGrid> children = new List<IMyCubeGrid>();
			parent.GetGridGroup(GridLinkTypeEnum.Logical).GetGrids(children);

			foreach (IMyCubeGrid child in children)
			{
				if (child == parent) continue;
				MatrixD child_matrix = (child.WorldMatrix * parent_matrix) * parent.WorldMatrix;
				child.PositionComp?.SetWorldMatrix(ref child_matrix, child.Parent, true, true, true, true, true);
			}
		}

		/// <summary>
		/// Executes a callback on all elements of an enumerable in parallel<br />
		/// Blocks until all threads are complete
		/// </summary>
		/// <typeparam name="T">The collection type</typeparam>
		/// <param name="enumerable">The collection enumerate</param>
		/// <param name="action">The callback to execute</param>
		public static void ParallelFor<T>(IEnumerable<T> enumerable, Action<T> action)
		{
			if (enumerable == null) return;
			int count = 0;
			object mutex = new object();

			foreach (T element in enumerable)
			{
				lock (mutex) ++count;
				MyAPIGateway.Parallel.Start(() => {
					try
					{
						action(element);
					}
					finally
					{
						lock (mutex) --count;
					}
				});
			}

			while (count > 0) MyAPIGateway.Parallel.Sleep(new TimeSpan(10));
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
			value /= Math.Pow(10, l10);
			return $"{sign}{Math.Round(value, places)}{e}{l10}";
		}

		/// <summary>
		/// Converts a value to time units
		/// </summary>
		/// <param name="seconds">The seconds to convert</param>
		/// <param name="places">The number of places to round to</param>
		/// <returns>The resulting value in time notation</returns>
		public static string AutoconvertTimeUnits(double seconds, int places)
		{
			if (double.IsInfinity(seconds)) return "INF";
			else if (double.IsNaN(seconds)) return "NaN";
			seconds = Math.Round(seconds, places + 2);
			if (seconds == 1) return $"1 second";
			else if (seconds < 60) return $"{Math.Round(seconds, places)} seconds";
			else if (seconds == 60) return $"1 minute";
			else if (seconds < 3600) return $"{Math.Round(seconds / 60, places)} minutes";
			else if (seconds == 3600) return $"1 hour";
			else if (seconds < 86400) return $"{Math.Round(seconds / 3600, places)} hours";
			else if (seconds == 86400) return $"1 day";
			else if (seconds < 604800) return $"{Math.Round(seconds / 86400, places)} days";
			else if (seconds == 604800) return $"1 week";
			else return $"{Math.Round(seconds / 604800, places)} weeks";
		}

		/// <summary>
		/// Converts a value to HH:MM:SS format
		/// </summary>
		/// <param name="total_seconds">The total seconds to convert</param>
		/// <returns>The time string</returns>
		public static string AutoconvertTimeHHMMSS(double total_seconds)
		{
			if (double.IsInfinity(total_seconds)) return "--:--:--";
			else if (double.IsNaN(total_seconds) || total_seconds <= 0) return "00:00:00";
			uint hours = (uint) (total_seconds / 3600);
			uint minutes = (uint) (total_seconds % 3600 / 60d);
			uint seconds = (uint) (total_seconds % 60d);
			return $"{hours:00}:{minutes:00}:{seconds:00}";
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
			this.SessionStatus = MySessionStatusEnum.UNLOADING;
			Logger.Log("Closing...");
			base.UnloadData();
			this.ModAPIInterface.Close();
			this.UpdateSaveModDataFile();
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
				MyJumpGateModSession.Network.Off(MyPacketTypeEnum.MARK_GATES_DIRTY, this.OnNetworkGridGateReconstruct);
				MyJumpGateModSession.Network.Off(MyPacketTypeEnum.CONSTRUCT_UPDATE_NOTICE, this.OnNetworkConstructUpdateNotice);
				MyJumpGateModSession.Network.Off(MyPacketTypeEnum.GATE_DETONATION, this.OnNetworkJumpGateDetonation);
				MyJumpGateModSession.Network.Off(MyPacketTypeEnum.VERSION_CHECK, this.OnNetworkVersionCheck);
				MyJumpGateModSession.Network.Off(MyPacketTypeEnum.UPDATE_COCKPIT, this.OnNetworkCockpitSettingsUpdate);
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
			foreach (MyThreadedUpdateInfo info in this.GridUpdateThreads) info.Dispose();

			this.GridUpdateTimeTicks.Clear();
			this.SessionUpdateTimeTicks.Clear();
			this.GridMap.Clear();
			this.GridReinitCounts.Clear();
			this.JumpGateAnimations.Clear();
			this.EntityWarps.Clear();
			this.GateDetonations.Clear();
			this.PartialSuspendedGridsQueue.Clear();
			this.GridAddRequests.Clear();
			this.GridUpdateThreads.Clear();
			this.CockpitBlockSettings.Clear();

			this.ModAPIInterface = null;
			this.GridUpdateTimeTicks = null;
			this.SessionUpdateTimeTicks = null;
			this.GridCloseRequests = null;
			this.GateCloseRequests = null;
			this.GridMap = null;
			this.GridReinitCounts = null;
			this.JumpGateAnimations = null;
			this.EntityWarps = null;
			this.GateDetonations = null;
			this.PartialSuspendedGridsQueue = null;
			this.GridAddRequests = null;
			this.GridUpdateThreads = null;
			this.CockpitBlockSettings = null;
			this.ActiveModLogFile = null;

			MyAPIGateway.Entities.OnEntityAdd -= this.OnEntityAdd;
			MyAPIGateway.Entities.OnEntityRemove -= this.OnEntityRemove;
			MyAPIGateway.TerminalControls.CustomControlGetter -= this.OnTerminalSelector;

			MyCubeBlockTerminal.Unload();
			MyCockpitTerminal.Unload();
			MyJumpGateCapacitorTerminal.Unload();
			MyJumpGateControllerTerminal.Unload();
			MyJumpGateDriveTerminal.Unload();
			MyJumpGateRemoteAntennaTerminal.Unload();
			MyJumpGateRemoteLinkTerminal.Unload();

			JumpGatePhaseChangedEvent.Dispose();
			JumpGateStatusChangedEvent.Dispose();
			MyCubeBlockBase.DisposeAll();
			Particle.Dispose();

			MyJumpGateModSession.Configuration.Save();
			MyJumpGateModSession.Configuration = null;
			MyJumpGateModSession.Network = null;
			MyJumpGateModSession.MATERIALS = null;
			MyJumpGateModSession.MODSLIST = null;

			this.SessionStatus = MySessionStatusEnum.OFFLINE;

			Logger.Log("Closed.");
			Logger.Flush();
			Logger.Close();

			MyJumpGateModSession.MODID = null;
			MyJumpGateModSession.DISPLAYNAME = null;
			MyJumpGateModSession.Instance = null;
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
			bool log_decode_success;
			bool is_old;

			if (is_old = MyAPIGateway.Utilities.FileExistsInLocalStorage(this.ModLogSettingsFile, this.GetType()))
			{
				log_decode_success = this.UpdateLoadLogFileList();
				this.UpdateSaveModDataFile();
				MyAPIGateway.Utilities.DeleteFileInLocalStorage(this.ModLogSettingsFile, this.GetType());
			}
			else
			{
				log_decode_success = this.UpdateLoadModDataFile();
			}

			int log_file_count = this.ModData.ModLogFiles.Count;
			Logger.Init();
			Vector3I version = MyJumpGateModSession.ModVersion;
			Logger.Log($"Mod Version: {version.X}.{version.Y}.{version.Z}");
			Logger.Log("PREINIT - Loading Data...");
			this.SessionStatus = MySessionStatusEnum.LOADING;
			MyJumpGateModSession.Configuration = Configuration.Load();
			MyJumpGateModSession.Network = new MyNetworkInterface(0xFFFF, this.ModContext.ModId);
			if (MyNetworkInterface.IsServerLike && !MyAPIGateway.Utilities.GetVariable($"{MyJumpGateModSession.MODID}.DebugMode", out this.DebugMode)) this.DebugMode = false;
			this.UpdateOnPause = true;
			this.PurgeStoredLogFiles(this.MaxModSpecificLogFiles);
			if (is_old) Logger.Log("Upgraded old mod-specific log file data to newer mod data file");
			if (log_decode_success) Logger.Log($"Mod data loaded from local storage; Version={this.ModData.ModVersion.X}.{this.ModData.ModVersion.Y}.{this.ModData.ModVersion.Z}, Log Capacity={log_file_count}/{this.MaxModSpecificLogFiles}");
			else Logger.Warn("Failed to load mod-specific log file list. System cannot auto-delete extra log files");

			if (!MyNetworkInterface.IsSingleplayer)
			{
				MyJumpGateModSession.Network.Register();
				MyJumpGateModSession.Network.On(MyPacketTypeEnum.UPDATE_GRIDS, this.OnNetworkGridsUpdate);
				MyJumpGateModSession.Network.On(MyPacketTypeEnum.CLOSE_GRID, this.OnNetworkGridClose);
				MyJumpGateModSession.Network.On(MyPacketTypeEnum.DOWNLOAD_GRID, this.OnNetworkGridDownload);
				MyJumpGateModSession.Network.On(MyPacketTypeEnum.UPDATE_CONFIG, this.OnConfigurationUpdate);
				MyJumpGateModSession.Network.On(MyPacketTypeEnum.CONSTRUCT_UPDATE_NOTICE, this.OnNetworkConstructUpdateNotice);
				MyJumpGateModSession.Network.On(MyPacketTypeEnum.GATE_DETONATION, this.OnNetworkJumpGateDetonation);
				MyJumpGateModSession.Network.On(MyPacketTypeEnum.VERSION_CHECK, this.OnNetworkVersionCheck);
				MyJumpGateModSession.Network.On(MyPacketTypeEnum.UPDATE_COCKPIT, this.OnNetworkCockpitSettingsUpdate);
			}

			if (MyNetworkInterface.IsMultiplayerServer)
			{
				MyJumpGateModSession.Network.On(MyPacketTypeEnum.COMM_LINKED, this.OnNetworkCommLinkedUpdate);
				MyJumpGateModSession.Network.On(MyPacketTypeEnum.BEACON_LINKED, this.OnNetworkBeaconLinkedUpdate);
				MyJumpGateModSession.Network.On(MyPacketTypeEnum.MARK_GATES_DIRTY, this.OnNetworkGridGateReconstruct);
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
			MyJumpGateModSession.MODSLIST = new MyExternalModInfo(this.ModContext);
			if (MyJumpGateModSession.Network.Registered && MyNetworkInterface.IsStandaloneMultiplayerClient) this.RequestGridsDownload();
			if (!MyNetworkInterface.IsDedicatedMultiplayerServer) MyAPIGateway.Utilities.ShowMessage(MyJumpGateModSession.DISPLAYNAME, "Initializing Constructs...");
			MyAPIGateway.TerminalControls.CustomControlGetter += this.OnTerminalSelector;
			MyChatCommandHandler.Init();
			Logger.Log("INIT - Loaded.");

			// Display startup messages
			TextReader file_reader;
			string filename;
			string content;
			MyObjectBuilder_Checkpoint.ModItem moditem = this.ModContext.ModItem;
			Vector3I current_version = MyJumpGateModSession.ModVersion;

			if (MyAPIGateway.Session.IsUserAdmin(MyAPIGateway.Multiplayer.MyId))
			{
				filename = "Data/StartupMessage/Admin.txt";
				file_reader = (MyAPIGateway.Utilities.FileExistsInModLocation(filename, moditem)) ? MyAPIGateway.Utilities.ReadFileInModLocation(filename, moditem) : null;
				content = file_reader?.ReadToEnd();
				if (!string.IsNullOrEmpty(content)) MyAPIGateway.Utilities.ShowMessage(MyJumpGateModSession.DISPLAYNAME, content);
			}

			filename = "Data/StartupMessage/General.txt";
			file_reader = (MyAPIGateway.Utilities.FileExistsInModLocation(filename, moditem)) ? MyAPIGateway.Utilities.ReadFileInModLocation(filename, moditem) : null;
			content = file_reader?.ReadToEnd();
			if (!string.IsNullOrEmpty(content)) MyAPIGateway.Utilities.ShowMessage(MyJumpGateModSession.DISPLAYNAME, content);

			if (current_version.X > this.ModData.ModVersion.X
				|| (current_version.X == this.ModData.ModVersion.X && current_version.Y > this.ModData.ModVersion.Y)
				|| (current_version.X == this.ModData.ModVersion.X && current_version.Y == this.ModData.ModVersion.Y && current_version.Z > this.ModData.ModVersion.Z))
			{
				this.ModData.ModVersion = current_version;
				MyAPIGateway.Utilities.ShowMessage(MyJumpGateModSession.DISPLAYNAME, $"Mod updated to version {current_version.X}.{current_version.Y}.{current_version.Z}");
				filename = "Data/StartupMessage/Update.txt";
				file_reader = (MyAPIGateway.Utilities.FileExistsInModLocation(filename, moditem)) ? MyAPIGateway.Utilities.ReadFileInModLocation(filename, moditem) : null;
				content = file_reader?.ReadToEnd();
				if (!string.IsNullOrEmpty(content)) MyAPIGateway.Utilities.ShowMessage(MyJumpGateModSession.DISPLAYNAME, content);
			}

			if (this.ModDataLoadError != null)
			{
				Logger.Error($"Error whilst reading mod-log file list\n  ...\n[ {this.ModDataLoadError.GetType().Name} ]: {this.ModDataLoadError.Message}\n{this.ModDataLoadError.StackTrace}\n{this.ModDataLoadError.InnerException}");
				MyAPIGateway.Utilities.ShowMessage(MyJumpGateModSession.DISPLAYNAME, "An error occured loading the mod-log list.\nExisting mod log files may not be deleted automatically.\nCheck the log for more details");
				this.ModDataLoadError = null;
			}

			if (MyNetworkInterface.IsStandaloneMultiplayerClient)
			{
				MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet {
					PacketType = MyPacketTypeEnum.VERSION_CHECK,
					TargetID = 0,
					Broadcast = false,
				};
				packet.Send();
			}
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
				this.FlushAdditionQueues();
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
				this.SessionStatus = MySessionStatusEnum.RUNNING;
				++this.GameTick;
				this.GameTick = (this.GameTick >= 0xFFFFFFFFFFFFFFF0) ? 0 : this.GameTick;
				this.EntityLoadedDelayTicks -= (this.EntityLoadedDelayTicks > 0) ? (ushort) 1 : (ushort) 0;
				
				// Update grids
				if (MyNetworkInterface.IsStandaloneMultiplayerClient && this.GameTick == this.FirstUpdateTimeTicks)
				{
					this.RequestGridsDownload();
					Logger.Debug($"INITAL_GRID_UPDATE", 2);
				}
				
				// Check initialization
				if (MyNetworkInterface.IsServerLike && !this.AllSessionEntitiesLoaded) return;

				if (!this.InitializationComplete && ((MyNetworkInterface.IsServerLike && this.AllFirstTickComplete()) || (MyNetworkInterface.IsStandaloneMultiplayerClient && this.GameTick == this.FirstUpdateTimeTicks)))
				{
					if (MyNetworkInterface.IsStandaloneMultiplayerClient)
					{
						foreach (MyJumpGate jump_gate in this.GetAllJumpGates())
						{
							if (!jump_gate.MarkClosed) jump_gate.PollGateWormholeStatus();
						}
					}

					this.InitializationComplete = true;
					MyAPIGateway.Utilities.ShowMessage(MyJumpGateModSession.DISPLAYNAME, "Initialization Complete!");
				}
				
				// Tick grids
				{
					byte concurrency = (MyNetworkInterface.IsStandaloneMultiplayerClient) ? (byte) 1 : MyJumpGateModSession.Configuration.GeneralConfiguration.ConcurrentGridUpdateThreads;
					int grids_per_thread = (int) Math.Ceiling(this.GridMap.Count / (float) concurrency);
					while (this.GridUpdateThreads.Count < concurrency) this.GridUpdateThreads.Add(new MyThreadedUpdateInfo());
					while (this.GridUpdateThreads.Count > concurrency) this.GridUpdateThreads.Pop();
					int index = 0;
					
					foreach (KeyValuePair<long, MyJumpGateConstruct> pair in this.GridMap)
					{
						if (pair.Value.QueuedForUpdate) continue;
						pair.Value.QueuedForUpdate = true;
						MyThreadedUpdateInfo queue = this.GridUpdateThreads[index++ / grids_per_thread];
						queue.EnqueueID(pair.Key);
					}

					foreach (MyThreadedUpdateInfo update_info in this.GridUpdateThreads)
					{
						if (!update_info.IsFinished) continue;
						update_info.IsFinished = false;
						MyAPIGateway.Parallel.StartBackground(() => this.TickUpdateGrids(update_info));
					}
				}
				
				// Redraw Terminal Controls
				if (MyNetworkInterface.IsClientLike && this.InitializationComplete && (this.GameTick % 60 == 0 || this.__RedrawAllTerminalControls))
				{
					MyCubeBlockTerminal.UpdateRedrawControls();
					MyCockpitTerminal.UpdateRedrawControls();
					MyJumpGateCapacitorTerminal.UpdateRedrawControls();
					MyJumpGateControllerTerminal.UpdateRedrawControls();
					MyJumpGateDriveTerminal.UpdateRedrawControls();
					MyJumpGateRemoteAntennaTerminal.UpdateRedrawControls();
					MyJumpGateRemoteLinkTerminal.UpdateRedrawControls();
					this.__RedrawAllTerminalControls = false;
				}
				if (MyNetworkInterface.IsClientLike && MyAPIGateway.Gui.GetCurrentScreen != MyTerminalPageEnum.ControlPanel)
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
							animation_info.CompletionCallback?.Invoke(animation_info.IterCallbackException);
							this.JumpGateAnimations.Remove(pair.Key);
						}
						else if (animation_info.IterCallback != null)
						{
							bool stop = true;

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
							finally
							{
								if (stop)
								{
									animation.Stop();
									this.JumpGateAnimations.Remove(pair.Key);
								}
							}
						}
						else if (animation.JumpGate.Closed)
						{
							animation.Stop();
							this.JumpGateAnimations.Remove(pair.Key);
						}
					}
					
					// Tick queued gate detonations
					DateTime now = DateTime.UtcNow;

					foreach (KeyValuePair<JumpGateUUID, GateDetonationInfo> pair in this.GateDetonations)
					{
						if (now >= pair.Value.NextTickTime && pair.Value.TickDestroyGate()) this.GateDetonations.Remove(pair.Key);
					}
				}
				
				// Update Log
				if (this.GameTick % 300 == 0) Logger.Flush();
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
			if (this.DebugMode && !MyNetworkInterface.IsDedicatedMultiplayerServer)
			{
				Vector4 color4;
				Color color;
				Vector3D this_position = MyAPIGateway.Session.Camera.Position;
				MyStringId line_material = MyJumpGateModSession.MATERIALS.WeaponLaser;
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
					MatrixD orientation = jump_grid.GetConstructCalculatedOrienation();
					float extent = (float) jump_grid.GetCombinedAABB().HalfExtents.Max();
					MyStringId square = MyStringId.GetOrCompute("Square");
					MyTransparentGeometry.AddLineBillboard(square, Color.Red, orientation.Translation, orientation.Forward, extent, 0.1f);
					MyTransparentGeometry.AddLineBillboard(square, Color.Lime, orientation.Translation, orientation.Up, extent, 0.1f);
					MyTransparentGeometry.AddLineBillboard(square, Color.Blue, orientation.Translation, orientation.Right, extent, 0.1f);

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
							MatrixD gate_matrix_normalized = MatrixD.Normalize(gate_matrix);
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
							BoundingBoxD ellipse_aabb = new BoundingBoxD(-Vector3D.One, Vector3D.One);
							Vector3D jump_node = gate.WorldJumpNode;
							color = Color.AliceBlue;
							MySimpleObjectDraw.DrawTransparentBox(ref jump_ellipse.WorldMatrix, ref ellipse_aabb, ref color, MySimpleObjectRasterizer.Wireframe, 1, 0.1f, null, line_material);

							// Display collider sphere
							new BoundingEllipsoidD(MyJumpGateModSession.MaxJumpGateColliderRadius, ref jump_ellipse.WorldMatrix).Draw(Color.Chartreuse, 90, 0.1f, line_material);

							// Display gate normal override
							MyJumpGateControlObject control_object = gate.ControlObject;

							if (control_object != null && control_object.BlockSettings != null && control_object.BlockSettings.HasVectorNormalOverride())
							{
								color4 = Color.Magenta;
								Vector3D normal = gate.GetWorldMatrix(false, true).Forward;
								MySimpleObjectDraw.DrawLine(jump_node, jump_node + normal * 500d, line_material, ref color4, 1);
							}

							// Display jump gate endpoint aligned axes
							color4 = Color.Magenta;
							Vector3D axis = MyJumpGateModSession.LocalVectorToWorldVectorP(ref gate_matrix_normalized, new Vector3D(5, 0, 0));
							MySimpleObjectDraw.DrawLine(axis, jump_node, line_material, ref color4, 1f);
							color4 = Color.Yellow;
							axis = MyJumpGateModSession.LocalVectorToWorldVectorP(ref gate_matrix_normalized, new Vector3D(0, 5, 0));
							MySimpleObjectDraw.DrawLine(axis, jump_node, line_material, ref color4, 1f);
							color4 = Color.Cyan;
							axis = MyJumpGateModSession.LocalVectorToWorldVectorP(ref gate_matrix_normalized, new Vector3D(0, 0, 5));
							MySimpleObjectDraw.DrawLine(axis, jump_node, line_material, ref color4, 1f);

							// Display jump gate axes
							gate_matrix = jump_ellipse.WorldMatrix;
							color4 = Color.Red;
							axis = MyJumpGateModSession.LocalVectorToWorldVectorP(ref gate_matrix_normalized, new Vector3D(7.5, 0, 0));
							MySimpleObjectDraw.DrawLine(axis, jump_node, line_material, ref color4, 0.5f);
							color4 = Color.Green;
							axis = MyJumpGateModSession.LocalVectorToWorldVectorP(ref gate_matrix_normalized, new Vector3D(0, 7.5, 0));
							MySimpleObjectDraw.DrawLine(axis, jump_node, line_material, ref color4, 0.5f);
							color4 = Color.Blue;
							axis = MyJumpGateModSession.LocalVectorToWorldVectorP(ref gate_matrix_normalized, new Vector3D(0, 0, 7.5));
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
			
			// Cockpit HUD draw
			if (MyAPIGateway.Session.CameraController?.Entity is IMyCockpit)
			{
				IMyCockpit cockpit = (IMyCockpit) MyAPIGateway.Session.CameraController.Entity;
				MyCockpitInfo settings = this.GetCockpitTerminalSettings(cockpit);
				if (settings == null || !settings.DisplayHudMarkers) return;
				MatrixD camera = MyAPIGateway.Session.Camera.WorldMatrix;
				Vector3D camera_position = cockpit.WorldMatrix.Translation;
				MyJumpGate target = this.GetAllJumpGates()
					.Select((gate) => new MyTuple<MyJumpGate, double>(gate, Vector3D.DistanceSquared(camera_position, gate.WorldJumpNode)))
					.FirstOrDefault((pair) => {
						double radius = 2 * pair.Item1.JumpNodeRadius();
						return pair.Item2 <= radius * radius && pair.Item1.JumpGateGrid != null && (settings.DisplayForAttachedGates || !pair.Item1.JumpGateGrid.HasCubeGrid(cockpit.CubeGrid));
					}).Item1;
				
				if (target != null)
				{
					double extents = Math.Max(cockpit.CubeGrid.WorldAABB.Extents.Max(), 64);
					double vertex_distance_squared = extents * extents;
					float angle = (float) Vector3D.Angle(Vector3D.Up, MyAPIGateway.Session.Camera.WorldMatrix.Up);
					float size = (float) (target.JumpNodeRadius() * 0.01);

					foreach (MyTuple<Vector3D, double> pair in target.GetEffectiveJumpEllipse().GetVertices(32, 64)
						.Select((vertex) => new MyTuple<Vector3D, double>(vertex, Vector3D.DistanceSquared(vertex, camera_position)))
						.Where((pair) => pair.Item2 <= vertex_distance_squared))
					{
						float ratio = (float) (pair.Item2 / vertex_distance_squared);
						float radius = MathHelper.Lerp(0.75f, 0, ratio);
						Vector4 dot_color = Vector4.Lerp(Color.Aqua, Color.Red, ratio);
						MyTransparentGeometry.AddPointBillboard(MyJumpGateModSession.MATERIALS.WhiteDot, dot_color, pair.Item1, radius, 0);
					}
					
					foreach (MyJumpGateAnimation animation in this.GetGateAnimationsPlaying(target))
					{
						MyJumpGateWormholeAnimation wormhole_animation = null;
						if (animation.ActiveAnimationIndex == 3) wormhole_animation = animation.GateWormholeOpenAnimation;
						else if (animation.ActiveAnimationIndex == 4) wormhole_animation = animation.GateWormholeLoopAnimation;
						else if (animation.ActiveAnimationIndex == 5) wormhole_animation = animation.GateWormholeCloseAnimation;
						if (wormhole_animation == null) continue;

						foreach (ShapeCollider collider in wormhole_animation.GetCollidersOfType())
						{
							if (!collider.IsValid) continue;
							MatrixD collider_matrix = collider.WorldMatrix;

							switch (collider.ShapeColliderDefinition.CollisionEffectType)
							{
								case CollisionEffectTypeEnum.DAMAGE:
									MyTransparentGeometry.AddBillboardOriented(MyJumpGateModSession.MATERIALS.SpacialMarkerDamagePoint, Color.Orange, collider_matrix.Translation, camera.Left, camera.Up, size, size);
									break;
								case CollisionEffectTypeEnum.JUMP:
									MyTransparentGeometry.AddBillboardOriented(MyJumpGateModSession.MATERIALS.SpacialMarkerJumpPoint, Color.Aqua, collider_matrix.Translation, camera.Left, camera.Up, size, size);
									break;
								case CollisionEffectTypeEnum.DELETE:
									MyTransparentGeometry.AddBillboardOriented(MyJumpGateModSession.MATERIALS.SpacialMarkerDeletePoint, Color.Red, collider_matrix.Translation, camera.Left, camera.Up, size, size);
									break;
							}
						}
					}

					MyTransparentGeometry.AddBillboardOriented(MyJumpGateModSession.MATERIALS.SpacialMarkerJumpNode, Color.Aqua, target.WorldJumpNode, camera.Left, camera.Up, size, size);
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
			if (MyNetworkInterface.IsStandaloneMultiplayerClient) return;
			MyJumpGateModSession.Configuration?.Save();
			MyAPIGateway.Utilities.SetVariable($"{MyJumpGateModSession.MODID}.DebugMode", this.DebugMode);
			this.UpdateSaveModDataFile();
			List<MyCockpitInfo> cockpit_terminal_settings = new List<MyCockpitInfo>(this.CockpitBlockSettings.Count);

			foreach (KeyValuePair<IMyCockpit, MyCockpitInfo> pair in this.CockpitBlockSettings)
			{
				if (pair.Key.MarkedForClose) this.CockpitBlockSettings.Remove(pair.Key);
				else if (!pair.Value.IsDefault()) cockpit_terminal_settings.Add(pair.Value);
			}

			MyAPIGateway.Utilities.SetVariable("IOTA.CockpitSettings", Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(cockpit_terminal_settings)));
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
			Logger.Debug($"Entity \"{entity.DisplayName}\" added to session @ {entity.EntityId} >> SESSION_ADD", 5);
			if (MyNetworkInterface.IsStandaloneMultiplayerClient && this.GameTick > this.FirstUpdateTimeTicks) this.RequestGridDownload(cube_grid.EntityId);
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
			Logger.Debug($"Entity \"{entity.DisplayName}\" removed from session @ {entity.EntityId} >> {(entity.Closed ? "CLOSED" : "SESSION_REMOVE")}", 5);
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
			List<string> standard_control_ids = new List<string>() { "OnOff", "ShowInTerminal", "ShowInInventory", "ShowInToolbarConfig", "Name", "ShowOnHUD", "CustomData" };

			if (MyJumpGateModSession.IsBlockJumpGateController(block))
			{
				List<IMyTerminalControl> lcd_controls = controls.Where((control) => !standard_control_ids.Contains(control.Id) && !MyJumpGateControllerTerminal.IsControl(control)).ToList();
				foreach (IMyTerminalControl control in lcd_controls) controls.Remove(control);
				if (MyJumpGateControllerTerminal.TerminalSection == MyJumpGateControllerTerminal.MyTerminalSection.SCREENS) controls.AddList(lcd_controls);
			}
			else if (MyJumpGateModSession.IsBlockJumpGateRemoteAntenna(block))
			{
				List<IMyTerminalControl> removed = new List<IMyTerminalControl>();

				foreach (IMyTerminalControl control in controls)
				{
					if (control.Id.Length == 0 || MyJumpGateRemoteAntennaTerminal.IsControl(control) || standard_control_ids.Contains(control.Id)) continue;
					removed.Add(control);
				}

				controls.RemoveAll(removed.Contains);
			}
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
		/// Event handler for grid gate reconstruct packet
		/// </summary>
		/// <param name="packet">The received packet</param>
		private void OnNetworkGridGateReconstruct(MyNetworkInterface.Packet packet)
		{
			if (packet == null || MyNetworkInterface.IsStandaloneMultiplayerClient || packet.PhaseFrame != 1) return;
			KeyValuePair<long, bool> data = packet.Payload<KeyValuePair<long, bool>>();
			if (data.Key == -1) foreach (KeyValuePair<long, MyJumpGateConstruct> pair in this.GridMap) if (!pair.Value.MarkClosed) pair.Value.MarkGatesForUpdate(data.Value);
			else this.GridMap.GetValueOrDefault(data.Key, null)?.MarkGatesForUpdate();
		}

		/// <summary>
		/// Event handler for grid update failure notice
		/// </summary>
		/// <param name="packet">The received packet</param>
		private void OnNetworkConstructUpdateNotice(MyNetworkInterface.Packet packet)
		{
			MyConstructUpdateNotice notice;
			ulong this_id = MyAPIGateway.Multiplayer.MyId;
			if (MyNetworkInterface.IsServerLike || packet == null || packet.PhaseFrame != 1 || (notice = packet.Payload<MyConstructUpdateNotice>()) == null || (!MyAPIGateway.Session.IsUserAdmin(this_id) && !notice.ConstructOwnerIDs.Contains(this_id))) return;
			if (notice.IsFullFailure) this.DisplayConstructFullFailureNotice(null, notice.ConstructMainID, notice.ConstructName);
			else this.DisplayConstructPartialFailureNotice(null, notice.ConstructMainID, notice.ConstructName);
		}

		/// <summary>
		/// Event handler for jump gate detonation
		/// </summary>
		/// <param name="packet">The received packet</param>
		private void OnNetworkJumpGateDetonation(MyNetworkInterface.Packet packet)
		{
			if (packet == null || packet.PhaseFrame != 1 || !MyNetworkInterface.IsStandaloneMultiplayerClient) return;
			GateDetonationInfo.SerializedGateDetonationInfo detonation_info = packet.Payload<GateDetonationInfo.SerializedGateDetonationInfo>();
			MyJumpGate jump_gate = this.GetJumpGate(detonation_info.JumpGate);
			jump_gate?.StopSounds();
			this.GateDetonations.TryAdd(detonation_info.JumpGate, new GateDetonationInfo(ref detonation_info));
			Logger.Debug($"Jump gate server CORE detonation event: {detonation_info.JumpGate.GetJumpGateGrid()}::{detonation_info.JumpGate.GetJumpGate()}, RANGE={detonation_info.MaxExplosionRange}, DAMAGE={detonation_info.MaxExplosionDamage}, FORCE={detonation_info.MaxExplosionForce}", 3);
		}

		/// <summary>
		/// Event handler for client version request
		/// </summary>
		/// <param name="packet">The received packet</param>
		private void OnNetworkVersionCheck(MyNetworkInterface.Packet packet)
		{
			if (packet == null) return;
			else if (packet.PhaseFrame == 1 && MyNetworkInterface.IsMultiplayerServer)
			{
				packet = packet.Forward(packet.SenderID, false);
				packet.Payload(MyJumpGateModSession.ModVersion);
				packet.Send();
			}
			else if (packet.PhaseFrame == 2 && MyNetworkInterface.IsStandaloneMultiplayerClient)
			{
				Vector3I server_version = packet.Payload<Vector3I>();
				Vector3I client_version = MyJumpGateModSession.ModVersion;
				if (server_version == client_version) return;
				Logger.Warn($"Version mismatch with server - Client version: {client_version}, Server version: {server_version}");
				MyAPIGateway.Utilities.ShowMessage(MyJumpGateModSession.DISPLAYNAME, MyTexts.GetString("ModNotif_Error_ServerClientVersionMismatch")
					.Replace("{%0}", $"{client_version.X}.{client_version.Y}.{client_version.Z}")
					.Replace("{%1}", $"{server_version.X}.{server_version.Y}.{server_version.Z}"));
			}
		}

		/// <summary>
		/// Event handler for cockpit settings update
		/// </summary>
		private void OnNetworkCockpitSettingsUpdate(MyNetworkInterface.Packet packet)
		{
			MyCockpitInfo info = packet?.Payload<MyCockpitInfo>();

			if (packet == null || info == null) return;
			else if (MyNetworkInterface.IsMultiplayerServer && packet.PhaseFrame == 1)
			{
				IMyCockpit cockpit = info.Cockpit;
				MyCockpitInfo current = this.CockpitBlockSettings.GetValueOrDefault(cockpit, null);
				if (cockpit == null || (current != null && current.LastUpdateTimeEpoch >= info.LastUpdateTimeEpoch)) return;
				this.CockpitBlockSettings[cockpit] = info;
				this.RedrawAllTerminalControls();
				packet = packet.Forward(0, true);
				packet.Send();
			}
			else if (MyNetworkInterface.IsStandaloneMultiplayerClient && (packet.PhaseFrame == 1 || packet.PhaseFrame == 2))
			{
				IMyCockpit cockpit = info.Cockpit;
				if (cockpit == null) return;
				this.CockpitBlockSettings[cockpit] = info;
				this.RedrawAllTerminalControls();
			}
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

				if (MyNetworkInterface.IsClientLike)
				{
					if (!config.JumpGateConfiguration.EnableGateExplosions && MyJumpGateControllerTerminal.TerminalSection == MyJumpGateControllerTerminal.MyTerminalSection.DETONATION) MyJumpGateControllerTerminal.TerminalSection = MyJumpGateControllerTerminal.MyTerminalSection.JUMP_GATE;
					if (!config.JumpGateConfiguration.EnableGateExplosions && MyJumpGateRemoteAntennaTerminal.TerminalSection == MyJumpGateRemoteAntennaTerminal.MyTerminalSection.DETONATION) MyJumpGateRemoteAntennaTerminal.TerminalSection = MyJumpGateRemoteAntennaTerminal.MyTerminalSection.JUMP_GATE;
					this.RedrawAllTerminalControls();
				}

				MyAPIGateway.Utilities.ShowMessage(MyJumpGateModSession.DISPLAYNAME, MyTexts.GetString("Session_OnConfigReload"));
			}
		}

		/// <summary>
		/// Updates all stored grids and sends grid update packet every "x" seconds
		/// </summary>
		private void TickUpdateGrids(MyThreadedUpdateInfo grid_queue)
		{
			Stopwatch sw = new Stopwatch();
			sw.Start();

			try
			{
				grid_queue.IsFinished = false;
				grid_queue.Swap();

				foreach (long gridid in grid_queue.ReadEnqueuedIDs())
				{
					MyJumpGateConstruct grid = this.GridMap.GetValueOrDefault(gridid, null);

					if (grid == null) continue;
					else if (grid.Closed)
					{
						this.GridMap.Remove(gridid);
						Logger.Log($"Grid {gridid} FULL_CLOSE");
						continue;
					}

					try
					{
						bool true_update;
						if (MyJumpGateModSession.Network.Registered && MyNetworkInterface.IsMultiplayerServer && this.GameTick % this.GridNetworkUpdateDelay == 0) grid.SetDirty();
						grid.Update(out true_update);

						if (true_update)
						{
							if (grid.FailedTickCount > 0) grid.SetDirty();
							grid.FailedTickCount = 0;
						}
						
					}
					catch (Exception e)
					{
						Logger.Error($"Error during construct thread tick - {grid.CubeGridID} ({gridid}) - {grid.FailedTickCount + 1}/3\n  ...\n[ {e.GetType().Name} ]: {e.Message}\n{e.StackTrace}\n{e.InnerException}");

						if (MyNetworkInterface.IsServerLike && ++grid.FailedTickCount >= 3)
						{
							IMyCubeGrid main_grid = grid.CubeGrid;
							this.CloseGrid(grid);
							byte reinit_count = (byte) (this.GridReinitCounts.GetValueOrDefault(gridid, (byte) 0) + 1);

							if (main_grid == null)
							{
								Logger.Warn($"A grid failed its construct tick but was null - Cannot reinitialize null grid");
							}
							else if (reinit_count >= 3)
							{
								Logger.Critical($"Grid \"{main_grid.DisplayName}\" @ {main_grid.EntityId} failed its construct tick and exhauseted all reinitialization attempts - {reinit_count}/3");
								this.DisplayConstructFullFailureNotice(grid, gridid);
								this.GridReinitCounts.Remove(gridid);
							}
							else
							{
								Logger.Warn($"Grid \"{main_grid.DisplayName}\" @ {main_grid.EntityId} failed its construct tick and will be reinitialized - {reinit_count}/3");
								this.DisplayConstructPartialFailureNotice(grid, gridid);
								this.AddCubeGridToSession(main_grid, true, (construct) => {
									this.GridReinitCounts[construct.CubeGridID] = reinit_count;

									if (MyJumpGateModSession.Network.Registered)
									{
										MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet {
											PacketType = MyPacketTypeEnum.UPDATE_GRIDS,
											TargetID = 0,
											Broadcast = true,
										};
										packet.Payload(new List<MySerializedJumpGateConstruct>() { construct.ToSerialized(false) });
										packet.Send();
									}

									Logger.Log($"Grid \"{main_grid.DisplayName}\" @ {main_grid.EntityId} was reinitialized with construct ID {construct.CubeGridID}");
								});
							}
						}
					}
					finally
					{
						grid.QueuedForUpdate = false;
					}
				}
			}
			finally
			{
				sw.Stop();
				this.GridUpdateTimeTicks.Enqueue(sw.Elapsed.TotalMilliseconds);
				grid_queue.IsFinished = true;
			}
		}

		/// <summary>
		/// Closes all pending grids and gates
		/// </summary>
		private void FlushClosureQueues()
		{
			int count = this.GridCloseRequests.Count;

			for (int i = 0; i < count; ++i)
			{
				KeyValuePair<MyJumpGateConstruct, bool> pair;
				if (!this.GridCloseRequests.TryDequeue(out pair)) break;
				pair.Key.IsClosing = true;
				if (!pair.Key.CanFinalizeClosure()) this.GridCloseRequests.Enqueue(pair);
				else if (!pair.Key.Closed) pair.Key.Close(pair.Value);
			}

			count = this.GateCloseRequests.Count;

			for (int i = 0; i < count; ++i)
			{
				MyJumpGate gate;
				if (!this.GateCloseRequests.TryDequeue(out gate)) break;
				gate.IsClosing = true;
				if (!gate.CanFinalizeClosure()) this.GateCloseRequests.Enqueue(gate);
				else if (!gate.Closed) gate.Close();
			}
		}

		/// <summary>
		/// Attempts to add all pending grids
		/// </summary>
		private void FlushAdditionQueues()
		{
			foreach (KeyValuePair<IMyCubeGrid, Action<MyJumpGateConstruct>> pair in this.GridAddRequests)
			{
				MyJumpGateConstruct construct = this.AddCubeGridToSession(pair.Key);
				if (construct == null) continue;
				this.GridAddRequests.Remove(pair.Key);
				try { pair.Value(construct); }
				catch (Exception e) { Logger.Error($"Error during add construct request callback\n  ...\n[ {e.GetType().Name} ]: {e.Message}\n{e.StackTrace}\n{e.InnerException}"); }
			}
		}

		/// <summary>
		/// Displays a construct full failure notice<br />
		/// If server, broadcasts results to applicable clients
		/// </summary>
		/// <param name="construct">The failed construct</param>
		/// <param name="construct_id">The failed construct's fallback ID</param>
		/// <param name="construct_name">The failed construct's fallback name</param>
		private void DisplayConstructFullFailureNotice(MyJumpGateConstruct construct, long? construct_id = null, string construct_name = null)
		{
			if (MyJumpGateModSession.Network.Registered && MyNetworkInterface.IsServerLike && construct != null)
			{
				MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet {
					PacketType = MyPacketTypeEnum.CONSTRUCT_UPDATE_NOTICE,
					TargetID = 0,
					Broadcast = true,
				};
				packet.Payload(new MyConstructUpdateNotice() {
					ConstructMainID = construct.CubeGridID,
					ConstructOwnerIDs = construct.BigOwners,
					IsFullFailure = true,
				});
				packet.Send();
			}

			string gridid = construct?.CubeGridID.ToString() ?? construct_id.ToString() ?? "N/A";
			string gridname = construct?.PrimaryCubeGridCustomName ?? construct_name ?? "N/A";
			MyVisualScriptLogicProvider.SendChatMessageColored($"A construct has failed 3 consecutive ticks across 3 consecutive reload attempts!\nThis construct is closed and may not interact further with this mod!\n- Repaste grid to reattempt-\nCONSTRUCT_ID={gridid}\nCONSTRUCT_NAME={gridname}", Color.Red, MyJumpGateModSession.DISPLAYNAME, font: "Red");
			MyAPIGateway.Utilities.ShowNotification($"Critical failure whilst updating construct - \"{gridname}\"", 3000, "Red");
		}

		/// <summary>
		/// Displays a construct partial failure notice<br />
		/// If server, broadcasts results to applicable clients
		/// </summary>
		/// <param name="construct">The failed construct</param>
		/// <param name="construct_id">The failed construct's fallback ID</param>
		/// <param name="construct_name">The failed construct's fallback name</param>
		private void DisplayConstructPartialFailureNotice(MyJumpGateConstruct construct, long? construct_id = null, string construct_name = null)
		{
			if (MyJumpGateModSession.Network.Registered && MyNetworkInterface.IsServerLike && construct != null)
			{
				MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet
				{
					PacketType = MyPacketTypeEnum.CONSTRUCT_UPDATE_NOTICE,
					TargetID = 0,
					Broadcast = false,
				};
				packet.Payload(new MyConstructUpdateNotice()
				{
					ConstructMainID = construct.CubeGridID,
					ConstructOwnerIDs = construct.BigOwners,
					IsFullFailure = true,
				});
				packet.Send();
			}

			string gridid = construct?.CubeGridID.ToString() ?? construct_id.ToString() ?? "N/A";
			string gridname = construct?.PrimaryCubeGridCustomName ?? construct_name ?? "N/A";
			MyVisualScriptLogicProvider.SendChatMessageColored($"A construct has failed 3 consecutive ticks and will reinitialize!\nCONSTRUCT_ID={gridid}\nCONSTRUCT_NAME={gridname}", Color.Red, MyJumpGateModSession.DISPLAYNAME, font: "Red");
		}

		/// <summary>
		/// Stores the mod-specific log file list to sandbox.sbc
		/// </summary>
		private void UpdateSaveModDataFile()
		{
			uint max_logfiles = this.MaxModSpecificLogFiles;
			BinaryWriter writer = null;

			try
			{
				writer = MyAPIGateway.Utilities.WriteBinaryFileInLocalStorage(this.ModDataFile, this.GetType());
				this.ModData.ModLogFiles.LastOrDefault()?.UpdateTime();
				this.ModData.ModLogFiles.RemoveAll((file) => file.Filename != this.ActiveModLogFile && !MyAPIGateway.Utilities.FileExistsInLocalStorage(file.Filename, this.GetType()));
				this.ModData.MaxModSpecificLogFiles = max_logfiles;
				writer.Write(MyAPIGateway.Utilities.SerializeToBinary(this.ModData));
				Logger.Log($"Mod data file updated; Version={this.ModData.ModVersion.X}.{this.ModData.ModVersion.Y}.{this.ModData.ModVersion.Z}, Log Capacity={this.ModData.ModLogFiles.Count}/{max_logfiles}");
			}
			catch (Exception e)
			{
				Logger.Error($"Failed to serialize mod data\n  ...\n[ {e.GetType().Name} ]: {e.Message}\n{e.StackTrace}\n{e.InnerException}");
				MyAPIGateway.Utilities.ShowMessage(MyJumpGateModSession.DISPLAYNAME, "Failed to save mod data; See log for details");
			}
			finally
			{
				writer?.Close();
			}
		}

		/// <summary>
		/// Loads the mod-specific log file list from sandbox.sbc
		/// </summary>
		/// <returns>Whether data was successfully loaded</returns>
		private bool UpdateLoadLogFileList()
		{
			if (!MyAPIGateway.Utilities.FileExistsInLocalStorage(this.ModLogSettingsFile, this.GetType()))
			{
				this.ModData = new MyModDataInfo {
					ModVersion = new Vector3I(0, 0, 9),
					MaxModSpecificLogFiles = MyJumpGateModSession.Configuration.GeneralConfiguration.MaxStoredModSpecificLogFiles,
					ModLogFiles = new List<MyModLogFileInfo>(),
				};
				return false;
			}

			BinaryReader reader = null;

			try
			{
				reader = MyAPIGateway.Utilities.ReadBinaryFileInLocalStorage(this.ModLogSettingsFile, this.GetType());
				this.FallbackMaxModSpecificLogFiles = reader.ReadUInt32();
				byte[] buffer = new byte[reader.BaseStream.Length - reader.BaseStream.Position];
				reader.Read(buffer, 0, buffer.Length);
				List<MyModLogFileInfo> log_files = MyAPIGateway.Utilities.SerializeFromBinary<List<MyModLogFileInfo>>(buffer) ?? new List<MyModLogFileInfo>();
				log_files.RemoveAll((file) => file.Filename != this.ActiveModLogFile && !MyAPIGateway.Utilities.FileExistsInLocalStorage(file.Filename, this.GetType()));
				this.ModData = new MyModDataInfo {
					ModVersion = new Vector3I(0, 0, 9),
					MaxModSpecificLogFiles = this.FallbackMaxModSpecificLogFiles,
					ModLogFiles = log_files,
				};
			}
			catch (Exception e)
			{
				this.ModDataLoadError = new ModFileLoadingException("Failed to deserialize mod-specific log file settings", e);
			}
			finally
			{
				reader?.Close();
			}

			return true;
		}

		/// <summary>
		/// Loads the mod data file from local storage
		/// </summary>
		/// <returns>Whether file loaded succesfully</returns>
		private bool UpdateLoadModDataFile()
		{
			if (!MyAPIGateway.Utilities.FileExistsInLocalStorage(this.ModDataFile, this.GetType()))
			{
				this.ModData = new MyModDataInfo {
					ModVersion = MyJumpGateModSession.ModVersion,
					MaxModSpecificLogFiles = MyJumpGateModSession.Configuration.GeneralConfiguration.MaxStoredModSpecificLogFiles,
					ModLogFiles = new List<MyModLogFileInfo>(),
				};
				return false;
			}

			BinaryReader reader = null;

			try
			{
				reader = MyAPIGateway.Utilities.ReadBinaryFileInLocalStorage(this.ModDataFile, this.GetType());
				byte[] buffer = reader.ReadBytes((int) reader.BaseStream.Length);
				this.ModData = MyAPIGateway.Utilities.SerializeFromBinary<MyModDataInfo>(buffer);
				this.ModData.ModLogFiles.RemoveAll((file) => file.Filename != this.ActiveModLogFile && !MyAPIGateway.Utilities.FileExistsInLocalStorage(file.Filename, this.GetType()));
			}
			catch (Exception e)
			{
				this.ModDataLoadError = new ModFileLoadingException("Failed to deserialize mod data", e);
			}
			finally
			{
				reader?.Close();
			}

			return true;
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
			
			if (MyNetworkInterface.IsClientLike)
			{
				if (!config.JumpGateConfiguration.EnableGateExplosions && MyJumpGateControllerTerminal.TerminalSection == MyJumpGateControllerTerminal.MyTerminalSection.DETONATION) MyJumpGateControllerTerminal.TerminalSection = MyJumpGateControllerTerminal.MyTerminalSection.JUMP_GATE;
				if (!config.JumpGateConfiguration.EnableGateExplosions && MyJumpGateRemoteAntennaTerminal.TerminalSection == MyJumpGateRemoteAntennaTerminal.MyTerminalSection.DETONATION) MyJumpGateRemoteAntennaTerminal.TerminalSection = MyJumpGateRemoteAntennaTerminal.MyTerminalSection.JUMP_GATE;
				this.RedrawAllTerminalControls();
			}

			if (MyJumpGateModSession.Network.Registered)
			{
				MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet {
					PacketType = MyPacketTypeEnum.UPDATE_CONFIG,
					Broadcast = true,
					TargetID = 0,
				};

				packet.Payload(config);
				packet.Send();
			}

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
		public void WarpEntityBatchOverTime(MyJumpGate jump_gate, List<MyEntity> entity_batch, ref MatrixD source_matrix, ref MatrixD dest_matrix, ref Vector3D end_position, ushort time, double max_safe_speed, Action<EntityWarpInfo> callback = null)
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
		/// Detonates the specified jump gate<br />
		/// Gate must be valid and not marked closed
		/// </summary>
		/// <param name="gate">The gate to detonate</param>
		/// <param name="initiator">The drive at which to start the explosion chain</param>
		public void DetonateJumpGate(MyJumpGate gate, MyJumpGateDrive initiator = null)
		{
			if (gate == null || gate.MarkClosed || !gate.IsValid()) return;
			this.GateDetonations.TryAdd(JumpGateUUID.FromJumpGate(gate), new GateDetonationInfo(gate, initiator));
		}

		/// <summary>
		/// Purges all but the current active log file
		/// </summary>
		public void PurgeAllStoredLogFiles()
		{
			for (int i = 0; i < this.ModData.ModLogFiles.Count; ++i)
			{
				MyModLogFileInfo info = this.ModData.ModLogFiles[i];
				if (info.Filename == this.ActiveModLogFile) continue;
				MyAPIGateway.Utilities.DeleteFileInLocalStorage(info.Filename, this.GetType());
				this.ModData.ModLogFiles.RemoveAt(i--);
			}
		}

		/// <summary>
		/// Purges the oldenst log files keeping the newest 'n' files<br />
		/// Current file will not be purged
		/// </summary>
		/// <param name="count">The number of log files to keep or 0 to purge all</param>
		public void PurgeStoredLogFiles(uint count)
		{
			if (count == 0)
			{
				this.PurgeAllStoredLogFiles();
				return;
			}

			List<MyModLogFileInfo> closed = this.ModData.ModLogFiles.Where((info) => info.Filename != this.ActiveModLogFile).OrderBy((info) => info.ModificationTime).Skip((int) count).ToList();
			foreach (MyModLogFileInfo file in closed) MyAPIGateway.Utilities.DeleteFileInLocalStorage(file.Filename, this.GetType());
			this.ModData.ModLogFiles.RemoveAll((info) => closed.Contains(info));
		}

		/// <summary>
		/// Updates a cockpit's terminal settings<br />
		/// If standalone multiplayer client, also sends new settings to server
		/// </summary>
		/// <param name="cockpit">The cockpit whose settings to update</param>
		/// <param name="new_settings">The new settings</param>
		public void UpdateCockpitTerminalSettings(IMyCockpit cockpit, MyCockpitInfo new_settings)
		{
			if (cockpit == null || cockpit.MarkedForClose || new_settings == null) return;
			new_settings.CockpitId = cockpit.EntityId;
			new_settings.LastUpdateTime = DateTime.UtcNow;
			this.CockpitBlockSettings[cockpit] = new_settings;

			if (MyNetworkInterface.IsStandaloneMultiplayerClient)
			{
				MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet {
					PacketType = MyPacketTypeEnum.UPDATE_COCKPIT,
					TargetID = 0,
					Broadcast = false,
				};
				packet.Payload(new_settings);
				packet.Send();
			}
			else if (MyNetworkInterface.IsMultiplayerServer)
			{
				MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet {
					PacketType = MyPacketTypeEnum.UPDATE_COCKPIT,
					TargetID = 0,
					Broadcast = true,
				};
				packet.Payload(new_settings);
				packet.Send();
			}
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
			this.GridReinitCounts[new_id] = this.GridReinitCounts.GetValueOrDefault(grid.CubeGridID, (byte) 0);
			this.GridMap.Remove(grid.CubeGridID);
			this.GridReinitCounts.Remove(grid.CubeGridID);
			return true;
		}

		/// <summary>
		/// </summary>
		/// <param name="grid">The construct to get</param>
		/// <returns>The number of times this construct was reinitialized</returns>
		public byte GridReinitializationCount(MyJumpGateConstruct grid)
		{
			return this.GridReinitCounts?.GetValueOrDefault(grid.CubeGridID, (byte) 0) ?? 0;
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
		/// Creates a new mod-specific log file<br />
		/// Extra log files will be deleted if max stored log files is exceeded
		/// </summary>
		/// <param name="filename">The new filename</param>
		/// <returns>A file writer</returns>
		public TextWriter CreateNewModSpecificLogFile(string filename)
		{
			uint max_count = this.MaxModSpecificLogFiles;
			this.ActiveModLogFile = filename;
			this.ModData.ModLogFiles.Add(new MyModLogFileInfo(filename));
			this.PurgeStoredLogFiles(max_count);
			return MyAPIGateway.Utilities.WriteFileInLocalStorage(filename, this.GetType());
		}

		/// <summary>
		/// </summary>
		/// <param name="uuid">The UUID of the gate</param>
		/// <returns>The gate's detonation info or null if not found</returns>
		public GateDetonationInfo GetGateDetonationInfo(JumpGateUUID uuid)
		{
			return this.GateDetonations.GetValueOrDefault(uuid, null);
		}

		/// <summary>
		/// Adds a new cube grid to this session's internal map
		/// </summary>
		/// <param name="cube_grid">The grid to add</param>
		/// <param name="queue">Whether to queue if grid cannot be added</param>
		/// <param name="addition_callback">Callback to call once queued grid is added</param>
		public MyJumpGateConstruct AddCubeGridToSession(IMyCubeGrid cube_grid, bool queue = false, Action<MyJumpGateConstruct> addition_callback = null)
		{
			MyJumpGateConstruct grid = this.GetJumpGateGrid(cube_grid);
			if (grid != null && !grid.MarkClosed && !queue) return grid;
			grid = new MyJumpGateConstruct(cube_grid, cube_grid.EntityId);
			MyJumpGateConstruct existing = this.GridMap.GetValueOrDefault(cube_grid.EntityId, null);

			if (existing == null)
			{
				this.GridMap[cube_grid.EntityId] = grid;
				this.GridReinitCounts[cube_grid.EntityId] = 0;
				Logger.Debug($"Grid {cube_grid.EntityId} added to session", 1);
				return grid;
			}
			else if (existing.Closed)
			{
				this.GridMap[cube_grid.EntityId] = grid;
				this.GridReinitCounts[cube_grid.EntityId] = 0;
				this.CloseGrid(existing, true);
				Logger.Debug($"Grid {cube_grid.EntityId} added to session; CLOSED_EXISTING", 1);
				return grid;
			}
			else if (queue)
			{
				grid.Release();
				this.GridAddRequests[cube_grid] = addition_callback;
				Logger.Debug($"Grid {cube_grid.EntityId} skipped session add; QUEUED", 1);
				return null;
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
		/// </summary>
		/// <param name="grid">The grid construct</param>
		/// <returns>A grid's duplicate construct or null</returns>
		public MyJumpGateConstruct GetDuplicateGrid(MyJumpGateConstruct grid)
		{
			if (grid == null) return null;
			MyJumpGateConstruct duplicate;
			foreach (IMyCubeGrid subgrid in grid.GetCubeGrids()) if (subgrid.EntityId != grid.CubeGridID && this.GridMap.TryGetValue(subgrid.EntityId, out duplicate)) return duplicate;
			return null;
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
		/// Gets all animations playing from the specified jump gate
		/// </summary>
		/// <param name="gate">The gate to poll</param>
		/// <return>All animations playing for the specified gate</return>
		public IEnumerable<MyJumpGateAnimation> GetGateAnimationsPlaying(MyJumpGate gate)
		{
			if (gate != null)
			{
				foreach (KeyValuePair<ulong, AnimationInfo> pair in this.JumpGateAnimations)
				{
					if (pair.Value.Animation.JumpGate == gate)
					{
						yield return pair.Value.Animation;
					}
				}
			}
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

		/// <summary>
		/// Gets a cockpit's terminal settings
		/// </summary>
		/// <param name="cockpit">The cockpit whose settings to retreive</param>
		/// <returns>The settings or null if none set</returns>
		public MyCockpitInfo GetCockpitTerminalSettings(IMyCockpit cockpit)
		{
			if (cockpit == null || cockpit.MarkedForClose) return null;
			MyCockpitInfo settings;
			string serialized_cockpit_settings;
			if (this.CockpitBlockSettings.TryGetValue(cockpit, out settings)) return settings;
			else if (MyAPIGateway.Utilities.GetVariable("IOTA.CockpitSettings", out serialized_cockpit_settings))
			{
				List<MyCockpitInfo> deserialized_cockpit_settings = MyAPIGateway.Utilities.SerializeFromBinary<List<MyCockpitInfo>>(Convert.FromBase64String(serialized_cockpit_settings));

				foreach (MyCockpitInfo cockpit_info in deserialized_cockpit_settings)
				{
					IMyCockpit stored = cockpit_info.Cockpit;
					if (stored == null || stored.MarkedForClose) continue;
					else this.CockpitBlockSettings.TryAdd(cockpit, cockpit_info);
				}

				return this.CockpitBlockSettings.GetValueOrDefault(cockpit, new MyCockpitInfo(cockpit));
			}
			else return new MyCockpitInfo(cockpit);
		}
		#endregion
	}
}
