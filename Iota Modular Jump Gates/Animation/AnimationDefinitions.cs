using ProtoBuf;
using System;
using System.Xml.Serialization;

namespace IOTA.ModularJumpGates.Animation
{
	/// <summary>
	/// Definition defining the "charging/jumping" phase animation
	/// </summary>
	[Serializable]
	[XmlRoot("JumpGateJumpingAnimationDef")]
	[ProtoContract(UseProtoMembersOnly = true)]
	public class JumpGateJumpingAnimationDef
	{
		#region Public Variables
		/// <summary>
		/// The duration of the animaton in game ticks
		/// </summary>
		[ProtoMember(1)]
		[XmlElement("Duration")]
		public ushort Duration = 0;

		/// <summary>
		/// The list of particle definitions for each drive<br />
		/// These particles will each be played for every drive in the gate
		/// </summary>
		[ProtoMember(2)]
		[XmlArray("PerDriveParticles")]
		[XmlArrayItem("Particle")]
		public ParticleDef[] PerDriveParticles = null;

		/// <summary>
		/// The list of particle definitions for each anti drive<br />
		/// These particles will each be played for every drive in the targeted gate
		/// </summary>
		[ProtoMember(3)]
		[XmlArray("PerAntiDriveParticles")]
		[XmlArrayItem("Particle")]
		public ParticleDef[] PerAntiDriveParticles = null;

		/// <summary>
		/// The list of particle definitions for each jump space entity<br />
		/// These particles will each be played for every entity in the gate's jump space
		/// </summary>
		[ProtoMember(4)]
		[XmlArray("PerEntityParticles")]
		[XmlArrayItem("Particle")]
		public ParticleDef[] PerEntityParticles = null;

		/// <summary>
		/// The list of ParticleDef definitions for the gate's jump node<br />
		/// These particles will be played once at the gate's jump node
		/// </summary>
		[ProtoMember(5)]
		[XmlArray("NodeParticles")]
		[XmlArrayItem("Particle")]
		public ParticleDef[] NodeParticles = null;

		/// <summary>
		/// The list of SoundDef definitions<br />
		/// These sounds will be played once at the gate's jump node
		/// </summary>
		[ProtoMember(6)]
		[XmlArray("NodeSounds")]
		[XmlArrayItem("Sound")]
		public SoundDef[] NodeSounds = null;

		/// <summary>
		/// The DriveEmissiveColorDef defining the color for this gate's jump drive emitter emissives
		/// </summary>
		[ProtoMember(7)]
		[XmlElement("DriveEmissiveColor")]
		public DriveEmissiveColorDef DriveEmissiveColor = null;

		/// <summary>
		/// The NodePhysicsDef defining the attractor forces for this gate's jump node
		/// </summary>
		[ProtoMember(8)]
		[XmlElement("NodePhysics")]
		public NodePhysicsDef NodePhysics = null;

		/// <summary>
		/// The list of ParticleDef definitions for the gate's anti-node<br />
		/// These particles will be played once at the gate's anti-node<br />
		/// <i>The anti-node is the region at the endpoint of this gate</i>
		/// </summary>
		[ProtoMember(9)]
		[XmlArray("AntiNodeParticles")]
		[XmlArrayItem("Particle")]
		public ParticleDef[] AntiNodeParticles = null;

		/// <summary>
		/// The list of SoundDef definitions for the gate's anti-node<br />
		/// These sounds will be played once at the gate's anti-node<br />
		/// <i>The anti-node is the region at the endpoint of this gate</i>
		/// </summary>
		[ProtoMember(10)]
		[XmlArray("AntiNodeSounds")]
		[XmlArrayItem("Sound")]
		public SoundDef[] AntiNodeSounds = null;

		/// <summary>
		/// The NodePhysicsDef defining the attractor forces for this gate's anti-node<br />
		/// <i>The anti-node is the region at the endpoint of this gate</i>
		/// </summary>
		[ProtoMember(11)]
		[XmlElement("AntiNodePhysics")]
		public NodePhysicsDef AntiNodePhysics = null;

