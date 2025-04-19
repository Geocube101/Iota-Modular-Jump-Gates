using IOTA.ModularJumpGates.API.CubeBlock;
using IOTA.ModularJumpGates.CubeBlock;
using IOTA.ModularJumpGates.Util;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;
using static IOTA.ModularJumpGates.MyJumpGate;

namespace IOTA.ModularJumpGates.API
{
	public class MyAPIJumpGate : MyAPIInterface
	{
		internal MyJumpGate JumpGate;

		internal MyAPIJumpGate(MyJumpGate jump_gate)
		{
			this.JumpGate = jump_gate;
		}

		#region Public Static Operators
		/// <summary>
		/// Overloads equality operator "==" to check equality
		/// </summary>
		/// <param name="a">The first MyJumpGate operand</param>
		/// <param name="b">The second MyJumpGate operand</param>
		/// <returns>Equality</returns>
		public static bool operator ==(MyAPIJumpGate a, MyAPIJumpGate b)
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
		public static bool operator !=(MyAPIJumpGate a, MyAPIJumpGate b)
		{
			return !(a == b);
		}
		#endregion

		#region Public Static API Methods
		/// <summary>
		/// Gets the associated desription for the specified failure reason
		/// </summary>
		/// <param name="reason">The failure reason</param>
		/// <returns>The associated description or null if no description available</returns>
		public static string GetFailureDescription(MyJumpFailReason reason)
		{
			return MyJumpGate.GetFailureDescription(reason);
		}

		/// <summary>
		/// Gets the associated sound name for the specified failure reason
		/// </summary>
		/// <param name="reason">The failure reason</param>
		/// <param name="is_init_phase">Whether the failure occured during jump initialization</param>
		/// <returns>The associated sound name or null if no sound name available</returns>
		public static string GetFailureSound(MyJumpFailReason reason, bool is_init_phase)
		{
			return MyJumpGate.GetFailureSound(reason, is_init_phase);
		}
		#endregion

		#region Public API Variables
		/// <summary>
		/// Whether this gate is marked for close
		/// </summary>
		public bool MarkClosed { get { return this.JumpGate.MarkClosed; } }

		/// <summary>
		/// Whether this gate is closed
		/// </summary>
		public bool Closed { get { return this.JumpGate.Closed; } }

		/// <summary>
		/// Whether this jump gate should be synced<br/>
		/// If true, will be synced on next component tick
		/// </summary>
		public bool IsDirty { get { return this.JumpGate.IsDirty; } }

		/// <summary>
		/// The status of this gate
		/// </summary>
		public MyJumpGateStatus Status { get { return this.JumpGate.Status; } }

		/// <summary>
		/// The jump phase of this gate
		/// </summary>
		public MyJumpGatePhase Phase { get { return this.JumpGate.Phase; } }

		/// <summary>
		/// The reason this gate's last jump failed or SUCCESS if successfull or NONE if unset and whether failure was during INIT phase
		/// </summary>
		public KeyValuePair<MyJumpFailReason, bool> LastFailureReason { get { return this.JumpGate.JumpFailureReason; } }

		/// <summary>
		/// This gate's ID
		/// </summary>
		public long JumpGateID { get { return this.JumpGate.JumpGateID; } }

		/// <summary>
		/// The construct-local coordinate of this gate's jump node (the center of the jump space)
		/// </summary>
		public Vector3D LocalJumpNode { get { return this.JumpGate.LocalJumpNode; } }

		/// <summary>
		/// The world coordinate of this gate's jump node (the center of the jump space)
		/// </summary>
		public Vector3D WorldJumpNode { get { return this.JumpGate.WorldJumpNode; } }

		/// <summary>
		/// The true endpoint of this jump gate<br />
		/// Only non-null when this gate is outbound
		/// </summary>
		public Vector3D? TrueEndpoint { get { return this.JumpGate.TrueEndpoint; } }

		/// <summary>
		/// A direction vector indicating the velocity of this gate's jump node
		/// </summary>
		public Vector3D JumpNodeVelocity { get { return this.JumpGate.JumpNodeVelocity; } }

