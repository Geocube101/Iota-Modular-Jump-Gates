using IOTA.ModularJumpGates.CubeBlock;
using IOTA.ModularJumpGates.Util;
using Sandbox.ModAPI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;

namespace IOTA.ModularJumpGates.API
{
	internal class MyAPIInterface
	{
		public static readonly int[] ModAPIVersion = new int[2] { 1, 3 };
		public static readonly int[] AnimationAPIVersion = new int[2] { 1, 1 };

		private bool Registered = false;
		private readonly ConcurrentDictionary<long, Dictionary<string, object>> ConstructWrappers = new ConcurrentDictionary<long, Dictionary<string, object>>();
		private readonly ConcurrentDictionary<JumpGateUUID, Dictionary<string, object>> JumpGateWrappers = new ConcurrentDictionary<JumpGateUUID, Dictionary<string, object>>();
		private readonly ConcurrentDictionary<long, Dictionary<string, object>> BlockBaseWrappers = new ConcurrentDictionary<long, Dictionary<string, object>>();
		private readonly ConcurrentDictionary<long, Dictionary<string, object>> ControllerWrappers = new ConcurrentDictionary<long, Dictionary<string, object>>();
		private readonly ConcurrentDictionary<long, Dictionary<string, object>> DriveWrappers = new ConcurrentDictionary<long, Dictionary<string, object>>();
		private readonly ConcurrentDictionary<long, Dictionary<string, object>> CapacitorWrppers = new ConcurrentDictionary<long, Dictionary<string, object>>();
		private readonly ConcurrentDictionary<long, Dictionary<string, object>> RemoteAntennaWrappers = new ConcurrentDictionary<long, Dictionary<string, object>>();
		private readonly ConcurrentDictionary<long, Dictionary<string, object>> RemoteLinkWrappers = new ConcurrentDictionary<long, Dictionary<string, object>>();
		private readonly ConcurrentDictionary<long, Dictionary<string, object>> ServerAntennaWrappers = new ConcurrentDictionary<long, Dictionary<string, object>>();

		public readonly Dictionary<IMyModContext, List<AnimationDef>> ModAnimationDefinitions = new Dictionary<IMyModContext, List<AnimationDef>>();

		public MyAPIInterface() { }

		public void Init()
		{
			if (this.Registered) return;
			MyAPIGateway.Utilities.RegisterMessageHandler(3313236685, this.OnModMessage);
			this.Registered = true;
			Logger.Log("ModAPI Initialized");
		}

		public void Close()
		{
			if (!this.Registered) return;
			MyAPIGateway.Utilities.UnregisterMessageHandler(3313236685, this.OnModMessage);
			this.ConstructWrappers.Clear();
			this.JumpGateWrappers.Clear();
			this.BlockBaseWrappers.Clear();
			this.ControllerWrappers.Clear();
			this.DriveWrappers.Clear();
			this.CapacitorWrppers.Clear();
			this.RemoteAntennaWrappers.Clear();
			this.ServerAntennaWrappers.Clear();
			this.ModAnimationDefinitions.Clear();
			this.Registered = false;
			Logger.Log("ModAPI Closed");
		}

		private void OnModMessage(object obj)
		{
			string api_type;
			IMyModContext context = this.HandleApiInit(obj, out api_type);
			if (context == null) Logger.Error($"Failed to connect API - Malformed payload");
			else if (api_type == "modapi") Logger.Log($"Mod API connected - \"{context.ModItem.FriendlyName}\"");
			else if (api_type == "animationapi") Logger.Log($"Animation API connected - \"{context.ModItem.FriendlyName}\"");
			else Logger.Warn($"Unknown API connected - \"{context.ModItem.FriendlyName}\"");
		}

		private IMyModContext HandleApiInit(object obj, out string api_type)
		{
			api_type = null;
			if (obj == null || !(obj is Dictionary<string, object>)) return null;
			Dictionary<string, object> payload = (Dictionary<string, object>) obj;
			if (!payload.ContainsKey("Type")) return null;
			api_type = ((string) payload["Type"]).ToLower();

			if (api_type == "modapi")
			{
				if (!payload.ContainsKey("Callback") || !(payload["Callback"] is Action<Dictionary<string, object>>)) return null;
				Action<Dictionary<string, object>> callback = (Action<Dictionary<string, object>>) payload["Callback"];
				if (!payload.ContainsKey("Version") || !(payload["Version"] is int[])) return null;
				int[] version = (int[]) payload["Version"];
				if (version.Length != 2 || version[0] != MyAPIInterface.ModAPIVersion[0]) return null;
				else if (version[1] < MyAPIInterface.ModAPIVersion[1]) Logger.Warn("ModAPI version is not latest - Current");
				if (!payload.ContainsKey("ModContext") || !(payload["ModContext"] is MyModContext)) return null;
				IMyModContext context = (IMyModContext) payload["ModContext"];
				callback(this.ReturnSessionWrapper());
				return context;
			}
			else if (api_type == "animationapi")
			{
				if (!payload.ContainsKey("Callback") || !(payload["Callback"] is Action<Action<IMyModContext, byte[]>>)) return null;
				Action<Action<IMyModContext, byte[]>> callback = (Action<Action<IMyModContext, byte[]>>) payload["Callback"];
				if (!payload.ContainsKey("Version") || !(payload["Version"] is int[])) return null;
				int[] version = (int[]) payload["Version"];
				if (version.Length != 2 || version[0] != MyAPIInterface.ModAPIVersion[0]) return null;
				else if (version[1] < MyAPIInterface.ModAPIVersion[1]) Logger.Warn("AnimationAPI version is not latest - Current");
				if (!payload.ContainsKey("ModContext") || !(payload["ModContext"] is MyModContext)) return null;
				IMyModContext context = (IMyModContext) payload["ModContext"];
				callback(this.OnModAnimationAdd);
				return context;
			}
			else throw new InvalidOperationException($"Invalid API type: \"{api_type}\"");
		}

		private void OnModAnimationAdd(IMyModContext context, byte[] definition)
		{
			AnimationDef animation = MyAPIGateway.Utilities.SerializeFromBinary<AnimationDef>(definition);
			animation.SourceMod = context.ModItem.Name;
			if (this.ModAnimationDefinitions.ContainsKey(context)) this.ModAnimationDefinitions[context].Add(animation);
			else this.ModAnimationDefinitions[context] = new List<AnimationDef> { animation };
		}

