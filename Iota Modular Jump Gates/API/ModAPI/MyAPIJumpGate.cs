using Sandbox.ModAPI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;
using static IOTA.ModularJumpGates.API.ModAPI.Util;

namespace IOTA.ModularJumpGates.API.ModAPI
{
	public class MyAPIJumpGate : MyAPIObjectBase
	{
		private readonly ConcurrentDictionary<Action<MyAPIJumpGate, MyEntity, bool>, Action<Dictionary<string, object>, MyEntity, bool>> EntityEnteredCallbacks = new ConcurrentDictionary<Action<MyAPIJumpGate, MyEntity, bool>, Action<Dictionary<string, object>, MyEntity, bool>>();

		internal static MyAPIJumpGate New(Dictionary<string, object> attributes)
		{
			return (attributes == null) ? null : new MyAPIJumpGate(attributes);
		}

		private MyAPIJumpGate(Dictionary<string, object> attributes) : base(attributes) { }

		public Guid Guid => this.GetAttribute<Guid>("Guid");

		/// <summary>
		/// Whether this gate is marked for close
		/// </summary>
		public bool MarkClosed => this.GetAttribute<bool>("MarkClosed");

		/// <summary>
		/// Whether this gate is closed
		/// </summary>
		public bool Closed => this.GetAttribute<bool>("Closed");

		/// <summary>
		/// Whether this jump gate should be synced<br/>
		/// If true, will be synced on next component tick
		/// </summary>
		public bool IsDirty => this.GetAttribute<bool>("IsDirty");

		/// <summary>
		/// The status of this gate
		/// </summary>
		public MyAPIJumpGateStatus Status => (MyAPIJumpGateStatus) this.GetAttribute<byte>("Status");

		/// <summary>
		/// The jump phase of this gate
		/// </summary>
		public MyAPIJumpGatePhase Phase => (MyAPIJumpGatePhase) this.GetAttribute<byte>("Phase");

		/// <summary>
		/// The reason this gate's last jump failed or SUCCESS if successfull or NONE if unset and whether failure was during INIT phase
		/// </summary>
		public KeyValuePair<MyAPIJumpFailReason, bool> JumpFailureReason
		{
			get
			{
				KeyValuePair<byte, bool> reason = this.GetAttribute<KeyValuePair<byte, bool>>("JumpFailureReason");
				return new KeyValuePair<MyAPIJumpFailReason, bool>((MyAPIJumpFailReason) reason.Key, reason.Value);
			}
		}

		/// <summary>
		/// This gate's ID
		/// </summary>
		public long JumpGateID => this.GetAttribute<long>("JumpGateID");

		/// <summary>
		/// The construct-local coordinate of this gate's jump node (the center of the jump space)
		/// </summary>
		public Vector3D LocalJumpNode => this.GetAttribute<Vector3D>("LocalJumpNode");

		/// <summary>
		/// The world coordinate of this gate's jump node (the center of the jump space)
		/// </summary>
		public Vector3D WorldJumpNode => this.GetAttribute<Vector3D>("WorldJumpNode");

		/// <summary>
		/// The true endpoint of this jump gate<br />
		/// Only non-null when this gate is outbound
		/// </summary>
		public Vector3D? TrueEndpoint => this.GetAttribute<Vector3D?>("TrueEndpoint");

		/// <summary>
		/// A direction vector indicating the velocity of this gate's jump node in m/s
		/// </summary>
		public Vector3D JumpNodeVelocity => this.GetAttribute<Vector3D>("JumpNodeVelocity");

		/// <summary>
		/// The PlaneD representing this gate's primary drive plane
		/// </summary>
		public PlaneD? PrimaryDrivePlane => this.GetAttribute<PlaneD?>("PrimaryDrivePlane");

		/// <summary>
		/// The world matrix of this gate's jump gate grid's main cube grid
		/// </summary>
		public MatrixD ConstructMatrix => this.GetAttribute<MatrixD>("ConstructMatrix");

		/// <summary>
		/// The UTC date time representation of MyJumpGate.LastUpdateTime
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
		/// The gate configuration variables for this gate
		/// </summary>
		public Dictionary<string, object> JumpGateConfiguration => this.GetAttribute<Dictionary<string, object>>("JumpGateConfiguration");

