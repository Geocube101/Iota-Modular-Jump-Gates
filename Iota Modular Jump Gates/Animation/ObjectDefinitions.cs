using IOTA.ModularJumpGates.JumpGates;
using IOTA.ModularJumpGates.Util;
using ProtoBuf;
using System;
using System.Linq;
using System.Xml.Serialization;
using VRageMath;

namespace IOTA.ModularJumpGates.Animation
{
	/// <summary>
	/// Definition defining a particle orientation
	/// </summary>
	[Serializable]
	[XmlRoot("ParticleOrientationDef")]
	[ProtoContract(UseProtoMembersOnly = true)]
	public sealed class ParticleOrientationDef
	{
		#region Public Variables
		/// <summary>
		/// The particle's orientation
		/// </summary>
		[ProtoMember(1)]
		[XmlElement("ParticleOrientation")]
		public ParticleOrientationEnum ParticleOrientation = ParticleOrientationEnum.GATE_ENDPOINT_NORMAL;

		[ProtoMember(2)]
		[XmlArray("WorldMatrix")]
		[XmlArrayItem("Cell")]
		public double[] WorldMatrixBase = new double[16] { -1, 0, 0, 0, 0, 1, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0 };

		/// <summary>
		/// If fixed, the particle's world matrix otherwise, the particle's rotation offset
		/// </summary>
		[XmlIgnore]
		public MatrixD WorldMatrix
		{
			get
			{
				return new MatrixD(
					this.WorldMatrixBase[0], this.WorldMatrixBase[1], this.WorldMatrixBase[2], this.WorldMatrixBase[3],
					this.WorldMatrixBase[4], this.WorldMatrixBase[5], this.WorldMatrixBase[6], this.WorldMatrixBase[7],
					this.WorldMatrixBase[8], this.WorldMatrixBase[9], this.WorldMatrixBase[10], this.WorldMatrixBase[11],
					this.WorldMatrixBase[12], this.WorldMatrixBase[13], this.WorldMatrixBase[14], this.WorldMatrixBase[15]
				);
			}

			set
			{
				this.WorldMatrixBase[0] = value.M11;
				this.WorldMatrixBase[1] = value.M12;
				this.WorldMatrixBase[2] = value.M13;
				this.WorldMatrixBase[3] = value.M14;
				this.WorldMatrixBase[4] = value.M21;
				this.WorldMatrixBase[5] = value.M22;
				this.WorldMatrixBase[6] = value.M23;
				this.WorldMatrixBase[7] = value.M24;
				this.WorldMatrixBase[8] = value.M31;
				this.WorldMatrixBase[9] = value.M32;
				this.WorldMatrixBase[10] = value.M33;
				this.WorldMatrixBase[11] = value.M34;
				this.WorldMatrixBase[12] = value.M41;
				this.WorldMatrixBase[13] = value.M42;
				this.WorldMatrixBase[14] = value.M43;
				this.WorldMatrixBase[15] = value.M44;
			}
		}
		#endregion

		#region Internal Static Methods
		/// <summary>
		/// Gets the resulting particle matrix from a particle orientation definition
		/// </summary>
		/// <param name="gate">The calling jump gate</param>
		/// <param name="target_gate">The targeted jump gate or null</param>
		/// <param name="is_anti_node">Whether to calculate for the endpoint anti-node instead of this gate's jump node</param>
		/// <param name="endpoint">The gate's targeted endpoint</param>
		/// <param name="particle_orientation">The particle orientation definition</param>
		/// <returns>The oriented particle matrix</returns>
		/// <exception cref="ArgumentNullException">If the jump gate is null</exception>
		internal static MatrixD GetJumpGateMatrix(MyJumpGate gate, MyJumpGate target_gate, bool is_anti_node, ref Vector3D endpoint, ParticleOrientationDef particle_orientation)
		{
			if (gate == null) throw new ArgumentNullException("MyJumpGate is null");
			MatrixD matrix;
			bool null_orientation = particle_orientation == null || particle_orientation.ParticleOrientation == ParticleOrientationEnum.GATE_ENDPOINT_NORMAL;
			bool fixed_ = false;

			if (null_orientation && is_anti_node && target_gate != null) target_gate.GetWorldMatrix(out matrix, true, true);
			else if (null_orientation) gate.GetWorldMatrix(out matrix, true, !is_anti_node || target_gate != null);
			else if (particle_orientation.ParticleOrientation == ParticleOrientationEnum.GATE_TRUE_ENDPOINT_NORMAL) gate.GetWorldMatrix(out matrix, true, false);
			else if (particle_orientation.ParticleOrientation == ParticleOrientationEnum.GATE_DRIVE_NORMAL) gate.GetWorldMatrix(out matrix, false, false);
			else if (particle_orientation.ParticleOrientation == ParticleOrientationEnum.ANTIGATE_DRIVE_NORMAL) matrix = (target_gate?.GetWorldMatrix(true, true) ?? gate.GetWorldMatrix(true, false));
			else
			{
				matrix = particle_orientation.WorldMatrix;
				matrix.Translation = gate.WorldJumpNode;
				fixed_ = true;
			}

			if (!gate.IsWormholeActive && is_anti_node)
			{
				matrix.Forward = -matrix.Forward;
				matrix.Up = -matrix.Up;
				matrix.Translation = endpoint;
			}

			return (fixed_) ? matrix : ((particle_orientation == null) ? matrix : particle_orientation.WorldMatrix * matrix);
		}
		#endregion

