using ProtoBuf;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRageMath;

namespace IOTA.ModularJumpGates.API.AnimationAPI.Util
{
	#region Enums
	/// <summary>
	/// Enum representing the type of easing
	/// </summary>
	public enum EasingTypeEnum
	{
		EASE_IN,
		EASE_OUT,
		EASE_IN_OUT
	};

	/// <summary>
	/// Enum representing the easing curve
	/// </summary>
	public enum EasingCurveEnum
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
	internal enum MathOperationEnum
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
	public enum ParticleOrientationEnum
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

	[ProtoContract(UseProtoMembersOnly = true)]
	public class AnimationExpression
	{
		[ProtoContract(UseProtoMembersOnly = true)]
		internal struct EvaluatedResult
		{
			[ProtoMember(1)]
			public bool IsVector;
			[ProtoMember(2)]
			public double DoubleResult;
			[ProtoMember(3)]
			public Vector4D VectorResult;

			public EvaluatedResult(double result)
			{
				this.IsVector = false;
				this.DoubleResult = result;
				this.VectorResult = default(Vector4D);
			}
			public EvaluatedResult(Vector3D result)
			{
				this.IsVector = true;
				this.DoubleResult = default(double);
				this.VectorResult = new Vector4D(result, 0);
			}
			public EvaluatedResult(Vector4D result)
			{
				this.IsVector = true;
				this.DoubleResult = default(double);
				this.VectorResult = result;
			}

			public Vector4D AsVector()
			{
				return (this.IsVector) ? this.VectorResult : new Vector4D(this.DoubleResult);
			}
			public double AsDouble()
			{
				return (this.IsVector) ? (this.VectorResult.X + this.VectorResult.Y + this.VectorResult.Z + this.VectorResult.W) / 4d : this.DoubleResult;
			}
		}

		#region Internal Variables
		[ProtoMember(1)]
		internal MathOperationEnum Operation;
		[ProtoMember(2)]
		internal List<AnimationExpression> Arguments;
		[ProtoMember(3)]
		internal double? SingleDoubleValue;
		[ProtoMember(4)]
		internal Vector4D? SingleVectorValue;
		[ProtoMember(5)]
		internal AnimationSourceEnum SingleSourceValue;
		internal readonly Func<double, double> Function;
		[ProtoMember(6)]
		internal EvaluatedResult[] ClampBounds;
		[ProtoMember(7)]
		internal RatioTypeEnum RatioType;
		#endregion

		#region Operators
		public static AnimationExpression operator +(AnimationExpression left, double right)
		{
			return new AnimationExpression(left, new AnimationExpression(right, RatioTypeEnum.NONE, null, null), MathOperationEnum.ADD);
		}
		public static AnimationExpression operator +(AnimationExpression left, Vector3D right)
		{
			return new AnimationExpression(left, new AnimationExpression(right, RatioTypeEnum.NONE, null, null), MathOperationEnum.ADD);
		}
		public static AnimationExpression operator +(AnimationExpression left, Vector4D right)
		{
			return new AnimationExpression(left, new AnimationExpression(right, RatioTypeEnum.NONE, null, null), MathOperationEnum.ADD);
		}
		public static AnimationExpression operator +(AnimationExpression left, AnimationSourceEnum right)
		{
			return new AnimationExpression(left, new AnimationExpression(right, RatioTypeEnum.NONE, (double?) null, (double?) null), MathOperationEnum.ADD);
		}
		public static AnimationExpression operator +(AnimationExpression left, AnimationExpression right)
		{
			return new AnimationExpression(left, right, MathOperationEnum.ADD);
		}

		public static AnimationExpression operator -(AnimationExpression left, double right)
		{
			return new AnimationExpression(left, new AnimationExpression(right, RatioTypeEnum.NONE, null, null), MathOperationEnum.SUBTRACT);
		}
		public static AnimationExpression operator -(AnimationExpression left, Vector3D right)
		{
			return new AnimationExpression(left, new AnimationExpression(right, RatioTypeEnum.NONE, null, null), MathOperationEnum.SUBTRACT);
		}
		public static AnimationExpression operator -(AnimationExpression left, Vector4D right)
		{
			return new AnimationExpression(left, new AnimationExpression(right, RatioTypeEnum.NONE, null, null), MathOperationEnum.SUBTRACT);
		}
		public static AnimationExpression operator -(AnimationExpression left, AnimationSourceEnum right)
		{
			return new AnimationExpression(left, new AnimationExpression(right, RatioTypeEnum.NONE, (double?) null, (double?) null), MathOperationEnum.SUBTRACT);
		}
		public static AnimationExpression operator -(AnimationExpression left, AnimationExpression right)
		{
			return new AnimationExpression(left, right, MathOperationEnum.SUBTRACT);
		}
		public static AnimationExpression operator -(AnimationExpression unary)
		{
			return new AnimationExpression(unary, MathOperationEnum.SUBTRACT);
		}