		/// <summary>
		/// This gate's attached controller or null
		/// </summary>
		public MyAPIJumpGateController Controller => MyAPIJumpGateController.New(this.GetAttribute<Dictionary<string, object>>("Controller"));

		/// <summary>
		/// This gate's attached server antenna or null
		/// </summary>
		public MyAPIJumpGateServerAntenna ServerAntenna => MyAPIJumpGateServerAntenna.New(this.GetAttribute<Dictionary<string, object>>("ServerAntenna"));

		/// <summary>
		/// The gate jumping to this one or null
		/// </summary>
		public MyAPIJumpGate SenderGate => MyAPIJumpGate.New(this.GetAttribute<Dictionary<string, object>>("SenderGate"));

		/// <summary>
		/// This gate's MyJumpGateConstruct construct
		/// </summary>
		public MyAPIJumpGateConstruct JumpGateGrid => MyAPIJumpGateConstruct.New(this.GetAttribute<Dictionary<string, object>>("JumpGateGrid"));

		/// <summary>
		/// Gets this gate's world-aligned artificial JumpEllipse using the associated controller
		/// </summary>
		public BoundingEllipsoidD JumpEllipse => BoundingEllipsoidD.FromSerialized(this.GetAttribute<byte[]>("JumpEllipse"), 0);

		/// <summary>
		/// Gets this gate's world-aligned shearing ellipse<br />
		/// This ellipse is used to calculate grid shearing<br />
		/// This ellipse is the jump ellipse padded by 5 meters
		/// </summary>
		public BoundingEllipsoidD ShearEllipse => BoundingEllipsoidD.FromSerialized(this.GetAttribute<byte[]>("ShearEllipse"), 0);

		/// <summary>
		/// Gets the list of construct-local space drive ray-cast intersections for this jump gate
		/// </summary>
		public ImmutableList<Vector3D> LocalDriveIntersectNodes => this.GetAttribute<ImmutableList<Vector3D>>("LocalDriveIntersectNodes");

		/// <summary>
		/// Gets the list of world space drive ray-cast intersections for this jump gate
		/// </summary>
		public ImmutableList<Vector3D> WorldDriveIntersectNodes => this.GetAttribute<ImmutableList<Vector3D>>("WorldDriveIntersectNodes");

		/// <summary>
		/// Gets the associated desription for the specified failure reason
		/// </summary>
		/// <param name="reason">The failure reason</param>
		/// <returns>The associated description or null if no description available</returns>
		public string GetFailureDescription(MyAPIJumpFailReason reason)
		{
			return this.GetMethod<Func<byte, string>>("GetFailureDescription")((byte) reason);
		}

		/// <summary>
		/// Gets the associated sound name for the specified failure reason
		/// </summary>
		/// <param name="reason">The failure reason</param>
		/// <param name="is_init_phase">Whether the failure occured during jump initialization</param>
		/// <returns>The associated sound name or null if no sound name available</returns>
		public string GetFailureSound(MyAPIJumpFailReason reason, bool is_init_phase)
		{
			return this.GetMethod<Func<byte, bool, string>>("GetFailureSound")((byte) reason, is_init_phase);
		}

		/// <summary>
		/// Requests a jump gate update from the server<br />
		/// Does nothing if singleplayer or server<br />
		/// Does nothing if parent construct is suspended or closed
		/// </summary>
		public void RequestGateUpdate()
		{
			this.GetMethod<Action>("RequestGateUpdate")();
		}

		/// <summary>
		/// Begins the activation sequence for this gate<br />
		/// For singleplayer: Begins the jump<br />
		/// For servers: Begins the jump, broadcasts jump request to all clients<br />
		/// For clients: Sends a jump request to server
		/// </summary>
		/// <param name="controller">The controller to use for the jump</param>
		public void Jump(MyAPIJumpGateController controller)
		{
			this.GetMethod<Action<IMyTerminalBlock, Dictionary<string, object>>>("Jump")(controller.TerminalBlock, controller.BlockSettings.ToDictionary());
		}

