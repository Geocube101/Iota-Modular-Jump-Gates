using IOTA.ModularJumpGates.JumpGateConstruct;
using IOTA.ModularJumpGates.Util;
using IOTA.ModularJumpGates.Util.ConcurrentCollections;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VRage;
using VRage.Game.ModAPI;
using VRageMath;

namespace IOTA.ModularJumpGates.Session
{
	internal partial class MyJumpGateModSession
	{
		#region Private Variables
		/// <summary>
		/// Master map of threaded construct update queues
		/// </summary>
		private List<MyThreadedUpdateInfo> GridUpdateThreads = new List<MyThreadedUpdateInfo>();

		/// <summary>
		/// A list of the last 60 update times
		/// </summary>
		private ConcurrentSpinQueue<double> GridUpdateTimeTicks = new ConcurrentSpinQueue<double>(60);

		/// <summary>
		/// Used to store all grids requesting closure
		/// </summary>
		private ConcurrentQueue<MyTuple<MyJumpGateConstruct, bool, Action>> GridCloseRequests = new ConcurrentQueue<MyTuple<MyJumpGateConstruct, bool, Action>>();

		/// <summary>
		/// Used to store all constructs awaiting session add
		/// </summary>
		private ConcurrentQueue<MyTuple<IMyCubeGrid, Event>> GridAddRequests = new ConcurrentQueue<MyTuple<IMyCubeGrid, Event>>();

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
		#endregion

		#region Public Static Methods
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
		#endregion

		#region Private Methods
		/// <summary>
		/// Unloads all constructs and their jump gates
		/// </summary>
		private void UnloadConstructs()
		{
			int closed_grids_count = 0;
			this.GateDetonations.Clear();

			foreach (KeyValuePair<long, MyJumpGateConstruct> pair in this.GridMap)
			{
				if (!pair.Value.Closed)
				{
					pair.Value.Dispose();
					this.CloseGrid(pair.Value, true);
					++closed_grids_count;
				}
			}

			MyTuple<IMyCubeGrid, Event> callbacks;
			while (this.GridAddRequests.TryDequeue(out callbacks)) ;

			Logger.Log("Flushing closure queues...");
			Stopwatch sw = Stopwatch.StartNew();
			while (this.GridCloseRequests.Count > 0 || this.GateCloseRequests.Count > 0) this.FlushClosureQueues();
			sw.Stop();
			Logger.Log($"Closure queues flushed; took {MyJumpGateModSession.AutoconvertTimeUnits(sw.Elapsed.TotalSeconds, 2)}");

			Logger.Log($"Closed {closed_grids_count} Grids");
			foreach (KeyValuePair<ulong, AnimationInfo> pair in this.JumpGateAnimations) pair.Value.Animation.Clean();
			foreach (KeyValuePair<long, EntityWarpInfo> pair in this.EntityWarps) pair.Value.Close();
			foreach (MyThreadedUpdateInfo info in this.GridUpdateThreads) info.Dispose();

			this.GridUpdateTimeTicks.Clear();
			this.GridMap.Clear();
			this.GridReinitCounts.Clear();
			this.PartialSuspendedGridsQueue.Clear();
			this.GridUpdateThreads.Clear();

			this.GridCloseRequests = null;
			this.GateCloseRequests = null;
			this.GridReinitCounts = null;
			this.GridUpdateTimeTicks = null;
			this.PartialSuspendedGridsQueue = null;
			this.GridAddRequests = null;
			this.GridUpdateThreads = null;
			this.GridMap = null;
		}

