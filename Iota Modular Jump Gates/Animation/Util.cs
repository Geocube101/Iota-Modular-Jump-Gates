using IOTA.ModularJumpGates.CubeBlock;
using IOTA.ModularJumpGates.JumpGates;
using ProtoBuf;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRageMath;

namespace IOTA.ModularJumpGates.Animation
{
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

	[Serializable]
	[XmlRoot("AnimationExpression")]
	[ProtoContract(UseProtoMembersOnly = true)]
	public sealed class AnimationExpression
	{
		public struct EvaluatedResult
		{
			public bool IsVector;
			public double DoubleResult;
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

		internal class ExpressionArguments
		{
			public readonly ushort CurrentTick;
			public readonly ushort TotalDuration;
			public readonly MyJumpGate JumpGate;
			public readonly MyJumpGate TargetGate;
			public readonly List<MyJumpGateDrive> JumpGateDrives;
			public readonly List<MyEntity> JumpSpaceEntities;
			public readonly Vector3D Endpoint;
			public Vector3D? ThisPosition;
			public MyEntity ThisEntity;

			public ExpressionArguments(ushort current_tick, ushort total_duration, MyJumpGate jump_gate, MyJumpGate target_gate, List<MyJumpGateDrive> drives, List<MyEntity> entities, ref Vector3D endpoint, Vector3D? this_position, MyEntity this_entity)
			{
				this.CurrentTick = current_tick;
				this.TotalDuration = total_duration;
				this.JumpGate = jump_gate;
				this.TargetGate = target_gate;
				this.JumpGateDrives = drives;
				this.JumpSpaceEntities = entities;
				this.Endpoint = endpoint;
				this.ThisPosition = this_position;
				this.ThisEntity = this_entity;
			}

			public ExpressionArguments SetThis(Vector3D? this_position)
			{
				this.ThisPosition = this_position;
				return this;
			}
			public ExpressionArguments SetThis(MyEntity this_entity)
			{
				this.ThisEntity = this_entity;
				return this;
			}
			public ExpressionArguments SetThis(Vector3D? this_position, MyEntity this_entity)
			{
				this.ThisPosition = this_position;
				this.ThisEntity = this_entity;
				return this;
			}
		}

		#region Internal Variables
		[ProtoMember(1)]
		[XmlElement("Operation")]
		public MathOperationEnum Operation;

		[ProtoMember(2)]
		[XmlArray("Arguments")]
		[XmlArrayItem("Argument")]
		public List<AnimationExpression> Arguments;

		[ProtoMember(3)]
		[XmlElement("SingleDoubleValue")]
		public double? SingleDoubleValue;

		[ProtoMember(4)]
		[XmlElement("SingleVectorValue")]
		public Vector4D? SingleVectorValue;

		[ProtoMember(5)]
		[XmlElement("SingleSourceValue")]
		public AnimationSourceEnum SingleSourceValue;

		[XmlIgnore]
		internal readonly Func<double, double> Function;

		[ProtoMember(6)]
		[XmlArray("ClampBounds")]
		[XmlArrayItem("ClampBound")]
		public EvaluatedResult[] ClampBounds;

		[ProtoMember(7)]
		[XmlElement("RatioType")]
		public RatioTypeEnum RatioType;
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

