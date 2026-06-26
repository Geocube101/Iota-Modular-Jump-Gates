using IOTA.ModularJumpGates.JumpGates;
using IOTA.ModularJumpGates.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using VRage.Game.Entity;
using VRageMath;

namespace IOTA.ModularJumpGates.Session
{
	internal partial class MyJumpGateModSession
	{
		#region Private Variables
		/// <summary>
		/// Master map for storing entity warps
		/// </summary>
		private ConcurrentDictionary<long, EntityWarpInfo> EntityWarps = new ConcurrentDictionary<long, EntityWarpInfo>();
		#endregion

		#region Private Methods
		/// <summary>
		/// Ticks all in progress entity warps
		/// </summary>
		private void TickEntityWarps()
		{
			foreach (KeyValuePair<long, EntityWarpInfo> pair in this.EntityWarps)
			{
				if (pair.Value.Update())
				{
					pair.Value.Close();
					this.EntityWarps.Remove(pair.Key);
				}
			}
		}
		#endregion

		#region Public Methods
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
		#endregion
	}
}
