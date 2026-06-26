using IOTA.ModularJumpGates.JumpGates;
using IOTA.ModularJumpGates.Session;
using IOTA.ModularJumpGates.Util;
using IOTA.ModularJumpGates.Util.ConcurrentCollections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using VRage.Game.ModAPI;

namespace IOTA.ModularJumpGates.JumpGateConstruct
{
	internal partial class MyJumpGateConstruct
	{
		#region Private Variables
		/// <summary>
		/// Whether drive max raycast distances should be updated on next tick
		/// </summary>
		private bool UpdateDriveMaxRaycastDistances = false;

		/// <summary>
		/// Whether to cause a tick failure
		/// </summary>
		private byte CauseTickFailure = 0;

		/// <summary>
		/// The local game tick for this object
		/// </summary>
		private ulong LocalGameTick = 0;

		/// <summary>
		/// The mutex lock for exclusive construct updates
		/// </summary>
		private readonly object UpdateLock = new object();

		/// <summary>
		/// A list of the last 60 update times
		/// </summary>
		private ConcurrentSpinQueue<double> UpdateTimeTicks = new ConcurrentSpinQueue<double>(60);
		#endregion

		#region Public Variables
		/// <summary>
		/// Whether this grid is queued for update
		/// </summary>
		public bool QueuedForUpdate = false;

		/// <summary>
		/// Whether this grid is queued for closure
		/// </summary>
		public bool QueuedForClose = false;

		/// <summary>
		/// Number of times this construct failed consecutive ticks
		/// </summary>
		public byte FailedTickCount;

		/// <summary>
		/// The longest time it took for this grid to update
		/// </summary>
		public double LongestUpdateTime { get; private set; } = -1;
		#endregion

		#region Public Methods
		/// <summary>
		/// Does one update of this construct, ticking all associated jump gates<br />
		/// This will close this construct if:<br />
		///  ... This construct is marked for close<br />
		///  ... The session is not running<br />
		///  ... This construct is not valid<br />
		///  ... This construct's main grid is not the true main grid
		/// </summary>
		public void Update(out bool true_update)
		{
			true_update = false;
			if (this.Closed || this.IsClosing) return;
			MyGridInvalidationReason reason;

			// Check update validity
			if (MyNetworkInterface.IsStandaloneMultiplayerClient && this.IsSuspended && this.CubeGrid != null)
			{
				this.SuspendConstruct();
				return;
			}

			if (this.SuspensionLockUpdatingPaused || !Monitor.TryEnter(this.UpdateLock)) return;
			bool suspended = this.IsSuspended && MyNetworkInterface.IsStandaloneMultiplayerClient;
			string grid_id = this.CubeGridID.ToString();
			//Logger.Debug($"[{grid_id}] - UPDATE_START", 4);
			Stopwatch sw = new Stopwatch();
			sw.Start();

			try
			{
				//Logger.Debug($"[{grid_id}] - IN_SCENE={this.CubeGrid?.InScene ?? false}", 1);
				if (MyJumpGateModSession.Instance.SessionStatus != MySessionStatusEnum.RUNNING)
				{
					MyJumpGateModSession.Instance.CloseGrid(this);
					this.SendNetworkGridUpdate();
					Logger.Debug($"[{grid_id}] - Session is not running ({MyJumpGateModSession.Instance.SessionStatus}); CLOSED", 2);
					return;
				}
				else if (!suspended && (reason = this.GetInvalidationReason()) != MyGridInvalidationReason.NONE)
				{
					MyJumpGateModSession.Instance.CloseGrid(this);
					this.SendNetworkGridUpdate();
					Logger.Debug($"[{grid_id}] - Grid is not valid ({reason}); CLOSED", 2);
					return;
				}

				// Main Grid Check
				if (!suspended)
				{
					this.SetupConstruct();
					IMyCubeGrid main_grid = this.GetMainCubeGrid();
					MyJumpGateConstruct duplicate;

					if (main_grid != this.CubeGrid && !MyJumpGateModSession.Instance.HasCubeGrid(main_grid.EntityId))
					{
						if (MyJumpGateModSession.Instance.MoveGrid(this, main_grid.EntityId))
						{
							this.CubeGrid = main_grid;
							this.SuspendedCubeGridID = main_grid.EntityId;
							this.MarkUpdateJumpGates = true;
							this.SetupConstruct();
							Logger.Debug($"[{grid_id}]] - Main grid changed -> {main_grid.EntityId}", 2);
							grid_id = main_grid.EntityId.ToString();
						}
						else
						{
							MyJumpGateModSession.Instance.CloseGrid(this, true);
							Logger.Debug($"[{grid_id}]] - Grid is not Main Grid @ {main_grid.EntityId}; CLOSED", 2);
							return;
						}
					}
					else if (main_grid != this.CubeGrid)
					{
						MyJumpGateModSession.Instance.CloseGrid(this, true);
						this.SendNetworkGridUpdate();
						Logger.Debug($"[{grid_id}]] - Grid is not Main Grid @ {main_grid.EntityId}; CLOSED", 2);
						return;
					}
					else if ((duplicate = MyJumpGateModSession.Instance.GetDuplicateGrid(this)) != null)
					{
						long gid = this.CubeGridID;
						MyJumpGateModSession.Instance.CloseGrid(duplicate, true, () => MyJumpGateModSession.Instance.RequestGridDownload(gid));
						Logger.Debug($"[{grid_id}]] - Grid duplicate exists; UPDATE_SKIPPED", 2);
						return;
					}
				}

				// Initialize update
				true_update = true;
				this.PrimaryCubeGridCustomName = this.CubeGrid?.CustomName ?? this.PrimaryCubeGridCustomName;
				bool first_update = !suspended && this.LocalGameTick == 0;
				if (!first_update && this.JumpGateBlocks.Count == 0 && this.JumpGates.Count == 0) return;
				if (!suspended) this.LocalGameTick = (this.LocalGameTick + 1) % 0xFFFFFFFFFFFFFFF0;
				bool full_gate_update = this.MarkUpdateJumpGates || this.LocalGameTick == 180;
				bool gate_entity_update = this.LocalGameTick % 30 == 0;
				bool is_dirty = this.IsDirty || full_gate_update;

				if (MyNetworkInterface.IsServerLike)
				{
					//Check tick failure
					if (this.CauseTickFailure > 0)
					{
						--this.CauseTickFailure;
						throw new ExplicitTickFailureException("An explicit tick failure event was raised");
					}

					// Update drive combinations
					this.UpdateDriveCombinations(ref full_gate_update);

					// Update drive max raycast distances
					this.UpdateDriveRaycastDistances();

					// Rebuild all jump gates
					if (full_gate_update)
					{
						this.RebuildJumpGates();
						this.MarkUpdateJumpGates = false;
					}

					// Update comm linked grids
					this.SearchUpdateCommLinkedGrids();

					// Update beacon linked beacons
					this.SearchUpdateBeaconLinkedBeacons();
				}

				// Update jump gates
				if (this.LocalGameTick > 1) this.TickUpdateJumpGates(full_gate_update, gate_entity_update);

				// Finalize update
				this.FullyInitialized = this.FullyInitialized || first_update;
				if (MyJumpGateModSession.Instance.Network.Registered && (is_dirty || first_update) && !suspended) this.SendNetworkGridUpdate();

				if (first_update)
				{
					IMyCubeGrid main = this.GetMainCubeGrid();
					Logger.Debug($"[{grid_id}] (MAIN.{main?.EntityId ?? -1}) @ \"{main?.CustomName ?? "N/A"}\" - FIRST_UPDATE", 1);
				}
			}
			finally
			{
				sw.Stop();
				this.UpdateTimeTicks?.Enqueue(sw.Elapsed.TotalMilliseconds);
				this.LongestUpdateTime = Math.Max(this.LongestUpdateTime, sw.Elapsed.TotalMilliseconds);
				if (Monitor.IsEntered(this.UpdateLock)) Monitor.Exit(this.UpdateLock);
				//Logger.Debug($"[{grid_id}] - UPDATE_END", 4);
			}
		}