		/// <summary>
		/// The world matrix of this gate's jump gate grid's main cube grid
		/// </summary>
		public MatrixD ConstructMatrix { get { return this.JumpGate.ConstructMatrix; } }

		/// <summary>
		/// This gate's attached controller or null
		/// </summary>
		public MyAPIJumpGateController Controller { get { return MyAPISession.GetNewBlock(this.JumpGate.Controller); } }

		/// <summary>
		/// This gate's attached server antenna or null
		/// </summary>
		public MyAPIJumpGateServerAntenna ServerAntenna { get { return MyAPISession.GetNewBlock(this.JumpGate.ServerAntenna); } }

		/// <summary>
		/// The gate jumping to this one or null
		/// </summary>
		public MyAPIJumpGate SenderGate { get { return MyAPISession.GetNewJumpGate(this.JumpGate.SenderGate); } }

		/// <summary>
		/// This gate's MyJumpGateConstruct construct
		/// </summary>
		public MyAPIJumpGateConstruct JumpGateGrid { get { return MyAPISession.GetNewConstruct(this.JumpGate.JumpGateGrid); } }

		/// <summary>
		/// This gate's configuration variables
		/// </summary>
		public Configuration.LocalJumpGateConfiguration Configuration { get { return this.JumpGate.JumpGateConfiguration; } }

		/// <summary>
		/// Gets this gate's world-aligned artifical JumpEllipse using the associated controller
		/// </summary>
		public BoundingEllipsoidD JumpEllipse { get { return this.JumpGate.JumpEllipse; } }

		/// <summary>
		/// Gets this gate's world-aligned shearing ellipse<br />
		/// This ellipse is used to calculate grid shearing<br />
		/// This ellipse is the jump ellipse padded by 2.5 meters
		/// </summary>
		public BoundingEllipsoidD ShearEllipse { get { return this.JumpGate.ShearEllipse; } }
		#endregion

		#region "object" Methods
		/// <summary>
		/// Checks if this MyJumpGate equals another
		/// </summary>
		/// <param name="other">The object to check</param>
		/// <returns>Equality</returns>
		public override bool Equals(object other)
		{
			if (other == null || !(other is MyAPIJumpGate)) return false;
			else return this.Equals((MyAPIJumpGate) other);
		}

		/// <summary>
		/// The hashcode for this object
		/// </summary>
		/// <returns>The hashcode of this object</returns>
		public override int GetHashCode()
		{
			return this.JumpGate.GetHashCode();
		}
		#endregion

		#region Public API Methods
		/// <summary>
		/// Requests a jump gate update from the server<br />
		/// Does nothing if singleplayer or server<br />
		/// Does nothing if parent construct is suspended
		/// </summary>
		public void RequestGateUpdate()
		{
			this.JumpGate.RequestGateUpdate();
		}

		/// <summary>
		/// Begins the activation sequence for this gate<br />
		/// For singleplayer: Begins the jump<br />
		/// For servers: Begins the jump, broadcasts jump request to all clients<br />
		/// For clients: Sends a jump request to server
		/// </summary>
		/// <param name="controller_settings">The controller settings to use for the jump</param>
		/// <param name="caller">The steam ID of the caller</param>
		public void Jump(MyAPIJumpGateController.ControllerBlockSettingsWrapper settings)
		{
			if (settings == null || (!MyNetworkInterface.IsSingleplayer && !MyNetworkInterface.IsMultiplayerServer)) return;
			this.JumpGate.Jump(settings.BlockSettings);
		}

		/// <summary>
		/// Begins the activation sequence for this gate<br />
		/// Destination is the void; <b>All jump entities will be destroyed</b><br />
		/// This method must be called on singleplayer or multiplayer server
		/// </summary>
		/// <param name="controller_settings">The controller settings to use for the jump</param>
		/// <param name="distance">The distance to use for power calculations</param>
		public void JumpToVoid(MyAPIJumpGateController.ControllerBlockSettingsWrapper controller_settings, double distance)
		{
			this.JumpGate.JumpToVoid(controller_settings.BlockSettings, distance);
		}

