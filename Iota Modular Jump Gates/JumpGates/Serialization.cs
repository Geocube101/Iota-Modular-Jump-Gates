using IOTA.ModularJumpGates.CubeBlock;
using IOTA.ModularJumpGates.Util;
using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using VRage.Game;
using VRage.Game.Entity;
using VRageMath;

namespace IOTA.ModularJumpGates.JumpGates
{
	internal partial class MyJumpGate
	{
		#region Public Methods
		/// <summary>
		/// Updates this gate data from a serialized jump gate
		/// </summary>
		/// <param name="jump_gate">The serialized jump gate data</param>
		/// <param name="force_update">Whether to ignore ID checks and deserialize</param>
		/// <param name="parent">The containing grid or null to calculate</param>
		/// <returns>Whether this gate was updated</returns>
		public bool FromSerialized(MySerializedJumpGate jump_gate, bool force_update = false, MyJumpGateConstruct parent = null)
		{
			if (this.Closed) return false;
			else if (!force_update && (this.MarkClosed || jump_gate.UUID != JumpGateUUID.FromJumpGate(this)) || jump_gate.IsClientRequest) return false;

			lock (this.UpdateLock)
			{
				if (MyNetworkInterface.IsStandaloneMultiplayerClient && (jump_gate.Closed || jump_gate.JumpGateGrid == JumpGateUUID.Empty))
				{
					this.Dispose();
					return true;
				}

				if (MyNetworkInterface.IsStandaloneMultiplayerClient)
				{
					this.MarkClosed = false;
					this.JumpGateID = jump_gate.UUID.GetJumpGate();
					this.Status = jump_gate.Status;
					this.Phase = jump_gate.Phase;
					this.ManualDetonationTimeout = jump_gate.ManualDetonationTimeout;
					this.ConstructMatrix = BoundingEllipsoidD.FromSerialized(Convert.FromBase64String(jump_gate.ConstructMatrix), 0).WorldMatrix;
					MatrixD.Invert(ref this.ConstructMatrix, out this.ConstructMatrixInv);
					this.LocalJumpNode = jump_gate.LocalJumpNode;
					this.TrueLocalJumpEllipse = BoundingEllipsoidD.FromSerialized(Convert.FromBase64String(jump_gate.LocalJumpEllipse), 0);
					this.TrueLocalJumpEllipse.Transform(ref this.ConstructMatrix, out this.TrueWorldJumpEllipse);
					this.JumpGateGrid = parent ?? MyJumpGateModSession.Instance.GetJumpGateGrid(jump_gate.JumpGateGrid.GetJumpGateGrid());
					this.DriveGridSize = jump_gate.GridSize;
					this.JumpSpaceEntities.Clear();
					if (jump_gate.JumpSpaceEntities != null) foreach (KeyValuePair<long, float> pair in jump_gate.JumpSpaceEntities) this.JumpSpaceEntities[pair.Key] = new KeyValuePair<float, MyEntity>(pair.Value, (MyEntity) MyAPIGateway.Entities.GetEntityById(pair.Key));
					this.MPGateColliderStatus = jump_gate.ColliderStatus;
					this.WormholeStartTime = jump_gate.WormholeStartTime;
					this.UpdateDriveIntersectNodes(jump_gate.IntersectNodes);
					if (this.JumpGateGrid == null) throw new NullReferenceException("Jump gate host construct is null");
				}

				this.Controller = (jump_gate.Controller == JumpGateUUID.Empty) ? null : this.JumpGateGrid.GetController(jump_gate.Controller.GetBlock());
				this.RemoteAntenna = (jump_gate.RemoteAntenna == JumpGateUUID.Empty) ? null : this.JumpGateGrid.GetRemoteAntenna(jump_gate.RemoteAntenna.GetBlock());
				this.ServerAntenna = (jump_gate.ServerAntenna == JumpGateUUID.Empty) ? null : this.JumpGateGrid.GetServerAntenna(jump_gate.ServerAntenna.GetBlock());
				return true;
			}
		}

