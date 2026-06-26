using IOTA.ModularJumpGates.JumpGates;
using IOTA.ModularJumpGates.JumpGateConstruct;
using IOTA.ModularJumpGates.ModConfiguration;
using IOTA.ModularJumpGates.Terminal;
using IOTA.ModularJumpGates.Util;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage;
using VRageMath;

namespace IOTA.ModularJumpGates.Session
{
	internal partial class MyJumpGateModSession
	{
		#region Private Variables
		/// <summary>
		/// The time in game ticks after which all grids are sent from server to all clients
		/// </summary>
		private readonly ushort GridNetworkUpdateDelay = 18000;
		#endregion

		#region Public Variables
		/// <summary>
		/// Reference to the instane of this session's network interface
		/// </summary>
		public MyNetworkInterface Network { get; private set; } = null;
		#endregion

		#region Private Methods
		/// <summary>
		/// Unloads the network handler and all registered callbacks
		/// </summary>
		private void UnloadNetwork()
		{
			if (this.Network == null || !this.Network.Registered) return;
			{
				this.Network.Off(MyPacketTypeEnum.UPDATE_GRIDS, this.OnNetworkGridsUpdate);
				this.Network.Off(MyPacketTypeEnum.CLOSE_GRID, this.OnNetworkGridClose);
				this.Network.Off(MyPacketTypeEnum.DOWNLOAD_GRID, this.OnNetworkGridDownload);
				this.Network.Off(MyPacketTypeEnum.COMM_LINKED, this.OnNetworkCommLinkedUpdate);
				this.Network.Off(MyPacketTypeEnum.BEACON_LINKED, this.OnNetworkBeaconLinkedUpdate);
				this.Network.Off(MyPacketTypeEnum.UPDATE_CONFIG, this.OnConfigurationUpdate);
				this.Network.Off(MyPacketTypeEnum.MARK_GATES_DIRTY, this.OnNetworkGridGateReconstruct);
				this.Network.Off(MyPacketTypeEnum.CONSTRUCT_UPDATE_NOTICE, this.OnNetworkConstructUpdateNotice);
				this.Network.Off(MyPacketTypeEnum.GATE_DETONATION, this.OnNetworkJumpGateDetonation);
				this.Network.Off(MyPacketTypeEnum.VERSION_CHECK, this.OnNetworkVersionCheck);
				this.Network.Off(MyPacketTypeEnum.UPDATE_COCKPIT, this.OnNetworkCockpitSettingsUpdate);
				this.Network.Unregister();
			}

			this.Network = null;
		}

		/// <summary>
		/// Event handler for grid update packet<br />
		/// If server: Sends current grids to client<br />
		/// If client: Updates internal grid map from server
		/// </summary>
		/// <param name="packet">The received packet</param>
		private void OnNetworkGridsUpdate(MyNetworkInterface.Packet packet)
		{
			if (packet == null) return;

			if (MyNetworkInterface.IsMultiplayerServer && packet.PhaseFrame == 1)
			{
				List<MySerializedJumpGateConstruct> serialized_grids = this.GridMap.Where((pair) => pair.Value.AtLeastOneUpdate() && !pair.Value.MarkClosed).Select((pair) => pair.Value.ToSerialized(false)).ToList();
				MyNetworkInterface.Packet grids_packet = new MyNetworkInterface.Packet
				{
					PacketType = MyPacketTypeEnum.UPDATE_GRIDS,
					TargetID = packet.SenderID,
					Broadcast = false,
				};
				grids_packet.Payload(serialized_grids);
				grids_packet.Send();
				Logger.Debug($"Got client grid update request - Sent grids: {string.Join(", ", serialized_grids.Select((grid) => grid.UUID.GetJumpGateGrid().ToString()))}", 2);
			}
			else if (MyNetworkInterface.IsStandaloneMultiplayerClient && (packet.PhaseFrame == 1 || packet.PhaseFrame == 2))
			{
				Logger.Debug($"Got server grid update packet", 2);
				List<MySerializedJumpGateConstruct> serialized_grids = packet.Payload<List<MySerializedJumpGateConstruct>>() ?? new List<MySerializedJumpGateConstruct>();
				List<long> closed = new List<long>(this.GridMap.Keys);

				foreach (MySerializedJumpGateConstruct serialized in serialized_grids)
				{
					MyJumpGateConstruct new_grid = this.StoreSerializedGrid(serialized);
					if (new_grid == null) continue;
					new_grid.LastUpdateDateTimeUTC = packet.EpochDateTimeUTC;
					closed.Remove(new_grid.CubeGridID);
				}

				foreach (long gridid in closed) this.CloseGrid(this.GridMap.GetValueOrDefault(gridid, null));
			}
		}