		#region Constructors
		/// <summary>
		/// Dummy default constructor for ProtoBuf
		/// </summary>
		public ParticleOrientationDef() { }

		/// <summary>
		/// Creates a new ParticleOrientationDef
		/// </summary>
		/// <param name="particle_orientation">The particle orientation type</param>
		public ParticleOrientationDef(ParticleOrientationEnum particle_orientation)
		{
			this.ParticleOrientation = particle_orientation;
			this.WorldMatrix = (particle_orientation == ParticleOrientationEnum.FIXED) ? MatrixD.Identity : MatrixD.CreateFromYawPitchRoll(0, 0, 0);
		}

		/// <summary>
		/// Creates a new ParticleOrientationDef
		/// </summary>
		/// <param name="particle_orientation">The particle orientation type</param>
		/// <param name="world_matrix">The fixed world matrix if fixed, otherwise the rotation matrix</param>
		public ParticleOrientationDef(ParticleOrientationEnum particle_orientation, MatrixD world_matrix)
		{
			this.ParticleOrientation = particle_orientation;
			this.WorldMatrix = world_matrix;
		}
		#endregion
	}

	/// <summary>
	/// Definition defining an animation predicate<br />
	/// This animation will only show on controllers who's gate matches all contraints
	/// </summary>
	[Serializable]
	[XmlRoot("AnimationConstraintDef")]
	[ProtoContract(UseProtoMembersOnly = true)]
	public sealed class AnimationConstraintDef
	{
		#region Public Variables
		/// <summary>
		/// The allowed range for a gate's jump space lateral radius
		/// </summary>
		[ProtoMember(1)]
		[XmlElement("AllowedJumpGateRadius")]
		public NumberRange<double> AllowedJumpGateRadius = NumberRange<double>.RangeII(0, double.PositiveInfinity);

		/// <summary>
		/// The allowed range for a gate's drive count
		/// </summary>
		[ProtoMember(2)]
		[XmlElement("AllowedJumpGateSize")]
		public NumberRange<uint> AllowedJumpGateSize = NumberRange<uint>.RangeII(0, uint.MaxValue);

		/// <summary>
		/// The allowed range for a gate's working drive count
		/// </summary>
		[ProtoMember(3)]
		[XmlElement("AllowedJumpGateWorkingSize")]
		public NumberRange<uint> AllowedJumpGateWorkingSize = NumberRange<uint>.RangeII(0, uint.MaxValue);

		/// <summary>
		/// The allowed range for a gate's jump node endpoint distance
		/// </summary>
		[ProtoMember(4)]
		[XmlElement("AllowedJumpGateEndpointDistance")]
		public NumberRange<double> AllowedJumpGateEndpointDistance = NumberRange<double>.RangeII(-1, double.PositiveInfinity);

		/// <summary>
		/// The allowed date range
		/// </summary>
		[ProtoMember(5)]
		[XmlElement("AllowedDate")]
		public DateTimeRange AllowedDate = DateTimeRange.RangeII(DateTime.MinValue, DateTime.MaxValue);
		#endregion

		#region Internal Methods
		/// <summary>
		/// Checks a jump gate againts this constraint
		/// </summary>
		/// <param name="jump_gate">The jump gate to check</param>
		/// <returns>True if this jump gate passes this constraint</returns>
		internal bool Validate(MyJumpGate jump_gate)
		{
			if (!jump_gate?.IsValid() ?? true) return false;
			Vector3D? endpoint = jump_gate.Controller?.BlockSettings?.SelectedWaypoint()?.GetEndpoint();
			double distance = (endpoint == null) ? -1 : Vector3D.Distance(endpoint.Value, jump_gate.WorldJumpNode);
			return this.AllowedJumpGateRadius.Match(jump_gate.JumpNodeRadius())
				&& this.AllowedJumpGateSize.Match((uint) jump_gate.GetJumpGateDrives().Count())
				&& this.AllowedJumpGateWorkingSize.Match((uint) jump_gate.GetWorkingJumpGateDrives().Count())
				&& this.AllowedJumpGateEndpointDistance.Match(distance)
				&& this.AllowedDate.Match(DateTime.Now);
		}
		#endregion
	}