		public static AnimationExpression operator *(AnimationExpression left, double right)
		{
			return new AnimationExpression(left, new AnimationExpression(right, RatioTypeEnum.NONE, null, null), MathOperationEnum.MULTIPLY);
		}
		public static AnimationExpression operator *(AnimationExpression left, Vector3D right)
		{
			return new AnimationExpression(left, new AnimationExpression(right, RatioTypeEnum.NONE, null, null), MathOperationEnum.MULTIPLY);
		}
		public static AnimationExpression operator *(AnimationExpression left, Vector4D right)
		{
			return new AnimationExpression(left, new AnimationExpression(right, RatioTypeEnum.NONE, null, null), MathOperationEnum.MULTIPLY);
		}
		public static AnimationExpression operator *(AnimationExpression left, AnimationSourceEnum right)
		{
			return new AnimationExpression(left, new AnimationExpression(right, RatioTypeEnum.NONE, (double?) null, (double?) null), MathOperationEnum.MULTIPLY);
		}
		public static AnimationExpression operator *(AnimationExpression left, AnimationExpression right)
		{
			return new AnimationExpression(left, right, MathOperationEnum.MULTIPLY);
		}

		public static AnimationExpression operator /(AnimationExpression left, double right)
		{
			return new AnimationExpression(left, new AnimationExpression(right, RatioTypeEnum.NONE, null, null), MathOperationEnum.DIVIDE);
		}
		public static AnimationExpression operator /(AnimationExpression left, Vector3D right)
		{
			return new AnimationExpression(left, new AnimationExpression(right, RatioTypeEnum.NONE, null, null), MathOperationEnum.DIVIDE);
		}
		public static AnimationExpression operator /(AnimationExpression left, Vector4D right)
		{
			return new AnimationExpression(left, new AnimationExpression(right, RatioTypeEnum.NONE, null, null), MathOperationEnum.DIVIDE);
		}
		public static AnimationExpression operator /(AnimationExpression left, AnimationSourceEnum right)
		{
			return new AnimationExpression(left, new AnimationExpression(right, RatioTypeEnum.NONE, (double?) null, (double?) null), MathOperationEnum.DIVIDE);
		}
		public static AnimationExpression operator /(AnimationExpression left, AnimationExpression right)
		{
			return new AnimationExpression(left, right, MathOperationEnum.DIVIDE);
		}

		public static AnimationExpression operator %(AnimationExpression left, double right)
		{
			return new AnimationExpression(left, new AnimationExpression(right, RatioTypeEnum.NONE, null, null), MathOperationEnum.MODULO);
		}
		public static AnimationExpression operator %(AnimationExpression left, Vector3D right)
		{
			return new AnimationExpression(left, new AnimationExpression(right, RatioTypeEnum.NONE, null, null), MathOperationEnum.MODULO);
		}
		public static AnimationExpression operator %(AnimationExpression left, Vector4D right)
		{
			return new AnimationExpression(left, new AnimationExpression(right, RatioTypeEnum.NONE, null, null), MathOperationEnum.MODULO);
		}
		public static AnimationExpression operator %(AnimationExpression left, AnimationSourceEnum right)
		{
			return new AnimationExpression(left, new AnimationExpression(right, RatioTypeEnum.NONE, (double?) null, (double?) null), MathOperationEnum.MODULO);
		}
		public static AnimationExpression operator %(AnimationExpression left, AnimationExpression right)
		{
			return new AnimationExpression(left, right, MathOperationEnum.MODULO);
		}

		public static AnimationExpression operator ^(AnimationExpression left, double right)
		{
			return new AnimationExpression(left, new AnimationExpression(right, RatioTypeEnum.NONE, null, null), MathOperationEnum.POWER);
		}
		public static AnimationExpression operator ^(AnimationExpression left, Vector3D right)
		{
			return new AnimationExpression(left, new AnimationExpression(right, RatioTypeEnum.NONE, null, null), MathOperationEnum.POWER);
		}
		public static AnimationExpression operator ^(AnimationExpression left, Vector4D right)
		{
			return new AnimationExpression(left, new AnimationExpression(right, RatioTypeEnum.NONE, null, null), MathOperationEnum.POWER);
		}
		public static AnimationExpression operator ^(AnimationExpression left, AnimationSourceEnum right)
		{
			return new AnimationExpression(left, new AnimationExpression(right, RatioTypeEnum.NONE, (double?) null, (double?) null), MathOperationEnum.POWER);
		}
		public static AnimationExpression operator ^(AnimationExpression left, AnimationExpression right)
		{
			return new AnimationExpression(left, right, MathOperationEnum.POWER);
		}
		#endregion

		#region Public Static Methods
		public static AnimationExpression Average(params double[] arguments)
		{
			return new AnimationExpression(arguments.Select((value) => new AnimationExpression(value, RatioTypeEnum.NONE, null, null)), MathOperationEnum.AVERAGE);
		}
		public static AnimationExpression Average(params Vector3D[] arguments)
		{
			return new AnimationExpression(arguments.Select((value) => new AnimationExpression(value, RatioTypeEnum.NONE, null, null)), MathOperationEnum.AVERAGE);
		}
		public static AnimationExpression Average(params Vector4D[] arguments)
		{
			return new AnimationExpression(arguments.Select((value) => new AnimationExpression(value, RatioTypeEnum.NONE, null, null)), MathOperationEnum.AVERAGE);
		}
		public static AnimationExpression Average(params AnimationSourceEnum[] arguments)
		{
			return new AnimationExpression(arguments.Select((value) => new AnimationExpression(value, RatioTypeEnum.NONE, (double?) null, null)), MathOperationEnum.AVERAGE);
		}
		public static AnimationExpression Average(params AnimationExpression[] arguments)
		{
			return new AnimationExpression(arguments, MathOperationEnum.AVERAGE);
		}

