namespace IOTA.ModularJumpGates.Animation
{
	/// <summary>
	/// Enum representing the type of easing
	/// </summary>
	public enum EasingTypeEnum : byte
	{
		EASE_IN,
		EASE_OUT,
		EASE_IN_OUT
	};

	/// <summary>
	/// Enum representing the easing curve
	/// </summary>
	public enum EasingCurveEnum : byte
	{
		CONSTANT,
		LINEAR,
		QUADRATIC,
		CUBIC,
		QUARTIC,
		QUINTIC,
		EXPONENTIAL,
		SINE,
		BOUNCE,
		BACK,
		CIRCULAR,
		ELASTiC
	};

	/// <summary>
	/// Bit flag representing the independent variables by which to animate
	/// </summary>
	public enum AnimationSourceEnum : byte
	{
		/// <summary>
		/// Attribute is not animated and uses the specified fixed initial value
		/// </summary>
		FIXED,

		/// <summary>
		/// Attribute is animated over time
		/// </summary>
		TIME,

		/// <summary>
		/// Attribute is modified based on this jump gate's size (size of jump space in meters)<br />
		/// The value is the jump gate size in meters
		/// </summary>
		JUMP_GATE_SIZE,

		/// <summary>
		/// Attribute is modified based on this jump gate's height (height of jump space in meters)<br />
		/// The value is the jump gate height in meters
		/// </summary>
		JUMP_GATE_HEIGHT,

		/// <summary>
		/// Attribute is modified based on this jump gate's radii (size of jump space axes in meters)<br />
		/// For double values: The value is the length of the jump gate radii vector<br />
		/// For vector values: The value is the jump gate radii in meters
		/// </summary>
		JUMP_GATE_RADII,

		/// <summary>
		/// Attribute is modified based on this jump gate's velocity (velovity of jump node in meters per second)<br />
		/// For double values: The value is the length of the velocity vector<br />
		/// For vector values: The value is the jump gate velocity in meters
		/// </summary>
		JUMP_GATE_VELOCITY,

		/// <summary>
		/// Attribute is modified based on the target jump gate's size (size of jump space in meters)<br />
		/// If the target is not a jump gate, this value defaults to "JUMP_GATE_SIZE"<br />
		/// The value is the jump gate size in meters
		/// </summary>
		JUMP_ANTIGATE_SIZE,

		/// <summary>
		/// Attribute is modified based on the target jump gate's height (height of jump space in meters)<br />
		/// If the target is not a jump gate, this value defaults to "JUMP_GATE_HEIGHT"<br />
		/// The value is the jump gate height in meters
		/// </summary>
		JUMP_ANTIGATE_HEIGHT,

		/// <summary>
		/// Attribute is modified based on this jump gate's radii (size of jump space axes in meters)<br />
		/// For double values: The value is the length of the jump gate radii vector<br />
		/// For vector values: The value is the jump gate radii in meters
		/// </summary>
		JUMP_ANTIGATE_RADII,

		/// <summary>
		/// Attribute is modified based on the target jump gate's velocity (velovity of jump node in meters per second)<br />
		/// For double values: The value is the length of the velocity vector<br />
		/// For vector values: The value is the jump gate velocity in meters
		/// </summary>
		JUMP_ANTIGATE_VELOCITY,

		/// <summary>
		/// Attribute is modified based on the normal vector between this gate and the destination<br />
		/// The resulting vector is a normalized vector pointing from this gate to this gate's destination<br />
		/// For double values: The value is one<br />
		/// For vector values: The value is the normalized normal
		/// </summary>
		JUMP_NORMAL,

		/// <summary>
		/// Attribute is modified based on a random value rolled every tick<br />
		/// The value will be between the two specified values
		/// </summary>
		RANDOM,

		/// <summary>
		/// Attribute is modified based on the number of entities within the jump space<br />
		/// The value will be the number of entities within the jump space
		/// </summary>
		N_ENTITIES,

		/// <summary>
		/// Attribute is modified based on the number of drives this gate has<br />
		/// The value will be the total number of gate drives
		/// </summary>
		N_DRIVES,

		/// <summary>
		/// Attribute is modified based on the distance from this entity to the gate's endpoint (in meters)<br />
		/// <i>Only effective for per-entity particles or per-drive particles</i><br />
		/// The value will be the distance from this entity to the gate's endpoint in meters
		/// </summary>
		DISTANCE_ENTITY_TO_ENDPOINT,

		/// <summary>
		/// Attribute is modified based on the distance from this gate to this gate's endpoint (in meters)<br />
		/// The value will be the distance from this gate to this gate's endpoint in meters
		/// </summary>
		DISTANCE_GATE_TO_ENDPOINT,

		/// <summary>
		/// Attribute is modified based on the distance from this entity to the gate's jump node (in meters)<br />
		/// <i>Only effective for per-entity particles or per-drive particles</i><br />
		/// The value will be the distance from this entity to the gate's jump node in meters
		/// </summary>
		DISTANCE_ENTITY_TO_GATE,

		/// <summary>
		/// Attribute is modified based on the distance from this entity to the nearest jump space entity (in meters)<br />
		/// <i>Only effective for per-entity particles or per-drive particles</i><br />
		/// The value will be the distance from this entity to the nearest jump space entity in meters
		/// </summary>
		DISTANCE_ENTITY_TO_ENTITY,

		/// <summary>
		/// Attribute is modified based on the distance from this particle's position to the specified jump space entity (in meters)<br />
		/// <i>Only effective for per-entity particles, per-drive particles, and entity lock particles</i><br />
		/// The value will be the distance from this particle's position to the target entity in meters
		/// </summary>
		DISTANCE_THIS_TO_ENTITY,

