using IOTA.ModularJumpGates.Animations;
using IOTA.ModularJumpGates.CubeBlock;
using IOTA.ModularJumpGates.Util;
using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.VisualScripting.Utils;
using VRage.Utils;
using VRageMath;
using Sandbox.Game.Entities;
using Sandbox.Engine.Physics;
using VRage.Game.Components;
using IOTA.ModularJumpGates.Extensions;
using VRageRender;

namespace IOTA.ModularJumpGates.Animations
{
	internal partial class AnimationDefinitions
	{
		
	}
}

namespace IOTA.ModularJumpGates
{
	#region Enums
	/// <summary>
	/// Enum representing the type of easing
	/// </summary>
	public enum EasingTypeEnum {
		EASE_IN,
		EASE_OUT,
		EASE_IN_OUT
	};

	/// <summary>
	/// Enum representing the easing curve
	/// </summary>
	public enum EasingCurveEnum {
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
	public enum AnimationSourceEnum : byte {
		/// <summary>
		/// Attribute is not animated and uses the specified fixed value<br />
		/// The value is the average of "from" and "to"
		/// </summary>
		FIXED,

		/// <summary>
		/// Attribute is animated over time<br />
		/// The value is animated from "from" to "to"
		/// </summary>
		TIME,

		/// <summary>
		/// Attribute is modified based on this jump gate's size (size of jump space in meters)<br />
		/// The value is the jump gate size in meters
		/// </summary>
		JUMP_GATE_SIZE,

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
		/// Attribute is modified based on this entity's velocity<br />
		/// For double values: The value is the length of the velocity vector<br />
		/// For vector values: The value is the jump gate velocity in meters
		/// </summary>
		ENTITY_VELOCITY,

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
	internal enum MathOperationEnum {
		/// <summary>
		/// All modifiers of the same type will be added
		/// </summary>
		ADD,

		/// <summary>
		/// All modifiers of the same type will be subtracted
		/// </summary>
		SUBTRACT,

		/// <summary>
		/// All modifiers of the same type will be multiplied
		/// </summary>
		MULTIPLY,

		/// <summary>
		/// All modifiers of the same type will be divided
		/// </summary>
		DIVIDE,

		/// <summary>
		/// All modifiers of the same type will be modulo'd
		/// </summary>
		MODULO,

		/// <summary>
		/// All modifiers of the same type will be exponentiated
		/// </summary>
		POWER,

		/// <summary>
		/// All modifiers of the same type will be averaged
		/// </summary>
		AVERAGE,

		/// <summary>
		/// The smallest result will be used from all modifiers of the same type
		/// </summary>
		SMALLEST,

		/// <summary>
		/// The largest result will be used from all modifiers of the same type
		/// </summary>
		LARGEST
	};

	/// <summary>
	/// Enum representing the method by which values should be ratioed
	/// </summary>
	public enum RatioTypeEnum
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
	public enum ParticleOrientationEnum {
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
	#endregion

	#region Animation Util Classes
	/// <summary>
	/// Static class calculating various ratios for different easing types and curves
	/// </summary>
	internal static class EasingFunctor
	{
		#region Private Static Methods
		/// <summary>
		/// Returns the ratio for ease-out and bounce curve
		/// </summary>
		/// <param name="x">The input ratio</param>
		/// <returns>The result ratio</returns>
		private static double EaseOutBounce(double x)
		{
			double n1 = 7.5625;
			double d1 = 2.75;

			if (x < 1 / d1)
			{
				return n1 * x * x;
			}
			else if (x < 2 / d1)
			{
				return n1 * (x -= 1.5 / d1) * x + 0.75;
			}
			else if (x < 2.5 / d1)
			{
				return n1 * (x -= 2.25 / d1) * x + 0.9375;
			}
			else
			{
				return n1 * (x -= 2.625 / d1) * x + 0.984375;
			}
		}
		#endregion

		#region Public Static Methods
		/// <summary>
		/// Gets the result ratio of an easing operation
		/// </summary>
		/// <param name="x">The input ratio</param>
		/// <param name="easing">The easing type</param>
		/// <param name="curve">The easing curve</param>
		/// <returns>The eased ratio</returns>
		public static double GetEaseResult(double x, EasingTypeEnum easing, EasingCurveEnum curve)
		{
			switch (curve)
			{
				case EasingCurveEnum.CONSTANT:
					return (x >= 0.5) ? 1 : 0;
				case EasingCurveEnum.LINEAR:
					return x;
				case EasingCurveEnum.QUADRATIC:
					if (easing == EasingTypeEnum.EASE_IN) return x * x;
					else if (easing == EasingTypeEnum.EASE_OUT) return 1 - Math.Pow(1 - x, 2);
					else if (easing == EasingTypeEnum.EASE_IN_OUT) return x < 0.5 ? 2 * x * x : 1 - Math.Pow(-2 * x + 2, 2) / 2;
					else return -1;
				case EasingCurveEnum.CUBIC:
					if (easing == EasingTypeEnum.EASE_IN) return x * x * x;
					else if (easing == EasingTypeEnum.EASE_OUT) return 1 - Math.Pow(1 - x, 3);
					else if (easing == EasingTypeEnum.EASE_IN_OUT) return x < 0.5 ? 4 * x * x * x : 1 - Math.Pow(-2 * x + 2, 3) / 2;
					else return -1;
				case EasingCurveEnum.QUARTIC:
					if (easing == EasingTypeEnum.EASE_IN) return x * x * x * x;
					else if (easing == EasingTypeEnum.EASE_OUT) return 1 - Math.Pow(1 - x, 4);
					else if (easing == EasingTypeEnum.EASE_IN_OUT) return x < 0.5 ? 4 * x * x * x * x : 1 - Math.Pow(-2 * x + 2, 4) / 2;
					else return -1;
				case EasingCurveEnum.QUINTIC:
					if (easing == EasingTypeEnum.EASE_IN) return x * x * x * x * x;
					else if (easing == EasingTypeEnum.EASE_OUT) return 1 - Math.Pow(1 - x, 5);
					else if (easing == EasingTypeEnum.EASE_IN_OUT) return x < 0.5 ? 4 * x * x * x * x * x : 1 - Math.Pow(-2 * x + 2, 5) / 2;
					else return -1;
				case EasingCurveEnum.EXPONENTIAL:
					if (easing == EasingTypeEnum.EASE_IN) return x == 0 ? 0 : Math.Pow(2, 10 * x - 10);
					else if (easing == EasingTypeEnum.EASE_OUT) return x == 1 ? 1 : 1 - Math.Pow(2, -10 * x);
					else if (easing == EasingTypeEnum.EASE_IN_OUT) return (x == 0) ? 0 : (x == 1) ? 1 : x < 0.5 ? Math.Pow(2, 20 * x - 10) / 2 : (2 - Math.Pow(2, -20 * x + 10)) / 2;
					else return -1;
				case EasingCurveEnum.SINE:
					if (easing == EasingTypeEnum.EASE_IN) return 1 - Math.Cos((x * Math.PI) / 2);
					else if (easing == EasingTypeEnum.EASE_OUT) return Math.Sin((x * Math.PI) / 2);
					else if (easing == EasingTypeEnum.EASE_IN_OUT) return -(Math.Cos(Math.PI * x) - 1) / 2;
					else return -1;
				case EasingCurveEnum.BOUNCE:
					if (easing == EasingTypeEnum.EASE_IN) return 1 - EasingFunctor.EaseOutBounce(1 - x);
					else if (easing == EasingTypeEnum.EASE_OUT) return EasingFunctor.EaseOutBounce(x);
					else if (easing == EasingTypeEnum.EASE_IN_OUT) return (x < 0.5) ? (1 - EasingFunctor.EaseOutBounce(1 - 2 * x)) / 2 : (1 + EasingFunctor.EaseOutBounce(2 * x - 1)) / 2;
					else return -1;
				case EasingCurveEnum.BACK:
					if (easing == EasingTypeEnum.EASE_IN)
					{
						double c1 = 1.70158;
						double c3 = c1 + 1;
						return c3 * x * x * x - c1 * x * x;
					}
					else if (easing == EasingTypeEnum.EASE_OUT)
					{
						double c1 = 1.70158;
						double c3 = c1 + 1;
						return 1 + c3 * Math.Pow(x - 1, 3) + c1 * Math.Pow(x - 1, 2);
					}
					else if (easing == EasingTypeEnum.EASE_IN_OUT)
					{
						double c1 = 1.70158;
						double c2 = c1 * 1.525;
						return (x < 0.5) ? (Math.Pow(2 * x, 2) * ((c2 + 1) * 2 * x - c2)) / 2 : (Math.Pow(2 * x - 2, 2) * ((c2 + 1) * (x * 2 - 2) + c2) + 2) / 2;
					}
					else return -1;
				case EasingCurveEnum.CIRCULAR:
					if (easing == EasingTypeEnum.EASE_IN) return 1 - Math.Sqrt(1 - Math.Pow(x, 2));
					else if (easing == EasingTypeEnum.EASE_OUT) return Math.Sqrt(1 - Math.Pow(x - 1, 2));
					else if (easing == EasingTypeEnum.EASE_IN_OUT) return (x < 0.5) ? (1 - Math.Sqrt(1 - Math.Pow(2 * x, 2))) / 2 : (Math.Sqrt(1 - Math.Pow(-2 * x + 2, 2)) + 1) / 2;
					else return -1;
				case EasingCurveEnum.ELASTiC:
					if (easing == EasingTypeEnum.EASE_IN) return (x == 0) ? 0 : (x == 1) ? 1 : -Math.Pow(2, 10 * x - 10) * Math.Sin((x * 10 - 10.75) * ((2 * Math.PI) / 3));
					else if (easing == EasingTypeEnum.EASE_OUT) return (x == 0) ? 0 : (x == 1) ? 1 : Math.Pow(2, -10 * x) * Math.Sin((x * 10 - 0.75) * ((2 * Math.PI) / 3)) + 1;
					else if (easing == EasingTypeEnum.EASE_IN_OUT)
					{
						double c5 = (2 * Math.PI) / 4.5;
						return (x == 0) ? 0 : (x == 1) ? 1 : x < 0.5 ? -(Math.Pow(2, 20 * x - 10) * Math.Sin((20 * x - 11.125) * c5)) / 2 : (Math.Pow(2, -20 * x + 10) * Math.Sin((20 * x - 11.125) * c5)) / 2 + 1;
					}
					else return -1;
				default:
					return -1;
			}
		}

		/// <summary>
		/// Gets the result ratio of an easing operation
		/// </summary>
		/// <param name="x">The input ratio</param>
		/// <param name="easing">The easing type</param>
		/// <param name="curve">The easing curve</param>
		/// <returns>The eased ratio</returns>
		public static Vector4D GetEaseResult(Vector4D x, EasingTypeEnum easing, EasingCurveEnum curve)
		{
			return new Vector4D(
				EasingFunctor.GetEaseResult(x.X, easing, curve),
				EasingFunctor.GetEaseResult(x.Y, easing, curve),
				EasingFunctor.GetEaseResult(x.Z, easing, curve),
				EasingFunctor.GetEaseResult(x.W, easing, curve)
			);
		}

		/// <summary>
		/// Gets the result ratio of an easing operation
		/// </summary>
		/// <param name="x">The input ratio</param>
		/// <param name="easing">The easing type</param>
		/// <param name="curve">The easing curve</param>
		/// <returns>The eased ratio</returns>
		public static Vector3D GetEaseResult(Vector3D x, EasingTypeEnum easing, EasingCurveEnum curve)
		{
			return new Vector3D(
				EasingFunctor.GetEaseResult(x.X, easing, curve),
				EasingFunctor.GetEaseResult(x.Y, easing, curve),
				EasingFunctor.GetEaseResult(x.Z, easing, curve)
			);
		}
		#endregion
	}

	[ProtoContract]
	internal class DoubleValue
	{
		[ProtoMember(1, IsRequired = true)]
		public MathOperationEnum Operation = MathOperationEnum.ADD;
		[ProtoMember(2, IsRequired = true)]
		public double InitialValue;
		[ProtoMember(3, IsRequired = true)]
		public AnimationSourceEnum AnimationSource;
		[ProtoMember(4, IsRequired = true)]
		public double LowerClamp = double.NegativeInfinity;
		[ProtoMember(5, IsRequired = true)]
		public double UpperClamp = double.PositiveInfinity;
		[ProtoMember(6, IsRequired = true)]
		public bool ClampResult = false;
		[ProtoMember(7, IsRequired = true)]
		internal RatioTypeEnum RatioType = RatioTypeEnum.NONE;

		/// <summary>
		/// Parameterless constructor for serialization
		/// </summary>
		internal DoubleValue() { }

		public DoubleValue(double initial_value, MathOperationEnum operation, RatioTypeEnum ratio_type = RatioTypeEnum.NONE, double? lower = null, double? upper = null)
		{
			this.InitialValue = initial_value;
			this.Operation = operation;
			this.AnimationSource = AnimationSourceEnum.FIXED;
			this.RatioType = ratio_type;
			this.LowerClamp = lower ?? double.NegativeInfinity;
			this.UpperClamp = upper ?? double.PositiveInfinity;
			this.ClampResult = lower != null || upper != null;
		}

		public DoubleValue(AnimationSourceEnum animation_source, MathOperationEnum operation, RatioTypeEnum ratio_type = RatioTypeEnum.NONE, double? lower = null, double? upper = null)
		{
			this.InitialValue = 0;
			this.Operation = operation;
			this.AnimationSource = animation_source;
			this.RatioType = ratio_type;
			this.LowerClamp = lower ?? double.NegativeInfinity;
			this.UpperClamp = upper ?? double.PositiveInfinity;
			this.ClampResult = lower != null || upper != null;
		}

		public double GetValue(ushort current_tick, ushort total_duration, MyJumpGate jump_gate, MyJumpGate target_gate, List<MyJumpGateDrive> drives, List<MyEntity> entities, ref Vector3D endpoint, Vector3D? this_position, MyEntity this_entity)
		{
			Vector3D jump_node = jump_gate.WorldJumpNode;
			double result, ratio;

			switch (this.AnimationSource)
			{
				case AnimationSourceEnum.FIXED:
					result = this.InitialValue;
					break;
				case AnimationSourceEnum.TIME:
					result = (double) current_tick / (double) total_duration;
					break;
				case AnimationSourceEnum.JUMP_GATE_SIZE:
					result = (jump_gate.Closed) ? 0 : jump_gate.JumpNodeRadius();
					break;
				case AnimationSourceEnum.JUMP_GATE_RADII:
					result = (jump_gate.Closed) ? 0 : jump_gate.JumpEllipse.Radii.Length();
					break;
				case AnimationSourceEnum.JUMP_GATE_VELOCITY:
					result = (jump_gate.Closed) ? 0 : jump_gate.JumpNodeVelocity.Length();
					break;
				case AnimationSourceEnum.JUMP_ANTIGATE_SIZE:
					result = (target_gate != null && !target_gate.Closed) ? target_gate.JumpNodeRadius() : ((jump_gate.Closed) ? 0 : jump_gate.JumpNodeRadius());
					break;
				case AnimationSourceEnum.JUMP_ANTIGATE_RADII:
					result = (target_gate != null && !target_gate.Closed) ? target_gate.JumpEllipse.Radii.Length() : ((jump_gate.Closed) ? 0 : jump_gate.JumpEllipse.Radii.Length());
					break;
				case AnimationSourceEnum.JUMP_ANTIGATE_VELOCITY:
					result = (target_gate != null && !target_gate.Closed) ? target_gate.JumpNodeVelocity.Length() : 0;
					break;
				case AnimationSourceEnum.RANDOM:
					result = new Random().NextDouble();
					break;
				case AnimationSourceEnum.N_ENTITIES:
					result = entities.Count;
					break;
				case AnimationSourceEnum.N_DRIVES:
					result = drives.Count;
					break;
				case AnimationSourceEnum.DISTANCE_ENTITY_TO_ENDPOINT:
					result = (this_position == null) ? 0 : Vector3D.Distance(endpoint, this_position.Value);
					break;
				case AnimationSourceEnum.DISTANCE_GATE_TO_ENDPOINT:
					result = (jump_gate.Closed) ? 0 : Vector3D.Distance(jump_node, endpoint);
					break;
				case AnimationSourceEnum.DISTANCE_ENTITY_TO_GATE:
					result = (this_position == null || jump_gate.Closed) ? 0 : Vector3D.Distance(jump_node, this_position.Value);
					break;
				case AnimationSourceEnum.DISTANCE_ENTITY_TO_ENTITY:
				{
					if (this_position == null || entities.Count == 0) result = 0;
					else
					{
						result = double.MaxValue;

						foreach (MyEntity entity in entities)
						{
							double distance = Vector3D.Distance(entity.WorldMatrix.Translation, this_position.Value);
							result = (distance > 0) ? Math.Min(result, distance) : result;
						}
					}
					
					break;
				}
				case AnimationSourceEnum.ENTITY_VELOCITY:
					result = (this_entity == null && jump_gate.Closed) ? 0 : (this_entity?.Physics?.LinearVelocity ?? jump_gate.JumpNodeVelocity).Length();
					break;
				case AnimationSourceEnum.GRAVITY:
					result = (jump_gate.Closed) ? 0 : MyAPIGateway.GravityProviderSystem.CalculateNaturalGravityInPoint(jump_node).Length();
					break;
				case AnimationSourceEnum.ATMOSPHERE:
				{
					MyPlanet planet = (jump_gate.Closed) ? null : MyGamePruningStructure.GetClosestPlanet(jump_node);
					result = (planet == null || !planet.HasAtmosphere) ? 0 : Math.Max(0, 1 - (Vector3D.Distance(jump_node, planet.WorldMatrix.Translation) / (double) planet.AtmosphereRadius));
					break;
				}
				default:
					throw new InvalidOperationException($"Invalid animation source - {(byte) this.AnimationSource}");
			}

			switch (this.RatioType)
			{
				case RatioTypeEnum.NONE:
					return (this.ClampResult) ? MathHelper.Clamp(result, this.LowerClamp, this.UpperClamp) : result;
				case RatioTypeEnum.TIME:
					ratio = (double) current_tick / total_duration;
					break;
				case RatioTypeEnum.ENDPOINT_DISTANCE:
					ratio = (this_position == null) ? 0.5 : (1 - (Vector3D.Distance(this_position.Value, endpoint) / Vector3D.Distance(jump_node, endpoint)));
					break;
				case RatioTypeEnum.ENTITY_DISTANCE:
					ratio = (this_position == null) ? 0.5 : (1 - (Vector3D.Distance(this_position.Value, jump_node) / entities.Max((entity) => Vector3D.Distance(entity.WorldMatrix.Translation, jump_node))));
					break;
				case RatioTypeEnum.ATMOSPHERE:
				{
					MyPlanet planet = (this_position == null) ? null : MyGamePruningStructure.GetClosestPlanet(this_position.Value);
					ratio = (planet == null || !planet.HasAtmosphere) ? 0.5 : Math.Max(0, 1 - (Vector3D.Distance(jump_node, planet.WorldMatrix.Translation) / (double) planet.AtmosphereRadius));
					break;
				}
				case RatioTypeEnum.GRAVITY:
					ratio = (this_position == null) ? 0.5 : MyAPIGateway.GravityProviderSystem.CalculateNaturalGravityInPoint(jump_node).Length();
					break;
				case RatioTypeEnum.RANDOM:
					ratio = new Random().NextDouble();
					break;
				default:
					throw new InvalidOperationException($"Invalid animation ratio relator - {(byte) this.RatioType}");
			}

			return (this.UpperClamp - this.LowerClamp) * (ratio % 1) + this.LowerClamp;
		}
	}

	[ProtoContract]
	internal class VectorValue
	{
		[ProtoMember(1, IsRequired = true)]
		public MathOperationEnum Operation = MathOperationEnum.ADD;
		[ProtoMember(2, IsRequired = true)]
		public Vector4D InitialValue;
		[ProtoMember(3, IsRequired = true)]
		public AnimationSourceEnum AnimationSource;
		[ProtoMember(4, IsRequired = true)]
		public Vector4D LowerClamp = new Vector4D(double.NegativeInfinity);
		[ProtoMember(5, IsRequired = true)]
		public Vector4D UpperClamp = new Vector4D(double.PositiveInfinity);
		[ProtoMember(6, IsRequired = true)]
		public bool ClampResult = false;
		[ProtoMember(7, IsRequired = true)]
		internal RatioTypeEnum RatioType = RatioTypeEnum.NONE;

		/// <summary>
		/// Parameterless constructor for serialization
		/// </summary>
		internal VectorValue() { }

		public VectorValue(double initial_value, MathOperationEnum operation, RatioTypeEnum ratio_type = RatioTypeEnum.NONE, Vector4D? lower = null, Vector4D? upper = null)
		{
			this.InitialValue = new Vector4D(initial_value);
			this.Operation = operation;
			this.AnimationSource = AnimationSourceEnum.FIXED;
			this.RatioType = ratio_type;
			this.LowerClamp = lower ?? new Vector4D(double.NegativeInfinity);
			this.UpperClamp = upper ?? new Vector4D(double.PositiveInfinity);
			this.ClampResult = lower != null || upper != null;
		}

		public VectorValue(Vector4D initial_value, MathOperationEnum operation, RatioTypeEnum ratio_type = RatioTypeEnum.NONE, Vector4D? lower = null, Vector4D? upper = null)
		{
			this.InitialValue = initial_value;
			this.Operation = operation;
			this.AnimationSource = AnimationSourceEnum.FIXED;
			this.RatioType = ratio_type;
			this.LowerClamp = lower ?? new Vector4D(double.NegativeInfinity);
			this.UpperClamp = upper ?? new Vector4D(double.PositiveInfinity);
			this.ClampResult = lower != null || upper != null;
		}

		public VectorValue(AnimationSourceEnum animation_source, MathOperationEnum operation, RatioTypeEnum ratio_type = RatioTypeEnum.NONE, Vector4D? lower = null, Vector4D? upper = null)
		{
			this.InitialValue = Vector4D.Zero;
			this.Operation = operation;
			this.AnimationSource = animation_source;
			this.RatioType = ratio_type;
			this.LowerClamp = lower ?? new Vector4D(double.NegativeInfinity);
			this.UpperClamp = upper ?? new Vector4D(double.PositiveInfinity);
			this.ClampResult = lower != null || upper != null;
		}

