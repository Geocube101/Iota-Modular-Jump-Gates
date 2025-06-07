using IOTA.ModularJumpGates.CubeBlock;
using ProtoBuf;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
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

	/// <summary>
	/// Class responsible for warping a batch of entities across space
	/// </summary>
	internal sealed class EntityWarpInfo
	{
		[ProtoContract]
		private sealed class SerializedEntityWarpInfo
		{
			[ProtoMember(1)]
			public Vector3D StartPos;
			[ProtoMember(2)]
			public Vector3D EndPos;
			[ProtoMember(3)]
			public Vector3D[] FinalPos;
			[ProtoMember(4)]
			public List<long> EntityBatch;
			[ProtoMember(5)]
			public List<string> RelativeEntityOffsets;
			[ProtoMember(6)]
			public List<Vector3> EntityStartingAngularVelocities;
			[ProtoMember(7)]
			public JumpGateUUID JumpGate;
			[ProtoMember(8)]
			public ushort Duration;
			[ProtoMember(9)]
			public ushort CurrentTick;
			[ProtoMember(10)]
			public double MaxSafeSpeed;
		}

		private static readonly ushort GlobalMaxSpeedCooldownTime = 30;
		private static readonly ushort GlobalTickSkip = 2;

		private bool IsComplete = false;
		private ushort MaxSpeedCooldownTime = 0;
		private Vector3D StartPos;
		private Vector3D EndPos;
		private MatrixD FinalPos;
		private readonly List<MyEntity> EntityBatch;
		private readonly List<MatrixD> RelativeEntityOffsets;
		private readonly List<Vector3> EntityStartingAngularVelocities;
		private readonly Action<List<MyEntity>> Callback;

		public readonly MyJumpGate JumpGate;
		public readonly ushort Duration;
		public readonly double MaxSafeSpeed;
		public ushort CurrentTick { get; private set; }

		public MyEntity Parent
		{
			get
			{
				return this.EntityBatch[0];
			}
		}

		public static EntityWarpInfo FromSerialized(string serialized_warp_info, Action<List<MyEntity>> callback)
		{
			SerializedEntityWarpInfo serialized = MyAPIGateway.Utilities.SerializeFromBinary<SerializedEntityWarpInfo>(Convert.FromBase64String(serialized_warp_info));
			if (serialized.EntityBatch == null || serialized.RelativeEntityOffsets == null) return null;
			MatrixD final_pos = MatrixD.CreateWorld(serialized.FinalPos[0], serialized.FinalPos[1], serialized.FinalPos[2]);
			List<MyEntity> batch_entities = serialized.EntityBatch.Select((id) => (MyEntity) MyAPIGateway.Entities.GetEntityById(id))?.Where((entity) => entity != null)?.ToList() ?? new List<MyEntity>();
			List<MatrixD> relative_entity_offsets = serialized.RelativeEntityOffsets.Select((matrix) => {
				double[] doubles = MyAPIGateway.Utilities.SerializeFromBinary<double[]>(Convert.FromBase64String(matrix));
				return new MatrixD(
					doubles[0], doubles[1], doubles[2], doubles[3],
					doubles[4], doubles[5], doubles[6], doubles[7],
					doubles[8], doubles[9], doubles[10], doubles[11],
					doubles[12], doubles[13], doubles[14], doubles[15]
				);
			}).ToList();
			MyJumpGate jump_gate = MyJumpGateModSession.Instance.GetJumpGate(serialized.JumpGate);
			if (jump_gate == null || batch_entities.Count == 0) return null;
			return new EntityWarpInfo(ref serialized.StartPos, ref serialized.EndPos, ref final_pos, batch_entities, relative_entity_offsets, serialized.EntityStartingAngularVelocities, jump_gate, serialized.Duration, serialized.CurrentTick, serialized.MaxSafeSpeed, callback);
		}

		private EntityWarpInfo(ref Vector3D startpos, ref Vector3D endpos, ref MatrixD final_pos, List<MyEntity> batch_entities, List<MatrixD> relative_offsets, List<Vector3> starting_angular_velocities, MyJumpGate jump_gate, ushort duration, ushort current_tick, double max_safe_speed, Action<List<MyEntity>> callback)
		{
			MyEntity parent = batch_entities[0];
			this.JumpGate = jump_gate;
			this.StartPos = startpos;
			this.EndPos = endpos;
			this.FinalPos = final_pos;
			this.EntityBatch = batch_entities;
			this.RelativeEntityOffsets = relative_offsets;
			this.EntityStartingAngularVelocities = starting_angular_velocities;
			this.Duration = duration;
			this.CurrentTick = current_tick;
			this.MaxSafeSpeed = max_safe_speed;
			this.Callback = callback;

			for (int i = 1; i < batch_entities.Count; ++i)
			{
				MyEntity entity = batch_entities[i];
				if (entity == null || entity.MarkedForClose || entity.Physics == null) continue;
				MyJumpGateConstruct construct = (entity is MyCubeGrid) ? MyJumpGateModSession.Instance.GetJumpGateGrid(entity.EntityId) : null;
				if (construct != null) construct.BatchingGate = jump_gate;
			}
		}

		public EntityWarpInfo(MyJumpGate jump_gate, ref MatrixD current_position, ref Vector3D target_position, ref MatrixD final_position, List<MyEntity> entity_batch, ushort time, double max_safe_speed, Action<List<MyEntity>> callback)
		{
			MyEntity parent = entity_batch[0];
			Vector3D angular = parent.Physics?.AngularVelocity ?? Vector3D.Zero;
			this.JumpGate = jump_gate;
			this.Duration = time;
			this.CurrentTick = 0;
			this.EntityBatch = new List<MyEntity>(entity_batch);
			this.StartPos = current_position.Translation;
			this.EndPos = target_position;
			this.FinalPos = final_position;
			this.RelativeEntityOffsets = new List<MatrixD>();
			this.EntityStartingAngularVelocities = new List<Vector3>() { angular };
			this.MaxSafeSpeed = max_safe_speed;
			this.Callback = callback;
			MyJumpGateConstruct construct;
			if (parent is MyCubeGrid && (construct = MyJumpGateModSession.Instance.GetUnclosedJumpGateGrid(parent.EntityId)) != null) construct.BatchingGate = jump_gate;
			MatrixD parent_inv = MatrixD.Invert(parent.WorldMatrix);

			foreach (MyEntity entity in entity_batch.Skip(1))
			{
				this.RelativeEntityOffsets.Add(entity.WorldMatrix * parent_inv);
				this.EntityStartingAngularVelocities.Add(entity.Physics?.AngularVelocity ?? angular);
				construct = (entity is MyCubeGrid) ? MyJumpGateModSession.Instance.GetJumpGateGrid(entity.EntityId) : null;
				if (construct != null) construct.BatchingGate = jump_gate;
			}
		}

		public void Close()
		{
			if (!this.IsComplete)
			{
				this.IsComplete = true;
				this.MaxSpeedCooldownTime = EntityWarpInfo.GlobalMaxSpeedCooldownTime;
				MyEntity parent = this.EntityBatch[0];

				if (!parent.MarkedForClose && parent is IMyPlayer)
				{
					MatrixD parent_matrix = parent.WorldMatrix;
					parent_matrix.Translation = this.FinalPos.Translation;
					parent.Teleport(parent_matrix);
				}
				else if (!parent.MarkedForClose) parent.Teleport(this.FinalPos);

				MyJumpGateConstruct construct;
				if (parent is MyCubeGrid && (construct = MyJumpGateModSession.Instance.GetUnclosedJumpGateGrid(parent.EntityId)) != null) construct.BatchingGate = null;

				for (int i = 0; i < this.EntityBatch.Count; ++i)
				{
					MyEntity entity = this.EntityBatch[i];
					if (entity.MarkedForClose || entity.Physics == null) continue;
					construct = (entity is MyCubeGrid) ? MyJumpGateModSession.Instance.GetJumpGateGrid(entity.EntityId) : null;
					if (construct != null) construct.BatchingGate = null;
				}
			}

			this.EntityBatch.Clear();
			this.RelativeEntityOffsets.Clear();
			this.EntityStartingAngularVelocities.Clear();
		}

		public bool Update()
		{
			if (this.CurrentTick++ % EntityWarpInfo.GlobalTickSkip != 0 && !this.IsComplete) return false;
			bool complete = this.CurrentTick >= this.Duration;
			MyEntity parent = this.EntityBatch[0];

			if (parent.Closed)
			{
				Logger.Debug($"Batch parent @ {parent.EntityId} closed");
				return true;
			}
			else if (this.IsComplete)
			{
				for (int i = 0; i < this.EntityBatch.Count; ++i)
				{
					MyEntity entity = this.EntityBatch[i];
					if (entity.MarkedForClose || entity.Physics == null) continue;
					Vector3D velocity = new Vector3D(entity.Physics.LinearVelocity);
					entity.Physics.LinearVelocity = (velocity.LengthSquared() <= this.MaxSafeSpeed * this.MaxSafeSpeed) ? velocity : (velocity.Normalized() * this.MaxSafeSpeed);
					entity.Physics.AngularVelocity = this.EntityStartingAngularVelocities[i];
				}

				return ++this.MaxSpeedCooldownTime >= EntityWarpInfo.GlobalMaxSpeedCooldownTime; ;
			}
			else if (complete && parent is IMyPlayer)
			{
				MatrixD parent_matrix = parent.WorldMatrix;
				parent_matrix.Translation = this.FinalPos.Translation;
				parent.Teleport(parent_matrix);
			}
			else if (complete) parent.Teleport(this.FinalPos);
			else
			{
				double tick_ratio = MathHelper.Clamp((double) this.CurrentTick / this.Duration, 0, 1);
				Vector3D current_pos;
				Vector3D.Lerp(ref this.StartPos, ref this.EndPos, tick_ratio, out current_pos);
				MatrixD parent_matrix = parent.WorldMatrix;
				parent_matrix.Translation = current_pos;
				parent.Teleport(parent_matrix);
			}

			for (int i = 0; i < this.EntityBatch.Count - 1; ++i)
			{
				MyEntity entity = this.EntityBatch[i + 1];
				if (entity.MarkedForClose) continue;
				MatrixD relative_offsets = this.RelativeEntityOffsets[i];
				MatrixD result = relative_offsets * parent.WorldMatrix;
				if (!complete && entity is IMyPlayer) result.SetRotationAndScale(entity.WorldMatrix);
				entity.Teleport(result);
			}

			if (complete)
			{
				MyJumpGateConstruct construct;
				if (parent is MyCubeGrid && (construct = MyJumpGateModSession.Instance.GetUnclosedJumpGateGrid(parent.EntityId)) != null) construct.BatchingGate = null;
				
				for (int i = 1; i < this.EntityBatch.Count; ++i)
				{
					MyEntity entity = this.EntityBatch[i];
					if (entity.MarkedForClose || entity.Physics == null) continue;
					construct = (entity is MyCubeGrid) ? MyJumpGateModSession.Instance.GetJumpGateGrid(entity.EntityId) : null;
					if (construct != null) construct.BatchingGate = null;
				}

				for (int i = 0; i < this.EntityBatch.Count; ++i)
				{
					MyEntity entity = this.EntityBatch[i];
					if (!(entity is MyCubeGrid) || (construct = MyJumpGateModSession.Instance.GetUnclosedJumpGateGrid(entity.EntityId)) == null) continue;
					foreach (MyDoorBase door in construct.GetAllFatBlocksOfType<MyDoorBase>()) door.UpdateOnceBeforeFrame();
					//foreach (MyCubeBlock block in construct.GetAllFatBlocksOfType<MyCubeBlock>()) block.UpdateOnceBeforeFrame();
				}

				if (this.Callback != null) this.Callback(this.EntityBatch);
				this.IsComplete = true;
			}

			return false;
		}

		public string ToSerialized()
		{
			return Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(new SerializedEntityWarpInfo {
				StartPos = this.StartPos,
				EndPos = this.EndPos,
				FinalPos = new Vector3D[3] { this.FinalPos.Translation, this.FinalPos.Forward, this.FinalPos.Up },
				EntityBatch = this.EntityBatch.Select((entity) => entity.EntityId).ToList(),
				RelativeEntityOffsets = this.RelativeEntityOffsets.Select((offsets) => Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(new double[16] {
					offsets.M11, offsets.M12, offsets.M13, offsets.M14,
					offsets.M21, offsets.M22, offsets.M23, offsets.M24,
					offsets.M31, offsets.M32, offsets.M33, offsets.M34,
					offsets.M41, offsets.M42, offsets.M43, offsets.M44
				}))).ToList(),
				EntityStartingAngularVelocities = this.EntityStartingAngularVelocities,
				JumpGate = JumpGateUUID.FromJumpGate(this.JumpGate),
				Duration = this.Duration,
				CurrentTick = this.CurrentTick,
				MaxSafeSpeed = this.MaxSafeSpeed,
			}));
		}
	}

	/// <summary>
	/// Class holding information on an entity batch
	/// </summary>
	internal class EntityBatch
	{
		[ProtoContract]
		private sealed class SerializedEntityBatch
		{
			[ProtoMember(1)]
			public List<long> Batch;
			[ProtoMember(2)]
			public Vector3D ParentTargetPosition;
			[ProtoMember(3)]
			public Vector3D[] ObstructAABB;
			[ProtoMember(4)]
			public Vector3D[] ParentFinalMatrix;
		}

		public readonly List<MyEntity> Batch;
		public Vector3D ParentTargetPosition;
		public BoundingBoxD ObstructAABB;
		public MatrixD ParentFinalMatrix;

		public MyEntity Parent => this.Batch?.FirstOrDefault();

		public double BatchMass
		{
			get
			{
				if (this.Batch == null) return 0;
				double mass = 0;

				foreach (MyEntity child in this.Batch)
				{
					MyJumpGateConstruct parent = (child is MyCubeGrid) ? MyJumpGateModSession.Instance.GetJumpGateGrid(child.EntityId) : null;
					mass += (parent != null) ? parent.ConstructMass() : ((child is IMyPlayer) ? ((IMyPlayer) child).Character.CurrentMass : (child.Physics?.Mass ?? 0));
				}

				return mass;
			}
		}

		public EntityBatch(string serialized_batch)
		{
			SerializedEntityBatch serialized = MyAPIGateway.Utilities.SerializeFromBinary<SerializedEntityBatch>(Convert.FromBase64String(serialized_batch));
			this.Batch = serialized.Batch?.Select((id) => (MyEntity) MyAPIGateway.Entities.GetEntityById(id))?.Where((entity) => entity != null)?.ToList() ?? new List<MyEntity>();
			this.ParentTargetPosition = serialized.ParentTargetPosition;
			this.ObstructAABB = new BoundingBoxD(serialized.ObstructAABB[0], serialized.ObstructAABB[1]);
			this.ParentFinalMatrix = MatrixD.CreateWorld(serialized.ParentFinalMatrix[0], serialized.ParentFinalMatrix[1], serialized.ParentFinalMatrix[2]);
		}

		public EntityBatch(List<MyEntity> batch, ref Vector3D target_position, ref BoundingBoxD obstruct_aabb, ref MatrixD final_matrix)
		{
			this.Batch = batch;
			this.ParentTargetPosition = target_position;
			this.ObstructAABB = obstruct_aabb;
			this.ParentFinalMatrix = final_matrix;
		}

		public bool IsEntityInBatch(MyEntity entity)
		{
			MyEntity topmost = entity.GetTopMostParent();
			return this.Batch != null && (this.Batch.Contains(topmost) || this.Batch.Any((parent) => topmost == parent));
		}

		public string ToSerialized()
		{
			return Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(new SerializedEntityBatch {
				Batch = this.Batch?.Select((entity) => entity.EntityId)?.ToList(),
				ParentTargetPosition = this.ParentTargetPosition,
				ObstructAABB = new Vector3D[2] { this.ObstructAABB.Min, this.ObstructAABB.Max },
				ParentFinalMatrix = new Vector3D[3] { this.ParentFinalMatrix.Translation, this.ParentFinalMatrix.Forward, this.ParentFinalMatrix.Up },
			}));
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

	/// <summary>
	/// Class wrapping a beacon for use on server and clients
	/// </summary>
	[ProtoContract(UseProtoMembersOnly = true)]
	public class MyBeaconLinkWrapper : IEquatable<MyBeaconLinkWrapper>
	{
		#region Private Variables
		[ProtoMember(1, IsRequired = true)]
		private Vector3D _BeaconPosition;
		[ProtoMember(2, IsRequired = true)]
		private string _BroadcastName;
		[ProtoMember(3, IsRequired = true)]
		private string _CubeGridName;
		#endregion

		#region Public Variables
		[ProtoMember(4, IsRequired = true)]
		public long BeaconID { get; private set; }

		[ProtoIgnore]
		public IMyBeacon Beacon => (IMyBeacon) MyAPIGateway.Entities.GetEntityById(this.BeaconID);

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

		[ProtoIgnore]
		public string CubeGridCustomName
		{
			get
			{
				if (this.Beacon != null) this._CubeGridName = this.Beacon.CubeGrid?.CustomName;
				return this._CubeGridName;
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
			this._CubeGridName = beacon.CubeGrid?.CustomName;
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

	/// <summary>
	/// Class holding information on prefabs and their spawning info
	/// </summary>
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

		public MyPrefabInfo(Dictionary<string, object> info)
		{
			this.PrefabName = (string) info["PrefabName"];
			this.Position = (Vector3D) info["Position"];
			this.Forward = (Vector3D) info["Forward"];
			this.Up = (Vector3D) info["Up"];
			this.InitialLinearVelocity = (Vector3D) info["InitialLinearVelocity"];
			this.InitialAngularVelocity = (Vector3D) info["InitialAngularVelocity"];
			this.BeaconName = (string) info["BeaconName"];
			this.SpawningOptions = (SpawningOptions) info["SpawningOptions"];
			this.UpdateSync = (bool) info["UpdateSync"];
			this.Callback = (Action) info["Callback"];
		}
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

		public Dictionary<string, object> ToDictionary()
		{
			return new Dictionary<string, object>
			{
				["PrefabName"] = this.PrefabName,
				["Position"] = this.Position,
				["Forward"] = this.Forward,
				["Up"] = this.Up,
				["InitialLinearVelocity"] = this.InitialLinearVelocity,
				["InitialAngularVelocity"] = this.InitialAngularVelocity,
				["BeaconName"] = this.BeaconName,
				["SpawningOptions"] = this.SpawningOptions,
				["UpdateSync"] = this.UpdateSync,
				["Callback"] = this.Callback,
			};
		}
	}
}