		/// <summary>
		/// Begins the activation sequence for this gate<br />
		/// Destination is the void; <b>All jump entities will be destroyed</b><br />
		/// This method must be called on singleplayer or multiplayer server
		/// </summary>
		/// <param name="controller">The controller to use for the jump</param>
		/// <param name="distance">The distance to use for power calculations</param>
		public void JumpToVoid(MyAPIJumpGateController controller, double distance)
		{
			this.GetMethod<Action<double, IMyTerminalBlock, Dictionary<string, object>>>("JumpToVoid")(distance, controller.TerminalBlock, controller.BlockSettings.ToDictionary());
		}

		/// <summary>
		/// Begins the activation sequence for this gate<br />
		/// Source is the void; Allows spawning of prefab entities<br />
		/// This method must be called on singleplayer or multiplayer server
		/// </summary>
		/// <param name="controller">The controller to use for the jump</param>
		/// <param name="spawn_prefabs">A list of prefabs to spawn<br />Jump will fail if empty</param>
		/// <param name="spawned_grids">A list containing the list of spawned grids per prefab</param>
		public void JumpFromVoid(MyAPIJumpGateController controller, List<MyAPIPrefabInfo> spawn_prefabs, List<List<IMyCubeGrid>> spawned_grids)
		{
			this.GetMethod<Action<List<Dictionary<string, object>>, List<List<IMyCubeGrid>>, IMyTerminalBlock, Dictionary<string, object>>>("JumpFromVoid")(spawn_prefabs.Select((prefab) => prefab.ToDictionary()).ToList(), spawned_grids, controller.TerminalBlock, controller.BlockSettings.ToDictionary());
		}

		/// <summary>
		/// Marks this jump gate's physics collider as dirty<br />
		/// It will be updated on next physics tick
		/// </summary>
		public void SetColliderDirty()
		{
			this.GetMethod<Action>("SetColliderDirty")();
		}

		/// <summary>
		/// Marks this jump gate's jump ellipsoid as dirty<br />
		/// It will be updated on next tick
		/// </summary>
		public void SetJumpSpaceEllipsoidDirty()
		{
			this.GetMethod<Action>("SetJumpSpaceEllipsoidDirty")();
		}

		/// <summary>
		/// Stops all sound emitters with the specified sound
		/// </summary>
		/// <param name="sound">The sound to stop</param>
		public void StopSound(string sound)
		{
			this.GetMethod<Action<string>>("StopSoundByName")(sound);
		}

		/// <summary>
		/// Stops the sound emitter with the specified ID
		/// </summary>
		/// <param name="sound_id">The sound emitter ID to stop</param>
		public void StopSound(ulong? sound_id)
		{
			this.GetMethod<Action<ulong?>>("StopSoundByID")(sound_id);
		}

		/// <summary>
		/// Sets the volume for the specified sound emitter
		/// </summary>
		/// <param name="sound_id">The ID of the sound emitter</param>
		/// <param name="volume">The new volume</param>
		public void SetSoundVolume(ulong? sound_id, float volume)
		{
			this.GetMethod<Action<ulong?, float>>("SetSoundVolume")(sound_id, volume);
		}

		/// <summary>
		/// Sets the range for the specified sound emitter
		/// </summary>
		/// <param name="sound_id">The ID of the sound emitter</param>
		/// <param name="volume">The new range or null</param>
		public void SetSoundDistance(ulong? sound_id, float? distance)
		{
			this.GetMethod<Action<ulong?, float?>>("SetSoundDistance")(sound_id, distance);
		}

		/// <summary>
		/// Stops all sounds from this gate
		/// </summary>
		public void StopSounds()
		{
			this.GetMethod<Action>("StopSounds")();
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
			this.GetMethod<Action<double, ulong, Action<bool>, bool>>("CanDoSyphonGridPower")(power_mw, ticks, callback, syphon_grid_only);
		}

		/// <summary>
		/// Cancels the gate's jump
		/// For servers and singleplayer: Cancels the jump<br />
		/// For clients: Sends a cancel jump request to server
		/// </summary>
		public void CancelJump()
		{
			this.GetMethod<Action>("CancelJump")();
		}