		internal EvaluatedResult Evaluate(ExpressionArguments arguments)
		{
			if (this.Arguments == null || this.Arguments.Count == 0)
			{
				if (this.SingleDoubleValue == null && this.SingleVectorValue == null && this.SingleSourceValue == AnimationSourceEnum.FIXED) throw new InvalidOperationException("Operation on null value");
				Vector3D jump_node = arguments.JumpGate.WorldJumpNode;
				EvaluatedResult lower_clamp = (this.ClampBounds == null) ? new EvaluatedResult(double.NegativeInfinity) : this.ClampBounds[0];
				EvaluatedResult upper_clamp = (this.ClampBounds == null) ? new EvaluatedResult(double.PositiveInfinity) : this.ClampBounds[1];
				double ratio;

				switch (this.RatioType)
				{
					case RatioTypeEnum.NONE:
					{
						EvaluatedResult result;

						switch (this.SingleSourceValue)
						{
							case AnimationSourceEnum.FIXED:
								result = (this.SingleDoubleValue == null) ? new EvaluatedResult(this.SingleVectorValue.Value) : new EvaluatedResult(this.SingleDoubleValue.Value);
								break;
							case AnimationSourceEnum.TIME:
								result = new EvaluatedResult((double) arguments.CurrentTick / arguments.TotalDuration);
								break;
							case AnimationSourceEnum.JUMP_GATE_SIZE:
								result = new EvaluatedResult((arguments.JumpGate.Closed) ? 0 : arguments.JumpGate.JumpNodeRadius());
								break;
							case AnimationSourceEnum.JUMP_GATE_HEIGHT:
								result = new EvaluatedResult((arguments.JumpGate.Closed) ? 0 : arguments.JumpGate.JumpEllipse.Radii.Y);
								break;
							case AnimationSourceEnum.JUMP_GATE_RADII:
								result = new EvaluatedResult((arguments.JumpGate.Closed) ? Vector3D.Zero : arguments.JumpGate.JumpEllipse.Radii);
								break;
							case AnimationSourceEnum.JUMP_GATE_VELOCITY:
								result = new EvaluatedResult((arguments.JumpGate.Closed) ? Vector3D.Zero : arguments.JumpGate.JumpNodeVelocity);
								break;
							case AnimationSourceEnum.JUMP_ANTIGATE_SIZE:
								result = new EvaluatedResult((arguments.TargetGate != null && !arguments.TargetGate.Closed) ? arguments.TargetGate.JumpNodeRadius() : ((arguments.JumpGate.Closed) ? 0 : arguments.JumpGate.JumpNodeRadius()));
								break;
							case AnimationSourceEnum.JUMP_ANTIGATE_HEIGHT:
								result = new EvaluatedResult((arguments.TargetGate != null && !arguments.TargetGate.Closed) ? arguments.TargetGate.JumpEllipse.Radii.Y : (((arguments.JumpGate.Closed) ? 0 : arguments.JumpGate.JumpEllipse.Radii.Y)));
								break;
							case AnimationSourceEnum.JUMP_ANTIGATE_RADII:
								result = new EvaluatedResult((arguments.TargetGate != null && !arguments.TargetGate.Closed) ? arguments.TargetGate.JumpEllipse.Radii : ((arguments.JumpGate.Closed) ? Vector3D.Zero : arguments.JumpGate.JumpEllipse.Radii));
								break;
							case AnimationSourceEnum.JUMP_ANTIGATE_VELOCITY:
								result = new EvaluatedResult((arguments.TargetGate != null && !arguments.TargetGate.Closed) ? arguments.TargetGate.JumpNodeVelocity : ((arguments.JumpGate.Closed) ? Vector3D.Zero : arguments.JumpGate.JumpNodeVelocity));
								break;
							case AnimationSourceEnum.JUMP_NORMAL:
								result = new EvaluatedResult((arguments.Endpoint - jump_node).Normalized());
								break;
							case AnimationSourceEnum.RANDOM:
								result = new EvaluatedResult(new Random().NextDouble());
								break;
							case AnimationSourceEnum.N_ENTITIES:
								result = new EvaluatedResult(arguments.JumpSpaceEntities.Count);
								break;
							case AnimationSourceEnum.N_DRIVES:
								result = new EvaluatedResult(arguments.JumpGateDrives.Count);
								break;
							case AnimationSourceEnum.DISTANCE_ENTITY_TO_ENDPOINT:
								result = new EvaluatedResult((arguments.ThisPosition == null) ? 0 : Vector3D.Distance(arguments.Endpoint, arguments.ThisPosition.Value));
								break;
							case AnimationSourceEnum.DISTANCE_GATE_TO_ENDPOINT:
								result = new EvaluatedResult((arguments.JumpGate.Closed) ? 0 : Vector3D.Distance(jump_node, arguments.Endpoint));
								break;
							case AnimationSourceEnum.DISTANCE_ENTITY_TO_GATE:
								result = new EvaluatedResult((arguments.ThisPosition == null || arguments.JumpGate.Closed) ? 0 : Vector3D.Distance(jump_node, arguments.ThisPosition.Value));
								break;
							case AnimationSourceEnum.DISTANCE_ENTITY_TO_ENTITY:
								if (arguments.ThisPosition == null || arguments.JumpSpaceEntities.Count == 0) result = new EvaluatedResult(0);
								else
								{
									result = new EvaluatedResult(double.MaxValue);

									foreach (MyEntity entity in arguments.JumpSpaceEntities)
									{
										double distance = Vector3D.Distance(entity.WorldMatrix.Translation, arguments.ThisPosition.Value);
										result.DoubleResult = Math.Min(result.DoubleResult, distance);
									}

									return result;
								}
								break;
							case AnimationSourceEnum.DISTANCE_THIS_TO_ENTITY:
								result = new EvaluatedResult((arguments.ThisPosition == null || arguments.ThisEntity == null) ? 0 : Vector3D.Distance(arguments.ThisPosition.Value, arguments.ThisEntity.WorldMatrix.Translation));
								break;
							case AnimationSourceEnum.ENTITY_VELOCITY:
								result = new EvaluatedResult((arguments.ThisEntity == null && arguments.JumpGate.Closed) ? Vector3.Zero : (arguments.ThisEntity?.Physics?.LinearVelocity ?? arguments.JumpGate.JumpNodeVelocity));
								break;
							case AnimationSourceEnum.ENTITY_VOLUME:
							{
								if (arguments.ThisEntity == null) result = new EvaluatedResult(0);
								BoundingBoxD aabb = ((IMyEntity) arguments.ThisEntity).WorldAABB;
								BoundingSphereD sphere = ((IMyEntity) arguments.ThisEntity).WorldVolume;
								result = new EvaluatedResult(Math.Min(aabb.Volume, (4 * Math.PI * sphere.Radius * sphere.Radius * sphere.Radius) / 3));
								break;
							}
							case AnimationSourceEnum.ENTITY_MASS:
								result = new EvaluatedResult(arguments.ThisEntity?.Physics?.Mass ?? 0);
								break;
							case AnimationSourceEnum.ENTITY_EXTENTS:
								result = new EvaluatedResult((arguments.ThisEntity == null) ? Vector3D.Zero : ((IMyEntity) arguments.ThisEntity).WorldAABB.Extents);
								break;
							case AnimationSourceEnum.ENTITY_FACING_EXTENTS:
								if (arguments.ThisEntity == null || arguments.JumpGate.PrimaryDrivePlane == null) result = new EvaluatedResult(Vector3D.Zero);
								else
								{
									Vector3D extents = ((IMyEntity) arguments.ThisEntity).WorldAABB.Extents;
									Vector3D normal = arguments.JumpGate.PrimaryDrivePlane.Value.Normal;
									result = new EvaluatedResult(Vector3D.ProjectOnPlane(ref extents, ref normal));
								}

								break;
							case AnimationSourceEnum.GRAVITY:
								result = new EvaluatedResult((arguments.JumpGate.Closed) ? 0 : MyAPIGateway.GravityProviderSystem.CalculateNaturalGravityInPoint(jump_node).Length());
								break;
							case AnimationSourceEnum.ATMOSPHERE:
							{
								MyPlanet planet = (arguments.JumpGate.Closed) ? null : MyGamePruningStructure.GetClosestPlanet(jump_node);
								result = new EvaluatedResult((planet == null || !planet.HasAtmosphere) ? 0 : Math.Max(0, 1 - (Vector3D.Distance(jump_node, planet.WorldMatrix.Translation) / (double) planet.AtmosphereRadius)));
								break;
							}
							default:
								throw new InvalidOperationException($"Invalid animation source - {(byte) this.SingleSourceValue}");
						}

						if (this.ClampBounds != null && result.IsVector)
						{
							Vector4D _lower = lower_clamp.AsVector();
							Vector4D _upper = upper_clamp.AsVector();

							return new EvaluatedResult(new Vector4D(
								MathHelper.Clamp(result.VectorResult.X, _lower.X, _upper.X),
								MathHelper.Clamp(result.VectorResult.X, _lower.Y, _upper.Y),
								MathHelper.Clamp(result.VectorResult.X, _lower.Z, _upper.Z),
								MathHelper.Clamp(result.VectorResult.X, _lower.W, _upper.W)
							));
						}
						else if (this.ClampBounds != null) return new EvaluatedResult(MathHelper.Clamp(result.DoubleResult, lower_clamp.AsDouble(), upper_clamp.AsDouble()));
						else return result;
					}
					case RatioTypeEnum.TIME:
						ratio = (double) arguments.CurrentTick / arguments.TotalDuration;
						break;
					case RatioTypeEnum.ENDPOINT_DISTANCE:
						ratio = (arguments.ThisPosition == null) ? 0.5 : (1 - (Vector3D.Distance(arguments.ThisPosition.Value, arguments.Endpoint) / Vector3D.Distance(jump_node, arguments.Endpoint)));
						break;
					case RatioTypeEnum.ENTITY_DISTANCE:
						ratio = (arguments.ThisPosition == null) ? 0.5 : (1 - (Vector3D.Distance(arguments.ThisPosition.Value, jump_node) / arguments.JumpSpaceEntities.Max((entity) => Vector3D.Distance(entity.WorldMatrix.Translation, jump_node))));
						break;
					case RatioTypeEnum.ATMOSPHERE:
					{
						MyPlanet planet = (arguments.ThisPosition == null) ? null : MyGamePruningStructure.GetClosestPlanet(arguments.ThisPosition.Value);
						ratio = (planet == null || !planet.HasAtmosphere) ? 0.5 : Math.Max(0, 1 - (Vector3D.Distance(jump_node, planet.WorldMatrix.Translation) / (double) planet.AtmosphereRadius));
						break;
					}
					case RatioTypeEnum.GRAVITY:
						ratio = (arguments.ThisPosition == null) ? 0.5 : MyAPIGateway.GravityProviderSystem.CalculateNaturalGravityInPoint(jump_node).Length();
						break;
					case RatioTypeEnum.RANDOM:
						ratio = new Random().NextDouble();
						break;
					default:
						throw new InvalidOperationException($"Invalid animation ratio relator - {(byte) this.RatioType}");
				}

				if (lower_clamp.IsVector) return new EvaluatedResult(Vector4D.Lerp(lower_clamp.AsVector(), upper_clamp.AsVector(), ratio % 1));
				else return new EvaluatedResult(MathHelper.Lerp(lower_clamp.AsDouble(), upper_clamp.AsDouble(), ratio % 1));
			}
			else if (this.Operation == MathOperationEnum.CLAMP)
			{
				EvaluatedResult target_result = this.Arguments[0].Evaluate(arguments);
				EvaluatedResult lower_result = this.Arguments[1].Evaluate(arguments);
				EvaluatedResult upper_result = this.Arguments[2].Evaluate(arguments);

				if (target_result.IsVector) return new EvaluatedResult(Vector4D.Clamp(target_result.VectorResult, lower_result.AsVector(), upper_result.AsVector()));
				else return new EvaluatedResult(MathHelper.Clamp(target_result.DoubleResult, lower_result.AsDouble(), upper_result.AsDouble()));
			}
			else if (this.Arguments.Count == 1)
			{
				EvaluatedResult result = this.Arguments[0].Evaluate(arguments);

				switch (this.Operation)
				{
					case MathOperationEnum.ADD:
						return result;
					case MathOperationEnum.SUBTRACT:
						if (result.IsVector) return new EvaluatedResult(-result.VectorResult);
						else return new EvaluatedResult(-result.DoubleResult);
					case MathOperationEnum.FUNCTION:
						if (result.IsVector) return new EvaluatedResult(new Vector4D(
							this.Function(result.VectorResult.X),
							this.Function(result.VectorResult.Y),
							this.Function(result.VectorResult.Z),
							this.Function(result.VectorResult.W)
						));
						else return new EvaluatedResult(this.Function(result.DoubleResult));
					case MathOperationEnum.LENGTH:
						if (result.IsVector) return new EvaluatedResult(result.VectorResult.Length());
						else return result;
					case MathOperationEnum.VMIN:
						if (result.IsVector) result = new EvaluatedResult(Math.Min(Math.Min(result.VectorResult.X, result.VectorResult.Y), Math.Min(result.VectorResult.Z, result.VectorResult.W)));
						else return result;
						return result;
					case MathOperationEnum.VMAX:
						if (result.IsVector) return new EvaluatedResult(Math.Max(Math.Max(result.VectorResult.X, result.VectorResult.Y), Math.Max(result.VectorResult.Z, result.VectorResult.W)));
						else return result;
					default:
						throw new InvalidOperationException($"Invalid animation source - {(byte) this.SingleSourceValue}");
				}
			}
			else
			{
				List<EvaluatedResult> results = this.Arguments.Select((argument) => argument.Evaluate(arguments)).ToList();
				bool returns_vector = results.Any((result) => result.IsVector);
				Vector4D final;

				switch (this.Operation)
				{
					case MathOperationEnum.ADD:
						final = results[0].AsVector();
						foreach (EvaluatedResult result in results.Skip(1)) final += result.AsVector();
						break;
					case MathOperationEnum.SUBTRACT:
						final = results[0].AsVector();
						foreach (EvaluatedResult result in results.Skip(1)) final -= result.AsVector();
						break;
					case MathOperationEnum.MULTIPLY:
						final = results[0].AsVector();
						foreach (EvaluatedResult result in results.Skip(1)) final *= result.AsVector();
						break;
					case MathOperationEnum.DIVIDE:
						final = results[0].AsVector();
						foreach (EvaluatedResult result in results.Skip(1)) final /= result.AsVector();
						break;
					case MathOperationEnum.MODULO:
						final = results[0].AsVector();

						foreach (EvaluatedResult result in results.Skip(1))
						{
							Vector4D vector = result.AsVector();
							final = new Vector4D(final.X % vector.X, final.Y % vector.Y, final.Z % vector.Z, final.W % vector.W);
						}

						break;
					case MathOperationEnum.POWER:
						final = results[0].AsVector();

						foreach (EvaluatedResult result in results.Skip(1))
						{
							Vector4D vector = result.AsVector();
							final = new Vector4D(Math.Pow(final.X, vector.X), Math.Pow(final.Y, vector.Y), Math.Pow(final.Z, vector.Z), Math.Pow(final.W, vector.W));
						}

						break;
					case MathOperationEnum.AVERAGE:
						final = Vector4D.Zero;
						foreach (EvaluatedResult result in results) final += result.AsVector();
						final /= results.Count;
						break;
					case MathOperationEnum.SMALLEST:
						final = Vector4D.One;
						foreach (EvaluatedResult result in results) final = Vector4D.Min(final, result.AsVector());
						break;
					case MathOperationEnum.LARGEST:
						final = Vector4D.One;
						foreach (EvaluatedResult result in results) final = Vector4D.Max(final, result.AsVector());
						break;
					default:
						throw new InvalidOperationException($"Invalid animation source - {(byte) this.SingleSourceValue}");
				}

				return (returns_vector) ? new EvaluatedResult(final) : new EvaluatedResult(final.X);
			}
		}
	}