	/// <summary>
	/// Definition defining an atribute being animated
	/// </summary>
	[Serializable]
	[XmlRoot("AttributeAnimationDef")]
	[ProtoContract(UseProtoMembersOnly = true)]
	public sealed class AttributeAnimationDef
	{
		internal static double GetAnimatedDoubleValue(DoubleKeyframe[] keyframes, AnimationExpression.ExpressionArguments arguments, double default_ = default(double))
		{
			if (keyframes == null || keyframes.Length == 0) return default_;
			else if (keyframes.Length == 1) return keyframes[0].GetValueFromStack(arguments);
			DoubleKeyframe last_keyframe = null;
			DoubleKeyframe next_keyframe = null;

			foreach (DoubleKeyframe keyframe in keyframes.OrderBy((frame) => frame.Position))
			{
				if (keyframe.Position == arguments.CurrentTick) return keyframe.GetValueFromStack(arguments);
				else if (keyframe.Position > arguments.CurrentTick)
				{
					next_keyframe = keyframe;
					break;
				}

				last_keyframe = keyframe;
			}

			if (next_keyframe == null) return last_keyframe.GetValueFromStack(arguments);
			else if (last_keyframe == null) return next_keyframe.GetValueFromStack(arguments);

			double curr = arguments.CurrentTick;
			double last = last_keyframe.Position;
			double next = next_keyframe.Position;
			double ratio = MathHelper.Clamp((curr - last) / (next - last), 0, 1);
			ratio = EasingFunctor.GetEaseResult(ratio, last_keyframe.EasingType, last_keyframe.EasingCurve);
			last = last_keyframe.GetValueFromStack(arguments);
			next = next_keyframe.GetValueFromStack(arguments);
			return (next - last) * ratio + last;
		}

		internal static Vector4D GetAnimatedVectorValue(VectorKeyframe[] keyframes, AnimationExpression.ExpressionArguments arguments, Vector4D default_ = default(Vector4D))
		{
			if (keyframes == null || keyframes.Length == 0) return default_;
			else if (keyframes.Length == 1) return keyframes[0].GetValueFromStack(arguments);
			VectorKeyframe last_keyframe = null;
			VectorKeyframe next_keyframe = null;

			foreach (VectorKeyframe keyframe in keyframes.OrderBy((frame) => frame.Position))
			{
				if (keyframe.Position == arguments.CurrentTick) return keyframe.GetValueFromStack(arguments);
				else if (keyframe.Position > arguments.CurrentTick)
				{
					next_keyframe = keyframe;
					break;
				}

				last_keyframe = keyframe;
			}

			if (next_keyframe == null) return last_keyframe.GetValueFromStack(arguments);
			else if (last_keyframe == null) return next_keyframe.GetValueFromStack(arguments);

			double curr = arguments.CurrentTick;
			double last = last_keyframe.Position;
			double next = next_keyframe.Position;
			double ratio = MathHelper.Clamp((curr - last) / (next - last), 0, 1);
			ratio = EasingFunctor.GetEaseResult(ratio, last_keyframe.EasingType, last_keyframe.EasingCurve);
			Vector4D last_value = last_keyframe.GetValueFromStack(arguments);
			Vector4D next_value = next_keyframe.GetValueFromStack(arguments);
			return (next_value - last_value) * ratio + last_value;
		}

		/// <summary>
		/// Modifies or animates a sound's volume
		/// </summary>
		[ProtoMember(1)]
		[XmlArray("SoundVolumeAnimation")]
		[XmlArrayItem("DoubleKeyframe")]
		public DoubleKeyframe[] SoundVolumeAnimation = null;

		/// <summary>
		/// Modifies or animates a sound's distance
		/// </summary>
		[ProtoMember(2)]
		[XmlArray("SoundDistanceAnimation")]
		[XmlArrayItem("DoubleKeyframe")]
		public DoubleKeyframe[] SoundDistanceAnimation = null;

		/// <summary>
		/// Modifies or animates a particle's birth multiplier
		/// </summary>
		[ProtoMember(3)]
		[XmlArray("ParticleBirthAnimation")]
		[XmlArrayItem("DoubleKeyframe")]
		public DoubleKeyframe[] ParticleBirthAnimation = null;

		/// <summary>
		/// Modifies or animates a particle's color intensity multiplier
		/// </summary>
		[ProtoMember(4)]
		[XmlArray("ParticleColorIntensityAnimation")]
		[XmlArrayItem("DoubleKeyframe")]
		public DoubleKeyframe[] ParticleColorIntensityAnimation = null;

		/// <summary>
		/// Modifies or animates a particle's color multiplier
		/// </summary>
		[ProtoMember(5)]
		[XmlArray("ParticleColorAnimation")]
		[XmlArrayItem("VectorKeyframe")]
		public VectorKeyframe[] ParticleColorAnimation = null;

