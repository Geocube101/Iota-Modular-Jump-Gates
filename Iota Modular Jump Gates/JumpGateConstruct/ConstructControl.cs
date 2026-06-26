using IOTA.ModularJumpGates.CubeBlock;
using IOTA.ModularJumpGates.JumpGates;
using IOTA.ModularJumpGates.Session;
using IOTA.ModularJumpGates.Util;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI;

namespace IOTA.ModularJumpGates.JumpGateConstruct
{
	internal partial class MyJumpGateConstruct
	{
		#region Public Variables
		/// <summary>
		/// Whether this construct is closed
		/// </summary>
		public bool Closed { get; private set; } = false;

		/// <summary>
		/// Whether this construct is marked for close
		/// </summary>
		public bool MarkClosed { get; private set; } = false;

		/// <summary>
		/// Whether this construct is closing
		/// </summary>
		public bool IsClosing = false;

		/// <summary>
		/// Whether this construct is fully initialized<br />
		/// True when all gates are constructed and resources initialized
		/// </summary>
		public bool FullyInitialized { get; private set; } = false;
		#endregion

		#region Public Methods
		/// <summary>
		/// Closes this construct, unregisters handlers, and frees all resources<br />
		/// If client: Does nothing<br />
		/// To be called only by session
		/// </summary>
		/// <param name="override_client">If true, will close grid on clients instead of doing nothing</param>
		public void Close(bool override_client = false)
		{
			if (this.Closed || (!override_client && MyNetworkInterface.IsStandaloneMultiplayerClient && this.CubeGrid != null && MyJumpGateModSession.Instance.HasCubeGrid(this.CubeGridID))) return;

			lock (this.UpdateLock)
			{
				MyGridInvalidationReason reason = this.GetInvalidationReason();
				this.SendNetworkGridUpdate(true);
				string name = this.CubeGrid?.CustomName ?? "N/A";

				foreach (KeyValuePair<long, MyJumpGate> pair in this.JumpGates)
				{
					if (!pair.Value.Closed)
					{
						pair.Value.Dispose();
						MyJumpGateModSession.Instance.CloseGate(pair.Value);
					}
				}

				foreach (KeyValuePair<long, IMyCubeGrid> pair in this.CubeGrids) this.OnGridRemoved(pair.Value);
				this.NextJumpGateID = 0;
				this.Release();

				this.ClosedJumpGateIDs.Clear();
				this.JumpGates.Clear();
				this.JumpGateBlocks.Clear();
				this.CubeGrids.Clear();
				this.DriveCombinations?.Clear();
				this.GridBlocks.Clear();
				this.CommLinkedGridsPoll.Clear();
				this.CommBlocks.Clear();
				this.SuspendedCubeGridIDs.Clear();
				using (this.CommLinkedLock.WithWriter()) this.CommLinkedGrids?.Clear();

				this.ClosedJumpGateIDs = null;
				this.JumpGates = null;
				this.JumpGateBlocks = null;
				this.UpdateTimeTicks = null;
				this.CubeGrids = null;
				this.DriveCombinations = null;
				this.GridBlocks = null;
				this.CommLinkedGridsPoll = null;
				this.CommLinkedGrids = null;
				this.CommBlocks = null;
				this.SuspendedCubeGridIDs = null;

				this.BatchingGate = null;
				this.CubeGrid = null;
				this.MarkClosed = true;
				this.IsClosing = false;
				this.Closed = true;

				Logger.Debug($"Jump Gate Grid \"{name}\" ({this.CubeGridID}) Closed - {reason}; SESSION_STATUS={MyJumpGateModSession.Instance.SessionStatus}", 1);
			}
		}

		/// <summary>
		/// Markes this construct for close or closes immediatly if session is not running
		/// </summary>
		public void Dispose()
		{
			if (this.Closed || this.MarkClosed || MyNetworkInterface.IsStandaloneMultiplayerClient) return;
			this.MarkClosed = true;
			foreach (KeyValuePair<long, MyJumpGate> pair in this.JumpGates) pair.Value.Dispose();
			if (MyJumpGateModSession.Instance.SessionStatus == MySessionStatusEnum.UNLOADING) MyJumpGateModSession.Instance.CloseGrid(this);
		}

		/// <summary>
		/// Destroys all grids attached to this construct
		/// </summary>
		public void Destroy()
		{
			if (this.Closed) return;
			foreach (KeyValuePair<long, IMyCubeGrid> pair in this.CubeGrids) pair.Value.Close();
		}

		/// <summary>
		/// Whether this grid is valid
		/// </summary>
		/// <returns>True if valid</returns>
		public bool IsValid()
		{
			return this.GetInvalidationReason() == MyGridInvalidationReason.NONE;
		}

		/// <summary>
		/// </summary>
		/// <returns>Whether this construct can be safely closed</returns>
		public bool CanFinalizeClosure()
		{
			return this.Closed || this.JumpGates.All((pair) => pair.Value.CanFinalizeClosure());
		}

