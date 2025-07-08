using IOTA.ModularJumpGates.Terminal;
using IOTA.ModularJumpGates.Util;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Components;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using VRage;
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
			public byte RemoteAntennaChannel_V = 0xFF;
			[ProtoMember(6)]
			private MyFactionDisplayType FactionDisplayType_V = MyFactionDisplayType.FRIENDLY | MyFactionDisplayType.OWNED;
			[ProtoMember(7)]
			private double JumpSpaceRadius_V = 250d;
			[ProtoMember(8)]
			private double JumpSpaceDepthPercent_V = 0.2d;
			[ProtoMember(9)]
			private long JumpGateID_V = -1;
			[ProtoMember(10)]
			private long RemoteAntennaID_V = -1;
			[ProtoMember(11)]
			private string JumpEffectName_V = "standard";
			[ProtoMember(12)]
			private string JumpGateName_V = null;
			[ProtoMember(13)]
			private Vector3D VectorNormal_V = Vector3D.Zero;
			[ProtoMember(14)]
			private Color EffectColorShift_V = Color.White;
			[ProtoMember(15)]
			private MyJumpGateWaypoint SelectedWaypoint_V = null;
			[ProtoMember(16)]
			private List<long> BlacklistedEntities = new List<long>();
			[ProtoMember(17)]
			private float MinimumEntityMass_V = 0;
			[ProtoMember(18)]
			private float MaximumEntityMass_V = float.PositiveInfinity;
			[ProtoMember(19)]
			private uint MinimumCubeGridSize_V = 0;
			[ProtoMember(20)]
			private uint MaximumCubeGridSize_V = uint.MaxValue;
			[ProtoMember(21)]
			private float MinimumAutoMass_V = 0;
			[ProtoMember(22)]
			private float MaximumAutoMass_V = float.PositiveInfinity;
			[ProtoMember(23)]
			private double MinimumAutoPower_V = 0;
			[ProtoMember(24)]
			private double MaximumAutoPower_V = double.PositiveInfinity;
			[ProtoMember(25)]
			private float AutoActivationDelay_V = 0;
			[ProtoMember(26)]
			private MyJumpSpaceFitType JumpSpaceFitType_V = MyJumpSpaceFitType.INNER;

			private readonly object WriterLock = new object();

			public MyControllerBlockSettingsStruct() { }
			public MyControllerBlockSettingsStruct(MyJumpGateController controller, Dictionary<string, object> mapping)
			{
				this.FromDictionary(controller?.AttachedJumpGate(), mapping);
			}

			public void AcquireLock()
			{
				Monitor.Enter(this.WriterLock);
			}
			public void ReleaseLock()
			{
				if (Monitor.IsEntered(this.WriterLock)) Monitor.Exit(this.WriterLock);
			}

			public void FromDictionary(MyJumpGate attached_gate, Dictionary<string, object> mapping)
			{
				if (mapping == null || attached_gate == null) return;

				lock (this.WriterLock)
				{
					this.CanAutoActivate_V = (bool) mapping.GetValueOrDefault("CanAutoActivate", this.CanAutoActivate_V);
					this.CanBeInbound_V = (bool) mapping.GetValueOrDefault("CanBeInbound", this.CanBeInbound_V);
					this.CanBeOutbound_V = (bool) mapping.GetValueOrDefault("CanBeOutbound", this.CanBeOutbound_V);
					this.HasVectorNormalOverride_V = (bool) mapping.GetValueOrDefault("HasVectorNormalOverride", this.HasVectorNormalOverride_V);
					this.FactionDisplayType_V = (MyFactionDisplayType) (byte) mapping.GetValueOrDefault("FactionDisplayType", (byte) this.FactionDisplayType_V);
					this.JumpSpaceRadius_V = (double) mapping.GetValueOrDefault("JumpSpaceRadius", this.JumpSpaceRadius_V);
					this.JumpSpaceDepthPercent_V = (double) mapping.GetValueOrDefault("JumpSpaceDepthPercent", this.JumpSpaceDepthPercent_V);
					this.JumpSpaceFitType_V = (MyJumpSpaceFitType) (byte) mapping.GetValueOrDefault("JumpSpaceFitType", (byte) this.JumpSpaceFitType_V);
					this.JumpEffectName_V = (string) mapping.GetValueOrDefault("JumpEffectName", this.JumpEffectName_V);
					string name = (string) mapping.GetValueOrDefault("JumpGateName", this.JumpGateName_V);
					this.VectorNormal_V = (Vector3D) mapping.GetValueOrDefault("VectorNormal", this.VectorNormal_V);
					this.EffectColorShift_V = (Color) mapping.GetValueOrDefault("EffectColorShift", this.EffectColorShift_V);
					this.JumpGateName_V = (name != null && (name.StartsWith("#") || name.Contains(';'))) ? this.JumpGateName_V : name;
					this.RemoteAntennaChannel_V = (byte) mapping.GetValueOrDefault("RemoteAntennaChannel", this.RemoteAntennaChannel_V);

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
						else if (selected_waypoint is long[])
						{
							MyJumpGate target_gate = MyJumpGateModSession.Instance.GetJumpGate(new JumpGateUUID((long[]) selected_waypoint));
							if (target_gate == null) throw new InvalidGuuidException("The specified jump gate does not exist");
							result_waypoint = new MyJumpGateWaypoint(target_gate);
						}

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

					return new Dictionary<string, object>() {
						["CanAutoActivate"] = this.CanAutoActivate_V,
						["CanBeInbound"] = this.CanBeInbound_V,
						["CanBeOutbound"] = this.CanBeOutbound_V,
						["HasVectorNormalOverride"] = this.HasVectorNormalOverride_V,
						["RemoteAntennaChannel"] = this.RemoteAntennaChannel_V,
						["FactionDisplayType"] = (byte) this.FactionDisplayType_V,
						["JumpSpaceRadius"] = this.JumpSpaceRadius_V,
						["JumpSpaceDepthPercent"] = this.JumpSpaceDepthPercent_V,
						["JumpSpaceFitType"] = (byte) this.JumpSpaceFitType_V,
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
			public void JumpSpaceFitType(MyJumpSpaceFitType value)
			{
				lock (this.WriterLock) this.JumpSpaceFitType_V = value;
			}
			public void HasVectorNormalOverride(bool flag)
			{
				lock (this.WriterLock) this.HasVectorNormalOverride_V = flag;
			}
			public void RemoteAntennaChannel(byte channel)
			{
				lock (this.WriterLock) this.RemoteAntennaChannel_V = (channel >= MyJumpGateRemoteAntenna.ChannelCount) ? (byte) 0xFF : channel;
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
			public void RemoteAntennaID(long id)
			{
				id = Math.Max(id, -1);
				lock (this.WriterLock) this.RemoteAntennaID_V = id;
			}
			public void JumpEffectAnimationName(string name)
			{
				lock (this.WriterLock) this.JumpEffectName_V = (name == null || name.Length == 0) ? "standard" : name;
			}
			public void JumpGateName(string name)
			{
				if (name != null && (name.StartsWith("#") || name.Contains(';'))) return;
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
			public void AllowedEntityMass(KeyValuePair<float?, float?> mass_kg)
			{
				this.AllowedEntityMass(mass_kg.Key, mass_kg.Value);
			}
			public void AllowedEntityMass(KeyValuePair<float, float> mass_kg)
			{
				this.AllowedEntityMass(mass_kg.Key, mass_kg.Value);
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
			public void AllowedCubeGridSize(KeyValuePair<uint?, uint?> size)
			{
				this.AllowedCubeGridSize(size.Key, size.Value);
			}
			public void AllowedCubeGridSize(KeyValuePair<uint, uint> size)
			{
				this.AllowedCubeGridSize(size.Key, size.Value);
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
			public void AutoActivateMass(KeyValuePair<float?, float?> mass_kg)
			{
				this.AutoActivateMass(mass_kg.Key, mass_kg.Value);
			}
			public void AutoActivateMass(KeyValuePair<float, float> mass_kg)
			{
				this.AutoActivateMass(mass_kg.Key, mass_kg.Value);
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
			public void AutoActivatePower(KeyValuePair<double?, double?> mass_kg)
			{
				this.AutoActivatePower(mass_kg.Key, mass_kg.Value);
			}
			public void AutoActivatePower(KeyValuePair<double, double> mass_kg)
			{
				this.AutoActivatePower(mass_kg.Key, mass_kg.Value);
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
			public MyJumpSpaceFitType JumpSpaceFitType()
			{
				return this.JumpSpaceFitType_V;
			}
			public bool HasVectorNormalOverride()
			{
				return this.HasVectorNormalOverride_V;
			}
			public byte RemoteAntennaChannel()
			{
				return this.RemoteAntennaChannel_V;
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
			public long RemoteAntennaID()
			{
				return Math.Max(this.RemoteAntennaID_V, -1);
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

		/// <summary>
		/// The block data for this block accounting for remote antennas
		/// </summary>
		private MyControllerBlockSettingsStruct OverlayedBlockSettings = null;
		#endregion

		#region Temporary Collections
		/// <summary>
		/// Temporary list of construct grids
		/// </summary>
		private List<IMyCubeGrid> TEMP_DetailedInfoConstructsList = new List<IMyCubeGrid>();

		/// <summary>
		/// Temporary list of jump gate drives for holo display
		/// </summary>
		private Dictionary<MyJumpGateDrive, KeyValuePair<MatrixD, BoundingBoxD>> TEMP_DriveHoloBoxes = new Dictionary<MyJumpGateDrive, KeyValuePair<MatrixD, BoundingBoxD>>();
		#endregion

		#region Public Variables
		/// <summary>
		/// IMyTextSurfaceProvider Property
		/// </summary>
		public bool UseGenericLcd => this.MultiPanelComponent.UseGenericLcd;

		/// <summary>
		/// IMyTextSurfaceProvider Property
		/// </summary>
		public int SurfaceCount => this.MultiPanelComponent.SurfaceCount;

		/// <summary>
		/// The block data for this block
		/// </summary>
		public MyControllerBlockSettingsStruct BaseBlockSettings {get; private set; }

		/// <summary>
		/// The block data for this block<br />
		/// This is adjusted for remote antennas
		/// </summary>
		public MyControllerBlockSettingsStruct BlockSettings
		{
			get
			{
				if (this.BaseBlockSettings == null) return null;
				KeyValuePair<MyJumpGateRemoteAntenna, byte> pair = this.RemoteAntennaChannel;
				MyJumpGateRemoteAntenna antenna = pair.Key?.GetConnectedRemoteAntenna(pair.Value);
				if (antenna == null) return this.BaseBlockSettings;
				this.OverlayedBlockSettings = this.OverlayedBlockSettings ?? new MyControllerBlockSettingsStruct();
				MyAllowedRemoteSettings allowed = antenna.BlockSettings.AllowedRemoteSettings;
				MyControllerBlockSettingsStruct remote = antenna.BlockSettings.BaseControllerSettings[pair.Value];

				// Overlay settings
				this.OverlayedBlockSettings.JumpGateName(((allowed & MyAllowedRemoteSettings.NAME) != 0) ? this.BaseBlockSettings.JumpGateName() : antenna.GetJumpGateName(pair.Value));
				this.OverlayedBlockSettings.SelectedWaypoint(((allowed & MyAllowedRemoteSettings.DESTINATIONS) != 0) ? this.BaseBlockSettings.SelectedWaypoint() : remote.SelectedWaypoint());
				this.OverlayedBlockSettings.JumpEffectAnimationName(((allowed & MyAllowedRemoteSettings.ANIMATIONS) != 0) ? this.BaseBlockSettings.JumpEffectAnimationName() : remote.JumpEffectAnimationName());
				this.OverlayedBlockSettings.CanBeInbound(((allowed & MyAllowedRemoteSettings.ROUTING) != 0) ? this.BaseBlockSettings.CanBeInbound() : remote.CanBeInbound());
				this.OverlayedBlockSettings.CanBeOutbound(((allowed & MyAllowedRemoteSettings.ROUTING) != 0) ? this.BaseBlockSettings.CanBeOutbound() : remote.CanBeOutbound());
				this.OverlayedBlockSettings.SetBlacklistedEntities(((allowed & MyAllowedRemoteSettings.ENTITY_FILTER) != 0) ? this.BaseBlockSettings.GetBlacklistedEntities() : remote.GetBlacklistedEntities());
				this.OverlayedBlockSettings.AllowedEntityMass(((allowed & MyAllowedRemoteSettings.ENTITY_FILTER) != 0) ? this.BaseBlockSettings.AllowedEntityMass() : remote.AllowedEntityMass());
				this.OverlayedBlockSettings.AllowedCubeGridSize(((allowed & MyAllowedRemoteSettings.ENTITY_FILTER) != 0) ? this.BaseBlockSettings.AllowedCubeGridSize() : remote.AllowedCubeGridSize());
				this.OverlayedBlockSettings.AutoActivateMass(((allowed & MyAllowedRemoteSettings.AUTO_ACTIVATE) != 0) ? this.BaseBlockSettings.AutoActivateMass() : remote.AutoActivateMass());
				this.OverlayedBlockSettings.AutoActivatePower(((allowed & MyAllowedRemoteSettings.AUTO_ACTIVATE) != 0) ? this.BaseBlockSettings.AutoActivatePower() : remote.AutoActivatePower());
				this.OverlayedBlockSettings.AutoActivationDelay(((allowed & MyAllowedRemoteSettings.AUTO_ACTIVATE) != 0) ? this.BaseBlockSettings.AutoActivationDelay() : remote.AutoActivationDelay());
				this.OverlayedBlockSettings.CanAccept(((allowed & MyAllowedRemoteSettings.COMM_LINKAGE) != 0) ? this.BaseBlockSettings.CanAccept() : remote.CanAccept());
				this.OverlayedBlockSettings.JumpSpaceFitType(((allowed & MyAllowedRemoteSettings.JUMPSPACE) != 0) ? this.BaseBlockSettings.JumpSpaceFitType() : remote.JumpSpaceFitType());
				this.OverlayedBlockSettings.JumpSpaceDepthPercent(((allowed & MyAllowedRemoteSettings.JUMPSPACE) != 0) ? this.BaseBlockSettings.JumpSpaceDepthPercent() : remote.JumpSpaceDepthPercent());
				this.OverlayedBlockSettings.JumpSpaceRadius(((allowed & MyAllowedRemoteSettings.JUMPSPACE) != 0) ? this.BaseBlockSettings.JumpSpaceRadius() : remote.JumpSpaceRadius());
				this.OverlayedBlockSettings.JumpEffectAnimationColorShift(((allowed & MyAllowedRemoteSettings.COLOR_OVERRIDE) != 0) ? this.BaseBlockSettings.JumpEffectAnimationColorShift() : remote.JumpEffectAnimationColorShift());
				this.OverlayedBlockSettings.VectorNormalOverride(((allowed & MyAllowedRemoteSettings.VECTOR_OVERRIDE) != 0) ? this.BaseBlockSettings.VectorNormalOverride() : remote.VectorNormalOverride());
				this.OverlayedBlockSettings.HasVectorNormalOverride(((allowed & MyAllowedRemoteSettings.VECTOR_OVERRIDE) != 0) ? this.BaseBlockSettings.HasVectorNormalOverride() : remote.HasVectorNormalOverride());

				return this.OverlayedBlockSettings;
			}

			private set
			{
				this.BaseBlockSettings = value;
			}
		}

		/// <summary>
		/// The antenna and channel this controller is searching for jump gates through
		/// </summary>
		public KeyValuePair<MyJumpGateRemoteAntenna, byte> RemoteAntennaChannel
		{
			get
			{
				if (this.BaseBlockSettings == null) return new KeyValuePair<MyJumpGateRemoteAntenna, byte>(null, 0xFF);
				long antenna_id = this.BaseBlockSettings.RemoteAntennaID();
				if (antenna_id == -1) return new KeyValuePair<MyJumpGateRemoteAntenna, byte>(null, 0xFF);
				MyJumpGateRemoteAntenna antenna = this.JumpGateGrid?.GetRemoteAntenna(antenna_id);
				byte channel = this.BaseBlockSettings.RemoteAntennaChannel();
				return new KeyValuePair<MyJumpGateRemoteAntenna, byte>((channel == 0xFF) ? null : antenna, channel);
			}
		}

		/// <summary>
		/// The antenna this controller is searching for jump gates through
		/// </summary>
		public MyJumpGateRemoteAntenna RemoteAntenna
		{
			get
			{
				KeyValuePair<MyJumpGateRemoteAntenna, byte> antenna = this.RemoteAntennaChannel;
				return (antenna.Value == 0xFF) ? null : antenna.Key;
			}
		}

		/// <summary>
		/// The antenna this controller's antenna is connected to or null
		/// </summary>
		public MyJumpGateRemoteAntenna ConnectedRemoteAntenna
		{
			get
			{
				KeyValuePair<MyJumpGateRemoteAntenna, byte> remote_antenna = this.RemoteAntennaChannel;
				return remote_antenna.Key?.GetConnectedRemoteAntenna(remote_antenna.Value);
			}
		}
		#endregion

		#region Constructors
		public MyJumpGateController() : base() { }

		/// <summary>
		/// Creates a new null wrapper
		/// </summary>
		/// <param name="serialized">The serialized block data</param>
		/// <param name="parent">The containing grid or null to calculate</param>
		public MyJumpGateController(MySerializedJumpGateController serialized, MyJumpGateConstruct parent = null) : base(serialized, parent)
		{
			this.FromSerialized(serialized, parent);
		}
		#endregion

		#region "CubeBlockBase" Methods
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
				double total_available_power_mw = jump_gate?.CalculateTotalAvailableInstantPower() ?? double.NaN;
				int reachable_grids = (MyJumpGateModSession.Configuration.ConstructConfiguration.RequireGridCommLink) ? (this_grid?.GetCommLinkedJumpGateGrids().Count() ?? 0) : MyJumpGateModSession.Instance.GetAllJumpGateGrids().Count();

				sb.Append($"\n-=-=-=( {MyTexts.GetString("DisplayName_CubeBlock_JumpGateController")} )=-=-=-\n");
				sb.Append($" {MyTexts.GetString("DetailedInfo_BlockBase_Input")}: {input_wattage} MW\n");
				sb.Append($" {MyTexts.GetString("DetailedInfo_BlockBase_RequiredInput")}: {required_input} MW\n");
				sb.Append($" {MyTexts.GetString("DetailedInfo_BlockBase_InputRatio")}: {Math.Round(MathHelperD.Clamp(input_wattage / required_input, 0, 1) * 100, 2):#.00}%\n");
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

				if (MyJumpGateModSession.DebugMode)
				{
					sb.Append($"\n--- {MyTexts.GetString("DetailedInfo_JumpGateController_HeaderDebugInfo")} ---[color=#FFCCCCCC]\n");

					sb.Append($"\n :{MyTexts.GetString("DetailedInfo_JumpGateController_HeaderConstructInfo")}:\n");
					sb.Append($" ... {MyTexts.GetString("DetailedInfo_JumpGateController_ThisGridId")}: {(this.TerminalBlock?.CubeGrid?.EntityId.ToString() ?? "N/A")}\n");
					sb.Append($" ... {MyTexts.GetString("DetailedInfo_JumpGateController_IsCurrentGridMain")}: {((this_grid == null) ? "N/A" : (this.TerminalBlock.CubeGrid.EntityId == this_grid.CubeGridID).ToString())}\n");
					sb.Append($" ... {MyTexts.GetString("DetailedInfo_JumpGateController_GateColliderStatus")}: {((jump_gate == null) ? "N/A" : MyTexts.GetString($"ColliderStatusText_{jump_gate.JumpSpaceColliderStatus()}"))}\n");
					sb.Append($" ... {MyTexts.GetString("DetailedInfo_JumpGateController_MarkedForGateUpdate")}: {((this_grid == null) ? "N/A" : this_grid.MarkUpdateJumpGates.ToString())}\n");
					sb.Append($"\n <<< {MyTexts.GetString("DetailedInfo_JumpGateController_UpdateTimes")} >>>\n");
					sb.Append($" ... {MyTexts.GetString("DetailedInfo_JumpGateController_AverageUpdateTime")}: {((this_grid == null) ? "N/A" : Math.Round(this_grid.AverageUpdateTime60(), 4).ToString())}ms\n");
					sb.Append($" ... {MyTexts.GetString("DetailedInfo_JumpGateController_LongestUpdateTime")}: {((this_grid == null) ? "N/A" : Math.Round(this_grid.LongestUpdateTime, 4).ToString())}ms\n");
					sb.Append($" ... {MyTexts.GetString("DetailedInfo_JumpGateController_LongestLocalUpdateTime")}: {((this_grid == null) ? "N/A" : Math.Round(this_grid.LocalLongestUpdateTime60(), 4).ToString())}ms\n");

					sb.Append($"\n :{MyTexts.GetString("DetailedInfo_JumpGateController_HeaderSessionInfo")}:\n");
					sb.Append($" ... {MyTexts.GetString("DetailedInfo_JumpGateController_SessionEntitiesLoaded")}: {(MyNetworkInterface.IsServerLike ? MyJumpGateModSession.Instance.AllSessionEntitiesLoaded.ToString() : "N/A")}\n");
					sb.Append($" ... {MyTexts.GetString("DetailedInfo_JumpGateController_SessionEntitiesLoadTime")}: {(MyNetworkInterface.IsServerLike ? Math.Round((MyJumpGateModSession.Instance.EntityLoadedDelayTicks / 60d), 2).ToString() : "N/A")}\n");
					sb.Append($"\n <<< {MyTexts.GetString("DetailedInfo_JumpGateController_GridUpdateTimes")} >>>\n");
					sb.Append($" ... {MyTexts.GetString("DetailedInfo_JumpGateController_AverageUpdateTime")}: {Math.Round(MyJumpGateModSession.Instance.AverageGridUpdateTime60(), 4)}ms\n");
					sb.Append($" ... {MyTexts.GetString("DetailedInfo_JumpGateController_LongestLocalUpdateTime")}: {Math.Round(MyJumpGateModSession.Instance.LocalLongestGridUpdateTime60(), 4)}ms\n");
					sb.Append($"\n <<< {MyTexts.GetString("DetailedInfo_JumpGateController_SessionUpdateTimes")} >>>\n");
					sb.Append($" ... {MyTexts.GetString("DetailedInfo_JumpGateController_AverageUpdateTime")}: {Math.Round(MyJumpGateModSession.Instance.AverageSessionUpdateTime60(), 4)}ms\n");
					sb.Append($" ... {MyTexts.GetString("DetailedInfo_JumpGateController_LongestLocalUpdateTime")}: {Math.Round(MyJumpGateModSession.Instance.LocalLongestSessionUpdateTime60(), 4)}ms\n");

					sb.Append($"\n :{MyTexts.GetString("DetailedInfo_JumpGateController_HeaderMPUpdateInfo")}:\n");
					sb.Append($" ... {MyTexts.GetString("DetailedInfo_JumpGateController_LastBlockUpdate")}: {this.LastUpdateDateTimeUTC.ToLocalTime().ToLongTimeString()}\n");
					sb.Append($" ... {MyTexts.GetString("DetailedInfo_JumpGateController_LastGateUpdate")}: {(jump_gate?.LastUpdateDateTimeUTC.ToLocalTime().ToLongTimeString() ?? "N/A")}\n");
					sb.Append($" ... {MyTexts.GetString("DetailedInfo_JumpGateController_LastGridUpdate")}: {this_grid.LastUpdateDateTimeUTC.ToLocalTime().ToLongTimeString()}[/color]\n");
				}
			}
		}

		protected override void Clean()
		{
			base.Clean();
			lock (this.WaypointsListMutex) this.WaypointsList.Clear();
			this.AttachedJumpGateDrives.Clear();

			this.TEMP_DetailedInfoConstructsList.Clear();
			this.TEMP_DriveHoloBoxes.Clear();

			this.WaypointsList = null;
			this.WaypointsListMutex = null;
			this.AttachedJumpGateDrives = null;
			this.MultiPanelComponent = null;
			this.TEMP_DetailedInfoConstructsList = null;
			this.TEMP_DriveHoloBoxes = null;
			this.BaseBlockSettings = null;
		}

		protected override void UpdateOnceAfterInit()
		{
			base.UpdateOnceAfterInit();
			KeyValuePair<MyJumpGateRemoteAntenna, byte> remote_antenna = this.RemoteAntennaChannel;
			if (remote_antenna.Key != null && remote_antenna.Value != 0xFF) remote_antenna.Key.SetControllerForOutboundControl(remote_antenna.Value, this);
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
					this.BaseBlockSettings = MyAPIGateway.Utilities.SerializeFromBinary<MyControllerBlockSettingsStruct>(Convert.FromBase64String(blockdata));
				}
				catch (Exception e)
				{
					this.BaseBlockSettings = new MyControllerBlockSettingsStruct();
					Logger.Error($"Failed to load block data: {this.GetType().Name}-{this.TerminalBlock.CustomName}\n{e}");
				}
			}
			else
			{
				this.BaseBlockSettings = new MyControllerBlockSettingsStruct();
				this.ModStorageComponent.Add(MyJumpGateModSession.BlockComponentDataGUID, "");
			}

			if (MyJumpGateModSession.Network.Registered && MyNetworkInterface.IsStandaloneMultiplayerClient)
			{
				MyNetworkInterface.Packet request = new MyNetworkInterface.Packet {
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
			if (this.BaseBlockSettings == null) return;

			if (!this.TerminalBlock.InScene)
			{
				byte[] serialized = MyAPIGateway.Utilities.SerializeToBinary(this.ToSerialized(false));
				string cached = Convert.ToBase64String(serialized);
			}

			if (this.BaseBlockSettings.JumpGateID() != -1 && this.JumpGateGrid != null && !this.JumpGateGrid.Closed)
			{
				MyJumpGate jump_gate = this.JumpGateGrid.GetJumpGate(this.BaseBlockSettings.JumpGateID());
				if (jump_gate != null) jump_gate.Controller = null;
			}

			if (MyJumpGateModSession.Network.Registered) MyJumpGateModSession.Network.Off(MyPacketTypeEnum.UPDATE_CAPACITOR, this.OnNetworkBlockUpdate);
			this.BaseBlockSettings.JumpGateID(-1);
			this.BaseBlockSettings.SelectedWaypoint(null);
		}

		public override void UpdateOnceBeforeFrame()
		{
			base.UpdateOnceBeforeFrame();
			if (!MyJumpGateControllerTerminal.IsLoaded) MyJumpGateControllerTerminal.Load(this.ModContext);
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
			else if (working = this.IsWorking) this.TerminalBlock.SetEmissiveParts("Emissive0", Color.Lime, 1);
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

			// Update antenna
			{
				MyJumpGateRemoteAntenna antenna = this.RemoteAntenna;

				if (antenna != null && (antenna.JumpGateGrid?.MarkClosed ?? true))
				{
					this.BaseBlockSettings.RemoteAntennaChannel(0xFF);
					this.BaseBlockSettings.RemoteAntennaID(-1);
				}
			}

			// Update waypoints
			if (jump_gate_valid && !MyNetworkInterface.IsDedicatedMultiplayerServer && MyAPIGateway.Gui.GetCurrentScreen == MyTerminalPageEnum.ControlPanel && MyJumpGateModSession.GameTick % 60 == 0)
			{
				long player_identity = MyAPIGateway.Players.TryGetIdentityId(MyAPIGateway.Multiplayer.MyId);
				double distance;
				IEnumerable<MyJumpGateConstruct> reachable_grids = (MyJumpGateModSession.Configuration.ConstructConfiguration.RequireGridCommLink) ? this.JumpGateGrid.GetCommLinkedJumpGateGrids() : MyJumpGateModSession.Instance.GetAllJumpGateGrids();
				Vector3D jump_node = jump_gate.WorldJumpNode;
				lock (this.WaypointsListMutex) this.WaypointsList.Clear();

				if (jump_gate.ServerAntenna != null)
				{

				}

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
						lock (this.WaypointsListMutex) if (!this.WaypointsList.Contains(waypoint)) this.WaypointsList.Add(waypoint);
					}

					foreach (MyJumpGateRemoteAntenna antenna in connected_grid.GetAttachedJumpGateRemoteAntennas())
					{
						for (byte channel = 0; channel < MyJumpGateRemoteAntenna.ChannelCount; ++channel)
						{
							MyJumpGate other_gate = antenna.GetInboundControlGate(channel);
							if (other_gate == null || other_gate.MarkClosed) continue;
							distance = Vector3D.Distance(jump_node, other_gate.WorldJumpNode);
							if (distance < jump_gate.JumpGateConfiguration.MinimumJumpDistance || distance > jump_gate.JumpGateConfiguration.MaximumJumpDistance || !antenna.IsFactionRelationValid(channel, player_identity)) continue;
							MyJumpGateWaypoint waypoint = new MyJumpGateWaypoint(other_gate);
							lock (this.WaypointsListMutex) if (!this.WaypointsList.Contains(waypoint)) this.WaypointsList.Add(waypoint);
						}
					}
				}

				lock (this.WaypointsListMutex)
				{
					this.WaypointsList.AddRange(this.JumpGateGrid.GetBeaconsWithinReverseBroadcastSphere().Where((beacon) => (distance = Vector3D.Distance(jump_node, beacon.BeaconPosition)) >= jump_gate.JumpGateConfiguration.MinimumJumpDistance && distance <= jump_gate.JumpGateConfiguration.MaximumJumpDistance).OrderBy((beacon) => Vector3D.Distance(beacon.BeaconPosition, jump_node)).Select((beacon) => new MyJumpGateWaypoint(beacon)));
					this.WaypointsList.AddRange(MyAPIGateway.Session.GPS.GetGpsList(player_identity).Where((gps) => gps.Coords.IsValid()).OrderBy((gps) => Vector3D.Distance(gps.Coords, jump_node)).Select((gps) => new MyJumpGateWaypoint(gps)));
				}
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
						this.BaseBlockSettings.SelectedWaypoint(null);
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
					jump_gate_valid = jump_gate != null && jump_gate.IsControlled();
					BoundingEllipsoidD jump_ellipse = jump_gate?.GetEffectiveJumpEllipse() ?? BoundingEllipsoidD.Zero;

					Color aqua = new Color(97, 205, 202);
					Color red = Color.Red;
					Vector4 intense_red = red.ToVector4() * new Vector4(2, 2, 2, 1);
					Vector4 intense_aqua = aqua.ToVector4() * new Vector4(2, 2, 2, 1);

					if (jump_gate_valid && this.LocalGameTick % 30 == 0)
					{
						this.AttachedJumpGateDrives.Clear();
						this.AttachedJumpGateDrives.AddRange(jump_gate.GetJumpGateDrives());

						if (this.AttachedJumpGateDrives.Count > 0)
						{
							double max_distance = (jump_gate_valid) ? Math.Sqrt(this.AttachedJumpGateDrives.Max((drive) => Vector3D.DistanceSquared(drive.WorldMatrix.Translation, jump_ellipse.WorldMatrix.Translation))) : 0;
							this.HoloDisplayScale = 0.75 / max_distance;
							this.HoloDisplayScalar = MatrixD.CreateScale(this.HoloDisplayScale);
						}
					}

					if (jump_gate_valid && this.AttachedJumpGateDrives.Count((drive) => drive.IsWorking) >= 2)
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

						foreach (MyJumpGateDrive drive in this.AttachedJumpGateDrives)
						{
							Color color = (drive.IsWorking) ? aqua : red;
							MatrixD drive_matrix = this.HoloDisplayScalar * drive.WorldMatrix;
							Vector3D pos = MyJumpGateModSession.WorldVectorToLocalVectorP(ref jump_ellipse.WorldMatrix, drive.WorldMatrix.Translation);
							drive_matrix.Translation = MyJumpGateModSession.LocalVectorToWorldVectorP(ref scaled_matrix, pos);
							BoundingBoxD drive_box = BoundingBoxD.CreateFromSphere(new BoundingSphereD(Vector3D.Zero, (jump_gate.IsLargeGrid()) ? 10 : 2));
							MySimpleObjectDraw.DrawTransparentBox(ref drive_matrix, ref drive_box, ref color, MySimpleObjectRasterizer.Wireframe, 1, 0.00025f, null, MyJumpGateModSession.MyMaterialsHolder.WeaponLaser, intensity: 10);
						}

						foreach (KeyValuePair<MyEntity, float> pair in jump_gate.GetEntitiesInJumpSpace())
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
								foreach (IMyCubeGrid subgrid in construct.GetCubeGrids())
								{
									MatrixD subgrid_matrix = this.HoloDisplayScalar * subgrid.WorldMatrix;
									Vector3D pos = MyJumpGateModSession.WorldVectorToLocalVectorP(ref jump_ellipse.WorldMatrix, subgrid.WorldMatrix.Translation);
									subgrid_matrix.Translation = MyJumpGateModSession.LocalVectorToWorldVectorP(ref scaled_matrix, pos);
									BoundingBoxD grid_box = subgrid.LocalAABB;
									MySimpleObjectDraw.DrawTransparentBox(ref subgrid_matrix, ref grid_box, ref red, MySimpleObjectRasterizer.Wireframe, 1, 0.00025f, null, MyJumpGateModSession.MyMaterialsHolder.WeaponLaser, intensity: 10);
								}
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
					else if (this.RemoteAntenna != null)
					{
						Vector3D normal = this.WorldMatrix.Up;
						Vector3D player_facing = table_holo_center - MyAPIGateway.Session.Camera.Position;
						player_facing = Vector3D.ProjectOnPlane(ref player_facing, ref normal);
						MatrixD billboard_matrix;
						MatrixD.CreateWorld(ref table_holo_center, ref player_facing, ref normal, out billboard_matrix);
						MyTransparentGeometry.AddBillboardOriented(MyJumpGateModSession.MyMaterialsHolder.GateAntennaDisconnectedControllerIcon, intense_red, billboard_matrix.Translation, billboard_matrix.Left, billboard_matrix.Up, 0.75f, 0.75f);
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
			this.BaseBlockSettings.AcquireLock();
			this.ModStorageComponent[MyJumpGateModSession.BlockComponentDataGUID] = Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(this.BaseBlockSettings));
			this.BaseBlockSettings.ReleaseLock();
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
			if (packet == null) return;
			MySerializedJumpGateController serialized = packet.Payload<MySerializedJumpGateController>();
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
				MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet {
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
			this.BaseBlockSettings.JumpGateID((jump_gate == null || jump_gate.Closed) ? -1 : jump_gate.JumpGateID);
			if (jump_gate == null || jump_gate.Closed) return;
			KeyValuePair<MyJumpGateRemoteAntenna, byte> remote_antenna = this.RemoteAntennaChannel;
			jump_gate.Controller = this;
			remote_antenna.Key?.SetControllerForOutboundControl(remote_antenna.Value, null);
			this.BaseBlockSettings.RemoteAntennaID(-1);
			this.BaseBlockSettings.RemoteAntennaChannel(0xFF);
			this.BaseBlockSettings.JumpGateName(jump_gate.GetName());
		}

		/// <summary>
		/// Attaches this controller to the specified remote jump gate<br />
		/// Jump gate must be nul or a jump gate on a different construct as this controller
		/// </summary>
		/// <param name="jump_gate">The jump gate to attach to or null to detach</param>
		public void AttachedRemoteJumpGate(MyJumpGate jump_gate)
		{
			MyJumpGate old_gate = this.AttachedJumpGate();
			if (old_gate == jump_gate || (jump_gate != null && this.JumpGateGrid == jump_gate.JumpGateGrid)) return;
			else if (old_gate != null && !old_gate.Closed) old_gate.Controller = null;
			KeyValuePair<MyJumpGateRemoteAntenna, byte> remote_antenna = this.RemoteAntennaChannel;
			if (remote_antenna.Key == null || remote_antenna.Value == 0xFF || remote_antenna.Key.GetConnectedControlledJumpGate(remote_antenna.Value) != jump_gate) jump_gate = null;
			this.BaseBlockSettings.JumpGateID((jump_gate == null || jump_gate.Closed) ? -1 : jump_gate.JumpGateID);
			if (jump_gate == null || jump_gate.Closed || this.RemoteAntenna == null) return;
			jump_gate.Controller = this;
			this.BaseBlockSettings.JumpGateName(jump_gate.GetName());
		}

		/// <summary>
		/// </summary>
		/// <returns>True if the attached jump gate is remote</returns>
		public bool IsAttachedJumpGateRemote()
		{
			MyJumpGate gate = this.AttachedJumpGate();
			return this.RemoteAntenna != null && gate != null && gate.JumpGateGrid != this.JumpGateGrid;
		}

		/// <summary>
		/// Updates this block data from a serialized controller
		/// </summary>
		/// <param name="controller">The serialized controller data</param>
		/// <param name="parent">The containing grid or null to calculate</param>
		/// <returns>Whether this block was updated</returns>
		public bool FromSerialized(MySerializedJumpGateController controller, MyJumpGateConstruct parent = null)
		{
			if (!base.FromSerialized(controller, parent)) return false;
			if (controller.SerializedPanelInfo != null) this.MultiPanelComponent?.Deserialize(controller.SerializedPanelInfo);
			MyControllerBlockSettingsStruct new_settings = MyAPIGateway.Utilities.SerializeFromBinary<MyControllerBlockSettingsStruct>(controller.BlockSettings);

			if (this.BaseBlockSettings == null)
			{
				this.BaseBlockSettings = new_settings;
				return true;
			}

			MyJumpGate jump_gate = this.AttachedJumpGate();
			bool can_update_gate_settings = jump_gate?.IsIdle() ?? true;
			this.BaseBlockSettings.AcquireLock();
			this.BaseBlockSettings.CanBeInbound(new_settings.CanBeInbound());
			this.BaseBlockSettings.CanBeOutbound(new_settings.CanBeOutbound());
			this.BaseBlockSettings.CanAcceptUnowned(new_settings.CanAcceptUnowned());
			this.BaseBlockSettings.CanAcceptEnemy(new_settings.CanAcceptEnemy());
			this.BaseBlockSettings.CanAcceptNeutral(new_settings.CanAcceptNeutral());
			this.BaseBlockSettings.CanAcceptFriendly(new_settings.CanAcceptFriendly());
			this.BaseBlockSettings.CanAcceptOwned(new_settings.CanAcceptOwned());
			this.BaseBlockSettings.JumpGateName(new_settings.JumpGateName());
			this.BaseBlockSettings.AutoActivationDelay(new_settings.AutoActivationDelay());

			KeyValuePair<float, float> mass = new_settings.AutoActivateMass();
			this.BaseBlockSettings.AutoActivateMass(mass.Key, mass.Value);

			KeyValuePair<double, double> power = new_settings.AutoActivatePower();
			this.BaseBlockSettings.AutoActivatePower(power.Key, power.Value);

			mass = new_settings.AllowedEntityMass();
			this.BaseBlockSettings.AllowedEntityMass(mass.Key, mass.Value);

			KeyValuePair<uint, uint> size = new_settings.AllowedCubeGridSize();
			this.BaseBlockSettings.AllowedCubeGridSize(size.Key, size.Value);
			
			if (can_update_gate_settings)
			{
				this.BaseBlockSettings.SelectedWaypoint(new_settings.SelectedWaypoint());
				this.BaseBlockSettings.JumpSpaceRadius(new_settings.JumpSpaceRadius());
				this.BaseBlockSettings.JumpGateID(new_settings.JumpGateID());
				this.BaseBlockSettings.JumpEffectAnimationName(new_settings.JumpEffectAnimationName());
				this.BaseBlockSettings.VectorNormalOverride(new_settings.VectorNormalOverride());
				this.BaseBlockSettings.JumpEffectAnimationColorShift(new_settings.JumpEffectAnimationColorShift());
				this.BaseBlockSettings.JumpSpaceDepthPercent(new_settings.JumpSpaceDepthPercent());
				this.BaseBlockSettings.JumpSpaceFitType(new_settings.JumpSpaceFitType());
				jump_gate?.SetJumpSpaceEllipsoidDirty();
				jump_gate?.SetDirty();

				KeyValuePair<MyJumpGateRemoteAntenna, byte> old_antenna = this.RemoteAntennaChannel;
				this.BaseBlockSettings.RemoteAntennaChannel(new_settings.RemoteAntennaChannel());
				this.BaseBlockSettings.RemoteAntennaID(new_settings.RemoteAntennaID());
				KeyValuePair<MyJumpGateRemoteAntenna, byte> new_antenna = this.RemoteAntennaChannel;

				if (old_antenna.Key != new_antenna.Key || old_antenna.Value != new_antenna.Value)
				{
					old_antenna.Key?.SetControllerForOutboundControl(old_antenna.Value, null);
					new_antenna.Key?.SetControllerForOutboundControl(new_antenna.Value, this);
				}
			}

			this.BaseBlockSettings.ReleaseLock();
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
		/// <param name="player_identity">The player's identiy to check</param>
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
		/// Gets the jump gate this controller is attached to
		/// </summary>
		/// <returns>The attached gate or null if none attached</returns>
		public MyJumpGate AttachedJumpGate()
		{
			long gate_id = this.BaseBlockSettings?.JumpGateID() ?? -1;
			KeyValuePair<MyJumpGateRemoteAntenna, byte> antenna = this.RemoteAntennaChannel;
			if (gate_id >= 0 && antenna.Value != 0xFF && antenna.Key != null) return antenna.Key?.GetConnectedControlledJumpGate(antenna.Value);
			else if (gate_id >= 0 && this.JumpGateGrid != null && !this.JumpGateGrid.Closed) return this.JumpGateGrid.GetJumpGate(gate_id);
			else return null;
		}

		/// <summary>
		/// Gets the list of waypoints this controller has<br />
		/// This is client dependent
		/// </summary>
		/// <returns>An enumerable containing this controller's waypoints</returns>
		public IEnumerable<MyJumpGateWaypoint> GetWaypointsList()
		{
			lock (this.WaypointsListMutex) return this.WaypointsList?.Distinct() ?? Enumerable.Empty<MyJumpGateWaypoint>();
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
			serialized.BlockSettings = MyAPIGateway.Utilities.SerializeToBinary(this.BaseBlockSettings);
			serialized.SerializedPanelInfo = this.MultiPanelComponent?.Serialize(true);
			return serialized;
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
		public byte[] BlockSettings;

		/// <summary>
		/// The serialized text panel component info
		/// </summary>
		[ProtoMember(21)]
		public MyObjectBuilder_ComponentBase SerializedPanelInfo;
	}
}
