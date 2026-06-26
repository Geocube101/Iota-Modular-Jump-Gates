using IOTA.ModularJumpGates.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOTA.ModularJumpGates.JumpGateConstruct
{
	internal partial class MyJumpGateConstruct
	{
		#region Private Variables
		/// <summary>
		/// The ID of the CubeGrid this construct is bound to when suspended
		/// </summary>
		private long SuspendedCubeGridID;

		/// <summary>
		/// List of subgrid IDs used whilst construct is suspended
		/// </summary>
		private List<long> SuspendedCubeGridIDs = new List<long>();
		#endregion

		#region Public Variables
		/// <summary>
		/// Whether this construct is suspended from updates<br />
		/// Only used on muliplayer clients when grid is not in scene
		/// </summary>
		public bool IsSuspended { get; private set; } = false;

		/// <summary>
		/// Whether this construct will receive updates<br />
		/// Used whilst suspending a grid
		/// </summary>
		public bool SuspensionLockUpdatingPaused { get; private set; } = false;
		#endregion

		#region Private Methods
		private void SuspendConstruct()
		{
			if (this.CubeGrids != null)
			{
				this.SuspendedCubeGridIDs.AddRange(this.CubeGrids.Keys);
				this.CubeGrids.Clear();
			}

			this.CubeGrid = null;
		}
		#endregion

		#region Public Methods

		/// <summary>
		/// Marks this construct as suspended<br />
		/// <i>Has no effect on server</i>
		/// </summary>
		/// <param name="fallback_id">Fallback grid ID used whilst construct is suspended</param>
		/// <param name="do_network_sync">Whether to re-download serialized construct from server (updates will be paused whilst awaiting response)</param>
		public void Suspend(long fallback_id, bool do_network_sync)
		{
			if (this.Closed || MyNetworkInterface.IsMultiplayerServer) return;
			this.IsSuspended = true;
			this.SuspendedCubeGridID = this.CubeGrid?.EntityId ?? fallback_id;
			this.SuspensionLockUpdatingPaused = do_network_sync;
			if (do_network_sync) MyJumpGateModSession.Instance.RequestGridDownload(this.SuspendedCubeGridID);
		}

		/// <summary>
		/// Clears this construct from suspension<br />
		/// Construct will continue updates in next tick
		/// </summary>
		/// <param name="serialized">The serialized construct to update from</param>
		/// <param name="update_time">The new optional last update time for this construct</param>
		/// <returns>Whether this grid was resumed</returns>
		public bool Persist(MySerializedJumpGateConstruct serialized, DateTime? update_time = null)
		{
			if (this.Closed || !this.IsSuspended) return true;
			bool resumed = this.FromSerialized(serialized);
			this.IsSuspended = !resumed;

			if (resumed)
			{
				this.LastUpdateDateTimeUTC = update_time ?? DateTime.UtcNow;
				this.SuspendedCubeGridIDs.Clear();
			}

			return resumed;
		}
		#endregion
	}
}
