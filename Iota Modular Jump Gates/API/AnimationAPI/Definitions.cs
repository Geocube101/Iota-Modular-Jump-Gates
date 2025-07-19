using IOTA.ModularJumpGates.API.AnimationAPI.Util;
using ProtoBuf;
using VRageMath;

namespace IOTA.ModularJumpGates.API.AnimationAPI.Definitions
{
	#region Animation Object Definitions
	/// <summary>
	/// Definition defining a particle orientation
	/// </summary>
	[ProtoContract(UseProtoMembersOnly = true)]
	public class ParticleOrientationDef
	{
		#region Public Variables
		/// <summary>
		/// The particle's orientation
		/// </summary>
		[ProtoMember(1)]
		public ParticleOrientationEnum ParticleOrientation = ParticleOrientationEnum.GATE_ENDPOINT_NORMAL;

		[ProtoMember(2)]
		internal double[] WorldMatrixBase = new double[16] { -1, 0, 0, 0, 0, 1, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0 };

		/// <summary>
		/// If fixed, the particle's world matrix otherwise, the particle's rotation offset
		/// </summary>
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
			this.WorldMatrix = (particle_orientation == ParticleOrientationEnum.FIXED) ? MyAnimationAPISession.WorldMatrix : MatrixD.CreateFromYawPitchRoll(0, 0, 0);
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
	[ProtoContract(UseProtoMembersOnly = true)]
	public class AnimationConstraintDef
	{
		#region Public Variables
		/// <summary>
		/// The allowed range for a gate's jump space lateral radius
		/// </summary>
		[ProtoMember(1)]
		public NumberRange<double> AllowedJumpGateRadius = NumberRange<double>.RangeII(0, double.PositiveInfinity);

		/// <summary>
		/// The allowed range for a gate's drive count
		/// </summary>
		[ProtoMember(2)]
		public NumberRange<uint> AllowedJumpGateSize = NumberRange<uint>.RangeII(0, uint.MaxValue);

		/// <summary>
		/// The allowed range for a gate's working drive count
		/// </summary>
		[ProtoMember(3)]
		public NumberRange<uint> AllowedJumpGateWorkingSize = NumberRange<uint>.RangeII(0, uint.MaxValue);

		/// <summary>
		/// The allowed range for a gate's jump node endpoint distance
		/// </summary>
		[ProtoMember(4)]
		public NumberRange<double> AllowedJumpGateEndpointDistance = NumberRange<double>.RangeII(-1, double.PositiveInfinity);
		#endregion
	}

	/// <summary>
	/// Definition defining an atribute being animated
	/// </summary>
	[ProtoContract(UseProtoMembersOnly = true)]
	public sealed class AttributeAnimationDef
	{
		/// Modifies or animates a sound's volume
		/// </summary>
		[ProtoMember(1)]
		public DoubleKeyframe[] SoundVolumeAnimation = null;

		/// <summary>
		/// Modifies or animates a sound's distance
		/// </summary>
		[ProtoMember(2)]
		public DoubleKeyframe[] SoundDistanceAnimation = null;

		/// <summary>
		/// Modifies or animates a particle's birth multiplier
		/// </summary>
		[ProtoMember(3)]
		public DoubleKeyframe[] ParticleBirthAnimation = null;

		/// <summary>
		/// Modifies or animates a particle's color intensity multiplier
		/// </summary>
		[ProtoMember(4)]
		public DoubleKeyframe[] ParticleColorIntensityAnimation = null;

		/// <summary>
		/// Modifies or animates a particle's color multiplier
		/// </summary>
		[ProtoMember(5)]
		public VectorKeyframe[] ParticleColorAnimation = null;

		/// <summary>
		/// Modifies or animates a particle's fade multiplier
		/// </summary>
		[ProtoMember(6)]
		public DoubleKeyframe[] ParticleFadeAnimation = null;

		/// <summary>
		/// Modifies or animates a particle's life multiplier
		/// </summary>
		[ProtoMember(7)]
		public DoubleKeyframe[] ParticleLifeAnimation = null;

		/// <summary>
		/// Modifies or animates a particle's radius multiplier
		/// </summary>
		[ProtoMember(8)]
		public DoubleKeyframe[] ParticleRadiusAnimation = null;

