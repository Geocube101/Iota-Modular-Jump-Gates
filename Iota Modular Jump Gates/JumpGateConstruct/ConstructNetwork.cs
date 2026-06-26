using IOTA.ModularJumpGates.JumpGates;
using IOTA.ModularJumpGates.Session;
using IOTA.ModularJumpGates.Util;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IOTA.ModularJumpGates.JumpGateConstruct
{
	internal partial class MyJumpGateConstruct
	{
		#region Private Variables
		/// <summary>
		/// The last time this grid was updated (in timer ticks ~100ns)
		/// </summary>
		private ulong LastUpdateTime = 0;
		#endregion

		#region Public Variables
		/// <summary>
		/// Whether this construct should be synced<br/>
		/// If true, will be synced on next component tick
		/// </summary>
		public bool IsDirty { get; private set; } = false;

		/// <summary>
		/// The UTC date time representation of MyJumpGateConstruct.LastUpdateTime
		/// </summary>
		public DateTime LastUpdateDateTimeUTC
		{
			get
			{
				return (DateTime.MinValue.ToUniversalTime() + new TimeSpan((long) this.LastUpdateTime));
			}

			set
			{
				if (this.Closed) return;
				this.LastUpdateTime = (ulong) (value.ToUniversalTime() - DateTime.MinValue).Ticks;
				foreach (KeyValuePair<long, MyJumpGate> pair in this.JumpGates) if (!pair.Value.Closed) pair.Value.LastUpdateDateTimeUTC = value;
			}
		}
		#endregion

		#region Private Methods
		/// <summary>
		/// Callback triggered when this object receives a grid update packet
		/// </summary>
		/// <param name="packet">The update request packet</param>
		private void OnNetworkJumpGateGridUpdate(MyNetworkInterface.Packet packet)
		{
			if (packet == null || packet.EpochTime <= this.LastUpdateTime || !this.AtLeastOneUpdate()) return;
			MySerializedJumpGateConstruct serialized = packet.Payload<MySerializedJumpGateConstruct>();
			if (serialized == null || serialized.UUID != JumpGateUUID.FromJumpGateGrid(this)) return;

			if (MyNetworkInterface.IsMultiplayerServer && packet.PhaseFrame == 1 && serialized.IsClientRequest)
			{
				packet = packet.Forward(packet.SenderID, false);
				packet.Payload(this.ToSerialized(false));
				packet.Send();
			}
			else if (MyNetworkInterface.IsMultiplayerClient && (packet.PhaseFrame == 1 || packet.PhaseFrame == 2) && this.FromSerialized(serialized))
			{
				this.LastUpdateTime = packet.EpochTime;
				this.IsDirty = false;
				Logger.Debug($"SERVER_GRID_UPDATE -> {this.CubeGridID}", 3);
			}
		}

		/// <summary>
		/// Callback triggered when this object receives a comm-linked grids update request
		/// </summary>
		/// <param name="packet">The update request packet</param>
		private void OnNetworkCommLinkedUpdate(MyNetworkInterface.Packet packet)
		{
			if (packet == null || MyNetworkInterface.IsMultiplayerServer || packet.PhaseFrame != 2) return;
			KeyValuePair<long, List<long>> info = packet.Payload<KeyValuePair<long, List<long>>>();
			if (info.Key != this.CubeGridID) return;
			this.CommLinkedLock.AcquireWriter();

			try
			{
				if (this.CommLinkedGrids == null) this.CommLinkedGrids = new List<MyJumpGateConstruct>();
				else this.CommLinkedGrids.Clear();
				this.LastCommLinkUpdate = DateTime.Now;
				if (info.Value == null) return;

				foreach (long id in info.Value)
				{
					MyJumpGateConstruct grid = MyJumpGateModSession.Instance.GetJumpGateGrid(id);

					if (grid == null || grid.Closed || grid.MarkClosed)
					{
						MyJumpGateModSession.Instance.RequestGridDownload(id);
						Logger.Debug($" ... Got Comm-Linked grid - {id}; NEEDS_DOWNLOAD", 5);
					}
					else
					{
						this.CommLinkedGrids.Add(grid);
						Logger.Debug($" ... Got Comm-Linked grid - {grid}; OK", 5);
					}
				}
			}
			finally
			{
				this.CommLinkedClientUpdate = false;
				this.CommLinkedLock.ReleaseWriter();
				Logger.Debug($"COMMLINKED_UPDATE -> {this.CubeGridID}; Got {this.CommLinkedGrids.Count} grid(s)", 3);
			}
		}

		/// <summary>
		/// Callback triggered when this object receives a beacon-linked grids update request
		/// </summary>
		/// <param name="packet">The update request packet</param>
		private void OnNetworkBeaconLinkedUpdate(MyNetworkInterface.Packet packet)
		{
			if (packet == null || MyNetworkInterface.IsMultiplayerServer || packet.PhaseFrame != 2) return;
			KeyValuePair<long, List<MyBeaconLinkWrapper>> info = packet.Payload<KeyValuePair<long, List<MyBeaconLinkWrapper>>>();
			if (info.Key != this.CubeGridID) return;
			this.BeaconLinkedLock.AcquireWriter();

			try
			{
				if (this.BeaconLinks == null) this.BeaconLinks = new List<MyBeaconLinkWrapper>();
				else this.BeaconLinks.Clear();
				if (info.Value == null) return;

				foreach (MyBeaconLinkWrapper beacon_link in info.Value)
				{
					IMyBeacon beacon = (IMyBeacon) MyAPIGateway.Entities.GetEntityById(beacon_link.BeaconID);
					if (beacon != null) this.BeaconLinks.Add(new MyBeaconLinkWrapper(beacon));
					else this.BeaconLinks.Add(beacon_link);
					Logger.Debug($"{beacon_link.BroadcastName}, {beacon_link.BeaconPosition}");
				}

				this.LastBeaconLinkUpdate = DateTime.Now;
			}
			finally
			{
				this.BeaconLinkedClientUpdate = false;
				this.BeaconLinkedLock.ReleaseWriter();
				Logger.Debug($"BEACONLINKED_UPDATE -> {this.CubeGridID}; Got {this.BeaconLinks.Count} beacon(s)", 3);
			}
		}

		/// <summary>
		/// Callback triggered when this object receives a grid staticify request
		/// </summary>
		/// <param name="packet">The network packet</param>
		private void OnStaticifyConstruct(MyNetworkInterface.Packet packet)
		{
			if (packet == null || this.Closed || this.IsSuspended || !this.AtLeastOneUpdate()) return;
			KeyValuePair<long, bool> payload = packet.Payload<KeyValuePair<long, bool>>();
			if (payload.Key != this.CubeGridID) return;

			if (MyNetworkInterface.IsMultiplayerServer && packet.PhaseFrame == 1)
			{
				if (payload.Value && this.CubeGrids.Any((pair) => pair.Value.Speed > 1e-3 || pair.Value.Physics.AngularVelocity.Length() > 1e-3)) return;
				this.SetConstructStaticness(payload.Value);
				MyNetworkInterface.Packet response = packet.Forward(packet.SenderID, true);
				packet.Send();
			}
			else if (MyNetworkInterface.IsMultiplayerClient && (packet.PhaseFrame == 1 || packet.PhaseFrame == 2) && !this.Closed)
			{
				this.SetConstructStaticness(payload.Value);
			}
		}

		/// <summary>
		/// If server: Transmits this grid to all clients
		/// </summary>
		private void SendNetworkGridUpdate(bool closed = false)
		{
			if (!MyNetworkInterface.IsMultiplayerServer || this.CubeGrid == null || !this.FullyInitialized || !MyJumpGateModSession.Instance.Network.Registered) return;

			if (closed || this.Closed)
			{
				MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet
				{
					PacketType = MyPacketTypeEnum.CLOSE_GRID,
					TargetID = 0,
					Broadcast = true,
				};

				packet.Payload(this.CubeGrid.EntityId);
				packet.Send();
			}
			else
			{
				MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet
				{
					PacketType = MyPacketTypeEnum.UPDATE_GRID,
					TargetID = 0,
					Broadcast = true,
				};

				packet.Payload(this.ToSerialized(false));
				packet.Send();
			}

			this.IsDirty = false;
			if (this.JumpGates != null) foreach (KeyValuePair<long, MyJumpGate> pair in this.JumpGates) pair.Value.IsDirty = false;
			Logger.Debug($"[{this.CubeGridID}]: Sent Network Grid Update", 4);
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Unregisters resources for partially initialized constructs<br />
		/// For fully initialized constructs: Call MyJumpGateConstruct::Dispose()
		/// </summary>
		public void Release()
		{
			if (MyJumpGateModSession.Instance.Network.Registered)
			{
				MyJumpGateModSession.Instance.Network.Off(MyPacketTypeEnum.UPDATE_GRID, this.OnNetworkJumpGateGridUpdate);
				MyJumpGateModSession.Instance.Network.Off(MyPacketTypeEnum.STATICIFY_CONSTRUCT, this.OnStaticifyConstruct);
				MyJumpGateModSession.Instance.Network.Off(MyPacketTypeEnum.COMM_LINKED, this.OnNetworkCommLinkedUpdate);
				MyJumpGateModSession.Instance.Network.Off(MyPacketTypeEnum.BEACON_LINKED, this.OnNetworkBeaconLinkedUpdate);
			}
		}

		/// <summary>
		/// Marks this construct as dirty<br />
		/// It will be synced to clients on next tick
		/// </summary>
		public void SetDirty()
		{
			if (this.Closed) return;
			this.IsDirty = true;
			this.LastUpdateDateTimeUTC = DateTime.UtcNow;
		}
		#endregion
	}
}
