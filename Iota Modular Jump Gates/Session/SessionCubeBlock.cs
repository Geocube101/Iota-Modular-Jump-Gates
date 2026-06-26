using IOTA.ModularJumpGates.CubeBlock;
using IOTA.ModularJumpGates.JumpGateConstruct;
using IOTA.ModularJumpGates.Util;
using Sandbox.ModAPI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using VRage.ModAPI;

namespace IOTA.ModularJumpGates.Session
{
	internal partial class MyJumpGateModSession
	{
		#region Private Variables
		/// <summary>
		/// Master map for storing cockpit terminal settings
		/// </summary>
		private ConcurrentDictionary<IMyCockpit, MyCockpitInfo> CockpitBlockSettings = new ConcurrentDictionary<IMyCockpit, MyCockpitInfo>();
		#endregion

		#region Public Variables
		/// <summary>
		/// The largest beacon broadcast radius of any placed beacon in the world
		/// </summary>
		public float MaxBeaconBroadcastRadius { get; private set; } = 200000;

		/// <summary>
		/// The Guid used to store information in mod storage components
		/// </summary>
		public Guid BlockComponentDataGUID => new Guid("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF");
		#endregion

		#region Public Static Methods
		/// <summary>
		/// Checks whether the block is a derivative of CubeBlockBase
		/// </summary>
		/// <param name="block">The block to check</param>
		/// <returns>Whether the "CubeBlockBase" game logic component is attached</returns>
		public static bool IsBlockCubeBlockBase(IMyTerminalBlock block)
		{
			return MyJumpGateModSession.GetBlockAsCubeBlockBase(block) != null;
		}

		/// <summary>
		/// Checks whether the block is a jump gate controller
		/// </summary>
		/// <param name="block">The block to check</param>
		/// <returns>Whether the "JumpGateController" game logic component is attached</returns>
		public static bool IsBlockJumpGateController(IMyTerminalBlock block)
		{
			return MyJumpGateModSession.GetBlockAsJumpGateController(block) != null;
		}

		/// <summary>
		/// Checks whether the block is a jump gate drive
		/// </summary>
		/// <param name="block">The block to check</param>
		/// <returns>Whether the "JumpGateDrive" game logic component is attached</returns>
		public static bool IsBlockJumpGateDrive(IMyTerminalBlock block)
		{
			return MyJumpGateModSession.GetBlockAsJumpGateDrive(block) != null;
		}

		/// <summary>
		/// Checks whether the block is a jump gate capacitor
		/// </summary>
		/// <param name="block">The block to check</param>
		/// <returns>Whether the "JumpGateCapacitor" game logic component is attached</returns>
		public static bool IsBlockJumpGateCapacitor(IMyTerminalBlock block)
		{
			return MyJumpGateModSession.GetBlockAsJumpGateCapacitor(block) != null;
		}

		/// <summary>
		/// Checks whether the block is a jump gate remote antenna
		/// </summary>
		/// <param name="block">The block to check</param>
		/// <returns>Whether the "JumpGateRemoteAntenna" game logic component is attached</returns>
		public static bool IsBlockJumpGateRemoteAntenna(IMyTerminalBlock block)
		{
			return MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block) != null;
		}

		/// <summary>
		/// Checks whether the block is a jump gate server antenna
		/// </summary>
		/// <param name="block">The block to check</param>
		/// <returns>Whether the "JumpGateServerAntenna" game logic component is attached</returns>
		public static bool IsBlockJumpGateServerAntenna(IMyTerminalBlock block)
		{
			return MyJumpGateModSession.GetBlockAsJumpGateServerAntenna(block) != null;
		}

		/// <summary>
		/// Checks whether the block is a jump gate remote link
		/// </summary>
		/// <param name="block">The block to check</param>
		/// <returns>Whether the "JumpGateRemoteLink" game logic component is attached</returns>
		public static bool IsBlockJumpGateRemoteLink(IMyTerminalBlock block)
		{
			return MyJumpGateModSession.GetBlockAsJumpGateRemoteLink(block) != null;
		}