		/// <summary>
		/// Modifies or animates a particle's scale multiplier
		/// </summary>
		[ProtoMember(9)]
		public DoubleKeyframe[] ParticleScaleAnimation = null;

		/// <summary>
		/// Modifies or animates a particle's velocity multiplier
		/// </summary>
		[ProtoMember(10)]
		public DoubleKeyframe[] ParticleVelocityAnimation = null;

		/// <summary>
		/// Modifies or animates a particle's rotation speed
		/// </summary>
		[ProtoMember(11)]
		public VectorKeyframe[] ParticleRotationSpeedAnimation = null;

		/// <summary>
		/// Modifies or animates a particle's offset in meters
		/// </summary>
		[ProtoMember(12)]
		public VectorKeyframe[] ParticleOffsetAnimation = null;

		/// <summary>
		/// Modifies or animates a beam pulse's frequency<br />
		/// For a solid beam, set this to 0<br />
		/// For a gradient, this value must not be 0
		/// </summary>
		[ProtoMember(13)]
		public DoubleKeyframe[] BeamFrequencyAnimation = null;

		/// <summary>
		/// Modifies or animates a beam pulse's duty cycle<br />
		/// For a gradient beam with no breaks, set this to 1
		/// </summary>
		[ProtoMember(14)]
		public DoubleKeyframe[] BeamDutyCycleAnimation = null;

		/// <summary>
		/// Modifies or animates a beam pulse's offset
		/// </summary>
		[ProtoMember(15)]
		public DoubleKeyframe[] BeamOffsetAnimation = null;

		/// <summary>
		/// Modifies or animates the jump space attractor force<br />
		/// If not-zero, a force will be applied to all entities within the jump space during jump<br />
		/// Positive values attract entities to jump node<br />
		/// Negative values repel entities from jump node<br />
		/// </summary>
		[ProtoMember(16)]
		public DoubleKeyframe[] PhysicsForceAnimation = null;

		/// <summary>
		/// Modifies or animates the jump space attractor force falloff
		/// </summary>
		[ProtoMember(17)]
		public DoubleKeyframe[] PhysicsForceFalloffAnimation = null;

		/// <summary>
		/// Modifies or animates the jump space attractor force offset
		/// </summary>
		[ProtoMember(18)]
		public VectorKeyframe[] PhysicsForceOffsetAnimation = null;

		/// <summary>
		/// Modifies or animates the jump space attractor force max allowed speed
		/// </summary>
		[ProtoMember(19)]
		public DoubleKeyframe[] PhysicsForceMaxSpeedAnimation = null;

		/// <summary>
		/// Modifies or animates the jump space attractor force torque
		/// </summary>
		[ProtoMember(20)]
		public VectorKeyframe[] PhysicsForceTorqueAnimation = null;

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
	[ProtoContract(UseProtoMembersOnly = true)]
	[ProtoInclude(100, typeof(ParticleDef))]
	[ProtoInclude(200, typeof(SoundDef))]
	[ProtoInclude(300, typeof(BeamPulseDef))]
	[ProtoInclude(400, typeof(DriveEmissiveColorDef))]
	[ProtoInclude(500, typeof(NodePhysicsDef))]
	[ProtoInclude(600, typeof(DriveEntityLockDef))]
	public class AnimatableDef
	{
		#region Public Variables
		/// <summary>
		/// The start time of this animation
		/// </summary>
		[ProtoMember(1)]
		public ushort StartTime;

		/// <summary>
		/// The duraton of this animation in game ticks
		/// </summary>
		[ProtoMember(2)]
		public ushort Duration;

		/// <summary>
		/// The keyframe holder for animations
		/// </summary>
		[ProtoMember(3)]
		public AttributeAnimationDef Animations = null;
		#endregion
	}

	/// <summary>
	/// Definition defining particles
	/// </summary>
	[ProtoContract(UseProtoMembersOnly = true)]
	public class ParticleDef : AnimatableDef
	{
		#region Public Variables
		/// <summary>
		/// Whether to clean this particle effect once it's completed<br />
		/// If false, effect is cleaned when entire gate animation is completed<br />
		/// This will prevent particle rotations persisting through animation states
		/// </summary>
		[ProtoMember(1)]
		public bool CleanOnEffectEnd = false;