		/// <summary>
		/// Serializes this gate's data
		/// </summary>
		/// <param name="as_client_request">If true, this will be an update request to server</param>
		/// <returns>The serialized jump gate data</returns>
		public MySerializedJumpGate ToSerialized(bool as_client_request)
		{
			if (as_client_request)
			{
				return new MySerializedJumpGate
				{
					UUID = JumpGateUUID.FromJumpGate(this),
					IsClientRequest = true,
				};
			}
			else
			{
				return new MySerializedJumpGate
				{
					UUID = JumpGateUUID.FromJumpGate(this),
					Closed = this.Closed,
					Status = this.Status,
					Phase = this.Phase,
					ColliderStatus = this.JumpSpaceColliderStatus(),
					LocalJumpNode = this.LocalJumpNode,
					LocalJumpEllipse = (this.Closed) ? null : Convert.ToBase64String(this.TrueLocalJumpEllipse.ToSerialized()),
					Controller = (this.Controller == null) ? JumpGateUUID.Empty : JumpGateUUID.FromBlock(this.Controller),
					RemoteAntenna = (this.RemoteAntenna == null) ? JumpGateUUID.Empty : JumpGateUUID.FromBlock(this.RemoteAntenna),
					ServerAntenna = (this.ServerAntenna == null) ? JumpGateUUID.Empty : JumpGateUUID.FromBlock(this.ServerAntenna),
					JumpGateGrid = (this.Closed) ? JumpGateUUID.Empty : JumpGateUUID.FromJumpGateGrid(this.JumpGateGrid),
					IntersectNodes = (this.Closed) ? null : this.LocalDriveIntersectNodes,
					GridSize = (this.Closed) ? MyCubeSize.Large : this.CubeGridSize(),
					ConstructMatrix = (this.Closed) ? null : Convert.ToBase64String(new BoundingEllipsoidD(ref this.ConstructMatrix).ToSerialized()),
					JumpSpaceEntities = (MyNetworkInterface.IsMultiplayerServer && !this.Closed) ? this.JumpSpaceEntities.Select((pair) => new KeyValuePair<long, float>(pair.Key, pair.Value.Key)).ToImmutableDictionary() : null,
					ManualDetonationTimeout = this.ManualDetonationTimeout,
					WormholeStartTime = this.WormholeStartTime,
					IsClientRequest = false,
				};
			}
		}
		#endregion
	}

	/// <summary>
	/// Class for holding serialized MyJumpGate data
	/// </summary>
	[ProtoContract]
	internal class MySerializedJumpGate
	{
		/// <summary>
		/// The gate's JumpGateUUID
		/// </summary>
		[ProtoMember(1)]
		public JumpGateUUID UUID;

		/// <summary>
		/// Whether this gate is closed or marked for close
		/// </summary>
		[ProtoMember(2)]
		public bool Closed;

		/// <summary>
		/// This gate's status
		/// </summary>
		[ProtoMember(3)]
		public MyJumpGateStatus Status;

		/// <summary>
		/// This gate's phase
		/// </summary>
		[ProtoMember(4)]
		public MyJumpGatePhase Phase;

		/// <summary>
		/// This gate's collider status
		/// </summary>
		[ProtoMember(5)]
		public MyGateColliderStatus ColliderStatus;

		/// <summary>
		/// This jump gate's self destruct time in game ticks or -1 if not armed
		/// </summary>
		[ProtoMember(6)]
		public int ManualDetonationTimeout;

		/// <summary>
		/// This gate's local jump node
		/// </summary>
		[ProtoMember(7)]
		public Vector3D LocalJumpNode;

		/// <summary>
		/// This gate's local jump ellipse as a base64 string
		/// </summary>
		[ProtoMember(8)]
		public string LocalJumpEllipse;

		/// <summary>
		/// This gate's attached controller or an empty Guid
		/// </summary>
		[ProtoMember(9)]
		public JumpGateUUID Controller;