		/// <summary>
		/// Gets all entities within this gate's jump space ellipsoid not yet initialized on client
		/// </summary>
		/// <param name="entities">All unconfirmed entities within this jump space<br />Dictionary will not be cleared</param>
		/// <param name="filtered">Whether to remove blacklisted entities per this gate's controller</param>
		public void GetUninitializedEntititesInJumpSpace(IDictionary<long, float> entities, bool filtered = false)
		{
			this.GetMethod<Action<IDictionary<long, float>, bool>>("GetUninitializedEntititesInJumpSpace")(entities, filtered);
		}

		/// <summary>
		/// Gets all entities within this gate's jump space ellipsoid
		/// </summary>
		/// <param name="entities">All entities within this jump space<br />Dictionary will not be cleared</param>
		/// <param name="filtered">Whether to remove blacklisted entities per this gate's controller</param>
		public void GetEntitiesInJumpSpace(IDictionary<MyEntity, float> entities, bool filtered = false)
		{
			this.GetMethod<Action<IDictionary<MyEntity, float>, bool>>("GetEntitiesInJumpSpace")(entities, filtered);
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
			Action<Dictionary<string, object>, MyEntity, bool> _callback = (gate, entity, is_entering) => callback(MyAPIJumpGate.New(gate), entity, is_entering);
			this.EntityEnteredCallbacks[callback] = _callback;
			this.GetMethod<Action<Action<Dictionary<string, object>, MyEntity, bool>>>("OnEntityCollision")(_callback);
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
			Action<Dictionary<string, object>, MyEntity, bool> _callback = this.EntityEnteredCallbacks.GetValueOrDefault(callback, null);
			if (_callback != null) this.GetMethod<Action<Action<Dictionary<string, object>, MyEntity, bool>>>("OffEntityCollision")(_callback);
		}

		/// <summary>
		/// Checks if the specified callback is registered with this gate
		/// </summary>
		/// <param name="callback">The callback to check</param>
		/// <returns>True if already registered</returns>
		public bool IsEntityCollisionCallbackRegistered(Action<MyAPIJumpGate, MyEntity, bool> callback)
		{
			Action<Dictionary<string, object>, MyEntity, bool> _callback;
			if (!this.EntityEnteredCallbacks.TryGetValue(callback, out _callback)) return false;
			return this.GetMethod<Func<Action<Dictionary<string, object>, MyEntity, bool>, bool>>("IsEntityCollisionCallbackRegistered")(_callback);
		}

		/// <summary>
		/// Checks if a point is within twice the distance of the largest jump ellipse radius or within 500 meters of the attached controller
		/// </summary>
		/// <param name="pos">The world position to check</param>
		/// <returns>True if in range</returns>
		public bool IsPointInHudBroadcastRange(ref Vector3D pos)
		{
			return this.GetMethod<Func<Vector3D, bool>>("IsPointInHudBroadcastRange")(pos);
		}

		/// <summary>
		/// Checks if entity passes all controller filters and is allowed by gate
		/// </summary>
		/// <param name="entity">The entity to check</param>
		/// <returns>True if entity can be jumped by this gate</returns>
		public bool IsEntityValidForJumpSpace(MyEntity entity)
		{
			return this.GetMethod<Func<MyEntity, bool>>("IsEntityValidForJumpSpace")(entity);
		}

		/// <summary>
		/// Whether this grid is valid
		/// </summary>
		/// <returns>True if valid</returns>
		public bool IsValid()
		{
			return this.GetMethod<Func<bool>>("IsValid")();
		}

		/// <summary>
		/// Whether this grid is valid and a controller attached
		/// </summary>
		/// <returns>True if complete</returns>
		public bool IsComplete()
		{
			return this.GetMethod<Func<bool>>("IsComplete")();
		}

		/// <summary>
		/// </summary>
		/// <returns>True if this gate is idle</returns>
		public bool IsIdle()
		{
			return this.GetMethod<Func<bool>>("IsIdle")();
		}

		/// <summary>
		/// </summary>
		/// <returns>True if this gate is inbound or outbound</returns>
		public bool IsJumping()
		{
			return this.GetMethod<Func<bool>>("IsJumping")();
		}

		/// <summary>
		/// </summary>
		/// <returns>True if this gate is large grid</returns>
		public bool IsLargeGrid()
		{
			return this.GetMethod<Func<bool>>("IsLargeGrid")();
		}