		/// <summary>
		/// Whether this particle effect is marked dirty every tick<br />
		/// Should be false for effects using internal timers
		/// </summary>
		[ProtoMember(2)]
		public bool DirtifyEffect = false;

		/// <summary>
		/// The name of the particle to display
		/// </summary>
		[ProtoMember(3)]
		public string[] ParticleNames = null;

		/// <summary>
		/// The local offset of this particle effect
		/// </summary>
		[ProtoMember(4)]
		public Vector3D ParticleOffset = Vector3D.Zero;

		/// <summary>
		/// The particle's orientation definition
		/// </summary>
		[ProtoMember(5)]
		public ParticleOrientationDef ParticleOrientation = null;

		/// <summary>
		/// The transience IDs <br />
		/// Used to persist the specified particle effect between animation states<br />
		/// Must be higher than 0 to enable<br />
		/// Particles will be matched with other particle definitions with the same ID
		/// </summary>
		[ProtoMember(6)]
		public byte[] TransientIDs = null;
		#endregion
	}

	/// <summary>
	/// Definition defining sounds
	/// </summary>
	[ProtoContract(UseProtoMembersOnly = true)]
	public class SoundDef : AnimatableDef
	{
		#region Public Variables
		/// <summary>
		/// The sound names to play
		/// </summary>
		[ProtoMember(1)]
		public string[] SoundNames;

		/// <summary>
		/// The volume to play at
		/// </summary>
		[ProtoMember(2)]
		public float Volume = 1;

		/// <summary>
		/// The range this sound can be heard at
		/// </summary>
		[ProtoMember(3)]
		public float? Distance = null;
		#endregion
	}

	/// <summary>
	/// Definition defining the beam pulse
	/// </summary>
	[ProtoContract(UseProtoMembersOnly = true)]
	public class BeamPulseDef : AnimatableDef
	{
		#region Public Variables
		/// <summary>
		/// The time (in game ticks) this beam will take to travel from jump node to endpoint
		/// </summary>
		[ProtoMember(1)]
		public ushort TravelTime = 0;

		/// <summary>
		/// The beam's color
		/// </summary>
		[ProtoMember(2)]
		public Color BeamColor = Color.Transparent;

		/// <summary>
		/// The beam's maximum length
		/// </summary>
		[ProtoMember(3)]
		public double BeamLength = -1;

		/// <summary>
		/// The beam's width (in meters)
		/// </summary>
		[ProtoMember(4)]
		public double BeamWidth = 0;

		/// <summary>
		/// The beam's brightness
		/// </summary>
		[ProtoMember(5)]
		public double BeamBrightness = 1;

		/// <summary>
		/// The beam's frequency<br />
		/// Higher values result in smaller segments<br />
		/// Set to 0 for a constant, unbroken beam
		/// </summary>
		[ProtoMember(6)]
		public double BeamFrequency = 0;

		/// <summary>
		/// The beam's duty cycle<br />
		/// Has no effect if the frequency is 0<br />
		/// Set to 1 for a segmented beam with no gaps<br />
		/// Set to 0.5 for a segmented beam with equally spaced gaps
		/// </summary>
		[ProtoMember(7)]
		public double BeamDutyCycle = 1;

		/// <summary>
		/// The beam's offset
		/// </summary>
		[ProtoMember(8)]
		public double BeamOffset = 0;

		/// <summary>
		/// The beam's material
		/// </summary>
		[ProtoMember(9)]
		public string Material = "WeaponLaser";

		/// <summary>
		/// The particle to use for the beam's head
		/// </summary>
		[ProtoMember(10)]
		public ParticleDef[] FlashPointParticles = null;
		#endregion
	}

	/// <summary>
	/// Definition defining a gate's drive emitter emissive colors
	/// </summary>
	[ProtoContract(UseProtoMembersOnly = true)]
	public class DriveEmissiveColorDef : AnimatableDef
	{
		#region Public Variables
		/// <summary>
		/// The intended emissive color
		/// </summary>
		[ProtoMember(1)]
		public Color EmissiveColor = Color.Black;

