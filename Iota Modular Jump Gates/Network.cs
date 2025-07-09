using IOTA.ModularJumpGates.Util;
using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace IOTA.ModularJumpGates
{
	public enum MyPacketTypeEnum { DOWNLOAD_GRID, UPDATE_GRIDS, UPDATE_GRID, UPDATE_JUMP_GATE, UPDATE_DRIVE, UPDATE_CAPACITOR, UPDATE_CONTROLLER, UPDATE_REMOTE_ANTENNA, UPDATE_REMOTE_LINK, UPDATE_JUMP_ENDPOINT, UPDATE_EVENT_CONTROLLER_EVENT, UPDATE_CONFIG, CLOSE_BLOCK, CLOSE_GRID, JUMP_GATE_JUMP, JUMP_GATE_VOID_IN, JUMP_GATE_VOID_OUT, STATICIFY_CONSTRUCT, COMM_LINKED, BEACON_LINKED, AUTOACTIVATE, SHEARWARNING, GATE_DEBUG };

	/// <summary>
	/// Class holding all network functionality
	/// </summary>
	internal class MyNetworkInterface
	{
		/// <summary>
		/// Class for sending data
		/// </summary>
		[ProtoContract(UseProtoMembersOnly = true)]
		internal class Packet
		{
			#region Private Variables
			/// <summary>
			/// Whether the payload is null
			/// </summary>
			[ProtoMember(1)]
			private bool PayloadIsNull;
			#endregion

			#region Public Variables
			/// <summary>
			/// The time in timer ticks (100ns) this packet was sent
			/// </summary>
			[ProtoMember(2)]
			public ulong EpochTime { get; private set; } = 0;

			/// <summary>
			/// The hop count of this packet
			/// </summary>
			[ProtoMember(3)]
			public byte PhaseFrame { get; private set; } = 0;

			/// <summary>
			/// The packet's intended purpose
			/// </summary>
			[ProtoMember(4)]
			public MyPacketTypeEnum PacketType;

			/// <summary>
			/// The steam ID of the sender
			/// </summary>
			[ProtoMember(5)]
			public ulong SenderID { get; private set; }

			/// <summary>
			/// The steam ID of the target
			/// </summary>
			[ProtoMember(6)]
			public ulong TargetID;

			/// <summary>
			/// Whether to broadcast this packet to all clients (will be routed through server if client)
			/// </summary>
			[ProtoMember(7)]
			public bool Broadcast = false;

			/// <summary>
			/// The mod ID that sent this packet
			/// </summary>
			[ProtoMember(8)]
			public string ModID { get; private set; } = null;

			/// <summary>
			/// The base64 string containing this packet's payload
			/// </summary>
			[ProtoMember(9)]
			public string Buffer { get; private set; }

			/// <summary>
			/// The UTC date time representation of Packet.EpochTime
			/// </summary>
			public DateTime EpochDateTimeUTC
			{
				get
				{
					return DateTime.MinValue.ToUniversalTime() + new TimeSpan((long) this.EpochTime);
				}
			}
			#endregion

			#region Public Methods
			/// <summary>
			/// Updates sender ID, EpochTime, PhaseFrame, and TargetID and Sends this packet<br />
			/// If server: Sends this packet to target(s)
			/// If client: Sends this packet to server for routing
			/// </summary>
			/// <exception cref="InvalidOperationException"></exception>
			public void Send()
			{
				if (!MyJumpGateModSession.Network.Registered) throw new InvalidOperationException("Cannot send packet on unregistered network handler");
				ulong server = MyAPIGateway.Multiplayer.ServerId;
				this.EpochTime = (ulong) (DateTime.UtcNow - DateTime.MinValue).Ticks;
				this.SenderID = MyAPIGateway.Multiplayer.MyId;
				this.TargetID = this.Broadcast || this.TargetID == 0 ? server : this.TargetID;
				this.ModID = MyJumpGateModSession.Network.ModID;
				++this.PhaseFrame;
				byte[] data = MyAPIGateway.Utilities.SerializeToBinary(this);

				if (MyNetworkInterface.IsMultiplayerServer && this.Broadcast) MyAPIGateway.Multiplayer.SendMessageToOthers(MyJumpGateModSession.Network.ChannelID, data);
				else if (MyNetworkInterface.IsMultiplayerServer) MyAPIGateway.Multiplayer.SendMessageTo(MyJumpGateModSession.Network.ChannelID, data, this.TargetID);
				else MyAPIGateway.Multiplayer.SendMessageToServer(MyJumpGateModSession.Network.ChannelID, data);
			}

			/// <summary>
			/// Sets the payload of this packet
			/// </summary>
			/// <typeparam name="T">The typename of the data</typeparam>
			/// <param name="data">The data</param>
			public void Payload<T>(T data)
			{
				if (data == null)
				{
					this.PayloadIsNull = true;
					this.Buffer = null;
				}
				else
				{
					byte[] serialized = MyAPIGateway.Utilities.SerializeToBinary(data);
					this.Buffer = Convert.ToBase64String(serialized);
					this.PayloadIsNull = false;
				}
			}

			/// <summary>
			/// Gets the payload of this packet
			/// </summary>
			/// <typeparam name="T">The typename of the data</typeparam>
			/// <returns>The data</returns>
			public T Payload<T>()
			{
				if (this.PayloadIsNull) return default(T);
				byte[] deserialized = Convert.FromBase64String(this.Buffer);
				return MyAPIGateway.Utilities.SerializeFromBinary<T>(deserialized);
			}

			/// <summary>
			/// Duplicates this packet for transmission to another target
			/// </summary>
			/// <param name="target">The steam ID of the new target</param>
			/// <param name="broadcast">Whether to broadcast this packet to all clients</param>
			/// <returns></returns>
			public Packet Forward(ulong target, bool broadcast)
			{
				Packet copy = new Packet
				{
					EpochTime = this.EpochTime,
					PhaseFrame = this.PhaseFrame,
					PacketType = this.PacketType,
					SenderID = MyAPIGateway.Multiplayer.MyId,
					TargetID = target,
					Broadcast = broadcast,
					Buffer = this.Buffer == null ? null : string.Copy(this.Buffer),
				};

				return copy;
			}
			#endregion
		}

		#region Private Static Variables
		/// <summary>
		/// The maximum buffer time above which packets are ignored
		/// </summary>
		private static readonly double MaxPacketDeltaMS = 10000;
		#endregion

		#region Public Static Variables
		/// <summary>
		/// Whether this session is a multiplayer server (dedicated or not)
		/// </summary>
		public static readonly bool IsMultiplayerServer = MyAPIGateway.Multiplayer.IsServer && MyAPIGateway.Multiplayer.MultiplayerActive;

		/// <summary>
		/// Whether this session is a dedicated multiplayer server
		/// </summary>
		public static readonly bool IsDedicatedMultiplayerServer = MyNetworkInterface.IsMultiplayerServer && MyAPIGateway.Utilities.IsDedicated;

		/// <summary>
		/// Whether this sesson is singleplayer
		/// </summary>
		public static readonly bool IsSingleplayer = !MyAPIGateway.Multiplayer.MultiplayerActive;

		/// <summary>
		/// Whether this session is a multiplayer client (whether local hosted or not)
		/// </summary>
		public static readonly bool IsMultiplayerClient = !MyAPIGateway.Multiplayer.IsServer && MyAPIGateway.Multiplayer.MultiplayerActive;

		/// <summary>
		/// Whether this session is a local hosted multiplayer server
		/// </summary>
		public static readonly bool IsMultiplayerServerClient = MyNetworkInterface.IsMultiplayerServer && !MyAPIGateway.Utilities.IsDedicated;

		/// <summary>
		/// Whether this session is a multiplayer client that isn't also a server
		/// </summary>
		public static readonly bool IsStandaloneMultiplayerClient = MyNetworkInterface.IsMultiplayerClient && !MyNetworkInterface.IsMultiplayerServer;

		/// <summary>
		/// Whether this session is either singleplayer or a multiplayer standalone client
		/// </summary>
		public static readonly bool IsClientLike = MyNetworkInterface.IsMultiplayerClient || MyNetworkInterface.IsSingleplayer;

		/// <summary>
		/// Whether this session is either singleplayer or a server
		/// </summary>
		public static readonly bool IsServerLike = MyNetworkInterface.IsSingleplayer || MyNetworkInterface.IsMultiplayerServer;
		#endregion

		#region Private Variables
		/// <summary>
		/// Mutex lock for exclusive read-writes on callback arrays
		/// </summary>
		private readonly object MutexLock = new object();

		/// <summary>
		/// Master map storing event callbacks
		/// </summary>
		private readonly ConcurrentDictionary<MyPacketTypeEnum, List<Action<Packet>>> EventCallbacks = new ConcurrentDictionary<MyPacketTypeEnum, List<Action<Packet>>>();
		#endregion

		#region Public Variables
		/// <summary>
		/// Whether this network interface is registered
		/// </summary>
		public bool Registered { get; private set; } = false;

		/// <summary>
		/// The channel this network interface was registered on
		/// </summary>
		public readonly ushort ChannelID;

		/// <summary>
		/// The mod ID of this mod
		/// </summary>
		public readonly string ModID;
		#endregion

		#region Constructors
		/// <summary>
		/// Creates a new MyNetworkInterface on the specified channel<br />
		/// This does not register the interface
		/// </summary>
		/// <param name="channel_id">The channel to initialize on</param>
		public MyNetworkInterface(ushort channel_id, string modid)
		{
			this.ChannelID = channel_id;
			this.ModID = modid;
			if (modid == null) Logger.Warn($"Network interface mod id was null, packets may contain interference data");
		}
		#endregion

		#region Private Methods
		/// <summary>
		/// Event handler for receiving packets
		/// </summary>
		/// <param name="channel_id">The channel the packet is received on</param>
		/// <param name="payload">The packet as a byte array</param>
		/// <param name="sender_id">The steam ID of the packet sender</param>
		/// <param name="from_server">Whether the packet is from server</param>
		private void OnPacketReceived(ushort channel_id, byte[] payload, ulong sender_id, bool from_server)
		{
			if (channel_id != this.ChannelID || payload == null) return;
			Packet packet = null;

			try
			{
				packet = MyAPIGateway.Utilities.SerializeFromBinary<Packet>(payload);

				if (packet == null)
				{
					Logger.Error($"NULL packet during packet decode");
					return;
				}
			}
			catch (Exception e)
			{
				Logger.Error($"\"{e.GetType().FullName}\" during packet decode\n -> {e.Message}\n{e.StackTrace}");
				return;
			}

			if (packet.SenderID == MyAPIGateway.Multiplayer.MyId || (!packet.Broadcast && packet.TargetID != MyAPIGateway.Multiplayer.MyId) || packet.ModID != this.ModID) return;
			else if (MyNetworkInterface.IsMultiplayerServer)
			{
				double this_epoch = (ulong) (DateTime.UtcNow - DateTime.MinValue).Ticks;
				double sender_epoch = packet.EpochTime;
				if (Math.Abs(sender_epoch - this_epoch) > MaxPacketDeltaMS * 10000) return;
				List<Action<Packet>> callbacks = this.EventCallbacks.GetValueOrDefault(packet.PacketType, null);

				if (callbacks != null)
				{
					List<Action<Packet>> callbacks_local = new List<Action<Packet>>(callbacks.Count);
					lock (this.MutexLock) callbacks_local.AddList(callbacks);

					for (int i = 0; i < callbacks_local.Count; ++i)
					{
						Action<Packet> callback = callbacks_local[i];

						try
						{
							callback(packet);
						}
						catch (Exception e)
						{
							Logger.Error($"\"{e.GetType().FullName}\" during packet callback[{packet.PacketType}/{i}]\n -> {e.Message}\nStackTrace:\n  ...\n{e.StackTrace}\nInnerStackTrace:\n  ...\n{e.InnerException?.StackTrace}");
						}
					}
				}

				if (packet.Broadcast) MyAPIGateway.Multiplayer.SendMessageToOthers(this.ChannelID, payload);
				else if (packet.TargetID != 0 && packet.TargetID != MyAPIGateway.Multiplayer.MyId) MyAPIGateway.Multiplayer.SendMessageTo(this.ChannelID, payload, packet.TargetID);

			}
			else if (from_server)
			{
				List<Action<Packet>> callbacks = this.EventCallbacks.GetValueOrDefault(packet.PacketType, null);

				if (callbacks != null)
				{
					List<Action<Packet>> callbacks_local = new List<Action<Packet>>(callbacks.Count);
					lock (this.MutexLock) callbacks_local.AddList(callbacks);

					for (int i = 0; i < callbacks_local.Count; ++i)
					{
						Action<Packet> callback = callbacks_local[i];

						try
						{
							callback(packet);
						}
						catch (Exception e)
						{
							Logger.Error($"\"{e.GetType()?.FullName ?? "UNKNOWN"}\" during packet callback[{packet.PacketType}/{i}]\n -> {e.Message}\nStackTrace:\n  ...\n{e.StackTrace}\nInnerStackTrace:\n  ...\n{e.InnerException?.StackTrace ?? "???"}");
						}
					}
				}
			}
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Registers the network interface
		/// </summary>
		/// <exception cref="InvalidOperationException">If this session is singleplayer or the interface is already registered</exception>
		public void Register()
		{
			if (IsSingleplayer) throw new InvalidOperationException("Cannot register network handler in singleplayer");
			else if (this.Registered) throw new InvalidOperationException("Network handler already registered");
			MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(this.ChannelID, this.OnPacketReceived);
			this.Registered = true;
			Logger.Log($"Registered network handler on channel {this.ChannelID}");
		}

		/// <summary>
		/// Unregisteres the network interface
		/// </summary>
		/// <exception cref="InvalidOperationException">If this session is singleplayer or the interface is not registered</exception>
		public void Unregister()
		{
			if (IsSingleplayer) throw new InvalidOperationException("Cannot register network handler in singleplayer");
			else if (!this.Registered) throw new InvalidOperationException("Network handler not registered");
			MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(this.ChannelID, this.OnPacketReceived);
			this.Registered = false;
			lock (this.MutexLock) this.EventCallbacks.Clear();
			Logger.Log("Unregistered network handler");
		}

		/// <summary>
		/// Registers a callback for the specified packet type
		/// </summary>
		/// <param name="type">The packet type to listen for</param>
		/// <param name="callback">The callback accepting the received packet</param>
		public void On(MyPacketTypeEnum type, Action<Packet> callback)
		{
			if (!this.Registered) return;
			List<Action<Packet>> callbacks = this.EventCallbacks.GetOrAdd(type, new List<Action<Packet>>());
			lock (this.MutexLock) if (!callbacks.Contains(callback)) callbacks.Add(callback);
		}

		/// <summary>
		/// Unregisters a single callback for the specified packet type
		/// </summary>
		/// <param name="type">The packet type to listen for</param>
		/// <param name="callback">The callback to unbind</param>
		public void Off(MyPacketTypeEnum type, Action<Packet> callback)
		{
			List<Action<Packet>> callbacks;
			if (!this.EventCallbacks.TryGetValue(type, out callbacks)) return;
			lock (this.MutexLock) if (callbacks.Contains(callback)) callbacks.Remove(callback);
		}
		#endregion
	}
}