		/// <summary>
		/// The DriveEntityLockDef defining the entity lock particles for this gate's drives
		/// </summary>
		[ProtoMember(12)]
		[XmlElement("DriveEntityLock")]
		public DriveEntityLockDef DriveEntityLock = null;
		#endregion

		#region Internal Methods
		/// <summary>
		/// Finalizes this animation<br />
		/// MyAnimation keyframes are sorted by position
		/// </summary>
		internal void Prepare()
		{

		}
		#endregion
	}

	/// <summary>
	/// Definition defining the "jumped" phase animation
	/// </summary>
	[Serializable]
	[XmlRoot("JumpGateJumpedAnimationDef")]
	[ProtoContract(UseProtoMembersOnly = true)]
	public class JumpGateJumpedAnimationDef
	{
		#region Public Variables
		/// <summary>
		/// The duration of the animaton in game ticks
		/// </summary>
		[ProtoMember(1)]
		[XmlElement("Duration")]
		public ushort Duration = 0;

		/// <summary>
		/// The duration of the travel warp in game ticks
		/// </summary>
		[ProtoMember(2)]
		[XmlElement("TravelTime")]
		public ushort TravelTime = 0;

		/// <summary>
		/// The list of particle definitions for each drive<br />
		/// These particles will each be played for every drive in the gate
		/// </summary>
		[ProtoMember(3)]
		[XmlArray("PerDriveParticles")]
		[XmlArrayItem("Particle")]
		public ParticleDef[] PerDriveParticles = null;

		/// <summary>
		/// The list of particle definitions for each anti drive<br />
		/// These particles will each be played for every drive in the targeted gate
		/// </summary>
		[ProtoMember(4)]
		[XmlArray("PerAntiDriveParticles")]
		[XmlArrayItem("Particle")]
		public ParticleDef[] PerAntiDriveParticles = null;

		/// <summary>
		/// The list of particle definitions for each jump space entity<br />
		/// These particles will each be played for every entity in the gate's jump space
		/// </summary>
		[ProtoMember(5)]
		[XmlArray("PerEntityParticles")]
		[XmlArrayItem("Particle")]
		public ParticleDef[] PerEntityParticles = null;

		/// <summary>
		/// The list of ParticleDef definitions for the gate's jump node<br />
		/// These particles will be played once at the gate's jump node
		/// </summary>
		[ProtoMember(6)]
		[XmlArray("NodeParticles")]
		[XmlArrayItem("Particle")]
		public ParticleDef[] NodeParticles = null;

		/// <summary>
		/// The travel particle effect shown to entities within the jump space
		/// </summary>
		[ProtoMember(7)]
		[XmlArray("TravelEffects")]
		[XmlArrayItem("Particle")]
		public ParticleDef[] TravelEffects = null;

		/// <summary>
		/// The list of SoundDef definitions<br />
		/// These sounds will be played once at the gate's jump node
		/// </summary>
		[ProtoMember(8)]
		[XmlArray("NodeSounds")]
		[XmlArrayItem("Sound")]
		public SoundDef[] NodeSounds = null;

		/// <summary>
		/// The list of SoundDef definitions<br />
		/// These sounds will be played once to entities currently being jumped
		/// </summary>
		[ProtoMember(9)]
		[XmlArray("TravelSounds")]
		[XmlArrayItem("Sound")]
		public SoundDef[] TravelSounds = null;

		/// <summary>
		/// The BeamPulseDef defining the beam pulse for this gate
		/// </summary>
		[ProtoMember(10)]
		[XmlElement("BeamPulse")]
		public BeamPulseDef BeamPulse = null;

		/// <summary>
		/// The DriveEmissiveColorDef defining the color for this gate's jump drive emitter emissives
		/// </summary>
		[ProtoMember(11)]
		[XmlElement("DriveEmissiveColor")]
		public DriveEmissiveColorDef DriveEmissiveColor = null;