		/// <summary>
		/// Begins the activation sequence for this gate<br />
		/// Source is the void; Allows spawning of prefab entities<br />
		/// This method must be called on singleplayer or multiplayer server
		/// </summary>
		/// <param name="controller_settings">The controller settings to use for the jump</param>
		/// <param name="spawn_prefabs">A list of prefabs to spawn<br />Jump will fail if empty</param>
		/// <param name="spawned_grids">A list containing the list of spawned grids per prefab</param>
		public void JumpFromVoid(MyAPIJumpGateController.ControllerBlockSettingsWrapper controller_settings, List<MyPrefabInfo> spawn_prefabs, List<List<IMyCubeGrid>> spawned_grids)
		{
			this.JumpGate.JumpFromVoid(controller_settings.BlockSettings, spawn_prefabs, spawned_grids);
		}

		/// <summary>
		/// Marks this gate as dirty<br />
		/// It will be synced over network by next tick
		/// </summary>
		public void SetDirty()
		{
			this.JumpGate.IsDirty = true;
		}

		/// <summary>
		/// Marks this jump gate's physics collider as dirty<br />
		/// It will be updated on next physics tick
		/// </summary>
		public void SetColliderDirty()
		{
			this.JumpGate.SetColliderDirty();
		}

		/// <summary>
		/// Marks this jump gate's jump ellipsoid as dirty<br />
		/// It will be updated on next tick
		/// </summary>
		public void SetJumpSpaceEllipsoidDirty()
		{
			this.JumpGate.SetJumpSpaceEllipsoidDirty();
		}

		/// <summary>
		/// Stops all sound emitters with the specified sound
		/// </summary>
		/// <param name="sound">The sound to stop</param>
		public void StopSound(string sound)
		{
			this.JumpGate.StopSound(sound);
		}

		/// <summary>
		/// Stops the sound emitter with the specified ID
		/// </summary>
		/// <param name="sound_id">The sound emitter ID to stop</param>
		public void StopSound(ulong? sound_id)
		{
			this.JumpGate.StopSound(sound_id);
		}

		/// <summary>
		/// Sets the volume for the specified sound emitter
		/// </summary>
		/// <param name="sound_id">The ID of the sound emitter</param>
		/// <param name="volume">The new volume</param>
		public void SetSoundVolume(ulong? sound_id, float volume)
		{
			this.JumpGate.SetSoundVolume(sound_id, volume);
		}

		/// <summary>
		/// Sets the range for the specified sound emitter
		/// </summary>
		/// <param name="sound_id">The ID of the sound emitter</param>
		/// <param name="volume">The new range or null</param>
		public void SetSoundDistance(ulong? sound_id, float? distance)
		{
			this.JumpGate.SetSoundDistance(sound_id, distance);
		}

		/// <summary>
		/// Stops all sounds from this gate
		/// </summary>
		public void StopSounds()
		{
			this.JumpGate.StopSounds();
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
			this.JumpGate.CanDoSyphonGridPower(power_mw, ticks, callback, syphon_grid_only);
		}

		/// <summary>
		/// Cancels the gate's jump
		/// For servers and singleplayer: Cancels the jump<br />
		/// For clients: Sends a cancel jump request to server
		/// </summary>
		public void CancelJump()
		{
			this.JumpGate.CancelJump();
		}

		/// <summary>
		/// Gets all jump gate drives attached to this gate
		/// </summary>
		/// <param name="drives">All attached jump gate drives<br />List will not be cleared</param>
		/// <param name="filter">A predicate to match drives against</param>
		public void GetJumpGateDrives(List<MyAPIJumpGateDrive> drives, Func<MyAPIJumpGateDrive, bool> filter = null)
		{
			if (filter == null) this.JumpGate.GetJumpGateDrives(drives, MyAPISession.GetNewBlock);
			else this.JumpGate.GetJumpGateDrives(drives, MyAPISession.GetNewBlock, (drive) => {
				MyAPIJumpGateDrive api = MyAPISession.GetNewBlock(drive);
				try { return filter == null || filter(api); }
				finally { api.Release(); }
			});
		}