		/// <summary>
		/// The intended emissive color brightness
		/// </summary>
		[ProtoMember(2)]
		public double Brightness = 1;
		#endregion
	}

	/// <summary>
	/// Definition defining a gate's node attractor force
	/// </summary>
	[ProtoContract(UseProtoMembersOnly = true)]
	public class NodePhysicsDef : AnimatableDef
	{
		#region Public Variables
		/// <summary>
		/// The attractor force strength<br />
		/// Positive values attract entities towards jump node<br />
		/// Negative values repel entities away from jump node
		/// </summary>
		[ProtoMember(1)]
		public double AttractorForce = 0;

		/// <summary>
		/// The attractor force falloff
		/// </summary>
		[ProtoMember(2)]
		public double AttractorForceFalloff = 0;

		/// <summary>
		/// The attractor force max speed<br />
		/// Objects above or at this speed will not be affected by the attractor force
		/// </summary>
		[ProtoMember(3)]
		public double MaxSpeed = 0;

		/// <summary>
		/// The attractor force offset
		/// </summary>
		[ProtoMember(4)]
		public Vector3D ForceOffset = Vector3D.Zero;

		/// <summary>
		/// The attractor force torque
		/// </summary>
		[ProtoMember(5)]
		public Vector3D AttractorTorque = Vector3D.Zero;
		#endregion
	}

	/// <summary>
	/// Definition defining a drive's entity lock particles
	/// </summary>
	[ProtoContract(UseProtoMembersOnly = true)]
	public class DriveEntityLockDef : AnimatableDef
	{
		/// <summary>
		/// The lock delay shift<br />
		/// Controls how long it takes for drives to lock onto an entity
		/// </summary>
		[ProtoMember(1)]
		public LockDelayShiftEnum LockDelayShift = LockDelayShiftEnum.FIXED;

		/// <summary>
		/// The entity lock sorting type
		/// </summary>
		[ProtoMember(2)]
		public EntityLockTypeEnum EntityLockType = EntityLockTypeEnum.LOCK_NEAREST;

		/// <summary>
		/// The easing type for the lock rotation
		/// </summary>
		[ProtoMember(3)]
		public EasingTypeEnum LockDelayEasingType = EasingTypeEnum.EASE_IN_OUT;

		/// <summary>
		/// The easing durve for the lock rotation
		/// </summary>
		[ProtoMember(4)]
		public EasingCurveEnum LockDelayEasingCurve = EasingCurveEnum.LINEAR;

		/// <summary>
		/// The minimum lock time in game ticks
		/// </summary>
		[ProtoMember(5)]
		public ushort MinLockTime = 0;

		/// <summary>
		/// The maximum lock time in game ticks
		/// </summary>
		[ProtoMember(6)]
		public ushort MaxLockTime = 0;

		/// <summary>
		/// The maximum number of entities that can locked onto at once
		/// </summary>
		[ProtoMember(7)]
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
		public float RatioModifier = 0;

		/// <summary>
		/// The initial rotation (in degrees) of the entity lock particles relative to each drives' orientation
		/// </summary>
		[ProtoMember(9)]
		public Vector3D InitialRotation = Vector3D.Zero;

		/// <summary>
		/// The entity lock particles to play
		/// </summary>
		[ProtoMember(10)]
		public ParticleDef[] EntityLockParticles = null;
	}
	#endregion

	#region Animation Definitions
	/// <summary>
	/// Definition defining the "charging/jumping" phase animation
	/// </summary>
	[ProtoContract(UseProtoMembersOnly = true)]
	public class JumpGateJumpingAnimationDef
	{
		#region Public Variables
		/// <summary>
		/// The duration of the animaton in game ticks
		/// </summary>
		[ProtoMember(1)]
		public ushort Duration = 0;

		/// <summary>
		/// The list of particle definitions for each drive<br />
		/// These particles will each be played for every drive in the gate
		/// </summary>
		[ProtoMember(2)]
		public ParticleDef[] PerDriveParticles = null;

		/// <summary>
		/// The list of particle definitions for each anti drive<br />
		/// These particles will each be played for every drive in the targeted gate
		/// </summary>
		[ProtoMember(3)]
		public ParticleDef[] PerAntiDriveParticles = null;