		public Vector4D GetValue(ushort current_tick, ushort total_duration, MyJumpGate jump_gate, MyJumpGate target_gate, List<MyJumpGateDrive> drives, List<MyEntity> entities, ref Vector3D endpoint, Vector3D? this_position, MyEntity this_entity)
		{
			Vector3D jump_node = jump_gate.WorldJumpNode;
			Vector4D result;
			double ratio;

			switch (this.AnimationSource)
			{
				case AnimationSourceEnum.FIXED:
					result = this.InitialValue;
					break;
				case AnimationSourceEnum.TIME:
					result = new Vector4D((double) current_tick / (double) total_duration);
					break;
				case AnimationSourceEnum.JUMP_GATE_SIZE:
					result = new Vector4D((jump_gate.Closed) ? 0 : jump_gate.JumpNodeRadius());
					break;
				case AnimationSourceEnum.JUMP_GATE_RADII:
					result = (jump_gate.Closed) ? Vector4D.Zero : new Vector4D(jump_gate.JumpEllipse.Radii, 0);
					break;
				case AnimationSourceEnum.JUMP_GATE_VELOCITY:
					result = (jump_gate.Closed) ? Vector4D.Zero : new Vector4D(jump_gate.JumpNodeVelocity, 0);
					break;
				case AnimationSourceEnum.JUMP_ANTIGATE_SIZE:
					result = new Vector4D((target_gate != null && !target_gate.Closed) ? target_gate.JumpNodeRadius() : ((jump_gate.Closed) ? 0 : jump_gate.JumpNodeRadius()));
					break;
				case AnimationSourceEnum.JUMP_ANTIGATE_RADII:
					result = (target_gate != null && !target_gate.Closed) ? new Vector4D(target_gate.JumpEllipse.Radii, 0) : ((jump_gate.Closed) ? Vector4D.Zero : new Vector4D(jump_gate.JumpEllipse.Radii, 0));
					break;
				case AnimationSourceEnum.JUMP_ANTIGATE_VELOCITY:
					result = (target_gate != null && !target_gate.Closed) ? new Vector4D(target_gate.JumpNodeVelocity, 0) : Vector4D.Zero;
					break;
				case AnimationSourceEnum.RANDOM:
				{
					Random prng = new Random();
					result = new Vector4D(prng.NextDouble(), prng.NextDouble(), prng.NextDouble(), prng.NextDouble());
					break;
				}
				case AnimationSourceEnum.N_ENTITIES:
					result = new Vector4D(entities.Count);
					break;
				case AnimationSourceEnum.N_DRIVES:
					result = new Vector4D(drives.Count);
					break;
				case AnimationSourceEnum.DISTANCE_ENTITY_TO_ENDPOINT:
					result = new Vector4D((this_position == null) ? 0 : Vector3D.Distance(endpoint, this_position.Value));
					break;
				case AnimationSourceEnum.DISTANCE_GATE_TO_ENDPOINT:
					result = new Vector4D((jump_gate.Closed) ? 0 : Vector3D.Distance(jump_node, endpoint));
					break;
				case AnimationSourceEnum.DISTANCE_ENTITY_TO_GATE:
					result = new Vector4D((this_position == null || jump_gate.Closed) ? 0 : Vector3D.Distance(jump_node, this_position.Value));
					break;
				case AnimationSourceEnum.DISTANCE_ENTITY_TO_ENTITY:
				{
					if (this_position == null || entities.Count == 0) result = Vector4D.Zero;
					else
					{
						double min_distance = double.MaxValue;

						foreach (MyEntity entity in entities)
						{
							double distance = Vector3D.Distance(entity.WorldMatrix.Translation, this_position.Value);
							min_distance = (distance > 0) ? Math.Min(min_distance, distance) : min_distance;
						}
						result = new Vector4D(min_distance);
					}
					break;
				}
				case AnimationSourceEnum.ENTITY_VELOCITY:
					result = (this_entity == null && jump_gate.Closed) ? Vector4D.Zero : new Vector4D(this_entity?.Physics?.LinearVelocity ?? jump_gate.JumpNodeVelocity, 0);
					break;
				case AnimationSourceEnum.GRAVITY:
					result = new Vector4D((jump_gate.Closed) ? 0 : MyAPIGateway.GravityProviderSystem.CalculateNaturalGravityInPoint(jump_node).Length());
					break;
				case AnimationSourceEnum.ATMOSPHERE:
				{
					MyPlanet planet = (jump_gate.Closed) ? null : MyGamePruningStructure.GetClosestPlanet(jump_node);
					result = new Vector4D((planet == null || !planet.HasAtmosphere) ? 0 : Math.Max(0, 1 - (Vector3D.Distance(jump_node, planet.WorldMatrix.Translation) / (double) planet.AtmosphereRadius)));
					break;
				}
				default:
					throw new InvalidOperationException($"Invalid animation source - {(byte) this.AnimationSource}");
			}

			switch (this.RatioType)
			{
				case RatioTypeEnum.NONE:
					if (this.ClampResult)
					{
						double x = MathHelper.Clamp(result.X, Math.Min(this.LowerClamp.X, this.UpperClamp.X), Math.Max(this.LowerClamp.X, this.UpperClamp.X));
						double y = MathHelper.Clamp(result.Y, Math.Min(this.LowerClamp.Y, this.UpperClamp.Y), Math.Max(this.LowerClamp.Y, this.UpperClamp.Y));
						double z = MathHelper.Clamp(result.Z, Math.Min(this.LowerClamp.Z, this.UpperClamp.Z), Math.Max(this.LowerClamp.Z, this.UpperClamp.Z));
						double w = MathHelper.Clamp(result.W, Math.Min(this.LowerClamp.W, this.UpperClamp.W), Math.Max(this.LowerClamp.W, this.UpperClamp.W));
						return new Vector4D(x, y, z, w);
					}
					else return result;
				case RatioTypeEnum.TIME:
					ratio = (double) current_tick / total_duration;
					break;
				case RatioTypeEnum.ENDPOINT_DISTANCE:
					ratio = (this_position == null) ? 0.5 : (1 - (Vector3D.Distance(this_position.Value, endpoint) / Vector3D.Distance(jump_node, endpoint)));
					break;
				case RatioTypeEnum.ENTITY_DISTANCE:
					ratio = (this_position == null) ? 0.5 : (1 - (Vector3D.Distance(this_position.Value, jump_node) / entities.Max((entity) => Vector3D.Distance(entity.WorldMatrix.Translation, jump_node))));
					break;
				case RatioTypeEnum.ATMOSPHERE:
				{
					MyPlanet planet = (this_position == null) ? null : MyGamePruningStructure.GetClosestPlanet(this_position.Value);
					ratio = (planet == null || !planet.HasAtmosphere) ? 0.5 : Math.Max(0, 1 - (Vector3D.Distance(jump_node, planet.WorldMatrix.Translation) / (double) planet.AtmosphereRadius));
					break;
				}
				case RatioTypeEnum.GRAVITY:
					ratio = (this_position == null) ? 0.5 : MyAPIGateway.GravityProviderSystem.CalculateNaturalGravityInPoint(jump_node).Length();
					break;
				case RatioTypeEnum.RANDOM:
					ratio = new Random().NextDouble();
					break;
				default:
					throw new InvalidOperationException($"Invalid animation ratio relator - {(byte) this.RatioType}");
			}

			return (this.UpperClamp - this.LowerClamp) * (ratio % 1) + this.LowerClamp;
		}
	}

	[ProtoContract]
	public class DoubleKeyframe
	{
		#region Internal Variables
		/// <summary>
		/// The value of this keyframe
		/// </summary>
		[ProtoMember(1, IsRequired = true)]
		internal List<DoubleValue> Values;

		/// <summary>
		/// The position of this keyframe<br />
		/// Relative to animation start
		/// </summary>
		[ProtoMember(2, IsRequired = true)]
		internal ushort Position;

		/// <summary>
		/// The interpolation method from this keyframe to the next
		/// </summary>
		[ProtoMember(3, IsRequired = true)]
		internal EasingCurveEnum EasingCurve = EasingCurveEnum.LINEAR;

		/// <summary>
		/// The easing method from this keyframe to the next
		/// </summary>
		[ProtoMember(4, IsRequired = true)]
		internal EasingTypeEnum EasingType = EasingTypeEnum.EASE_IN_OUT;
		#endregion

		#region Operators
		public static DoubleKeyframe operator +(DoubleKeyframe a, double b)
		{
			a.Values.Add(new DoubleValue(b, MathOperationEnum.ADD));
			return a;
		}
		public static DoubleKeyframe operator +(DoubleKeyframe a, AnimationSourceEnum b)
		{
			a.Values.Add(new DoubleValue(b, MathOperationEnum.ADD));
			return a;
		}

		public static DoubleKeyframe operator -(DoubleKeyframe a, double b)
		{
			a.Values.Add(new DoubleValue(b, MathOperationEnum.SUBTRACT));
			return a;
		}
		public static DoubleKeyframe operator -(DoubleKeyframe a, AnimationSourceEnum b)
		{
			a.Values.Add(new DoubleValue(b, MathOperationEnum.SUBTRACT));
			return a;
		}

		public static DoubleKeyframe operator *(DoubleKeyframe a, double b)
		{
			a.Values.Add(new DoubleValue(b, MathOperationEnum.MULTIPLY));
			return a;
		}
		public static DoubleKeyframe operator *(DoubleKeyframe a, AnimationSourceEnum b)
		{
			a.Values.Add(new DoubleValue(b, MathOperationEnum.MULTIPLY));
			return a;
		}

		public static DoubleKeyframe operator /(DoubleKeyframe a, double b)
		{
			a.Values.Add(new DoubleValue(b, MathOperationEnum.DIVIDE));
			return a;
		}
		public static DoubleKeyframe operator /(DoubleKeyframe a, AnimationSourceEnum b)
		{
			a.Values.Add(new DoubleValue(b, MathOperationEnum.DIVIDE));
			return a;
		}

		public static DoubleKeyframe operator %(DoubleKeyframe a, double b)
		{
			a.Values.Add(new DoubleValue(b, MathOperationEnum.MODULO));
			return a;
		}
		public static DoubleKeyframe operator %(DoubleKeyframe a, AnimationSourceEnum b)
		{
			a.Values.Add(new DoubleValue(b, MathOperationEnum.MODULO));
			return a;
		}

		public static DoubleKeyframe operator ^(DoubleKeyframe a, double b)
		{
			a.Values.Add(new DoubleValue(b, MathOperationEnum.POWER));
			return a;
		}
		public static DoubleKeyframe operator ^(DoubleKeyframe a, AnimationSourceEnum b)
		{
			a.Values.Add(new DoubleValue(b, MathOperationEnum.POWER));
			return a;
		}
		#endregion

		#region Public Static Methods
		public DoubleKeyframe Average(ushort position, double initial_value, EasingCurveEnum easing_curve = EasingCurveEnum.LINEAR, EasingTypeEnum easing_type = EasingTypeEnum.EASE_IN_OUT)
		{
			DoubleKeyframe keyframe = new DoubleKeyframe(position, initial_value, easing_curve, easing_type);
			keyframe.Values[0].Operation = MathOperationEnum.AVERAGE;
			return keyframe;
		}

		public DoubleKeyframe Smallest(ushort position, double initial_value, EasingCurveEnum easing_curve = EasingCurveEnum.LINEAR, EasingTypeEnum easing_type = EasingTypeEnum.EASE_IN_OUT)
		{
			DoubleKeyframe keyframe = new DoubleKeyframe(position, initial_value, easing_curve, easing_type);
			keyframe.Values[0].Operation = MathOperationEnum.SMALLEST;
			return keyframe;
		}

		public DoubleKeyframe Largest(ushort position, double initial_value, EasingCurveEnum easing_curve = EasingCurveEnum.LINEAR, EasingTypeEnum easing_type = EasingTypeEnum.EASE_IN_OUT)
		{
			DoubleKeyframe keyframe = new DoubleKeyframe(position, initial_value, easing_curve, easing_type);
			keyframe.Values[0].Operation = MathOperationEnum.LARGEST;
			return keyframe;
		}
		#endregion

		#region Constructors
		/// <summary>
		/// Parameterless constructor for serialization
		/// </summary>
		internal DoubleKeyframe() { }

		/// <summary>
		/// Creates a new single-value keyframe
		/// </summary>
		/// <param name="position">The position of this keyframe (in game ticks) relative to parent</param>
		/// <param name="initial_value">The value of this keyframe</param>
		/// <param name="easing_curve">The interpolation method from this keyframe to the next</param>
		/// <param name="easing_type">The easing method from this keyframe to the next</param>
		public DoubleKeyframe(ushort position, double initial_value, EasingCurveEnum easing_curve = EasingCurveEnum.LINEAR, EasingTypeEnum easing_type = EasingTypeEnum.EASE_IN_OUT, RatioTypeEnum ratio_type = RatioTypeEnum.NONE, double? lower = null, double? upper = null)
		{
			this.Position = position;
			this.Values = new List<DoubleValue>() { new DoubleValue(initial_value, MathOperationEnum.ADD, ratio_type, lower, upper) };
			this.EasingCurve = easing_curve;
			this.EasingType = easing_type;
		}

		/// <summary>
		/// Creates a new single-value keyframe
		/// </summary>
		/// <param name="position">The position of this keyframe (in game ticks) relative to parent</param>
		/// <param name="initial_value">The value of this keyframe</param>
		/// <param name="easing_curve">The interpolation method from this keyframe to the next</param>
		/// <param name="easing_type">The easing method from this keyframe to the next</param>
		public DoubleKeyframe(ushort position, AnimationSourceEnum initial_value, EasingCurveEnum easing_curve = EasingCurveEnum.LINEAR, EasingTypeEnum easing_type = EasingTypeEnum.EASE_IN_OUT, RatioTypeEnum ratio_type = RatioTypeEnum.NONE, double? lower = null, double? upper = null)
		{
			this.Position = position;
			this.Values = new List<DoubleValue>() { new DoubleValue(initial_value, MathOperationEnum.ADD, ratio_type, lower, upper) };
			this.EasingCurve = easing_curve;
			this.EasingType = easing_type;
		}
		#endregion

		#region Internal Methods
		internal double GetValueFromStack(ushort current_tick, ushort total_duration, MyJumpGate jump_gate, MyJumpGate target_gate, List<MyJumpGateDrive> drives, List<MyEntity> entities, ref Vector3D endpoint, Vector3D? this_position, MyEntity this_entity)
		{
			DoubleValue initial = this.Values[0];

			if (initial.Operation == MathOperationEnum.AVERAGE)
			{
				double sum = 0;
				foreach (DoubleValue value in this.Values) sum += value.GetValue(current_tick, total_duration, jump_gate, target_gate, drives, entities, ref endpoint, this_position, this_entity);
				return sum / this.Values.Count;
			}
			else if (initial.Operation == MathOperationEnum.SMALLEST)
			{
				double smallest = 0;
				foreach (DoubleValue value in this.Values) smallest = Math.Min(smallest, value.GetValue(current_tick, total_duration, jump_gate, target_gate, drives, entities, ref endpoint, this_position, this_entity));
				return smallest;
			}
			else if (initial.Operation == MathOperationEnum.LARGEST)
			{
				double largest = 0;
				foreach (DoubleValue value in this.Values) largest = Math.Max(largest, value.GetValue(current_tick, total_duration, jump_gate, target_gate, drives, entities, ref endpoint, this_position, this_entity));
				return largest;
			}

			double result = initial.GetValue(current_tick, total_duration, jump_gate, target_gate, drives, entities, ref endpoint, this_position, this_entity);

			foreach (DoubleValue value in this.Values.Skip(1))
			{
				double local_result = value.GetValue(current_tick, total_duration, jump_gate, target_gate, drives, entities, ref endpoint, this_position, this_entity);

				switch (value.Operation)
				{
					case MathOperationEnum.ADD:
						result += local_result;
						break;
					case MathOperationEnum.SUBTRACT:
						result -= local_result;
						break;
					case MathOperationEnum.MULTIPLY:
						result *= local_result;
						break;
					case MathOperationEnum.DIVIDE:
						result /= local_result;
						break;
					case MathOperationEnum.MODULO:
						result %= local_result;
						break;
					case MathOperationEnum.POWER:
						result = Math.Pow(result, local_result);
						break;
					default:
						throw new InvalidOperationException($"Invalid operation on double value - {(byte) value.Operation}");
				}
			}

			return result;
		}
		#endregion
	}

	[ProtoContract]
	public class VectorKeyframe
	{
		#region Internal Variables
		/// <summary>
		/// The value of this keyframe
		/// </summary>
		[ProtoMember(1, IsRequired = true)]
		internal List<VectorValue> Values;

		/// <summary>
		/// The position of this keyframe<br />
		/// Relative to animation start
		/// </summary>
		[ProtoMember(2, IsRequired = true)]
		internal ushort Position;

		/// <summary>
		/// The interpolation method from this keyframe to the next
		/// </summary>
		[ProtoMember(3, IsRequired = true)]
		internal EasingCurveEnum EasingCurve = EasingCurveEnum.LINEAR;

		/// <summary>
		/// The easing method from this keyframe to the next
		/// </summary>
		[ProtoMember(4, IsRequired = true)]
		internal EasingTypeEnum EasingType = EasingTypeEnum.EASE_IN_OUT;

		/// <summary>
		/// The ratio type used for ratioing/clamping values
		/// </summary>
		[ProtoMember(5, IsRequired = true)]
		internal RatioTypeEnum RatioType = RatioTypeEnum.NONE;
		#endregion

		#region Operators
		public static VectorKeyframe operator+ (VectorKeyframe a, double b)
		{
			a.Values.Add(new VectorValue(b, MathOperationEnum.ADD));
			return a;
		}
		public static VectorKeyframe operator+ (VectorKeyframe a, Vector4D b)
		{
			a.Values.Add(new VectorValue(b, MathOperationEnum.ADD));
			return a;
		}
		public static VectorKeyframe operator+ (VectorKeyframe a, AnimationSourceEnum b)
		{
			a.Values.Add(new VectorValue(b, MathOperationEnum.ADD));
			return a;
		}

		public static VectorKeyframe operator- (VectorKeyframe a, double b)
		{
			a.Values.Add(new VectorValue(b, MathOperationEnum.SUBTRACT));
			return a;
		}
		public static VectorKeyframe operator- (VectorKeyframe a, Vector4D b)
		{
			a.Values.Add(new VectorValue(b, MathOperationEnum.SUBTRACT));
			return a;
		}
		public static VectorKeyframe operator- (VectorKeyframe a, AnimationSourceEnum b)
		{
			a.Values.Add(new VectorValue(b, MathOperationEnum.SUBTRACT));
			return a;
		}

		public static VectorKeyframe operator* (VectorKeyframe a, double b)
		{
			a.Values.Add(new VectorValue(b, MathOperationEnum.MULTIPLY));
			return a;
		}
		public static VectorKeyframe operator* (VectorKeyframe a, Vector4D b)
		{
			a.Values.Add(new VectorValue(b, MathOperationEnum.MULTIPLY));
			return a;
		}
		public static VectorKeyframe operator* (VectorKeyframe a, AnimationSourceEnum b)
		{
			a.Values.Add(new VectorValue(b, MathOperationEnum.MULTIPLY));
			return a;
		}

		public static VectorKeyframe operator/ (VectorKeyframe a, double b)
		{
			a.Values.Add(new VectorValue(b, MathOperationEnum.DIVIDE));
			return a;
		}
		public static VectorKeyframe operator/ (VectorKeyframe a, Vector4D b)
		{
			a.Values.Add(new VectorValue(b, MathOperationEnum.DIVIDE));
			return a;
		}
		public static VectorKeyframe operator/ (VectorKeyframe a, AnimationSourceEnum b)
		{
			a.Values.Add(new VectorValue(b, MathOperationEnum.DIVIDE));
			return a;
		}

		public static VectorKeyframe operator% (VectorKeyframe a, double b)
		{
			a.Values.Add(new VectorValue(b, MathOperationEnum.MODULO));
			return a;
		}
		public static VectorKeyframe operator% (VectorKeyframe a, Vector4D b)
		{
			a.Values.Add(new VectorValue(b, MathOperationEnum.MODULO));
			return a;
		}
		public static VectorKeyframe operator% (VectorKeyframe a, AnimationSourceEnum b)
		{
			a.Values.Add(new VectorValue(b, MathOperationEnum.MODULO));
			return a;
		}

		public static VectorKeyframe operator^ (VectorKeyframe a, double b)
		{
			a.Values.Add(new VectorValue(b, MathOperationEnum.POWER));
			return a;
		}
		public static VectorKeyframe operator^ (VectorKeyframe a, Vector4D b)
		{
			a.Values.Add(new VectorValue(b, MathOperationEnum.POWER));
			return a;
		}
		public static VectorKeyframe operator^ (VectorKeyframe a, AnimationSourceEnum b)
		{
			a.Values.Add(new VectorValue(b, MathOperationEnum.POWER));
			return a;
		}
		#endregion

		#region Public Static Methods
		public VectorKeyframe Average(ushort position, Vector4D initial_value, EasingCurveEnum easing_curve = EasingCurveEnum.LINEAR, EasingTypeEnum easing_type = EasingTypeEnum.EASE_IN_OUT)
		{
			VectorKeyframe keyframe = new VectorKeyframe(position, initial_value, easing_curve, easing_type);
			keyframe.Values[0].Operation = MathOperationEnum.AVERAGE;
			return keyframe;
		}

		public VectorKeyframe Smallest(ushort position, Vector4D initial_value, EasingCurveEnum easing_curve = EasingCurveEnum.LINEAR, EasingTypeEnum easing_type = EasingTypeEnum.EASE_IN_OUT)
		{
			VectorKeyframe keyframe = new VectorKeyframe(position, initial_value, easing_curve, easing_type);
			keyframe.Values[0].Operation = MathOperationEnum.SMALLEST;
			return keyframe;
		}

		public VectorKeyframe Largest(ushort position, Vector4D initial_value, EasingCurveEnum easing_curve = EasingCurveEnum.LINEAR, EasingTypeEnum easing_type = EasingTypeEnum.EASE_IN_OUT)
		{
			VectorKeyframe keyframe = new VectorKeyframe(position, initial_value, easing_curve, easing_type);
			keyframe.Values[0].Operation = MathOperationEnum.LARGEST;
			return keyframe;
		}
		#endregion

		#region Constructors
		/// <summary>
		/// Parameterless constructor for serialization
		/// </summary>
		internal VectorKeyframe() { }

		/// <summary>
		/// Creates a new vector-value keyframe
		/// </summary>
		/// <param name="position">The position of this keyframe (in game ticks) relative to parent</param>
		/// <param name="initial_value">The value of this keyframe</param>
		/// <param name="easing_curve">The interpolation method from this keyframe to the next</param>
		/// <param name="easing_type">The easing method from this keyframe to the next</param>
		public VectorKeyframe(ushort position, double initial_value, EasingCurveEnum easing_curve = EasingCurveEnum.LINEAR, EasingTypeEnum easing_type = EasingTypeEnum.EASE_IN_OUT, RatioTypeEnum ratio_type = RatioTypeEnum.NONE, Vector4D? lower = null, Vector4D? upper = null)
		{
			this.Position = position;
			this.Values = new List<VectorValue>() { new VectorValue(initial_value, MathOperationEnum.ADD, ratio_type, lower, upper) };
			this.EasingCurve = easing_curve;
			this.EasingType = easing_type;
		}
		
		/// <summary>
		/// Creates a new vector-value keyframe
		/// </summary>
		/// <param name="position">The position of this keyframe (in game ticks) relative to parent</param>
		/// <param name="initial_value">The value of this keyframe</param>
		/// <param name="easing_curve">The interpolation method from this keyframe to the next</param>
		/// <param name="easing_type">The easing method from this keyframe to the next</param>
		public VectorKeyframe(ushort position, Vector4D initial_value, EasingCurveEnum easing_curve = EasingCurveEnum.LINEAR, EasingTypeEnum easing_type = EasingTypeEnum.EASE_IN_OUT, RatioTypeEnum ratio_type = RatioTypeEnum.NONE, Vector4D? lower = null, Vector4D? upper = null)
		{
			this.Position = position;
			this.Values = new List<VectorValue>() { new VectorValue(initial_value, MathOperationEnum.ADD, ratio_type, lower, upper) };
			this.EasingCurve = easing_curve;
			this.EasingType = easing_type;
		}
		