		/// <summary>
		/// The NodePhysicsDef defining the attractor forces for this gate's jump node
		/// </summary>
		[ProtoMember(12)]
		[XmlElement("NodePhysics")]
		public NodePhysicsDef NodePhysics = null;

		/// <summary>
		/// The list of ParticleDef definitions for the gate's anti-node<br />
		/// These particles will be played once at the gate's anti-node<br />
		/// <i>The anti-node is the region at the endpoint of this gate</i>
		/// </summary>
		[ProtoMember(13)]
		[XmlArray("AntiNodeParticles")]
		[XmlArrayItem("Particle")]
		public ParticleDef[] AntiNodeParticles = null;

		/// <summary>
		/// The list of SoundDef definitions for the gate's anti-node<br />
		/// These sounds will be played once at the gate's anti-node<br />
		/// <i>The anti-node is the region at the endpoint of this gate</i>
		/// </summary>
		[ProtoMember(14)]
		[XmlArray("AntiNodeSounds")]
		[XmlArrayItem("Sound")]
		public SoundDef[] AntiNodeSounds = null;

		/// <summary>
		/// The NodePhysicsDef defining the attractor forces for this gate's anti-ode<br />
		/// <i>The anti-node is the region at the endpoint of this gate</i>
		/// </summary>
		[ProtoMember(15)]
		[XmlElement("AntiNodePhysics")]
		public NodePhysicsDef AntiNodePhysics = null;

		/// <summary>
		/// The DriveEntityLockDef defining the entity lock particles for this gate's drives
		/// </summary>
		[ProtoMember(16)]
		[XmlElement("DriveEntityLock")]
		public DriveEntityLockDef DriveEntityLock = null;
		#endregion

		#region Internal Methods
		/// <summary>
		/// Finalizes this animation<br />
		/// MyAnimation keyframes are sorted by position
		/// </summary>
		internal void Prepare()
		{

		}
		#endregion
	}

	/// <summary>
	/// Definition defining the "failed" phase animation
	/// </summary>
	[Serializable]
	[XmlRoot("JumpGateJumpedAnimationDef")]
	[ProtoContract(UseProtoMembersOnly = true)]
	public class JumpGateFailedAnimationDef
	{
		#region Public Variables
		/// <summary>
		/// The duration of the animaton in game ticks
		/// </summary>
		[ProtoMember(1)]
		[XmlElement("Duration")]
		public ushort Duration = 0;

		/// <summary>
		/// The list of particle definitions for each drive<br />
		/// These particles will each be played for every drive in the gate
		/// </summary>
		[ProtoMember(2)]
		[XmlArray("PerDriveParticles")]
		[XmlArrayItem("Particle")]
		public ParticleDef[] PerDriveParticles = null;

		/// <summary>
		/// The list of particle definitions for each anti drive<br />
		/// These particles will each be played for every drive in the targeted gate
		/// </summary>
		[ProtoMember(3)]
		[XmlArray("PerAntiDriveParticles")]
		[XmlArrayItem("Particle")]
		public ParticleDef[] PerAntiDriveParticles = null;

		/// <summary>
		/// The list of particle definitions for each jump space entity<br />
		/// These particles will each be played for every entity in the gate's jump space
		/// </summary>
		[ProtoMember(4)]
		[XmlArray("PerEntityParticles")]
		[XmlArrayItem("Particle")]
		public ParticleDef[] PerEntityParticles = null;

		/// <summary>
		/// The list of ParticleDef definitions for the gate's jump node<br />
		/// These particles will be played once at the gate's jump node
		/// </summary>
		[ProtoMember(5)]
		[XmlArray("NodeParticles")]
		[XmlArrayItem("Particle")]
		public ParticleDef[] NodeParticles = null;

		/// <summary>
		/// The list of SoundDef definitions<br />
		/// These sounds will be played once at the gate's jump node
		/// </summary>
		[ProtoMember(6)]
		[XmlArray("NodeSounds")]
		[XmlArrayItem("Sound")]
		public SoundDef[] NodeSounds = null;