		/// <summary>
		/// Event handler for grid download packet<br />
		/// If server: Sends the specified grid to client<br />
		/// If client: Updates the specified grid from server
		/// </summary>
		/// <param name="packet">The received packet</param>
		private void OnNetworkGridDownload(MyNetworkInterface.Packet packet)
		{
			if (packet == null) return;

			if (MyNetworkInterface.IsMultiplayerServer && packet.PhaseFrame == 1)
			{
				long grid_id = packet.Payload<long>();
				packet = packet.Forward(packet.SenderID, false);
				MyJumpGateConstruct construct = this.GridMap.GetValueOrDefault(grid_id, null);

				if (construct == null || construct.MarkClosed)
				{
					Logger.Debug($"Got client grid download request for closed or null grid - IGNORED", 2);
					return;
				}

				MySerializedJumpGateConstruct serialized = construct.ToSerialized(false);
				packet.Payload(serialized);
				packet.Send();
				Logger.Debug($"Got client grid download request - Sent grid: {grid_id}", 2);
			}
			else if (MyNetworkInterface.IsStandaloneMultiplayerClient && packet.PhaseFrame == 2)
			{
				MyJumpGateConstruct new_grid = this.StoreSerializedGrid(packet.Payload<MySerializedJumpGateConstruct>());
				if (new_grid == null) return;
				new_grid.LastUpdateDateTimeUTC = packet.EpochDateTimeUTC;
				Logger.Debug($"Grid '{new_grid.CubeGridID}' NETWORK_DOWNLOAD", 2);
			}
		}

		/// <summary>
		/// Callback triggered when this object receives a comm-linked grids update request
		/// </summary>
		/// <param name="packet">The update request packet</param>
		private void OnNetworkCommLinkedUpdate(MyNetworkInterface.Packet packet)
		{
			if (packet == null || !MyNetworkInterface.IsMultiplayerServer || packet.PhaseFrame != 1) return;
			long construct_id = packet.Payload<long>();
			MyJumpGateConstruct target = this.GridMap.GetValueOrDefault(construct_id, null);
			List<MyJumpGateConstruct> comm_linked = target?.GetCommLinkedJumpGateGrids().ToList();
			packet = packet.Forward(packet.SenderID, false);
			packet.Payload(new KeyValuePair<long, List<long>>(construct_id, comm_linked?.Select((grid) => grid.CubeGridID)?.ToList()));
			packet.Send();
			Logger.Debug($"Got client comm-link request for \"{construct_id}\", sent grids: {string.Join(", ", comm_linked?.Select((grid) => grid.CubeGridID.ToString()).ToList() ?? new List<string>() { "N/A" })}", 3);
		}

		/// <summary>
		/// Callback triggered when this object receives a beacon-linked grids update request
		/// </summary>
		/// <param name="packet">The update request packet</param>
		private void OnNetworkBeaconLinkedUpdate(MyNetworkInterface.Packet packet)
		{
			if (packet == null || !MyNetworkInterface.IsMultiplayerServer || packet.PhaseFrame != 1) return;
			long construct_id = packet.Payload<long>();
			MyJumpGateConstruct target = this.GridMap.GetValueOrDefault(construct_id, null);
			List<MyBeaconLinkWrapper> beacon_linked = target?.GetBeaconsWithinReverseBroadcastSphere().ToList();
			packet = packet.Forward(packet.SenderID, false);
			packet.Payload(new KeyValuePair<long, List<MyBeaconLinkWrapper>>(construct_id, beacon_linked));
			packet.Send();
			Logger.Debug($"Got client beacon-link request for \"{construct_id}\", sent beacons: {string.Join(", ", beacon_linked?.Select((beacon) => beacon.BeaconID.ToString()).ToList() ?? new List<string>() { "N/A" })}", 3);
		}

		/// <summary>
		/// Event handler for grid closed packet
		/// If client: Closes the associated MyJumpGateConstruct indicated from server
		/// </summary>
		/// <param name="packet">The received packet</param>
		private void OnNetworkGridClose(MyNetworkInterface.Packet packet)
		{
			if (packet == null || MyNetworkInterface.IsServerLike) return;
			long gridid = packet.Payload<long>();
			this.GridMap.GetValueOrDefault(gridid, null)?.Dispose();
			Logger.Debug($"Grid '{gridid}' PURGED", 2);
		}