		/// <summary>
		/// Creates a new vector-value keyframe
		/// </summary>
		/// <param name="position">The position of this keyframe (in game ticks) relative to parent</param>
		/// <param name="initial_value">The value of this keyframe</param>
		/// <param name="easing_curve">The interpolation method from this keyframe to the next</param>
		/// <param name="easing_type">The easing method from this keyframe to the next</param>
		public VectorKeyframe(ushort position, AnimationSourceEnum initial_value, EasingCurveEnum easing_curve = EasingCurveEnum.LINEAR, EasingTypeEnum easing_type = EasingTypeEnum.EASE_IN_OUT, RatioTypeEnum ratio_type = RatioTypeEnum.NONE, Vector4D? lower = null, Vector4D? upper = null)
		{
			this.Position = position;
			this.Values = new List<VectorValue>() { new VectorValue(initial_value, MathOperationEnum.ADD, ratio_type, lower, upper) };
			this.EasingCurve = easing_curve;
			this.EasingType = easing_type;
		}
		#endregion

		#region Internal Methods
		internal Vector4D GetValueFromStack(ushort current_tick, ushort total_duration, MyJumpGate jump_gate, MyJumpGate target_gate, List<MyJumpGateDrive> drives, List<MyEntity> entities, ref Vector3D endpoint, Vector3D? this_position, MyEntity this_entity)
		{
			VectorValue initial = this.Values[0];

			if (initial.Operation == MathOperationEnum.AVERAGE)
			{
				Vector4D average = Vector4D.Zero;
				foreach (VectorValue value in this.Values) average += value.GetValue(current_tick, total_duration, jump_gate, target_gate, drives, entities, ref endpoint, this_position, this_entity);
				return average / this.Values.Count;
			}
			else if (initial.Operation == MathOperationEnum.SMALLEST)
			{
				Vector4D smallest = this.Values[0].GetValue(current_tick, total_duration, jump_gate, target_gate, drives, entities, ref endpoint, this_position, this_entity);

				foreach (VectorValue value in this.Values)
				{
					Vector4D local = value.GetValue(current_tick, total_duration, jump_gate, target_gate, drives, entities, ref endpoint, this_position, this_entity);
					if (local.Length() < smallest.Length()) smallest = local;
				}

				return smallest;
			}
			else if (initial.Operation == MathOperationEnum.LARGEST)
			{
				Vector4D largest = this.Values[0].GetValue(current_tick, total_duration, jump_gate, target_gate, drives, entities, ref endpoint, this_position, this_entity);

				foreach (VectorValue value in this.Values)
				{
					Vector4D local = value.GetValue(current_tick, total_duration, jump_gate, target_gate, drives, entities, ref endpoint, this_position, this_entity);
					if (local.Length() > largest.Length()) largest = local;
				}

				return largest;
			}

			Vector4D result = initial.GetValue(current_tick, total_duration, jump_gate, target_gate, drives, entities, ref endpoint, this_position, this_entity);

			foreach (VectorValue value in this.Values.Skip(1))
			{
				Vector4D local_result = value.GetValue(current_tick, total_duration, jump_gate, target_gate, drives, entities, ref endpoint, this_position, this_entity);

				switch (value.Operation)
				{
					case MathOperationEnum.ADD:
						result += local_result;
						break;
					case MathOperationEnum.SUBTRACT:
						result -= local_result;
						break;
					case MathOperationEnum.MULTIPLY:
						result *= local_result;
						break;
					case MathOperationEnum.DIVIDE:
						result /= local_result;
						break;
					case MathOperationEnum.MODULO:
						result = new Vector4D(result.X % local_result.X, result.Y % local_result.Y, result.Z % local_result.Z, result.W % local_result.W);
						break;
					case MathOperationEnum.POWER:
						result = new Vector4D(Math.Pow(result.X, local_result.X), Math.Pow(result.Y, local_result.Y), Math.Pow(result.Z, local_result.Z), Math.Pow(result.W, local_result.W));
						break;
					default:
						throw new InvalidOperationException($"Invalid operation on double value - {(byte) value.Operation}");
				}
			}

			return result;
		}
		#endregion
	}
	#endregion

	#region Animation Object Definitions
	/// <summary>
	/// Definition defining a particle orientation
	/// </summary>
	[ProtoContract]
	public class ParticleOrientationDef
	{
		#region Public Variables
		/// <summary>
		/// The particle's orientation
		/// </summary>
		[ProtoMember(1, IsRequired = true)]
		public ParticleOrientationEnum ParticleOrientation = ParticleOrientationEnum.GATE_ENDPOINT_NORMAL;

		/// <summary>
		/// If fixed, the particle's world matrix
		/// </summary>
		[ProtoMember(2, IsRequired = true)]
		public MatrixD WorldMatrix = MatrixD.CreateFromYawPitchRoll(0, 0, 0);
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
			if (null_orientation && is_anti_node && target_gate != null) target_gate.GetWorldMatrix(out matrix, true, true);
			else if (null_orientation) gate.GetWorldMatrix(out matrix, true, (is_anti_node) ? target_gate != null : true);
			else if (particle_orientation.ParticleOrientation == ParticleOrientationEnum.GATE_TRUE_ENDPOINT_NORMAL) gate.GetWorldMatrix(out matrix, true, false);
			else if (particle_orientation.ParticleOrientation == ParticleOrientationEnum.GATE_DRIVE_NORMAL) matrix = particle_orientation.WorldMatrix * gate.GetWorldMatrix(false, false);
			else if (particle_orientation.ParticleOrientation == ParticleOrientationEnum.ANTIGATE_DRIVE_NORMAL) matrix = particle_orientation.WorldMatrix * (target_gate?.GetWorldMatrix(true, true) ?? gate.GetWorldMatrix(true, false));
			else
			{
				matrix = particle_orientation.WorldMatrix;
				matrix.Translation = gate.WorldJumpNode;
			}

			if (is_anti_node)
			{
				matrix.Forward = -matrix.Forward;
				matrix.Up = -matrix.Up;
				matrix.Translation = endpoint;
			}

			return matrix;
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
			this.WorldMatrix = (particle_orientation == ParticleOrientationEnum.FIXED) ? MatrixD.Zero : MatrixD.CreateFromYawPitchRoll(0, 0, 0);
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
	[ProtoContract]
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
				&& this.AllowedJumpGateSize.Match((uint) jump_gate.JumpGateGrid.GetDriveCount((drive) => drive.JumpGateID == jump_gate.JumpGateID))
				&& this.AllowedJumpGateWorkingSize.Match((uint) jump_gate.JumpGateGrid.GetDriveCount((drive) => drive.JumpGateID == jump_gate.JumpGateID && drive.IsWorking()))
				&& this.AllowedJumpGateEndpointDistance.Match(distance);
		}
		#endregion
	}

	/// <summary>
	/// Definition defining an atribute being animated
	/// </summary>
	[ProtoContract]
	public sealed class AttributeAnimationDef
	{
		internal static double GetAnimatedDoubleValue(DoubleKeyframe[] keyframes, ushort current_tick, ushort total_duration, MyJumpGate jump_gate, MyJumpGate target_gate, List <MyJumpGateDrive> drives, List <MyEntity> entities, ref Vector3D endpoint, Vector3D? this_position, MyEntity this_entity, double default_ = default(double))
		{
			if (keyframes == null || keyframes.Length == 0) return default_;
			else if (keyframes.Length == 1) return keyframes[0].GetValueFromStack(current_tick, total_duration, jump_gate, target_gate, drives, entities, ref endpoint, this_position, this_entity);
			DoubleKeyframe last_keyframe = null;
			DoubleKeyframe next_keyframe = null;

			foreach (DoubleKeyframe keyframe in keyframes.OrderBy((frame) => frame.Position))
			{
				if (keyframe.Position == current_tick) return keyframe.GetValueFromStack(current_tick, total_duration, jump_gate, target_gate, drives, entities, ref endpoint, this_position, this_entity);
				else if (keyframe.Position > current_tick)
				{
					next_keyframe = keyframe;
					break;
				}

				last_keyframe = keyframe;
			}

			if (next_keyframe == null) return last_keyframe.GetValueFromStack(current_tick, total_duration, jump_gate, target_gate, drives, entities, ref endpoint, this_position, this_entity);
			else if (last_keyframe == null) return next_keyframe.GetValueFromStack(current_tick, total_duration, jump_gate, target_gate, drives, entities, ref endpoint, this_position, this_entity);

			double curr = current_tick;
			double last = last_keyframe.Position;
			double next = next_keyframe.Position;
			double ratio = MathHelper.Clamp((curr - last) / (next - last), 0, 1);
			ratio = EasingFunctor.GetEaseResult(ratio, last_keyframe.EasingType, last_keyframe.EasingCurve);
			last = last_keyframe.GetValueFromStack(current_tick, total_duration, jump_gate, target_gate, drives, entities, ref endpoint, this_position, this_entity);
			next = next_keyframe.GetValueFromStack(current_tick, total_duration, jump_gate, target_gate, drives, entities, ref endpoint, this_position, this_entity);
			return (next - last) * ratio + last;
		}

		internal static Vector4D GetAnimatedVectorValue(VectorKeyframe[] keyframes, ushort current_tick, ushort total_duration, MyJumpGate jump_gate, MyJumpGate target_gate, List<MyJumpGateDrive> drives, List<MyEntity> entities, ref Vector3D endpoint, Vector3D? this_position, MyEntity this_entity, Vector4D default_ = default(Vector4D))
		{
			if (keyframes == null || keyframes.Length == 0) return default_;
			else if (keyframes.Length == 1) return keyframes[0].GetValueFromStack(current_tick, total_duration, jump_gate, target_gate, drives, entities, ref endpoint, this_position, this_entity);
			VectorKeyframe last_keyframe = null;
			VectorKeyframe next_keyframe = null;

			foreach (VectorKeyframe keyframe in keyframes.OrderBy((frame) => frame.Position))
			{
				if (keyframe.Position == current_tick) return keyframe.GetValueFromStack(current_tick, total_duration, jump_gate, target_gate, drives, entities, ref endpoint, this_position, this_entity);
				else if (keyframe.Position > current_tick)
				{
					next_keyframe = keyframe;
					break;
				}

				last_keyframe = keyframe;
			}

			if (next_keyframe == null) return last_keyframe.GetValueFromStack(current_tick, total_duration, jump_gate, target_gate, drives, entities, ref endpoint, this_position, this_entity);
			else if (last_keyframe == null) return next_keyframe.GetValueFromStack(current_tick, total_duration, jump_gate, target_gate, drives, entities, ref endpoint, this_position, this_entity);

			double curr = current_tick;
			double last = last_keyframe.Position;
			double next = next_keyframe.Position;
			double ratio = MathHelper.Clamp((curr - last) / (next - last), 0, 1);
			ratio = EasingFunctor.GetEaseResult(ratio, last_keyframe.EasingType, last_keyframe.EasingCurve);
			Vector4D last_value = last_keyframe.GetValueFromStack(current_tick, total_duration, jump_gate, target_gate, drives, entities, ref endpoint, this_position, this_entity);
			Vector4D next_value = next_keyframe.GetValueFromStack(current_tick, total_duration, jump_gate, target_gate, drives, entities, ref endpoint, this_position, this_entity);
			return (next_value - last_value) * ratio + last_value;
		}

		/// <summary>
		/// Modifies or animates a sound's volume
		/// </summary>
		[ProtoMember(1, IsRequired = true)]
		public DoubleKeyframe[] SoundVolumeAnimation = null;

		/// <summary>
		/// Modifies or animates a sound's distance
		/// </summary>
		[ProtoMember(2, IsRequired = true)]
		public DoubleKeyframe[] SoundDistanceAnimation = null;

		/// <summary>
		/// Modifies or animates a particle's birth multiplier
		/// </summary>
		[ProtoMember(3, IsRequired = true)]
		public DoubleKeyframe[] ParticleBirthAnimation = null;

		/// <summary>
		/// Modifies or animates a particle's color intensity multiplier
		/// </summary>
		[ProtoMember(4, IsRequired = true)]
		public DoubleKeyframe[] ParticleColorIntensityAnimation = null;

		/// <summary>
		/// Modifies or animates a particle's color multiplier
		/// </summary>
		[ProtoMember(5, IsRequired = true)]
		public VectorKeyframe[] ParticleColorAnimation = null;

		/// <summary>
		/// Modifies or animates a particle's fade multiplier
		/// </summary>
		[ProtoMember(6, IsRequired = true)]
		public DoubleKeyframe[] ParticleFadeAnimation = null;

		/// <summary>
		/// Modifies or animates a particle's life multiplier
		/// </summary>
		[ProtoMember(7, IsRequired = true)]
		public DoubleKeyframe[] ParticleLifeAnimation = null;

		/// <summary>
		/// Modifies or animates a particle's radius multiplier
		/// </summary>
		[ProtoMember(8, IsRequired = true)]
		public DoubleKeyframe[] ParticleRadiusAnimation = null;

		/// <summary>
		/// Modifies or animates a particle's scale multiplier
		/// </summary>
		[ProtoMember(9, IsRequired = true)]
		public DoubleKeyframe[] ParticleScaleAnimation = null;

		/// <summary>
		/// Modifies or animates a particle's velocity multiplier
		/// </summary>
		[ProtoMember(10, IsRequired = true)]
		public DoubleKeyframe[] ParticleVelocityAnimation = null;

		/// <summary>
		/// Modifies or animates a particle's rotation speed
		/// </summary>
		[ProtoMember(11, IsRequired = true)]
		public VectorKeyframe[] ParticleRotationSpeedAnimation = null;

		/// <summary>
		/// Modifies or animates a particle's offset in meters
		/// </summary>
		[ProtoMember(12, IsRequired = true)]
		public VectorKeyframe[] ParticleOffsetAnimation = null;

		/// <summary>
		/// Modifies or animates a beam pulse's frequency<br />
		/// For a solid beam, set this to 0<br />
		/// For a gradient, this value must not be 0
		/// </summary>
		[ProtoMember(13, IsRequired = true)]
		public DoubleKeyframe[] BeamFrequencyAnimation = null;

		/// <summary>
		/// Modifies or animates a beam pulse's duty cycle<br />
		/// For a gradient beam with no breaks, set this to 1
		/// </summary>
		[ProtoMember(14, IsRequired = true)]
		public DoubleKeyframe[] BeamDutyCycleAnimation = null;

		/// <summary>
		/// Modifies or animates a beam pulse's offset
		/// </summary>
		[ProtoMember(15, IsRequired = true)]
		public DoubleKeyframe[] BeamOffsetAnimation = null;

		/// <summary>
		/// Modifies or animates the jump space attractor force<br />
		/// If not-zero, a force will be applied to all entities within the jump space during jump<br />
		/// Positive values attract entities to jump node<br />
		/// Negative values repel entities from jump node<br />
		/// </summary>
		[ProtoMember(16, IsRequired = true)]
		public DoubleKeyframe[] PhysicsForceAnimation = null;

		/// <summary>
		/// Modifies or animates the jump space attractor force falloff
		/// </summary>
		[ProtoMember(17, IsRequired = true)]
		public DoubleKeyframe[] PhysicsForceFalloffAnimation = null;

		/// <summary>
		/// Modifies or animates the jump space attractor force offset
		/// </summary>
		[ProtoMember(18, IsRequired = true)]
		public VectorKeyframe[] PhysicsForceOffsetAnimation = null;

		/// <summary>
		/// Modifies or animates the jump space attractor force max allowed speed
		/// </summary>
		[ProtoMember(19, IsRequired = true)]
		public DoubleKeyframe[] PhysicsForceMaxSpeedAnimation = null;

		/// <summary>
		/// Modifies or animates the jump space attractor force torque
		/// </summary>
		[ProtoMember(20, IsRequired = true)]
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
	[ProtoContract]
	[ProtoInclude(100, typeof(ParticleDef))]
	[ProtoInclude(200, typeof(SoundDef))]
	[ProtoInclude(300, typeof(BeamPulseDef))]
	[ProtoInclude(400, typeof(DriveEmissiveColorDef))]
	[ProtoInclude(500, typeof(NodePhysicsDef))]
	public class AnimatableDef
	{
		#region Public Variables
		/// <summary>
		/// The start time of this animation
		/// </summary>
		[ProtoMember(1, IsRequired = true)]
		public ushort StartTime;

		/// <summary>
		/// The duraton of this animation in game ticks
		/// </summary>
		[ProtoMember(2, IsRequired = true)]
		public ushort Duration;

		/// <summary>
		/// The attribute modifiers this animation contains
		/// </summary>
		//[ProtoMember(3, IsRequired = true)]
		//public AttributeModifier[] Animations = null;

		[ProtoMember(3, IsRequired = true)]
		public AttributeAnimationDef Animations = null;
		#endregion
	}

	/// <summary>
	/// Definition defining particles
	/// </summary>
	[ProtoContract]
	public class ParticleDef : AnimatableDef
	{
		#region Public Variables
		/// <summary>
		/// Whether to clean this particle effect once it's completed<br />
		/// If false, effect is cleaned when entire gate animation is completed<br />
		/// This will prevent particle rotations persisting through animation states
		/// </summary>
		[ProtoMember(5, IsRequired = true)]
		public bool CleanOnEffectEnd = false;

		/// <summary>
		/// The name of the particle to display
		/// </summary>
		[ProtoMember(6, IsRequired = true)]
		public string[] ParticleNames = null;

		/// <summary>
		/// The local offset of this particle effect
		/// </summary>
		[ProtoMember(7, IsRequired = true)]
		public Vector3D ParticleOffset = Vector3D.Zero;

		/// <summary>
		/// The particle's orientation definition
		/// </summary>
		[ProtoMember(8, IsRequired = true)]
		public ParticleOrientationDef ParticleOrientation = null;

		/// <summary>
		/// The transience IDs <br />
		/// Used to persist the specified particle effect between animation states<br />
		/// Must be higher than 0 to enable<br />
		/// Particles will be matched with other particle definitions with the same ID
		/// </summary>
		[ProtoMember(9, IsRequired = true)]
		public byte[] TransientIDs = null;
		#endregion
	}

	/// <summary>
	/// Definition defining sounds
	/// </summary>
	[ProtoContract]
	public class SoundDef : AnimatableDef
	{
		#region Public Variables
		/// <summary>
		/// The sound names to play
		/// </summary>
		[ProtoMember(5, IsRequired = true)]
		public string[] SoundNames;

		/// <summary>
		/// The volume to play at
		/// </summary>
		[ProtoMember(6, IsRequired = true)]
		public float Volume = 1;
		
		/// <summary>
		/// The range this sound can be heard at
		/// </summary>
		[ProtoMember(7, IsRequired = true)]
		public float? Distance = null;
		#endregion
	}

	/// <summary>
	/// Definition defining the beam pulse
	/// </summary>
	[ProtoContract]
	public class BeamPulseDef : AnimatableDef
	{
		#region Public Variables
		/// <summary>
		/// The time (in game ticks) this beam will take to travel from jump node to endpoint
		/// </summary>
		[ProtoMember(5, IsRequired = true)]
		public ushort TravelTime = 0;

		/// <summary>
		/// The beam's color
		/// </summary>
		[ProtoMember(6, IsRequired = true)]
		public Color BeamColor = Color.Transparent;

		/// <summary>
		/// The beam's maximum length
		/// </summary>
		[ProtoMember(7, IsRequired = true)]
		public double BeamLength = -1;

		/// <summary>
		/// The beam's width (in meters?)
		/// </summary>
		[ProtoMember(8, IsRequired = true)]
		public double BeamWidth = 0;

		/// <summary>
		/// The beam's brightness
		/// </summary>
		[ProtoMember(9, IsRequired = true)]
		public double BeamBrightness = 1;

		/// <summary>
		/// The beam's frequency<br />
		/// Higher values result in smaller segments<br />
		/// Set to 0 for a constant, unbroken beam
		/// </summary>
		[ProtoMember(10, IsRequired = true)]
		public double BeamFrequency = 0;

		/// <summary>
		/// The beam's duty cycle<br />
		/// Has no effect if the frequency is 0<br />
		/// Set to 1 for a segmented beam with no gaps<br />
		/// Set to 0.5 for a segmented beam with equally spaced gaps
		/// </summary>
		[ProtoMember(11, IsRequired = true)]
		public double BeamDutyCycle = 1;

		/// <summary>
		/// The beam's offset
		/// </summary>
		[ProtoMember(12, IsRequired = true)]
		public double BeamOffset = 0;

		/// <summary>
		/// The beam's material
		/// </summary>
		[ProtoMember(13, IsRequired = true)]
		public string Material = "WeaponLaser";
		#endregion
	}

	/// <summary>
	/// Definition defining a gate's drive emitter emissive colors
	/// </summary>
	[ProtoContract]
	public class DriveEmissiveColorDef : AnimatableDef
	{
		#region Public Variables
		/// <summary>
		/// The intended emissive color
		/// </summary>
		[ProtoMember(5, IsRequired = true)]
		public Color EmissiveColor = Color.Black;

		/// <summary>
		/// The intended emissive color brightness
		/// </summary>
		[ProtoMember(6, IsRequired = true)]
		public double Brightness = 1;
		#endregion
	}

	/// <summary>
	/// Definition defining a gate's node attractor force
	/// </summary>
	[ProtoContract]
	public class NodePhysicsDef : AnimatableDef
	{
		#region Public Variables
		/// <summary>
		/// The attractor force strength<br />
		/// Positive values attract entities towards jump node<br />
		/// Negative values repel entities away from jump node
		/// </summary>
		[ProtoMember(5, IsRequired = true)]
		public double AttractorForce = 0;

		/// <summary>
		/// The attractor force falloff
		/// </summary>
		[ProtoMember(6, IsRequired = true)]
		public double AttractorForceFalloff = 0;

		/// <summary>
		/// The attractor force max speed<br />
		/// Objects above or at this speed will not be affected by the attractor force
		/// </summary>
		[ProtoMember(7, IsRequired = true)]
		public double MaxSpeed = 0;

		/// <summary>
		/// The attractor force offset
		/// </summary>
		[ProtoMember(8, IsRequired = true)]
		public Vector3D ForceOffset = Vector3D.Zero;

		/// <summary>
		/// The attractor force torque
		/// </summary>
		[ProtoMember(9, IsRequired = true)]
		public Vector3D AttractorTorque = Vector3D.Zero;
		#endregion
	}
	#endregion

	#region Animation Definitions
	/// <summary>
	/// Definition defining the "charging/jumping" phase animation
	/// </summary>
	[ProtoContract]
	public class JumpGateJumpingAnimationDef
	{
		#region Public Variables
		/// <summary>
		/// The duration of the animaton in game ticks
		/// </summary>
		[ProtoMember(1, IsRequired = true)]
		public ushort Duration = 0;

		/// <summary>
		/// The list of particle definitions for each drive<br />
		/// These particles will each be played for every drive in the gate
		/// </summary>
		[ProtoMember(2, IsRequired = true)]
		public ParticleDef[] PerDriveParticles = null;

		/// <summary>
		/// The list of particle definitions for each anti drive<br />
		/// These particles will each be played for every drive in the targeted gate
		/// </summary>
		[ProtoMember(3, IsRequired = true)]
		public ParticleDef[] PerAntiDriveParticles = null;