		/// <summary>
		/// The DriveEmissiveColorDef defining the color for this gate's jump drive emitter emissives
		/// </summary>
		[ProtoMember(7)]
		[XmlElement("DriveEmissiveColor")]
		public DriveEmissiveColorDef DriveEmissiveColor = null;

		/// <summary>
		/// The NodePhysicsDef defining the attractor forces for this gate's jump node
		/// </summary>
		[ProtoMember(8)]
		[XmlElement("NodePhysics")]
		public NodePhysicsDef NodePhysics = null;

		/// <summary>
		/// The list of ParticleDef definitions for the gate's anti-node<br />
		/// These particles will be played once at the gate's anti-node<br />
		/// <i>The anti-node is the region at the endpoint of this gate</i>
		/// </summary>
		[ProtoMember(9)]
		[XmlArray("AntiNodeParticles")]
		[XmlArrayItem("Particle")]
		public ParticleDef[] AntiNodeParticles = null;

		/// <summary>
		/// The list of SoundDef definitions for the gate's anti-node<br />
		/// These sounds will be played once at the gate's anti-node<br />
		/// <i>The anti-node is the region at the endpoint of this gate</i>
		/// </summary>
		[ProtoMember(10)]
		[XmlArray("AntiNodeSounds")]
		[XmlArrayItem("Sound")]
		public SoundDef[] AntiNodeSounds = null;

		/// <summary>
		/// The NodePhysicsDef defining the attractor forces for this gate's anti-ode<br />
		/// <i>The anti-node is the region at the endpoint of this gate</i>
		/// </summary>
		[ProtoMember(11)]
		[XmlElement("AntiNodePhysics")]
		public NodePhysicsDef AntiNodePhysics = null;

		/// <summary>
		/// The DriveEntityLockDef defining the entity lock particles for this gate's drives
		/// </summary>
		[ProtoMember(12)]
		[XmlElement("DriveEntityLock")]
		public DriveEntityLockDef DriveEntityLock = null;
		#endregion

		#region Internal Methods
		/// <summary>
		/// Finalizes this animation<br />
		/// MyAnimation keyframes are sorted by position
		/// </summary>
		internal void Prepare()
		{

		}
		#endregion
	}

	/// <summary>
	/// Definition defining wormhole animations
	/// </summary>
	[Serializable]
	[XmlRoot("JumpGateWormholeAnimationDef")]
	[ProtoContract(UseProtoMembersOnly = true)]
	public class JumpGateWormholeAnimationDef
	{
		#region Public Variables
		/// <summary>
		/// The duration of the animaton in game ticks
		/// </summary>
		[ProtoMember(1)]
		[XmlElement("Duration")]
		public ushort Duration = 0;

		/// <summary>
		/// The list of particle definitions for each drive<br />
		/// These particles will each be played for every drive in the gate
		/// </summary>
		[ProtoMember(2)]
		[XmlArray("PerDriveParticles")]
		[XmlArrayItem("Particle")]
		public ParticleDef[] PerDriveParticles = null;

		/// <summary>
		/// The list of particle definitions for each anti drive<br />
		/// These particles will each be played for every drive in the targeted gate
		/// </summary>
		[ProtoMember(3)]
		[XmlArray("PerAntiDriveParticles")]
		[XmlArrayItem("Particle")]
		public ParticleDef[] PerAntiDriveParticles = null;

		/// <summary>
		/// The list of particle definitions for each jump space entity<br />
		/// These particles will each be played for every entity in the gate's jump space
		/// </summary>
		[ProtoMember(4)]
		[XmlArray("PerEntityParticles")]
		[XmlArrayItem("Particle")]
		public ParticleDef[] PerEntityParticles = null;

		/// <summary>
		/// The list of ParticleDef definitions for the gate's jump node<br />
		/// These particles will be played once at the gate's jump node
		/// </summary>
		[ProtoMember(5)]
		[XmlArray("NodeParticles")]
		[XmlArrayItem("Particle")]
		public ParticleDef[] NodeParticles = null;

