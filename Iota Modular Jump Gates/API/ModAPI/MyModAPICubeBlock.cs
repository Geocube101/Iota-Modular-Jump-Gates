﻿using IOTA.ModularJumpGates.API.ModAPI.Util;
using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;

namespace IOTA.ModularJumpGates.API.ModAPI
{
	public class MyModAPICubeBlockBase : MyModAPIObjectBase
	{
		internal static MyModAPICubeBlockBase New(Dictionary<string, object> attributes)
		{
			return MyModAPIObjectBase.GetObjectOrNew<MyModAPICubeBlockBase>(attributes, () => new MyModAPICubeBlockBase(attributes));
		}

		private MyModAPICubeBlockBase(Dictionary<string, object> attributes) : base(attributes) { }
		protected MyModAPICubeBlockBase(Dictionary<string, object> block_base_attributes, Dictionary<string, object> attributes) : base(block_base_attributes.Concat(attributes).ToDictionary(pair => pair.Key, pair => pair.Value)) { }

		/// <summary>
		/// Whether this block should be synced<br/>
		/// If true, will be synced on next component tick
		/// </summary>
		public bool IsDirty => this.GetAttribute<bool>("IsDirty");

		/// <summary>
		/// Whether this block is large grid
		/// </summary>
		public bool IsLargeGrid => this.GetAttribute<bool>("IsLargeGrid");

		/// <summary>
		/// Whether this block is small grid
		/// </summary>
		public bool IsSmallGrid => this.GetAttribute<bool>("IsSmallGrid");

		/// <summary>
		/// Whether this is a wrapper around a block that isn't initialized<br />
		/// Always false on singleplayer or server
		/// </summary>
		public bool IsNullWrapper => this.GetAttribute<bool>("IsNullWrapper");

		/// <summary>
		/// Whether this component or it's attached block is closed
		/// </summary>
		public bool IsClosed => this.GetAttribute<bool>("IsClosed");

		/// <summary>
		/// Whether block is powered<br />
		/// <i>"current input >= required input"</i>
		/// </summary>
		public bool IsPowered => this.GetAttribute<bool>("IsPowered");

		/// <summary>
		/// Whether block is enabled
		/// </summary>
		public bool IsEnabled => this.GetAttribute<bool>("IsEnabled");

		/// <summary>
		/// Whether this block is working: (not closed, enabled, powered)
		/// </summary>
		public bool IsWorking => this.GetAttribute<bool>("IsWorking");

		/// <summary>
		/// The block ID of this block
		/// </summary>
		public long BlockID => this.GetAttribute<long>("BlockID");

		/// <summary>
		/// The cube grid ID of this block
		/// </summary>
		public long CubeGridID => this.GetAttribute<long>("CubeGridID");

		/// <summary>
		/// The construct ID of this block
		/// </summary>
		public long ConstructID => this.GetAttribute<long>("ConstructID");

		/// <summary>
		/// The player ID of this block's owner
		/// </summary>
		public long OwnerID => this.GetAttribute<long>("OwnerID");

		/// <summary>
		/// The steam ID of this block's owner
		/// </summary>
		public ulong OwnerSteamID => this.GetAttribute<ulong>("OwnerSteamID");

		/// <summary>
		/// The block local game tick
		/// </summary>
		public ulong LocalGameTick => this.GetAttribute<ulong>("LocalGameTick");

		/// <summary>
		/// The grid size of this block
		/// </summary>
		public MyCubeSize CubeGridSize => this.GetAttribute<MyCubeSize>("CubeGridSize");

		/// <summary>
		/// The terminal block this component is bound to
		/// </summary>
		public IMyUpgradeModule TerminalBlock => this.GetAttribute<IMyUpgradeModule>("TerminalBlock");

		/// <summary>
		/// The UTC date time representation of CubeBlockBase.LastUpdateTime
		/// </summary>
		public DateTime LastUpdateDateTimeUTC
		{
			get
			{
				return this.GetAttribute<DateTime>("LastUpdateDateTimeUTC");
			}

			set
			{
				this.SetAttribute<DateTime>("LastUpdateDateTimeUTC", value);
			}
		}

		/// <summary>
		/// The world matrix of this block
		/// </summary>
		public MatrixD WorldMatrix => this.GetAttribute<MatrixD>("WorldMatrix");

		/// <summary>
		/// The jump gate grid this component is bound to
		/// </summary>
		public MyModAPIJumpGateConstruct JumpGateGrid => MyModAPIJumpGateConstruct.New(this.GetAttribute<Dictionary<string, object>>("JumpGateGrid"));

		/// <summary>
		/// Sets the internal "IsDirty" flag to true
		/// <br />
		/// Marks block for sync on next component tick
		/// </summary>
		public void SetDirty()
		{
			this.GetMethod<Action>("SetDirty")();
		}

		public override bool Equals(object other)
		{
			return other != null && other is MyModAPICubeBlockBase && base.Equals(((MyModAPICubeBlockBase) other).TerminalBlock);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}