		/// <summary>
		/// Gets the block as a CubeBlockBase instance or null if not a derivative of CubeBlockBase
		/// </summary>
		/// <param name="block">The block to convert</param>
		/// <returns>The "CubeBlockBase" game logic component or null</returns>
		public static MyCubeBlockBase GetBlockAsCubeBlockBase(IMyTerminalBlock block)
		{
			return block?.GameLogic?.GetAs<MyCubeBlockBase>();
		}

		/// <summary>
		/// Gets the block as a jump gate controller or null if not a jump gate controller
		/// </summary>
		/// <param name="block">The block to convert</param>
		/// <returns>The "JumpGateController" game logic component or null</returns>
		public static MyJumpGateController GetBlockAsJumpGateController(IMyTerminalBlock block)
		{
			return block?.GameLogic?.GetAs<MyJumpGateController>();
		}

		/// <summary>
		/// Gets the block as a jump gate drive or null if not a jump gate drive
		/// </summary>
		/// <param name="block">The block to convert</param>
		/// <returns>The "JumpGateDrive" game logic component or null</returns>
		public static MyJumpGateDrive GetBlockAsJumpGateDrive(IMyTerminalBlock block)
		{
			return block?.GameLogic?.GetAs<MyJumpGateDrive>();
		}

		/// <summary>
		/// Gets the block as a jump gate capacitor or null if not a jump gate capacitor
		/// </summary>
		/// <param name="block">The block to convert</param>
		/// <returns>The "JumpGateCapacitor" game logic component or null</returns>
		public static MyJumpGateCapacitor GetBlockAsJumpGateCapacitor(IMyTerminalBlock block)
		{
			return block?.GameLogic?.GetAs<MyJumpGateCapacitor>();
		}

		/// <summary>
		/// Gets the block as a jump gate remote antenna or null if not a jump gate remote antenna
		/// </summary>
		/// <param name="block">The block to convert</param>
		/// <returns>The "JumpGateRemoteAntenna" game logic component or null</returns>
		public static MyJumpGateRemoteAntenna GetBlockAsJumpGateRemoteAntenna(IMyTerminalBlock block)
		{
			return block?.GameLogic?.GetAs<MyJumpGateRemoteAntenna>();
		}

		/// <summary>
		/// Gets the block as a jump gate server antenna or null if not a jump gate server antenna
		/// </summary>
		/// <param name="block">The block to convert</param>
		/// <returns>The "JumpGateServerAntenna" game logic component or null</returns>
		public static MyJumpGateServerAntenna GetBlockAsJumpGateServerAntenna(IMyTerminalBlock block)
		{
			return block?.GameLogic?.GetAs<MyJumpGateServerAntenna>();
		}

		/// <summary>
		/// Gets the block as a jump gate remote link or null if not a jump gate remote link
		/// </summary>
		/// <param name="block">The block to convert</param>
		/// <returns>The "MyJumpGateRemoteLink" game logic component or null</returns>
		public static MyJumpGateRemoteLink GetBlockAsJumpGateRemoteLink(IMyTerminalBlock block)
		{
			return block?.GameLogic?.GetAs<MyJumpGateRemoteLink>();
		}
		#endregion