		/// <summary>
		/// Modifies or animates a particle's fade multiplier
		/// </summary>
		[ProtoMember(6)]
		[XmlArray("ParticleFadeAnimation")]
		[XmlArrayItem("DoubleKeyframe")]
		public DoubleKeyframe[] ParticleFadeAnimation = null;

		/// <summary>
		/// Modifies or animates a particle's life multiplier
		/// </summary>
		[ProtoMember(7)]
		[XmlArray("ParticleLifeAnimation")]
		[XmlArrayItem("DoubleKeyframe")]
		public DoubleKeyframe[] ParticleLifeAnimation = null;

		/// <summary>
		/// Modifies or animates a particle's radius multiplier
		/// </summary>
		[ProtoMember(8)]
		[XmlArray("ParticleRadiusAnimation")]
		[XmlArrayItem("DoubleKeyframe")]
		public DoubleKeyframe[] ParticleRadiusAnimation = null;

		/// <summary>
		/// Modifies or animates a particle's scale multiplier
		/// </summary>
		[ProtoMember(9)]
		[XmlArray("ParticleScaleAnimation")]
		[XmlArrayItem("DoubleKeyframe")]
		public DoubleKeyframe[] ParticleScaleAnimation = null;

		/// <summary>
		/// Modifies or animates a particle's velocity multiplier
		/// </summary>
		[ProtoMember(10)]
		[XmlArray("ParticleVelocityAnimation")]
		[XmlArrayItem("DoubleKeyframe")]
		public DoubleKeyframe[] ParticleVelocityAnimation = null;

		/// <summary>
		/// Modifies or animates a particle's rotation speed
		/// </summary>
		[ProtoMember(11)]
		[XmlArray("ParticleRotationSpeedAnimation")]
		[XmlArrayItem("VectorKeyframe")]
		public VectorKeyframe[] ParticleRotationSpeedAnimation = null;

		/// <summary>
		/// Modifies or animates a particle's offset in meters
		/// </summary>
		[ProtoMember(12)]
		[XmlArray("ParticleOffsetAnimation")]
		[XmlArrayItem("VectorKeyframe")]
		public VectorKeyframe[] ParticleOffsetAnimation = null;

		/// <summary>
		/// Modifies or animates a beam pulse's frequency<br />
		/// For a solid beam, set this to 0<br />
		/// For a gradient, this value must not be 0
		/// </summary>
		[ProtoMember(13)]
		[XmlArray("BeamFrequencyAnimation")]
		[XmlArrayItem("DoubleKeyframe")]
		public DoubleKeyframe[] BeamFrequencyAnimation = null;

		/// <summary>
		/// Modifies or animates a beam pulse's duty cycle<br />
		/// For a gradient beam with no breaks, set this to 1
		/// </summary>
		[ProtoMember(14)]
		[XmlArray("BeamDutyCycleAnimation")]
		[XmlArrayItem("DoubleKeyframe")]
		public DoubleKeyframe[] BeamDutyCycleAnimation = null;

		/// <summary>
		/// Modifies or animates a beam pulse's offset
		/// </summary>
		[ProtoMember(15)]
		[XmlArray("BeamOffsetAnimation")]
		[XmlArrayItem("DoubleKeyframe")]
		public DoubleKeyframe[] BeamOffsetAnimation = null;

		/// <summary>
		/// Modifies or animates the jump space attractor force<br />
		/// If not-zero, a force will be applied to all entities within the jump space during jump<br />
		/// Positive values attract entities to jump node<br />
		/// Negative values repel entities from jump node<br />
		/// </summary>
		[ProtoMember(16)]
		[XmlArray("PhysicsForceAnimation")]
		[XmlArrayItem("DoubleKeyframe")]
		public DoubleKeyframe[] PhysicsForceAnimation = null;

		/// <summary>
		/// Modifies or animates the jump space attractor force falloff
		/// </summary>
		[ProtoMember(17)]
		[XmlArray("PhysicsForceFalloffAnimation")]
		[XmlArrayItem("DoubleKeyframe")]
		public DoubleKeyframe[] PhysicsForceFalloffAnimation = null;

		/// <summary>
		/// Modifies or animates the jump space attractor force offset
		/// </summary>
		[ProtoMember(18)]
		[XmlArray("PhysicsForceOffsetAnimation")]
		[XmlArrayItem("VectorKeyframe")]
		public VectorKeyframe[] PhysicsForceOffsetAnimation = null;

		/// <summary>
		/// Modifies or animates the jump space attractor force max allowed speed
		/// </summary>
		[ProtoMember(19)]
		[XmlArray("PhysicsForceMaxSpeedAnimation")]
		[XmlArrayItem("DoubleKeyframe")]
		public DoubleKeyframe[] PhysicsForceMaxSpeedAnimation = null;