		/// <summary>
		/// Event handler for grid gate reconstruct packet
		/// </summary>
		/// <param name="packet">The received packet</param>
		private void OnNetworkGridGateReconstruct(MyNetworkInterface.Packet packet)
		{
			if (packet == null || MyNetworkInterface.IsStandaloneMultiplayerClient || packet.PhaseFrame != 1) return;
			KeyValuePair<long, bool> data = packet.Payload<KeyValuePair<long, bool>>();
			if (data.Key == -1) foreach (KeyValuePair<long, MyJumpGateConstruct> pair in this.GridMap) if (!pair.Value.MarkClosed) pair.Value.MarkGatesForUpdate(data.Value);
					else this.GridMap.GetValueOrDefault(data.Key, null)?.MarkGatesForUpdate();
		}

		/// <summary>
		/// Event handler for grid update failure notice
		/// </summary>
		/// <param name="packet">The received packet</param>
		private void OnNetworkConstructUpdateNotice(MyNetworkInterface.Packet packet)
		{
			MyConstructUpdateNotice notice;
			ulong this_id = MyAPIGateway.Multiplayer.MyId;
			if (MyNetworkInterface.IsServerLike || packet == null || packet.PhaseFrame != 1 || (notice = packet.Payload<MyConstructUpdateNotice>()) == null || (!MyAPIGateway.Session.IsUserAdmin(this_id) && !notice.ConstructOwnerIDs.Contains(this_id))) return;
			if (notice.IsFullFailure) this.DisplayConstructFullFailureNotice(null, notice.ConstructMainID, notice.ConstructName);
			else this.DisplayConstructPartialFailureNotice(null, notice.ConstructMainID, notice.ConstructName);
		}

		/// <summary>
		/// Event handler for jump gate detonation
		/// </summary>
		/// <param name="packet">The received packet</param>
		private void OnNetworkJumpGateDetonation(MyNetworkInterface.Packet packet)
		{
			if (packet == null || packet.PhaseFrame != 1 || !MyNetworkInterface.IsStandaloneMultiplayerClient) return;
			GateDetonationInfo.SerializedGateDetonationInfo detonation_info = packet.Payload<GateDetonationInfo.SerializedGateDetonationInfo>();
			MyJumpGate jump_gate = this.GetJumpGate(detonation_info.JumpGate);
			jump_gate?.StopSounds();
			this.GateDetonations.TryAdd(detonation_info.JumpGate, new GateDetonationInfo(ref detonation_info));
			Logger.Debug($"Jump gate server CORE detonation event: {detonation_info.JumpGate.GetJumpGateGrid()}::{detonation_info.JumpGate.GetJumpGate()}, RANGE={detonation_info.MaxExplosionRange}, DAMAGE={detonation_info.MaxExplosionDamage}, FORCE={detonation_info.MaxExplosionForce}", 3);
		}

		/// <summary>
		/// Event handler for client version request
		/// </summary>
		/// <param name="packet">The received packet</param>
		private void OnNetworkVersionCheck(MyNetworkInterface.Packet packet)
		{
			if (packet == null) return;
			else if (packet.PhaseFrame == 1 && MyNetworkInterface.IsMultiplayerServer)
			{
				packet = packet.Forward(packet.SenderID, false);
				packet.Payload(this.ModVersion);
				packet.Send();
			}
			else if (packet.PhaseFrame == 2 && MyNetworkInterface.IsStandaloneMultiplayerClient)
			{
				Vector3I server_version = packet.Payload<Vector3I>();
				Vector3I client_version = this.ModVersion;
				if (server_version == client_version) return;
				Logger.Warn($"Version mismatch with server - Client version: {client_version}, Server version: {server_version}");
				MyAPIGateway.Utilities.ShowMessage(this.DisplayName, MyTexts.GetString("ModNotif_Error_ServerClientVersionMismatch")
					.Replace("{%0}", $"{client_version.X}.{client_version.Y}.{client_version.Z}")
					.Replace("{%1}", $"{server_version.X}.{server_version.Y}.{server_version.Z}"));
			}
		}

