using IOTA.ModularJumpGates.Util;
using Sandbox.Game;
using System;
using System.Collections.Generic;
using VRage;
using VRageMath;

namespace IOTA.ModularJumpGates.JumpGates
{
	internal partial class MyJumpGate : IEquatable<MyJumpGate>
	{
		#region Public Static Operators
		/// <summary>
		/// Overloads equality operator "==" to check equality
		/// </summary>
		/// <param name="a">The first MyJumpGate operand</param>
		/// <param name="b">The second MyJumpGate operand</param>
		/// <returns>Equality</returns>
		public static bool operator ==(MyJumpGate a, MyJumpGate b)
		{
			if (object.ReferenceEquals(a, b)) return true;
			else if (object.ReferenceEquals(a, null)) return object.ReferenceEquals(b, null);
			else if (object.ReferenceEquals(b, null)) return object.ReferenceEquals(a, null);
			else return a.Equals(b);
		}

		/// <summary>
		/// Overloads inequality operator "!=" to check inequality
		/// </summary>
		/// <param name="a">The first MyJumpGate operand</param>
		/// <param name="b">The second MyJumpGate operand</param>
		/// <returns>Inequality</returns>
		public static bool operator !=(MyJumpGate a, MyJumpGate b)
		{
			return !(a == b);
		}
		#endregion

		#region Public Static Methods
		/// <summary>
		/// Gets the associated desription for the specified failure reason
		/// </summary>
		/// <param name="reason">The failure reason</param>
		/// <returns>The associated description or null if no description available</returns>
		public static string GetFailureDescription(MyJumpFailReason reason)
		{
			switch (reason)
			{
				case MyJumpFailReason.NONE:
				case MyJumpFailReason.SUCCESS:
				case MyJumpFailReason.IN_PROGRESS:
					return null;
				default:
					return MyTexts.GetString($"JumpFailReason_{reason}");
			}
		}