		/// <summary>
		/// The list of particle definitions for each jump space entity<br />
		/// These particles will each be played for every entity in the gate's jump space
		/// </summary>
		[ProtoMember(4, IsRequired = true)]
		public ParticleDef[] PerEntityParticles = null;

		/// <summary>
		/// The list of ParticleDef definitions for the gate's jump node<br />
		/// These particles will be played once at the gate's jump node
		/// </summary>
		[ProtoMember(5, IsRequired = true)]
		public ParticleDef[] NodeParticles = null;

		/// <summary>
		/// The list of SoundDef definitions<br />
		/// These sounds will be played once at the gate's jump node
		/// </summary>
		[ProtoMember(6, IsRequired = true)]
		public SoundDef[] NodeSounds = null;

		/// <summary>
		/// The DriveEmissiveColorDef defining the color for this gate's jump drive emitter emissives
		/// </summary>
		[ProtoMember(7, IsRequired = true)]
		public DriveEmissiveColorDef DriveEmissiveColor = null;

		/// <summary>
		/// The NodePhysicsDef defining the attractor forces for this gate's jump node
		/// </summary>
		[ProtoMember(8, IsRequired = true)]
		public NodePhysicsDef NodePhysics = null;

		/// <summary>
		/// The list of ParticleDef definitions for the gate's anti-node<br />
		/// These particles will be played once at the gate's anti-node<br />
		/// <i>The anti-node is the region at the endpoint of this gate</i>
		/// </summary>
		[ProtoMember(9, IsRequired = true)]
		public ParticleDef[] AntiNodeParticles = null;

		/// <summary>
		/// The list of SoundDef definitions for the gate's anti-node<br />
		/// These sounds will be played once at the gate's anti-node<br />
		/// <i>The anti-node is the region at the endpoint of this gate</i>
		/// </summary>
		[ProtoMember(10, IsRequired = true)]
		public SoundDef[] AntiNodeSounds = null;

		/// <summary>
		/// The NodePhysicsDef defining the attractor forces for this gate's anti-ode<br />
		/// <i>The anti-node is the region at the endpoint of this gate</i>
		/// </summary>
		[ProtoMember(11, IsRequired = true)]
		public NodePhysicsDef AntiNodePhysics = null;
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
	[ProtoContract]
	public class JumpGateJumpedAnimationDef
	{
		#region Public Variables
		/// <summary>
		/// The duration of the animaton in game ticks
		/// </summary>
		[ProtoMember(1, IsRequired = true)]
		public ushort Duration = 0;

		/// <summary>
		/// The list of particle definitions for each drive<br />
		/// These particles will each be played for every drive in the gate
		/// </summary>
		[ProtoMember(2, IsRequired = true)]
		public ParticleDef[] PerDriveParticles = null;

		/// <summary>
		/// The list of particle definitions for each anti drive<br />
		/// These particles will each be played for every drive in the targeted gate
		/// </summary>
		[ProtoMember(3, IsRequired = true)]
		public ParticleDef[] PerAntiDriveParticles = null;

		/// <summary>
		/// The list of particle definitions for each jump space entity<br />
		/// These particles will each be played for every entity in the gate's jump space
		/// </summary>
		[ProtoMember(4, IsRequired = true)]
		public ParticleDef[] PerEntityParticles = null;

		/// <summary>
		/// The list of ParticleDef definitions for the gate's jump node<br />
		/// These particles will be played once at the gate's jump node
		/// </summary>
		[ProtoMember(5, IsRequired = true)]
		public ParticleDef[] NodeParticles = null;

		/// <summary>
		/// The travel particle effect shown to entities within the jump space
		/// </summary>
		[ProtoMember(6, IsRequired = true)]
		public ParticleDef[] TravelEffects = null;

		/// <summary>
		/// The list of SoundDef definitions<br />
		/// These sounds will be played once at the gate's jump node
		/// </summary>
		[ProtoMember(7, IsRequired = true)]
		public SoundDef[] NodeSounds = null;

		/// <summary>
		/// The list of SoundDef definitions<br />
		/// These sounds will be played once to entities currently being jumped
		/// </summary>
		[ProtoMember(8, IsRequired = true)]
		public SoundDef[] TravelSounds = null;

		/// <summary>
		/// The BeamPulseDef defining the beam pulse for this gate
		/// </summary>
		[ProtoMember(9, IsRequired = true)]
		public BeamPulseDef BeamPulse = null;

		/// <summary>
		/// The DriveEmissiveColorDef defining the color for this gate's jump drive emitter emissives
		/// </summary>
		[ProtoMember(10, IsRequired = true)]
		public DriveEmissiveColorDef DriveEmissiveColor = null;

		/// <summary>
		/// The NodePhysicsDef defining the attractor forces for this gate's jump node
		/// </summary>
		[ProtoMember(11, IsRequired = true)]
		public NodePhysicsDef NodePhysics = null;

		/// <summary>
		/// The list of ParticleDef definitions for the gate's anti-node<br />
		/// These particles will be played once at the gate's anti-node<br />
		/// <i>The anti-node is the region at the endpoint of this gate</i>
		/// </summary>
		[ProtoMember(12, IsRequired = true)]
		public ParticleDef[] AntiNodeParticles = null;

		/// <summary>
		/// The list of SoundDef definitions for the gate's anti-node<br />
		/// These sounds will be played once at the gate's anti-node<br />
		/// <i>The anti-node is the region at the endpoint of this gate</i>
		/// </summary>
		[ProtoMember(13, IsRequired = true)]
		public SoundDef[] AntiNodeSounds = null;

		/// <summary>
		/// The NodePhysicsDef defining the attractor forces for this gate's anti-ode<br />
		/// <i>The anti-node is the region at the endpoint of this gate</i>
		/// </summary>
		[ProtoMember(14, IsRequired = true)]
		public NodePhysicsDef AntiNodePhysics = null;
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
	[ProtoContract]
	public class JumpGateFailedAnimationDef
	{
		#region Public Variables
		/// <summary>
		/// The duration of the animaton in game ticks
		/// </summary>
		[ProtoMember(1, IsRequired = true)]
		public ushort Duration = 0;

		/// <summary>
		/// The list of particle definitions for each drive<br />
		/// These particles will each be played for every drive in the gate
		/// </summary>
		[ProtoMember(2, IsRequired = true)]
		public ParticleDef[] PerDriveParticles = null;

		/// <summary>
		/// The list of particle definitions for each anti drive<br />
		/// These particles will each be played for every drive in the targeted gate
		/// </summary>
		[ProtoMember(3, IsRequired = true)]
		public ParticleDef[] PerAntiDriveParticles = null;

		/// <summary>
		/// The list of particle definitions for each jump space entity<br />
		/// These particles will each be played for every entity in the gate's jump space
		/// </summary>
		[ProtoMember(4, IsRequired = true)]
		public ParticleDef[] PerEntityParticles = null;

		/// <summary>
		/// The list of ParticleDef definitions for the gate's jump node<br />
		/// These particles will be played once at the gate's jump node
		/// </summary>
		[ProtoMember(5, IsRequired = true)]
		public ParticleDef[] NodeParticles = null;

		/// <summary>
		/// The list of SoundDef definitions<br />
		/// These sounds will be played once at the gate's jump node
		/// </summary>
		[ProtoMember(6, IsRequired = true)]
		public SoundDef[] NodeSounds = null;

		/// <summary>
		/// The DriveEmissiveColorDef defining the color for this gate's jump drive emitter emissives
		/// </summary>
		[ProtoMember(7, IsRequired = true)]
		public DriveEmissiveColorDef DriveEmissiveColor = null;

		/// <summary>
		/// The NodePhysicsDef defining the attractor forces for this gate's jump node
		/// </summary>
		[ProtoMember(8, IsRequired = true)]
		public NodePhysicsDef NodePhysics = null;

		/// <summary>
		/// The list of ParticleDef definitions for the gate's anti-node<br />
		/// These particles will be played once at the gate's anti-node<br />
		/// <i>The anti-node is the region at the endpoint of this gate</i>
		/// </summary>
		[ProtoMember(9, IsRequired = true)]
		public ParticleDef[] AntiNodeParticles = null;

		/// <summary>
		/// The list of SoundDef definitions for the gate's anti-node<br />
		/// These sounds will be played once at the gate's anti-node<br />
		/// <i>The anti-node is the region at the endpoint of this gate</i>
		/// </summary>
		[ProtoMember(10, IsRequired = true)]
		public SoundDef[] AntiNodeSounds = null;

		/// <summary>
		/// The NodePhysicsDef defining the attractor forces for this gate's anti-ode<br />
		/// <i>The anti-node is the region at the endpoint of this gate</i>
		/// </summary>
		[ProtoMember(11, IsRequired = true)]
		public NodePhysicsDef AntiNodePhysics = null;
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
	[ProtoContract]
	public class AnimationDef
	{
		#region Internal Variables
		/// <summary>
		/// Whether to serialize this animation to XML after session unload
		/// </summary>
		internal bool SerializeOnEnd = false;

		/// <summary>
		/// The mod that defined this animation
		/// </summary>
		internal string SourceMod = null;

		/// <summary>
		/// The subtype ID of this animation<br />
		/// If multiple animatons with the same name are defined, and all but one animation have a contraint defined, this value will be non-null
		/// </summary>
		internal ulong? SubtypeID = null;
		#endregion

		#region Public Variables
		/// <summary>
		/// Whether this animation is enabled<br />
		/// Disabled animations are not shown in the controller list
		/// </summary>
		[ProtoMember(1, IsRequired = true)]
		public bool Enabled = true;

		/// <summary>
		/// Whether this animation can be cancelled immediatly<br />
		/// If false, animation in the jumping phase will cancel once complete
		/// </summary>
		[ProtoMember(2, IsRequired = true)]
		public bool ImmediateCancel = true;

		/// <summary>
		/// The name of this animation
		/// </summary>
		[ProtoMember(3, IsRequired = true)]
		public string AnimationName;

		/// <summary>
		/// The description of this animation
		/// </summary>
		[ProtoMember(4, IsRequired = true)]
		public string Description;

		/// <summary>
		/// The JumpGateJumpingAnimationDef definition defining the jumping phase of this animation
		/// </summary>
		[ProtoMember(5, IsRequired = true)]
		public JumpGateJumpingAnimationDef JumpingAnimationDef = null;

		/// <summary>
		/// The JumpGateJumpedAnimationDef definition defining the jumped phase of this animation
		/// </summary>
		[ProtoMember(6, IsRequired = true)]
		public JumpGateJumpedAnimationDef JumpedAnimationDef = null;

		/// <summary>
		/// The JumpGateFailedAnimationDef definition defining the failed phase of this animation
		/// </summary>
		[ProtoMember(7, IsRequired = true)]
		public JumpGateFailedAnimationDef FailedAnimationDef = null;

		/// <summary>
		/// The AnimationConstraintDef definition defining a jump gate constraint for this animation
		/// </summary>
		[ProtoMember(8, IsRequired = true)]
		public AnimationConstraintDef AnimationContraint = null;
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
		#endregion
	}
	#endregion

	#region Animation Object Implementations
	/// <summary>
	/// Implementation holding functionality for particle definitions
	/// </summary>
	internal class Particle
	{
		private sealed class TransientParticle
		{
			public MyParticleEffect ParticleEffect;
			public Vector3D ParticleRotation;

			public TransientParticle(MyParticleEffect effect, Vector3D rotation)
			{
				this.ParticleEffect = effect;
				this.ParticleRotation = rotation;
			}
		}

		#region Private Static Variables
		/// <summary>
		/// Master map storing transient particles
		/// </summary>
		private static Dictionary<MyJumpGate, Dictionary<byte, TransientParticle>> TransientParticles = new Dictionary<MyJumpGate, Dictionary<byte, TransientParticle>>();
		#endregion

		#region Private Variables
		/// <summary>
		/// Whether this particle should be spawned at the gate's anti-node
		/// </summary>
		private readonly bool IsAntiNode;

		/// <summary>
		/// The duration of this particle effect in game ticks
		/// </summary>
		private readonly ushort Duration;

		/// <summary>
		/// The last particle rotations
		/// </summary>
		private List<Vector3D> ParticleRotations = null;

		/// <summary>
		/// The particle effects
		/// </summary>
		private List<MyParticleEffect> ParticleEffects = null;

		/// <summary>
		/// The transient IDs
		/// </summary>
		private List<byte> ParticleTransientIDs = null;

		/// <summary>
		/// The calling jump gate
		/// </summary>
		private MyJumpGate JumpGate;

		/// <summary>
		/// The targeted jump gate or null
		/// </summary>
		private MyJumpGate TargetGate;

		/// <summary>
		/// The calling jump gate's controller settings
		/// </summary>
		private MyJumpGateController.MyControllerBlockSettingsStruct ControllerSettings;
		#endregion

		#region Public Variables
		/// <summary>
		/// The particle definition
		/// </summary>
		public ParticleDef ParticleDefinition { get; private set; }
		#endregion

		#region Constructors
		/// <summary>
		/// Creates a new particle effect
		/// </summary>
		/// <param name="def">The particle definition</param>
		/// <param name="animation_duration">The parent animation duration in game ticks</param>
		/// <param name="jump_gate">The calling jump gate</param>
		/// <param name="target_gate">The targeted jump gate or null</param>
		/// <param name="controller_settings">The calling jump gate's controller settings</param>
		/// <param name="matrix">The particle orientation matrix</param>
		/// <param name="position">The particle effect position</param>
		/// <param name="anti_node">Whether this particle should spawn at the anti-node</param>
		/// <exception cref="ArgumentNullException">If the particle definition is null</exception>
		public Particle(ParticleDef def, ushort animation_duration, MyJumpGate jump_gate, MyJumpGate target_gate, MyJumpGateController.MyControllerBlockSettingsStruct controller_settings, MatrixD matrix, Vector3D position, bool anti_node)
		{
			if (def == null) throw new ArgumentNullException("ParticleDef cannot be null");
			this.Duration = (def.Duration == 0) ? animation_duration : def.Duration;
			this.ParticleDefinition = def;
			this.JumpGate = jump_gate;
			this.TargetGate = target_gate;
			this.ControllerSettings = controller_settings;
			matrix.Translation = position;
			this.IsAntiNode = anti_node;
			this.ParticleEffects = new List<MyParticleEffect>();
			this.ParticleTransientIDs = new List<byte>();
			this.ParticleRotations = new List<Vector3D>();
			if (this.ParticleDefinition.ParticleNames == null) return;

			Dictionary<byte, TransientParticle> transient_particles = Particle.TransientParticles.GetValueOrDefault(this.JumpGate, null);

			for (int i = 0; i < this.ParticleDefinition.ParticleNames.Length; ++i)
			{
				string particle_name = this.ParticleDefinition.ParticleNames[i];
				byte transient_id = (this.ParticleDefinition.TransientIDs != null && i < this.ParticleDefinition.TransientIDs.Length) ? this.ParticleDefinition.TransientIDs[i] : (byte) 0;
				MyParticleEffect effect;

				if (transient_id != 0 && transient_particles != null && transient_particles.ContainsKey(transient_id))
				{
					TransientParticle transient = transient_particles[transient_id];
					transient.ParticleEffect.StopEmitting();
					transient.ParticleEffect.StopLights();
					this.ParticleEffects.Add(transient.ParticleEffect);
					this.ParticleTransientIDs.Add(transient_id);
					this.ParticleRotations.Add(transient.ParticleRotation);
				}
				else if (MyParticlesManager.TryCreateParticleEffect(particle_name, ref matrix, ref position, uint.MaxValue, out effect))
				{
					effect.StopEmitting();
					effect.StopLights();
					this.ParticleEffects.Add(effect);
					this.ParticleTransientIDs.Add(transient_id);
					this.ParticleRotations.Add(Vector3D.Zero);
				}
			}
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Ticks this particle effect
		/// </summary>
		/// <param name="current_tick">The parent animation's current tick</param>
		/// <param name="source">The matrix defining a particle's position or null to calculate</param>
		/// <param name="drives">The list of drives belonging to this gate</param>
		/// <param name="entities">The list of entities within this gate's jump space</param>
		/// <param name="endpoint">The gate's targeted endpoint</param>
		/// <param name="this_entity">This entity or null if not bound to an entity</param>
		public void Tick(ushort current_tick, MatrixD? source, List<MyJumpGateDrive> drives, List<MyEntity> entities, ref Vector3D endpoint, MyEntity this_entity = null)
		{
			if (this.ParticleEffects == null || this.ParticleEffects.Count == 0) return;
			else if (current_tick >= this.ParticleDefinition.StartTime && current_tick <= this.ParticleDefinition.StartTime + this.Duration)
			{
				if (current_tick >= this.ParticleDefinition.StartTime) foreach (MyParticleEffect effect in this.ParticleEffects) effect.Play();

				ushort local_tick = (ushort) (current_tick - this.ParticleDefinition.StartTime);
				Vector3D rotations_per_second = Vector3D.Zero;
				Vector3D offset = this.ParticleDefinition.ParticleOffset;
				Vector4D rps, off;

				float birth_mp = (float) AttributeAnimationDef.GetAnimatedDoubleValue(this.ParticleDefinition.Animations?.ParticleBirthAnimation, local_tick, this.Duration, this.JumpGate, this.TargetGate, drives, entities, ref endpoint, this_entity?.WorldMatrix.Translation, this_entity, 1);
				float color_intensity_mp = (float) AttributeAnimationDef.GetAnimatedDoubleValue(this.ParticleDefinition.Animations?.ParticleColorIntensityAnimation, local_tick, this.Duration, this.JumpGate, this.TargetGate, drives, entities, ref endpoint, this_entity?.WorldMatrix.Translation, this_entity, 1);
				float fade_mp = (float) AttributeAnimationDef.GetAnimatedDoubleValue(this.ParticleDefinition.Animations?.ParticleFadeAnimation, local_tick, this.Duration, this.JumpGate, this.TargetGate, drives, entities, ref endpoint, this_entity?.WorldMatrix.Translation, this_entity, 1);
				float life_mp = (float) AttributeAnimationDef.GetAnimatedDoubleValue(this.ParticleDefinition.Animations?.ParticleLifeAnimation, local_tick, this.Duration, this.JumpGate, this.TargetGate, drives, entities, ref endpoint, this_entity?.WorldMatrix.Translation, this_entity, 1);
				float radius_mp = (float) AttributeAnimationDef.GetAnimatedDoubleValue(this.ParticleDefinition.Animations?.ParticleRadiusAnimation, local_tick, this.Duration, this.JumpGate, this.TargetGate, drives, entities, ref endpoint, this_entity?.WorldMatrix.Translation, this_entity, 1);
				float scale_mp = (float) AttributeAnimationDef.GetAnimatedDoubleValue(this.ParticleDefinition.Animations?.ParticleScaleAnimation, local_tick, this.Duration, this.JumpGate, this.TargetGate, drives, entities, ref endpoint, this_entity?.WorldMatrix.Translation, this_entity, 1);
				float velocity_mp = (float) AttributeAnimationDef.GetAnimatedDoubleValue(this.ParticleDefinition.Animations?.ParticleVelocityAnimation, local_tick, this.Duration, this.JumpGate, this.TargetGate, drives, entities, ref endpoint, this_entity?.WorldMatrix.Translation, this_entity, 1);

				Vector4D color = AttributeAnimationDef.GetAnimatedVectorValue(this.ParticleDefinition.Animations?.ParticleColorAnimation, local_tick, this.Duration, this.JumpGate, this.TargetGate, drives, entities, ref endpoint, this_entity?.WorldMatrix.Translation, this_entity, Vector4D.One);
				color *= this.ControllerSettings?.JumpEffectAnimationColorShift().ToVector4D() ?? Vector4D.One;

				rps = AttributeAnimationDef.GetAnimatedVectorValue(this.ParticleDefinition.Animations?.ParticleRotationSpeedAnimation, local_tick, this.Duration, this.JumpGate, this.TargetGate, drives, entities, ref endpoint, this_entity?.WorldMatrix.Translation, this_entity, Vector4D.Zero);
				rotations_per_second = new Vector3D(rps.X, rps.Y, rps.Z) * 360d / 60d * (Math.PI / 180d);

				off = AttributeAnimationDef.GetAnimatedVectorValue(this.ParticleDefinition.Animations?.ParticleOffsetAnimation, local_tick, this.Duration, this.JumpGate, this.TargetGate, drives, entities, ref endpoint, this_entity?.WorldMatrix.Translation, this_entity, new Vector4D(offset, 0));
				offset = new Vector3D(off.X, off.Y, off.Z);

				MatrixD base_matrix = source ?? ParticleOrientationDef.GetJumpGateMatrix(this.JumpGate, this.TargetGate, this.IsAntiNode, ref endpoint, this.ParticleDefinition.ParticleOrientation);
				
				for (int i = 0; i < this.ParticleEffects.Count; ++i)
				{
					MyParticleEffect effect = this.ParticleEffects[i];
					Vector3D rotation = this.ParticleRotations[i] + rotations_per_second;
					MatrixD particle_matrix = MatrixD.CreateFromYawPitchRoll(rotation.Y, rotation.Z, rotation.X) * base_matrix;
					particle_matrix.Translation += MyJumpGateModSession.LocalVectorToWorldVectorD(ref particle_matrix, offset);
					this.ParticleRotations[i] = rotation;
					effect.UserBirthMultiplier = birth_mp;
					effect.UserColorIntensityMultiplier = color_intensity_mp;
					effect.UserFadeMultiplier = fade_mp;
					effect.UserLifeMultiplier = life_mp;
					effect.UserRadiusMultiplier = radius_mp;
					effect.UserScale = scale_mp;
					effect.UserVelocityMultiplier = velocity_mp;
					effect.WorldMatrix = particle_matrix;
					effect.UserColorMultiplier = color;
					effect.SetDirty();
				}
			}
			else if (current_tick > this.ParticleDefinition.StartTime + this.Duration)
			{
				this.Stop();
				if (this.ParticleDefinition.CleanOnEffectEnd) this.Clean();
			}
		}

		/// <summary>
		/// Stops this effect fully<br />
		/// Effect is cleaned and cannot be replayed
		/// </summary>
		public void Clean()
		{
			if (this.ParticleEffects == null) return;
			foreach (MyParticleEffect effect in this.ParticleEffects) effect.Stop();
			if (Particle.TransientParticles.ContainsKey(this.JumpGate))
				foreach (TransientParticle effect in Particle.TransientParticles[this.JumpGate].Values) if (!effect.ParticleEffect.IsStopped) effect.ParticleEffect.Stop();
			Particle.TransientParticles.Remove(this.JumpGate);
			this.ParticleEffects.Clear();
			this.ParticleTransientIDs.Clear();
			this.ParticleRotations.Clear();
			this.ParticleRotations = null;
			this.ParticleTransientIDs = null;
			this.ParticleEffects = null;
			this.ParticleDefinition = null;
			this.JumpGate = null;
			this.TargetGate = null;
			this.ControllerSettings = null;
		}

		/// <summary>
		/// Stops this effect temporarily
		/// </summary>
		public void Stop()
		{
			if (this.ParticleEffects == null) return;
			Dictionary<byte, TransientParticle> transient_particles = Particle.TransientParticles.GetValueOrNew(this.JumpGate);

			for (int i = 0; i < this.ParticleEffects.Count; ++i)
			{
				MyParticleEffect effect = this.ParticleEffects[i];
				byte transient_id = this.ParticleTransientIDs[i];
				effect.StopEmitting();
				effect.StopLights();
				if (transient_id == 0) continue;
				TransientParticle old_effect = transient_particles.GetValueOrDefault(transient_id, null);
				if (old_effect != null && old_effect.ParticleEffect != effect) old_effect.ParticleEffect.Stop();
				transient_particles[transient_id] = new TransientParticle(effect, this.ParticleRotations[i]);
			}
		}
		#endregion
	}