		/// <summary>
		/// Modifies or animates the jump space attractor force torque
		/// </summary>
		[ProtoMember(20)]
		[XmlArray("PhysicsForceTorqueAnimation")]
		[XmlArrayItem("VectorKeyframe")]
		public VectorKeyframe[] PhysicsForceTorqueAnimation = null;

		/// <summary>
		/// Modifies or animates a shape collider's inner cutout multiplier
		/// </summary>
		[ProtoMember(21)]
		[XmlArray("ShapeInnerCutoutAnimation")]
		[XmlArrayItem("VectorKeyframe")]
		public VectorKeyframe[] ShapeInnerCutoutAnimation = null;

		/// <summary>
		/// Modifies or animates a shape collider's locked rotation
		/// </summary>
		[ProtoMember(22)]
		[XmlArray("ShapeLockedRotationAnimation")]
		[XmlArrayItem("VectorKeyframe")]
		public VectorKeyframe[] ShapeLockedRotationAnimation = null;

		/// <summary>
		/// Modifies or animates a shape collider's free rotation
		/// </summary>
		[ProtoMember(23)]
		[XmlArray("ShapeFreeRotationAnimation")]
		[XmlArrayItem("VectorKeyframe")]
		public VectorKeyframe[] ShapeFreeRotationAnimation = null;

		/// <summary>
		/// Modifies or animates a shape collider's scale
		/// </summary>
		[ProtoMember(24)]
		[XmlArray("ShapeScaleAnimation")]
		[XmlArrayItem("VectorKeyframe")]
		public VectorKeyframe[] ShapeScaleAnimation = null;

		/// <summary>
		/// Modifies or animates a shape collider's shape arguments
		/// </summary>
		[ProtoMember(25)]
		[XmlArray("ShapeArgumentsAnimation")]
		[XmlArrayItem("VectorKeyframe")]
		public VectorKeyframe[] ShapeArgumentsAnimation = null;

		/// <summary>
		/// Overlays another animation definition atop this one<br />
		/// Any values missing in this animation will be replaced by the outside animation
		/// </summary>
		/// <param name="animation_def">The outside animation definition</param>
		/// <returns>This animation definition</returns>
		public AttributeAnimationDef Overlay(AttributeAnimationDef animation_def)
		{
			this.SoundVolumeAnimation = this.SoundVolumeAnimation ?? animation_def.SoundVolumeAnimation;
			this.SoundDistanceAnimation = this.SoundDistanceAnimation ?? animation_def.SoundDistanceAnimation;
			this.ParticleBirthAnimation = this.ParticleBirthAnimation ?? animation_def.ParticleBirthAnimation;
			this.ParticleColorIntensityAnimation = this.ParticleColorIntensityAnimation ?? animation_def.ParticleColorIntensityAnimation;
			this.ParticleColorAnimation = this.ParticleColorAnimation ?? animation_def.ParticleColorAnimation;
			this.ParticleFadeAnimation = this.ParticleFadeAnimation ?? animation_def.ParticleFadeAnimation;
			this.ParticleLifeAnimation = this.ParticleLifeAnimation ?? animation_def.ParticleLifeAnimation;
			this.ParticleRadiusAnimation = this.ParticleRadiusAnimation ?? animation_def.ParticleRadiusAnimation;
			this.ParticleScaleAnimation = this.ParticleScaleAnimation ?? animation_def.ParticleScaleAnimation;
			this.ParticleVelocityAnimation = this.ParticleVelocityAnimation ?? animation_def.ParticleVelocityAnimation;
			this.ParticleRotationSpeedAnimation = this.ParticleRotationSpeedAnimation ?? animation_def.ParticleRotationSpeedAnimation;
			this.ParticleOffsetAnimation = this.ParticleOffsetAnimation ?? animation_def.ParticleOffsetAnimation;
			this.BeamFrequencyAnimation = this.BeamFrequencyAnimation ?? animation_def.BeamFrequencyAnimation;
			this.BeamDutyCycleAnimation = this.BeamDutyCycleAnimation ?? animation_def.BeamDutyCycleAnimation;
			this.BeamOffsetAnimation = this.BeamOffsetAnimation ?? animation_def.BeamOffsetAnimation;
			this.PhysicsForceAnimation = this.PhysicsForceAnimation ?? animation_def.PhysicsForceAnimation;
			this.PhysicsForceFalloffAnimation = this.PhysicsForceFalloffAnimation ?? animation_def.PhysicsForceFalloffAnimation;
			this.PhysicsForceOffsetAnimation = this.PhysicsForceOffsetAnimation ?? animation_def.PhysicsForceOffsetAnimation;
			this.PhysicsForceMaxSpeedAnimation = this.PhysicsForceMaxSpeedAnimation ?? animation_def.PhysicsForceMaxSpeedAnimation;
			this.PhysicsForceTorqueAnimation = this.PhysicsForceTorqueAnimation ?? animation_def.PhysicsForceTorqueAnimation;
			return this;
		}
	}