		/// <summary>
		/// The list of SoundDef definitions<br />
		/// These sounds will be played once at the gate's jump node
		/// </summary>
		[ProtoMember(6)]
		[XmlArray("NodeSounds")]
		[XmlArrayItem("Sound")]
		public SoundDef[] NodeSounds = null;

		/// <summary>
		/// The DriveEmissiveColorDef defining the color for this gate's jump drive emitter emissives
		/// </summary>
		[ProtoMember(7)]
		[XmlElement("DriveEmissiveColor")]
		public DriveEmissiveColorDef DriveEmissiveColor = null;

		/// <summary>
		/// The NodePhysicsDef defining the attractor forces for this gate's jump node
		/// </summary>
		[ProtoMember(8)]
		[XmlElement("NodePhysics")]
		public NodePhysicsDef NodePhysics = null;

		/// <summary>
		/// The list of ParticleDef definitions for the gate's anti-node<br />
		/// These particles will be played once at the gate's anti-node<br />
		/// <i>The anti-node is the region at the endpoint of this gate</i>
		/// </summary>
		[ProtoMember(9)]
		[XmlArray("AntiNodeParticles")]
		[XmlArrayItem("Particle")]
		public ParticleDef[] AntiNodeParticles = null;

		/// <summary>
		/// The list of SoundDef definitions for the gate's anti-node<br />
		/// These sounds will be played once at the gate's anti-node<br />
		/// <i>The anti-node is the region at the endpoint of this gate</i>
		/// </summary>
		[ProtoMember(10)]
		[XmlArray("AntiNodeSounds")]
		[XmlArrayItem("Sound")]
		public SoundDef[] AntiNodeSounds = null;

		/// <summary>
		/// The NodePhysicsDef defining the attractor forces for this gate's anti-node<br />
		/// <i>The anti-node is the region at the endpoint of this gate</i>
		/// </summary>
		[ProtoMember(11)]
		[XmlElement("AntiNodePhysics")]
		public NodePhysicsDef AntiNodePhysics = null;

		/// <summary>
		/// The DriveEntityLockDef defining the entity lock particles for this gate's drives
		/// </summary>
		[ProtoMember(12)]
		[XmlElement("DriveEntityLock")]
		public DriveEntityLockDef DriveEntityLock = null;

		/// <summary>
		/// The shape colliders to apply during this animation for this gate's jump node
		/// </summary>
		[ProtoMember(13)]
		[XmlArray("NodeShapeColliders")]
		[XmlArrayItem("ShapeCollider")]
		public ShapeColliderDef[] NodeShapeColliders = null;

		/// <summary>
		/// The shape colliders to apply during this animation for the targate gate's jump node<br />
		/// <i>The anti-node is the region at the endpoint of this gate</i>
		/// </summary>
		[ProtoMember(15)]
		[XmlArray("AntiNodeShapeColliders")]
		[XmlArrayItem("ShapeCollider")]
		public ShapeColliderDef[] AntiNodeShapeColliders = null;
		#endregion

		#region Internal Methods
		/// <summary>
		/// Finalizes this animation<br />
		/// MyAnimation keyframes are sorted by position
		/// </summary>
		internal void Prepare()
		{

		}
		#endregion
	}

	/// <summary>
	/// Definition defining an entire gate animation
	/// </summary>
	[Serializable]
	[XmlRoot("AnimationDef")]
	[ProtoContract(UseProtoMembersOnly = true)]
	public class AnimationDef
	{
		#region Internal Variables
		/// <summary>
		/// Whether to serialize this animation to XML after session unload
		/// </summary>
		[ProtoMember(1)]
		[XmlElement("SerializeOnEnd")]
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
		[XmlElement("SubtypeID")]
		internal ulong? SubtypeID = null;

		/// <summary>
		/// The weight of this animation<br />
		/// A random animation will be selected from the animations with this ID that pass the constraint check<br />
		/// This value controls how often this animation is selected based on the total weights
		/// </summary>
		[ProtoMember(12)]
		[XmlElement("RandomWeight")]
		internal byte RandomWeight = 1;
		#endregion

