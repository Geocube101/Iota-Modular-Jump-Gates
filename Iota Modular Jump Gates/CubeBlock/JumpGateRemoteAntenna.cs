using IOTA.ModularJumpGates.Terminal;
using IOTA.ModularJumpGates.Util;
using IOTA.ModularJumpGates.Util.ConcurrentCollections;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace IOTA.ModularJumpGates.CubeBlock
{
	public enum MyAllowedRemoteSettings : ushort {
		NAME = 1, DESTINATIONS = 2, ANIMATIONS = 4, ROUTING = 8, ENTITY_FILTER = 16, AUTO_ACTIVATE = 32, COMM_LINKAGE = 64, JUMPSPACE = 128, COLOR_OVERRIDE = 256, VECTOR_OVERRIDE = 512, JUMP = 1024, NOJUMP = 2048,
		ALL = 4095, NONE = 0,
	}

	/// <summary>
	/// Game logic for jump gate remote antennas
	/// </summary>
	[MyEntityComponentDescriptor(typeof(MyObjectBuilder_UpgradeModule), false, "IOTA.JumpGate.JumpGateRemoteAntenna.Large", "IOTA.JumpGate.JumpGateRemoteAntenna.Small")]
	internal class MyJumpGateRemoteAntenna : MyCubeBlockBase
	{
		/// <summary>
		/// Stores block data for this block type
		/// </summary>
		[ProtoContract]
		public sealed class MyRemoteAntennaBlockSettingsStruct
		{
			[ProtoMember(1)]
			public long[] InboundConnectionJumpGates;
			[ProtoMember(2)]
			public long[] OutboundConnectionControllers;
			[ProtoMember(3)]
			public MyAllowedRemoteSettings AllowedRemoteSettings = MyAllowedRemoteSettings.ALL;
			[ProtoMember(4)]
			public double AntennaRange = MyJumpGateRemoteAntenna.MaxCollisionDistance;
			[ProtoMember(5)]
			public string[] JumpGateNames;
			[ProtoMember(6)]
			public MyJumpGateController.MyControllerBlockSettingsStruct[] BaseControllerSettings;

			public MyRemoteAntennaBlockSettingsStruct() { }

			public void Validate()
			{
				this.InboundConnectionJumpGates = this.InboundConnectionJumpGates ?? new long[MyJumpGateRemoteAntenna.ChannelCount] { -1, -1, -1 };
				this.OutboundConnectionControllers = this.OutboundConnectionControllers ?? new long[MyJumpGateRemoteAntenna.ChannelCount] { -1, -1, -1 };
				this.JumpGateNames = this.JumpGateNames ?? new string[MyJumpGateRemoteAntenna.ChannelCount] { "", "", "" };
				this.BaseControllerSettings = this.BaseControllerSettings ?? new MyJumpGateController.MyControllerBlockSettingsStruct[MyJumpGateRemoteAntenna.ChannelCount] { null, null, null };
				
				for (byte i = 0; i < MyJumpGateRemoteAntenna.ChannelCount; ++i)
				{
					this.BaseControllerSettings[i] = this.BaseControllerSettings[i] ?? new MyJumpGateController.MyControllerBlockSettingsStruct();
					this.BaseControllerSettings[i].JumpGateName(this.JumpGateNames[i]);
				}
			}

			public void AllowSetting(MyAllowedRemoteSettings setting, bool flag)
			{
				if (flag) this.AllowedRemoteSettings |= setting;
				else this.AllowedRemoteSettings &= ~setting;
			}
		}

		#region Public Static Variables
		/// <summary>
		/// The number of available antenna channels
		/// </summary>
		public const byte ChannelCount = 3;

		/// <summary>
		/// The maximum distance (in meters) to scan for nearby antennas
		/// </summary>
		public const double MaxCollisionDistance = 1500;
		#endregion

		#region Private Variables
		/// <summary>
		/// The current channel being shown on emissives
		/// </summary>
		private byte ConnectionEmissiveIndex = 0;

		/// <summary>
		/// The last game tick at which nearby antennas were updated
		/// </summary>
		private ulong LastNearbyAntennasUpdateTime = 0;

		/// <summary>
		/// Mutex object for exclusive read-write operations on the NearbyAntennas list
		/// </summary>
		private object NearbyAntennasMutex = new object();

		/// <summary>
		/// Mutex object for exclusive read-write operations on the WaypointsList
		/// </summary>
		private object[] WaypointsListMutex = new object[MyJumpGateRemoteAntenna.ChannelCount] { new object(), new object(), new object() };

		/// <summary>
		/// The list of jump gates listening from this antenna
		/// </summary>
		private readonly long[] InboundConnectionJumpGates = new long[MyJumpGateRemoteAntenna.ChannelCount] { -1, -1, -1 };

		/// <summary>
		/// The list of controllers listening to this antenna
		/// </summary>
		private readonly long[] OutboundConnectionControllers = new long[MyJumpGateRemoteAntenna.ChannelCount] { -1, -1, -1 };

		/// <summary>
		/// The list of active antenna connections
		/// </summary>
		private readonly MyJumpGateRemoteAntenna[] ConnectionThreads = new MyJumpGateRemoteAntenna[MyJumpGateRemoteAntenna.ChannelCount] { null, null, null };

		/// <summary>
		/// The collision detector for finding nearby antennas
		/// </summary>
		private MyEntity AntennaDetector = null;

		/// <summary>
		/// Queue of pairings that cannot be completed
		/// </summary>
		private KeyValuePair<JumpGateUUID, JumpGateUUID>?[] UninitializedPairings = new KeyValuePair<JumpGateUUID, JumpGateUUID>?[MyJumpGateRemoteAntenna.ChannelCount] { null, null, null };

		/// <summary>
		/// A list of all nearby antennas
		/// </summary>
		private List<MyJumpGateRemoteAntenna> NearbyAntennas = new List<MyJumpGateRemoteAntenna>();

		/// <summary>
		/// Client-side only<br />
		/// Stores all applicable waypoints for this block
		/// </summary>
		private List<MyJumpGateWaypoint>[] WaypointsList = new List<MyJumpGateWaypoint>[MyJumpGateRemoteAntenna.ChannelCount] { new List<MyJumpGateWaypoint>(), new List<MyJumpGateWaypoint>(), new List<MyJumpGateWaypoint>() };

		/// <summary>
		/// A collection of all topmost entities within search range
		/// </summary>
		private ConcurrentLinkedHashSet<long> NearbyEntities = new ConcurrentLinkedHashSet<long>();
		#endregion

		#region Temporary Collections
		/// <summary>
		/// Temporary list of construct grids
		/// </summary>
		private List<IMyCubeGrid> TEMP_DetailedInfoConstructsList = new List<IMyCubeGrid>();
		#endregion

		#region Public Variables
		/// <summary>
		/// The current channel who's gate settings to modify
		/// </summary>
		public byte CurrentTerminalChannel = 0;

		/// <summary>
		/// The block data for this block
		/// </summary>
		public MyRemoteAntennaBlockSettingsStruct BlockSettings { get; private set; } = new MyRemoteAntennaBlockSettingsStruct();

		/// <summary>
		/// Callbacks for when an antenna entered range<br />
		/// Callback takes the form "Action(this_antenna, other_antenna, is_entering)"
		/// </summary>
		public event Action<MyJumpGateRemoteAntenna, MyJumpGateRemoteAntenna, bool> OnAntennaEnteredRange;

		/// <summary>
		/// Callbacks for when an antenna connection is established<br />
		/// Callback takes the form "Action(this_antenna, connected_controller, connected_gate, channel, connection_formed)"
		/// </summary>
		public event Action<MyJumpGateRemoteAntenna, MyJumpGateController, MyJumpGate, byte, bool> OnAntennaConnection;
		#endregion

		#region Constructors
		public MyJumpGateRemoteAntenna() : base() { }

		/// <summary>
		/// Creates a new null wrapper
		/// </summary>
		/// <param name="serialized">The serialized block data</param>
		/// <param name="parent">The containing grid or null to calculate</param>
		public MyJumpGateRemoteAntenna(MySerializedJumpGateRemoteAntenna serialized, MyJumpGateConstruct parent = null) : base(serialized, parent)
		{
			this.FromSerialized(serialized, parent);
		}
		#endregion

		#region "CubeBlockBase" Methods
		protected override void Clean()
		{
			base.Clean();
			this.NearbyEntities.Clear();
			lock (this.NearbyAntennasMutex) this.NearbyAntennas.Clear();
			this.NearbyEntities = null;
			this.NearbyAntennas = null;

			for (byte i = 0; i < MyJumpGateRemoteAntenna.ChannelCount; ++i)
			{
				List<MyJumpGateWaypoint> waypoints = this.WaypointsList[i];
				lock (this.WaypointsListMutex[i]) waypoints.Clear();
				this.WaypointsList[i] = null;
				this.WaypointsListMutex[i] = null;
			}
		}

		protected override void UpdateOnceAfterInit()
		{
			base.UpdateOnceAfterInit();
			if (MyNetworkInterface.IsStandaloneMultiplayerClient || this.TerminalBlock?.CubeGrid?.Physics == null) return;
			this.AntennaDetector = new MyEntity();
			this.AntennaDetector.EntityId = 0;
			this.AntennaDetector.Init(new StringBuilder($"JumpGateRemoteAntenna_{this.BlockID}"), null, (MyEntity) this.Entity, 1f);
			this.AntennaDetector.Save = false;
			this.AntennaDetector.Flags = EntityFlags.IsNotGamePrunningStructureObject | EntityFlags.NeedsWorldMatrix;
			PhysicsSettings settings = MyAPIGateway.Physics.CreateSettingsForDetector(this.AntennaDetector, this.OnEntityCollision, MyJumpGateModSession.WorldMatrix, Vector3D.Zero, RigidBodyFlag.RBF_KINEMATIC, 15, true);
			MyAPIGateway.Physics.CreateSpherePhysics(settings, (float) MyJumpGateRemoteAntenna.MaxCollisionDistance);
			MyAPIGateway.Entities.AddEntity(this.AntennaDetector);
			Logger.Debug($"CREATED_COLLIDER REMOTE_ANTENNA={this.BlockID}, COLLIDER={this.AntennaDetector.DisplayName}", 4);

			BoundingSphereD bounding_sphere = new BoundingSphereD(this.WorldMatrix.Translation, MyJumpGateRemoteAntenna.MaxCollisionDistance);
			List<MyEntity> entities = new List<MyEntity>();
			MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref bounding_sphere, entities);
			this.NearbyEntities.Clear();
			foreach (MyEntity entity in entities) this.OnEntityCollision(entity, true);

			for (byte i = 0; i < MyJumpGateRemoteAntenna.ChannelCount; ++i)
			{
				long gateid = this.InboundConnectionJumpGates[i];
				if (gateid == -1) continue;
				MyJumpGate jump_gate = this.JumpGateGrid.GetJumpGate(gateid);
				if (jump_gate != null && (jump_gate.RemoteAntenna == null || jump_gate.RemoteAntenna == this))
				{
					jump_gate.RemoteAntenna = this;
					jump_gate.RemoteAntennaChannel = i;
					jump_gate.SetDirty();
				}
				else if (jump_gate != null) this.InboundConnectionJumpGates[i] = -1;
			}

			this.SetDirty();
		}

		/// <summary>
		/// CubeBlockBase Method<br />
		/// Appends info to the detailed info screen
		/// </summary>
		/// <param name="info">The string builder to append to</param>
		protected override void AppendCustomInfo(StringBuilder sb)
		{
			if (this.ResourceSink != null)
			{
				double input_wattage = this.ResourceSink.CurrentInputByType(MyResourceDistributorComponent.ElectricityId);
				double max_input = this.ResourceSink.RequiredInputByType(MyResourceDistributorComponent.ElectricityId);
				double input_ratio = input_wattage / max_input;

				sb.Append($"\n-=-=( {MyTexts.GetString("DisplayName_CubeBlock_JumpGateRemoteAntenna")} )=-=-\n");
				sb.Append($" {MyTexts.GetString("DetailedInfo_BlockBase_Input")}: {MyJumpGateModSession.AutoconvertMetricUnits(input_wattage * 1e6, "W/s", 4)}\n");
				sb.Append($" {MyTexts.GetString("DetailedInfo_BlockBase_RequiredInput")}: {MyJumpGateModSession.AutoconvertMetricUnits(max_input * 1e6, "W/s", 4)}\n");
				sb.Append($" {MyTexts.GetString("DetailedInfo_BlockBase_InputRatio")}: {((double.IsNaN(input_ratio)) ? "N/A" : $"{Math.Round(MathHelperD.Clamp(input_ratio, 0, 1) * 100, 2):#.00}%")}\n");
				sb.Append($" {MyTexts.GetString("DetailedInfo_JumpGateRemoteAntenna_NearbyAntennas")}: {this.GetNearbyAntennas().Count()}\n");

				for (byte i = 0; i < MyJumpGateRemoteAntenna.ChannelCount; ++i)
				{
					MyJumpGateRemoteAntenna antenna = this.ConnectionThreads[i];
					MyJumpGateController outbound_controller = this.GetOutboundControlController(i);
					MyJumpGateController inbound_controller = antenna?.GetOutboundControlController(i);
					MyJumpGate inbound_controllee = antenna?.GetInboundControlGate(i);
					MyJumpGate outbound_controllee = this.GetInboundControlGate(i);
					sb.Append($"\n {MyTexts.GetString("DetailedInfo_JumpGateRemoteAntenna_Channel")} {i}:\n");
					sb.Append($" ... {MyTexts.GetString("DetailedInfo_JumpGateRemoteAntenna_IsConnected")}: {antenna != null}\n");
					sb.Append($" ... {MyTexts.GetString("DetailedInfo_JumpGateRemoteAntenna_HasOutboundController")}: {outbound_controller != null}\n");
					sb.Append($" ... {MyTexts.GetString("DetailedInfo_JumpGateRemoteAntenna_JumpGateConnected")}: {inbound_controllee != null || outbound_controllee != null}\n");
					sb.Append($" ... {MyTexts.GetString("DetailedInfo_JumpGateRemoteAntenna_HasInboundController")}: {inbound_controller != null}\n");
				}
				
				MyJumpGateConstruct this_grid = (this.JumpGateGrid?.MarkClosed ?? true) ? null : this.JumpGateGrid;
				MyJumpGate jump_gate = this.GetInboundControlGate(this.CurrentTerminalChannel);
				jump_gate = (jump_gate?.MarkClosed ?? true) ? null : jump_gate;
				BoundingEllipsoidD? jump_ellipse = jump_gate?.JumpEllipse;
				Vector3D? jump_node = jump_gate?.WorldJumpNode;
				Vector3D endpoint = this.BlockSettings.BaseControllerSettings[this.CurrentTerminalChannel].SelectedWaypoint()?.GetEndpoint() ?? Vector3D.Zero;

				if (this.LocalGameTick % 30 == 0)
				{
					this.TEMP_DetailedInfoConstructsList.Clear();
					if (this_grid != null) this.TEMP_DetailedInfoConstructsList.AddRange(this_grid.GetCubeGrids());
				}

				double distance = (jump_node == null) ? -1 : Vector3D.Distance(endpoint, jump_node.Value);
				double distance_ratio = jump_gate?.CalculateDistanceRatio(ref endpoint) ?? -1;
				double total_mass_kg = 0;
				StringBuilder entity_list = new StringBuilder();

				if (jump_gate != null)
				{
					foreach (KeyValuePair<MyEntity, float> pair in jump_gate.GetEntitiesInJumpSpace(true))
					{
						string name = $" {pair.Key.DisplayName}";
						string info = $" - {MyJumpGateModSession.AutoconvertMetricUnits(pair.Value * 1e3, "g", 4)}";
						const int max_length = 30;
						int remaining_length = max_length - info.Length - name.Length;
						int chop_length = -Math.Min(remaining_length, 0);
						total_mass_kg += pair.Value;

						if (chop_length > 0)
						{
							chop_length += 3;
							info = $"...{info}";
						}

						entity_list.Append($"[color=#FF911CBF] - {name.Substring(0, name.Length - chop_length)}{info}[/color]\n");
					}

					foreach (KeyValuePair<long, float> pair in jump_gate.GetUninitializedEntititesInJumpSpace(true))
					{
						string name = $" U:{pair.Key}";
						string info = $" - {MyJumpGateModSession.AutoconvertMetricUnits(pair.Value * 1e3, "g", 4)}";
						const int max_length = 30;
						int remaining_length = max_length - info.Length - name.Length;
						int chop_length = -Math.Min(remaining_length, 0);
						total_mass_kg += pair.Value;

						if (chop_length > 0)
						{
							chop_length += 3;
							info = $"...{info}";
						}

						entity_list.Append($"[color=#FF911CBF] - {name.Substring(0, name.Length - chop_length)}{info}[/color]\n");
					}
				}

				double total_required_power_mw = jump_gate?.CalculateTotalRequiredPower(endpoint, null, total_mass_kg) ?? double.NaN;
				double total_available_power_mw = this_grid?.CalculateTotalAvailableInstantPower(jump_gate?.JumpGateID ?? -1) ?? double.NaN;
				int reachable_grids = (MyJumpGateModSession.Configuration.ConstructConfiguration.RequireGridCommLink) ? (this_grid?.GetCommLinkedJumpGateGrids().Count() ?? 0) : MyJumpGateModSession.Instance.GetAllJumpGateGrids().Count();

				sb.Append($"\n-=( {MyTexts.GetString("DetailedInfo_JumpGateRemoteAntenna_ChannelControllerSettings").Replace("{%0}", this.CurrentTerminalChannel.ToString())} )=-\n");
				sb.Append($" {MyTexts.GetString("DetailedInfo_JumpGateController_AttachedJumpGate")}: {jump_gate?.GetPrintableName() ?? "N/A"}\n");
				sb.Append($" {MyTexts.GetString("DetailedInfo_JumpGateController_AttachedJumpGateId")}: {jump_gate?.JumpGateID.ToString() ?? "N/A"}\n");

				sb.Append($"\n[color=#FF78FFFB]--- {MyTexts.GetString("DetailedInfo_JumpGateController_HeaderJumpGateInfo")} ---[/color][color=#FF5ABFBC]\n");
				sb.Append($" - {MyTexts.GetString("StatusText_Status")}: {((jump_gate == null) ? "N/A" : MyTexts.GetString($"StatusText_{jump_gate.Status}"))}\n");
				sb.Append($" - {MyTexts.GetString("PhaseText_Phase")}: {((jump_gate == null) ? "N/A" : MyTexts.GetString($"PhaseText_{jump_gate.Phase}"))}\n");
				sb.Append($" - {MyTexts.GetString("DetailedInfo_JumpGateController_DriveCount")}: {jump_gate?.GetWorkingJumpGateDrives().Count().ToString() ?? "N/A"}\n");
				sb.Append($" - {MyTexts.GetString("DetailedInfo_JumpGateController_GridSize")}: {jump_gate?.CubeGridSize().ToString() ?? "N/A"}\n");
				sb.Append($" - {MyTexts.GetString("DetailedInfo_JumpGateController_Radius")}: {((jump_gate == null) ? "N/A" : MyJumpGateModSession.AutoconvertMetricUnits(jump_ellipse.Value.Radii.X, "m", 4))}\n");
				sb.Append($" - {MyTexts.GetString("DetailedInfo_JumpGateController_EffectiveRadius")}: {((jump_gate == null) ? "N/A" : MyJumpGateModSession.AutoconvertMetricUnits(jump_ellipse.Value.Radii.X, "m", 4))}\n");
				sb.Append($" - {MyTexts.GetString("DetailedInfo_JumpGateController_NodeVelocity")}: {((jump_gate == null) ? "N/A" : MyJumpGateModSession.AutoconvertMetricUnits(jump_gate.JumpNodeVelocity.Length(), "m/s", 4))}\n");
				sb.Append($" - {MyTexts.GetString("DetailedInfo_JumpGateController_CommReachableGrids")}: {reachable_grids}[/color]\n");

				sb.Append($"\n[color=#FF26FF3C]--- {MyTexts.GetString("DetailedInfo_JumpGateController_HeaderJumpGateDistances")} ---[/color][color=#FF1CBF2D]\n");
				sb.Append($" - {MyTexts.GetString("DetailedInfo_JumpGateController_MaxPossibleDistance")}: {((jump_gate == null) ? "N/A" : MyJumpGateModSession.AutoconvertMetricUnits(jump_gate.JumpGateConfiguration.MaximumJumpDistance, "m", 4))}\n");
				sb.Append($" - {MyTexts.GetString("DetailedInfo_JumpGateController_MinPossibleDistance")}: {((jump_gate == null) ? "N/A" : MyJumpGateModSession.AutoconvertMetricUnits(jump_gate.JumpGateConfiguration.MinimumJumpDistance, "m", 4))}\n");
				sb.Append($" - {MyTexts.GetString("DetailedInfo_JumpGateController_ReasonableDistance")}: {((jump_gate == null) ? "N/A" : MyJumpGateModSession.AutoconvertMetricUnits(jump_gate.CalculateMaxGateDistance(), "m", 4))}\n");
				sb.Append($" - {MyTexts.GetString("DetailedInfo_JumpGateController_Ideal50Distance")}: {((jump_gate == null) ? "N/A" : MyJumpGateModSession.AutoconvertMetricUnits(jump_gate.JumpGateConfiguration.MaxJumpGate50Distance, "m", 4))}\n");
				sb.Append($" - {MyTexts.GetString("DetailedInfo_JumpGateController_TargetDistance")}: {((jump_gate == null) ? "N/A" : MyJumpGateModSession.AutoconvertMetricUnits(distance, "m", 4))}\n");
				sb.Append($" - {MyTexts.GetString("DetailedInfo_JumpGateController_NeededDrives")}: {((jump_gate == null) ? "N/A" : jump_gate.CalculateDrivesRequiredForDistance(distance).ToString())}\n");
				sb.Append($" - {MyTexts.GetString("DetailedInfo_JumpGateController_DistanceRatio")}: {((jump_gate == null) ? "N/A" : MyJumpGateModSession.AutoconvertSciNotUnits(distance_ratio, 4))}\n");
				sb.Append("[/color]");

				sb.Append($"\n[color=#FFFF6E26]--- {MyTexts.GetString("DetailedInfo_JumpGateController_HeaderPowerInfo")} ---[/color][color=#FFBF521C]\n");
				sb.Append($" - {MyTexts.GetString("DetailedInfo_JumpGateController_PowerFactor")}: {((jump_gate == null) ? "N/A" : MyJumpGateModSession.AutoconvertSciNotUnits(jump_gate.CalculatePowerFactorFromDistanceRatio(distance_ratio), 4))}\n");
				sb.Append($" - {MyTexts.GetString("DetailedInfo_JumpGateController_JumpSpaceMass")}: {((jump_gate == null) ? "N/A" : MyJumpGateModSession.AutoconvertMetricUnits(total_mass_kg * 1000, "g", 4))}\n");
				sb.Append($" - {MyTexts.GetString("DetailedInfo_JumpGateController_PowerDensity")}: {((jump_gate == null) ? "N/A" : MyJumpGateModSession.AutoconvertMetricUnits(jump_gate.CalculateUnitPowerRequiredForJump(ref endpoint) * 1000, "w/kg", 4))}\n");
				sb.Append($" - {MyTexts.GetString("DetailedInfo_JumpGateController_TotalRequiredPower")}: {((jump_gate == null) ? "N/A" : MyJumpGateModSession.AutoconvertMetricUnits(total_required_power_mw * 1e6, "w", 2))}\n");
				sb.Append($" - {MyTexts.GetString("DetailedInfo_JumpGateController_AvailableInstantPower")}: {((jump_gate == null) ? "N/A" : MyJumpGateModSession.AutoconvertMetricUnits(total_available_power_mw * 1e6, "w", 2))}\n");
				sb.Append($" - {MyTexts.GetString("DetailedInfo_JumpGateController_PowerPercentage")}: {((jump_gate == null) ? "N/A" : $"{Math.Round(MathHelper.Clamp(total_available_power_mw / total_required_power_mw, 0, 1) * 100, 2)}%")}[/color]\n");

				sb.Append($"\n[color=#FFC226FF]--- {MyTexts.GetString("DetailedInfo_JumpGateController_HeaderJumpSpaceEntities")} ---[/color]\n");
				sb.Append(entity_list);

				sb.Append($"\n[color=#FF78FFFB]--- {MyTexts.GetString("DetailedInfo_JumpGateController_HeaderConstructInfo")} ---[/color][color=#FF5ABFBC]\n");
				sb.Append($" - {MyTexts.GetString("DetailedInfo_JumpGateController_MainGrid")}: {(this_grid?.CubeGridID.ToString() ?? "N/A")}\n");
				sb.Append($" - {MyTexts.GetString("DetailedInfo_JumpGateController_MainGridName")}: {(this_grid?.PrimaryCubeGridCustomName ?? "N/A")}\n");
				sb.Append($" - {MyTexts.GetString("DetailedInfo_JumpGateController_StaticGrids")}: {((this_grid == null) ? "N/A" : $"{this.TEMP_DetailedInfoConstructsList.Count((grid) => grid.IsStatic)}")}/{this.TEMP_DetailedInfoConstructsList.Count}\n");
				sb.Append($" - {MyTexts.GetString("DetailedInfo_JumpGateController_TotalGridDrives")}: {(this_grid?.GetAttachedJumpGateDrives().Count().ToString() ?? "N/A")}\n");
				sb.Append($" - {MyTexts.GetString("DetailedInfo_JumpGateController_TotalGridGates")}: {(this_grid?.GetJumpGates().Count().ToString() ?? "N/A")}[/color]\n");
			}
		}

		/// <summary>
		/// CubeBlockBase Method<br />
		/// Initializes the block's game logic
		/// </summary>
		/// <param name="object_builder">This entity's object builder</param>
		public override void Init(MyObjectBuilder_EntityBase object_builder)
		{
			this.Init(object_builder, 0, () => (this.TerminalBlock.IsWorking) ? (float) (0.1 + this.ConnectionThreads.Count((other) => other != null) * 0.05) : 0, MyJumpGateModSession.BlockComponentDataGUID, false);
			this.TerminalBlock.Synchronized = true;
			if (MyJumpGateModSession.Network.Registered) MyJumpGateModSession.Network.On(MyPacketTypeEnum.UPDATE_REMOTE_ANTENNA, this.OnNetworkBlockUpdate);
			string blockdata;
			this.ResourceSink.SetMaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId, 250f);

			if (this.ModStorageComponent.TryGetValue(MyJumpGateModSession.BlockComponentDataGUID, out blockdata) && blockdata.Length > 0)
			{
				try
				{
					MyRemoteAntennaBlockSettingsStruct serialized = MyAPIGateway.Utilities.SerializeFromBinary<MyRemoteAntennaBlockSettingsStruct>(Convert.FromBase64String(blockdata));
					serialized.Validate();
					this.BlockSettings = serialized;
				}
				catch (Exception e)
				{
					this.BlockSettings = new MyRemoteAntennaBlockSettingsStruct();
					this.BlockSettings.Validate();
					Logger.Error($"Failed to load block data: {this.GetType().Name}-{this.TerminalBlock.CustomName}\n{e}");
				}
			}
			else
			{
				this.BlockSettings = new MyRemoteAntennaBlockSettingsStruct();
				this.BlockSettings.Validate();
				this.ModStorageComponent.Add(MyJumpGateModSession.BlockComponentDataGUID, "");
			}

			for (byte i = 0; i < MyJumpGateRemoteAntenna.ChannelCount; ++i) this.InboundConnectionJumpGates[i] = this.BlockSettings.InboundConnectionJumpGates[i];
			for (byte i = 0; i < MyJumpGateRemoteAntenna.ChannelCount; ++i) this.OutboundConnectionControllers[i] = this.BlockSettings.OutboundConnectionControllers[i];

			if (MyJumpGateModSession.Network.Registered && MyNetworkInterface.IsStandaloneMultiplayerClient)
			{
				MyNetworkInterface.Packet request = new MyNetworkInterface.Packet {
					TargetID = 0,
					Broadcast = false,
					PacketType = MyPacketTypeEnum.UPDATE_REMOTE_ANTENNA,
				};
				request.Payload<MySerializedJumpGateCapacitor>(null);
				request.Send();
			}
		}

		public override void UpdateOnceBeforeFrame()
		{
			base.UpdateOnceBeforeFrame();
			if (!MyJumpGateRemoteAntennaTerminal.IsLoaded) MyJumpGateRemoteAntennaTerminal.Load(this.ModContext);
		}

		/// <summary>
		/// CubeBlockBase Method<br />
		/// Called once every tick after simulation<br />
		/// Updates power usage and emissives
		/// </summary>
		public override void UpdateAfterSimulation()
		{
			base.UpdateAfterSimulation();

			// Update Emmissives
			bool is_working;
			if (this.TerminalBlock?.CubeGrid?.Physics == null || this.TerminalBlock.MarkedForClose) return;
			else if (is_working = this.IsWorking) this.TerminalBlock.SetEmissiveParts("Emissive0", Color.Lime, 1);
			else if (this.TerminalBlock.IsFunctional) this.TerminalBlock.SetEmissiveParts("Emissive0", Color.Red, 1);
			else this.TerminalBlock.SetEmissiveParts("Emissive0", Color.Black, 1);

			// Update remote antenna connections
			if (MyNetworkInterface.IsServerLike && this.IsInitFrameCalled)
			{
				double connection_distance = this.BlockSettings.AntennaRange * this.BlockSettings.AntennaRange;

				for (byte i = 0; i < MyJumpGateRemoteAntenna.ChannelCount; ++i)
				{
					MyJumpGateRemoteAntenna antenna = this.ConnectionThreads[i];
					MyJumpGateController outbound_controller = this.GetOutboundControlController(i);
					MyJumpGateController remote_controller = antenna?.GetOutboundControlController(i);
					MyJumpGate inbound_controllee = this.GetInboundControlGate(i);
					MyJumpGate remote_gate = antenna?.GetInboundControlGate(i);
					MyJumpGate controlled_gate = outbound_controller?.AttachedJumpGate();
					KeyValuePair<MyJumpGateRemoteAntenna, byte> controller_channel;
					
					if (antenna == null || Vector3D.DistanceSquared(this.WorldMatrix.Translation, antenna.WorldMatrix.Translation) > connection_distance)
					{
						if (controlled_gate != null)
						{
							outbound_controller.AttachedRemoteJumpGate(null);
							outbound_controller.SetDirty();
						}
						
						if (inbound_controllee != null)
						{
							if ((remote_controller = inbound_controllee.Controller) != null)
							{
								remote_controller.AttachedRemoteJumpGate(null);
								remote_controller.SetDirty();
							}
							
							inbound_controllee.Controller = null;
							inbound_controllee.SetDirty();
						}
						
						if (antenna != null)
						{
							this.ConnectionThreads[i] = null;
							antenna.ConnectionThreads[i] = null;
							antenna.SetDirty();
							this.OnAntennaConnection?.Invoke(this, outbound_controller, inbound_controllee, i, false);
							antenna.OnAntennaConnection?.Invoke(antenna, outbound_controller, inbound_controllee, i, false);
						}
						
						this.SetDirty();
						continue;
					}
					
					if (outbound_controller != null && ((controller_channel = outbound_controller.RemoteAntennaChannel).Key != this || controller_channel.Value != i || remote_gate != controlled_gate))
					{
						outbound_controller.AttachedRemoteJumpGate(null);
						outbound_controller.SetDirty();
						if (remote_gate != null) remote_gate.Controller = null;
						this.ConnectionThreads[i] = null;
						antenna.ConnectionThreads[i] = null;
						this.SetDirty();
						antenna.SetDirty();
						this.OnAntennaConnection?.Invoke(this, outbound_controller, inbound_controllee, i, false);
						antenna.OnAntennaConnection?.Invoke(antenna, outbound_controller, inbound_controllee, i, false);
					}
					else if (outbound_controller == null && remote_gate != null)
					{
						if (remote_gate.Controller != null)
						{
							remote_gate.Controller = null;
							remote_gate.SetDirty();
						}

						this.ConnectionThreads[i] = null;
						antenna.ConnectionThreads[i] = null;
						this.SetDirty();
						antenna.SetDirty();
						this.OnAntennaConnection?.Invoke(this, outbound_controller, inbound_controllee, i, false);
						antenna.OnAntennaConnection?.Invoke(antenna, outbound_controller, inbound_controllee, i, false);
					}
					else if (inbound_controllee == null && remote_controller != null)
					{
						remote_controller.AttachedRemoteJumpGate(null);
						remote_controller.SetDirty();
						this.ConnectionThreads[i] = null;
						antenna.ConnectionThreads[i] = null;
						this.SetDirty();
						antenna.SetDirty();
						this.OnAntennaConnection?.Invoke(this, outbound_controller, inbound_controllee, i, false);
						antenna.OnAntennaConnection?.Invoke(antenna, outbound_controller, inbound_controllee, i, false);
					}
					else if (!is_working || !antenna.IsWorking)
					{
						if (outbound_controller != null)
						{
							outbound_controller.AttachedRemoteJumpGate(null);
							outbound_controller.SetDirty();
						}

						if (inbound_controllee != null)
						{
							remote_controller = inbound_controllee.Controller;
							remote_controller?.AttachedRemoteJumpGate(null);
							remote_controller?.SetDirty();
							inbound_controllee.Controller = null;
							inbound_controllee.SetDirty();
						}

						this.ConnectionThreads[i] = null;
						antenna.ConnectionThreads[i] = null;
						this.SetDirty();
						antenna.SetDirty();
						this.OnAntennaConnection?.Invoke(this, outbound_controller, inbound_controllee, i, false);
						antenna.OnAntennaConnection?.Invoke(antenna, outbound_controller, inbound_controllee, i, false);
					}
				}
				
				if (is_working)
				{
					foreach (MyJumpGateRemoteAntenna antenna in this.GetNearbyAntennas().OrderBy((antenna) => Vector3D.DistanceSquared(antenna.WorldMatrix.Translation, this.WorldMatrix.Translation)))
					{
						for (byte i = 0; i < MyJumpGateRemoteAntenna.ChannelCount; ++i)
						{
							MyJumpGateController outbound_controller = this.GetOutboundControlController(i);
							MyJumpGate inbound_controllee = antenna.GetInboundControlGate(i);
							if (outbound_controller == null || outbound_controller.Closed || inbound_controllee == null || inbound_controllee.Closed || this.ConnectionThreads[i] != null || antenna.ConnectionThreads[i] != null || inbound_controllee.Controller != null) continue;
							this.ConnectionThreads[i] = antenna;
							antenna.ConnectionThreads[i] = this;
							this.SetDirty();
							antenna.SetDirty();
							outbound_controller.AttachedRemoteJumpGate(inbound_controllee);
							outbound_controller.SetDirty();
							inbound_controllee.SetDirty();
							this.OnAntennaConnection?.Invoke(this, outbound_controller, inbound_controllee, i, true);
							antenna.OnAntennaConnection?.Invoke(antenna, outbound_controller, inbound_controllee, i, true);
						}
					}
				}
			}
			else if (MyNetworkInterface.IsStandaloneMultiplayerClient)
			{
				for (byte i = 0; i < MyJumpGateRemoteAntenna.ChannelCount; ++i)
				{
					KeyValuePair<JumpGateUUID, JumpGateUUID>? null_pair = this.UninitializedPairings[i];
					if (null_pair == null) continue;
					KeyValuePair<JumpGateUUID, JumpGateUUID> pair = null_pair.Value;
					if (pair.Key == JumpGateUUID.Empty || pair.Value == JumpGateUUID.Empty) continue;
					MyJumpGateController controller = this.JumpGateGrid.GetController(pair.Key.GetBlock());
					MyJumpGate remote_gate = MyJumpGateModSession.Instance.GetJumpGate(pair.Value);

					if (controller == null || remote_gate == null)
					{
						this.UninitializedPairings[i] = pair;
						continue;
					}

					controller.AttachedRemoteJumpGate(remote_gate);
					remote_gate.RemoteAntenna = this.ConnectionThreads[i];
					this.UninitializedPairings[i] = null;
				}
			}

			// Update waypoints
			if (is_working && !MyNetworkInterface.IsDedicatedMultiplayerServer && MyAPIGateway.Gui.GetCurrentScreen == MyTerminalPageEnum.ControlPanel && MyJumpGateModSession.GameTick % 60 == 0)
			{
				long player_identity = MyAPIGateway.Players.TryGetIdentityId(MyAPIGateway.Multiplayer.MyId);
				IEnumerable<MyJumpGateConstruct> reachable_grids = (MyJumpGateModSession.Configuration.ConstructConfiguration.RequireGridCommLink) ? this.JumpGateGrid.GetCommLinkedJumpGateGrids() : MyJumpGateModSession.Instance.GetAllJumpGateGrids();

				for (byte channel = 0; channel < MyJumpGateRemoteAntenna.ChannelCount; ++channel)
				{
					MyJumpGate jump_gate = this.GetInboundControlGate(channel);
					if (jump_gate == null || !jump_gate.IsValid()) continue;
					Vector3D jump_node = jump_gate.WorldJumpNode;
					double distance;
					List<MyJumpGateWaypoint> waypoints = this.WaypointsList[channel];
					object mutex = this.WaypointsListMutex[channel];
					lock (mutex) waypoints.Clear();

					foreach (MyJumpGateConstruct connected_grid in reachable_grids)
					{
						if (connected_grid == this.JumpGateGrid || !MyJumpGateModSession.Instance.IsJumpGateGridMultiplayerValid(connected_grid)) continue;

						foreach (MyJumpGateController controller in connected_grid.GetAttachedJumpGateControllers())
						{
							MyJumpGate other_gate = controller.AttachedJumpGate();
							if (other_gate == null || other_gate.MarkClosed) continue;
							distance = Vector3D.Distance(jump_node, other_gate.WorldJumpNode);
							if (distance < jump_gate.JumpGateConfiguration.MinimumJumpDistance || distance > jump_gate.JumpGateConfiguration.MaximumJumpDistance || !controller.IsFactionRelationValid(player_identity)) continue;
							MyJumpGateWaypoint waypoint = new MyJumpGateWaypoint(other_gate);
							lock (mutex) if (!waypoints.Contains(waypoint)) waypoints.Add(waypoint);
						}

						foreach (MyJumpGateRemoteAntenna antenna in connected_grid.GetAttachedJumpGateRemoteAntennas())
						{
							for (byte i = 0; i < MyJumpGateRemoteAntenna.ChannelCount; ++i)
							{
								MyJumpGate other_gate = antenna.GetInboundControlGate(i);
								if (other_gate == null || other_gate.MarkClosed) continue;
								distance = Vector3D.Distance(jump_node, other_gate.WorldJumpNode);
								if (distance < jump_gate.JumpGateConfiguration.MinimumJumpDistance || distance > jump_gate.JumpGateConfiguration.MaximumJumpDistance || !antenna.IsFactionRelationValid(i, player_identity)) continue;
								MyJumpGateWaypoint waypoint = new MyJumpGateWaypoint(other_gate);
								lock (mutex) if (!waypoints.Contains(waypoint)) waypoints.Add(waypoint);
							}
						}
					}

					lock (mutex)
					{
						waypoints.AddRange(this.JumpGateGrid.GetBeaconsWithinReverseBroadcastSphere().Where((beacon) => (distance = Vector3D.Distance(jump_node, beacon.BeaconPosition)) >= jump_gate.JumpGateConfiguration.MinimumJumpDistance && distance <= jump_gate.JumpGateConfiguration.MaximumJumpDistance).OrderBy((beacon) => Vector3D.Distance(beacon.BeaconPosition, jump_node)).Select((beacon) => new MyJumpGateWaypoint(beacon)));
						waypoints.AddRange(MyAPIGateway.Session.GPS.GetGpsList(player_identity).Where((gps) => gps.Coords.IsValid()).OrderBy((gps) => Vector3D.Distance(gps.Coords, jump_node)).Select((gps) => new MyJumpGateWaypoint(gps)));
					}

					// Fix selected waypoint
					if (MyNetworkInterface.IsServerLike)
					{
						MyJumpGateWaypoint selected_waypoint = this.BlockSettings.BaseControllerSettings[channel].SelectedWaypoint();
						Vector3D? waypoint_endpoint = selected_waypoint?.GetEndpoint();

						if (waypoint_endpoint != null)
						{
							Vector3D endpoint = waypoint_endpoint.Value;
							distance = Vector3D.Distance(endpoint, jump_node);

							if (distance < jump_gate.JumpGateConfiguration.MinimumJumpDistance || distance > jump_gate.JumpGateConfiguration.MaximumJumpDistance)
							{
								this.BlockSettings.BaseControllerSettings[channel].SelectedWaypoint(null);
								this.SetDirty();
							}
						}
					}
				}
			}

			// Update connection emissives
			ulong tick_offset = this.LocalGameTick % 30;
			this.ConnectionEmissiveIndex = (tick_offset == 0) ? (byte) ((this.ConnectionEmissiveIndex + 1) % MyJumpGateRemoteAntenna.ChannelCount) : this.ConnectionEmissiveIndex;
			byte connections = (byte) this.ConnectionThreads.Count((other) => other != null);
			if (connections == 0) this.TerminalBlock.SetEmissiveParts("Emissive1", Color.Red, 1);
			else if (connections == MyJumpGateRemoteAntenna.ChannelCount) this.TerminalBlock.SetEmissiveParts("Emissive1", Color.Lime, 1);
			else if (tick_offset >= 25) this.TerminalBlock.SetEmissiveParts("Emissive1", Color.Black, 1);
			else this.TerminalBlock.SetEmissiveParts("Emissive1", (this.ConnectionThreads[this.ConnectionEmissiveIndex] != null) ? Color.Blue : Color.Gold, 1);

			this.CheckSendGlobalUpdate();
		}

		/// <summary>
		/// CubeBlockBase Method<br />
		/// Unregisters events and marks block for close
		/// </summary>
		public override void MarkForClose()
		{
			base.MarkForClose();
			if (MyJumpGateModSession.Network.Registered) MyJumpGateModSession.Network.Off(MyPacketTypeEnum.UPDATE_REMOTE_ANTENNA, this.OnNetworkBlockUpdate);
			if (this.AntennaDetector == null) return;
			MyAPIGateway.Entities.RemoveEntity(this.AntennaDetector);
			this.AntennaDetector = null;
			this.NearbyEntities.Clear();
		}

		/// <summary>
		/// Serializes block for streaming and saving
		/// </summary>
		/// <returns>false</returns>
		public override bool IsSerialized()
		{
			bool res = base.IsSerialized();
			this.BlockSettings.InboundConnectionJumpGates = this.InboundConnectionJumpGates;
			this.BlockSettings.OutboundConnectionControllers = this.OutboundConnectionControllers;
			this.ModStorageComponent[MyJumpGateModSession.BlockComponentDataGUID] = Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(this.BlockSettings));
			return res;
		}
		#endregion

		#region Private Methods
		/// <summary>
		/// Callback triggered when this component recevies a block update request from server
		/// </summary>
		/// <param name="packet">The update request packet</param>
		private void OnNetworkBlockUpdate(MyNetworkInterface.Packet packet)
		{
			if (packet == null) return;
			MySerializedJumpGateRemoteAntenna serialized = packet.Payload<MySerializedJumpGateRemoteAntenna>();
			if (serialized == null || serialized.UUID.GetBlock() != this.BlockID) return;

			if (MyNetworkInterface.IsMultiplayerServer && packet.PhaseFrame == 1 && packet.EpochTime > this.LastUpdateTime)
			{
				if (serialized.IsClientRequest)
				{
					packet.Payload(this.ToSerialized(false));
					packet.Send();
				}
				else if (this.LastUpdateDateTimeUTC < packet.EpochDateTimeUTC && this.FromSerialized(serialized))
				{
					this.LastUpdateTime = packet.EpochTime;
					this.IsDirty = false;
					packet.Forward(0, true).Send();
				}
			}
			else if (MyNetworkInterface.IsStandaloneMultiplayerClient && (packet.PhaseFrame == 1 || packet.PhaseFrame == 2) && this.FromSerialized(serialized))
			{
				this.LastUpdateTime = packet.EpochTime;
				this.IsDirty = false;
			}
		}

		/// <summary>
		/// Server only<br />
		/// Checks if this block should be updated and if true -<br />
		/// sends an update request for this block to all clients
		/// </summary>
		private void CheckSendGlobalUpdate()
		{
			if (MyJumpGateModSession.Network.Registered && ((MyNetworkInterface.IsMultiplayerServer && MyJumpGateModSession.GameTick % MyCubeBlockBase.ForceUpdateDelay == 0) || this.IsDirty))
			{
				MyNetworkInterface.Packet update_packet = new MyNetworkInterface.Packet {
					PacketType = MyPacketTypeEnum.UPDATE_REMOTE_ANTENNA,
					TargetID = 0,
					Broadcast = MyNetworkInterface.IsMultiplayerServer,
				};

				update_packet.Payload(this.ToSerialized(false));
				update_packet.Send();
				if (MyNetworkInterface.IsMultiplayerServer) this.LastUpdateTime = update_packet.EpochTime;
				this.IsDirty = false;
			}
		}

		private void OnEntityCollision(IMyEntity entity, bool is_entering)
		{
			if (entity == null || entity.Physics == null || !(entity is MyCubeGrid) || this.MarkedForClose) return;
			else if (is_entering) this.NearbyEntities?.Add(entity.EntityId);
			else this.NearbyEntities?.Remove(entity.EntityId);
			this.SetDirty();
		}

		/// <summary>
		/// Updates the internal listing of nearby antennas
		/// </summary>
		private void UpdateNearbyAntennas()
		{
			if (this.NearbyEntities == null || this.NearbyAntennas == null || this.JumpGateGrid == null) return;
			List<long> checked_grids = new List<long>(this.JumpGateGrid.GetCubeGrids().Select((grid) => grid.EntityId));
			MyJumpGateConstruct construct = null;
			Vector3D this_pos = this.WorldMatrix.Translation;
			string name = this.JumpGateGrid.PrimaryCubeGridCustomName;

			lock (this.NearbyAntennasMutex)
			{
				this.NearbyAntennas?.Clear();

				if (this.IsWorking)
				{
					foreach (long entity in this.NearbyEntities)
					{
						if (checked_grids.Contains(entity) || !MyJumpGateModSession.Instance.IsJumpGateGridMultiplayerValid(construct = MyJumpGateModSession.Instance.GetDirectJumpGateGrid(entity))) continue;
						checked_grids.AddRange(construct.GetCubeGrids().Select((grid) => grid.EntityId));

						foreach (MyJumpGateRemoteAntenna antenna in construct.GetAttachedJumpGateRemoteAntennas())
						{
							double distance = Math.Min(this.BlockSettings.AntennaRange, antenna.BlockSettings.AntennaRange);
							if (!antenna.IsWorking || Vector3D.DistanceSquared(antenna.WorldMatrix.Translation, this_pos) > distance * distance) continue;
							lock (this.NearbyAntennasMutex) this.NearbyAntennas?.Add(antenna);
						}
					}
				}
			}
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Sets the channel to a specified jump gate or clears the channel if the gate is null or closed
		/// </summary>
		/// <param name="channel">The channel or 255 for next available</param>
		/// <param name="gate">The gate to bind</param>
		public void SetGateForInboundControl(byte channel, MyJumpGate gate)
		{
			if (channel >= MyJumpGateRemoteAntenna.ChannelCount && (gate == null || channel != 0xFF) || (gate != null && gate.JumpGateGrid != this.JumpGateGrid)) return;
			else if (channel == 0xFF && this.InboundConnectionJumpGates.Contains(gate.JumpGateID)) channel = (byte) Array.IndexOf(this.InboundConnectionJumpGates, gate.JumpGateID);
			else if (channel == 0xFF)
			{
				for (byte i = 0; i < MyJumpGateRemoteAntenna.ChannelCount; ++i)
				{
					if (this.InboundConnectionJumpGates[i] == -1)
					{
						channel = i;
						break;
					}
				}

				if (channel == 0xFF) return;
			}

			if (gate != null && this.InboundConnectionJumpGates.Contains(gate.JumpGateID)) this.SetGateForInboundControl((byte) Array.IndexOf(this.InboundConnectionJumpGates, gate.JumpGateID), null);
			MyJumpGate old_gate = this.GetInboundControlGate(channel);
			this.InboundConnectionJumpGates[channel] = (gate == null || gate.Closed) ? -1 : gate.JumpGateID;

			if (gate != null)
			{
				gate.RemoteAntenna = this;
				gate.RemoteAntennaChannel = channel;
			}

			if (old_gate == null || old_gate == gate) return;
			old_gate.Controller?.AttachedRemoteJumpGate(null);
			old_gate.Controller = null;
			old_gate.RemoteAntenna = null;
			old_gate.RemoteAntennaChannel = 0xFF;
		}

		/// <summary>
		/// Sets the channel to a specified jump gate controller or clears the channel if the controller is null or closed
		/// </summary>
		/// <param name="channel">The channel or 255 for next available</param>
		/// <param name="controller">The controller to bind</param>
		public void SetControllerForOutboundControl(byte channel, MyJumpGateController controller)
		{
			if (channel >= MyJumpGateRemoteAntenna.ChannelCount && (channel != 0xFF || controller == null) || (controller != null && controller.JumpGateGrid != this.JumpGateGrid)) return;
			else if (channel == 0xFF && this.OutboundConnectionControllers.Contains(controller.BlockID)) channel = (byte) Array.IndexOf(this.OutboundConnectionControllers, controller.BlockID);
			else if (channel == 0xFF)
			{
				for (byte i = 0; i < MyJumpGateRemoteAntenna.ChannelCount; ++i)
				{
					if (this.OutboundConnectionControllers[i] == -1)
					{
						channel = i;
						break;
					}
				}

				if (channel == 0xFF) return;
			}

			if (controller != null && this.OutboundConnectionControllers.Contains(controller.BlockID)) this.SetControllerForOutboundControl((byte) Array.IndexOf(this.OutboundConnectionControllers, controller.BlockID), null);
			MyJumpGateController old_controller = this.GetOutboundControlController(channel);
			if (old_controller == controller) return;
			old_controller?.AttachedRemoteJumpGate(null);
			this.OutboundConnectionControllers[channel] = (controller == null) ? -1 : controller.BlockID;
			controller?.BaseBlockSettings.RemoteAntennaChannel(channel);
			controller?.BaseBlockSettings.RemoteAntennaID(this.BlockID);
		}

		public bool FromSerialized(MySerializedJumpGateRemoteAntenna antenna, MyJumpGateConstruct parent = null)
		{
			if (!base.FromSerialized(antenna, parent)) return false;
			else if (this.JumpGateGrid == null) return true;
			
			if (antenna.InboundControlJumpGates != null)
			{
				for (byte i = 0; i < this.InboundConnectionJumpGates.Length; ++i)
				{
					MyJumpGate jump_gate = this.JumpGateGrid.GetJumpGate(antenna.InboundControlJumpGates[i]);
					this.SetGateForInboundControl(i, jump_gate);
				}
			}
			
			if (antenna.OutboundControlControllers != null)
			{
				for (byte i = 0; i < this.OutboundConnectionControllers.Length; ++i)
				{
					MyJumpGateController controller = this.JumpGateGrid.GetController(antenna.OutboundControlControllers[i]);
					this.SetControllerForOutboundControl(i, controller);
				}
			}
			
			if (MyNetworkInterface.IsStandaloneMultiplayerClient)
			{
				if (antenna.RemoteConnectionThreads != null)
				{
					for (byte i = 0; i < this.ConnectionThreads.Length; ++i)
					{
						long blockid = antenna.RemoteConnectionThreads[i];

						if (blockid == -1)
						{
							this.ConnectionThreads[i] = null;
							continue;
						}	

						IMyEntity entity = MyAPIGateway.Entities.GetEntityById(blockid);
						if (entity == null || !(entity is IMyTerminalBlock)) continue;
						this.ConnectionThreads[i] = MyJumpGateModSession.GetBlockAsJumpGateRemoteAntenna((IMyTerminalBlock) entity);
					}
				}
				
				this.NearbyEntities.Clear();
				if (antenna.NearbyEntities != null) this.NearbyEntities.AddRange(antenna.NearbyEntities);
				
				if (antenna.ControllerGatePairings != null)
				{
					for (byte i = 0; i < MyJumpGateRemoteAntenna.ChannelCount; ++i)
					{
						KeyValuePair<JumpGateUUID, JumpGateUUID> pair = antenna.ControllerGatePairings[i];
						if (pair.Key == JumpGateUUID.Empty ||  pair.Value == JumpGateUUID.Empty) continue;
						MyJumpGateController controller = this.JumpGateGrid.GetController(pair.Key.GetBlock());
						MyJumpGate remote_gate = MyJumpGateModSession.Instance.GetJumpGate(pair.Value);

						if (controller == null || remote_gate == null)
						{
							this.UninitializedPairings[i] = pair;
							continue;
						}

						controller.AttachedRemoteJumpGate(remote_gate);
						remote_gate.RemoteAntenna = this.ConnectionThreads[i];
					}
				}
			}
			
			this.BlockSettings = MyAPIGateway.Utilities.SerializeFromBinary<MyRemoteAntennaBlockSettingsStruct>(antenna.BlockSettings);
			return true;
		}

		/// <summary>
		/// </summary>
		/// <param name="channel">The channel to check</param>
		/// <param name="steam_id">The player's steam ID to check</param>
		/// <returns>True if the caller's faction can jump to this gate</returns>
		public bool IsFactionRelationValid(byte channel, ulong steam_id)
		{
			if (channel >= MyJumpGateRemoteAntenna.ChannelCount) return false;
			MyJumpGateController.MyControllerBlockSettingsStruct controller_settings = this.BlockSettings.BaseControllerSettings[channel];
			long player_id = MyAPIGateway.Players.TryGetIdentityId(steam_id);
			if (player_id == 0) return false;
			MyRelationsBetweenPlayerAndBlock relation = this.TerminalBlock?.GetUserRelationToOwner(player_id) ?? MyJumpGateModSession.GetPlayerByID(this.OwnerID)?.GetRelationTo(player_id) ?? MyRelationsBetweenPlayerAndBlock.NoOwnership;

			switch (relation)
			{
				case MyRelationsBetweenPlayerAndBlock.NoOwnership:
					return controller_settings.CanAcceptUnowned();
				case MyRelationsBetweenPlayerAndBlock.Neutral:
					return controller_settings.CanAcceptNeutral();
				case MyRelationsBetweenPlayerAndBlock.Friends:
					return controller_settings.CanAcceptFriendly();
				case MyRelationsBetweenPlayerAndBlock.Enemies:
					return controller_settings.CanAcceptEnemy();
				case MyRelationsBetweenPlayerAndBlock.Owner:
				case MyRelationsBetweenPlayerAndBlock.FactionShare:
					return controller_settings.CanAcceptOwned();
				default:
					return false;
			}
		}

		/// <summary>
		/// </summary>
		/// <param name="channel">The channel to check</param>
		/// <param name="player_identity">The player's identiy to check</param>
		/// <returns>True if the caller's faction can jump to this gate</returns>
		public bool IsFactionRelationValid(byte channel, long player_identity)
		{
			if (channel >= MyJumpGateRemoteAntenna.ChannelCount) return false;
			MyJumpGateController.MyControllerBlockSettingsStruct controller_settings = this.BlockSettings.BaseControllerSettings[channel];
			MyRelationsBetweenPlayerAndBlock relation = this.TerminalBlock?.GetUserRelationToOwner(player_identity) ?? MyJumpGateModSession.GetPlayerByID(this.OwnerID)?.GetRelationTo(player_identity) ?? MyRelationsBetweenPlayerAndBlock.NoOwnership;

			switch (relation)
			{
				case MyRelationsBetweenPlayerAndBlock.NoOwnership:
					return controller_settings.CanAcceptUnowned();
				case MyRelationsBetweenPlayerAndBlock.Neutral:
					return controller_settings.CanAcceptNeutral();
				case MyRelationsBetweenPlayerAndBlock.Friends:
					return controller_settings.CanAcceptFriendly();
				case MyRelationsBetweenPlayerAndBlock.Enemies:
					return controller_settings.CanAcceptEnemy();
				case MyRelationsBetweenPlayerAndBlock.Owner:
				case MyRelationsBetweenPlayerAndBlock.FactionShare:
					return controller_settings.CanAcceptOwned();
				default:
					return false;
			}
		}

		/// <summary>
		/// Gets the control channel this jump gate is bound to
		/// </summary>
		/// <param name="gate">The gate to check</param>
		/// <returns>The control channel or 255 if not registered</returns>
		public byte GetJumpGateInboundControlChannel(MyJumpGate gate)
		{
			return (byte) ((gate != null && gate.JumpGateGrid == this.JumpGateGrid && this.InboundConnectionJumpGates.Contains(gate.JumpGateID)) ? Array.IndexOf(this.InboundConnectionJumpGates, gate.JumpGateID) : 0xFF);
		}

		/// <summary>
		/// Gets the control channel this jump gate controller is bound to
		/// </summary>
		/// <param name="controller">The controller to check</param>
		/// <returns>The control channel or 255 if not registered</returns>
		public byte GetControllerOutboundControlChannel(MyJumpGateController controller)
		{
			return (byte) ((controller != null && controller.JumpGateGrid == this.JumpGateGrid && this.OutboundConnectionControllers.Contains(controller.BlockID)) ? Array.IndexOf(this.OutboundConnectionControllers, controller.BlockID) : 0xFF);
		}

		/// <summary>
		/// Gets the name of the jump gate attached on the specified channel
		/// </summary>
		/// <param name="channel">The channel to check</param>
		/// <returns>The attached jump gate's name or null</returns>
		public string GetJumpGateName(byte channel)
		{
			string name = (channel >= MyJumpGateRemoteAntenna.ChannelCount) ? null : this.BlockSettings.JumpGateNames[channel];
			return (name == null || name.Length == 0) ? null : name;
		}

		/// <summary>
		/// </summary>
		/// <param name="channel">The channel to check</param>
		/// <returns>The gate listening on the specified inbound channel or null</returns>
		public MyJumpGate GetInboundControlGate(byte channel)
		{
			return (channel > this.InboundConnectionJumpGates.Length) ? null : this.JumpGateGrid?.GetJumpGate(this.InboundConnectionJumpGates[channel]);
		}

		/// <summary>
		/// Gets the jump gate on the other end of this antenna's outbound connection or null
		/// </summary>
		/// <param name="channel">The channel to check</param>
		/// <returns>The connected gate</returns>
		public MyJumpGate GetConnectedControlledJumpGate(byte channel)
		{
			if (channel >= this.ConnectionThreads.Length) return null;
			MyJumpGateRemoteAntenna antenna = this.ConnectionThreads[channel];
			return antenna?.GetInboundControlGate(channel);
		}

		/// <summary>
		/// </summary>
		/// <param name="channel">The channel to check</param>
		/// <returns>The controller listening on the specified outbound channel or null</returns>
		public MyJumpGateController GetOutboundControlController(byte channel)
		{
			return (channel > this.OutboundConnectionControllers.Length) ? null : this.JumpGateGrid?.GetController(this.OutboundConnectionControllers[channel]);
		}

		/// <summary>
		/// Gets the antenna on the other end of this connection for the specified channel
		/// </summary>
		/// <param name="channel">The channel to check</param>
		/// <returns>The connected antenna or null</returns>
		public MyJumpGateRemoteAntenna GetConnectedRemoteAntenna(byte channel)
		{
			return (channel < MyJumpGateRemoteAntenna.ChannelCount) ? this.ConnectionThreads[channel] : null;
		}

		/// <summary>
		/// </summary>
		/// <returns>Gets the IDs of all inbound registered jump gates</returns>
		public IEnumerable<long> RegisteredInboundControlGateIDs()
		{
			return this.InboundConnectionJumpGates?.AsEnumerable() ?? Enumerable.Empty<long>();
		}

		/// <summary>
		/// </summary>
		/// <returns>Gets the IDs of all outbound registered jump gate controllers</returns>
		public IEnumerable<long> RegisteredOutboundControlControllerIDs()
		{
			return this.OutboundConnectionControllers?.AsEnumerable() ?? Enumerable.Empty<long>();
		}

		/// <summary>
		/// </summary>
		/// <returns>Gets all inbound registered jump gates</returns>
		public IEnumerable<MyJumpGate> RegisteredInboundControlGates()
		{
			return this.InboundConnectionJumpGates?.Select((id) => this.JumpGateGrid?.GetJumpGate(id)).Where((gate) => gate != null) ?? Enumerable.Empty<MyJumpGate>();
		}

		/// <summary>
		/// </summary>
		/// <returns>Gets all inbound registered jump gate controllers</returns>
		public IEnumerable<MyJumpGateController> RegisteredOutboundControlControllers()
		{
			return this.OutboundConnectionControllers?.Select((id) => this.JumpGateGrid?.GetController(id)).Where((controller) => controller != null) ?? Enumerable.Empty<MyJumpGateController>();
		}

		/// <summary>
		/// Gets all working antennas within range of this one
		/// </summary>
		/// <returns>An enumerable containing all nearby working antennas</returns>
		public IEnumerable<MyJumpGateRemoteAntenna> GetNearbyAntennas()
		{
			if (this.LastNearbyAntennasUpdateTime == 0 || (this.LocalGameTick - this.LastNearbyAntennasUpdateTime) >= 60)
			{
				this.LastNearbyAntennasUpdateTime = this.LocalGameTick;
				this.UpdateNearbyAntennas();
			}

			return this.NearbyAntennas?.AsEnumerable() ?? Enumerable.Empty<MyJumpGateRemoteAntenna>();
		}

		/// <summary>
		/// Gets the list of waypoints this controller has<br />
		/// This is client dependent
		/// </summary>
		/// <param name="channel">The channel who's visible waypoints to get</param>
		/// <returns>An enumerable containing this controller's waypoints</returns>
		public IEnumerable<MyJumpGateWaypoint> GetWaypointsList(byte channel)
		{
			if (channel >= MyJumpGateRemoteAntenna.ChannelCount) return Enumerable.Empty<MyJumpGateWaypoint>();
			lock (this.WaypointsListMutex[channel]) return this.WaypointsList[channel]?.Distinct() ?? Enumerable.Empty<MyJumpGateWaypoint>();
		}

		public new MySerializedJumpGateRemoteAntenna ToSerialized(bool as_client_request)
		{
			if (as_client_request) return base.ToSerialized<MySerializedJumpGateRemoteAntenna>(true);
			KeyValuePair<JumpGateUUID, JumpGateUUID>[] pairings = new KeyValuePair<JumpGateUUID, JumpGateUUID>[MyJumpGateRemoteAntenna.ChannelCount];

			for (byte i = 0; i < MyJumpGateRemoteAntenna.ChannelCount; ++i)
			{
				MyJumpGateController outbound_controller = this.JumpGateGrid.GetController(this.OutboundConnectionControllers[i]);
				MyJumpGate inbound_controlee = this.GetConnectedControlledJumpGate(i);
				if (outbound_controller == null || inbound_controlee == null || outbound_controller.AttachedJumpGate() != inbound_controlee || inbound_controlee.Controller != outbound_controller) pairings[i] = new KeyValuePair<JumpGateUUID, JumpGateUUID>(JumpGateUUID.Empty, JumpGateUUID.Empty);
				else pairings[i] = new KeyValuePair<JumpGateUUID, JumpGateUUID>(JumpGateUUID.FromBlock(outbound_controller), JumpGateUUID.FromJumpGate(inbound_controlee));
			}

			MySerializedJumpGateRemoteAntenna serialized = base.ToSerialized<MySerializedJumpGateRemoteAntenna>(false);
			serialized.InboundControlJumpGates = this.InboundConnectionJumpGates;
			serialized.OutboundControlControllers = this.OutboundConnectionControllers;
			serialized.RemoteConnectionThreads = this.ConnectionThreads.Select((antenna) => antenna?.BlockID ?? -1).ToArray();
			serialized.NearbyEntities = this.NearbyEntities.ToList();
			serialized.BlockSettings = MyAPIGateway.Utilities.SerializeToBinary(this.BlockSettings);
			serialized.ControllerGatePairings = pairings;
			return serialized;
		}
		#endregion
	}

	[ProtoContract]
	internal class MySerializedJumpGateRemoteAntenna : MySerializedCubeBlockBase
	{
		/// <summary>
		/// The connected jump gates
		/// </summary>
		[ProtoMember(20)]
		public long[] InboundControlJumpGates;
		[ProtoMember(21)]
		public long[] OutboundControlControllers;
		[ProtoMember(22)]
		public long[] RemoteConnectionThreads;
		[ProtoMember(23)]
		public List<long> NearbyEntities;
		[ProtoMember(24)]
		public byte[] BlockSettings;
		[ProtoMember(25)]
		public KeyValuePair<JumpGateUUID, JumpGateUUID>[] ControllerGatePairings;
	}
}