	/// <summary>
	/// The base class for an animatable definition
	/// </summary>
	[Serializable]
	[XmlRoot("AnimatableDef")]
	[ProtoContract(UseProtoMembersOnly = true)]
	[ProtoInclude(100, typeof(ParticleDef))]
	[ProtoInclude(200, typeof(SoundDef))]
	[ProtoInclude(300, typeof(BeamPulseDef))]
	[ProtoInclude(400, typeof(DriveEmissiveColorDef))]
	[ProtoInclude(500, typeof(NodePhysicsDef))]
	[ProtoInclude(600, typeof(DriveEntityLockDef))]
	[ProtoInclude(700, typeof(ShapeColliderDef))]
	public class AnimatableDef
	{
		#region Public Variables
		/// <summary>
		/// The start time of this animation
		/// </summary>
		[ProtoMember(1)]
		[XmlElement("StartTime")]
		public ushort StartTime;

		/// <summary>
		/// The duraton of this animation in game ticks
		/// </summary>
		[ProtoMember(2)]
		[XmlElement("Duration")]
		public ushort Duration;

		/// <summary>
		/// The keyframe holder for animations
		/// </summary>
		[ProtoMember(3)]
		[XmlElement("Animations")]
		public AttributeAnimationDef Animations = null;
		#endregion
	}

	/// <summary>
	/// Definition defining particles
	/// </summary>
	[Serializable]
	[XmlRoot("ParticleDef")]
	[ProtoContract(UseProtoMembersOnly = true)]
	public sealed class ParticleDef : AnimatableDef
	{
		#region Public Variables
		/// <summary>
		/// Whether to clean this particle effect once it's completed<br />
		/// If false, effect is cleaned when entire gate animation is completed<br />
		/// This will prevent particle rotations persisting through animation states
		/// </summary>
		[ProtoMember(1)]
		[XmlElement("CleanOnEffectEnd")]
		public bool CleanOnEffectEnd = false;

		/// <summary>
		/// Whether this particle effect is marked dirty every tick<br />
		/// Should be false for effects using internal timers
		/// </summary>
		[ProtoMember(2)]
		[XmlElement("DirtifyEffect")]
		public bool DirtifyEffect = false;

		/// <summary>
		/// The name of the particle to display
		/// </summary>
		[ProtoMember(3)]
		[XmlArray("ParticleNames")]
		[XmlArrayItem("ParticleName")]
		public string[] ParticleNames = null;

		/// <summary>
		/// The local offset of this particle effect
		/// </summary>
		[ProtoMember(4)]
		[XmlElement("ParticleOffset")]
		public Vector3D ParticleOffset = Vector3D.Zero;

		/// <summary>
		/// The particle's orientation definition
		/// </summary>
		[ProtoMember(5)]
		[XmlElement("ParticleOrientation")]
		public ParticleOrientationDef ParticleOrientation = null;

		/// <summary>
		/// The transience IDs <br />
		/// Used to persist the specified particle effect between animation states<br />
		/// Must be higher than 0 to enable<br />
		/// Particles will be matched with other particle definitions with the same ID
		/// </summary>
		[ProtoMember(6)]
		[XmlArray("TransientIDs")]
		[XmlArrayItem("ID")]
		public byte[] TransientIDs = null;
		#endregion
	}

	/// <summary>
	/// Definition defining sounds
	/// </summary>
	[Serializable]
	[XmlRoot("SoundDef")]
	[ProtoContract(UseProtoMembersOnly = true)]
	public sealed class SoundDef : AnimatableDef
	{
		#region Public Variables
		/// <summary>
		/// The sound names to play
		/// </summary>
		[ProtoMember(1)]
		[XmlArray("SoundNames")]
		[XmlArrayItem("SoundName")]
		public string[] SoundNames;

		/// <summary>
		/// The volume to play at
		/// </summary>
		[ProtoMember(2)]
		[XmlElement("Volume")]
		public float Volume = 1;

		/// <summary>
		/// The range this sound can be heard at
		/// </summary>
		[ProtoMember(3)]
		[XmlElement("Distance")]
		public float? Distance = null;
		#endregion
	}

	/// <summary>
	/// Definition defining the beam pulse
	/// </summary>
	[Serializable]
	[XmlRoot("BeamPulseDef")]
	[ProtoContract(UseProtoMembersOnly = true)]
	public sealed class BeamPulseDef : AnimatableDef
	{
		#region Public Variables
		/// <summary>
		/// The time (in game ticks) this beam will take to travel from jump node to endpoint
		/// </summary>
		[ProtoMember(1)]
		[XmlElement("TravelTime")]
		public ushort TravelTime = 0;