		/// <summary>
		/// Gets the associated sound name for the specified failure reason
		/// </summary>
		/// <param name="reason">The failure reason</param>
		/// <param name="is_init_phase">Whether the failure occured during jump initialization</param>
		/// <returns>The associated sound name or null if no sound name available</returns>
		public static string GetFailureSound(MyJumpFailReason reason, bool is_init_phase)
		{
			switch (reason)
			{
				case MyJumpFailReason.SRC_INVALID:
					return (is_init_phase) ? "IOTA.ComputerSFX.InitFailedSourceInvalid" : "IOTA.ComputerSFX.JumpFailedSourceInvalid";
				case MyJumpFailReason.CONTROLLER_NOT_CONNECTED:
					return (is_init_phase) ? "IOTA.ComputerSFX.InitFailedSourceControllerOffline" : "IOTA.ComputerSFX.JumpFailedSourceControllerOffline";
				case MyJumpFailReason.SRC_DISABLED:
					return (is_init_phase) ? "IOTA.ComputerSFX.InitFailedSourceOffline" : "IOTA.ComputerSFX.JumpFailedSourceOffline";
				case MyJumpFailReason.SRC_NOT_CONFIGURED:
					return (is_init_phase) ? "IOTA.ComputerSFX.InitFailedSourceNotConfigured" : "IOTA.ComputerSFX.JumpFailedSourceNotConfigured";
				case MyJumpFailReason.SRC_BUSY:
					return (is_init_phase) ? "IOTA.ComputerSFX.InitFailedSourceBusy" : null;
				case MyJumpFailReason.SRC_ROUTING_DISABLED:
					return (is_init_phase) ? "IOTA.ComputerSFX.InitFailedSourceRoutingOffline" : null;
				case MyJumpFailReason.SRC_INBOUND_ONLY:
					return (is_init_phase) ? "IOTA.ComputerSFX.InitFailedSourceInboundOnly" : null;
				case MyJumpFailReason.SRC_ROUTING_CHANGED:
					return (is_init_phase) ? null : "IOTA.ComputerSFX.JumpFailedSourceRoutingChanged";
				case MyJumpFailReason.SRC_CLOSED:
					return (is_init_phase) ? null : "IOTA.ComputerSFX.JumpFailedSourceClosed";

				case MyJumpFailReason.NULL_DESTINATION:
					return (is_init_phase) ? "IOTA.ComputerSFX.InitFailedNoDestination" : null;
				case MyJumpFailReason.DESTINATION_UNAVAILABLE:
					return (is_init_phase) ? "IOTA.ComputerSFX.InitFailedNullDestination" : "IOTA.ComputerSFX.JumpFailedNullDestination";
				case MyJumpFailReason.NULL_ANIMATION:
					return (is_init_phase) ? "IOTA.ComputerSFX.InitFailedNullAnimation" : null;
				case MyJumpFailReason.SUBSPACE_BUSY:
					return (is_init_phase) ? "IOTA.ComputerSFX.InitFailedSourceSubspaceBusy" : null;
				case MyJumpFailReason.RADIO_LINK_FAILED:
					return (is_init_phase) ? "IOTA.ComputerSFX.InitFailedTargetConnectionFailed" : null;
				case MyJumpFailReason.BEACON_LINK_FAILED:
					return (is_init_phase) ? "IOTA.ComputerSFX.InitFailedTargetBeaconConnectionFailed" : null;
				case MyJumpFailReason.JUMP_SPACE_TRANSPOSED:
					return (is_init_phase) ? "IOTA.ComputerSFX.InitFailedJumpSpaceTransposed" : "IOTA.ComputerSFX.JumpFailedJumpSpaceTransposed";
				case MyJumpFailReason.CANCELLED:
					return (is_init_phase) ? null : "IOTA.ComputerSFX.JumpFailedJumpOverridden";
				case MyJumpFailReason.UNKNOWN_ERROR:
					return (is_init_phase) ? null : "IOTA.ComputerSFX.JumpFailedUnknownError";
				case MyJumpFailReason.NO_ENTITIES:
					return (is_init_phase) ? null : "IOTA.ComputerSFX.JumpFailedNoEntities";
				case MyJumpFailReason.NO_ENTITIES_JUMPED:
					return (is_init_phase) ? null : "IOTA.ComputerSFX.JumpFailedNoEntitiesJumped";
				case MyJumpFailReason.INSUFFICIENT_POWER:
					return (is_init_phase) ? null : "IOTA.ComputerSFX.JumpFailedInsufficientPower";
				case MyJumpFailReason.CROSS_SERVER_JUMP:
					return (is_init_phase) ? null : "IOTA.ComputerSFX.JumpFailedCrossServerUnavailabe";

				case MyJumpFailReason.DST_UNAVAILABLE:
					return (is_init_phase) ? "IOTA.ComputerSFX.InitFailedTargetUnavailable" : "IOTA.ComputerSFX.JumpFailedTargetUnavailable";
				case MyJumpFailReason.DST_ROUTING_DISABLED:
					return (is_init_phase) ? "IOTA.ComputerSFX.InitFailedTargetRoutingOffline" : null;
				case MyJumpFailReason.DST_FORBIDDEN:
					return (is_init_phase) ? "IOTA.ComputerSFX.InitFailedTargetForbidden" : "IOTA.ComputerSFX.JumpFailedTargetForbidden";
				case MyJumpFailReason.DST_DISABLED:
					return (is_init_phase) ? "IOTA.ComputerSFX.InitFailedTargetOffline" : "IOTA.ComputerSFX.JumpFailedTargetOffline";
				case MyJumpFailReason.DST_NOT_CONFIGURED:
					return (is_init_phase) ? "IOTA.ComputerSFX.InitFailedTargetNotConfigured" : "IOTA.ComputerSFX.JumpFailedTargetNotConfigured";
				case MyJumpFailReason.DST_BUSY:
					return (is_init_phase) ? "IOTA.ComputerSFX.InitFailedTargetBusy" : null;
				case MyJumpFailReason.DST_RADIO_CONNECTION_INTERRUPTED:
					return (is_init_phase) ? null : "IOTA.ComputerSFX.JumpFailedTargetConnectionInterrupted";
				case MyJumpFailReason.DST_BEACON_CONNECTION_INTERRUPTED:
					return (is_init_phase) ? null : "IOTA.ComputerSFX.JumpFailedTargetBeaconConnectionInterrupted";
				case MyJumpFailReason.DST_ROUTING_CHANGED:
					return (is_init_phase) ? null : "IOTA.ComputerSFX.JumpFailedTargetRoutingChanged";
				case MyJumpFailReason.DST_VOIDED:
					return (is_init_phase) ? null : "IOTA.ComputerSFX.JumpFailedTargetVoided";
				case MyJumpFailReason.BEACON_BLOCKED:
					return (is_init_phase) ? null : "IOTA.ComputerSFX.JumpFailedTargetBeaconBlocked";
				default:
					return null;
			}
		}
		#endregion