	/// <summary>
	/// Implementation holding functionality for sound definitions
	/// </summary>
	internal class Sound
	{
		#region Private Variables
		/// <summary>
		/// Whether this sound should be played at the gate's anti-node
		/// </summary>
		private readonly bool IsAntiNode;

		/// <summary>
		/// The duration of this sound effect in game ticks
		/// </summary>
		private readonly ushort Duration;

		/// <summary>
		/// The gate's assigned sound emitter ID
		/// </summary>
		private List<ulong?> SoundIDs = null;

		/// <summary>
		/// The calling jump gate
		/// </summary>
		private MyJumpGate JumpGate;

		/// <summary>
		/// The targeted jump gate or null
		/// </summary>
		private MyJumpGate TargetGate;

		/// <summary>
		/// The sound emitters for non-gate sounds
		/// </summary>
		private List<MyEntity3DSoundEmitter> SoundEmitters = null;
		#endregion

		#region Public Variables
		/// <summary>
		/// The sound definition
		/// </summary>
		public SoundDef SoundDefinition { get; private set; }
		#endregion

		#region Constructors
		/// <summary>
		/// Creates a new sound effect
		/// </summary>
		/// <param name="def">The sound definition</param>
		/// <param name="jump_gate">The calling jump gate</param>
		/// <param name="target_gate">The targeted jump gate or null</param>
		/// <param name="anti_node">Whether this sound should play at the anti-node</param>
		/// <exception cref="ArgumentNullException">If the sound definition or jump gate is null</exception>
		public Sound(SoundDef def, ushort animation_duration, MyJumpGate jump_gate, MyJumpGate target_gate, bool anti_node)
		{
			if (def == null) throw new ArgumentNullException("SoundDef cannot be null");
			if (jump_gate == null) throw new ArgumentNullException("MyJumpGate cannot be null");
			this.SoundDefinition = def;
			this.JumpGate = jump_gate;
			this.TargetGate = target_gate;
			this.Duration = (def.Duration == 0) ? animation_duration : def.Duration;
			this.IsAntiNode = anti_node;
			this.SoundIDs = new List<ulong?>();
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Ticks this sound effect
		/// </summary>
		/// <param name="current_tick">The parent animation's current tick</param>
		/// <param name="drives">The list of drives belonging to this gate</param>
		/// <param name="entities">The list of entities within this gate's jump space</param>
		/// <param name="endpoint">The gate's targeted endpoint</param>
		public void Tick(ushort current_tick, List<MyJumpGateDrive> drives, List<MyEntity> entities, ref Vector3D endpoint, MyEntity source)
		{
			if (this.JumpGate == null || this.JumpGate.Closed) return;

			if (current_tick >= this.SoundDefinition.StartTime && current_tick <= this.SoundDefinition.StartTime + this.Duration)
			{
				bool is_start = current_tick == this.SoundDefinition.StartTime;
				ushort local_tick = (ushort) (current_tick - this.SoundDefinition.StartTime);

				if (is_start && source != null)
				{
					this.SoundEmitters = new List<MyEntity3DSoundEmitter>();

					foreach (string sound_name in this.SoundDefinition.SoundNames)
					{
						MyEntity3DSoundEmitter emitter = new MyEntity3DSoundEmitter(source);
						emitter.PlaySound(new MySoundPair(sound_name), true, alwaysHearOnRealistic: true, force3D: true);
						this.SoundEmitters.Add(emitter);
					}
				}
				else if (is_start && this.IsAntiNode) foreach (string sound_name in this.SoundDefinition.SoundNames) this.SoundIDs.Add(this.JumpGate.PlaySound(sound_name, pos: endpoint));
				else if (is_start) foreach (string sound_name in this.SoundDefinition.SoundNames) this.SoundIDs.Add(this.JumpGate.PlaySound(sound_name));

				float volume = this.SoundDefinition.Volume;
				float? distance = this.SoundDefinition.Distance;

				volume = (float) AttributeAnimationDef.GetAnimatedDoubleValue(this.SoundDefinition.Animations?.SoundVolumeAnimation, local_tick, this.Duration, this.JumpGate, this.TargetGate, drives, entities, ref endpoint, source?.WorldMatrix.Translation, source, volume);
				if (this.SoundDefinition.Animations?.SoundDistanceAnimation != null) distance = (float) AttributeAnimationDef.GetAnimatedDoubleValue(this.SoundDefinition.Animations?.SoundDistanceAnimation, local_tick, this.Duration, this.JumpGate, this.TargetGate, drives, entities, ref endpoint, source?.WorldMatrix.Translation, source);

				if (this.SoundEmitters == null)
				{
					foreach (ulong? sound_id in this.SoundIDs)
					{
						this.JumpGate.SetSoundVolume(sound_id, volume);
						this.JumpGate.SetSoundDistance(sound_id, distance);
					}
				}
				else
				{
					foreach (MyEntity3DSoundEmitter emitter in this.SoundEmitters)
					{
						emitter.VolumeMultiplier = volume;
						emitter.CustomMaxDistance = distance;
						emitter.SetPosition(source?.WorldMatrix.Translation);
					}
				}
			}
			else if (current_tick > this.SoundDefinition.StartTime + this.Duration) this.Stop();
		}

		/// <summary>
		/// Stops this sound fully<br />
		/// Sound is cleaned and cannot be replayed
		/// </summary>
		public void Clean()
		{
			this.Stop();
			this.SoundIDs = null;
			this.JumpGate = null;
			this.TargetGate = null;
			this.SoundDefinition = null;
			this.SoundEmitters = null;
		}

		/// <summary>
		/// Stops this sound temporarily
		/// </summary>
		public void Stop()
		{
			foreach (ulong? sound_id in this.SoundIDs) this.JumpGate.StopSound(sound_id);
			if (this.SoundEmitters != null) foreach (MyEntity3DSoundEmitter emitter in this.SoundEmitters) emitter.StopSound(true);
			this.SoundIDs.Clear();
			this.SoundEmitters?.Clear();
		}
		#endregion
	}

	/// <summary>
	/// Implementation holding functionality for beam pulse definitions
	/// </summary>
	internal class BeamPulse
	{
		#region Private Variables
		/// <summary>
		/// The duration of this beam pulse effect in game ticks
		/// </summary>
		private readonly ushort Duration;

		/// <summary>
		/// The calling jump gate
		/// </summary>
		private MyJumpGate JumpGate;

		/// <summary>
		/// The targeted jump gate or null
		/// </summary>
		private MyJumpGate TargetGate;

		/// <summary>
		/// The calling jump gate's controller settings
		/// </summary>
		private MyJumpGateController.MyControllerBlockSettingsStruct ControllerSettings;

		/// <summary>
		/// The beam material
		/// </summary>
		private MyStringId? BeamMaterial = null;
		#endregion

		#region Public Variables
		/// <summary>
		/// The beam pulse definition
		/// </summary>
		public BeamPulseDef BeamPulseDefinition { get; private set; }
		#endregion

		#region Constructors
		/// <summary>
		/// Creates a new Beam Pulse effect
		/// </summary>
		/// <param name="def">The beam pulse definition</param>
		/// <param name="animation_duration">The parent animation duration in game ticks</param>
		/// <param name="jump_gate">The calling jump gate</param>
		/// <param name="target_gate">The targeted jump gate or null</param>
		/// <param name="controller_settings">The calling jump gate's controller settings</param>
		/// <exception cref="ArgumentNullException">If the beam pulse definition is null</exception>
		public BeamPulse(BeamPulseDef def, ushort animation_duration, MyJumpGate jump_gate, MyJumpGate target_gate, MyJumpGateController.MyControllerBlockSettingsStruct controller_settings)
		{
			if (def == null) throw new ArgumentNullException("BeamPulseDef cannot be null");
			this.BeamPulseDefinition = def;
			this.JumpGate = jump_gate;
			this.TargetGate = target_gate;
			this.ControllerSettings = controller_settings;
			this.Duration = (def.Duration == 0) ? animation_duration : def.Duration;
			if (this.BeamPulseDefinition.Material != null) this.BeamMaterial = MyStringId.GetOrCompute(this.BeamPulseDefinition.Material);
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Ticks this beam pulse effect
		/// </summary>
		/// <param name="current_tick">The parent animation's current tick</param>
		/// <param name="drives">The list of drives belonging to this gate</param>
		/// <param name="entities">The list of entities within this gate's jump space</param>
		/// <param name="endpoint">The gate's targeted endpoint</param>
		/// <param name="jump_node">The calling gate's world jump node</param>
		public void Tick(ushort current_tick, List<MyJumpGateDrive> drives, List<MyEntity> entities, ref Vector3D endpoint, ref Vector3D jump_node)
		{
			if (!this.JumpGate.Closed && current_tick >= this.BeamPulseDefinition.StartTime && current_tick <= this.BeamPulseDefinition.StartTime + this.Duration && this.Duration > 0)
			{
				ushort local_tick = (ushort) (current_tick - this.BeamPulseDefinition.StartTime);
				ushort travel_time = Math.Max((ushort) 0, this.BeamPulseDefinition.TravelTime);
				double tick_ratio = (travel_time == 0) ? 1 : ((double) local_tick / travel_time);
				double frequency = Math.Max(0, AttributeAnimationDef.GetAnimatedDoubleValue(this.BeamPulseDefinition.Animations?.BeamFrequencyAnimation, local_tick, this.Duration, this.JumpGate, this.TargetGate, drives, entities, ref endpoint, null, null, this.BeamPulseDefinition.BeamFrequency));
				double beam_length = AttributeAnimationDef.GetAnimatedDoubleValue(this.BeamPulseDefinition.Animations?.ParticleLifeAnimation, local_tick, this.Duration, this.JumpGate, this.TargetGate, drives, entities, ref endpoint, null, null, this.BeamPulseDefinition.BeamLength);
				double duty_cycle = MathHelper.Clamp(AttributeAnimationDef.GetAnimatedDoubleValue(this.BeamPulseDefinition.Animations?.BeamDutyCycleAnimation, local_tick, this.Duration, this.JumpGate, this.TargetGate, drives, entities, ref endpoint, null, null, this.BeamPulseDefinition.BeamDutyCycle), 0, 1);
				double offset = AttributeAnimationDef.GetAnimatedDoubleValue(this.BeamPulseDefinition.Animations?.BeamOffsetAnimation, local_tick, this.Duration, this.JumpGate, this.TargetGate, drives, entities, ref endpoint, null, null, this.BeamPulseDefinition.BeamOffset);
				
				Vector3D beam_dir = endpoint - jump_node;
				Vector3D beam_dir_n = beam_dir.Normalized();
				Vector3D offset_vec = beam_dir_n * offset;
				beam_dir -= offset_vec;
				beam_length = (beam_length < 0) ? beam_dir.Length() : beam_length;
				Vector3D beam_end = jump_node + beam_dir * tick_ratio;
				Vector3D beam_start = ((Vector3D.Distance(beam_end, jump_node) <= beam_length) ? jump_node : (beam_end - beam_dir_n * beam_length)) + offset_vec;

				double beam_width;
				Vector4 beam_color;

				if (frequency == 0)
				{
					beam_width = Math.Abs(beam_width = AttributeAnimationDef.GetAnimatedDoubleValue(this.BeamPulseDefinition.Animations?.ParticleRadiusAnimation, local_tick, this.Duration, this.JumpGate, this.TargetGate, drives, entities, ref endpoint, null, null, this.BeamPulseDefinition.BeamWidth));
					beam_color = AttributeAnimationDef.GetAnimatedVectorValue(this.BeamPulseDefinition.Animations?.ParticleColorAnimation, local_tick, this.Duration, this.JumpGate, this.TargetGate, drives, entities, ref endpoint, null, null, this.BeamPulseDefinition.BeamColor.ToVector4D()) * new Vector4D(new Vector3D(Math.Abs(this.BeamPulseDefinition.BeamBrightness)), 1) * (this.ControllerSettings?.JumpEffectAnimationColorShift().ToVector4D() ?? Vector4D.One);
					MySimpleObjectDraw.DrawLine(beam_start, beam_end, this.BeamMaterial, ref beam_color, (float) beam_width);
					return;
				}

				double beam_dir_length = beam_dir.Length();
				double waveform = (beam_dir_length - (beam_dir_length - beam_length)) / frequency;
				double w0 = waveform * duty_cycle;
				double w1 = waveform - w0;
				Vector3D delta = beam_dir_n * waveform;
				Vector3D on_delta = beam_dir_n * w0;
				beam_dir_length = 0;

				for (double i = 0; i < frequency; ++i)
				{
					beam_width = Math.Abs(beam_width = AttributeAnimationDef.GetAnimatedDoubleValue(this.BeamPulseDefinition.Animations?.ParticleRadiusAnimation, local_tick, this.Duration, this.JumpGate, this.TargetGate, drives, entities, ref endpoint, beam_start, null, this.BeamPulseDefinition.BeamWidth));
					beam_color = AttributeAnimationDef.GetAnimatedVectorValue(this.BeamPulseDefinition.Animations?.ParticleColorAnimation, local_tick, this.Duration, this.JumpGate, this.TargetGate, drives, entities, ref endpoint, beam_start, null, this.BeamPulseDefinition.BeamColor.ToVector4D()) * new Vector4D(new Vector3D(Math.Abs(this.BeamPulseDefinition.BeamBrightness)), 1) * (this.ControllerSettings?.JumpEffectAnimationColorShift().ToVector4D() ?? Vector4D.One);
					beam_dir_length += waveform;
					beam_end = beam_start + ((beam_dir_length > beam_length) ? (beam_dir_n * (waveform - (beam_dir_length - beam_length))) : on_delta);
					MySimpleObjectDraw.DrawLine(beam_start, beam_end, this.BeamMaterial, ref beam_color, (float) beam_width);
					beam_start += delta;
				}
			}
		}

		/// <summary>
		/// Stops this effect fully<br />
		/// Effect is cleaned and cannot be replayed
		/// </summary>
		public void Clean()
		{
			this.BeamPulseDefinition = null;
			this.JumpGate = null;
			this.TargetGate = null;
			this.ControllerSettings = null;
			this.BeamMaterial = null;
		}
		#endregion
	}

	/// <summary>
	/// Implementation holding functionality for drive emitter emissive color animations
	/// </summary>
	internal class DriveEmissiveColor
	{
		#region Private Variables
		/// <summary>
		/// The duration of this drive emissive effect in game ticks
		/// </summary>
		private readonly ushort Duration;

		/// <summary>
		/// The calling jump gate
		/// </summary>
		private MyJumpGate JumpGate;

		/// <summary>
		/// The targeted jump gate or null
		/// </summary>
		private MyJumpGate TargetGate;

		/// <summary>
		/// The calling jump gate's controller settings
		/// </summary>
		private MyJumpGateController.MyControllerBlockSettingsStruct ControllerSettings;

		/// <summary>
		/// Map mapping all drives to animate with their initial emitter emissive colors
		/// </summary>
		private readonly Dictionary<long, Color> InitialDriveColors = new Dictionary<long, Color>();
		#endregion

		#region Public Variables
		/// <summary>
		/// The emitter emissive color definition
		/// </summary>
		public DriveEmissiveColorDef DriveEmissiveColorDef { get; private set; }
		#endregion

		#region Constructors
		/// <summary>
		/// Creates a new emitter emissive color effect
		/// </summary>
		/// <param name="def">The emitter emissive color definition</param>
		/// <param name="animation_duration">The parent animation duration in game ticks</param>
		/// <param name="jump_gate">The calling jump gate</param>
		/// <param name="target_gate">The targeted jump gate or null</param>
		/// <param name="controller_settings">The calling jump gate's controller settings</param>
		/// <exception cref="ArgumentNullException">If the emitter emissive color definition is null</exception>
		public DriveEmissiveColor(DriveEmissiveColorDef def, ushort animation_duration, MyJumpGate jump_gate, MyJumpGate target_gate, MyJumpGateController.MyControllerBlockSettingsStruct controller_settings)
		{
			if (def == null) throw new ArgumentNullException("DriveEmissiveColorDef cannot be null");
			this.DriveEmissiveColorDef = def;
			this.JumpGate = jump_gate;
			this.TargetGate = target_gate;
			this.ControllerSettings = controller_settings;
			this.Duration = (def.Duration == 0) ? animation_duration : def.Duration;
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Ticks this emitter emissive color effect
		/// </summary>
		/// <param name="current_tick">The parent animation's current tick</param>
		/// <param name="drives">The list of drives belonging to this gate</param>
		/// <param name="entities">The list of entities within this gate's jump space</param>
		/// <param name="endpoint">The gate's targeted endpoint</param>
		public void Tick(ushort current_tick, List<MyJumpGateDrive> drives, List<MyEntity> entities, ref Vector3D endpoint)
		{
			if (!this.JumpGate.Closed && current_tick >= this.DriveEmissiveColorDef.StartTime && current_tick <= this.DriveEmissiveColorDef.StartTime + this.Duration && this.Duration > 0)
			{
				double tick = current_tick - this.DriveEmissiveColorDef.StartTime;
				ushort local_tick = (ushort) (current_tick - this.DriveEmissiveColorDef.StartTime);
				double tick_ratio = MathHelperD.Clamp((this.Duration == 0) ? tick : (tick / this.Duration), 0, 1);
				List<MyJumpGateDrive> working_drives = drives.Where((drive) => drive != null && drive.IsWorking()).ToList();

				foreach (MyJumpGateDrive drive in working_drives)
				{
					if (!this.InitialDriveColors.ContainsKey(drive.TerminalBlock.EntityId)) this.InitialDriveColors.Add(drive.TerminalBlock.EntityId, drive.DriveEmitterColor);
					double brightness = this.DriveEmissiveColorDef.Brightness;
					Vector4D color = this.DriveEmissiveColorDef.EmissiveColor.ToVector4D() * (this.ControllerSettings?.JumpEffectAnimationColorShift().ToVector4D() ?? Vector4D.One);

					color = AttributeAnimationDef.GetAnimatedVectorValue(this.DriveEmissiveColorDef.Animations?.ParticleColorAnimation, local_tick, this.Duration, this.JumpGate, this.TargetGate, drives, entities, ref endpoint, null, null, color);
					brightness = AttributeAnimationDef.GetAnimatedDoubleValue(this.DriveEmissiveColorDef.Animations?.ParticleColorIntensityAnimation, local_tick, this.Duration, this.JumpGate, this.TargetGate, drives, entities, ref endpoint, null, null, brightness);

					Vector4D start = this.InitialDriveColors[drive.TerminalBlock.EntityId].ToVector4();
					Vector4D result = (color - start) * tick_ratio + start;
					drive.EmitterEmissiveBrightness = brightness;
					drive.SetDriveEmitterColor(new Vector4((float) result.X, (float) result.Y, (float) result.Z, (float) result.W));
				}
			}
		}

		/// <summary>
		/// Stops this effect fully<br />
		/// Effect is cleaned and cannot be replayed
		/// </summary>
		public void Clean()
		{
			this.JumpGate = null;
			this.TargetGate = null;
			this.ControllerSettings = null;
			this.DriveEmissiveColorDef = null;
		}
		#endregion
	}

	/// <summary>
	/// Implementation holding functionality for node physics definitions
	/// </summary>
	internal class NodePhysics
	{
		#region Private Variables
		/// <summary>
		/// Whether node physics should be placed at the gate's anti-node
		/// </summary>
		private readonly bool IsAntiNode;

		/// <summary>
		/// The duration of this node physics effect in game ticks
		/// </summary>
		private readonly ushort Duration;

		/// <summary>
		/// The calling jump gate
		/// </summary>
		private MyJumpGate JumpGate;

		/// <summary>
		/// The targeted jump gate or null
		/// </summary>
		private MyJumpGate TargetGate;
		#endregion

		#region Public Variables
		/// <summary>
		/// The node physics definition
		/// </summary>
		public NodePhysicsDef NodePhysicsDefinition { get; private set; }
		#endregion

		#region Constructors
		/// <summary>
		/// Creates a new node physics effect
		/// </summary>
		/// <param name="def">The node physics definition</param>
		/// <param name="animation_duration">The parent animation duration in game ticks</param>
		/// <param name="jump_gate">The calling jump gate</param>
		/// <param name="target_gate">The targeted jump gate or null</param>
		/// <param name="anti_node">Whether node physics should be placed at the anti-node</param>
		/// <exception cref="ArgumentNullException">If the node physics definition is null</exception>
		public NodePhysics(NodePhysicsDef def, ushort animation_duration, MyJumpGate jump_gate, MyJumpGate target_gate, bool anti_node)
		{
			if (def == null) throw new ArgumentNullException("NodePhysicsDef cannot be null");
			this.NodePhysicsDefinition = def;
			this.Duration = (def.Duration == 0) ? animation_duration : def.Duration;
			this.IsAntiNode = anti_node;
			this.JumpGate = jump_gate;
			this.TargetGate = target_gate;
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Ticks this node physics effect
		/// </summary>
		/// <param name="current_tick">The parent animation's current tick</param>
		/// <param name="drives">The list of drives belonging to this gate</param>
		/// <param name="entities">The list of entities within this gate's jump space</param>
		/// <param name="endpoint">The gate's targeted endpoint</param>
		public void Tick(ushort current_tick, List<MyJumpGateDrive> drives, List<MyEntity> entities, ref Vector3D endpoint)
		{
			if (!this.JumpGate.Closed && current_tick >= this.NodePhysicsDefinition.StartTime && current_tick <= this.NodePhysicsDefinition.StartTime + this.Duration && this.Duration > 0)
			{
				ushort local_tick = (ushort) (current_tick - this.NodePhysicsDefinition.StartTime);
				Vector3D jump_node = this.JumpGate.WorldJumpNode;
				double attractor_force = this.NodePhysicsDefinition.AttractorForce;
				double attractor_force_falloff = this.NodePhysicsDefinition.AttractorForceFalloff;
				Vector4D force_offset = new Vector4D(this.NodePhysicsDefinition.ForceOffset, 0);
				double max_speed = this.NodePhysicsDefinition.MaxSpeed;
				Vector4D torque = new Vector4D(this.NodePhysicsDefinition.AttractorTorque, 0);

				attractor_force = AttributeAnimationDef.GetAnimatedDoubleValue(this.NodePhysicsDefinition.Animations?.PhysicsForceAnimation, local_tick, this.Duration, this.JumpGate, this.TargetGate, drives, entities, ref endpoint, null, null, attractor_force);
				attractor_force_falloff = AttributeAnimationDef.GetAnimatedDoubleValue(this.NodePhysicsDefinition.Animations?.PhysicsForceFalloffAnimation, local_tick, this.Duration, this.JumpGate, this.TargetGate, drives, entities, ref endpoint, null, null, attractor_force_falloff);
				max_speed = AttributeAnimationDef.GetAnimatedDoubleValue(this.NodePhysicsDefinition.Animations?.PhysicsForceMaxSpeedAnimation, local_tick, this.Duration, this.JumpGate, this.TargetGate, drives, entities, ref endpoint, null, null, max_speed);

				force_offset = AttributeAnimationDef.GetAnimatedVectorValue(this.NodePhysicsDefinition.Animations?.PhysicsForceOffsetAnimation, local_tick, this.Duration, this.JumpGate, this.TargetGate, drives, entities, ref endpoint, null, null, force_offset);
				torque = AttributeAnimationDef.GetAnimatedVectorValue(this.NodePhysicsDefinition.Animations?.PhysicsForceTorqueAnimation, local_tick, this.Duration, this.JumpGate, this.TargetGate, drives, entities, ref endpoint, null, null, torque);

				if (attractor_force != 0)
				{
					Vector3D offset = new Vector3D(force_offset.X, force_offset.Y, force_offset.Z);
					Vector3D? _torque = null;
					float? speed = null;
					if (max_speed > 0) speed = (float) max_speed;
					if (torque != Vector4D.Zero) _torque = new Vector3D(torque.X, torque.Y, torque.Z);
					double max_distance = this.JumpGate.JumpEllipse.Radii.Max();

					foreach (MyEntity entity in entities)
					{
						if (entity.GetPhysicsBody() == null) continue;
						Vector3D force_dir = ((((this.IsAntiNode) ? endpoint : jump_node) - entity.WorldMatrix.Translation).Normalized() + offset) * attractor_force;
						Vector3D mass_center;

						try
						{
							mass_center = entity.GetPhysicsBody().CenterOfMassWorld;
						}
						catch (NullReferenceException)
						{
							mass_center = entity.WorldMatrix.Translation;
						}

						double distance = Vector3D.Distance(mass_center, jump_node);
						double ratio = Math.Pow(1 - MathHelperD.Clamp(distance / max_distance, 0, 1), attractor_force_falloff);
						entity.GetPhysicsBody().AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, force_dir * ratio, mass_center, _torque, speed);
					}
				}
			}
		}