		/// <summary>
		/// The beam's color
		/// </summary>
		[ProtoMember(2)]
		[XmlElement("BeamColor")]
		public Color BeamColor = Color.Transparent;

		/// <summary>
		/// The beam's maximum length
		/// </summary>
		[ProtoMember(3)]
		[XmlElement("BeamLength")]
		public double BeamLength = -1;

		/// <summary>
		/// The beam's width (in meters)
		/// </summary>
		[ProtoMember(4)]
		[XmlElement("BeamWidth")]
		public double BeamWidth = 0;

		/// <summary>
		/// The beam's brightness
		/// </summary>
		[ProtoMember(5)]
		[XmlElement("BeamBrightness")]
		public double BeamBrightness = 1;

		/// <summary>
		/// The beam's frequency<br />
		/// Higher values result in smaller segments<br />
		/// Set to 0 for a constant, unbroken beam
		/// </summary>
		[ProtoMember(6)]
		[XmlElement("BeamFrequency")]
		public double BeamFrequency = 0;

		/// <summary>
		/// The beam's duty cycle<br />
		/// Has no effect if the frequency is 0<br />
		/// Set to 1 for a segmented beam with no gaps<br />
		/// Set to 0.5 for a segmented beam with equally spaced gaps
		/// </summary>
		[ProtoMember(7)]
		[XmlElement("BeamDutyCycle")]
		public double BeamDutyCycle = 1;

		/// <summary>
		/// The beam's offset
		/// </summary>
		[ProtoMember(8)]
		[XmlElement("BeamOffset")]
		public double BeamOffset = 0;

		/// <summary>
		/// The beam's material
		/// </summary>
		[ProtoMember(9)]
		[XmlElement("BeamMaterial")]
		public string Material = "WeaponLaser";

		/// <summary>
		/// The particle to use for the beam's head
		/// </summary>
		[ProtoMember(10)]
		[XmlArray("FlashPointParticles")]
		[XmlArrayItem("Particle")]
		public ParticleDef[] FlashPointParticles = null;
		#endregion
	}

	/// <summary>
	/// Definition defining a gate's drive emitter emissive colors
	/// </summary>
	[Serializable]
	[XmlRoot("DriveEmissiveColorDef")]
	[ProtoContract(UseProtoMembersOnly = true)]
	public sealed class DriveEmissiveColorDef : AnimatableDef
	{
		#region Public Variables
		/// <summary>
		/// The intended emissive color
		/// </summary>
		[ProtoMember(1)]
		[XmlElement("EmissiveColor")]
		public Color EmissiveColor = Color.Black;

		/// <summary>
		/// The intended emissive color brightness
		/// </summary>
		[ProtoMember(2)]
		[XmlElement("EmissiveBrightness")]
		public double Brightness = 1;
		#endregion
	}

	/// <summary>
	/// Definition defining a gate's node attractor force
	/// </summary>
	[Serializable]
	[XmlRoot("NodePhysicsDef")]
	[ProtoContract(UseProtoMembersOnly = true)]
	public sealed class NodePhysicsDef : AnimatableDef
	{
		#region Public Variables
		/// <summary>
		/// The attractor force strength<br />
		/// Positive values attract entities towards jump node<br />
		/// Negative values repel entities away from jump node
		/// </summary>
		[ProtoMember(1)]
		[XmlElement("Force")]
		public double AttractorForce = 0;

		/// <summary>
		/// The attractor force falloff
		/// </summary>
		[ProtoMember(2)]
		[XmlElement("ForceFalloff")]
		public double AttractorForceFalloff = 0;

		/// <summary>
		/// The attractor force max speed<br />
		/// Objects above or at this speed will not be affected by the attractor force
		/// </summary>
		[ProtoMember(3)]
		[XmlElement("MaxSpeed")]
		public double MaxSpeed = 0;

		/// <summary>
		/// The attractor force offset
		/// </summary>
		[ProtoMember(4)]
		[XmlElement("ForceOffset")]
		public Vector3D ForceOffset = Vector3D.Zero;

		/// <summary>
		/// The attractor force torque
		/// </summary>
		[ProtoMember(5)]
		[XmlElement("Torque")]
		public Vector3D AttractorTorque = Vector3D.Zero;
		#endregion
	}

	/// <summary>
	/// Definition defining a drive's entity lock particles
	/// </summary>
	[Serializable]
	[XmlRoot("DriveEntityLockDef")]
	[ProtoContract(UseProtoMembersOnly = true)]
	public sealed class DriveEntityLockDef : AnimatableDef
	{
		#region Public Variables
		/// <summary>
		/// The lock delay shift<br />
		/// Controls how long it takes for drives to lock onto an entity
		/// </summary>
		[ProtoMember(1)]
		[XmlElement("LockDelayShift")]
		public LockDelayShiftEnum LockDelayShift = LockDelayShiftEnum.FIXED;