		/// <summary>
		/// Attribute is modified based on this entity's velocity
		/// </summary>
		ENTITY_VELOCITY,

		/// <summary>
		/// Attribute is modified based on this entity's volume<br />
		/// The value is the volume of this entity
		/// </summary>
		ENTITY_VOLUME,

		/// <summary>
		/// Attribute is modified based on this entity's mass<br />
		/// The value is the mass of this entity
		/// </summary>
		ENTITY_MASS,

		/// <summary>
		/// Attribute is modified based on this entity's bounding box extents
		/// </summary>
		ENTITY_EXTENTS,

		/// <summary>
		/// Attribute is modified based on this entity's bounding box extents projected onto the endpoint plane
		/// </summary>
		ENTITY_FACING_EXTENTS,

		/// <summary>
		/// Attribute is modified based on current planetary gravity<br />
		/// The value will be the length of the current gravity vector
		/// </summary>
		GRAVITY,

		/// <summary>
		/// Attribute is modified based on current atmosphere density<br />
		/// The value will be the ratio between the current atlitude and the planet's atmospheric radius
		/// </summary>
		ATMOSPHERE,
	};

	/// <summary>
	/// Enum representing the operation by which values should be joined
	/// </summary>
	public enum MathOperationEnum : byte
	{
		ADD,
		SUBTRACT,
		MULTIPLY,
		DIVIDE,
		MODULO,
		POWER,
		AVERAGE,
		SMALLEST,
		LARGEST,
		FUNCTION,
		CLAMP,
		LENGTH,
		VMIN,
		VMAX,
	};

	/// <summary>
	/// Enum representing the method by which values should be ratioed
	/// </summary>
	public enum RatioTypeEnum : byte
	{
		/// <summary>
		/// The standard value
		/// </summary>
		NONE,

		/// <summary>
		/// Relation between this animation's start tick and end tick
		/// </summary>
		TIME,

		/// <summary>
		/// Relation between this entity's position and the gate's endpoint
		/// </summary>
		ENDPOINT_DISTANCE,

		/// <summary>
		/// Relation between this entity's position and the gate's jump node
		/// </summary>
		ENTITY_DISTANCE,

		/// <summary>
		/// Relation between the atmosphere density at this entity's position and 1
		/// </summary>
		ATMOSPHERE,

		/// <summary>
		/// Relation between gravity strength at this entity's position and 1
		/// </summary>
		GRAVITY,

		/// <summary>
		/// Random relation between 0 and 1
		/// </summary>
		RANDOM,
	}

	/// <summary>
	/// Enum representing how to orient particles
	/// </summary>
	public enum ParticleOrientationEnum : byte
	{
		/// <summary>
		/// Particles are oriented according to the gate's endpoint
		/// </summary>
		GATE_ENDPOINT_NORMAL,

		/// <summary>
		/// Particles are oriented according to the gate's endpoint and not affected by the gate's normal override
		/// </summary>
		GATE_TRUE_ENDPOINT_NORMAL,

		/// <summary>
		/// Particles are oriented according to the gate's primary plane
		/// </summary>
		GATE_DRIVE_NORMAL,

		/// <summary>
		/// Particles are oriented according to the target gate's primary plane<br />
		/// If target is not a gate, this defaults to "GATE_ENDPOINT_NORMAL"
		/// </summary>
		ANTIGATE_DRIVE_NORMAL,

		/// <summary>
		/// Particles use a fixed matrix
		/// </summary>
		FIXED
	};

	public enum EntityLockTypeEnum : byte
	{
		/// <summary>
		/// Locks the entities closest to the gate's jump node
		/// </summary>
		LOCK_NEAREST,

		/// <summary>
		/// Locks the entities farthest from the gate's jump node
		/// </summary>
		LOCK_FARTHEST,

		/// <summary>
		/// Locks a random set of entities within the jump space
		/// </summary>
		LOCK_RANDOM,
	}

	public enum LockDelayShiftEnum : byte
	{
		/// <summary>
		/// All drives use the same lock times<br />
		/// Lock time will be the average of min and max
		/// </summary>
		FIXED,

		/// <summary>
		/// Drives will use a lock time based on their distance to the jump node<br />
		/// The closest drive will use the minimum lock time, the farthest drive will use the maximum lock time
		/// </summary>
		NEAREST,

		/// <summary>
		/// Drives will use a lock time based on their distance to the jump node<br />
		/// The farthest drive will use the minimum lock time, the closest drive will use the maximum lock time
		/// </summary>
		FARTHEST,

		/// <summary>
		/// Drives will use a random lock time between min and max
		/// </summary>
		RANDOM,
	}

	public enum CollisionShapeEnum : byte { NONE, ELLIPSOID, CUBE, CYLINDER }

	public enum CollisionEffectTypeEnum : byte
	{
		NONE,

		/// <summary>
		/// Damages all entities inside this collider<br />
		/// Effect arguments:<br />
		///  ... 0: Tick modulo (only ticked every n ticks)<br />
		///  ... 1: Damage per tick<br />
		/// </summary>
		DAMAGE,

		/// <summary>
		/// Jumps all entities inside this collider<br />
		/// Effect arguments:<br />
		///  ... 0: Tick modulo (only ticked every n ticks)<br />
		/// </summary>
		JUMP,

		/// <summary>
		/// Deletes all entities inside this collider
		/// Effect arguments:<br />
		///  ... 0: Tick modulo (only ticked every n ticks)<br />
		/// </summary>
		DELETE,
	}
}