		/// <summary>
		/// Gets a list of all drives attached to this gate as the specified type
		/// </summary>
		/// <param name="drives">All attached jump gate drives<br />List will not be cleared</param>
		/// <param name="transformer">A transformer converting a controller to the specified type</param>
		/// <param name="filter">A predicate to match jump gate drives against</param>
		public void GetJumpGateDrives<T>(List<T> drives, Func<MyAPIJumpGateDrive, T> transformer, Func<MyAPIJumpGateDrive, bool> filter = null)
		{
			this.JumpGate.GetJumpGateDrives(drives, (block) => {
				MyAPIJumpGateDrive api = MyAPISession.GetNewBlock(block);
				try { return transformer(api); }
				finally { api.Release(); }
			}, (block) => {
				MyAPIJumpGateDrive api = MyAPISession.GetNewBlock(block);
				try { return filter == null || filter(api); }
				finally { api.Release(); }
			});
		}

		/// <summary>
		/// Gets all working jump gate drives attached to this gate
		/// </summary>
		/// <param name="drives">All attached, working jump gate drives<br />List will not be cleared</param>
		/// <param name="filter">A predicate to match drives against</param>
		public void GetWorkingJumpGateDrives(List<MyAPIJumpGateDrive> drives, Func<MyAPIJumpGateDrive, bool> filter = null)
		{
			if (filter == null) this.JumpGate.GetWorkingJumpGateDrives(drives, MyAPISession.GetNewBlock);
			else this.JumpGate.GetWorkingJumpGateDrives(drives, MyAPISession.GetNewBlock, (drive) => {
				MyAPIJumpGateDrive api = MyAPISession.GetNewBlock(drive);
				try { return filter == null || filter(api); }
				finally { api.Release(); }
			});
		}

		/// <summary>
		/// Gets a list of all working drives attached to this gate as the specified type
		/// </summary>
		/// <param name="drives">All attached jump gate drives<br />List will not be cleared</param>
		/// <param name="transformer">A transformer converting a controller to the specified type</param>
		/// <param name="filter">A predicate to match jump gate drives against</param>
		public void GetWorkingJumpGateDrives<T>(List<T> drives, Func<MyAPIJumpGateDrive, T> transformer, Func<MyAPIJumpGateDrive, bool> filter = null)
		{
			this.JumpGate.GetWorkingJumpGateDrives(drives, (block) => {
				MyAPIJumpGateDrive api = MyAPISession.GetNewBlock(block);
				try { return transformer(api); }
				finally { api.Release(); }
			}, (block) => {
				MyAPIJumpGateDrive api = MyAPISession.GetNewBlock(block);
				try { return filter == null || filter(api); }
				finally { api.Release(); }
			});
		}

		/// <summary>
		/// Gets all entities within this gate's jump space ellipsoid not yet initialized on client
		/// </summary>
		/// <param name="entities">All unconfirmed entities within this jump space<br />Dictionary will not be cleared</param>
		/// <param name="filtered">Whether to remove blacklisted entities per this gate's controller</param>
		public void GetUninitializedEntititesInJumpSpace(Dictionary<long, float> entities, bool filtered = false)
		{
			this.JumpGate.GetUninitializedEntititesInJumpSpace(entities, filtered);
		}

		/// <summary>
		/// Gets all entities within this gate's jump space ellipsoid
		/// </summary>
		/// <param name="entities">All entities within this jump space<br />Dictionary will not be cleared</param>
		/// <param name="filtered">Whether to remove blacklisted entities per this gate's controller</param>
		public void GetEntitiesInJumpSpace(Dictionary<MyEntity, float> entities, bool filtered = false)
		{
			this.JumpGate.GetEntitiesInJumpSpace(entities, filtered);
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
			this.JumpGate.OffEntityCollision(callback);
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
			this.JumpGate.OffEntityCollision(callback);
		}

		/// <summary>
		/// Checks if the specified callback is registered with this gate
		/// </summary>
		/// <param name="callback">The callback to check</param>
		/// <returns>True if already registered</returns>
		public bool IsEntityCollisionCallbackRegistered(Action<MyAPIJumpGate, MyEntity, bool> callback)
		{
			return this.JumpGate.IsEntityCollisionCallbackRegistered(callback);
		}