		/// <summary>
		/// The entity lock sorting type
		/// </summary>
		[ProtoMember(2)]
		[XmlElement("EntityLockType")]
		public EntityLockTypeEnum EntityLockType = EntityLockTypeEnum.LOCK_NEAREST;

		/// <summary>
		/// The easing type for the lock rotation
		/// </summary>
		[ProtoMember(3)]
		[XmlElement("LockDelayEasingType")]
		public EasingTypeEnum LockDelayEasingType = EasingTypeEnum.EASE_IN_OUT;

		/// <summary>
		/// The easing durve for the lock rotation
		/// </summary>
		[ProtoMember(4)]
		[XmlElement("LockDelayEasingCurve")]
		public EasingCurveEnum LockDelayEasingCurve = EasingCurveEnum.LINEAR;

		/// <summary>
		/// The minimum lock time in game ticks
		/// </summary>
		[ProtoMember(5)]
		[XmlElement("MinLockTime")]
		public ushort MinLockTime = 0;

		/// <summary>
		/// The maximum lock time in game ticks
		/// </summary>
		[ProtoMember(6)]
		[XmlElement("MaxLockTime")]
		public ushort MaxLockTime = 0;

		/// <summary>
		/// The maximum number of entities that can locked onto at once
		/// </summary>
		[ProtoMember(7)]
		[XmlElement("MaxEntityLockCount")]
		public int MaxEntityLockCount = 0;

		/// <summary>
		/// Modifies how the ratio is calculated (clamped between -1 and 1)<br />
		/// 0    - Normal<br />
		/// 0.5  - Starts halfway<br />
		/// 1    - Starts fully locked<br />
		/// -0.5 - Starts halfway but goes backwards<br />
		/// -1   - Starts fully locked but goes backwards
		/// </summary>
		[ProtoMember(8)]
		[XmlElement("RatioModifier")]
		public float RatioModifier = 0;

		/// <summary>
		/// The initial rotation (in degrees) of the entity lock particles relative to each drives' orientation
		/// </summary>
		[ProtoMember(9)]
		[XmlElement("InitialRotation")]
		public Vector3D InitialRotation = Vector3D.Zero;

		/// <summary>
		/// The entity lock particles to play
		/// </summary>
		[ProtoMember(10)]
		[XmlArray("EntityLockParticles")]
		[XmlArrayItem("Particle")]
		public ParticleDef[] EntityLockParticles = null;
		#endregion
	}

	/// <summary>
	/// Definition defining a gate's shape collider for wormhole animations
	/// </summary>
	[Serializable]
	[XmlRoot("ShapeColliderDef")]
	[ProtoContract(UseProtoMembersOnly = true)]
	public sealed class ShapeColliderDef : AnimatableDef
	{
		#region Public Variables
		/// <summary>
		/// The collider shape
		/// </summary>
		[ProtoMember(1)]
		[XmlElement("CollisionShape")]
		public CollisionShapeEnum CollisionShape = CollisionShapeEnum.NONE;

		/// <summary>
		/// The collider effect type
		/// </summary>
		[ProtoMember(2)]
		[XmlElement("CollisionEffectType")]
		public CollisionEffectTypeEnum CollisionEffectType = CollisionEffectTypeEnum.NONE;

		/// <summary>
		/// The percentage of the collider to cut out from the center for each axis<br />
		/// Inner area will not have effect applied
		/// </summary>
		[ProtoMember(3)]
		[XmlElement("InnerCutoutPercent")]
		public Vector3 InnerCutoutPercent = Vector3.Zero;

		/// <summary>
		/// The collider's position relative to the gate's jump node in meters
		/// </summary>
		[ProtoMember(4)]
		[XmlElement("Position")]
		public Vector3D Position = Vector3D.Zero;

		/// <summary>
		/// The collider's rotation around the jump node in degrees
		/// </summary>
		[ProtoMember(5)]
		[XmlElement("LockedRotation")]
		public Vector3D LockedRotation = Vector3D.Zero;

		/// <summary>
		/// The collider's rotation around itself in degrees
		/// </summary>
		[ProtoMember(6)]
		[XmlElement("FreeRotation")]
		public Vector3D FreeRotation = Vector3D.Zero;

		/// <summary>
		/// The collider's scale
		/// </summary>
		[ProtoMember(7)]
		[XmlElement("Scale")]
		public Vector3D Scale = Vector3D.One;

		[ProtoMember(8)]
		[XmlArray("EffectArguments")]
		[XmlArrayItem("Argument")]
		public double[] EffectArguments = null;
		#endregion
	}
}