		/// <summary>
		/// Event handler for cockpit settings update
		/// </summary>
		private void OnNetworkCockpitSettingsUpdate(MyNetworkInterface.Packet packet)
		{
			MyCockpitInfo info = packet?.Payload<MyCockpitInfo>();

			if (packet == null || info == null) return;
			else if (MyNetworkInterface.IsMultiplayerServer && packet.PhaseFrame == 1)
			{
				IMyCockpit cockpit = info.Cockpit;
				MyCockpitInfo current = this.CockpitBlockSettings.GetValueOrDefault(cockpit, null);
				if (cockpit == null || (current != null && current.LastUpdateTimeEpoch >= info.LastUpdateTimeEpoch)) return;
				this.CockpitBlockSettings[cockpit] = info;
				this.RedrawAllTerminalControls();
				packet = packet.Forward(0, true);
				packet.Send();
			}
			else if (MyNetworkInterface.IsStandaloneMultiplayerClient && (packet.PhaseFrame == 1 || packet.PhaseFrame == 2))
			{
				IMyCockpit cockpit = info.Cockpit;
				if (cockpit == null) return;
				this.CockpitBlockSettings[cockpit] = info;
				this.RedrawAllTerminalControls();
			}
		}

		/// <summary>
		/// Event handler for configuration update
		/// </summary>
		/// <param name="packet">The received packet</param>
		private void OnConfigurationUpdate(MyNetworkInterface.Packet packet)
		{
			if (packet == null || packet.PhaseFrame != 1) return;
			else if (MyNetworkInterface.IsServerLike)
			{
				bool local_config_exists, global_config_exists;
				Exception local_load_err, global_load_err;
				MyModConfigurationV1 configuration = MyModConfigurationV1.Load(MyJumpGateModSession.Instance, out global_config_exists, out local_config_exists, out global_load_err, out local_load_err);
				if (global_config_exists) Logger.Log("Found global mod config file");
				else Logger.Warn($"Failed to locate global mod config file; SKIPPED");
				if (local_config_exists) Logger.Log("Found local mod config file");
				else Logger.Warn($"Failed to locate local mod config file; SKIPPED");
				if (global_load_err != null) Logger.Error($"Error loading global config file:\n  ...\n[ {global_load_err.GetType().Name} ]: {global_load_err.Message}\n{global_load_err.StackTrace}\n{global_load_err.InnerException}");
				if (local_load_err != null) Logger.Error($"Error loading local config file:\n  ...\n[ {local_load_err.GetType().Name} ]: {local_load_err.Message}\n{local_load_err.StackTrace}\n{local_load_err.InnerException}");
				if (!local_config_exists || !global_config_exists || local_load_err != null || global_load_err != null) return;
				this.UpdateConfiguration(configuration);
			}
			else
			{
				MyModConfigurationV1.MyGlobalModConfiguration packed_config = packet.Payload<MyModConfigurationV1.MyGlobalModConfiguration>();
				if (packed_config == null) return;
				this.Configuration.Update(packed_config.LocalModConfiguration, packed_config.ModSettings);
				if (!packed_config.LocalModConfiguration.JumpGateConfiguration.JumpGateExplosionConfiguration.EnableGateExplosions.Value && MyJumpGateControllerTerminal.TerminalSection == MyJumpGateControllerTerminal.MyTerminalSection.DETONATION) MyJumpGateControllerTerminal.TerminalSection = MyJumpGateControllerTerminal.MyTerminalSection.JUMP_GATE;
				if (!packed_config.LocalModConfiguration.JumpGateConfiguration.JumpGateExplosionConfiguration.EnableGateExplosions.Value && MyJumpGateRemoteAntennaTerminal.TerminalSection == MyJumpGateRemoteAntennaTerminal.MyTerminalSection.DETONATION) MyJumpGateRemoteAntennaTerminal.TerminalSection = MyJumpGateRemoteAntennaTerminal.MyTerminalSection.JUMP_GATE;
				this.RedrawAllTerminalControls();
				MyAPIGateway.Utilities.ShowMessage(this.DisplayName, MyTexts.GetString("Session_OnConfigReload"));
			}
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Requests a download of the specified construct from server<br />
		/// Does nothing if not standalone multiplayer client
		/// </summary>
		/// <param name="grid_id">The grid ID to download</param>
		public void RequestGridDownload(long grid_id)
		{
			if (MyNetworkInterface.IsServerLike) return;

			MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet
			{
				PacketType = MyPacketTypeEnum.DOWNLOAD_GRID,
				TargetID = 0,
				Broadcast = false,
			};

			packet.Payload(grid_id);
			packet.Send();
		}

		/// <summary>
		/// Requests a download of all constructs from server<br />
		/// Does nothing if not standalone multiplayer client
		/// </summary>
		public void RequestGridsDownload()
		{
			if (MyNetworkInterface.IsServerLike) return;

			MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet
			{
				PacketType = MyPacketTypeEnum.UPDATE_GRIDS,
				TargetID = 0,
				Broadcast = false,
			};

			packet.Send();
		}
		#endregion
	}
}