		private Dictionary<string, object> ReturnSessionWrapper()
		{
			return new Dictionary<string, object> {
				["GUID"] = JumpGateUUID.Empty.Packed(),
				["GameTick"] = new object[2] { (Func<ulong>) (() => MyJumpGateModSession.GameTick), null },
				["DebugMode"] = new object[2] { (Func<bool>) (() => MyJumpGateModSession.DebugMode), (Action<bool>) ((value) => MyJumpGateModSession.DebugMode = value) },
				["SessionStatus"] = new object[2] { (Func<byte>) (() => (byte) MyJumpGateModSession.SessionStatus), null },
				["WorldMatrix"] = new object[2] { (Func<MatrixD>) (() => MyJumpGateModSession.WorldMatrix), null },
				["AllSessionEntitiesLoaded"] = new object[2] { (Func<bool>) (() => MyJumpGateModSession.Instance.AllSessionEntitiesLoaded), null },
				["InitializationComplete"] = new object[2] { (Func<bool>) (() => MyJumpGateModSession.Instance.InitializationComplete), null },
				["IsBlockCubeBlockBase"] = (Func<IMyTerminalBlock, bool>) MyJumpGateModSession.IsBlockCubeBlockBase,
				["IsBlockJumpGateController"] = (Func<IMyTerminalBlock, bool>) MyJumpGateModSession.IsBlockJumpGateController,
				["IsBlockJumpGateDrive"] = (Func<IMyTerminalBlock, bool>) MyJumpGateModSession.IsBlockJumpGateDrive,
				["IsBlockJumpGateCapacitor"] = (Func<IMyTerminalBlock, bool>) MyJumpGateModSession.IsBlockJumpGateCapacitor,
				["IsBlockJumpGateRemoteAntenna"] = (Func<IMyTerminalBlock, bool>) MyJumpGateModSession.IsBlockJumpGateRemoteAntenna,
				["IsBlockJumpGateRemoteLink"] = (Func<IMyTerminalBlock, bool>) MyJumpGateModSession.IsBlockJumpGateRemoteLink,
				["IsBlockJumpGateServerAntenna"] = (Func<IMyTerminalBlock, bool>) MyJumpGateModSession.IsBlockJumpGateServerAntenna,
				["GetBlockAsCubeBlockBase"] = (Func<IMyTerminalBlock, Dictionary<string, object>>) ((block) => this.ReturnCubeBlockBaseWrapper(MyJumpGateModSession.GetBlockAsCubeBlockBase(block))),
				["GetBlockAsJumpGateController"] = (Func<IMyTerminalBlock, Dictionary<string, object>>) ((block) => this.ReturnCubeBlockControllerWrapper(MyJumpGateModSession.GetBlockAsJumpGateController(block))),
				["GetBlockAsJumpGateDrive"] = (Func<IMyTerminalBlock, Dictionary<string, object>>) ((block) => this.ReturnCubeBlockDriveWrapper(MyJumpGateModSession.GetBlockAsJumpGateDrive(block))),
				["GetBlockAsJumpGateCapacitor"] = (Func<IMyTerminalBlock, Dictionary<string, object>>) ((block) => this.ReturnCubeBlockCapacitorWrapper(MyJumpGateModSession.GetBlockAsJumpGateCapacitor(block))),
				["GetBlockAsJumpGateRemoteAntenna"] = (Func<IMyTerminalBlock, Dictionary<string, object>>) ((block) => this.ReturnCubeBlockRemoteAntennaWrapper(MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna(block))),
				["GetBlockAsJumpGateRemoteLink"] = (Func<IMyTerminalBlock, Dictionary<string, object>>) ((block) => this.ReturnCubeBlockRemoteLinkWrapper(MyJumpGateModSession.GetBlockAsJumpGateRemoteLink(block))),
				["GetBlockAsJumpGateServerAntenna"] = (Func<IMyTerminalBlock, Dictionary<string, object>>) ((block) => this.ReturnCubeBlockServerAntennaWrapper(MyJumpGateModSession.GetBlockAsJumpGateServerAntenna(block))),
				["GetAllJumpGates"] = (Func<IEnumerable<Dictionary<string, object>>>) (() => MyJumpGateModSession.Instance.GetAllJumpGates().Select(this.ReturnJumpGateWrapper)),
				["GetAllJumpGateGrids"] = (Func<IEnumerable<Dictionary<string, object>>>) (() => MyJumpGateModSession.Instance.GetAllJumpGateGrids().Select(this.ReturnConstructWrapper)),
				["RequestGridDownload"] = (Action<long>) MyJumpGateModSession.Instance.RequestGridDownload,
				["RequestGridsDownload"] = (Action) MyJumpGateModSession.Instance.RequestGridsDownload,
				["AllFirstTickComplete"] = (Func<bool>) MyJumpGateModSession.Instance.AllFirstTickComplete,
				["HasCubeGrid"] = (Func<long, bool>) MyJumpGateModSession.Instance.HasCubeGrid,
				["IsJumpGateGridMultiplayerValid"] = (Func<long, bool>) ((construct_id) => MyJumpGateModSession.Instance.IsJumpGateGridMultiplayerValid(MyJumpGateModSession.Instance.GetJumpGateGrid(construct_id))),
				["HasDuplicateGrid"] = (Func<long, bool>) ((construct_id) => MyJumpGateModSession.Instance.HasDuplicateGrid(MyJumpGateModSession.Instance.GetJumpGateGrid(construct_id))),
				["MoveGrid"] = (Func<long, long, bool>) ((construct_id, new_id) => MyJumpGateModSession.Instance.MoveGrid(MyJumpGateModSession.Instance.GetJumpGateGrid(construct_id), new_id)),
				["AverageGridUpdateTime60"] = (Func<double>) MyJumpGateModSession.Instance.AverageGridUpdateTime60,
				["LocalLongestGridUpdateTime60"] = (Func<double>) MyJumpGateModSession.Instance.LocalLongestGridUpdateTime60,
				["AverageSessionUpdateTime60"] = (Func<double>) MyJumpGateModSession.Instance.AverageSessionUpdateTime60,
				["LocalLongestSessionUpdateTime60"] = (Func<double>) MyJumpGateModSession.Instance.LocalLongestSessionUpdateTime60,
				["GetJumpGateGridL"] = (Func<long, Dictionary<string, object>>) ((grid_id) => this.ReturnConstructWrapper(MyJumpGateModSession.Instance.GetJumpGateGrid(grid_id))),
				["GetJumpGateGridC"] = (Func<IMyCubeGrid, Dictionary<string, object>>) ((grid) => this.ReturnConstructWrapper(MyJumpGateModSession.Instance.GetJumpGateGrid(grid))),
				["GetJumpGateGridG"] = (Func<long[], Dictionary<string, object>>) ((grid_id) => this.ReturnConstructWrapper(MyJumpGateModSession.Instance.GetJumpGateGrid(new JumpGateUUID(grid_id)))),
				["GetUnclosedJumpGateGridL"] = (Func<long, Dictionary<string, object>>) ((grid_id) => this.ReturnConstructWrapper(MyJumpGateModSession.Instance.GetUnclosedJumpGateGrid(grid_id))),
				["GetUnclosedJumpGateGridC"] = (Func<IMyCubeGrid, Dictionary<string, object>>) ((grid) => this.ReturnConstructWrapper(MyJumpGateModSession.Instance.GetUnclosedJumpGateGrid(grid))),
				["GetUnclosedJumpGateGridG"] = (Func<long[], Dictionary<string, object>>) ((grid_id) => this.ReturnConstructWrapper(MyJumpGateModSession.Instance.GetUnclosedJumpGateGrid(new JumpGateUUID(grid_id)))),
				["GetDirectJumpGateGridL"] = (Func<long, Dictionary<string, object>>) ((grid_id) => this.ReturnConstructWrapper(MyJumpGateModSession.Instance.GetDirectJumpGateGrid(grid_id))),
				["GetDirectJumpGateGridC"] = (Func<IMyCubeGrid, Dictionary<string, object>>) ((grid) => this.ReturnConstructWrapper(MyJumpGateModSession.Instance.GetDirectJumpGateGrid(grid))),
				["GetDirectJumpGateGridG"] = (Func<long[], Dictionary<string, object>>) ((grid_id) => this.ReturnConstructWrapper(MyJumpGateModSession.Instance.GetDirectJumpGateGrid(new JumpGateUUID(grid_id)))),
				["GetJumpGate"] = (Func<long[], Dictionary<string, object>>) ((gate_id) => this.ReturnJumpGateWrapper(MyJumpGateModSession.Instance.GetJumpGate(new JumpGateUUID(gate_id)))),
			};
		}