		/// <summary>
		/// Begings ticking of grids<br />
		/// Grids are handed off to respective update threads for ticking
		/// </summary>
		private void TickDistributedGrids()
		{
			byte concurrency = (MyNetworkInterface.IsStandaloneMultiplayerClient) ? (byte) 1 : this.Configuration.GeneralConfiguration.ConcurrentGridUpdateThreads.Value;
			int grids_per_thread = (int) Math.Ceiling(this.GridMap.Count / (float) concurrency);
			while (this.GridUpdateThreads.Count < concurrency) this.GridUpdateThreads.Add(new MyThreadedUpdateInfo());
			while (this.GridUpdateThreads.Count > concurrency) this.GridUpdateThreads.Pop().Dispose();
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

		/// <summary>
		/// Ticks all constructs' non-threadable operations
		/// </summary>
		private void TickNonthreadableGrids()
		{
			foreach (KeyValuePair<long, MyJumpGateConstruct> pair in this.GridMap)
			{
				MyJumpGateConstruct grid = pair.Value;

				if (!grid.MarkClosed && !grid.Closed && grid.AtLeastOneUpdate() && !this.IsConstructQueuedForPartialReload(grid.CubeGridID))
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
				else if (grid.MarkClosed && !grid.QueuedForClose) this.CloseGrid(grid);
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
					long cube_grid_id;

					if (grid == null || this.IsConstructQueuedForPartialReload(gridid)) continue;
					else if (grid.Closed)
					{
						this.GridMap.Remove(gridid);
						Logger.Log($"Grid {gridid} FULL_CLOSE");
						continue;
					}
					else if ((cube_grid_id = grid.CubeGridID) != gridid)
					{
						if (this.GridMap.TryAdd(cube_grid_id, grid))
						{
							Logger.Warn($"Grid at ID {gridid} does not match grid ID {cube_grid_id}; CONSTRUCT_MOVED");
							this.GridMap.Remove(gridid);
						}
						else if (grid.FailedTickCount >= 3)
						{
							Logger.Warn($"Grid at ID {gridid} does not match grid ID {cube_grid_id} and target exists; CONSTRUCT_CLOSED");
							grid.Close(true);
							this.GridMap.Remove(gridid);
							Logger.Log($"Grid {gridid} FULL_CLOSE");
						}
						else
						{
							Logger.Warn($"Grid at ID {gridid} does not match grid ID {cube_grid_id} and target exists; CONSTRUCT_MOVE_DEFERRED_{grid.FailedTickCount}");
							grid.FailedTickCount += 1;
						}

						continue;
					}

					try
					{
						bool true_update;
						if (this.Network.Registered && MyNetworkInterface.IsMultiplayerServer && this.GameTick % this.GridNetworkUpdateDelay == 0) grid.SetDirty();
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

									if (this.Network.Registered)
									{
										MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet
										{
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
		/// Displays a construct full failure notice<br />
		/// If server, broadcasts results to applicable clients
		/// </summary>
		/// <param name="construct">The failed construct</param>
		/// <param name="construct_id">The failed construct's fallback ID</param>
		/// <param name="construct_name">The failed construct's fallback name</param>
		private void DisplayConstructFullFailureNotice(MyJumpGateConstruct construct, long? construct_id = null, string construct_name = null)
		{
			if (this.Network.Registered && MyNetworkInterface.IsServerLike && construct != null)
			{
				MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet
				{
					PacketType = MyPacketTypeEnum.CONSTRUCT_UPDATE_NOTICE,
					TargetID = 0,
					Broadcast = true,
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
			MyVisualScriptLogicProvider.SendChatMessageColored($"A construct has failed 3 consecutive ticks across 3 consecutive reload attempts!\nThis construct is closed and may not interact further with this mod!\n- Repaste grid to reattempt-\nCONSTRUCT_ID={gridid}\nCONSTRUCT_NAME={gridname}", Color.Red, this.DisplayName, font: "Red");
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
			if (this.Network.Registered && MyNetworkInterface.IsServerLike && construct != null)
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
			MyVisualScriptLogicProvider.SendChatMessageColored($"A construct has failed 3 consecutive ticks and will reinitialize!\nCONSTRUCT_ID={gridid}\nCONSTRUCT_NAME={gridname}", Color.Red, this.DisplayName, font: "Red");
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
			MyJumpGateConstruct construct;
			if (!this.GridMap.TryGetValue(gridid, out construct)) this.GridMap[gridid] = construct = new MyJumpGateConstruct(cube_grid, gridid);
			bool deserialized = (construct.IsSuspended && cube_grid != null) ? construct.Persist(serialized) : construct.FromSerialized(serialized);
			return (deserialized) ? construct : null;
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Queues a grid for closure on main game thread
		/// </summary>
		/// <param name="grid">The grid to close</param>
		/// <param name="override_client">Whether to force closure on client</param>
		/// <param name="callback">Callback called after grid is closed</param>
		public void CloseGrid(MyJumpGateConstruct grid, bool override_client = false, Action callback = null)
		{
			if (grid == null || grid.Closed || grid.QueuedForClose || !this.GridMap.ContainsKey(grid.CubeGridID)) return;
			grid.QueuedForClose = true;
			this.GridCloseRequests.Enqueue(new MyTuple<MyJumpGateConstruct, bool, Action>(grid, override_client, callback));
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

		/// <param name="main_grid_id">The construct main grid ID to check</param>
		/// <returns>Whether construct is queued for partial reload</returns>
		public bool IsConstructQueuedForPartialReload(long main_grid_id)
		{
			return this.PartialSuspendedGridsQueue?.ContainsKey(main_grid_id) ?? false;
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
				Event callbacks = null;

				if (addition_callback != null)
				{
					foreach (MyTuple<IMyCubeGrid, Event> item in this.GridAddRequests)
					{
						if (item.Item1 == cube_grid)
						{
							callbacks = item.Item2;
							callbacks.Add(addition_callback);
							break;
						}
					}
				}

				if (callbacks == null && addition_callback == null) this.GridAddRequests.Enqueue(new MyTuple<IMyCubeGrid, Event>(cube_grid, new Event()));
				else if (callbacks == null) this.GridAddRequests.Enqueue(new MyTuple<IMyCubeGrid, Event>(cube_grid, new Event(addition_callback)));
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
			if (this.GridMap.TryGetValue(cube_grid.EntityId, out jump_gate_grid) && !jump_gate_grid.MarkClosed && !jump_gate_grid.Closed) return jump_gate_grid;
			List<IMyCubeGrid> connected_grids = new List<IMyCubeGrid>();
			cube_grid.GetGridGroup(GridLinkTypeEnum.Logical).GetGrids(connected_grids);
			foreach (IMyCubeGrid grid in connected_grids) if (this.GridMap.TryGetValue(grid.EntityId, out jump_gate_grid) && !jump_gate_grid.MarkClosed && !jump_gate_grid.Closed) return jump_gate_grid;
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
		/// Gets all valid jump gate grids stored in this session
		/// </summary>
		/// <param name="filter">A predicate to match jump gate grids against</param>
		/// <param name="grids">All jump gate grid constructs<br />List will be cleared</param>
		public IEnumerable<MyJumpGateConstruct> GetAllJumpGateGrids()
		{
			return this.GridMap?.Select((pair) => pair.Value).Where(this.IsJumpGateGridMultiplayerValid) ?? Enumerable.Empty<MyJumpGateConstruct>();
		}
		#endregion
	}
}