		/// <summary>
		/// The list of particle definitions for each jump space entity<br />
		/// These particles will each be played for every entity in the gate's jump space
		/// </summary>
		[ProtoMember(4)]
		public ParticleDef[] PerEntityParticles = null;

		/// <summary>
		/// The list of ParticleDef definitions for the gate's jump node<br />
		/// These particles will be played once at the gate's jump node
		/// </summary>
		[ProtoMember(5)]
		public ParticleDef[] NodeParticles = null;

		/// <summary>
		/// The list of SoundDef definitions<br />
		/// These sounds will be played once at the gate's jump node
		/// </summary>
		[ProtoMember(6)]
		public SoundDef[] NodeSounds = null;

		/// <summary>
		/// The DriveEmissiveColorDef defining the color for this gate's jump drive emitter emissives
		/// </summary>
		[ProtoMember(7)]
		public DriveEmissiveColorDef DriveEmissiveColor = null;

		/// <summary>
		/// The NodePhysicsDef defining the attractor forces for this gate's jump node
		/// </summary>
		[ProtoMember(8)]
		public NodePhysicsDef NodePhysics = null;

		/// <summary>
		/// The list of ParticleDef definitions for the gate's anti-node<br />
		/// These particles will be played once at the gate's anti-node<br />
		/// <i>The anti-node is the region at the endpoint of this gate</i>
		/// </summary>
		[ProtoMember(9)]
		public ParticleDef[] AntiNodeParticles = null;

		/// <summary>
		/// The list of SoundDef definitions for the gate's anti-node<br />
		/// These sounds will be played once at the gate's anti-node<br />
		/// <i>The anti-node is the region at the endpoint of this gate</i>
		/// </summary>
		[ProtoMember(10)]
		public SoundDef[] AntiNodeSounds = null;

		/// <summary>
		/// The NodePhysicsDef defining the attractor forces for this gate's anti-ode<br />
		/// <i>The anti-node is the region at the endpoint of this gate</i>
		/// </summary>
		[ProtoMember(11)]
		public NodePhysicsDef AntiNodePhysics = null;

		/// <summary>
		/// The DriveEntityLockDef defining the entity lock particles for this gate's drives
		/// </summary>
		[ProtoMember(12)]
		public DriveEntityLockDef DriveEntityLock = null;
		#endregion
	}

	/// <summary>
	/// Definition defining the "jumped" phase animation
	/// </summary>
	[ProtoContract(UseProtoMembersOnly = true)]
	public class JumpGateJumpedAnimationDef
	{
		#region Public Variables
		/// <summary>
		/// The duration of the animaton in game ticks
		/// </summary>
		[ProtoMember(1)]
		public ushort Duration = 0;

		/// <summary>
		/// The duration of the travel warp in game ticks
		/// </summary>
		[ProtoMember(2)]
		public ushort TravelTime = 0;

		/// <summary>
		/// The list of particle definitions for each drive<br />
		/// These particles will each be played for every drive in the gate
		/// </summary>
		[ProtoMember(3)]
		public ParticleDef[] PerDriveParticles = null;

		/// <summary>
		/// The list of particle definitions for each anti drive<br />
		/// These particles will each be played for every drive in the targeted gate
		/// </summary>
		[ProtoMember(4)]
		public ParticleDef[] PerAntiDriveParticles = null;

		/// <summary>
		/// The list of particle definitions for each jump space entity<br />
		/// These particles will each be played for every entity in the gate's jump space
		/// </summary>
		[ProtoMember(5)]
		public ParticleDef[] PerEntityParticles = null;

		/// <summary>
		/// The list of ParticleDef definitions for the gate's jump node<br />
		/// These particles will be played once at the gate's jump node
		/// </summary>
		[ProtoMember(6)]
		public ParticleDef[] NodeParticles = null;

		/// <summary>
		/// The travel particle effect shown to entities within the jump space
		/// </summary>
		[ProtoMember(7)]
		public ParticleDef[] TravelEffects = null;

		/// <summary>
		/// The list of SoundDef definitions<br />
		/// These sounds will be played once at the gate's jump node
		/// </summary>
		[ProtoMember(8)]
		public SoundDef[] NodeSounds = null;

