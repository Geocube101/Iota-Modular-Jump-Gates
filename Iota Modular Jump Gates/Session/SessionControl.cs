using IOTA.ModularJumpGates.API;
using IOTA.ModularJumpGates.JumpGates;
using IOTA.ModularJumpGates.JumpGateConstruct;
using IOTA.ModularJumpGates.Util;
using IOTA.ModularJumpGates.Util.ConcurrentCollections;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace IOTA.ModularJumpGates.Session
{
	internal partial class MyJumpGateModSession
	{
		#region Private Variables
		/// <summary>
		/// The time in game ticks after session start to update grids
		/// </summary>
		private readonly ushort FirstUpdateTimeTicks = 300;

		/// <summary>
		/// The gameplay frame counter's value 1 tick ago
		/// </summary>
		private int LastGameplayFrameCounter = 0;

		/// <summary>
		/// A list of the last 60 update times
		/// </summary>
		private ConcurrentSpinQueue<double> SessionUpdateTimeTicks = new ConcurrentSpinQueue<double>(60);
		#endregion

		#region Internal Variables
		/// <summary>
		/// The time in game ticks used to detect entity load completed
		/// </summary>
		internal ushort EntityLoadedDelayTicks { get; private set; } = 300;
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
		/// The current session component status
		/// </summary>
		public MySessionStatusEnum SessionStatus { get; private set; } = MySessionStatusEnum.OFFLINE;

		/// <summary>
		/// The instance GUID<br />
		/// Changes every time the world is loaded
		/// </summary>
		public readonly Guid InstanceID = Guid.NewGuid();

		/// <summary>
		/// Callback called when session begins unloading
		/// </summary>
		public event Action OnSessionUnload;

		/// <summary>
		/// Interface for handling API requests
		/// </summary>
		public MyAPIInterface ModAPIInterface { get; private set; } = new MyAPIInterface();

		/// <summary>
		/// The materials holder for common materials
		/// </summary>
		public MyMaterialsHolder Materials { get; private set; } = new MyMaterialsHolder();

		/// <summary>
		/// External mod information
		/// </summary>
		public MyExternalModInfo ModsList { get; private set; } = null;
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
			else if (MyNetworkInterface.IsServerLike) this.AddCubeGridToSession(cube_grid, true);
		}

		/// <summary>
		/// Event handler for when entity removed from session
		/// </summary>
		/// <param name="entity">The removed entity</param>
		private void OnEntityRemove(IMyEntity entity)
		{
			IMyCubeGrid cube_grid = entity as IMyCubeGrid;
			if (cube_grid?.Physics == null) return;
			MyJumpGateConstruct parent_grid = this.GridMap.GetValueOrDefault(cube_grid.EntityId, null);

			if (parent_grid?.Closed ?? true) return;
			else if (MyNetworkInterface.IsStandaloneMultiplayerClient && cube_grid.EntityId == parent_grid.CubeGridID)
			{
				parent_grid.Suspend(cube_grid.EntityId, true);
				Logger.Debug($"Entity \"{entity.DisplayName}\" suspended @ {entity.EntityId} >> {(entity.Closed ? "CLOSED" : "SESSION_RELOAD")}", 5);
			}
			else if (MyNetworkInterface.IsServerLike)
			{
				parent_grid.Dispose();
				Logger.Debug($"Entity \"{entity.DisplayName}\" removed from session @ {entity.EntityId} >> {(entity.Closed ? "CLOSED" : "SESSION_REMOVE")}", 5);
			}
		}

		/// <summary>
		/// Closes all pending grids and gates
		/// </summary>
		private void FlushClosureQueues()
		{
			int count = this.GridCloseRequests.Count;
			int final_count = 0;

			for (int i = 0; i < count; ++i)
			{
				MyTuple<MyJumpGateConstruct, bool, Action> data;
				if (!this.GridCloseRequests.TryDequeue(out data)) break;
				bool can_finalize = data.Item1.CanFinalizeClosure();
				long gridid = data.Item1.CubeGridID;
				data.Item1.IsClosing = true;
				Logger.Debug($"Closing grid construct {gridid} (CanFinalize={can_finalize})...", 4);

				if (!can_finalize) this.GridCloseRequests.Enqueue(data);
				else if (!data.Item1.Closed)
				{
					data.Item1.Close(data.Item2);
					data.Item3?.Invoke();
					++final_count;
					Logger.Debug($"Closed grid construct {gridid}.", 4);
				}
			}

			if (final_count > 0) Logger.Debug($"Closed {final_count} grid constructs", 3);
			count = this.GateCloseRequests.Count;
			final_count = 0;

			for (int i = 0; i < count; ++i)
			{
				MyTuple<MyJumpGate, Action> data;
				if (!this.GateCloseRequests.TryDequeue(out data)) break;
				bool can_finalize = data.Item1.CanFinalizeClosure();
				JumpGateUUID gateid = JumpGateUUID.FromJumpGate(data.Item1);
				data.Item1.IsClosing = true;
				Logger.Debug($"Closing jump gate {gateid} (CanFinalize={can_finalize})...", 4);

				if (!can_finalize) this.GateCloseRequests.Enqueue(data);
				else if (!data.Item1.Closed)
				{
					data.Item1.Close();
					data.Item2?.Invoke();
					++final_count;
					Logger.Debug($"Closed jump gate {gateid}.", 4);
				}
			}

			if (final_count > 0) Logger.Debug($"Closed {final_count} jump gates", 3);
		}

		/// <summary>
		/// Attempts to add all pending grids
		/// </summary>
		private void FlushAdditionQueues()
		{
			MyTuple<IMyCubeGrid, Event> pair;

			while (this.GridAddRequests.TryDequeue(out pair))
			{
				MyJumpGateConstruct construct = this.AddCubeGridToSession(pair.Item1);
				if (construct == null) continue;
				try { pair.Item2.Invoke(construct); }
				catch (Exception e) { Logger.Error($"Error during add construct request callback\n  ...\n[ {e.GetType().Name} ]: {e.Message}\n{e.StackTrace}\n{e.InnerException}"); }
			}
		}
		#endregion

		#region Public Methods
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
		#endregion
	}
}