		/// <summary>
		/// Stops this effect fully<br />
		/// Effect is cleaned and cannot be replayed
		/// </summary>
		public void Clean()
		{
			this.NodePhysicsDefinition = null;
			this.JumpGate = null;
			this.TargetGate = null;
		}
		#endregion
	}
	#endregion

	#region Animation Implementations
	/// <summary>
	/// Base class holding functionality for a single animation
	/// </summary>
	internal class MyAnimation
	{
		#region Protected Variables
		/// <summary>
		/// If true, animation will be cleaned automatically on end
		/// </summary>
		protected bool DoCleanOnEnd = false;

		/// <summary>
		/// If true, animation is stopped
		/// </summary>
		protected bool StopActive { get; private set; } = false;

		/// <summary>
		/// If true, animation is stopped and cleaned
		/// </summary>
		protected bool Cleaned { get; private set; } = false;

		/// <summary>
		/// The jump type of the calling jump gate
		/// </summary>
		protected MyJumpTypeEnum JumpType { get; private set; }

		/// <summary>
		/// Map mapping all jump gate drives with their associated particle effects
		/// </summary>
		protected Dictionary<MyJumpGateDrive, List<Particle>> PerDriveParticles { get; private set; } = new Dictionary<MyJumpGateDrive, List<Particle>>();

		/// <summary>
		/// Map mapping all jump gate drives with their associated particle effects
		/// </summary>
		protected Dictionary<MyJumpGateDrive, List<Particle>> PerAntiDriveParticles { get; private set; } = new Dictionary<MyJumpGateDrive, List<Particle>>();

		/// <summary>
		/// Map mapping all jump space entities with their associated particle effects
		/// </summary>
		protected Dictionary<MyEntity, List<Particle>> PerEntityParticles { get; private set; } = new Dictionary<MyEntity, List<Particle>>();

		/// <summary>
		/// List of the jump node particle effects
		/// </summary>
		protected List<Particle> NodeParticles { get; private set; } = new List<Particle>();

		/// <summary>
		/// List of the anti-node particle effects
		/// </summary>
		protected List<Particle> AntiNodeParticles { get; private set; } = new List<Particle>();

		/// <summary>
		/// List of the anti-node sound effects
		/// </summary>
		protected List<Sound> AntiNodeSounds { get; private set; } = new List<Sound>();

		/// <summary>
		/// List of the jump node sound effects
		/// </summary>
		protected List<Sound> NodeSounds { get; private set; } = new List<Sound>();

		/// <summary>
		/// List of closed entities
		/// </summary>
		protected List<MyEntity> ClosedEntities { get; private set; } = new List<MyEntity>();

		/// <summary>
		/// List of closed drives
		/// </summary>
		protected List<MyJumpGateDrive> ClosedDrives { get; private set; } = new List<MyJumpGateDrive>();

		/// <summary>
		/// The drive emitter emissive color animation or null
		/// </summary>
		protected DriveEmissiveColor DriveColor;

		/// <summary>
		/// The node physics or null
		/// </summary>
		protected NodePhysics NodePhysics;

		/// <summary>
		/// The anti-node physics or null
		/// </summary>
		protected NodePhysics AntiNodePhysics;

		/// <summary>
		/// The calling jump gate
		/// </summary>
		protected MyJumpGate JumpGate { get; private set; }

		/// <summary>
		/// The targeted jump gate or null
		/// </summary>
		protected MyJumpGate TargetGate { get; private set; }

		/// <summary>
		/// The calling jump gate's controller settings
		/// </summary>
		protected MyJumpGateController.MyControllerBlockSettingsStruct ControllerSettings { get; private set; }
		#endregion

		#region Public Variables
		/// <summary>
		/// The current tick of this animation
		/// </summary>
		public ushort CurrentTick { get; protected set; } = 0;

		/// <summary>
		/// Whether this animation is playing
		/// </summary>
		public bool IsPlaying { get; protected set; } = false;
		#endregion

		#region Constructors
		/// <summary>
		/// Creates a new MyAnimation
		/// </summary>
		/// <param name="jump_gate">The calling jump gate</param>
		/// <param name="target_gate">The targeted jump gate or null</param>
		/// <param name="controller_settings">The calling jump gate's controller settings</param>
		/// <param name="jump_type">The jump type of the calling gate</param>
		/// <exception cref="ArgumentNullException">If the jump gate is null or closed</exception>
		protected MyAnimation(MyJumpGate jump_gate, MyJumpGate target_gate, MyJumpGateController.MyControllerBlockSettingsStruct controller_settings, MyJumpTypeEnum jump_type)
		{
			if (jump_gate == null || jump_gate.Closed) throw new ArgumentNullException("Invalid jump gate");
			this.JumpGate = jump_gate;
			this.TargetGate = target_gate;
			this.JumpType = jump_type;
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Ticks this animation, ticking all sounds, particles, physics, and other effects
		/// </summary>
		/// <param name="endpoint">The calling jump gate's targeted endpoint (may be affected by normal override)</param>
		/// <param name="anti_node">The calling jump gate's true targeted endpoint</param>
		/// <param name="jump_gate_drives">The calling jump gate's associated drives</param>
		/// <param name="target_jump_gate_drives">The targeted jump gate's associated drives</param>
		/// <param name="jump_gate_entities">The calling jump gate's jump space entities</param>
		/// <param name="world_jump_node">The calling jump gate's world jump node</param>
		public virtual void Tick(ref Vector3D endpoint, ref Vector3D anti_node, ref Vector3D world_jump_node, List<MyJumpGateDrive> jump_gate_drives, List<MyJumpGateDrive> target_jump_gate_drives, List<MyEntity> jump_gate_entities) { }

		/// <summary>
		/// Stops this animation
		/// </summary>
		/// <param name="full_close">Whther to clean this animation</param>
		public void Stop(bool full_close = false)
		{
			this.StopActive = true;
			this.Clean(full_close);
		}

		/// <summary>
		/// Stops all effects in this animation
		/// </summary>
		/// <param name="full_close">Whether to fully clean all effects</param>
		public virtual void Clean(bool full_close = true)
		{
			this.PerDriveParticles.Values.ToList().ForEach((list) => list.ForEach((particle) => particle.Stop()));
			this.PerAntiDriveParticles.Values.ToList().ForEach((list) => list.ForEach((particle) => particle.Stop()));
			this.PerEntityParticles.Values.ToList().ForEach((list) => list.ForEach((particle) => particle.Stop()));
			this.NodeParticles.ForEach((particle) => particle.Stop());
			this.AntiNodeParticles.ForEach((particle) => particle.Stop());
			this.NodeSounds.ForEach((sound) => sound.Stop());
			this.AntiNodeSounds.ForEach((sound) => sound.Stop());
			this.ClosedEntities.Clear();
			this.ClosedDrives.Clear();

			if (full_close)
			{
				if (this.JumpGate != null && !this.JumpGate.Closed)
				{
					List<MyJumpGateDrive> drives = new List<MyJumpGateDrive>();
					this.JumpGate.GetJumpGateDrives(drives);
					foreach (MyJumpGateDrive drive in drives) drive.CycleDriveEmitter(drive.DriveEmitterColor, Color.Black, 300);
				}

				this.DoCleanOnEnd = false;
				this.PerDriveParticles.Values.ToList().ForEach((list) => list.ForEach((particle) => particle.Clean()));
				this.PerAntiDriveParticles.Values.ToList().ForEach((list) => list.ForEach((particle) => particle.Clean()));
				this.PerEntityParticles.Values.ToList().ForEach((list) => list.ForEach((particle) => particle.Clean()));
				this.NodeParticles.ForEach((particle) => particle.Clean());
				this.AntiNodeParticles.ForEach((particle) => particle.Clean());
				this.NodeSounds.ForEach((sound) => sound.Clean());
				this.AntiNodeSounds.ForEach((sound) => sound.Clean());
				this.DriveColor?.Clean();
				this.PerDriveParticles.Clear();
				this.PerAntiDriveParticles.Clear();
				this.PerEntityParticles.Clear();
				this.NodeParticles.Clear();
				this.AntiNodeParticles.Clear();
				this.NodeSounds.Clear();
				this.AntiNodeSounds.Clear();

				this.JumpGate = null;
				this.TargetGate = null;
				this.ControllerSettings = null;
				this.DriveColor = null;
				this.NodePhysics = null;
				this.ClosedEntities = null;
				this.ClosedDrives = null;
				this.PerDriveParticles = null;
				this.PerAntiDriveParticles = null;
				this.PerEntityParticles = null;
				this.NodeParticles = null;
				this.AntiNodeParticles = null;
				this.NodeSounds = null;
				this.AntiNodeSounds = null;
				this.Cleaned = true;
				Logger.Debug("Animation Cleaned!", 2);
			}
		}

		/// <summary>
		/// Marks this animation to clean on end<br />
		/// If not playing, this animtaion is cleaned immediatly
		/// </summary>
		public void CleanOnEnd()
		{
			this.DoCleanOnEnd = true;
			if (!this.IsPlaying) this.Clean();
		}

		/// <summary>
		/// Restarts this animaton<br />
		/// <i>Has no effect if this animation is cleaned</i>
		/// </summary>
		public void Restart()
		{
			if (this.Cleaned)
			{
				Logger.Warn("Attempt to restart fully cleaned animation");
				return;
			}

			this.StopActive = false;
			this.CurrentTick = 0;
		}

		/// <summary>
		/// </summary>
		/// <returns>True if this animation is stopped</returns>
		public bool Stopped()
		{
			return this.StopActive || this.CurrentTick > this.Duration();
		}

		/// <summary>
		/// Overridable<br />
		/// Override this to add set this animation's total duration in game ticks<br />
		/// </summary>
		/// <returns>The duration of this animation in game ticks</returns>
		public virtual ushort Duration()
		{
			return 0;
		}
		#endregion
	}

	internal class MyJumpGateJumpingAnimation : MyAnimation
	{
		#region Public Variables
		/// <summary>
		/// The animation definition for the "jumping/charging" animation
		/// </summary>
		public JumpGateJumpingAnimationDef AnimationDefinition { get; private set; }
		#endregion

		#region Constructors
		/// <summary>
		/// Creates a new MyJumpGateJumpingAnimation
		/// </summary>
		/// <param name="def">The "jumping" animation definition</param>
		/// <param name="jump_gate">The calling jump gate</param>
		/// <param name="target_gate">The targered jump gate or null</param>
		/// <param name="controller_settings">The calling jump gate's controller settings</param>
		/// <param name="endpoint">The calling jump gate's targeted endpoint (may be affected by normal override)</param>
		/// <param name="anti_node">The calling jump gate's true targeted endpoint</param>
		/// <param name="jump_type">The jump type of the calling gate</param>
		/// <exception cref="ArgumentNullException">If the definition or jump gate are null</exception>
		public MyJumpGateJumpingAnimation(JumpGateJumpingAnimationDef def, MyJumpGate jump_gate, MyJumpGate target_gate, MyJumpGateController.MyControllerBlockSettingsStruct controller_settings, ref Vector3D endpoint, ref Vector3D anti_node, MyJumpTypeEnum jump_type) : base(jump_gate, target_gate, controller_settings, jump_type)
		{
			if (def == null || jump_gate == null) throw new ArgumentNullException("Definition is null");
			this.AnimationDefinition = def;
			this.DriveColor = (def.DriveEmissiveColor == null) ? null : new DriveEmissiveColor(def.DriveEmissiveColor, def.Duration, jump_gate, target_gate, controller_settings);
			this.NodePhysics = (def.NodePhysics == null) ? null : new NodePhysics(def.NodePhysics, def.Duration, jump_gate, target_gate, false);
			this.AntiNodePhysics = (def.AntiNodePhysics == null) ? null : new NodePhysics(def.AntiNodePhysics, def.Duration, jump_gate, target_gate, true);

			if (def.NodeSounds != null) foreach (SoundDef sound_def in def.NodeSounds) this.NodeSounds.Add(new Sound(sound_def, def.Duration, jump_gate, target_gate, false));
			if (def.AntiNodeSounds != null) foreach (SoundDef sound_def in def.AntiNodeSounds) this.AntiNodeSounds.Add(new Sound(sound_def, def.Duration, jump_gate, target_gate, true));
			if (def.NodeParticles != null) foreach (ParticleDef particle_def in def.NodeParticles) this.NodeParticles.Add(new Particle(particle_def, def.Duration, jump_gate, target_gate, controller_settings, ParticleOrientationDef.GetJumpGateMatrix(jump_gate, target_gate, false, ref endpoint, particle_def.ParticleOrientation), jump_gate.WorldJumpNode, false));
			if (def.AntiNodeParticles != null) foreach (ParticleDef particle_def in def.AntiNodeParticles) this.AntiNodeParticles.Add(new Particle(particle_def, def.Duration, jump_gate, target_gate, controller_settings, ParticleOrientationDef.GetJumpGateMatrix(jump_gate, target_gate, true, ref anti_node, particle_def.ParticleOrientation), anti_node, true));;
		}
		#endregion

		#region Public Methods
		public override void Tick(ref Vector3D endpoint, ref Vector3D anti_node, ref Vector3D world_jump_node, List<MyJumpGateDrive> jump_gate_drives, List<MyJumpGateDrive> target_jump_gate_drives, List<MyEntity> jump_gate_entities)
		{
			base.Tick(ref endpoint, ref anti_node, ref world_jump_node, jump_gate_drives, target_jump_gate_drives, jump_gate_entities);

			if (this.CurrentTick > this.AnimationDefinition.Duration || this.StopActive)
			{
				this.IsPlaying = false;
				if (this.DoCleanOnEnd) this.Clean();
				return;
			}

			this.IsPlaying = true;

			if (!MyNetworkInterface.IsDedicatedMultiplayerServer)
			{
				if (this.AnimationDefinition.PerEntityParticles != null && (this.JumpType == MyJumpTypeEnum.STANDARD || this.JumpType == MyJumpTypeEnum.OUTBOUND_VOID))
				{
					this.ClosedEntities.AddRange(this.PerEntityParticles.Keys);

					foreach (MyEntity entity in jump_gate_entities)
					{
						if (this.PerEntityParticles.ContainsKey(entity)) this.ClosedEntities.Remove(entity);
						else this.PerEntityParticles.Add(entity, this.AnimationDefinition.PerEntityParticles.Select((particle) => new Particle(particle, this.AnimationDefinition.Duration, this.JumpGate, this.TargetGate, this.ControllerSettings, entity.WorldMatrix, entity.WorldMatrix.Translation, false)).ToList());
						foreach (Particle particle in this.PerEntityParticles[entity]) particle.Tick(this.CurrentTick, entity.WorldMatrix, jump_gate_drives, jump_gate_entities, ref endpoint, entity);
					}

					foreach (MyEntity entity in this.ClosedEntities)
					{
						foreach (Particle particle in this.PerEntityParticles[entity]) particle.Stop();
						this.PerEntityParticles.Remove(entity);
					}

					this.ClosedEntities.Clear();
				}

				if (this.AnimationDefinition.PerDriveParticles != null && jump_gate_drives != null && (this.JumpType == MyJumpTypeEnum.STANDARD || this.JumpType == MyJumpTypeEnum.OUTBOUND_VOID))
				{
					this.ClosedDrives.AddRange(this.PerDriveParticles.Keys);

					foreach (MyJumpGateDrive drive in jump_gate_drives)
					{
						if (!drive.IsWorking()) continue;
						MatrixD drive_emitter_pos = drive.TerminalBlock.WorldMatrix;
						drive_emitter_pos.Translation = drive.GetDriveRaycastStartpoint();
						if (this.PerDriveParticles.ContainsKey(drive)) this.ClosedDrives.Remove(drive);
						else this.PerDriveParticles.Add(drive, this.AnimationDefinition.PerDriveParticles.Select((particle) => new Particle(particle, this.AnimationDefinition.Duration, this.JumpGate, this.TargetGate, this.ControllerSettings, drive.TerminalBlock.WorldMatrix, drive_emitter_pos.Translation, false)).ToList());
						foreach (Particle particle in this.PerDriveParticles[drive]) particle.Tick(this.CurrentTick, drive_emitter_pos, jump_gate_drives, jump_gate_entities, ref endpoint, drive.TerminalBlock as MyEntity);
					}

					foreach (MyJumpGateDrive drive in this.ClosedDrives)
					{
						foreach (Particle particle in this.PerDriveParticles[drive]) particle.Stop();
						this.PerDriveParticles.Remove(drive);
					}

					this.ClosedDrives.Clear();
				}

				if (this.AnimationDefinition.PerAntiDriveParticles != null && target_jump_gate_drives != null && (this.JumpType == MyJumpTypeEnum.STANDARD || this.JumpType == MyJumpTypeEnum.INBOUND_VOID))
				{
					this.ClosedDrives.AddRange(this.PerAntiDriveParticles.Keys);

					foreach (MyJumpGateDrive drive in target_jump_gate_drives)
					{
						if (!drive.IsWorking()) continue;
						MatrixD drive_emitter_pos = drive.TerminalBlock.WorldMatrix;
						drive_emitter_pos.Translation = drive.GetDriveRaycastStartpoint();
						if (this.PerAntiDriveParticles.ContainsKey(drive)) this.ClosedDrives.Remove(drive);
						else this.PerAntiDriveParticles.Add(drive, this.AnimationDefinition.PerAntiDriveParticles.Select((particle) => new Particle(particle, this.AnimationDefinition.Duration, this.JumpGate, this.TargetGate, this.ControllerSettings, drive.TerminalBlock.WorldMatrix, drive_emitter_pos.Translation, false)).ToList());
						foreach (Particle particle in this.PerAntiDriveParticles[drive]) particle.Tick(this.CurrentTick, drive_emitter_pos, jump_gate_drives, jump_gate_entities, ref endpoint, drive.TerminalBlock as MyEntity);
					}

					foreach (MyJumpGateDrive drive in this.ClosedDrives)
					{
						foreach (Particle particle in this.PerAntiDriveParticles[drive]) particle.Stop();
						this.PerAntiDriveParticles.Remove(drive);
					}

					this.ClosedDrives.Clear();
				}

				if (this.JumpType == MyJumpTypeEnum.STANDARD || this.JumpType == MyJumpTypeEnum.OUTBOUND_VOID)
				{
					foreach (Particle particle in this.NodeParticles) particle.Tick(this.CurrentTick, null, jump_gate_drives, jump_gate_entities, ref endpoint);
					foreach (Sound sound in this.NodeSounds) sound.Tick(this.CurrentTick, jump_gate_drives, jump_gate_entities, ref endpoint, null);
					this.NodePhysics?.Tick(this.CurrentTick, jump_gate_drives, jump_gate_entities, ref endpoint);
					this.DriveColor?.Tick(this.CurrentTick, jump_gate_drives, jump_gate_entities, ref endpoint);
				}

				if (this.JumpType == MyJumpTypeEnum.STANDARD || this.JumpType == MyJumpTypeEnum.INBOUND_VOID)
				{
					foreach (Particle particle in this.AntiNodeParticles) particle.Tick(this.CurrentTick, null, jump_gate_drives, jump_gate_entities, ref anti_node);
					foreach (Sound sound in this.AntiNodeSounds) sound.Tick(this.CurrentTick, jump_gate_drives, jump_gate_entities, ref anti_node, null);
					this.AntiNodePhysics?.Tick(this.CurrentTick, jump_gate_drives, jump_gate_entities, ref anti_node);
				}
			}

			++this.CurrentTick;
		}

		public override ushort Duration()
		{
			return this.AnimationDefinition.Duration;
		}
		#endregion
	}

	internal class MyJumpGateJumpedAnimation : MyAnimation
	{
		#region Private Variables
		/// <summary>
		/// The beam pulse definition
		/// </summary>
		private BeamPulse Beam;

		/// <summary>
		/// The travel particle effects
		/// </summary>
		private List<Particle> TravelParticles = null;

		/// <summary>
		/// The travel sound effects
		/// </summary>
		private List<Sound> TravelSounds = null;

		/// <summary>
		/// A list of the attached jump gate's batch entities
		/// </summary>
		private List<MyEntity> JumpedEntities = new List<MyEntity>();
		#endregion

		#region Public Variables
		/// <summary>
		/// The animation definition for the "jumped" animation
		/// </summary>
		public JumpGateJumpedAnimationDef AnimationDefinition { get; private set; }
		#endregion