	public class MyModAPIJumpGateController : MyModAPICubeBlockBase
	{
		internal static new MyModAPIJumpGateController New(Dictionary<string, object> attributes)
		{
			return MyModAPIObjectBase.GetObjectOrNew<MyModAPIJumpGateController>(attributes, () => new MyModAPIJumpGateController(attributes));
		}

		private MyModAPIJumpGateController(Dictionary<string, object> attributes) : base((Dictionary<string, object>) (attributes["BlockBase"]), attributes) { }

		/// <summary>
		/// Stores block data for this block type
		/// </summary>
		[ProtoContract]
		public sealed class MyAPIControllerBlockSettings
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
			private MyAPIFactionDisplayType FactionDisplayType_V = MyAPIFactionDisplayType.FRIENDLY | MyAPIFactionDisplayType.OWNED;
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
			private MyAPIJumpGateWaypoint SelectedWaypoint_V = null;
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
			private MyAPIJumpSpaceFitType JumpSpaceFitType_V = MyAPIJumpSpaceFitType.INNER;

			private readonly object WriterLock = new object();

			public MyAPIControllerBlockSettings() { }
			public MyAPIControllerBlockSettings(MyModAPIJumpGateController controller, Dictionary<string, object> mapping)
			{
				this.FromDictionary(controller?.AttachedJumpGate, mapping);
			}

			public void AcquireLock()
			{
				Monitor.Enter(this.WriterLock);
			}
			public void ReleaseLock()
			{
				if (Monitor.IsEntered(this.WriterLock)) Monitor.Exit(this.WriterLock);
			}