	[Serializable]
	[XmlRoot("DoubleKeyframe")]
	[ProtoContract(UseProtoMembersOnly = true)]
	public sealed class DoubleKeyframe
	{
		#region Internal Variables
		/// <summary>
		/// The value of this keyframe
		/// </summary>
		[ProtoMember(1)]
		[XmlElement("Expression")]
		public AnimationExpression Expression;

		/// <summary>
		/// The position of this keyframe<br />
		/// Relative to animation start
		/// </summary>
		[ProtoMember(2)]
		[XmlElement("Position")]
		public ushort Position;

		/// <summary>
		/// The interpolation method from this keyframe to the next
		/// </summary>
		[ProtoMember(3)]
		[XmlElement("EasingCurve")]
		public EasingCurveEnum EasingCurve = EasingCurveEnum.LINEAR;

		/// <summary>
		/// The easing method from this keyframe to the next
		/// </summary>
		[ProtoMember(4)]
		[XmlElement("EasingType")]
		public EasingTypeEnum EasingType = EasingTypeEnum.EASE_IN_OUT;
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

		#region Internal Methods
		internal double GetValueFromStack(AnimationExpression.ExpressionArguments arguments)
		{
			return this.Expression.Evaluate(arguments).AsDouble();
		}
		#endregion
	}