		#region Constructors
		/// <summary>
		/// Creates a new MyJumpGateJumpedAnimation
		/// </summary>
		/// <param name="def">The "jumped" animation definition</param>
		/// <param name="jump_gate">The calling jump gate</param>
		/// <param name="target_gate">The targered jump gate or null</param>
		/// <param name="controller_settings">The calling jump gate's controller settings</param>
		/// <param name="endpoint">The calling jump gate's targeted endpoint (may be affected by normal override)</param>
		/// <param name="anti_node">The calling jump gate's true targeted endpoint</param>
		/// <param name="jump_type">The jump type of the calling gate</param>
		/// <exception cref="ArgumentNullException">If the definition or jump gate are null</exception>
		public MyJumpGateJumpedAnimation(JumpGateJumpedAnimationDef def, MyJumpGate jump_gate, MyJumpGate target_gate, MyJumpGateController.MyControllerBlockSettingsStruct controller_settings, ref Vector3D endpoint, ref Vector3D anti_node, MyJumpTypeEnum jump_type) : base(jump_gate, target_gate, controller_settings, jump_type)
		{
			if (def == null || jump_gate == null) throw new ArgumentNullException("Definition is null");
			this.AnimationDefinition = def;
			this.DriveColor = (def.DriveEmissiveColor == null) ? null : new DriveEmissiveColor(def.DriveEmissiveColor, def.Duration, jump_gate, target_gate, controller_settings);
			this.NodePhysics = (def.NodePhysics == null) ? null : new NodePhysics(def.NodePhysics, def.Duration, jump_gate, target_gate, false);
			this.AntiNodePhysics = (def.AntiNodePhysics == null) ? null : new NodePhysics(def.AntiNodePhysics, def.Duration, jump_gate, target_gate, true);
			this.Beam = (def.BeamPulse == null) ? null : new BeamPulse(def.BeamPulse, def.Duration, jump_gate, target_gate, controller_settings);

			if (def.NodeSounds != null) foreach (SoundDef sound_def in def.NodeSounds) this.NodeSounds.Add(new Sound(sound_def, def.Duration, jump_gate, target_gate, false));
			if (def.AntiNodeSounds != null) foreach (SoundDef sound_def in def.AntiNodeSounds) this.AntiNodeSounds.Add(new Sound(sound_def, def.Duration, jump_gate, target_gate, true));
			if (def.NodeParticles != null) foreach (ParticleDef particle_def in def.NodeParticles) this.NodeParticles.Add(new Particle(particle_def, def.Duration, jump_gate, target_gate, controller_settings, ParticleOrientationDef.GetJumpGateMatrix(jump_gate, target_gate, false, ref endpoint, particle_def.ParticleOrientation), jump_gate.WorldJumpNode, false));
			if (def.AntiNodeParticles != null) foreach (ParticleDef particle_def in def.AntiNodeParticles) this.AntiNodeParticles.Add(new Particle(particle_def, def.Duration, jump_gate, target_gate, controller_settings, ParticleOrientationDef.GetJumpGateMatrix(jump_gate, target_gate, true, ref anti_node, particle_def.ParticleOrientation), anti_node, true));
		}
		#endregion

		#region Public Methods
		public override void Tick(ref Vector3D endpoint, ref Vector3D anti_node, ref Vector3D world_jump_node, List<MyJumpGateDrive> jump_gate_drives, List<MyJumpGateDrive> target_jump_gate_drives, List<MyEntity> jump_gate_entities)
		{
			base.Tick(ref endpoint, ref anti_node, ref world_jump_node, jump_gate_drives, target_jump_gate_drives, jump_gate_entities);

			if (this.CurrentTick > this.AnimationDefinition.Duration || this.StopActive)
			{
				this.IsPlaying = false;
				if (this.DoCleanOnEnd) this.Clean();
				return;
			}

			this.IsPlaying = true;

			if (!MyNetworkInterface.IsDedicatedMultiplayerServer)
			{
				if (this.AnimationDefinition.PerEntityParticles != null && (this.JumpType == MyJumpTypeEnum.STANDARD || this.JumpType == MyJumpTypeEnum.OUTBOUND_VOID))
				{
					this.ClosedEntities.AddRange(this.PerEntityParticles.Keys);

					foreach (MyEntity entity in jump_gate_entities)
					{
						if (this.PerEntityParticles.ContainsKey(entity)) this.ClosedEntities.Remove(entity);
						else this.PerEntityParticles.Add(entity, this.AnimationDefinition.PerEntityParticles.Select((particle) => new Particle(particle, this.AnimationDefinition.Duration, this.JumpGate, this.TargetGate, this.ControllerSettings, entity.WorldMatrix, entity.WorldMatrix.Translation, false)).ToList());
						foreach (Particle particle in this.PerEntityParticles[entity]) particle.Tick(this.CurrentTick, entity.WorldMatrix, jump_gate_drives, jump_gate_entities, ref endpoint, entity);
					}

					foreach (MyEntity entity in this.ClosedEntities)
					{
						foreach (Particle particle in this.PerEntityParticles[entity]) particle.Stop();
						this.PerEntityParticles.Remove(entity);
					}

					this.ClosedEntities.Clear();
				}
				
				MyEntity controller = (MyNetworkInterface.IsDedicatedMultiplayerServer) ? null : MyAPIGateway.Session.CameraController?.Entity?.GetTopMostParent();
				EntityBatch batch = this.JumpGate.GetEntityBatchFromEntity(controller);

				if (batch == null && this.TravelParticles != null)
				{
					foreach (Particle particle in this.TravelParticles) particle.Stop();
					this.TravelParticles.Clear();
					this.TravelParticles = null;
				}
				else if (batch != null && this.AnimationDefinition.TravelEffects != null && (this.JumpType == MyJumpTypeEnum.STANDARD || this.JumpType == MyJumpTypeEnum.OUTBOUND_VOID))
				{
					ushort duration = this.Beam?.BeamPulseDefinition?.Duration ?? this.AnimationDefinition.Duration;
					MyEntity parent = batch.Parent;
					this.TravelParticles = this.TravelParticles ?? this.AnimationDefinition.TravelEffects.Select((particle) => new Particle(particle, duration, this.JumpGate, this.TargetGate, this.ControllerSettings, parent.WorldMatrix, parent.WorldMatrix.Translation, false)).ToList();
					
					foreach (Particle particle in this.TravelParticles)
					{
						MatrixD source = ParticleOrientationDef.GetJumpGateMatrix(this.JumpGate, this.TargetGate, false, ref endpoint, particle.ParticleDefinition.ParticleOrientation);
						source.Translation = parent.WorldMatrix.Translation;
						particle.Tick(this.CurrentTick, source, jump_gate_drives, this.JumpedEntities, ref endpoint, parent);
					}
				}

				if (batch == null && this.TravelSounds != null)
				{
					foreach (Sound sound in this.TravelSounds) sound.Stop();
					this.TravelSounds.Clear();
					this.TravelSounds = null;
				}
				else if (batch != null && this.AnimationDefinition.TravelSounds != null && (this.JumpType == MyJumpTypeEnum.STANDARD || this.JumpType == MyJumpTypeEnum.OUTBOUND_VOID))
				{
					ushort duration = this.Beam?.BeamPulseDefinition?.Duration ?? this.AnimationDefinition.Duration;
					MyEntity parent = batch.Parent;
					this.TravelSounds = this.TravelSounds ?? this.AnimationDefinition.TravelSounds.Select((sound) => new Sound(sound, duration, this.JumpGate, this.TargetGate, false)).ToList();
					foreach (Sound sound in this.TravelSounds) sound.Tick(this.CurrentTick, jump_gate_drives, this.JumpedEntities, ref endpoint, parent);
				}

				if (this.AnimationDefinition.PerDriveParticles != null && jump_gate_drives != null && (this.JumpType == MyJumpTypeEnum.STANDARD || this.JumpType == MyJumpTypeEnum.OUTBOUND_VOID))
				{
					this.ClosedDrives.AddRange(this.PerDriveParticles.Keys);

					foreach (MyJumpGateDrive drive in jump_gate_drives)
					{
						if (!drive.IsWorking()) continue;
						MatrixD drive_emitter_pos = drive.TerminalBlock.WorldMatrix;
						drive_emitter_pos.Translation = drive.GetDriveRaycastStartpoint();
						if (this.PerDriveParticles.ContainsKey(drive)) this.ClosedDrives.Remove(drive);
						else this.PerDriveParticles.Add(drive, this.AnimationDefinition.PerDriveParticles.Select((particle) => new Particle(particle, this.AnimationDefinition.Duration, this.JumpGate, this.TargetGate, this.ControllerSettings, drive.TerminalBlock.WorldMatrix, drive_emitter_pos.Translation, false)).ToList());
						foreach (Particle particle in this.PerDriveParticles[drive]) particle.Tick(this.CurrentTick, drive_emitter_pos, jump_gate_drives, jump_gate_entities, ref endpoint, drive.TerminalBlock as MyEntity);
					}

					foreach (MyJumpGateDrive drive in this.ClosedDrives)
					{
						foreach (Particle particle in this.PerDriveParticles[drive]) particle.Stop();
						this.PerDriveParticles.Remove(drive);
					}

					this.ClosedDrives.Clear();
				}

				if (this.AnimationDefinition.PerAntiDriveParticles != null && target_jump_gate_drives != null && (this.JumpType == MyJumpTypeEnum.STANDARD || this.JumpType == MyJumpTypeEnum.INBOUND_VOID))
				{
					this.ClosedDrives.AddRange(this.PerAntiDriveParticles.Keys);

					foreach (MyJumpGateDrive drive in target_jump_gate_drives)
					{
						if (!drive.IsWorking()) continue;
						MatrixD drive_emitter_pos = drive.TerminalBlock.WorldMatrix;
						drive_emitter_pos.Translation = drive.GetDriveRaycastStartpoint();
						if (this.PerAntiDriveParticles.ContainsKey(drive)) this.ClosedDrives.Remove(drive);
						else this.PerAntiDriveParticles.Add(drive, this.AnimationDefinition.PerAntiDriveParticles.Select((particle) => new Particle(particle, this.AnimationDefinition.Duration, this.JumpGate, this.TargetGate, this.ControllerSettings, drive.TerminalBlock.WorldMatrix, drive_emitter_pos.Translation, false)).ToList());
						foreach (Particle particle in this.PerAntiDriveParticles[drive]) particle.Tick(this.CurrentTick, drive_emitter_pos, jump_gate_drives, jump_gate_entities, ref endpoint, drive.TerminalBlock as MyEntity);
					}

					foreach (MyJumpGateDrive drive in this.ClosedDrives)
					{
						foreach (Particle particle in this.PerAntiDriveParticles[drive]) particle.Stop();
						this.PerAntiDriveParticles.Remove(drive);
					}

					this.ClosedDrives.Clear();
				}

				if (batch == null) this.Beam?.Tick(this.CurrentTick, jump_gate_drives, jump_gate_entities, ref endpoint, ref world_jump_node);

				if (this.JumpType == MyJumpTypeEnum.STANDARD || this.JumpType == MyJumpTypeEnum.OUTBOUND_VOID)
				{
					foreach (Particle particle in this.NodeParticles) particle.Tick(this.CurrentTick, null, jump_gate_drives, jump_gate_entities, ref endpoint);
					foreach (Sound sound in this.NodeSounds) sound.Tick(this.CurrentTick, jump_gate_drives, jump_gate_entities, ref endpoint, null);
					this.NodePhysics?.Tick(this.CurrentTick, jump_gate_drives, jump_gate_entities, ref endpoint);
					this.DriveColor?.Tick(this.CurrentTick, jump_gate_drives, jump_gate_entities, ref endpoint);
				}

				if (this.JumpType == MyJumpTypeEnum.STANDARD || this.JumpType == MyJumpTypeEnum.INBOUND_VOID)
				{
					foreach (Particle particle in this.AntiNodeParticles) particle.Tick(this.CurrentTick, null, jump_gate_drives, jump_gate_entities, ref anti_node);
					foreach (Sound sound in this.AntiNodeSounds) sound.Tick(this.CurrentTick, jump_gate_drives, jump_gate_entities, ref anti_node, null);
					this.AntiNodePhysics?.Tick(this.CurrentTick, jump_gate_drives, jump_gate_entities, ref anti_node);
				}
			}

			++this.CurrentTick;
		}

		public override ushort Duration()
		{
			return this.AnimationDefinition.Duration;
		}

		public override void Clean(bool full_close = true)
		{
			if (this.TravelParticles != null)
			{
				foreach (Particle particle in this.TravelParticles)
				{
					if (full_close) particle.Clean();
					else particle.Stop();
				}
			}

			if (this.TravelSounds != null)
			{
				foreach (Sound sound in this.TravelSounds)
				{
					if (full_close) sound.Clean();
					else sound.Stop();
				}
			}

			this.TravelParticles?.Clear();
			this.TravelSounds?.Clear();
			this.JumpedEntities.Clear();
			this.TravelParticles = null;
			this.TravelSounds = null;

			if (full_close)
			{
				this.Beam?.Clean();
				this.Beam = null;
				this.JumpedEntities = null;
			}

			base.Clean(full_close);
		}
		#endregion
	}

	internal class MyJumpGateFailedAnimation : MyAnimation
	{
		#region Public Variables
		/// <summary>
		/// The animation definition for the "failed" animation
		/// </summary>
		public JumpGateFailedAnimationDef AnimationDefinition { get; private set; }
		#endregion

		#region Constructors
		/// <summary>
		/// Creates a new JumpGateFailedAnimationDef
		/// </summary>
		/// <param name="def">The "failed" animation definition</param>
		/// <param name="jump_gate">The calling jump gate</param>
		/// <param name="target_gate">The targered jump gate or null</param>
		/// <param name="controller_settings">The calling jump gate's controller settings</param>
		/// <param name="endpoint">The calling jump gate's targeted endpoint (may be affected by normal override)</param>
		/// <param name="anti_node">The calling jump gate's true targeted endpoint</param>
		/// <param name="jump_type">The jump type of the calling gate</param>
		/// <exception cref="ArgumentNullException">If the definition or jump gate are null</exception>
		public MyJumpGateFailedAnimation(JumpGateFailedAnimationDef def, MyJumpGate jump_gate, MyJumpGate target_gate, MyJumpGateController.MyControllerBlockSettingsStruct controller_settings, ref Vector3D endpoint, ref Vector3D anti_node, MyJumpTypeEnum jump_type) : base(jump_gate, target_gate, controller_settings, jump_type)
		{
			if (def == null || jump_gate == null) throw new ArgumentNullException("Definition is null");
			this.AnimationDefinition = def;
			this.DriveColor = (def.DriveEmissiveColor == null) ? null : new DriveEmissiveColor(def.DriveEmissiveColor, def.Duration, jump_gate, target_gate, controller_settings);
			this.NodePhysics = (def.NodePhysics == null) ? null : new NodePhysics(def.NodePhysics, def.Duration, jump_gate, target_gate, false);
			this.AntiNodePhysics = (def.AntiNodePhysics == null) ? null : new NodePhysics(def.AntiNodePhysics, def.Duration, jump_gate, target_gate, true);

			if (def.NodeSounds != null) foreach (SoundDef sound_def in def.NodeSounds) this.NodeSounds.Add(new Sound(sound_def, def.Duration, jump_gate, target_gate, false));
			if (def.AntiNodeSounds != null) foreach (SoundDef sound_def in def.AntiNodeSounds) this.AntiNodeSounds.Add(new Sound(sound_def, def.Duration, jump_gate, target_gate, true));
			if (def.NodeParticles != null) foreach (ParticleDef particle_def in def.NodeParticles) this.NodeParticles.Add(new Particle(particle_def, def.Duration, jump_gate, target_gate, controller_settings, ParticleOrientationDef.GetJumpGateMatrix(jump_gate, target_gate, false, ref endpoint, particle_def.ParticleOrientation), jump_gate.WorldJumpNode, false));
			if (def.AntiNodeParticles != null) foreach (ParticleDef particle_def in def.AntiNodeParticles) this.AntiNodeParticles.Add(new Particle(particle_def, def.Duration, jump_gate, target_gate, controller_settings, ParticleOrientationDef.GetJumpGateMatrix(jump_gate, target_gate, true, ref anti_node, particle_def.ParticleOrientation), anti_node, true)); ;
		}
		#endregion

		#region Public Methods
		public override void Tick(ref Vector3D endpoint, ref Vector3D anti_node, ref Vector3D world_jump_node, List<MyJumpGateDrive> jump_gate_drives, List<MyJumpGateDrive> target_jump_gate_drives, List<MyEntity> jump_gate_entities)
		{
			base.Tick(ref endpoint, ref anti_node, ref world_jump_node, jump_gate_drives, target_jump_gate_drives, jump_gate_entities);

			if (this.CurrentTick > this.AnimationDefinition.Duration || this.StopActive)
			{
				this.IsPlaying = false;
				if (this.DoCleanOnEnd) this.Clean();
				return;
			}

			this.IsPlaying = true;

			if (!MyNetworkInterface.IsDedicatedMultiplayerServer)
			{
				if (this.AnimationDefinition.PerEntityParticles != null && (this.JumpType == MyJumpTypeEnum.STANDARD || this.JumpType == MyJumpTypeEnum.OUTBOUND_VOID))
				{
					this.ClosedEntities.AddRange(this.PerEntityParticles.Keys);

					foreach (MyEntity entity in jump_gate_entities)
					{
						if (this.PerEntityParticles.ContainsKey(entity)) this.ClosedEntities.Remove(entity);
						else this.PerEntityParticles.Add(entity, this.AnimationDefinition.PerEntityParticles.Select((particle) => new Particle(particle, this.AnimationDefinition.Duration, this.JumpGate, this.TargetGate, this.ControllerSettings, entity.WorldMatrix, entity.WorldMatrix.Translation, false)).ToList());
						foreach (Particle particle in this.PerEntityParticles[entity]) particle.Tick(this.CurrentTick, entity.WorldMatrix, jump_gate_drives, jump_gate_entities, ref endpoint, entity);
					}

					foreach (MyEntity entity in this.ClosedEntities)
					{
						foreach (Particle particle in this.PerEntityParticles[entity]) particle.Stop();
						this.PerEntityParticles.Remove(entity);
					}

					this.ClosedEntities.Clear();
				}

				if (this.AnimationDefinition.PerDriveParticles != null && jump_gate_drives != null && (this.JumpType == MyJumpTypeEnum.STANDARD || this.JumpType == MyJumpTypeEnum.OUTBOUND_VOID))
				{
					this.ClosedDrives.AddRange(this.PerDriveParticles.Keys);

					foreach (MyJumpGateDrive drive in jump_gate_drives)
					{
						if (!drive.IsWorking()) continue;
						MatrixD drive_emitter_pos = drive.TerminalBlock.WorldMatrix;
						drive_emitter_pos.Translation = drive.GetDriveRaycastStartpoint();
						if (this.PerDriveParticles.ContainsKey(drive)) this.ClosedDrives.Remove(drive);
						else this.PerDriveParticles.Add(drive, this.AnimationDefinition.PerDriveParticles.Select((particle) => new Particle(particle, this.AnimationDefinition.Duration, this.JumpGate, this.TargetGate, this.ControllerSettings, drive.TerminalBlock.WorldMatrix, drive_emitter_pos.Translation, false)).ToList());
						foreach (Particle particle in this.PerDriveParticles[drive]) particle.Tick(this.CurrentTick, drive_emitter_pos, jump_gate_drives, jump_gate_entities, ref endpoint, drive.TerminalBlock as MyEntity);
					}

					foreach (MyJumpGateDrive drive in this.ClosedDrives)
					{
						foreach (Particle particle in this.PerDriveParticles[drive]) particle.Stop();
						this.PerDriveParticles.Remove(drive);
					}

					this.ClosedDrives.Clear();
				}

				if (this.AnimationDefinition.PerAntiDriveParticles != null && target_jump_gate_drives != null && (this.JumpType == MyJumpTypeEnum.STANDARD || this.JumpType == MyJumpTypeEnum.INBOUND_VOID))
				{
					this.ClosedDrives.AddRange(this.PerAntiDriveParticles.Keys);

					foreach (MyJumpGateDrive drive in target_jump_gate_drives)
					{
						if (!drive.IsWorking()) continue;
						MatrixD drive_emitter_pos = drive.TerminalBlock.WorldMatrix;
						drive_emitter_pos.Translation = drive.GetDriveRaycastStartpoint();
						if (this.PerAntiDriveParticles.ContainsKey(drive)) this.ClosedDrives.Remove(drive);
						else this.PerAntiDriveParticles.Add(drive, this.AnimationDefinition.PerAntiDriveParticles.Select((particle) => new Particle(particle, this.AnimationDefinition.Duration, this.JumpGate, this.TargetGate, this.ControllerSettings, drive.TerminalBlock.WorldMatrix, drive_emitter_pos.Translation, false)).ToList());
						foreach (Particle particle in this.PerAntiDriveParticles[drive]) particle.Tick(this.CurrentTick, drive_emitter_pos, jump_gate_drives, jump_gate_entities, ref endpoint, drive.TerminalBlock as MyEntity);
					}

					foreach (MyJumpGateDrive drive in this.ClosedDrives)
					{
						foreach (Particle particle in this.PerAntiDriveParticles[drive]) particle.Stop();
						this.PerAntiDriveParticles.Remove(drive);
					}

					this.ClosedDrives.Clear();
				}

				if (this.JumpType == MyJumpTypeEnum.STANDARD || this.JumpType == MyJumpTypeEnum.OUTBOUND_VOID)
				{
					foreach (Particle particle in this.NodeParticles) particle.Tick(this.CurrentTick, null, jump_gate_drives, jump_gate_entities, ref endpoint);
					foreach (Sound sound in this.NodeSounds) sound.Tick(this.CurrentTick, jump_gate_drives, jump_gate_entities, ref endpoint, null);
					this.NodePhysics?.Tick(this.CurrentTick, jump_gate_drives, jump_gate_entities, ref endpoint);
					this.DriveColor?.Tick(this.CurrentTick, jump_gate_drives, jump_gate_entities, ref endpoint);
				}

				if (this.JumpType == MyJumpTypeEnum.STANDARD || this.JumpType == MyJumpTypeEnum.INBOUND_VOID)
				{
					foreach (Particle particle in this.AntiNodeParticles) particle.Tick(this.CurrentTick, null, jump_gate_drives, jump_gate_entities, ref anti_node);
					foreach (Sound sound in this.AntiNodeSounds) sound.Tick(this.CurrentTick, jump_gate_drives, jump_gate_entities, ref anti_node, null);
					this.AntiNodePhysics?.Tick(this.CurrentTick, jump_gate_drives, jump_gate_entities, ref anti_node);
				}
			}

			++this.CurrentTick;
		}

		public override ushort Duration()
		{
			return this.AnimationDefinition.Duration;
		}
		#endregion
	}
	#endregion

	/// <summary>
	/// Wrapper containing the full animation for all phases
	/// </summary>
	internal class MyJumpGateAnimation : IEquatable<MyJumpGateAnimation>
	{
		public enum AnimationTypeEnum { JUMPING, JUMPED, FAILED }

		#region Private Variables
		/// <summary>
		/// The index indicating the currently playing animation
		/// </summary>
		private short ActiveAnimationIndex = -1;

		/// <summary>
		/// The jump type of the calling gate
		/// </summary>
		private MyJumpTypeEnum GateJumpType;

		/// <summary>
		/// The "jumping/charging" animation
		/// </summary>
		private MyJumpGateJumpingAnimation GateJumpingAnimation;

		/// <summary>
		/// The "jumped" animation
		/// </summary>
		private MyJumpGateJumpedAnimation GateJumpedAnimation;

		/// <summary>
		/// The "failed" animation
		/// </summary>
		private MyJumpGateFailedAnimation GateFailedAnimation;
		#endregion

		#region Temporary Collections
		private List<MyJumpGateDrive> TEMP_JumpGateDrives = new List<MyJumpGateDrive>();
		private List<MyJumpGateDrive> TEMP_JumpGateAntiDrives = new List<MyJumpGateDrive>();
		private List<MyEntity> TEMP_JumpGateEntitiesL = new List<MyEntity>();
		private Dictionary<MyEntity, float> TEMP_JumpGateEntities = new Dictionary<MyEntity, float>();
		#endregion

		#region Public Variables
		/// <summary>
		/// The calling jump gate
		/// </summary>
		public MyJumpGate JumpGate { get; private set; }

		/// <summary>
		/// The targeted jump gate or null
		/// </summary>
		public MyJumpGate TargetGate { get; private set; }