		/// <summary>
		/// This gate's attached remote antenna or an empty Guid
		/// </summary>
		[ProtoMember(10)]
		public JumpGateUUID RemoteAntenna;

		/// <summary>
		/// This gate's attached server antenna or an empty Guid
		/// </summary>
		[ProtoMember(11)]
		public JumpGateUUID ServerAntenna;

		/// <summary>
		/// This gate's attached grid construct
		/// </summary>
		[ProtoMember(12)]
		public JumpGateUUID JumpGateGrid;

		/// <summary>
		/// A list of this gate's drive intersect nodes
		/// </summary>
		[ProtoMember(13)]
		public ImmutableList<Vector3D> IntersectNodes;

		/// <summary>
		/// This gate's grid size
		/// </summary>
		[ProtoMember(14)]
		public MyCubeSize GridSize;

		/// <summary>
		/// This gate's construct matrix
		/// </summary>
		[ProtoMember(15)]
		public string ConstructMatrix;

		/// <summary>
		/// A map of this gate's jump space entities with their mass
		/// </summary>
		[ProtoMember(16)]
		public ImmutableDictionary<long, float> JumpSpaceEntities;

		/// <summary>
		/// The time at which this gate opened as a wormhole
		/// </summary>
		[ProtoMember(17)]
		public DateTime? WormholeStartTime;

		/// <summary>
		/// If true, this data should be used by server to identify gate and send updated data
		/// </summary>
		[ProtoMember(18)]
		public bool IsClientRequest;
	}

	internal class MyJumpGateControlObject
	{
		public readonly bool IsController;
		public readonly byte RemoteAntennaChannel;
		public readonly MyJumpGateController Controller;
		public readonly MyJumpGateRemoteAntenna RemoteAntenna;

		public bool IsWorking => (this.IsController) ? this.Controller.IsWorking : this.RemoteAntenna.IsWorking;
		public long BlockID => (this.IsController) ? this.Controller.BlockID : this.RemoteAntenna.BlockID;
		public MyJumpGateController.MyControllerBlockSettingsStruct BlockSettings => (this.IsController) ? this.Controller.BlockSettings : ((this.RemoteAntennaChannel == 0xFF) ? null : this.RemoteAntenna.BlockSettings.BaseControllerSettings[this.RemoteAntennaChannel]);
		public MatrixD WorldMatrix => (this.IsController) ? this.Controller.WorldMatrix : this.RemoteAntenna.WorldMatrix;
		public MyJumpGateConstruct JumpGateGrid => (this.IsController) ? this.Controller.JumpGateGrid : this.RemoteAntenna.JumpGateGrid;

		public MyJumpGateControlObject(MyJumpGateController controller)
		{
			this.IsController = true;
			this.RemoteAntennaChannel = 0xFF;
			this.Controller = controller;
			this.RemoteAntenna = null;
		}

		public MyJumpGateControlObject(MyJumpGateRemoteAntenna antenna, byte channel)
		{
			this.IsController = false;
			this.RemoteAntennaChannel = channel;
			this.Controller = null;
			this.RemoteAntenna = antenna;
		}

		/// <summary>
		/// </summary>
		/// <param name="steam_id">The player's steam ID to check</param>
		/// <returns>True if the caller's faction can jump to this gate</returns>
		public bool IsFactionRelationValid(ulong steam_id)
		{
			return (this.IsController) ? this.Controller.IsFactionRelationValid(steam_id) : this.RemoteAntenna.IsFactionRelationValid(this.RemoteAntennaChannel, steam_id);
		}

		/// <summary>
		/// </summary>
		/// <param name="player_identity">The player's identiy to check</param>
		/// <returns>True if the caller's faction can jump to this gate</returns>
		public bool IsFactionRelationValid(long player_identity)
		{
			return (this.IsController) ? this.Controller.IsFactionRelationValid(player_identity) : this.RemoteAntenna.IsFactionRelationValid(this.RemoteAntennaChannel, player_identity);
		}
	}
}
