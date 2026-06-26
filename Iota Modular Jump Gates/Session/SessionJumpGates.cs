using IOTA.ModularJumpGates.CubeBlock;
using IOTA.ModularJumpGates.JumpGates;
using IOTA.ModularJumpGates.JumpGateConstruct;
using IOTA.ModularJumpGates.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;

namespace IOTA.ModularJumpGates.Session
{
	internal partial class MyJumpGateModSession
	{
		#region Private Variables
		/// <summary>
		/// Used to store all jump gates requesting closure
		/// </summary>
		private ConcurrentQueue<MyTuple<MyJumpGate, Action>> GateCloseRequests = new ConcurrentQueue<MyTuple<MyJumpGate, Action>>();

		/// <summary>
		/// Master map for storing gate detonations
		/// </summary>
		private ConcurrentDictionary<JumpGateUUID, GateDetonationInfo> GateDetonations = new ConcurrentDictionary<JumpGateUUID, GateDetonationInfo>();
		#endregion

		#region Internal Variables
		/// <summary>
		/// The number of currently outbound jump gates
		/// </summary>
		internal uint ConcurrentJumpsCounter = 0;
		#endregion

		#region Public Variables
		/// <summary>
		/// The maximum jump gate collider radius allowed based on config values
		/// </summary>
		public double MaxJumpGateColliderRadius => 2 * Math.Max(this.Configuration.DriveConfiguration.LargeDriveRaycastDistance.Value, this.Configuration.DriveConfiguration.SmallDriveRaycastDistance.Value);
		#endregion

		#region Private Methods
		/// <summary>
		/// Ticks all in progress jump gate detonations
		/// </summary>
		private void TickJumpGateDetonations()
		{
			DateTime now = DateTime.UtcNow;

			foreach (KeyValuePair<JumpGateUUID, GateDetonationInfo> pair in this.GateDetonations)
			{
				if (now >= pair.Value.NextTickTime && pair.Value.TickDestroyGate()) this.GateDetonations.Remove(pair.Key);
			}
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Queues a gate for closure on main game thread
		/// </summary>
		/// <param name="gate">The gate to close</param>
		/// <param name="callback">Callback called after gate is closed</param>
		public void CloseGate(MyJumpGate gate, Action callback = null)
		{
			if (gate == null || gate.Closed) return;
			this.GateCloseRequests.Enqueue(new MyTuple<MyJumpGate, Action>(gate, callback));
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
		/// </summary>
		/// <param name="uuid">The UUID of the gate</param>
		/// <returns>The gate's detonation info or null if not found</returns>
		public GateDetonationInfo GetGateDetonationInfo(JumpGateUUID uuid)
		{
			return this.GateDetonations.GetValueOrDefault(uuid, null);
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
		#endregion
	}
}