		/// <summary>
		/// Gets the reason this construct is invalid
		/// </summary>
		/// <returns>The invalidation reason or InvalidationReason.NONE</returns>
		public MyGridInvalidationReason GetInvalidationReason()
		{
			if (this.Closed || this.MarkClosed) return MyGridInvalidationReason.CLOSED;
			else if (this.CubeGrid == null) return (this.IsSuspended) ? MyGridInvalidationReason.NONE : MyGridInvalidationReason.NULL_GRID;
			else if (this.CubeGrids.Count == 0) return MyGridInvalidationReason.INSUFFICIENT_GRIDS;
			else if (this.GetCubeGridPhysics() == null) return MyGridInvalidationReason.NULL_PHYSICS;
			else return MyGridInvalidationReason.NONE;
		}

		/// <summary>
		/// Attemps to drain the specified power from all on-construct capacitors
		/// </summary>
		/// <param name="power_mw">The amount of drain to syphon in MegaWatts</param>
		/// <returns>The remaining power after syphon</returns>
		public double SyphonConstructCapacitorPower(double power_mw)
		{
			if (this.Closed || power_mw <= 0) return 0;
			List<MyJumpGateCapacitor> capacitors = new List<MyJumpGateCapacitor>();
			foreach (MyJumpGateCapacitor capacitor in this.GetAttachedJumpGateCapacitors()) if (capacitor.IsWorking && capacitor.StoredChargeMW > 0 && capacitor.BlockSettings.RechargeEnabled) capacitors.Add(capacitor);
			if (capacitors.Count == 0) return power_mw;
			double power_per = power_mw / capacitors.Count;
			power_mw = 0;
			foreach (MyJumpGateCapacitor capacitor in capacitors) power_mw += capacitor.DrainStoredCharge(power_per);
			return power_mw;
		}

		/// <summary>
		/// Calculates the total available power stored within construct capacitors and drives for the specified jump gate
		/// </summary>
		/// <param name="jump_gate_id">The jump gate to calculate for</param>
		/// <returns>Total available instant power in Mega-Watts</returns>
		public double CalculateTotalAvailableInstantPower(long jump_gate_id)
		{
			if (this.Closed) return 0;
			double total_power = 0;
			MyJumpGateDrive drive;
			MyJumpGateCapacitor capacitor;

			foreach (KeyValuePair<long, MyCubeBlockBase> pair in this.JumpGateBlocks)
			{
				if (pair.Value is MyJumpGateDrive && (drive = pair.Value as MyJumpGateDrive).JumpGateID == jump_gate_id && drive.IsWorking) total_power += drive.StoredChargeMW;
				else if (pair.Value is MyJumpGateCapacitor && (capacitor = pair.Value as MyJumpGateCapacitor).IsWorking && capacitor.BlockSettings.RechargeEnabled) total_power += capacitor.StoredChargeMW;
			}

			return total_power;
		}

		/// <summary>
		/// Calculates the total possible power stored within construct capacitors and drives for the specified jump gate
		/// </summary>
		/// <param name="jump_gate_id">The jump gate to calculate for</param>
		/// <returns>Total possible instant power in Mega-Watts</returns>
		public double CalculateTotalPossibleInstantPower(long jump_gate_id)
		{
			if (this.Closed) return 0;
			double total_power = 0;
			MyJumpGateDrive drive;
			MyJumpGateCapacitor capacitor;

			foreach (KeyValuePair<long, MyCubeBlockBase> pair in this.JumpGateBlocks)
			{
				if (pair.Value is MyJumpGateDrive && (drive = pair.Value as MyJumpGateDrive).JumpGateID == jump_gate_id && drive.IsWorking) total_power += drive.Configuration.MaxDriveChargeMW;
				else if (pair.Value is MyJumpGateCapacitor && (capacitor = pair.Value as MyJumpGateCapacitor).IsWorking && capacitor.BlockSettings.RechargeEnabled) total_power += capacitor.Configuration.MaxCapacitorChargeMW;
			}

			return total_power;
		}

		/// <summary>
		/// Calculates the total possible power stored within construct capacitors and drives for the specified jump gate<br />
		/// This method ignores block settings and whether block is working
		/// </summary>
		/// <param name="jump_gate_id">The jump gate to calculate for</param>
		/// <returns>Total max possible instant power in Mega-Watts</returns>
		public double CalculateTotalMaxPossibleInstantPower(long jump_gate_id)
		{
			if (this.Closed) return 0;
			double total_power = 0;
			MyJumpGateDrive drive;

			foreach (KeyValuePair<long, MyCubeBlockBase> pair in this.JumpGateBlocks)
			{
				if (pair.Value is MyJumpGateDrive && (drive = pair.Value as MyJumpGateDrive).JumpGateID == jump_gate_id) total_power += drive.Configuration.MaxDriveChargeMW;
				else if (pair.Value is MyJumpGateCapacitor) total_power += (pair.Value as MyJumpGateCapacitor).Configuration.MaxCapacitorChargeMW;
			}

			return total_power;
		}
		#endregion
	}
}