		private Dictionary<string, object> ReturnCubeBlockBaseWrapper(MyCubeBlockBase block)
		{
			Dictionary<string, object> attributes;
			if (block == null) return null;
			else if (this.BlockBaseWrappers.TryGetValue(block.BlockID, out attributes)) return attributes;
			attributes = new Dictionary<string, object> {
				["GUID"] = JumpGateUUID.FromBlock(block).Packed(),
				["IsDirty"] = new object[2] { (Func<bool>) (() => block.IsDirty), null },
				["IsLargeGrid"] = new object[2] { (Func<bool>) (() => block.IsLargeGrid), null },
				["IsSmallGrid"] = new object[2] { (Func<bool>) (() => block.IsSmallGrid), null },
				["IsNullWrapper"] = new object[2] { (Func<bool>) (() => block.IsNullWrapper), null },
				["IsClosed"] = new object[2] { (Func<bool>) (() => block.IsClosed), null },
				["IsPowered"] = new object[2] { (Func<bool>) (() => block.IsPowered), null },
				["IsEnabled"] = new object[2] { (Func<bool>) (() => block.IsEnabled), null },
				["IsWorking"] = new object[2] { (Func<bool>) (() => block.IsWorking), null },
				["BlockID"] = new object[2] { (Func<long>) (() => block.BlockID), null },
				["CubeGridID"] = new object[2] { (Func<long>) (() => block.CubeGridID), null },
				["ConstructID"] = new object[2] { (Func<long>) (() => block.ConstructID), null },
				["OwnerID"] = new object[2] { (Func<long>) (() => block.OwnerID), null },
				["OwnerSteamID"] = new object[2] { (Func<ulong>) (() => block.OwnerSteamID), null },
				["LocalGameTick"] = new object[2] { (Func<ulong>) (() => block.LocalGameTick), null },
				["CubeGridSize"] = new object[2] { (Func<MyCubeSize>) (() => block.CubeGridSize), null },
				["TerminalBlock"] = new object[2] { (Func<IMyUpgradeModule>) (() => block.TerminalBlock), null },
				["LastUpdateDateTimeUTC"] = new object[2] { (Func<DateTime>) (() => block.LastUpdateDateTimeUTC), (Action<DateTime>) ((datetime) => block.LastUpdateDateTimeUTC = datetime) },
				["WorldMatrix"] = new object[2] { (Func<MatrixD>) (() => block.WorldMatrix), null },
				["JumpGateGrid"] = new object[2] { (Func<Dictionary<string, object>>) (() => this.ReturnConstructWrapper(block.JumpGateGrid)), null },
				["SetDirty"] = (Action) block.SetDirty,
				["#HASH"] = (Func<int>) block.GetHashCode,
				["#EQUALS"] = (Func<object, bool>) ((other) => other is IMyTerminalBlock && block.Equals(MyJumpGateModSession.GetBlockAsCubeBlockBase((IMyTerminalBlock) other))),
			};
			this.BlockBaseWrappers[block.BlockID] = attributes;
			return attributes;
		}

		private Dictionary<string, object> ReturnCubeBlockControllerWrapper(MyJumpGateController block)
		{
			Dictionary<string, object> attributes;
			if (block == null) return null;
			else if (this.ControllerWrappers.TryGetValue(block.BlockID, out attributes)) return attributes;
			attributes = new Dictionary<string, object> {
				["GUID"] = JumpGateUUID.FromBlock(block).Packed(),
				["BlockBase"] = this.ReturnCubeBlockBaseWrapper(block),
				["UseGenericLcd"] = new object[2] { (Func<bool>) (() => block.UseGenericLcd), null },
				["SurfaceCount"] = new object[2] { (Func<int>) (() => block.SurfaceCount), null },
				["BlockSettings"] = new object[2] { (Func<Dictionary<string, object>>) (() => block.BlockSettings.ToDictionary()), (Action<Dictionary<string, object>>) ((settings) => block.BlockSettings.FromDictionary(block.AttachedJumpGate(), settings)) },
				["SelectedWaypoint"] = new object[2] { (Func<byte[]>) (() => MyAPIGateway.Utilities.SerializeToBinary(block.BlockSettings.SelectedWaypoint())), (Action<byte[]>) ((waypoint) => block.BlockSettings.SelectedWaypoint(MyAPIGateway.Utilities.SerializeFromBinary<MyJumpGateWaypoint>(waypoint))) },
				["RemoteAntenna"] = new object[2] { (Func<Dictionary<string, object>>) (() => this.ReturnCubeBlockRemoteAntennaWrapper(block.RemoteAntenna)), null },
				["RemoteAntennaChannel"] = new object[2] { (Func<KeyValuePair<Dictionary<string, object>, byte>>) (() => {
					KeyValuePair<MyJumpGateRemoteAntenna, byte> antenna = block.RemoteAntennaChannel;
					return new KeyValuePair<Dictionary<string, object>, byte>(this.ReturnCubeBlockRemoteAntennaWrapper(antenna.Key), antenna.Value);
				}), null },
				["ConnectedRemoteAntenna"] = new object[2] { (Func<Dictionary<string, object>>) (() => this.ReturnCubeBlockRemoteAntennaWrapper(block.ConnectedRemoteAntenna)), null },
				["SetAttachedJumpGate"] = (Action<long[]>) ((gate_id) => block.AttachedJumpGate(MyJumpGateModSession.Instance.GetJumpGate(new JumpGateUUID(gate_id)))),
				["SetAttachedRemoteJumpGate"] = (Action<long[]>) ((gate_id) => block.AttachedRemoteJumpGate(MyJumpGateModSession.Instance.GetJumpGate(new JumpGateUUID(gate_id)))),
				["IsPlayerFactionRelationValid"] = (Func<long, bool>) block.IsFactionRelationValid,
				["IsSteamFactionRelationValid"] = (Func<ulong, bool>) block.IsFactionRelationValid,
				["GetAttachedJumpGate"] = (Func<Dictionary<string, object>>) (() => this.ReturnJumpGateWrapper(block.AttachedJumpGate())),
				["GetWaypointsList"] = (Func<IEnumerable<byte[]>>) (() => block.GetWaypointsList().Select(MyAPIGateway.Utilities.SerializeToBinary)),
			};
			this.ControllerWrappers[block.BlockID] = attributes;
			return attributes;
		}

