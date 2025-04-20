using IOTA.ModularJumpGates.API;
using IOTA.ModularJumpGates.CubeBlock;
using IOTA.ModularJumpGates.Util;
using IOTA.ModularJumpGates.Util.ConcurrentCollections;
using ProtoBuf;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace IOTA.ModularJumpGates
{
	public enum MyJumpGateStatus : byte { NONE, SWITCHING, IDLE, OUTBOUND, INBOUND, CANCELLED, INVALID = 0xFF };
	public enum MyJumpGatePhase : byte { NONE, IDLE, CHARGING, JUMPING, RESETING, INVALID = 0xFF };
	public enum MyGateInvalidationReason : byte { NONE, CLOSED, INSUFFICIENT_DRIVES, NULL_GRID, NULL_STATUS, NULL_PHASE, INVALID_ID, INSUFFICIENT_NODES };
	public enum MyJumpFailReason : byte { NONE, SUCCESS, IN_PROGRESS,
		SRC_INVALID, CONTROLLER_NOT_CONNECTED, SRC_DISABLED, SRC_NOT_CONFIGURED, SRC_BUSY, SRC_ROUTING_DISABLED, SRC_INBOUND_ONLY, SRC_ROUTING_CHANGED, SRC_CLOSED,
		NULL_DESTINATION, DESTINATION_UNAVAILABLE, NULL_ANIMATION, SUBSPACE_BUSY, RADIO_LINK_FAILED, BEACON_LINK_FAILED, JUMP_SPACE_TRANSPOSED, CANCELLED, UNKNOWN_ERROR, NO_ENTITIES, NO_ENTITIES_JUMPED, INSUFFICIENT_POWER, CROSS_SERVER_JUMP, BEACON_BLOCKED,
		DST_UNAVAILABLE, DST_ROUTING_DISABLED, DST_OUTBOUND_ONLY, DST_FORBIDDEN, DST_DISABLED, DST_NOT_CONFIGURED, DST_BUSY, DST_RADIO_CONNECTION_INTERRUPTED, DST_BEACON_CONNECTION_INTERRUPTED, DST_ROUTING_CHANGED, DST_VOIDED,
		INVALID = 0xFF
	}
	public enum MyGateColliderStatus { NONE, ATTACHED, CLOSED };
	public enum MyJumpTypeEnum { STANDARD, INBOUND_VOID, OUTBOUND_VOID }

	internal class MyJumpGate : IEquatable<MyJumpGate>
	{
		#region JumpGate Classes
		/// <summary>
		/// Class holding a jump request
		/// </summary>
		[ProtoContract]
		private sealed class JumpGateInfo
		{
			public enum TypeEnum { JUMP_START, JUMP_FAIL, JUMP_SUCCESS, CLOSED };

			#region Public Variables
			/// <summary>
			/// Whether the jump failure was a cancelled jump
			/// </summary>
			[ProtoMember(1)]
			public bool CancelOverride;

			/// <summary>
			/// The type of request
			/// </summary>
			[ProtoMember(2)]
			public TypeEnum Type;

			[ProtoMember(3)]
			public MyJumpTypeEnum JumpType;

			/// <summary>
			/// The gate's true endpoint
			/// </summary>
			[ProtoMember(4)]
			public Vector3D? TrueEndpoint = null;

			/// <summary>
			/// The JumpGateUUID of the targeted gate as a Guid
			/// </summary>
			[ProtoMember(5)]
			public Guid JumpGateID;

			/// <summary>
			/// The controller settings used to enact the jump
			/// </summary>
			[ProtoMember(6)]
			public MyJumpGateController.MyControllerBlockSettingsStruct ControllerSettings;

			/// <summary>
			/// The targeted jump gate controller settings
			/// </summary>
			[ProtoMember(7)]
			public MyJumpGateController.MyControllerBlockSettingsStruct TargetControllerSettings;

			/// <summary>
			/// The resulting message from the jump
			/// </summary>
			[ProtoMember(8)]
			public string ResultMessage;

			/// <summary>
			/// The entity batches for this jump
			/// </summary>
			[ProtoMember(9)]
			public List<string> EntityBatches;
			#endregion

			#region Constructors
			/// <summary>
			/// Dummy default constructor for use with ProtoBuf
			/// </summary>
			public JumpGateInfo() { }
			#endregion
		}

		/// <summary>
		/// Class storing information on the drive intersect planes
		/// </summary>
		private sealed class JumpDriveIntersectPlane
		{
			#region Public Variables
			/// <summary>
			/// The plane
			/// </summary>
			public PlaneD Plane;

			/// <summary>
			/// The plane's primary normal
			/// </summary>
			public Vector3D PlanePrimaryNormal;

			/// <summary>
			/// The drives used to build this plane
			/// </summary>
			public HashSet<MyJumpGateDrive> JumpGateDrives = new HashSet<MyJumpGateDrive>();

			/// <summary>
			/// The number of drivs axis aligned with this plane
			/// </summary>
			public int AlignedDrivesCount = 0;
			#endregion
		}

		/// <summary>
		/// Class holding power syphon functionality
		/// </summary>
		private sealed class GridPowerSyphon
		{
			#region Private Variables
			/// <summary>
			/// The gate syphoning power
			/// </summary>
			private readonly MyJumpGate JumpGate;

			/// <summary>
			/// The number of working drives for this gate
			/// </summary>
			private int WorkingDrivesCount;

			/// <summary>
			/// The remaining ticks in the syphon
			/// </summary>
			private ulong SyphonTimeTicks;

			/// <summary>
			/// The total amount of power to syphon in MegaWatts
			/// </summary>
			private double SyphonPower = -1;

			/// <summary>
			/// The remaining power to syphon in MegaWatts
			/// </summary>
			private double RemainingPower = -1;

			/// <summary>
			/// The callback to execute once syphon is complete
			/// </summary>
			private Action<bool> Callback;

			/// <summary>
			/// A temporary list of jump gate drives
			/// </summary>
			private List<MyJumpGateDrive> JumpGateDrives = new List<MyJumpGateDrive>();
			#endregion

			#region Constructors
			/// <summary>
			/// Creates a new GridPowerSyphhon
			/// </summary>
			/// <param name="gate">The jump gate to attach to</param>
			/// <exception cref="ArgumentNullException">If the gate is null</exception>
			public GridPowerSyphon(MyJumpGate gate)
			{
				if (gate == null) throw new ArgumentNullException($"Jump gate is null");
				this.JumpGate = gate;
			}
			#endregion

			#region Public Methods
			/// <summary>
			/// Begins a power syphon from grid
			/// </summary>
			/// <param name="power_mw">The power to syphon in MegaWatts</param>
			/// <param name="ticks">The number of game ticks to syphon for</param>
			/// <param name="callback">The callback to execute once syphon is complete<br />Accepts one bool indicating whether the syphon was successfull</param>
			/// <param name="syphon_grid_only">Whether to only syphon grid power and ignore capacitors and drives</param>
			public void DoSyphonPower(double power_mw, ulong ticks, Action<bool> callback, bool syphon_grid_only = false)
			{
				MyJumpGateConstruct grid = this.JumpGate.JumpGateGrid;

				if (!this.IsValid())
				{
					callback(false);
					return;
				}
				else if (power_mw <= 0)
				{
					callback(true);
					return;
				}

				this.JumpGate.GetWorkingJumpGateDrives(this.JumpGateDrives);
				double power_per_drive = power_mw / this.JumpGateDrives.Count;
				power_mw = (syphon_grid_only) ? power_mw : this.JumpGateDrives.Select((drive) => drive.DrainStoredCharge(power_per_drive)).Sum();

				if (power_mw <= 0)
				{
					callback(true);
					return;
				}

				power_mw = (syphon_grid_only) ? power_mw : grid.SyphonConstructCapacitorPower(power_mw);

				if (power_mw <= 0)
				{
					callback(true);
					return;
				}

				power_per_drive = power_mw / this.JumpGateDrives.Count / ticks;
				foreach (MyJumpGateDrive drive in this.JumpGateDrives) drive.SetWattageSinkOverride(power_per_drive);
				this.SyphonPower = power_mw;
				this.RemainingPower = power_mw;
				this.SyphonTimeTicks = ticks;
				this.WorkingDrivesCount = this.JumpGateDrives.Count;
				this.Callback = callback;
				this.JumpGateDrives.Clear();
			}

			/// <summary>
			/// Ticks this power syphon
			/// </summary>
			public void Tick()
			{
				if (this.SyphonPower <= 0 || !this.IsValid()) return;
				this.JumpGate.GetJumpGateDrives(this.JumpGateDrives);
				this.WorkingDrivesCount = 0;
				double aquired_power = 0;

				foreach (MyJumpGateDrive drive in this.JumpGateDrives)
				{
					if (!drive.IsWorking()) continue;
					aquired_power += drive.GetCurrentWattageSinkInput();
					++this.WorkingDrivesCount;
				}

				this.RemainingPower -= aquired_power;
				--this.SyphonTimeTicks;
				double power_per_Drive = this.RemainingPower / this.WorkingDrivesCount / this.SyphonTimeTicks;
				foreach (MyJumpGateDrive drive in this.JumpGateDrives) if (drive.IsWorking()) drive.SetWattageSinkOverride(power_per_Drive);

				if (this.SyphonTimeTicks == 0)
				{
					foreach (MyJumpGateDrive drive in this.JumpGateDrives) drive.SetWattageSinkOverride(-1);
					this.Callback(this.RemainingPower <= 0);
					this.SyphonPower = -1;
				}

				this.JumpGateDrives.Clear();
			}

			/// <summary>
			/// </summary>
			/// <returns>True if this gate is not null and valid and this gate's grid is not null and valid</returns>
			public bool IsValid()
			{
				MyJumpGateConstruct grid = this.JumpGate.JumpGateGrid;
				return this.JumpGate != null && this.JumpGate.IsValid() && grid != null && grid.IsValid();
			}
			#endregion
		}
		#endregion

		#region Private Variables
		/// <summary>
		/// Wether the jump handler is active
		/// </summary>
		private bool JumpHandlerActive = false;

		/// <summary>
		/// Whether the collider should be forcefully updated on next physics tick
		/// </summary>
		private bool ForceUpdateCollider = false;

		/// <summary>
		/// Whether the jump space ellipse should be forcefully updated on next tick
		/// </summary>
		private bool ForceUpdateJumpEllipsoid = false;

		/// <summary>
		/// Whether this gate has blocks within the shear ellipsoid<br />
		/// Always false on server or singleplayer
		/// </summary>
		private bool HasBlocksWithinShearZone = false;

		/// <summary>
		/// The last time this grid was updated (in timer ticks ~100ns)
		/// </summary>
		private ulong LastUpdateTime = 0;

		/// <summary>
		/// The next available sound emitter ID
		/// </summary>
		private ulong SoundEmitterID = 0;

		/// <summary>
		/// The last name assigned to this jump gate
		/// </summary>
		private string LastStoredName = null;

		/// <summary>
		/// A mutex for exclusive read-write to the intersect nodes list
		/// </summary>
		private object DriveIntersectNodesMutex = new object();

		/// <summary>
		/// The mutex lock for exclusive jump gate updates
		/// </summary>
		private object UpdateLock = new object();

		/// <summary>
		/// The grid size of this gate
		/// </summary>
		private MyCubeSize? DriveGridSize = null;

		/// <summary>
		/// The actual local-aligned bounding ellipsoid of this gate's jump space
		/// </summary>
		private BoundingEllipsoidD TrueLocalJumpEllipse = BoundingEllipsoidD.Zero;

		/// <summary>
		/// The actual world-aligned bounding ellipsoid of this gate's jump space
		/// </summary>
		private BoundingEllipsoidD TrueWorldJumpEllipse = BoundingEllipsoidD.Zero;

		/// <summary>
		/// The old jump node position
		/// </summary>
		private Vector3D OldPosition;

		/// <summary>
		/// The new jump node position
		/// </summary>
		private Vector3D NewPosition;

		/// <summary>
		/// The last node position scan time
		/// </summary>
		private DateTime LastScanTime;

		/// <summary>
		/// The time at which this gate passed an auto activation check
		/// </summary>
		private DateTime? AutoActivateStartTime = null;

		/// <summary>
		/// Notification for autoactivation status
		/// </summary>
		private IMyHudNotification AutoActivationNotif = (MyNetworkInterface.IsDedicatedMultiplayerServer) ? null : MyAPIGateway.Utilities.CreateNotification("");

		/// <summary>
		/// Notification for whether blocks are within shear zone
		/// </summary>
		private IMyHudNotification ShearBlocksWarning = (MyNetworkInterface.IsDedicatedMultiplayerServer) ? null : MyAPIGateway.Utilities.CreateNotification("!Warning! - Blocks detected in Damage Zone", font: "Red");

		/// <summary>
		/// The client-side network handler for jump responses<br />
		/// Always null on server
		/// </summary>
		private Action<MyNetworkInterface.Packet> OnNetworkJumpResponse = null;

		/// <summary>
		/// The client-side network handler for true endpoint updates<br />
		/// Always null on server
		/// </summary>
		private Action<MyNetworkInterface.Packet> OnNetworkUpdateJumpEndpoint = null;

		/// <summary>
		/// The response information from a jump<br />
		/// Always null on server
		/// </summary>
		private JumpGateInfo ServerJumpResponse = null;

		/// <summary>
		/// The jump space collider for this gate
		/// </summary>
		private MyEntity JumpSpaceCollisionDetector = null;

		/// <summary>
		/// The list of intersect nodes for this gate
		/// </summary>
		private List<Vector3D> InnerDriveIntersectNodes = new List<Vector3D>();

		/// <summary>
		/// List of callbacks for entity enter/exit
		/// </summary>
		private List<Action<MyJumpGate, MyEntity, bool>> EntityEnterCallbacks = new List<Action<MyJumpGate, MyEntity, bool>>();

		/// <summary>
		/// List of API callbacks for entity enter/exit
		/// </summary>
		private List<Action<MyAPIJumpGate, MyEntity, bool>> APIEntityEnterCallbacks = new List<Action<MyAPIJumpGate, MyEntity, bool>>();

		/// <summary>
		/// A list of all blocks to be destroyed during jump
		/// </summary>
		private ConcurrentLinkedHashSet<IMySlimBlock> ShearBlocks = new ConcurrentLinkedHashSet<IMySlimBlock>();

		/// <summary>
		/// The syphon for syphoning grid power over some length of time
		/// </summary>
		private GridPowerSyphon PowerSyphon;

		/// <summary>
		/// A list of entities found during pruning call
		/// </summary>
		private List<MyEntity> ColliderPruningList = new List<MyEntity>();

		/// <summary>
		/// Map mapping plane normals to a drive intersect plane
		/// </summary>
		private Dictionary<Vector3D, JumpDriveIntersectPlane> DriveIntersectionPlanes = new Dictionary<Vector3D, JumpDriveIntersectPlane>();

		/// <summary>
		/// The master map of sound emitters bound to this gate
		/// </summary>
		private ConcurrentDictionary<ulong, KeyValuePair<Vector3D?, MySoundEmitter3D>> SoundEmitters = new ConcurrentDictionary<ulong, KeyValuePair<Vector3D?, MySoundEmitter3D>>();

		/// <summary>
		/// List of all entities in this gate's jump space collider
		/// </summary>
		private ConcurrentDictionary<long, MyEntity> JumpSpaceColliderEntities = new ConcurrentDictionary<long, MyEntity>();

		/// <summary>
		/// The master map of entities within this gate's jump space
		/// </summary>
		private ConcurrentDictionary<long, float> JumpSpaceEntities = new ConcurrentDictionary<long, float>();
		#endregion

		#region Public Variables
		/// <summary>
		/// Whether this gate is marked for close
		/// </summary>
		public bool MarkClosed { get; private set; } = false;

		/// <summary>
		/// Whether this gate is closed
		/// </summary>
		public bool Closed { get; private set; } = false;

		/// <summary>
		/// Whether this jump gate should be synced<br/>
		/// If true, will be synced on next component tick
		/// </summary>
		public bool IsDirty = false;

		/// <summary>
		/// The status of this gate
		/// </summary>
		public MyJumpGateStatus Status { get; private set; } = MyJumpGateStatus.NONE;

		/// <summary>
		/// The jump phase of this gate
		/// </summary>
		public MyJumpGatePhase Phase { get; private set; } = MyJumpGatePhase.NONE;

		/// <summary>
		/// The reason this gate's last jump failed or SUCCESS if successfull or NONE if unset and whether failure was during INIT phase
		/// </summary>
		public KeyValuePair<MyJumpFailReason, bool> JumpFailureReason { get; private set; } = new KeyValuePair<MyJumpFailReason, bool>(MyJumpFailReason.NONE, true);

		/// <summary>
		/// This gate's ID
		/// </summary>
		public long JumpGateID = -1;

		/// <summary>
		/// The construct-local coordinate of this gate's jump node (the center of the jump space)
		/// </summary>
		public Vector3D LocalJumpNode;

		/// <summary>
		/// The world coordinate of this gate's jump node (the center of the jump space)
		/// </summary>
		public Vector3D WorldJumpNode
		{
			get
			{
				return MyJumpGateModSession.LocalVectorToWorldVectorP(ref this.ConstructMatrix, this.LocalJumpNode);
			}
			set
			{
				this.LocalJumpNode = MyJumpGateModSession.WorldVectorToLocalVectorP(ref this.ConstructMatrix, value);
			}
		}

		/// <summary>
		/// The true endpoint of this jump gate<br />
		/// Only non-null when this gate is outbound
		/// </summary>
		public Vector3D? TrueEndpoint;

		/// <summary>
		/// A direction vector indicating the velocity of this gate's jump node
		/// </summary>
		public Vector3D JumpNodeVelocity { get; private set; }

		/// <summary>
		/// The PlaneD representing this gate's primary drive plane
		/// </summary>
		public PlaneD? PrimaryDrivePlane { get; private set; }

		/// <summary>
		/// The world matrix of this gate's jump gate grid's main cube grid
		/// </summary>
		public MatrixD ConstructMatrix;

		/// <summary>
		/// The UTC date time representation of MyJumpGate.LastUpdateTime
		/// </summary>
		public DateTime LastUpdateDateTimeUTC
		{
			get
			{
				return (DateTime.MinValue.ToUniversalTime() + new TimeSpan((long) this.LastUpdateTime));
			}

			set
			{
				this.CheckClosed();
				this.LastUpdateTime = (ulong) (value.ToUniversalTime() - DateTime.MinValue).Ticks;
			}
		}

		/// <summary>
		/// The gate configuration variables for this gate
		/// </summary>
		public Configuration.LocalJumpGateConfiguration JumpGateConfiguration { get; private set; }

		/// <summary>
		/// This gate's attached controller or null
		/// </summary>
		public MyJumpGateController Controller = null;

		/// <summary>
		/// This gate's attached server antenna or null
		/// </summary>
		public MyJumpGateServerAntenna ServerAntenna = null;

		/// <summary>
		/// The gate jumping to this one or null
		/// </summary>
		public MyJumpGate SenderGate = null;

		/// <summary>
		/// This gate's MyJumpGateConstruct construct
		/// </summary>
		public MyJumpGateConstruct JumpGateGrid { get; private set; } = null;

		/// <summary>
		/// Gets this gate's world-aligned artificial JumpEllipse using the associated controller
		/// </summary>
		public BoundingEllipsoidD JumpEllipse
		{
			get
			{
				BoundingEllipsoidD world_ellipse = this.TrueWorldJumpEllipse;
				if (this.Controller == null) return world_ellipse;
				world_ellipse.Radii.Y = world_ellipse.Radii.X * this.Controller.BlockSettings.JumpSpaceDepthPercent();
				return world_ellipse;
			}
		}

		/// <summary>
		/// Gets this gate's world-aligned shearing ellipse<br />
		/// This ellipse is used to calculate grid shearing<br />
		/// This ellipse is the jump ellipse padded by 2.5 meters
		/// </summary>
		public BoundingEllipsoidD ShearEllipse
		{
			get
			{
				return this.JumpEllipse + 5;
			}
		}

		/// <summary>
		/// Gets the list of construct-local space drive ray-cast intersections for this jump gate
		/// </summary>
		public ImmutableList<Vector3D> LocalDriveIntersectNodes
		{
			get
			{
				lock (this.DriveIntersectNodesMutex) return this.InnerDriveIntersectNodes.ToImmutableList();
			}
		}

		/// <summary>
		/// Gets the list of world space drive ray-cast intersections for this jump gate
		/// </summary>
		public ImmutableList<Vector3D> WorldDriveIntersectNodes
		{
			get
			{
				lock (this.DriveIntersectNodesMutex) return this.InnerDriveIntersectNodes.Select((node) => MyJumpGateModSession.LocalVectorToWorldVectorP(ref this.ConstructMatrix, node)).ToImmutableList();
			}
		}

		/// <summary>
		/// A map of jump space entities and their children entities
		/// </summary>
		public ConcurrentDictionary<MyEntity, EntityBatch> EntityBatches { get; private set; } = new ConcurrentDictionary<MyEntity, EntityBatch>();
		#endregion

		#region Public Static Operators
		/// <summary>
		/// Overloads equality operator "==" to check equality
		/// </summary>
		/// <param name="a">The first MyJumpGate operand</param>
		/// <param name="b">The second MyJumpGate operand</param>
		/// <returns>Equality</returns>
		public static bool operator ==(MyJumpGate a, MyJumpGate b)
		{
			if (object.ReferenceEquals(a, b)) return true;
			else if (object.ReferenceEquals(a, null)) return object.ReferenceEquals(b, null);
			else if (object.ReferenceEquals(b, null)) return object.ReferenceEquals(a, null);
			else return a.Equals(b);
		}

		/// <summary>
		/// Overloads inequality operator "!=" to check inequality
		/// </summary>
		/// <param name="a">The first MyJumpGate operand</param>
		/// <param name="b">The second MyJumpGate operand</param>
		/// <returns>Inequality</returns>
		public static bool operator !=(MyJumpGate a, MyJumpGate b)
		{
			return !(a == b);
		}
		#endregion

		#region Public Static Methods
		/// <summary>
		/// Gets the associated desription for the specified failure reason
		/// </summary>
		/// <param name="reason">The failure reason</param>
		/// <returns>The associated description or null if no description available</returns>
		public static string GetFailureDescription(MyJumpFailReason reason)
		{
			switch (reason)
			{
				case MyJumpFailReason.SRC_INVALID:
					return "Jump Gate not Valid";
				case MyJumpFailReason.CONTROLLER_NOT_CONNECTED:
					return "Controller not Connected";
				case MyJumpFailReason.SRC_DISABLED:
					return "Jump Gate is Disabled";
				case MyJumpFailReason.SRC_NOT_CONFIGURED:
					return "Jump Gate not Configured";
				case MyJumpFailReason.SRC_BUSY:
					return "Jump Gate Busy";
				case MyJumpFailReason.SRC_ROUTING_DISABLED:
					return "Jump Gate Routing is Disabled";
				case MyJumpFailReason.SRC_INBOUND_ONLY:
					return "Jump Gate is Inbound Only";
				case MyJumpFailReason.SRC_ROUTING_CHANGED:
					return "Jump Gate Routing Changed";
				case MyJumpFailReason.SRC_CLOSED:
					return "Jump Gate Closed";

				case MyJumpFailReason.NULL_DESTINATION:
					return "No Destination Selected";
				case MyJumpFailReason.DESTINATION_UNAVAILABLE:
					return "Destination not available";
				case MyJumpFailReason.NULL_ANIMATION:
					return "No Animation Selected";
				case MyJumpFailReason.SUBSPACE_BUSY:
					return "Subspace Pathways Busy";
				case MyJumpFailReason.RADIO_LINK_FAILED:
				case MyJumpFailReason.BEACON_LINK_FAILED:
					return "Failed to Establish Connection";

				case MyJumpFailReason.JUMP_SPACE_TRANSPOSED:
					return "Jump Space Transposed";
				case MyJumpFailReason.CANCELLED:
					return "Jump Cancelled";
				case MyJumpFailReason.UNKNOWN_ERROR:
					return "Unknown Gate Error";
				case MyJumpFailReason.NO_ENTITIES:
					return "No Entities to Jump";
				case MyJumpFailReason.INSUFFICIENT_POWER:
					return "Insufficient Power";
				case MyJumpFailReason.CROSS_SERVER_JUMP:
					return "Cross Server Jumps Unavailable";

				case MyJumpFailReason.DST_UNAVAILABLE:
					return "Destination Jump Gate not Available";
				case MyJumpFailReason.DST_ROUTING_DISABLED:
					return "Destination Jump Gate Routing is Disabled";
				case MyJumpFailReason.DST_FORBIDDEN:
					return "Destination Jump Gate is Forbidden";
				case MyJumpFailReason.DST_DISABLED:
					return "Destination Jump Gate is Disabled";
				case MyJumpFailReason.DST_NOT_CONFIGURED:
					return "Destination Jump Gate is not Configured";
				case MyJumpFailReason.DST_BUSY:
					return "Destination Jump Gate is Busy";
				case MyJumpFailReason.DST_RADIO_CONNECTION_INTERRUPTED:
					return "Destination Jump Gate Connection Interrupted";
				case MyJumpFailReason.DST_BEACON_CONNECTION_INTERRUPTED:
					return "Destination Beacon Connection Interrupted";
				case MyJumpFailReason.DST_ROUTING_CHANGED:
					return "Destination Jump Gate Routing Changed";
				case MyJumpFailReason.DST_VOIDED:
					return "Destination Jump Gate Voided";
				case MyJumpFailReason.BEACON_BLOCKED:
					return "Destination Beacon Blocked";
				default:
					return null;
			}
		}

		/// <summary>
		/// Gets the associated sound name for the specified failure reason
		/// </summary>
		/// <param name="reason">The failure reason</param>
		/// <param name="is_init_phase">Whether the failure occured during jump initialization</param>
		/// <returns>The associated sound name or null if no sound name available</returns>
		public static string GetFailureSound(MyJumpFailReason reason, bool is_init_phase)
		{
			switch (reason)
			{
				case MyJumpFailReason.SRC_INVALID:
					return (is_init_phase) ? "IOTA.ComputerSFX.InitFailedSourceInvalid" : "IOTA.ComputerSFX.JumpFailedSourceInvalid";
				case MyJumpFailReason.CONTROLLER_NOT_CONNECTED:
					return (is_init_phase) ? "IOTA.ComputerSFX.InitFailedSourceControllerOffline" : "IOTA.ComputerSFX.JumpFailedSourceControllerOffline";
				case MyJumpFailReason.SRC_DISABLED:
					return (is_init_phase) ? "IOTA.ComputerSFX.InitFailedSourceOffline" : "IOTA.ComputerSFX.JumpFailedSourceOffline";
				case MyJumpFailReason.SRC_NOT_CONFIGURED:
					return (is_init_phase) ? "IOTA.ComputerSFX.InitFailedSourceNotConfigured" : "IOTA.ComputerSFX.JumpFailedSourceNotConfigured";
				case MyJumpFailReason.SRC_BUSY:
					return (is_init_phase) ? "IOTA.ComputerSFX.InitFailedSourceBusy" : null;
				case MyJumpFailReason.SRC_ROUTING_DISABLED:
					return (is_init_phase) ? "IOTA.ComputerSFX.InitFailedSourceRoutingOffline" : null;
				case MyJumpFailReason.SRC_INBOUND_ONLY:
					return (is_init_phase) ? "IOTA.ComputerSFX.InitFailedSourceInboundOnly" : null;
				case MyJumpFailReason.SRC_ROUTING_CHANGED:
					return (is_init_phase) ? null : "IOTA.ComputerSFX.JumpFailedSourceRoutingChanged";
				case MyJumpFailReason.SRC_CLOSED:
					return (is_init_phase) ? null : "IOTA.ComputerSFX.JumpFailedSourceClosed";

				case MyJumpFailReason.NULL_DESTINATION:
					return (is_init_phase) ? "IOTA.ComputerSFX.InitFailedNoDestination" : null;
				case MyJumpFailReason.DESTINATION_UNAVAILABLE:
					return (is_init_phase) ? "IOTA.ComputerSFX.InitFailedNullDestination" : "IOTA.ComputerSFX.JumpFailedNullDestination";
				case MyJumpFailReason.NULL_ANIMATION:
					return (is_init_phase) ? "IOTA.ComputerSFX.InitFailedNullAnimation" : null;
				case MyJumpFailReason.SUBSPACE_BUSY:
					return (is_init_phase) ? "IOTA.ComputerSFX.InitFailedSourceSubspaceBusy" : null;
				case MyJumpFailReason.RADIO_LINK_FAILED:
					return (is_init_phase) ? "IOTA.ComputerSFX.InitFailedTargetConnectionFailed" : null;
				case MyJumpFailReason.BEACON_LINK_FAILED:
					return (is_init_phase) ? "IOTA.ComputerSFX.InitFailedTargetBeaconConnectionFailed" : null;
				case MyJumpFailReason.JUMP_SPACE_TRANSPOSED:
					return (is_init_phase) ? "IOTA.ComputerSFX.InitFailedJumpSpaceTransposed" : "IOTA.ComputerSFX.JumpFailedJumpSpaceTransposed";
				case MyJumpFailReason.CANCELLED:
					return (is_init_phase) ? null : "IOTA.ComputerSFX.JumpFailedJumpOverridden";
				case MyJumpFailReason.UNKNOWN_ERROR:
					return (is_init_phase) ? null : "IOTA.ComputerSFX.JumpFailedUnknownError";
				case MyJumpFailReason.NO_ENTITIES:
					return (is_init_phase) ? null : "IOTA.ComputerSFX.JumpFailedNoEntities";
				case MyJumpFailReason.NO_ENTITIES_JUMPED:
					return (is_init_phase) ? null : "IOTA.ComputerSFX.JumpFailedNoEntitiesJumped";
				case MyJumpFailReason.INSUFFICIENT_POWER:
					return (is_init_phase) ? null : "IOTA.ComputerSFX.JumpFailedInsufficientPower";
				case MyJumpFailReason.CROSS_SERVER_JUMP:
					return (is_init_phase) ? null : "IOTA.ComputerSFX.JumpFailedCrossServerUnavailabe";

				case MyJumpFailReason.DST_UNAVAILABLE:
					return (is_init_phase) ? "IOTA.ComputerSFX.InitFailedTargetUnavailable" : "IOTA.ComputerSFX.JumpFailedTargetUnavailable";
				case MyJumpFailReason.DST_ROUTING_DISABLED:
					return (is_init_phase) ? "IOTA.ComputerSFX.InitFailedTargetRoutingOffline" : null;
				case MyJumpFailReason.DST_FORBIDDEN:
					return (is_init_phase) ? "IOTA.ComputerSFX.InitFailedTargetForbidden" : "IOTA.ComputerSFX.JumpFailedTargetForbidden";
				case MyJumpFailReason.DST_DISABLED:
					return (is_init_phase) ? "IOTA.ComputerSFX.InitFailedTargetOffline" : "IOTA.ComputerSFX.JumpFailedTargetOffline";
				case MyJumpFailReason.DST_NOT_CONFIGURED:
					return (is_init_phase) ? "IOTA.ComputerSFX.InitFailedTargetNotConfigured" : "IOTA.ComputerSFX.JumpFailedTargetNotConfigured";
				case MyJumpFailReason.DST_BUSY:
					return (is_init_phase) ? "IOTA.ComputerSFX.InitFailedTargetBusy" : null;
				case MyJumpFailReason.DST_RADIO_CONNECTION_INTERRUPTED:
					return (is_init_phase) ? null : "IOTA.ComputerSFX.JumpFailedTargetConnectionInterrupted";
				case MyJumpFailReason.DST_BEACON_CONNECTION_INTERRUPTED:
					return (is_init_phase) ? null : "IOTA.ComputerSFX.JumpFailedTargetBeaconConnectionInterrupted";
				case MyJumpFailReason.DST_ROUTING_CHANGED:
					return (is_init_phase) ? null : "IOTA.ComputerSFX.JumpFailedTargetRoutingChanged";
				case MyJumpFailReason.DST_VOIDED:
					return (is_init_phase) ? null : "IOTA.ComputerSFX.JumpFailedTargetVoided";
				case MyJumpFailReason.BEACON_BLOCKED:
					return (is_init_phase) ? null : "IOTA.ComputerSFX.JumpFailedTargetBeaconBlocked";
				default:
					return null;
			}
		}
		#endregion

		#region Constructors
		/// <summary>
		/// Creates a new jump gate
		/// </summary>
		/// <param name="grid">The parent construct</param>
		/// <param name="id">The jump gate ID</param>
		/// <param name="jump_node">The jump node world coordinate</param>
		/// <param name="nodes">The drive intersect nodes</param>
		/// <exception cref="ArgumentException"></exception>
		public MyJumpGate(MyJumpGateConstruct grid, long id, Vector3D jump_node, IEnumerable<Vector3D> nodes)
		{
			if (grid == null) throw new ArgumentException("MyJumpGate CONSTRUCTOR[MyJumpGateConstruct grid] was NULL");
			this.ConstructMatrix = grid.CubeGrid.WorldMatrix;
			this.JumpGateGrid = grid;
			this.JumpGateID = id;
			this.LocalJumpNode = MyJumpGateModSession.WorldVectorToLocalVectorP(ref this.ConstructMatrix, jump_node);

			lock (this.DriveIntersectNodesMutex)
			{
				foreach (Vector3D node in nodes)
				{
					bool add = true;
					Vector3D node1 = MyJumpGateModSession.WorldVectorToLocalVectorP(ref this.ConstructMatrix, node);

					foreach (Vector3D node2 in this.InnerDriveIntersectNodes)
					{
						if (Vector3D.Distance(node1, node2) < 1)
						{
							add = false;
							break;
						}
					}

					if (add) this.InnerDriveIntersectNodes.Add(node1);
				}
			}

			Logger.Debug($"CREATE_JUMP_GATE: MyJumpGateConstruct={(this.JumpGateGrid?.CubeGrid?.EntityId.ToString() ?? "N/A")} GRID_CLOSED={(grid?.Closed.ToString() ?? "N/A")} JumpGateID={this.JumpGateID} DriveIntersectNodes={this.InnerDriveIntersectNodes.Count}", 3);

			if (this.JumpGateGrid != null && !grid.Closed && this.JumpGateID >= 0 && this.InnerDriveIntersectNodes.Count >= 1)
			{
				this.Status = MyJumpGateStatus.IDLE;
				this.Phase = MyJumpGatePhase.IDLE;
				this.PowerSyphon = new GridPowerSyphon(this);

				if (MyJumpGateModSession.Network.Registered)
				{
					MyJumpGateModSession.Network.On(MyPacketTypeEnum.JUMP_GATE_JUMP, this.OnNetworkJumpGateJump);
					MyJumpGateModSession.Network.On(MyPacketTypeEnum.UPDATE_JUMP_GATE, this.OnNetworkJumpGateUpdate);
					MyJumpGateModSession.Network.On(MyPacketTypeEnum.AUTOACTIVATE, this.OnNetworkAutoActivationAlert);
					MyJumpGateModSession.Network.On(MyPacketTypeEnum.SHEARWARNING, this.OnNetworkShearBlockAlert);
				}
			}
			else this.Dispose();
		}

		/// <summary>
		/// Creates a new jump gate from a serialized grid
		/// </summary>
		/// <param name="serialized">The serialized jump gate data</param>
		public MyJumpGate(MySerializedJumpGate serialized)
		{
			this.FromSerialized(serialized, true);

			if (this.JumpGateGrid != null && !this.JumpGateGrid.Closed && this.JumpGateID >= 0 && this.InnerDriveIntersectNodes.Count >= 1)
			{
				this.Status = MyJumpGateStatus.IDLE;
				this.Phase = MyJumpGatePhase.IDLE;
				this.PowerSyphon = new GridPowerSyphon(this);

				if (MyJumpGateModSession.Network.Registered)
				{
					MyJumpGateModSession.Network.On(MyPacketTypeEnum.JUMP_GATE_JUMP, this.OnNetworkJumpGateJump);
					MyJumpGateModSession.Network.On(MyPacketTypeEnum.UPDATE_JUMP_GATE, this.OnNetworkJumpGateUpdate);
					MyJumpGateModSession.Network.On(MyPacketTypeEnum.AUTOACTIVATE, this.OnNetworkAutoActivationAlert);
					MyJumpGateModSession.Network.On(MyPacketTypeEnum.SHEARWARNING, this.OnNetworkShearBlockAlert);
				}
			}
			else this.Dispose();
		}
		#endregion

		#region "object" Methods
		/// <summary>
		/// Checks if this MyJumpGate equals another
		/// </summary>
		/// <param name="other">The object to check</param>
		/// <returns>Equality</returns>
		public override bool Equals(object other)
		{
			if (other == null || !(other is MyJumpGate)) return false;
			else return this.Equals((MyJumpGate) other);
		}

		/// <summary>
		/// The hashcode for this object
		/// </summary>
		/// <returns>The hashcode of this object</returns>
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
		#endregion
		
		#region Private Methods
		/// <summary>
		/// Checks if this gate is closed
		/// </summary>
		/// <exception cref="InvalidOperationException">If this gate is closed</exception>
		private void CheckClosed()
		{
			if (this.Closed) throw new InvalidOperationException("Operation on closed jump gate");
		}

		/// <summary>
		/// Callback triggered when this object recevies a jump gate update request from server
		/// </summary>
		/// <param name="packet">The update request packet</param>
		private void OnNetworkJumpGateUpdate(MyNetworkInterface.Packet packet)
		{
			if (packet == null) return;
			MySerializedJumpGate serialized = packet.Payload<MySerializedJumpGate>();
			if (serialized == null || JumpGateUUID.FromGuid(serialized.UUID) != JumpGateUUID.FromJumpGate(this)) return;

			if (MyNetworkInterface.IsMultiplayerServer && packet.PhaseFrame == 1 && packet.EpochTime > this.LastUpdateTime)
			{
				if (packet.EpochTime < this.LastUpdateTime || serialized.IsClientRequest)
				{
					packet = packet.Forward(packet.SenderID, false);
					packet.Payload(this.ToSerialized(false));
					packet.Send();
					Logger.Debug($"CLIENT_GATE_REQUEST -> {this.JumpGateGrid.CubeGridID}-{this.JumpGateID}", 3);
				}
				else if (this.FromSerialized(serialized))
				{
					this.LastUpdateTime = packet.EpochTime;
					this.IsDirty = false;
					packet = packet.Forward(0, true);
					packet.Payload(serialized);
					packet.Send();
					Logger.Debug($"CLIENT_GATE_UPDATE -> {this.JumpGateGrid.CubeGridID}-{this.JumpGateID}", 3);
				}
			}
			else if (MyNetworkInterface.IsMultiplayerClient && (packet.PhaseFrame == 1 || packet.PhaseFrame == 2) && this.FromSerialized(serialized))
			{
				this.LastUpdateTime = packet.EpochTime;
				this.IsDirty = false;
				Logger.Debug($"SERVER_GATE_UPDATE -> {this.JumpGateGrid.CubeGridID}-{this.JumpGateID}", 3);
			}
		}

		/// <summary>
		/// Callback triggered when this object recevies a gate jump request from server
		/// </summary>
		/// <param name="packet">The jump request packet</param>
		private void OnNetworkJumpGateJump(MyNetworkInterface.Packet packet)
		{
			if (packet == null) return;
			JumpGateInfo jump_request = packet.Payload<JumpGateInfo>();
			if (jump_request == null || jump_request.JumpGateID != JumpGateUUID.FromJumpGate(this).ToGuid()) return;

			if (MyNetworkInterface.IsMultiplayerServer && packet.PhaseFrame == 1)
			{
				if (this.Closed || this.MarkClosed)
				{
					packet.Payload(new JumpGateInfo {
						Type = JumpGateInfo.TypeEnum.CLOSED,
						CancelOverride = false,
						JumpGateID = JumpGateUUID.FromJumpGate(this).ToGuid(),
						ResultMessage = "",
					});
					packet.TargetID = packet.SenderID;
					packet.Broadcast = false;
					packet.Send();
					return;
				}
				
				switch (jump_request.Type)
				{
					case JumpGateInfo.TypeEnum.JUMP_START:
						this.JumpWrapper(jump_request.ControllerSettings, null, null, MyJumpTypeEnum.STANDARD);
						break;
					case JumpGateInfo.TypeEnum.JUMP_FAIL:
						if (jump_request.CancelOverride) this.CancelJump();
						break;
				}
			}
			else if (MyNetworkInterface.IsStandaloneMultiplayerClient && packet.PhaseFrame <= 2)
			{
				switch (jump_request.Type)
				{
					case JumpGateInfo.TypeEnum.JUMP_START:
						this.AutoActivationNotif?.Hide();
						this.JumpWrapper(jump_request.ControllerSettings, jump_request.TargetControllerSettings, jump_request.TrueEndpoint, jump_request.JumpType);
						break;
					case JumpGateInfo.TypeEnum.JUMP_FAIL:
						if (!this.JumpHandlerActive) this.SendHudMessage(jump_request.ResultMessage, font: "Red");
						break;
					case JumpGateInfo.TypeEnum.JUMP_SUCCESS:
						if (!this.JumpHandlerActive) this.SendHudMessage(jump_request.ResultMessage);
						break;
				}
			}
		}

		/// <summary>
		/// Callback triggered when server sends auto activation alert
		/// </summary>
		/// <param name="packet">The alert packet</param>
		private void OnNetworkAutoActivationAlert(MyNetworkInterface.Packet packet)
		{
			if (packet == null || MyNetworkInterface.IsServerLike || packet.PhaseFrame != 1) return;
			KeyValuePair<Guid, DateTime?> payload = packet.Payload<KeyValuePair<Guid,  DateTime?>>();
			if (payload.Key != JumpGateUUID.FromJumpGate(this).ToGuid()) return;
			this.AutoActivateStartTime = payload.Value;
		}

		/// <summary>
		/// Callback triggered when server sends block damage alert
		/// </summary>
		/// <param name="packet">The alert packet</param>
		private void OnNetworkShearBlockAlert(MyNetworkInterface.Packet packet)
		{
			if (packet == null || MyNetworkInterface.IsServerLike || packet.PhaseFrame != 1) return;
			KeyValuePair<Guid, int?> payload = packet.Payload<KeyValuePair<Guid, int?>>();
			if (payload.Key != JumpGateUUID.FromJumpGate(this).ToGuid()) return;
			if (this.HasBlocksWithinShearZone = payload.Value.HasValue) this.ShearBlocksWarning.AliveTime = payload.Value.Value;
		}

		/// <summary>
		/// If server: Transmits this jump gate to all clients
		/// </summary>
		private void SendNetworkJumpGateUpdate()
		{
			if (!MyNetworkInterface.IsMultiplayerServer) return;

			MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet {
				PacketType = MyPacketTypeEnum.UPDATE_JUMP_GATE,
				TargetID = 0,
				Broadcast = true,
			};

			packet.Payload(this.ToSerialized(false));
			packet.Send();
			this.IsDirty = false;
			this.LastUpdateTime = packet.EpochTime;
			Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID}: Sent Network Gate Update", 4);
		}

		/// <summary>
		/// Sends a message to this player's hud if within range of this gate or it's controller
		/// </summary>
		/// <param name="msg">The message</param>
		/// <param name="delay">The message duration in ms</param>
		/// <param name="font">The font</param>
		private void SendHudMessage(string msg, int delay = 1000, string font = "White")
		{
			if (MyAPIGateway.Session.LocalHumanPlayer == null) return;
			Vector3D character_pos = MyAPIGateway.Session.LocalHumanPlayer.GetPosition();
			if (!MyNetworkInterface.IsDedicatedMultiplayerServer && character_pos != null && this.IsPointInHudBroadcastRange(ref character_pos)) MyAPIGateway.Utilities.ShowNotification(msg, delay, font);
		}

		/// <summary>
		/// Wrapper which executes the jump animation depending on multiplayer status
		/// </summary>
		/// <param name="controller_settings">The controller settings to use for the jump</param>
		/// <param name="caller">The steam ID of the caller</param>
		private void JumpWrapper(MyJumpGateController.MyControllerBlockSettingsStruct controller_settings, MyJumpGateController.MyControllerBlockSettingsStruct target_controller_settings, Vector3D? client_true_endpoint, MyJumpTypeEnum jump_type)
		{
			Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID}: Jump Wrapper on {(MyNetworkInterface.IsServerLike ? "server" : "client")}", 4);
			if (MyNetworkInterface.IsServerLike) this.ExecuteJumpServer(controller_settings, target_controller_settings);
			else if (MyNetworkInterface.IsMultiplayerClient) this.ExecuteJumpClient(controller_settings, target_controller_settings, client_true_endpoint.Value, jump_type);
		}

		/// <summary>
		/// Executes the gate jump for multiplayer server and singleplayer sessons
		/// </summary>
		/// <param name="controller_settings">The controller settings to use for the jump</param>
		/// <param name="caller">The steam ID of the caller</param>
		private void ExecuteJumpServer(MyJumpGateController.MyControllerBlockSettingsStruct controller_settings, MyJumpGateController.MyControllerBlockSettingsStruct target_controller_settings)
		{
			this.CheckClosed();
			MyJumpGate target_gate = null;
			MyJumpGateAnimation gate_animation = null;
			MyJumpGateWaypoint dst = controller_settings.SelectedWaypoint();
			List<KeyValuePair<MyEntity, bool>> entities_to_jump = null;
			List<MyJumpGate> other_gates = new List<MyJumpGate>();
			Dictionary<MyEntity, float> jump_space_entities = new Dictionary<MyEntity, float>(this.JumpSpaceEntities.Count);
			Func<MyJumpGateDrive, bool> working_src_drive_filter = (drive) => drive.JumpGateID == this.JumpGateID && drive.IsWorking();
			Func<MyJumpGateDrive, bool> working_dst_drive_filter = (drive) => target_gate != null && drive.JumpGateID == target_gate.JumpGateID && drive.IsWorking();
			MyJumpFailReason result = MyJumpFailReason.NONE;
			this.JumpFailureReason = new KeyValuePair<MyJumpFailReason, bool>(MyJumpFailReason.IN_PROGRESS, true);
			bool is_init = true;
			Vector3D jump_node = this.WorldJumpNode;
			Guid this_id = JumpGateUUID.FromJumpGate(this).ToGuid();
			BoundingEllipsoidD jump_ellipse = this.JumpEllipse;

			Action<MyJumpFailReason, bool, string> SendJumpResponse = (reason, result_status, override_message) => {
				this.Status = MyJumpGateStatus.SWITCHING;
				this.Phase = MyJumpGatePhase.RESETING;
				this.JumpFailureReason = new KeyValuePair<MyJumpFailReason, bool>((result_status) ? MyJumpFailReason.SUCCESS : reason, is_init);
				string message = override_message ?? MyJumpGate.GetFailureDescription(this.JumpFailureReason.Key);

				if (target_gate != null)
				{
					target_gate.Status = MyJumpGateStatus.SWITCHING;
					target_gate.Phase = MyJumpGatePhase.RESETING;
				}

				// Is server, broadcast jump results
				if (MyNetworkInterface.IsMultiplayerServer)
				{
					List<EntityWarpInfo> batch_warps = new List<EntityWarpInfo>();
					MyJumpGateModSession.Instance.GetEntityBatchWarpsForGate(this, batch_warps);

					MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet {
						PacketType = MyPacketTypeEnum.JUMP_GATE_JUMP,
						TargetID = 0,
						Broadcast = true,
					};

					packet.Payload(new JumpGateInfo {
						Type = (result_status) ? JumpGateInfo.TypeEnum.JUMP_SUCCESS : JumpGateInfo.TypeEnum.JUMP_FAIL,
						JumpType = MyJumpTypeEnum.STANDARD,
						JumpGateID = JumpGateUUID.FromJumpGate(this).ToGuid(),
						ControllerSettings = controller_settings,
						ResultMessage = message,
						CancelOverride = this.Status == MyJumpGateStatus.CANCELLED,
						EntityBatches = this.EntityBatches.Select((pair) => pair.Value.ToSerialized()).ToList(),
					});
					packet.Send();
				}

				Action<Exception> onend = (error) => {
					gate_animation?.Clean();
					this.TrueEndpoint = null;
					this.EntityBatches.Clear();
					if (target_gate != null && !target_gate.Closed && !target_gate.MarkClosed) target_gate.Reset();
					else if (!this.Closed) this.Reset();
					if (entities_to_jump != null) foreach (KeyValuePair<MyEntity, bool> entity in entities_to_jump) entity.Key.Render.Visible = entity.Value;
				};

				if (gate_animation != null) MyJumpGateModSession.Instance.PlayAnimation(gate_animation, (result_status) ? MyJumpGateAnimation.AnimationTypeEnum.JUMPED : MyJumpGateAnimation.AnimationTypeEnum.FAILED, null, onend);
				else onend(null);
				--MyJumpGateModSession.Instance.ConcurrentJumpsCounter;
				this.SendHudMessage(message, 3000, (result_status) ? "White" : "Red");
				Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} END_JUMP {result_status}/{message}", 3);
			};

			Action<Vector3D> SendUpdatedJumpEndpoint = (new_endpoint) => {
				MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet {
					PacketType = MyPacketTypeEnum.UPDATE_JUMP_ENDPOINT,
					Broadcast = true,
					TargetID = 0,
				};
				packet.Payload(new KeyValuePair<Guid, Vector3D>(this_id, new_endpoint));
				packet.Send();
			};

			try
			{
				// Check this gate
				Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} JUMP_PRECHECK", 3);
				++MyJumpGateModSession.Instance.ConcurrentJumpsCounter;

				if (!this.IsValid()) result = MyJumpFailReason.SRC_INVALID;
				else if (!this.IsComplete() || !this.Controller.IsWorking()) result = MyJumpFailReason.CONTROLLER_NOT_CONNECTED;
				else if (this.JumpGateGrid.GetDriveCount(working_src_drive_filter) < 2) result = MyJumpFailReason.SRC_DISABLED;
				else if (this.Status == MyJumpGateStatus.NONE) result = MyJumpFailReason.SRC_NOT_CONFIGURED;
				else if (!this.IsIdle()) result = MyJumpFailReason.SRC_BUSY;
				else if (!controller_settings.CanBeInbound() && !controller_settings.CanBeOutbound()) result = MyJumpFailReason.SRC_ROUTING_DISABLED;
				else if (!controller_settings.CanBeOutbound()) result = MyJumpFailReason.SRC_INBOUND_ONLY;
				else if (dst == null || dst.WaypointType == MyWaypointType.NONE) result = MyJumpFailReason.NULL_DESTINATION;
				else if (this.JumpGateConfiguration.MaxConcurrentJumps > 0 && MyJumpGateModSession.Instance.ConcurrentJumpsCounter > this.JumpGateConfiguration.MaxConcurrentJumps) result = MyJumpFailReason.SUBSPACE_BUSY;

				// Check target gate if applicable
				if (result == MyJumpFailReason.NONE && dst.WaypointType == MyWaypointType.JUMP_GATE)
				{
					JumpGateUUID uuid = JumpGateUUID.FromGuid(dst.JumpGate);
					MyJumpGateConstruct grid = MyJumpGateModSession.Instance.GetJumpGateGrid(uuid.GetJumpGateGrid());
					target_gate = grid?.GetJumpGate(uuid.GetJumpGate());
					target_controller_settings = target_gate?.Controller.BlockSettings;
					if (MyJumpGateModSession.Configuration.ConstructConfiguration.RequireGridCommLink && !this.JumpGateGrid.IsConstructCommLinked(grid, true)) result = MyJumpFailReason.RADIO_LINK_FAILED;
					else if (target_gate == null || !target_gate.IsComplete() || !target_gate.Controller.IsWorking()) result = MyJumpFailReason.DST_UNAVAILABLE;
					else if (!target_controller_settings.CanBeInbound() && !target_controller_settings.CanBeOutbound()) result = MyJumpFailReason.DST_ROUTING_DISABLED;
					else if (!target_controller_settings.CanBeInbound()) result = MyJumpFailReason.DST_OUTBOUND_ONLY;
					else if (!target_gate.Controller.IsFactionRelationValid(this.Controller.OwnerSteamID)) result = MyJumpFailReason.DST_FORBIDDEN;
					else if (target_gate.JumpGateGrid.GetDriveCount(working_dst_drive_filter) < 2) result = MyJumpFailReason.DST_DISABLED;
					else if (target_gate.Status == MyJumpGateStatus.NONE) result = MyJumpFailReason.DST_NOT_CONFIGURED;
					else if (!target_gate.IsIdle()) result = MyJumpFailReason.DST_BUSY;
					else target_gate.ConnectAsInbound(this);
					if (result != MyJumpFailReason.NONE) target_gate = null;
				}

				// Check target beacon if applicable
				else if (result == MyJumpFailReason.NONE && dst.WaypointType == MyWaypointType.BEACON && (dst.Beacon.Beacon == null || !dst.Beacon.Beacon.IsWorking || !this.JumpGateGrid.IsBeaconWithinReverseBroadcastSphere(dst.Beacon, true)))
				{
					result = MyJumpFailReason.BEACON_LINK_FAILED;
				}

				// Check gate overlap
				if (result == MyJumpFailReason.NONE)
				{
					MyJumpGateModSession.Instance.GetAllJumpGates(other_gates);

					foreach (MyJumpGate other_gate in other_gates)
					{
						if (other_gate != this && (other_gate.Status == MyJumpGateStatus.OUTBOUND || other_gate == target_gate) && jump_ellipse.Intersects(other_gate.JumpEllipse))
						{
							result = MyJumpFailReason.JUMP_SPACE_TRANSPOSED;
							break;
						}
					}

					other_gates.Clear();
				}

				// Send jump-failure if failed
				if (result != MyJumpFailReason.NONE)
				{
					SendJumpResponse(result, false, null);
					return;
				}

				// Setup endpoint
				Vector3D? _endpoint = dst.GetEndpoint();

				if (_endpoint == null)
				{
					SendJumpResponse(MyJumpFailReason.DESTINATION_UNAVAILABLE, false, null);
					return;
				}

				Vector3D endpoint = _endpoint.Value;
				Vector3D default_endpoint = endpoint;
				double distance_to_endpoint = Vector3D.Distance(endpoint, jump_node);
				this.TrueEndpoint = endpoint;

				// Setup gate animation
				string gate_animation_name = controller_settings.JumpEffectAnimationName();
				gate_animation = MyAnimationHandler.GetAnimation(gate_animation_name, this, target_gate, controller_settings, target_controller_settings, ref endpoint, MyJumpTypeEnum.STANDARD);

				if (gate_animation == null)
				{
					SendJumpResponse(MyJumpFailReason.NULL_ANIMATION, false, null);
					return;
				}

				// Final setup
				double tick_duration = 1000d / 60d;
				double time_to_jump_ms = (gate_animation == null) ? 0 : gate_animation.Durations()[0] * tick_duration;
				IMyHudNotification hud_notification = MyAPIGateway.Utilities.CreateNotification("Gate Activation in --.-s", (int) Math.Round(time_to_jump_ms), "White");
				IMyHudNotification cancel_request_notificiation = null;

				if (!MyNetworkInterface.IsDedicatedMultiplayerServer)
				{
					hud_notification.Show();
					this.ShearBlocksWarning.AliveTime = (int) Math.Round(time_to_jump_ms);
				}

				// If server, broadcast jump start
				if (MyNetworkInterface.IsMultiplayerServer)
				{
					MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet
					{
						PacketType = MyPacketTypeEnum.JUMP_GATE_JUMP,
						TargetID = 0,
						Broadcast = true,
					};

					packet.Payload(new JumpGateInfo {
						Type = JumpGateInfo.TypeEnum.JUMP_START,
						JumpGateID = JumpGateUUID.FromJumpGate(this).ToGuid(),
						ControllerSettings = controller_settings,
						TargetControllerSettings = target_controller_settings,
						TrueEndpoint = endpoint,
					});

					packet.Send();
				}

				// Update gate status
				this.Status = MyJumpGateStatus.OUTBOUND;
				this.Phase = MyJumpGatePhase.CHARGING;
				is_init = false;
				MyJumpGateModSession.Instance.RedrawAllTerminalControls();
				Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} JUMP_CHARGE", 3	);

				// Play animation and check for invalidation of jump event
				MyJumpGateModSession.Instance.PlayAnimation(gate_animation, MyJumpGateAnimation.AnimationTypeEnum.JUMPING, () => {
					if (!this.IsValid()) result = MyJumpFailReason.SRC_INVALID;
					else if (!this.IsComplete() || !this.Controller.IsWorking()) result = MyJumpFailReason.CONTROLLER_NOT_CONNECTED;
					else if (this.JumpGateGrid.GetDriveCount(working_src_drive_filter) < 2) result = MyJumpFailReason.SRC_DISABLED;
					else if (this.Status == MyJumpGateStatus.NONE) result = MyJumpFailReason.SRC_NOT_CONFIGURED;
					else if (!controller_settings.CanBeInbound() && !controller_settings.CanBeOutbound()) result = MyJumpFailReason.SRC_ROUTING_CHANGED;
					else if (!controller_settings.CanBeOutbound()) result = MyJumpFailReason.SRC_ROUTING_CHANGED;
					else if (dst == null || dst.WaypointType == MyWaypointType.NONE) result = MyJumpFailReason.NULL_DESTINATION;
					else if (target_gate != null)
					{
						JumpGateUUID uuid = JumpGateUUID.FromGuid(dst.JumpGate);
						MyJumpGateConstruct grid = MyJumpGateModSession.Instance.GetJumpGateGrid(uuid.GetJumpGateGrid());
						if (MyJumpGateModSession.Configuration.ConstructConfiguration.RequireGridCommLink && !this.JumpGateGrid.IsConstructCommLinked(grid, true)) result = MyJumpFailReason.DST_RADIO_CONNECTION_INTERRUPTED;
						else if (target_gate.Closed || !target_gate.IsComplete() || !target_gate.Controller.IsWorking()) result = MyJumpFailReason.DST_UNAVAILABLE;
						else if (!target_controller_settings.CanBeInbound()) result = MyJumpFailReason.DST_ROUTING_CHANGED;
						else if (!target_gate.Controller.IsFactionRelationValid(this.Controller.OwnerID)) result = MyJumpFailReason.DST_FORBIDDEN;
						else if (target_gate.JumpGateGrid.GetDriveCount(working_dst_drive_filter) < 2) result = MyJumpFailReason.DST_DISABLED;
						else if (target_gate.Status == MyJumpGateStatus.NONE) result = MyJumpFailReason.DST_NOT_CONFIGURED;
						else if (target_gate.SenderGate != this) result = MyJumpFailReason.DST_VOIDED;
						else
						{
							endpoint = target_gate.WorldJumpNode;
							if (MyJumpGateModSession.Network.Registered && endpoint != this.TrueEndpoint) SendUpdatedJumpEndpoint(endpoint);
							this.TrueEndpoint = endpoint;
						}
					}
					else if (dst.WaypointType == MyWaypointType.BEACON)
					{
						if (dst.Beacon.Beacon == null || !dst.Beacon.Beacon.IsWorking || !this.JumpGateGrid.IsBeaconWithinReverseBroadcastSphere(dst.Beacon, true)) result = MyJumpFailReason.DST_BEACON_CONNECTION_INTERRUPTED;
						endpoint = dst.Beacon.BeaconPosition;
						if (MyJumpGateModSession.Network.Registered && endpoint != this.TrueEndpoint) SendUpdatedJumpEndpoint(endpoint);
						this.TrueEndpoint = endpoint;
					}

					if (MyJumpGateModSession.GameTick % 30 == 0)
					{
						MyJumpGateModSession.Instance.GetAllJumpGates(other_gates);

						foreach (MyJumpGate other_gate in other_gates)
						{
							if (other_gate != this && other_gate.Status == MyJumpGateStatus.OUTBOUND && jump_ellipse.Intersects(other_gate.JumpEllipse))
							{
								result = MyJumpFailReason.JUMP_SPACE_TRANSPOSED;
								break;
							}
						}

						other_gates.Clear();
					}

					time_to_jump_ms -= tick_duration;
					hud_notification.Text = $"Gate Activation in {MyJumpGateModSession.AutoconvertMetricUnits(Math.Max(0, time_to_jump_ms / 1000d), "s", 1)}";
					hud_notification.Hide();
					cancel_request_notificiation?.Hide();
					Vector3D character_pos = MyAPIGateway.Session.LocalHumanPlayer?.GetPosition() ?? Vector3D.PositiveInfinity;
					bool show_notifs = character_pos != null && this.IsPointInHudBroadcastRange(ref character_pos);

					if (show_notifs)
					{
						hud_notification.Show();
						cancel_request_notificiation?.Show();
						if (this.ShearBlocks.Count == 0) this.ShearBlocksWarning?.Hide();
						else this.ShearBlocksWarning.Show();
					}

					if (this.Closed || this.JumpGateGrid == null || this.JumpGateGrid.Closed) result = MyJumpFailReason.SRC_CLOSED;
					else if (this.Status == MyJumpGateStatus.CANCELLED && gate_animation.ImmediateCancel) result = MyJumpFailReason.CANCELLED;
					else if (this.Status == MyJumpGateStatus.CANCELLED && cancel_request_notificiation == null && show_notifs)
					{
						cancel_request_notificiation = MyAPIGateway.Utilities.CreateNotification("Jump Cancelling...", (int) Math.Round(time_to_jump_ms), "Red");
						cancel_request_notificiation.Show();
					}

					List<MyJumpGateDrive> working_drives = new List<MyJumpGateDrive>();
					this.GetWorkingJumpGateDrives(working_drives);

					foreach (MyJumpGateDrive drive in working_drives)
					{
						if (drive.IsNullWrapper) continue;
						Vector3D force_dir = (drive.TerminalBlock.WorldMatrix.Translation - this.WorldJumpNode).Normalized() * this.JumpGateConfiguration.ChargingDriveEffectorForceN;
						drive.TerminalBlock.GetTopMostParent().Physics.AddForce(VRage.Game.Components.MyPhysicsForceType.APPLY_WORLD_FORCE, force_dir, drive.TerminalBlock.WorldMatrix.Translation, null);
					}

					return result == MyJumpFailReason.NONE;
				}, (Exception err) => {

					try
					{
						// Confirm no errors occured
						Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} JUMP_POST_CHECK", 3);
						if (err != null) result = MyJumpFailReason.UNKNOWN_ERROR;
						else if (this.Status == MyJumpGateStatus.CANCELLED) result = MyJumpFailReason.CANCELLED;
						hud_notification.Hide();
						cancel_request_notificiation?.Hide();
						this.ShearBlocksWarning?.Hide();

						if (result != MyJumpFailReason.NONE)
						{
							SendJumpResponse(result, false, null);
							return;
						}

						this.Phase = MyJumpGatePhase.JUMPING;
						this.GetEntitiesInJumpSpace(jump_space_entities, true);
						entities_to_jump = jump_space_entities.Keys.Select((entity) => new KeyValuePair<MyEntity, bool>(entity, entity.Render.Visible)).ToList();
						jump_space_entities.Clear();
						Random prng = new Random();

						if (entities_to_jump.Count == 0)
						{
							SendJumpResponse(MyJumpFailReason.NO_ENTITIES, false, null);
							return;
						}

						// Find available beacon jump point
						if (dst.WaypointType == MyWaypointType.BEACON)
						{
							BoundingSphereD endsphere = new BoundingSphereD(endpoint, Math.Max(jump_ellipse.Radii.Max() * 2, 1000));
							BoundingSphereD largest_sphere = new BoundingSphereD(jump_node, jump_ellipse.Radii.Max());
							List<MyEntity> topmost = new List<MyEntity>();
							bool found = false;

							for (int i = 0; i < 10; ++i)
							{
								double rand1 = prng.NextDouble();
								double rand2 = prng.NextDouble();
								double rand3 = prng.NextDouble();
								largest_sphere.Center = endsphere.RandomToUniformPointInSphere(rand1, rand2, rand3);
								MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref largest_sphere, topmost);
								
								if (!topmost.Any((entity) => entity is MyCubeGrid))
								{
									topmost.Clear();
									endpoint = largest_sphere.Center;
									found = true;
									break;
								}
								
								topmost.Clear();
							}

							if (!found)
							{
								SendJumpResponse(MyJumpFailReason.BEACON_BLOCKED, false, null);
								return;
							}
						}

						// Apply Randomness to endpoint if untethered
						if (!MyJumpGateModSession.Configuration.GeneralConfiguration.SafeJumps && target_gate == null)
						{
							// Bend jump path around gravity
							byte distance_multiplier = 1;
							uint per_meter = 1000u * distance_multiplier;
							ulong segments = (ulong) Math.Round(distance_to_endpoint / per_meter);
							Vector3D direction = (endpoint - jump_node).Normalized() * per_meter;
							Vector3D startpos = jump_node;

							for (ulong i = 0; i < segments; ++i)
							{
								float gravity_strength;
								Vector3D gravity_direction = MyAPIGateway.GravityProviderSystem.CalculateNaturalGravityInPoint(startpos, out gravity_strength);
								double g_effector = this.JumpGateConfiguration.GateKilometerOffsetPerUnitG * gravity_strength * distance_multiplier;
								direction += gravity_direction * g_effector;
								startpos += direction;
							}

							distance_to_endpoint = Vector3D.Distance(startpos, jump_node);

							// Apply random offset to endpoint
							if (this.JumpGateConfiguration.ConfineUntetheredSpread)
							{
								double max_offset = distance_to_endpoint * this.JumpGateConfiguration.GateRandomOffsetPerKilometer;
								double rand1 = prng.NextDouble();
								double rand2 = prng.NextDouble();
								double rand3 = prng.NextDouble();
								endpoint = new BoundingSphereD(startpos, max_offset).RandomToUniformPointInSphere(rand1, rand2, rand3);
								distance_to_endpoint = Vector3D.Distance(endpoint, jump_node);
								Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} RANDOM_ENDPOINT_OFFSET - OLD={_endpoint.Value}; NEW={endpoint}; OFFSET={Vector3D.Distance(endpoint, _endpoint.Value)}", 5);
							}
							else
							{
								endpoint = startpos;
								distance_to_endpoint = Vector3D.Distance(endpoint, jump_node);
							}
						}

						// Jump entities
						if (dst.WaypointType == MyWaypointType.SERVER)
						{
							SendJumpResponse(MyJumpFailReason.CROSS_SERVER_JUMP, false, null);
							return;
						}

						ushort jump_duration = gate_animation?.GateJumpedAnimationDef?.BeamPulse?.TravelTime ?? gate_animation?.GateJumpedAnimationDef?.TravelTime ?? gate_animation?.GateJumpedAnimationDef?.Duration ?? 0;
						int skipped_entities_count = 0;
						double syphon_power = 0;
						MatrixD this_matrix = this.TrueWorldJumpEllipse.WorldMatrix;
						MatrixD target_matrix = target_gate?.TrueWorldJumpEllipse.WorldMatrix ?? MatrixD.CreateWorld(endpoint, this_matrix.Forward, this_matrix.Up);
						Vector3D src_gate_velocity = this.JumpNodeVelocity;
						Vector3D dst_gate_velocity = (target_gate == null) ? Vector3D.Zero : target_gate.JumpNodeVelocity;

						List<MyEntity> obstructing = new List<MyEntity>();
						List<MyEntity> syphon_entities = new List<MyEntity>();
						List<MyVoxelBase> terrain = new List<MyVoxelBase>();
						List<IMySlimBlock> destroyed = new List<IMySlimBlock>();
						List<IMyCubeGrid> grids = new List<IMyCubeGrid>();

						Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} JUMP_ENTITY_TRANSIT - Assembling batches...", 3);

						// Assemble batches
						foreach (KeyValuePair<MyEntity, bool> pair in entities_to_jump)
						{
							MyEntity entity = pair.Key;
							List<MyEntity> batch = new List<MyEntity>() { entity };
							MatrixD entity_target_matrix;
							BoundingBoxD obstruction_aabb;
							MyJumpGateConstruct parent = (entity is MyCubeGrid) ? MyJumpGateModSession.Instance.GetUnclosedJumpGateGrid(entity.EntityId) : null;
							entity = (parent == null) ? entity : (MyCubeGrid) parent.CubeGrid;
							Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} BATCHING - BATCH_PARENT={entity.EntityId}; ISGRID={parent != null}", 5);

							// Calculate end position
							Vector3D position = MyJumpGateModSession.WorldVectorToLocalVectorP(ref this_matrix, entity.WorldMatrix.Translation);
							position = MyJumpGateModSession.LocalVectorToWorldVectorP(ref target_matrix, position);
							Vector3D forward_dir = MyJumpGateModSession.WorldVectorToLocalVectorD(ref this_matrix, entity.WorldMatrix.Forward);
							forward_dir = MyJumpGateModSession.LocalVectorToWorldVectorD(ref target_matrix, forward_dir);
							Vector3D up_dir = MyJumpGateModSession.WorldVectorToLocalVectorD(ref this_matrix, entity.WorldMatrix.Up);
							up_dir = MyJumpGateModSession.LocalVectorToWorldVectorD(ref target_matrix, up_dir);
							MatrixD.CreateWorld(ref position, ref forward_dir, ref up_dir, out entity_target_matrix);

							// Apply randomness to end position of applicable
							if (target_gate == null && !this.JumpGateConfiguration.ConfineUntetheredSpread)
							{
								double max_offset = distance_to_endpoint * this.JumpGateConfiguration.GateRandomOffsetPerKilometer;
								double rand1 = prng.NextDouble();
								double rand2 = prng.NextDouble();
								double rand3 = prng.NextDouble();
								entity_target_matrix.Translation = position = new BoundingSphereD(endpoint, max_offset).RandomToUniformPointInSphere(rand1, rand2, rand3);
								Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... RANDOM_ENDPOINT_OFFSET_LOCAL @ {entity.EntityId} - OLD={endpoint}; NEW={position}; OFFSET={Vector3D.Distance(position, endpoint)}", 5);
							}

							// Construct specific checks
							if (parent == this.JumpGateGrid) continue;
							else if (parent != null)
							{
								// Check cube grid obstructions
								BoundingBoxD construct_aabb = parent.GetCombinedAABB();
								obstruction_aabb = construct_aabb;
								obstruction_aabb.TransformFast(ref entity_target_matrix, ref obstruction_aabb);
								MyGamePruningStructure.GetTopmostEntitiesInBox(ref obstruction_aabb, obstructing);

								obstructing.RemoveAll((obstruct) => {
									if (!(obstruct is MyCubeGrid)) return true;
									MyJumpGateConstruct obgrid = MyJumpGateModSession.Instance.GetJumpGateGrid(obstruct.EntityId);
									if (obgrid == null || obgrid.MarkClosed) return true;
									IEnumerator<IMySlimBlock> enumerator1 = parent.GetConstructBlocks();
									IEnumerator<IMySlimBlock> enumerator2 = obgrid.GetConstructBlocks();

									while (enumerator1.MoveNext())
									{
										IMySlimBlock block1 = enumerator1.Current;
										Vector3D block_pos_1 = block1.CubeGrid.GridIntegerToWorld(block1.Position);
										block_pos_1 = MyJumpGateModSession.WorldVectorToLocalVectorP(ref this_matrix, block_pos_1);
										block_pos_1 = MyJumpGateModSession.LocalVectorToWorldVectorP(ref target_matrix, block_pos_1);

										while (enumerator2.MoveNext())
										{
											IMySlimBlock block2 = enumerator2.Current;
											BoundingBoxD block2_bounds;
											block2.GetWorldBoundingBox(out block2_bounds);
											if (block2_bounds.Contains(block_pos_1) != ContainmentType.Disjoint) return false;
										}

										enumerator2.Reset();
									}

									return true;
								});

								if (obstructing.Count > 0)
								{
									++skipped_entities_count;
									obstructing.Clear();
									Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... BATCH_SKIP_FAIL_OBSTRUCTED_CONSTRUCT", 4);
									continue;
								}

								obstructing.Clear();

								// Gather contained child entities
								MyGamePruningStructure.GetTopmostEntitiesInBox(ref construct_aabb, obstructing);
								MyJumpGateConstruct subparent;

								foreach (MyEntity sub_entity in obstructing)
								{
									subparent = (sub_entity is MyCubeGrid) ? MyJumpGateModSession.Instance.GetUnclosedJumpGateGrid(sub_entity.EntityId) : null;
									if (sub_entity == entity || subparent == parent || parent.IsPositionInsideAnySubgrid(sub_entity.WorldMatrix.Translation) == null) continue;
									batch.Add(sub_entity);
									Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... ... BATCH_CHILD={sub_entity.EntityId}", 4);
								}

								obstructing.Clear();
							}
							else
							{
								obstruction_aabb = ((IMyEntity) entity).WorldAABB;
								obstruction_aabb.TransformFast(ref entity_target_matrix, ref obstruction_aabb);
							}

							// Merge batches
							List<MyEntity> mergers = new List<MyEntity>();
							int i, j;

							for (i = 0, j = batch.Count; i < j; ++i)
							{
								MyEntity child = batch[i];
								EntityBatch child_batch = this.EntityBatches.GetValueOrDefault(child, null);

								if (child_batch != null)
								{
									batch.AddRange(child_batch.Batch.Where((child_) => !batch.Contains(child_)));
									this.EntityBatches.Remove(child);
									Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... BATCH_MERGED - {child.EntityId} --> {entity.EntityId}", 4);
								}

								foreach (KeyValuePair<MyEntity, EntityBatch> batch_pair in this.EntityBatches)
								{
									if (!batch_pair.Value.Batch.Contains(child)) continue;
									mergers.Add(batch_pair.Key);
									break;
								}
							}

							if (mergers.Count == 1)
							{
								EntityBatch target = this.EntityBatches[mergers[0]];
								target.Batch.AddRange(batch.Where((child) => !target.Batch.Contains(child)));
								BoundingBoxD.CreateMerged(ref target.ObstructAABB, ref obstruction_aabb, out target.ObstructAABB);
								Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... BATCH_MERGED - {entity.EntityId} --> {target.Parent.EntityId}", 4);
							}
							else
							{
								foreach (MyEntity merger in mergers) batch.RemoveAll(this.EntityBatches[merger].Batch.Contains);
								this.EntityBatches[entity] = new EntityBatch(batch, ref obstruction_aabb, ref entity_target_matrix);
								Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... BATCH_ADDED_{entity.EntityId}", 4);
							}
						}

						// Teleport batches
						Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} JUMP_ENTITY_TRANSIT - Teleporting batches...", 3);

						foreach (KeyValuePair<MyEntity, EntityBatch> pair in this.EntityBatches)
						{
							EntityBatch batch = pair.Value;
							MyEntity entity = pair.Key;
							MyJumpGateConstruct parent = (entity is MyCubeGrid) ? MyJumpGateModSession.Instance.GetUnclosedJumpGateGrid(entity.EntityId) : null;
							Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} TELEPORTING - BATCH_PARENT={entity.EntityId}; ISGRID={parent != null}; BATCH_SIZE={batch.Batch.Count}", 4);

							// Check terrain obstructions
							MyGamePruningStructure.GetAllVoxelMapsInBox(ref batch.ObstructAABB, terrain);

							if (terrain.Any((voxel) => voxel.GetIntersectionWithAABB(ref batch.ObstructAABB)))
							{
								++skipped_entities_count;
								terrain.Clear();
								Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... TP_FAIL_OBSTRUCTED_TERRAIN", 4);
								continue;
							}

							terrain.Clear();

							// Destroy neccessary construct blocks
							if (parent != null)
							{
								parent.GetCubeGrids(grids);

								foreach (IMyCubeGrid grid in grids)
								{
									foreach (IMyShipConnector connector in grid.GetFatBlocks<IMyShipConnector>())
									{
										if (connector.IsWorking && connector.Status == Sandbox.ModAPI.Ingame.MyShipConnectorStatus.Connected && this.JumpGateGrid.HasCubeGrid(connector.OtherConnector.CubeGrid))
										{
											destroyed.Add(connector.SlimBlock);
										}
									}

									foreach (IMyLandingGear gear in grid.GetFatBlocks<IMyLandingGear>())
									{
										IMyEntity attached_entity;
										if (gear.IsWorking && gear.IsLocked && (attached_entity = gear.GetAttachedEntity()) is IMyCubeGrid && this.JumpGateGrid.HasCubeGrid(attached_entity as IMyCubeGrid))
										{
											destroyed.Add(gear.SlimBlock);
										}
									}

									if (destroyed.Count > 0 && this.JumpGateConfiguration.IgnoreDockedGrids)
									{
										++skipped_entities_count;
										grids.Clear();
										destroyed.Clear();
										Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... TP_FAIL_DOCKED", 4);
										continue;
									}
									else
									{
										foreach (IMySlimBlock block in destroyed) grid.RemoveDestroyedBlock(block);
									}

									destroyed.Clear();

									// Shear grid
									if (!MyJumpGateModSession.Configuration.GeneralConfiguration.LenientJumps)
									{
										MyCubeGrid cubegrid = entity as MyCubeGrid;
										foreach (IMySlimBlock block in this.ShearBlocks) if (block.CubeGrid == grid) grid.RemoveDestroyedBlock(block);
										destroyed.AddRange(cubegrid.CubeBlocks.Where((block) => !jump_ellipse.IsPointInEllipse(grid.GridIntegerToWorld(((IMySlimBlock) block).Position))));
										parent.SplitGrid(grid, destroyed);
										destroyed.Clear();
									}
								}

								grids.Clear();
							}

							// Calculate power
							double batch_mass = batch.BatchMass;
							double available_power = this.CalculateTotalAvailableInstantPower();
							double power_scaler = (target_gate == null) ? this.JumpGateConfiguration.UntetheredJumpEnergyCost : 1;
							double required_power = Math.Round(batch_mass * this.CalculateUnitPowerRequiredForJump(ref default_endpoint), 8) * power_scaler / 1000;
							Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... INSTANT_POWER_SYPHON - BATCH_MASS={batch_mass} Kg; AVAILABLE_INSTANT_POWER={available_power} MW", 4);
							Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... ... INITIAL - REQUIRED_POWER={required_power} Mw", 4);

							if (available_power < required_power)
							{
								syphon_entities.Add(entity);
								syphon_power += required_power;
								Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... TP_DEFER_INSUFFICIENT_POWER", 4);
								continue;
							}

							required_power = Math.Round(this.SyphonGridDrivePower(required_power), 8);
							Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... ... POST_DRIVES - REQUIRED_POWER={required_power} Mw", 4);
							if (required_power > 0) required_power = Math.Round(this.JumpGateGrid.SyphonConstructCapacitorPower(required_power), 8);
							Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... ... POST_CAPACITORS - REQUIRED_POWER={required_power} Mw", 4);

							if (required_power > 0)
							{
								syphon_entities.Add(entity);
								syphon_power += required_power;
								Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... TP_DEFER_INSUFFICIENT_POWER", 4);
								continue;
							}

							// Teleport batch
							if (jump_duration == 0)
							{
								foreach (MyEntity child in batch.Batch)
								{
									// Calculate end position and velocity
									child.Teleport(batch.ParentTargetMatrix);
									Vector3D velocity = MyJumpGateModSession.WorldVectorToLocalVectorD(ref this_matrix, child.Physics.LinearVelocity);
									child.Physics.LinearVelocity = MyJumpGateModSession.LocalVectorToWorldVectorD(ref target_matrix, velocity);
									Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... ... TELEPORT_CHILD_{child.EntityId} - ENDPOINT={batch.ParentTargetMatrix.Translation}", 5);
								}

								Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... TP_SUCCESS", 4);
							}
							else
							{
								// Calculate end position and velocity
								MatrixD entity_matrix = entity.WorldMatrix;
								Vector3D velocity = MyJumpGateModSession.WorldVectorToLocalVectorD(ref this_matrix, entity.Physics.LinearVelocity);
								velocity = MyJumpGateModSession.LocalVectorToWorldVectorD(ref target_matrix, velocity);
								MyJumpGateModSession.Instance.WarpEntityBatchOverTime(this, batch.Batch, ref entity_matrix, ref batch.ParentTargetMatrix, jump_duration, (batch_) => { foreach (MyEntity child in batch_) if (child != null && !child.MarkedForClose) child.Physics.LinearVelocity = velocity; });
								Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... TP_SUCCESS_BATCH_{entity.EntityId} - ENDPOINT={batch.ParentTargetMatrix.Translation}", 4);
							}
						}

						// Check power syphon availability
						if (!MyJumpGateModSession.Configuration.GeneralConfiguration.SyphonReactorPower && syphon_entities.Count == entities_to_jump.Count)
						{
							SendJumpResponse(MyJumpFailReason.INSUFFICIENT_POWER, false, null);
							return;
						}
						else if (!MyJumpGateModSession.Configuration.GeneralConfiguration.SyphonReactorPower && syphon_entities.Count > 0)
						{
							int final_count = entities_to_jump.Count - skipped_entities_count - syphon_entities.Count;
							SendJumpResponse((final_count > 0) ? MyJumpFailReason.SUCCESS : MyJumpFailReason.NO_ENTITIES_JUMPED, final_count > 0, $"Jumped {final_count}/{entities_to_jump.Count} entities");
							return;
						}

						// Handle power syphon
						Action<bool> power_syphon_callback = (power_syphoned) => {
							if (!power_syphoned && syphon_entities.Count == entities_to_jump.Count)
							{
								SendJumpResponse(MyJumpFailReason.INSUFFICIENT_POWER, false, null);
								return;
							}
							else if (!power_syphoned)
							{
								int final_count = entities_to_jump.Count - skipped_entities_count - syphon_entities.Count;
								SendJumpResponse((final_count > 0) ? MyJumpFailReason.SUCCESS : MyJumpFailReason.NO_ENTITIES_JUMPED, final_count > 0, $"Jumped {final_count}/{entities_to_jump.Count} entities");
								return;
							}

							// Recheck syphon entities
							Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} JUMP_ENTITY_TRANSIT - Reevaluate syphon batches...", 3);

							foreach (MyEntity entity in syphon_entities)
							{
								EntityBatch batch = this.EntityBatches[entity];
								MyJumpGateConstruct parent = (entity is MyCubeGrid) ? MyJumpGateModSession.Instance.GetUnclosedJumpGateGrid(entity.EntityId) : null;
								Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} TELEPORTING - BATCH_PARENT={entity.EntityId}; ISGRID={parent != null}", 5);

								// Construct specific checks
								if (parent != null)
								{
									// Check cube grid obstructions
									MyGamePruningStructure.GetTopmostEntitiesInBox(ref batch.ObstructAABB, obstructing);

									obstructing.RemoveAll((obstruct) => {
										if (!(obstruct is MyCubeGrid)) return true;
										MyJumpGateConstruct obgrid = MyJumpGateModSession.Instance.GetJumpGateGrid(obstruct.EntityId);
										if (obgrid == null || obgrid.MarkClosed) return true;
										IEnumerator<IMySlimBlock> enumerator1 = parent.GetConstructBlocks();
										IEnumerator<IMySlimBlock> enumerator2 = obgrid.GetConstructBlocks();

										while (enumerator1.MoveNext())
										{
											IMySlimBlock block1 = enumerator1.Current;
											Vector3D block_pos_1 = block1.CubeGrid.GridIntegerToWorld(block1.Position);
											block_pos_1 = MyJumpGateModSession.WorldVectorToLocalVectorP(ref this_matrix, block_pos_1);
											block_pos_1 = MyJumpGateModSession.LocalVectorToWorldVectorP(ref target_matrix, block_pos_1);

											while (enumerator2.MoveNext())
											{
												IMySlimBlock block2 = enumerator2.Current;
												BoundingBoxD block2_bounds;
												block2.GetWorldBoundingBox(out block2_bounds);
												if (block2_bounds.Contains(block_pos_1) != ContainmentType.Disjoint) return false;

											}

											enumerator2.Reset();
										}

										return true;
									});

									if (obstructing.Count > 0)
									{
										++skipped_entities_count;
										obstructing.Clear();
										Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... BATCH_SKIP_FAIL_OBSTRUCTED_CONSTRUCT", 4);
										continue;
									}

									obstructing.Clear();
								}

								// Check terrain obstructions
								MyGamePruningStructure.GetAllVoxelMapsInBox(ref batch.ObstructAABB, terrain);

								if (terrain.Any((voxel) => voxel.GetIntersectionWithAABB(ref batch.ObstructAABB)))
								{
									++skipped_entities_count;
									terrain.Clear();
									Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... TP_FAIL_OBSTRUCTED_TERRAIN", 4);
									continue;
								}

								terrain.Clear();
								target_matrix = target_gate?.TrueWorldJumpEllipse.WorldMatrix ?? MatrixD.CreateWorld(endpoint, this_matrix.Forward, this_matrix.Up);
								src_gate_velocity = this.JumpNodeVelocity;
								dst_gate_velocity = (target_gate == null) ? Vector3D.Zero : target_gate.JumpNodeVelocity;

								// Teleport batch
								if (jump_duration == 0)
								{
									foreach (MyEntity child in batch.Batch)
									{
										// Calculate end position and velocity
										Vector3D position = MyJumpGateModSession.WorldVectorToLocalVectorP(ref this_matrix, child.WorldMatrix.Translation);
										position = MyJumpGateModSession.LocalVectorToWorldVectorP(ref target_matrix, position);
										Vector3D forward_dir = MyJumpGateModSession.WorldVectorToLocalVectorD(ref this_matrix, entity.WorldMatrix.Forward);
										forward_dir = MyJumpGateModSession.LocalVectorToWorldVectorD(ref target_matrix, forward_dir);
										Vector3D up_dir = MyJumpGateModSession.WorldVectorToLocalVectorD(ref this_matrix, entity.WorldMatrix.Up);
										up_dir = MyJumpGateModSession.LocalVectorToWorldVectorD(ref target_matrix, up_dir);
										MatrixD.CreateWorld(ref position, ref forward_dir, ref up_dir, out batch.ParentTargetMatrix);
										child.Teleport(batch.ParentTargetMatrix);
										Vector3D velocity = MyJumpGateModSession.WorldVectorToLocalVectorD(ref this_matrix, child.Physics.LinearVelocity);
										child.Physics.LinearVelocity = MyJumpGateModSession.LocalVectorToWorldVectorD(ref target_matrix, velocity);
										Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... ... TELEPORT_CHILD_{child.EntityId} - ENDPOINT={position}", 5);
									}

									Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... TP_SUCCESS", 4);
								}
								else
								{
									// Calculate end position and velocity
									MatrixD entity_matrix = entity.WorldMatrix;
									Vector3D velocity = MyJumpGateModSession.WorldVectorToLocalVectorD(ref this_matrix, entity.Physics.LinearVelocity);
									velocity = MyJumpGateModSession.LocalVectorToWorldVectorD(ref target_matrix, velocity);
									MyJumpGateModSession.Instance.WarpEntityBatchOverTime(this, batch.Batch, ref entity_matrix, ref batch.ParentTargetMatrix, jump_duration, (batch_) => { foreach (MyEntity child in batch_) if (child != null && !child.MarkedForClose) child.Physics.LinearVelocity = velocity; });
									Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... TP_SUCCESS_BATCH_{entity.EntityId} - ENDPOINT={batch.ParentTargetMatrix.Translation}", 4);
								}
							}

							if (dst.WaypointType == MyWaypointType.SERVER)
							{
								SendJumpResponse(MyJumpFailReason.CROSS_SERVER_JUMP, false, null);
							}
							else if (dst.WaypointType != MyWaypointType.SERVER)
							{

								int final_count = entities_to_jump.Count - skipped_entities_count;
								SendJumpResponse((final_count > 0) ? MyJumpFailReason.SUCCESS : MyJumpFailReason.NO_ENTITIES_JUMPED, final_count > 0, $"Jumped {final_count}/{entities_to_jump.Count} entities");
							}
						};

						this.CanDoSyphonGridPower(syphon_power, 180, power_syphon_callback, false);
					}
					catch (Exception e)
					{
						SendJumpResponse(MyJumpFailReason.UNKNOWN_ERROR, false, null);
						int drive_count = this.JumpGateGrid.GetDriveCount((drive) => drive.JumpGateID == this.JumpGateID);
						int working_drive_count = this.JumpGateGrid.GetDriveCount(working_src_drive_filter);
						Logger.Error($"Error during jump gate jump:\n\tPHASE=POSTANIM\n\tGATE_CUBE_GRID={this.JumpGateGrid?.CubeGrid?.EntityId.ToString() ?? "N/A"}\n\tGATE_ID={this.JumpGateID}\n\tGATE_SIZE={drive_count}\n\tGATE_SIZE_WORKING={working_drive_count}\n\n\tSTACKTRACE:\n{e}");
					}

				});
			}
			catch (Exception e)
			{
				SendJumpResponse(MyJumpFailReason.UNKNOWN_ERROR, false, null);
				int drive_count = this.JumpGateGrid.GetDriveCount((drive) => drive.JumpGateID == this.JumpGateID);
				int working_drive_count = this.JumpGateGrid.GetDriveCount(working_src_drive_filter);
				Logger.Error($"Error during jump gate jump:\n\tPHASE=PREANIM\n\tGATE_CUBE_GRID={this.JumpGateGrid?.CubeGrid?.EntityId.ToString() ?? "N/A"}\n\tGATE_ID={this.JumpGateID}\n\tGATE_SIZE={drive_count}\n\tGATE_SIZE_WORKING={working_drive_count}\n\n\tSTACKTRACE:\n{e}");
			}
		}

		/// <summary>
		/// Executes the gate jump for multiplayer client sessons
		/// </summary>
		/// <param name="controller_settings">The controller settings to use for the jump</param>
		private void ExecuteJumpClient(MyJumpGateController.MyControllerBlockSettingsStruct controller_settings, MyJumpGateController.MyControllerBlockSettingsStruct target_controller_settings, Vector3D true_endpoint, MyJumpTypeEnum jump_type)
		{
			this.CheckClosed();
			Logger.Debug($"[{this.JumpGateGrid?.CubeGridID ?? -1}]-{this.JumpGateID} BEGIN_JUMP CLIENT PRE_CHECK", 3);
			MyJumpGate target_gate = null;
			MyJumpGateAnimation gate_animation = null;
			MyJumpGateWaypoint dst = controller_settings.SelectedWaypoint();
			this.ServerJumpResponse = null;
			Action<string, bool> SendJumpResponse = null;
			Vector3D jump_node = this.WorldJumpNode;
			IMyHudNotification hud_notification = null;
			Guid this_id = JumpGateUUID.FromJumpGate(this).ToGuid();
			this.AutoActivateStartTime = null;
			this.AutoActivationNotif.Hide();

			Action<MyNetworkInterface.Packet> OnNetworkJumpResponse  = (packet) => {
				if (packet == null || packet.PhaseFrame != 1) return;
				JumpGateInfo jump_gate_info = packet.Payload<JumpGateInfo>();

				if (MyNetworkInterface.IsStandaloneMultiplayerClient && jump_gate_info.Type != JumpGateInfo.TypeEnum.JUMP_START && jump_gate_info.JumpGateID == JumpGateUUID.FromJumpGate(this).ToGuid())
				{
					this.ServerJumpResponse = jump_gate_info;
					this.Phase = MyJumpGatePhase.JUMPING;

					foreach (string serialized_batch in jump_gate_info.EntityBatches)
					{
						EntityBatch batch = new EntityBatch(serialized_batch);
						if (batch.Batch.Count == 0) continue;
						this.EntityBatches[batch.Parent] = batch;
					}

					switch (this.ServerJumpResponse.Type)
					{
						case JumpGateInfo.TypeEnum.JUMP_FAIL:
						case JumpGateInfo.TypeEnum.CLOSED:
							this.Status = (this.ServerJumpResponse.CancelOverride) ? MyJumpGateStatus.CANCELLED : this.Status;
							SendJumpResponse(this.ServerJumpResponse.ResultMessage, false);
							break;
						case JumpGateInfo.TypeEnum.JUMP_SUCCESS:
							SendJumpResponse(this.ServerJumpResponse.ResultMessage, true);
							MyJumpGateModSession.Instance.RequestGridsDownload();
							break;
					}
				}
			};

			Action<MyNetworkInterface.Packet> OnNetworkUpdateJumpEndpoint = (packet) => {
				if (packet == null || packet.PhaseFrame != 1) return;
				KeyValuePair<Guid, Vector3D> payload = packet.Payload<KeyValuePair<Guid, Vector3D>>();
				this.TrueEndpoint = (payload.Key == this_id && this.JumpHandlerActive) ? payload.Value : this.TrueEndpoint;
			};

			SendJumpResponse = (message, result_status) => {
				this.Status = MyJumpGateStatus.SWITCHING;
				this.Phase = MyJumpGatePhase.RESETING;

				if (target_gate != null)
				{
					target_gate.Status = MyJumpGateStatus.SWITCHING;
					target_gate.Phase = MyJumpGatePhase.RESETING;
				}

				MyJumpGateModSession.Network.Off(MyPacketTypeEnum.JUMP_GATE_JUMP, this.OnNetworkJumpResponse);
				MyJumpGateModSession.Network.Off(MyPacketTypeEnum.UPDATE_JUMP_ENDPOINT, this.OnNetworkUpdateJumpEndpoint);
				this.JumpHandlerActive = false;

				Action<Exception> onend = (error) => {
					gate_animation?.Clean();
					this.TrueEndpoint = null;
					this.EntityBatches.Clear();
					if (this.ServerJumpResponse != null && this.ServerJumpResponse.Type == JumpGateInfo.TypeEnum.CLOSED) this.Dispose();
					else if (!this.Closed && !this.MarkClosed) this.Reset();
					this.ServerJumpResponse = null;
					this.OnNetworkJumpResponse = null;
					this.OnNetworkUpdateJumpEndpoint = null;
				};

				if (gate_animation != null) MyJumpGateModSession.Instance.PlayAnimation(gate_animation, (result_status) ? MyJumpGateAnimation.AnimationTypeEnum.JUMPED : MyJumpGateAnimation.AnimationTypeEnum.FAILED, null, onend);
				else onend(null);
				hud_notification?.Hide();
				this.HasBlocksWithinShearZone = false;
				this.ShearBlocksWarning.Hide();
				this.SendHudMessage(message, 3000, (result_status) ? "White" : "Red");
				if (target_gate != null && !target_gate.Closed && !target_gate.MarkClosed) target_gate.Reset();
				Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} END_JUMP {result_status}/{message}", 3);
			};

			this.OnNetworkJumpResponse = this.OnNetworkJumpResponse ?? OnNetworkJumpResponse;
			this.OnNetworkUpdateJumpEndpoint = this.OnNetworkUpdateJumpEndpoint ?? OnNetworkUpdateJumpEndpoint;
			MyJumpGateModSession.Network.On(MyPacketTypeEnum.JUMP_GATE_JUMP, this.OnNetworkJumpResponse);
			MyJumpGateModSession.Network.On(MyPacketTypeEnum.UPDATE_JUMP_ENDPOINT, this.OnNetworkUpdateJumpEndpoint);
			this.JumpHandlerActive = true;

			try
			{
				// Get target gate if applicable
				if (dst.WaypointType == MyWaypointType.JUMP_GATE)
				{
					JumpGateUUID uuid = JumpGateUUID.FromGuid(dst.JumpGate);
					MyJumpGateConstruct grid = MyJumpGateModSession.Instance.GetJumpGateGrid(uuid.GetJumpGateGrid());
					target_gate = grid?.GetJumpGate(uuid.GetJumpGate());
				}

				// Setup endpoint
				Vector3D? _endpoint = dst.GetEndpoint();

				if (_endpoint == null)
				{
					SendJumpResponse("Destination not available", false);
					return;
				}

				// Setup gate animation
				string gate_animation_name = controller_settings.JumpEffectAnimationName();
				gate_animation = MyAnimationHandler.GetAnimation(gate_animation_name, this, target_gate, controller_settings, target_controller_settings, ref true_endpoint, jump_type);

				if (gate_animation == null)
				{
					SendJumpResponse("No Animation Selected", false);
					return;
				}

				// Final setup
				this.TrueEndpoint = true_endpoint;
				double tick_duration = 1000d / 60d;
				double time_to_jump_ms = (gate_animation == null) ? 0 : gate_animation.Durations()[0] * tick_duration;
				hud_notification = MyAPIGateway.Utilities.CreateNotification("Gate Activation in --.-s", (int) Math.Round(time_to_jump_ms), "White");

				if (!MyNetworkInterface.IsMultiplayerServer)
				{
					hud_notification.Show();
					this.ShearBlocksWarning.AliveTime = (int) Math.Round(time_to_jump_ms);
				}

				// Update gate status
				this.Status = MyJumpGateStatus.OUTBOUND;
				this.Phase = MyJumpGatePhase.CHARGING;
				MyJumpGateModSession.Instance.RedrawAllTerminalControls();
				
				// Play animation and check for invalidation of jump event
				MyJumpGateModSession.Instance.PlayAnimation(gate_animation, MyJumpGateAnimation.AnimationTypeEnum.JUMPING, () => {
					time_to_jump_ms -= tick_duration;
					hud_notification.Text = $"Gate Activation in {MyJumpGateModSession.AutoconvertMetricUnits(Math.Max(0, time_to_jump_ms / 1000d), "s", 1)}";
					hud_notification.Hide();
					Vector3D character_pos = MyAPIGateway.Session.LocalHumanPlayer?.GetPosition() ?? Vector3D.PositiveInfinity;

					if (character_pos != null && this.IsPointInHudBroadcastRange(ref character_pos))
					{
						hud_notification.Show();
						if (this.ShearBlocks.Count == 0) this.ShearBlocksWarning.Hide();
						else this.ShearBlocksWarning.Show();
					}

					return this.ServerJumpResponse == null;
				}, (Exception err) => {
					hud_notification.Hide();
					if (err != null) SendJumpResponse("Unknown Local Gate Error", false);
				});
			}
			catch (Exception)
			{
				SendJumpResponse("Unknown Gate Error", false);
			}
		}

		/// <summary>
		/// Callback for jump space physics collider
		/// </summary>
		/// <param name="entity">The entity entering/leaving the jump sphere</param>
		/// <param name="is_entering">Whether the entity is entering</param>
		private void OnEntityCollision(IMyEntity entity, bool is_entering)
		{
			if (this.Closed) return;
			MyEntity topmost = (MyEntity) entity.GetTopMostParent();
			MyJumpGateConstruct parent = null;
			if (topmost is MyCubeGrid && (parent = MyJumpGateModSession.Instance.GetUnclosedJumpGateGrid(topmost.EntityId)) != null) topmost = (MyCubeGrid) parent.CubeGrid;
			if (!this.IsComplete() || this.MarkClosed || topmost?.Physics == null || entity == this.JumpSpaceCollisionDetector || entity != topmost) return;

			if (is_entering && !this.JumpGateGrid.HasCubeGrid(topmost.EntityId)) this.JumpSpaceColliderEntities[topmost.EntityId] = topmost;
			else if (!is_entering && !this.ForceUpdateCollider)
			{
				BoundingEllipsoidD jump_ellipse = this.JumpEllipse;
				if (parent != null && parent.IsConstructWithinBoundingEllipsoid(ref jump_ellipse)) return;
				this.JumpSpaceColliderEntities.Remove(topmost.EntityId);
				float _;
				if (this.JumpSpaceEntities.TryRemove(topmost.EntityId, out _)) this.OnEntityJumpSpaceLeave(topmost);
			}
		}

		/// <summary>
		/// Callback for entity updater<br />
		/// Called when an entity enters this gate's jump ellipse (directly or as sub entity)
		/// </summary>
		/// <param name="entity">The entity entering the jump space</param>
		private void OnEntityJumpSpaceEnter(MyEntity entity)
		{
			Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID}: ENTITY={entity.DisplayName} @ {entity.EntityId}, !ENTERED!", 4);
			this.IsDirty = this.IsDirty || MyNetworkInterface.IsMultiplayerServer;
			MyAPIJumpGate api = MyAPISession.GetNewJumpGate(this);

			lock (this.EntityEnterCallbacks)
			{
				foreach (Action<MyJumpGate, MyEntity, bool> callback in this.EntityEnterCallbacks)
				{
					callback(this, entity, true);
				}
			}

			lock (this.APIEntityEnterCallbacks)
			{
				foreach (Action<MyAPIJumpGate, MyEntity, bool> callback in this.APIEntityEnterCallbacks)
				{
					callback(api, entity, true);
				}
			}
		}

		/// <summary>
		/// Callback for entity updater<br />
		/// Called when an entity leaves this gate's jump ellipse (directly or as sub entity)
		/// </summary>
		/// <param name="entity">The entity leaving the jump space</param>
		private void OnEntityJumpSpaceLeave(MyEntity entity)
		{
			Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID}: ENTITY={entity.DisplayName} @ {entity.EntityId}, !LEFT!", 4);
			this.IsDirty = this.IsDirty || MyNetworkInterface.IsMultiplayerServer;
			MyAPIJumpGate api = MyAPISession.GetNewJumpGate(this);

			lock (this.EntityEnterCallbacks)
			{
				foreach (Action<MyJumpGate, MyEntity, bool> callback in this.EntityEnterCallbacks)
				{
					callback(this, entity, false);
				}
			}

			lock (this.APIEntityEnterCallbacks)
			{
				foreach (Action<MyAPIJumpGate, MyEntity, bool> callback in this.APIEntityEnterCallbacks)
				{
					callback(api, entity, false);
				}
			}
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Closes this gate, unregisters handlers, and frees all resources<br />
		/// To be called only by session
		/// </summary>
		public void Close()
		{
			this.CheckClosed();
			MyGateInvalidationReason reason = this.GetInvalidationReason();
			JumpGateUUID uuid = JumpGateUUID.FromJumpGate(this);
			string name = this.GetPrintableName();
			List<MyJumpGateDrive> drives = new List<MyJumpGateDrive>();
			this.GetJumpGateDrives(drives);
			foreach (MyJumpGateDrive drive in drives) drive.SetAttachedJumpGate(null);
			foreach (KeyValuePair<ulong, KeyValuePair<Vector3D?, MySoundEmitter3D>> pair in this.SoundEmitters) pair.Value.Value.Dispose(true);
			if (this.Controller != null) this.Controller.AttachedJumpGate(null);
			this.Release();
			lock (this.DriveIntersectNodesMutex) this.InnerDriveIntersectNodes.Clear();
			this.Closed = true;

			foreach (KeyValuePair<long, float> pair in this.JumpSpaceEntities)
			{
				MyEntity entity = (MyEntity) MyAPIGateway.Entities.GetEntityById(pair.Key);
				if (entity != null) this.OnEntityJumpSpaceLeave(entity);
			}

			if (this.JumpSpaceCollisionDetector != null)
			{
				this.JumpSpaceCollisionDetector.Close();
				this.JumpSpaceCollisionDetector.Delete();
				MyAPIGateway.Entities.RemoveEntity(this.JumpSpaceCollisionDetector);
				this.JumpSpaceCollisionDetector = null;
			}

			if (MyJumpGateModSession.Network.Registered && MyNetworkInterface.IsMultiplayerServer)
			{
				MyNetworkInterface.Packet update_packet = new MyNetworkInterface.Packet
				{
					PacketType = MyPacketTypeEnum.UPDATE_JUMP_GATE,
					TargetID = 0,
					Broadcast = true,
				};
				update_packet.Payload(this.ToSerialized(false));
				update_packet.Send();
			}

			lock (this.EntityEnterCallbacks) this.EntityEnterCallbacks.Clear();
			lock (this.APIEntityEnterCallbacks) this.APIEntityEnterCallbacks.Clear();

			this.SoundEmitters.Clear();
			this.JumpSpaceEntities.Clear();
			this.ShearBlocks.Clear();
			this.EntityBatches.Clear();
			this.JumpSpaceColliderEntities.Clear();
			this.DriveIntersectionPlanes.Clear();

			this.Status = MyJumpGateStatus.NONE;
			this.Phase = MyJumpGatePhase.NONE;
			this.JumpGateID = -1;
			this.LocalJumpNode = Vector3D.Zero;
			this.TrueLocalJumpEllipse = BoundingEllipsoidD.Zero;
			this.TrueWorldJumpEllipse = BoundingEllipsoidD.Zero;

			this.Controller = null;
			this.JumpGateGrid = null;
			this.InnerDriveIntersectNodes = null;
			this.JumpSpaceEntities = null;
			this.ShearBlocks = null;
			this.JumpSpaceColliderEntities = null;
			this.DriveIntersectionPlanes = null;
			this.SoundEmitters = null;
			this.EntityBatches = null;
			this.PowerSyphon = null;

			Logger.Debug($"Jump Gate \"{name}\" ({uuid.GetJumpGateGrid()}-{uuid.GetJumpGate()}) Closed - {reason}", 1);
		}

		/// <summary>
		/// Resets this gate's status and phase after jump
		/// </summary>
		public void Reset()
		{
			this.CheckClosed();
			bool complete = this.IsComplete();
			this.Phase = (complete) ? MyJumpGatePhase.IDLE : MyJumpGatePhase.NONE;
			this.Status = (complete) ? MyJumpGateStatus.IDLE : MyJumpGateStatus.NONE;
			if (!(this.SenderGate?.Closed ?? true)) this.SenderGate.Reset();
			this.SenderGate = null;
			this.TrueEndpoint = null;
		}

		/// <summary>
		/// Unregisteres resources for partially initialized gates<br />
		/// For fully initialized gates: Call MyJumpGate::Dispose()
		/// </summary>
		public void Release()
		{
			if (MyJumpGateModSession.Network.Registered)
			{
				MyJumpGateModSession.Network.Off(MyPacketTypeEnum.JUMP_GATE_JUMP, this.OnNetworkJumpGateJump);
				MyJumpGateModSession.Network.Off(MyPacketTypeEnum.UPDATE_JUMP_GATE, this.OnNetworkJumpGateUpdate);
				MyJumpGateModSession.Network.Off(MyPacketTypeEnum.AUTOACTIVATE, this.OnNetworkAutoActivationAlert);
				MyJumpGateModSession.Network.Off(MyPacketTypeEnum.SHEARWARNING, this.OnNetworkShearBlockAlert);
			}
		}

		/// <summary>
		/// Requests a jump gate update from the server<br />
		/// Does nothing if singleplayer or server<br />
		/// Does nothing if parent construct is suspended
		/// </summary>
		public void RequestGateUpdate()
		{
			this.CheckClosed();
			if (MyNetworkInterface.IsServerLike || this.JumpGateGrid.IsSuspended) return;
			MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet {
				PacketType = MyPacketTypeEnum.UPDATE_JUMP_GATE,
				Broadcast = false,
				TargetID = 0,
			};
			packet.Payload<MySerializedJumpGate>(this.ToSerialized(true));
			packet.Send();
		}

		/// <summary>
		/// Begins the activation sequence for this gate<br />
		/// For singleplayer: Begins the jump<br />
		/// For servers: Begins the jump, broadcasts jump request to all clients<br />
		/// For clients: Sends a jump request to server
		/// </summary>
		/// <param name="controller_settings">The controller settings to use for the jump</param>
		public void Jump(MyJumpGateController.MyControllerBlockSettingsStruct controller_settings)
		{
			this.CheckClosed();
			if (controller_settings == null) return;
			else if (MyNetworkInterface.IsStandaloneMultiplayerClient)
			{
				MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet {
					PacketType = MyPacketTypeEnum.JUMP_GATE_JUMP,
					TargetID = 0,
					Broadcast = false,
				};
				packet.Payload(new JumpGateInfo {
					CancelOverride = false,
					Type = JumpGateInfo.TypeEnum.JUMP_START,
					JumpGateID = JumpGateUUID.FromJumpGate(this).ToGuid(),
					ControllerSettings = controller_settings,
				});
				packet.Send();
			}
			else if (MyNetworkInterface.IsServerLike) this.JumpWrapper(controller_settings, null, null, MyJumpTypeEnum.STANDARD);
		}

		/// <summary>
		/// Begins the activation sequence for this gate<br />
		/// Destination is the void; <b>All jump entities will be destroyed</b><br />
		/// This method must be called on singleplayer or multiplayer server
		/// </summary>
		/// <param name="controller_settings">The controller settings to use for the jump</param>
		/// <param name="distance">The distance to use for power calculations</param>
		public void JumpToVoid(MyJumpGateController.MyControllerBlockSettingsStruct controller_settings, double distance)
		{
			if (!MyNetworkInterface.IsServerLike) return;
			this.CheckClosed();
			Vector3D endpoint = this.WorldJumpNode + this.GetWorldMatrix(false, true).Forward * distance;
			Vector3D animation_endpoint = this.WorldJumpNode + this.GetWorldMatrix(false, true).Forward * 1e6;
			this.TrueEndpoint = endpoint;
			MyJumpGateAnimation gate_animation = null;
			List<KeyValuePair<MyEntity, bool>> entities_to_jump = null;
			List<MyJumpGate> other_gates = new List<MyJumpGate>();
			Dictionary<MyEntity, float> jump_space_entities = new Dictionary<MyEntity, float>(this.JumpSpaceEntities.Count);
			Func<MyJumpGateDrive, bool> working_src_drive_filter = (drive) => drive.JumpGateID == this.JumpGateID && drive.IsWorking();
			MyJumpFailReason result = MyJumpFailReason.NONE;
			this.JumpFailureReason = new KeyValuePair<MyJumpFailReason, bool>(MyJumpFailReason.IN_PROGRESS, true);
			bool is_init = true;
			Vector3D jump_node = this.WorldJumpNode;
			Guid this_id = JumpGateUUID.FromJumpGate(this).ToGuid();
			BoundingEllipsoidD jump_ellipse = this.JumpEllipse;
			
			Action<MyJumpFailReason, bool, string> SendJumpResponse = (reason, result_status, override_message) => {
				this.JumpFailureReason = new KeyValuePair<MyJumpFailReason, bool>((result_status) ? MyJumpFailReason.SUCCESS : reason, is_init);
				string message = override_message ?? MyJumpGate.GetFailureDescription(this.JumpFailureReason.Key);

				// Is server, broadcast jump results
				if (MyNetworkInterface.IsMultiplayerServer)
				{
					MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet {
						PacketType = MyPacketTypeEnum.JUMP_GATE_JUMP,
						TargetID = 0,
						Broadcast = true,
					};

					packet.Payload(new JumpGateInfo {
						Type = (result_status) ? JumpGateInfo.TypeEnum.JUMP_SUCCESS : JumpGateInfo.TypeEnum.JUMP_FAIL,
						JumpGateID = JumpGateUUID.FromJumpGate(this).ToGuid(),
						ControllerSettings = controller_settings,
						ResultMessage = message,
						CancelOverride = this.Status == MyJumpGateStatus.CANCELLED,
					});
					packet.Send();
				}

				Action<Exception> onend = (error) => {
					gate_animation?.Clean();
					this.TrueEndpoint = null;
					if (!this.Closed) this.Reset();
					if (entities_to_jump != null) foreach (KeyValuePair<MyEntity, bool> entity in entities_to_jump) entity.Key.Render.Visible = entity.Value;
				};

				this.Status = MyJumpGateStatus.SWITCHING;
				if (gate_animation != null) MyJumpGateModSession.Instance.PlayAnimation(gate_animation, (result_status) ? MyJumpGateAnimation.AnimationTypeEnum.JUMPED : MyJumpGateAnimation.AnimationTypeEnum.FAILED, null, onend);
				else onend(null);
				--MyJumpGateModSession.Instance.ConcurrentJumpsCounter;
				this.SendHudMessage(message, 3000, (result_status) ? "White" : "Red");
				Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} END_JUMP {result_status}/{message}", 3);
			};

			try
			{
				// Check this gate
				Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} VOIDJUMP_PRECHECK", 3);
				++MyJumpGateModSession.Instance.ConcurrentJumpsCounter;

				if (!this.IsValid()) result = MyJumpFailReason.SRC_INVALID;
				else if (!this.IsComplete() || !this.Controller.IsWorking()) result = MyJumpFailReason.CONTROLLER_NOT_CONNECTED;
				else if (this.JumpGateGrid.GetDriveCount(working_src_drive_filter) < 2) result = MyJumpFailReason.SRC_DISABLED;
				else if (this.Status == MyJumpGateStatus.NONE) result = MyJumpFailReason.SRC_NOT_CONFIGURED;
				else if (!this.IsIdle()) result = MyJumpFailReason.SRC_BUSY;
				else if (!controller_settings.CanBeInbound() && !controller_settings.CanBeOutbound()) result = MyJumpFailReason.SRC_ROUTING_DISABLED;
				else if (!controller_settings.CanBeOutbound()) result = MyJumpFailReason.SRC_INBOUND_ONLY;
				else if (this.JumpGateConfiguration.MaxConcurrentJumps > 0 && MyJumpGateModSession.Instance.ConcurrentJumpsCounter > this.JumpGateConfiguration.MaxConcurrentJumps) result = MyJumpFailReason.SUBSPACE_BUSY;

				// Check gate overlap
				if (result == MyJumpFailReason.NONE)
				{
					MyJumpGateModSession.Instance.GetAllJumpGates(other_gates);

					foreach (MyJumpGate other_gate in other_gates)
					{
						if (other_gate != this && (other_gate.Status == MyJumpGateStatus.OUTBOUND) && jump_ellipse.Intersects(other_gate.JumpEllipse))
						{
							result = MyJumpFailReason.JUMP_SPACE_TRANSPOSED;
							break;
						}
					}

					other_gates.Clear();
				}

				// Send jump-failure if failed
				if (result != MyJumpFailReason.NONE)
				{
					SendJumpResponse(result, false, null);
					return;
				}

				// Setup gate animation
				string gate_animation_name = controller_settings.JumpEffectAnimationName();
				gate_animation = MyAnimationHandler.GetAnimation(gate_animation_name, this, null, controller_settings, null, ref animation_endpoint, MyJumpTypeEnum.OUTBOUND_VOID);

				if (gate_animation == null)
				{
					SendJumpResponse(MyJumpFailReason.NULL_ANIMATION, false, null);
					return;
				}

				// Final setup
				double tick_duration = 1000d / 60d;
				double time_to_jump_ms = (gate_animation == null) ? 0 : gate_animation.Durations()[0] * tick_duration;
				IMyHudNotification hud_notification = MyAPIGateway.Utilities.CreateNotification("Gate Activation in --.-s", (int) Math.Round(time_to_jump_ms), "White");
				IMyHudNotification cancel_request_notificiation = null;

				if (!MyNetworkInterface.IsDedicatedMultiplayerServer)
				{
					hud_notification.Show();
					this.ShearBlocksWarning.AliveTime = (int) Math.Round(time_to_jump_ms);
				}

				// If server, broadcast jump start
				if (MyNetworkInterface.IsMultiplayerServer)
				{
					MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet {
						PacketType = MyPacketTypeEnum.JUMP_GATE_JUMP,
						TargetID = 0,
						Broadcast = true,
					};

					packet.Payload(new JumpGateInfo {
						Type = JumpGateInfo.TypeEnum.JUMP_START,
						JumpType = MyJumpTypeEnum.OUTBOUND_VOID,
						JumpGateID = JumpGateUUID.FromJumpGate(this).ToGuid(),
						ControllerSettings = controller_settings,
						TargetControllerSettings = null,
						TrueEndpoint = endpoint,
					});

					packet.Send();
				}

				// Update gate status
				this.Status = MyJumpGateStatus.OUTBOUND;
				this.Phase = MyJumpGatePhase.JUMPING;
				is_init = false;
				MyJumpGateModSession.Instance.RedrawAllTerminalControls();
				Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} VOIDJUMP_CHARGE", 3);

				// Play animation and check for invalidation of jump event
				MyJumpGateModSession.Instance.PlayAnimation(gate_animation, MyJumpGateAnimation.AnimationTypeEnum.JUMPING, () => {
					if (!this.IsValid()) result = MyJumpFailReason.SRC_INVALID;
					else if (!this.IsComplete() || !this.Controller.IsWorking()) result = MyJumpFailReason.CONTROLLER_NOT_CONNECTED;
					else if (this.JumpGateGrid.GetDriveCount(working_src_drive_filter) < 2) result = MyJumpFailReason.SRC_DISABLED;
					else if (this.Status == MyJumpGateStatus.NONE) result = MyJumpFailReason.SRC_NOT_CONFIGURED;
					else if (!controller_settings.CanBeInbound() && !controller_settings.CanBeOutbound()) result = MyJumpFailReason.SRC_ROUTING_CHANGED;
					else if (!controller_settings.CanBeOutbound()) result = MyJumpFailReason.SRC_ROUTING_CHANGED;
					
					if (MyJumpGateModSession.GameTick % 30 == 0)
					{
						MyJumpGateModSession.Instance.GetAllJumpGates(other_gates);

						foreach (MyJumpGate other_gate in other_gates)
						{
							if (other_gate != this && other_gate.Status == MyJumpGateStatus.OUTBOUND && jump_ellipse.Intersects(other_gate.JumpEllipse))
							{
								result = MyJumpFailReason.JUMP_SPACE_TRANSPOSED;
								break;
							}
						}

						other_gates.Clear();
					}

					time_to_jump_ms -= tick_duration;
					hud_notification.Text = $"Gate Activation in {MyJumpGateModSession.AutoconvertMetricUnits(Math.Max(0, time_to_jump_ms / 1000d), "s", 1)}";
					hud_notification.Hide();
					cancel_request_notificiation?.Hide();
					Vector3D character_pos = MyAPIGateway.Session.LocalHumanPlayer?.GetPosition() ?? Vector3D.PositiveInfinity;
					bool show_notifs = character_pos != null && this.IsPointInHudBroadcastRange(ref character_pos);

					if (show_notifs)
					{
						hud_notification.Show();
						cancel_request_notificiation?.Show();
						if (this.ShearBlocks.Count == 0) this.ShearBlocksWarning?.Hide();
						else this.ShearBlocksWarning.Show();
					}

					if (this.Closed || this.JumpGateGrid == null || this.JumpGateGrid.Closed) result = MyJumpFailReason.SRC_CLOSED;
					else if (this.Status == MyJumpGateStatus.CANCELLED && gate_animation.ImmediateCancel) result = MyJumpFailReason.CANCELLED;
					else if (this.Status == MyJumpGateStatus.CANCELLED && cancel_request_notificiation == null && show_notifs)
					{
						cancel_request_notificiation = MyAPIGateway.Utilities.CreateNotification("Jump Cancelling...", (int) Math.Round(time_to_jump_ms), "Red");
						cancel_request_notificiation.Show();
					}

					List<MyJumpGateDrive> working_drives = new List<MyJumpGateDrive>();
					this.GetWorkingJumpGateDrives(working_drives);

					foreach (MyJumpGateDrive drive in working_drives)
					{
						if (drive.IsNullWrapper) continue;
						Vector3D force_dir = (drive.TerminalBlock.WorldMatrix.Translation - this.WorldJumpNode).Normalized() * this.JumpGateConfiguration.ChargingDriveEffectorForceN;
						drive.TerminalBlock.GetTopMostParent().Physics.AddForce(VRage.Game.Components.MyPhysicsForceType.APPLY_WORLD_FORCE, force_dir, drive.TerminalBlock.WorldMatrix.Translation, null);
					}

					return result == MyJumpFailReason.NONE;
				}, (Exception err) => {

					try
					{
						// Confirm no errors occured
						Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} VOIDJUMP_POSTCHECK", 3);
						if (err != null) result = MyJumpFailReason.UNKNOWN_ERROR;
						else if (this.Status == MyJumpGateStatus.CANCELLED) result = MyJumpFailReason.CANCELLED;
						hud_notification.Hide();
						cancel_request_notificiation?.Hide();
						this.ShearBlocksWarning?.Hide();

						if (result != MyJumpFailReason.NONE)
						{
							SendJumpResponse(result, false, null);
							return;
						}

						this.GetEntitiesInJumpSpace(jump_space_entities, true);
						entities_to_jump = jump_space_entities.Keys.Select((entity) => new KeyValuePair<MyEntity, bool>(entity, entity.Render.Visible)).ToList();
						jump_space_entities.Clear();

						if (entities_to_jump.Count == 0)
						{
							SendJumpResponse(MyJumpFailReason.NO_ENTITIES, false, null);
							return;
						}

						// Calculate Power
						double total_mass_kg = entities_to_jump.Sum((entity) => (double) entity.Key.Physics.Mass);
						double power_scaler = this.JumpGateConfiguration.UntetheredJumpEnergyCost;
						double remaining_power_mw = Math.Round(total_mass_kg * this.CalculateUnitPowerRequiredForJump(ref endpoint), 8) * power_scaler / 1000;
						Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... INSTANT_POWER_SYPHON - TOTAL_MASS={total_mass_kg} Kg; AVAILABLE_INSTANT_POWER={this.CalculateTotalAvailableInstantPower()} MW", 4);
						Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... ... INITIAL - REQUIRED_POWER={remaining_power_mw} Mw", 4);
						remaining_power_mw = Math.Round(this.SyphonGridDrivePower(remaining_power_mw), 8);
						Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... ... POST_DRIVES - REQUIRED_POWER={remaining_power_mw} Mw", 4);
						if (remaining_power_mw > 0) remaining_power_mw = Math.Round(this.JumpGateGrid.SyphonConstructCapacitorPower(remaining_power_mw), 8);
						Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... ... POST_CAPACITORS - REQUIRED_POWER={remaining_power_mw} Mw", 4);

						if (!MyJumpGateModSession.Configuration.GeneralConfiguration.SyphonReactorPower && remaining_power_mw > 0)
						{
							SendJumpResponse(MyJumpFailReason.INSUFFICIENT_POWER, false, null);
							return;
						}

						Action<bool> power_syphon_callback = (power_syphoned) => {
							if (!power_syphoned)
							{
								SendJumpResponse(MyJumpFailReason.INSUFFICIENT_POWER, false, null);
								return;
							}

							List<MyEntity> subentities = new List<MyEntity>();
							List<IMySlimBlock> destroyed = new List<IMySlimBlock>();
							List<IMyCubeGrid> grids = new List<IMyCubeGrid>();
							this.Phase = MyJumpGatePhase.JUMPING;
							MatrixD this_matrix = this.TrueWorldJumpEllipse.WorldMatrix;

							// Jump entities
							Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} VOIDJUMP_ENTITY_TRANSIT - Teleporting batches...", 3);
							int skipped_entities_count = 0;
							BoundingEllipsoidD effective_target_ellipse = this.GetEffectiveJumpEllipse();

							for (int i = 0; i < entities_to_jump.Count; ++i)
							{
								MyEntity entity = entities_to_jump[i].Key;
								MyJumpGateConstruct parent = null;
								Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... TELEPORTING - ENTITY={entity.EntityId}; ISGRID={parent != null}", 4);

								if (entity is MyCubeGrid)
								{
									parent = MyJumpGateModSession.Instance.GetUnclosedJumpGateGrid(entity.EntityId);
									if (parent == null) continue;
									parent.GetCubeGrids(grids);

									foreach (IMyCubeGrid grid in grids)
									{
										// Shear constraints attaching this cube grid to this jump gate
										BoundingBoxD box = grid.WorldAABB;
										MyGamePruningStructure.GetTopmostEntitiesInBox(ref box, subentities);
										MyJumpGateConstruct subparent;

										foreach (MyEntity sub_entity in subentities)
										{
											subparent = (sub_entity is MyCubeGrid) ? MyJumpGateModSession.Instance.GetUnclosedJumpGateGrid(sub_entity.EntityId) : null;
											if (sub_entity != entity && subparent != parent && MyJumpGateModSession.IsPositionInsideGrid(grid, sub_entity.WorldMatrix.Translation)) entities_to_jump.Add(new KeyValuePair<MyEntity, bool>(sub_entity, sub_entity.Render.Visible));
										}

										subentities.Clear();

										foreach (IMyShipConnector connector in grid.GetFatBlocks<IMyShipConnector>())
										{
											if (connector.IsWorking && connector.Status == Sandbox.ModAPI.Ingame.MyShipConnectorStatus.Connected && this.JumpGateGrid.HasCubeGrid(connector.OtherConnector.CubeGrid))
											{
												destroyed.Add(connector.SlimBlock);
											}
										}

										foreach (IMyLandingGear gear in grid.GetFatBlocks<IMyLandingGear>())
										{
											IMyEntity attached_entity;
											if (gear.IsWorking && gear.IsLocked && (attached_entity = gear.GetAttachedEntity()) is IMyCubeGrid && this.JumpGateGrid.HasCubeGrid(attached_entity as IMyCubeGrid))
											{
												destroyed.Add(gear.SlimBlock);
											}
										}

										if (destroyed.Count > 0 && this.JumpGateConfiguration.IgnoreDockedGrids)
										{
											++skipped_entities_count;
											Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} ... TP_FAIL_DOCKED", 4);
											continue;
										}
										else
										{
											foreach (IMySlimBlock block in destroyed) grid.RemoveDestroyedBlock(block);
										}

										destroyed.Clear();

										// Shear grid
										if (!MyJumpGateModSession.Configuration.GeneralConfiguration.LenientJumps)
										{
											MyCubeGrid cubegrid = entity as MyCubeGrid;
											foreach (IMySlimBlock block in this.ShearBlocks) if (block.CubeGrid == grid) grid.RemoveDestroyedBlock(block);
											destroyed.AddRange(cubegrid.CubeBlocks.Where((block) => !jump_ellipse.IsPointInEllipse(grid.GridIntegerToWorld(((IMySlimBlock) block).Position))));
											parent.SplitGrid(grid, destroyed);
											destroyed.Clear();
										}
									}
								}

								// Teleport to void
								if (entity is IMyCharacter) ((IMyCharacter) entity).Die();
								else if (parent == null) entity.Close();
								else parent.Destroy();
							}

							int final_count = entities_to_jump.Count - skipped_entities_count;
							SendJumpResponse((final_count > 0) ? MyJumpFailReason.SUCCESS : MyJumpFailReason.NO_ENTITIES_JUMPED, final_count > 0, $"Jumped {final_count}/{entities_to_jump.Count} entities");
						};

						this.CanDoSyphonGridPower(remaining_power_mw, 180, power_syphon_callback, true);
					}
					catch (Exception e)
					{
						SendJumpResponse(MyJumpFailReason.UNKNOWN_ERROR, false, null);
						int drive_count = this.JumpGateGrid.GetDriveCount((drive) => drive.JumpGateID == this.JumpGateID);
						int working_drive_count = this.JumpGateGrid.GetDriveCount(working_src_drive_filter);
						Logger.Error($"Error during jump gate jump:\n\tPHASE=POSTANIM\n\tGATE_CUBE_GRID={this.JumpGateGrid?.CubeGrid?.EntityId.ToString() ?? "N/A"}\n\tGATE_ID={this.JumpGateID}\n\tGATE_SIZE={drive_count}\n\tGATE_SIZE_WORKING={working_drive_count}\n\n\tSTACKTRACE:\n{e}");
					}

				});
			}
			catch (Exception e)
			{
				SendJumpResponse(MyJumpFailReason.UNKNOWN_ERROR, false, null);
				int drive_count = this.JumpGateGrid.GetDriveCount((drive) => drive.JumpGateID == this.JumpGateID);
				int working_drive_count = this.JumpGateGrid.GetDriveCount(working_src_drive_filter);
				Logger.Error($"Error during jump gate jump:\n\tPHASE=PREANIM\n\tGATE_CUBE_GRID={this.JumpGateGrid?.CubeGrid?.EntityId.ToString() ?? "N/A"}\n\tGATE_ID={this.JumpGateID}\n\tGATE_SIZE={drive_count}\n\tGATE_SIZE_WORKING={working_drive_count}\n\n\tSTACKTRACE:\n{e}");
			}
		}

		/// <summary>
		/// Begins the activation sequence for this gate<br />
		/// Source is the void; Allows spawning of prefab entities<br />
		/// This method must be called on singleplayer or multiplayer server
		/// </summary>
		/// <param name="controller_settings">The controller settings to use for the jump</param>
		/// <param name="spawn_prefabs">A list of prefabs to spawn<br />Jump will fail if empty</param>
		/// <param name="spawned_grids">A list containing the list of spawned grids per prefab</param>
		public void JumpFromVoid(MyJumpGateController.MyControllerBlockSettingsStruct controller_settings, List<MyPrefabInfo> spawn_prefabs, List<List<IMyCubeGrid>> spawned_grids)
		{
			if (!MyNetworkInterface.IsServerLike) return;
			this.CheckClosed();
			Vector3D endpoint = this.WorldJumpNode + this.GetWorldMatrix(false, true).Forward * 1e6;
			this.TrueEndpoint = endpoint;
			MyJumpGateAnimation gate_animation = null;
			Func<MyJumpGateDrive, bool> working_src_drive_filter = (drive) => drive.JumpGateID == this.JumpGateID && drive.IsWorking();
			MyJumpFailReason result = MyJumpFailReason.NONE;
			this.JumpFailureReason = new KeyValuePair<MyJumpFailReason, bool>(MyJumpFailReason.IN_PROGRESS, true);
			bool is_init = true;
			Vector3D jump_node = this.WorldJumpNode;
			Guid this_id = JumpGateUUID.FromJumpGate(this).ToGuid();
			BoundingEllipsoidD jump_ellipse = this.JumpEllipse;

			Action<MyJumpFailReason, bool, string> SendJumpResponse = (reason, result_status, override_message) => {
				this.JumpFailureReason = new KeyValuePair<MyJumpFailReason, bool>((result_status) ? MyJumpFailReason.SUCCESS : reason, is_init);
				string message = override_message ?? MyJumpGate.GetFailureDescription(this.JumpFailureReason.Key);

				// Is server, broadcast jump results
				if (MyNetworkInterface.IsMultiplayerServer)
				{
					MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet
					{
						PacketType = MyPacketTypeEnum.JUMP_GATE_JUMP,
						TargetID = 0,
						Broadcast = true,
					};

					packet.Payload(new JumpGateInfo
					{
						Type = (result_status) ? JumpGateInfo.TypeEnum.JUMP_SUCCESS : JumpGateInfo.TypeEnum.JUMP_FAIL,
						JumpGateID = JumpGateUUID.FromJumpGate(this).ToGuid(),
						ControllerSettings = controller_settings,
						ResultMessage = message,
						CancelOverride = this.Status == MyJumpGateStatus.CANCELLED,
					});
					packet.Send();
				}

				Action<Exception> onend = (error) => {
					gate_animation?.Clean();
					this.TrueEndpoint = null;
					if (!this.Closed) this.Reset();
				};

				this.Status = MyJumpGateStatus.SWITCHING;
				if (gate_animation != null) MyJumpGateModSession.Instance.PlayAnimation(gate_animation, (result_status) ? MyJumpGateAnimation.AnimationTypeEnum.JUMPED : MyJumpGateAnimation.AnimationTypeEnum.FAILED, null, onend);
				else onend(null);
				--MyJumpGateModSession.Instance.ConcurrentJumpsCounter;
				this.SendHudMessage(message, 3000, (result_status) ? "White" : "Red");
				Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} END_JUMP {result_status}/{message}", 3);
			};

			try
			{
				// Check this gate
				Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} VOIDJUMP_PRECHECK", 3);
				++MyJumpGateModSession.Instance.ConcurrentJumpsCounter;

				if (!this.IsValid() || !this.IsComplete() || !this.Controller.IsWorking()) result = MyJumpFailReason.DST_UNAVAILABLE;
				else if (this.JumpGateGrid.GetDriveCount(working_src_drive_filter) < 2) result = MyJumpFailReason.DST_DISABLED;
				else if (this.Status == MyJumpGateStatus.NONE) result = MyJumpFailReason.DST_NOT_CONFIGURED;
				else if (!this.IsIdle()) result = MyJumpFailReason.DST_BUSY;
				else if (!controller_settings.CanBeInbound() && !controller_settings.CanBeOutbound()) result = MyJumpFailReason.DST_ROUTING_DISABLED;
				else if (!controller_settings.CanBeInbound()) result = MyJumpFailReason.DST_OUTBOUND_ONLY;
				else if (this.JumpGateConfiguration.MaxConcurrentJumps > 0 && MyJumpGateModSession.Instance.ConcurrentJumpsCounter > this.JumpGateConfiguration.MaxConcurrentJumps) result = MyJumpFailReason.SUBSPACE_BUSY;

				// Send jump-failure if failed
				if (result != MyJumpFailReason.NONE)
				{
					SendJumpResponse(result, false, null);
					return;
				}

				// Setup gate animation
				string gate_animation_name = controller_settings.JumpEffectAnimationName();
				gate_animation = MyAnimationHandler.GetAnimation(gate_animation_name, this, this, controller_settings, controller_settings, ref endpoint, MyJumpTypeEnum.INBOUND_VOID);

				if (gate_animation == null)
				{
					SendJumpResponse(MyJumpFailReason.NULL_ANIMATION, false, null);
					return;
				}

				// Final setup
				double tick_duration = 1000d / 60d;
				double time_to_jump_ms = (gate_animation == null) ? 0 : gate_animation.Durations()[0] * tick_duration;
				IMyHudNotification hud_notification = MyAPIGateway.Utilities.CreateNotification("Gate Activation in --.-s", (int) Math.Round(time_to_jump_ms), "White");
				IMyHudNotification cancel_request_notificiation = null;

				if (!MyNetworkInterface.IsDedicatedMultiplayerServer)
				{
					hud_notification.Show();
					this.ShearBlocksWarning.AliveTime = (int) Math.Round(time_to_jump_ms);
				}

				// If server, broadcast jump start
				if (MyNetworkInterface.IsMultiplayerServer)
				{
					MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet {
						PacketType = MyPacketTypeEnum.JUMP_GATE_JUMP,
						TargetID = 0,
						Broadcast = true,
					};

					packet.Payload(new JumpGateInfo {
						Type = JumpGateInfo.TypeEnum.JUMP_START,
						JumpType = MyJumpTypeEnum.INBOUND_VOID,
						JumpGateID = JumpGateUUID.FromJumpGate(this).ToGuid(),
						ControllerSettings = controller_settings,
						TargetControllerSettings = null,
						TrueEndpoint = endpoint,
					});

					packet.Send();
				}

				// Update gate status
				this.Status = MyJumpGateStatus.INBOUND;
				this.Phase = MyJumpGatePhase.JUMPING;
				is_init = false;
				MyJumpGateModSession.Instance.RedrawAllTerminalControls();
				Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} VOIDJUMP_CHARGE", 3);

				// Play animation and check for invalidation of jump event
				MyJumpGateModSession.Instance.PlayAnimation(gate_animation, MyJumpGateAnimation.AnimationTypeEnum.JUMPING, () => {
					if (!this.IsValid() || !this.IsComplete() || !this.Controller.IsWorking()) result = MyJumpFailReason.DST_UNAVAILABLE;
					else if (this.JumpGateGrid.GetDriveCount(working_src_drive_filter) < 2) result = MyJumpFailReason.DST_DISABLED;
					else if (this.Status == MyJumpGateStatus.NONE) result = MyJumpFailReason.DST_NOT_CONFIGURED;
					else if (!controller_settings.CanBeInbound() && !controller_settings.CanBeOutbound()) result = MyJumpFailReason.DST_ROUTING_CHANGED;
					else if (!controller_settings.CanBeInbound()) result = MyJumpFailReason.DST_ROUTING_CHANGED;

					time_to_jump_ms -= tick_duration;
					hud_notification.Text = $"Gate Activation in {MyJumpGateModSession.AutoconvertMetricUnits(Math.Max(0, time_to_jump_ms / 1000d), "s", 1)}";
					hud_notification.Hide();
					cancel_request_notificiation?.Hide();
					Vector3D character_pos = MyAPIGateway.Session.LocalHumanPlayer?.GetPosition() ?? Vector3D.PositiveInfinity;
					bool show_notifs = character_pos != null && this.IsPointInHudBroadcastRange(ref character_pos);

					if (show_notifs)
					{
						hud_notification.Show();
						cancel_request_notificiation?.Show();
						if (this.ShearBlocks.Count == 0) this.ShearBlocksWarning?.Hide();
						else this.ShearBlocksWarning.Show();
					}

					if (this.Closed || this.JumpGateGrid == null || this.JumpGateGrid.Closed) result = MyJumpFailReason.DST_VOIDED;
					List<MyJumpGateDrive> working_drives = new List<MyJumpGateDrive>();
					this.GetWorkingJumpGateDrives(working_drives);

					foreach (MyJumpGateDrive drive in working_drives)
					{
						if (drive.IsNullWrapper) continue;
						Vector3D force_dir = (drive.TerminalBlock.WorldMatrix.Translation - this.WorldJumpNode).Normalized() * this.JumpGateConfiguration.ChargingDriveEffectorForceN;
						drive.TerminalBlock.GetTopMostParent().Physics.AddForce(VRage.Game.Components.MyPhysicsForceType.APPLY_WORLD_FORCE, force_dir, drive.TerminalBlock.WorldMatrix.Translation, null);
					}

					return result == MyJumpFailReason.NONE;
				}, (Exception err) => {

					try
					{
						// Confirm no errors occured
						Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} VOIDJUMP_POSTCHECK", 3);
						if (err != null) result = MyJumpFailReason.UNKNOWN_ERROR;
						hud_notification.Hide();
						cancel_request_notificiation?.Hide();
						this.ShearBlocksWarning?.Hide();

						if (result != MyJumpFailReason.NONE)
						{
							SendJumpResponse(result, false, null);
							return;
						}
						else if (spawn_prefabs.Count == 0)
						{
							SendJumpResponse(MyJumpFailReason.NO_ENTITIES, false, null);
							return;
						}
						else if (this.JumpSpaceEntities.Count > 0)
						{
							SendJumpResponse(MyJumpFailReason.NO_ENTITIES_JUMPED, false, $"Jump Space Obstructed");
							return;
						}

						// Spawn prefabs
						this.Phase = MyJumpGatePhase.JUMPING;
						int skipped_entities_count = 0;
						MatrixD gate_matrix = jump_ellipse.WorldMatrix;

						for (int i = 0; i < spawn_prefabs.Count; ++i)
						{
							MyPrefabInfo prefab = spawn_prefabs[i];
							List<IMyCubeGrid> grids = (i >= spawned_grids.Count) ? new List<IMyCubeGrid>() : spawned_grids[i];
							Vector3D world_pos = MyJumpGateModSession.LocalVectorToWorldVectorP(ref gate_matrix, prefab.Position);

							if (!jump_ellipse.IsPointInEllipse(world_pos))
							{
								++skipped_entities_count;
								continue;
							}

							Vector3D forward = MyJumpGateModSession.LocalVectorToWorldVectorD(ref gate_matrix, prefab.Forward);
							Vector3D up = MyJumpGateModSession.LocalVectorToWorldVectorD(ref gate_matrix, prefab.Up);
							MyAPIGateway.PrefabManager.SpawnPrefab(grids, prefab.PrefabName, world_pos, forward, up, prefab.InitialLinearVelocity, prefab.InitialAngularVelocity, prefab.BeaconName, prefab.SpawningOptions, prefab.UpdateSync, prefab.Callback);
						}

						int final_count = spawn_prefabs.Count - skipped_entities_count;
						SendJumpResponse((final_count > 0) ? MyJumpFailReason.SUCCESS : MyJumpFailReason.NO_ENTITIES_JUMPED, final_count > 0, $"Jumped {final_count}/{spawn_prefabs.Count} entities");
					}
					catch (Exception e)
					{
						SendJumpResponse(MyJumpFailReason.UNKNOWN_ERROR, false, null);
						int drive_count = this.JumpGateGrid.GetDriveCount((drive) => drive.JumpGateID == this.JumpGateID);
						int working_drive_count = this.JumpGateGrid.GetDriveCount(working_src_drive_filter);
						Logger.Error($"Error during jump gate jump:\n\tPHASE=POSTANIM\n\tGATE_CUBE_GRID={this.JumpGateGrid?.CubeGrid?.EntityId.ToString() ?? "N/A"}\n\tGATE_ID={this.JumpGateID}\n\tGATE_SIZE={drive_count}\n\tGATE_SIZE_WORKING={working_drive_count}\n\n\tSTACKTRACE:\n{e}");
					}

				});
			}
			catch (Exception e)
			{
				SendJumpResponse(MyJumpFailReason.UNKNOWN_ERROR, false, null);
				int drive_count = this.JumpGateGrid.GetDriveCount((drive) => drive.JumpGateID == this.JumpGateID);
				int working_drive_count = this.JumpGateGrid.GetDriveCount(working_src_drive_filter);
				Logger.Error($"Error during jump gate jump:\n\tPHASE=PREANIM\n\tGATE_CUBE_GRID={this.JumpGateGrid?.CubeGrid?.EntityId.ToString() ?? "N/A"}\n\tGATE_ID={this.JumpGateID}\n\tGATE_SIZE={drive_count}\n\tGATE_SIZE_WORKING={working_drive_count}\n\n\tSTACKTRACE:\n{e}");
			}
		}

		/// <summary>
		/// Does one update of this gate's non-threadable attributes<br />
		/// This will tick this gate's power syphon, physics collider, and sound emitters<br />
		/// <b>Must be called from game thread</b>
		/// </summary>
		public void UpdateNonThreadable()
		{
			this.CheckClosed();
			
			// Update physics collider
			if (this.JumpSpaceCollisionDetector != null && this.JumpSpaceCollisionDetector.MarkedForClose && this.JumpSpaceCollisionDetector.Closed)
			{
				MyAPIGateway.Entities.RemoveEntity(this.JumpSpaceCollisionDetector);
				this.JumpSpaceCollisionDetector = null;
			}
			else if (this.ForceUpdateCollider && this.JumpSpaceCollisionDetector != null && !this.JumpSpaceCollisionDetector.MarkedForClose) this.JumpSpaceCollisionDetector.Close();

			// Update jump space detector
			if (MyNetworkInterface.IsServerLike && this.JumpSpaceCollisionDetector == null && this.TrueLocalJumpEllipse != BoundingEllipsoidD.Zero && MyJumpGateModSession.Instance.AllSessionEntitiesLoaded && MyJumpGateModSession.Instance.AllFirstTickComplete())
			{
				this.JumpSpaceCollisionDetector = new MyEntity();
				this.JumpSpaceCollisionDetector.EntityId = 0;
				this.JumpSpaceCollisionDetector.Init(new StringBuilder($"JumpGate_{this.JumpGateGrid.CubeGrid.EntityId}_{this.JumpGateID}"), null, (MyCubeGrid) this.JumpGateGrid.CubeGrid, 1f);
				this.JumpSpaceCollisionDetector.Save = false;
				this.JumpSpaceCollisionDetector.Flags = EntityFlags.IsNotGamePrunningStructureObject | EntityFlags.NeedsWorldMatrix;
				PhysicsSettings settings = MyAPIGateway.Physics.CreateSettingsForDetector(this.JumpSpaceCollisionDetector, this.OnEntityCollision, MyJumpGateModSession.WorldMatrix, this.LocalJumpNode, VRage.Game.Components.RigidBodyFlag.RBF_KINEMATIC, 15, true);
				MyAPIGateway.Physics.CreateSpherePhysics(settings, (float) this.JumpEllipse.Radii.Max());
				MyAPIGateway.Entities.AddEntity(this.JumpSpaceCollisionDetector);
				Logger.Debug($"CREATED_COLLIDER JUMP_GATE={this.GetPrintableName()}, COLLIDER={this.JumpSpaceCollisionDetector.DisplayName}", 4);

				BoundingEllipsoidD jump_ellipse = this.JumpEllipse;
				BoundingSphereD bounding_sphere = new BoundingSphereD(jump_ellipse.WorldMatrix.Translation, jump_ellipse.Radii.Max());
				MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref bounding_sphere, this.ColliderPruningList);

				this.JumpSpaceColliderEntities.Clear();
				this.JumpSpaceEntities.Clear();

				foreach (MyEntity entity in this.ColliderPruningList)
				{
					MyEntity topmost = (MyEntity) entity.GetTopMostParent();
					MyJumpGateConstruct parent = null;
					if (topmost is MyCubeGrid && (parent = MyJumpGateModSession.Instance.GetUnclosedJumpGateGrid(topmost.EntityId)) != null) topmost = (MyCubeGrid) parent.CubeGrid;
					if (this.IsComplete() && !this.MarkClosed && topmost?.Physics != null && entity != this.JumpSpaceCollisionDetector && entity == topmost && !this.JumpGateGrid.HasCubeGrid(topmost.EntityId)) this.JumpSpaceColliderEntities[topmost.EntityId] = topmost;
				}

				this.ColliderPruningList.Clear();
			}
			
			this.ForceUpdateCollider = false;
			
			// Update power syphon
			this.PowerSyphon.Tick();
			
			// Update sound emitters
			if (MyNetworkInterface.IsMultiplayerClient || MyNetworkInterface.IsSingleplayer)
			{
				foreach (KeyValuePair<ulong, KeyValuePair<Vector3D?, MySoundEmitter3D>> pair in this.SoundEmitters)
				{
					MySoundEmitter3D sound_emitter = pair.Value.Value;
					if (sound_emitter.Closed) this.SoundEmitters.Remove(pair.Key);
					else if (!sound_emitter.IsPlaying()) sound_emitter.Dispose();
					else sound_emitter.Update(pair.Value.Key ?? this.WorldJumpNode);
				}
			}
			
			Vector3D camera_pos = MyAPIGateway.Session.Camera.Position;
			double distance = Vector3D.Distance(camera_pos, this.WorldJumpNode);
			
			// Draw shear blocks
			if (MyJumpGateModSession.Configuration.GeneralConfiguration.HighlightShearBlocks && this.Phase == MyJumpGatePhase.JUMPING && this.Status == MyJumpGateStatus.OUTBOUND && distance <= MyJumpGateModSession.Configuration.GeneralConfiguration.DrawSyncDistance)
			{
				BoundingBoxD localbox;
				Color warn_color = Color.Red;
				MyStringId warn_material = MyStringId.GetOrCompute("IOTA.SpecialEffects.BlockWarningOutline");
				MyStringId line_material = MyStringId.GetOrCompute("WeaponLaser");

				foreach (IMySlimBlock block in this.ShearBlocks)
				{
					MatrixD world_matrix = block.CubeGrid.WorldMatrix;
					BoundingSphereD sphere = new BoundingSphereD(block.Position * block.CubeGrid.GridSize, block.CubeGrid.GridSize / 2f * 1.025d);
					BoundingBoxD.CreateFromSphere(ref sphere, out localbox);
					MySimpleObjectDraw.DrawTransparentBox(ref world_matrix, ref localbox, ref warn_color, MySimpleObjectRasterizer.SolidAndWireframe, 1, 0.01f, warn_material, line_material, intensity: 10);
				}
			}
			
			// Display notifications
			if (MyNetworkInterface.IsClientLike && this.IsPointInHudBroadcastRange(ref camera_pos))
			{
				if (this.AutoActivateStartTime != null && this.AutoActivationNotif != null && this.Controller != null)
				{
					float auto_activation_delay = this.Controller.BlockSettings.AutoActivationDelay();
					this.AutoActivationNotif.Hide();
					this.AutoActivationNotif.ResetAliveTime();
					this.AutoActivationNotif.Text = $"Gate Auto Activation in {Math.Max(0, Math.Round(auto_activation_delay - (DateTime.UtcNow - this.AutoActivateStartTime.Value).TotalSeconds, 2))} seconds";
					this.AutoActivationNotif.Show();
				}
				else this.AutoActivationNotif?.Hide();

				if (this.HasBlocksWithinShearZone && this.Phase == MyJumpGatePhase.JUMPING && this.Status == MyJumpGateStatus.OUTBOUND) this.ShearBlocksWarning.Show();
				else this.ShearBlocksWarning.Hide();
			}
		}

		/// <summary>
		/// Does one update of this gate<br />
		/// This will close this construct if:<br />
		///  ... The session is not running<br />
		///  ... This gate is not valid<br />
		/// </summary>
		public void Update(bool update_ellipses = false, bool update_entities = false)
		{
			this.CheckClosed();

			// Check update validity
			if (!Monitor.TryEnter(this.UpdateLock)) return;

			try
			{
				bool grid_invalid = (!this.JumpGateGrid?.IsValid() ?? true) || this.MarkClosed || this.JumpGateGrid.IsSuspended;

				if ((grid_invalid && this.Status != MyJumpGateStatus.OUTBOUND && this.Status != MyJumpGateStatus.SWITCHING && this.Status != MyJumpGateStatus.CANCELLED) || MyJumpGateModSession.SessionStatus != MySessionStatusEnum.RUNNING)
				{
					List<MyJumpGateAnimation> this_animations = new List<MyJumpGateAnimation>();
					MyJumpGateModSession.Instance.GetGateAnimationsPlaying(this_animations, this);
					foreach (MyJumpGateAnimation animation in this_animations) MyJumpGateModSession.Instance.StopAnimation(animation);
					MyJumpGateModSession.Instance.CloseGate(this);
					return;
				}
				else if (grid_invalid && (this.Status == MyJumpGateStatus.OUTBOUND || this.Status == MyJumpGateStatus.SWITCHING || this.Status == MyJumpGateStatus.CANCELLED)) return;

				if (this.JumpGateConfiguration == null) this.Init();
				if (this.Controller?.IsClosed() ?? false) this.Controller = null;
				this.ConstructMatrix = this.JumpGateGrid.CubeGrid.WorldMatrix;
				this.TrueWorldJumpEllipse = this.TrueLocalJumpEllipse.ToWorldSpace(ref this.ConstructMatrix);

				// Update jump space ellipsoid
				if (update_ellipses || this.ForceUpdateJumpEllipsoid || this.TrueLocalJumpEllipse == BoundingEllipsoidD.Zero)
				{
					int most_aligned_plane = 0;
					int largest_plane = 0;
					List<MyJumpGateDrive> drives = new List<MyJumpGateDrive>();
					this.GetJumpGateDrives(drives);
					this.ForceUpdateJumpEllipsoid = false;
					Vector3D jump_node = this.WorldJumpNode;

					for (int start_index = 0; start_index < drives.Count; ++start_index)
					{
						MyJumpGateDrive drive1 = drives[start_index];
						Vector3D pos1 = drive1.GetDriveRaycastStartpoint();

						for (int i = start_index + 1; i < drives.Count; ++i)
						{
							MyJumpGateDrive drive2 = drives[i];
							Vector3D pos2 = drive2.GetDriveRaycastStartpoint();
							PlaneD drive_plane = new PlaneD(pos1, pos2, jump_node);
							if (!drive_plane.Normal.IsValid()) continue;
							Vector3D normal = drive_plane.Normal;
							Vector3D normal_inv = -normal;
							Vector3D primary_normal = (Vector3D.Angle(normal, this.ConstructMatrix.Up) < MathHelperD.PiOver2) ? normal : -normal_inv;
							JumpDriveIntersectPlane plane;

							if (this.DriveIntersectionPlanes.ContainsKey(primary_normal)) plane = this.DriveIntersectionPlanes[primary_normal];
							else
							{
								plane = new JumpDriveIntersectPlane();
								plane.Plane = drive_plane;
								plane.PlanePrimaryNormal = primary_normal;
								this.DriveIntersectionPlanes.Add(primary_normal, plane);
							}

							plane.JumpGateDrives.Add(drive1);
							plane.JumpGateDrives.Add(drive2);

							Vector3D direction = drive1.TerminalBlock.WorldMatrix.Up;
							double deviation = Math.Abs(Vector3D.Angle(direction, primary_normal)) % Math.PI;
							if (deviation >= 3.132866 || deviation <= 0.00872665) ++plane.AlignedDrivesCount;

							direction = drive2.TerminalBlock.WorldMatrix.Up;
							deviation = Math.Abs(Vector3D.Angle(direction, primary_normal)) % Math.PI;
							if (deviation >= 3.132866 || deviation <= 0.00872665) ++plane.AlignedDrivesCount;

							most_aligned_plane = Math.Max(most_aligned_plane, plane.AlignedDrivesCount);
							largest_plane = Math.Max(largest_plane, plane.JumpGateDrives.Count);
						}
					}

					if (this.DriveIntersectionPlanes.Count > 0)
					{
						List<JumpDriveIntersectPlane> intersection_planes = new List<JumpDriveIntersectPlane>();

						foreach (KeyValuePair<Vector3D, JumpDriveIntersectPlane> pair in this.DriveIntersectionPlanes)
						{
							if (pair.Value.AlignedDrivesCount == most_aligned_plane && pair.Value.JumpGateDrives.Count == largest_plane)
							{
								intersection_planes.Add(pair.Value);
							}
						}

						JumpDriveIntersectPlane intersection_plane = intersection_planes.OrderBy((plane) => (plane.PlanePrimaryNormal - this.ConstructMatrix.Forward).Length()).FirstOrDefault();

						if (intersection_plane == null)
						{
							this.TrueLocalJumpEllipse = BoundingEllipsoidD.Zero;
							this.TrueWorldJumpEllipse = BoundingEllipsoidD.Zero;
							this.DriveIntersectionPlanes.Clear();
							this.PrimaryDrivePlane = null;
							return;
						}

						double plane_height = MyJumpGateDrive.ModelBoundingBoxSize.Y;
						double closest_distance = intersection_plane.JumpGateDrives.Select((drive) => Vector3D.Distance(drive.GetDriveRaycastStartpoint(), jump_node)).Min();
						Vector3D forward = Vector3D.Cross(intersection_plane.PlanePrimaryNormal, this.ConstructMatrix.Forward);
						MatrixD plane_matrix = MatrixD.CreateWorld(jump_node, forward, intersection_plane.PlanePrimaryNormal);

						foreach (MyJumpGateDrive drive in drives)
						{
							if (intersection_plane.JumpGateDrives.Contains(drive)) continue;
							Vector3D local_pos = MyJumpGateModSession.WorldVectorToLocalVectorP(ref plane_matrix, drive.GetDriveRaycastStartpoint());
							if (!double.IsNaN(local_pos.Y)) plane_height = Math.Max(plane_height, Math.Abs(local_pos.Y));
						}

						this.PrimaryDrivePlane = intersection_plane.Plane;
						Vector3D radii = new Vector3D(closest_distance, plane_height, closest_distance);
						this.TrueWorldJumpEllipse = new BoundingEllipsoidD(ref radii, ref plane_matrix);
						this.TrueLocalJumpEllipse = this.TrueWorldJumpEllipse.ToLocalSpace(ref this.ConstructMatrix);
					}
					else
					{
						this.PrimaryDrivePlane = null;
						this.TrueLocalJumpEllipse = BoundingEllipsoidD.Zero;
						this.TrueWorldJumpEllipse = BoundingEllipsoidD.Zero;
					}

					this.DriveIntersectionPlanes.Clear();
				}

				// Update jump space entities
				if (MyNetworkInterface.IsServerLike && update_entities)
				{
					BoundingEllipsoidD jump_ellipse = this.GetEffectiveJumpEllipse();
					List<MyEntity> closed_entities = new List<MyEntity>();
					MyJumpGateConstruct parent;

					foreach (KeyValuePair<long, MyEntity> pair in this.JumpSpaceColliderEntities)
					{
						MyEntity entity = pair.Value;

						if (entity?.Physics == null || entity.MarkedForClose)
						{
							closed_entities.Add(entity);
							continue;
						}

						MyEntity add_entity = null;
						float? mass = null;

						if (entity is MyCubeGrid && (parent = MyJumpGateModSession.Instance.GetUnclosedJumpGateGrid(entity.EntityId)) != null && !parent.IsStatic() && parent.IsConstructWithinBoundingEllipsoid(ref jump_ellipse))
						{
							add_entity = (MyCubeGrid) parent.CubeGrid;
							mass = (float) parent.ConstructMass();
						}
						else if (entity is IMyCharacter && jump_ellipse.IsPointInEllipse(entity.WorldMatrix.Translation))
						{
							add_entity = entity;
							mass = ((IMyCharacter) entity).CurrentMass;
						}
						else if (!(entity is MyCubeGrid)) add_entity = (jump_ellipse.IsPointInEllipse(entity.WorldMatrix.Translation)) ? entity : null;

						float old_mass;
						float new_mass = mass ?? add_entity?.Physics.Mass ?? float.NaN;

						if (this.JumpSpaceEntities.ContainsKey(entity.EntityId) && add_entity == null) closed_entities.Add(entity);
						else if (add_entity != null && this.JumpSpaceEntities.TryGetValue(add_entity.EntityId, out old_mass) && old_mass != new_mass) this.JumpSpaceEntities[add_entity.EntityId] = new_mass;
						else if (add_entity != null && this.JumpSpaceEntities.TryAdd(add_entity.EntityId, new_mass)) this.OnEntityJumpSpaceEnter(add_entity);
					}

					float _;

					foreach (MyEntity entity in closed_entities)
					{
						this.JumpSpaceColliderEntities.Remove(entity.EntityId);
						if (this.JumpSpaceEntities.TryRemove(entity.EntityId, out _)) this.OnEntityJumpSpaceLeave(entity);
					}
				}

				// Check auto activation
				if (MyNetworkInterface.IsServerLike && this.Controller != null && !this.Controller.MarkedForClose && this.Controller.BlockSettings.CanAutoActivate() && this.Phase == MyJumpGatePhase.IDLE && this.Controller.BlockSettings.SelectedWaypoint() != null)
				{
					double total_mass = this.JumpSpaceEntities.Sum((pair) => (double) pair.Value);
					double total_power = this.CalculateTotalRequiredPower(mass: total_mass);
					KeyValuePair<float, float> required_mass = this.Controller.BlockSettings.AutoActivateMass();
					KeyValuePair<double, double> required_power = this.Controller.BlockSettings.AutoActivatePower();
					bool check_pass = total_mass > required_mass.Key && total_mass < required_mass.Value && total_power > required_power.Key && total_power < required_power.Value;
					float activation_delay = this.Controller.BlockSettings.AutoActivationDelay();
					DateTime now = DateTime.UtcNow;
					
					if (total_mass == 0 || this.JumpSpaceEntities.Count == 0 || !check_pass)
					{
						if (MyJumpGateModSession.Network.Registered && this.AutoActivateStartTime != null)
						{
							MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet {
								PacketType = MyPacketTypeEnum.AUTOACTIVATE,
								TargetID = 0,
								Broadcast = true,
							};

							packet.Payload(new KeyValuePair<Guid, DateTime?>(JumpGateUUID.FromJumpGate(this).ToGuid(), this.AutoActivateStartTime));
							packet.Send();
						}

						this.AutoActivateStartTime = null;
					}
					else if (this.AutoActivateStartTime == null && activation_delay == 0) this.JumpWrapper(this.Controller.BlockSettings, null, null, MyJumpTypeEnum.STANDARD);
					else if (this.AutoActivateStartTime == null)
					{
						this.AutoActivateStartTime = now;

						if (MyJumpGateModSession.Network.Registered)
						{
							MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet {
								PacketType = MyPacketTypeEnum.AUTOACTIVATE,
								TargetID = 0,
								Broadcast = true,
							};

							packet.Payload(new KeyValuePair<Guid, DateTime?>(JumpGateUUID.FromJumpGate(this).ToGuid(), this.AutoActivateStartTime));
							packet.Send();
						}
					}
					else if (this.AutoActivateStartTime != null && (now - this.AutoActivateStartTime.Value).TotalSeconds >= activation_delay) this.JumpWrapper(this.Controller.BlockSettings, null, null, MyJumpTypeEnum.STANDARD);
				}
				else if (MyNetworkInterface.IsServerLike && this.AutoActivateStartTime != null) this.AutoActivateStartTime = null;

				// Check lenient jump blocks
				if (this.Controller != null && !MyJumpGateModSession.Configuration.GeneralConfiguration.LenientJumps && MyJumpGateModSession.GameTick % 60 == 0)
				{
					this.ShearBlocks.Clear();
					BoundingEllipsoidD jump_ellipse = this.JumpEllipse;
					BoundingEllipsoidD shear_ellipse = this.ShearEllipse;

					foreach (KeyValuePair<long, float> pair in this.JumpSpaceEntities)
					{
						MyJumpGateConstruct grid = MyJumpGateModSession.Instance.GetJumpGateGrid(pair.Key);
						if (grid == null) continue;
						IEnumerator<IMySlimBlock> iterator = grid.GetConstructBlocks();

						while (iterator.MoveNext())
						{
							IMySlimBlock block = iterator.Current;
							Vector3D pos = block.CubeGrid.GridIntegerToWorld(block.Position);
							if (shear_ellipse.IsPointInEllipse(pos) && !jump_ellipse.IsPointInEllipse(pos)) this.ShearBlocks.Add(block);
						}
					}
				}

				// Update Jump Node Velocity
				this.OldPosition = this.NewPosition;
				this.NewPosition = this.LocalJumpNode;
				DateTime new_time = DateTime.UtcNow;

				if (this.OldPosition != this.NewPosition)
				{
					double time_delta = (new_time - this.LastScanTime).TotalSeconds;
					this.JumpNodeVelocity = (this.NewPosition - this.OldPosition) / time_delta;
				}

				this.LastScanTime = new_time;

				// Clean Invalid
				if (!this.IsValid())
				{
					Logger.Debug($"Invalid Jump Gate {(this.JumpGateGrid?.CubeGrid?.EntityId.ToString() ?? "N/A")}::{this.JumpGateID} - {this.GetInvalidationReason()}", 2);
					this.Status = MyJumpGateStatus.NONE;
					this.Phase = MyJumpGatePhase.NONE;
					this.Dispose();
				}
				else if (this.Controller == null)
				{
					this.Status = MyJumpGateStatus.IDLE;
					this.Phase = MyJumpGatePhase.IDLE;
				}

				if (this.IsDirty && MyJumpGateModSession.Network.Registered) this.SendNetworkJumpGateUpdate();
			}
			finally
			{
				if (Monitor.IsEntered(this.UpdateLock)) Monitor.Exit(this.UpdateLock);
			}
		}

		/// <summary>
		/// Markes this construct for close
		/// </summary>
		public void Dispose()
		{
			this.CheckClosed();
			this.MarkClosed = true;
			if (MyJumpGateModSession.SessionStatus == MySessionStatusEnum.UNLOADING) MyJumpGateModSession.Instance.CloseGate(this);
		}

		/// <summary>
		/// Marks this jump gate's physics collider as dirty<br />
		/// It will be updated on next physics tick
		/// </summary>
		public void SetColliderDirty()
		{
			this.CheckClosed();
			this.ForceUpdateCollider = true;
		}

		/// <summary>
		/// Marks this jump gate's jump ellipsoid as dirty<br />
		/// It will be updated on next tick
		/// </summary>
		public void SetJumpSpaceEllipsoidDirty()
		{
			this.CheckClosed();
			this.ForceUpdateJumpEllipsoid = true;
		}

		/// <summary>
		/// Stops all sound emitters with the specified sound
		/// </summary>
		/// <param name="sound">The sound to stop</param>
		public void StopSound(string sound)
		{
			this.CheckClosed();
			if (!MyNetworkInterface.IsMultiplayerClient && !MyNetworkInterface.IsSingleplayer) return;

			foreach (KeyValuePair<ulong, KeyValuePair<Vector3D?, MySoundEmitter3D>> pair in this.SoundEmitters)
			{
				MySoundEmitter3D sound_emitter = pair.Value.Value;
				if (sound_emitter.Sound == sound) sound_emitter.Dispose();
			}
		}

		/// <summary>
		/// Stops the sound emitter with the specified ID
		/// </summary>
		/// <param name="sound_id">The sound emitter ID to stop</param>
		public void StopSound(ulong? sound_id)
		{
			this.CheckClosed();
			if ((!MyNetworkInterface.IsMultiplayerClient && !MyNetworkInterface.IsSingleplayer) || sound_id == null) return;
			KeyValuePair<Vector3D?, MySoundEmitter3D> pair;
			if (this.SoundEmitters.TryGetValue(sound_id.Value, out pair)) pair.Value?.Dispose();
		}

		/// <summary>
		/// Sets the volume for the specified sound emitter
		/// </summary>
		/// <param name="sound_id">The ID of the sound emitter</param>
		/// <param name="volume">The new volume</param>
		public void SetSoundVolume(ulong? sound_id, float volume)
		{
			this.CheckClosed();
			if ((!MyNetworkInterface.IsMultiplayerClient && !MyNetworkInterface.IsSingleplayer) || sound_id == null) return;
			KeyValuePair<Vector3D?, MySoundEmitter3D> pair;
			if (this.SoundEmitters.TryGetValue(sound_id.Value, out pair)) pair.Value?.SetVolume(Math.Max(0, volume));
		}

		/// <summary>
		/// Sets the range for the specified sound emitter
		/// </summary>
		/// <param name="sound_id">The ID of the sound emitter</param>
		/// <param name="volume">The new range or null</param>
		public void SetSoundDistance(ulong? sound_id, float? distance)
		{
			this.CheckClosed();
			if ((!MyNetworkInterface.IsMultiplayerClient && !MyNetworkInterface.IsSingleplayer) || sound_id == null) return;
			KeyValuePair<Vector3D?, MySoundEmitter3D> pair;
			if (this.SoundEmitters.TryGetValue(sound_id.Value, out pair)) pair.Value?.SetDistance((distance == null) ? distance : Math.Max(0, distance.Value));
		}

		/// <summary>
		/// Stops all sounds from this gate
		/// </summary>
		public void StopSounds()
		{
			this.CheckClosed();
			if (!MyNetworkInterface.IsMultiplayerClient && !MyNetworkInterface.IsSingleplayer) return;
			foreach (KeyValuePair<ulong, KeyValuePair<Vector3D?, MySoundEmitter3D>> pair in this.SoundEmitters) pair.Value.Value.Dispose();
		}

		/// <summary>
		/// Updates the public list of drive intersect nodes
		/// </summary>
		/// <param name="intersect_nodes">The new intersections list</param>
		public void UpdateDriveIntersectNodes(IEnumerable<Vector3D> intersect_nodes)
		{
			lock (this.DriveIntersectNodesMutex)
			{
				this.InnerDriveIntersectNodes.Clear();
				if (intersect_nodes == null) return;

				foreach (Vector3D node in intersect_nodes)
				{
					bool add = true;
					Vector3D node1 = MyJumpGateModSession.WorldVectorToLocalVectorP(ref this.ConstructMatrix, node);

					foreach (Vector3D node2 in this.InnerDriveIntersectNodes)
					{
						if (Vector3D.Distance(node1, node2) < 1)
						{
							add = false;
							break;
						}
					}

					if (add) this.InnerDriveIntersectNodes.Add(node1);
				}
			}
		}

		/// <summary>
		/// Syphons power from the parent MyJumpGateConstruct
		/// </summary>
		/// <param name="power_mw">The power to draw (will be split amongst all working drives for this gate and across the time specified)</param>
		/// <param name="ticks">The duration of the test in game ticks</param>
		/// <param name="callback">The callback to pass the result to, accepting a single bool representing whether all power was draw successfully</param>
		/// <param name="syphon_grid_only">If true, skips drive and capacitor power syphon</param>
		public void CanDoSyphonGridPower(double power_mw, ulong ticks, Action<bool> callback, bool syphon_grid_only = false)
		{
			this.CheckClosed();
			this.PowerSyphon.DoSyphonPower(power_mw, ticks, callback, syphon_grid_only);
		}

		/// <summary>
		/// Marks this gate as inbound, receiving a connection from the specified gate
		/// </summary>
		/// <param name="source">Null to reset this gate's status or the gate connecting to this one</param>
		/// <exception cref="InvalidOperationException">If this gate is not IDLE</exception>
		public void ConnectAsInbound(MyJumpGate source)
		{
			this.CheckClosed();

			if (source == null || !source.IsValid())
			{
				this.Reset();
				this.SenderGate = null;
			}
			else if (this.IsIdle())
			{
				this.Status = MyJumpGateStatus.INBOUND;
				this.Phase = MyJumpGatePhase.JUMPING;
				this.SenderGate = source;
			}
			else throw new InvalidOperationException("Failed to set inbound");
		}

		/// <summary>
		/// Sets this gate's configuration data
		/// </summary>
		public void Init()
		{
			this.CheckClosed();
			this.JumpGateConfiguration = new Configuration.LocalJumpGateConfiguration(this, MyJumpGateModSession.Configuration.JumpGateConfiguration);
		}

		/// <summary>
		/// Cancels the gate's jump
		/// For servers and singleplayer: Cancels the jump<br />
		/// For clients: Sends a cancel jump request to server
		/// </summary>
		public void CancelJump()
		{
			this.CheckClosed();

			if (MyNetworkInterface.IsServerLike)
			{
				this.Status = MyJumpGateStatus.CANCELLED;
				return;
			}

			MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet {
				PacketType = MyPacketTypeEnum.JUMP_GATE_JUMP,
				TargetID = 0,
				Broadcast = false,
			};

			packet.Payload(new JumpGateInfo {
				CancelOverride = true,
				Type = JumpGateInfo.TypeEnum.JUMP_FAIL,
				JumpGateID = JumpGateUUID.FromJumpGate(this).ToGuid(),
				ControllerSettings = this.Controller?.BlockSettings,
			});
			packet.Send();
		}

		/// <summary>
		/// Gets a list of all drives attached to this gate
		/// </summary>
		/// <param name="drives">All attached jump gate drives<br />List will not be cleared</param>
		/// <param name="filter">A predicate to match jump gate drives against</param>
		public void GetJumpGateDrives(List<MyJumpGateDrive> drives, Func<MyJumpGateDrive, bool> filter = null)
		{
			this.CheckClosed();
			this.JumpGateGrid?.GetAttachedJumpGateDrives(drives, (drive) => drive.JumpGateID == this.JumpGateID && !drive.IsClosed() && (filter == null || filter(drive)));
		}

		/// <summary>
		/// Gets a list of all drives attached to this gate as the specified type
		/// </summary>
		/// <param name="drives">All attached jump gate drives<br />List will not be cleared</param>
		/// <param name="transformer">A transformer converting a controller to the specified type</param>
		/// <param name="filter">A predicate to match jump gate drives against</param>
		public void GetJumpGateDrives<T>(List<T> drives, Func<MyJumpGateDrive, T> transformer, Func<MyJumpGateDrive, bool> filter = null)
		{
			this.CheckClosed();
			this.JumpGateGrid?.GetAttachedJumpGateDrives(drives, transformer, (drive) => drive.JumpGateID == this.JumpGateID && !drive.IsClosed() && (filter == null || filter(drive)));
		}

		/// <summary>
		/// Gets a list of all working drives attached to this gate
		/// </summary>
		/// <param name="drives">All attached jump gate drives<br />List will not be cleared</param>
		/// <param name="filter">A predicate to match jump gate drives against</param>
		public void GetWorkingJumpGateDrives(List<MyJumpGateDrive> drives, Func<MyJumpGateDrive, bool> filter = null)
		{
			this.CheckClosed();
			this.JumpGateGrid?.GetAttachedJumpGateDrives(drives, (drive) => drive.JumpGateID == this.JumpGateID && drive.IsWorking() && (filter == null || filter(drive)));
		}

		/// <summary>
		/// Gets a list of all working drives attached to this gate as the specified type
		/// </summary>
		/// <param name="drives">All attached jump gate drives<br />List will not be cleared</param>
		/// <param name="transformer">A transformer converting a controller to the specified type</param>
		/// <param name="filter">A predicate to match jump gate drives against</param>
		public void GetWorkingJumpGateDrives<T>(List<T> drives, Func<MyJumpGateDrive, T> transformer, Func<MyJumpGateDrive, bool> filter = null)
		{
			this.CheckClosed();
			this.JumpGateGrid?.GetAttachedJumpGateDrives(drives, transformer, (drive) => drive.JumpGateID == this.JumpGateID && drive.IsWorking() && (filter == null || filter(drive)));
		}

		/// <summary>
		/// Gets all entities within this gate's jump space ellipsoid not yet initialized on client
		/// </summary>
		/// <param name="entities">All unconfirmed entities within this jump space<br />Dictionary will not be cleared</param>
		/// <param name="filtered">Whether to remove blacklisted entities per this gate's controller</param>
		public void GetUninitializedEntititesInJumpSpace(IDictionary<long, float> entities, bool filtered = false)
		{
			this.CheckClosed();

			if (filtered && this.Controller != null)
			{
				foreach (KeyValuePair<long, float> pair in this.JumpSpaceEntities)
				{
					IMyEntity entity = MyAPIGateway.Entities.GetEntityById(pair.Key);
					if (entity == null && !this.Controller.BlockSettings.IsEntityBlacklisted(pair.Key)) entities.Add(pair.Key, pair.Value);
				}
			}
			else
			{
				foreach (KeyValuePair<long, float> pair in this.JumpSpaceEntities)
				{
					IMyEntity entity = MyAPIGateway.Entities.GetEntityById(pair.Key);
					if (entity == null) entities.Add(pair.Key, pair.Value);
				}
			}
		}

		/// <summary>
		/// Gets all entities within this gate's jump space ellipsoid
		/// </summary>
		/// <param name="entities">All entities within this jump space<br />Dictionary will not be cleared</param>
		/// <param name="filtered">Whether to remove blacklisted entities per this gate's controller</param>
		public void GetEntitiesInJumpSpace(IDictionary<MyEntity, float> entities, bool filtered = false)
		{
			this.CheckClosed();

			if (filtered && this.Controller != null)
			{
				foreach (KeyValuePair<long, float> pair in this.JumpSpaceEntities)
				{
					MyEntity entity = (MyEntity) MyAPIGateway.Entities.GetEntityById(pair.Key);
					if (entity != null && !this.Controller.BlockSettings.IsEntityBlacklisted(entity.EntityId) && this.IsEntityValidForJumpSpace(entity)) entities.Add(entity, pair.Value);
				}
			}
			else
			{
				foreach (KeyValuePair<long, float> pair in this.JumpSpaceEntities)
				{
					MyEntity entity = (MyEntity) MyAPIGateway.Entities.GetEntityById(pair.Key);
					if (entity != null) entities.Add(entity, pair.Value);
				}
			}
		}

		/// <summary>
		/// Registers a callback for when an entity enters or exits this gate's jump space<br />
		/// Callback parameters:<br />
		///  ... MyJumpGate - The colliding gate
		///  ... MyEntity - The entity entering or leaving
		///  ... bool - True if this entity is entering the jump space
		/// </summary>
		/// <param name="callback">The callback to call</param>
		public void OnEntityCollision(Action<MyJumpGate, MyEntity, bool> callback)
		{
			this.CheckClosed();
			lock (this.EntityEnterCallbacks) if (!this.EntityEnterCallbacks.Contains(callback)) this.EntityEnterCallbacks.Add(callback);
		}

		/// <summary>
		/// Unregisters a callback for when an entity enters or exits this gate's jump space<br />
		/// Callback parameters:<br />
		///  ... MyJumpGate - The colliding gate
		///  ... MyEntity - The entity entering or leaving
		///  ... bool - True if this entity is entering the jump space
		/// </summary>
		/// <param name="callback">The callback to call</param>
		public void OffEntityCollision(Action<MyJumpGate, MyEntity, bool> callback)
		{
			this.CheckClosed();
			lock (this.EntityEnterCallbacks) this.EntityEnterCallbacks.Remove(callback);
		}

		/// <summary>
		/// Registers a callback for when an entity enters or exits this gate's jump space<br />
		/// Callback parameters:<br />
		///  ... MyJumpGate - The colliding gate
		///  ... MyEntity - The entity entering or leaving
		///  ... bool - True if this entity is entering the jump space
		/// </summary>
		/// <param name="callback">The callback to call</param>
		public void OnEntityCollision(Action<MyAPIJumpGate, MyEntity, bool> callback)
		{
			this.CheckClosed();
			lock (this.APIEntityEnterCallbacks) if (!this.APIEntityEnterCallbacks.Contains(callback)) this.APIEntityEnterCallbacks.Add(callback);
		}

		/// <summary>
		/// Unregisters a callback for when an entity enters or exits this gate's jump space<br />
		/// Callback parameters:<br />
		///  ... MyJumpGate - The colliding gate
		///  ... MyEntity - The entity entering or leaving
		///  ... bool - True if this entity is entering the jump space
		/// </summary>
		/// <param name="callback">The callback to call</param>
		public void OffEntityCollision(Action<MyAPIJumpGate, MyEntity, bool> callback)
		{
			this.CheckClosed();
			lock (this.APIEntityEnterCallbacks) this.APIEntityEnterCallbacks.Remove(callback);
		}

		/// <summary>
		/// Checks if the specified callback is registered with this gate
		/// </summary>
		/// <param name="callback">The callback to check</param>
		/// <returns>True if already registered</returns>
		public bool IsEntityCollisionCallbackRegistered(Action<MyJumpGate, MyEntity, bool> callback)
		{
			this.CheckClosed();
			lock (this.EntityEnterCallbacks) return this.EntityEnterCallbacks.Contains(callback);
		}

		/// <summary>
		/// Checks if the specified callback is registered with this gate
		/// </summary>
		/// <param name="callback">The callback to check</param>
		/// <returns>True if already registered</returns>
		public bool IsEntityCollisionCallbackRegistered(Action<MyAPIJumpGate, MyEntity, bool> callback)
		{
			this.CheckClosed();
			lock (this.APIEntityEnterCallbacks) return this.APIEntityEnterCallbacks.Contains(callback);
		}

		/// <summary>
		/// Updates this gate data from a serialized jump gate
		/// </summary>
		/// <param name="jump_gate">The serialized jump gate data</param>
		/// <returns>Whether this gate was updated</returns>
		public bool FromSerialized(MySerializedJumpGate jump_gate, bool force_update = false)
		{
			this.CheckClosed();
			JumpGateUUID gate_uuid = JumpGateUUID.FromGuid(jump_gate.UUID);
			if (!force_update && (this.MarkClosed || gate_uuid != JumpGateUUID.FromJumpGate(this)) || jump_gate.IsClientRequest) return false;

			lock (this.UpdateLock)
			{
				if (jump_gate.Closed || jump_gate.JumpGateGrid == Guid.Empty)
				{
					this.Dispose();
					return true;
				}

				JumpGateUUID jump_gate_grid = JumpGateUUID.FromGuid(jump_gate.JumpGateGrid);
				this.JumpGateID = gate_uuid.GetJumpGate();
				this.MarkClosed = false;
				this.Status = jump_gate.Status;
				this.Phase = jump_gate.Phase;
				this.LocalJumpNode = jump_gate.LocalJumpNode;
				this.TrueLocalJumpEllipse = BoundingEllipsoidD.FromSerialized(Convert.FromBase64String(jump_gate.LocalJumpEllipse), 0);
				this.TrueWorldJumpEllipse = this.TrueLocalJumpEllipse.ToWorldSpace(ref this.ConstructMatrix);
				this.JumpGateGrid = MyJumpGateModSession.Instance.GetJumpGateGrid(jump_gate_grid.GetJumpGateGrid());
				this.DriveGridSize = jump_gate.GridSize;
				this.ConstructMatrix = BoundingEllipsoidD.FromSerialized(Convert.FromBase64String(jump_gate.ConstructMatrix), 0).WorldMatrix;

				if (this.JumpGateGrid == null)
				{
					this.Dispose();
					return true;
				}

				this.JumpSpaceEntities.Clear();
				if (MyNetworkInterface.IsStandaloneMultiplayerClient && jump_gate.JumpSpaceEntities != null) foreach (KeyValuePair<long, float> pair in jump_gate.JumpSpaceEntities) this.JumpSpaceEntities.TryAdd(pair.Key, pair.Value);
				this.UpdateDriveIntersectNodes(jump_gate.IntersectNodes);
				this.Controller = (jump_gate.Controller == Guid.Empty) ? null : this.JumpGateGrid.GetController(JumpGateUUID.FromGuid(jump_gate.Controller).GetBlock());
				this.ServerAntenna = (jump_gate.ServerAntenna == Guid.Empty) ? null : this.JumpGateGrid.GetServerAntenna(JumpGateUUID.FromGuid(jump_gate.ServerAntenna).GetBlock());
				return true;
			}
		}

		/// <summary>
		/// Checks if a point is within twice the distance of the largest jump ellipse radius or within 500 meters of the attached controller
		/// </summary>
		/// <param name="pos">The world position to check</param>
		/// <returns>True if in range</returns>
		public bool IsPointInHudBroadcastRange(ref Vector3D pos)
		{
			this.CheckClosed();
			BoundingEllipsoidD jump_ellipse = this.JumpEllipse;
			return new BoundingSphereD(jump_ellipse.WorldMatrix.Translation, jump_ellipse.Radii.Max() * 2).Contains(pos) == ContainmentType.Contains || (this.Controller != null && new BoundingSphereD(this.Controller.WorldMatrix.Translation, 50).Contains(pos) == ContainmentType.Contains);
		}

		/// <summary>
		/// Checks if entity passes all controller filters and is allowed by gate
		/// </summary>
		/// <param name="entity">The entity to check</param>
		/// <returns>True if entity can be jumped by this gate</returns>
		public bool IsEntityValidForJumpSpace(MyEntity entity)
		{
			this.CheckClosed();
			if (this.Controller == null) return false;
			KeyValuePair<float, float> allowed_mass = this.Controller.BlockSettings.AllowedEntityMass();
			KeyValuePair<uint, uint> allowed_size = this.Controller.BlockSettings.AllowedCubeGridSize();
			if (entity == null || entity.Physics == null || entity.MarkedForClose || entity.Closed) return false;
			else if (entity.Physics.Mass < allowed_mass.Key || entity.Physics.Mass > allowed_mass.Value) return false;
			else if (this.Controller.BlockSettings.IsEntityBlacklisted(entity.EntityId)) return false;
			else if (entity is MyCubeGrid)
			{
				IMyCubeGrid grid = (MyCubeGrid) entity;
				MyJumpGateConstruct construct = MyJumpGateModSession.Instance.GetJumpGateGrid(grid);
				int count = construct?.GetConstructBlockCount() ?? -1;
				if (construct == null || count < allowed_size.Key || count > allowed_size.Value) return false;
			}
			return true;
		}

		/// <summary>
		/// Whether this grid is valid
		/// </summary>
		/// <returns>True if valid</returns>
		public bool IsValid()
		{
			return this.GetInvalidationReason() == MyGateInvalidationReason.NONE;
		}

		/// <summary>
		/// Whether this grid is valid and a controller attached
		/// </summary>
		/// <returns>True if complete</returns>
		public bool IsComplete()
		{
			return this.IsValid() && this.Controller != null;
		}

		/// <summary>
		/// </summary>
		/// <returns>True if this gate is idle</returns>
		public bool IsIdle()
		{
			this.CheckClosed();
			return this.Status == MyJumpGateStatus.IDLE && this.Phase == MyJumpGatePhase.IDLE;
		}

		/// <summary>
		/// </summary>
		/// <returns>True if this gate is inbound or outbound</returns>
		public bool IsJumping()
		{
			this.CheckClosed();
			return this.Phase == MyJumpGatePhase.JUMPING && (this.Status == MyJumpGateStatus.OUTBOUND || this.Status == MyJumpGateStatus.INBOUND);
		}

		/// <summary>
		/// </summary>
		/// <returns>True if this gate is large grid</returns>
		public bool IsLargeGrid()
		{
			this.CheckClosed();
			if (this.DriveGridSize == null) return this.JumpGateGrid.GetFirstDrive((drive) => drive.IsLargeGrid) != null;
			else return this.DriveGridSize.Value == MyCubeSize.Large;
		}

		/// <summary>
		/// </summary>
		/// <returns>True if this gate is small grid</returns>
		public bool IsSmallGrid()
		{
			this.CheckClosed();
			if (this.DriveGridSize == null) return this.JumpGateGrid.GetFirstDrive((drive) => drive.IsSmallGrid) != null;
			else return this.DriveGridSize.Value == MyCubeSize.Small;
		}

		/// <summary>
		/// Checks if this MyJumpGate equals another
		/// </summary>
		/// <param name="other">The MyJumpGate to check</param>
		/// <returns>Equality</returns>
		public bool Equals(MyJumpGate other)
		{
			return other != null && this.JumpGateGrid == other.JumpGateGrid && this.JumpGateID == other.JumpGateID;
		}

		/// <summary>
		/// </summary>
		/// <returns>The status of this gate's jump space collision detector</returns>
		public MyGateColliderStatus JumpSpaceColliderStatus()
		{
			return (this.JumpSpaceCollisionDetector == null) ? MyGateColliderStatus.NONE : (this.JumpSpaceCollisionDetector.MarkedForClose) ? MyGateColliderStatus.CLOSED : MyGateColliderStatus.ATTACHED;
		}

		/// <summary>
		/// Gets the reason this gate is invalid
		/// </summary>
		/// <returns>The invalidation reason or InvalidationReason.NONE</returns>
		public MyGateInvalidationReason GetInvalidationReason()
		{
			if (this.Closed) return MyGateInvalidationReason.CLOSED;
			else if (this.JumpGateGrid == null) return MyGateInvalidationReason.NULL_GRID;
			else if (this.Status == MyJumpGateStatus.NONE) return MyGateInvalidationReason.NULL_STATUS;
			else if (this.Phase == MyJumpGatePhase.NONE) return MyGateInvalidationReason.NULL_PHASE;
			else if (this.JumpGateID < 0) return MyGateInvalidationReason.INVALID_ID;
			else if (this.InnerDriveIntersectNodes.Count < 1) return MyGateInvalidationReason.INSUFFICIENT_NODES;
			else if (this.MarkClosed) return MyGateInvalidationReason.CLOSED;
			else if (this.JumpGateGrid.GetDriveCount((drive) => drive.JumpGateID == this.JumpGateID) < 2) return MyGateInvalidationReason.INSUFFICIENT_DRIVES;
			else return MyGateInvalidationReason.NONE;
		}

		/// <summary>
		/// Get the number of jump space entities matching the specified predicate or all entities if predicate is null
		/// </summary>
		/// <param name="filtered">Whether to filter entities based on controller settings</param>
		/// <param name="filter">A predicate to filter entities by</param>
		/// <returns>The number of matching entities</returns>
		public int GetJumpSpaceEntityCount(bool filtered = false, Func<MyEntity, bool> filter = null)
		{
			this.CheckClosed();
			int count = 0;

			if (filtered && this.Controller != null)
			{
				foreach (KeyValuePair<long, float> pair in this.JumpSpaceEntities)
				{
					MyEntity entity = (MyEntity) MyAPIGateway.Entities.GetEntityById(pair.Key);
					if (entity != null && !this.Controller.BlockSettings.IsEntityBlacklisted(entity.EntityId) && this.IsEntityValidForJumpSpace(entity) && (filter == null || filter(entity))) ++count;
				}
			}
			else
			{
				foreach (KeyValuePair<long, float> pair in this.JumpSpaceEntities)
				{
					MyEntity entity = (MyEntity) MyAPIGateway.Entities.GetEntityById(pair.Key);
					if (entity != null && (filter == null || filter(entity))) ++count;
				}
			}

			return count;
		}

		/// <summary>
		/// Plays a sound from this gate
		/// </summary>
		/// <param name="sound">The sound to play</param>
		/// <param name="distance">The sound range or null</param>
		/// <param name="volume">The sound volume</param>
		/// <param name="pos">The world position to play this sound at</param>
		/// <returns>The sound emitter ID or null if failed</returns>
		public ulong? PlaySound(string sound, float? distance = null, float volume = 1, Vector3D? pos = null)
		{
			this.CheckClosed();
			if (!MyNetworkInterface.IsMultiplayerClient && !MyNetworkInterface.IsSingleplayer) return null;
			ulong sound_id = this.SoundEmitterID++;
			if (this.SoundEmitters.TryAdd(sound_id, new KeyValuePair<Vector3D?, MySoundEmitter3D>(pos, new MySoundEmitter3D(sound, pos ?? this.WorldJumpNode, distance, volume)))) return sound_id;
			else return null;
		}

		/// <summary>
		/// Attemps to drain the specified power from all working drives attached to this gate
		/// </summary>
		/// <param name="power_mw">The amount of drain to syphon in MegaWatts</param>
		/// <returns>The remaining power after syphon</returns>
		public double SyphonGridDrivePower(double power_mw)
		{
			this.CheckClosed();
			if (power_mw <= 0) return 0;
			List<MyJumpGateDrive> drives = new List<MyJumpGateDrive>();
			this.GetWorkingJumpGateDrives(drives, (drive) => drive.StoredChargeMW > 0);
			double power_per = power_mw / drives.Count;
			power_mw = 0;
			foreach (MyJumpGateDrive drive in drives) power_mw += drive.DrainStoredCharge(power_per);
			return power_mw;
		}

		/// <summary>
		/// Calculates the distance ratio given a target endpoint
		/// </summary>
		/// <param name="endpoint">The target endpont</param>
		/// <returns>A ratio between this gate's maximum distance and minimum distance</returns>
		public double CalculateDistanceRatio(ref Vector3D endpoint)
		{
			this.CheckClosed();
			double distance = Vector3D.Distance(this.WorldJumpNode, endpoint);
			double max_distance = this.CalculateMaxGateDistance();
			double min_distance = this.JumpGateConfiguration.MinimumJumpDistance;
			return (distance - min_distance) / (max_distance - min_distance);
		}

		/// <summary>
		/// Calculates the power multiplier given a distance ratio
		/// </summary>
		/// <param name="ratio">The distance ratio</param>
		/// <returns>The power factor</returns>
		public double CalculatePowerFactorFromDistanceRatio(double ratio)
		{
			this.CheckClosed();
			double z = this.JumpGateConfiguration.GatePowerFalloffFactor;
			double s = this.JumpGateConfiguration.GatePowerFactorExponent;
			double x = Math.Max(ratio, 0);
			double power_factor = Math.Pow(z * x - z, s) + 1;
			return double.IsNaN(power_factor) ? double.PositiveInfinity : power_factor;
		}

		/// <summary>
		/// Calculates the power density (in Kw/Kg) required for a single kilogram to jump
		/// </summary>
		/// <param name="endpoint">The target endpoint</param>
		/// <returns>The required power density</returns>
		public double CalculateUnitPowerRequiredForJump(ref Vector3D endpoint)
		{
			this.CheckClosed();
			if (!this.IsValid()) return -1;
			double ratio = this.CalculateDistanceRatio(ref endpoint);
			double factor = this.CalculatePowerFactorFromDistanceRatio(ratio);
			return this.JumpGateConfiguration.GateKilowattPerKilogram * factor;
		}

		/// <summary>
		/// Calculates the maximum distance this gate can jump such that the power factor does not exceed 1
		/// </summary>
		/// <returns>The max reasonable distance in meters</returns>
		public double CalculateMaxGateDistance()
		{
			this.CheckClosed();
			int drive_count = this.JumpGateGrid.GetDriveCount((drive) => drive.JumpGateID == this.JumpGateID && drive.IsWorking());
			return Math.Pow(drive_count, this.JumpGateConfiguration.GateDistanceScaleExponent) * 1000d + this.JumpGateConfiguration.MinimumJumpDistance;
		}

		/// <summary>
		/// Calculates the total required power to jump some amount of mass a to the specified coordinate
		/// </summary>
		/// <param name="endpoint">The target world coordinate</param>
		/// <param name="has_target_gate">Whether to treat the calculation as if a target gate is present</param>
		/// <param name="mass">The mass in kilograms</param>
		/// <returns>The required power in megawatts</returns>
		public double CalculateTotalRequiredPower(Vector3D? endpoint = null, bool? has_target_gate = null, double? mass = null)
		{
			this.CheckClosed();
			MyJumpGateWaypoint waypoint = this.Controller?.BlockSettings.SelectedWaypoint();
			endpoint = endpoint ?? (waypoint?.GetEndpoint());
			if (endpoint == null) return double.NaN;
			bool _has_target_gate = has_target_gate ?? (waypoint?.WaypointType == MyWaypointType.JUMP_GATE);
			double total_mass_kg = mass ?? this.JumpSpaceEntities.Sum((pair) => (double) pair.Value);
			double power_scaler = (_has_target_gate) ? 1 : this.JumpGateConfiguration.UntetheredJumpEnergyCost;
			Vector3D end = endpoint.Value;
			return Math.Round(total_mass_kg * this.CalculateUnitPowerRequiredForJump(ref end), 8) * power_scaler / 1000;
		}

		/// <summary>
		/// Calculates the total available power stored within construct capacitors and drives for the specified jump gate
		/// </summary>
		/// <param name="jump_gate_id">The jump gate to calculate for</param>
		/// <returns>Total available instant power in Mega-Watts</returns>
		public double CalculateTotalAvailableInstantPower()
		{
			this.CheckClosed();
			return this.JumpGateGrid.CalculateTotalAvailableInstantPower(this.JumpGateID);
		}

		/// <summary>
		/// Calculates the total possible power stored within construct capacitors and drives for the specified jump gate
		/// </summary>
		/// <returns>Total possible instant power in Mega-Watts</returns>
		public double CalculateTotalPossibleInstantPower()
		{
			this.CheckClosed();
			return this.JumpGateGrid.CalculateTotalPossibleInstantPower(this.JumpGateID);
		}

		/// <summary>
		/// Calculates the total possible power stored within construct capacitors and drives for the specified jump gate<br />
		/// This method ignores block settings and whether block is working
		/// </summary>
		/// <returns>Total max possible instant power in Mega-Watts</returns>
		public double CalculateTotalMaxPossibleInstantPower()
		{
			this.CheckClosed();
			return this.JumpGateGrid.CalculateTotalMaxPossibleInstantPower(this.JumpGateID);
		}

		/// <summary>
		/// Calculates the minimum number of drives needed to achieve a jump of some distance such that the power factor does not exceed 1
		/// </summary>
		/// <param name="distance">The jump distance in meters</param>
		/// <returns>The number of drives required</returns>
		public int CalculateDrivesRequiredForDistance(double distance)
		{
			this.CheckClosed();
			double drive_count = Math.Pow((distance - this.JumpGateConfiguration.MinimumJumpDistance) / 1000d, 1d / this.JumpGateConfiguration.GateDistanceScaleExponent);
			return (int) Math.Ceiling(drive_count);
		}

		/// <summary>
		/// Gets the raduis of this gate's jump ellipse taking into account controller settings
		/// </summary>
		/// <returns>This gate's jump ellipsoid lateral radius</returns>
		public double JumpNodeRadius()
		{
			this.CheckClosed();
			double controller_limit = this.Controller?.BlockSettings?.JumpSpaceRadius() ?? double.MaxValue;
			return Math.Min(this.TrueLocalJumpEllipse.Radii.X, controller_limit);
		}

		/// <summary>
		/// </summary>
		/// <returns>The cube grid size of this gate</returns>
		public MyCubeSize CubeGridSize()
		{
			this.CheckClosed();
			return this.DriveGridSize ?? this.JumpGateGrid.GetFirstDrive((drive) => drive.JumpGateID == this.JumpGateID).CubeGridSize;
		}

		/// <summary>
		/// Gets the effective jump space ellipsoid for this gate<br />
		/// This differes from the standard ellipsoid only when the target is another jump gate<br />
		/// In this case, the effective ellipsoid is an ellipsoid composed of the smallest radii from the two ellipsoids
		/// </summary>
		/// <returns>The effective jump ellipsoid</returns>
		public BoundingEllipsoidD GetEffectiveJumpEllipse()
		{
			this.CheckClosed();
			MyJumpGateWaypoint waypoint = this.Controller?.BlockSettings?.SelectedWaypoint();
			MyJumpGate target_gate = (waypoint == null || waypoint.WaypointType != MyWaypointType.JUMP_GATE) ? null : MyJumpGateModSession.Instance.GetJumpGate(JumpGateUUID.FromGuid(waypoint.JumpGate));
			if (target_gate == null) return this.JumpEllipse;

			Vector3D radii = new Vector3D(
				Math.Min(this.JumpEllipse.Radii.X, target_gate.JumpEllipse.Radii.X),
				Math.Min(this.JumpEllipse.Radii.Y, target_gate.JumpEllipse.Radii.Y),
				Math.Min(this.JumpEllipse.Radii.Z, target_gate.JumpEllipse.Radii.Z)
			);

			return new BoundingEllipsoidD(ref radii, this.TrueWorldJumpEllipse.WorldMatrix);
		}

		/// <summary>
		/// Gets the world matrix for this gate<br />
		/// Resulting matrix may be affected by the normal override and endpoint
		/// </summary>
		/// <param name="use_endpoint">Whether to calculate the world matrix using this gate's endpoint</param>
		/// <param name="use_normal_override">Whether to calculate the world matrix using this gate's normal override</param>
		/// <returns>This gate's world matrix</returns>
		public MatrixD GetWorldMatrix(bool use_endpoint, bool use_normal_override)
		{
			this.CheckClosed();
			Vector3D? endpoint = this.Controller?.BlockSettings?.SelectedWaypoint()?.GetEndpoint();
			Vector3D? normal_override = this.Controller?.BlockSettings?.VectorNormalOverride();
			Vector3D world_node = this.WorldJumpNode;
			MatrixD jump_ellipse_matrix = this.TrueWorldJumpEllipse.WorldMatrix;

			if (use_normal_override && normal_override != null)
			{
				double distance = (endpoint == null) ? 10d : Vector3D.Distance(endpoint.Value, world_node);
				Vector3D vector = jump_ellipse_matrix.Up.Rotate(jump_ellipse_matrix.Forward, normal_override.Value.X).Rotate(jump_ellipse_matrix.Up, normal_override.Value.Y).Rotate(jump_ellipse_matrix.Left, normal_override.Value.Z);
				Vector3D second = ((Vector3D.Angle(vector, jump_ellipse_matrix.Forward) % Math.PI) == 0) ? jump_ellipse_matrix.Left : jump_ellipse_matrix.Forward;
				return MatrixD.CreateWorld(world_node, vector, Vector3D.Cross(vector, second));
			}
			else if (endpoint == null || !use_endpoint) return jump_ellipse_matrix;
			else
			{
				Vector3D forward = (endpoint.Value - world_node).Normalized();
				Vector3D second = ((Vector3D.Angle(forward, this.Controller.WorldMatrix.Up) % Math.PI) == 0) ? this.Controller.WorldMatrix.Left : this.Controller.WorldMatrix.Up;
				return MatrixD.CreateWorld(world_node, forward, Vector3D.Cross(forward, second).Normalized());
			}
		}

		/// <summary>
		/// Gets the world matrix for this gate<br />
		/// Resulting matrix may be affected by the normal override and endpoint
		/// </summary>
		/// <param name="use_endpoint">Whether to calculate the world matrix using this gate's endpoint</param>
		/// <param name="use_normal_override">Whether to calculate the world matrix using this gate's normal override</param>
		/// <returns>This gate's world matrix</returns>
		public void GetWorldMatrix(out MatrixD world_matrix, bool use_endpoint, bool use_normal_override)
		{
			this.CheckClosed();
			Vector3D? endpoint = this.Controller?.BlockSettings?.SelectedWaypoint()?.GetEndpoint();
			Vector3D? normal_override = this.Controller?.BlockSettings?.VectorNormalOverride();
			Vector3D world_node = this.WorldJumpNode;
			MatrixD jump_ellipse_matrix = this.TrueWorldJumpEllipse.WorldMatrix;

			if (use_normal_override && normal_override != null)
			{
				double distance = (endpoint == null) ? 10d : Vector3D.Distance(endpoint.Value, world_node);
				Vector3D vector = jump_ellipse_matrix.Up.Rotate(jump_ellipse_matrix.Forward, normal_override.Value.X).Rotate(jump_ellipse_matrix.Up, normal_override.Value.Y).Rotate(jump_ellipse_matrix.Left, normal_override.Value.Z);
				Vector3D second = ((Vector3D.Angle(vector, jump_ellipse_matrix.Forward) % Math.PI) == 0) ? jump_ellipse_matrix.Left : jump_ellipse_matrix.Forward;
				Vector3D.Cross(ref vector, ref second, out second);
				MatrixD.CreateWorld(ref world_node, ref vector, ref second, out world_matrix);
			}
			else if (endpoint == null || !use_endpoint) world_matrix = jump_ellipse_matrix;
			else
			{
				Vector3D forward = (endpoint.Value - world_node).Normalized();
				Vector3D second = ((Vector3D.Angle(forward, this.Controller.WorldMatrix.Up) % Math.PI) == 0) ? this.Controller.WorldMatrix.Left : this.Controller.WorldMatrix.Up;
				Vector3D.Cross(ref forward, ref second, out second);
				second.Normalize();
				MatrixD.CreateWorld(ref world_node, ref forward, ref second, out world_matrix);
			}
		}

		/// <summary>
		/// Gets this gate's name
		/// </summary>
		/// <returns>The name from the attached controller or the last name if no controller attached</returns>
		public string GetName()
		{
			this.CheckClosed();
			string name = this.Controller?.BlockSettings?.JumpGateName();
			this.LastStoredName = name ?? this.LastStoredName;
			return name;
		}

		/// <summary>
		/// </summary>
		/// <returns>The printable name for this gate</returns>
		public string GetPrintableName()
		{
			this.CheckClosed();
			return this.GetName() ?? this.LastStoredName ?? "�Unnamed�";
		}

		/// <summary>
		/// Gets the entity batch containing the specified entity
		/// </summary>
		/// <param name="entity">The entity who's batch to get</param>
		/// <returns>The entity's batch or null if not batched</returns>
		public EntityBatch GetEntityBatchFromEntity(MyEntity entity)
		{
			this.CheckClosed();
			if (entity == null) return null;
			return this.EntityBatches.GetValueOrDefault(entity, this.EntityBatches.FirstOrDefault((pair) => pair.Value.Batch.Contains(entity)).Value);
		}

		/// <summary>
		/// Serializes this gate's data
		/// </summary>
		/// <param name="as_client_request">If true, this will be an update request to server</param>
		/// <returns>The serialized jump gate data</returns>
		public MySerializedJumpGate ToSerialized(bool as_client_request)
		{
			if (as_client_request)
			{
				return new MySerializedJumpGate
				{
					UUID = JumpGateUUID.FromJumpGate(this).ToGuid(),
					IsClientRequest = true,
				};
			}
			else
			{
				return new MySerializedJumpGate
				{
					UUID = JumpGateUUID.FromJumpGate(this).ToGuid(),
					Closed = this.Closed,
					Status = this.Status,
					Phase = this.Phase,
					LocalJumpNode = this.LocalJumpNode,
					LocalJumpEllipse = (this.Closed) ? null : Convert.ToBase64String(this.TrueLocalJumpEllipse.ToSerialized()),
					Controller = (this.Controller == null) ? Guid.Empty : JumpGateUUID.FromBlock(this.Controller).ToGuid(),
					ServerAntenna = (this.ServerAntenna == null) ? Guid.Empty : JumpGateUUID.FromBlock(this.ServerAntenna).ToGuid(),
					JumpGateGrid = (this.Closed) ? Guid.Empty : JumpGateUUID.FromJumpGateGrid(this.JumpGateGrid).ToGuid(),
					IntersectNodes = (this.Closed) ? null : this.LocalDriveIntersectNodes,
					GridSize = (this.Closed) ? MyCubeSize.Large : this.CubeGridSize(),
					ConstructMatrix = (this.Closed) ? null : Convert.ToBase64String(new BoundingEllipsoidD(ref Vector3D.Zero, this.ConstructMatrix).ToSerialized()),
					JumpSpaceEntities = (MyNetworkInterface.IsMultiplayerServer && !this.Closed) ? this.JumpSpaceEntities.ToImmutableDictionary() : null,
					IsClientRequest = false,
				};
			}
		}
		#endregion
	}

	/// <summary>
	/// Class for holding serialized MyJumpGate data
	/// </summary>
	[ProtoContract]
	public class MySerializedJumpGate
	{
		/// <summary>
		/// The gate's JumpGateUUID as a Guid
		/// </summary>
		[ProtoMember(1)]
		public Guid UUID;

		/// <summary>
		/// Whether this gate is closed or marked for close
		/// </summary>
		[ProtoMember(2)]
		public bool Closed;

		/// <summary>
		/// This gate's status
		/// </summary>
		[ProtoMember(3)]
		public MyJumpGateStatus Status;
		
		/// <summary>
		/// This gate's phase
		/// </summary>
		[ProtoMember(4)]
		public MyJumpGatePhase Phase;

		/// <summary>
		/// This gate's local jump node
		/// </summary>
		[ProtoMember(5)]
		public Vector3D LocalJumpNode;

		/// <summary>
		/// This gate's local jump ellipse as a base64 string
		/// </summary>
		[ProtoMember(6)]
		public string LocalJumpEllipse;

		/// <summary>
		/// This gate's attached controller or an empty Guid
		/// </summary>
		[ProtoMember(7)]
		public Guid Controller;

		/// <summary>
		/// This gate's attached server antenna or an empty Guid
		/// </summary>
		[ProtoMember(8)]
		public Guid ServerAntenna;

		/// <summary>
		/// This gate's attached grid construct
		/// </summary>
		[ProtoMember(9)]
		public Guid JumpGateGrid;

		/// <summary>
		/// A list of this gate's drive intersect nodes
		/// </summary>
		[ProtoMember(10)]
		public ImmutableList<Vector3D> IntersectNodes;

		/// <summary>
		/// This gate's grid size
		/// </summary>
		[ProtoMember(11)]
		public MyCubeSize GridSize;

		/// <summary>
		/// This gate's construct matrix
		/// </summary>
		[ProtoMember(12)]
		public string ConstructMatrix;

		/// <summary>
		/// A map of this gate's jump space entities with their mass
		/// </summary>
		[ProtoMember(13)]
		public ImmutableDictionary<long, float> JumpSpaceEntities;

		/// <summary>
		/// If true, this data should be used by server to identify gate and send updated data
		/// </summary>
		[ProtoMember(14)]
		public bool IsClientRequest;
	}
}