		/// <summary>
		/// The list of SoundDef definitions<br />
		/// These sounds will be played once to entities currently being jumped
		/// </summary>
		[ProtoMember(9)]
		public SoundDef[] TravelSounds = null;

		/// <summary>
		/// The BeamPulseDef defining the beam pulse for this gate
		/// </summary>
		[ProtoMember(10)]
		public BeamPulseDef BeamPulse = null;

		/// <summary>
		/// The DriveEmissiveColorDef defining the color for this gate's jump drive emitter emissives
		/// </summary>
		[ProtoMember(11)]
		public DriveEmissiveColorDef DriveEmissiveColor = null;

		/// <summary>
		/// The NodePhysicsDef defining the attractor forces for this gate's jump node
		/// </summary>
		[ProtoMember(12)]
		public NodePhysicsDef NodePhysics = null;

		/// <summary>
		/// The list of ParticleDef definitions for the gate's anti-node<br />
		/// These particles will be played once at the gate's anti-node<br />
		/// <i>The anti-node is the region at the endpoint of this gate</i>
		/// </summary>
		[ProtoMember(13)]
		public ParticleDef[] AntiNodeParticles = null;

		/// <summary>
		/// The list of SoundDef definitions for the gate's anti-node<br />
		/// These sounds will be played once at the gate's anti-node<br />
		/// <i>The anti-node is the region at the endpoint of this gate</i>
		/// </summary>
		[ProtoMember(14)]
		public SoundDef[] AntiNodeSounds = null;

		/// <summary>
		/// The NodePhysicsDef defining the attractor forces for this gate's anti-ode<br />
		/// <i>The anti-node is the region at the endpoint of this gate</i>
		/// </summary>
		[ProtoMember(15)]
		public NodePhysicsDef AntiNodePhysics = null;

		/// <summary>
		/// The DriveEntityLockDef defining the entity lock particles for this gate's drives
		/// </summary>
		[ProtoMember(16)]
		public DriveEntityLockDef DriveEntityLock = null;
		#endregion
	}

	/// <summary>
	/// Definition defining the "failed" phase animation
	/// </summary>
	[ProtoContract(UseProtoMembersOnly = true)]
	public class JumpGateFailedAnimationDef
	{
		#region Public Variables
		/// <summary>
		/// The duration of the animaton in game ticks
		/// </summary>
		[ProtoMember(1)]
		public ushort Duration = 0;

		/// <summary>
		/// The list of particle definitions for each drive<br />
		/// These particles will each be played for every drive in the gate
		/// </summary>
		[ProtoMember(2)]
		public ParticleDef[] PerDriveParticles = null;

		/// <summary>
		/// The list of particle definitions for each anti drive<br />
		/// These particles will each be played for every drive in the targeted gate
		/// </summary>
		[ProtoMember(3)]
		public ParticleDef[] PerAntiDriveParticles = null;

		/// <summary>
		/// The list of particle definitions for each jump space entity<br />
		/// These particles will each be played for every entity in the gate's jump space
		/// </summary>
		[ProtoMember(4)]
		public ParticleDef[] PerEntityParticles = null;

		/// <summary>
		/// The list of ParticleDef definitions for the gate's jump node<br />
		/// These particles will be played once at the gate's jump node
		/// </summary>
		[ProtoMember(5)]
		public ParticleDef[] NodeParticles = null;

		/// <summary>
		/// The list of SoundDef definitions<br />
		/// These sounds will be played once at the gate's jump node
		/// </summary>
		[ProtoMember(6)]
		public SoundDef[] NodeSounds = null;

		/// <summary>
		/// The DriveEmissiveColorDef defining the color for this gate's jump drive emitter emissives
		/// </summary>
		[ProtoMember(7)]
		public DriveEmissiveColorDef DriveEmissiveColor = null;

		/// <summary>
		/// The NodePhysicsDef defining the attractor forces for this gate's jump node
		/// </summary>
		[ProtoMember(8)]
		public NodePhysicsDef NodePhysics = null;