	[Serializable]
	[XmlRoot("VectorKeyframe")]
	[ProtoContract(UseProtoMembersOnly = true)]
	public sealed class VectorKeyframe
	{
		#region Internal Variables
		/// <summary>
		/// The value of this keyframe
		/// </summary>
		[ProtoMember(1)]
		[XmlElement("Expression")]
		public AnimationExpression Expression;

		/// <summary>
		/// The position of this keyframe<br />
		/// Relative to animation start
		/// </summary>
		[ProtoMember(2)]
		[XmlElement("Position")]
		public ushort Position;

		/// <summary>
		/// The interpolation method from this keyframe to the next
		/// </summary>
		[ProtoMember(3)]
		[XmlElement("EasingCurve")]
		public EasingCurveEnum EasingCurve = EasingCurveEnum.LINEAR;

		/// <summary>
		/// The easing method from this keyframe to the next
		/// </summary>
		[ProtoMember(4)]
		[XmlElement("EasingType")]
		public EasingTypeEnum EasingType = EasingTypeEnum.EASE_IN_OUT;
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

		#region Internal Methods
		internal Vector4D GetValueFromStack(AnimationExpression.ExpressionArguments arguments)
		{
			return this.Expression.Evaluate(arguments).AsVector();
		}
		#endregion
	}
}