		/// <summary>
		/// The controller settings used to enact the jump
		/// </summary>
		public MyJumpGateController.MyControllerBlockSettingsStruct ControllerSettings {  get; private set; }

		/// <summary>
		/// The controller settings of the targeted jump gate or null
		/// </summary>
		public MyJumpGateController.MyControllerBlockSettingsStruct TargetControllerSettings { get; private set; }

		/// <summary>
		/// The "jumping/charging" animation definition
		/// </summary>
		public JumpGateJumpingAnimationDef GateJumpingAnimationDef { get; private set; }

		/// <summary>
		/// The "jumped" animation definition
		/// </summary>
		public JumpGateJumpedAnimationDef GateJumpedAnimationDef { get; private set; }

		/// <summary>
		/// The "failed" animation definition
		/// </summary>
		public JumpGateFailedAnimationDef GateFailedAnimationDef { get; private set; }

		/// <summary>
		/// Whether this animation can be cancelled immediatly<br />
		/// If false, animation in the jumping phase will cancel once complete
		/// </summary>
		public readonly bool ImmediateCancel;

		/// <summary>
		/// The name of this animation as defined in the animation definition
		/// </summary>
		public readonly string AnimationName;

		/// <summary>
		/// The full name of this animation as defined from the animation handler
		/// </summary>
		public readonly string FullAnimationName;
		#endregion

		#region Public Static Operators
		/// <summary>
		/// Overloads equality operator "==" to check equality
		/// </summary>
		/// <param name="a">The first MyJumpGateAnimation operand</param>
		/// <param name="b">The second MyJumpGateAnimation operand</param>
		/// <returns>Equality</returns>
		public static bool operator ==(MyJumpGateAnimation a, MyJumpGateAnimation b)
		{
			if (object.ReferenceEquals(a, b)) return true;
			else if (object.ReferenceEquals(a, null) || object.ReferenceEquals(b, null)) return false;
			else return a.Equals(b);
		}

		/// <summary>
		/// Overloads inequality operator "!=" to check inequality
		/// </summary>
		/// <param name="a">The first MyJumpGateAnimation operand</param>
		/// <param name="b">The second MyJumpGateAnimation operand</param>
		/// <returns>Inequality</returns>
		public static bool operator !=(MyJumpGateAnimation a, MyJumpGateAnimation b)
		{
			return !(a == b);
		}
		#endregion

		#region Constructors
		/// <summary>
		/// Creates a new MyJumpGateAnimation animation wrapper
		/// </summary>
		/// <param name="def">The animation definition</param>
		/// <param name="full_name">The animation's full name</param>
		/// <param name="jump_gate">The calling jump gate</param>
		/// <param name="target_gate">The targeted jump gate or null</param>
		/// <param name="controller_settings">The controller settings used to activate said jump gate</param>
		/// <param name="endpoint">The jump gate's targeted endpoint</param>
		/// <param name="jump_type">The jump type of the calling gate</param>
		public MyJumpGateAnimation(AnimationDef def, string full_name, MyJumpGate jump_gate, MyJumpGate target_gate, MyJumpGateController.MyControllerBlockSettingsStruct controller_settings, MyJumpGateController.MyControllerBlockSettingsStruct target_controller_settings, ref Vector3D endpoint, MyJumpTypeEnum jump_type)
		{
			this.GateJumpingAnimationDef = def.JumpingAnimationDef;
			this.GateJumpedAnimationDef = def.JumpedAnimationDef;
			this.GateFailedAnimationDef = def.FailedAnimationDef;
			this.JumpGate = jump_gate;
			this.TargetGate = target_gate;
			this.ControllerSettings = controller_settings ?? jump_gate?.Controller?.BlockSettings;
			this.TargetControllerSettings = target_controller_settings ?? target_gate?.Controller?.BlockSettings;
			this.AnimationName = def.AnimationName;
			this.FullAnimationName = full_name;
			this.ImmediateCancel = def.ImmediateCancel;
			this.GateJumpType = jump_type;
		}
		#endregion

		#region "object" Methods
		/// <summary>
		/// Checks if this MyJumpGateAnimation equals another
		/// </summary>
		/// <param name="other">The object to check</param>
		/// <returns>Equality</returns>
		public override bool Equals(object obj)
		{
			return this.Equals(obj as MyJumpGateAnimation);
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

		#region Public Methods
		/// <summary>
		/// Cleans this animation fully<br />
		/// MyAnimation cannot be replayed
		/// </summary>
		public void Clean()
		{
			this.GateJumpingAnimation?.Clean();
			this.GateJumpedAnimation?.Clean();
			this.GateFailedAnimation?.Clean();
			this.ActiveAnimationIndex = -1;
			this.GateJumpingAnimation = null;
			this.GateJumpedAnimation = null;
			this.GateFailedAnimation = null;
			this.GateJumpingAnimationDef = null;
			this.GateJumpedAnimationDef = null;
			this.GateFailedAnimationDef = null;
			this.JumpGate = null;
			this.TargetGate = null;
			this.ControllerSettings = null;

			this.TEMP_JumpGateDrives?.Clear();
			this.TEMP_JumpGateAntiDrives?.Clear();
			this.TEMP_JumpGateEntities?.Clear();
			this.TEMP_JumpGateEntitiesL?.Clear();

			this.TEMP_JumpGateDrives = null;
			this.TEMP_JumpGateAntiDrives = null;
			this.TEMP_JumpGateEntities = null;
			this.TEMP_JumpGateEntitiesL = null;
		}

		/// <summary>
		/// Ticks the specified effect
		/// </summary>
		/// <param name="index">The animation to tick<br />1 - Ticks jumping animation<br />2 - Ticks jumped animation<br />3 - Ticks failed animation</param>
		/// <exception cref="InvalidOperationException">If the animation index is invalid</exception>
		public void Tick(byte index)
		{
			if (index < 0 || index > 2) throw new InvalidOperationException("Invalid animation index");
			else if (this.ActiveAnimationIndex != -1 && this.ActiveAnimationIndex != index)
			{
				switch (this.ActiveAnimationIndex)
				{
					case 0:
						this.GateJumpingAnimation?.Stop();
						break;
					case 1:
						this.GateJumpedAnimation?.Stop();
						break;
					case 2:
						this.GateFailedAnimation?.Stop();
						break;
				}
			}

			this.ActiveAnimationIndex = index;
			if ((this.JumpGate?.Closed ?? true) || (this.JumpGate?.JumpGateGrid?.Closed ?? true)) return;
			List<MyJumpGateDrive> drives = null;
			this.JumpGate.GetJumpGateDrives(this.TEMP_JumpGateDrives);
			this.TEMP_JumpGateEntitiesL.AddRange(this.TEMP_JumpGateEntities.Keys);
			this.TEMP_JumpGateEntitiesL.AddRange(this.JumpGate.EntityBatches.Keys);
			drives = this.TEMP_JumpGateDrives;

			List<MyJumpGateDrive> anti_drives = null;

			if (this.GateJumpType == MyJumpTypeEnum.INBOUND_VOID)
			{
				anti_drives = drives;
				drives = null;
			}
			else if (this.TargetGate != null && !this.TargetGate.Closed && !(this.TargetGate.JumpGateGrid?.Closed ?? true))
			{
				this.TargetGate.GetJumpGateDrives(this.TEMP_JumpGateAntiDrives);
				anti_drives = this.TEMP_JumpGateAntiDrives;
			}

			Vector3D jump_node, endpoint, anti_node, target_world_jump_node;

			if (this.GateJumpType == MyJumpTypeEnum.INBOUND_VOID)
			{
				Vector3D? _startpoint = this.JumpGate.TrueEndpoint;
				if (_startpoint == null) return;
				endpoint = this.JumpGate.WorldJumpNode;
				anti_node = endpoint;
				jump_node = _startpoint.Value;
				target_world_jump_node = jump_node;
			}
			else
			{
				jump_node = this.JumpGate.WorldJumpNode;
				Vector3D? _endpoint = this.JumpGate.TrueEndpoint;
				if (_endpoint == null) return;
				endpoint = _endpoint.Value;
				anti_node = endpoint;
				target_world_jump_node = jump_node;

				if (!MyNetworkInterface.IsDedicatedMultiplayerServer && this.ControllerSettings.HasVectorNormalOverride() && Vector3D.Distance(MyAPIGateway.Session.Camera.Position, this.JumpGate.WorldJumpNode) <= this.JumpGate.JumpGateConfiguration.MinimumJumpDistance) endpoint = jump_node + this.JumpGate.GetWorldMatrix(true, true).Forward * Vector3D.Distance(jump_node, endpoint);
				if (!MyNetworkInterface.IsDedicatedMultiplayerServer && this.TargetControllerSettings != null && this.TargetGate != null && this.TargetControllerSettings.HasVectorNormalOverride() && Vector3D.Distance(MyAPIGateway.Session.Camera.Position, this.TargetGate.WorldJumpNode) <= this.JumpGate.JumpGateConfiguration.MinimumJumpDistance) target_world_jump_node = endpoint + this.TargetGate.GetWorldMatrix(false, true).Forward * Vector3D.Distance(endpoint, target_world_jump_node);
			}

			switch (this.ActiveAnimationIndex)
			{
				case 0:
					if (this.GateJumpingAnimation == null) this.GateJumpingAnimation = (this.GateJumpingAnimationDef == null) ? null : new MyJumpGateJumpingAnimation(this.GateJumpingAnimationDef, this.JumpGate, this.TargetGate, this.ControllerSettings, ref endpoint, ref anti_node, this.GateJumpType);
					this.GateJumpingAnimation?.Tick(ref endpoint, ref anti_node, ref target_world_jump_node, drives, anti_drives, this.TEMP_JumpGateEntitiesL);
					break;
				case 1:
					if (this.GateJumpedAnimation == null) this.GateJumpedAnimation = (this.GateJumpedAnimationDef == null) ? null : new MyJumpGateJumpedAnimation(this.GateJumpedAnimationDef, this.JumpGate, this.TargetGate, this.ControllerSettings, ref endpoint, ref anti_node, this.GateJumpType);
					this.GateJumpedAnimation?.Tick(ref endpoint, ref anti_node, ref target_world_jump_node, drives, anti_drives, this.TEMP_JumpGateEntitiesL);
					break;
				case 2:
					if (this.GateFailedAnimation == null) this.GateFailedAnimation = (this.GateFailedAnimationDef == null) ? null : new MyJumpGateFailedAnimation(this.GateFailedAnimationDef, this.JumpGate, this.TargetGate, this.ControllerSettings, ref endpoint, ref anti_node, this.GateJumpType);
					this.GateFailedAnimation?.Tick(ref endpoint, ref anti_node, ref target_world_jump_node, drives, anti_drives, this.TEMP_JumpGateEntitiesL);
					break;
			}

			this.TEMP_JumpGateDrives.Clear();
			this.TEMP_JumpGateAntiDrives.Clear();
			this.TEMP_JumpGateEntitiesL.Clear();
			this.TEMP_JumpGateEntities.Clear();
		}

		/// <summary>
		/// Restarts the specified animation
		/// </summary>
		/// <param name="index">The animation to restart<br />1 - Ticks jumping animation<br />2 - Ticks jumped animation<br />3 - Ticks failed animation</param>
		/// <exception cref="InvalidOperationException">If the animation index is invalid</exception>
		public void Restart(byte index)
		{
			if (index < 0 || index > 2) throw new InvalidOperationException("Invalid animation index");
			else
			{
				switch (index)
				{
					case 0:
						this.GateJumpingAnimation?.Restart();
						break;
					case 1:
						this.GateJumpedAnimation?.Restart();
						break;
					case 2:
						this.GateFailedAnimation?.Restart();
						break;
				}

				this.TEMP_JumpGateDrives.Clear();
				this.TEMP_JumpGateAntiDrives.Clear();
				this.TEMP_JumpGateEntities.Clear();
			}
		}

		/// <summary>
		/// Stops all animations<br />
		/// Animations may be restarted
		/// </summary>
		public void Stop()
		{
			this.ActiveAnimationIndex = -1;
			this.GateJumpingAnimation?.Stop();
			this.GateJumpedAnimation?.Stop();
			this.GateFailedAnimation?.Stop();

			this.TEMP_JumpGateDrives.Clear();
			this.TEMP_JumpGateAntiDrives.Clear();
			this.TEMP_JumpGateEntities.Clear();
			this.TEMP_JumpGateEntitiesL.Clear();
		}

		/// <summary>
		/// Checks if this MyJumpGateAnimation equals another
		/// </summary>
		/// <param name="other">The MyJumpGateAnimation to check</param>
		/// <returns>Equality</returns>
		public bool Equals(MyJumpGateAnimation other)
		{
			if (object.ReferenceEquals(other, null)) return false;
			else if (object.ReferenceEquals(this, other)) return true;
			else return this.FullAnimationName == other.FullAnimationName;
		}

		/// <summary>
		/// Checks if the specified animation is stopped
		/// </summary>
		/// <param name="index">The animation to restart<br />1 - Ticks jumping animation<br />2 - Ticks jumped animation<br />3 - Ticks failed animation</param>
		/// <returns>True if the specified animation is stopped</returns>
		/// <exception cref="InvalidOperationException">If the animation index is invalid</exception>
		public bool Stopped(short index)
		{
			if (index < 0 || index > 2) throw new InvalidOperationException("Invalid animation index");
			ushort current_tick = 0;
			ushort duration = 0;
			bool stopped = true;

			switch (index)
			{
				case 0:
					if (this.GateJumpingAnimation == null) break;
					current_tick = this.GateJumpingAnimation.CurrentTick;
					duration = this.GateJumpingAnimation.Duration();
					stopped = (current_tick >= duration || this.GateJumpingAnimation.Stopped()) && !this.GateJumpingAnimation.IsPlaying;
					break;
				case 1:
					if (this.GateJumpedAnimation == null) break;
					current_tick = this.GateJumpedAnimation.CurrentTick;
					duration = this.GateJumpedAnimation.Duration();
					stopped = (current_tick >= duration || this.GateJumpedAnimation.Stopped()) && !this.GateJumpedAnimation.IsPlaying;
					break;
				case 2:
					if (this.GateFailedAnimation == null) break;
					current_tick = this.GateFailedAnimation.CurrentTick;
					duration = this.GateFailedAnimation.Duration();
					stopped = (current_tick >= duration || this.GateFailedAnimation.Stopped()) && !this.GateFailedAnimation.IsPlaying;
					break;
			}

			return stopped;
		}

		/// <summary>
		/// </summary>
		/// <returns>The durations for all animation effects or 0 if there is no effect for that index</returns>
		public ushort[] Durations()
		{
			return new ushort[3] {
				this.GateJumpingAnimation?.Duration() ?? this.GateJumpingAnimationDef?.Duration ?? (ushort) 0u,
				this.GateJumpedAnimation?.Duration() ?? this.GateJumpedAnimationDef?.Duration ?? (ushort) 0u,
				this.GateFailedAnimation?.Duration() ?? this.GateFailedAnimationDef?.Duration ?? (ushort) 0u
			};
		}
		#endregion
	}

	/// <summary>
	/// Container holding all defined animations
	/// </summary>
	internal static class MyAnimationHandler
	{
		#region Private Static Variables
		/// <summary>
		/// Stores the next available subtype ID for animations
		/// </summary>
		private static ulong NextSubtypeID = 0;

		/// <summary>
		/// Holds the list of pre-load animation definitions<br />
		/// All animations definined in code will be here
		/// </summary>
		private readonly static List<AnimationDef> PreloadedAnimationDefinitions = new List<AnimationDef>();

		/// <summary>
		/// Master map mapping a full animation name with it's animation definition
		/// </summary>
		private readonly static Dictionary<string, List<AnimationDef>> Animations = new Dictionary<string, List<AnimationDef>>();
		#endregion

		#region Public Static Methods
		/// <summary>
		/// Stores a new preload animation definition
		/// </summary>
		/// <param name="animation">The animation definition to store</param>
		public static void AddAnimationDefinition(AnimationDef animation)
		{
			MyAnimationHandler.PreloadedAnimationDefinitions.Add(animation);
		}

		/// <summary>
		/// Loads all animation definitions from file(s)<br />
		/// This will scan all loaded mods and load any animations defined within them
		/// </summary>
		public static void Load()
		{
			new AnimationDefinitions();
			string animations_list_file = "Data/Animations.txt";

			foreach (MyObjectBuilder_Checkpoint.ModItem mod in MyAPIGateway.Session.Mods)
			{
				if (MyAPIGateway.Utilities.FileExistsInModLocation(animations_list_file, mod))
				{
					try
					{
						uint count = 0;
						TextReader reader = MyAPIGateway.Utilities.ReadFileInModLocation(animations_list_file, mod);
						string[] animations_list = reader.ReadToEnd().Split('\n');
						reader.Close();
						Logger.Log($"Found animations list in {mod.FriendlyName}; LOADING ANIMATIONS...");

						foreach (string animation_path in animations_list)
						{
							if (MyAPIGateway.Utilities.FileExistsInModLocation(animation_path, mod))
							{
								try
								{
									reader = MyAPIGateway.Utilities.ReadFileInModLocation(animation_path, mod);
									string serialized_animation = reader.ReadToEnd();
									reader.Close();
									AnimationDef animation = MyAPIGateway.Utilities.SerializeFromXML<AnimationDef>(serialized_animation);

									if (animation == null || !animation.Enabled)
									{
										Logger.Warn($"\tSkipped loading of NULL or DISABLED animation: {mod.FriendlyName}::{animation_path}");
										continue;
									}

									string name = (animation.AnimationName == null || animation.AnimationName.Trim().Length == 0) ? "<NULL>" : animation.AnimationName.Trim();
									string full_name = $"{mod.FriendlyName}.{animation.GetType().FullName}.{name}";

									if (MyAnimationHandler.Animations.ContainsKey(full_name)) Logger.Warn($"\tDuplicate animation: {full_name}; SKIPPED");
									else
									{
										animation.SourceMod = mod.Name;
										animation.Prepare();
										MyAnimationHandler.Animations.Add(full_name, new List<AnimationDef>() { animation });
										++count;
									}
								}
								catch (Exception e)
								{
									Logger.Error($"\tFailed to load animation at {mod.FriendlyName}::{animation_path}\n{e.Message}\n...\n{e.StackTrace}\n...\n...\n...\n{e.InnerException}");
								}
							}
							else Logger.Warn($"\tNo file at path: {mod.FriendlyName}::{animation_path}");
						}

						Logger.Log($"Loaded {count} animations from {mod.FriendlyName}");
					}
					catch (Exception e)
					{
						Logger.Error($"Failed to read animations list at {mod.FriendlyName}::{animations_list_file}\n{e.Message}\n...\n{e.StackTrace}\n...\n...\n...\n{e.InnerException}");
					}
				}
			}

			foreach (AnimationDef animation in MyAnimationHandler.PreloadedAnimationDefinitions)
			{
				if (!animation.Enabled) continue;
				string name = (animation.AnimationName == null || animation.AnimationName.Trim().Length == 0) ? "<NULL>" : animation.AnimationName.Trim();
				string full_name = $"{animation.GetType().FullName}.{name}";
				if (animation.AnimationContraint == null) animation.SubtypeID = null;
				else animation.SubtypeID = MyAnimationHandler.NextSubtypeID++;
				animation.Prepare();

				if (MyAnimationHandler.Animations.ContainsKey(full_name))
				{
					bool duplicate = MyAnimationHandler.Animations[full_name].Where((def) => def.SubtypeID == null).Any();
					if (duplicate) Logger.Warn($"Duplicate animation: {full_name}; SKIPPED");
					else MyAnimationHandler.Animations[full_name].Add(animation);
				}
				else MyAnimationHandler.Animations.Add(full_name, new List<AnimationDef>() { animation });
			}

			MyAnimationHandler.PreloadedAnimationDefinitions.Clear();
			Logger.Log($"Loaded {MyAnimationHandler.Animations.Count} animation(s)");
		}

		/// <summary>
		/// Unloads all animation definitions<br />
		/// For any animation marked for serialization, this will serialize it to XML and write it to global storage
		/// </summary>
		public static void Unload()
		{
			if (!MyNetworkInterface.IsMultiplayerClient)
			{
				foreach (KeyValuePair<string, List<AnimationDef>> pair in MyAnimationHandler.Animations)
				{
					string full_name = pair.Key;
					
					foreach (AnimationDef animation in pair.Value)
					{
						if (animation.SourceMod != null || !animation.SerializeOnEnd) continue;
						string out_file = $"{full_name}_{((animation.SubtypeID == null) ? "-1" : animation.SubtypeID.Value.ToString())}.xml";

						try
						{
							string xml = MyAPIGateway.Utilities.SerializeToXML(animation);
							TextWriter writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(out_file, MyJumpGateModSession.Instance.GetType());
							writer.Write(xml);
							writer.Close();
						}
						catch (Exception e)
						{
							Logger.Error($"\tFailed to write serialized animation at {out_file}\n{e.Message}\n...\n{e.StackTrace}\n...\n...\n...\n{e.InnerException}");
						}
					}
				}
			}

			MyAnimationHandler.Animations.Clear();
			Logger.Log($"Animations Unloaded");
		}

		/// <summary>
		/// </summary>
		/// <returns>The full names of all stored animations</returns>
		public static HashSet<string> GetAnimationNames()
		{
			return new HashSet<string>(MyAnimationHandler.Animations.Keys);
		}

		/// <summary>
		/// Gets an animation definition
		/// </summary>
		/// <param name="name">The animation's full name</param>
		/// <param name="jump_gate">The calling jump gate</param>
		/// <returns>The animation definition or null if the jump gate fails the animation's constraint</returns>
		public static AnimationDef GetAnimationDef(string name, MyJumpGate jump_gate)
		{
			List<AnimationDef> animations = MyAnimationHandler.Animations.GetValueOrDefault(name, null);
			if (animations == null) return null;
			else if (jump_gate == null) return animations.FirstOrDefault();
			else if (!jump_gate.IsValid()) return null;
			AnimationDef _default = animations.Where((animation) => animation.AnimationContraint == null).FirstOrDefault();
			AnimationDef matched = animations.Where((animation) => animation.AnimationContraint?.Validate(jump_gate) ?? false).FirstOrDefault();
			return matched ?? _default ?? null;
		}

		/// <summary>
		/// Gets a playable animation
		/// </summary>
		/// <param name="name">The animation's full name</param>
		/// <param name="jump_gate">The calling jump gate</param>
		/// <param name="target_gate">The targeted jump gate or null</param>
		/// <param name="controller_settings">The controller settings used to activate the jump gate</param>
		/// <param name="endpoint">The jump gate's targeted endpoint</param>
		/// <returns>The playabe animation wrapper</returns>
		public static MyJumpGateAnimation GetAnimation(string name, MyJumpGate jump_gate, MyJumpGate target_gate, MyJumpGateController.MyControllerBlockSettingsStruct controller_settings, MyJumpGateController.MyControllerBlockSettingsStruct target_controller_settings, ref Vector3D endpoint, MyJumpTypeEnum jump_type)
		{
			AnimationDef animation_def = MyAnimationHandler.GetAnimationDef(name, jump_gate);
			if (animation_def == null || jump_gate == null || !jump_gate.IsValid()) return null;
			return new MyJumpGateAnimation(animation_def, name, jump_gate, target_gate, controller_settings, target_controller_settings, ref endpoint, jump_type);
		}
		#endregion
	}
}