		#region Private Methods
		private void UnloadCubeBlocks()
		{
			this.CockpitBlockSettings.Clear();
			this.CockpitBlockSettings = null;
			MyCubeBlockBase.DisposeAll();
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Updates a cockpit's terminal settings<br />
		/// If standalone multiplayer client, also sends new settings to server
		/// </summary>
		/// <param name="cockpit">The cockpit whose settings to update</param>
		/// <param name="new_settings">The new settings</param>
		public void UpdateCockpitTerminalSettings(IMyCockpit cockpit, MyCockpitInfo new_settings)
		{
			if (cockpit == null || cockpit.MarkedForClose || new_settings == null) return;
			new_settings.CockpitId = cockpit.EntityId;
			new_settings.LastUpdateTime = DateTime.UtcNow;
			this.CockpitBlockSettings[cockpit] = new_settings;

			if (MyNetworkInterface.IsStandaloneMultiplayerClient)
			{
				MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet
				{
					PacketType = MyPacketTypeEnum.UPDATE_COCKPIT,
					TargetID = 0,
					Broadcast = false,
				};
				packet.Payload(new_settings);
				packet.Send();
			}
			else if (MyNetworkInterface.IsMultiplayerServer)
			{
				MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet
				{
					PacketType = MyPacketTypeEnum.UPDATE_COCKPIT,
					TargetID = 0,
					Broadcast = true,
				};
				packet.Payload(new_settings);
				packet.Send();
			}
		}

		/// <summary>
		/// Updates the max beacon broadcast radius using the specified beacon<br />
		/// If the specified beacon has a larger radius, the session max broadcast distance will be updated
		/// </summary>
		/// <param name="beacon">The beacon</param>
		public void UpdateMaxBeaconBroadcastRadius(IMyBeacon beacon)
		{
			if (beacon == null || beacon.Radius <= this.MaxBeaconBroadcastRadius) return;
			this.MaxBeaconBroadcastRadius = beacon.Radius;
		}

		/// <summary>
		/// Gets a block from its UUID
		/// </summary>
		/// <typeparam name="T">The block type to get</typeparam>
		/// <param name="uuid">The block's UUID</param>
		/// <returns>The cube block component or null</returns>
		public T GetJumpGateBlock<T>(JumpGateUUID uuid) where T : MyCubeBlockBase
		{
			if (uuid == null) return null;
			MyJumpGateConstruct construct = this.GetJumpGateGrid(uuid);
			MyCubeBlockBase block = construct?.GetCubeBlock(uuid.GetBlock());
			return (block is T) ? (T) block : null;
		}

		/// <summary>
		/// Gets a block from its UUID
		/// </summary>
		/// <typeparam name="T">The block type to get</typeparam>
		/// <param name="block_id">The block's entity ID</param>
		/// <returns>The cube block component or null</returns>
		public T GetJumpGateBlock<T>(long block_id) where T : MyCubeBlockBase
		{
			if (block_id <= 0) return null;
			IMyEntity entity = MyAPIGateway.Entities.GetEntityById(block_id);
			return (entity == null || !(entity is IMyTerminalBlock)) ? null : entity.GameLogic?.GetAs<T>();
		}

		/// <summary>
		/// Gets a cockpit's terminal settings
		/// </summary>
		/// <param name="cockpit">The cockpit whose settings to retreive</param>
		/// <returns>The settings or null if none set</returns>
		public MyCockpitInfo GetCockpitTerminalSettings(IMyCockpit cockpit)
		{
			if (cockpit == null || cockpit.MarkedForClose) return null;
			MyCockpitInfo settings;
			string serialized_cockpit_settings;
			if (this.CockpitBlockSettings.TryGetValue(cockpit, out settings)) return settings;
			else if (MyAPIGateway.Utilities.GetVariable("IOTA.CockpitSettings", out serialized_cockpit_settings))
			{
				List<MyCockpitInfo> deserialized_cockpit_settings = MyAPIGateway.Utilities.SerializeFromBinary<List<MyCockpitInfo>>(Convert.FromBase64String(serialized_cockpit_settings));

				foreach (MyCockpitInfo cockpit_info in deserialized_cockpit_settings)
				{
					IMyCockpit stored = cockpit_info.Cockpit;
					if (stored == null || stored.MarkedForClose) continue;
					else this.CockpitBlockSettings.TryAdd(cockpit, cockpit_info);
				}

				return this.CockpitBlockSettings.GetValueOrDefault(cockpit, new MyCockpitInfo(cockpit));
			}
			else return new MyCockpitInfo(cockpit);
		}
		#endregion
	}
}
