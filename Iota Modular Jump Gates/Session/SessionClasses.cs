using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace IOTA.ModularJumpGates.Session
{
	internal partial class MyJumpGateModSession
	{
		public sealed class MyMaterialsHolder
		{
			public readonly MyStringId WeaponLaser = MyStringId.GetOrCompute("WeaponLaser");
			public readonly MyStringId GizmoDrawLine = MyStringId.GetOrCompute("GizmoDrawLine");
			public readonly MyStringId EnabledEntityMarker = MyStringId.GetOrCompute("IOTA.JumpGateControllerIcon.EntityMarker");
			public readonly MyStringId DisabledEntityMarker = MyStringId.GetOrCompute("IOTA.JumpGateControllerIcon.EntityMarker");
			public readonly MyStringId GateOfflineControllerIcon = MyStringId.GetOrCompute("IOTA.JumpGateControllerIcon.GateOffline");
			public readonly MyStringId GateDisconnectedControllerIcon = MyStringId.GetOrCompute("IOTA.JumpGateControllerIcon.NoGateConnected");
			public readonly MyStringId GateAntennaDisconnectedControllerIcon = MyStringId.GetOrCompute("IOTA.JumpGateControllerIcon.NoAntennaConnection");
			public readonly MyStringId SpacialMarkerJumpNode = MyStringId.GetOrCompute("IOTA.HUD.SpacialMarker.JumpNode");
			public readonly MyStringId SpacialMarkerJumpPoint = MyStringId.GetOrCompute("IOTA.HUD.SpacialMarker.JumpPoint");
			public readonly MyStringId SpacialMarkerDamagePoint = MyStringId.GetOrCompute("IOTA.HUD.SpacialMarker.DamagePoint");
			public readonly MyStringId SpacialMarkerDeletePoint = MyStringId.GetOrCompute("IOTA.HUD.SpacialMarker.DeletePoint");
			public readonly MyStringId WhiteDot = MyStringId.GetOrCompute("WhiteDot");
		}

		public sealed class MyThreadedUpdateInfo
		{
			private readonly object ReadBufferLock = new object();
			private readonly object WriteBufferLock = new object();
			private List<long> GridBuffer1 = new List<long>();
			private List<long> GridBuffer2 = new List<long>();
			private List<long> ReadBuffer;
			private List<long> WriteBuffer;

			public bool IsFinished = true;

			public MyThreadedUpdateInfo()
			{
				this.ReadBuffer = this.GridBuffer1;
				this.WriteBuffer = this.GridBuffer2;
			}

			public void Dispose()
			{
				lock (this.ReadBufferLock)
				{
					lock (this.WriteBufferLock)
					{
						this.IsFinished = true;
						this.GridBuffer1?.Clear();
						this.GridBuffer2?.Clear();
						this.GridBuffer1 = null;
						this.GridBuffer2 = null;
						this.ReadBuffer = null;
						this.WriteBuffer = null;
					}
				}
			}

			public void Swap()
			{
				lock (this.ReadBufferLock)
				{
					lock (this.WriteBufferLock)
					{
						List<long> temp = this.ReadBuffer;
						this.ReadBuffer = this.WriteBuffer;
						this.WriteBuffer = temp;
					}
				}
			}

			public void EnqueueID(long id)
			{
				lock (this.WriteBufferLock) this.WriteBuffer?.Add(id);
			}

			public IEnumerable<long> ReadEnqueuedIDs()
			{
				lock (this.ReadBufferLock)
				{
					if (this.ReadBuffer != null)
					{
						foreach (long id in this.ReadBuffer) yield return id;
						this.ReadBuffer.Clear();
					}
				}
			}
		}

		public sealed class MyExternalModInfo
		{
			public readonly bool RealSolarSystemsEnabled;
			public readonly ImmutableHashSet<ulong> LoadedModIDs;

			public MyExternalModInfo(IMyModContext context)
			{
				this.LoadedModIDs = MyAPIGateway.Session.Mods.Select((mod) => mod.GetWorkshopId().Id).ToImmutableHashSet();
				this.RealSolarSystemsEnabled = this.LoadedModIDs.Contains(3351055036);
			}
		}

		[ProtoContract]
		public sealed class MyModLogFileInfo
		{
			[ProtoMember(1)]
			public readonly string Filename;
			[ProtoMember(2)]
			public long ModificationTime;

			public MyModLogFileInfo() { }
			public MyModLogFileInfo(string filename)
			{
				this.Filename = filename;
				this.ModificationTime = DateTime.UtcNow.Ticks;
			}

			public void UpdateTime()
			{
				this.ModificationTime = DateTime.UtcNow.Ticks;
			}
		}

		[ProtoContract]
		public sealed class MyModDataInfo
		{
			[ProtoMember(1)]
			public Vector3I ModVersion;

			[ProtoMember(2)]
			public uint MaxModSpecificLogFiles;

			[ProtoMember(3)]
			public List<MyModLogFileInfo> ModLogFiles;
		}

		[ProtoContract]
		public sealed class MyCockpitInfo : IEquatable<MyCockpitInfo>
		{
			[ProtoMember(1)]
			public long CockpitId;

			[ProtoMember(2)]
			public bool DisplayHudMarkers = true;

			[ProtoMember(3)]
			public bool DisplayForAttachedGates = false;

			[ProtoMember(4)]
			public long LastUpdateTimeEpoch;

			public IMyCockpit Cockpit => MyAPIGateway.Entities.GetEntityById(this.CockpitId) as IMyCockpit;

			public DateTime LastUpdateTime
			{
				get { return new DateTime(this.LastUpdateTimeEpoch); }
				set { this.LastUpdateTimeEpoch = value.Ticks; }
			}

			public static bool operator ==(MyCockpitInfo a, MyCockpitInfo b)
			{
				return object.ReferenceEquals(a, b)
					|| (!object.ReferenceEquals(a, null) && !object.ReferenceEquals(b, null) && a.CockpitId == b.CockpitId && a.DisplayHudMarkers == b.DisplayHudMarkers && a.DisplayForAttachedGates == b.DisplayForAttachedGates);
			}

			public static bool operator !=(MyCockpitInfo a, MyCockpitInfo b)
			{
				return !(a == b);
			}

			public MyCockpitInfo() { }

			public MyCockpitInfo(IMyCockpit cockpit)
			{
				this.CockpitId = cockpit.EntityId;
			}

			public bool IsDefault()
			{
				return this.SettingsEqual(new MyCockpitInfo());
			}

			public bool SettingsEqual(MyCockpitInfo other)
			{
				return other != null && other.DisplayHudMarkers == this.DisplayHudMarkers && other.DisplayForAttachedGates == this.DisplayForAttachedGates;
			}

			public bool Equals(MyCockpitInfo other)
			{
				return this == other;
			}

			public override bool Equals(object obj)
			{
				return this == (obj as MyCockpitInfo);
			}

			public override int GetHashCode()
			{
				return this.CockpitId.GetHashCode();
			}
		}
	}
}