		/// <summary>
		/// Checks if a point is within twice the distance of the largest jump ellipse radius or within 500 meters of the attached controller
		/// </summary>
		/// <param name="pos">The world position to check</param>
		/// <returns>True if in range</returns>
		public bool IsPointInHudBroadcastRange(ref Vector3D pos)
		{
			return this.JumpGate.IsPointInHudBroadcastRange(ref pos);
		}

		/// <summary>
		/// Checks if entity passes all controller filters and is allowed by gate
		/// </summary>
		/// <param name="entity">The entity to check</param>
		/// <returns>True if entity can be jumped by this gate</returns>
		public bool IsEntityValidForJumpSpace(MyEntity entity)
		{
			return this.JumpGate.IsEntityValidForJumpSpace(entity);
		}

		/// <summary>
		/// Whether this grid is valid
		/// </summary>
		/// <returns>True if valid</returns>
		public bool IsValid()
		{
			return this.JumpGate.IsValid();
		}

		/// <summary>
		/// Whether this grid is valid and a controller attached
		/// </summary>
		/// <returns>True if complete</returns>
		public bool IsComplete()
		{
			return this.JumpGate.IsComplete();
		}

		/// <summary>
		/// </summary>
		/// <returns>True if this gate is idle</returns>
		public bool IsIdle()
		{
			return this.JumpGate.IsIdle();
		}

		/// <summary>
		/// </summary>
		/// <returns>True if this gate is inbound or outbound</returns>
		public bool IsJumping()
		{
			return this.JumpGate.IsJumping();
		}

		/// <summary>
		/// </summary>
		/// <returns>True if this gate is large grid</returns>
		public bool IsLargeGrid()
		{
			return this.JumpGate.IsLargeGrid();
		}

		/// <summary>
		/// </summary>
		/// <returns>True if this gate is small grid</returns>
		public bool IsSmallGrid()
		{
			return this.JumpGate.IsSmallGrid();
		}

		/// <summary>
		/// Checks if this MyJumpGate equals another
		/// </summary>
		/// <param name="other">The MyJumpGate to check</param>
		/// <returns>Equality</returns>
		public bool Equals(MyAPIJumpGate other)
		{
			return this.JumpGate == other.JumpGate;
		}

		/// <summary>
		/// </summary>
		/// <returns>The status of this gate's jump space collision detector</returns>
		public MyGateColliderStatus JumpSpaceColliderStatus()
		{
			return this.JumpGate.JumpSpaceColliderStatus();
		}

		/// <summary>
		/// Gets the reason this gate is invalid
		/// </summary>
		/// <returns>The invalidation reason or InvalidationReason.NONE</returns>
		public MyGateInvalidationReason GetInvalidationReason()
		{
			return this.JumpGate.GetInvalidationReason();
		}

		/// <summary>
		/// Get the number of jump space entities matching the specified predicate or all entities if predicate is null
		/// </summary>
		/// <param name="filtered">Whether to filter entities based on controller settings</param>
		/// <param name="filter">A predicate to filter entities by</param>
		/// <returns>The number of matching entities</returns>
		public int GetJumpSpaceEntityCount(bool filtered = false, Func<MyEntity, bool> filter = null)
		{
			return this.JumpGate.GetJumpSpaceEntityCount(filtered, filter);
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
			return this.JumpGate.PlaySound(sound, distance, volume, pos);
		}

		/// <summary>
		/// Attemps to drain the specified power from all working drives attached to this gate
		/// </summary>
		/// <param name="power_mw">The amount of drain to syphon in MegaWatts</param>
		/// <returns>The remaining power after syphon</returns>
		public double SyphonGridDrivePower(double power_mw)
		{
			return this.JumpGate.SyphonGridDrivePower(power_mw);
		}

		/// <summary>
		/// Calculates the distance ratio given a taret endpoint
		/// </summary>
		/// <param name="endpoint">The target endpont</param>
		/// <returns>A ratio between this gate's maximum distance and minimum distance</returns>
		public double CalculateDistanceRatio(ref Vector3D endpoint)
		{
			return this.JumpGate.CalculateDistanceRatio(ref endpoint);
		}