		/// <summary>
		/// Does one update of this construct's non-threadable attributes<br />
		/// This will tick all jump gate power syphons, physics colliders, and sound emitters<br />
		/// <b>Must be called from game thread</b>
		/// </summary>
		public void UpdateNonThreadable()
		{
			if (this.Closed || this.IsClosing || this.SuspensionLockUpdatingPaused) return;

			foreach (KeyValuePair<long, MyJumpGate> pair in this.JumpGates)
			{
				if (!pair.Value.Closed)
				{
					pair.Value.UpdateNonThreadable();
				}
			}
		}

		/// <summary>
		/// Clears all timing information for this construct
		/// </summary>
		public void ClearResetTimes()
		{
			if (this.Closed) return;
			this.LongestUpdateTime = -1;
			this.UpdateTimeTicks.Clear();
		}

		/// <summary>
		/// </summary>
		/// <returns>The average of all update times</returns>
		public double AverageUpdateTime60()
		{
			if (this.Closed) return -1;
			try { return (this.UpdateTimeTicks.Count == 0) ? -1 : this.UpdateTimeTicks.Average(); }
			catch (InvalidOperationException) { return -1; }
		}

		/// <summary>
		/// </summary>
		/// <returns>Returns the longest update time within the 60 tick buffer</returns>
		public double LocalLongestUpdateTime60()
		{
			if (this.Closed) return -1;
			try { return (this.UpdateTimeTicks.Count == 0) ? -1 : this.UpdateTimeTicks.Max(); }
			catch (InvalidOperationException) { return -1; }
		}

		/// <summary>
		/// </summary>
		/// <returns>True if this construct completed at least one tick</returns>
		public bool AtLeastOneUpdate()
		{
			return (this.UpdateTimeTicks?.Count ?? 0) > 0;
		}

		/// <summary>
		/// Forces this construct to throw an "ExplicitTickFailureException" on next tick
		/// </summary>
		/// <param name="count">The number of consecutive ticks to raise over</param>
		public void RaiseTickFailure(byte count)
		{
			this.CauseTickFailure = count;
		}
		#endregion
	}
}