		private Dictionary<string, object> ReturnCubeBlockDriveWrapper(MyJumpGateDrive block)
		{
			Dictionary<string, object> attributes;
			if (block == null) return null;
			else if (this.DriveWrappers.TryGetValue(block.BlockID, out attributes)) return attributes;
			attributes = new Dictionary<string, object>	{
				["GUID"] = JumpGateUUID.FromBlock(block).Packed(),
				["BlockBase"] = this.ReturnCubeBlockBaseWrapper(block),
				["StoredChargeMW"] = new object[2] { (Func<double>) (() => block.StoredChargeMW), null },
				["MaxRaycastDistance"] = new object[2] { (Func<double>) (() => block.MaxRaycastDistance), null },
				["EmitterEmissiveBrightness"] = new object[2] { (Func<double>) (() => block.EmitterEmissiveBrightness), null },
				["BasePowerDrawMW"] = new object[2] { (Func<double>) (() => block.BasePowerDrawMW), null },
				["JumpGateID"] = new object[2] { (Func<long>) (() => block.JumpGateID), null },
				["DriveEmitterColor"] = new object[2] { (Func<Color>) (() => block.DriveEmitterColor), null },
				["Configuration"] = new object[2] { (Func<Dictionary<string, object>>) (() => block.DriveConfiguration.ToDictionary()), null },
				["SetWattageSinkOverride"] = (Action<double>) block.SetWattageSinkOverride,
				["CycleDriveEmitter"] = (Action<Color, Color, ushort>) block.CycleDriveEmitter,
				["SetDriveEmitterColor"] = (Action<Color>) block.SetDriveEmitterColor,
				["DriveEmitterCycling"] = (Func<bool>) block.DriveEmitterCycling,
				["GetWattageSinkOverride"] = (Func<double>) block.GetWattageSinkOverride,
				["GetCurrentWattageSinkInput"] = (Func<double>) block.GetCurrentWattageSinkInput,
				["DrainStoredCharge"] = (Func<double, double>) block.DrainStoredCharge,
				["GetDriveRaycastEndpoint"] = (Func<double, Vector3D>) block.GetDriveRaycastEndpoint,
				["GetDriveRaycastStartpoint"] = (Func<Vector3D>) block.GetDriveRaycastStartpoint,
			};
			this.DriveWrappers[block.BlockID] = attributes;
			return attributes;
		}

		private Dictionary<string, object> ReturnCubeBlockCapacitorWrapper(MyJumpGateCapacitor block)
		{
			Dictionary<string, object> attributes;
			if (block == null) return null;
			else if (this.CapacitorWrppers.TryGetValue(block.BlockID, out attributes)) return attributes;
			attributes = new Dictionary<string, object> {
				["GUID"] = JumpGateUUID.FromBlock(block).Packed(),
				["BlockBase"] = this.ReturnCubeBlockBaseWrapper(block),
				["StoredChargeMW"] = new object[2] { (Func<double>) (() => block.StoredChargeMW), null },
				["BlockSettings"] = new object[2] { (Func<Dictionary<string, object>>) (() => block.BlockSettings.ToDictionary()), null },
				["Configuration"] = new object[2] { (Func<Dictionary<string, object>>) (() => block.CapacitorConfiguration.ToDictionary()), null },
				["DrainStoredCharge"] = (Func<double, double>) block.DrainStoredCharge,
			};
			this.CapacitorWrppers[block.BlockID] = attributes;
			return attributes;
		}

		private Dictionary<string, object> ReturnCubeBlockRemoteAntennaWrapper(MyJumpGateRemoteAntenna block)
		{
			Dictionary<string, object> attributes;
			if (block == null) return null;
			else if (this.RemoteAntennaWrappers.TryGetValue(block.BlockID, out attributes)) return attributes;
			attributes = new Dictionary<string, object> {
				["GUID"] = JumpGateUUID.FromBlock(block).Packed(),
				["BlockBase"] = this.ReturnCubeBlockBaseWrapper(block),
				["AllowedRemoteSettings"] = new object[2] { (Func<ushort>) (() => (ushort) block.BlockSettings.AllowedRemoteSettings), (Action<ushort>) ((flag) => block.BlockSettings.AllowedRemoteSettings = (MyAllowedRemoteSettings) flag) },
				["BroadcastRange"] = new object[2] { (Func<double>) (() => block.BlockSettings.AntennaRange), (Action<double>) ((range) => block.BlockSettings.AntennaRange = MathHelper.Clamp(range, 0, 1500)) },
				["JumpGateNames"] = new object[2] { (Func<string[]>) (() => block.BlockSettings.JumpGateNames), (Action<string[]>) ((names) => {
					if (names == null || names.Length != MyJumpGateRemoteAntenna.ChannelCount) throw new ArgumentException($"Expected array of length {MyJumpGateRemoteAntenna.ChannelCount}, got array of length {(names?.Length.ToString() ?? "NULL")}");
					block.BlockSettings.JumpGateNames = names;
				}) },
				["ControllerBlockSettings"] = new object[2] { (Func<Dictionary<string, object>[]>) (() => block.BlockSettings.BaseControllerSettings.Select((settings) => settings.ToDictionary()).ToArray()), (Action<Dictionary<string, object>[]>) ((settings) => {
					if (settings == null || settings.Length != MyJumpGateRemoteAntenna.ChannelCount) throw new ArgumentException($"Expected array of length {MyJumpGateRemoteAntenna.ChannelCount}, got array of length {(settings?.Length.ToString() ?? "NULL")}");
					for (byte i = 0; i < MyJumpGateRemoteAntenna.ChannelCount; ++i) block.BlockSettings.BaseControllerSettings[i].FromDictionary(block.GetInboundControlGate(i), settings[i]);
				}) },
				["SetGateForInboundControl"] = (Action<byte, long>) ((channel, id) => block.SetGateForInboundControl(channel, block.JumpGateGrid?.GetJumpGate(id))),
				["SetControllerForOutboundControl"] = (Action<byte, long>) ((channel, id) => block.SetControllerForOutboundControl(channel, block.JumpGateGrid?.GetController(id))),
				["GetJumpGateInboundControlChannel"] = (Func<long[], byte>) ((id) => block.GetJumpGateInboundControlChannel(MyJumpGateModSession.Instance.GetJumpGate(new JumpGateUUID(id)))),
				["GetControllerOutboundControlChannel"] = (Func<long[], byte>) ((id) => block.GetControllerOutboundControlChannel(MyJumpGateModSession.Instance.GetJumpGateBlock<MyJumpGateController>(new JumpGateUUID(id)))),
				["GetInboundControlGate"] = (Func<byte, Dictionary<string, object>>) ((channel) => this.ReturnJumpGateWrapper(block.GetInboundControlGate(channel))),
				["GetConnectedControlledJumpGate"] = (Func<byte, Dictionary<string, object>>) ((channel) => this.ReturnJumpGateWrapper(block.GetConnectedControlledJumpGate(channel))),
				["ReturnCubeBlockControllerWrapper"] = (Func<byte, Dictionary<string, object>>) ((channel) => this.ReturnCubeBlockControllerWrapper(block.GetOutboundControlController(channel))),
				["GetConnectedRemoteAntenna"] = (Func<byte, Dictionary<string, object>>) ((channel) => this.ReturnCubeBlockRemoteAntennaWrapper(block.GetConnectedRemoteAntenna(channel))),
				["RegisteredInboundControlGateIDs"] = (Func<IEnumerable<long>>) block.RegisteredInboundControlGateIDs,
				["RegisteredOutboundControlControllerIDs"] = (Func<IEnumerable<long>>) block.RegisteredOutboundControlControllerIDs,
				["RegisteredInboundControlGates"] = (Func<IEnumerable<Dictionary<string, object>>>) (() => block.RegisteredInboundControlGates().Select(this.ReturnJumpGateWrapper)),
				["RegisteredOutboundControlControllers"] = (Func<IEnumerable<Dictionary<string, object>>>) (() => block.RegisteredOutboundControlControllers().Select(this.ReturnCubeBlockControllerWrapper)),
				["GetNearbyAntennas"] = (Func<IEnumerable<Dictionary<string, object>>>) (() => block.GetNearbyAntennas().Select(this.ReturnCubeBlockRemoteAntennaWrapper)),
				["IsPlayerFactionRelationValid"] = (Func<byte, long, bool>) block.IsFactionRelationValid,
				["IsSteamFactionRelationValid"] = (Func<byte, ulong, bool>) block.IsFactionRelationValid,
				["GetWaypointsList"] = (Func<byte, IEnumerable<byte[]>>) ((channel) => block.GetWaypointsList(channel).Select(MyAPIGateway.Utilities.SerializeToBinary)),
				["GetJumpGateName"] = (Func<byte, string>) block.GetJumpGateName,
			};
			this.RemoteAntennaWrappers[block.BlockID] = attributes;
			return attributes;
		}

