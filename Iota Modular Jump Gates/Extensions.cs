using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRageMath;

namespace IOTA.ModularJumpGates.Extensions
{
	internal static partial class Extensions
	{
		/// <summary>
		/// Converts a color to a Vector4D
		/// </summary>
		/// <param name="color">The color to convert</param>
		/// <returns>The converted Vector4D</returns>
		public static Vector4D ToVector4D(this Color color)
		{
			return new Vector4D(color.R / 255d, color.G / 255d, color.B / 255d, color.A / 255d);
		}

		/// <summary>
		/// Checks if this IEnumerable has at least "count" elements
		/// </summary>
		/// <typeparam name="T">The typename</typeparam>
		/// <param name="source">The IEnumerable</param>
		/// <param name="count">The minimum number of elements</param>
		/// <returns>Whether the sequence has at least the minimum number of elements</returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static bool AtLeast<T>(this IEnumerable<T> source, int count)
		{
			if (source == null) throw new ArgumentNullException("source");
			int found = 0;
			foreach (T item in source) if (++found == count) return true;
			return false;
		}

		/// <summary>
		/// Rounds this matrix to t he specified number of places
		/// </summary>
		/// <param name="matrix">This matrix</param>
		/// <param name="digits">The number of places to round to</param>
		/// <returns>The rounded matrix</returns>
		public static MatrixD Round(this MatrixD matrix, int digits = 0)
		{
			return new MatrixD(
				Math.Round(matrix.M11, digits), Math.Round(matrix.M12, digits), Math.Round(matrix.M13, digits), Math.Round(matrix.M14, digits),
				Math.Round(matrix.M21, digits), Math.Round(matrix.M22, digits), Math.Round(matrix.M23, digits), Math.Round(matrix.M24, digits),
				Math.Round(matrix.M31, digits), Math.Round(matrix.M32, digits), Math.Round(matrix.M33, digits), Math.Round(matrix.M34, digits),
				Math.Round(matrix.M41, digits), Math.Round(matrix.M42, digits), Math.Round(matrix.M43, digits), Math.Round(matrix.M44, digits)
			);
		}

		/// <summary>
		/// Applies a quaternion rotation to the specified matrix<br />
		/// Equivilent to "MatrixD.Transform" but takes a QuaternionD instead of a Quaternion
		/// </summary>
		/// <param name="value">The matrix to transform</param>
		/// <param name="rotation">The quaterion to rotate by</param>
		/// <returns>The transformed matrix</returns>
		public static MatrixD Transform(ref MatrixD value, ref QuaternionD rotation)
		{
			double num = rotation.X + rotation.X;
			double num2 = rotation.Y + rotation.Y;
			double num3 = rotation.Z + rotation.Z;
			double num4 = rotation.W * num;
			double num5 = rotation.W * num2;
			double num6 = rotation.W * num3;
			double num7 = rotation.X * num;
			double num8 = rotation.X * num2;
			double num9 = rotation.X * num3;
			double num10 = rotation.Y * num2;
			double num11 = rotation.Y * num3;
			double num12 = rotation.Z * num3;
			double num13 = 1.0 - num10 - num12;
			double num14 = num8 - num6;
			double num15 = num9 + num5;
			double num16 = num8 + num6;
			double num17 = 1.0 - num7 - num12;
			double num18 = num11 - num4;
			double num19 = num9 - num5;
			double num20 = num11 + num4;
			double num21 = 1.0 - num7 - num10;
			MatrixD result = default(MatrixD);
			result.M11 = value.M11 * num13 + value.M12 * num14 + value.M13 * num15;
			result.M12 = value.M11 * num16 + value.M12 * num17 + value.M13 * num18;
			result.M13 = value.M11 * num19 + value.M12 * num20 + value.M13 * num21;
			result.M14 = value.M14;
			result.M21 = value.M21 * num13 + value.M22 * num14 + value.M23 * num15;
			result.M22 = value.M21 * num16 + value.M22 * num17 + value.M23 * num18;
			result.M23 = value.M21 * num19 + value.M22 * num20 + value.M23 * num21;
			result.M24 = value.M24;
			result.M31 = value.M31 * num13 + value.M32 * num14 + value.M33 * num15;
			result.M32 = value.M31 * num16 + value.M32 * num17 + value.M33 * num18;
			result.M33 = value.M31 * num19 + value.M32 * num20 + value.M33 * num21;
			result.M34 = value.M34;
			result.M41 = value.M41 * num13 + value.M42 * num14 + value.M43 * num15;
			result.M42 = value.M41 * num16 + value.M42 * num17 + value.M43 * num18;
			result.M43 = value.M41 * num19 + value.M42 * num20 + value.M43 * num21;
			result.M44 = value.M44;
			return result;
		}
	}
}