		public static AnimationExpression Largest(params double[] arguments)
		{
			return new AnimationExpression(arguments.Select((value) => new AnimationExpression(value, RatioTypeEnum.NONE, null, null)), MathOperationEnum.LARGEST);
		}
		public static AnimationExpression Largest(params Vector3D[] arguments)
		{
			return new AnimationExpression(arguments.Select((value) => new AnimationExpression(value, RatioTypeEnum.NONE, null, null)), MathOperationEnum.LARGEST);
		}
		public static AnimationExpression Largest(params Vector4D[] arguments)
		{
			return new AnimationExpression(arguments.Select((value) => new AnimationExpression(value, RatioTypeEnum.NONE, null, null)), MathOperationEnum.LARGEST);
		}
		public static AnimationExpression Largest(params AnimationSourceEnum[] arguments)
		{
			return new AnimationExpression(arguments.Select((value) => new AnimationExpression(value, RatioTypeEnum.NONE, (double?) null, null)), MathOperationEnum.LARGEST);
		}
		public static AnimationExpression Largest(params AnimationExpression[] arguments)
		{
			return new AnimationExpression(arguments, MathOperationEnum.LARGEST);
		}

		public static AnimationExpression Smallest(params double[] arguments)
		{
			return new AnimationExpression(arguments.Select((value) => new AnimationExpression(value, RatioTypeEnum.NONE, null, null)), MathOperationEnum.SMALLEST);
		}
		public static AnimationExpression Smallest(params Vector3D[] arguments)
		{
			return new AnimationExpression(arguments.Select((value) => new AnimationExpression(value, RatioTypeEnum.NONE, null, null)), MathOperationEnum.SMALLEST);
		}
		public static AnimationExpression Smallest(params Vector4D[] arguments)
		{
			return new AnimationExpression(arguments.Select((value) => new AnimationExpression(value, RatioTypeEnum.NONE, null, null)), MathOperationEnum.SMALLEST);
		}
		public static AnimationExpression Smallest(params AnimationSourceEnum[] arguments)
		{
			return new AnimationExpression(arguments.Select((value) => new AnimationExpression(value, RatioTypeEnum.NONE, (double?) null, null)), MathOperationEnum.SMALLEST);
		}
		public static AnimationExpression Smallest(params AnimationExpression[] arguments)
		{
			return new AnimationExpression(arguments, MathOperationEnum.SMALLEST);
		}

		public static AnimationExpression Length(double expr)
		{
			return new AnimationExpression(expr, RatioTypeEnum.NONE, null, null);
		}
		public static AnimationExpression Length(Vector3D expr)
		{
			return new AnimationExpression(expr.Length(), RatioTypeEnum.NONE, null, null);
		}
		public static AnimationExpression Length(Vector4D expr)
		{
			return new AnimationExpression(expr.Length(), RatioTypeEnum.NONE, null, null);
		}
		public static AnimationExpression Length(AnimationSourceEnum expr)
		{
			return new AnimationExpression(new AnimationExpression(expr, RatioTypeEnum.NONE, (double?) null, null), MathOperationEnum.LENGTH);
		}
		public static AnimationExpression Length(AnimationExpression expr)
		{
			return new AnimationExpression(expr, MathOperationEnum.LENGTH);
		}

		public static AnimationExpression VectorMin(double expr)
		{
			return new AnimationExpression(expr, RatioTypeEnum.NONE, null, null);
		}
		public static AnimationExpression VectorMin(Vector3D expr)
		{
			return new AnimationExpression(expr.Min(), RatioTypeEnum.NONE, null, null);
		}
		public static AnimationExpression VectorMin(Vector4D expr)
		{
			return new AnimationExpression(Math.Min(Math.Min(expr.X, expr.Y), Math.Min(expr.Z, expr.W)), RatioTypeEnum.NONE, null, null);
		}
		public static AnimationExpression VectorMin(AnimationSourceEnum expr)
		{
			return new AnimationExpression(new AnimationExpression(expr, RatioTypeEnum.NONE, (double?) null, null), MathOperationEnum.VMIN);
		}
		public static AnimationExpression VectorMin(AnimationExpression expr)
		{
			return new AnimationExpression(expr, MathOperationEnum.VMIN);
		}