		/// <summary>
		/// </summary>
		/// <returns>True if this gate is small grid</returns>
		public bool IsSmallGrid()
		{
			return this.GetMethod<Func<bool>>("IsSmallGrid")();
		}

		/// <summary>
		/// Checks whether this jump gate is suspended<br />
		/// Gate is suspended if parent construct is suspended or at least one drive is a null wrapper<br />
		/// Always false on server or singleplayer
		/// </summary>
		/// <returns>Suspendedness</returns>
		public bool IsSuspended()
		{
			return this.GetMethod<Func<bool>>("IsSuspended")();
		}

		public override bool Equals(object other)
		{
			return other != null && other is MyAPIJumpGate && base.Equals(((MyAPIJumpGate) other).Guid);
		}

		/// <summary>
		/// </summary>
		/// <returns>The status of this gate's jump space collision detector</returns>
		public MyAPIGateColliderStatus JumpSpaceColliderStatus()
		{
			return (MyAPIGateColliderStatus) this.GetMethod<Func<byte>>("JumpSpaceColliderStatus")();
		}

		/// <summary>
		/// Gets the reason this gate is invalid
		/// </summary>
		/// <returns>The invalidation reason or InvalidationReason.NONE</returns>
		public MyAPIGateInvalidationReason GetInvalidationReason()
		{
			return (MyAPIGateInvalidationReason) this.GetMethod<Func<byte>>("GetInvalidationReason")();
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		/// <summary>
		/// Get the number of jump space entities matching the specified predicate or all entities if predicate is null
		/// </summary>
		/// <param name="filtered">Whether to filter entities based on controller settings</param>
		/// <param name="filter">A predicate to filter entities by</param>
		/// <returns>The number of matching entities</returns>
		public int GetJumpSpaceEntityCount(bool filtered = false, Func<MyEntity, bool> filter = null)
		{
			return this.GetMethod<Func<bool, Func<MyEntity, bool>, int>>("GetJumpSpaceEntityCount")(filtered, filter);
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
			return this.GetMethod<Func<string, float?, float, Vector3D?, ulong?>>("PlaySound")(sound, distance, volume, pos);
		}

		/// <summary>
		/// Attemps to drain the specified power from all working drives attached to this gate
		/// </summary>
		/// <param name="power_mw">The amount of drain to syphon in MegaWatts</param>
		/// <returns>The remaining power after syphon</returns>
		public double SyphonGridDrivePower(double power_mw)
		{
			return this.GetMethod<Func<double, double>>("SyphonGridDrivePower")(power_mw);
		}

		/// <summary>
		/// Calculates the distance ratio given a target endpoint
		/// </summary>
		/// <param name="endpoint">The target endpont</param>
		/// <returns>A ratio between this gate's maximum distance and minimum distance</returns>
		public double CalculateDistanceRatio(ref Vector3D endpoint)
		{
			return this.GetMethod<Func<Vector3D, double>>("CalculateDistanceRatio")(endpoint);
		}

		/// <summary>
		/// Calculates the power multiplier given a distance ratio
		/// </summary>
		/// <param name="ratio">The distance ratio</param>
		/// <returns>The power factor</returns>
		public double CalculatePowerFactorFromDistanceRatio(double ratio)
		{
			return this.GetMethod<Func<double, double>>("CalculatePowerFactorFromDistanceRatio")(ratio);
		}

		/// <summary>
		/// Calculates the power density (in Kw/Kg) required for a single kilogram to jump
		/// </summary>
		/// <param name="endpoint">The target endpoint</param>
		/// <returns>The required power density</returns>
		public double CalculateUnitPowerRequiredForJump(ref Vector3D endpoint)
		{
			return this.GetMethod<Func<Vector3D, double>>("CalculateUnitPowerRequiredForJump")(endpoint);
		}

		/// <summary>
		/// Calculates the maximum distance this gate can jump such that the power factor does not exceed 1
		/// </summary>
		/// <returns>The max reasonable distance in meters</returns>
		public double CalculateMaxGateDistance()
		{
			return this.GetMethod<Func<double>>("CalculateMaxGateDistance")();
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
			return this.GetMethod<Func<Vector3D?, bool?, double?, double>>("CalculateMaxGateDistance")(endpoint, has_target_gate, mass);
		}

		/// <summary>
		/// Calculates the total available power stored within construct capacitors and drives for the specified jump gate
		/// </summary>
		/// <returns>Total available instant power in Mega-Watts</returns>
		public double CalculateTotalAvailableInstantPower()
		{
			return this.GetMethod<Func<double>>("CalculateTotalAvailableInstantPower")();
		}

		/// <summary>
		/// Calculates the total possible power stored within construct capacitors and drives for the specified jump gate
		/// </summary>
		/// <returns>Total possible instant power in Mega-Watts</returns>
		public double CalculateTotalPossibleInstantPower()
		{
			return this.GetMethod<Func<double>>("CalculateTotalPossibleInstantPower")();
		}

		/// <summary>
		/// Calculates the total possible power stored within construct capacitors and drives for the specified jump gate<br />
		/// This method ignores block settings and whether block is working
		/// </summary>
		/// <returns>Total max possible instant power in Mega-Watts</returns>
		public double CalculateTotalMaxPossibleInstantPower()
		{
			return this.GetMethod<Func<double>>("CalculateTotalMaxPossibleInstantPower")();
		}

		/// <summary>
		/// Calculates the minimum number of drives needed to achieve a jump of some distance such that the power factor does not exceed 1
		/// </summary>
		/// <param name="distance">The jump distance in meters</param>
		/// <returns>The number of drives required</returns>
		public int CalculateDrivesRequiredForDistance(double distance)
		{
			return this.GetMethod<Func<double, int>>("CalculateDrivesRequiredForDistance")(distance);
		}

		/// <summary>
		/// Gets the raduis of this gate's jump ellipse taking into account controller settings
		/// </summary>
		/// <returns>This gate's jump ellipsoid lateral radius</returns>
		public double JumpNodeRadius()
		{
			return this.GetMethod<Func<double>>("JumpNodeRadius")();
		}

		/// <summary>
		/// </summary>
		/// <returns>The cube grid size of this gate</returns>
		public MyCubeSize CubeGridSize()
		{
			return this.GetMethod<Func<MyCubeSize>>("CubeGridSize")();
		}

		/// <summary>
		/// Gets the effective jump space ellipsoid for this gate<br />
		/// This differes from the standard ellipsoid only when the target is another jump gate<br />
		/// In this case, the effective ellipsoid is an ellipsoid composed of the smallest radii from the two ellipsoids
		/// </summary>
		/// <returns>The effective jump ellipsoid</returns>
		public BoundingEllipsoidD GetEffectiveJumpEllipse()
		{
			return BoundingEllipsoidD.FromSerialized(this.GetMethod<Func<byte[]>>("GetEffectiveJumpEllipse")(), 0);
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
			return this.GetMethod<Func<bool, bool, MatrixD>>("GetWorldMatrix")(use_endpoint, use_normal_override);
		}

		/// <summary>
		/// Gets this gate's name
		/// </summary>
		/// <returns>The name from the attached controller or the last name if no controller attached</returns>
		public string GetName()
		{
			return this.GetMethod<Func<string>>("GetName")();
		}

		/// <summary>
		/// </summary>
		/// <returns>The printable name for this gate</returns>
		public string GetPrintableName()
		{
			return this.GetMethod<Func<string>>("GetPrintableName")();
		}

		/// <summary>
		/// Gets a list of all drives attached to this gate
		/// </summary>
		/// <returns>An IEnumerable referencing all attached drives</returns>
		public IEnumerable<MyAPIJumpGateDrive> GetJumpGateDrives()
		{
			return this.GetMethod<Func<IEnumerable<Dictionary<string, object>>>>("GetJumpGateDrives")().Select(MyAPIJumpGateDrive.New);
		}

		/// <summary>
		/// Gets a list of all working drives attached to this gate
		/// </summary>
		/// <returns>An IEnumerable referencing all attached, working drives</returns>
		public IEnumerable<MyAPIJumpGateDrive> GetWorkingJumpGateDrives()
		{
			return this.GetMethod<Func<IEnumerable<Dictionary<string, object>>>>("GetWorkingJumpGateDrives")().Select(MyAPIJumpGateDrive.New);
		}
	}
}