		/// <summary>
		/// The list of ParticleDef definitions for the gate's anti-node<br />
		/// These particles will be played once at the gate's anti-node<br />
		/// <i>The anti-node is the region at the endpoint of this gate</i>
		/// </summary>
		[ProtoMember(9)]
		public ParticleDef[] AntiNodeParticles = null;

		/// <summary>
		/// The list of SoundDef definitions for the gate's anti-node<br />
		/// These sounds will be played once at the gate's anti-node<br />
		/// <i>The anti-node is the region at the endpoint of this gate</i>
		/// </summary>
		[ProtoMember(10)]
		public SoundDef[] AntiNodeSounds = null;

		/// <summary>
		/// The NodePhysicsDef defining the attractor forces for this gate's anti-ode<br />
		/// <i>The anti-node is the region at the endpoint of this gate</i>
		/// </summary>
		[ProtoMember(11)]
		public NodePhysicsDef AntiNodePhysics = null;

		/// <summary>
		/// The DriveEntityLockDef defining the entity lock particles for this gate's drives
		/// </summary>
		[ProtoMember(12)]
		public DriveEntityLockDef DriveEntityLock = null;
		#endregion
	}

	/// <summary>
	/// Definition defining an entire gate animation
	/// </summary>
	[ProtoContract(UseProtoMembersOnly = true)]
	public class AnimationDef
	{
		#region Internal Variables
		/// <summary>
		/// Whether to serialize this animation to XML after session unload
		/// </summary>
		[ProtoMember(1)]
		internal bool SerializeOnEnd = false;

		/// <summary>
		/// The mod that defined this animation
		/// </summary>
		[ProtoMember(2)]
		internal string SourceMod = null;

		/// <summary>
		/// The subtype ID of this animation<br />
		/// If multiple animatons with the same name are defined, and all but one animation have a contraint defined, this value will be non-null
		/// </summary>
		[ProtoMember(3)]
		internal ulong? SubtypeID = null;
		#endregion

		#region Public Variables
		/// <summary>
		/// Whether this animation is enabled<br />
		/// Disabled animations are not shown in the controller list
		/// </summary>
		[ProtoMember(4)]
		public bool Enabled = true;

		/// <summary>
		/// Whether this animation can be cancelled immediatly<br />
		/// If false, animation in the jumping phase will cancel once complete
		/// </summary>
		[ProtoMember(5)]
		public bool ImmediateCancel = true;

		/// <summary>
		/// The name of this animation
		/// </summary>
		[ProtoMember(6)]
		public string AnimationName;

		/// <summary>
		/// The description of this animation
		/// </summary>
		[ProtoMember(7)]
		public string Description;

		/// <summary>
		/// The JumpGateJumpingAnimationDef definition defining the jumping phase of this animation
		/// </summary>
		[ProtoMember(8)]
		public JumpGateJumpingAnimationDef JumpingAnimationDef = null;

		/// <summary>
		/// The JumpGateJumpedAnimationDef definition defining the jumped phase of this animation
		/// </summary>
		[ProtoMember(9)]
		public JumpGateJumpedAnimationDef JumpedAnimationDef = null;

		/// <summary>
		/// The JumpGateFailedAnimationDef definition defining the failed phase of this animation
		/// </summary>
		[ProtoMember(10)]
		public JumpGateFailedAnimationDef FailedAnimationDef = null;

		/// <summary>
		/// The AnimationConstraintDef definition defining a jump gate constraint for this animation
		/// </summary>
		[ProtoMember(11)]
		public AnimationConstraintDef AnimationConstraint = null;
		#endregion

		#region Constructors
		/// <summary>
		/// Dummy default constuctor for ProtoBuf
		/// </summary>
		public AnimationDef() { }

		/// <summary>
		/// Creates a new AnimationDef
		/// </summary>
		/// <param name="name">The animation's name</param>
		/// <param name="description">The animation's description</param>
		/// <param name="serialize">Whether to serialize this animation to XML on session unload<br />Serialized animations are stored in the global mod storage folder</param>
		public AnimationDef(string name, string description = null, bool serialize = false)
		{
			this.AnimationName = name;
			this.Description = description;
			this.SerializeOnEnd = serialize;
			MyAnimationAPISession.AddAnimation(this);
		}
		#endregion
	}
	#endregion
}