		public static AnimationExpression VectorMax(double expr)
		{
			return new AnimationExpression(expr, RatioTypeEnum.NONE, null, null);
		}
		public static AnimationExpression VectorMax(Vector3D expr)
		{
			return new AnimationExpression(expr.Max(), RatioTypeEnum.NONE, null, null);
		}
		public static AnimationExpression VectorMax(Vector4D expr)
		{
			return new AnimationExpression(Math.Max(Math.Max(expr.X, expr.Y), Math.Max(expr.Z, expr.W)), RatioTypeEnum.NONE, null, null);
		}
		public static AnimationExpression VectorMax(AnimationSourceEnum expr)
		{
			return new AnimationExpression(new AnimationExpression(expr, RatioTypeEnum.NONE, (double?) null, null), MathOperationEnum.VMAX);
		}
		public static AnimationExpression VectorMax(AnimationExpression expr)
		{
			return new AnimationExpression(expr, MathOperationEnum.VMAX);
		}

		public static AnimationExpression Sin(double expr)
		{
			return new AnimationExpression(Math.Sin(expr), RatioTypeEnum.NONE, null, null);
		}
		public static AnimationExpression Sin(Vector3D expr)
		{
			return new AnimationExpression(new Vector3D(Math.Sin(expr.X), Math.Sin(expr.Y), Math.Sin(expr.Z)), RatioTypeEnum.NONE, null, null);
		}
		public static AnimationExpression Sin(Vector4D expr)
		{
			return new AnimationExpression(new Vector4D(Math.Sin(expr.X), Math.Sin(expr.Y), Math.Sin(expr.Z), Math.Sin(expr.W)), RatioTypeEnum.NONE, null, null);
		}
		public static AnimationExpression Sin(AnimationSourceEnum expr)
		{
			return new AnimationExpression(new AnimationExpression(expr, RatioTypeEnum.NONE, (double?) null, null), Math.Sin);
		}
		public static AnimationExpression Sin(AnimationExpression expr)
		{
			return new AnimationExpression(expr, Math.Sin);
		}

		public static AnimationExpression Cos(double expr)
		{
			return new AnimationExpression(Math.Cos(expr), RatioTypeEnum.NONE, null, null);
		}
		public static AnimationExpression Cos(Vector3D expr)
		{
			return new AnimationExpression(new Vector3D(Math.Cos(expr.X), Math.Cos(expr.Y), Math.Cos(expr.Z)), RatioTypeEnum.NONE, null, null);
		}
		public static AnimationExpression Cos(Vector4D expr)
		{
			return new AnimationExpression(new Vector4D(Math.Sin(expr.X), Math.Sin(expr.Y), Math.Sin(expr.Z), Math.Sin(expr.W)), RatioTypeEnum.NONE, null, null);
		}
		public static AnimationExpression Cos(AnimationSourceEnum expr)
		{
			return new AnimationExpression(new AnimationExpression(expr, RatioTypeEnum.NONE, (double?) null, null), Math.Cos);
		}
		public static AnimationExpression Cos(AnimationExpression expr)
		{
			return new AnimationExpression(expr, Math.Cos);
		}

		public static AnimationExpression Tan(double expr)
		{
			return new AnimationExpression(Math.Tan(expr), RatioTypeEnum.NONE, null, null);
		}
		public static AnimationExpression Tan(Vector3D expr)
		{
			return new AnimationExpression(new Vector3D(Math.Tan(expr.X), Math.Tan(expr.Y), Math.Tan(expr.Z)), RatioTypeEnum.NONE, null, null);
		}
		public static AnimationExpression Tan(Vector4D expr)
		{
			return new AnimationExpression(new Vector4D(Math.Tan(expr.X), Math.Tan(expr.Y), Math.Tan(expr.Z), Math.Tan(expr.W)), RatioTypeEnum.NONE, null, null);
		}
		public static AnimationExpression Tan(AnimationSourceEnum expr)
		{
			return new AnimationExpression(new AnimationExpression(expr, RatioTypeEnum.NONE, (double?) null, null), Math.Tan);
		}
		public static AnimationExpression Tan(AnimationExpression expr)
		{
			return new AnimationExpression(expr, Math.Tan);
		}

		public static AnimationExpression ASin(double expr)
		{
			return new AnimationExpression(Math.Asin(expr), RatioTypeEnum.NONE, null, null);
		}
		public static AnimationExpression ASin(Vector3D expr)
		{
			return new AnimationExpression(new Vector3D(Math.Asin(expr.X), Math.Asin(expr.Y), Math.Asin(expr.Z)), RatioTypeEnum.NONE, null, null);
		}
		public static AnimationExpression ASin(Vector4D expr)
		{
			return new AnimationExpression(new Vector4D(Math.Asin(expr.X), Math.Asin(expr.Y), Math.Asin(expr.Z), Math.Asin(expr.W)), RatioTypeEnum.NONE, null, null);
		}
		public static AnimationExpression ASin(AnimationSourceEnum expr)
		{
			return new AnimationExpression(new AnimationExpression(expr, RatioTypeEnum.NONE, (double?) null, null), Math.Asin);
		}
		public static AnimationExpression ASin(AnimationExpression expr)
		{
			return new AnimationExpression(expr, Math.Asin);
		}

