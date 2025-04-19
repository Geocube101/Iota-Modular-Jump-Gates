using IOTA.ModularJumpGates.Util;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.Gui.FactionTerminal;
using Sandbox.ModAPI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace IOTA.ModularJumpGates.CubeBlock
{
	public enum MyFactionDisplayType : byte { UNOWNED = 1, ENEMY = 2, NEUTRAL = 4, FRIENDLY = 8, OWNED = 16 }
	
	/// <summary>
	/// Game logic for jump gate controllers
	/// </summary>
	[MyEntityComponentDescriptor(typeof(MyObjectBuilder_UpgradeModule), false, "IOTA.JumpGate.JumpGateController.Large", "IOTA.JumpGate.JumpGateController.Small")]
	internal class MyJumpGateController : MyCubeBlockBase, Sandbox.ModAPI.Ingame.IMyTextSurfaceProvider
	{
		/// <summary>
		/// Stores block data for this block type
		/// </summary>
		[ProtoContract]
		public sealed class MyControllerBlockSettingsStruct
		{
			[ProtoMember(1)]
			private bool CanAutoActivate_V = false;
			[ProtoMember(2)]
			private bool CanBeInbound_V = true;
			[ProtoMember(3)]
			private bool CanBeOutbound_V = true;
			[ProtoMember(4)]
			private bool HasVectorNormalOverride_V = false;
			[ProtoMember(5)]
			private MyFactionDisplayType FactionDisplayType_V = MyFactionDisplayType.FRIENDLY | MyFactionDisplayType.OWNED;
			[ProtoMember(6)]
			private double JumpSpaceRadius_V = 250d;
			[ProtoMember(7)]
			private double JumpSpaceDepthPercent_V = 0.2d;
			[ProtoMember(8)]
			private long JumpGateID_V = -1;
			[ProtoMember(9)]
			private string JumpEffectName_V = "standard";
			[ProtoMember(10)]
			private string JumpGateName_V = null;
			[ProtoMember(11)]
			private Vector3D VectorNormal_V = Vector3D.Zero;
			[ProtoMember(12)]
			private Color EffectColorShift_V = Color.White;
			[ProtoMember(13)]
			private MyJumpGateWaypoint SelectedWaypoint_V = null;
			[ProtoMember(14)]
			private List<long> BlacklistedEntities = new List<long>();
			[ProtoMember(15)]
			private float MinimumEntityMass_V = 0;
			[ProtoMember(16)]
			private float MaximumEntityMass_V = float.PositiveInfinity;
			[ProtoMember(17)]
			private uint MinimumCubeGridSize_V = 0;
			[ProtoMember(18)]
			private uint MaximumCubeGridSize_V = uint.MaxValue;
			[ProtoMember(19)]
			private float MinimumAutoMass_V = 0;
			[ProtoMember(20)]
			private float MaximumAutoMass_V = float.PositiveInfinity;
			[ProtoMember(21)]
			private double MinimumAutoPower_V = 0;
			[ProtoMember(22)]
			private double MaximumAutoPower_V = double.PositiveInfinity;
			[ProtoMember(23)]
			private float AutoActivationDelay_V = 0;

			private readonly object WriterLock = new object();

			public void AcquireLock()
			{
				Monitor.Enter(this.WriterLock);
			}
			public void ReleaseLock()
			{
				if (Monitor.IsEntered(this.WriterLock)) Monitor.Exit(this.WriterLock);
			}

			public void FromDictionary(MyJumpGateController controller, Dictionary<string, object> mapping)
			{
				if (mapping == null || controller == null) return;

				lock (this.WriterLock)
				{
					this.CanAutoActivate_V = (bool) mapping.GetValueOrDefault("CanAutoActivate", this.CanAutoActivate_V);
					this.CanBeInbound_V = (bool) mapping.GetValueOrDefault("CanBeInbound", this.CanBeInbound_V);
					this.CanBeOutbound_V = (bool) mapping.GetValueOrDefault("CanBeOutbound", this.CanBeOutbound_V);
					this.HasVectorNormalOverride_V = (bool) mapping.GetValueOrDefault("HasVectorNormalOverride", this.HasVectorNormalOverride_V);
					this.FactionDisplayType_V = (MyFactionDisplayType) (byte) mapping.GetValueOrDefault("FactionDisplayType", (byte) this.FactionDisplayType_V);
					this.JumpSpaceRadius_V = (double) mapping.GetValueOrDefault("JumpSpaceRadius", this.JumpSpaceRadius_V);
					this.JumpSpaceDepthPercent_V = (double) mapping.GetValueOrDefault("JumpSpaceDepthPercent", this.JumpSpaceDepthPercent_V);
					this.JumpEffectName_V = (string) mapping.GetValueOrDefault("JumpEffectName", this.JumpEffectName_V);
					this.VectorNormal_V = (Vector3D) mapping.GetValueOrDefault("VectorNormal", this.VectorNormal_V);
					this.EffectColorShift_V = (Color) mapping.GetValueOrDefault("EffectColorShift", this.EffectColorShift_V);

					{
						object selected_waypoint = mapping.GetValueOrDefault("SelectedWaypoint", null);
						MyJumpGateWaypoint result_waypoint = null;
						if (selected_waypoint == null) result_waypoint = null;
						else if (selected_waypoint is IMyGps) result_waypoint = new MyJumpGateWaypoint((IMyGps) selected_waypoint);
						else if (selected_waypoint is Vector3D)
						{
							Vector3D position = (Vector3D) selected_waypoint;
							IMyGps gps = MyAPIGateway.Session.GPS.Create("�TemporaryGPS�", $"[{Math.Round(position.X, 2)}, {Math.Round(position.Y, 2)}, {Math.Round(position.Z, 2)}]", position, false);
							result_waypoint = new MyJumpGateWaypoint(gps);
						}
						else if (selected_waypoint is Guid)
						{
							MyJumpGate target_gate = MyJumpGateModSession.Instance.GetJumpGate(JumpGateUUID.FromGuid((Guid) selected_waypoint));
							if (target_gate == null) throw new InvalidGuuidException("The specified jump gate does not exist");
							result_waypoint = new MyJumpGateWaypoint(target_gate);
						}

						MyJumpGate attached_gate = controller.AttachedJumpGate();
						Vector3D? endpoint = result_waypoint?.GetEndpoint();
						Vector3D? node = attached_gate?.WorldJumpNode;

						if (endpoint != null && node != null)
						{
							double distance = Vector3D.Distance(endpoint.Value, node.Value);
							if (distance > attached_gate.JumpGateConfiguration.MaximumJumpDistance || distance < attached_gate.JumpGateConfiguration.MinimumJumpDistance) throw new InvalidOperationException("Specified waypoint distance is out of bounds");
							this.SelectedWaypoint_V = result_waypoint;
						}
					}

					this.BlacklistedEntities.Clear();
					this.BlacklistedEntities.AddRange((IEnumerable<long>) mapping.GetValueOrDefault("BlacklistedEntities", this.BlacklistedEntities));

					{
						float? minimum_mass_kg = (float?) mapping.GetValueOrDefault("MinimumEntityMass", null);
						float? maximum_mass_kg = (float?) mapping.GetValueOrDefault("MaximumEntityMass", null);
						float minimum = (minimum_mass_kg == null || float.IsNaN(minimum_mass_kg.Value)) ? this.MinimumEntityMass_V : minimum_mass_kg.Value;
						float maximum = (maximum_mass_kg == null || float.IsNaN(maximum_mass_kg.Value)) ? this.MaximumEntityMass_V : maximum_mass_kg.Value;
						this.MinimumEntityMass_V = MathHelper.Clamp(minimum, 0, maximum);
						if (this.MinimumEntityMass_V == float.MaxValue) this.MinimumEntityMass_V = float.PositiveInfinity;
						this.MaximumEntityMass_V = Math.Max(maximum, this.MinimumEntityMass_V);
						if (this.MaximumEntityMass_V == float.MaxValue) this.MaximumEntityMass_V = float.PositiveInfinity;
					}

					{
						uint? minimum_size = (uint?) mapping.GetValueOrDefault("MinimumCubeGridSize", null);
						uint? maximum_size = (uint?) mapping.GetValueOrDefault("MaximumCubeGridSize", null);
						this.MinimumCubeGridSize_V = minimum_size ?? this.MinimumCubeGridSize_V;
						this.MaximumCubeGridSize_V = maximum_size ?? this.MaximumCubeGridSize_V;
						this.MinimumCubeGridSize_V = Math.Max(0, Math.Min(this.MinimumCubeGridSize_V, this.MaximumCubeGridSize_V));
						this.MaximumCubeGridSize_V = Math.Max(this.MinimumCubeGridSize_V, this.MaximumCubeGridSize_V);
					}
				}
			}

			public Dictionary<string, object> ToDictionary()
			{
				lock (this.WriterLock)
				{
					MyJumpGateWaypoint waypoint = this.SelectedWaypoint_V;
					object selected_waypoint;

					switch (waypoint.WaypointType)
					{
						case MyWaypointType.JUMP_GATE:
							selected_waypoint = waypoint.JumpGate;
							break;
						case MyWaypointType.GPS:
							selected_waypoint = waypoint.GPS;
							break;
						default:
							selected_waypoint = null;
							break;
					}

					return new Dictionary<string, object>()
					{
						["CanAutoActivate"] = this.CanAutoActivate_V,
						["CanBeInbound"] = this.CanBeInbound_V,
						["CanBeOutbound"] = this.CanBeOutbound_V,
						["HasVectorNormalOverride"] = this.HasVectorNormalOverride_V,
						["FactionDisplayType"] = this.FactionDisplayType_V,
						["JumpSpaceRadius"] = this.JumpSpaceRadius_V,
						["JumpSpaceDepthPercent"] = this.JumpSpaceDepthPercent_V,
						["JumpEffectName"] = this.JumpEffectName_V,
						["JumpGateName"] = this.JumpGateName_V,
						["VectorNormal"] = this.VectorNormal_V,
						["EffectColorShift"] = this.EffectColorShift_V,
						["SelectedWaypoint"] = selected_waypoint,
						["BlacklistedEntities"] = new List<long>(this.BlacklistedEntities),
						["MinimumEntityMass"] = this.MinimumEntityMass_V,
						["MaximumEntityMass"] = this.MaximumEntityMass_V,
						["MinimumCubeGridSize"] = this.MinimumCubeGridSize_V,
						["MaximumCubeGridSize"] = this.MaximumCubeGridSize_V,
					};
				}
			}

			public void CanAutoActivate(bool flag)
			{
				lock (this.WriterLock) this.CanAutoActivate_V = flag;
			}
			public void CanBeInbound(bool flag)
			{
				lock (this.WriterLock) this.CanBeInbound_V = flag;
			}
			public void CanBeOutbound(bool flag)
			{
				lock (this.WriterLock) this.CanBeOutbound_V = flag;
			}
			public void CanAcceptUnowned(bool flag)
			{
				lock (this.WriterLock)
				{
					if (flag) this.FactionDisplayType_V |= MyFactionDisplayType.UNOWNED;
					else this.FactionDisplayType_V &= ~MyFactionDisplayType.UNOWNED;
				}
			}
			public void CanAcceptEnemy(bool flag)
			{
				lock (this.WriterLock)
				{
					if (flag) this.FactionDisplayType_V |= MyFactionDisplayType.ENEMY;
					else this.FactionDisplayType_V &= ~MyFactionDisplayType.ENEMY;
				}
			}
			public void CanAcceptNeutral(bool flag)
			{
				lock (this.WriterLock)
				{
					if (flag) this.FactionDisplayType_V |= MyFactionDisplayType.NEUTRAL;
					else this.FactionDisplayType_V &= ~MyFactionDisplayType.NEUTRAL;
				}
			}
			public void CanAcceptFriendly(bool flag)
			{
				lock (this.WriterLock)
				{
					if (flag) this.FactionDisplayType_V |= MyFactionDisplayType.FRIENDLY;
					else this.FactionDisplayType_V &= ~MyFactionDisplayType.FRIENDLY;
				}
			}
			public void CanAcceptOwned(bool flag)
			{
				lock (this.WriterLock)
				{
					if (flag) this.FactionDisplayType_V |= MyFactionDisplayType.OWNED;
					else this.FactionDisplayType_V &= ~MyFactionDisplayType.OWNED;
				}
			}
			public void CanAccept(MyFactionDisplayType value)
			{
				lock (this.WriterLock) this.FactionDisplayType_V = value;
			}
			public void HasVectorNormalOverride(bool flag)
			{
				lock (this.WriterLock) this.HasVectorNormalOverride_V = flag;
			}
			public void AutoActivationDelay(float seconds)
			{
				lock (this.WriterLock) this.AutoActivationDelay_V = MathHelper.Clamp(seconds, 0, 60);
			}
			public void JumpSpaceRadius(double radius)
			{
				radius = Math.Max(radius, 0);
				lock (this.WriterLock) this.JumpSpaceRadius_V = radius;
			}
			public void JumpSpaceDepthPercent(double depth_percentage)
			{
				depth_percentage = MathHelperD.Clamp(depth_percentage, 0.01, 1);
				lock (this.WriterLock) this.JumpSpaceDepthPercent_V = depth_percentage;
			}
			public void JumpGateID(long id)
			{
				id = Math.Max(id, -1);
				lock (this.WriterLock) this.JumpGateID_V = id;
			}
			public void JumpEffectAnimationName(string name)
			{
				lock (this.WriterLock) this.JumpEffectName_V = (name == null || name.Length == 0) ? "standard" : name;
			}
			public void JumpGateName(string name)
			{
				lock (this.WriterLock) this.JumpGateName_V = ((name?.Length ?? 0) == 0) ? null : name;
			}
			public void VectorNormalOverride(Vector3D? vector)
			{
				lock (this.WriterLock)
				{
					this.HasVectorNormalOverride_V = vector != null;
					this.VectorNormal_V = (vector == null || !vector.IsValid()) ? Vector3D.Zero : vector.Value;
				}
			}
			public void JumpEffectAnimationColorShift(Color color_shift)
			{
				lock (this.WriterLock) this.EffectColorShift_V = color_shift;
			}
			public void SelectedWaypoint(MyJumpGateWaypoint waypoint)
			{
				lock (this.WriterLock) this.SelectedWaypoint_V = waypoint;
			}
			public void AddBlacklistedEntity(long entity_id)
			{
				lock (this.WriterLock) this.BlacklistedEntities.Add(entity_id);
			}
			public void RemoveBlacklistedEntity(long entity_id)
			{
				lock (this.WriterLock) this.BlacklistedEntities.Remove(entity_id);
			}
			public void SetBlacklistedEntities(IEnumerable<long> entities)
			{
				lock (this.WriterLock)
				{
					this.BlacklistedEntities.Clear();
					if (entities != null) this.BlacklistedEntities.AddRange(entities);
				}
			}
			public void AllowedEntityMass(float? minimum_mass_kg = null, float? maximum_mass_kg = null)
			{
				lock (this.WriterLock)
				{
					float minimum = (minimum_mass_kg == null || float.IsNaN(minimum_mass_kg.Value)) ? this.MinimumEntityMass_V : minimum_mass_kg.Value;
					float maximum = (maximum_mass_kg == null || float.IsNaN(maximum_mass_kg.Value)) ? this.MaximumEntityMass_V : maximum_mass_kg.Value;
					this.MinimumEntityMass_V = MathHelper.Clamp(minimum, 0, maximum);
					if (this.MinimumEntityMass_V == float.MaxValue) this.MinimumEntityMass_V = float.PositiveInfinity;
					this.MaximumEntityMass_V = Math.Max(maximum, this.MinimumEntityMass_V);
					if (this.MaximumEntityMass_V == float.MaxValue) this.MaximumEntityMass_V = float.PositiveInfinity;
				}
			}
			public void AllowedCubeGridSize(uint? minimum_size = null, uint? maximum_size = null)
			{
				lock (this.WriterLock)
				{
					this.MinimumCubeGridSize_V = minimum_size ?? this.MinimumCubeGridSize_V;
					this.MaximumCubeGridSize_V = maximum_size ?? this.MaximumCubeGridSize_V;
					this.MinimumCubeGridSize_V = Math.Max(0, Math.Min(this.MinimumCubeGridSize_V, this.MaximumCubeGridSize_V));
					this.MaximumCubeGridSize_V = Math.Max(this.MinimumCubeGridSize_V, this.MaximumCubeGridSize_V);
				}
			}
			public void AutoActivateMass(float? minimum_mass_kg = null, float? maximum_mass_kg = null)
			{
				lock (this.WriterLock)
				{
					float minimum = (minimum_mass_kg == null || float.IsNaN(minimum_mass_kg.Value)) ? this.MinimumAutoMass_V : minimum_mass_kg.Value;
					float maximum = (maximum_mass_kg == null || float.IsNaN(maximum_mass_kg.Value)) ? this.MaximumAutoMass_V : maximum_mass_kg.Value;
					this.MinimumAutoMass_V = MathHelper.Clamp(minimum, 0, maximum);
					if (this.MaximumAutoMass_V == float.MaxValue) this.MaximumAutoMass_V = float.PositiveInfinity;
					this.MaximumAutoMass_V = Math.Max(maximum, this.MinimumAutoMass_V);
					if (this.MaximumAutoMass_V == float.MaxValue) this.MaximumAutoMass_V = float.PositiveInfinity;
				}
			}
			public void AutoActivatePower(double? minimum_power_mw = null, double? maximum_power_mw = null)
			{
				lock (this.WriterLock)
				{
					double minimum = (minimum_power_mw == null || double.IsNaN(minimum_power_mw.Value)) ? this.MinimumAutoPower_V : minimum_power_mw.Value;
					double maximum = (maximum_power_mw == null || double.IsNaN(maximum_power_mw.Value)) ? this.MaximumAutoPower_V : maximum_power_mw.Value;
					this.MinimumAutoPower_V = MathHelper.Clamp(minimum, 0, maximum);
					if (this.MaximumAutoPower_V == double.MaxValue) this.MaximumAutoPower_V = double.PositiveInfinity;
					this.MaximumAutoPower_V = Math.Max(maximum, this.MinimumAutoPower_V);
					if (this.MaximumAutoPower_V == double.MaxValue) this.MaximumAutoPower_V = double.PositiveInfinity;
				}
			}

			public bool CanAutoActivate()
			{
				return this.CanAutoActivate_V;
			}
			public bool CanBeInbound()
			{
				return this.CanBeInbound_V;
			}
			public bool CanBeOutbound()
			{
				return this.CanBeOutbound_V;
			}
			public bool CanAcceptUnowned()
			{
				return (this.FactionDisplayType_V & MyFactionDisplayType.UNOWNED) != 0;
			}
			public bool CanAcceptEnemy()
			{
				return (this.FactionDisplayType_V & MyFactionDisplayType.ENEMY) != 0;
			}
			public bool CanAcceptNeutral()
			{
				return (this.FactionDisplayType_V & MyFactionDisplayType.NEUTRAL) != 0;
			}
			public bool CanAcceptFriendly()
			{
				return (this.FactionDisplayType_V & MyFactionDisplayType.FRIENDLY) != 0;
			}
			public bool CanAcceptOwned()
			{
				return (this.FactionDisplayType_V & MyFactionDisplayType.OWNED) != 0;
			}
			public MyFactionDisplayType CanAccept()
			{
				return this.FactionDisplayType_V;
			}
			public bool HasVectorNormalOverride()
			{
				return this.HasVectorNormalOverride_V;
			}
			public float AutoActivationDelay()
			{
				return this.AutoActivationDelay_V;
			}
			public double JumpSpaceRadius()
			{
				return Math.Max(this.JumpSpaceRadius_V, 0);
			}
			public double JumpSpaceDepthPercent()
			{
				return this.JumpSpaceDepthPercent_V;
			}
			public long JumpGateID()
			{
				return Math.Max(this.JumpGateID_V, -1);
			}
			public string JumpEffectAnimationName()
			{
				return this.JumpEffectName_V;
			}
			public string JumpGateName()
			{
				return this.JumpGateName_V;
			}
			public Vector3D? VectorNormalOverride()
			{
				if (this.HasVectorNormalOverride_V && this.VectorNormal_V.IsValid()) return this.VectorNormal_V;
				else return null;
			}
			public Color JumpEffectAnimationColorShift()
			{
				return this.EffectColorShift_V;
			}
			public MyJumpGateWaypoint SelectedWaypoint()
			{
				return this.SelectedWaypoint_V;
			}
			public bool IsEntityBlacklisted(long entity_id)
			{
				lock (this.WriterLock) return this.BlacklistedEntities.Contains(entity_id);
			}
			public List<long> GetBlacklistedEntities()
			{
				lock (this.WriterLock) return new List<long>(this.BlacklistedEntities);
			}
			public KeyValuePair<float, float> AllowedEntityMass()
			{
				return new KeyValuePair<float, float>(this.MinimumEntityMass_V, this.MaximumEntityMass_V);
			}
			public KeyValuePair<uint, uint> AllowedCubeGridSize()
			{
				return new KeyValuePair<uint, uint>(this.MinimumCubeGridSize_V, this.MaximumCubeGridSize_V);
			}
			public KeyValuePair<float, float> AutoActivateMass()
			{
				return new KeyValuePair<float, float>(this.MinimumAutoMass_V, this.MaximumAutoMass_V);
			}
			public KeyValuePair<double, double> AutoActivatePower()
			{
				return new KeyValuePair<double, double>(this.MinimumAutoPower_V, this.MaximumAutoPower_V);
			}
		}

		#region Private Static Variables
		/// <summary>
		/// The maximum viewing angle in radians
		/// </summary>
		private static readonly double MaxHoloViewAngle = 60d * (Math.PI / 180d);
		#endregion

		#region Private Variables
		private double HoloDisplayScale = 1;

		/// <summary>
		/// Mutex object for exclusive read-write operations on the WaypointsList
		/// </summary>
		private object WaypointsListMutex = new object();

		private MatrixD HoloDisplayScalar = MatrixD.CreateScale(1);

		/// <summary>
		/// Client-side only<br />
		/// Stores all applicable waypoints for this block
		/// </summary>
		private List<MyJumpGateWaypoint> WaypointsList = new List<MyJumpGateWaypoint>();

		/// <summary>
		/// A temporary list of this controller's attached jump gate drives
		/// </summary>
		private List<MyJumpGateDrive> AttachedJumpGateDrives = new List<MyJumpGateDrive>();

		/// <summary>
		/// The MyMultiTextPanelComponent renderer to support multi-lcd screens
		/// </summary>
		private MyMultiTextPanelComponent MultiPanelComponent;
		#endregion

		#region Temporary Collections
		/// <summary>
		/// Temporary list of construct grids
		/// </summary>
		private List<IMyCubeGrid> TEMP_DetailedInfoConstructsList = new List<IMyCubeGrid>();

		/// <summary>
		/// Temporary list of beacon linked grids
		/// </summary>
		private List<MyBeaconLinkWrapper> TEMP_BeaconLinkedGrids = new List<MyBeaconLinkWrapper>();

		/// <summary>
		/// Temporary list of construct controllers
		/// </summary>
		private List<MyJumpGateController> TEMP_ConstructControllers = new List<MyJumpGateController>();

		/// <summary>
		/// Temporary list of comm linked grids
		/// </summary>
		private List<MyJumpGateConstruct> TEMP_CommLinkedGrids = new List<MyJumpGateConstruct>();

		/// <summary>
		/// Temporary list of jump gate drives for holo display
		/// </summary>
		private Dictionary<MyJumpGateDrive, KeyValuePair<MatrixD, BoundingBoxD>> TEMP_DriveHoloBoxes = new Dictionary<MyJumpGateDrive, KeyValuePair<MatrixD, BoundingBoxD>>();

		/// <summary>
		/// Temporary list of jump space entities
		/// </summary>
		private ConcurrentDictionary<MyEntity, float> TEMP_JumpGateEntities = new ConcurrentDictionary<MyEntity, float>();

		/// <summary>
		/// Temporary list of unconfirmed jump space entities
		/// </summary>
		private ConcurrentDictionary<long, float> TEMP_UnconfirmedJumpGateEntities = new ConcurrentDictionary<long, float>();
		#endregion

		#region Public Variables
		/// <summary>
		/// IMyTextSurfaceProvider Property
		/// </summary>
		public bool UseGenericLcd { get { return this.MultiPanelComponent.UseGenericLcd; } }

		/// <summary>
		/// IMyTextSurfaceProvider Property
		/// </summary>
		public int SurfaceCount { get { return this.MultiPanelComponent.SurfaceCount; } }

		/// <summary>
		/// The block data for this block
		/// </summary>
		public MyControllerBlockSettingsStruct BlockSettings { get; private set; }
		#endregion

		#region Constructors
		public MyJumpGateController() : base() { }

		/// <summary>
		/// Creates a new null wrapper
		/// </summary>
		/// <param name="serialized">The serialized block data</param>
		public MyJumpGateController(MySerializedJumpGateController serialized) : base(serialized)
		{
			this.FromSerialized(serialized);
		}
		#endregion

		#region CubeBlockBase Methods
		/// <summary>
		/// CubeBlockBase Method<br />
		/// Appends info to the detailed info screen
		/// </summary>
		/// <param name="info">The string builder to append to</param>
		protected override void AppendCustomInfo(StringBuilder sb)
		{
			base.AppendCustomInfo(sb);
			MyResourceSinkComponent sink = this.ResourceSink;
			
			if (sink != null && this.LocalGameTick % 15 == 0)
			{
				float input_wattage = sink.CurrentInputByType(MyResourceDistributorComponent.ElectricityId);
				float required_input = sink.RequiredInputByType(MyResourceDistributorComponent.ElectricityId);
				MyJumpGateConstruct this_grid = (this.JumpGateGrid?.MarkClosed ?? true) ? null : this.JumpGateGrid;
				MyJumpGate jump_gate = this.AttachedJumpGate();
				jump_gate = (jump_gate?.MarkClosed ?? true) ? null : jump_gate;
				BoundingEllipsoidD? jump_ellipse = jump_gate?.JumpEllipse;
				Vector3D? jump_node = jump_gate?.WorldJumpNode;
				Vector3D endpoint = this.BlockSettings.SelectedWaypoint()?.GetEndpoint() ?? Vector3D.Zero;

				if (this.LocalGameTick % 30 == 0)
				{
					this.TEMP_DetailedInfoConstructsList.Clear();
					this_grid?.GetCubeGrids(this.TEMP_DetailedInfoConstructsList);
				}

				double distance = (jump_node == null) ? -1 : Vector3D.Distance(endpoint, jump_node.Value);
				double distance_ratio = jump_gate?.CalculateDistanceRatio(ref endpoint) ?? -1;
				double total_mass_kg = (this.TEMP_JumpGateEntities?.Sum((pair) => (double) pair.Value) ?? 0) + (this.TEMP_UnconfirmedJumpGateEntities?.Sum((pair) => (double) pair.Value) ?? 0);
				double total_required_power_mw = jump_gate?.CalculateTotalRequiredPower(endpoint, null, total_mass_kg) ?? double.NaN;
				double total_available_power_mw = this_grid?.CalculateTotalAvailableInstantPower(jump_gate?.JumpGateID ?? -1) ?? double.NaN;

				sb.Append($"\n-=-=-=( Jump Gate Controller )=-=-=-\n");
				sb.Append($" Input: {input_wattage} MW\n");
				sb.Append($" Required Input: {required_input} MW\n");
				sb.Append($" Input Ratio: {Math.Round(MathHelperD.Clamp(input_wattage / required_input, 0, 1) * 100, 2):#.00}%\n");
				sb.Append($" Attached Jump Gate: {jump_gate?.GetPrintableName() ?? "N/A"}\n");
				sb.Append($" Attached Jump Gate ID: {jump_gate?.JumpGateID.ToString() ?? "N/A"}\n");				

				sb.Append($"\n[color=#FF78FFFB]--- Jump Gate Info ---[/color][color=#FF5ABFBC]\n");
				sb.Append($" - Status: {jump_gate?.Status.ToString() ?? "N/A"}\n");
				sb.Append($" - Phase: {jump_gate?.Phase.ToString() ?? "N/A"}\n");
				sb.Append($" - Drive Count: {this_grid?.GetDriveCount((drive) => drive.JumpGateID == jump_gate?.JumpGateID).ToString() ?? "N/A"}\n");
				sb.Append($" - Grid Size Type: {jump_gate?.CubeGridSize().ToString() ?? "N/A"}\n");
				sb.Append($" - Radius: {((jump_gate == null) ? "N/A" : MyJumpGateModSession.AutoconvertMetricUnits(jump_ellipse.Value.Radii.X, "m", 4))}\n");
				sb.Append($" - Effective Radius: {((jump_gate == null) ? "N/A" : MyJumpGateModSession.AutoconvertMetricUnits(jump_ellipse.Value.Radii.X, "m", 4))}\n");
				sb.Append($" - Node Velocity: {((jump_gate == null) ? "N/A" : MyJumpGateModSession.AutoconvertMetricUnits(jump_gate.JumpNodeVelocity.Length(), "m/s", 4))}[/color]\n");

				sb.Append($"\n[color=#FF26FF3C]--- Jump Gate Distances ---[/color][color=#FF1CBF2D]\n");
				sb.Append($" - Max Possible Distance: {((jump_gate == null) ? "N/A" : MyJumpGateModSession.AutoconvertMetricUnits(jump_gate.JumpGateConfiguration.MaximumJumpDistance, "m", 4))}\n");
				sb.Append($" - Min Possible Distance: {((jump_gate == null) ? "N/A" : MyJumpGateModSession.AutoconvertMetricUnits(jump_gate.JumpGateConfiguration.MinimumJumpDistance, "m", 4))}\n");
				sb.Append($" - Reasonable Distance: {((jump_gate == null) ? "N/A" : MyJumpGateModSession.AutoconvertMetricUnits(jump_gate.CalculateMaxGateDistance(), "m", 4))}\n");
				sb.Append($" - Ideal 50-Gate Distance: {((jump_gate == null) ? "N/A" : MyJumpGateModSession.AutoconvertMetricUnits(jump_gate.JumpGateConfiguration.MaxJumpGate50Distance, "m", 4))}\n");
				sb.Append($" - Distance to Target: {((jump_gate == null) ? "N/A" : MyJumpGateModSession.AutoconvertMetricUnits(distance, "m", 4))}\n");
				sb.Append($" - Drives Needed: {((jump_gate == null) ? "N/A" : jump_gate.CalculateDrivesRequiredForDistance(distance).ToString())}\n");
				sb.Append($" - Distance Ratio: {((jump_gate == null) ? "N/A" : MyJumpGateModSession.AutoconvertSciNotUnits(distance_ratio, 4))}\n");
				sb.Append("[/color]");

				sb.Append($"\n[color=#FFFF6E26]--- Power Info ---[/color][color=#FFBF521C]\n");
				sb.Append($" - Power Factor: {((jump_gate == null) ? "N/A" : MyJumpGateModSession.AutoconvertSciNotUnits(jump_gate.CalculatePowerFactorFromDistanceRatio(distance_ratio), 4))}\n");
				sb.Append($" - Jump Space Mass: {((jump_gate == null) ? "N/A" : MyJumpGateModSession.AutoconvertMetricUnits(total_mass_kg * 1000, "g", 4))}\n");
				sb.Append($" - Power Density: {((jump_gate == null) ? "N/A" : MyJumpGateModSession.AutoconvertMetricUnits(jump_gate.CalculateUnitPowerRequiredForJump(ref endpoint) * 1000, "w/kg", 4))}\n");
				sb.Append($" - Total Required Power: {((jump_gate == null) ? "N/A" : MyJumpGateModSession.AutoconvertMetricUnits(total_required_power_mw * 1e6, "w", 2))}\n");
				sb.Append($" - Available Instant Power: {((jump_gate == null) ? "N/A" : MyJumpGateModSession.AutoconvertMetricUnits(total_available_power_mw * 1e6, "w", 2))}\n");
				sb.Append($" - Power Percentage: {((jump_gate == null) ? "N/A" : $"{Math.Round(MathHelper.Clamp(total_available_power_mw / total_required_power_mw, 0, 1) * 100, 2)}%")}[/color]\n");

				sb.Append($"\n[color=#FFC226FF]--- Jump Space Entities ---[/color]\n");

				foreach (KeyValuePair<MyEntity, float> pair in this.TEMP_JumpGateEntities)
				{
					string name = $" {pair.Key.DisplayName}";
					string info = $" - {MyJumpGateModSession.AutoconvertMetricUnits(pair.Value * 1e3, "g", 4)}";
					const int max_length = 30;
					int remaining_length = max_length - info.Length - name.Length;
					int chop_length = -Math.Min(remaining_length, 0);

					if (chop_length > 0)
					{
						chop_length += 3;
						info = $"...{info}";
					}

					sb.Append($"[color=#FF911CBF] - {name.Substring(0, name.Length - chop_length)}{info}[/color]\n");
				}

				foreach (KeyValuePair<long, float> pair in this.TEMP_UnconfirmedJumpGateEntities)
				{
					string name = $" U:{pair.Key}";
					string info = $" - {MyJumpGateModSession.AutoconvertMetricUnits(pair.Value * 1e3, "g", 4)}";
					const int max_length = 30;
					int remaining_length = max_length - info.Length - name.Length;
					int chop_length = -Math.Min(remaining_length, 0);

					if (chop_length > 0)
					{
						chop_length += 3;
						info = $"...{info}";
					}

					sb.Append($"[color=#FF911CBF] - {name.Substring(0, name.Length - chop_length)}{info}[/color]\n");
				}

				sb.Append($"\n[color=#FF78FFFB]--- Construct Info ---[/color][color=#FF5ABFBC]\n");
				sb.Append($" - Main Grid: {(this_grid?.CubeGridID.ToString() ?? "N/A")}\n");
				sb.Append($" - Static Grids: {((this_grid == null) ? "N/A" : $"{this.TEMP_DetailedInfoConstructsList.Count((grid) => grid.IsStatic)}")}/{this.TEMP_DetailedInfoConstructsList.Count}\n");
				sb.Append($" - Total Grid Drives: {(this_grid?.GetDriveCount().ToString() ?? "N/A")}\n");
				sb.Append($" - Total Grid Gates: {(this_grid?.GetJumpGateCount().ToString() ?? "N/A")}[/color]\n");

				if (MyJumpGateModSession.DebugMode)
				{
					sb.Append($"\n--- Debug Info ---[color=#FFCCCCCC]\n");

					sb.Append($"\n :Construct Info:\n");
					sb.Append($" ... This Grid ID: {(this.TerminalBlock?.CubeGrid?.EntityId.ToString() ?? "N/A")}\n");
					sb.Append($" ... Is Current Grid Main: {((this_grid == null) ? "N/A" : (this.TerminalBlock.CubeGrid.EntityId == this_grid.CubeGridID).ToString())}\n");
					sb.Append($" ... Gate Collider: {jump_gate?.JumpSpaceColliderStatus().ToString() ?? "N/A"}\n");
					sb.Append($" ... Marked for Gate Update: {((this_grid == null) ? "N/A" : this_grid.MarkUpdateJumpGates.ToString())}\n");
					sb.Append($"\n <<< Update Times >>>\n");
					sb.Append($" ... Average: {((this_grid == null) ? "N/A" : Math.Round(this_grid.AverageUpdateTime60(), 4).ToString())}ms\n");
					sb.Append($" ... Longest: {((this_grid == null) ? "N/A" : Math.Round(this_grid.LongestUpdateTime, 4).ToString())}ms\n");
					sb.Append($" ... Longest Local: {((this_grid == null) ? "N/A" : Math.Round(this_grid.LocalLongestUpdateTime60(), 4).ToString())}ms\n");

					sb.Append($"\n :Session Info:\n");
					sb.Append($" ... Session Entities Loaded: {(MyNetworkInterface.IsServerLike ? MyJumpGateModSession.Instance.AllSessionEntitiesLoaded.ToString() : "N/A")}\n");
					sb.Append($" ... Session Entity Load Time: {(MyNetworkInterface.IsServerLike ? Math.Round((MyJumpGateModSession.Instance.EntityLoadedDelayTicks / 60d), 2).ToString() : "N/A")}\n");
					sb.Append($"\n <<< Grid Update Times >>>\n");
					sb.Append($" ... Average: {Math.Round(MyJumpGateModSession.Instance.AverageGridUpdateTime60(), 4)}ms\n");
					sb.Append($" ... Longest Local: {Math.Round(MyJumpGateModSession.Instance.LocalLongestGridUpdateTime60(), 4)}ms\n");
					sb.Append($"\n <<< Session Update Times >>>\n");
					sb.Append($" ... Average: {Math.Round(MyJumpGateModSession.Instance.AverageSessionUpdateTime60(), 4)}ms\n");
					sb.Append($" ... Longest Local: {Math.Round(MyJumpGateModSession.Instance.LocalLongestSessionUpdateTime60(), 4)}ms\n");

					sb.Append($"\n :MP Update Info:\n");
					sb.Append($" ... Last Block Update: {this.LastUpdateDateTimeUTC.ToLocalTime().ToLongTimeString()}\n");
					sb.Append($" ... Last Gate Update: {(jump_gate?.LastUpdateDateTimeUTC.ToLocalTime().ToLongTimeString() ?? "N/A")}\n");
					sb.Append($" ... Last Grid Update: {this_grid.LastUpdateDateTimeUTC.ToLocalTime().ToLongTimeString()}[/color]\n");
				}
			}
		}

		protected override void Clean()
		{
			base.Clean();
			this.WaypointsList.Clear();
			this.AttachedJumpGateDrives.Clear();

			this.TEMP_DetailedInfoConstructsList.Clear();
			this.TEMP_ConstructControllers.Clear();
			this.TEMP_CommLinkedGrids.Clear();
			this.TEMP_BeaconLinkedGrids.Clear();
			this.TEMP_JumpGateEntities.Clear();
			this.TEMP_UnconfirmedJumpGateEntities.Clear();
			this.TEMP_DriveHoloBoxes.Clear();

			this.WaypointsList = null;
			this.AttachedJumpGateDrives = null;
			this.MultiPanelComponent = null;
			this.TEMP_DetailedInfoConstructsList = null;
			this.TEMP_ConstructControllers = null;
			this.TEMP_CommLinkedGrids = null;
			this.TEMP_BeaconLinkedGrids = null;
			this.TEMP_JumpGateEntities = null;
			this.TEMP_UnconfirmedJumpGateEntities = null;
			this.TEMP_DriveHoloBoxes = null;
			this.BlockSettings = null;
		}

		/// <summary>
		/// Initializes the block's game logic
		/// </summary>
		/// <param name="object_builder"></param>
		public override void Init(MyObjectBuilder_EntityBase object_builder)
		{
			MyEntity block_entity = (MyEntity) this.Entity;
			MyRenderComponentScreenAreas renderer = new MyRenderComponentScreenAreas(block_entity);
			MyRenderComponentBase old_renderer = this.Entity.Render;
			this.Entity.Render = renderer;
			this.MultiPanelComponent = block_entity.Components.Get<MyMultiTextPanelComponent>();

			if (this.MultiPanelComponent == null)
			{
				this.MultiPanelComponent = new MyMultiTextPanelComponent();
				this.Entity.Components.Add(this.MultiPanelComponent);
			}

			this.MultiPanelComponent?.SetRender(renderer);
			renderer.ColorMaskHsv = old_renderer.ColorMaskHsv;
			renderer.EnableColorMaskHsv = old_renderer.EnableColorMaskHsv;
			renderer.TextureChanges = old_renderer.TextureChanges;
			renderer.MetalnessColorable = old_renderer.MetalnessColorable;
			renderer.PersistentFlags = old_renderer.PersistentFlags;
			this.Init(block_entity.GetObjectBuilder(), MyEntityUpdateEnum.EACH_FRAME, 0.3f, MyJumpGateModSession.BlockComponentDataGUID);
			this.TerminalBlock.Synchronized = true;
			if (MyJumpGateModSession.Network.Registered) MyJumpGateModSession.Network.On(MyPacketTypeEnum.UPDATE_CONTROLLER, this.OnNetworkBlockUpdate);
			string blockdata;

			if (this.ModStorageComponent.TryGetValue(MyJumpGateModSession.BlockComponentDataGUID, out blockdata) && blockdata.Length > 0)
			{
				try
				{
					this.BlockSettings = MyAPIGateway.Utilities.SerializeFromBinary<MyControllerBlockSettingsStruct>(Convert.FromBase64String(blockdata));
				}
				catch (Exception e)
				{
					this.BlockSettings = new MyControllerBlockSettingsStruct();
					Logger.Error($"Failed to load block data: {this.GetType().Name}-{this.TerminalBlock.CustomName}\n{e}");
				}
			}
			else
			{
				this.BlockSettings = new MyControllerBlockSettingsStruct();
				this.ModStorageComponent.Add(MyJumpGateModSession.BlockComponentDataGUID, "");
			}

			if (MyJumpGateModSession.Network.Registered && MyNetworkInterface.IsStandaloneMultiplayerClient)
			{
				MyNetworkInterface.Packet request = new MyNetworkInterface.Packet
				{
					TargetID = 0,
					Broadcast = false,
					PacketType = MyPacketTypeEnum.UPDATE_CONTROLLER,
				};
				request.Payload<MySerializedJumpGateController>(this.ToSerialized(true));
				request.Send();
			}
		}

		/// <summary>
		/// Called when this block is marked for close <br />
		/// Detaches itself from the attached gate if attached
		/// </summary>
		public override void MarkForClose()
		{
			base.MarkForClose();
			if (this.BlockSettings == null) return;

			if (!this.TerminalBlock.InScene)
			{
				byte[] serialized = MyAPIGateway.Utilities.SerializeToBinary(this.ToSerialized(false));
				string cached = Convert.ToBase64String(serialized);
			}

			if (this.BlockSettings.JumpGateID() != -1 && this.JumpGateGrid != null && !this.JumpGateGrid.Closed)
			{
				MyJumpGate jump_gate = this.JumpGateGrid.GetJumpGate(this.BlockSettings.JumpGateID());
				if (jump_gate != null) jump_gate.Controller = null;
			}

			if (MyJumpGateModSession.Network.Registered) MyJumpGateModSession.Network.Off(MyPacketTypeEnum.UPDATE_CAPACITOR, this.OnNetworkBlockUpdate);
			this.BlockSettings.JumpGateID(-1);
			this.BlockSettings.SelectedWaypoint(null);
		}

		/// <summary>
		/// CubeBlockBase Method<br />
		/// Called once every tick after simulation<br />
		/// Updates emissives and holo-display
		/// </summary>
		public override void UpdateAfterSimulation()
		{
			base.UpdateAfterSimulation();
			
			bool working;
			if (this.TerminalBlock?.CubeGrid?.Physics == null || this.TerminalBlock.MarkedForClose) return;
			else if (working = this.IsWorking()) this.TerminalBlock.SetEmissiveParts("Emissive0", Color.Green, 1);
			else if (this.TerminalBlock.IsFunctional) this.TerminalBlock.SetEmissiveParts("Emissive0", Color.Red, 1);
			else this.TerminalBlock.SetEmissiveParts("Emissive0", Color.Black, 1);
			
			if (!working || !MyJumpGateModSession.Instance.IsJumpGateGridMultiplayerValid(this.JumpGateGrid)) return;
			MyJumpGate jump_gate = this.AttachedJumpGate();
			bool jump_gate_valid = jump_gate?.IsValid() ?? false;
			if (jump_gate_valid && jump_gate.Controller == null) jump_gate.Controller = this;
			else if (jump_gate_valid && jump_gate.Controller != this)
			{
				this.AttachedJumpGate(null);
				jump_gate = null;
				jump_gate_valid = false;
			}

			// Update waypoints
			if (jump_gate_valid && !MyNetworkInterface.IsDedicatedMultiplayerServer && MyAPIGateway.Gui.GetCurrentScreen == MyTerminalPageEnum.ControlPanel && MyJumpGateModSession.GameTick % 60 == 0)
			{
				long player_identity = MyAPIGateway.Players.TryGetIdentityId(MyAPIGateway.Multiplayer.MyId);
				double distance;
				if (MyJumpGateModSession.Configuration.ConstructConfiguration.RequireGridCommLink) this.JumpGateGrid?.GetCommLinkedJumpGateGrids(this.TEMP_CommLinkedGrids);
				else MyJumpGateModSession.Instance.GetAllJumpGateGrids(this.TEMP_CommLinkedGrids);
				Vector3D jump_node = jump_gate.WorldJumpNode;
				this.JumpGateGrid.GetBeaconsWithinReverseBroadcastSphere(this.TEMP_BeaconLinkedGrids, (beacon) => (distance = Vector3D.Distance(jump_node, beacon.BeaconPosition)) >= jump_gate.JumpGateConfiguration.MinimumJumpDistance && distance <= jump_gate.JumpGateConfiguration.MaximumJumpDistance);
				lock (this.WaypointsListMutex) this.WaypointsList.Clear();

				if (jump_gate.ServerAntenna != null)
				{

				}

				foreach (MyJumpGateConstruct connected_grid in this.TEMP_CommLinkedGrids)
				{
					if (connected_grid == this.JumpGateGrid || !connected_grid.IsValid()) continue;
					connected_grid.GetAttachedJumpGateControllers(this.TEMP_ConstructControllers);

					foreach (MyJumpGateController controller in this.TEMP_ConstructControllers)
					{
						MyJumpGate other_gate = controller.AttachedJumpGate();
						if (other_gate == null || other_gate.MarkClosed || !controller.IsWorking()) continue;
						distance = Vector3D.Distance(jump_node, other_gate.WorldJumpNode);
						if (distance < jump_gate.JumpGateConfiguration.MinimumJumpDistance || distance > jump_gate.JumpGateConfiguration.MaximumJumpDistance || !controller.IsFactionRelationValid(player_identity)) continue;
						lock (this.WaypointsListMutex) this.WaypointsList.Add(new MyJumpGateWaypoint(other_gate));
					}

					this.TEMP_ConstructControllers.Clear();
				}

				lock (this.WaypointsListMutex)
				{
					this.WaypointsList.AddRange(this.TEMP_BeaconLinkedGrids.OrderBy((beacon) => Vector3D.Distance(beacon.BeaconPosition, jump_node)).Select((beacon) => new MyJumpGateWaypoint(beacon)));
					this.WaypointsList.AddRange(MyAPIGateway.Session.GPS.GetGpsList(player_identity).Where((gps) => gps.Coords.IsValid()).OrderBy((gps) => Vector3D.Distance(gps.Coords, jump_node)).Select((gps) => new MyJumpGateWaypoint(gps)));
				}

				this.TEMP_CommLinkedGrids.Clear();
				this.TEMP_BeaconLinkedGrids.Clear();
			}
			
			// Fix selected waypoint
			if (jump_gate_valid && MyNetworkInterface.IsServerLike)
			{
				MyJumpGateWaypoint selected_waypoint = this.BlockSettings.SelectedWaypoint();
				Vector3D? waypoint_endpoint = selected_waypoint?.GetEndpoint();

				if (waypoint_endpoint != null)
				{
					Vector3D endpoint = waypoint_endpoint.Value;
					double distance = Vector3D.Distance(endpoint, jump_gate.WorldJumpNode);

					if (distance < jump_gate.JumpGateConfiguration.MinimumJumpDistance || distance > jump_gate.JumpGateConfiguration.MaximumJumpDistance)
					{
						this.BlockSettings.SelectedWaypoint(null);
						this.SetDirty();
					}
				}
			}
			
			// Tick holo display
			if (!MyNetworkInterface.IsDedicatedMultiplayerServer)
			{
				Vector3D table_holo_center = MyJumpGateModSession.LocalVectorToWorldVectorP(this.TerminalBlock.WorldMatrix, new Vector3D(0, (this.IsLargeGrid) ? 0.5 : 1, 0));
				Vector3D table_camera_dir = table_holo_center - MyAPIGateway.Session.Camera.Position;
				double view_distance = Vector3D.Distance(MyAPIGateway.Session.Camera.Position, table_holo_center);

				if (view_distance <= 250d && Vector3D.Angle(MyAPIGateway.Session.Camera.WorldMatrix.Forward, table_camera_dir) < MyJumpGateController.MaxHoloViewAngle)
				{
					jump_gate_valid = jump_gate != null && jump_gate.IsComplete();
					BoundingEllipsoidD jump_ellipse = jump_gate?.JumpEllipse ?? BoundingEllipsoidD.Zero;

					Color aqua = new Color(97, 205, 202);
					Color red = Color.Red;
					Vector4 intense_red = red.ToVector4() * new Vector4(2, 2, 2, 1);
					Vector4 intense_aqua = aqua.ToVector4() * new Vector4(2, 2, 2, 1);

					if (jump_gate_valid && this.LocalGameTick % 30 == 0)
					{
						this.AttachedJumpGateDrives.Clear();
						this.TEMP_JumpGateEntities.Clear();
						this.TEMP_UnconfirmedJumpGateEntities.Clear();
						jump_gate?.GetJumpGateDrives(this.AttachedJumpGateDrives);
						jump_gate?.GetEntitiesInJumpSpace(this.TEMP_JumpGateEntities, true);
						jump_gate?.GetUninitializedEntititesInJumpSpace(this.TEMP_UnconfirmedJumpGateEntities, true);

						double max_distance = (jump_gate_valid) ? Math.Sqrt(this.AttachedJumpGateDrives.Max((drive) => Vector3D.DistanceSquared(drive.WorldMatrix.Translation, jump_ellipse.WorldMatrix.Translation))) : 0;
						this.HoloDisplayScale = 0.75 / max_distance;
						this.HoloDisplayScalar = MatrixD.CreateScale(this.HoloDisplayScale);
					}

					if (jump_gate_valid && this.AttachedJumpGateDrives.Count((drive) => drive.IsWorking()) >= 2)
					{
						MatrixD holo_matrix = jump_ellipse.WorldMatrix;
						holo_matrix.Translation = table_holo_center;
						MatrixD scaled_matrix = this.HoloDisplayScalar * holo_matrix;

						Vector3D player_facing = table_holo_center - MyAPIGateway.Session.Camera.Position;
						Vector3D up = Vector3D.Cross(player_facing, MyAPIGateway.Session.Camera.WorldMatrix.Left);
						MatrixD player_holo_matrix;
						MatrixD.CreateWorld(ref table_holo_center, ref player_facing, ref up, out player_holo_matrix);

						BoundingEllipsoidD draw_ellipse = new BoundingEllipsoidD(new Vector3D(jump_ellipse.Radii.X, jump_ellipse.Radii.Z, jump_ellipse.Radii.Y) * this.HoloDisplayScale, holo_matrix);
						draw_ellipse.WorldMatrix.Forward = holo_matrix.Up;
						draw_ellipse.WorldMatrix.Up = holo_matrix.Forward;
						draw_ellipse.Draw2(aqua, 20, 16, 0.00125f, MyJumpGateModSession.MyMaterialsHolder.WeaponLaser, 10);

						List<IMyCubeGrid> subgrids = new List<IMyCubeGrid>();

						foreach (MyJumpGateDrive drive in this.AttachedJumpGateDrives)
						{
							Color color = (drive.IsWorking()) ? aqua : red;
							MatrixD drive_matrix = this.HoloDisplayScalar * drive.WorldMatrix;
							Vector3D pos = MyJumpGateModSession.WorldVectorToLocalVectorP(ref jump_ellipse.WorldMatrix, drive.WorldMatrix.Translation);
							drive_matrix.Translation = MyJumpGateModSession.LocalVectorToWorldVectorP(ref scaled_matrix, pos);
							BoundingBoxD drive_box = BoundingBoxD.CreateFromSphere(new BoundingSphereD(Vector3D.Zero, (this.IsLargeGrid) ? 10 : 2));
							MySimpleObjectDraw.DrawTransparentBox(ref drive_matrix, ref drive_box, ref color, MySimpleObjectRasterizer.Wireframe, 1, 0.000125f, null, MyJumpGateModSession.MyMaterialsHolder.WeaponLaser, intensity: 10);
						}

						foreach (KeyValuePair<MyEntity, float> pair in this.TEMP_JumpGateEntities)
						{
							MyEntity entity = pair.Key;
							MyJumpGateConstruct construct = MyJumpGateModSession.Instance.GetJumpGateGrid(entity.EntityId);

							if (construct == null)
							{
								Vector3D pos = MyJumpGateModSession.WorldVectorToLocalVectorP(ref jump_ellipse.WorldMatrix, entity.WorldMatrix.Translation);
								pos = MyJumpGateModSession.LocalVectorToWorldVectorP(ref scaled_matrix, pos);
								MyStringId marker = (jump_gate.IsEntityValidForJumpSpace(entity)) ? MyJumpGateModSession.MyMaterialsHolder.EnabledEntityMarker : MyJumpGateModSession.MyMaterialsHolder.DisabledEntityMarker;
								MyTransparentGeometry.AddBillboardOriented(marker, intense_red, pos, player_holo_matrix.Right, player_holo_matrix.Down, 0.05f);
							}
							else
							{
								construct.GetCubeGrids(subgrids);

								foreach (IMyCubeGrid subgrid in subgrids)
								{
									MatrixD subgrid_matrix = this.HoloDisplayScalar * subgrid.WorldMatrix;
									Vector3D pos = MyJumpGateModSession.WorldVectorToLocalVectorP(ref jump_ellipse.WorldMatrix, subgrid.WorldMatrix.Translation);
									subgrid_matrix.Translation = MyJumpGateModSession.LocalVectorToWorldVectorP(ref scaled_matrix, pos);
									BoundingBoxD grid_box = subgrid.LocalAABB;
									MySimpleObjectDraw.DrawTransparentBox(ref subgrid_matrix, ref grid_box, ref red, MySimpleObjectRasterizer.Wireframe, 1, 0.000125f, null, MyJumpGateModSession.MyMaterialsHolder.WeaponLaser, intensity: 10);
								}

								subgrids.Clear();
							}
						}
					}
					else if (jump_gate_valid)
					{
						Vector3D normal = this.WorldMatrix.Up;
						Vector3D player_facing = table_holo_center - MyAPIGateway.Session.Camera.Position;
						player_facing = Vector3D.ProjectOnPlane(ref player_facing, ref normal);
						MatrixD billboard_matrix;
						MatrixD.CreateWorld(ref table_holo_center, ref player_facing, ref normal, out billboard_matrix);
						MyTransparentGeometry.AddBillboardOriented(MyJumpGateModSession.MyMaterialsHolder.GateOfflineControllerIcon, intense_aqua, billboard_matrix.Translation, billboard_matrix.Left, billboard_matrix.Up, 0.75f, 0.75f);
					}
					else
					{
						Vector3D normal = this.WorldMatrix.Up;
						Vector3D player_facing = table_holo_center - MyAPIGateway.Session.Camera.Position;
						player_facing = Vector3D.ProjectOnPlane(ref player_facing, ref normal);
						MatrixD billboard_matrix;
						MatrixD.CreateWorld(ref table_holo_center, ref player_facing, ref normal, out billboard_matrix);
						MyTransparentGeometry.AddBillboardOriented(MyJumpGateModSession.MyMaterialsHolder.GateDisconnectedControllerIcon, intense_red, billboard_matrix.Translation, billboard_matrix.Left, billboard_matrix.Up, 0.75f, 0.75f);
					}
				}
			}
			
			this.CheckSendGlobalUpdate();
		}

		/// <summary>
		/// Serializes block for streaming and saving
		/// </summary>
		/// <returns>false</returns>
		public override bool IsSerialized()
		{
			bool res = base.IsSerialized();
			this.BlockSettings.AcquireLock();
			this.ModStorageComponent[MyJumpGateModSession.BlockComponentDataGUID] = Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(this.BlockSettings));
			this.BlockSettings.ReleaseLock();
			return res;
		}
		#endregion

		#region IMyTextSurfaceProvider Methods
		/// <summary>
		/// Gets a surface by ID
		/// </summary>
		/// <param name="surface_id">The surface ID</param>
		/// <returns>The text surface</returns>
		public Sandbox.ModAPI.Ingame.IMyTextSurface GetSurface(int surface_id)
		{
			return this.MultiPanelComponent.GetSurface(surface_id);
		}
		#endregion

		#region Private Methods
		/// <summary>
		/// Callback triggered when this component recevies a block update request from server
		/// </summary>
		/// <param name="packet">The update request packet</param>
		private void OnNetworkBlockUpdate(MyNetworkInterface.Packet packet)
		{
			if (packet == null || packet.EpochTime <= this.LastUpdateTime) return;
			MySerializedJumpGateController serialized = packet.Payload<MySerializedJumpGateController>();
			if (serialized == null || JumpGateUUID.FromGuid(serialized.UUID).GetBlock() != this.TerminalBlock.EntityId) return;

			if (MyNetworkInterface.IsMultiplayerServer && packet.PhaseFrame == 1)
			{
				if (serialized.IsClientRequest)
				{
					packet.Payload(this.ToSerialized(false));
					packet.Send();
				}
				else if (this.FromSerialized(serialized))
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
				MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet
				{
					PacketType = MyPacketTypeEnum.UPDATE_CONTROLLER,
					TargetID = 0,
					Broadcast = MyNetworkInterface.IsMultiplayerServer,
				};

				packet.Payload(this.ToSerialized(false));
				packet.Send();
				if (MyNetworkInterface.IsMultiplayerServer) this.LastUpdateTime = packet.EpochTime;
				this.IsDirty = false;
			}
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Attaches this controller to the specified jump gate<br />
		/// Jump gate must be null or a jump gate on the same construct as this controller
		/// </summary>
		/// <param name="jump_gate">The jump gate to attach to or null to detach</param>
		public void AttachedJumpGate(MyJumpGate jump_gate)
		{
			MyJumpGate old_gate = this.AttachedJumpGate();
			if (old_gate == jump_gate || (jump_gate != null && this.JumpGateGrid != jump_gate.JumpGateGrid)) return;
			else if (old_gate != null && !old_gate.Closed) old_gate.Controller = null;
			this.BlockSettings.JumpGateID((jump_gate == null || jump_gate.Closed) ? -1 : jump_gate.JumpGateID);
			if (jump_gate == null || jump_gate.Closed) return;
			jump_gate.Controller = this;
			this.BlockSettings.JumpGateName(jump_gate.GetName());
		}

		/// <summary>
		/// Gets the list of waypoints this controller has<br />
		/// This is client dependent
		/// </summary>
		/// <param name="waypoints">The list of waypoints to populate<br />List will not be cleared</param>
		public void GetWaypointsList(List<MyJumpGateWaypoint> waypoints)
		{
			if (waypoints == null) return;
			lock (this.WaypointsListMutex) waypoints.AddRange(this.WaypointsList);
		}

		/// <summary>
		/// Updates this block data from a serialized controller
		/// </summary>
		/// <param name="controller">The serialized controller data</param>
		/// <returns>Whether this block was updated</returns>
		public bool FromSerialized(MySerializedJumpGateController controller)
		{
			if (!base.FromSerialized(controller)) return false;
			if (controller.SerializedPanelInfo != null) this.MultiPanelComponent?.Deserialize(controller.SerializedPanelInfo);
			MyControllerBlockSettingsStruct new_settings = MyAPIGateway.Utilities.SerializeFromBinary<MyControllerBlockSettingsStruct>(Convert.FromBase64String(controller.SerializedBlockSettings));
			bool can_update_gate_settings = this.AttachedJumpGate()?.IsIdle() ?? true;

			if (this.BlockSettings == null)
			{
				this.BlockSettings = new_settings;
				return true;
			}

			this.BlockSettings.AcquireLock();
			this.BlockSettings.CanAutoActivate(new_settings.CanAutoActivate());
			this.BlockSettings.CanBeInbound(new_settings.CanBeInbound());
			this.BlockSettings.CanBeOutbound(new_settings.CanBeOutbound());
			this.BlockSettings.CanAcceptUnowned(new_settings.CanAcceptUnowned());
			this.BlockSettings.CanAcceptEnemy(new_settings.CanAcceptEnemy());
			this.BlockSettings.CanAcceptNeutral(new_settings.CanAcceptNeutral());
			this.BlockSettings.CanAcceptFriendly(new_settings.CanAcceptFriendly());
			this.BlockSettings.CanAcceptOwned(new_settings.CanAcceptOwned());
			this.BlockSettings.JumpGateName(new_settings.JumpGateName());
			this.BlockSettings.AutoActivationDelay(new_settings.AutoActivationDelay());

			KeyValuePair<float, float> mass = new_settings.AutoActivateMass();
			this.BlockSettings.AutoActivateMass(mass.Key, mass.Value);

			KeyValuePair<double, double> power = new_settings.AutoActivatePower();
			this.BlockSettings.AutoActivatePower(power.Key, power.Value);

			mass = new_settings.AllowedEntityMass();
			this.BlockSettings.AllowedEntityMass(mass.Key, mass.Value);

			KeyValuePair<uint, uint> size = new_settings.AllowedCubeGridSize();
			this.BlockSettings.AllowedCubeGridSize(size.Key, size.Value);
			
			if (can_update_gate_settings)
			{
				this.BlockSettings.SelectedWaypoint(new_settings.SelectedWaypoint());
				this.BlockSettings.JumpSpaceRadius(new_settings.JumpSpaceRadius());
				this.BlockSettings.JumpGateID(new_settings.JumpGateID());
				this.BlockSettings.JumpEffectAnimationName(new_settings.JumpEffectAnimationName());
				this.BlockSettings.VectorNormalOverride(new_settings.VectorNormalOverride());
				this.BlockSettings.JumpEffectAnimationColorShift(new_settings.JumpEffectAnimationColorShift());
				this.BlockSettings.JumpSpaceDepthPercent(new_settings.JumpSpaceDepthPercent());
			}

			this.BlockSettings.ReleaseLock();
			return true;
		}

		/// <summary>
		/// </summary>
		/// <param name="steam_id">The player's steam ID to check</param>
		/// <returns>True if the caller's faction can jump to this gate</returns>
		public bool IsFactionRelationValid(ulong steam_id)
		{
			long player_id = MyAPIGateway.Players.TryGetIdentityId(steam_id);
			if (player_id == 0) return false;
			MyRelationsBetweenPlayerAndBlock relation = this.TerminalBlock?.GetUserRelationToOwner(player_id) ?? MyJumpGateModSession.GetPlayerByID(this.OwnerID)?.GetRelationTo(player_id) ?? MyRelationsBetweenPlayerAndBlock.NoOwnership;

			switch (relation)
			{
				case MyRelationsBetweenPlayerAndBlock.NoOwnership:
					return this.BlockSettings.CanAcceptUnowned();
				case MyRelationsBetweenPlayerAndBlock.Neutral:
					return this.BlockSettings.CanAcceptNeutral();
				case MyRelationsBetweenPlayerAndBlock.Friends:
					return this.BlockSettings.CanAcceptFriendly();
				case MyRelationsBetweenPlayerAndBlock.Enemies:
					return this.BlockSettings.CanAcceptEnemy();
				case MyRelationsBetweenPlayerAndBlock.Owner:
				case MyRelationsBetweenPlayerAndBlock.FactionShare:
					return this.BlockSettings.CanAcceptOwned();
				default:
					return false;
			}
		}

		/// <summary>
		/// </summary>
		/// <param name="steam_id">The player's identiy to check</param>
		/// <returns>True if the caller's faction can jump to this gate</returns>
		public bool IsFactionRelationValid(long player_identity)
		{
			MyRelationsBetweenPlayerAndBlock relation = this.TerminalBlock?.GetUserRelationToOwner(player_identity) ?? MyJumpGateModSession.GetPlayerByID(this.OwnerID)?.GetRelationTo(player_identity) ?? MyRelationsBetweenPlayerAndBlock.NoOwnership;

			switch (relation)
			{
				case MyRelationsBetweenPlayerAndBlock.NoOwnership:
					return this.BlockSettings.CanAcceptUnowned();
				case MyRelationsBetweenPlayerAndBlock.Neutral:
					return this.BlockSettings.CanAcceptNeutral();
				case MyRelationsBetweenPlayerAndBlock.Friends:
					return this.BlockSettings.CanAcceptFriendly();
				case MyRelationsBetweenPlayerAndBlock.Enemies:
					return this.BlockSettings.CanAcceptEnemy();
				case MyRelationsBetweenPlayerAndBlock.Owner:
				case MyRelationsBetweenPlayerAndBlock.FactionShare:
					return this.BlockSettings.CanAcceptOwned();
				default:
					return false;
			}
		}

		/// <summary>
		/// Serializes this block's data
		/// </summary>
		/// <param name="as_client_request">If true, this will be an update request to server</param>
		/// <returns>The serialized controller data</returns>
		public new MySerializedJumpGateController ToSerialized(bool as_client_request)
		{
			if (as_client_request) return base.ToSerialized<MySerializedJumpGateController>(true);
			MySerializedJumpGateController serialized = base.ToSerialized<MySerializedJumpGateController>(false);
			serialized.SerializedBlockSettings = Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(this.BlockSettings));
			serialized.SerializedPanelInfo = this.MultiPanelComponent?.Serialize(true);
			return serialized;
		}

		/// <summary>
		/// Gets the jump gate this controller is attached to
		/// </summary>
		/// <returns>The attached gate or null if none attached</returns>
		public MyJumpGate AttachedJumpGate()
		{
			long gate_id = this.BlockSettings?.JumpGateID() ?? -1;
			if (gate_id < 0 || this.JumpGateGrid == null || this.JumpGateGrid.Closed) return null;
			return this.JumpGateGrid.GetJumpGate(gate_id);
		}
		#endregion
	}

	/// <summary>
	/// Class for holding serialized JumpGateCapacitor data
	/// </summary>
	[ProtoContract]
	internal class MySerializedJumpGateController : MySerializedCubeBlockBase
	{
		/// <summary>
		/// The serialized block settings as a base64 string
		/// </summary>
		[ProtoMember(20)]
		public string SerializedBlockSettings;

		/// <summary>
		/// The serialized text panel component info
		/// </summary>
		[ProtoMember(21)]
		public MyObjectBuilder_ComponentBase SerializedPanelInfo;
	}
}