		private Dictionary<string, object> ReturnCubeBlockRemoteLinkWrapper(MyJumpGateRemoteLink block)
		{
			Dictionary<string, object> attributes;
			if (block == null) return null;
			else if (this.RemoteLinkWrappers.TryGetValue(block.BlockID, out attributes)) return attributes;
			attributes = new Dictionary<string, object> {
				["GUID"] = JumpGateUUID.FromBlock(block).Packed(),
				["BlockBase"] = this.ReturnCubeBlockBaseWrapper(block),
				["IsLinkParent"] = new object[2] { (Func<bool>) (() => block.IsLinkParent), null },
				["DisplayConnectionEffect"] = new object[2] { (Func<bool>) (() => block.BlockSettings.DisplayConnectionEffect), (Action<bool>) ((value) => block.BlockSettings.DisplayConnectionEffect = value) },
				["IsPhysicallyConnected"] = new object[2] { (Func<bool>) (() => block.BlockSettings.IsPhysicallyConnected), null },
				["AllowedFactionConnections"] = new object[2] { (Func<byte>) (() => (byte) block.BlockSettings.AllowedFactionConnections), (Action<byte>) ((value) => block.BlockSettings.AllowedFactionConnections = (MyFactionDisplayType) value) },
				["ChannelID"] = new object[2] { (Func<ushort>) (() => block.BlockSettings.ChannelID), (Action<ushort>) ((value) => block.BlockSettings.ChannelID = value) },
				["AttachedRemoteLinkID"] = new object[2] { (Func<long>) (() => block.BlockSettings.AttachedRemoteLink), (Action<long>) ((value) => block.BlockSettings.AttachedRemoteLink = value) },
				["MaxConnectionDistance"] = new object[2] { (Func<double>) (() => block.MaxConnectionDistance), null },
				["ConnectionEffectColor"] = new object[2] { (Func<Color>) (() => block.BlockSettings.ConnectionEffectColor), (Action<Color>) ((value) => block.BlockSettings.ConnectionEffectColor = value) },
				["AttachedRemoteLink"] = new object[2] { (Func<Dictionary<string, object>>) (() => this.ReturnCubeBlockRemoteLinkWrapper(block.AttachedRemoteLink)), null },
				["BreakConnection"] = (Action<bool>) block.BreakConnection,
				["Connect"] = (Func<long[], bool>) ((id) => block.Connect(MyJumpGateModSession.Instance.GetJumpGateBlock<MyJumpGateRemoteLink>(new JumpGateUUID(id)))),
				["IsPlayerFactionRelationValid"] = (Func<long, bool>) block.IsFactionRelationValid,
				["IsSteamFactionRelationValid"] = (Func<ulong, bool>) block.IsFactionRelationValid,
				["GetNearbyLinks"] = (Func<IEnumerable<Dictionary<string, object>>>) (() => block.GetNearbyLinks().Select(this.ReturnCubeBlockRemoteLinkWrapper)),
			};
			this.RemoteLinkWrappers[block.BlockID] = attributes;
			return attributes;
		}

		private Dictionary<string, object> ReturnCubeBlockServerAntennaWrapper(MyJumpGateServerAntenna block)
		{
			Dictionary<string, object> attributes;
			if (block == null) return null;
			else if (this.ServerAntennaWrappers.TryGetValue(block.BlockID, out attributes)) return attributes;
			attributes = new Dictionary<string, object> {
				["GUID"] = JumpGateUUID.FromBlock(block).Packed(),
				["BlockBase"] = this.ReturnCubeBlockBaseWrapper(block)
			};
			this.ServerAntennaWrappers[block.BlockID] = attributes;
			return attributes;
		}

