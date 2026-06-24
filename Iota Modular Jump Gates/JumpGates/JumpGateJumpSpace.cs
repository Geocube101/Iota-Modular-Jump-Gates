using IOTA.ModularJumpGates.Animation;
using IOTA.ModularJumpGates.CubeBlock;
using IOTA.ModularJumpGates.Util;
using IOTA.ModularJumpGates.Util.ConcurrentCollections;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace IOTA.ModularJumpGates.JumpGates
{
	internal partial class MyJumpGate
	{
		#region Private Variables
		/// <summary>
		/// Whether the jump space physical detector should be forcefully updated on next tick
		/// </summary>
		private bool ForceUpdateJumpSpaceDetector = false;

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
		/// A mutex for exclusive read-write to the intersect nodes list
		/// </summary>
		private readonly object DriveIntersectNodesMutex = new object();

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
		/// The jump space collider for this gate
		/// </summary>
		private MyEntity JumpSpaceCollisionDetector = null;

		/// <summary>
		/// The jump space detector for this gate
		/// </summary>
		private MyPhysicalDetector PhysicalDetector = null;

		/// <summary>
		/// The list of intersect nodes for this gate
		/// </summary>
		private List<Vector3D> InnerDriveIntersectNodes = new List<Vector3D>();

		/// <summary>
		/// Temporary list used when updating jump space entities
		/// </summary>
		private List<MyEntity> JumpSpaceColliderTopmost = new List<MyEntity>();

		/// <summary>
		/// A list of all blocks to be destroyed during jump
		/// </summary>
		private ConcurrentLinkedHashSet<IMySlimBlock> ShearBlocks = new ConcurrentLinkedHashSet<IMySlimBlock>();

		/// <summary>
		/// Map mapping plane normals to a drive intersect plane
		/// </summary>
		private Dictionary<Vector3D, JumpDriveIntersectPlane> DriveIntersectionPlanes = new Dictionary<Vector3D, JumpDriveIntersectPlane>();

		/// <summary>
		/// List of all entities in this gate's jump space collider
		/// </summary>
		private ConcurrentDictionary<long, MyEntity> JumpSpaceColliderEntities = new ConcurrentDictionary<long, MyEntity>();

		/// <summary>
		/// The master map of entities within this gate's jump space
		/// </summary>
		private ConcurrentDictionary<long, KeyValuePair<float, MyEntity>> JumpSpaceEntities = new ConcurrentDictionary<long, KeyValuePair<float, MyEntity>>();
		#endregion

		#region Public Variables

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
				return this.TrueWorldJumpEllipse.WorldMatrix.Translation;
			}
			set
			{
				this.LocalJumpNode = MyJumpGateModSession.WorldVectorToLocalVectorP(ref this.ConstructMatrix, value);
				this.TrueLocalJumpEllipse.WorldMatrix.Translation = this.LocalJumpNode;
				this.TrueWorldJumpEllipse.WorldMatrix.Translation = value;
			}
		}

		/// <summary>
		/// The true endpoint of this jump gate<br />
		/// Only non-null when this gate is outbound
		/// </summary>
		public Vector3D? TrueEndpoint;

		/// <summary>
		/// A direction vector indicating the velocity of this gate's jump node in m/s
		/// </summary>
		public Vector3D JumpNodeVelocity { get; private set; }

		/// <summary>
		/// The PlaneD representing this gate's primary drive plane
		/// </summary>
		public PlaneD? PrimaryDrivePlane { get; private set; }

		/// <summary>
		/// Callback event for entity entering jump space
		/// Callback accepts 3 parameters:
		/// 1: This jump gate
		/// 2: The entity entering or leaving
		/// 3: Whether the entity is entering
		/// </summary>
		public event Action<MyJumpGate, MyEntity, bool> EntityEnterered;

		/// <summary>
		/// Gets this gate's world-aligned artificial JumpEllipse using the associated controller
		/// </summary>
		public BoundingEllipsoidD JumpEllipse
		{
			get
			{
				BoundingEllipsoidD world_ellipse = this.TrueWorldJumpEllipse;
				double? depth_percent = this.ControlObject?.BlockSettings?.JumpSpaceDepthPercent();
				if (depth_percent == null) return world_ellipse;
				Vector3D radii = world_ellipse.Radii;
				radii.Y = world_ellipse.Radii.X * depth_percent.Value;
				world_ellipse.Radii = radii;
				return world_ellipse;
			}
		}

		/// <summary>
		/// Gets this gate's world-aligned shearing ellipse<br />
		/// This ellipse is used to calculate grid shearing<br />
		/// This ellipse is the jump ellipse padded by 5 meters
		/// </summary>
		public BoundingEllipsoidD ShearEllipse => this.JumpEllipse + 5;

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
		#endregion

		#region Private Methods
		/// <summary>
		/// Callback for jump space physics collider
		/// </summary>
		/// <param name="entity">The entity entering/leaving the jump sphere</param>
		/// <param name="is_entering">Whether the entity is entering</param>
		private void OnEntityCollision(IMyEntity entity, bool is_entering)
		{
			if (this.Closed || entity == this.JumpSpaceCollisionDetector || entity == null) return;
			if (is_entering) this.JumpSpaceColliderEntities[entity.EntityId] = (MyEntity) entity;
			else if (!is_entering) this.JumpSpaceColliderEntities.Remove(entity.EntityId);
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
			this.EntityEnterered?.Invoke(this, entity, true);
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
			this.EntityEnterered?.Invoke(this, entity, false);
		}

		/// <summary>
		/// Updates this jump gate's local jump space ellipsoid
		/// </summary>
		private void UpdateJumpSpaceEllipsoid()
		{
			if (MyNetworkInterface.IsStandaloneMultiplayerClient) return;
			int most_aligned_plane = 0;
			int largest_plane = 0;
			List<MyJumpGateDrive> drives = this.GetJumpGateDrives().ToList();
			this.ForceUpdateJumpEllipsoid = false;
			Vector3D jump_node = this.WorldJumpNode;
			Vector3D facing = jump_node - this.ConstructMatrix.Translation;

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
					Vector3D primary_normal = ((normal - facing).LengthSquared() < (normal_inv - facing).LengthSquared()) ? normal : normal_inv;
					JumpDriveIntersectPlane plane;

					if (this.DriveIntersectionPlanes.ContainsKey(primary_normal)) plane = this.DriveIntersectionPlanes[primary_normal];
					else
					{
						plane = new JumpDriveIntersectPlane()
						{
							Plane = drive_plane,
							PlanePrimaryNormal = primary_normal,
						};
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
				JumpDriveIntersectPlane intersection_plane = this.DriveIntersectionPlanes.Select((pair) => pair.Value).FirstOrDefault((plane) => plane.AlignedDrivesCount == most_aligned_plane && plane.JumpGateDrives.Count == largest_plane);

				if (intersection_plane == null)
				{
					this.TrueLocalJumpEllipse = BoundingEllipsoidD.Zero;
					this.TrueWorldJumpEllipse = BoundingEllipsoidD.Zero;
					this.DriveIntersectionPlanes.Clear();
					this.PrimaryDrivePlane = null;
					return;
				}

				double plane_height = drives[0].ModelBoundingBoxSize.Y;
				double distance = 0;
				MyJumpGateControlObject control_object = this.ControlObject;
				MyJumpSpaceFitType fit_type = MyJumpSpaceFitType.INNER;
				if (control_object != null && control_object.IsController) fit_type = control_object.Controller.BlockSettings.JumpSpaceFitType();
				else if (control_object != null) fit_type = control_object.RemoteAntenna.BlockSettings.BaseControllerSettings[this.RemoteAntennaChannel].JumpSpaceFitType();
				Vector3D forward = Vector3D.Cross(intersection_plane.PlanePrimaryNormal, this.ConstructMatrix.Forward);
				MatrixD plane_matrix = MatrixD.CreateWorld(jump_node, forward, intersection_plane.PlanePrimaryNormal);

				foreach (MyJumpGateDrive drive in drives)
				{
					if (intersection_plane.JumpGateDrives.Contains(drive)) continue;
					Vector3D local_pos = MyJumpGateModSession.WorldVectorToLocalVectorP(ref plane_matrix, drive.GetDriveRaycastStartpoint());
					if (!double.IsNaN(local_pos.Y)) plane_height = Math.Max(plane_height, Math.Abs(local_pos.Y));
				}

				switch (fit_type)
				{
					case MyJumpSpaceFitType.OUTER:
						distance = Math.Sqrt(drives.Select((drive) => Vector3D.DistanceSquared(drive.GetDriveRaycastStartpoint(), jump_node)).Max());
						break;
					case MyJumpSpaceFitType.AVERAGE:
						distance = Math.Sqrt(drives.Select((drive) => Vector3D.DistanceSquared(drive.GetDriveRaycastStartpoint(), jump_node)).Average());
						break;
					case MyJumpSpaceFitType.INNER:
					default:
						distance = Math.Sqrt(drives.Select((drive) => Vector3D.DistanceSquared(drive.GetDriveRaycastStartpoint(), jump_node)).Min());
						break;
				}

				this.PrimaryDrivePlane = intersection_plane.Plane;
				Vector3D radii = new Vector3D(distance, plane_height, distance);
				this.TrueWorldJumpEllipse = new BoundingEllipsoidD(ref radii, ref plane_matrix);
				this.TrueLocalJumpEllipse = this.TrueWorldJumpEllipse.Transformed(ref this.ConstructMatrixInv);
			}
			else
			{
				this.PrimaryDrivePlane = null;
				this.TrueLocalJumpEllipse = BoundingEllipsoidD.Zero;
				this.TrueWorldJumpEllipse = BoundingEllipsoidD.Zero;
			}

			this.DriveIntersectionPlanes.Clear();
		}

		/// <summary>
		/// Updates this jump gate's internal jump space entity list
		/// </summary>
		private void UpdateJumpSpaceEntities()
		{
			if (this.Closed) return;
			BoundingEllipsoidD jump_ellipse = this.GetEffectiveJumpEllipse();
			MyJumpGateConstruct parent;
			this.JumpSpaceColliderTopmost.AddRange(this.JumpSpaceColliderEntities.Select((pair) => pair.Value.GetTopMostParent()).Distinct());

			foreach (KeyValuePair<long, KeyValuePair<float, MyEntity>> pair in this.JumpSpaceEntities)
			{
				MyEntity child = pair.Value.Value ?? (MyEntity) MyAPIGateway.Entities.GetEntityById(pair.Key);
				if (child == null || this.JumpSpaceColliderTopmost.Contains(child)) continue;
				this.OnEntityJumpSpaceLeave(child);
				this.JumpSpaceEntities.Remove(pair.Key);
				this.ArrivingWormholeEntities.Remove(child);
				Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} Entity marked for jump-space removal @ {pair.Key} >> OUTSIDE_PHYSICAL_COLLIDER", 5);
			}

			foreach (MyEntity entity in this.JumpSpaceColliderTopmost)
			{
				bool is_self = this.JumpGateGrid.IsEntityPartOfConstruct(entity);

				if (entity?.Physics == null || entity.MarkedForClose || is_self)
				{
					KeyValuePair<float, MyEntity> _;
					if (entity == null || !this.JumpSpaceEntities.TryRemove(entity.EntityId, out _)) continue;
					this.OnEntityJumpSpaceLeave(entity);
					this.ArrivingWormholeEntities.Remove(entity);
					Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} Entity marked for jump-space removal @ {entity.EntityId} >> PHYSICS={entity.Physics}, CLOSED={entity.MarkedForClose}, IS_SELF={is_self}", 5);
					continue;
				}

				MyEntity add_entity = null;
				float? mass = null;
				bool bounded = false;

				if (entity is MyCubeGrid && (parent = MyJumpGateModSession.Instance.GetUnclosedJumpGateGrid(entity.EntityId)) != null && !parent.IsStatic() && (bounded = parent.IsConstructWithinBoundingEllipsoid(ref jump_ellipse)))
				{
					add_entity = (MyCubeGrid) parent.CubeGrid;
					mass = (float) parent.ConstructMass();
				}
				else if (entity is IMyCharacter && (bounded = jump_ellipse.IsPointInEllipse(entity.WorldMatrix.Translation)))
				{
					add_entity = entity;
					mass = ((IMyCharacter) entity).CurrentMass;
				}
				else if (!(entity is MyCubeGrid)) add_entity = (bounded = jump_ellipse.IsPointInEllipse(entity.WorldMatrix.Translation)) ? entity : null;

				KeyValuePair<float, MyEntity> old_pairing;
				float new_mass = mass ?? add_entity?.Physics.Mass ?? float.NaN;

				if (this.JumpSpaceEntities.ContainsKey(entity.EntityId) && add_entity == null)
				{
					this.JumpSpaceEntities.Remove(entity.EntityId);
					this.ArrivingWormholeEntities.Remove(entity);
					this.OnEntityJumpSpaceLeave(entity);
					Logger.Debug($"[{this.JumpGateGrid.CubeGridID}]-{this.JumpGateID} Entity marked for jump-space removal @ {entity.EntityId} >> TYPE={entity.GetType().Name}, STATIC={entity is MyCubeGrid && ((MyCubeGrid) entity).IsStatic}, BOUNDED={bounded}, ISSELF={is_self}", 5);
				}
				else if (add_entity != null && this.JumpSpaceEntities.TryGetValue(add_entity.EntityId, out old_pairing) && old_pairing.Key != new_mass) this.JumpSpaceEntities[add_entity.EntityId] = new KeyValuePair<float, MyEntity>(new_mass, add_entity);
				else if (add_entity != null && this.JumpSpaceEntities.TryAdd(add_entity.EntityId, new KeyValuePair<float, MyEntity>(new_mass, add_entity))) this.OnEntityJumpSpaceEnter(add_entity);
			}

			this.JumpSpaceColliderTopmost.Clear();
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Marks this jump gate's physics collider as dirty<br />
		/// It will be updated on next physics tick
		/// </summary>
		public void SetColliderDirty()
		{
			this.ForceUpdateJumpSpaceDetector = !this.Closed;
		}

		/// <summary>
		/// Marks this jump gate's physics collider as dirty<br />
		/// It will be updated on next physics tick or a packet sent to server if in standalone client
		/// </summary>
		public void SetColliderDirtyMP()
		{
			if (MyNetworkInterface.IsStandaloneMultiplayerClient)
			{
				MyNetworkInterface.Packet packet = new MyNetworkInterface.Packet
				{
					PacketType = MyPacketTypeEnum.GATE_DEBUG,
					TargetID = 0,
					Broadcast = false,
				};
				packet.Payload(new JumpGateDebugPayload
				{
					DebugType = 0,
					JumpGateUUID = JumpGateUUID.FromJumpGate(this),
				});
				packet.Send();
				return;
			}

			this.ForceUpdateJumpSpaceDetector = !this.Closed;
		}

		/// <summary>
		/// Marks this jump gate's jump ellipsoid as dirty<br />
		/// It will be updated on next tick
		/// </summary>
		public void SetJumpSpaceEllipsoidDirty()
		{
			this.ForceUpdateJumpEllipsoid = !this.Closed;
		}

		/// <summary>
		/// Updates the public list of drive intersect nodes
		/// </summary>
		/// <param name="intersect_nodes">The new intersections list</param>
		public void UpdateDriveIntersectNodes(IEnumerable<Vector3D> intersect_nodes)
		{
			if (this.Closed) return;

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
		/// Checks if entity passes all controller filters and is allowed by gate
		/// </summary>
		/// <param name="entity">The entity to check</param>
		/// <returns>True if entity can be jumped by this gate</returns>
		public bool IsEntityValidForJumpSpace(MyEntity entity)
		{
			if (this.Closed || this.Controller == null) return false;
			KeyValuePair<float, float> allowed_mass = this.Controller.BlockSettings.AllowedEntityMass();
			KeyValuePair<uint, uint> allowed_size = this.Controller.BlockSettings.AllowedCubeGridSize();
			if (entity == null || entity.Physics == null || entity.MarkedForClose || entity.Closed || this.JumpGateGrid.IsEntityPartOfConstruct(entity)) return false;
			else if (entity.Physics.Mass < allowed_mass.Key || entity.Physics.Mass > allowed_mass.Value) return false;
			else if (this.Controller.BlockSettings.IsEntityBlacklisted(entity.EntityId)) return false;
			else if (entity is MyCubeGrid)
			{
				IMyCubeGrid grid = (MyCubeGrid) entity;
				MyJumpGateConstruct construct = MyJumpGateModSession.Instance.GetJumpGateGrid(grid);
				int count = construct?.GetConstructBlockCount() ?? -1;
				if (construct == null || count < allowed_size.Key || count > allowed_size.Value) return false;
				else if (this.EntityBatches != null && this.EntityBatches.Any((pair) => pair.Value.IsEntityInBatch(entity))) return false;
				else if (this.ArrivingWormholeEntities.Contains((MyCubeGrid) construct.GetMainCubeGrid())) return false;
			}
			else if (this.EntityBatches != null && this.EntityBatches.Any((pair) => pair.Value.IsEntityInBatch(entity))) return false;
			else if (this.ArrivingWormholeEntities.Contains(entity)) return false;
			return true;
		}

		/// <summary>
		/// </summary>
		/// <returns>The status of this gate's jump space collision detector</returns>
		public MyGateColliderStatus JumpSpaceColliderStatus()
		{
			if (MyNetworkInterface.IsStandaloneMultiplayerClient) return this.MPGateColliderStatus;
			else if (this.Closed || this.JumpSpaceCollisionDetector == null) return MyGateColliderStatus.NONE;
			else return (this.JumpSpaceCollisionDetector.MarkedForClose) ? MyGateColliderStatus.CLOSED : MyGateColliderStatus.ATTACHED;
		}

		/// <summary>
		/// Get the number of jump space entities matching the specified predicate or all entities if predicate is null
		/// </summary>
		/// <param name="filtered">Whether to filter entities based on controller settings</param>
		/// <param name="filter">A predicate to filter entities by</param>
		/// <returns>The number of matching entities</returns>
		public int GetJumpSpaceEntityCount(bool filtered = false, Func<MyEntity, bool> filter = null)
		{
			if (this.Closed) return 0;
			int count = 0;

			if (filtered && this.Controller != null)
			{
				foreach (KeyValuePair<long, KeyValuePair<float, MyEntity>> pair in this.JumpSpaceEntities)
				{
					MyEntity entity = (MyEntity) MyAPIGateway.Entities.GetEntityById(pair.Key);
					if (entity != null && !this.Controller.BlockSettings.IsEntityBlacklisted(entity.EntityId) && this.IsEntityValidForJumpSpace(entity) && (filter == null || filter(entity))) ++count;
				}
			}
			else
			{
				foreach (KeyValuePair<long, KeyValuePair<float, MyEntity>> pair in this.JumpSpaceEntities)
				{
					MyEntity entity = (MyEntity) MyAPIGateway.Entities.GetEntityById(pair.Key);
					if (entity != null && (filter == null || filter(entity))) ++count;
				}
			}

			return count;
		}

		/// <summary>
		/// Gets the raduis of this gate's jump ellipse taking into account controller settings
		/// </summary>
		/// <returns>This gate's jump ellipsoid lateral radius</returns>
		public double JumpNodeRadius()
		{
			if (this.Closed) return 0;
			double controller_limit = this.ControlObject?.BlockSettings?.JumpSpaceRadius() ?? double.MaxValue;
			return Math.Min(this.TrueLocalJumpEllipse.Radii.X, controller_limit);
		}

		/// <returns>The world matrix of this gate's physical collider or null if no collider assigned</returns>
		public MatrixD? GetColliderMatrix()
		{
			return this.JumpSpaceCollisionDetector?.WorldMatrix;
		}

		/// <summary>
		/// Gets the effective jump space ellipsoid for this gate<br />
		/// This differes from the standard ellipsoid only when the target is another jump gate<br />
		/// In this case, the effective ellipsoid is an ellipsoid composed of the smallest radii from the two ellipsoids
		/// </summary>
		/// <returns>The effective jump ellipsoid</returns>
		public BoundingEllipsoidD GetEffectiveJumpEllipse()
		{
			if (this.Closed) return BoundingEllipsoidD.Zero;
			MyJumpGateWaypoint waypoint = this.ControlObject?.BlockSettings?.SelectedWaypoint();
			MyJumpGate target_gate = (waypoint == null || waypoint.WaypointType != MyWaypointType.JUMP_GATE) ? null : MyJumpGateModSession.Instance.GetJumpGate(waypoint.JumpGate);
			BoundingEllipsoidD this_ellipse = this.JumpEllipse;
			if (target_gate == null) return this_ellipse;
			BoundingEllipsoidD target_ellipse = target_gate.JumpEllipse;

			Vector3D radii = new Vector3D(
				Math.Min(this_ellipse.Radii.X, target_ellipse.Radii.X),
				Math.Min(this_ellipse.Radii.Y, target_ellipse.Radii.Y),
				Math.Min(this_ellipse.Radii.Z, target_ellipse.Radii.Z)
			);

			return new BoundingEllipsoidD(ref radii, ref this.TrueWorldJumpEllipse.WorldMatrix);
		}

		/// <summary>
		/// Gets all entities within this gate's jump space ellipsoid<br />
		/// Resulting key-value pair is an entity and a float (entity's mass in kilograms)
		/// </summary>
		/// <param name="filtered">Whether to remove blacklisted entities per this gate's controller</param>
		/// <returns>An IEnumerable referencing all entities within the jump space</returns>
		public IEnumerable<KeyValuePair<MyEntity, float>> GetEntitiesInJumpSpace(bool filtered = false)
		{
			if (this.Closed) return Enumerable.Empty<KeyValuePair<MyEntity, float>>();
			if (filtered && this.Controller != null) return this.JumpSpaceEntities.Select((pair) => new KeyValuePair<MyEntity, float>((MyEntity) MyAPIGateway.Entities.GetEntityById(pair.Key), pair.Value.Key)).Where((pair) => pair.Key != null && !this.Controller.BlockSettings.IsEntityBlacklisted(pair.Key.EntityId) && this.IsEntityValidForJumpSpace(pair.Key));
			else return this.JumpSpaceEntities.Select((pair) => new KeyValuePair<MyEntity, float>((MyEntity) MyAPIGateway.Entities.GetEntityById(pair.Key), pair.Value.Key)).Where((pair) => pair.Key != null);
		}

		/// <summary>
		/// Gets all entities within this gate's jump space collider sphere
		/// </summary>
		/// <param name="filtered">Whether to remove blacklisted entities per this gate's controller</param>
		/// <returns>An IEnumerable referencing all entities within the jump space collider</returns>
		public IEnumerable<MyEntity> GetEntitiesInCollider(bool filtered = false)
		{
			if (this.Closed) return Enumerable.Empty<MyEntity>();
			if (filtered && this.Controller != null) return this.JumpSpaceColliderEntities.Select((pair) => pair.Value).Where((entity) => !this.Controller.BlockSettings.IsEntityBlacklisted(entity.EntityId) && this.IsEntityValidForJumpSpace(entity));
			else return this.JumpSpaceColliderEntities.Values;
		}

		/// <summary>
		/// Gets all entities within a jump collider<br />
		/// If a wormhole is currenly active and at least one jump collider defined, the entities will be those currently intersecting the collider<br />
		/// Otherwise, the entities will be those within the jump space ellipsoid<br />
		/// Resulting key-value pair is an entity and a float (entity's mass in kilograms)
		/// </summary>
		/// <param name="filtered">Whether to remove blacklisted entities per this gate's controller</param>
		/// <returns>An IEnumerable referencing all entities within a jump space</returns>
		public IEnumerable<KeyValuePair<MyEntity, float>> GetEntitiesReadyForJump(bool filtered = false)
		{
			MyJumpGateWormholeAnimation animation = MyJumpGateModSession.Instance.GetGateAnimationsPlaying(this.SenderGate ?? this).FirstOrDefault((anim) => anim.ActiveAnimationIndex == 4)?.GateWormholeLoopAnimation;
			
			if (animation != null && animation.GetCollidersOfType(CollisionEffectTypeEnum.JUMP, this).Any())
			{
				foreach (MyEntity entity in animation.GetAllColliderEntities(CollisionEffectTypeEnum.JUMP, this).Select((entity) => entity.GetTopMostParent()).Distinct())
				{
					if (entity?.Physics == null) continue;
					else if (!filtered || (this.Controller != null && !this.Controller.BlockSettings.IsEntityBlacklisted(entity.EntityId) && this.IsEntityValidForJumpSpace(entity)))
					{
						KeyValuePair<float, MyEntity> pair;
						if (this.JumpSpaceEntities.TryGetValue(entity.EntityId, out pair)) yield return new KeyValuePair<MyEntity, float>(pair.Value, pair.Key);
						else yield return new KeyValuePair<MyEntity, float>(entity, (float) entity.Physics.Mass);
					}
				}
			}
			else
			{
				foreach (KeyValuePair<MyEntity, float> pair in this.GetEntitiesInJumpSpace(filtered))
				{
					yield return pair;
				}
			}
		}
		#endregion
	}
}