		public static AnimationExpression ACos(double expr)
		{
			return new AnimationExpression(Math.Acos(expr), RatioTypeEnum.NONE, null, null);
		}
		public static AnimationExpression ACos(Vector3D expr)
		{
			return new AnimationExpression(new Vector3D(Math.Acos(expr.X), Math.Acos(expr.Y), Math.Acos(expr.Z)), RatioTypeEnum.NONE, null, null);
		}
		public static AnimationExpression ACos(Vector4D expr)
		{
			return new AnimationExpression(new Vector4D(Math.Acos(expr.X), Math.Acos(expr.Y), Math.Acos(expr.Z), Math.Acos(expr.W)), RatioTypeEnum.NONE, null, null);
		}
		public static AnimationExpression ACos(AnimationSourceEnum expr)
		{
			return new AnimationExpression(new AnimationExpression(expr, RatioTypeEnum.NONE, (double?) null, null), Math.Acos);
		}
		public static AnimationExpression ACos(AnimationExpression expr)
		{
			return new AnimationExpression(expr, Math.Acos);
		}

		public static AnimationExpression ATan(double expr)
		{
			return new AnimationExpression(Math.Atan(expr), RatioTypeEnum.NONE, null, null);
		}
		public static AnimationExpression ATan(Vector3D expr)
		{
			return new AnimationExpression(new Vector3D(Math.Atan(expr.X), Math.Atan(expr.Y), Math.Atan(expr.Z)), RatioTypeEnum.NONE, null, null);
		}
		public static AnimationExpression ATan(Vector4D expr)
		{
			return new AnimationExpression(new Vector4D(Math.Atan(expr.X), Math.Atan(expr.Y), Math.Atan(expr.Z), Math.Atan(expr.W)), RatioTypeEnum.NONE, null, null);
		}
		public static AnimationExpression ATan(AnimationSourceEnum expr)
		{
			return new AnimationExpression(new AnimationExpression(expr, RatioTypeEnum.NONE, (double?) null, null), Math.Atan);
		}
		public static AnimationExpression ATan(AnimationExpression expr)
		{
			return new AnimationExpression(expr, Math.Atan);
		}
		#endregion

		#region Private Static Methods
		private static EvaluatedResult[] CreateBounds(double? lower, double? upper)
		{
			return (lower == null && upper == null) ? null : new EvaluatedResult[2] { new EvaluatedResult(lower ?? double.NegativeInfinity), new EvaluatedResult(upper ?? double.PositiveInfinity) };
		}

		private static EvaluatedResult[] CreateBounds(Vector3D? lower, Vector3D? upper)
		{
			if (lower == null && upper == null) return null;
			else if (lower != null && upper != null) return new EvaluatedResult[2] { new EvaluatedResult(lower.Value), new EvaluatedResult(upper.Value) };
			else if (lower == null) return new EvaluatedResult[2] { new EvaluatedResult(new Vector4D(double.NegativeInfinity, 0, 0, 0)), new EvaluatedResult(upper.Value) };
			else if (upper == null) return new EvaluatedResult[2] { new EvaluatedResult(lower.Value), new EvaluatedResult(new Vector4D(double.PositiveInfinity, 0, 0, 0)) };
			else return null;
		}

		private static EvaluatedResult[] CreateBounds(Vector4D? lower, Vector4D? upper)
		{
			if (lower == null && upper == null) return null;
			else if (lower != null && upper != null) return new EvaluatedResult[2] { new EvaluatedResult(lower.Value), new EvaluatedResult(upper.Value) };
			else if (lower == null) return new EvaluatedResult[2] { new EvaluatedResult(new Vector4D(double.NegativeInfinity, 0, 0, 0)), new EvaluatedResult(upper.Value) };
			else if (upper == null) return new EvaluatedResult[2] { new EvaluatedResult(lower.Value), new EvaluatedResult(new Vector4D(double.PositiveInfinity, 0, 0, 0)) };
			else return null;
		}
		#endregion