		private Dictionary<string, object> ReturnConstructWrapper(MyJumpGateConstruct construct)
		{
			Dictionary<string, object> attributes;
			if (construct == null) return null;
			else if (this.ConstructWrappers.TryGetValue(construct.CubeGridID, out attributes)) return attributes;
			attributes = new Dictionary<string, object> {
				["GUID"] = JumpGateUUID.FromJumpGateGrid(construct).Packed(),
				["Closed"] = new object[2] { (Func<bool>) (() => construct.Closed), null },
				["MarkClosed"] = new object[2] { (Func<bool>) (() => construct.MarkClosed), null },
				["IsSuspended"] = new object[2] { (Func<bool>) (() => construct.IsSuspended), null },
				["MarkUpdateJumpGates"] = new object[2] { (Func<bool>) (() => construct.MarkUpdateJumpGates), null },
				["IsDirty"] = new object[2] { (Func<bool>) (() => construct.IsDirty), null },
				["FullyInitialized"] = new object[2] { (Func<bool>) (() => construct.FullyInitialized), null },
				["CubeGridID"] = new object[2] { (Func<long>) (() => construct.CubeGridID), null },
				["LongestUpdateTime"] = new object[2] { (Func<double>) (() => construct.LongestUpdateTime), null },
				["PrimaryCubeGridCustomName"] = new object[2] { (Func<string>) (() => construct.PrimaryCubeGridCustomName), null },
				["LastUpdateDateTimeUTC"] = new object[2] { (Func<DateTime>) (() => construct.LastUpdateDateTimeUTC), (Action<DateTime>) ((datetime) => construct.LastUpdateDateTimeUTC = datetime) },
				["CubeGrid"] = new object[2] { (Func<IMyCubeGrid>) (() => construct.CubeGrid), null },
				["BatchingGate"] = new object[2] { (Func<Dictionary<string, object>>) (() => this.ReturnJumpGateWrapper(construct.BatchingGate)), null },
				["#HASH"] = (Func<int>) construct.GetHashCode,
				["#EQUALS"] = (Func<object, bool>) ((id) => id is long && construct.Equals(MyJumpGateModSession.Instance.GetJumpGateGrid((long) id))),
				["RemapJumpGateIDs"] = (Action) construct.RemapJumpGateIDs,
				["Destroy"] = (Action) construct.Destroy,
				["ClearResetTimes"] = (Action) construct.ClearResetTimes,
				["MarkGatesForUpdate"] = (Action) construct.MarkGatesForUpdate,
				["RequestGateUpdate"] = (Action) construct.RequestGateUpdate,
				["SetDirty"] = (Action) construct.SetDirty,
				["SetConstructStaticness"] = (Action<bool>) construct.SetConstructStaticness,
				["GetBlocksIntersectingEllipsoid"] = (Action<byte[], ICollection<IMySlimBlock>, ICollection<IMySlimBlock>>) ((serialized, contained, unconctained) => {
					BoundingEllipsoidD ellipsoid = BoundingEllipsoidD.FromSerialized(serialized, 0);
					construct.GetBlocksIntersectingEllipsoid(ref ellipsoid, contained, unconctained);
				}),
				["IsValid"] = (Func<bool>) construct.IsValid,
				["IsStatic"] = (Func<bool>) construct.IsStatic,
				["HasCommLink"] = (Func<bool>) construct.HasCommLink,
				["AtLeastOneUpdate"] = (Func<bool>) construct.AtLeastOneUpdate,
				["HasCubeGridByID"] = (Func<long, bool>) construct.HasCubeGrid,
				["HasCubeGrid"] = (Func<IMyCubeGrid, bool>) construct.HasCubeGrid,
				["IsConstructCommLinked"] = (Func<long, bool>) ((id) => construct.IsConstructCommLinked(MyJumpGateModSession.Instance.GetJumpGateGrid(id))),
				["IsBeaconWithinReverseBroadcastSphere"] = (Func<IMyBeacon, bool>) ((beacon) => (beacon == null) ? false : construct.IsBeaconWithinReverseBroadcastSphere(new MyBeaconLinkWrapper(beacon))),
				["IsBeaconWrapperWithinReverseBroadcastSphere"] = (Func<byte[], bool>) ((beacon) => (beacon == null) ? false : construct.IsBeaconWithinReverseBroadcastSphere(MyAPIGateway.Utilities.SerializeFromBinary<MyBeaconLinkWrapper>(beacon))),
				["IsConstructWithinBoundingEllipsoid"] = (Func<byte[], bool>) ((serialized) => {
					BoundingEllipsoidD ellipsoid = BoundingEllipsoidD.FromSerialized(serialized, 0);
					return construct.IsConstructWithinBoundingEllipsoid(ref ellipsoid);
				}),
				["GetConstructBlockCount"] = (Func<Func<IMySlimBlock, bool>, int>) construct.GetConstructBlockCount,
				["GetInvalidationReason"] = (Func<byte>) (() => (byte) construct.GetInvalidationReason()),
				["ConstructMass"] = (Func<double>) construct.ConstructMass,
				["SyphonConstructCapacitorPower"] = (Func<double, double>) construct.SyphonConstructCapacitorPower,
				["AverageUpdateTime60"] = (Func<double>) construct.AverageUpdateTime60,
				["LocalLongestUpdateTime60"] = (Func<double>) construct.LocalLongestUpdateTime60,
				["CalculateTotalAvailableInstantPower"] = (Func<long, double>) construct.CalculateTotalAvailableInstantPower,
				["CalculateTotalPossibleInstantPower"] = (Func<long, double>) construct.CalculateTotalPossibleInstantPower,
				["CalculateTotalMaxPossibleInstantPower"] = (Func<long, double>) construct.CalculateTotalMaxPossibleInstantPower,
				["ConstructVolumeCenter"] = (Func<Vector3D>) construct.ConstructVolumeCenter,
				["ConstructMassCenter"] = (Func<Vector3D>) construct.ConstructMassCenter,
				["IsPositionInsideAnySubgrid"] = (Func<Vector3D, IMyCubeGrid>) construct.IsPositionInsideAnySubgrid,
				["GetMainCubeGrid"] = (Func<IMyCubeGrid>) construct.GetMainCubeGrid,
				["GetCubeGrid"] = (Func<long, IMyCubeGrid>) construct.GetCubeGrid,
				["SplitGrid"] = (Func<IMyCubeGrid, List<IMySlimBlock>, IMyCubeGrid>) construct.SplitGrid,
				["GetLaserAntenna"] = (Func<long, IMyLaserAntenna>) construct.GetLaserAntenna,
				["GetRadioAntenna"] = (Func<long, IMyRadioAntenna>) construct.GetRadioAntenna,
				["GetBeacon"] = (Func<long, IMyBeacon>) construct.GetBeacon,
				["GetConstructBlocks"] = (Func<IEnumerator<IMySlimBlock>>) construct.GetConstructBlocks,
				["GetCombinedAABB"] = (Func<BoundingBoxD>) construct.GetCombinedAABB,
				["GetJumpGate"] = (Func<long, Dictionary<string, object>>) ((id) => this.ReturnJumpGateWrapper(construct.GetJumpGate(id))),
				["GetCubeBlock"] = (Func<long, Dictionary<string, object>>) ((id) => this.ReturnCubeBlockBaseWrapper(construct.GetCubeBlock(id))),
				["GetCapacitor"] = (Func<long, Dictionary<string, object>>) ((id) => this.ReturnCubeBlockCapacitorWrapper(construct.GetCapacitor(id))),
				["GetDrive"] = (Func<long, Dictionary<string, object>>) ((id) => this.ReturnCubeBlockDriveWrapper(construct.GetDrive(id))),
				["GetRemoteAntenna"] = (Func<long, Dictionary<string, object>>) ((id) => this.ReturnCubeBlockRemoteAntennaWrapper(construct.GetRemoteAntenna(id))),
				["GetRemoteLink"] = (Func<long, Dictionary<string, object>>) ((id) => this.ReturnCubeBlockRemoteLinkWrapper(construct.GetRemoteLink(id))),
				["GetServerAntenna"] = (Func<long, Dictionary<string, object>>) ((id) => this.ReturnCubeBlockServerAntennaWrapper(construct.GetServerAntenna(id))),
				["GetController"] = (Func<long, Dictionary<string, object>>) ((id) => this.ReturnCubeBlockControllerWrapper(construct.GetController(id))),
				["GetCubeGridPhysics"] = (Func<MyPhysicsComponentBase>) construct.GetCubeGridPhysics,
				["GetAttachedJumpGateControllers"] = (Func<IEnumerable<Dictionary<string, object>>>) (() => construct.GetAttachedJumpGateControllers().Select(this.ReturnCubeBlockControllerWrapper)),
				["GetAttachedJumpGateDrives"] = (Func<IEnumerable<Dictionary<string, object>>>) (() => construct.GetAttachedJumpGateDrives().Select(this.ReturnCubeBlockDriveWrapper)),
				["GetAttachedUnassociatedJumpGateDrives"] = (Func<IEnumerable<Dictionary<string, object>>>) (() => construct.GetAttachedUnassociatedJumpGateDrives().Select(this.ReturnCubeBlockDriveWrapper)),
				["GetAttachedJumpGateCapacitors"] = (Func<IEnumerable<Dictionary<string, object>>>) (() => construct.GetAttachedJumpGateCapacitors().Select(this.ReturnCubeBlockCapacitorWrapper)),
				["GetAttachedJumpGateRemoteAntennas"] = (Func<IEnumerable<Dictionary<string, object>>>) (() => construct.GetAttachedJumpGateRemoteAntennas().Select(this.ReturnCubeBlockRemoteAntennaWrapper)),
				["GetAttachedJumpGateRemoteLinks"] = (Func<IEnumerable<Dictionary<string, object>>>) (() => construct.GetAttachedJumpGateRemoteLinks().Select(this.ReturnCubeBlockRemoteLinkWrapper)),
				["GetAttachedJumpGateServerAntennas"] = (Func<IEnumerable<Dictionary<string, object>>>) (() => construct.GetAttachedJumpGateServerAntennas().Select(this.ReturnCubeBlockServerAntennaWrapper)),
				["GetAttachedLaserAntennas"] = (Func<IEnumerable<IMyLaserAntenna>>) construct.GetAttachedLaserAntennas,
				["GetAttachedRadioAntennas"] = (Func<IEnumerable<IMyRadioAntenna>>) construct.GetAttachedRadioAntennas,
				["GetAttachedBeacons"] = (Func<IEnumerable<IMyBeacon>>) construct.GetAttachedBeacons,
				["GetJumpGates"] = (Func<IEnumerable<Dictionary<string, object>>>) (() => construct.GetJumpGates().Select(this.ReturnJumpGateWrapper)),
				["GetCubeGrids"] = (Func<IEnumerable<IMyCubeGrid>>) construct.GetCubeGrids,
				["GetCommLinkedJumpGateGrids"] = (Func<IEnumerable<Dictionary<string, object>>>) (() => construct.GetCommLinkedJumpGateGrids().Select(this.ReturnConstructWrapper)),
				["GetBeaconsWithinReverseBroadcastSphere"] = (Func<IEnumerable<byte[]>>) (() => construct.GetBeaconsWithinReverseBroadcastSphere().Select(MyAPIGateway.Utilities.SerializeToBinary)),
			};
			this.ConstructWrappers[construct.CubeGridID] = attributes;
			return attributes;
		}