		/// <summary>
		/// Calculates the power multiplier given a distance ratio
		/// </summary>
		/// <param name="ratio">The distance ratio</param>
		/// <returns>The power factor</returns>
		public double CalculatePowerFactorFromDistanceRatio(double ratio)
		{
			return this.JumpGate.CalculatePowerFactorFromDistanceRatio(ratio);
		}

		/// <summary>
		/// Calculates the power density (in Kw/Kg) required for a single kilogram to jump
		/// </summary>
		/// <param name="endpoint">The target endpoint</param>
		/// <returns>The required power density</returns>
		public double CalculateUnitPowerRequiredForJump(ref Vector3D endpoint)
		{
			return this.JumpGate.CalculateUnitPowerRequiredForJump(ref endpoint);
		}

		/// <summary>
		/// Calculates the maximum distance this gate can jump such that the power factor does not exceed 1
		/// </summary>
		/// <returns>The max reasonable distance in meters</returns>
		public double CalculateMaxGateDistance()
		{
			return this.JumpGate.CalculateMaxGateDistance();
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
			return this.JumpGate.CalculateTotalRequiredPower(endpoint, has_target_gate, mass);
		}

		/// <summary>
		/// Calculates the minimum number of drives needed to achieve a jump of some distance such that the power factor does not exceed 1
		/// </summary>
		/// <param name="distance">The jump distance in meters</param>
		/// <returns>The number of drives required</returns>
		public int CalculateDrivesRequiredForDistance(double distance)
		{
			return this.JumpGate.CalculateDrivesRequiredForDistance(distance);
		}

		/// <summary>
		/// Gets the raduis of this gate's jump ellipse taking into account controller settings
		/// </summary>
		/// <returns>This gate's jump ellipsoid lateral radius</returns>
		public double JumpNodeRadius()
		{
			return this.JumpGate.JumpNodeRadius();
		}

		/// <summary>
		/// </summary>
		/// <returns>The cube grid size of this gate</returns>
		public MyCubeSize CubeGridSize()
		{
			return this.JumpGate.CubeGridSize();
		}

		/// <summary>
		/// Gets the effective jump space ellipsoid for this gate<br />
		/// This differes from the standard ellipsoid only when the target is another jump gate<br />
		/// In this case, the effective ellipsoid is an ellipsoid composed of the smallest radii from the two ellipsoids
		/// </summary>
		/// <returns>The effective jump ellipsoid</returns>
		public BoundingEllipsoidD GetEffectiveJumpEllipse()
		{
			return this.JumpGate.GetEffectiveJumpEllipse();
		}

		/// <summary>
		/// Gets the world matrix for this gate<br />
		/// Resulting matrix may be affected by the normal override and endpoint
		/// </summary>
		/// <param name="use_endpoint">Whether to calculate the world matrix using this gate's endpoint</param>
		/// <returns>This gate's world matrix</returns>
		public MatrixD GetWorldMatrix(bool use_endpoint, bool use_normal_override)
		{
			return this.JumpGate.GetWorldMatrix(use_endpoint, use_normal_override);
		}

		/// <summary>
		/// Gets the world matrix for this gate<br />
		/// Resulting matrix may be affected by the normal override and endpoint
		/// </summary>
		/// <param name="use_endpoint">Whether to calculate the world matrix using this gate's endpoint</param>
		/// <returns>This gate's world matrix</returns>
		public void GetWorldMatrix(out MatrixD world_matrix, bool use_endpoint, bool use_normal_override)
		{
			this.GetWorldMatrix(out world_matrix, use_endpoint, use_normal_override);
		}

		/// <summary>
		/// Gets this gate's name
		/// </summary>
		/// <returns>The name from the attached controller or the last name if no controller attached</returns>
		public string GetName()
		{
			return this.JumpGate.GetName();
		}

		/// <summary>
		/// </summary>
		/// <returns>The printable name for this gate</returns>
		public string GetPrintableName()
		{
			return this.JumpGate.GetPrintableName();
		}
		#endregion
	}
}