		#region Constructors
		private AnimationExpression(AnimationExpression left, AnimationExpression right, MathOperationEnum operation)
		{
			if (left == null || right == null) throw new ArgumentNullException("One or more sides of equation is null");
			this.SingleDoubleValue = null;
			this.SingleVectorValue = null;
			this.SingleSourceValue = AnimationSourceEnum.FIXED;
			this.Arguments = new List<AnimationExpression>() { left, right };
			this.Operation = operation;
			this.Function = null;
			this.ClampBounds = null;
			this.RatioType = RatioTypeEnum.NONE;
		}
		private AnimationExpression(IEnumerable<AnimationExpression> arguments, MathOperationEnum operation)
		{
			this.SingleDoubleValue = null;
			this.SingleVectorValue = null;
			this.SingleSourceValue = AnimationSourceEnum.FIXED;
			this.Arguments = new List<AnimationExpression>(arguments);
			this.Operation = operation;
			this.Function = null;
			this.ClampBounds = null;
			this.RatioType = RatioTypeEnum.NONE;
			if (this.Arguments.Any((side) => side != null)) throw new ArgumentNullException("One or more sides of equation is null");
		}
		private AnimationExpression(AnimationExpression unary, MathOperationEnum operation)
		{
			if (unary == null) throw new ArgumentNullException("Unary side of equation is null");
			this.SingleDoubleValue = null;
			this.SingleVectorValue = null;
			this.SingleSourceValue = AnimationSourceEnum.FIXED;
			this.Arguments = new List<AnimationExpression>() { unary };
			this.Operation = operation;
			this.Function = null;
			this.ClampBounds = null;
			this.RatioType = RatioTypeEnum.NONE;
		}
		private AnimationExpression(AnimationExpression unary, Func<double, double> function)
		{
			if (unary == null) throw new ArgumentNullException("Unary side of equation is null");
			this.SingleDoubleValue = null;
			this.SingleVectorValue = null;
			this.SingleSourceValue = AnimationSourceEnum.FIXED;
			this.Arguments = new List<AnimationExpression>() { unary };
			this.Operation = MathOperationEnum.FUNCTION;
			this.Function = function;
			this.ClampBounds = null;
			this.RatioType = RatioTypeEnum.NONE;
		}
		internal AnimationExpression(double value, RatioTypeEnum ratio_type, double? lower, double? upper)
		{
			this.SingleDoubleValue = value;
			this.SingleVectorValue = null;
			this.SingleSourceValue = AnimationSourceEnum.FIXED;
			this.Arguments = new List<AnimationExpression>();
			this.Operation = MathOperationEnum.ADD;
			this.Function = null;
			this.ClampBounds = AnimationExpression.CreateBounds(lower, upper);
			this.RatioType = ratio_type;
		}
		internal AnimationExpression(Vector3D value, RatioTypeEnum ratio_type, Vector3D? lower, Vector3D? upper)
		{
			this.SingleDoubleValue = null;
			this.SingleVectorValue = new Vector4D(value, 0);
			this.SingleSourceValue = AnimationSourceEnum.FIXED;
			this.Arguments = new List<AnimationExpression>();
			this.Operation = MathOperationEnum.ADD;
			this.Function = null;
			this.ClampBounds = AnimationExpression.CreateBounds(lower, upper);
			this.RatioType = ratio_type;
		}
		internal AnimationExpression(Vector4D value, RatioTypeEnum ratio_type, Vector4D? lower, Vector4D? upper)
		{
			this.SingleDoubleValue = null;
			this.SingleVectorValue = value;
			this.SingleSourceValue = AnimationSourceEnum.FIXED;
			this.Arguments = new List<AnimationExpression>();
			this.Operation = MathOperationEnum.ADD;
			this.Function = null;
			this.ClampBounds = AnimationExpression.CreateBounds(lower, upper);
			this.RatioType = ratio_type;
		}
		internal AnimationExpression(AnimationSourceEnum value, RatioTypeEnum ratio_type, double? lower, double? upper)
		{
			this.SingleDoubleValue = null;
			this.SingleVectorValue = null;
			this.SingleSourceValue = value;
			this.Arguments = new List<AnimationExpression>();
			this.Operation = MathOperationEnum.ADD;
			this.Function = null;
			this.ClampBounds = AnimationExpression.CreateBounds(lower, upper);
			this.RatioType = ratio_type;
		}
		internal AnimationExpression(AnimationSourceEnum value, RatioTypeEnum ratio_type, Vector3D? lower, Vector3D? upper)
		{
			this.SingleDoubleValue = null;
			this.SingleVectorValue = null;
			this.SingleSourceValue = value;
			this.Arguments = new List<AnimationExpression>();
			this.Operation = MathOperationEnum.ADD;
			this.Function = null;
			this.ClampBounds = AnimationExpression.CreateBounds(lower, upper);
			this.RatioType = ratio_type;
		}
		internal AnimationExpression(AnimationSourceEnum value, RatioTypeEnum ratio_type, Vector4D? lower, Vector4D? upper)
		{
			this.SingleDoubleValue = null;
			this.SingleVectorValue = null;
			this.SingleSourceValue = value;
			this.Arguments = new List<AnimationExpression>();
			this.Operation = MathOperationEnum.ADD;
			this.Function = null;
			this.ClampBounds = AnimationExpression.CreateBounds(lower, upper);
			this.RatioType = ratio_type;
		}
		public AnimationExpression() { }
		#endregion
	}

	[ProtoContract(UseProtoMembersOnly = true)]
	public class DoubleKeyframe
	{
		#region Internal Variables
		/// <summary>
		/// The value of this keyframe
		/// </summary>
		[ProtoMember(1)]
		internal AnimationExpression Expression;

		/// <summary>
		/// The position of this keyframe<br />
		/// Relative to animation start
		/// </summary>
		[ProtoMember(2)]
		internal ushort Position;

		/// <summary>
		/// The interpolation method from this keyframe to the next
		/// </summary>
		[ProtoMember(3)]
		internal EasingCurveEnum EasingCurve = EasingCurveEnum.LINEAR;

		/// <summary>
		/// The easing method from this keyframe to the next
		/// </summary>
		[ProtoMember(4)]
		internal EasingTypeEnum EasingType = EasingTypeEnum.EASE_IN_OUT;
		#endregion