		private Dictionary<string, object> ReturnJumpGateWrapper(MyJumpGate gate)
		{
			Dictionary<string, object> attributes;
			if (gate == null) return null;
			JumpGateUUID uuid = JumpGateUUID.FromJumpGate(gate);
			if (this.JumpGateWrappers.TryGetValue(uuid, out attributes)) return attributes;
			ConcurrentDictionary<int, Action<Dictionary<string, object>, MyEntity, bool>> entity_entered_callbacks = new ConcurrentDictionary<int, Action<Dictionary<string, object>, MyEntity, bool>>();

			Action<MyJumpGate, MyEntity, bool> entity_entered_callback = (caller, entity, entered) => {
				foreach (KeyValuePair<int, Action<Dictionary<string, object>, MyEntity, bool>> pair in entity_entered_callbacks)
				{
					pair.Value(this.ReturnJumpGateWrapper(caller), entity, entered);
				}
			};

			attributes = new Dictionary<string, object> {
				["GUID"] = JumpGateUUID.FromJumpGate(gate).Packed(),
				["Guid"] = new object[2] { (Func<long[]>) (() => JumpGateUUID.FromJumpGate(gate).Packed()), null },
				["MarkClosed"] = new object[2] { (Func<bool>) (() => gate.MarkClosed), null },
				["Closed"] = new object[2] { (Func<bool>) (() => gate.Closed), null },
				["IsDirty"] = new object[2] { (Func<bool>) (() => gate.IsDirty), null },
				["Status"] = new object[2] { (Func<byte>) (() => (byte) gate.Status), null },
				["Phase"] = new object[2] { (Func<byte>) (() => (byte) gate.Phase), null },
				["JumpGateID"] = new object[2] { (Func<long>) (() => gate.JumpGateID), null },
				["JumpFailureReason"] = new object[2] { (Func<KeyValuePair<byte, bool>>) (() => new KeyValuePair<byte, bool>((byte) gate.JumpFailureReason.Key, gate.JumpFailureReason.Value)), null },
				["LocalJumpNode"] = new object[2] { (Func<Vector3D>) (() => gate.LocalJumpNode), null },
				["WorldJumpNode"] = new object[2] { (Func<Vector3D>) (() => gate.WorldJumpNode), null },
				["TrueEndpoint"] = new object[2] { (Func<Vector3D?>) (() => gate.TrueEndpoint), null },
				["JumpNodeVelocity"] = new object[2] { (Func<Vector3D>) (() => gate.JumpNodeVelocity), null },
				["PrimaryDrivePlane"] = new object[2] { (Func<PlaneD?>) (() => gate.PrimaryDrivePlane), null },
				["ConstructMatrix"] = new object[2] { (Func<MatrixD>) (() => gate.ConstructMatrix), null },
				["LastUpdateDateTimeUTC"] = new object[2] { (Func<DateTime>) (() => gate.LastUpdateDateTimeUTC), (Action<DateTime>) ((datetime) => gate.LastUpdateDateTimeUTC = datetime) },
				["JumpGateConfiguration"] = new object[2] { (Func<Dictionary<string, object>>) (() => gate.JumpGateConfiguration.ToDictionary()), null },
				["Controller"] = new object[2] { (Func<Dictionary<string, object>>) (() => this.ReturnCubeBlockControllerWrapper(gate.Controller)), null },
				["RemoteAntenna"] = new object[2] { (Func<Dictionary<string, object>>) (() => this.ReturnCubeBlockRemoteAntennaWrapper(gate.RemoteAntenna)), null },
				["ServerAntenna"] = new object[2] { (Func<Dictionary<string, object>>) (() => this.ReturnCubeBlockServerAntennaWrapper(gate.ServerAntenna)), null },
				["SenderGate"] = new object[2] { (Func<Dictionary<string, object>>) (() => this.ReturnJumpGateWrapper(gate.SenderGate)), null },
				["JumpGateGrid"] = new object[2] { (Func<Dictionary<string, object>>) (() => this.ReturnConstructWrapper(gate.JumpGateGrid)), null },
				["JumpEllipse"] = new object[2] { (Func<byte[]>) (() => gate.JumpEllipse.ToSerialized()), null },
				["ShearEllipse"] = new object[2] { (Func<byte[]>) (() => gate.ShearEllipse.ToSerialized()), null },
				["LocalDriveIntersectNodes"] = new object[2] { (Func<ImmutableList<Vector3D>>) (() => gate.LocalDriveIntersectNodes), null },
				["WorldDriveIntersectNodes"] = new object[2] { (Func<ImmutableList<Vector3D>>) (() => gate.WorldDriveIntersectNodes), null },
				["GetFailureDescription"] = (Func<byte, string>) ((failure) => MyJumpGate.GetFailureDescription((MyJumpFailReason) failure)),
				["GetFailureSound"] = (Func<byte, bool, string>) ((failure, isinit) => MyJumpGate.GetFailureSound((MyJumpFailReason) failure, isinit)),
				["#HASH"] = (Func<int>) gate.GetHashCode,
				["#EQUALS"] = (Func<object, bool>) ((id) => id is long[] && gate.Equals(MyJumpGateModSession.Instance.GetJumpGate(new JumpGateUUID((long[]) id)))),
				["#DEINIT"] = (Action) (() => gate.EntityEnterered -= entity_entered_callback),
				["RequestGateUpdate"] = (Action) gate.RequestGateUpdate,
				["Jump"] = (Action<IMyTerminalBlock, Dictionary<string, object>>) ((block, settings) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					MyJumpGateController.MyControllerBlockSettingsStruct controller_settings = (controller == null || settings == null) ? gate.Controller?.BlockSettings : new MyJumpGateController.MyControllerBlockSettingsStruct(controller, settings);
					gate.Jump(controller_settings);
				}),
				["JumpToVoid"] = (Action<double, IMyTerminalBlock, Dictionary<string, object>>) ((distance, block, settings) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					MyJumpGateController.MyControllerBlockSettingsStruct controller_settings = (controller == null || settings == null) ? gate.Controller?.BlockSettings : new MyJumpGateController.MyControllerBlockSettingsStruct(controller, settings);
					gate.JumpToVoid(controller_settings, distance);
				}),
				["JumpFromVoid"] = (Action<List<Dictionary<string, object>>, List<List<IMyCubeGrid>>, IMyTerminalBlock, Dictionary<string, object>>) ((prefabs, spawned_grids, block, settings) => {
					MyJumpGateController controller = MyJumpGateModSession.GetBlockAsJumpGateController(block);
					MyJumpGateController.MyControllerBlockSettingsStruct controller_settings = (controller == null || settings == null) ? gate.Controller?.BlockSettings : new MyJumpGateController.MyControllerBlockSettingsStruct(controller, settings);
					gate.JumpFromVoid(controller_settings, prefabs?.Select((info) => new MyPrefabInfo(info)).ToList(), spawned_grids);
				}),
				["SetColliderDirty"] = (Action) gate.SetColliderDirty,
				["SetColliderDirtyMP"] = (Action) gate.SetColliderDirtyMP,
				["SetJumpSpaceEllipsoidDirty"] = (Action) gate.SetJumpSpaceEllipsoidDirty,
				["SetDirty"] = (Action) gate.SetDirty,
				["StopSoundByName"] = (Action<string>) gate.StopSound,
				["StopSoundByID"] = (Action<ulong?>) gate.StopSound,
				["SetSoundVolume"] = (Action<ulong?, float>) gate.SetSoundVolume,
				["SetSoundDistance"] = (Action<ulong?, float?>) gate.SetSoundDistance,
				["StopSounds"] = (Action) gate.StopSounds,
				["CanDoSyphonGridPower"] = (Action<double, ulong, Action<bool>, bool>) gate.CanDoSyphonGridPower,
				["CancelJump"] = (Action) gate.CancelJump,
				["OnEntityCollision"] = (Action<Action<Dictionary<string, object>, MyEntity, bool>>) ((callback) => entity_entered_callbacks.TryAdd(callback.GetHashCode(), callback)),
				["OffEntityCollision"] = (Action<Action<Dictionary<string, object>, MyEntity, bool>>) ((callback) => entity_entered_callbacks.Remove(callback.GetHashCode())),
				["IsPointInHudBroadcastRange"] = (Func<Vector3D, bool>) ((pos) => gate.IsPointInHudBroadcastRange(ref pos)),
				["IsEntityValidForJumpSpace"] = (Func<MyEntity, bool>) gate.IsEntityValidForJumpSpace,
				["IsValid"] = (Func<bool>) gate.IsValid,
				["IsControlled"] = (Func<bool>) gate.IsControlled,
				["IsComplete"] = (Func<bool>) gate.IsComplete,
				["IsIdle"] = (Func<bool>) gate.IsIdle,
				["IsJumping"] = (Func<bool>) gate.IsJumping,
				["IsLargeGrid"] = (Func<bool>) gate.IsLargeGrid,
				["IsSmallGrid"] = (Func<bool>) gate.IsSmallGrid,
				["IsSuspended"] = (Func<bool>) gate.IsSuspended,
				["JumpSpaceColliderStatus"] = (Func<byte>) (() => (byte) gate.JumpSpaceColliderStatus()),
				["GetInvalidationReason"] = (Func<byte>) (() => (byte) gate.GetInvalidationReason()),
				["GetJumpSpaceEntityCount"] = (Func<bool, Func<MyEntity, bool>, int>) gate.GetJumpSpaceEntityCount,
				["PlaySound"] = (Func<string, float?, float, Vector3D?, ulong?>) gate.PlaySound,
				["SyphonGridDrivePower"] = (Func<double, double>) gate.SyphonGridDrivePower,
				["CalculateDistanceRatio"] = (Func<Vector3D, double>) ((endpoint) => gate.CalculateDistanceRatio(ref endpoint)),
				["CalculatePowerFactorFromDistanceRatio"] = (Func<double, double>) gate.CalculatePowerFactorFromDistanceRatio,
				["CalculateUnitPowerRequiredForJump"] = (Func<Vector3D, double>) ((endpoint) => gate.CalculateUnitPowerRequiredForJump(ref endpoint)),
				["CalculateMaxGateDistance"] = (Func<double>) gate.CalculateMaxGateDistance,
				["CalculateTotalRequiredPower"] = (Func<Vector3D?, bool?, double?, double>) gate.CalculateTotalRequiredPower,
				["CalculateTotalAvailableInstantPower"] = (Func<double>) gate.CalculateTotalAvailableInstantPower,
				["CalculateTotalPossibleInstantPower"] = (Func<double>) gate.CalculateTotalPossibleInstantPower,
				["CalculateTotalMaxPossibleInstantPower"] = (Func<double>) gate.CalculateTotalMaxPossibleInstantPower,
				["CalculateDrivesRequiredForDistance"] = (Func<double, int>) gate.CalculateDrivesRequiredForDistance,
				["JumpNodeRadius"] = (Func<double>) gate.JumpNodeRadius,
				["CubeGridSize"] = (Func<MyCubeSize>) gate.CubeGridSize,
				["GetEffectiveJumpEllipse"] = (Func<byte[]>) (() => gate.GetEffectiveJumpEllipse().ToSerialized()),
				["GetWorldMatrix"] = (Func<bool, bool, MatrixD>) gate.GetWorldMatrix,
				["GetName"] = (Func<string>) gate.GetName,
				["GetPrintableName"] = (Func<string>) gate.GetPrintableName,
				["GetJumpGateDrives"] = (Func<IEnumerable<Dictionary<string, object>>>) (() => gate.GetJumpGateDrives().Select(this.ReturnCubeBlockDriveWrapper)),
				["GetWorkingJumpGateDrives"] = (Func<IEnumerable<Dictionary<string, object>>>) (() => gate.GetWorkingJumpGateDrives().Select(this.ReturnCubeBlockDriveWrapper)),
				["GetUninitializedEntititesInJumpSpace"] = (Func<bool, IEnumerable<KeyValuePair<long, float>>>) gate.GetUninitializedEntititesInJumpSpace,
				["GetEntitiesInJumpSpace"] = (Func<bool, IEnumerable<KeyValuePair<MyEntity, float>>>) gate.GetEntitiesInJumpSpace,
			};

			gate.EntityEnterered += entity_entered_callback;
			this.JumpGateWrappers[uuid] = attributes;
			return attributes;
		}

		/// <summary>
		/// Creates a new session API interface for PB scripts
		/// </summary>
		/// <param name="version">The API version being used</param>
		/// <param name="callback">The callback to accept session data</param>
		/// <returns>A key value pair containing two booleans:<br />1: Whether the session handle was created<br />2: Value checking if the version is up to date - will be false if session handle failed</returns>
		public KeyValuePair<bool, bool> HandleModApiScriptingInit(int[] version, Action<Dictionary<string, object>> callback)
		{
			bool version_ok = false;
			if (version == null || callback == null || !(version is int[]) || !(callback is Action<Dictionary<string, object>>)) return new KeyValuePair<bool, bool>(false, false);
			else if (version.Length != 2 || version[0] != MyAPIInterface.ModAPIVersion[0]) return new KeyValuePair<bool, bool>(false, false);
			else if (version[1] < MyAPIInterface.ModAPIVersion[1]) Logger.Warn("Scripting ModAPI version is not latest - Current");
			else version_ok = true;
			callback(this.ReturnSessionWrapper());
			return new KeyValuePair<bool, bool>(true, version_ok);
		}
	}
}