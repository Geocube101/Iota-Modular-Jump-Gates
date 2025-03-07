using IOTA.ModularJumpGates.CubeBlock;
using ProtoBuf;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace IOTA.ModularJumpGates.Util
{
	/// <summary>
	/// Class holding an animation request
	/// </summary>
	internal class AnimationInfo
    {
		#region Public Variables
		/// <summary>
		/// The jump gate animation to play
		/// </summary>
		public readonly MyJumpGateAnimation Animation;

		/// <summary>
		/// The animation index to play
		/// </summary>
        public byte AnimationIndex;

		/// <summary>
		/// The callback executed every animation tick
		/// </summary>
        public Func<bool> IterCallback;

		/// <summary>
		/// The callback executed when animation is stopped
		/// </summary>
        public Action<Exception> CompletionCallback;

		/// <summary>
		/// The exeption caught during IterCallback execution or null
		/// </summary>
		public Exception IterCallbackException = null;
		#endregion

		#region Constructors
		/// <summary>
		/// Creates a new AnimationInfo
		/// </summary>
		/// <param name="animation">The animation to play</param>
		/// <param name="animation_type">The animation type to play</param>
		/// <param name="callback">The callback to execute every animation tick</param>
		/// <param name="on_complete">The callback to execute when animation stopped, accepting the resulting exception or null</param>
		public AnimationInfo(MyJumpGateAnimation animation, MyJumpGateAnimation.AnimationTypeEnum animation_type, Func<bool> callback = null, Action<Exception> on_complete = null)
        {
            this.Animation = animation;
            this.AnimationIndex = (byte) animation_type;
            this.IterCallback = callback;
            this.CompletionCallback = on_complete;
        }
		#endregion
	}

	internal class EntityWarpInfo
	{
		private MatrixD Endpoint;
		private MatrixD StartPoint;
		private MatrixD CurrentPos;
		private readonly List<MyEntity> EntityBatch;
		private readonly List<Vector3D[]> RelativeEntityOffsets;
		private readonly List<MyPhysicsComponentBase> EntityPhysics;
		private readonly Action<List<MyEntity>> Callback;

		public readonly ushort Duration;
		public ushort CurrentTick { get; private set; }

		public EntityWarpInfo(MyJumpGate jump_gate, ref MatrixD current_position, ref MatrixD target_position, List<MyEntity> entity_batch, ushort time, Action<List<MyEntity>> callback)
		{
			MyEntity parent = entity_batch[0];
			this.Duration = time;
			this.CurrentTick = 0;
			this.EntityBatch = entity_batch;
			this.Endpoint = target_position;
			this.StartPoint = current_position;
			this.CurrentPos = current_position;
			this.RelativeEntityOffsets = new List<Vector3D[]>();
			this.Callback = callback;
			this.EntityPhysics = new List<MyPhysicsComponentBase>() { parent.Physics };
			
			foreach (MyEntity entity in entity_batch.Skip(1))
			{
				this.RelativeEntityOffsets.Add(new Vector3D[3] {
					MyJumpGateModSession.WorldVectorToLocalVectorP(ref current_position, entity.WorldMatrix.Translation),
					MyJumpGateModSession.WorldVectorToLocalVectorD(ref current_position, entity.WorldMatrix.Forward),
					MyJumpGateModSession.WorldVectorToLocalVectorD(ref current_position, entity.WorldMatrix.Up),
				});

				MyJumpGateConstruct construct = (entity is MyCubeGrid) ? MyJumpGateModSession.Instance.GetJumpGateGrid(entity.EntityId) : null;
				if (construct != null) construct.BatchingGate = jump_gate;
				this.EntityPhysics.Add(entity.Physics);
				entity.Physics.Enabled = false;
				entity.Physics = null;
			}
		}

		public bool Update()
		{
			++this.CurrentTick;
			bool complete = this.CurrentTick >= this.Duration;
			double tick_ratio = (double) this.CurrentTick / this.Duration;
			MyEntity parent = this.EntityBatch[0];
			MatrixD world_matrix;
			MatrixD.Lerp(ref this.StartPoint, ref this.Endpoint, tick_ratio, out this.CurrentPos);
			
			if (parent is IMyPlayer)
			{
				world_matrix = parent.WorldMatrix;
				world_matrix.Translation = this.CurrentPos.Translation;
				parent.Teleport(world_matrix);
			}
			else
			{
				parent.Teleport(this.CurrentPos);
				parent.WorldMatrix = MatrixD.Normalize(this.CurrentPos);
			}

			for (int i = 0; i < this.EntityBatch.Count - 1; ++i)
			{
				MyEntity entity = this.EntityBatch[i + 1];
				Vector3D[] relative_offsets = this.RelativeEntityOffsets[i];
				Vector3D translation = MyJumpGateModSession.LocalVectorToWorldVectorP(ref this.CurrentPos, relative_offsets[0]);
				
				if (entity is IMyPlayer)
				{
					world_matrix = entity.WorldMatrix;
					world_matrix.Translation = translation;
					entity.Teleport(world_matrix);
				}
				else
				{
					Vector3D forward = MyJumpGateModSession.LocalVectorToWorldVectorD(ref this.CurrentPos, relative_offsets[1]);
					Vector3D up = MyJumpGateModSession.LocalVectorToWorldVectorD(ref this.CurrentPos, relative_offsets[2]);
					forward.Normalize();
					up.Normalize();
					MatrixD.CreateWorld(ref translation, ref forward, ref up, out world_matrix);
					entity.Teleport(world_matrix);
					entity.WorldMatrix = world_matrix;
				}
			}

			if (complete)
			{
				for (int i = 0; i < this.EntityBatch.Count; ++i)
				{
					MyEntity entity = this.EntityBatch[i];
					entity.Physics = this.EntityPhysics[i];
					entity.Physics.Enabled = true;
					MyJumpGateConstruct construct = (entity is MyCubeGrid) ? MyJumpGateModSession.Instance.GetJumpGateGrid(entity.EntityId) : null;
					if (construct != null) construct.BatchingGate = null;
				}

				if (this.Callback != null) this.Callback(this.EntityBatch);
			}

			return complete;
		}
	}

	internal class EntityBatch
	{
		public readonly List<MyEntity> Batch;
		public BoundingBoxD ObstructAABB;
		public MatrixD ParentTargetMatrix;

		public MyEntity Parent { get { return this.Batch[0]; } }

		public double BatchMass
		{
			get
			{
				double mass = 0;

				foreach (MyEntity child in this.Batch)
				{
					MyJumpGateConstruct parent = (child is MyCubeGrid) ? MyJumpGateModSession.Instance.GetJumpGateGrid(child.EntityId) : null;
					mass += (parent != null) ? parent.ConstructMass() : ((child is IMyPlayer) ? ((IMyPlayer) child).Character.CurrentMass : (child.Physics?.Mass ?? 0));
				}

				return mass;
			}
		}

		public EntityBatch(List<MyEntity> batch, ref BoundingBoxD obstruct_aabb, ref MatrixD target_matrix)
		{
			this.Batch = batch;
			this.ObstructAABB = obstruct_aabb;
			this.ParentTargetMatrix = target_matrix;
		}
	}

	/// <summary>
	/// Class wrapping a MyEntity3DSoundEmitter object
	/// </summary>
	public class MySoundEmitter3D
	{
		#region Private Variables
		/// <summary>
		/// The sound emitter
		/// </summary>
		private MyEntity3DSoundEmitter SoundEmitter;
		#endregion

		#region Public Variables
		/// <summary>
		/// Whether this sound emitter is closed
		/// </summary>
		public bool Closed { get; private set; } = false;

		/// <summary>
		/// Whether this sound emitter is marked for close
		/// </summary>
		public bool MarkedForClose { get; private set; } = false;

		/// <summary>
		/// The sound to play
		/// </summary>
		public readonly string Sound;
		#endregion

		#region Constructors
		/// <summary>
		/// Creates a new SoundEmitter3D
		/// </summary>
		/// <param name="sound">The sound to play</param>
		/// <param name="pos">The world position this sound is playing at</param>
		/// <param name="distance">The distance at which this sound can be heard</param>
		/// <param name="volume">The volume of this sound</param>
		/// <param name="parent">The parent entity this sound is attached to</param>
		public MySoundEmitter3D(string sound, Vector3D pos, float? distance, float volume = 1, MyEntity parent = null)
		{
			this.SoundEmitter = new MyEntity3DSoundEmitter(parent);
			this.Sound = sound;
			this.SoundEmitter.VolumeMultiplier = volume;
			this.SoundEmitter.SetPosition(pos);
			this.SoundEmitter.PlaySound(new MySoundPair(sound), true, alwaysHearOnRealistic: true, force3D: true);
            this.SoundEmitter.CustomMaxDistance = distance;
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Updates this sound emitter's position
		/// </summary>
		/// <param name="pos">The new emitter position</param>
		public void Update(Vector3D pos)
		{
			if (this.MarkedForClose)
			{
				if (this.SoundEmitter.IsPlaying) this.SoundEmitter.StopSound(true);
				this.SoundEmitter.Cleanup();
				this.SoundEmitter = null;
				this.Closed = true;
				return;
			}
			else if (this.SoundEmitter == null) return;
            else if (this.SoundEmitter.SourcePosition != pos)
            {
				this.SoundEmitter.SetPosition(pos);
				this.SoundEmitter.Update();
			}
		}

		/// <summary>
		/// Updates this sounds emitter's volume
		/// </summary>
		/// <param name="volume">The new emitter volume</param>
		public void SetVolume(float volume)
		{
			if (!this.MarkedForClose && this.SoundEmitter != null) this.SoundEmitter.VolumeMultiplier = volume;
		}

		/// <summary>
		/// Updates this sounds emitter's maximum range
		/// </summary>
		/// <param name="volume">The new emitter range in meters</param>
		public void SetDistance(float? distance)
        {
            if (!this.MarkedForClose && this.SoundEmitter != null) this.SoundEmitter.CustomMaxDistance = distance;
		}

		/// <summary>
		/// Marks this emitter for close<br />
		/// Emitter will be cleaned on next tick
		/// </summary>
		public void Dispose(bool immediate = false)
		{
			if (this.MarkedForClose || this.SoundEmitter == null) return;
			this.MarkedForClose = true;
			if (!immediate) return;
			if (this.SoundEmitter.IsPlaying) this.SoundEmitter.StopSound(true);
			this.SoundEmitter.Cleanup();
			this.SoundEmitter = null;
			this.Closed = true;
		}

		/// <summary>
		/// </summary>
		/// <returns>Whether this sound emitter is not close and playing</returns>
		public bool IsPlaying()
		{
			return this.SoundEmitter?.IsPlaying ?? false;
		}
		#endregion
	}

	/// <summary>
	/// Class representing a numerical range
	/// </summary>
	/// <typeparam name="T">The typename; must implement IComparable</typeparam>
	[ProtoContract]
    public struct NumberRange<T> where T : IComparable<T>
	{
		#region Public Variables
		/// <summary>
		/// The lower value of this range
		/// </summary>
		[ProtoMember(1)]
        public T LowerBound;
		
		/// <summary>
		/// The upper value of this range
		/// </summary>
        [ProtoMember(2)]
		public T UpperBound;

		/// <summary>
		/// Whether the lower value is inclusive
		/// </summary>
        [ProtoMember(3)]
		public bool LowerInclusive;

		/// <summary>
		/// Whether the upper value is inclusive
		/// </summary>
        [ProtoMember(4)]
		public bool UpperInclusive;
		#endregion

		#region Public Static Methods
		/// <summary>
		/// Creates an inclusive-inclusive range
		/// </summary>
		/// <param name="inclusive_min">The inclusive minimum</param>
		/// <param name="inclusive_max">The inclusive maximum</param>
		/// <returns>The range [min, max]</returns>
		public static NumberRange<T> RangeII(T inclusive_min, T inclusive_max)
        {
            return new NumberRange<T>(inclusive_min, inclusive_max, true, true);
		}

		/// <summary>
		/// Creates an exclusive-inclusive range
		/// </summary>
		/// <param name="exclusive_min">The exclusive minimum</param>
		/// <param name="inclusive_max">The inclusive maximum</param>
		/// <returns>The range (min, max]</returns>
		public static NumberRange<T> RangeEI(T exclusive_min, T inclusive_max)
		{
			return new NumberRange<T>(exclusive_min, inclusive_max, false, true);
		}

		/// <summary>
		/// Creates an inclusive-exclusive range
		/// </summary>
		/// <param name="inclusive_min">The inclusive minimum</param>
		/// <param name="exclusive_max">The exclusive maximum</param>
		/// <returns>The range [min, max)</returns>
		public static NumberRange<T> RangeIE(T inclusive_min, T exclusive_max)
		{
			return new NumberRange<T>(inclusive_min, exclusive_max, true, false);
		}

		/// <summary>
		/// Creates an exclusive-exclusive range
		/// </summary>
		/// <param name="exclusive_min">The exclusive minimum</param>
		/// <param name="exclusive_max">The exclusive maximum</param>
		/// <returns>The range (min, max)</returns>
		public static NumberRange<T> RangeEE(T exclusive_min, T exclusive_max)
		{
			return new NumberRange<T>(exclusive_min, exclusive_max, false, false);
		}
		#endregion

		#region Constructors
		/// <summary>
		/// Creates a new number ange
		/// </summary>
		/// <param name="min">The minimum value</param>
		/// <param name="max">The maximum value</param>
		/// <param name="lower_inclusive">Whether the minimum is inclusive</param>
		/// <param name="upper_inclusive">Whether the maximum is inclusive</param>
		private NumberRange(T min, T max, bool lower_inclusive, bool upper_inclusive)
        {
            this.LowerBound = min;
            this.UpperBound = max;
            this.LowerInclusive = lower_inclusive;
            this.UpperInclusive = upper_inclusive;
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Checks if the specified value is within this range
		/// </summary>
		/// <param name="value">The value to check</param>
		/// <returns>true if value in range</returns>
		public bool Match(T value)
        {
            int lmatch = value.CompareTo(this.LowerBound);
			int umatch = value.CompareTo(this.UpperBound);
			return (this.LowerInclusive && lmatch >= 0 || lmatch > 0) && (this.UpperInclusive && umatch <= 0 || umatch < 0);
        }
		#endregion
	}

	internal class IntersectionInfo
	{
		public List<MyJumpGateDrive> IntersectingDrives = new List<MyJumpGateDrive>();
		public List<Vector3D> IntersectNodes = new List<Vector3D>();
	}

	[ProtoContract(UseProtoMembersOnly = true)]
	public class MyBeaconLinkWrapper : IEquatable<MyBeaconLinkWrapper>
	{
		#region Private Variables
		[ProtoMember(1, IsRequired = true, Name = "F_BeaconPosition")]
		private Vector3D _BeaconPosition;
		[ProtoMember(2, IsRequired = true, Name = "F_BroadcastName")]
		private string _BroadcastName;
		#endregion

		#region Public Variables
		[ProtoMember(3, IsRequired = true)]
		public long BeaconID { get; private set; }

		[ProtoIgnore]
		public IMyBeacon Beacon
		{
			get
			{
				return (IMyBeacon) MyAPIGateway.Entities.GetEntityById(this.BeaconID);
			}
		}

		[ProtoIgnore]
		public Vector3D BeaconPosition
		{
			get
			{
				if (this.Beacon != null) this._BeaconPosition = this.Beacon.WorldMatrix.Translation;
				return this._BeaconPosition;
			}
		}

		[ProtoIgnore]
		public string BroadcastName
		{
			get
			{
				if (this.Beacon != null) this._BroadcastName = (this.Beacon.HudText == null || this.Beacon.HudText.Length == 0) ? this.Beacon.CustomName : this.Beacon.HudText;
				return this._BroadcastName;
			}
		}
		#endregion

		#region Public Static Operators
		/// <summary>
		/// Overloads equality operator "==" to check equality
		/// </summary>
		/// <param name="a">The first BeaconLinkWrapper operand</param>
		/// <param name="b">The second BeaconLinkWrapper operand</param>
		/// <returns>Equality</returns>
		public static bool operator ==(MyBeaconLinkWrapper a, MyBeaconLinkWrapper b)
		{
			if (object.ReferenceEquals(a, b)) return true;
			else if (object.ReferenceEquals(a, null)) return object.ReferenceEquals(b, null);
			else if (object.ReferenceEquals(b, null)) return object.ReferenceEquals(a, null);
			else return a.Equals(b);
		}

		/// <summary>
		/// Overloads inequality operator "!=" to check inequality
		/// </summary>
		/// <param name="a">The first BeaconLinkWrapper operand</param>
		/// <param name="b">The second BeaconLinkWrapper operand</param>
		/// <returns>Inequality</returns>
		public static bool operator !=(MyBeaconLinkWrapper a, MyBeaconLinkWrapper b)
		{
			return !(a == b);
		}
		#endregion

		#region Constructors
		internal MyBeaconLinkWrapper() { }

		public MyBeaconLinkWrapper(IMyBeacon beacon)
		{
			this._BeaconPosition = beacon.WorldMatrix.Translation;
			this._BroadcastName = (beacon.HudText == null || beacon.HudText.Length == 0) ? beacon.CustomName : beacon.HudText;
			this.BeaconID = beacon.EntityId;
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Checks if this BeaconLinkWrapper equals another
		/// </summary>
		/// <param name="obj">The object to check</param>
		/// <returns>Equality</returns>
		public override bool Equals(object obj)
		{
			return this.Equals(obj as MyBeaconLinkWrapper);
		}

		/// <summary>
		/// The hashcode for this object
		/// </summary>
		/// <returns>The hashcode of this object</returns>
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		/// <summary>
		/// Checks if this BeaconLinkWrapper equals another
		/// </summary>
		/// <param name="other">The BeaconLinkWrapper to check</param>
		/// <returns>Equality</returns>
		public bool Equals(MyBeaconLinkWrapper other)
		{
			if (object.ReferenceEquals(other, null)) return false;
			else if (object.ReferenceEquals(this, other)) return true;
			else return this.BeaconID == other.BeaconID;
		}
		#endregion
	}

	public struct MyPrefabInfo
	{
		public readonly string PrefabName;
		public readonly Vector3D Position;
		public readonly Vector3D Forward;
		public readonly Vector3D Up;
		public readonly Vector3D InitialLinearVelocity;
		public readonly Vector3D InitialAngularVelocity;
		public readonly string BeaconName;
		public readonly SpawningOptions SpawningOptions;
		public readonly bool UpdateSync;
		public readonly Action Callback;

		public MyPrefabInfo(string prefab_name, Vector3D position, Vector3D forward, Vector3D up, Vector3D initial_linear_velocity = default(Vector3D), Vector3D initial_angular_velocity = default(Vector3D), string beacon_name = null, SpawningOptions spawning_options = SpawningOptions.None, bool update_sync = false, Action callback = null)
		{
			this.PrefabName = prefab_name;
			this.Position = position;
			this.Forward = forward;
			this.Up = up;
			this.InitialLinearVelocity = initial_linear_velocity;
			this.InitialAngularVelocity = initial_angular_velocity;
			this.BeaconName = beacon_name;
			this.SpawningOptions = spawning_options;
			this.UpdateSync = update_sync;
			this.Callback = callback;
		}

		public void Spawn(List<IMyCubeGrid> spawned_grids)
		{
			MyAPIGateway.PrefabManager.SpawnPrefab(spawned_grids, this.PrefabName, this.Position, this.Forward, this.Up, this.InitialLinearVelocity, this.InitialAngularVelocity, this.BeaconName, this.SpawningOptions, this.UpdateSync, this.Callback);
		}
	}
}