		#region Operators
		public static DoubleKeyframe operator +(DoubleKeyframe a, double b)
		{
			a.Expression += b;
			return a;
		}
		public static DoubleKeyframe operator +(DoubleKeyframe a, Vector3D b)
		{
			a.Expression += b;
			return a;
		}
		public static DoubleKeyframe operator +(DoubleKeyframe a, Vector4D b)
		{
			a.Expression += b;
			return a;
		}
		public static DoubleKeyframe operator +(DoubleKeyframe a, AnimationSourceEnum b)
		{
			a.Expression += b;
			return a;
		}
		public static DoubleKeyframe operator +(DoubleKeyframe a, AnimationExpression b)
		{
			a.Expression += b;
			return a;
		}

		public static DoubleKeyframe operator -(DoubleKeyframe a, double b)
		{
			a.Expression -= b;
			return a;
		}
		public static DoubleKeyframe operator -(DoubleKeyframe a, Vector3D b)
		{
			a.Expression -= b;
			return a;
		}
		public static DoubleKeyframe operator -(DoubleKeyframe a, Vector4D b)
		{
			a.Expression -= b;
			return a;
		}
		public static DoubleKeyframe operator -(DoubleKeyframe a, AnimationSourceEnum b)
		{
			a.Expression -= b;
			return a;
		}
		public static DoubleKeyframe operator -(DoubleKeyframe a, AnimationExpression b)
		{
			a.Expression -= b;
			return a;
		}

		public static DoubleKeyframe operator *(DoubleKeyframe a, double b)
		{
			a.Expression *= b;
			return a;
		}
		public static DoubleKeyframe operator *(DoubleKeyframe a, Vector3D b)
		{
			a.Expression *= b;
			return a;
		}
		public static DoubleKeyframe operator *(DoubleKeyframe a, Vector4D b)
		{
			a.Expression *= b;
			return a;
		}
		public static DoubleKeyframe operator *(DoubleKeyframe a, AnimationSourceEnum b)
		{
			a.Expression *= b;
			return a;
		}
		public static DoubleKeyframe operator *(DoubleKeyframe a, AnimationExpression b)
		{
			a.Expression *= b;
			return a;
		}

		public static DoubleKeyframe operator /(DoubleKeyframe a, double b)
		{
			a.Expression /= b;
			return a;
		}
		public static DoubleKeyframe operator /(DoubleKeyframe a, Vector3D b)
		{
			a.Expression /= b;
			return a;
		}
		public static DoubleKeyframe operator /(DoubleKeyframe a, Vector4D b)
		{
			a.Expression /= b;
			return a;
		}
		public static DoubleKeyframe operator /(DoubleKeyframe a, AnimationSourceEnum b)
		{
			a.Expression /= b;
			return a;
		}
		public static DoubleKeyframe operator /(DoubleKeyframe a, AnimationExpression b)
		{
			a.Expression /= b;
			return a;
		}

		public static DoubleKeyframe operator %(DoubleKeyframe a, double b)
		{
			a.Expression %= b;
			return a;
		}
		public static DoubleKeyframe operator %(DoubleKeyframe a, Vector3D b)
		{
			a.Expression %= b;
			return a;
		}
		public static DoubleKeyframe operator %(DoubleKeyframe a, Vector4D b)
		{
			a.Expression %= b;
			return a;
		}
		public static DoubleKeyframe operator %(DoubleKeyframe a, AnimationSourceEnum b)
		{
			a.Expression %= b;
			return a;
		}
		public static DoubleKeyframe operator %(DoubleKeyframe a, AnimationExpression b)
		{
			a.Expression %= b;
			return a;
		}

		public static DoubleKeyframe operator ^(DoubleKeyframe a, double b)
		{
			a.Expression ^= b;
			return a;
		}
		public static DoubleKeyframe operator ^(DoubleKeyframe a, Vector3D b)
		{
			a.Expression ^= b;
			return a;
		}
		public static DoubleKeyframe operator ^(DoubleKeyframe a, Vector4D b)
		{
			a.Expression ^= b;
			return a;
		}
		public static DoubleKeyframe operator ^(DoubleKeyframe a, AnimationSourceEnum b)
		{
			a.Expression ^= b;
			return a;
		}
		public static DoubleKeyframe operator ^(DoubleKeyframe a, AnimationExpression b)
		{
			a.Expression ^= b;
			return a;
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
			this.Expression = new AnimationExpression(initial_value, ratio_type, lower, upper);
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
			this.Expression = new AnimationExpression(initial_value, ratio_type, lower, upper);
			this.EasingCurve = easing_curve;
			this.EasingType = easing_type;
		}
		#endregion
	}

	[ProtoContract(UseProtoMembersOnly = true)]
	public class VectorKeyframe
	{
		#region Internal Variables
		/// <summary>
		/// The value of this keyframe
		/// </summary>
		[ProtoMember(1)]
		internal AnimationExpression Expression;

		/// <summary>
		/// The position of this keyframe<br />
		/// Relative to animation start
		/// </summary>
		[ProtoMember(2)]
		internal ushort Position;

		/// <summary>
		/// The interpolation method from this keyframe to the next
		/// </summary>
		[ProtoMember(3)]
		internal EasingCurveEnum EasingCurve = EasingCurveEnum.LINEAR;