		#region Public Variables
		/// <summary>
		/// Whether this animation is enabled<br />
		/// Disabled animations are not shown in the controller list
		/// </summary>
		[ProtoMember(4)]
		[XmlElement("Enabled")]
		public bool Enabled = true;

		/// <summary>
		/// Whether this animation can be cancelled immediatly<br />
		/// If false, animation in the jumping phase will cancel once complete
		/// </summary>
		[ProtoMember(5)]
		[XmlElement("ImmediateCancel")]
		public bool ImmediateCancel = true;

		/// <summary>
		/// The name of this animation
		/// </summary>
		[ProtoMember(6)]
		[XmlElement("AnimationName")]
		public string AnimationName;

		/// <summary>
		/// The description of this animation
		/// </summary>
		[ProtoMember(7)]
		[XmlElement("Description")]
		public string Description;

		/// <summary>
		/// The JumpGateJumpingAnimationDef definition defining the jumping phase of this animation
		/// </summary>
		[ProtoMember(8)]
		[XmlElement("JumpingAnimationDef")]
		public JumpGateJumpingAnimationDef JumpingAnimationDef = null;

		/// <summary>
		/// The JumpGateJumpedAnimationDef definition defining the jumped phase of this animation
		/// </summary>
		[ProtoMember(9)]
		[XmlElement("JumpedAnimationDef")]
		public JumpGateJumpedAnimationDef JumpedAnimationDef = null;

		/// <summary>
		/// The JumpGateFailedAnimationDef definition defining the failed phase of this animation
		/// </summary>
		[ProtoMember(10)]
		[XmlElement("FailedAnimationDef")]
		public JumpGateFailedAnimationDef FailedAnimationDef = null;

		/// <summary>
		/// The JumpGateWormholeAnimationDef definition defining the wormhole opening phase of this animation
		/// </summary>
		[ProtoMember(11)]
		[XmlElement("WormholeOpenAnimationDef")]
		public JumpGateWormholeAnimationDef WormholeOpenAnimationDef = null;

		/// <summary>
		/// The JumpGateWormholeAnimationDef definition defining the wormhole loop phase of this animation
		/// </summary>
		[ProtoMember(12)]
		[XmlElement("WormholeLoopAnimationDef")]
		public JumpGateWormholeAnimationDef WormholeLoopAnimationDef = null;

		/// <summary>
		/// The JumpGateWormholeAnimationDef definition defining the wormhole closing phase of this animation
		/// </summary>
		[ProtoMember(13)]
		[XmlElement("WormholeCloseAnimationDef")]
		public JumpGateWormholeAnimationDef WormholeCloseAnimationDef = null;

		/// <summary>
		/// The AnimationConstraintDef definition defining a jump gate constraint for this animation
		/// </summary>
		[ProtoMember(14)]
		[XmlArray("AnimationConstraints")]
		[XmlArrayItem("Constraint")]
		public AnimationConstraintDef[] AnimationConstraints = null;
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
		/// <param name="random_weight">Random weight used to control random change this animation is selected (higher = more likely)</param>
		public AnimationDef(string name, string description = null, bool serialize = false, byte random_weight = 1)
		{
			this.AnimationName = name;
			this.Description = description;
			this.SerializeOnEnd = serialize;
			this.RandomWeight = random_weight;
			MyAnimationHandler.AddAnimationDefinition(this);
		}
		#endregion

		#region Internal Methods
		/// <summary>
		/// Finalizes this animation<br />
		/// MyAnimation keyframes are sorted by position
		/// </summary>
		internal void Prepare()
		{
			this.JumpingAnimationDef?.Prepare();
			this.JumpedAnimationDef?.Prepare();
			this.FailedAnimationDef?.Prepare();
		}

		/// <returns>Whether this animation is a wormhole animation</returns>
		internal bool IsWormholeAnimation()
		{
			return this.WormholeOpenAnimationDef != null || this.WormholeLoopAnimationDef != null || this.WormholeCloseAnimationDef != null;
		}
		#endregion
	}
}