			public void FromDictionary(MyModAPIJumpGate attached_gate, Dictionary<string, object> mapping)
			{
				if (mapping == null || attached_gate == null) return;

				lock (this.WriterLock)
				{
					this.CanAutoActivate_V = (bool) mapping.GetValueOrDefault("CanAutoActivate", this.CanAutoActivate_V);
					this.CanBeInbound_V = (bool) mapping.GetValueOrDefault("CanBeInbound", this.CanBeInbound_V);
					this.CanBeOutbound_V = (bool) mapping.GetValueOrDefault("CanBeOutbound", this.CanBeOutbound_V);
					this.HasVectorNormalOverride_V = (bool) mapping.GetValueOrDefault("HasVectorNormalOverride", this.HasVectorNormalOverride_V);
					this.FactionDisplayType_V = (MyAPIFactionDisplayType) (byte) mapping.GetValueOrDefault("FactionDisplayType", (byte) this.FactionDisplayType_V);
					this.JumpSpaceFitType_V = (MyAPIJumpSpaceFitType) (byte) mapping.GetValueOrDefault("JumpSpaceFitType", (byte) this.JumpSpaceFitType_V);
					this.JumpSpaceRadius_V = (double) mapping.GetValueOrDefault("JumpSpaceRadius", this.JumpSpaceRadius_V);
					this.JumpSpaceDepthPercent_V = (double) mapping.GetValueOrDefault("JumpSpaceDepthPercent", this.JumpSpaceDepthPercent_V);
					this.JumpEffectName_V = (string) mapping.GetValueOrDefault("JumpEffectName", this.JumpEffectName_V);
					this.VectorNormal_V = (Vector3D) mapping.GetValueOrDefault("VectorNormal", this.VectorNormal_V);
					this.EffectColorShift_V = (Color) mapping.GetValueOrDefault("EffectColorShift", this.EffectColorShift_V);

					{
						object selected_waypoint = mapping.GetValueOrDefault("SelectedWaypoint", null);
						MyAPIJumpGateWaypoint result_waypoint = null;
						if (selected_waypoint == null) result_waypoint = null;
						else if (selected_waypoint is IMyGps) result_waypoint = new MyAPIJumpGateWaypoint((IMyGps) selected_waypoint);
						else if (selected_waypoint is Vector3D)
						{
							Vector3D position = (Vector3D) selected_waypoint;
							IMyGps gps = MyAPIGateway.Session.GPS.Create("�TemporaryGPS�", $"[{Math.Round(position.X, 2)}, {Math.Round(position.Y, 2)}, {Math.Round(position.Z, 2)}]", position, false);
							result_waypoint = new MyAPIJumpGateWaypoint(gps);
						}
						else if (selected_waypoint is long[])
						{
							MyModAPIJumpGate target_gate = MyModAPISession.Instance.GetJumpGate(new JumpGateUUID((long[]) selected_waypoint));
							if (target_gate == null) throw new KeyNotFoundException("The specified jump gate does not exist");
							result_waypoint = new MyAPIJumpGateWaypoint(target_gate);
						}

						Vector3D? endpoint = result_waypoint?.GetEndpoint();
						Vector3D? node = attached_gate?.WorldJumpNode;

						if (endpoint != null && node != null)
						{
							double distance = Vector3D.Distance(endpoint.Value, node.Value);
							if (distance > (double) attached_gate.JumpGateConfiguration["MaximumJumpDistance"] || distance < (double) attached_gate.JumpGateConfiguration["MinimumJumpDistance"]) throw new InvalidOperationException("Specified waypoint distance is out of bounds");
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
					MyAPIJumpGateWaypoint waypoint = this.SelectedWaypoint_V;
					object selected_waypoint;

					switch (waypoint.WaypointType)
					{
						case MyAPIWaypointType.JUMP_GATE:
							selected_waypoint = waypoint.JumpGate;
							break;
						case MyAPIWaypointType.GPS:
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
						["FactionDisplayType"] = (byte) this.FactionDisplayType_V,
						["JumpSpaceFitType"] = (byte) this.JumpSpaceFitType_V,
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
					if (flag) this.FactionDisplayType_V |= MyAPIFactionDisplayType.UNOWNED;
					else this.FactionDisplayType_V &= ~MyAPIFactionDisplayType.UNOWNED;
				}
			}
			public void CanAcceptEnemy(bool flag)
			{
				lock (this.WriterLock)
				{
					if (flag) this.FactionDisplayType_V |= MyAPIFactionDisplayType.ENEMY;
					else this.FactionDisplayType_V &= ~MyAPIFactionDisplayType.ENEMY;
				}
			}
			public void CanAcceptNeutral(bool flag)
			{
				lock (this.WriterLock)
				{
					if (flag) this.FactionDisplayType_V |= MyAPIFactionDisplayType.NEUTRAL;
					else this.FactionDisplayType_V &= ~MyAPIFactionDisplayType.NEUTRAL;
				}
			}
			public void CanAcceptFriendly(bool flag)
			{
				lock (this.WriterLock)
				{
					if (flag) this.FactionDisplayType_V |= MyAPIFactionDisplayType.FRIENDLY;
					else this.FactionDisplayType_V &= ~MyAPIFactionDisplayType.FRIENDLY;
				}
			}
			public void CanAcceptOwned(bool flag)
			{
				lock (this.WriterLock)
				{
					if (flag) this.FactionDisplayType_V |= MyAPIFactionDisplayType.OWNED;
					else this.FactionDisplayType_V &= ~MyAPIFactionDisplayType.OWNED;
				}
			}
			public void CanAccept(MyAPIFactionDisplayType value)
			{
				lock (this.WriterLock) this.FactionDisplayType_V = value;
			}
			public void JumpSpaceFitType(MyAPIJumpSpaceFitType value)
			{
				lock (this.WriterLock) this.JumpSpaceFitType_V = value;
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
			public void SelectedWaypoint(MyAPIJumpGateWaypoint waypoint)
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
				return (this.FactionDisplayType_V & MyAPIFactionDisplayType.UNOWNED) != 0;
			}
			public bool CanAcceptEnemy()
			{
				return (this.FactionDisplayType_V & MyAPIFactionDisplayType.ENEMY) != 0;
			}
			public bool CanAcceptNeutral()
			{
				return (this.FactionDisplayType_V & MyAPIFactionDisplayType.NEUTRAL) != 0;
			}
			public bool CanAcceptFriendly()
			{
				return (this.FactionDisplayType_V & MyAPIFactionDisplayType.FRIENDLY) != 0;
			}
			public bool CanAcceptOwned()
			{
				return (this.FactionDisplayType_V & MyAPIFactionDisplayType.OWNED) != 0;
			}
			public MyAPIFactionDisplayType CanAccept()
			{
				return this.FactionDisplayType_V;
			}
			public MyAPIJumpSpaceFitType JumpSpaceFitType()
			{
				return this.JumpSpaceFitType_V;
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
			public MyAPIJumpGateWaypoint SelectedWaypoint()
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

		/// <summary>
		/// IMyTextSurfaceProvider Property
		/// </summary>
		public bool UseGenericLcd => this.GetAttribute<bool>("UseGenericLcd");

		/// <summary>
		/// IMyTextSurfaceProvider Property
		/// </summary>
		public int SurfaceCount => this.GetAttribute<int>("SurfaceCount");

		/// <summary>
		/// The block data for this block<br />
		/// Updating values in this class will not update the internal controller's settings - Once done modifying values, set this value with the updated controller settings
		/// </summary>
		public MyAPIControllerBlockSettings BlockSettings
		{
			get
			{
				MyAPIControllerBlockSettings settings = new MyAPIControllerBlockSettings();
				settings.FromDictionary(this.AttachedJumpGate, this.GetAttribute<Dictionary<string, object>>("BlockSettings"));
				return settings;
			}
			set
			{
				this.SetAttribute<Dictionary<string, object>>("BlockSettings", value.ToDictionary());
			}
		}

		/// <summary>
		/// Sets or gets this controller's attached jump gate
		/// </summary>
		public MyModAPIJumpGate AttachedJumpGate
		{
			get
			{
				return MyModAPIJumpGate.New(this.GetMethod<Func<Dictionary<string, object>>>("GetAttachedJumpGate")());
			}
			set
			{
				if (value != null && value.JumpGateGrid.CubeGridID != this.JumpGateGrid.CubeGridID) this.GetMethod<Action<long[]>>("SetAttachedRemoteJumpGate")((value?.Guid ?? JumpGateUUID.Empty).Packed());
				else this.GetMethod<Action<long[]>>("SetAttachedJumpGate")((value?.Guid ?? JumpGateUUID.Empty).Packed());
			}
		}

		/// <summary>
		/// Sets or gets this controller's attached jump gate
		/// </summary>
		public MyAPIJumpGateWaypoint SelectedWaypoint
		{
			get
			{
				return MyAPIGateway.Utilities.SerializeFromBinary<MyAPIJumpGateWaypoint>(this.GetAttribute<byte[]>("SelectedWaypoint"));
			}
			set
			{
				this.SetAttribute<byte[]>("SelectedWaypoint", MyAPIGateway.Utilities.SerializeToBinary(value));
			}
		}

		/// <summary>
		/// The antenna and channel this controller is searching for jump gates through
		/// </summary>
		public KeyValuePair<MyModAPIJumpGateRemoteAntenna, byte> RemoteAntennaChannel
		{
			get
			{
				KeyValuePair<Dictionary<string, object>, byte> remote_antenna_channel = this.GetAttribute<KeyValuePair<Dictionary<string, object>, byte>>("RemoteAntennaChannel");
				return new KeyValuePair<MyModAPIJumpGateRemoteAntenna, byte>(MyModAPIJumpGateRemoteAntenna.New(remote_antenna_channel.Key), remote_antenna_channel.Value);
			}
		}

		/// <summary>
		/// The antenna this controller is searching for jump gates through
		/// </summary>
		public MyModAPIJumpGateRemoteAntenna RemoteAntenna => MyModAPIJumpGateRemoteAntenna.New(this.GetAttribute<Dictionary<string, object>>("RemoteAntenna"));

		/// <summary>
		/// The antenna this controller's antenna is connected to or null
		/// </summary>
		public MyModAPIJumpGateRemoteAntenna ConnectedRemoteAntenna => MyModAPIJumpGateRemoteAntenna.New(this.GetAttribute<Dictionary<string, object>>("ConnectedRemoteAntenna"));

		/// <summary>
		/// </summary>
		/// <param name="steam_id">The player's steam ID to check</param>
		/// <returns>True if the caller's faction can jump to this gate</returns>
		public bool IsFactionRelationValid(ulong steam_id)
		{
			return this.GetMethod<Func<ulong, bool>>("IsSteamFactionRelationValid")(steam_id);
		}

		/// <summary>
		/// </summary>
		/// <param name="player_identity">The player's identiy to check</param>
		/// <returns>True if the caller's faction can jump to this gate</returns>
		public bool IsFactionRelationValid(long player_identity)
		{
			return this.GetMethod<Func<long, bool>>("IsPlayerFactionRelationValid")(player_identity);
		}

		/// <summary>
		/// Gets the list of waypoints this controller has<br />
		/// This is client dependent
		/// </summary>
		/// <returns>An enumerable containing this controller's waypoints</returns>
		public IEnumerable<MyAPIJumpGateWaypoint> GetWaypointsList()
		{
			return this.GetMethod<Func<IEnumerable<byte[]>>>("GetWaypointsList")().Select(MyAPIGateway.Utilities.SerializeFromBinary<MyAPIJumpGateWaypoint>);
		}
	}

	public class MyModAPIJumpGateDrive : MyModAPICubeBlockBase
	{
		internal static new MyModAPIJumpGateDrive New(Dictionary<string, object> attributes)
		{
			return MyModAPIObjectBase.GetObjectOrNew<MyModAPIJumpGateDrive>(attributes, () => new MyModAPIJumpGateDrive(attributes));
		}

		private MyModAPIJumpGateDrive(Dictionary<string, object> attributes) : base((Dictionary<string, object>) (attributes["BlockBase"]), attributes) { }

		/// <summary>
		/// The stored capacitor charge in MegaWatts
		/// </summary>
		public double StoredChargeMW => this.GetAttribute<double>("StoredChargeMW");

		/// <summary>
		/// The maximum possible distance this drive can raycast
		/// </summary>
		public double MaxRaycastDistance => this.GetAttribute<double>("MaxRaycastDistance");

		/// <summary>
		/// The brightness for the emitter emissives
		/// </summary>
		public double EmitterEmissiveBrightness => this.GetAttribute<double>("EmitterEmissiveBrightness");

		/// <summary>
		/// Gets the base power draw of this block (in Megawatts) from config
		/// </summary>
		public double BasePowerDrawMW => this.GetAttribute<double>("BasePowerDrawMW");

		/// <summary>
		/// Gets or sets this block's wattage sink override (in megawatts)<br />
		/// This value overrides this block's power draw
		/// </summary>
		public double WattageSinkOverride
		{
			get
			{
				return this.GetMethod<Func<double>>("GetWattageSinkOverride")();
			}
			set
			{
				this.GetMethod<Action<double>>("SetWattageSinkOverride")(value);
			}
		}

		/// <summary>
		/// The ID of the jump gate this drive is linked to or -1 if not linked
		/// </summary>
		public long JumpGateID => this.GetAttribute<long>("JumpGateID");

		/// <summary>
		/// The color of the emitter emissives
		/// </summary>
		public Color DriveEmitterColor => this.GetAttribute<Color>("DriveEmitterColor");

		/// <summary>
		/// The drive configuration variables for this block
		/// </summary>
		public Dictionary<string, object> DriveConfiguration => this.GetAttribute<Dictionary<string, object>>("Configuration");

		/// <summary>
		/// Animates this drive's emitter emissives' color
		/// </summary>
		/// <param name="from">The start color</param>
		/// <param name="to">The end color</param>
		/// <param name="duration">The duration in game ticks</param>
		public void CycleDriveEmitter(Color from, Color to, ushort duration)
		{
			this.GetMethod<Action<Color, Color, ushort>>("CycleDriveEmitter")(from, to, duration);
		}

		/// <summary>
		/// Sets this drive's emitter emissive color
		/// </summary>
		/// <param name="color">The new emitter color</param>
		public void SetDriveEmitterColor(Color color)
		{
			this.GetMethod<Action<Color>>("SetDriveEmitterColor")(color);
		}

		/// <summary>
		/// Whether this drive's emitter emissives are being animated
		/// </summary>
		/// <returns>Animatedness</returns>
		public bool DriveEmitterCycling()
		{
			return this.GetMethod<Func<bool>>("DriveEmitterCycling")();
		}

		/// <summary>
		/// Gets the amount of power being drawn by this drive's power sink
		/// </summary>
		/// <returns>The current input power in MW</returns>
		public double GetCurrentWattageSinkInput()
		{
			return this.GetMethod<Func<double>>("GetCurrentWattageSinkInput")();
		}

		/// <summary>
		/// Drains the capacitor's charge
		/// </summary>
		/// <param name="power_mw">The power (in MegaWatts) to drain</param>
		/// <returns>The remaining power in MegaWatts</returns>
		public double DrainStoredCharge(double power_mw)
		{
			return this.GetMethod<Func<double, double>>("DrainStoredCharge")(power_mw);
		}

		/// <summary>
		/// Gets the endpoint for a ray starting at the end of the collision mesh and ending after the specified distance
		/// </summary>
		/// <param name="distance">The distance to cast</param>
		/// <returns>A world coordinate indicating the raycast endpoint</returns>
		public Vector3D GetDriveRaycastEndpoint(double distance)
		{
			return this.GetMethod<Func<double, Vector3D>>("GetDriveRaycastEndpoint")(distance);
		}

		/// <summary>
		/// Gets the endpoint for a ray starting at the block's center and ending at the end of the collision mesh
		/// </summary>
		/// <returns>A world coordinate indicating the raycast start point</returns>
		public Vector3D GetDriveRaycastStartpoint()
		{
			return this.GetMethod<Func<Vector3D>>("GetDriveRaycastStartpoint")();
		}
	}

	public class MyModAPIJumpGateCapacitor : MyModAPICubeBlockBase
	{
		internal static new MyModAPIJumpGateCapacitor New(Dictionary<string, object> attributes)
		{
			return MyModAPIObjectBase.GetObjectOrNew<MyModAPIJumpGateCapacitor>(attributes, () => new MyModAPIJumpGateCapacitor(attributes));
		}

		private MyModAPIJumpGateCapacitor(Dictionary<string, object> attributes) : base((Dictionary<string, object>) (attributes["BlockBase"]), attributes) { }

		/// <summary>
		/// The stored capacitor charge in MegaWatts
		/// </summary>
		public double StoredChargeMW => this.GetAttribute<double>("StoredChargeMW");

		/// <summary>
		/// The block data for this block
		/// </summary>
		public Dictionary<string, object> BlockSettings => this.GetAttribute<Dictionary<string, object>>("BlockSettings");

		/// <summary>
		/// The capacitor configuration variables for this block
		/// </summary>
		public Dictionary<string, object> CapacitorConfiguration => this.GetAttribute<Dictionary<string, object>>("Configuration");

		/// <summary>
		/// Drains the capacitor's charge
		/// </summary>
		/// <param name="power_mw">The power (in MegaWatts) to drain</param>
		/// <returns>The remaining power (in MegaWatts)</returns>
		public double DrainStoredCharge(double power_mw)
		{
			return this.GetMethod<Func<double, double>>("DrainStoredCharge")(power_mw);
		}
	}

	public class MyModAPIJumpGateRemoteAntenna : MyModAPICubeBlockBase
	{
		internal static new MyModAPIJumpGateRemoteAntenna New(Dictionary<string, object> attributes)
		{
			return MyModAPIObjectBase.GetObjectOrNew<MyModAPIJumpGateRemoteAntenna>(attributes, () => new MyModAPIJumpGateRemoteAntenna(attributes));
		}

		private MyModAPIJumpGateRemoteAntenna(Dictionary<string, object> attributes) : base((Dictionary<string, object>) (attributes["BlockBase"]), attributes) { }

		/// <summary>
		/// A bit flag of settings allowed to be modified by a controller
		/// </summary>
		public MyAPIAllowedRemoteSettings AllowedRemoteSettings
		{
			get { return (MyAPIAllowedRemoteSettings) this.GetAttribute<byte>("AllowedRemoteSettings"); }
			set { this.SetAttribute<byte>("AllowedRemoteSettings", (byte) value); }
		}

		/// <summary>
		/// The range this antenna may accept connections at
		/// </summary>
		public double BroadcastRange
		{
			get { return this.GetAttribute<double>("BroadcastRange"); }
			set { this.SetAttribute<double>("BroadcastRange", value); }
		}

		/// <summary>
		/// The names to be given to jump gates on this antenna<br />
		/// Names are by channel
		/// </summary>
		public string[] JumpGateNames
		{
			get { return this.GetAttribute<string[]>("JumpGateNames"); }
			set { this.SetAttribute<string[]>("JumpGateNames", value); }
		}

		/// <summary>
		/// Gets the base controller settings for each channel of this antenna
		/// </summary>
		public MyModAPIJumpGateController.MyAPIControllerBlockSettings[] BaseControllerSettings
		{
			get {
				byte channel = 0;
				return this.GetAttribute<Dictionary<string, object>[]>("ControllerBlockSettings").Select((setting) => {
					MyModAPIJumpGateController.MyAPIControllerBlockSettings settings = new MyModAPIJumpGateController.MyAPIControllerBlockSettings();
					settings.FromDictionary(this.GetInboundControlGate(channel++), setting);
					return settings;
				}).ToArray();
			}
			set { this.SetAttribute<Dictionary<string, object>[]>("ControllerBlockSettings", value.Select((settings) => settings.ToDictionary()).ToArray()); }
		}

		/// <summary>
		/// Sets the channel to a specified jump gate or clears the channel if the gate is null or closed
		/// </summary>
		/// <param name="channel">The channel or 255 for next available</param>
		/// <param name="gate">The gate to bind</param>
		public void SetGateForInboundControl(byte channel, MyModAPIJumpGate gate)
		{
			this.GetMethod<Action<byte, long>>("SetGateForInboundControl")(channel, gate?.JumpGateID ?? -1);
		}

		/// <summary>
		/// Sets the channel to a specified jump gate controller or clears the channel if the controller is null or closed
		/// </summary>
		/// <param name="channel">The channel or 255 for next available</param>
		/// <param name="controller">The controller to bind</param>
		public void SetControllerForOutboundControl(byte channel, MyModAPIJumpGateController controller)
		{
			this.GetMethod<Action<byte, long>>("SetControllerForOutboundControl")(channel, controller?.BlockID ?? -1);
		}

		/// <summary>
		/// </summary>
		/// <param name="channel">The channel to check</param>
		/// <param name="steam_id">The player's steam ID to check</param>
		/// <returns>True if the caller's faction can jump to this gate</returns>
		public bool IsFactionRelationValid(byte channel, ulong steam_id)
		{
			return this.GetMethod<Func<byte, ulong, bool>>("IsSteamFactionRelationValid")(channel, steam_id);
		}

		/// <summary>
		/// </summary>
		/// <param name="channel">The channel to check</param>
		/// <param name="player_identity">The player's identiy to check</param>
		/// <returns>True if the caller's faction can jump to this gate</returns>
		public bool IsFactionRelationValid(byte channel, long player_identity)
		{
			return this.GetMethod<Func<byte, long, bool>>("IsPlayerFactionRelationValid")(channel, player_identity);
		}

		/// <summary>
		/// Gets the control channel this jump gate is bound to
		/// </summary>
		/// <param name="gate">The gate to check</param>
		/// <returns>The control channel or 255 if not registered</returns>
		public byte GetJumpGateInboundControlChannel(MyModAPIJumpGate gate)
		{
			return (gate == null) ? (byte) 0xFF : this.GetMethod<Func<long[], byte>>("GetJumpGateInboundControlChannel")(gate.Guid.Packed());
		}

		/// <summary>
		/// Gets the control channel this jump gate controller is bound to
		/// </summary>
		/// <param name="controller">The controller to check</param>
		/// <returns>The control channel or 255 if not registered</returns>
		public byte GetControllerOutboundControlChannel(MyModAPIJumpGateController controller)
		{
			return (controller == null || !controller.HandleValid) ? (byte) 0xFF : this.GetMethod<Func<long[], byte>>("GetControllerOutboundControlChannel")(controller.ObjectID.Value.Packed());
		}

		/// <summary>
		/// Gets the name of the jump gate attached on the specified channel
		/// </summary>
		/// <param name="channel">The channel to check</param>
		/// <returns>The attached jump gate's name or null</returns>
		public string GetJumpGateName(byte channel)
		{
			return this.GetMethod<Func<byte, string>>("GetJumpGateName")(channel);
		}

		/// <summary>
		/// </summary>
		/// <param name="channel">The channel to check</param>
		/// <returns>The gate listening on the specified inbound channel or null</returns>
		public MyModAPIJumpGate GetInboundControlGate(byte channel)
		{
			return MyModAPIJumpGate.New(this.GetMethod<Func<byte, Dictionary<string, object>>>("GetInboundControlGate")(channel));
		}

		/// <summary>
		/// Gets the jump gate on the other end of this antenna's outbound connection or null
		/// </summary>
		/// <param name="channel">The channel to check</param>
		/// <returns>The connected gate</returns>
		public MyModAPIJumpGate GetConnectedControlledJumpGate(byte channel)
		{
			return MyModAPIJumpGate.New(this.GetMethod<Func<byte, Dictionary<string, object>>>("GetConnectedControlledJumpGate")(channel));
		}

		/// <summary>
		/// </summary>
		/// <param name="channel">The channel to check</param>
		/// <returns>The controller listening on the specified outbound channel or null</returns>
		public MyModAPIJumpGateController GetOutboundControlController(byte channel)
		{
			return MyModAPIJumpGateController.New(this.GetMethod<Func<byte, Dictionary<string, object>>>("GetOutboundControlController")(channel));
		}

		/// <summary>
		/// Gets the antenna on the other end of this connection for the specified channel
		/// </summary>
		/// <param name="channel">The channel to check</param>
		/// <returns>The connected antenna or null</returns>
		public MyModAPIJumpGateRemoteAntenna GetConnectedRemoteAntenna(byte channel)
		{
			return MyModAPIJumpGateRemoteAntenna.New(this.GetMethod<Func<byte, Dictionary<string, object>>>("GetConnectedRemoteAntenna")(channel));
		}

		/// <summary>
		/// </summary>
		/// <returns>Gets the IDs of all inbound registered jump gates</returns>
		public IEnumerable<long> RegisteredInboundControlGateIDs()
		{
			return this.GetMethod<Func<IEnumerable<long>>>("RegisteredInboundControlGateIDs")();
		}

		/// <summary>
		/// </summary>
		/// <returns>Gets the IDs of all outbound registered jump gate controllers</returns>
		public IEnumerable<long> RegisteredOutboundControlControllerIDs()
		{
			return this.GetMethod<Func<IEnumerable<long>>>("RegisteredOutboundControlControllerIDs")();
		}

		/// <summary>
		/// </summary>
		/// <returns>Gets all inbound registered jump gates</returns>
		public IEnumerable<MyModAPIJumpGate> RegisteredInboundControlGates()
		{
			return this.GetMethod<Func<IEnumerable<Dictionary<string, object>>>>("RegisteredInboundControlGates")().Select(MyModAPIJumpGate.New);
		}

		/// <summary>
		/// </summary>
		/// <returns>Gets all inbound registered jump gate controllers</returns>
		public IEnumerable<MyModAPIJumpGateController> RegisteredOutboundControlControllers()
		{
			return this.GetMethod<Func<IEnumerable<Dictionary<string, object>>>>("RegisteredOutboundControlControllers")().Select(MyModAPIJumpGateController.New);
		}

		/// <summary>
		/// Gets all working antennas within range of this one
		/// </summary>
		/// <returns>An enumerable containing all nearby working antennas</returns>
		public IEnumerable<MyModAPIJumpGateRemoteAntenna> GetNearbyAntennas()
		{
			return this.GetMethod<Func<IEnumerable<Dictionary<string, object>>>>("GetNearbyAntennas")().Select(MyModAPIJumpGateRemoteAntenna.New);
		}

		/// <summary>
		/// Gets the list of waypoints this controller has<br />
		/// This is client dependent
		/// </summary>
		/// <param name="channel">The channel who's visible waypoints to get</param>
		/// <returns>An enumerable containing this controller's waypoints</returns>
		public IEnumerable<MyAPIJumpGateWaypoint> GetWaypointsList(byte channel)
		{
			return this.GetMethod<Func<byte, IEnumerable<byte[]>>>("GetWaypointsList")(channel).Select(MyAPIGateway.Utilities.SerializeFromBinary<MyAPIJumpGateWaypoint>);
		}
	}

	public class MyModAPIJumpGateRemoteLink : MyModAPICubeBlockBase
	{
		internal static new MyModAPIJumpGateRemoteLink New(Dictionary<string, object> attributes)
		{
			return MyModAPIObjectBase.GetObjectOrNew<MyModAPIJumpGateRemoteLink>(attributes, () => new MyModAPIJumpGateRemoteLink(attributes));
		}

		private MyModAPIJumpGateRemoteLink(Dictionary<string, object> attributes) : base((Dictionary<string, object>) (attributes["BlockBase"]), attributes) { }

		/// <summary>
		/// True if this remote link is the parent of a link connection
		/// </summary>
		public bool IsLinkParent => this.GetAttribute<bool>("IsLinkParent");

		/// <summary>
		/// Whether to display the connection effect when this link is connected to a remote link
		/// </summary>
		public bool DisplayConnectionEffect
		{
			get { return this.GetAttribute<bool>("DisplayConnectionEffect"); }
			set { this.SetAttribute<bool>("DisplayConnectionEffect", value); }
		}

		/// <summary>
		/// Whether this remote link is actually connected to another remote link
		/// </summary>
		public bool IsPhysicallyConnected => this.GetAttribute<bool>("IsPhysicallyConnected");

		/// <summary>
		/// A bit flag of faction connection types allowed to connect to this remote link
		/// </summary>
		public MyAPIFactionDisplayType AllowedFactionConnections
		{
			get { return (MyAPIFactionDisplayType) this.GetAttribute<byte>("AllowedFactionConnections"); }
			set { this.SetAttribute<byte>("AllowedFactionConnections", (byte) value); }
		}

		/// <summary>
		/// The channel this link accepts connections on
		/// </summary>
		public ushort ChannelID
		{
			get { return this.GetAttribute<ushort>("ChannelID"); }
			set { this.SetAttribute<ushort>("ChannelID", value); }
		}

		/// <summary>
		/// The currently set block ID of the remote link this link is connected to
		/// </summary>
		public long AttachedRemoteLinkID
		{
			get { return this.GetAttribute<long>("AttachedRemoteLinkID"); }
			set { this.SetAttribute<long>("AttachedRemoteLinkID", value); }
		}

		/// <summary>
		/// Max connection distance allowed between two remote links
		/// </summary>
		public double MaxConnectionDistance => this.GetAttribute<double>("MaxConnectionDistance");

		/// <summary>
		/// The connection effect's display color
		/// </summary>
		public Color ConnectionEffectColor
		{
			get { return this.GetAttribute<Color>("ConnectionEffectColor"); }
			set { this.SetAttribute<Color>("ConnectionEffectColor", value); }
		}

		/// <summary>
		/// The attached remote link or null
		/// </summary>
		public MyModAPIJumpGateRemoteLink AttachedRemoteLink => MyModAPIJumpGateRemoteLink.New(this.GetAttribute<Dictionary<string, object>>("AttachedRemoteLink"));

		/// <summary>
		/// Breaks the connection between this remote link and its attached remote link
		/// </summary>
		public void BreakConnection(bool permanent)
		{
			this.GetMethod<Action<bool>>("BreakConnection")(permanent);
		}

		/// <summary>
		/// Allows a certain faction relation to connect to this remote link
		/// </summary>
		/// <param name="setting">The faction relations to modify</param>
		/// <param name="flag">Whether to allow specified relation</param>
		public void AllowFactionConnection(MyAPIFactionDisplayType setting, bool flag)
		{
			if (flag) this.AllowedFactionConnections |= setting;
			else this.AllowedFactionConnections &= ~setting;
		}

		/// <summary>
		/// Checks if the specified faction relation is allowed to connect to this remote link
		/// </summary>
		/// <param name="setting">The faction relations to check</param>
		/// <returns>True if allowed</returns>
		public bool IsFactionConnectionAllowed(MyAPIFactionDisplayType setting)
		{
			return (this.AllowedFactionConnections & setting) != 0;
		}

		/// <summary>
		/// Connects two remote links together<br />
		/// Links must be within the max connection distance of both links
		/// </summary>
		/// <param name="link">The other remote link</param>
		/// <returns>True if successfull</returns>
		public bool Connect(MyModAPIJumpGateRemoteLink link)
		{
			return link != null && this.GetMethod<Func<long[], bool>>("Connect")(link.ObjectID.Value.Packed());
		}

		/// <summary>
		/// </summary>
		/// <param name="steam_id">The player's steam ID to check</param>
		/// <returns>True if the caller's faction can jump to this gate</returns>
		public bool IsFactionRelationValid(ulong steam_id)
		{
			return this.GetMethod<Func<ulong, bool>>("IsSteamFactionRelationValid")(steam_id);
		}

		/// <summary>
		/// </summary>
		/// <param name="player_identity">The player's identiy to check</param>
		/// <returns>True if the caller's faction can jump to this gate</returns>
		public bool IsFactionRelationValid(long player_identity)
		{
			return this.GetMethod<Func<long, bool>>("IsPlayerFactionRelationValid")(player_identity);
		}

		/// <summary>
		/// Gets all working links within range of this one
		/// </summary>
		/// <returns>An enumerable containing all nearby working links</returns>
		public IEnumerable<MyModAPIJumpGateRemoteLink> GetNearbyLinks()
		{
			return this.GetMethod<Func<IEnumerable<Dictionary<string, object>>>>("GetNearbyLinks")().Select(MyModAPIJumpGateRemoteLink.New);
		}
	}

	public class MyModAPIJumpGateServerAntenna : MyModAPICubeBlockBase
	{
		internal static new MyModAPIJumpGateServerAntenna New(Dictionary<string, object> attributes)
		{
			return MyModAPIObjectBase.GetObjectOrNew<MyModAPIJumpGateServerAntenna>(attributes, () => new MyModAPIJumpGateServerAntenna(attributes));
		}

		private MyModAPIJumpGateServerAntenna(Dictionary<string, object> attributes) : base((Dictionary<string, object>) (attributes["BlockBase"]), attributes) { }
	}
}