		/// <summary>
		/// The easing method from this keyframe to the next
		/// </summary>
		[ProtoMember(4)]
		internal EasingTypeEnum EasingType = EasingTypeEnum.EASE_IN_OUT;

		/// <summary>
		/// The ratio type used for ratioing/clamping values
		/// </summary>
		[ProtoMember(5)]
		internal RatioTypeEnum RatioType = RatioTypeEnum.NONE;
		#endregion

		#region Operators
		public static VectorKeyframe operator +(VectorKeyframe a, double b)
		{
			a.Expression += b;
			return a;
		}
		public static VectorKeyframe operator +(VectorKeyframe a, Vector3D b)
		{
			a.Expression += b;
			return a;
		}
		public static VectorKeyframe operator +(VectorKeyframe a, Vector4D b)
		{
			a.Expression += b;
			return a;
		}
		public static VectorKeyframe operator +(VectorKeyframe a, AnimationSourceEnum b)
		{
			a.Expression += b;
			return a;
		}
		public static VectorKeyframe operator +(VectorKeyframe a, AnimationExpression b)
		{
			a.Expression += b;
			return a;
		}

		public static VectorKeyframe operator -(VectorKeyframe a, double b)
		{
			a.Expression -= b;
			return a;
		}
		public static VectorKeyframe operator -(VectorKeyframe a, Vector3D b)
		{
			a.Expression -= b;
			return a;
		}
		public static VectorKeyframe operator -(VectorKeyframe a, Vector4D b)
		{
			a.Expression -= b;
			return a;
		}
		public static VectorKeyframe operator -(VectorKeyframe a, AnimationSourceEnum b)
		{
			a.Expression -= b;
			return a;
		}
		public static VectorKeyframe operator -(VectorKeyframe a, AnimationExpression b)
		{
			a.Expression -= b;
			return a;
		}

		public static VectorKeyframe operator *(VectorKeyframe a, double b)
		{
			a.Expression *= b;
			return a;
		}
		public static VectorKeyframe operator *(VectorKeyframe a, Vector3D b)
		{
			a.Expression *= b;
			return a;
		}
		public static VectorKeyframe operator *(VectorKeyframe a, Vector4D b)
		{
			a.Expression *= b;
			return a;
		}
		public static VectorKeyframe operator *(VectorKeyframe a, AnimationSourceEnum b)
		{
			a.Expression *= b;
			return a;
		}
		public static VectorKeyframe operator *(VectorKeyframe a, AnimationExpression b)
		{
			a.Expression *= b;
			return a;
		}

		public static VectorKeyframe operator /(VectorKeyframe a, double b)
		{
			a.Expression /= b;
			return a;
		}
		public static VectorKeyframe operator /(VectorKeyframe a, Vector3D b)
		{
			a.Expression /= b;
			return a;
		}
		public static VectorKeyframe operator /(VectorKeyframe a, Vector4D b)
		{
			a.Expression /= b;
			return a;
		}
		public static VectorKeyframe operator /(VectorKeyframe a, AnimationSourceEnum b)
		{
			a.Expression /= b;
			return a;
		}
		public static VectorKeyframe operator /(VectorKeyframe a, AnimationExpression b)
		{
			a.Expression /= b;
			return a;
		}

		public static VectorKeyframe operator %(VectorKeyframe a, double b)
		{
			a.Expression %= b;
			return a;
		}
		public static VectorKeyframe operator %(VectorKeyframe a, Vector3D b)
		{
			a.Expression %= b;
			return a;
		}
		public static VectorKeyframe operator %(VectorKeyframe a, Vector4D b)
		{
			a.Expression %= b;
			return a;
		}
		public static VectorKeyframe operator %(VectorKeyframe a, AnimationSourceEnum b)
		{
			a.Expression %= b;
			return a;
		}
		public static VectorKeyframe operator %(VectorKeyframe a, AnimationExpression b)
		{
			a.Expression %= b;
			return a;
		}

		public static VectorKeyframe operator ^(VectorKeyframe a, double b)
		{
			a.Expression ^= b;
			return a;
		}
		public static VectorKeyframe operator ^(VectorKeyframe a, Vector3D b)
		{
			a.Expression ^= b;
			return a;
		}
		public static VectorKeyframe operator ^(VectorKeyframe a, Vector4D b)
		{
			a.Expression ^= b;
			return a;
		}
		public static VectorKeyframe operator ^(VectorKeyframe a, AnimationSourceEnum b)
		{
			a.Expression ^= b;
			return a;
		}
		public static VectorKeyframe operator ^(VectorKeyframe a, AnimationExpression b)
		{
			a.Expression ^= b;
			return a;
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
			this.Expression = new AnimationExpression(new Vector4D(initial_value), ratio_type, lower, upper);
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
			this.Expression = new AnimationExpression(initial_value, ratio_type, lower, upper);
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
			this.Expression = new AnimationExpression(initial_value, ratio_type, lower, upper);
			this.EasingCurve = easing_curve;
			this.EasingType = easing_type;
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
	#endregion
}