		#region Constructors
		/// <summary>
		/// Creates a new jump gate
		/// </summary>
		/// <param name="grid">The parent construct</param>
		/// <param name="id">The jump gate ID</param>
		/// <param name="jump_node">The jump node world coordinate</param>
		/// <param name="nodes">The drive intersect nodes</param>
		/// <exception cref="ArgumentException"></exception>
		public MyJumpGate(MyJumpGateConstruct grid, long id, ref Vector3D jump_node, IEnumerable<Vector3D> nodes)
		{
			if (grid == null) throw new ArgumentException("MyJumpGate CONSTRUCTOR[MyJumpGateConstruct grid] was NULL");
			this.ConstructMatrix = grid.CubeGrid.WorldMatrix;
			this.JumpGateGrid = grid;
			this.JumpGateID = id;
			this.WorldJumpNode = jump_node;

			lock (this.DriveIntersectNodesMutex)
			{
				foreach (Vector3D node in nodes)
				{
					bool add = true;
					Vector3D node1 = MyJumpGateModSession.WorldVectorToLocalVectorP(ref this.ConstructMatrix, node);

					foreach (Vector3D node2 in this.InnerDriveIntersectNodes)
					{
						if (Vector3D.Distance(node1, node2) < 1)
						{
							add = false;
							break;
						}
					}

					if (add) this.InnerDriveIntersectNodes.Add(node1);
				}
			}

			Logger.Debug($"CREATE_JUMP_GATE: MyJumpGateConstruct={(this.JumpGateGrid?.CubeGrid?.EntityId.ToString() ?? "N/A")} GRID_CLOSED={(grid?.Closed.ToString() ?? "N/A")} JumpGateID={this.JumpGateID} DriveIntersectNodes={this.InnerDriveIntersectNodes.Count}", 3);

			if (this.JumpGateGrid != null && !grid.Closed && this.JumpGateID >= 0 && this.InnerDriveIntersectNodes.Count >= 1)
			{
				this.Status = MyJumpGateStatus.IDLE;
				this.Phase = MyJumpGatePhase.IDLE;
				this.PowerSyphon = new GridPowerSyphon(this);

				if (MyJumpGateModSession.Instance.Network.Registered)
				{
					MyJumpGateModSession.Instance.Network.On(MyPacketTypeEnum.JUMP_GATE_JUMP, this.OnNetworkJumpGateJump);
					MyJumpGateModSession.Instance.Network.On(MyPacketTypeEnum.JUMP_GATE_JUMP_POLL, this.OnNetworkJumpGateJumpPoll);
					MyJumpGateModSession.Instance.Network.On(MyPacketTypeEnum.UPDATE_JUMP_GATE, this.OnNetworkJumpGateUpdate);
					MyJumpGateModSession.Instance.Network.On(MyPacketTypeEnum.AUTOACTIVATE, this.OnNetworkAutoActivationAlert);
					MyJumpGateModSession.Instance.Network.On(MyPacketTypeEnum.SHEARWARNING, this.OnNetworkShearBlockAlert);
					MyJumpGateModSession.Instance.Network.On(MyPacketTypeEnum.GATE_DEBUG, this.OnGateDebug);
					MyJumpGateModSession.Instance.Network.On(MyPacketTypeEnum.GATE_DETONATE, this.OnGateDetonate);
				}

				if (MyNetworkInterface.IsServerLike) MyExplosions.OnExplosion += this.OnExplosion;
			}
			else this.Dispose();
		}

		/// <summary>
		/// Creates a new jump gate from a serialized grid
		/// </summary>
		/// <param name="serialized">The serialized jump gate data</param>
		/// <param name="parent">The containing grid or null to calculate</param>
		public MyJumpGate(MySerializedJumpGate serialized, MyJumpGateConstruct parent = null)
		{
			this.FromSerialized(serialized, true, parent);

			if (this.JumpGateGrid != null && !this.JumpGateGrid.Closed && this.JumpGateID >= 0 && this.InnerDriveIntersectNodes.Count >= 1)
			{
				this.Status = MyJumpGateStatus.IDLE;
				this.Phase = MyJumpGatePhase.IDLE;
				this.PowerSyphon = new GridPowerSyphon(this);

				if (MyJumpGateModSession.Instance.Network.Registered)
				{
					MyJumpGateModSession.Instance.Network.On(MyPacketTypeEnum.JUMP_GATE_JUMP, this.OnNetworkJumpGateJump);
					MyJumpGateModSession.Instance.Network.On(MyPacketTypeEnum.JUMP_GATE_JUMP_POLL, this.OnNetworkJumpGateJumpPoll);
					MyJumpGateModSession.Instance.Network.On(MyPacketTypeEnum.UPDATE_JUMP_GATE, this.OnNetworkJumpGateUpdate);
					MyJumpGateModSession.Instance.Network.On(MyPacketTypeEnum.AUTOACTIVATE, this.OnNetworkAutoActivationAlert);
					MyJumpGateModSession.Instance.Network.On(MyPacketTypeEnum.SHEARWARNING, this.OnNetworkShearBlockAlert);
					MyJumpGateModSession.Instance.Network.On(MyPacketTypeEnum.GATE_DEBUG, this.OnGateDebug);
					MyJumpGateModSession.Instance.Network.On(MyPacketTypeEnum.GATE_DETONATE, this.OnGateDetonate);
				}
			}
			else this.Dispose();
		}

		~MyJumpGate()
		{
			try
			{
				this.Close();
			}
			catch { }
		}
		#endregion

		#region "object" Methods
		/// <summary>
		/// Checks if this MyJumpGate equals another
		/// </summary>
		/// <param name="other">The object to check</param>
		/// <returns>Equality</returns>
		public override bool Equals(object other)
		{
			if (other == null || !(other is MyJumpGate)) return false;
			else return this.Equals((MyJumpGate) other);
		}

		/// <summary>
		/// The hashcode for this object
		/// </summary>
		/// <returns>The hashcode of this object</returns>
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
		#endregion
	}
}
